using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_AutoMachineTool.Utilities
{
    public class Option<T>
    {
        private readonly T value;

        private bool hasValue = false;

        public Option(T val)
        {
            if (val != null)
            {
                this.value = val;
                hasValue = true;
            }
        }

        public T Value
        {
            get { return value; }
        }

        public bool HasValue
        {
            get { return hasValue; }
        }

        public Option()
        {
        }

        public Option<TO> SelectMany<TO>(Func<T, Option<TO>> func)
        {
            return hasValue ? func(value) : new Nothing<TO>();
        }

        public Option<TO> Select<TO>(Func<T, TO> func)
        {
            return hasValue ? new Option<TO>(func(value)) : new Nothing<TO>();
        }

        public Option<T> Where(Predicate<T> pre)
        {
            return hasValue ? pre(this.value) ? this : new Nothing<T>() : new Nothing<T>();
        }

        public void ForEach(Action<T> act)
        {
            if (hasValue) act(this.value);
        }

        public List<T> ToList()
        {
            return this.hasValue ? new List<T>(new T[] { this.value }) : new List<T>();
        }

        public T GetOrDefault(T defaultValue)
        {
            return this.hasValue ? this.value : defaultValue;
        }

        public T GetOrDefault(Func<T> creator)
        {
            return this.hasValue ? this.value : creator();
        }

        public Func<Func<T, R>, R> Fold<R>(R defaultValue)
        {
            return this.hasValue ? (f) => f(this.value) : (Func<Func<T, R>, R>)((_) => defaultValue);
        }

        public Func<Func<T, R>, R> Fold<R>(Func<R> craetor)
        {
            return this.hasValue ? (f) => f(this.value) : (Func<Func<T, R>, R>)((_) => craetor());
        }

        public Option<T> Peek(Action<T> act)
        {
            if (hasValue)
            {
                act(this.value);
            }
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Option<T> && this.hasValue == ((Option<T>)obj).hasValue &&
                (!this.hasValue || this.value.Equals(((Option<T>)obj).value));
        }

        public override int GetHashCode()
        {
            return this.hasValue ? this.value.GetHashCode() : this.hasValue.GetHashCode();
        }

        public override string ToString()
        {
            return this.Fold("Option<Nothing>")(v => "Option<" + v.ToString() + ">");
        }
    }

    public class Nothing<T> : Option<T>
    {
        public Nothing() : base()
        {
        }
    }

    public class Just<T> : Option<T>
    {
        public Just(T value) : base(value)
        {
        }
    }
}
