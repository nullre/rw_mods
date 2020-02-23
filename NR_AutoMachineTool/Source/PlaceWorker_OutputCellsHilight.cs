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
    class PlaceWorker_OutputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_AutoMachineTool>();
            if (ext == null || ext.OutputCellResolver == null)
            {
                Debug.LogWarning("outputCellResolver not found.");
                return;
            }

            ext.OutputCellResolver.OutputCell(center, map, rot).ForEach(c =>
                GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c), ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputCell)));
            ext.OutputCellResolver.OutputZoneCells(center, map, rot)
                .Select(c => new { Cell = c, Color = ext.OutputCellResolver.GetColor(c, map, rot, CellPattern.OutputZone) })
                .GroupBy(a => a.Color)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}
