using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class SlaughterSettings : IExposable
    {
        public SlaughterSettings()
        {
            this.doSlaughter = false;
            this.hasBonds = false;
            this.pregnancy = false;
            this.trained = false;

            this.keepMaleAdultCount = 10;
            this.keepMaleYoungCount = 10;
            this.keepFemaleAdultCount = 10;
            this.keepFemaleYoungCount = 10;
        }

        public ThingDef def;
        public bool doSlaughter;
        public bool hasBonds;
        public bool pregnancy;
        public bool trained;

        public int keepMaleAdultCount;
        public int keepMaleYoungCount;
        public int keepFemaleAdultCount;
        public int keepFemaleYoungCount;

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.doSlaughter, "doSlaughter", false);
            Scribe_Values.Look<bool>(ref this.hasBonds, "hasBonds", false);
            Scribe_Values.Look<bool>(ref this.pregnancy, "pregnancy", false);
            Scribe_Values.Look<bool>(ref this.trained, "trained", false);

            Scribe_Values.Look<int>(ref this.keepMaleAdultCount, "keepMaleAdultCount", 10);
            Scribe_Values.Look<int>(ref this.keepMaleYoungCount, "keepMaleYoungCount", 10);
            Scribe_Values.Look<int>(ref this.keepFemaleAdultCount, "keepFemaleAdultCount", 10);
            Scribe_Values.Look<int>(ref this.keepFemaleYoungCount, "keepFemaleYoungCount", 10);

            Scribe_Defs.Look<ThingDef>(ref this.def, "def");
        }

        public int KeepCount(Gender gender, bool adult)
        {
            if(gender == Gender.Male)
            {
                if (adult)
                    return keepMaleAdultCount;
                else
                    return keepMaleYoungCount;
            }
            else
            {
                if (adult)
                    return keepFemaleAdultCount;
                else
                    return keepFemaleYoungCount;
            }
        }
    }

    interface ISlaughterhouse
    {
        Dictionary<ThingDef, SlaughterSettings> Settings { get; }
    }

    [StaticConstructorOnStartup]
    class ITab_Slaughterhouse : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(800f, 600f);

        public ITab_Slaughterhouse()
        {
            this.size = WinSize;
            this.labelKey = "NR_AutoMachineTool.SlaughterhouseSetting.TabName";
        }
        
        private string description;
        
        public ISlaughterhouse Machine
        {
            get => (ISlaughterhouse)this.SelThing;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            this.defs = Find.VisibleMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal && p.RaceProps.IsFlesh && p.SpawnedOrAnyParentSpawned).Select(p => p.def).Distinct().ToList();
        }

        private List<ThingDef> defs;

        private Vector2 scrollPosition;

        private static readonly float[] ColumnWidth = new float[] { 0.2f, 0.05f, 0.05f, 0.05f, 0.05f, 0.15f, 0.15f, 0.15f, 0.15f };

        private static readonly Texture2D PregnantIcon = Resources.Load<Texture2D>("Textures/UI/Icons/Pregnant");
        private static readonly Texture2D MaleIcon = GenderUtility.GetIcon(Gender.Male);
        private static readonly Texture2D FemaleIcon = GenderUtility.GetIcon(Gender.Female);

        private static Texture2D slaughterIcon;
        private static Texture2D SlaughterIcon => slaughterIcon = slaughterIcon ?? ContentFinder<Texture2D>.Get("UI/Icons/Animal/Slaughter", true);

        private static Texture2D trainedIcon;
        private static Texture2D TrainedIcon => trainedIcon = trainedIcon ?? ContentFinder<Texture2D>.Get("UI/Icons/Trainables/Obedience", true);

        private static Texture2D youngIcon;
        private static Texture2D YoungIcon => youngIcon = youngIcon ?? ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Young", true);

        private static Texture2D adultIcon;
        private static Texture2D AdultIcon => adultIcon = adultIcon ?? ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Adult", true);

        private Func<float, Rect> CutLeftFunc(Rect rect)
        {
            var curX = 0f;
            return (pct) =>
            {
                var r = new Rect(curX, rect.y, rect.width * pct, rect.height);
                curX += rect.width * pct;
                return r;
            };
        }

        private static TipSignal slaughterTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.DoSlaughterTip".Translate());
        private static TipSignal hasBondsTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.BondsTip".Translate());
        private static TipSignal pregnancyTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.PregnancyTip".Translate());
        private static TipSignal trainedTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.TrainedTip".Translate());

        private static TipSignal keepMaleChildCountTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.KeepCountTip".Translate("NR_AutoMachineTool.Male".Translate(), "NR_AutoMachineTool.Young".Translate()));
        private static TipSignal keepFemaleChildCountTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.KeepCountTip".Translate("NR_AutoMachineTool.Female".Translate(), "NR_AutoMachineTool.Young".Translate()));
        private static TipSignal keepMaleAdultCountTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.KeepCountTip".Translate("NR_AutoMachineTool.Male".Translate(), "NR_AutoMachineTool.Adult".Translate()));
        private static TipSignal keepFemaleAdultCountTip = new TipSignal("NR_AutoMachineTool.SlaughterhouseSetting.KeepCountTip".Translate("NR_AutoMachineTool.Female".Translate(), "NR_AutoMachineTool.Adult".Translate()));

        protected override void FillTab()
        {
            this.description = "NR_AutoMachineTool.SlaughterhouseSetting.Description".Translate();

            Rect outRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            var outList = new Listing_Standard();
            outList.Begin(outRect);
            var rect = outList.GetRect(40f);
            outList.Gap();

            Widgets.Label(rect, this.description);

            var headerRect = outList.GetRect(24f);
            headerRect.width -= 30f;

            var cutLeftHeader = CutLeftFunc(headerRect);

            int colIndex = 0;
            var col = cutLeftHeader(ColumnWidth[colIndex++]);
            Widgets.Label(col, "NR_AutoMachineTool.RaceName".Translate());

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), SlaughterIcon);
            TooltipHandler.TipRegion(col, slaughterTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            Widgets.Label(col, "NR_AutoMachineTool.Bonds".Translate());
            TooltipHandler.TipRegion(col, hasBondsTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), PregnantIcon);
            TooltipHandler.TipRegion(col, pregnancyTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), TrainedIcon);
            TooltipHandler.TipRegion(col, trainedTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), YoungIcon);
            TooltipHandler.TipRegion(col, keepMaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), YoungIcon);
            TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), MaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), AdultIcon);
            TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

            col = cutLeftHeader(ColumnWidth[colIndex++]);
            GUI.DrawTexture(col.LeftPartPixels(24f), FemaleIcon);
            GUI.DrawTexture(col.LeftPartPixels(48f).RightPartPixels(24f), AdultIcon);
            TooltipHandler.TipRegion(col, keepFemaleAdultCountTip);

            var scrollOutRect = outList.GetRect(outRect.height - outList.CurHeight);
            var scrollViewRect = new Rect(scrollOutRect.x, scrollOutRect.y, scrollOutRect.width - 30f, this.defs.Count() * 36f);

            Widgets.BeginScrollView(scrollOutRect, ref this.scrollPosition, scrollViewRect);
            var list = new Listing_Standard();
            list.Begin(scrollViewRect);
            this.defs.ForEach(d =>
            {
                list.GapLine();

                var rowRect = list.GetRect(24f);

                SlaughterSettings s = null;
                this.Machine.Settings.TryGetValue(d, out s);
                if (s == null)
                {
                    s = new SlaughterSettings();
                    s.def = d;
                    this.Machine.Settings[d] = s;
                }
                var cutLeft = CutLeftFunc(rowRect);

                colIndex = 0;
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Label(col, s.def.label);

                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.doSlaughter);
                TooltipHandler.TipRegion(col, slaughterTip);
                
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.hasBonds, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, hasBondsTip);
                
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.pregnancy, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, pregnancyTip);
                
                col = cutLeft(ColumnWidth[colIndex++]);
                Widgets.Checkbox(col.position, ref s.trained, disabled: !s.doSlaughter);
                TooltipHandler.TipRegion(col, trainedTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf1 = s.keepMaleYoungCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepMaleYoungCount, ref buf1, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf2 = s.keepFemaleYoungCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleYoungCount, ref buf2, 0, 1000);
                TooltipHandler.TipRegion(col, keepFemaleChildCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf3 = s.keepMaleAdultCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepMaleAdultCount, ref buf3, 0, 1000);
                TooltipHandler.TipRegion(col, keepMaleAdultCountTip);

                col = cutLeft(ColumnWidth[colIndex++]);
                string buf4 = s.keepFemaleAdultCount.ToString();
                Widgets.TextFieldNumeric<int>(col, ref s.keepFemaleAdultCount, ref buf4, 0, 1000);
                TooltipHandler.TipRegion(col, keepFemaleAdultCountTip);
            });

            list.End();
            Widgets.EndScrollView();
            outList.End();
        }
    }
}
