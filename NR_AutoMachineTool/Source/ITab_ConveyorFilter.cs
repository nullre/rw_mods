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
    class ITab_ConveyorFilter : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(300f, 500f);

        private static readonly Dictionary<Rot4, string> RotStrings = new Dictionary<Rot4, string>() { { Rot4.North, "N" }, { Rot4.East, "E" }, { Rot4.South, "S" }, { Rot4.West, "W" } };

        public ITab_ConveyorFilter()
        {
            this.size = WinSize;
            this.labelKey = "NR_AutoMachineTool_Conveyor.OutputItemFilterTab";

            this.description = "NR_AutoMachineTool_Conveyor.OutputItemFilterText".Translate();
        }

        private string description;

        private Building_BeltConveyor Conveyor { get => (Building_BeltConveyor)this.SelThing;}

        public override bool IsVisible => this.Conveyor.Filters.Count > 1;

        private Dictionary<Building_BeltConveyor, Dictionary<Rot4, bool>> rotSelectedDic = new Dictionary<Building_BeltConveyor, Dictionary<Rot4, bool>>();

        private Vector2 scrollPosition;

        public override void OnOpen()
        {
            base.OnOpen();

            this.groups = this.Conveyor.Map.haulDestinationManager.AllGroups.ToList();
        }

        private List<SlotGroup> groups;

        protected override void FillTab()
        {
            if (!this.rotSelectedDic.ContainsKey(this.Conveyor))
            {
                Dictionary<Rot4, bool> newDic = Enumerable.Range(0, 4).ToDictionary(k => new Rot4(k), v => false);
                newDic[this.Conveyor.Filters.First().Key] = true;
                this.rotSelectedDic[this.Conveyor] = newDic;
            }
            Dictionary<Rot4, bool> dic = this.rotSelectedDic[this.Conveyor];
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            var rect = list.GetRect(40f);
            Widgets.Label(rect, this.description);
            list.Gap();

            Dictionary<Rot4, Rect> pos = new Dictionary<Rot4, Rect>();
            rect = list.GetRect(30f);
            pos[Rot4.North] = new Rect(rect.x + rect.width / 4, rect.y, rect.width / 2, rect.height);
            rect = list.GetRect(30f);
            pos[Rot4.West] = rect.LeftHalf();
            pos[Rot4.East] = rect.RightHalf();
            rect = list.GetRect(30f);
            pos[Rot4.South] = new Rect(rect.x + rect.width / 4, rect.y, rect.width / 2, rect.height);

            new Dictionary<Rot4, bool>(dic).ForEach(kv =>
            {
                var dir = ("NR_AutoMachineTool.OutputDirection" + RotStrings[kv.Key]).Translate();

                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(pos[kv.Key].LeftHalf(), dir);
                if (this.Conveyor.Filters.ContainsKey(kv.Key))
                {
                    if (Widgets.RadioButton(pos[kv.Key].RightHalf().position, kv.Value))
                    {
                        new Dictionary<Rot4, bool>(dic).ForEach(x => dic[x.Key] = false);
                        dic[kv.Key] = true;
                    }
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GameFont tmp = Text.Font;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(pos[kv.Key].RightHalf(), "NR_AutoMachineTool_Conveyor.OutputItemFilterNotOutputDestination".Translate());
                    Text.Font = tmp;
                }
            });
            list.Gap();

            rect = list.GetRect(30f);
            if (Widgets.ButtonText(rect, "NR_AutoMachineTool_Puller.FilterCopyFrom".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(groups.Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(),
                    () => this.Conveyor.Filters[dic.Where(kv => kv.Value).First().Key].CopyAllowancesFrom(g.Settings.filter)
                    )).ToList()));
            }
            list.Gap();

            list.End();
            var height = list.CurHeight;

            ThingFilterUI.DoThingFilterConfigWindow(inRect.BottomPartPixels(inRect.height - height), ref this.scrollPosition, this.Conveyor.Filters[dic.Where(kv => kv.Value).First().Key]);
        }
    }
}
