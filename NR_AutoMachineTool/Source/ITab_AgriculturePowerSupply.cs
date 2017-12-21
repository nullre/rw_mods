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

    public interface IAgricultureMachine
    {
        int MinPowerForSpeed { get; }
        int MaxPowerForSpeed { get; }
        int MinPowerForRange { get; }
        int MaxPowerForRange { get; }

        float SupplyPowerForSpeed { get; set; }
        float SupplyPowerForRange { get; set; }
    }

    class ITab_AgriculturePowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(500f, 380f);

        public ITab_AgriculturePowerSupply()
        {
            this.size = WinSize;
            this.labelKey = "NR_AutoMachineTool.SupplyPowerTab";

            this.descriptionForSpeed = "NR_AutoMachineTool.SupplyPowerForSpeedText".Translate();
            this.descriptionForRange = "NR_AutoMachineTool.SupplyPowerForRangeText".Translate();
        }
        
        private string descriptionForSpeed;

        private string descriptionForRange;

        private IAgricultureMachine Machine
        {
            get {
                return (IAgricultureMachine)this.SelThing;
            }
        }

        protected override void FillTab()
        {
            int minPowerSpeed = this.Machine.MinPowerForSpeed;
            int maxPowerSpeed = this.Machine.MaxPowerForSpeed;
            int minPowerRange = this.Machine.MinPowerForRange;
            int maxPowerRange = this.Machine.MaxPowerForRange;

            string valueLabelForSpeed = "NR_AutoMachineTool.SupplyPowerForSpeedValueLabel".Translate() + " (" + minPowerSpeed + " to " + maxPowerSpeed + ") ";
            string valueLabelForRange = "NR_AutoMachineTool.SupplyPowerForRangeValueLabel".Translate() + " (" + minPowerRange + " to " + maxPowerRange + ") ";

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            // for speed
            var rect = list.GetRect(50f);
            Widgets.Label(rect, descriptionForSpeed);
            list.Gap();

            rect = list.GetRect(50f);
            this.Machine.SupplyPowerForSpeed = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPowerForSpeed, (float)minPowerSpeed, (float)maxPowerSpeed, true, valueLabelForSpeed, minPowerSpeed.ToString(), maxPowerSpeed.ToString(), 100);
            list.Gap();

            rect = list.GetRect(30f);
            string buf = this.Machine.SupplyPowerForSpeed.ToString();
            int power = (int)this.Machine.SupplyPowerForSpeed;
            Widgets.Label(rect.LeftHalf(), valueLabelForSpeed);
            Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref power, ref buf, this.Machine.SupplyPowerForSpeed, this.Machine.SupplyPowerForSpeed);
            list.Gap();

            this.Machine.SupplyPowerForSpeed = power;

            // for range
            rect = list.GetRect(50f);
            Widgets.Label(rect, descriptionForRange);
            list.Gap();
            
            rect = list.GetRect(50f);
            this.Machine.SupplyPowerForRange = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPowerForRange, (float)minPowerRange, (float)maxPowerRange, true, valueLabelForRange, minPowerRange.ToString(), maxPowerRange.ToString(), 500);
            list.Gap();

            list.End();
        }
    }
}
