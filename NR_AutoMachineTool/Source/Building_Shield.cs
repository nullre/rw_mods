using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class Building_Shield : Building_BaseRange<Thing>
    {
        protected override float SpeedFactor => 1f;
        public override int MinPowerForSpeed => 10000;
        public override int MaxPowerForSpeed => 10000;

        public override bool SpeedSetting => false;

        public Building_Shield()
        {
            this.startCheckIntervalTicks = 5;
            this.readyOnStart = true;
            base.targetEnumrationCount = 0;
        }

        protected override bool WorkInterruption(Thing working)
        {
            return !working.Spawned;
        }

        private static readonly Func<Projectile, Thing> getLauncher = GenerateGetFieldDelegate<Projectile, Thing>(typeof(Projectile).GetField("launcher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

        protected override bool TryStartWorking(out Thing target, out float workAmount)
        {
            var cells = this.GetAllTargetCells();
            // Destroy DropPod, CrashedShipPart, ShipChunk
            this.Map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Cast<Skyfaller>()
                .Where(f => cells.Contains(f.Position))
                .Where(f => f.innerContainer.SelectMany(t => Option(t as IActiveDropPod)).SelectMany(d => d.Contents.innerContainer).Any(i => Faction.OfPlayer.HostileTo(i.Faction)))
                .Concat(this.Map.listerThings.ThingsOfDef(ThingDefOf.CrashedShipPartIncoming).Cast<Skyfaller>())
                .Concat(this.Map.listerThings.ThingsOfDef(ThingDefOf.ShipChunkIncoming).Cast<Skyfaller>())
                .Concat(this.Map.listerThings.ThingsOfDef(ThingDefOf.MeteoriteIncoming).Cast<Skyfaller>())
                .Where(f => cells.Contains(f.Position))
                .Where(f => !workingSet.Contains(f))
                .Peek(f => workingSet.Add(f))
                // hit roof at remain ticks == 15.
                .ForEach(f => this.MapManager.AfterAction(f.ticksToImpact - 17, () => this.DestroySkyfaller(f)));

            // Destroy Projectile
            this.MapManager.ThingsList.ForAssignableFrom<Projectile>()
                .Where(f => cells.Contains(f.Position))
                .Where(p => getLauncher(p).Faction != Faction.OfPlayer)
                .ToList()
                .ForEach(p => this.DestroyProjectile(p));

            workAmount = 0f;
            target = null;
            return false;
        }

        private static HashSet<Thing> workingSet = new HashSet<Thing>();

        protected void DestroySkyfaller(Skyfaller faller)
        {
            if (this.IsActive())
            {
                GenExplosion.DoExplosion(faller.DrawPos.ToIntVec3(), this.Map, 1, DamageDefOf.Bomb, faller, 0);
                faller.Destroy();
            }
            workingSet.Remove(faller);
        }

        protected void DestroyProjectile(Projectile proj)
        {
            if (this.IsActive())
            {
                //MoteMaker.ThrowLightningGlow(proj.DrawPos, this.Map, 1f);
                proj.Destroy();
            }
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = new List<Thing>();
            return false;
        }
    }

    public class Building_ShieldTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.shieldSetting.minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.shieldSetting.maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return GenRadial.RadialCellsAround(pos, range, true);
        }

        public override bool NeedClearingCache => false;
    }
}
