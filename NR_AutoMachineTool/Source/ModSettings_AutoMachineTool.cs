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
        public static readonly Func<BasicMachineSetting> PullerDefault = () => new BasicMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 200, maxSupplyPowerForSpeed = 10000 };

        public RangeMachineSetting gathererSetting = GathererDefault();
        public static readonly Func<RangeMachineSetting> GathererDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 20000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000 };

        public RangeMachineSetting slaughterSetting = SlaughterDefault();
        public static readonly Func<RangeMachineSetting> SlaughterDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 20000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000 };

        public BasicMachineSetting minerSetting = MinerDefault();
        public static readonly Func<BasicMachineSetting> MinerDefault = () => new BasicMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 10000, maxSupplyPowerForSpeed = 1000000 };

        public RangeMachineSetting cleanerSetting = CleanerDefault();
        public static readonly Func<RangeMachineSetting> CleanerDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 20000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 3000 };

        public RangeMachineSetting repairerSetting = RepairerDefault();
        public static readonly Func<RangeMachineSetting> RepairerDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 10000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 10000 };

        public RangeMachineSetting stunnerSetting = StunnerDefault();
        public static readonly Func<RangeMachineSetting> StunnerDefault = () => new RangeMachineSetting() { speedFactor = 1f, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 50000, minSupplyPowerForRange = 0, maxSupplyPowerForRange = 10000 };

        private List<RangeSkillMachineSetting> autoMachineToolSetting = CreateAutoMachineToolDefault();

        private static List<RangeSkillMachineSetting> CreateAutoMachineToolDefault()
        {
            return new List<RangeSkillMachineSetting> {
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 500, minSupplyPowerForSpeed = 100, maxSupplyPowerForSpeed = 1000, skillLevel = 5, speedFactor = 1f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 500, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 5000, skillLevel = 10, speedFactor = 1.5f },
                new RangeSkillMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1000, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 100000, skillLevel = 20, speedFactor = 2f }
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

        private List<RangeMachineSetting> harvesterSetting = CreateHarvesterDefault();

        private static List<RangeMachineSetting> CreateHarvesterDefault()
        {
            return new List<RangeMachineSetting> {
                new RangeMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 1000, minSupplyPowerForSpeed = 300, maxSupplyPowerForSpeed = 1000, speedFactor = 1f },
                new RangeMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 2000, minSupplyPowerForSpeed = 500, maxSupplyPowerForSpeed = 5000, speedFactor = 1.5f },
                new RangeMachineSetting() { minSupplyPowerForRange = 0, maxSupplyPowerForRange = 5000, minSupplyPowerForSpeed = 1000, maxSupplyPowerForSpeed = 10000, speedFactor = 2f }
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
            this.minerSetting = MinerDefault();
            this.cleanerSetting = CleanerDefault();
            this.repairerSetting = RepairerDefault();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.autoMachineToolSetting, "autoMachineToolSetting");
            Scribe_Collections.Look(ref this.planterSetting, "planterSetting");
            Scribe_Collections.Look(ref this.harvesterSetting, "harvesterSetting");

            Scribe_Deep.Look(ref this.beltConveyorSetting, "beltConveyorSetting");
            Scribe_Deep.Look(ref this.pullerSetting, "pullerSetting");
            Scribe_Deep.Look(ref this.gathererSetting, "gathererSetting");
            Scribe_Deep.Look(ref this.slaughterSetting, "slaughterSetting");
            Scribe_Deep.Look(ref this.minerSetting, "minerSetting");
            Scribe_Deep.Look(ref this.cleanerSetting, "cleanerSetting");
            Scribe_Deep.Look(ref this.repairerSetting, "repairerSetting");
            Scribe_Deep.Look(ref this.stunnerSetting, "stunnerSetting");

            this.autoMachineToolSetting = this.autoMachineToolSetting ?? CreateAutoMachineToolDefault();
            this.planterSetting = this.planterSetting ?? CreatePlanterDefault();
            this.harvesterSetting = this.harvesterSetting ?? CreateHarvesterDefault();

            this.beltConveyorSetting = this.beltConveyorSetting ?? BeltConveyorDefault();
            this.pullerSetting = this.pullerSetting ?? PullerDefault();
            this.gathererSetting = this.gathererSetting ?? GathererDefault();
            this.slaughterSetting = this.slaughterSetting ?? SlaughterDefault();
            this.minerSetting = this.minerSetting ?? MinerDefault();
            this.cleanerSetting = this.cleanerSetting ?? CleanerDefault();
            this.repairerSetting = this.repairerSetting?? RepairerDefault();
            this.stunnerSetting = this.stunnerSetting ?? StunnerDefault();

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

        public RangeMachineSetting HarvesterTier(int tier)
        {
            this.harvesterSetting = this.harvesterSetting ?? CreateHarvesterDefault();
            return this.harvesterSetting[tier - 1];
        }

        public event EventHandler DataExposed;

        private Vector2 scrollPosition;

        public void DoSetting(Rect inRect)
        {
            var tierMachines = new[] {
                new { Name= "NR_AutoMachineTool.AutoMachineTool", Setting = this.autoMachineToolSetting.Cast<BasicMachineSetting>() },
                new { Name= "NR_AutoMachineTool.Planter", Setting = this.planterSetting.Cast<BasicMachineSetting>() },
                new { Name= "NR_AutoMachineTool.Harvester", Setting = this.harvesterSetting.Cast<BasicMachineSetting>() },
            };

            var machines = new[]
            {
                new { Name="Building_NR_AutoMachineTool_BeltConveyor", Setting = (BasicMachineSetting)this.beltConveyorSetting},
                new { Name="Building_NR_AutoMachineTool_Puller", Setting = (BasicMachineSetting)this.pullerSetting},
                new { Name="Building_NR_AutoMachineTool_AnimalResourceGatherer", Setting = (BasicMachineSetting)this.gathererSetting},
                new { Name="Building_NR_AutoMachineTool_Slaughterhouse", Setting = (BasicMachineSetting)this.slaughterSetting},
                new { Name="Building_NR_AutoMachineTool_Miner", Setting = (BasicMachineSetting)this.minerSetting},
                new { Name="Building_NR_AutoMachineTool_Cleaner", Setting = (BasicMachineSetting)this.cleanerSetting},
                new { Name="Building_NR_AutoMachineTool_Repairer", Setting = (BasicMachineSetting)this.repairerSetting},
                new { Name="Building_NR_AutoMachineTool_Stunner", Setting = (BasicMachineSetting)this.stunnerSetting},
            };

            var width = inRect.width - 30f;

            var height =
                tierMachines.Select(a => Text.CalcHeight(a.Name.Translate(), width) + 12f + a.Setting.Select(s => s.GetHeight() + 42f + 12f).Sum()).Sum() +
                machines.Select(a => Text.CalcHeight(ThingDef.Named(a.Name).label, width) + a.Setting.GetHeight() + 12f).Sum() + 50f;

            var viewRect = new Rect(inRect.x, inRect.y, width, height);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);

            tierMachines.ForEach(a =>
            {
                int i = 0;
                DrawMachineName(a.Name.Translate(), list);
                a.Setting.ForEach(s => DrawTier(list, s, ++i));
                list.GapLine();
            });

            machines.ForEach(a =>
            {
                DrawMachineName(ThingDef.Named(a.Name).label, list);
                DrawSetting(list, a.Setting);
                list.GapLine();
            });

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
