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

        public Type targetCellResolverType;
        private ITargetCellResolver targetCellResolver;
        public ITargetCellResolver TargetCellResolver
        {
            get
            {
                if (targetCellResolverType == null)
                {
                    return null;
                }
                if (targetCellResolver == null)
                {
                    this.targetCellResolver = (ITargetCellResolver)Activator.CreateInstance(targetCellResolverType);
                    this.targetCellResolver.Parent = this;
                }
                return this.targetCellResolver;
            }
        }

        public Type outputCellResolverType;
        private IOutputCellResolver outputCellResolver;
        public IOutputCellResolver OutputCellResolver
        {
            get
            {
                if (outputCellResolverType == null)
                {
                    return null;
                }
                if (outputCellResolver == null)
                {
                    this.outputCellResolver = (IOutputCellResolver)Activator.CreateInstance(outputCellResolverType);
                    this.outputCellResolver.Parent = this;
                }
                return this.outputCellResolver;
            }
        }

        public Type inputCellResolverType;
        private IInputCellResolver inputCellResolver;
        public IInputCellResolver InputCellResolver
        {
            get
            {
                if (inputCellResolverType == null)
                {
                    return null;
                }
                if (inputCellResolver == null)
                {
                    this.inputCellResolver = (IInputCellResolver)Activator.CreateInstance(inputCellResolverType);
                    this.inputCellResolver.Parent = this;
                }
                return this.inputCellResolver;
            }
        }
    }

    public interface IInputCellResolver
    {
        IntVec3 InputCell(IntVec3 cell, Map map, Rot4 rot);
        IEnumerable<IntVec3> InputZoneCells(IntVec3 cell, Map map, Rot4 rot);
        ModExtension_AutoMachineTool Parent { get; set; }
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
    }

    public interface IOutputCellResolver
    {
        IntVec3 OutputCell(IntVec3 cell, Map map, Rot4 rot);
        IEnumerable<IntVec3> OutputZoneCells(IntVec3 cell, Map map, Rot4 rot);
        ModExtension_AutoMachineTool Parent { get; set; }
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
    }

    public class OutputCellResolver : IOutputCellResolver
    {
        public ModExtension_AutoMachineTool Parent { get; set; }

        public virtual IntVec3 OutputCell(IntVec3 cell, Map map, Rot4 rot)
        {
            return cell + rot.Opposite.FacingCell;
        }

        public virtual IEnumerable<IntVec3> OutputZoneCells(IntVec3 cell, Map map, Rot4 rot)
        {
            return this.OutputCell(cell, map, rot).SlotGroupCells(map);
        }

        public virtual Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return cellPattern.ToColor();
        }
    }

    public interface ITargetCellResolver
    {
        IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range);
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
        ModExtension_AutoMachineTool Parent { get; set; }
        int MaxPowerForRange { get; }
        int MinPowerForRange { get; }
        int GetRange(float power);
    }

    public static class ITargetCellResolverExtension
    {
        public static int MaxRange(this ITargetCellResolver r)
        {
            return r.GetRange(r.MaxPowerForRange);
        }

        public static int MinRange(this ITargetCellResolver r)
        {
            return r.GetRange(r.MinPowerForRange);
        }
    }

    public abstract class BaseTargetCellResolver : ITargetCellResolver
    {
        protected ModSetting_AutoMachineTool Setting { get => LoadedModManager.GetMod<Mod_AutoMachineTool>().Setting; }

        public abstract int MinPowerForRange { get; }
        public abstract int MaxPowerForRange { get; }
        public ModExtension_AutoMachineTool Parent { get; set; }

        public virtual int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 3;
        }

        public virtual Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return cellPattern.ToColor();
        }

        public abstract IEnumerable<IntVec3> GetRangeCells(IntVec3 pos, Map map, Rot4 rot, int range);
    }

    public enum CellPattern {
        BlurprintMin,
        BlurprintMax,
        Instance,
        OtherInstance,
        OutputCell,
        OutputZone,
        InputCell,
        InputZone,
    }

    public static class CellPatternExtensions
    {
        public static Color ToColor(this CellPattern pat)
        {
            switch (pat)
            {
                case CellPattern.BlurprintMin:
                    return Color.white;
                case CellPattern.BlurprintMax:
                    return Color.white.A(0.5f);
                case CellPattern.Instance:
                    return Color.white;
                case CellPattern.OtherInstance:
                    return Color.white.A(0.35f);
                case CellPattern.OutputCell:
                    return Color.blue;
                case CellPattern.OutputZone:
                    return Color.blue.A(0.5f);
                case CellPattern.InputCell:
                    return Color.magenta;
                case CellPattern.InputZone:
                    return Color.magenta.A(0.5f);
            }
            return Color.white;
        }
    }

}
