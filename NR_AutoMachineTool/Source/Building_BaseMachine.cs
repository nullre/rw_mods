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
    public abstract class Building_BaseMachine<T> : Building_Base<T>, IPowerSupplyMachine, IBeltConbeyorSender where T : Thing
    {
        protected abstract float SpeedFactor { get; }
        protected virtual int? SkillLevel { get => null; }

        public abstract int MinPowerForSpeed { get; }
        public abstract int MaxPowerForSpeed { get; }

        [Unsaved]
        protected bool setInitialMinPower = true;

        protected CompPowerTrader powerComp;

        public virtual float SupplyPowerForSpeed
        {
            get => this.supplyPowerForSpeed;
            set
            {
                if (this.supplyPowerForSpeed != value)
                {
                    this.supplyPowerForSpeed = value;
                    this.SetPower();
                }
            }
        }
        private float supplyPowerForSpeed;


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            this.ReloadSettings(null, null);
        }

        protected virtual void ReloadSettings(object sender, EventArgs e)
        {
            if (this.SupplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.SupplyPowerForSpeed = this.MinPowerForSpeed;
            }
            if (this.SupplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.SupplyPowerForSpeed = this.MaxPowerForSpeed;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = this.TryGetComp<CompPowerTrader>();

            if (!respawningAfterLoad)
            {
                if (setInitialMinPower)
                    this.SupplyPowerForSpeed = this.MinPowerForSpeed;
            }
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;

            this.MapManager.NextAction(this.SetPower);
        }

        protected override bool IsActive()
        {
            if (this.powerComp == null || !this.powerComp.PowerOn)
            {
                return false;
            }
            return base.IsActive();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
            base.DeSpawn();
        }


        protected virtual void SetPower()
        {
            if (this.SupplyPowerForSpeed != this.powerComp.PowerOutput)
            {
                this.powerComp.PowerOutput = -this.SupplyPowerForSpeed;
            }
        }

        protected override float WorkAmountPerTick => 0.01f * this.SpeedFactor * this.SupplyPowerForSpeed * this.Factor2();

        protected virtual float Factor2()
        {
            return 0.1f;
        }
    }
}
