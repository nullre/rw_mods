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
    public class Building_Harvester : Building_BaseLimitation<Plant>
    {
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        protected override int? SkillLevel { get => this.Setting.HarvesterTier(Extension.tier).skillLevel; }
        protected override float SpeedFactor { get => this.Setting.HarvesterTier(Extension.tier).speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).maxSupplyPowerForSpeed; }
        public override int MinPowerForRange { get => this.Setting.HarvesterTier(Extension.tier).minSupplyPowerForRange; }
        public override int MaxPowerForRange { get => this.Setting.HarvesterTier(Extension.tier).maxSupplyPowerForRange; }

        protected override float GetTotalWorkAmount(Plant working)
        {
            return working.def.plant.harvestWork;
        }

        protected override bool WorkIntrruption(Plant working)
        {
            return !working.HarvestableNow || !working.Spawned;
        }

        protected override bool TryStartWorking(out Plant target)
        {
            var plant = FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => c.GetPlantable(this.Map).HasValue)
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetThingList(this.Map))
                .SelectMany(t => Option(t as Plant))
                .Where(p => p.HarvestableNow)
                .Where(p => p.LifeStage == PlantLifeStage.Mature)
                .Where(p => !InWorking(p))
                .Where(p => !this.ProductLimitation || this.Map.resourceCounter.GetCount(p.def.plant.harvestedThingDef) < this.ProductLimitCount)
                .FirstOption()
                .GetOrDefault(() => null);
            target = plant;
            return target != null;
        }

        protected override bool FinishWorking(Plant working, out List<Thing> products)
        {
            products = this.CreateThings(working.def.plant.harvestedThingDef, working.YieldNow());
            working.def.plant.soundHarvestFinish.PlayOneShot(this);
            working.PlantCollected();
            return true;
        }
    }
}
