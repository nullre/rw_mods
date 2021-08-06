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
    public class Building_Harvester : Building_BaseRange<Plant>
    {
        protected override float SpeedFactor { get => this.Setting.HarvesterTier(Extension.tier).speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.HarvesterTier(Extension.tier).maxSupplyPowerForSpeed; }

        protected override bool WorkInterruption(Plant working)
        {
            return !working.Spawned || (!working.Blighted && !working.HarvestableNow);
        }

        protected override bool TryStartWorking(out Plant target, out float workAmount)
        {
            var plant = this.GetTargetCells()
                .Where(c => c.GetPlantable(this.Map).HasValue)
                .SelectMany(c => c.GetThingList(this.Map))
                .SelectMany(t => Option(t as Plant))
                .Where(p => Harvestable(p))
                .FirstOption()
                .GetOrDefault(null);

            target = plant;
            workAmount = target?.def.plant.harvestWork ?? 0f;
            return target != null;
        }

        private bool Harvestable(Plant p)
        {
            return p.Blighted || (
                (p.HarvestableNow && p.LifeStage == PlantLifeStage.Mature) &&
                !InWorking(p) &&
                !IsLimit(p.def.plant.harvestedThingDef));
        }

        protected override bool FinishWorking(Plant working, out List<Thing> products)
        {
            products = new List<Thing>();
            working.def.plant.soundHarvestFinish.PlayOneShot(this);
            if (!working.Blighted)
            {
                products = this.CreateThings(working.def.plant.harvestedThingDef, working.YieldNow());
                working.PlantCollected(null);
            }
            else
            {
                working.Destroy();
            }
            return true;
        }
    }

    public class Building_HarvesterTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.HarvesterTier(this.Parent.tier).minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.HarvesterTier(this.Parent.tier).maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return FacingRect(pos, rot, range)
                .Where(c => (pos + rot.FacingCell).GetRoom(map) == c.GetRoom(map));
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

        public override bool NeedClearingCache => true;
    }
}
