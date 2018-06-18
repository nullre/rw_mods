using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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

            var hopmAsm = LoadedModManager.RunningMods.Where(m => m.Name.StartsWith("Harvest Organs Post Mortem - 2.0")).SelectMany(m => m.assemblies.loadedAssemblies)
                .Where(a => a.GetType("Autopsy.Mod") != null)
                .FirstOption();

            var hugsAsm = LoadedModManager.RunningMods.Where(m => m.Name == "HugsLib").SelectMany(m => m.assemblies.loadedAssemblies)
                .Where(a => a.GetType("HugsLib.Settings.SettingHandle") != null)
                .FirstOption();

            this.Hopm = hopmAsm.SelectMany(ho => hugsAsm.SelectMany(hu => HopmMod.CreateHopmMod(ho, hu)));
        }

        public Option<HopmMod> Hopm;

        public override string SettingsCategory()
        {
            return "NR_AutoMachineTool.SettingName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            this.Setting.DoSetting(inRect);
        }

        public class HopmMod
        {
            private Type modType;
            private Type recipeInfoType;
            private Type settingHandlerType;

            private string Constants_AutopsyBasic;
            private string Constants_AutopsyAdvanced;
            private string Constants_AutopsyGlitterworld;
            private string Constants_AutopsyAnimal;
            private MethodInfo traverseBody;

            private static Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();

            private static Dictionary<string, PropertyInfo> fieldValueProps = new Dictionary<string, PropertyInfo>();

            public static Option<HopmMod> CreateHopmMod(Assembly hopmAsm, Assembly hugsAsm)
            {
                try
                {
                    var hopm = new HopmMod();

                    hopm.modType = hopmAsm.GetType("Autopsy.Mod");
                    hopm.settingHandlerType = hugsAsm.GetType("HugsLib.Settings.SettingHandle`1");

                    hopm.recipeInfoType = hopmAsm.GetType("Autopsy.RecipeInfo");

                    var utilType = hopmAsm.GetType("Autopsy.NewMedicaRecipesUtility");
                    var traverseBody = utilType.GetMethod("TraverseBody", new Type[]{ hopm.recipeInfoType, typeof(Corpse), typeof(float) });
                    hopm.traverseBody = traverseBody;


                    var contentType = hopmAsm.GetType("Autopsy.Constants");
                    hopm.Constants_AutopsyBasic = (string)contentType.GetField("AutopsyBasic", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    hopm.Constants_AutopsyAdvanced = (string)contentType.GetField("AutopsyAdvanced", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    hopm.Constants_AutopsyGlitterworld = (string)contentType.GetField("AutopsyGlitterworld", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    hopm.Constants_AutopsyAnimal = (string)contentType.GetField("AutopsyAnimal", BindingFlags.Static | BindingFlags.Public).GetValue(null);

                    return Just(hopm);
                }
                catch (Exception e)
                {
                    Log.Error("HOPMのメタデータ取得エラー. " + e.ToString());
                    return Nothing<HopmMod>();
                }
            }

            private HopmMod()
            {
            }

            private object GetValue(string fieldName)
            {
                if (!fields.ContainsKey(fieldName))
                {
                    fields[fieldName] = modType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
                    fieldValueProps[fieldName] = settingHandlerType.MakeGenericType(fields[fieldName].FieldType.GetGenericArguments()).GetProperty("Value");
                }
                var val = fields[fieldName].GetValue(null);
                return fieldValueProps[fieldName].GetValue(val, new object[0]);
            }

            public void Postfix_MakeRecipeProducts(ref IEnumerable<Thing> __result, RecipeDef recipeDef, float skillChance, List<Thing> ingredients)
            {
                string prefix = null;
                if (recipeDef.defName == Constants_AutopsyBasic)
                {
                    prefix = "Basic";
                }
                else if (recipeDef.defName == Constants_AutopsyAdvanced)
                {
                    prefix = "Advanced";
                }
                else if (recipeDef.defName == Constants_AutopsyGlitterworld)
                {
                    prefix = "Glitter";
                }
                else if (recipeDef.defName == Constants_AutopsyAnimal)
                {
                    prefix = "Animal";
                }
                if (prefix != null)
                {
                    var maxChance = prefix == "Animal" ? 0f : GetValue(prefix + "AutopsyOrganMaxChance");
                    var age = prefix == "Animal" ? 0 : (int)GetValue(prefix + "AutopsyCorpseAge") * 2500;
                    var frozen = prefix == "Animal" ? 0f : GetValue(prefix + "AutopsyFrozenDecay");
                    var recipeSettings = Activator.CreateInstance(recipeInfoType,
                        maxChance,
                        age,
                        GetValue(prefix + "AutopsyBionicMaxChance"),
                        GetValue(prefix + "AutopsyMaxNumberOfOrgans"),
                        frozen);
                    skillChance *= (float)GetValue(prefix + "AutopsyMedicalSkillScaling");

                    List<Thing> result = __result as List<Thing> ?? __result.ToList();
                    foreach (Corpse corpse in ingredients.OfType<Corpse>())
                        result.AddRange((IEnumerable<Thing>)this.traverseBody.Invoke(null, new object[] { recipeSettings, corpse, skillChance }));

                    __result = result;
                }
            }
        }
    }
}
