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
    public class ModSetting_AutoMachineTool : ModSettings
    {
        public BasicMachineSetting beltConveyorSetting = BeltConveyorDefault();
        public static readonly Func<BasicMachineSetting> BeltConveyorDefault = () => new BasicMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 10, maxSupplyPowerForSpeed = 100 };

        public BasicMachineSetting pullerSetting = PullerDefault();
        public static readonly Func<BasicMachineSetting> PullerDefault = () => new BasicMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 10000 };

        public RangeMachineSetting gathererSetting = GathererDefault();
        public static readonly Func<RangeMachineSetting> GathererDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 20000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000 };

        public RangeMachineSetting slaughterSetting = SlaughterDefault();
        public static readonly Func<RangeMachineSetting> SlaughterDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 20000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000 };

        private List<RangeSkillMachineSetting> autoMachineToolSetting = CreateAutoMachineToolDefault();

        private static List<RangeSkillMachineSetting> CreateAutoMachineToolDefault()
        {
            return new List<RangeSkillMachineSetting> {
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1000, minSupplyPowerForSpeed = 100, maxSupplyPowerForSpeed = 1000, skillLevel = 5, speedFactor = 1f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1500, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 5000, skillLevel = 10, speedFactor = 1.5f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2500, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 100000, skillLevel = 20, speedFactor = 2f }
            };
        }

        private List<RangeSkillMachineSetting> planterSetting = CreatePlanterDefault();

        private static List<RangeSkillMachineSetting> CreatePlanterDefault()
        {
            return new List<RangeSkillMachineSetting> {
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1000, minSupplyPowerForSpeed = 300, maxSupplyPowerForSpeed = 1000, skillLevel = 5, speedFactor = 1f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 5000, skillLevel = 10, speedFactor = 1.5f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 5000, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 10000, skillLevel = 20, speedFactor = 2f }
            };
        }

        private List<RangeSkillMachineSetting> harvesterSetting = CreateHarvesterDefault();

        private static List<RangeSkillMachineSetting> CreateHarvesterDefault()
        {
            return new List<RangeSkillMachineSetting> {
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1000, minSupplyPowerForSpeed = 300, maxSupplyPowerForSpeed = 1000, skillLevel = 0, speedFactor = 1f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 5000, skillLevel = 0, speedFactor = 1.5f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 5000, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 10000, skillLevel = 0, speedFactor = 2f }
            };
        }

        public void RestoreDefault()
        {
            this.autoMachineToolSetting = CreateAutoMachineToolDefault();
            this.planterSetting = CreatePlanterDefault();
            this.harvesterSetting = CreateHarvesterDefault();

            this.beltConveyorSetting = BeltConveyorDefault();
            this.pullerSetting = PullerDefault();
            this.gathererSetting = GathererDefault();
            this.slaughterSetting = SlaughterDefault();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<RangeSkillMachineSetting>(ref this.autoMachineToolSetting, "autoMachineToolSetting");
            Scribe_Collections.Look<RangeSkillMachineSetting>(ref this.planterSetting, "planterSetting");
            Scribe_Collections.Look<RangeSkillMachineSetting>(ref this.harvesterSetting, "harvesterSetting");

            Scribe_Deep.Look<BasicMachineSetting>(ref this.beltConveyorSetting, "beltConveyorSetting");
            Scribe_Deep.Look<BasicMachineSetting>(ref this.pullerSetting, "pullerSetting");
            Scribe_Deep.Look<RangeMachineSetting>(ref this.gathererSetting, "gathererSetting");
            Scribe_Deep.Look<RangeMachineSetting>(ref this.slaughterSetting, "slaughterSetting");

            this.autoMachineToolSetting = this.autoMachineToolSetting ?? CreateAutoMachineToolDefault();
            this.planterSetting = this.planterSetting ?? CreatePlanterDefault();
            this.harvesterSetting = this.harvesterSetting ?? CreateHarvesterDefault();

            this.beltConveyorSetting = this.beltConveyorSetting ?? BeltConveyorDefault();
            this.pullerSetting = this.pullerSetting ?? PullerDefault();
            this.gathererSetting = this.gathererSetting ?? GathererDefault();
            this.slaughterSetting = this.slaughterSetting ?? SlaughterDefault();

            Option(this.DataExposed).ForEach(e => e(this, new EventArgs()));
        }

        public RangeSkillMachineSetting AutoMachineToolTier(int tier)
        {
            this.autoMachineToolSetting = this.autoMachineToolSetting ?? CreateAutoMachineToolDefault();
            return this.autoMachineToolSetting[tier - 1];
        }

        public RangeSkillMachineSetting PlanterTier(int tier)
        {
            this.planterSetting = this.planterSetting ?? CreatePlanterDefault();
            return this.planterSetting[tier - 1];
        }

        public RangeSkillMachineSetting HarvesterTier(int tier)
        {
            this.harvesterSetting = this.harvesterSetting ?? CreateHarvesterDefault();
            return this.planterSetting[tier - 1];
        }

        public event EventHandler DataExposed;

        private Vector2 scrollPosition;

        public void DoSetting(Rect inRect)
        {
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 30f, 3800f);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);

            int index = 0;
            DrawMachineName("NR_AutoMachineTool.AutoMachineTool".Translate(), list);
            this.autoMachineToolSetting.ForEach(s => DrawTier(list, s, ++index));
            list.GapLine();

            index = 0;
            DrawMachineName("NR_AutoMachineTool.Planter".Translate(), list);
            this.planterSetting.ForEach(s => DrawTier(list, s, ++index));
            list.GapLine();

            index = 0;
            DrawMachineName("NR_AutoMachineTool.Harvester".Translate(), list);
            this.harvesterSetting.ForEach(s => DrawTier(list, s, ++index));
            list.GapLine();

            DrawMachineName(ThingDef.Named("Building_NR_AutoMachineTool_BeltConveyor").label, list);
            DrawSetting(list, this.beltConveyorSetting);
            list.GapLine();

            DrawMachineName(ThingDef.Named("Building_NR_AutoMachineTool_Puller").label, list);
            DrawSetting(list, this.pullerSetting);
            list.GapLine();

            DrawMachineName(ThingDef.Named("Building_NR_AutoMachineTool_AnimalResourceGatherer").label, list);
            DrawSetting(list, this.gathererSetting);
            list.GapLine();

            DrawMachineName(ThingDef.Named("Building_NR_AutoMachineTool_Slaughterhouse").label, list);
            DrawSetting(list, this.slaughterSetting);
            list.GapLine();

            // Restore
            if (Widgets.ButtonText(list.GetRect(30f).RightHalf().RightHalf(), "NR_AutoMachineTool.SettingReset".Translate()))
            {
                this.RestoreDefault();
            }

            list.End();
            Widgets.EndScrollView();
        }

        private void DrawTier(Listing_Standard list, BasicMachineSetting s, int tier)
        {
            var rect = list.GetRect(s.GetHeight() + 42f);
            var inList = new Listing_Standard();
            inList.Begin(rect.RightPartPixels(rect.width - 50f));
            Text.Font = GameFont.Medium;
            Widgets.Label(inList.GetRect(30f), "Tier " + tier);
            inList.GapLine();
            Text.Font = GameFont.Small;
            s.DrawModSetting(inList);
            inList.End();
        }

        private void DrawSetting(Listing_Standard list, BasicMachineSetting s)
        {
            var rect = list.GetRect(s.GetHeight());
            var inList = new Listing_Standard();
            inList.Begin(rect.RightPartPixels(rect.width - 50f));
            s.DrawModSetting(inList);
            inList.End();
        }

        private void DrawMachineName(string name, Listing_Standard list)
        {
            var f = Text.Font;
            Text.Font = GameFont.Medium;
            list.Label(name);
            Text.Font = f;
        }
    }
}
