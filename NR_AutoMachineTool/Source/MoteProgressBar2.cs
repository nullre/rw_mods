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
    public class MoteProgressBar2 : MoteProgressBar
    {
        public override void Draw()
        {
            if (progressGetter != null)
            {
                this.progress = Mathf.Clamp01(this.progressGetter());
            }
            base.Draw();
        }

        public Func<float> progressGetter;
    }
}
