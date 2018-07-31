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
    public interface IBill_CompleteNotifier
    {
    }

    public class Bill_ProductionNotifyComplete : Bill_Production
    {
        public Bill_ProductionNotifyComplete() : base()
        {
        }

        public Bill_ProductionNotifyComplete(RecipeDef recipe) : base(recipe)
        {
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            Option(this.billStack?.billGiver as IBillNotificationReceiver).ForEach(r => r.OnComplete(this, ingredients));
        }
    }
}
