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
    public class Building_ItemPuller : Building
    {
        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private float SpeedFactor { get => this.Setting.pullSpeedFactor; }
        public ThingFilter Filter { get => this.filter; }

        private static int shift;

        private ThingFilter filter = new ThingFilter();
        private float amount;

        private bool active = false;

        [Unsaved]
        private bool wait;

        public override Graphic Graphic => Option(base.Graphic as Graphic_Selectable).Fold(base.Graphic)(g => g.Get(this.active ? 1 : 0));

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look<ThingFilter>(ref this.filter, "filter");
            Scribe_Values.Look<float>(ref this.amount, "amount", 0);
            Scribe_Values.Look<bool>(ref this.active, "active", false);

            if (this.filter == null) this.filter = new ThingFilter();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.filter = new ThingFilter();
                this.filter.SetAllowAll(null);
            }

            shift += 7;
            if (shift >= 30)
            {
                shift = shift - 30;
            }
        }

        private bool IsActive()
        {
            if (this.TryGetComp<CompPowerTrader>() == null || !this.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return false;
            }
            if (this.Destroyed || !this.active)
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
                return;
            }
            this.amount += 0.01f * this.SpeedFactor;
            if(this.amount > 1f)
            {
                if (Find.TickManager.TicksGame % 30 == shift || !wait)
                {
                    if (this.PullAndPush())
                    {
                        this.amount = 0;
                        this.wait = false;
                    }
                    else
                    {
                        this.wait = true;
                    }
                }
            }
        }

        private bool PullAndPush()
        {
            return TargetThing().Fold(true)(target => {
                var conveyor = OutputCell().GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as Building_BeltConveyor))
                    .FirstOption();
                if (conveyor.HasValue)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.Value.Acceptable())
                    {
                        conveyor.Value.TryStartCarry(target);
                        return true;
                    }
                }
                else
                {
                    if (target.Spawned) target.DeSpawn();
                    // ない場合は適当に流す.
                    if(!PlaceItem(target, OutputCell(), false, this.Map))
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                    }
                    return true;
                }
                return false;
            });
        }

        private Option<Thing> TargetThing()
        {
            return this.InputZone()
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Item)
                .Where(t => this.filter.Allows(t))
                .FirstOption();
        }

        public IntVec3 InputCell()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell);
        }

        public List<IntVec3> InputZone()
        {
            return this.InputCell().ZoneCells(this.Map);
        }

        public IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.FacingCell);
        }

        public List<IntVec3> OutputZone()
        {
            return this.OutputCell().ZoneCells(this.Map);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var act = new Command_Toggle();
            act.isActive = () => this.active;
            act.toggleAction = () => this.active = !this.active;
            act.defaultLabel = "NR_AutoMachineTool_Puller.SwitchActiveLabel".Translate();
            act.defaultDesc = "NR_AutoMachineTool_Puller.SwitchActiveDesc".Translate();
            act.icon = ContentFinder<Texture2D>.Get("NR_AutoMachineTool/UI/Play", true);
            yield return act;
        }
    }
}
