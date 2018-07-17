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
    public interface IRange
    {
        int GetRange();
        IntVec3 Position { get; }
        Rot4 Rotation { get; }
    }

    public abstract class Building_BaseRange<T> : Building_BaseLimitation<T>, IRange, IAgricultureMachine where T : Thing
    {
        public int MinPowerForRange => this.Extension.TargetCellResolver.MinPowerForRange;
        public int MaxPowerForRange => this.Extension.TargetCellResolver.MaxPowerForRange;

        public virtual bool Glowable { get => false; }

        private bool glow = false;
        public virtual bool Glow
        {
            get => this.glow;
            set
            {
                if (this.glow != value)
                {
                    this.glow = value;
                    this.ChangeGlow();
                }
            }
        }

        protected ModExtension_AutoMachineTool Extension => this.def.GetModExtension<ModExtension_AutoMachineTool>();

        protected IEnumerable<IntVec3> GetTargetCells()
        {
            return this.Extension.TargetCellResolver.GetRangeCells(this.Position, this.Map, this.Rotation, this.GetRange());
        }

        public float SupplyPowerForRange
        {
            get => this.supplyPowerForRange;
            set
            {
                if(this.supplyPowerForRange != value)
                {
                    this.supplyPowerForRange = value;
                    this.ChangeGlow();
                }
                this.SetPower();
            }
        }

        private float supplyPowerForRange;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
            Scribe_Values.Look<bool>(ref this.glow, "glow", false);
        }

        public int GetRange()
        {
            return this.Extension.TargetCellResolver.GetRange(this.SupplyPowerForRange);
        }

        protected override void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            if (this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = this.MaxPowerForRange;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            Option(this.TryGetComp<CompGlower>()).ForEach(g =>
            {
                CompProperties_Glower newProp = new CompProperties_Glower();
                newProp.compClass = g.Props.compClass;
                newProp.glowColor = g.Props.glowColor;
                newProp.glowRadius = g.Props.glowRadius;
                newProp.overlightRadius = g.Props.overlightRadius;
                g.props = newProp;
            });
            this.ChangeGlow();
        }

        protected override void SetPower()
        {
            if (-this.supplyPowerForRange - this.SupplyPowerForSpeed - (this.Glowable && this.Glow ? 2000 : 0) != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.powerComp.PowerOutput = -this.supplyPowerForRange - this.SupplyPowerForSpeed - (this.Glowable && this.Glow ? 2000 : 0);
            }
        }

        private void ChangeGlow()
        {
            Option(this.TryGetComp<CompGlower>()).ForEach(glower =>
            {
                var tmp = this.TryGetComp<CompFlickable>().SwitchIsOn;
                glower.Props.glowRadius = this.Glow ? (this.GetRange() + 2f) * 2f : 0;
                glower.Props.overlightRadius = this.Glow ? (this.GetRange() + 2.1f) : 0;
                this.TryGetComp<CompFlickable>().SwitchIsOn = !tmp;
                this.TryGetComp<CompPowerTrader>().PowerOn = !tmp;
                glower.UpdateLit(this.Map);
                this.TryGetComp<CompFlickable>().SwitchIsOn = tmp;
                this.TryGetComp<CompPowerTrader>().PowerOn = tmp;
                glower.UpdateLit(this.Map);
            });
        }
    }
}
