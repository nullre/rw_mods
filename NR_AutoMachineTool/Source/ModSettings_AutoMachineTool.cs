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
    public class ModSetting_AutoMachineTool :  ModSettings
    {
        public class TierSetting : IExposable
        {
            public int minSupplyPower;
            public int maxSupplyPower;
            public int skillLevel;
            public float speedFactor;

            public void ExposeData()
            {
                Scribe_Values.Look<int>(ref this.minSupplyPower, "minSupplyPower");
                Scribe_Values.Look<int>(ref this.maxSupplyPower, "maxSupplyPower");
                Scribe_Values.Look<int>(ref this.skillLevel, "skillLevel");
                Scribe_Values.Look<float>(ref this.speedFactor, "speedFactor");
            }
        }

        public float carrySpeedFactor = 1f;

        public float pullSpeedFactor = 1f;

        public void RestoreDefault()
        {
            this.setting = CreateDefault();
            this.carrySpeedFactor = 1f;
            this.pullSpeedFactor = 1f;
        }

        private List<TierSetting> setting = CreateDefault();

        private static List<TierSetting> CreateDefault()
        {
            return new List<TierSetting> {
                new TierSetting() { minSupplyPower = 100, maxSupplyPower = 1000, skillLevel = 5, speedFactor = 1f },
                new TierSetting() { minSupplyPower = 500, maxSupplyPower = 5000, skillLevel = 10, speedFactor = 1.5f },
                new TierSetting() { minSupplyPower = 1000, maxSupplyPower = 100000, skillLevel = 20, speedFactor = 2f }
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<TierSetting>(ref this.setting, "setting");
            Scribe_Values.Look<float>(ref this.carrySpeedFactor, "carrySpeed", 1f);
            Scribe_Values.Look<float>(ref this.pullSpeedFactor, "pullSpeedFactor", 1f);

            Option(this.DataExposed).ForEach(e => e(this, new EventArgs()));
        }

        public TierSetting Tier(int tier)
        {
            return this.setting[tier - 1];
        }

        public event EventHandler DataExposed;

    }
}
