using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class Building_Stunner : Building_BaseRange<Pawn>
    {
        protected override float SpeedFactor { get => this.Setting.stunnerSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.stunnerSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.stunnerSetting.maxSupplyPowerForSpeed; }

        public Building_Stunner()
        {
            this.readyOnStart = true;
        }

        protected override bool WorkInterruption(Pawn working)
        {
            return !working.Spawned || !this.GetAllTargetCells().Contains(working.Position);
        }

        protected override TargetInfo ProgressBarTarget()
        {
            return new TargetInfo(this);
        }

        protected override bool TryStartWorking(out Pawn target, out float workAmount)
        {
            var allCells = this.GetAllTargetCells();
            var pawns = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Where(p => allCells.Contains(p.Position)).SelectMany(t => Option(t as Pawn))
            // var pawns = this.GetTargetCells().SelectMany(c => c.GetThingList(this.Map)).SelectMany(t => Option(t as Pawn))
                .Where(p => p.Faction != Faction.OfPlayer)
                .Where(p => !p.Dead && !p.Downed)
                .Where(p => !InWorking(p))
                .ToList();

            var raid = pawns
                .Where(p => p.Faction.HostileTo(Faction.OfPlayer))
                .Where(p => !p.IsPrisoner || (p.IsPrisoner && PrisonBreakUtility.IsPrisonBreaking(p)))
                .Where(p => p.IsPrisoner || p.CurJobDef != JobDefOf.Goto || !p.CurJob.exitMapOnArrival);

            var manhunter = pawns
                .Where(p => p.MentalStateDef == MentalStateDefOf.Manhunter || p.MentalStateDef == MentalStateDefOf.ManhunterPermanent);

            target = raid.Concat(manhunter).FirstOption().GetOrDefault(null);

            workAmount = 2000f;
            return target != null;
        }

        protected override bool FinishWorking(Pawn working, out List<Thing> products)
        {
            var def = DefDatabase<HediffDef>.GetNamed("NR_AutoMachineTool_Hediff_Unconsciousness");
            if(working.Faction == Faction.OfMechanoids)
            {
                working.Kill(null);
            }
            else
            {
                Hediff hediff = HediffMaker.MakeHediff(def, working);
                working.health.AddHediff(hediff, null);
            }
            products = new List<Thing>();
            this.lightningCount = 15;
            this.target = working;
            lightning = DefDatabase<EffecterDef>.GetNamed("NR_AutoMachineTool_Effect_Lightning").Spawn();
            lightning.EffectTick(new TargetInfo(this), new TargetInfo(this.target));
            this.MapManager.EachTickAction(this.Lightning);
            return true;
        }

        protected bool Lightning()
        {
            this.lightningCount--;
            var result = this.lightningCount <= 0;
            if(result && this.lightning != null)
            {
                this.lightning.Cleanup();
            }
            return result;
        }

        [Unsaved]
        private int lightningCount = 0;
        [Unsaved]
        private Pawn target;
        [Unsaved]
        private Effecter lightning;
    }

    public class Building_StunnerTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.stunnerSetting.minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.stunnerSetting.maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return GenRadial.RadialCellsAround(pos, range, true);
        }

        public override bool NeedClearingCache => false;
    }
}
