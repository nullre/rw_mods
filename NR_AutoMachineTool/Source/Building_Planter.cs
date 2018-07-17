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
    public class Building_Planter : Building_BaseRange<Thing>
    {
        protected override int? SkillLevel { get => this.Setting.PlanterTier(Extension.tier).skillLevel; }
        protected override float SpeedFactor { get => this.Setting.PlanterTier(Extension.tier).speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.PlanterTier(Extension.tier).maxSupplyPowerForSpeed; }

        protected override void Reset()
        {
            if (this.Working != null)
            {
                if (this.Working.Spawned)
                    this.Working.Destroy();
            }
            base.Reset();
        }

        public override bool Glowable => true;

        protected override bool WorkInterruption(Thing working)
        {
            return !working.Spawned;
        }

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            target = GetTargetCells()
                .Select(c => new { Cell = c, Plantable = c.GetPlantable(this.Map) })
                .Where(a => a.Plantable.HasValue)
                .Select(a => new { Cell = a.Cell, Plantable = a.Plantable.Value })
//                .Where(c => c.Plantable.GetPlantDefToGrow().CanEverPlantAt(c.Cell, this.Map))
//                .Where(c => GenPlant.GrowthSeasonNow(c.Cell, this.Map))
//                .Where(c => GenPlant.SnowAllowsPlanting(c.Cell, this.Map))
//                .Where(c => Option(c.Plantable as Zone_Growing).Fold(true)(z => z.allowSow))
//                .Where(c => c.Plantable.GetPlantDefToGrow().plant.sowMinSkill <= this.SkillLevel)
//                .Where(c => GenPlant.AdjacentSowBlocker(c.Plantable.GetPlantDefToGrow(), c.Cell, this.Map) == null)
//                .Where(c => !c.Plantable.GetPlantDefToGrow().plant.interferesWithRoof || (c.Plantable.GetPlantDefToGrow().plant.interferesWithRoof && !c.Cell.Roofed(this.Map)))
                .Where(c => CanSow(c.Cell, c.Plantable))
                .Where(c => !IsLimit(c.Plantable.GetPlantDefToGrow().plant.harvestedThingDef))
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
                }).GetOrDefault(null);
            workAmount = target?.def.plant.sowWork ?? 0f;
            return target != null;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            Option(working as Plant).ForEach(x => x.sown = true);
            products = new List<Thing>();
            return true;
        }

        private bool CanSow(IntVec3 cell, IPlantToGrowSettable grower)
        {
            return
                grower.GetPlantDefToGrow().CanEverPlantAt(cell, this.Map) &&
                PlantUtility.GrowthSeasonNow(cell, this.Map) &&
                PlantUtility.SnowAllowsPlanting(cell, this.Map) &&
                Option(grower as Zone_Growing).Fold(true)(z => z.allowSow) &&
                grower.GetPlantDefToGrow().plant.sowMinSkill <= this.SkillLevel &&
                PlantUtility.AdjacentSowBlocker(grower.GetPlantDefToGrow(), cell, this.Map) == null &&
                (!grower.GetPlantDefToGrow().plant.interferesWithRoof || (grower.GetPlantDefToGrow().plant.interferesWithRoof && !cell.Roofed(this.Map)));
        }
    }

    public class Building_PlanterTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.PlanterTier(this.Parent.tier).minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.PlanterTier(this.Parent.tier).maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return GenRadial.RadialCellsAround(pos, range, true)
                .Where(c => c.GetRoom(map) == pos.GetRoom(map))
                .Where(c => !c.GetThingList(Find.CurrentMap).Any(t => t.def.passability == Traversability.Impassable));
        }

        public override Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            var col = base.GetColor(cell, map, rot, cellPattern);
            if (cell.GetPlantable(map).HasValue)
            {
                col = Color.green;
                if (cellPattern == CellPattern.BlurprintMax)
                {
                    col = col.A(0.5f);
                }
            }
            return col;
        }
    }
}
