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
    public class PlaceWorker_OutputHighlight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            var pos = (center + rot.FacingCell);
            GenDraw.DrawFieldEdges(new List<IntVec3>().Append(pos), Color.blue);
            GenDraw.DrawFieldEdges(pos.SlotGroupCells(Find.VisibleMap), Color.green);
        }
    }
}
