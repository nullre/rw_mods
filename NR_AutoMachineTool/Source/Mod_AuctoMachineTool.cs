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

            var hopmAsm = LoadedModManager.RunningMods.Where(m => m.Name.StartsWith("Harvest Organs Post Mortem -")).SelectMany(m => m.assemblies.loadedAssemblies)
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

            private RecipeDef Recipe_AutopsyBasic;
            private RecipeDef Recipe_AutopsyAdvanced;
            private RecipeDef Recipe_AutopsyGlitterworld;
            private RecipeDef Recipe_AutopsyAnimal;
            private MethodInfo traverseBody;
            private Type autopsyRecipeDefsType;

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

                    var utilType = hopmAsm.GetType("Autopsy.NewMedicalRecipesUtility");
                    var traverseBody = utilType.GetMethod("TraverseBody", new Type[]{ hopm.recipeInfoType, typeof(Corpse), typeof(float) });
                    hopm.traverseBody = traverseBody;

                    hopm.autopsyRecipeDefsType = hopmAsm.GetType("Autopsy.Util.AutopsyRecipeDefs");

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

            private bool initializedAtRuntime = false;

            private void InitializeAtRuntime()
            {
                if (!this.initializedAtRuntime)
                {
                    this.Recipe_AutopsyBasic = (RecipeDef)this.autopsyRecipeDefsType.GetField("AutopsyBasic", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    this.Recipe_AutopsyAdvanced = (RecipeDef)this.autopsyRecipeDefsType.GetField("AutopsyAdvanced", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    this.Recipe_AutopsyGlitterworld = (RecipeDef)this.autopsyRecipeDefsType.GetField("AutopsyGlitterworld", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    this.Recipe_AutopsyAnimal = (RecipeDef)this.autopsyRecipeDefsType.GetField("AutopsyAnimal", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    if (this.Recipe_AutopsyBasic == null)
                    {
                        throw new FieldAccessException("Autopsy.Util.AutopsyRecipeDefs.AutopsyBasic へのアクセスに失敗.");
                    }
                    if (this.Recipe_AutopsyAdvanced == null)
                    {
                        throw new FieldAccessException("Autopsy.Util.AutopsyRecipeDefs.AutopsyAdvanced へのアクセスに失敗.");
                    }
                    if (this.Recipe_AutopsyGlitterworld == null)
                    {
                        throw new FieldAccessException("Autopsy.Util.AutopsyRecipeDefs.AutopsyGlitterworld へのアクセスに失敗.");
                    }
                    if (this.Recipe_AutopsyAnimal == null)
                    {
                        throw new FieldAccessException("Autopsy.Util.AutopsyRecipeDefs.AutopsyAnimal へのアクセスに失敗.");
                    }
                }
                this.initializedAtRuntime = true;
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
                InitializeAtRuntime();
                try
                {
                    if (recipeDef.Equals(Recipe_AutopsyBasic))
                    {
                        prefix = "Basic";
                    }
                    else if (recipeDef.Equals(Recipe_AutopsyAdvanced))
                    {
                        prefix = "Advanced";
                    }
                    else if (recipeDef.Equals(Recipe_AutopsyGlitterworld))
                    {
                        prefix = "Glitter";
                    }
                    else if (recipeDef.Equals(Recipe_AutopsyAnimal))
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
                catch (Exception e)
                {
                    Log.ErrorOnce("HOPMの実行時エラー. " + e.ToString(), 1660882676);
                }
            }
        }
    }
}
