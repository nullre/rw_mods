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
    public abstract class Building_BaseComponent<T> : Building_BaseAutomation<T> where T : Thing
    {
        protected CompAutomationEnergyConsumer trader;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.trader = this.TryGetComp<CompAutomationEnergyConsumer>();
            this.showProgressBar = false;
        }

        protected override bool IsActive()
        {
            return base.IsActive();
        }

        protected override bool TryStartWorking(out T target)
        {
            var reuslt = (target = TargetThing()) != null;

            if (reuslt)
            {
                this.trader.RequestEnergy();
            }
            return reuslt;
        }

        protected abstract T TargetThing();

        private bool NeedExtinguish(T b)
        {
            return b.GetAttachment(ThingDefOf.Fire) != null;
        }

        private bool NeedRepair(T b)
        {
            return b.HitPoints < b.MaxHitPoints;
        }

        protected override bool FinishWorking(T working, out List<Thing> products)
        {
            products = new List<Thing>();
            this.trader.ReleaseEnergy();
            return true;
        }

        protected override bool WorkIntrruption(T working)
        {
            return !working.Spawned;
        }

        public override void Tick()
        {
            base.Tick();

            if (base.State == WorkingState.Working)
            {
                this.WorkingTick(this.working, this.workAmount);
            }
        }

        protected override void OnChangeState(WorkingState before, WorkingState after)
        {
            if(after == WorkingState.Ready)
            {
                this.trader.ReleaseEnergy();
            }
        }

        protected abstract void WorkingTick(T working, float workAmount);
    }
}
