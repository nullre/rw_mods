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
    public abstract class _Building_Base<T> : Building, IPowerSupplyMachine, IBeltConbeyorSender where T : Thing
    {
        protected ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }

        protected abstract float SpeedFactor { get; }
        protected virtual int? SkillLevel { get => null; }

        public abstract int MinPowerForSpeed { get; }
        public abstract int MaxPowerForSpeed { get; }

        private static HashSet<T> workingSet = new HashSet<T>();

        public virtual float SupplyPowerForSpeed
        {
            get => this.supplyPowerForSpeed;
            set
            {
                if (this.supplyPowerForSpeed != value)
                {
                    this.supplyPowerForSpeed = value;
                    this.SetPower();
                }
            }
        }

        protected readonly static List<Thing> emptyThingList = Enumerable.Empty<Thing>().ToList();

        private float supplyPowerForSpeed;
        private WorkingState state;
        private T working;

        protected List<Thing> products = new List<Thing>();

        private float totalWorkAmount;
        private int workStartTick;

        protected WorkingState State => this.state;

        protected T Working => this.working;

        protected void ClearActions()
        {
            this.MapManager.RemoveAfterAction(this.Ready);
            this.MapManager.RemoveAfterAction(this.Placing);
            this.MapManager.RemoveAfterAction(this.CheckWork);
            this.MapManager.RemoveAfterAction(this.StartWork);
            this.MapManager.RemoveAfterAction(this.FinishWork);
        }

        [Unsaved]
        private Effecter progressBar;
        [Unsaved]
        protected bool setInitialMinPower = true;

        protected virtual bool WorkingIsDespawned()
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<WorkingState>(ref this.state, "workingState", WorkingState.Ready);
            Scribe_Values.Look<float>(ref this.totalWorkAmount, "totalWorkAmount", 0f);
            Scribe_Values.Look<int>(ref this.workStartTick, "workStartTick", 0);
            Scribe_Collections.Look<Thing>(ref this.products, "products", LookMode.Deep);
            if (WorkingIsDespawned())
                Scribe_Deep.Look<T>(ref this.working, "working");
            else
                Scribe_References.Look<T>(ref this.working, "working");

            this.products = this.products ?? new List<Thing>();
            
            this.ReloadSettings(null, null);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            if (this.working == null && this.state == WorkingState.Working)
                this.state = WorkingState.Ready;
            if (this.products.Count == 0 && this.state == WorkingState.Placing)
                this.state = WorkingState.Ready;
        }

        protected virtual void ReloadSettings(object sender, EventArgs e)
        {
            if (this.SupplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.SupplyPowerForSpeed = this.MinPowerForSpeed;
            }
            if (this.SupplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.SupplyPowerForSpeed = this.MaxPowerForSpeed;
            }
        }

        protected static bool InWorking(T thing)
        {
            return workingSet.Contains(thing);
        }

        protected abstract float GetTotalWorkAmount(T working);

        protected virtual void SettingValues()
        {
            this.powerComp.PowerOutput = this.SupplyPowerForSpeed;
        }

        protected CompPowerTrader powerComp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapManager = map.GetComponent<MapTickManager>();
            this.powerComp = this.TryGetComp<CompPowerTrader>();

            if (!respawningAfterLoad)
            {
                this.products = new List<Thing>();
                if (setInitialMinPower)
                    this.SupplyPowerForSpeed = this.MinPowerForSpeed;
            }
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;

            if (this.state == WorkingState.Ready)
            {
                MapManager.AfterAction(Rand.Range(0, 30), this.Ready);
            }
            else if (this.state == WorkingState.Working)
            {
                MapManager.NextAction(this.StartWork);
            }
            else if (this.state == WorkingState.Placing)
            {
                MapManager.NextAction(this.Placing);
            }
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => this.mapManager;

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.ClearActions();
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
            this.Reset();
            base.DeSpawn();
        }

        protected virtual bool IsActive()
        {
            if (this.powerComp == null || !this.powerComp.PowerOn)
            {
                return false;
            }
            if (this.Destroyed)
            {
                return false;
            }

            return true;
        }

        protected virtual void Reset()
        {
            if (this.state != WorkingState.Ready)
            {
                this.CleanupProgressBar();
                this.products.ForEach(t =>
                {
                    if (!t.Spawned)
                    {
                        GenPlace.TryPlaceThing(t, this.Position, this.Map, ThingPlaceMode.Near);
                    }
                });
            }
            this.state = WorkingState.Ready;
            this.totalWorkAmount = 0;
            this.workStartTick = 0;
            Option(this.working).ForEach(h => workingSet.Remove(h));
            this.working = null;
            this.products.Clear();
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void CreateProgressBar()
        {
            this.CleanupProgressBar();
            if (this.working.Spawned)
            {
                this.progressBar = DefDatabase<EffecterDef>.GetNamed("NR_AutoMachineTool_ProgressBar").Spawn();
                this.progressBar.EffectTick(ProgressBarTarget(), TargetInfo.Invalid);
                ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => (this.CurrentWorkAmount / this.totalWorkAmount);
            }
        }

        protected virtual TargetInfo ProgressBarTarget()
        {
            return this.working;
        }

        protected virtual void SetPower()
        {
            if(this.SupplyPowerForSpeed != this.powerComp.PowerOutput)
            {
                this.powerComp.PowerOutput = -this.SupplyPowerForSpeed;
            }
        }

        protected virtual float Factor2()
        {
            return 0.1f;
        }

        protected virtual void Ready()
        {
            if (this.state != WorkingState.Ready || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }

            if (this.TryStartWorking(out this.working))
            {
                this.state = WorkingState.Working;
                this.totalWorkAmount = GetTotalWorkAmount(this.working);
                this.workStartTick = Find.TickManager.TicksAbs;
                MapManager.NextAction(this.StartWork);
                MapManager.NextAction(this.CheckWork);
            }
            else
            {
                MapManager.AfterAction(30, Ready);
            }
        }

        private int CalcRemainTick()
        {
            return Mathf.Max(1, Mathf.CeilToInt((this.totalWorkAmount - this.CurrentWorkAmount) / this.WorkPerTick));
        }

        protected float CurrentWorkAmount => (Find.TickManager.TicksAbs - this.workStartTick) * WorkPerTick;

        protected float WorkPerTick => 0.01f * this.SpeedFactor * this.SupplyPowerForSpeed * this.Factor2();

        protected float WorkLeft => this.totalWorkAmount - this.CurrentWorkAmount;

        protected virtual void StartWork()
        {
            if (this.state != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }
            CreateProgressBar();
            MapManager.AfterAction(30, this.CheckWork);
            MapManager.AfterAction(this.CalcRemainTick(), this.FinishWork);
        }

        protected void ForceStartWork(T working)
        {
            this.Reset();
            this.ClearActions();

            this.state = WorkingState.Working;
            this.working = working;
            this.totalWorkAmount = GetTotalWorkAmount(this.working);
            this.workStartTick = Find.TickManager.TicksAbs;
            MapManager.NextAction(StartWork);
        }

        protected virtual void CheckWork()
        {
            if (this.state != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.RemoveAfterAction(this.FinishWork);
                MapManager.AfterAction(30, Ready);
                return;
            }
            if(this.CurrentWorkAmount >= this.totalWorkAmount)
            {
                // 作業中に電力が変更されて終わってしまった場合、次TickでFinish呼び出し.
                // 動作中は電力変更できないようにする.
                MapManager.RemoveAfterAction(this.FinishWork);
                MapManager.NextAction(this.FinishWork);
            }
            MapManager.AfterAction(30, this.CheckWork);
        }

        protected virtual void FinishWork()
        {
            if (this.state != WorkingState.Working || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }
            if (this.WorkIntrruption(this.working))
            {
                this.Reset();
                MapManager.NextAction(this.Ready);
                return;
            }
            if (this.FinishWorking(this.working, out this.products))
            {
                this.state = WorkingState.Placing;
                this.CleanupProgressBar();
                this.working = null;
                MapManager.RemoveAfterAction(this.CheckWork);
                MapManager.NextAction(this.Placing);
            }
            else
            {
                this.Reset();
                MapManager.NextAction(this.Ready);
            }
        }

        protected virtual void Placing()
        {
            if(this.state != WorkingState.Placing || !this.Spawned)
            {
                return;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }

            if (this.PlaceProduct(ref this.products))
            {
                this.state = WorkingState.Ready;
                this.Reset();
                MapManager.NextAction(Ready);
            }
            else
            {
                MapManager.AfterAction(30, this.Placing);
            }
        }

        protected abstract bool WorkIntrruption(T working);

        protected abstract bool TryStartWorking(out T target);

        protected abstract bool FinishWorking(T working, out List<Thing> products);

        protected virtual bool PlaceProduct(ref List<Thing> products)
        {
            products = products.Aggregate(new List<Thing>(), (total, target) => {
                var conveyor = OutputCell().GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as IBeltConbeyorLinkable))
                    .Where(b => !b.IsUnderground)
                    .FirstOption();
                if (conveyor.HasValue)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.Value.ReceiveThing(false, target))
                    {
                        return total;
                    }
                }
                else
                {
                    // ない場合は適当に流す.
                    if (target.Spawned) target.DeSpawn();
                    if (!PlaceItem(target, OutputCell(), false, this.Map))
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                    }
                    return total;
                }
                return total.Append(target);
            });
            return this.products.Count() == 0;
        }

        public virtual IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.state)
            {
                case WorkingState.Working:
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.CurrentWorkAmount), Mathf.RoundToInt(this.totalWorkAmount), Mathf.RoundToInt(((this.CurrentWorkAmount) / this.totalWorkAmount) * 100));
                    break;
                case WorkingState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(this.products.Count);
                    break;
                default:
                    msg += this.state.ToString();
                    break;
            }
            if (this.SkillLevel.HasValue)
            {
                msg += "\n";
                msg += "NR_AutoMachineTool.SkillLevel".Translate(this.SkillLevel.ToString());
            }
            return msg;
        }

        public void NortifyReceivable()
        {
            this.Placing();
        }

        protected List<Thing> CreateThings(ThingDef def, int count)
        {
            var quot = count / def.stackLimit;
            var remain = count % def.stackLimit;

            return Enumerable.Range(0, quot + 1)
                .Select((c, i) => i == quot ? remain : def.stackLimit)
                .Select(c =>
                {
                    Thing p = ThingMaker.MakeThing(def, null);
                    p.stackCount = c;
                    return p;
                }).ToList();
        }
    }
}
