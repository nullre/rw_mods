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
    public class Building_Planter : Building, IAgricultureMachine
    {
        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        private int SkillLevel { get => this.Setting.Tier(Extension.tier).skillLevel; }
        private float SpeedFactor { get => this.Setting.Tier(Extension.tier).speedFactor; }

        public int MinPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).minSupplyPowerForSpeed; }
        public int MaxPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).maxSupplyPowerForSpeed; }
        public int MinPowerForRange { get => this.Setting.PlanterTier(Extension.tier).minSupplyPowerForRange; }
        public int MaxPowerForRange { get => this.Setting.PlanterTier(Extension.tier).maxSupplyPowerForRange; }

        public float SupplyPowerForSpeed
        {
            get
            {
                return this.supplyPowerForSpeed;
            }

            set
            {
                this.supplyPowerForSpeed = value;
                this.SetPower();
            }
        }

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

        private float supplyPowerForSpeed;
        private float supplyPowerForRange;
        private bool working;
        private float workLeft;
        private Thing planting;

        [Unsaved]
        private Effecter progressBar;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
            Scribe_Values.Look<bool>(ref this.working, "working", false);
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f);
            
            Scribe_References.Look<Thing>(ref this.planting, "planting");

            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.supplyPowerForSpeed = -this.MinPowerForSpeed;
            }
            if (-this.supplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.supplyPowerForSpeed = -this.MaxPowerForSpeed;
            }
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = -this.MinPowerForRange;
            }
            if (-this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = -this.MaxPowerForRange;
            }
        }

        private void SettingValues()
        {
            this.TryGetComp<CompPowerTrader>().PowerOutput = this.supplyPowerForSpeed + this.supplyPowerForRange;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
            }
        }

        public override void DeSpawn()
        {
            this.Reset();
            base.DeSpawn();
        }

        public int GetRange()
        {
            return Mathf.RoundToInt(this.supplyPowerForRange / 500) + 3;
        }

        private bool IsActive()
        {
            if (this.TryGetComp<CompPowerTrader>() == null || !this.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return false;
            }
            if (this.Destroyed)
            {
                return false;
            }

            return true;
        }

        private void Reset()
        {
            if (this.working)
            {
                if (this.planting.Spawned)
                    this.planting.Destroy();
                this.CleanupProgressBar();
            }
            this.working = false;
            this.workLeft = 0;
            this.planting = null;
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
            this.progressBar.EffectTick(this.planting, TargetInfo.Invalid);
            Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (this.planting.def.plant.sowWork - this.workLeft) / this.planting.def.plant.sowWork);
        }

        private void SetPower()
        {
            if(-this.supplyPowerForRange - this.supplyPowerForSpeed != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.supplyPowerForRange - this.supplyPowerForSpeed;
            }
        }

        public override void Tick()
        {
            base.Tick();

            this.SetPower();

            if (!this.IsActive())
            {
                this.Reset();
                return;
            }

            if (!working)
            {
                if(Find.TickManager.TicksGame % 30 == 10 || this.checkNext)
                {
                    this.TryStartPlanting();
                }
            }
            else
            {
                this.workLeft -= 0.01f * this.SpeedFactor * this.supplyPowerForSpeed * 0.1f;
                this.UpdateProgressBar();
                if (this.workLeft <= 0f)
                {
                    this.working = false;
                    this.Reset();
                    this.checkNext = true;
                    Option(this.planting as Plant).ForEach(x => x.sown = true);
                    this.CleanupProgressBar();
                }
                else if (!this.planting.Spawned)
                {
                    this.Reset();
                }
            }
        }

        [Unsaved]
        private bool checkNext = false;

        private void TryStartPlanting()
        {
            GenRadial.RadialCellsAround(this.Position, this.GetRange(), true)
                .Select(c => new { Cell = c, Zone = c.GetZone(this.Map) as Zone_Growing })
                .Where(c => c.Zone != null)
                .Where(c => c.Zone.GetPlantDefToGrow().CanEverPlantAt(c.Cell, this.Map))
                .Where(c => GenPlant.GrowthSeasonNow(c.Cell, this.Map))
                .Where(c => GenPlant.SnowAllowsPlanting(c.Cell, this.Map))
                .Where(c => c.Zone.allowSow)
                .Where(c => c.Cell.GetRoom(this.Map) == this.GetRoom())
                .Where(c => c.Zone.GetPlantDefToGrow().plant.sowMinSkill <= this.SkillLevel)
                .Where(c => GenPlant.AdjacentSowBlocker(c.Zone.GetPlantDefToGrow(), c.Cell, this.Map) == null)
                .FirstOption()
                .ForEach(c =>
                {
                    this.planting = ThingMaker.MakeThing(c.Zone.GetPlantDefToGrow());
                    Option(this.planting as Plant).ForEach(x => x.sown = false);
                    if (GenPlace.TryPlaceThing(this.planting, c.Cell, this.Map, ThingPlaceMode.Direct))
                    {
                        this.workLeft = this.planting.def.plant.sowWork;
                        this.working = true;
                    }
                });
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            if (this.working)
            {
                msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.planting.def.plant.sowWork - this.workLeft), Mathf.RoundToInt(this.planting.def.plant.sowWork), Mathf.RoundToInt(((this.planting.def.plant.sowWork - this.workLeft) / this.planting.def.plant.sowWork) * 100));
            }
            else
            {
                msg += "NR_AutoMachineTool.StatReady".Translate();
            }
            msg += "\n";
            msg += "NR_AutoMachineTool.SkillLevel".Translate(this.SkillLevel.ToString());
            return msg;
        }
    }
}
