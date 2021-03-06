﻿using System;
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
    public interface IBillNotificationReceiver
    {
        void OnComplete(Bill_Production bill, List<Thing> ingredients);
    }

    public class Building_MaterialMahcine : Building_WorkTable, IBillNotificationReceiver, ITabBillTable
    {
        public class MaterializeRecipeDefData : IExposable
        {
            public MaterializeRecipeDefData() {
            }

            public MaterializeRecipeDefData(ThingDef thing, ThingDef stuff, int prodCount, int workAmount)
            {
                this.thing = thing;
                this.stuff = stuff;
                this.prodCount = prodCount;
                this.workAmount = workAmount;
                this.defName = GetDefName(thing, stuff, prodCount);
            }
            public string defName;
            private ThingDef thing;
            private ThingDef stuff;
            private int prodCount;
            private int workAmount;

            public void ExposeData()
            {
                Scribe_Values.Look<string>(ref this.defName, "defName");
                Scribe_Values.Look<int>(ref this.prodCount, "prodCount");
                Scribe_Values.Look<int>(ref this.workAmount, "workAmount");
                Scribe_Defs.Look<ThingDef>(ref this.thing, "thing");
                Scribe_Defs.Look<ThingDef>(ref this.stuff, "stuff");
            }

            public RecipeDef GetRecipe()
            {
                return DefDatabase<RecipeDef>.GetNamed(this.defName);
            }

            public void Register(float loss)
            {
                if(DefDatabase<RecipeDef>.GetNamedSilentFail(defName) == null)
                {
                    var count = Mathf.CeilToInt(GetEnergyAmount(this.thing, this.stuff) * prodCount / loss);
                    var label = "NR_AutoMachineTool.MaterialMachine.RecipeMaterializeLabel".Translate(this.thing.label, this.stuff == null ? "" : (string)"NR_AutoMachineTool.MaterialMachine.RecipeMaterializeStuff".Translate(this.stuff.label), count, prodCount);
                    var jobName = "NR_AutoMachineTool.MaterialMachine.RecipeMaterializeJobName".Translate(this.thing.label);
                    var recipe = CreateMaterializeRecipeDef(defName, label, jobName, workAmount, count, this.thing, this.prodCount, this.stuff);
                    DefDatabase<RecipeDef>.Add(recipe);
                }
            }

            public static string GetDefName(ThingDef thing, ThingDef stuff, int prodCount)
            {
                return MaterializeRecipeDefPrefix + thing.defName + (stuff == null ? "" : "_" + stuff.defName) + "_" + prodCount;
            }

            public const string MaterializeRecipeDefPrefix = "NR_AutoMachineTool.Materialize_";
        }

        private const string ToEnergy10DefName = "NR_AutoMachineTool.ToEnergy10";
        private const string ToEnergy100DefName = "NR_AutoMachineTool.ToEnergy100";

        private const string ScanMaterialDefName = "NR_AutoMachineTool.ScanMaterial";

        private float Loss { get => 0.3f; }

        ThingDef ITabBillTable.def => this.def;

        BillStack ITabBillTable.billStack => this.billStack;

        private static readonly HashSet<string> ToEnergyDefNames = new HashSet<string>(new string[] { ToEnergy10DefName, ToEnergy100DefName });

        private static ThingDef energyThingDef;

        private static List<RecipeDef> toEnergyRecipes;

        private static RecipeDef scanMaterialRecipe;

        private List<MaterializeRecipeDefData> materializeRecipeData = new List<MaterializeRecipeDefData>();

        [Unsaved]
        private List<RecipeDef> allRecipes = new List<RecipeDef>();

        public IEnumerable<RecipeDef> AllRecipes => this.allRecipes;

        public bool IsRemovable(RecipeDef recipe)
        {
            return recipe.defName.StartsWith(MaterializeRecipeDefData.MaterializeRecipeDefPrefix);
        }

        public void RemoveRecipe(RecipeDef recipe)
        {
            this.billStack.Bills.Where(b => b.recipe.defName == recipe.defName).ForEach(b =>
            {
                b.suspended = true;
                b.deleted = true;
            });
            this.billStack.Bills.RemoveAll(b => b.recipe.defName == recipe.defName);
            this.materializeRecipeData.RemoveAll(r => r.defName == recipe.defName);
            this.allRecipes.RemoveAll(r => r.defName == recipe.defName);
        }

        private static Action<RecipeDef, Type> ingredientValueGetterClassSetter = GenerateSetFieldDelegate<RecipeDef, Type>(typeof(RecipeDef).GetField("ingredientValueGetterClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        private static Action<RecipeDef, IngredientValueGetter> ingredientValueGetterIntSetter = GenerateSetFieldDelegate<RecipeDef, IngredientValueGetter>(typeof(RecipeDef).GetField("ingredientValueGetterInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

        private static void RegisterToEnergyRecipes()
        {
            if (energyThingDef == null)
            {
                energyThingDef = ThingDef.Named("NR_AutoMachineTool_MaterialEnergy");
            }

            if (toEnergyRecipes == null)
            {
                var toEnergyDefs = new HashSet<ThingDef>(DefDatabase<ThingDef>.AllDefs.Where(t => t.category == ThingCategory.Item).Where(t => GetEnergyAmount(t) > 0.1f));
                toEnergyRecipes = new List<RecipeDef>();
                toEnergyRecipes.Add(CreateToEnergyRecipeDef(ToEnergy10DefName, "NR_AutoMachineTool.MaterialMachine.RecipeToEnergyLabel".Translate(10), "NR_AutoMachineTool.MaterialMachine.RecipeToEnergyJobName".Translate(10), 300, 10, toEnergyDefs));
                toEnergyRecipes.Add(CreateToEnergyRecipeDef(ToEnergy100DefName, "NR_AutoMachineTool.MaterialMachine.RecipeToEnergyLabel".Translate(100), "NR_AutoMachineTool.MaterialMachine.RecipeToEnergyJobName".Translate(100), 1000, 100, toEnergyDefs));
                toEnergyRecipes.ForEach(r =>
                {
                    ingredientValueGetterClassSetter(r, typeof(IngredientValueGetter_Energy));
                    ingredientValueGetterIntSetter(r, null);
                });
                DefDatabase<RecipeDef>.Add(toEnergyRecipes);
            }
            if (scanMaterialRecipe == null)
            {
                var toEnergyDefs = new HashSet<ThingDef>(DefDatabase<ThingDef>.AllDefs.Where(t => t.category == ThingCategory.Item).Where(t => GetEnergyAmount(t) > 0.1f));
                scanMaterialRecipe = CreateScanMaterialRecipeDef(ScanMaterialDefName, "NR_AutoMachineTool.MaterialMachine.RecipeScanMaterialLabel".Translate(), "NR_AutoMachineTool.MaterialMachine.RecipeScanMaterialJobName".Translate(), 5000, toEnergyDefs);
                DefDatabase<RecipeDef>.Add(scanMaterialRecipe);
            }
        }

        public override void ExposeData()
        {
            RegisterToEnergyRecipes();
            Scribe_Collections.Look<MaterializeRecipeDefData>(ref this.materializeRecipeData, "materializeRecipeData", LookMode.Deep);
            if(this.materializeRecipeData == null)
            {
                this.materializeRecipeData = new List<MaterializeRecipeDefData>();
            }
            this.materializeRecipeData.ForEach(d => d.Register(this.Loss));

            base.ExposeData();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RegisterToEnergyRecipes();

            this.allRecipes.Add(scanMaterialRecipe);
            this.allRecipes.AddRange(toEnergyRecipes);
            this.allRecipes.AddRange(this.materializeRecipeData.Select(r => r.GetRecipe()));
        }

        public void OnComplete(Bill_Production bill, List<Thing> ingredients)
        {
            if (ScanMaterialDefName == bill.recipe.defName)
            {
                ingredients.ForEach(i =>
                {
                    string defName = MaterializeRecipeDefData.GetDefName(i.def, i.Stuff, 1);
                    if (!this.materializeRecipeData.Any(r => r.defName == defName))
                    {
                        var newRecipe = new MaterializeRecipeDefData(i.def, i.Stuff, 1, 300);
                        this.materializeRecipeData.Add(newRecipe);
                        newRecipe.Register(this.Loss);
                        this.allRecipes.Add(newRecipe.GetRecipe());
                        if (i.def.stackLimit >= 10)
                        {
                            newRecipe = new MaterializeRecipeDefData(i.def, i.Stuff, 10, 1000);
                            newRecipe.Register(this.Loss);
                            this.materializeRecipeData.Add(newRecipe);
                            this.allRecipes.Add(newRecipe.GetRecipe());
                        }
                    }
                });
            }
        }

        private static RecipeDef CreateRecipeDef(string defName, string label, string jobString, float workAmount)
        {
            RecipeDef r = new RecipeDef();
            r.defName = defName;
            r.label = label;
            r.jobString = jobString;
            r.workAmount = workAmount;
            r.workSpeedStat = StatDefOf.WorkToMake;
            r.efficiencyStat = StatDefOf.WorkToMake;

            r.workSkill = SkillDefOf.Crafting;
            r.requiredGiverWorkType = WorkTypeDefOf.Crafting;
            r.workSkillLearnFactor = 0;

            return r;
        }

        private static RecipeDef CreateScanMaterialRecipeDef(string defName, string label, string jobString, float workAmount, HashSet<ThingDef> toEnergyDefs)
        {
            var r = CreateRecipeDef(defName, label, jobString, workAmount);

            var c = new IngredientCount();
            r.ingredients.Add(c);
            c.SetBaseCount(1);
            c.filter = new ThingFilter();
            toEnergyDefs.ForEach(d =>
            {
                if (d.defName != "NR_AutoMachineTool_MaterialEnergy")
                {
                    r.fixedIngredientFilter.SetAllow(d, true);
                    c.filter.SetAllow(d, true);
                }
            });
            c.filter.RecalculateDisplayRootCategory();
            r.defaultIngredientFilter = new ThingFilter();
            r.defaultIngredientFilter.SetDisallowAll(null);
            r.fixedIngredientFilter.RecalculateDisplayRootCategory();
            r.ResolveReferences();
            r.allowMixingIngredients = true;

            return r;
        }

        private static RecipeDef CreateToEnergyRecipeDef(string defName, string label, string jobString, float workAmount, int count, HashSet<ThingDef> toEnergyDefs)
        {
            var r = CreateRecipeDef(defName, label, jobString, workAmount);

            var c = new IngredientCount();
            r.ingredients.Add(c);
            c.SetBaseCount(count);
            c.filter = new ThingFilter();
            toEnergyDefs.ForEach(d =>
            {
                if (d.defName != "NR_AutoMachineTool_MaterialEnergy")
                {
                    r.fixedIngredientFilter.SetAllow(d, true);
                    c.filter.SetAllow(d, true);
                }
            });
            c.filter.RecalculateDisplayRootCategory();
            r.defaultIngredientFilter = new ThingFilter();
            r.defaultIngredientFilter.SetDisallowAll(null);
            r.fixedIngredientFilter.RecalculateDisplayRootCategory();
            r.ResolveReferences();
            r.products.Add(new ThingDefCount(energyThingDef, count));
            r.allowMixingIngredients = true;

            return r;
        }

        private static RecipeDef CreateMaterializeRecipeDef(string defName, string label, string jobString, float workAmount, int energyCount, ThingDef product, int productCount, ThingDef stuff)
        {
            var r = CreateRecipeDef(defName, label, jobString, workAmount);

            var c = new IngredientCount();
            r.ingredients.Add(c);
            c.SetBaseCount(energyCount);
            c.filter = new ThingFilter();
            r.defaultIngredientFilter = new ThingFilter();

            if (stuff != null)
            {
                var st = new IngredientCount();
                r.ingredients.Add(st);
                st.filter.SetAllow(stuff, true);
                // とりえあずstuff は 10個でいい.
                st.SetBaseCount(10);
            }

            c.filter.SetAllow(energyThingDef, true);
            r.defaultIngredientFilter.SetAllow(energyThingDef, true);
            c.filter.RecalculateDisplayRootCategory();
            
            r.fixedIngredientFilter.RecalculateDisplayRootCategory();
            r.ResolveReferences();
            r.products.Add(new ThingDefCount(product, productCount));

            return r;
        }

        public Bill MakeNewBill(RecipeDef recipe)
        {
            return new Bill_ProductionNotifyComplete(recipe);
        }
    }
}
