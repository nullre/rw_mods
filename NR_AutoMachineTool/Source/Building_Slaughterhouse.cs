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
    public class Building_Slaughterhouse : Building , IAgricultureMachine, IBeltConbeyorSender, ISlaughterhouse, IRange
    {
        public enum SlaughterState {
            Ready,
            Slaughtering,
            Placing
        }

        private ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }
        private float SpeedFactor { get => this.Setting.slaughterSpeedFactor; }

        public int MinPowerForSpeed { get => this.Setting.minSlaughterSupplyPowerForSpeed; }
        public int MaxPowerForSpeed { get => this.Setting.maxSlaughterSupplyPowerForSpeed; }
        public int MinPowerForRange { get => this.Setting.minSlaughterSupplyPowerForRange; }
        public int MaxPowerForRange { get => this.Setting.maxSlaughterSupplyPowerForRange; }

        private static HashSet<Pawn> slaughteringSet = new HashSet<Pawn>();

        public float SupplyPowerForSpeed
        {
            get
            {
                return this.supplyPowerForSpeed;
            }

            set
            {
                this.supplyPowerForSpeed = value;
                this.SetPower();
            }
        }

        public float SupplyPowerForRange
        {
            get
            {
                return this.supplyPowerForRange;
            }

            set
            {
                this.supplyPowerForRange = value;
                this.SetPower();
            }
        }

        public Dictionary<ThingDef, SlaughterSettings> Settings { get => this.slaughterSettings; }

        private Dictionary<ThingDef, SlaughterSettings> slaughterSettings = new Dictionary<ThingDef, SlaughterSettings>();

        private float supplyPowerForSpeed;
        private float supplyPowerForRange;
        private SlaughterState state;
        private float workLeft;
        private Pawn slaughtering;
        private Thing product;

        private float WorkAmount { get => 400f; }

        [Unsaved]
        private Effecter progressBar;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.supplyPowerForSpeed, "supplyPowerForSpeed", this.MinPowerForSpeed);
            Scribe_Values.Look<float>(ref this.supplyPowerForRange, "supplyPowerForRange", this.MinPowerForRange);
            Scribe_Values.Look<SlaughterState>(ref this.state, "working");
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f);

            Scribe_References.Look<Pawn>(ref this.slaughtering, "slaughtering");
            Scribe_Deep.Look<Thing>(ref this.product, "product");
            Scribe_Collections.Look<ThingDef, SlaughterSettings>(ref this.slaughterSettings, "slaughterSettings", LookMode.Def, LookMode.Deep);

            this.ReloadSettings(null, null);
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (this.supplyPowerForSpeed < this.MinPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
            }
            if (this.supplyPowerForSpeed > this.MaxPowerForSpeed)
            {
                this.supplyPowerForSpeed = this.MaxPowerForSpeed;
            }
            if (this.supplyPowerForRange < this.MinPowerForRange)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
            }
            if (this.supplyPowerForRange > this.MaxPowerForRange)
            {
                this.supplyPowerForRange = this.MaxPowerForRange;
            }
        }

        private void SettingValues()
        {
            this.TryGetComp<CompPowerTrader>().PowerOutput = this.supplyPowerForSpeed + this.supplyPowerForRange;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                this.supplyPowerForRange = this.MinPowerForRange;
                this.supplyPowerForSpeed = this.MinPowerForSpeed;
            }
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed += this.ReloadSettings;
        }

        public override void DeSpawn()
        {
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting.DataExposed -= this.ReloadSettings;
            this.Reset();
            base.DeSpawn();
        }

        public int GetRange()
        {
            return Mathf.RoundToInt(this.supplyPowerForRange / 500) + 2;
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

        private void Reset()
        {
            if (this.state != SlaughterState.Ready)
            {
                this.CleanupProgressBar();
                Option(this.product).ForEach(t =>
                {
                    if (!t.Spawned)
                    {
                        GenPlace.TryPlaceThing(this.product, this.Position, this.Map, ThingPlaceMode.Near);
                    }
                });

                if (this.slaughtering != null && this.slaughtering.jobs.curJob.def == JobDefOf.WaitMaintainPosture)
                {
                    this.slaughtering.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            }
            this.state = SlaughterState.Ready;
            this.workLeft = 0;
            Option(this.slaughtering).ForEach(h => slaughteringSet.Remove(h));
            this.slaughtering = null;
            this.product = null;
        }

        private void CleanupProgressBar()
        {
            Option(this.progressBar).ForEach(e => e.Cleanup());
            this.progressBar = null;
        }

        private void UpdateProgressBar()
        {
            this.progressBar = Option(this.progressBar).GetOrDefault(EffecterDefOf.ProgressBar.Spawn);
            this.progressBar.EffectTick(this.slaughtering, TargetInfo.Invalid);
            Option(((SubEffecter_ProgressBar)progressBar.children[0]).mote).ForEach(m => m.progress = (WorkAmount - this.workLeft) / WorkAmount);
        }

        private void SetPower()
        {
            if(-this.supplyPowerForRange - this.supplyPowerForSpeed != this.TryGetComp<CompPowerTrader>().PowerOutput)
            {
                this.TryGetComp<CompPowerTrader>().PowerOutput = -this.supplyPowerForRange - this.supplyPowerForSpeed;
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

            if (this.state == SlaughterState.Ready)
            {
                if (Find.TickManager.TicksGame % 30 == 20 || this.checkNextReady)
                {
                    this.TryStartSlaughtering();
                    this.checkNextReady = false;
                }
            }
            else if (this.state == SlaughterState.Slaughtering)
            {
                this.workLeft -= 0.01f * this.SpeedFactor * this.supplyPowerForSpeed * 0.1f;
                this.UpdateProgressBar();
                if (this.slaughtering.Dead || !this.slaughtering.Spawned)
                {
                    this.Reset();
                }
                if (this.workLeft <= 0f)
                {
                    this.FinishSlaughter();
                    this.CleanupProgressBar();
                    this.checkNextPlace = true;
                }
            }
            else if (this.state == SlaughterState.Placing)
            {
                if (Find.TickManager.TicksGame % 30 == 21 || checkNextPlace)
                {
                    if (this.PlaceProduct())
                    {
                        this.state = SlaughterState.Ready;
                        this.Reset();
                        this.checkNextReady = true;
                    }
                    this.checkNextPlace = false;
                }
            }
        }

        [Unsaved]
        private bool checkNextReady = false;

        [Unsaved]
        private bool checkNextPlace = false;

        private HashSet<Pawn> ShouldSlaughterPawns()
        {
            var mapPawns = this.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            return this.slaughterSettings.Values.Where(s => s.doSlaughter).SelectMany(s =>
            {
                var pawns = mapPawns.Where(p => p.def == s.def);
                Func<Pawn, bool> where = (p) =>
                {
                    bool result = true;
                    if (result && !s.hasBonds) result = p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) == null;
                    if (result && !s.pregnancy) result = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant, true) == null;
                    if (result && !s.trained) result = !p.training.IsCompleted(TrainableDefOf.Obedience);
                    return result;
                };
                Func<IEnumerable<Pawn>, bool, IOrderedEnumerable<Pawn>> orderBy = (e, adult) =>
                {
                    if (adult) return e.OrderByDescending(p => p.ageTracker.AgeChronologicalTicks);
                    else return e.OrderBy(p => p.ageTracker.AgeChronologicalTicks);
                };
                return new[] { new { Gender = Gender.Male, Adult = true }, new { Gender = Gender.Female, Adult = true }, new { Gender = Gender.Male, Adult = false }, new { Gender = Gender.Female, Adult = false } }
                    .Select(a => new { Group = a, Pawns = pawns.Where(p => p.gender == a.Gender && p.IsAdult() == a.Adult) })
                    .Select(g => new { Group = g.Group, Pawns = g.Pawns, SlaughterCount = g.Pawns.Count() - s.KeepCount(g.Group.Gender, g.Group.Adult) })
                    .Where(g => g.SlaughterCount > 0)
                    .SelectMany(g => orderBy(g.Pawns.Where(where), g.Group.Adult).Take(g.SlaughterCount));
            }).ToHashSet();
        }

        private void TryStartSlaughtering()
        {
            var tmp = FacingRect(this.Position, this.Rotation, this.GetRange())
                .Where(c => (this.Position + this.Rotation.FacingCell).GetRoom(this.Map) == c.GetRoom(this.Map))
                .SelectMany(c => c.GetThingList(this.Map))
                .Where(t => t.def.category == ThingCategory.Pawn)
                .SelectMany(t => Option(t as Pawn))
                .Where(p => !slaughteringSet.Contains(p))
                .Where(p => this.slaughterSettings.ContainsKey(p.def));
            if (!tmp.FirstOption().HasValue)
            {
                return;
            }
            var targets = ShouldSlaughterPawns();
            tmp.Where(p => targets.Contains(p))
                .FirstOption()
                .ForEach(p =>
                {
                    this.slaughtering = p;
                    this.workLeft = WorkAmount;
                    this.state = SlaughterState.Slaughtering;
                    PawnUtility.ForceWait(this.slaughtering, 15000, null, true);
                    slaughteringSet.Add(p);
                });
        }

        private void FinishSlaughter()
        {
            slaughteringSet.Remove(this.slaughtering);
            if (this.slaughtering.jobs.curJob.def == JobDefOf.WaitMaintainPosture)
            {
                this.slaughtering.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
            int num = Mathf.Max(GenMath.RoundRandom(this.slaughtering.BodySize * 8f), 1);
            for (int i = 0; i < num; i++)
            {
                this.slaughtering.health.DropBloodFilth();
            }
            this.slaughtering.Kill(null);
            this.product = this.slaughtering.Corpse;
            this.product.DeSpawn();
            this.product.SetForbidden(false);
            this.state = SlaughterState.Placing;
            this.slaughtering = null;
            this.workLeft = 0;
        }

        private bool PlaceProduct()
        {
            return Option(this.product).Fold(true)(target => {
                var conveyor = OutputCell().GetThingList(this.Map).Where(t => t.def.category == ThingCategory.Building)
                    .SelectMany(t => Option(t as IBeltConbeyorLinkable))
                    .Where(b => !b.IsUnderground)
                    .FirstOption();
                if (conveyor.HasValue)
                {
                    // コンベアがある場合、そっちに流す.
                    if (conveyor.Value.ReceivableNow(false))
                    {
                        conveyor.Value.ReceiveThing(target);
                        return true;
                    }
                }
                else
                {
                    // ない場合は適当に流す.
                    if (!PlaceItem(target, OutputCell(), false, this.Map))
                    {
                        GenPlace.TryPlaceThing(target, OutputCell(), this.Map, ThingPlaceMode.Near);
                    }
                    return true;
                }
                return false;
            });
        }

        public IntVec3 OutputCell()
        {
            return (this.Position + this.Rotation.Opposite.FacingCell);
        }

        public override string GetInspectString()
        {
            String msg = base.GetInspectString();
            msg += "\n";
            switch (this.state)
            {
                case SlaughterState.Slaughtering:
                    msg += "NR_AutoMachineTool.StatWorking".Translate(Mathf.RoundToInt(WorkAmount - this.workLeft), Mathf.RoundToInt(WorkAmount), Mathf.RoundToInt(((WorkAmount - this.workLeft) / WorkAmount) * 100));
                    break;
                case SlaughterState.Ready:
                    msg += "NR_AutoMachineTool.StatReady".Translate();
                    break;
                case SlaughterState.Placing:
                    msg += "NR_AutoMachineTool.StatPlacing".Translate(1);
                    break;
                default:
                    msg += this.state.ToString();
                    break;
            }
            return msg;
        }

        public void NortifyReceivable()
        {
            this.checkNextPlace = true;
        }
    }
}
