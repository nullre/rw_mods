using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class Building_AutoMachineTool : Building_BaseRange<Building_AutoMachineTool>, IRecipeProductWorker
    {
        public class Bill_ProductionPawnForbidded : Bill_Production
        {
            public Bill_ProductionPawnForbidded() : base()
            {
            }

            public Bill_ProductionPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }
        }

        public class Bill_ProductionWithUftPawnForbidded : Bill_ProductionWithUft
        {
            public Bill_ProductionWithUftPawnForbidded() : base()
            {
            }

            public Bill_ProductionWithUftPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }
        }

        public Building_AutoMachineTool()
        {
            this.forcePlace = false;
        }

        private Map M { get { return this.Map; } }
        private IntVec3 P { get { return this.Position; } }

        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private UnfinishedThing unfinished;
        private int outputIndex = 0;
        private bool forbidItem = false;

        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();
        [Unsaved]
        private Option<Sustainer> workingSound = Nothing<Sustainer>();
        [Unsaved]
        private Option<Building_WorkTable> workTable;

        public int GetSkillLevel(SkillDef def)
        {
            return this.SkillLevel ?? 0;
        }

        private IntVec3[] adjacent =
        {
            new IntVec3(0, 0, 1),
            new IntVec3(1, 0, 1),
            new IntVec3(1, 0, 0),
            new IntVec3(1, 0, -1),
            new IntVec3(0, 0, -1),
            new IntVec3(-1, 0, -1),
            new IntVec3(-1, 0, 0),
            new IntVec3(-1, 0, 1)
        };
        private string[] adjacentName =
        {
            "N",
            "NE",
            "E",
            "SE",
            "S",
            "SW",
            "W",
            "NW"
        };

        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }

        protected override int? SkillLevel { get { return this.Setting.AutoMachineToolTier(Extension.tier).skillLevel; } }
        public override int MaxPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).maxSupplyPowerForSpeed; } }
        public override int MinPowerForSpeed { get { return this.Setting.AutoMachineToolTier(Extension.tier).minSupplyPowerForSpeed; } }
        protected override float SpeedFactor { get { return this.Setting.AutoMachineToolTier(Extension.tier).speedFactor; } }

        public override int MinPowerForRange => this.Setting.AutoMachineToolTier(Extension.tier).minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.AutoMachineToolTier(Extension.tier).maxSupplyPowerForRange;

        public override bool Glowable => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Values.Look<bool>(ref this.forbidItem, "forbidItem");

            Scribe_Deep.Look<UnfinishedThing>(ref this.unfinished, "unfinished");

            Scribe_References.Look<Bill>(ref this.bill, "bill");
            Scribe_References.Look<Thing>(ref this.dominant, "dominant");
            Scribe_Collections.Look<Thing>(ref this.ingredients, "ingredients", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.workTable = Nothing<Building_WorkTable>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.workTable.ForEach(this.AllowWorkTable);
            base.DeSpawn();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.WorkTableSetting();
        }

        protected override void Reset()
        {
            if (this.State == WorkingState.Working)
            {
                if (this.unfinished == null)
                {
                    this.ingredients.ForEach(t => GenPlace.TryPlaceThing(t, P, this.M, ThingPlaceMode.Near));
                }
                else 
                {
                    GenPlace.TryPlaceThing(this.unfinished, P, this.M, ThingPlaceMode.Near);
                    this.unfinished.Destroy(DestroyMode.Cancel);
                }
            }
            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            base.Reset();
        }

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();

            this.workingEffect.ForEach(e => e.Cleanup());
            this.workingEffect = Nothing<Effecter>();

            this.workingSound.ForEach(s => s.End());
            this.workingSound = Nothing<Sustainer>();

            MapManager.RemoveEachTickAction(this.EffectTick);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();
            
            this.workingEffect = this.workingEffect.Fold(() => Option(this.bill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e));

            this.workingSound = this.workingSound.Fold(() => this.workTable.SelectMany(t => Option(this.bill.recipe.soundWorking).Select(s => s.TrySpawnSustainer(t))))(s => Option(s))
                .Peek(s => s.Maintain());

            MapManager.EachTickAction(this.EffectTick);
        }

        protected bool EffectTick()
        {
            this.workingEffect.ForEach(e => this.workTable.ForEach(w => e.EffectTick(new TargetInfo(this), new TargetInfo(w))));
            return !this.workingEffect.HasValue;
        }

        private int billCount = 0;

        private void ForbidWorkTable(Building_WorkTable worktable, bool force)
        {
            if (billCount != worktable.BillStack.Count || Find.TickManager.TicksGame % 30 == 0 || force)
            {
                this.ReplaceBill<Bill_Production, Bill_ProductionWithUft, Bill_ProductionPawnForbidded, Bill_ProductionWithUftPawnForbidded>(worktable);
            }
            this.billCount = worktable.BillStack.Count;
        }

        private void AllowWorkTable(Building_WorkTable worktable)
        {
            this.ReplaceBill<Bill_ProductionPawnForbidded, Bill_ProductionWithUftPawnForbidded, Bill_Production, Bill_ProductionWithUft>(worktable);
        }

        private bool ReplaceBill<BILL_FROM, BILL_FROM_UFT, BILL_TO, BILL_TO_UFT>(Building_WorkTable worktable)
            where BILL_FROM : Bill_Production
            where BILL_FROM_UFT : Bill_ProductionWithUft
            where BILL_TO : Bill_Production
            where BILL_TO_UFT : Bill_ProductionWithUft
        {
            if (worktable.billStack.Bills.Any(b => b.GetType() == typeof(BILL_FROM) || b.GetType() == typeof(BILL_FROM_UFT)))
            {
                var tmp = new List<Bill>().Append(worktable.billStack.Bills);
                worktable.billStack.Clear();
                tmp.ForEach(b =>
                {
                    if (b is BILL_FROM_UFT)
                    {
                        worktable.billStack.AddBill(((BILL_FROM_UFT)b).CopyTo((BILL_TO_UFT)Activator.CreateInstance(typeof(BILL_TO_UFT), b.recipe)));
                        b.deleted = true;
                    }
                    else if (b is BILL_FROM)
                    {
                        worktable.billStack.AddBill(((BILL_FROM)b).CopyTo((BILL_TO)Activator.CreateInstance(typeof(BILL_TO), b.recipe)));
                        b.deleted = true;
                    }
                });
                return true;
            }
            return false;
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return this.workTable.GetOrDefault(null);
        }

        private void WorkTableSetting()
        {
            var currentWotkTable = this.GetTargetWorkTable();
            if(this.workTable.HasValue && !currentWotkTable.HasValue)
            {
                this.AllowWorkTable(this.workTable.Value);
            }
            currentWotkTable.ForEach(w => this.ForbidWorkTable(w, !this.workTable.HasValue));
            this.workTable = currentWotkTable;
        }

        protected override void Ready()
        {
            base.Ready();
            this.WorkTableSetting();
        }

        private IntVec3 FacingCell()
        {
            return this.Position + this.Rotation.FacingCell;
        }

        private Option<Building_WorkTable> GetTargetWorkTable()
        {
            return this.FacingCell().GetThingList(M)
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as Building_WorkTable))
                .Where(t => t.InteractionCell == this.Position)
                .FirstOption();
        }

        protected override bool TryStartWorking(out Building_AutoMachineTool target, out float workAmount)
        {
            target = this;
            workAmount = 0;
            if (!this.workTable.Where(t => t.CurrentlyUsableForBills() && t.billStack.AnyShouldDoNow).HasValue)
            {
                return false;
            }
            var consumable = Consumable();
            var result = WorkableBill(consumable).Select(tuple =>
            {
                this.bill = tuple.Value1;
//                tuple.Value2.Select(v => v.thing).SelectMany(t => Option(t as Corpse)).ForEach(c => c.Strip());
                this.ingredients = tuple.Value2.Select(t => t.thing.SplitOff(t.count)).ToList();
                this.dominant = this.DominantIngredient(this.ingredients);
                if (this.bill.recipe.UsesUnfinishedThing)
                {
                    ThingDef stuff = (!this.bill.recipe.unfinishedThingDef.MadeFromStuff) ? null : this.dominant.def;
                    this.unfinished = (UnfinishedThing)ThingMaker.MakeThing(this.bill.recipe.unfinishedThingDef, stuff);
                    this.unfinished.BoundBill = (Bill_ProductionWithUft)this.bill;
                    this.unfinished.ingredients = this.ingredients;
                    CompColorable compColorable = this.unfinished.TryGetComp<CompColorable>();
                    if (compColorable != null)
                    {
                        compColorable.Color = this.dominant.DrawColor;
                    }
                }
                return new { Result = true, WorkAmount = this.bill.recipe.WorkAmountTotal(this.bill.recipe.UsesUnfinishedThing ? this.dominant?.def : null) };
            }).GetOrDefault(new { Result = false, WorkAmount = 0f });
            workAmount = result.WorkAmount;
            return result.Result;
        }

        protected override bool FinishWorking(Building_AutoMachineTool working, out List<Thing> products)
        {
            products = GenRecipe2.MakeRecipeProducts(this.bill.recipe, this, this.ingredients, this.dominant, this.workTable.GetOrDefault(null)).ToList();
            this.ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, M));
            Option(this.unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            this.bill.Notify_IterationCompleted(null, this.ingredients);

            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.ingredients = null;
            return true;
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().SlotGroupCells(M);
        }

        public override IntVec3 OutputCell()
        {
            return this.Position + this.adjacent[this.outputIndex];
        }

        public IEnumerable<IntVec3> IngredientScanCell()
        {
            // this.CellsAdjacent8WayAndInside()
            return GenAdj.CellsOccupiedBy(this.Position, this.Rotation, this.def.Size + new IntVec2(this.GetRange() * 2, this.GetRange() * 2));
        }

        private List<Thing> Consumable()
        {
            return this.IngredientScanCell()
                .SelectMany(c => c.GetThingList(M))
                .Where(c => c.def.category == ThingCategory.Item)
                .ToList();
        }

        private Option<Tuple<Bill, List<ThingAmount>>> WorkableBill(List<Thing> consumable)
        {
            return this.workTable
                .Where(t => t.CurrentlyUsableForBills())
                .SelectMany(wt => wt.billStack.Bills
                    .Where(b => b.ShouldDoNow())
                    .Where(b => b.recipe.AvailableNow)
                    .Where(b => Option(b.recipe.skillRequirements).Fold(true)(s => s.Where(x => x != null).All(r => r.minLevel <= this.GetSkillLevel(r.skill))))
                    .Select(b => Tuple(b, Ingredients(b, consumable)))
                    .Where(t => t.Value1.recipe.ingredients.Count == 0 || t.Value2.Count > 0)
                    .FirstOption()
                );
        }

        private struct ThingDefGroup
        {
            public ThingDef def;
            public List<ThingAmount> consumable;
        }

        private List<ThingAmount> Ingredients(Bill bill, List<Thing> consumable)
        {
            var initial = consumable
//                .Where(c => bill.IsFixedOrAllowedIngredient(c))
                .Select(x => new ThingAmount(x, x.stackCount))
                .ToList();

            Func<List<ThingAmount>, List<ThingDefGroup>> grouping = (consumableAmounts) =>
                consumableAmounts
                    .GroupBy(c => c.thing.def)
                    .Select(c => new { Def = c.Key, Count = c.Sum(t => t.count), Amounts = c.Select(t => t) })
                    .OrderByDescending(g => g.Def.IsStuff)
                    .ThenByDescending(g => g.Count * bill.recipe.IngredientValueGetter.ValuePerUnitOf(g.Def))
                    .Select(g => new ThingDefGroup() { def = g.Def, consumable = g.Amounts.ToList() })
                    .ToList();

            var grouped = grouping(initial);

            var ingredients = bill.recipe.ingredients.Select(i =>
            {
                var result = new List<ThingAmount>();
                float remain = i.GetBaseCount();

                foreach (var things in grouped)
                {
                    foreach (var amount in things.consumable)
                    {
                        var thing = amount.thing;
                        if (i.filter.Allows(thing) && (bill.ingredientFilter.Allows(thing) || i.IsFixedIngredient))
                        {
                            remain = remain - bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def) * amount.count;
                            int consumption = amount.count;
                            if (remain <= 0.0f)
                            {
                                consumption -= Mathf.RoundToInt(-remain / bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def));
                                remain = 0.0f;
                            }
                            result.Add(new ThingAmount(thing, consumption));
                        }
                        if (remain <= 0.0f)
                            break;
                    }
                    if (remain <= 0.0f)
                        break;

                    if ((things.def.IsStuff && bill.recipe.productHasIngredientStuff) || !bill.recipe.allowMixingIngredients)
                    {
                        // ミックスしたり、stuffの場合には、一つの要求素材に複数種類のものを混ぜられない.
                        // なので、この種類では満たせなかったので、残りを戻して、中途半端に入った利用予定を空にする.
                        remain = i.GetBaseCount();
                        result.Clear();
                    }
                }

                if (remain <= 0.0f)
                {
                    // 残りがなく、必要分が全て割り当てられれば、割り当てた分を減らして、その状態でソートして割り当て分を返す.
                    result.ForEach(r =>
                    {
                        var list = grouped.Find(x => x.def == r.thing.def).consumable;
                        var c = list.Find(x => x.thing == r.thing);
                        list.Remove(c);
                        c.count = c.count - r.count;
                        list.Add(c);
                    });
                    grouped = grouping(grouped.SelectMany(x => x.consumable).ToList());
                    return result;
                }
                else
                {
                    // 割り当てできなければ、空リスト.
                    return new List<ThingAmount>();
                }
            }).ToList();

            if (ingredients.All(x => x.Count > 0))
            {
                return ingredients.SelectMany(c => c).ToList();
            }
            else
            {
                return new List<ThingAmount>();
            }
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            return base.GetInspectTabs();
        }

        private Thing DominantIngredient(List<Thing> ingredients)
        {
            if (ingredients.Count == 0)
            {
                return null;
            }
            if (this.bill.recipe.productHasIngredientStuff)
            {
                return ingredients[0];
            }
            if (this.bill.recipe.products.Any(x => x.thingDef.MadeFromStuff))
            {
                return ingredients.Where(x => x.def.IsStuff).RandomElementByWeight((Thing x) => (float)x.stackCount);
            }
            return ingredients.RandomElementByWeight((Thing x) => (float)x.stackCount);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var direction = new Command_Action();
            direction.action = () =>
            {
                if (this.outputIndex + 1 >= this.adjacent.Count())
                {
                    this.outputIndex = 0;
                }
                else
                {
                    this.outputIndex++;
                }
            };
            direction.activateSound = SoundDefOf.Designate_AreaAdd;
            direction.defaultLabel = "NR_AutoMachineTool.SelectOutputDirectionLabel".Translate();
            direction.defaultDesc = "NR_AutoMachineTool.SelectOutputDirectionDesc".Translate();
            direction.icon = RS.OutputDirectionIcon;
            yield return direction;

            var forb = new Command_Toggle();
            forb.isActive = () => this.forbidItem;
            forb.toggleAction = () => this.forbidItem = !this.forbidItem;
            forb.defaultLabel = "NR_AutoMachineTool.ForbidOutputItemLabel".Translate();
            forb.defaultDesc = "NR_AutoMachineTool.ForbidOutputItemDesc".Translate();
            forb.icon = RS.ForbidIcon;
            yield return forb;
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            msg += "NR_AutoMachineTool.OutputDirection".Translate(("NR_AutoMachineTool.OutputDirection" + this.adjacentName[this.outputIndex]).Translate());
            return msg;
        }

        public override int GetRange()
        {
            return Mathf.RoundToInt(this.SupplyPowerForRange / 500) + 1;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        protected override bool WorkInterruption(Building_AutoMachineTool working)
        {
            if (!this.workTable.HasValue)
            {
                return true;
            }
            var currentTable = GetTargetWorkTable();
            if (!currentTable.HasValue)
            {
                return true;
            }
            if(currentTable.Value != this.workTable.Value)
            {
                return true;
            }
            return !this.workTable.Value.CurrentlyUsableForBills();
        }

        private class ThingAmount
        {
            public ThingAmount(Thing thing, int count)
            {
                this.thing = thing;
                this.count = count;
            }

            public Thing thing;

            public int count;
        }
    }
}
