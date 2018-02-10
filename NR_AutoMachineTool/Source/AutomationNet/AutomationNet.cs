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
    public class AutomationNet
    {
        public AutomationNetManager manager;

        public List<CompAutomation> nodes;
        private List<CompAutomationEnergyConsumer> consumers;
        private List<CompAutomationEnergySupplier> suppliers;

        public AutomationNet(IEnumerable<CompAutomation> nodes)
        {
            this.nodes = nodes.ToList();
            this.nodes.ForEach(n => n.connectedNet = this);
            this.consumers = this.nodes.SelectMany(n => Option(n as CompAutomationEnergyConsumer)).ToList();
            this.suppliers = this.nodes.SelectMany(n => Option(n as CompAutomationEnergySupplier)).ToList();
        }

        public float CurrentSuppliedEnergy()
        {
            return this.suppliers.Select(t => t.SupplyEnergyPerTick).Sum();
        }

        private void DistributeEnergy()
        {
            var requesting = this.consumers.Where(c => c.requesting).ToList();
            if (requesting.Count > 0)
            {
                requesting.ForEach(c => c.suppliedEnergy = this.CurrentSuppliedEnergy() / requesting.Count);
            }
        }

        private HashSet<CompAutomationEnergyConsumer> requesting = new HashSet<CompAutomationEnergyConsumer>();

        public void Tick()
        {
            if (Find.TickManager.TicksGame % 30 == 0)
            {
                this.DistributeEnergy();
            }
        }
    }
}
