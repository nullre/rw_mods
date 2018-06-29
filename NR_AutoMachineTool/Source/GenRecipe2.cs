using System;
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
    static class GenRecipe2
    {
        public static IEnumerable<Thing> MakeRecipeProducts(RecipeDef recipeDef, IntVec3 position, Map map, Room room, Func<SkillDef, int> skillLevelGetter, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            var result = MakeRecipeProductsInt(recipeDef, position, map, room, skillLevelGetter, ingredients, dominantIngredient, billGiver);
            LoadedModManager.GetMod<Mod_AutoMachineTool>().Hopm.ForEach(m => m.Postfix_MakeRecipeProducts(ref result, recipeDef, 1f, ingredients));
            return result;
        }

        public static IEnumerable<Thing> MakeRecipeProductsInt(RecipeDef recipeDef, IntVec3 position, Map map, Room room, Func<SkillDef, int> skillLevelGetter, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            float efficiency = 1f;
            if (recipeDef.workTableEfficiencyStat != null)
            {
                Building_WorkTable building_WorkTable = billGiver as Building_WorkTable;
                if (building_WorkTable != null)
                {
                    efficiency *= building_WorkTable.GetStatValue(recipeDef.workTableEfficiencyStat, true);
                }
            }
            if (recipeDef.products != null)
            {
                for (int i = 0; i < recipeDef.products.Count; i++)
                {
                    ThingDefCountClass prod = recipeDef.products[i];
                    ThingDef stuffDef;
                    if (prod.thingDef.MadeFromStuff)
                    {
                        stuffDef = dominantIngredient.def;
                    }
                    else
                    {
                        stuffDef = null;
                    }
                    Thing product = ThingMaker.MakeThing(prod.thingDef, stuffDef);
                    product.stackCount = Mathf.CeilToInt((float)prod.count * efficiency);
                    if (dominantIngredient != null)
                    {
                        product.SetColor(dominantIngredient.DrawColor, false);
                    }
                    CompIngredients ingredientsComp = product.TryGetComp<CompIngredients>();
                    if (ingredientsComp != null)
                    {
                        for (int l = 0; l < ingredients.Count; l++)
                        {
                            ingredientsComp.RegisterIngredient(ingredients[l].def);
                        }
                    }
                    CompFoodPoisonable foodPoisonable = product.TryGetComp<CompFoodPoisonable>();
                    if (foodPoisonable != null)
                    {
                        float chance = (room == null) ? RoomStatDefOf.FoodPoisonChance.roomlessScore : room.GetStat(RoomStatDefOf.FoodPoisonChance);
                        if (Rand.Chance(chance))
                        {
                            foodPoisonable.SetPoisoned(FoodPoisonCause.FilthyKitchen);
                        }
                        else
                        {
                            float statValue = 0.003f;
                            if (Rand.Chance(statValue))
                            {
                                foodPoisonable.SetPoisoned(FoodPoisonCause.IncompetentCook);
                            }
                        }
                    }
                    yield return GenRecipe2.PostProcessProduct(product, recipeDef, skillLevelGetter);
                }
            }
            if (recipeDef.specialProducts != null)
            {
                for (int j = 0; j < recipeDef.specialProducts.Count; j++)
                {
                    for (int k = 0; k < ingredients.Count; k++)
                    {
                        Thing ing = ingredients[k];
                        SpecialProductType specialProductType = recipeDef.specialProducts[j];
                        if (specialProductType != SpecialProductType.Butchery)
                        {
                            if (specialProductType == SpecialProductType.Smelted)
                            {
                                foreach (Thing product2 in ing.SmeltProducts(efficiency))
                                {
                                    yield return GenRecipe2.PostProcessProduct(product2, recipeDef, skillLevelGetter);
                                }
                            }
                        }
                        else
                        {
                            foreach (var prod in ing.ButcherProducts(null, efficiency))
                            {
                                yield return GenRecipe2.PostProcessProduct(prod, recipeDef, skillLevelGetter);
                            }
                        }
                    }
                }
            }
        }

        private static Thing PostProcessProduct(Thing product, RecipeDef recipeDef, Func<SkillDef, int> skillLevelGetter)
        {
            CompQuality compQuality = product.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                if (recipeDef.workSkill == null)
                {
                    Log.Error(recipeDef + " needs workSkill because it creates a product with a quality.", false);
                }
                int level = skillLevelGetter(recipeDef.workSkill);
                QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(level, false);
                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
            }
            CompArt compArt = product.TryGetComp<CompArt>();
            if (compArt != null)
            {
                if (compQuality.Quality >= QualityCategory.Excellent)
                {
                    /*
                    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                    {
                        product
                    });
                    */
                }
            }
            if (product.def.Minifiable)
            {
                product = product.MakeMinified();
            }
            return product;
        }
    }
}
