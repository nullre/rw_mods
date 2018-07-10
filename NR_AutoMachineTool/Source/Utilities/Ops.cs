using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace NR_AutoMachineTool.Utilities
{
    public static class Ops
    {
        public static Option<T> Option<T>(T value)
        {
            return new Option<T>(value);
        }

        public static Nothing<T> Nothing<T>()
        {
            return new Nothing<T>();
        }

        public static Just<T> Just<T>(T value)
        {
            return new Just<T>(value);
        }

        public static Option<T> FirstOption<T>(this IEnumerable<T> e)
        {
            var en = e.GetEnumerator();
            if (en.MoveNext())
            {
                return new Just<T>(en.Current);
            }
            else
            {
                return new Nothing<T>();
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> selector)
        {
            return source.SelectMany(e => selector(e).ToList());
        }

        public static List<T> Append<T>(this List<T> lhs, List<T> rhs)
        {
            lhs.AddRange(rhs);
            return lhs;
        }

        public static List<T> Append<T>(this List<T> lhs, T rhs)
        {
            lhs.Add(rhs);
            return lhs;
        }

        public static List<T> Ins<T>(this List<T> lhs, int index, T rhs)
        {
            lhs.Insert(index, rhs);
            return lhs;
        }

        public static List<T> Head<T>(this List<T> lhs, T rhs)
        {
            lhs.Insert(0, rhs);
            return lhs;
        }

        public static List<T> Del<T>(this List<T> lhs, T rhs)
        {
            lhs.Remove(rhs);
            return lhs;
        }

        public static Option<T> ElementAtOption<T>(this List<T> list, int index)
        {
            if(index >= list.Count)
            {
                return new Nothing<T>();
            }
            return Option(list[index]);
        }

        public static bool EqualValues<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            var l = lhs.ToList();
            var r = rhs.ToList();
            if (l.Count == r.Count)
            {
                for (int i = 0; i < l.Count; i++)
                {
                    if (!l[i].Equals(r[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence) action(item);
        }

        public static IEnumerable<T> Peek<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence)
            {
                action(item);
                yield return item;
            }
        }

        public static Option<T> FindOption<T>(this List<T> sequence, Predicate<T> predicate)
        {
            var i = sequence.FindIndex(predicate);
            if(i == -1)
            {
                return new Nothing<T>();
            }
            else
            {
                return new Just<T>(sequence[i]);
            }
        }

        public static Option<V> GetOption<K, V>(this Dictionary<K, V> dict, K key)
        {
            V val;
            if(dict.TryGetValue(key, out val))
            {
                return Just(val);
            }
            return Nothing<V>();
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        public static Tuple<T1, T2> Tuple<T1, T2>(T1 v1, T2 v2)
        {
            return new Tuple<T1, T2>(v1, v2);
        }

        public static Tuple<T1, T2, T3> Tuple<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
        {
            return new Tuple<T1, T2, T3>(v1, v2, v3);
        }

        #region for rimworld

        public static void L(object obj) { Log.Message(obj == null ? "null" : obj.ToString()); }

        public static bool PlaceItem(Thing t, IntVec3 cell, bool forbid, Map map, bool firstAbsorbStack = false)
        {
            Action<Thing> effect = (item) =>
            {
                item.def.soundDrop.PlayOneShot(item);
                MoteMaker.ThrowDustPuff(item.Position, map, 0.5f);
            };

            Func<bool> absorb = () =>
            {
                cell.SlotGroupCells(map).SelectMany(c => c.GetThingList(map)).Where(i => i.def == t.def).ForEach(i => i.TryAbsorbStack(t, true));
                if (t.stackCount == 0)
                {
                    effect(t);
                    return true;
                }
                return false;
            };

            if (firstAbsorbStack)
            {
                if (absorb())
                    return true;
            }
            if (cell.GetThingList(map).Where(ti => ti.def.category == ThingCategory.Item).Count() == 0)
            {
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            if (!firstAbsorbStack)
            {
                if (absorb())
                    return true;
            }
            var o = cell.SlotGroupCells(map).Where(c => c.IsValidStorageFor(map, t))
                .Where(c => c.GetThingList(map).Where(b => b.def.category == ThingCategory.Building).All(b => !(b is Building_BeltConveyor)))
                .FirstOption();
            if (o.HasValue)
            {
                GenPlace.TryPlaceThing(t, o.Value, map, ThingPlaceMode.Near);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            return false;
        }

        public static void Noop()
        {
        }

        public static List<IntVec3> SlotGroupCells(this IntVec3 c, Map map)
        {
            return Option(map.haulDestinationManager.SlotGroupAt(c)).Select(g => g.CellsList).GetOrDefault(new List<IntVec3>().Append(c));
        }

        public static Bill_Production CopyTo(this Bill_Production bill, Bill_Production copy)
        {
            copy.allowedSkillRange = bill.allowedSkillRange;
            copy.billStack = bill.billStack;
            copy.deleted = bill.deleted;
            copy.hpRange = bill.hpRange;
            copy.includeEquipped = bill.includeEquipped;
            copy.includeFromZone = bill.includeFromZone;
            copy.includeTainted = bill.includeTainted;
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.lastIngredientSearchFailTicks = bill.lastIngredientSearchFailTicks;
            copy.limitToAllowedStuff = bill.limitToAllowedStuff;
            copy.paused = bill.paused;
            copy.pauseWhenSatisfied = bill.pauseWhenSatisfied;
            copy.pawnRestriction = bill.pawnRestriction;
            copy.qualityRange = bill.qualityRange;
            copy.recipe = bill.recipe;
            copy.repeatCount = bill.repeatCount;
            copy.repeatMode = bill.repeatMode;
            copy.SetStoreMode(bill.GetStoreMode());
            copy.suspended = bill.suspended;
            copy.targetCount = bill.targetCount;
            copy.unpauseWhenYouHave = bill.unpauseWhenYouHave;

            return copy;
        }

        public static List<IntVec3> FacingRect(IntVec3 pos, Rot4 dir, int range)
        {
            var rightAngle = dir;
            rightAngle.Rotate(RotationDirection.Clockwise);
            return Enumerable.Range(1, range * 2 + 1).SelectMany(a => Enumerable.Range(-range, range * 2 + 1).Select(c => rightAngle.FacingCell.ToVector3() * c).Select(v => v + (a * dir.FacingCell.ToVector3()))).Select(x => pos + x.ToIntVec3()).ToList();
        }

        public static Rot4 RotateAsNew(this Rot4 rot, RotationDirection dir)
        {
            var n = rot;
            n.Rotate(dir);
            return n;
        }

        public static Option<IPlantToGrowSettable> GetPlantable(this IntVec3 pos, Map map)
        {
            return Option(pos.GetZone(map) as IPlantToGrowSettable)
                .Fold(() => pos.GetThingList(map).Where(t => t.def.category == ThingCategory.Building).SelectMany(t => Option(t as IPlantToGrowSettable)).FirstOption())
                (x => Option(x));
        }

        public static Option<Pawn> GetGatherable(this IntVec3 pos, Map map)
        {
            return pos.GetThingList(map).Where(t => t.def.category == ThingCategory.Pawn)
                .SelectMany(t => Option(t as Pawn))
                .Where(p => p.Faction == Faction.OfPlayer)
                .Where(p => p.TryGetComp<CompHasGatherableBodyResource>() != null)
                .FirstOption();
        }

        public static bool IsAdult(this Pawn p)
        {
            return p.ageTracker.CurLifeStageIndex >= 2;
        }
        #endregion

        public static Func<T, TValue> GenerateGetFieldDelegate<T, TValue>(FieldInfo field)
        {
            var d = new DynamicMethod("getter", typeof(TValue), new Type[] { typeof(T) }, true);
            var g = d.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldfld, field);
            g.Emit(OpCodes.Ret);

            return (Func<T, TValue>)d.CreateDelegate(typeof(Func<T, TValue>));
        }

        public static Action<T, TValue> GenerateSetFieldDelegate<T, TValue>(FieldInfo field)
        {
            var d = new DynamicMethod("setter", typeof(void), new Type[] { typeof(T), typeof(TValue) }, true);
            var g = d.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Stfld, field);
            g.Emit(OpCodes.Ret);

            return (Action<T, TValue>)d.CreateDelegate(typeof(Action<T, TValue>));
        }

        public static Func<T, TResult> GenerateMeshodDelegate<T, TResult>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Func<T, TResult>>(callExp, new List<ParameterExpression>().Append(instanceParam).Append(args)).Compile();
        }

        public static Func<T, TParam1, TResult> GenerateMeshodDelegate<T, TParam1, TResult>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { Expression.Parameter(typeof(TParam1), "param1") };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Func<T, TParam1, TResult>>(callExp, new List<ParameterExpression>().Append(instanceParam).Append(args)).Compile();
        }

        public static Action<T> GenerateVoidMeshodDelegate<T>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Action<T>>(callExp, new List<ParameterExpression>().Append(instanceParam)).Compile();
        }

        public static Action<T, TParam1> GenerateVoidMeshodDelegate<T, TParam1>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { Expression.Parameter(typeof(TParam1), "param1") };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Action<T, TParam1>>(callExp, new List<ParameterExpression>().Append(instanceParam)).Compile();
        }
    }
}