using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace RW_Tweak
{
    public class Mod_RW_Tweak : Mod
    {
        public ModSetting_RW_Tweak Setting { get; private set; }

        public Mod_RW_Tweak(ModContentPack content) : base(content)
        {
            new Harmony("Mod_RW_Tweak").PatchAll(Assembly.GetExecutingAssembly());
            this.Setting = this.GetSettings<ModSetting_RW_Tweak>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            this.Setting.DoSettingWindow(inRect);
        }

        public override string SettingsCategory()
        {
            return "RW_Tweak";
        }
    }
}
