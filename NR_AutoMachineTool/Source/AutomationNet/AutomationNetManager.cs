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
    public class AutomationNetManager : MapComponent
    {
        public List<AutomationNet> allNets = new List<AutomationNet>();

        private List<CompAutomation> newComps = new List<CompAutomation>();

        private List<CompAutomation> oldComps = new List<CompAutomation>();

        public AutomationNetManager(Map map) : base(map)
        {
            this.grid = new AutomationNetGrid(map);
        }

        public AutomationNetGrid grid;

        public void Notify_CompSpawn(CompAutomation comp)
        {
            this.newComps.Add(comp);
        }

        public void Notify_CompDespawn(CompAutomation comp)
        {
            this.oldComps.Add(comp);
        }

        public void RegisterNet(AutomationNet net)
        {
            this.allNets.Add(net);
            net.manager = this;
            this.grid.Notify_NetCreated(net);
        }

        public void DeleteNet(AutomationNet net)
        {
            this.allNets.Remove(net);
            this.grid.Notify_NetDeleted(net);
        }

        private void NetUpdate()
        {
            this.newComps
                .SelectMany(c => GenAdj.CellsAdjacentCardinal(c.parent))
                .Where(c => c.InBounds(this.map))
                .SelectMany(c => Option(this.grid.NetAt(c)))
                .ForEach(this.DeleteNet);

            this.oldComps
                .SelectMany(c => Option(this.grid.NetAt(c.parent.Position)))
                .ForEach(this.DeleteNet);

            this.newComps
                .Select(c => new { Comp = c, Net = Option(this.grid.NetAt(c.parent.Position)) })
                .Where(r => !r.Net.HasValue)
                .ForEach(r => this.RegisterNet(AutomationNetMaker.NewNetStartingFrom((Building)r.Comp.parent, this.map)));

            this.oldComps
                .SelectMany(c => GenAdj.CellsAdjacentCardinal(c.parent))
                .Where(c => c.InBounds(this.map))
                .SelectMany(c => c.GetThingList(this.map)
                    .SelectMany(t => Option(t as Building))
                    .Where(b => b.TryGetComp<CompAutomation>() != null)
                    .FirstOption()
                )
                .ForEach(b => this.RegisterNet(AutomationNetMaker.NewNetStartingFrom(b, this.map)));

            this.newComps.Clear();
            this.oldComps.Clear();

        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            this.allNets.ForEach(n => n.Tick());
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            this.NetUpdate();

            // ここでいいのか・・・？
            Option(Find.MainTabsRoot.OpenTab)
                .Select(r => r.TabWindow)
                .SelectMany(w => Option(w as MainTabWindow_Architect))
                .SelectMany(a => Option(a.selectedDesPanel))
                .Where(p => p.def.defName == "NR_AutoMachineTool_AutomationNet_DesignationCategory")
                .ForEach(p => OverlayDrawHandler_AutomationNet.DrawAitNetOverlayThisFrame());

            if (Find.Selector.FirstSelectedObject as Building_AutomationPipe != null)
            {
                OverlayDrawHandler_AutomationNet.DrawAitNetOverlayThisFrame();
            }
        }
    }
}
