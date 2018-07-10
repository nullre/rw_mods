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
    class PlaceWorker_Cleaner : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            var cells = GenRadial.RadialCellsAround(center,
                center.GetThingList(Find.CurrentMap).Where(t => t.def == def).SelectMany(t => Option(t as Building_Cleaner)).FirstOption().Fold(3)(p => p.GetRange()), true)
                .Where(c => c.GetRoom(Find.CurrentMap) == center.GetRoom(Find.CurrentMap))
                .Where(c => !c.GetThingList(Find.CurrentMap).Any(t => t.def.passability == Traversability.Impassable)).ToList();
            GenDraw.DrawFieldEdges(cells, Color.white);
        }
    }
}
