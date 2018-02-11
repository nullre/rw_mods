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
    public class PlaceWorker_MustFacingSlotGroup : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            if (map.slotGroupManager.SlotGroupAt((loc + rot.FacingCell)) != null)
            {
                return AcceptanceReport.WasAccepted;
            }
            else
            {
                return new AcceptanceReport("NR_AutoMachineTool.AutomationNet.MustFacingSlotGroup".Translate());
            }
        }
    }
}
