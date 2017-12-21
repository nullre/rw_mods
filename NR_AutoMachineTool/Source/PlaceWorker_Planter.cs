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
    class PlaceWorker_Planter : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            GenRadial.RadialCellsAround(center,
                center.GetThingList(Find.VisibleMap).Where(t => t.def == def).SelectMany(t => Option(t as Building_Planter)).FirstOption().Fold(3)(p => p.GetRange()),
                true)
                .Where(c => c.GetRoom(Find.VisibleMap) == center.GetRoom(Find.VisibleMap))
                .Where(c => !c.GetThingList(Find.VisibleMap).Any(t => t.def.passability == Traversability.Impassable))
                .Select(c => new { Cell = c, Zone = c.GetZone(Find.VisibleMap) as Zone_Growing})
                .GroupBy(c => c.Zone)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(c => c.Cell).ToList(), g.Key == null ? Color.white : Color.green));
        }
    }
}
