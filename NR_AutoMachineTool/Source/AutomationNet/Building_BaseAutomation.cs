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
    public abstract class Building_BaseAutomation<T> : Building where T : Thing
    {
        protected ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }

        private static HashSet<T> workingSet = new HashSet<T>();

        private WorkingState state;
        protected float workAmount;
        protected T working;
        protected List<Thing> products = new List<Thing>();

        protected WorkingState State
        {
            get { return this.state; }
            private set
            {
                if(this.state != value)
                {
                    OnChangeState(this.state, value);
                    this.state = value;
                }
            }
        }

        protected virtual void OnChangeState(WorkingState before, WorkingState after)
        {
        }

        [Unsaved]
        protected bool showProgressBar = true;

        [Unsaved]
        private Effecter progressBar;

        [Unsaved]
        private bool placeFirstAbsorb = true;

        protected virtual bool WorkingIsDespawned()
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<WorkingState>(ref this.state, "workingState", WorkingState.Ready);
            Scribe_Values.Look<float>(ref this.workAmount, "workAmount", 0f);
            Scribe_Collections.Look<Thing>(ref this.products, "products", LookMode.Deep);
            if (WorkingIsDespawned())
                Scribe_Deep.Look<T>(ref this.working, "working");
            else
                Scribe_References.Look<T>(ref this.working, "working");

            this.products = this.products ?? new List<Thing>();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            if (this.working == null && this.State == WorkingState.Working)
                this.State = WorkingState.Ready;
            if (this.products.Count == 0 && this.State == WorkingState.Placing)
                this.State = WorkingState.Ready;
        }

        protected static bool InWorking(T thing)
        {
            return workingSet.Contains(thing);
        }

        protected abstract float GetTotalWorkAmount(T working);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.mapManager = map.GetComponent<MapTickManager>();

            if (!respawningAfterLoad)
            {
                this.products = new List<Thing>();
            }
            if (this.State == WorkingState.Ready)
            {
                MapManager.AfterAction(Rand.Range(0, 30), Ready);
            }
            else if (this.State == WorkingState.Working)
            {
                MapManager.EachTickAction(Working);
            }
            else if (this.State == WorkingState.Placing)
            {
                MapManager.NextAction(Placing);
            }
        }

        private MapTickManager mapManager;
        protected MapTickManager MapManager => this.mapManager;

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.Reset();
            this.ClearActions();
            base.DeSpawn();
        }

        protected void ClearActions()
        {
            this.MapManager.RemoveAfterAction(this.Ready);
            this.MapManager.RemoveAfterAction(this.Placing);
            this.MapManager.EachTickAction(this.Working);
        }

        protected virtual bool IsActive()
        {
            if (this.Destroyed)
            {
                return false;
            }

            return true;
        }

        protected virtual void Reset()
        {
            if (this.State != WorkingState.Ready)
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
            this.State = WorkingState.Ready;
            this.workAmount = 0;
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
            if (this.working.Spawned && this.showProgressBar)
            {
                this.progressBar = Option(this.progressBar).GetOrDefaultF(EffecterDefOf.ProgressBar.Spawn);
                this.progressBar.EffectTick(ProgressBarTarget(), TargetInfo.Invalid);
                Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = this.workAmount / GetTotalWorkAmount(this.working));
            }
        }

        protected virtual TargetInfo ProgressBarTarget()
        {
            return this.working;
        }

        protected virtual float Factor2()
        {
            return 0.1f;
        }

        protected virtual void Ready()
        {
            if (this.State != WorkingState.Ready || !this.Spawned)
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
                this.State = WorkingState.Working;
                this.workAmount = 0;
                MapManager.EachTickAction(Working);
            }
            else
            {
                MapManager.AfterAction(30, Ready);
            }
        }

        protected virtual bool Working()
        {

            if (this.State != WorkingState.Working || !this.Spawned)
            {
                return true;
            }
            if (!this.IsActive())
            {
                this.Reset();
                MapManager.AfterAction(30, Ready);
                return true;
            }
            bool finish = false;
            this.workAmount += WorkAmountPerTick();
            this.UpdateProgressBar();
            if (this.WorkIntrruption(this.working))
            {
                this.Reset();
                MapManager.NextAction(this.Ready);
                finish = true;
            }
            else if (this.workAmount >= this.GetTotalWorkAmount(this.working))
            {
                if (this.FinishWorking(this.working, out this.products))
                {
                    this.State = WorkingState.Placing;
                    this.CleanupProgressBar();
                    this.working = null;
                    this.workAmount = 0;
                    MapManager.NextAction(this.Placing);
                }
                else
                {
                    this.Reset();
                    MapManager.NextAction(this.Ready);
                }
                finish = true;
            }

            return finish;
        }

        protected virtual void Placing()
        {
            if (this.State != WorkingState.Placing || !this.Spawned)
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
                this.State = WorkingState.Ready;
                this.Reset();
                MapManager.NextAction(Ready);
            }
            else
            {
                MapManager.AfterAction(30, this.Placing);
            }
        }

        protected abstract float WorkAmountPerTick();

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
                    if (!PlaceItem(target, OutputCell(), false, this.Map, this.placeFirstAbsorb))
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
            switch (this.State)
            {
                case WorkingState.Working:
                    if (float.IsInfinity(this.GetTotalWorkAmount(this.working)))
                    {
                        msg += "NR_AutoMachineTool.StatWorkingNoParam".Translate();
                    }
                    else
                    {
                        msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(this.workAmount), Mathf.RoundToInt(this.GetTotalWorkAmount(this.working)), Mathf.RoundToInt(((this.workAmount) / this.GetTotalWorkAmount(this.working)) * 100));
                    }
                    break;
                case WorkingState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(this.products.Count);
                    break;
                default:
                    msg += this.State.ToString();
                    break;
            }
            return msg;
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
