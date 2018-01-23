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
    public class Building_AnimalResourceGatherer : Building , IAgricultureMachine, IBeltConbeyorSender, IProductLimitation
    {
        public enum GatherState
        {
            Ready,
            Gathering,
            Placing
        }
        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private ModExtension_AutoMachineTool Extension { get => this.def.GetModExtension<ModExtension_AutoMachineTool>(); }
        
        private float SpeedFactor { get => this.Setting.gathererSpeedFactor; }

        public int MinPowerForSpeed { get => this.Setting.minGathererSupplyPowerForSpeed; }
        public int MaxPowerForSpeed { get => this.Setting.maxGathererSupplyPowerForSpeed; }
        public int MinPowerForRange { get => this.Setting.minGathererSupplyPowerForRange; }
        public int MaxPowerForRange { get => this.Setting.maxGathererSupplyPowerForRange; }

        private static HashSet<CompHasGatherableBodyResource> gatheringSet = new HashSet<CompHasGatherableBodyResource>();
        
        private static Func<CompHasGatherableBodyResource, ThingDef> resourceDefGetter;

        private static Func<CompHasGatherableBodyResource, int> resourceAmountGetter;

        private static Action<CompHasGatherableBodyResource, float> fullnessSetter;

        static Building_AnimalResourceGatherer()
        {
            var compType = typeof(CompHasGatherableBodyResource);
            resourceDefGetter = GenerateGetterDelegate<CompHasGatherableBodyResource, ThingDef>(compType.GetProperty("ResourceDef", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
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

        public float SupplyPowerForSpeed
        {
            get
            {
                return this.supplyPowerForSpeed;
            }

            set
            {
                this.supplyPowerForSpeed = value;
                this.SetPower();
            }
        }

        public float SupplyPowerForRange
        {
            get
            {
                return this.supplyPowerForRange;
            }

            set
            {
                this.supplyPowerForRange = value;
                this.SetPower();
            }
        }

        public int ProductLimitCount { get => this.productLimitCount; set => this.productLimitCount = value; }
        public bool ProductLimitation { get => this.productLimitation; set => this.productLimitation = value; }

        private int productLimitCount = 100;
        private bool productLimitation = false;

        private float supplyPowerForSpeed;
        private float supplyPowerForRange;
        private GatherState state;
        private float workLeft;
        private Pawn animal;
        private List<Thing> products;
        private string gatheringTypeName;

        [Unsaved]
        private Effecter progressBar;
        [Unsaved]
        private CompHasGatherableBodyResource gathering;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look<bool>(ref this.productLimitation, "productLimitation", false);

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
            Scribe_Values.Look<GatherState>(ref this.state, "working");
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f);

            Scribe_Collections.Look<Thing>(ref this.products, "products", LookMode.Deep);
            Scribe_References.Look<Pawn>(ref this.animal, "animal");

            if (this.gathering != null)
            {
                this.gatheringTypeName = this.gathering.GetType().FullName;
            }
            Scribe_Values.Look<string>(ref this.gatheringTypeName, "gatherType", null);
            if(this.gatheringTypeName != null && animal != null)
            {
                this.gathering = animal.GetComps<CompHasGatherableBodyResource>().Where(c => c.GetType().FullName == this.gatheringTypeName).FirstOption().GetOrDefault(() => null);
            }

            this.ReloadSettings(null, null);
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
            }
            if (this.supplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MaxPowerForSpeed;
            }
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            if (this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = this.MaxPowerForRange;
            }
        }

        private void SettingValues()
        {
            this.TryGetComp<CompPowerTrader>().PowerOutput = this.supplyPowerForSpeed + this.supplyPowerForRange;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
                this.products = new List<Thing>();
            }

            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        public override void DeSpawn()
        {
            this.Reset();
            base.DeSpawn();

            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
        }

        public int GetRange()
        {
            return Mathf.RoundToInt(this.supplyPowerForRange / 500) + 2;
        }

        private bool IsActive()
        {
            if (this.TryGetComp<CompPowerTrader>() == null || !this.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return false;
            }
            if (this.Destroyed)
            {
                return false;
            }

            return true;
        }

        private void Reset()
        {
            if (this.state != GatherState.Ready)
            {
                this.CleanupProgressBar();
                this.products.ForEach(t =>
                {
                    if (!t.Spawned)
                    {
                        GenPlace.TryPlaceThing(t, this.Position, this.Map, ThingPlaceMode.Near);
                    }
                });
                if (this.animal != null && this.animal.jobs.curJob.def == JobDefOf.WaitMaintainPosture)
                {
                    this.animal.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            }
            this.state = GatherState.Ready;
            this.workLeft = 0;
            Option(this.gathering).ForEach(g => gatheringSet.Remove(g));
            this.gathering = null;
            this.animal = null;
            this.products.Clear();
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
            this.progressBar.EffectTick(this.animal, TargetInfo.Invalid);
            Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (WorkAmount(this.gathering) - this.workLeft) / WorkAmount(this.gathering));
        }

        private void SetPower()
        {
            if(-this.supplyPowerForRange - this.supplyPowerForSpeed != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.supplyPowerForRange - this.supplyPowerForSpeed;
            }
        }

        public override void Tick()
        {
            base.Tick();

            this.SetPower();

            if (!this.IsActive())
            {
                this.Reset();
                return;
            }

            if (this.state == GatherState.Ready)
            {
                if (Find.TickManager.TicksGame % 30 == 25 || this.checkNextReady)
                {
                    this.TryStartGathering();
                    this.checkNextReady = false;
                }
            }
            else if (this.state == GatherState.Gathering)
            {
                this.workLeft -= 0.01f * this.SpeedFactor * this.supplyPowerForSpeed * 0.1f;
                this.UpdateProgressBar();
                if (this.workLeft <= 0f)
                {
                    this.FinishGathering();
                    this.CleanupProgressBar();
                    this.checkNextPlace = true;
                }
                else if (!this.gathering.ActiveAndFull)
                {
                    this.Reset();
                }
            }
            else if (this.state == GatherState.Placing)
            {
                if (Find.TickManager.TicksGame % 25 == 21 || checkNextPlace)
                {
                    if (this.PlaceProduct())
                    {
                        this.Reset();
                        this.checkNextReady = true;
                    }
                    this.checkNextPlace = false;
                }
            }
        }

        [Unsaved]
        private bool checkNextReady = false;

        [Unsaved]
        private bool checkNextPlace = false;

        private static float WorkAmount(CompHasGatherableBodyResource comp)
        {
            if (comp is CompShearable)
            {
                return 1700f;
            }
            else if (comp is CompMilkable)
            {
                return 400f;
            }
            return 1000f;
        }

        private void TryStartGathering()
        {
            FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetGatherable(this.Map))
                .SelectMany(a => a.GetComps<CompHasGatherableBodyResource>().Select(c => new { Animal = a, Comp = c }))
                .Where(a => a.Comp.ActiveAndFull)
                .Where(a => !this.ProductLimitation || this.Map.resourceCounter.GetCount(resourceDefGetter(a.Comp)) < this.ProductLimitCount)
                .FirstOption()
                .ForEach(a =>
                {
                    this.animal = a.Animal;
                    this.gathering = a.Comp;
                    this.workLeft = WorkAmount(this.gathering);
                    this.state = GatherState.Gathering;
                    gatheringSet.Add(this.gathering);
                    PawnUtility.ForceWait(animal, 15000, null, true);
                });
        }

        private void FinishGathering() 
        {
            if (this.gathering.ActiveAndFull)
            {
                gatheringSet.Remove(this.gathering);
                var resourceDef = resourceDefGetter(this.gathering);
                var resourceAmount = GenMath.RoundRandom((float)resourceAmountGetter(this.gathering) * this.gathering.Fullness);

                var quot = resourceAmount / resourceDef.stackLimit;
                var remain = resourceAmount % resourceDef.stackLimit;
                if(remain == 0)
                {
                    quot -= 1;
                    remain = resourceDef.stackLimit;
                }

                this.products = Enumerable.Range(0, quot + 1)
                    .Select((c, i) => i == quot ? remain : resourceDef.stackLimit)
                    .Select(c =>
                    {
                        Thing p = ThingMaker.MakeThing(resourceDef, null);
                        p.stackCount = c;
                        return p;
                    }).ToList();
            }

            if (this.animal.jobs.curJob.def == JobDefOf.WaitMaintainPosture)
            {
                this.animal.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            fullnessSetter(this.gathering, 0f);

            this.workLeft = 0;
            this.gathering = null;
            this.animal = null;
            this.state = GatherState.Placing;
        }

        private bool PlaceProduct()
        {
            this.products = this.products.Aggregate(new List<Thing>(), (total, target) => {
                var conveyor = OutputCell().GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as IBeltConbeyorLinkable))
                    .Where(b => !b.IsUnderground)
                    .FirstOption();
                if (conveyor.HasValue)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.Value.ReceivableNow(false))
                    {
                        conveyor.Value.ReceiveThing(target);
                        return total;
                    }
                }
                else
                {
                    // ない場合は適当に流す.
                    if (!PlaceItem(target, OutputCell(), false, this.Map))
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                    }
                    return total;
                }
                return total.Append(target);
            });
            var result = this.products.Count() == 0;
            if (result)
            {
                this.state = GatherState.Ready;
            }

            return result;
        }

        public IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.state)
            {
                case GatherState.Gathering:
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(WorkAmount(this.gathering) - this.workLeft), Mathf.RoundToInt(WorkAmount(this.gathering)), Mathf.RoundToInt(((WorkAmount(this.gathering) - this.workLeft) / WorkAmount(this.gathering)) * 100));
                    break;
                case GatherState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case GatherState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(1);
                    break;
                default:
                    msg += this.state.ToString();
                    break;
            }
            return msg;
        }

        public void NortifyReceivable()
        {
            this.checkNextPlace = true;
        }
    }
}
