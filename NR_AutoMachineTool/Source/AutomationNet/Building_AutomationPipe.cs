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
    public class Building_AutomationPipe : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            this.Position
                .GetThingList(this.Map)
                .SelectMany(t => Option(t as Building))
                .SelectMany(b => Option(b.TryGetComp<CompAutomationEnergyConsumer>()))
                .Where(c => c.DestructWithPipe)
                .ForEach(c => c.parent.Destroy(mode));

            base.Destroy(mode);
        }
    }
}
