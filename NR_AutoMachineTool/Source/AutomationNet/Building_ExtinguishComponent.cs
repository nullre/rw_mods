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
        protected override float WorkAmountPerTick => this.consumer.suppliedEnergy * 0.000001f;

        protected override bool WorkInterruption(Fire working)
        {
            if (working.fireSize <= 0 && working.Spawned)
            {
                working.Destroy();
            }
            return !working.Spawned || working.fireSize <= 0;
        }

        protected override Fire TargetThing(out float workAmount)
        {
            workAmount = float.PositiveInfinity;
            return this.Position.GetThingList(this.Map)
                .SelectMany(t => Option(t as Fire))
                .FirstOption()
                .GetOrDefault(null);
        }

        protected override void WorkingTick(Fire working, float workAmount)
        {
            working.fireSize -= this.WorkAmountPerTick;
            if (working.fireSize <= 0)
            {
                working.fireSize = 0;
                if (working.Spawned)
                {
                    working.Destroy();
                }
            }
        }
    }
}
