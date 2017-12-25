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
    public class Bill_Production2 : Bill_Production
    {
        public Bill_Production2() : base()
        {
        }

        public Bill_Production2(RecipeDef recipe, Action<Bill_Production2, List<Thing>> complete) : base(recipe)
        {
            this.complete = complete;
        }

        public Action<Bill_Production2, List<Thing>> complete;

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);

            this.complete(this, ingredients);
        }
    }
}
