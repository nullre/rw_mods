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
    public interface IRange
    {
        int GetRange();
    }

    class PlaceWorker_FacingRect : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            GenDraw.DrawFieldEdges(FacingRect(center, rot,
                center.GetThingList(Find.CurrentMap).Where(t => t.def == def).SelectMany(t => Option(t as IRange)).FirstOption().Fold(2)(p => p.GetRange()))
                .Where(c => (center + rot.FacingCell).GetRoom(Find.CurrentMap) == c.GetRoom(Find.CurrentMap))
                .ToList(), Color.white);

            var pos = (center + rot.Opposite.FacingCell);
            GenDraw.DrawFieldEdges(new List<IntVec3>().Append(pos), Color.blue);
            GenDraw.DrawFieldEdges(pos.SlotGroupCells(Find.CurrentMap), Color.gray);
        }
    }
}
