using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class PlaceWorker_AutomationNetComponent : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (loc.GetThingList(map)
                .Where(t => t.def.category == ThingCategory.Building)
                .Any(t => t as Building_AutomationPipe != null))
            {
                return AcceptanceReport.WasAccepted;
            }
            else
            {
                return new AcceptanceReport("NR_AutoMachineTool.AutomationNet.MustOnPipe".Translate());
            }
        }
    }
}
