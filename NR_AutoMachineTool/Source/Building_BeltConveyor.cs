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
    class Building_BeltConveyor : Building
    {
        private enum CarryState
        {
            Ready,
            Carring,
            Placing,
            Blocking
        }

        private CarryState state = CarryState.Ready;
        private float move = 0;
        private Thing thing = null;
        private Rot4 dest = default(Rot4);
        private Dictionary<Rot4, ThingFilter> filters = new Dictionary<Rot4, ThingFilter>();

        private static int shift;
        private static readonly Dictionary<IntVec3, int> GraphicNumbers = new Dictionary<IntVec3, int>() { { Rot4.West.FacingCell, 1 }, { Rot4.South.FacingCell, 2 }, { Rot4.East.FacingCell, 4 }, { Rot4.North.FacingCell, 0 } };

        [Unsaved]
        private int round = 0;
        [Unsaved]
        private int graphicIndex = 0;
        [Unsaved]
        private List<Rot4> outputRot = new List<Rot4>();
        [Unsaved]
        private bool nextCheck = false;

        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private float SpeedFactor { get => this.Setting.carrySpeedFactor; }

        public Dictionary<Rot4, ThingFilter> Filters { get => this.filters; }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<float>(ref this.move, "move");
            Scribe_Values.Look<Rot4>(ref this.dest, "dest");
            Scribe_Values.Look<CarryState>(ref this.state, "state");
            Scribe_Deep.Look<Thing>(ref this.thing, "thing");
            Scribe_Collections.Look<Rot4, ThingFilter>(ref this.filters, "filters", LookMode.Value, LookMode.Deep);
            if(this.filters == null)
            {
                this.filters = new Dictionary<Rot4, ThingFilter>();
            }
        }

        public override void PostMapInit()
        {
            base.PostMapInit();

            this.ChangeGraphic();
            this.FilterSetting();
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
                this.ChangeGraphic();
                this.FilterSetting();
                this.state = CarryState.Ready;
                this.move = 0;
            }
        }

        public override Graphic Graphic => Option(base.Graphic as Graphic_Selectable).Fold(base.Graphic)(g => g.Get(this.graphicIndex));
        
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (this.state != CarryState.Ready && Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                var p = CarryPosition();
                Vector2 result = Find.Camera.WorldToScreenPoint(p + new Vector3(0, 0, -0.4f)) / Prefs.UIScale;
                result.y = (float)UI.screenHeight - result.y;
                GenMapUI.DrawThingLabel(result, this.thing.stackCount.ToStringCached(), GenMapUI.DefaultThingLabelColor);
            }
        }
        
        public override void Draw()
        {
            base.Draw();

            if (this.state != CarryState.Ready)
            {
                var p = CarryPosition();
                this.thing.DrawAt(p);
            }

            var pos = this.Position.ToVector3() + new Vector3(0.5f, this.def.Altitude + 1f, 0.5f);

            var mat1 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor991_arrow");
            var mat2 = MaterialPool.MatFrom("NR_AutoMachineTool/Buildings/BeltConveyor/BeltConveyor992_arrow");

            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), pos, this.Rotation.AsQuat, mat1, 0);
            this.outputRot.Where(x => x != this.Rotation).ForEach(r =>
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), pos, r.AsQuat, mat2, 0));
        }

        private Vector3 CarryPosition()
        {
            return (this.dest.FacingCell.ToVector3() * this.move) + this.Position.ToVector3() + new Vector3(0.5f, 10f, 0.5f);
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
        
        public override void Tick()
        {
            base.Tick();
            if (!this.IsActive())
            {
                this.Reset();
                return;
            }
            if (this.state == CarryState.Ready)
            {
                if (Find.TickManager.TicksGame % 30 == shift || nextCheck)
                {
                    this.TryStartCarry();
                    this.nextCheck = false;
                }
            }
            else if (this.state == CarryState.Carring)
            {

                this.move += 0.01f * this.SpeedFactor;
                if (this.move >= 1f)
                {
                    this.FinishCarry();
                }
            }
            else if (this.state == CarryState.Placing)
            {
                if (this.PlaceThing())
                {
                    this.nextCheck = true;
                }
            }
            else if (this.state == CarryState.Blocking)
            {
                if (Find.TickManager.TicksGame % 30 == shift)
                {
                    this.PlaceThing();
                }
            }
        }

        public override bool CanStackWith(Thing other)
        {
            return base.CanStackWith(other) && this.state == CarryState.Ready;
        }

        private void TryStartCarry()
        {
            this.Position.GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Item).FirstOption().ForEach(t =>
            {
                this.TryStartCarry(t);
            });
        }

        public bool TryStartCarry(Thing t)
        {
            var d = Destination(t, true);
            this.thing = t;
            this.state = CarryState.Carring;
            this.dest = d;
            if (this.thing.Spawned) this.thing.DeSpawn();
            return true;
        }

        public bool Acceptable()
        {
            return this.state == CarryState.Ready && this.IsActive();
        }

        private void FinishCarry()
        {
            this.state = CarryState.Placing;
        }

        private Rot4 Destination(Thing t, bool doRotate)
        {
            var allowed = this.filters
                .Where(f => f.Value.Allows(t.def)).Select(f => f.Key)
                .ToList();
            var placable =
                allowed.Where(r => this.LinkTargetConveyor().Where(l => l.Position == this.Position + r.FacingCell).FirstOption().Select(b => b.Acceptable()).GetOrDefault(true))
                .ToList();

            if (placable.Count == 0)
            {
                if(allowed.Count == 0)
                {
                    return this.Rotation;
                }
                placable = allowed;
            }

            if (placable.Count <= this.round) this.round = 0;
            var index = this.round;
            if (doRotate) this.round++;
            return placable.ElementAt(index);
        }

        private bool PlaceThing()
        {
            var next = this.LinkTargetConveyor().Where(o => o.Position == this.dest.FacingCell + this.Position).FirstOption();
            if (next.HasValue)
            {
                // コンベアある場合、そっちに流す.
                if (next.Value.Acceptable())
                {
                    next.Value.TryStartCarry(this.thing);
                    this.move = 0;
                    this.thing = null;
                    this.state = CarryState.Ready;
                    return true;
                }
            }
            else
            {
                if (PlaceItem(this.thing, this.dest.FacingCell + this.Position, false, this.Map))
                {
                    this.move = 0;
                    this.thing = null;
                    this.state = CarryState.Ready;
                    return true;
                }
            }
            // 配置失敗.
            this.move = 0.5f;
            this.state = CarryState.Blocking;
            return false;
        }

        public void Link(Building_BeltConveyor link)
        {
            this.ChangeGraphic();
            this.FilterSetting();
        }

        public void Unlink(Building_BeltConveyor unlink)
        {
            this.ChangeGraphic();
            this.FilterSetting();
        }

        private void ChangeGraphic()
        {
            var result = this.LinkTargetConveyor().Aggregate(0, (t, n) =>
            {
                t += GraphicNumbers[(n.Position - this.Position).RotatedBy(new Rot4(4 - this.Rotation.AsByte))];
                return t;
            });
            if (result == 0) result = 2;
            this.graphicIndex = result;
        }

        private void FilterSetting()
        {
            Func<ThingFilter> createNew = () =>
            {
                var f = new ThingFilter();
                f.SetAllowAll(null);
                return f;
            };
            this.filters = Enumerable.Range(0, 4).Select(x => new Rot4(x))
                .Select(x => new { Rot = x, Pos = this.Position + x.FacingCell })
                .Where(x => this.OutputBeltConveyor().Any(l => l.Position == x.Pos) || this.Rotation == x.Rot)
                .ToDictionary(r => r.Rot, r => this.filters.ContainsKey(r.Rot) ? this.filters[r.Rot] : createNew());
            if(this.filters.Count <= 1)
            {
                this.filters.ForEach(x => x.Value.SetAllowAll(null));
            }
            this.outputRot = this.filters.Select(x => x.Key).ToList();
        }

        private List<Building_BeltConveyor> LinkTargetConveyor()
        {
            return Enumerable.Range(0, 4).Select(i => this.Position + new Rot4(i).FacingCell)
                .SelectMany(t => t.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Building)
                .SelectMany(t => Option(t as Building_BeltConveyor))
                .Where(b =>
                    b.Position + b.Rotation.FacingCell == this.Position ||
                    b.Position - b.Rotation.FacingCell == this.Position ||
                    this.Position + this.Rotation.FacingCell == b.Position ||
                    this.Position - this.Rotation.FacingCell == b.Position).ToList();
        }

        private List<Building_BeltConveyor> OutputBeltConveyor()
        {
            return this.LinkTargetConveyor().Where(x => x.Rotation.Opposite.FacingCell + x.Position == this.Position).ToList();
        }
    }
}
