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
    class Building_BeltConveyorUGConnecter : Building_Base<Thing>, IBeltConbeyorLinkable
    {
        protected override float SpeedFactor { get => this.Setting.beltConveyorSetting.speedFactor; }
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        public override float SupplyPowerForSpeed { get => Building_BeltConveyor.supplyPower; set => Building_BeltConveyor.supplyPower = (int)value; }
        public override int MinPowerForSpeed { get => this.Setting.beltConveyorSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.beltConveyorSetting.maxSupplyPowerForSpeed; }

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
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var targets = LinkTargetConveyor();
            this.Reset();

            base.DeSpawn();

            targets.ForEach(x => x.Unlink(this));
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (this.state != WorkingState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.workLeft > 0.7f)
                {
                    Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                    result.y = (float)UI.screenHeight - result.y;
                    GenMapUI.DrawThingLabel(result, this.CarryingThing().stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (this.state != WorkingState.Ready)
            {
                var p = CarryPosition();
                if (!this.ToUnderground || this.workLeft > 0.7f)
                {
                    this.CarryingThing().DrawAt(p);
                }
            }

            var pos = this.Position.ToVector3() + new Vector3(0.5f, this.def.Altitude + 1f, 0.5f);

            var mat1 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor991_arrow");
            var mat2 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor992_arrow");

            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), pos, this.Rotation.AsQuat, mat1, 0);
        }

        private Thing CarryingThing()
        {
            if (this.state == WorkingState.Working)
            {
                return this.working;
            }
            else if (this.state == WorkingState.Placing)
            {
                return this.products[0];
            }
            return null;
        }

        private Vector3 CarryPosition()
        {
            return (this.Rotation.FacingCell.ToVector3() * (1f - this.workLeft)) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
        }

        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.state == WorkingState.Ready;
        }

        public bool ReceiveThing(bool underground, Thing t)
        {
            if (!this.ReceivableNow(underground, t))
                return false;
            if (this.state == WorkingState.Ready)
            {
                this.working = t;
                this.state = WorkingState.Working;
                this.workLeft = 1f;
                if (this.working.Spawned) this.working.DeSpawn();
                return true;
            }
            else
            {
                var target = this.state == WorkingState.Working ? this.working : this.products[0];
                return target.TryAbsorbStack(t, true);
            }
        }

        protected override bool PlaceProduct(ref List<Thing> products)
        {
            var thing = products[0];
            var next = this.OutputConveyor();
            if (next.HasValue)
            {
                // コンベアある場合、そっちに流す.
                if (next.Value.ReceiveThing(this.ToUnderground, thing))
                {
                    return true;
                }
            }
            else
            {
                if (!this.ToUnderground && PlaceItem(thing, this.Rotation.FacingCell + this.Position, false, this.Map))
                {
                    return true;
                }
            }
            // 配置失敗.
            this.workLeft = 0.8f;
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
                .Select(r => new { Pos = this.Position + r.FacingCell, Rot = r })
                .SelectMany(p => p.Pos.GetThingList(this.Map).Select(t => new { Linkable = t as IBeltConbeyorLinkable, Rot = p.Rot }))
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

        public bool ReceivableNow(bool underground, Thing thing)
        {
            if (!this.IsActive() || this.ToUnderground == underground)
            {
                return false;
            }
            Func<Thing, bool> check = (t) => t.CanStackWith(thing) && t.stackCount < t.def.stackLimit;
            switch (this.state)
            {
                case WorkingState.Ready:
                    return true;
                case WorkingState.Working:
                    // return check(this.working);
                    return false;
                case WorkingState.Placing:
                    return check(this.products[0]);
                default:
                    return false;
            }
        }

        protected override float GetTotalWorkAmount(Thing working)
        {
            return 1f;
        }

        protected override bool WorkIntrruption(Thing working)
        {
            return false;
        }

        protected override bool TryStartWorking(out Thing target)
        {
            target = null;
            return false;
        }

        protected override bool FinishWorking(Thing working, out List<Thing> products)
        {
            products = new List<Thing>().Append(working);
            return true;
        }

        public bool IsUnderground { get => this.ToUnderground; }

        public bool ToUnderground { get => this.Extension.toUnderground; }

        protected override bool WorkingIsDespawned()
        {
            return true;
        }
    }
}
