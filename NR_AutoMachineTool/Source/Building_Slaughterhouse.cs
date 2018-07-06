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
    public class Building_Slaughterhouse : Building_BaseRange<Pawn>, ISlaughterhouse
    {
        protected override float SpeedFactor { get => this.Setting.slaughterSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.slaughterSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.slaughterSetting.maxSupplyPowerForSpeed; }
        public override int MinPowerForRange { get => this.Setting.slaughterSetting.minSupplyPowerForRange; }
        public override int MaxPowerForRange { get => this.Setting.slaughterSetting.maxSupplyPowerForRange; }

        public Dictionary<ThingDef, SlaughterSettings> Settings { get => this.slaughterSettings; }

        private Dictionary<ThingDef, SlaughterSettings> slaughterSettings = new Dictionary<ThingDef, SlaughterSettings>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look<ThingDef, SlaughterSettings>(ref this.slaughterSettings, "slaughterSettings", LookMode.Def, LookMode.Deep);
        }

        protected override void Reset()
        {
            if (this.Working != null && this.Working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.Working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            base.Reset();
        }

        private HashSet<Pawn> ShouldSlaughterPawns()
        {
            var mapPawns = this.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            return this.slaughterSettings.Values.Where(s => s.doSlaughter).SelectMany(s =>
            {
                var pawns = mapPawns.Where(p => p.def == s.def);
                Func<Pawn, bool> where = (p) =>
                {
                    bool result = true;
                    if (result && !s.hasBonds) result = p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) == null;
                    if (result && !s.pregnancy) result = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant, true) == null;
                    if (result && !s.trained) result = !p.training.HasLearned(TrainableDefOf.Obedience);
                    return result;
                };
                Func<IEnumerable<Pawn>, bool, IOrderedEnumerable<Pawn>> orderBy = (e, adult) =>
                {
                    if (adult) return e.OrderByDescending(p => p.ageTracker.AgeChronologicalTicks);
                    else return e.OrderBy(p => p.ageTracker.AgeChronologicalTicks);
                };
                return new[] { new { Gender = Gender.Male, Adult = true }, new { Gender = Gender.Female, Adult = true }, new { Gender = Gender.Male, Adult = false }, new { Gender = Gender.Female, Adult = false } }
                    .Select(a => new { Group = a, Pawns = pawns.Where(p => p.gender == a.Gender && p.IsAdult() == a.Adult) })
                    .Select(g => new { Group = g.Group, Pawns = g.Pawns, SlaughterCount = g.Pawns.Count() - s.KeepCount(g.Group.Gender, g.Group.Adult) })
                    .Where(g => g.SlaughterCount > 0)
                    .SelectMany(g => orderBy(g.Pawns.Where(where), g.Group.Adult).Take(g.SlaughterCount));
            }).ToHashSet();
        }

        protected override float GetTotalWorkAmount(Pawn working)
        {
            return 400f;
        }

        protected override bool WorkIntrruption(Pawn working)
        {
            return working.Dead || !working.Spawned;
        }

        protected override bool TryStartWorking(out Pawn target)
        {
            target = null;
            var tmp = FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Pawn)
                .SelectMany(t => Option(t as Pawn))
                .Where(p => !InWorking(p))
                .Where(p => this.slaughterSettings.ContainsKey(p.def));
            if (!tmp.FirstOption().HasValue)
            {
                return false;
            }
            var targets = ShouldSlaughterPawns();
            target = tmp.Where(p => targets.Contains(p))
                .FirstOption()
                .GetOrDefault(null);
            if (target != null)
            {
                PawnUtility.ForceWait(target, 15000, null, true);
            }
            return target != null;
        }

        protected override bool FinishWorking(Pawn working, out List<Thing> products)
        {
            if (working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            int num = Mathf.Max(GenMath.RoundRandom(working.BodySize * 8f), 1);
            for (int i = 0; i < num; i++)
            {
                working.health.DropBloodFilth();
            }
            working.Kill(null);
            products = new List<Thing>().Append(working.Corpse);
            working.Corpse.DeSpawn();
            working.Corpse.SetForbidden(false);
            return true;
        }
    }
}
