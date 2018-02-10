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
    public class AutomationNetGrid
    {
        private AutomationNet[] netGrid;

        public AutomationNetGrid(Map map)
        {
            this.map = map;
            this.netGrid = new AutomationNet[map.cellIndices.NumGridCells];
        }

        private Map map;

        public AutomationNet NetAt(IntVec3 c)
        {
            return this.netGrid[this.map.cellIndices.CellToIndex(c)];
        }

        public void Notify_NetCreated(AutomationNet net)
        {
            net.nodes.SelectMany(n => GenAdj.OccupiedRect(n.parent).Cells).ForEach(c => netGrid[this.map.cellIndices.CellToIndex(c)] = net);
        }

        public void Notify_NetDeleted(AutomationNet net)
        {
            net.nodes.SelectMany(n => GenAdj.OccupiedRect(n.parent).Cells).ForEach(c => netGrid[this.map.cellIndices.CellToIndex(c)] = null);
        }
    }
}
