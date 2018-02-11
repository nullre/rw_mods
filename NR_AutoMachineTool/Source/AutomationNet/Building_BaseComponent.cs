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
        protected CompAutomationEnergyConsumer consumer;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.consumer = this.TryGetComp<CompAutomationEnergyConsumer>();
            this.showProgressBar = false;
        }

        protected override bool IsActive()
        {
            return base.IsActive() && this.consumer.CanUseEnergy;
        }

        protected override bool TryStartWorking(out T target)
        {
            var reuslt = (target = TargetThing()) != null;

            if (reuslt)
            {
                this.consumer.RequestEnergy();
            }
            return reuslt;
        }

        protected abstract T TargetThing();

        protected override bool FinishWorking(T working, out List<Thing> products)
        {
            products = new List<Thing>();
            this.consumer.ReleaseEnergy();
            return true;
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
                this.consumer.ReleaseEnergy();
            }
        }

        protected abstract void WorkingTick(T working, float workAmount);

        public override IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.FacingCell);
        }
    }
}
