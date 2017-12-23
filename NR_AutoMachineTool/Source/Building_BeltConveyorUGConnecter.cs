using System;
using System.IO;
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
    class Building_BeltConveyorUGConnecter : Building , IBeltConbeyorLinkable, IPowerSupplyMachine, IBeltConbeyorSender
    {
        private enum CarryState
        {
            Ready,
            Carring,
            Placing
        }

        private CarryState state = CarryState.Ready;
        private float move = 0;
        private Thing thing = null;

        private static int shift;

        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private float SpeedFactor { get => this.Setting.carrySpeedFactor; }
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        public float SupplyPower { get => Building_BeltConveyor.supplyPower; set => Building_BeltConveyor.supplyPower = (int)value; }
        public int MinPower { get => this.Setting.minBeltConveyorSupplyPower; }
        public int MaxPower { get => this.Setting.maxBeltConveyorSupplyPower; }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.move, "move");
            Scribe_Values.Look<CarryState>(ref this.state, "state");
            Scribe_Deep.Look<Thing>(ref this.thing, "thing");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                LinkTargetConveyor().ForEach(x =>
                {
                    x.Link(this);
                    this.Link(x);
                });
            }
            shift++;
            if(shift >= 30)
            {
                shift = 0;
            }
        }
        
        public override void DeSpawn()
        {
            var targets = LinkTargetConveyor();
            this.Reset();

            base.DeSpawn();

            targets.ForEach(x => x.Unlink(this));
        }

        private void Reset()
        {
            if (this.state != CarryState.Ready)
            {
                Option(this.thing).ForEach(t => GenPlace.TryPlaceThing(t, this.Position, this.Map, ThingPlaceMode.Near));
                this.thing = null;
                this.state = CarryState.Ready;
                this.move = 0;
            }
        }
        
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (this.state != CarryState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.move < 0.3f)
                {
                    Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                    result.y = (float)UI.screenHeight - result.y;
                    GenMapUI.DrawThingLabel(result, this.thing.stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
                }
            }
        }
        
        public override void Draw()
        {
            base.Draw();

            if (this.state != CarryState.Ready)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.move < 0.3f)
                {
                    this.thing.DrawAt(p);
                }
            }

            var pos = this.Position.ToVector3() + new Vector3(0.5f, this.def.Altitude + 1f, 0.5f);

            var mat1 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor991_arrow");
            var mat2 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor992_arrow");

            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), pos, this.Rotation.AsQuat, mat1, 0);
        }

        private Vector3 CarryPosition()
        {
            return (this.Rotation.FacingCell.ToVector3() * this.move) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
        }

        private bool IsActive()
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

        private void SetPower()
        {
            if (-this.SupplyPower != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.SupplyPower;
            }
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
            if (this.state == CarryState.Ready)
            {
                // noop
                return;
            }
            else if (this.state == CarryState.Carring)
            {
                this.move += 0.01f * this.SpeedFactor * this.SupplyPower * 0.1f;
                if (this.move >= 1f)
                {
                    this.FinishCarry();
                    this.checkNextPlacing = true;
                }
            }
            else if (this.state == CarryState.Placing)
            {
                if (Find.TickManager.TicksGame % 30 == shift || this.checkNextPlacing)
                {
                    this.PlaceThing();
                }
                this.checkNextPlacing = false;
            }
        }

        private bool checkNextPlacing = false;

        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.state == CarryState.Ready;
        }

        public bool ReceiveThing(Thing t)
        {
            this.thing = t;
            this.state = CarryState.Carring;
            if (this.thing.Spawned) this.thing.DeSpawn();
            return true;
        }

        private void FinishCarry()
        {
            this.state = CarryState.Placing;
        }
        
        private bool PlaceThing()
        {
            var next = this.OutputConveyor();
            if (next.HasValue)
            {
                // コンベアある場合、そっちに流す.
                if (next.Value.ReceivableNow(this.ToUnderground))
                {
                    next.Value.ReceiveThing(this.thing);
                    this.move = 0;
                    this.thing = null;
                    this.state = CarryState.Ready;
                    return true;
                }
            }
            else
            {
                if (!this.ToUnderground && PlaceItem(this.thing, this.Rotation.FacingCell + this.Position, false, this.Map))
                {
                    this.move = 0;
                    this.thing = null;
                    this.state = CarryState.Ready;
                    return true;
                }
            }
            // 配置失敗.
            this.move = 0.2f;
            return false;
        }

        public void Link(IBeltConbeyorLinkable link)
        {
        }

        public void Unlink(IBeltConbeyorLinkable unlink)
        {
        }

        private List<IBeltConbeyorLinkable> LinkTargetConveyor()
        {
            return new List<Rot4> { this.Rotation, this.Rotation.Opposite }
                .Select(r => new { Pos = this.Position + r.FacingCell, Rot = r})
                .SelectMany(p => p.Pos.GetThingList(this.Map).Select(t => new { Linkable = t as IBeltConbeyorLinkable, Rot=p.Rot}))
                .Where(t => t.Linkable != null)
                .Where(t => t.Linkable.CanLink(this, (t.Rot == this.Rotation) ? this.ToUnderground : !this.ToUnderground))
                .Select(t => t.Linkable)
                .ToList();
        }

        private Option<IBeltConbeyorLinkable> OutputConveyor()
        {
            return this.LinkTargetConveyor()
                .Where(x => x.Position == this.Position + this.Rotation.FacingCell)
                .FirstOption();
        }

        public bool CanLink(IBeltConbeyorLinkable linkable, bool underground)
        {
            return
                (this.Position + this.Rotation.FacingCell == linkable.Position && this.ToUnderground == underground) ||
                (this.Position + this.Rotation.Opposite.FacingCell == linkable.Position && this.ToUnderground == !underground);
        }

        public bool ReceivableNow(bool underground)
        {
            return this.state == CarryState.Ready && this.IsActive() && !this.ToUnderground == underground;
        }
        
        public bool IsUnderground { get => this.ToUnderground; }

        public bool ToUnderground { get => this.Extension.toUnderground; }

        public string PowerSupplyMessage()
        {
            return "NR_AutoMachineTool.SupplyPowerConveyorText".Translate();
        }

        public void NortifyReceivable()
        {
            this.checkNextPlacing = true;
        }
    }
}
