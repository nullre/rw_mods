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
    public class Building_AutoMachineTool : Building
    {
        private enum WorkingState
        {
            Ready,
            Working,
            Placing
        }

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

        private Map M { get { return this.Map; } }
        private IntVec3 P { get { return this.Position; } }

        private WorkingState state = WorkingState.Ready;
        private float workLeft = 0;
        private float workAmount = 0;
        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private List<Thing> products;
        private UnfinishedThing unfinished;
        private int outputIndex = 0;
        private float supplyPower = 0;
        private bool forbidItem = false;

        [Unsaved]
        private Effecter progressBar;
        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();
        [Unsaved]
        private Option<Sustainer> workingSound = Nothing<Sustainer>();
        [Unsaved]
        private bool checkNext = true;
        [Unsaved]
        private Option<Building_WorkTable> workTable;

        private int GetSkillLevel(SkillDef def)
        {
            return this.SkillLevel;
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

        private ModSetting_AutoMachineTool Setting { get { return LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; } }
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }

        private int SkillLevel { get { return this.Setting.Tier(Extension.tier).skillLevel; } }
        public int MaxPower { get { return this.Setting.Tier(Extension.tier).maxSupplyPower; } }
        public int MinPower { get { return this.Setting.Tier(Extension.tier).minSupplyPower; } }
        private float SpeedFactor { get { return this.Setting.Tier(Extension.tier).speedFactor; } }

        public float SupplyPower
        {
            get
            {
                return -this.supplyPower;
            }

            set
            {
                this.supplyPower = -value;
                this.TryGetComp<CompPowerTrader>().PowerOutput = this.supplyPower;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<WorkingState>(ref this.state, "state");
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft");
            Scribe_Values.Look<float>(ref this.workAmount, "workAmount");
            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Values.Look<float>(ref this.supplyPower, "supplyPower", this.MinPower);
            Scribe_Values.Look<bool>(ref this.forbidItem, "forbidItem");

            Scribe_Deep.Look<UnfinishedThing>(ref this.unfinished, "unfinished");

            Scribe_References.Look<Bill>(ref this.bill, "bill");
            Scribe_References.Look<Thing>(ref this.dominant, "dominant");
            Scribe_Collections.Look<Thing>(ref this.ingredients, "ingredients", LookMode.Deep);
            Scribe_Collections.Look<Thing>(ref this.products, "products", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.workTable = Nothing<Building_WorkTable>();
            this.WorkTableSetting();

            if (!respawningAfterLoad)
            {
                this.outputIndex = this.adjacent.ToList().FindIndex(x => x == this.Rotation.FacingCell * -1);
                this.supplyPower = -this.MinPower;
            }
            else
            {
                this.ReloadSettings(this, new EventArgs());
            }
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (-this.supplyPower < this.MinPower)
            {
                this.supplyPower = -this.MinPower;
            }
            if (-this.supplyPower > this.MaxPower)
            {
                this.supplyPower = -this.MaxPower;
            }
        }

        public override void DeSpawn()
        {
            Reset(M);
            this.workTable.ForEach(this.AllowWorkTable);
            base.DeSpawn();
        }

        public override void Destroy(DestroyMode mode)
        {
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;

            base.Destroy(mode);
        }

        private void Reset(Map map)
        {
            this.CleanupProgressBar();
            this.CleanupWorkingEffect();
            if (this.state == WorkingState.Working)
            {
                if (this.unfinished == null)
                {
                    this.ingredients.ForEach(t => GenPlace.TryPlaceThing(t, P, map, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(this.unfinished, P, map, ThingPlaceMode.Near);
                    this.unfinished.Destroy(DestroyMode.Cancel);
                }
            }
            else if (this.state == WorkingState.Placing)
            {
                this.products.ForEach(t => GenPlace.TryPlaceThing(t, P, map, ThingPlaceMode.Near));
            }
            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.state = WorkingState.Ready;
            this.workLeft = 0.0f;
            this.workAmount = 0.0f;
            this.ingredients = null;
            this.products = null;
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
            this.workTable.ForEach(wt => this.progressBar.EffectTick(wt, TargetInfo.Invalid));
            Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (this.workAmount - this.workLeft) / this.workAmount);
        }

        private void CleanupWorkingEffect()
        {
            this.workingEffect.ForEach(e => e.Cleanup());
            this.workingEffect = Nothing<Effecter>();

            this.workingSound.ForEach(s => s.End());
            this.workingSound = Nothing<Sustainer>();
        }

        private void UpdateWorkingEffect()
        {
            this.workingEffect = this.workingEffect.Fold(() => Option(this.bill.recipe.effectWorking).Select(e => e.Spawn()))(e => Option(e))
                .Peek(e => this.workTable.ForEach(w => e.EffectTick(new TargetInfo(this), new TargetInfo(w))));

            this.workingSound = this.workingSound.Fold(() => this.workTable.SelectMany(t => Option(this.bill.recipe.soundWorking).Select(s => s.TrySpawnSustainer(t))))(s => Option(s))
                .Peek(s => s.Maintain());
        }

        private bool IsActive()
        {
            if (this.TryGetComp<CompPowerTrader>() == null || !this.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return false;
            }
            if (this.Destroyed)
            {
                return false;
            }

            return true;
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

        private void WorkTableSetting()
        {
            var currentWotkTable = this.GetTargetWorkTable();
            if(this.workTable.HasValue && !currentWotkTable.HasValue)
            {
                this.AllowWorkTable(this.workTable.Value);
                this.Reset(M);
            }
            currentWotkTable.ForEach(w => this.ForbidWorkTable(w, !this.workTable.HasValue));
            this.workTable = currentWotkTable;
        }

        public override void Tick()
        {
            base.Tick();

            if (this.supplyPower != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = this.supplyPower;
            }

            this.WorkTableSetting();

            if (!this.IsActive())
            {
                this.CleanupWorkingEffect();
                return;
            }

            if (this.state == WorkingState.Ready)
            {
                this.CleanupProgressBar();
                if (Find.TickManager.TicksGame % 30 == 0 || this.checkNext)
                {
                    this.TryStartWorking();
                    this.checkNext = false;
                }
            }
            else if (this.state == WorkingState.Working)
            {
                this.UpdateProgressBar();
                this.UpdateWorkingEffect();

                this.workTable.ForEach(w => w.UsedThisTick());

                if (this.workTable.Fold(false)(t => t.UsableNow))
                {
                    this.workLeft -= (-this.supplyPower / 1000.0f) * this.SpeedFactor;
                    Option(this.unfinished).ForEach(u => u.workLeft = this.workLeft);
                }

                if (this.workLeft <= 0)
                {
                    this.workLeft = 0;
                    this.FinishWorking();
                    this.checkNext = true;
                }
            }
            else if (this.state == WorkingState.Placing)
            {
                this.CleanupWorkingEffect();
                if (Find.TickManager.TicksGame % 30 == 0 || this.checkNext)
                {
                    this.checkNext = this.PlaceProducts();
                }
            }
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
                .FirstOption();
        }

        private void TryStartWorking()
        {
            var consumable = Consumable();
            WorkableBill(consumable).ForEach(tuple =>
            {
                this.bill = tuple.Value1;
                tuple.Value2.Select(v => v.thing).SelectMany(t => Option(t as Corpse)).ForEach(c => c.Strip());
                this.ingredients = tuple.Value2.Select(t => t.thing.SplitOff(t.count)).ToList();
                this.dominant = this.DominantIngredient(this.ingredients);
                this.state = WorkingState.Working;
                this.workAmount = this.bill.recipe.WorkAmountTotal(this.bill.recipe.UsesUnfinishedThing ? this.dominant.def : null);
                this.workLeft = this.workAmount;
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
            });
        }

        private void FinishWorking()
        {
            this.products = GenRecipe2.MakeRecipeProducts(this.bill.recipe, P, M, P.GetRoom(M), this.GetSkillLevel, this.ingredients, this.dominant).ToList();
            this.ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, M));
            Option(this.unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            this.bill.Notify_IterationCompleted(null, this.ingredients);

            this.state = WorkingState.Placing;
            this.bill = null;
            this.dominant = null;
            this.unfinished = null;
            this.workLeft = 0.0f;
            this.workAmount = 0.0f;
            this.ingredients = null;
        }

        private bool PlaceProducts()
        {
            this.products = this.products.Aggregate(new List<Thing>(), (t, n) =>
            {
                var conveyor = this.OutputCell().GetThingList(M).Where(b => b.def.category == ThingCategory.Building).SelectMany(b => Option(b as Building_BeltConveyor)).FirstOption();
                if (conveyor.HasValue)
                {
                    // ベルトコンベアがある場合には、そっちに渡す.
                    if (conveyor.Value.Acceptable())
                    {
                        conveyor.Value.TryStartCarry(n);
                        return t;
                    }
                }
                else
                {
                    // ない場合には、適当に出す.
                    var cells = OutputZone(OutputCell());
                    if (PlaceItem(n, OutputCell(), this.forbidItem, M))
                    {
                        return t;
                    }
                }
                return t.Append(n);
            });
            bool result = this.products.Count == 0;
            if (result)
            {
                this.state = WorkingState.Ready;
                this.Reset(M);
            }
            return result;
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().ZoneCells(M);
        }

        public IntVec3 OutputCell()
        {
            return this.Position + this.adjacent[this.outputIndex];
        }

        public List<IntVec3> OutputZone(IntVec3 outputCell)
        {
            return Option(OutputCell().GetZone(M) as RimWorld.Zone_Stockpile).Select(z => z.cells).GetOrDefault(new List<IntVec3>().Append(OutputCell()));
        }

        private List<Thing> Consumable()
        {
            return this.CellsAdjacent8WayAndInside()
                .SelectMany(c => c.GetThingList(M))
                .Where(c => c.def.category == ThingCategory.Item)
                .ToList();
        }

        private Option<Tuple<Bill, List<ThingAmount>>> WorkableBill(List<Thing> consumable)
        {
            return this.workTable
                .Where(t => t.UsableNow)
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
            if (this.bill.recipe.products.Any((ThingCountClass x) => x.thingDef.MadeFromStuff))
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
            direction.activateSound = SoundDefOf.DesignateAreaAdd;
            direction.defaultLabel = "NR_AutoMachineTool.SelectOutputDirectionLabel".Translate();
            direction.defaultDesc = "NR_AutoMachineTool.SelectOutputDirectionDesc".Translate();
            direction.icon = ContentFinder<Texture2D>.Get("NR_AutoMachineTool/UI/OutputDirection", true);
            yield return direction;

            var forb = new Command_Toggle();
            forb.isActive = () => this.forbidItem;
            forb.toggleAction = () => this.forbidItem = !this.forbidItem;
            forb.defaultLabel = "NR_AutoMachineTool.ForbidOutputItemLabel".Translate();
            forb.defaultDesc = "NR_AutoMachineTool.ForbidOutputItemDesc".Translate();
            forb.icon = ContentFinder<Texture2D>.Get("NR_AutoMachineTool/UI/Forbid", true);
            yield return forb;
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.state)
            {
                case WorkingState.Working:
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.workAmount - this.workLeft), Mathf.RoundToInt(this.workAmount), Mathf.RoundToInt(((this.workAmount - this.workLeft) / this.workAmount) * 100));
                    break;
                case WorkingState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(this.products.Count());
                    break;
                default:
                    msg += this.state.ToString();
                    break;
            }
            msg += "\n";
            msg += "NR_AutoMachineTool.OutputDirection".Translate(("NR_AutoMachineTool.OutputDirection" + this.adjacentName[this.outputIndex]).Translate());
            msg += "\n";
            msg += "NR_AutoMachineTool.SkillLevel".Translate(this.SkillLevel.ToString());
            return msg;
        }
    }
}
