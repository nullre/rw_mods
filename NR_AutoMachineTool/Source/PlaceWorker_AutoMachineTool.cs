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
    public class PlaceWorker_AutoMachineTool : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            center.GetThingList(Find.VisibleMap)
                .Where(t => t.def == def)
                .FirstOption()
                .Select(b => b as Building_AutoMachineTool)
                .ForEach(c =>
                {
                    GenDraw.DrawFieldEdges(new List<IntVec3>().Append(c.OutputCell()), Color.blue);
                    GenDraw.DrawFieldEdges(c.OutputZone(), Color.green);
                });
            GenDraw.DrawFieldEdges(GenAdj.CellsAdjacent8Way(center, rot, new IntVec2(1, 1)).ToList().Append(center));
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            var r = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore);
            if (r.Accepted)
            {
                if ((loc + rot.FacingCell).GetThingList(map)
                    .Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as Building_WorkTable))
                    .Where(b => b.InteractionCell == loc).Count() == 0)
                {
                    return new AcceptanceReport("NR_AutoMachineTool.PlaceNotAllowed".Translate());
                }
                return r;
            }
            else
            {
                return r;
            }
        }
    }
}
