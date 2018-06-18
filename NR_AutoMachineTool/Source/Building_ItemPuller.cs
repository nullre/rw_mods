using System;
using System.IO;
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
    public class Building_ItemPuller : Building_BaseLimitation<Thing>
    {
        protected override float SpeedFactor { get => this.Setting.pullerSetting.speedFactor; }
        public override int MinPowerForSpeed { get => this.Setting.pullerSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.pullerSetting.maxSupplyPowerForSpeed; }

        protected virtual int PullCount { get => Math.Max(Mathf.RoundToInt(this.SupplyPowerForSpeed / 100f), 1); }

        public ThingFilter Filter { get => this.filter; }

        private ThingFilter filter = new ThingFilter();
        private bool active = false;
        public override Graphic Graphic => Option(base.Graphic as Graphic_Selectable).Fold(base.Graphic)(g => g.Get("NR_AutoMachineTool/Buildings/Puller/Puller" + (this.active ? "1" : "0")));

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look<ThingFilter>(ref this.filter, "filter");
            Scribe_Values.Look<bool>(ref this.active, "active", false);

            if (this.filter == null) this.filter = new ThingFilter();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.filter = new ThingFilter();
                this.filter.SetAllowAll(null);
            }
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return this;
        }

        private Option<Thing> TargetThing()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell).SlotGroupCells(this.Map)
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => this.filter.Allows(t))
                .Where(t => !this.IsLimit(t))
                .FirstOption();
        }

        public override IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.FacingCell);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var act = new Command_Toggle();
            act.isActive = () => this.active;
            act.toggleAction = () => this.active = !this.active;
            act.defaultLabel = "NR_AutoMachineTool_Puller.SwitchActiveLabel".Translate();
            act.defaultDesc = "NR_AutoMachineTool_Puller.SwitchActiveDesc".Translate();
            act.icon = RS.PlayIcon;
            yield return act;
        }

        protected override bool IsActive()
        {
            return base.IsActive() && this.active;
        }

        protected override float GetTotalWorkAmount(Thing working)
        {
            return Math.Min(working.stackCount, PullCount) * 10f;
        }

        protected override bool WorkIntrruption(Thing working)
        {
            return !working.Spawned || working.Destroyed;
        }

        protected override bool TryStartWorking(out Thing target)
        {
            target = TargetThing().GetOrDefault(null);
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            var target = new List<Thing>();
            target.Append(working.SplitOff(Math.Min(working.stackCount, this.PullCount)));
            products = target;
            return true;
        }
    }
}
