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
    public class SpecialThingFilterWorker_Fresh : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            if (thingWithComps == null)
            {
                return false;
            }
            CompRottable comp = thingWithComps.GetComp<CompRottable>();
            return comp != null && !((CompProperties_Rottable)comp.props).rotDestroys && comp.Stage == RotStage.Fresh;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            CompProperties_Rottable compProperties = def.GetCompProperties<CompProperties_Rottable>();
            return compProperties != null && !compProperties.rotDestroys;
        }
    }
}
