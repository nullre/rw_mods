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

        public Graphic Get(string path)
        {
            if (path == null)
            {
                return this.subGraphics[0];
            }
            if (!pathDic.ContainsKey(path))
            {
                pathDic[path] = this.subGraphics.Where(x => x.path == path).First();
            }
            return this.pathDic[path];
        }

        private Dictionary<string, Graphic> pathDic = new Dictionary<string, Graphic>();

        public override bool ShouldDrawRotated
        {
            get
            {
                return true;
            }
        }
    }
}
