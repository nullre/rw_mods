using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class Building_AnimalResourceGatherer : Building_BaseRange<Pawn>
    {
        private static Func<CompHasGatherableBodyResource, ThingDef> resourceDefGetter;
        private static Func<CompHasGatherableBodyResource, bool> activeGetter;
        private static Func<CompHasGatherableBodyResource, int> resourceAmountGetter;
        private static Action<CompHasGatherableBodyResource, float> fullnessSetter;

        static Building_AnimalResourceGatherer()
        {
            var compType = typeof(CompHasGatherableBodyResource);
            resourceDefGetter = GenerateMeshodDelegate<CompHasGatherableBodyResource, ThingDef>(compType.GetProperty("ResourceDef", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            activeGetter = GenerateMeshodDelegate<CompHasGatherableBodyResource, bool>(compType.GetProperty("Active", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            resourceAmountGetter = GenerateMeshodDelegate<CompHasGatherableBodyResource, int>(compType.GetProperty("ResourceAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            fullnessSetter = GenerateSetFieldDelegate<CompHasGatherableBodyResource, float>(compType.GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        protected override float SpeedFactor { get => this.Setting.gathererSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.gathererSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.gathererSetting.maxSupplyPowerForSpeed; }

        protected override void Reset()
        {
            if (this.Working != null && this.Working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.Working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            base.Reset();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (this.comp != null)
            {
                this.compName = this.comp.GetType().FullName;
            }
            Scribe_Values.Look<string>(ref this.compName, "compName", null);
            if (this.compName != null && this.Working != null)
            {
                this.comp = this.Working.GetComps<CompHasGatherableBodyResource>().Where(c => c.GetType().FullName == this.compName).FirstOption().GetOrDefault(null);
            }
        }

        private CompHasGatherableBodyResource comp;

        private string compName;

        protected override bool WorkInterruption(Pawn working)
        {
            return !activeGetter(this.comp) || this.comp.Fullness < 0.5f;
        }

        protected override bool TryStartWorking(out Pawn target, out float workAmount)
        {
            var animal = GetTargetCells()
                .SelectMany(c => c.GetThingList(this.Map))
                .SelectMany(t => Option(t as Pawn))
                .Where(p => p.Faction == Faction.OfPlayer)
                .Where(p => p.TryGetComp<CompHasGatherableBodyResource>() != null)
                .SelectMany(a => a.GetComps<CompHasGatherableBodyResource>().Select(c => new { Animal = a, Comp = c }))
                .Where(a => activeGetter(a.Comp) && a.Comp.Fullness >= 0.5f)
                .Where(a => !IsLimit(resourceDefGetter(a.Comp)))
                .FirstOption()
                .GetOrDefault(null);
            target = null;
            workAmount = 0f;
            if (animal != null)
            {
                target = animal.Animal;
                this.comp = animal.Comp;
                if (this.comp is CompShearable)
                {
                    workAmount = 1700f;
                }
                else if (this.comp is CompMilkable)
                {
                    workAmount = 400f;
                }
                workAmount = 1000f;
                PawnUtility.ForceWait(target, 15000, null, true);
            }
            return animal != null;
        }

        protected override bool FinishWorking(Pawn working, out List<Thing> products)
        {
            var def = resourceDefGetter(this.comp);
            var amount = GenMath.RoundRandom((float)resourceAmountGetter(this.comp) * this.comp.Fullness);
            
            products = CreateThings(def, amount);

            if (this.Working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.Working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            fullnessSetter(this.comp, 0f);
            this.comp = null;

            return true;
        }
    }

    public class Building_AnimalResourceGathererTargetCellResolver : BaseTargetCellResolver
    {
        public override int MinPowerForRange => this.Setting.gathererSetting.minSupplyPowerForRange;
        public override int MaxPowerForRange => this.Setting.gathererSetting.maxSupplyPowerForRange;

        public override IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range)
        {
            return FacingRect(pos, rot, range)
                .Where(c => (pos + rot.FacingCell).GetRoom(map) == c.GetRoom(map));
        }

        public override int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 1;
        }

        public override bool NeedClearingCache => true;
    }
}
