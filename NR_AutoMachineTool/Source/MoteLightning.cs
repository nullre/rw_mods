using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using NR_AutoMachineTool.Utilities;
using static NR_AutoMachineTool.Utilities.Ops;

namespace NR_AutoMachineTool
{
    [StaticConstructorOnStartup]
    public class MoteLightning : MoteDualAttached
    {
        private Material[] LightningMaterials;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            LightningMaterials = new Material[4];
            for (int i = 0; i < LightningMaterials.Length; i++)
            {
                float x = (float)(i % 2) * 0.5f;
                float y = (float)(i / 2) * 0.5f;
                LightningMaterials[i] = new Material(Graphic.MatSingle);
                LightningMaterials[i].shader = ShaderDatabase.Transparent;
                LightningMaterials[i].name = "Thunder_" + i;
                LightningMaterials[i].mainTextureScale = new Vector2(0.5f, 0.5f);
                LightningMaterials[i].mainTextureOffset = new Vector2(x, y);
            }
        }

        public override void Draw()
        {
            base.Draw();
            var mat = this.LightningMaterials[(Find.TickManager.TicksAbs / 4) % 4];
            var a = ((float)(3 - Find.TickManager.TicksAbs % 4) / 3f) * 0.5f;
            if (mat != null)
            {
                Vector3 drawPos = this.DrawPos;
                drawPos.y += 0.01f;
                float alpha = this.Alpha - 0.5f + a;
                if (alpha <= 0f)
                {
                    return;
                }
                Color instanceColor = this.instanceColor;
                instanceColor.a *= alpha;
                if (instanceColor != mat.color)
                {
                    mat.color = instanceColor;
                }

                var vec = (this.link1.LastDrawPos - this.link2.LastDrawPos);
                Matrix4x4 matrix = default(Matrix4x4);
                var quat = Quaternion.FromToRotation(Vector3.forward, vec);
                matrix.SetTRS(drawPos, quat, new Vector3(Mathf.Clamp(vec.magnitude / 5, 1f, 2f), 1f, vec.magnitude));
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }
    }
}
