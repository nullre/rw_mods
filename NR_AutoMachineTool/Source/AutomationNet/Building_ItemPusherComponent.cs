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
    public class Building_ItemPusherComponent : Building_BaseComponent<Thing>, IThingFilter
    {
        private ThingFilter filter = new ThingFilter();

        private int keepCount = 10;

        public ThingFilter Filter => this.filter;

        public int MaxCount { get => Math.Max(1, Mathf.RoundToInt(this.consumer.UsableEnergyNow / 10)); }
        public int? Count { get => this.keepCount; set => this.keepCount = value ?? 10 ; }

        protected override float GetTotalWorkAmount(Thing working)
        {
            return working.stackCount * 100;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look<ThingFilter>(ref this.filter, "filter");
            Scribe_Values.Look<int>(ref this.keepCount, "keepCount");
        }

        protected override void Reset()
        {
            if(this.State == WorkingState.Working)
            {
                Option(this.working).ForEach(t => GenPlace.TryPlaceThing(t, this.OutputCell(), this.Map, ThingPlaceMode.Near));
            }
            base.Reset();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Option(this.Map.haulDestinationManager.SlotGroupAt(this.Position + this.Rotation.FacingCell))
                    .ForEach(g => this.filter.CopyAllowancesFrom(g.Settings.filter));
            }
        }

        protected override Thing TargetThing()
        {
            var storage = this.consumer.connectedNet.StorageItems().Where(t => this.filter.Allows(t)).ToList();
            return Option(this.Map.haulDestinationManager.SlotGroupAt(this.Position + this.Rotation.FacingCell))
                .SelectMany(g =>
                {
                    var stored = g.HeldThings.Where(t => this.filter.Allows(t))
                        .Select(t => new { Def = t.def, Count = t.stackCount })
                        .GroupBy(a => a.Def)
                        .Select(a => new { Def = a.Key, Count = a.Sum(b => b.Count) })
                        .ToDictionary(a => a.Def);
                    var notfull = g.HeldThings.Where(t => this.filter.Allows(t))
                        .Where(t => t.stackCount < t.def.stackLimit)
                        .Select(t => new { Def = t.def, Count = t.stackCount })
                        .GroupBy(a => a.Def)
                        .Select(a => new { Def = a.Key, Storeable = (a.Count() * a.Key.stackLimit) - a.Sum(b => b.Count) })
                        .ToDictionary(a => a.Def);

                    return storage
                        .Where(t => stored.ContainsKey(t.def) && stored[t.def].Count < this.keepCount)
                        .Select(t => new { Thing = t, Need = notfull.GetOption(t.def).Select(a => a.Storeable).GetOrDefaultF(() => this.keepCount - stored[t.def].Count) })
                        .Where(a => a.Need > 0)
                        .Concat(storage
                            .Where(t => !stored.ContainsKey(t.def))
                            .Select(t => new { Thing = t, Need = this.keepCount })
                        )
                        .Where(a => g.CellsList.Any(c => c.IsValidStorageFor(this.Map, a.Thing)))
                        .FirstOption();
                })
                .Select(a => a.Thing.SplitOff(Math.Min(MaxCount, Math.Min(a.Thing.stackCount, a.Need))))
                .GetOrDefault(null);
        }

        protected override float WorkAmountPerTick()
        {
            return this.consumer.suppliedEnergy * 0.01f;
        }

        protected override void WorkingTick(Thing working, float workAmount)
        {
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            var r = base.FinishWorking(working, out products);
            products.Append(working);
            return r;
        }

        protected override bool WorkIntrruption(Thing working)
        {
            return false;
        }
    }
}
