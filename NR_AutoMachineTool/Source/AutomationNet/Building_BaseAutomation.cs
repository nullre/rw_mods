﻿using System;
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
    public abstract class Building_BaseAutomation<T> : Building_Base<T> where T : Thing
    {
        public Building_BaseAutomation()
        {
            this.placeFirstAbsorb = true;
        }
    }
}
