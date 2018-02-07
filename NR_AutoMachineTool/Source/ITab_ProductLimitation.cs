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
    interface IProductLimitation
    {
        int ProductLimitCount { get; set; }
        bool ProductLimitation { get; set; }
        Option<SlotGroup> TargetSlotGroup { get; set; }
    }

    class ITab_ProductLimitation : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 220f);

        public ITab_ProductLimitation()
        {
            this.size = WinSize;
            this.labelKey = "NR_AutoMachineTool.ProductLimitation.TabName";
        }
        
        public IProductLimitation Machine
        {
            get => (IProductLimitation)this.SelThing;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            this.groups = Find.VisibleMap.slotGroupManager.AllGroups.ToList();
            this.Machine.TargetSlotGroup = this.Machine.TargetSlotGroup.Where(s => this.groups.Contains(s));
        }

        private List<SlotGroup> groups;

        protected override void FillTab()
        {
            var description = "NR_AutoMachineTool.ProductLimitation.Description".Translate();
            var label = "NR_AutoMachineTool.ProductLimitation.ValueLabel".Translate();
            var checkBoxLabel = "NR_AutoMachineTool.ProductLimitation.CheckBoxLabel".Translate();

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            
            list.Begin(inRect);
            list.Gap();

            var rect = list.GetRect(50f);
            Widgets.Label(rect, description);
            list.Gap();

            rect = list.GetRect(30f);
            bool limitation = this.Machine.ProductLimitation;
            Widgets.CheckboxLabeled(rect, checkBoxLabel, ref limitation);
            this.Machine.ProductLimitation = limitation;
            list.Gap();

            rect = list.GetRect(30f);
            string buf = this.Machine.ProductLimitCount.ToString();
            int limit = this.Machine.ProductLimitCount;
            Widgets.Label(rect.LeftHalf(), label);
            Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
            list.Gap();

            rect = list.GetRect(30f);
            Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.CountZone".Translate());
            if(Widgets.ButtonText(rect.RightHalf(), this.Machine.TargetSlotGroup.Fold("NR_AutoMachineTool.EntierMap".Translate())(s => s.parent.SlotYielderLabel())))
            {
                Find.WindowStack.Add(new FloatMenu(groups
                    .Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(), () => this.Machine.TargetSlotGroup = Option(g)))
                    .ToList()
                    .Ins(0, new FloatMenuOption("NR_AutoMachineTool.EntierMap".Translate(), () => this.Machine.TargetSlotGroup = Nothing<SlotGroup>()))));
            }
            list.Gap();

            list.End();

            this.Machine.ProductLimitCount = limit;
        }
    }
}
