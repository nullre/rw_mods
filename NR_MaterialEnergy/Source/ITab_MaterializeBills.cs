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
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
    }

    public class ITab_MaterializeBills : ITab
    {
        private float viewHeight = 1000f;

        private Vector2 scrollPosition = default(Vector2);

        private Bill mouseoverBill;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        protected Building_MaterialMahcine Machine
        {
            get
            {
                return (Building_MaterialMahcine)base.SelThing;
            }
        }

        public ITab_MaterializeBills()
        {
            this.size = ITab_MaterializeBills.WinSize;
            this.labelKey = "TabBills";
            this.tutorTag = "Bills";
        }

        protected override void FillTab()
        {
            var recipes = this.Machine.GetRecipes();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            Rect rect = new Rect(0f, 0f, ITab_MaterializeBills.WinSize.x, ITab_MaterializeBills.WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i].AvailableNow)
                    {
                        RecipeDef recipe = recipes[i];
                        list.Add(new FloatMenuOption(recipe.LabelCap, delegate
                        {
                            if (!this.Machine.Map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                            {
                                Bill.CreateNoPawnsWithSkillDialog(recipe);
                            }
                            Bill bill = new Bill_Production2(recipe, this.Machine.OnComplete);
                            this.Machine.billStack.AddBill(bill);
                            if (recipe.conceptLearned != null)
                            {
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                            }
                        }, MenuOptionPriority.Default, null, null, 58f, (rect2) =>
                        {
                            if (recipe.defName.StartsWith(Building_MaterialMahcine.MaterializeRecipeDefData.MaterializeRecipeDefPrefix))
                            {
                                if (Widgets.ButtonImage(new Rect(rect2.x + 34f, rect2.y + (rect2.height - 24f), 24f, 24f), Resources.DeleteX))
                                {
                                    this.Machine.RemoveMaterializeRecipe(recipe);
                                    return true;
                                }
                            }
                            return Widgets.InfoCardButton(rect2.x + 5f, rect2.y + (rect2.height - 24f) / 2f, recipe);
                        }, null));
                    }
                }
                if (!list.Any<FloatMenuOption>())
                {
                    list.Add(new FloatMenuOption("NoneBrackets".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                return list;
            };
            this.mouseoverBill = this.Machine.billStack.DoListing(rect, recipeOptionsMaker, ref this.scrollPosition, ref this.viewHeight);
        }

        public override void TabUpdate()
        {
            if (this.mouseoverBill != null)
            {
                this.mouseoverBill.TryDrawIngredientSearchRadiusOnMap(this.Machine.Position);
                this.mouseoverBill = null;
            }
        }
    }
}
