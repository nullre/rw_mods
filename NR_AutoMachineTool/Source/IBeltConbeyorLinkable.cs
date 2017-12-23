using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace NR_AutoMachineTool
{
    interface IBeltConbeyorLinkable
    {
        bool CanLink(IBeltConbeyorLinkable linkable, bool underground);
        void Link(IBeltConbeyorLinkable linkable);
        void Unlink(IBeltConbeyorLinkable linkable);
        Rot4 Rotation { get; }
        IntVec3 Position { get; }
        bool ReceivableNow(bool underground);
        bool ReceiveThing(Thing thing);
        bool IsUnderground { get; }
    }
}
