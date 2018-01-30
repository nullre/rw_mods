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
    public class BasicMachineSetting : IExposable
    {
        public int minSupplyPowerForSpeed;
        public int maxSupplyPowerForSpeed;
        public float speedFactor;

        public virtual void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.minSupplyPowerForSpeed, "minSupplyPowerForSpeed", 100);
            Scribe_Values.Look<int>(ref this.maxSupplyPowerForSpeed, "maxSupplyPowerForSpeed", 10000);
            Scribe_Values.Look<float>(ref this.speedFactor, "speedFactor");
        }

        protected virtual IEnumerable<Action<Listing>> ListDrawAction()
        {
            yield return (list) => DrawPower(list, "NR_AutoMachineTool.SettingMinSupplyPower", "NR_AutoMachineTool.Speed", ref this.minSupplyPowerForSpeed, 0, 100000);
            yield return (list) => DrawPower(list, "NR_AutoMachineTool.SettingMaxSupplyPower", "NR_AutoMachineTool.Speed", ref this.maxSupplyPowerForSpeed, 0, 10000000);
            yield return (list) => DrawSpeedFactor(list, ref this.speedFactor);
        }

        public void DrawModSetting(Listing list)
        {
            ListDrawAction().ForEach(a =>
            {
                a(list);
                list.Gap();
            });
            this.FinishDrawModSetting();
        }

        protected virtual void FinishDrawModSetting()
        {
            if (this.minSupplyPowerForSpeed > this.maxSupplyPowerForSpeed)
            {
                this.minSupplyPowerForSpeed = this.maxSupplyPowerForSpeed;
            }
        }

        public float GetHeight()
        {
            return this.ListDrawAction().Count() * 42f;
        }

        protected static void DrawPower(Listing list, string label, string labelParm, ref int power, float min, float max)
        {
            string buff = null;
            var rect = list.GetRect(30f);
            Widgets.Label(rect.LeftHalf(), label.Translate(labelParm.Translate()));
            Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref power, ref buff, min, max);
        }

        protected static void DrawSpeedFactor(Listing list, ref float factor)
        {
            var rect = list.GetRect(30f);
            Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingSpeedFactor".Translate(factor.ToString("F1")));
            factor = Widgets.HorizontalSlider(rect.RightHalf(), factor, 0.1f, 10.0f, true, "NR_AutoMachineTool.SettingSpeedFactor".Translate(factor.ToString("F1")), (0.1f).ToString("F1"), 10f.ToString("F1"), 0.1f);
        }

        protected static void DrawSkillLevel(Listing list, ref int skillLevel)
        {
            var rect = list.GetRect(30f);
            Widgets.Label(rect.LeftHalf(), "NR_AutoMachineTool.SettingSkillLevel".Translate(skillLevel));
            skillLevel = (int)Widgets.HorizontalSlider(rect.RightHalf(), skillLevel, 1, 20, true, "NR_AutoMachineTool.SettingSkillLevel".Translate(skillLevel), 1.ToString(), 20.ToString(), 1);
        }
    }

    public class SkillMachineSetting : BasicMachineSetting
    {
        public int skillLevel;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.skillLevel, "skillLevel");
        }

        protected override IEnumerable<Action<Listing>> ListDrawAction()
        {
            yield return (list) => DrawSkillLevel(list, ref this.skillLevel);
            foreach(var a in base.ListDrawAction())
            {
                yield return a;
            }
        }
    }

    public class RangeMachineSetting : BasicMachineSetting
    {
        public int minSupplyPowerForRange;
        public int maxSupplyPowerForRange;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.minSupplyPowerForRange, "minSupplyPowerForRange", 0);
            Scribe_Values.Look<int>(ref this.maxSupplyPowerForRange, "maxSupplyPowerForRange", 5000);
        }

        protected override IEnumerable<Action<Listing>> ListDrawAction()
        {
            var actions = base.ListDrawAction().ToList();
            yield return actions[0];
            yield return actions[1];
            yield return (list) => DrawPower(list, "NR_AutoMachineTool.SettingMinSupplyPower", "NR_AutoMachineTool.Range", ref this.minSupplyPowerForRange, 0, 1000);
            yield return (list) => DrawPower(list, "NR_AutoMachineTool.SettingMaxSupplyPower", "NR_AutoMachineTool.Range", ref this.maxSupplyPowerForRange, 0, 10000);
            yield return actions[2];
        }

        protected override void FinishDrawModSetting()
        {
            base.FinishDrawModSetting();
            if (this.minSupplyPowerForRange > this.maxSupplyPowerForRange)
            {
                this.minSupplyPowerForRange = this.maxSupplyPowerForRange;
            }
        }
    }

    public class RangeSkillMachineSetting : RangeMachineSetting, IExposable
    {
        public int skillLevel;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.skillLevel, "skillLevel");
        }

        protected override IEnumerable<Action<Listing>> ListDrawAction()
        {
            yield return (list) => DrawSkillLevel(list, ref this.skillLevel);
            foreach (var a in base.ListDrawAction())
            {
                yield return a;
            }
        }
    }
}
