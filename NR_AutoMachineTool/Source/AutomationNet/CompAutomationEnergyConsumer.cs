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
    public class CompAutomationEnergyConsumer : CompAutomation
    {
        public void RequestEnergy()
        {
            requesting = true;
        }

        public void ReleaseEnergy()
        {
            requesting = false;
        }

        public bool requesting = false;

        public float suppliedEnergy;

        public override void CompPrintForAutomationGrid(SectionLayer layer)
        {
            AutomationNetOverlayMats.GetOverlayGraphic((Building)this.parent).Print(layer, this.parent, 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.requesting, "requesting", false);
        }
    }
}
