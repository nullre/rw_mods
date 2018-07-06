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

            this.removeEachTickActions.ForEach(a => this.eachTickActions.Remove(a));
            this.removeEachTickActions.Clear();

            this.removeTickActions.ForEach(a => this.tickActionsDict.ForEach(kv => kv.Value.Remove(a)));
            this.removeTickActions.Clear();

            this.eachTickActions.AddRange(this.addEachTickActions);
            this.addEachTickActions.Clear();

            this.addTickActionsDict.ForEach(kv =>
            {
                List<Action> list = null;
                if (!this.tickActionsDict.TryGetValue(kv.Key, out list))
                {
                    list = new List<Action>();
                    this.tickActionsDict[kv.Key] = list;
                }
                list.AddRange(kv.Value);
            });

            this.addTickActionsDict.Clear();

            this.eachTickActions.RemoveAll(f =>
            {
                if (!this.removeEachTickActions.Contains(f))
                    return f();
                return true;
            });

            this.tickActionsDict.GetOption(Find.TickManager.TicksGame).ForEach(l => l.ForEach(a =>
            {
                if (!this.removeTickActions.Contains(a))
                    a();
            }));
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

        private Dictionary<int, List<Action>> tickActionsDict = new Dictionary<int, List<Action>>();

        private List<Func<bool>> eachTickActions = new List<Func<bool>>();

        private Dictionary<int, List<Action>> addTickActionsDict = new Dictionary<int, List<Action>>();

        private List<Func<bool>> addEachTickActions = new List<Func<bool>>();

        private List<Action> removeTickActions = new List<Action>();

        private List<Func<bool>> removeEachTickActions = new List<Func<bool>>();

        public void AfterAction(int ticks, Action act)
        {
            if (ticks < 1)
                ticks = 1;
            List<Action> list = null;
            if (!this.addTickActionsDict.TryGetValue(Find.TickManager.TicksGame + ticks, out list))
            {
                list = new List<Action>();
                this.addTickActionsDict[Find.TickManager.TicksGame + ticks] = list;
            }
            list.Add(act);
            this.removeTickActions.RemoveAll(a => a == act);
        }

        public void NextAction(Action act)
        {
            this.AfterAction(1, act);
        }

        public void EachTickAction(Func<bool> act)
        {
            this.removeEachTickActions.RemoveAll(a => a == act);
            this.addEachTickActions.Add(act);
        }

        public void RemoveAfterAction(Action act)
        {
            this.addTickActionsDict.ForEach(kv => kv.Value.RemoveAll(a => a == act));
            this.removeTickActions.Add(act);
        }

        public void RemoveEachTickAction(Func<bool> act)
        {
            this.addEachTickActions.RemoveAll(a => a == act);
            this.removeEachTickActions.Add(act);
        }
    }
}
