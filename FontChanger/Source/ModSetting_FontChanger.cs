using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using UnityEngine;

namespace FontChanger
{
    public class ModSetting_FontChanger : ModSettings
    {
        private Dictionary<int, string> fontName = new Dictionary<int, string>();
        private Dictionary<int, int> fontSize = new Dictionary<int, int>();

        public void DoSettingWindow(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            GameFont defaultFont = Text.Font;

            for (var i = 0; i < FontSetting.defaultFonts.Length; i++)
            {
                var rect = list.GetRect(40);
                var index = i;
                Text.Font = (GameFont)i;
                Widgets.Label(rect.LeftHalf().LeftHalf().LeftHalf(), Text.Font.ToString());
                Widgets.Label(rect.LeftHalf().LeftHalf().RightHalf(), fontName.ContainsKey(index) ? fontName[index] : "Default");
                Widgets.Label(rect.LeftHalf().RightHalf(), "123 abc あいう 漢字");
                Text.Font = defaultFont;

                if(Widgets.ButtonText(rect.RightHalf().LeftHalf().LeftHalf(), "Select Font"))
                {
                    Find.WindowStack.Add(new FloatMenu(FontSetting.installedFontNames.Select(n => new FloatMenuOption(n, () => this.fontName[index] = n)).ToList()));
                }

                if (this.fontName.ContainsKey(index))
                {
                    int size = this.fontSize.ContainsKey(index) ? this.fontSize[index] : FontSetting.defaultFonts[index].fontSize;
                    string buf = size.ToString();
                    Widgets.TextFieldNumericLabeled(rect.RightHalf().LeftHalf().RightHalf(), "Size", ref size, ref buf, 0, 50);
                    this.fontSize[index] = size;
                }

                if (Widgets.ButtonText(rect.RightHalf().RightHalf().LeftHalf(), "Apply"))
                {
                    this.UpdateFont(index);
                }

                if (Widgets.ButtonText(rect.RightHalf().RightHalf().RightHalf(), "Reset"))
                {
                    this.fontName.Remove(index);
                    this.fontSize.Remove(index);
                    this.UpdateFont(index);
                }
                list.Gap();
            }
            list.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.fontName, "fontName");
            Scribe_Collections.Look(ref this.fontSize, "fontSize");
            if (this.fontName == null)
                this.fontName = new Dictionary<int, string>();
            if (this.fontSize == null)
                this.fontSize = new Dictionary<int, int>();
        }

        public void UpdateFont(int index)
        {
            Text.fontStyles[index].font = this.fontName.ContainsKey(index) ?
                Font.CreateDynamicFontFromOSFont(this.fontName[index], this.fontSize.ContainsKey(index) ? this.fontSize[index] : FontSetting.defaultFonts[index].fontSize) :
                FontSetting.defaultFonts[index];
        }
    }

    [StaticConstructorOnStartup]
    public static class FontSetting
    {
        public static readonly string[] installedFontNames;
        public static readonly Font[] defaultFonts;

        static FontSetting()
        {
            installedFontNames = Font.GetOSInstalledFontNames().ToArray();
            defaultFonts = Text.fontStyles.Select(s => s.font).ToArray();

            for (var i = 0; i < defaultFonts.Length; i++)
            {
                LoadedModManager.GetMod<Mod_FontChanger>().Setting.UpdateFont(i);
            }
        }
    }
}
