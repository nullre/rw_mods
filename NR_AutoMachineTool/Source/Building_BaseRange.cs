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
    public abstract class Building_BaseRange<T> : Building_Base<T>, IRange, IAgricultureMachine where T : Thing
    {
        public abstract int MinPowerForRange { get; }
        public abstract int MaxPowerForRange { get; }

        public float SupplyPowerForRange
        {
            get
            {
                return this.supplyPowerForRange;
            }

            set
            {
                this.supplyPowerForRange = value;
                this.SetPower();
            }
        }

        private float supplyPowerForRange;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
        }

        public virtual int GetRange()
        {
            return Mathf.RoundToInt(this.supplyPowerForRange / 500) + 3;
        }

        protected override void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            if (this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = this.MaxPowerForRange;
            }
        }

        protected override void SettingValues()
        {
            this.TryGetComp<CompPowerTrader>().PowerOutput = this.SupplyPowerForSpeed + this.supplyPowerForRange;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
        }

        protected override void SetPower()
        {
            if (-this.supplyPowerForRange - this.SupplyPowerForSpeed != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.supplyPowerForRange - this.SupplyPowerForSpeed;
            }
        }
    }
}
