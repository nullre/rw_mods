using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RW_Tweak
{
    [HarmonyPatch(typeof(SkillRecord), "Interval", new Type[] { })]
    class Patch_SkillRecord_Interval
    {
        static bool Prefix(SkillRecord __instance)
        {
            return !LoadedModManager.GetMod<Mod_RW_Tweak>().Setting.noXPDown;
        }
    }
}
