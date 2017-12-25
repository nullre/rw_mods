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
    public class IngredientValueGetter_Energy : IngredientValueGetter
    {
        public override float ValuePerUnitOf(ThingDef t)
        {
            return GetEnergyAmount(t);
        }

        public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
        {
            return "NR_MaterialEnergy.RecipeEnergyDescription".Translate(ing.GetBaseCount());
        }

        public override string ExtraDescriptionLine(RecipeDef r)
        {
            return null;
        }
    }
}
