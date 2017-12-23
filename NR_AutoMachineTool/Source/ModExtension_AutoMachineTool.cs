using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    public class ModExtension_AutoMachineTool : DefModExtension
    {
        public int tier = 1;
        public bool underground = false;
        public bool toUnderground = false;
    }
}
