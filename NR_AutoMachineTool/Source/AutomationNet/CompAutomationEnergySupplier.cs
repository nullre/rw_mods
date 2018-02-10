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
    public class CompAutomationEnergySupplier : CompAutomation
    {
        private float supplyEnergyPerTick = 0;
        public float SupplyEnergyPerTick
        {
            get => this.supplyEnergyPerTick;
            set => this.supplyEnergyPerTick = value;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.supplyEnergyPerTick = this.Props.energyAmount;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<float>(ref this.supplyEnergyPerTick, "supplyEnergyPerTick", 0f);
        }
    }
}
