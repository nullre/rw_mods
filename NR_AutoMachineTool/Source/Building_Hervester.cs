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
    public class Building_Harvester : Building , IAgricultureMachine, IBeltConbeyorSender, IProductLimitation
    {
        public enum HarvestState {
            Ready,
            Harvesting,
            Placing
        }
        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        private int SkillLevel { get => this.Setting.HarvesterTier(Extension.tier).skillLevel; }
        private float SpeedFactor { get => this.Setting.HarvesterTier(Extension.tier).speedFactor; }

        public int MinPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).minSupplyPowerForSpeed; }
        public int MaxPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).maxSupplyPowerForSpeed; }
        public int MinPowerForRange { get => this.Setting.HarvesterTier(Extension.tier).minSupplyPowerForRange; }
        public int MaxPowerForRange { get => this.Setting.HarvesterTier(Extension.tier).maxSupplyPowerForRange; }

        private static HashSet<Plant> harvestingSet = new HashSet<Plant>();

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

        public int ProductLimitCount { get => this.productLimitCount; set => this.productLimitCount = value; }
        public bool ProductLimitation { get => this.productLimitation; set => this.productLimitation = value; }

        private int productLimitCount = 100;
        private bool productLimitation = false;

        private float supplyPowerForSpeed;
        private float supplyPowerForRange;
        private HarvestState state;
        private float workLeft;
        private Plant harvesting;
        private Thing product;

        [Unsaved]
        private Effecter progressBar;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look<bool>(ref this.productLimitation, "productLimitation", false);

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
            Scribe_Values.Look<HarvestState>(ref this.state, "working");
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f);

            Scribe_References.Look<Plant>(ref this.harvesting, "harvesting");
            Scribe_Deep.Look<Thing>(ref this.product, "product");

            this.ReloadSettings(null, null);
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
            }
            if (this.supplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MaxPowerForSpeed;
            }
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            if (this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = this.MaxPowerForRange;
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
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        public override void DeSpawn()
        {
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
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
            if (this.state != HarvestState.Ready)
            {
                this.CleanupProgressBar();
                Option(this.product).ForEach(t =>
                {
                    if (!t.Spawned)
                    {
                        GenPlace.TryPlaceThing(this.product, this.Position, this.Map, ThingPlaceMode.Near);
                    }
                });
            }
            this.state = HarvestState.Ready;
            this.workLeft = 0;
            Option(this.harvesting).ForEach(h => harvestingSet.Remove(h));
            this.harvesting = null;
            this.product = null;
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
            this.progressBar.EffectTick(this.harvesting, TargetInfo.Invalid);
            Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (this.harvesting.def.plant.harvestWork - this.workLeft) / this.harvesting.def.plant.harvestWork);
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

            if (this.state == HarvestState.Ready)
            {
                if (Find.TickManager.TicksGame % 30 == 20 || this.checkNextReady)
                {
                    this.TryStartHarvesting();
                    this.checkNextReady = false;
                }
            }
            else if (this.state == HarvestState.Harvesting)
            {
                this.workLeft -= 0.01f * this.SpeedFactor * this.supplyPowerForSpeed * 0.1f;
                this.UpdateProgressBar();
                if (this.workLeft <= 0f)
                {
                    this.FinishHarvest();
                    this.CleanupProgressBar();
                    this.checkNextPlace = true;
                }
                else if (!this.harvesting.HarvestableNow || !this.harvesting.Spawned)
                {
                    this.Reset();
                }
            }
            else if (this.state == HarvestState.Placing)
            {
                if (Find.TickManager.TicksGame % 30 == 21 || checkNextPlace)
                {
                    if (this.PlaceProduct())
                    {
                        this.state = HarvestState.Ready;
                        this.Reset();
                        this.checkNextReady = true;
                    }
                    this.checkNextPlace = false;
                }
            }
        }

        [Unsaved]
        private bool checkNextReady = false;

        [Unsaved]
        private bool checkNextPlace = false;

        private void TryStartHarvesting()
        {
            FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => c.GetPlantable(this.Map).HasValue)
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetThingList(this.Map))
                .SelectMany(t => Option(t as Plant))
                .Where(p => p.HarvestableNow)
                .Where(p => p.LifeStage == PlantLifeStage.Mature)
                .Where(p => !harvestingSet.Contains(p))
                .Where(p => !this.ProductLimitation || this.Map.resourceCounter.GetCount(p.def.plant.harvestedThingDef) < this.ProductLimitCount)
                .FirstOption()
                .ForEach(p =>
                {
                    this.harvesting = p;
                    this.workLeft = p.def.plant.harvestWork;
                    this.state = HarvestState.Harvesting;
                    harvestingSet.Add(p);
                });
        }

        private void FinishHarvest()
        {
            if (this.harvesting.Spawned)
            {
                harvestingSet.Remove(this.harvesting);
                this.product = ThingMaker.MakeThing(this.harvesting.def.plant.harvestedThingDef, null);
                this.product.stackCount = this.harvesting.YieldNow();
                this.harvesting.def.plant.soundHarvestFinish.PlayOneShot(this);
                this.harvesting.PlantCollected();
                this.harvesting = null;
                this.state = HarvestState.Placing;
                this.workLeft = 0;
            }
        }

        private bool PlaceProduct()
        {
            return Option(this.product).Fold(false)(target => {
                var conveyor = OutputCell().GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as IBeltConbeyorLinkable))
                    .Where(b => !b.IsUnderground)
                    .FirstOption();
                if (conveyor.HasValue)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.Value.ReceivableNow(false))
                    {
                        conveyor.Value.ReceiveThing(target);
                        return true;
                    }
                }
                else
                {
                    // ない場合は適当に流す.
                    if (!PlaceItem(target, OutputCell(), false, this.Map))
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                    }
                    return true;
                }
                return false;
            });
        }

        public IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.state)
            {
                case HarvestState.Harvesting:
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.harvesting.def.plant.harvestWork - this.workLeft), Mathf.RoundToInt(this.harvesting.def.plant.harvestWork), Mathf.RoundToInt(((this.harvesting.def.plant.harvestWork - this.workLeft) / this.harvesting.def.plant.harvestWork) * 100));
                    break;
                case HarvestState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case HarvestState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(1);
                    break;
                default:
                    msg += this.state.ToString();
                    break;
            }
            msg += "\n";
            msg += "NR_AutoMachineTool.SkillLevel".Translate(this.SkillLevel.ToString());
            return msg;
        }

        public void NortifyReceivable()
        {
            this.checkNextPlace = true;
        }
    }
}
