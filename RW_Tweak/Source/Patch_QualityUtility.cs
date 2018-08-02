using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;

namespace RW_Tweak
{
    [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new Type[] { typeof(int), typeof(bool) })]
    // public static QualityCategory GenerateQualityCreatedByPawn(int relevantSkillLevel, bool inspired)
    class Patch_QualityUtility_GenerateQualityCreatedByPawn
    {
        static bool Prefix(int relevantSkillLevel, bool inspired)
        {
            return !LoadedModManager.GetMod<Mod_RW_Tweak>().Setting.noRandomQuality;
        }

        static void Postfix(int relevantSkillLevel, bool inspired, ref QualityCategory __result)
        {
            var setting = LoadedModManager.GetMod<Mod_RW_Tweak>().Setting;
            if (!setting.noRandomQuality)
            {
                return;
            }
            for(var i = 0; i < setting.qualityThreshold.Count; i++)
            {
                if (relevantSkillLevel <= setting.qualityThreshold[i])
                {
                    __result = (QualityCategory)i;
                    return;
                }
            }

            __result = QualityCategory.Legendary;
        }
    }
}
