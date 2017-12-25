using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.Sound;

namespace NR_MaterialEnergy.Utilities
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

        public static float GetEnergyAmount(ThingDef def)
        {
            return ConvertEnergyAmount(StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, null)));
        }

        public static float GetEnergyAmount(ThingDef def, ThingDef stuffDef)
        {
            return ConvertEnergyAmount(StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, stuffDef)));
        }

        public static float ConvertEnergyAmount(float marketValue)
        {
            return marketValue * 0.1f;
        }
        #endregion
    }
}