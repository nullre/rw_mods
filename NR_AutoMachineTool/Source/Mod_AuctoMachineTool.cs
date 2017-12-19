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
    public class Mod_AutoMachineTool : Mod
    {
        public ModSetting_AutoMachineTool Setting { get; private set; }

        public Mod_AutoMachineTool(ModContentPack content) : base(content)
        {
            this.Setting = this.GetSettings<ModSetting_AutoMachineTool>();
        }

        public override string SettingsCategory()
        {
            return "NR_AutoMachineTool.SettingName".Translate();
        }

        private Vector2 scrollPosition;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 30f, 830f);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);
            for (int i = 1; i <= 3; i++)
            {
                Text.Font = GameFont.Medium;

                list.Label("tier " + i);
                list.GapLine();
                list.Gap();

                Text.Font = GameFont.Small;

                // skill level
                var rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingSkillLevel".Translate(this.Setting.Tier(i).skillLevel));
                this.Setting.Tier(i).skillLevel = (int)Widgets.HorizontalSlider(rect.RightHalf(), this.Setting.Tier(i).skillLevel, 1, 20, true, "NR_AutoMachineTool.SettingSkillLevel".Translate(this.Setting.Tier(i).skillLevel), 1.ToString(), 20.ToString(), 1);
                list.Gap();

                // min power
                string buffMin = null;
                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingMinSupplyPower".Translate(this.Setting.Tier(i).minSupplyPower));
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref this.Setting.Tier(i).minSupplyPower, ref buffMin, 100, 100000);
                list.Gap();

                // max power
                string buffMax = null;
                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingMaxSupplyPower".Translate(this.Setting.Tier(i).maxSupplyPower));
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref this.Setting.Tier(i).maxSupplyPower, ref buffMax, 1000, 10000000);
                list.Gap();

                // speedfactor
                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingSpeedFactor".Translate(this.Setting.Tier(i).speedFactor.ToString("F1")));
                this.Setting.Tier(i).speedFactor = Widgets.HorizontalSlider(rect.RightHalf(), this.Setting.Tier(i).speedFactor, 0.1f, 10.0f, true, "NR_AutoMachineTool.SettingSpeedFactor".Translate(this.Setting.Tier(i).speedFactor.ToString("F1")), (0.1f).ToString("F1"), 10f.ToString("F1"), 0.1f);
                list.Gap();

                if (this.Setting.Tier(i).minSupplyPower > this.Setting.Tier(i).maxSupplyPower)
                {
                    this.Setting.Tier(i).minSupplyPower = this.Setting.Tier(i).maxSupplyPower;
                }
            }
            list.GapLine();
            list.Gap();

            // belt conveyor speedfactor
            var rect2 = list.GetRect(30f);
            Widgets.Label(rect2.LeftHalf(), "NR_AutoMachineTool.SettingCarrySpeedFactor".Translate(this.Setting.carrySpeedFactor.ToString("F1")));
            this.Setting.carrySpeedFactor = Widgets.HorizontalSlider(rect2.RightHalf(), this.Setting.carrySpeedFactor, 0.1f, 10.0f, true, "NR_AutoMachineTool.SettingCarrySpeedFactor".Translate(this.Setting.carrySpeedFactor.ToString("F1")), (0.1f).ToString("F1"), 10f.ToString("F1"), 0.1f);
            list.Gap();

            // puller speedfactor
            rect2 = list.GetRect(30f);
            Widgets.Label(rect2.LeftHalf(), "NR_AutoMachineTool.SettingPullSpeedFactor".Translate(this.Setting.pullSpeedFactor.ToString("F1")));
            this.Setting.pullSpeedFactor = Widgets.HorizontalSlider(rect2.RightHalf(), this.Setting.pullSpeedFactor, 0.1f, 10.0f, true, "NR_AutoMachineTool.SettingPullSpeedFactor".Translate(this.Setting.pullSpeedFactor.ToString("F1")), (0.1f).ToString("F1"), 10f.ToString("F1"), 0.1f);
            list.Gap();

            // Restore
            if (Widgets.ButtonText(list.GetRect(30f).RightHalf().RightHalf(), "NR_AutoMachineTool.SettingReset".Translate()))
            {
                this.Setting.RestoreDefault();
            }

            list.End();
            Widgets.EndScrollView();
        }
    }
}
