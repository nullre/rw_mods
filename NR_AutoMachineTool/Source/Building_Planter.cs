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
    public class Building_Planter : Building_BaseLimitation<Thing>
    {
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        protected override int? SkillLevel { get => this.Setting.PlanterTier(Extension.tier).skillLevel; }
        protected override float SpeedFactor { get => this.Setting.PlanterTier(Extension.tier).speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).maxSupplyPowerForSpeed; }
        public override int MinPowerForRange { get => this.Setting.PlanterTier(Extension.tier).minSupplyPowerForRange; }
        public override int MaxPowerForRange { get => this.Setting.PlanterTier(Extension.tier).maxSupplyPowerForRange; }

        protected override void Reset()
        {
            if (this.working != null)
            {
                if (this.working.Spawned)
                    this.working.Destroy();
            }
            base.Reset();
        }

        protected override float GetTotalWorkAmount(Thing working)
        {
            return working.def.plant.sowWork;
        }

        protected override bool WorkIntrruption(Thing working)
        {
            return !working.Spawned;
        }

        protected override bool TryStartWorking(out Thing target)
        {
            target = GenRadial.RadialCellsAround(this.Position, this.GetRange(), true)
                .Select(c => new { Cell = c, Plantable = c.GetPlantable(this.Map) })
                .Where(c => c.Plantable.HasValue)
                .Select(c => new { Cell = c.Cell, Plantable = c.Plantable.Value })
                .Where(c => c.Plantable.GetPlantDefToGrow().CanEverPlantAt(c.Cell, this.Map))
                .Where(c => GenPlant.GrowthSeasonNow(c.Cell, this.Map))
                .Where(c => GenPlant.SnowAllowsPlanting(c.Cell, this.Map))
                .Where(c => Option(c.Plantable as Zone_Growing).Fold(true)(z => z.allowSow))
                .Where(c => c.Cell.GetRoom(this.Map) == this.GetRoom())
                .Where(c => c.Plantable.GetPlantDefToGrow().plant.sowMinSkill <= this.SkillLevel)
                .Where(c => GenPlant.AdjacentSowBlocker(c.Plantable.GetPlantDefToGrow(), c.Cell, this.Map) == null)
                .Where(c => !this.ProductLimitation || this.Map.resourceCounter.GetCount(c.Plantable.GetPlantDefToGrow().plant.harvestedThingDef) < this.ProductLimitCount)
                .FirstOption()
                .SelectMany(c =>
                {
                    var planting = ThingMaker.MakeThing(c.Plantable.GetPlantDefToGrow());
                    Option(planting as Plant).ForEach(x => x.sown = false);
                    if (!GenPlace.TryPlaceThing(planting, c.Cell, this.Map, ThingPlaceMode.Direct))
                    {
                        return Nothing<Thing>();
                    }
                    return Option(planting);
                }).GetOrDefault(() => null);
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            Option(working as Plant).ForEach(x => x.sown = true);
            products = new List<Thing>();
            return true;
        }
    }
}
