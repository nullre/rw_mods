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
    class PlaceWorker_InputCellsHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            Map map = Find.CurrentMap;
            var ext = def.GetModExtension<ModExtension_AutoMachineTool>();
            if (ext == null || ext.InputCellResolver == null)
            {
                Debug.LogWarning("inputCellResolver not found.");
                return;
            }

            GenDraw.DrawFieldEdges(new List<IntVec3>().Append(ext.InputCellResolver.InputCell(center, map, rot)),
                ext.InputCellResolver.GetColor(ext.InputCellResolver.InputCell(center, map, rot), map, rot, CellPattern.InputCell));
            ext.InputCellResolver.InputZoneCells(center, map, rot)
                .Select(c => new { Cell = c, Color = ext.InputCellResolver.GetColor(c, map, rot, CellPattern.InputZone) })
                .GroupBy(a => a.Color)
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));
        }
    }
}
