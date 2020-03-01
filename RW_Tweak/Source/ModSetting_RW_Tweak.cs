using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Verse;
using RimWorld;
using UnityEngine;

namespace RW_Tweak
{
    public class ModSetting_RW_Tweak : ModSettings
    {
        private Dictionary<int, string> fontName = new Dictionary<int, string>();
        private Dictionary<int, int> fontSize = new Dictionary<int, int>();

        public void DoSettingWindow(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            list.CheckboxLabeled("No Random Quality", ref this.noRandomQuality);
            
            if (this.noRandomQuality)
            {
                var length = Enum.GetValues(typeof(QualityCategory)).Length;
                for(int i = 0; i < length - 1; i++)
                {
                    var l = qualityThreshold[i];
                    var buf = l.ToString();
                    var min = 0;
                    var max = 20;
                    if(i > 0)
                    {
                        min = qualityThreshold[i - 1];
                    }
                    if(i < length - 2)
                    {
                        max = qualityThreshold[i + 1];
                    }
                    list.TextFieldNumericLabeled(((QualityCategory)i).ToString().Translate() + " MaxLevel", ref l, ref buf, min, max);
                    qualityThreshold[i] = l;
                    list.Gap();
                }
                list.TextEntryLabeled(((QualityCategory)length - 1).ToString().Translate() + " MaxLevel", "20");
            }

            list.GapLine();
            list.CheckboxLabeled("No XP Down", ref this.noXPDown);
            list.GapLine();

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

        public bool noRandomQuality = false;

        public List<int> qualityThreshold = defaultQualityThreshold();

        private static Func<List<int>> defaultQualityThreshold = () => new List<int> {
                2, // Awful
                5, // Poor
                9, // Normal
                12, // Good
                16, // Excellent
                19 // Masterwork
                   // Legendary
        };

        public bool noXPDown = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.fontName, "fontName");
            Scribe_Collections.Look(ref this.fontSize, "fontSize");
            if (this.fontName == null)
                this.fontName = new Dictionary<int, string>();
            if (this.fontSize == null)
                this.fontSize = new Dictionary<int, int>();

            Scribe_Values.Look(ref this.noRandomQuality, "noRandomQuality", false);
            Scribe_Values.Look(ref this.noXPDown, "noXPDown", false);

            Scribe_Collections.Look(ref this.qualityThreshold, "qualityThreshold");
            this.qualityThreshold = this.qualityThreshold ?? defaultQualityThreshold();
        }

        public void UpdateFont(int index)
        {
            var font = this.fontName.ContainsKey(index) ?
                Font.CreateDynamicFontFromOSFont(this.fontName[index], this.fontSize.ContainsKey(index) ? this.fontSize[index] : FontSetting.defaultFonts[index].fontSize) :
                FontSetting.defaultFonts[index];

            Text.fontStyles[index].font = font;
            Text.textFieldStyles[index].font = font;
            Text.textAreaStyles[index].font = font;
            Text.textAreaReadOnlyStyles[index].font = font;

            var lineHeights = (float[])typeof(Text).GetField("lineHeights", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var spaceBetweenLines = (float[])typeof(Text).GetField("spaceBetweenLines", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var fonts = (Font[])typeof(Text).GetField("fonts", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            lineHeights[index] = Text.fontStyles[index].CalcHeight(new GUIContent("W"), 999f);
            spaceBetweenLines[index] = Text.fontStyles[index].CalcHeight(new GUIContent("W\nW"), 999f) - Text.fontStyles[index].CalcHeight(new GUIContent("W"), 999f) * 2f;
            fonts[index] = Text.fontStyles[index].font;
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
                LoadedModManager.GetMod<Mod_RW_Tweak>().Setting.UpdateFont(i);
            }
        }
    }
}
