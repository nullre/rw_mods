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
    [StaticConstructorOnStartup]
    public static class AutomationNetOverlayMats
    {
        private static readonly Shader OverlayShader;

        public static readonly Graphic LinkedOverlayGraphic;

        public static readonly Graphic CompRepairOverlayGraphic;

        public static readonly Graphic CompExtinguishOverlayGraphic;

        static AutomationNetOverlayMats()
        {
            OverlayShader = ShaderDatabase.MetaOverlay;

            LinkedOverlayGraphic = GraphicDatabase.Get<Graphic_LinkedAutomationNet>("NR_AutoMachineTool/Buildings/AutomationNet/Special/NetOverlay", OverlayShader);
            LinkedOverlayGraphic.MatSingle.renderQueue = 3500;

            CompRepairOverlayGraphic = GraphicDatabase.Get<Graphic_Single>("NR_AutoMachineTool/Buildings/AutomationNet/Special/CompRepairOverlay", OverlayShader);
            CompRepairOverlayGraphic.MatSingle.renderQueue = 3500;

            CompExtinguishOverlayGraphic = GraphicDatabase.Get<Graphic_Single>("NR_AutoMachineTool/Buildings/AutomationNet/Special/CompExtinguishOverlay", OverlayShader);
            CompExtinguishOverlayGraphic.MatSingle.renderQueue = 3500;
        }

        public static Graphic GetOverlayGraphic(Building building)
        {
            if (building as Building_RepairComponent != null)
            {
                return CompRepairOverlayGraphic;
            }
            else if (building as Building_ExtinguishComponent != null)
            {
                return CompExtinguishOverlayGraphic;
            }
            throw new NotImplementedException();
        }
    }
}
