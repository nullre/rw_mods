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
    public class CompAutomation : ThingComp
    {
        public AutomationNet connectedNet;

        public CompProperties_Automation Props { get => (CompProperties_Automation)this.props; }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.parent.Map.GetComponent<AutomationNetManager>().Notify_CompSpawn(this);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            previousMap.GetComponent<AutomationNetManager>().Notify_CompDespawn(this);

            base.PostDestroy(mode, previousMap);
        }

        public virtual void CompPrintForAutomationGrid(SectionLayer layer)
        {
            AutomationNetOverlayMats.LinkedOverlayGraphic.Print(layer, this.parent, 0);
        }

        public bool DestructWithPipe
        {
            get => this.Props.destructWithPipe;
        }

        public float UsableEnergyNow
        {
            get => Option(this.connectedNet).Select(n => n.UsableEnergy).GetOrDefault(0f);
        }

        public bool CanUseEnergy
        {
            get => Option(this.connectedNet).Select(n => n.IsSuppliedEnergy).GetOrDefault(false);
        }
    }
}
