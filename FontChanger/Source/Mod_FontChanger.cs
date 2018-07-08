using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FontChanger
{
    public class Mod_FontChanger : Mod
    {
        public ModSetting_FontChanger Setting { get; private set; }

        public Mod_FontChanger(ModContentPack content) : base(content)
        {
            this.Setting = this.GetSettings<ModSetting_FontChanger>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            this.Setting.DoSettingWindow(inRect);
        }

        public override string SettingsCategory()
        {
            return "FontChanger";
        }
    }
}
