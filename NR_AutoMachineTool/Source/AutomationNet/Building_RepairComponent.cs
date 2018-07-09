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
    public class Building_RepairComponent : Building_BaseComponent<Building>
    {
        private bool NeedRepair(Building b)
        {
            return b.HitPoints < b.MaxHitPoints;
        }

        private float prevAmount = 0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.prevAmount, "prevAmount", 0f);
        }

        protected override float WorkAmountPerTick => this.consumer.suppliedEnergy * 0.001f;

        protected override bool WorkInterruption(Building working)
        {
            if(working.HitPoints >= working.MaxHitPoints)
            {
                working.HitPoints = working.MaxHitPoints;
            }
            return !working.Spawned || working.HitPoints >= working.MaxHitPoints;
        }

        protected override Building TargetThing(out float workAmount)
        {
            workAmount = float.PositiveInfinity;
            return this.Position.GetThingList(this.Map)
                .SelectMany(t => Option(t as Building))
                .Where(NeedRepair).ToList()
                .OrderBy(b => b.TryGetComp<CompAutomation>() == null)
                .FirstOption()
                .GetOrDefault(null);
        }

        protected override bool TryStartWorking(out Building target, out float workAmount)
        {
            var result = base.TryStartWorking(out target, out workAmount);
            if(result) this.prevAmount = 0;
            return result;
        }

        protected override void WorkingTick(Building working, float workAmount)
        {
            var diff = workAmount - prevAmount;
            if (diff > 1f)
            {
                var change = Mathf.RoundToInt(diff);
                working.HitPoints += change;
                if (working.HitPoints >= working.MaxHitPoints)
                {
                    working.HitPoints = working.MaxHitPoints;
                }
                prevAmount += change;
            }
        }
    }
}
