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
    public class IngredientValueGetter_Energy : IngredientValueGetter
    {
        public override float ValuePerUnitOf(ThingDef t)
        {
            return GetEnergyAmount(t);
        }

        public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
        {
            return "NR_AutoMachineTool.MaterialMachine.RecipeEnergyDescription".Translate(ing.GetBaseCount());
        }

        public override string ExtraDescriptionLine(RecipeDef r)
        {
            return null;
        }
    }
}
