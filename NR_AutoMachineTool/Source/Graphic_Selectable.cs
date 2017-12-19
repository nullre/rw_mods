using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;

using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    class Graphic_Selectable : Graphic_Collection
    {

        public override Material MatSingle
        {
            get
            {
                return this.subGraphics[0].MatSingle;
            }
        }

        public Graphic Get(int index)
        {
            return this.subGraphics[index];
        }

        public override bool ShouldDrawRotated
        {
            get
            {
                return true;
            }
        }
    }
}
