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
    public class MapTickManager : MapComponent
    {
        public MapTickManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            var removeSet = this.eachTickActions.ToList().Where(f => f()).ToHashSet();
            removeSet.ForEach(r => this.eachTickActions.Remove(r));

            this.tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(s => s.ToList().ForEach(a => a()));
            /*
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var beforeCount = GC.CollectionCount(0);

            StringBuilder b = new StringBuilder();
            var tickers = this.tickActionsDict.GetOption(Find.TickManager.TicksGame);
            if (tickers.HasValue)
            {
                foreach(var a in tickers.Value.ToList())
                {
                    System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
                    sw2.Start();
                    a();
                    sw2.Stop();
                    var micros = (double)sw2.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency * 1000d * 1000d;
                    b.Append(a.Target.GetType().ToString() + "." + a.Method.Name + " / elapse : " + micros + "\n");
                }
            }

            var afterCount = GC.CollectionCount(0);

            sw.Stop();
            var millis = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency * 1000d;
            if (millis > 2d)
            {
                var actions = this.tickActionsDict.GetOption(Find.TickManager.TicksGame).GetOrDefault(new HashSet<Action>());
                L("millis : " + millis + " / gcCount : " + (afterCount - beforeCount));
                L("methods : " + b.ToString());
            }
            */

            this.tickActionsDict.Remove(Find.TickManager.TicksGame);
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            // ここでいいのか・・・？
            Option(Find.MainTabsRoot.OpenTab)
                .Select(r => r.TabWindow)
                .SelectMany(w => Option(w as MainTabWindow_Architect))
                .SelectMany(a => Option(a.selectedDesPanel))
                .Where(p => p.def.defName == "NR_AutoMachineTool_DesignationCategory")
                .ForEach(p => OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame());

            if (Find.Selector.FirstSelectedObject as IBeltConbeyorLinkable != null)
            {
                OverlayDrawHandler_UGConveyor.DrawOverlayThisFrame();
            }
        }

        private Dictionary<int, HashSet<Action>> tickActionsDict = new Dictionary<int, HashSet<Action>>();

        private HashSet<Func<bool>> eachTickActions = new HashSet<Func<bool>>();

        public void AfterAction(int ticks, Action act)
        {
            if (ticks < 1)
                ticks = 1;

            if (!this.tickActionsDict.TryGetValue(Find.TickManager.TicksGame + ticks, out HashSet<Action> set))
            {
                set = new HashSet<Action>();
                this.tickActionsDict[Find.TickManager.TicksGame + ticks] = set;
            }

            set.Add(act);
        }

        public void NextAction(Action act)
        {
            this.AfterAction(1, act);
        }

        public void EachTickAction(Func<bool> act)
        {
            this.eachTickActions.Add(act);
        }

        public void RemoveAfterAction(Action act)
        {
            this.tickActionsDict.ForEach(kv => kv.Value.Remove(act));
        }

        public void RemoveEachTickAction(Func<bool> act)
        {
            this.eachTickActions.Remove(act);
        }
    }
}
