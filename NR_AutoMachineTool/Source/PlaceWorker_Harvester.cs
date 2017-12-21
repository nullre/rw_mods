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
    class PlaceWorker_Harvester : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            FacingRect(center, rot,
                center.GetThingList(Find.VisibleMap).Where(t => t.def == def).SelectMany(t => Option(t as Building_Harvester)).FirstOption().Fold(3)(p => p.GetRange()))
                .Where(c => (center + rot.FacingCell).GetRoom(Find.VisibleMap) == c.GetRoom(Find.VisibleMap))
                .Where(c => (center + rot.FacingCell).GetRoom(Find.VisibleMap) == c.GetRoom(Find.VisibleMap))
                .Select(c => new { Cell = c, Zone = c.GetZone(Find.VisibleMap) as Zone_Growing })
                .GroupBy(c => c.Zone)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(c => c.Cell).ToList(), g.Key == null ? Color.white : Color.green));

            var pos = (center + rot.Opposite.FacingCell);
            GenDraw.DrawFieldEdges(new List<IntVec3>().Append(pos), Color.blue);
            GenDraw.DrawFieldEdges(pos.ZoneCells(Find.VisibleMap), Color.gray);
        }
    }
}
