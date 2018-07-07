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
    public class PlaceWorker_Miner : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            center.GetThingList(Find.CurrentMap).Where(t => t.def.category == ThingCategory.Building).SelectMany(t => Option(t as Building_Miner)).Select(m => m.OutputCell()).FirstOption().ForEach(c =>
            {
                GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c), Color.blue);
                GenDraw.DrawFieldEdges(c.SlotGroupCells(Find.CurrentMap), Color.green);
            });
        }
    }
}
