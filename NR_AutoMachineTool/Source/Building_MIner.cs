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
    public class Building_Miner : Building_BaseMachine<Building_Miner>, IBillGiver, IRecipeProductWorker, ITabBillTable
    {
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        protected override float SpeedFactor { get => this.Setting.minerSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.minerSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.minerSetting.maxSupplyPowerForSpeed; }

        public BillStack BillStack => this.billStack;

        public IEnumerable<IntVec3> IngredientStackCells => Enumerable.Empty<IntVec3>();

        ThingDef ITabBillTable.def => this.def;

        BillStack ITabBillTable.billStack => this.BillStack;

        public BillStack billStack;

        public Building_Miner()
        {
            this.billStack = new BillStack(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.outputIndex, "outputIndex");
            Scribe_Deep.Look(ref this.billStack, "billStack", new object[]{ this });
            Scribe_References.Look(ref this.doingBill, "doingBill");
        }

        protected override float GetTotalWorkAmount(Building_Miner working)
        {
            return this.doingBill.recipe.workAmount;
        }

        protected override bool WorkIntrruption(Building_Miner working)
        {
            return !this.doingBill.ShouldDoNow();
        }

        private Bill doingBill;

        protected override bool TryStartWorking(out Building_Miner target)
        {
            target = this;
            if (this.billStack.AnyShouldDoNow)
            {
                this.doingBill = this.billStack.FirstShouldDoNow;
                return true;
            }
            return false;
        }

        protected override bool FinishWorking(Building_Miner working, out List<Thing> products)
        {
            products = GenRecipe2.MakeRecipeProducts(this.doingBill.recipe, this, new List<Thing>(), null, this).ToList();
            return true;
        }

        public bool CurrentlyUsableForBills()
        {
            return false;
        }

        public bool UsableForBillsAfterFueling()
        {
            return false;
        }

        public Room GetRoom(RegionType type)
        {
            return RegionAndRoomQuery.GetRoom(this, type);
        }

        public int GetSkillLevel(SkillDef def)
        {
            return 20;
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
        }

        private int outputIndex = 0;

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

        public override IntVec3 OutputCell()
        {
            return this.Position + this.adjacent[this.outputIndex];
        }
    }

    [StaticConstructorOnStartup]
    public static class RecipeRegister
    {
        static RecipeRegister()
        {
            var recipeDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.deepCommonality > 0f && d.deepCountPerCell > 0)
                .Select(d => CreateMineRecipe(d)).ToList();
            DefDatabase<RecipeDef>.Add(recipeDefs);
            DefDatabase<ThingDef>.GetNamed("Building_NR_AutoMachineTool_Miner").recipes = recipeDefs;
        }

        private static RecipeDef CreateMineRecipe(ThingDef def)
        {
            RecipeDef r = new RecipeDef();
            r.defName = "Recipe_NR_AutoMachineTool_Mine_" + def.defName;
            r.label = "NR_AutoMachineTool.AutoMiner.MineOre".Translate(def.label);
            r.jobString = "NR_AutoMachineTool.AutoMiner.MineOre".Translate(def.label);
            r.workAmount = StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, null)) * def.deepCountPerCell * 1000;
            r.workSpeedStat = StatDefOf.WorkToMake;
            r.efficiencyStat = StatDefOf.WorkToMake;

            r.workSkill = SkillDefOf.Crafting;
            r.workSkillLearnFactor = 0;

            r.products = new List<ThingDefCountClass>().Append(new ThingDefCountClass(def, def.deepCountPerCell));
            r.defaultIngredientFilter = new ThingFilter();

            return r;
        }
    }
}
