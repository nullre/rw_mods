using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using NR_MaterialEnergy.Utilities;
using static NR_MaterialEnergy.Utilities.Ops;

namespace NR_MaterialEnergy
{
    public class Building_MaterialMahcine : Building_WorkTable
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
                    var label = "NR_MaterialEnergy.RecipeMaterializeLabel".Translate(this.thing.label, this.stuff == null ? "" : "NR_MaterialEnergy.RecipeMaterializeStuff".Translate(this.stuff.label), count, prodCount);
                    var jobName = "NR_MaterialEnergy.RecipeMaterializeJobName".Translate(this.thing.label);
                    var recipe = CreateMaterializeRecipeDef(defName, label, jobName, workAmount, count, this.thing, this.prodCount, this.stuff);
                    DefDatabase<RecipeDef>.Add(recipe);
                }
            }

            public static string GetDefName(ThingDef thing, ThingDef stuff, int prodCount)
            {
                return MaterializeRecipeDefPrefix + thing.defName + (stuff == null ? "" : "_" + stuff.defName) + "_" + prodCount;
            }

            public const string MaterializeRecipeDefPrefix = "NR_MaterialEnergy.Materialize_";
        }

        private const string ToEnergy10DefName = "NR_MaterialEnergy.ToEnergy10";
        private const string ToEnergy100DefName = "NR_MaterialEnergy.ToEnergy100";

        private const string ScanMaterialDefName = "NR_MaterialEnergy.ScanMaterial";

        private float Loss { get => 0.3f; }

        private static readonly HashSet<string> ToEnergyDefNames = new HashSet<string>(new string[] { ToEnergy10DefName, ToEnergy100DefName });

        private static ThingDef energyThingDef;

        private static List<RecipeDef> toEnergyRecipes;

        private static RecipeDef scanMaterialRecipe;

        private List<MaterializeRecipeDefData> materializeRecipeData = new List<MaterializeRecipeDefData>();


        private static void RegisterToEnergyRecipes()
        {
            if (energyThingDef == null)
            {
                energyThingDef = ThingDef.Named("NR_MaterialEnergy_Energy");
            }

            if (toEnergyRecipes == null)
            {
                var toEnergyDefs = new HashSet<ThingDef>(DefDatabase<ThingDef>.AllDefs.Where(t => t.category == ThingCategory.Item).Where(t => GetEnergyAmount(t) > 0.1f));
                toEnergyRecipes = new List<RecipeDef>();
                toEnergyRecipes.Add(CreateToEnergyRecipeDef(ToEnergy10DefName, "NR_MaterialEnergy.RecipeToEnergyLabel".Translate(10), "NR_MaterialEnergy.RecipeToEnergyJobName".Translate(10), 300, 10, toEnergyDefs));
                toEnergyRecipes.Add(CreateToEnergyRecipeDef(ToEnergy100DefName, "NR_MaterialEnergy.RecipeToEnergyLabel".Translate(100), "NR_MaterialEnergy.RecipeToEnergyJobName".Translate(100), 1000, 100, toEnergyDefs));
                toEnergyRecipes.ForEach(r =>
                {
                    var f = r.GetType().GetField("ingredientValueGetterClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    f.SetValue(r, typeof(IngredientValueGetter_Energy));
                    f = r.GetType().GetField("ingredientValueGetterInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    f.SetValue(r, null);
                });
                DefDatabase<RecipeDef>.Add(toEnergyRecipes);
            }
            if (scanMaterialRecipe == null)
            {
                var toEnergyDefs = new HashSet<ThingDef>(DefDatabase<ThingDef>.AllDefs.Where(t => t.category == ThingCategory.Item).Where(t => GetEnergyAmount(t) > 0.1f));
                scanMaterialRecipe = CreateScanMaterialRecipeDef(ScanMaterialDefName, "NR_MaterialEnergy.RecipeScanMaterialLabel".Translate(), "NR_MaterialEnergy.RecipeScanMaterialJobName".Translate(), 5000, toEnergyDefs);
                DefDatabase<RecipeDef>.Add(scanMaterialRecipe);
            }
        }

        public void RemoveMaterializeRecipe(RecipeDef def)
        {
            this.billStack.Bills.RemoveAll(b => b.recipe.defName == def.defName);
            this.materializeRecipeData.RemoveAll(r => r.defName == def.defName);
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

            this.billStack.Bills.Select(b => b as Bill_Production2).ForEach(b => b.complete = this.OnComplete);
        }

        public List<RecipeDef> GetRecipes()
        {
            return new List<RecipeDef>().Append(scanMaterialRecipe).Append(toEnergyRecipes).Append(this.materializeRecipeData.Select(x => x.GetRecipe()).ToList());
        }

        public void OnComplete(Bill_Production2 bill, List<Thing> ingredients)
        {
            if (ScanMaterialDefName == bill.recipe.defName)
            {
                ingredients.ForEach(i =>
                {
                    string defName = MaterializeRecipeDefData.GetDefName(i.def, i.Stuff, 1);
                    if (!this.materializeRecipeData.Any(r => r.defName == defName))
                    {
                        this.materializeRecipeData.Add(new MaterializeRecipeDefData(i.def, i.Stuff, 1, 300));
                        if (i.def.stackLimit >= 10)
                        {
                            this.materializeRecipeData.Add(new MaterializeRecipeDefData(i.def, i.Stuff, 10, 1000));
                        }
                        this.materializeRecipeData.ForEach(x => x.Register(this.Loss));
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
                if (d.defName != "NR_MaterialEnergy_Energy")
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
                if (d.defName != "NR_MaterialEnergy_Energy")
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
    }
}
