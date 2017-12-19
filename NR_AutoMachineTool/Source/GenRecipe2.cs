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
        public static IEnumerable<Thing> MakeRecipeProducts(RecipeDef recipeDef, IntVec3 position, Map map, Room room, Func<SkillDef, int> skillLevelGetter, List<Thing> ingredients, Thing dominantIngredient)
        {
            float efficiency = 1f;

            if (recipeDef.products != null)
            {
                for (int i = 0; i < recipeDef.products.Count; i++)
                {
                    ThingCountClass prod = recipeDef.products[i];
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
                        float num = 0.003f;
                        // Room room = worker.GetRoom(RegionType.Set_Passable);
                        if (room != null)
                        {
                            num *= room.GetStat(RoomStatDefOf.FoodPoisonChanceFactor);
                        }
                        if (Rand.Value < num)
                        {
                            foodPoisonable.PoisonPercent = 1f;
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
                            Corpse corpse = ing as Corpse;
                            if (corpse != null)
                            {
                                foreach(var prod in corpse.InnerPawn.ButcherProducts(null, efficiency))
                                {
                                    yield return GenRecipe2.PostProcessProduct(prod, recipeDef, skillLevelGetter);
                                }
                                if (corpse.InnerPawn.RaceProps.BloodDef != null)
                                {
                                    FilthMaker.MakeFilth(position, map, corpse.InnerPawn.RaceProps.BloodDef, corpse.InnerPawn.LabelIndefinite(), 1);
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
        }

        private static Thing PostProcessProduct(Thing product, RecipeDef recipeDef, Func<SkillDef, int> skillLevelGetter)
        {
            CompQuality compQuality = product.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                if (recipeDef.workSkill == null)
                {
                    Log.Error(recipeDef + " needs workSkill because it creates a product with a quality.");
                }
                int level = skillLevelGetter(recipeDef.workSkill);
                QualityCategory qualityCategory = QualityUtility.RandomCreationQuality(level);
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
