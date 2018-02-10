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
    public class Building_ExtinguishComponent : Building_BaseComponent<Fire>
    {
        protected override float GetTotalWorkAmount(Fire working)
        {
            return float.PositiveInfinity;
        }

        protected override float WorkAmountPerTick()
        {
            return this.trader.suppliedEnergy * 0.000001f;
        }

        protected override bool WorkIntrruption(Fire working)
        {
            if (working.fireSize <= 0)
            {
                working.Destroy();
            }
            return base.WorkIntrruption(working) || !working.Spawned || working.fireSize <= 0;
        }

        protected override Fire TargetThing()
        {
            var target = this.Position.GetThingList(this.Map)
                .SelectMany(t => Option(t as Fire))
                .FirstOption()
                .GetOrDefault(null);
            return target;
        }

        protected override void WorkingTick(Fire working, float workAmount)
        {
            working.fireSize -= this.WorkAmountPerTick();
            if (working.fireSize <= 0)
            {
                working.fireSize = 0;
                working.Destroy();
            }
        }
    }
}
