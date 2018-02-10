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
    public static class OverlayDrawHandler_AutomationNet
    {
        private static int lastPowerGridDrawFrame;

        public static bool ShouldDrawNetOverlay
        {
            get
            {
                return lastPowerGridDrawFrame + 1 >= Time.frameCount;
            }
        }

        public static void DrawAitNetOverlayThisFrame()
        {
            lastPowerGridDrawFrame = Time.frameCount;
        }
    }
}
