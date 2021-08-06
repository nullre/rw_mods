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
    public class CompAutomationStorage : CompAutomation
    {
        public IEnumerable<Thing> StorageItems
        {
            get => Option(this.parent.Map.haulDestinationManager.SlotGroupAt(this.parent.Position + this.parent.Rotation.FacingCell))
                .Select(g => g.HeldThings)
                .GetOrDefault(Enumerable.Empty<Thing>());
        }

        public override void CompPrintForAutomationGrid(SectionLayer layer)
        {
            AutomationNetOverlayMats.CompStorageOverlayGraphic.Print(layer, this.parent, 0);
        }
    }
}
