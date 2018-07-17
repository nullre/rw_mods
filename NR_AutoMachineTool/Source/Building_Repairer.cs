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
    public class Building_Repairer : Building_BaseRange<Building_Repairer>
    {
        protected override float SpeedFactor => this.Setting.repairerSetting.speedFactor;

        public override int MinPowerForSpeed => this.Setting.repairerSetting.minSupplyPowerForSpeed;
        public override int MaxPowerForSpeed => this.Setting.repairerSetting.maxSupplyPowerForSpeed;

        public Building_Repairer()
        {
            base.forcePlace = false;
            base.readyOnStart = true;
            base.showProgressBar = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        protected override bool WorkInterruption(Building_Repairer working)
        {
            return false;
        }

        [Unsaved]
        private Thing working;
        [Unsaved]
        private Pawn pawn;
        [Unsaved]
        private float repairAmount;
        [Unsaved]
        private Effecter progressBar;

        protected override bool TryStartWorking(out Building_Repairer target, out float workAmount)
        {
            this.pawn = null;
            this.working = null;
            this.repairAmount = 0f;
            target = this;

            var things = GetTargetCells()
                .SelectMany(c => c.GetThingList(this.Map))
                .ToList();

            this.working = things
                .SelectMany(t => Option(t as Fire))
                .FirstOption()
                .GetOrDefault(null);

            if (this.working == null)
            {
                this.working = things
                    .Where(t => t.def.category == ThingCategory.Building)
                    .Where(p => p.Faction == Faction.OfPlayer)
                    .Where(t => t.HitPoints < t.MaxHitPoints)
                    .FirstOption()
                    .GetOrDefault(null);
            }

            if (this.working == null)
            {
                var pp = things.Where(t => t.def.category == ThingCategory.Pawn)
                    .Where(p => p.Faction == Faction.OfPlayer)
                    .SelectMany(t => Option(t as Pawn))
                    .FirstOption().GetOrDefault(null);
                this.pawn = things.Where(t => t.def.category == ThingCategory.Pawn)
                    .SelectMany(t => Option(t as Pawn))
                    .Where(p => p.equipment != null && p.equipment.AllEquipmentListForReading != null)
                    .Where(p => p.equipment.AllEquipmentListForReading.Cast<Thing>().ToList().Append<Thing>(p.apparel.WornApparel.Cast<Thing>().ToList()).Any(t => t.HitPoints < t.MaxHitPoints))
                    .FirstOption()
                    .GetOrDefault(null);
                if (this.pawn != null)
                {
                    this.working = this.pawn.equipment.AllEquipmentListForReading.Cast<Thing>().ToList().Append<Thing>(this.pawn.apparel.WornApparel.Cast<Thing>().ToList()).Where(t => t.HitPoints < t.MaxHitPoints).First();
                }
            }
            workAmount = this.working == null ? 0 : float.PositiveInfinity;
            if (this.working != null)
            {
                this.progressBar = DefDatabase<EffecterDef>.GetNamed("NR_AutoMachineTool_ProgressBar").Spawn();
                this.progressBar.EffectTick(new TargetInfo(this.pawn ?? this.working), TargetInfo.Invalid);
                if (this.working is Fire)
                {
                    ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => ((Fire.MaxFireSize - ((Fire)this.working).fireSize) / Fire.MaxFireSize);
                }
                else
                {
                    ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => ((float)this.working.HitPoints / (float)this.working.MaxHitPoints);
                }
                MapManager.EachTickAction(this.Repair);
            }
            return this.working != null;
        }

        protected override void Reset()
        {
            base.Reset();
            if (this.progressBar != null)
            {
                this.progressBar.Cleanup();
            }
        }

        protected bool Repair()
        {
            var result = this.RepairInt();
            if (result)
            {
                this.ForceReady();
                if (this.progressBar != null)
                {
                    this.progressBar.Cleanup();
                    this.progressBar = null;
                }
            }

            return result;
        }

        protected bool RepairInt()
        {
            if (!this.IsActive())
            {
                return true;
            }
            if (this.pawn != null && (!this.pawn.Spawned || !GenRadial.RadialCellsAround(this.Position, this.GetRange(), true).Contains(this.pawn.Position)))
            {
                return true;
            }
            if (this.pawn == null && (!this.working.Spawned || !GenRadial.RadialCellsAround(this.Position, this.GetRange(), true).Contains(this.working.Position)))
            {
                return true;
            }
            if (this.working is Fire)
            {
                var fire = (Fire)this.working;
                fire.fireSize -= this.WorkAmountPerTick * 0.1f;
                if (fire.fireSize <= 0)
                {
                    fire.fireSize = 0;
                    if (fire.Spawned)
                    {
                        fire.Destroy();
                    }
                    return true;
                }
                return false;
            }
            this.repairAmount += this.WorkAmountPerTick;
            if (this.repairAmount > 1)
            {
                var change = Mathf.RoundToInt(this.repairAmount);
                working.HitPoints += change;
                this.repairAmount -= change;
            }
            if (this.working.HitPoints >= this.working.MaxHitPoints)
            {
                this.working.HitPoints = this.working.MaxHitPoints;
                return true;
            }
            return false;
        }

        protected override float Factor2()
        {
            return base.Factor2() * 0.2f;
        }

        protected override void ClearActions()
        {
            base.ClearActions();
            MapManager.RemoveEachTickAction(this.Repair);
        }

        protected override bool FinishWorking(Building_Repairer working, out List<Thing> products)
        {
            products = new List<Thing>();
            return true;
        }
    }

    public class Building_RepairerTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.repairerSetting.minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.repairerSetting.maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return GenRadial.RadialCellsAround(pos, range, true);
        }
    }
}
