using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class Building_Cleaner : Building_BaseRange<Filth>
    {
        private ModExtension_AutoMachineTool Extension { get { return this.def.GetModExtension<ModExtension_AutoMachineTool>(); } }
        protected override float SpeedFactor { get => this.Setting.cleanerSetting.speedFactor; }

        public override int MinPowerForSpeed { get => this.Setting.cleanerSetting.minSupplyPowerForSpeed; }
        public override int MaxPowerForSpeed { get => this.Setting.cleanerSetting.maxSupplyPowerForSpeed; }
        public override int MinPowerForRange { get => this.Setting.cleanerSetting.minSupplyPowerForRange; }
        public override int MaxPowerForRange { get => this.Setting.cleanerSetting.maxSupplyPowerForRange; }

        static Building_Cleaner()
        {
            tryDropFilth = GenerateVoidMeshodDelegate<Pawn_FilthTracker>(typeof(Pawn_FilthTracker).GetMethod("TryDropFilth", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        private static readonly Action<Pawn_FilthTracker> tryDropFilth;

        protected override bool WorkInterruption(Filth working)
        {
            return !working.Spawned;
        }

        protected override bool TryStartWorking(out Filth target, out float workAmount)
        {
            var cells = GenRadial.RadialCellsAround(this.Position, this.GetRange(), true)
                .Where(c => c.GetRoom(Find.CurrentMap) == this.GetRoom());

            cells.SelectMany(c => c.GetThingList(this.Map).ToList())
                .SelectMany(t => Option(t as Pawn))
                .ForEach(p => tryDropFilth(p.filth));

            target = cells
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Filth)
                .SelectMany(t => Option(t as Filth))
                .FirstOption()
                .GetOrDefault(null);

            if (target != null)
            {
                workAmount = target.def.filth.cleaningWorkToReduceThickness * target.thickness;
            }
            else
            {
                workAmount = 0f;
            }
            return target != null;
        }

        protected override bool FinishWorking(Filth working, out List<Thing> products)
        {
            products = new List<Thing>();
            working.Destroy(DestroyMode.Vanish);
            return true;
        }

        [Unsaved]
        private Option<Effecter> workingEffect = Nothing<Effecter>();

        protected override void CleanupWorkingEffect()
        {
            base.CleanupWorkingEffect();

            this.workingEffect.ForEach(e => e.Cleanup());
            this.workingEffect = Nothing<Effecter>();

            MapManager.RemoveEachTickAction(this.EffectTick);
        }

        protected override void CreateWorkingEffect()
        {
            base.CreateWorkingEffect();

            this.workingEffect = this.workingEffect.Fold(() => Option(EffecterDefOf.Clean.Spawn()))(e => Option(e));

            MapManager.EachTickAction(this.EffectTick);
        }

        protected bool EffectTick()
        {
            this.workingEffect.ForEach(e => e.EffectTick(new TargetInfo(this.Working), TargetInfo.Invalid));
            return !this.workingEffect.HasValue;
        }
    }
}
