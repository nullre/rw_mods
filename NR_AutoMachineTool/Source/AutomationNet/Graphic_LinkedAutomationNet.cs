using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class Graphic_LinkedAutomationNet : Graphic_Link2<Graphic_LinkedAutomationNet>
    {
        public Graphic_LinkedAutomationNet() : base()
        {
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            var parentCheck = parent.TryGetComp<CompAutomation>() != null ||
                Option(parent as Blueprint).SelectMany(b => Option(b.def.entityDefToBuild as ThingDef)).Select(d => d.GetCompProperties<CompProperties_Automation>() != null).GetOrDefault(false);

            var cellCheck = 
                c.GetThingList(parent.Map)
                    .SelectMany(t => Option(t as Building))
                    .Any(b => b.TryGetComp<CompAutomation>() != null) ||
                c.GetThingList(parent.Map)
                    .SelectMany(t => Option(t as Blueprint))
                    .SelectMany(b => Option(b.def.entityDefToBuild as ThingDef))
                    .Any(d => d.GetCompProperties<CompProperties_Automation>() != null);

            return c.InBounds(parent.Map) && (parentCheck && cellCheck);
        }
    }
}
