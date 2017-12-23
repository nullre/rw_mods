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
    interface IPowerSupplyMachine
    {
        int MinPower { get; }
        int MaxPower { get; }
        float SupplyPower { get; set; }
        string PowerSupplyMessage();
    }

    class ITab_PowerSupply : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(500f, 250f);

        public ITab_PowerSupply()
        {
            this.size = WinSize;
            this.labelKey = "NR_AutoMachineTool.SupplyPowerTab";
        }
        
        private string description;
        
        public IPowerSupplyMachine Machine
        {
            get => (IPowerSupplyMachine)this.SelThing;
        }

        protected override void FillTab()
        {
            this.description = this.Machine.PowerSupplyMessage();

            int round = this.Machine.MinPower < 1000 ? 100 : this.Machine.MinPower < 10000 ? 500 : 1000;
            if (this.Machine.MinPower % round != 0 || this.Machine.MaxPower % round != 0)
            {
                round = 1;
            }

            string valueLabel = "NR_AutoMachineTool.SupplyPowerValueLabel".Translate() + " (" + this.Machine.MinPower + " to " + this.Machine.MaxPower + ") ";

            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            var rect = list.GetRect(50f);
            Widgets.Label(rect, description);
            list.Gap();

            rect = list.GetRect(50f);
            this.Machine.SupplyPower = (int)Widgets.HorizontalSlider(rect, (float)this.Machine.SupplyPower, (float)this.Machine.MinPower, (float)this.Machine.MaxPower, true, valueLabel, this.Machine.MinPower.ToString(), this.Machine.MaxPower.ToString(), round);
            list.Gap();

            rect = list.GetRect(30f);
            string buf = this.Machine.SupplyPower.ToString();
            int power = (int)this.Machine.SupplyPower;
            Widgets.Label(rect.LeftHalf(), valueLabel);
            Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref power, ref buf, this.Machine.MinPower, this.Machine.MaxPower);
            list.Gap();

            list.End();

            this.Machine.SupplyPower = power;
        }
    }
}
