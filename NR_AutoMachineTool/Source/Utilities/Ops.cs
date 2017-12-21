using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.Sound;

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

        public static Tuple<T1, T2> Tuple<T1, T2>(T1 v1, T2 v2)
        {
            return new Tuple<T1, T2>(v1, v2);
        }

        public static Tuple<T1, T2, T3> Tuple<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
        {
            return new Tuple<T1, T2, T3>(v1, v2, v3);
        }


        #region for rimworld

//        public static void L(object obj) { Log.Message(obj == null ? "null" : obj.ToString()); }

        public static bool PlaceItem(Thing t, IntVec3 cell, bool forbid, Map map)
        {
            Action<Thing> effect = (item) =>
            {
                item.def.soundDrop.PlayOneShot(item);
                MoteMaker.ThrowDustPuff(item.Position, map, 0.5f);
            };

            if (cell.GetThingList(map).Where(ti => ti.def.category == ThingCategory.Item).Count() == 0)
            {
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            var cells = cell.ZoneCells(map);
            cells.SelectMany(c => c.GetThingList(map)).Where(i => i.def == t.def).ForEach(i => i.TryAbsorbStack(t, true));
            if (t.stackCount == 0)
            {
                effect(t);
                return true;
            }
            var o = cells.Where(c => c.IsValidStorageFor(map, t))
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

        public static List<IntVec3> ZoneCells(this IntVec3 c, Map map)
        {
            return Option(c.GetZone(map) as RimWorld.Zone_Stockpile).Select(z => z.cells).GetOrDefault(new List<IntVec3>().Append(c));
        }

        public static Bill_Production CopyTo(this Bill_Production bill, Bill_Production copy)
        {
            copy.allowedSkillRange = bill.allowedSkillRange;
            copy.billStack = bill.billStack;
            copy.deleted = bill.deleted;
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.lastIngredientSearchFailTicks = bill.lastIngredientSearchFailTicks;
            copy.paused = bill.paused;
            copy.pauseWhenSatisfied = bill.pauseWhenSatisfied;
            copy.recipe = bill.recipe;
            copy.repeatCount = bill.repeatCount;
            copy.repeatMode = bill.repeatMode;
            copy.storeMode = bill.storeMode;
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
        #endregion
    }
}