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
            resourceDefGetter = GenerateGetterDelegate<CompHasGatherableBodyResource, ThingDef>(compType.GetProperty("ResourceDef", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            activeGetter = GenerateGetterDelegate<CompHasGatherableBodyResource, bool>(compType.GetProperty("Active", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            resourceAmountGetter = GenerateGetterDelegate<CompHasGatherableBodyResource, int>(compType.GetProperty("ResourceAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
            fullnessSetter = GenerateSetFieldDelegate<CompHasGatherableBodyResource, float>(compType.GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        private static Func<T, TValue> GenerateGetterDelegate<T, TValue>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            return Expression.Lambda<Func<T, TValue>>(
                Expression.Call(instanceParam, getter),
                instanceParam).Compile();
        }

        private static Action<T, TValue> GenerateSetFieldDelegate<T, TValue>(FieldInfo field)
        {
            var d = new DynamicMethod("setter", typeof(void), new Type[] { typeof(T), typeof(TValue) }, true);
            var g = d.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Stfld, field);
            g.Emit(OpCodes.Ret);

            return (Action<T, TValue>)d.CreateDelegate(typeof(Action<T, TValue>));
        }

        protected override float SpeedFactor { get => this.Setting.gathererSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.gathererSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.gathererSetting.maxSupplyPowerForSpeed; }
        public override int MinPowerForRange { get => this.Setting.gathererSetting.minSupplyPowerForRange; }
        public override int MaxPowerForRange { get => this.Setting.gathererSetting.maxSupplyPowerForRange; }

        protected override void Reset()
        {
            if (this.working != null && this.working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
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
            if (this.compName != null && this.working != null)
            {
                this.comp = this.working.GetComps<CompHasGatherableBodyResource>().Where(c => c.GetType().FullName == this.compName).FirstOption().GetOrDefault(null);
            }
        }

        private CompHasGatherableBodyResource comp;

        private string compName;

        protected override float GetTotalWorkAmount(Pawn working)
        {
            if (this.comp is CompShearable)
            {
                return 1700f;
            }
            else if (this.comp is CompMilkable)
            {
                return 400f;
            }
            return 1000f;
        }

        protected override bool WorkIntrruption(Pawn working)
        {
            return !activeGetter(this.comp) || this.comp.Fullness < 0.5f;
        }

        protected override bool TryStartWorking(out Pawn target)
        {
            var animal = FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetGatherable(this.Map))
                .SelectMany(a => a.GetComps<CompHasGatherableBodyResource>().Select(c => new { Animal = a, Comp = c }))
                .Where(a => activeGetter(a.Comp) && a.Comp.Fullness >= 0.5f)
                .Where(a => !IsLimit(resourceDefGetter(a.Comp)))
                .FirstOption()
                .GetOrDefault(null);
            target = null;
            if (animal != null)
            {
                target = animal.Animal;
                this.comp = animal.Comp;
                PawnUtility.ForceWait(target, 15000, null, true);
            }
            return animal != null;
        }

        protected override bool FinishWorking(Pawn working, out List<Thing> products)
        {
            var def = resourceDefGetter(this.comp);
            var amount = GenMath.RoundRandom((float)resourceAmountGetter(this.comp) * this.comp.Fullness);
            
            products = CreateThings(def, amount);

            if (this.working.jobs.curJob.def == JobDefOf.Wait_MaintainPosture)
            {
                this.working.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            fullnessSetter(this.comp, 0f);
            this.comp = null;

            return true;
        }
    }
}
