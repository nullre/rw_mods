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
    public static class AutomationNetMaker
    {
        private static IEnumerable<CompAutomation> ContiguousBuildings(Building root, Map map)
        {
            var closedSet = new HashSet<Building>();
            var openSet = new HashSet<Building>(
                root.Position.GetThingList(map)
                    .SelectMany(t => Option(t as Building))
                    .Where(b => b.TryGetComp<CompAutomation>() != null));
            do
            {
                closedSet.AddRange(openSet);
                openSet = new HashSet<Building>(openSet)
                    .SelectMany(b => GenAdj.CellsAdjacentCardinal(b))
                    .SelectMany(c => c.GetThingList(map))
                    .SelectMany(t => Option(t as Building))
                    .Where(b => b.TryGetComp<CompAutomation>() != null)
                    .Where(b => !openSet.Contains(b) && !closedSet.Contains(b))
                    .ToHashSet();
            } while (openSet.Count > 0);
            return closedSet.Select(b => b.TryGetComp<CompAutomation>());
        }

        public static AutomationNet NewNetStartingFrom(Building root, Map map)
        {
            return new AutomationNet(ContiguousBuildings(root, map));
        }
    }
}
