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
    public enum WorkingState
    {
        Ready,
        Working,
        Placing
    }

    public abstract class Building_Base<T> : Building, IPowerSupplyMachine, IBeltConbeyorSender where T : Thing
    {
        protected ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }

        protected abstract float SpeedFactor { get; }
        protected abstract int? SkillLevel { get; }

        public abstract int MinPowerForSpeed { get; }
        public abstract int MaxPowerForSpeed { get; }

        private static HashSet<T> workingSet = new HashSet<T>();

        public virtual float SupplyPowerForSpeed
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

        private float supplyPowerForSpeed;
        protected WorkingState state;
        protected float workLeft;
        protected T working;
        protected List<Thing> products;

        [Unsaved]
        private Effecter progressBar;

        protected virtual bool WorkingIsDespawned()
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<WorkingState>(ref this.state, "workingState", WorkingState.Ready);
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f);
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
            this.TryGetComp<CompPowerTrader>().PowerOutput = this.SupplyPowerForSpeed;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.SupplyPowerForSpeed = this.MinPowerForSpeed;
            }
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        public override void DeSpawn()
        {
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
            this.Reset();
            base.DeSpawn();
        }

        protected virtual bool IsActive()
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
            this.workLeft = 0;
            Option(this.working).ForEach(h => workingSet.Remove(h));
            this.working = null;
            this.products.Clear();
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            if (this.working.Spawned)
            {
                this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
                this.progressBar.EffectTick(this.working, TargetInfo.Invalid);
                Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (GetTotalWorkAmount(this.working) - this.workLeft) / GetTotalWorkAmount(this.working));
            }
        }

        protected virtual void SetPower()
        {
            if(this.SupplyPowerForSpeed != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.SupplyPowerForSpeed;
            }
        }

        protected virtual float Factor2()
        {
            return 0.1f;
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

            if (this.state == WorkingState.Ready)
            {
                if (Find.TickManager.TicksGame % 30 == 20 || this.checkNextReady)
                {
                    if (this.TryStartWorking(out this.working))
                    {
                        this.state = WorkingState.Working;
                        this.workLeft = GetTotalWorkAmount(this.working);
                    }
                    this.checkNextReady = false;
                }
            }
            else if (this.state == WorkingState.Working)
            {
                this.workLeft -= 0.01f * this.SpeedFactor * this.SupplyPowerForSpeed * this.Factor2();
                this.UpdateProgressBar();
                if (this.WorkIntrruption(this.working))
                {
                    this.Reset();
                }
                if (this.workLeft <= 0f)
                {
                    if(this.FinishWorking(this.working, out this.products))
                    {
                        this.state = WorkingState.Placing;
                        this.CleanupProgressBar();
                        this.checkNextPlace = true;
                        this.working = null;
                        this.workLeft = 0;
                    }
                    else
                    {
                        this.Reset();
                    }
                }
            }
            else if (this.state == WorkingState.Placing)
            {
                if (Find.TickManager.TicksGame % 30 == 20 || checkNextPlace)
                {
                    if (this.PlaceProduct(ref this.products))
                    {
                        this.state = WorkingState.Ready;
                        this.Reset();
                        this.checkNextReady = true;
                    }
                    this.checkNextPlace = false;
                }
            }
        }

        protected abstract bool WorkIntrruption(T working);

        [Unsaved]
        private bool checkNextReady = false;

        [Unsaved]
        private bool checkNextPlace = false;

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
                    if (conveyor.Value.ReceivableNow(false))
                    {
                        conveyor.Value.ReceiveThing(target);
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
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.GetTotalWorkAmount(this.working) - this.workLeft), Mathf.RoundToInt(this.GetTotalWorkAmount(this.working)), Mathf.RoundToInt(((this.GetTotalWorkAmount(this.working) - this.workLeft) / this.GetTotalWorkAmount(this.working)) * 100));
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
            this.checkNextPlace = true;
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
