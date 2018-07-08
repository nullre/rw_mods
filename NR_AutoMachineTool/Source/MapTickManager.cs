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
