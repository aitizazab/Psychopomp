using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace JamKit.Skyline
{
    /// <summary>
    /// The Rewind's snapshot of a pure object (docs/HELL-ENEMIES.md, "Rewind"): every instance field, captured so that
    /// <see cref="Apply"/> puts the object back exactly. Value fields (numbers, flags, vectors, enums, structs) are copied;
    /// <see cref="Rng"/> fields are captured deeply (the random stream rewinds too, so a rewound bot makes the same choices again);
    /// arrays and lists of values are copied; every other reference (the def, delegates, a door, a nav graph: shared and never
    /// changed by the object) is kept as the same reference. Used for BotBrain, HoverPatrol, EnemyBody, DamageStates, PositionTrail,
    /// BotArrival, the idle work, the Hell attack timelines.
    /// <para>Reflection with the fields cached per type; one snapshot boxes each value field once (fine at a few dozen objects
    /// per fixed step).</para>
    /// </summary>
    public static class StateCopy
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static readonly Dictionary<Type, FieldInfo[]> _fields = new Dictionary<Type, FieldInfo[]>();

        sealed class Captured
        {
            public Type type;
            public object[] values;
        }

        sealed class ArrayCopy { public Array items; }
        sealed class ListCopy { public IList items; public Type listType; }

        static FieldInfo[] Fields(Type t)
        {
            lock (_fields)
            {
                if (_fields.TryGetValue(t, out var f)) return f;
                var list = new List<FieldInfo>();
                for (var k = t; k != null && k != typeof(object); k = k.BaseType)
                    foreach (var fi in k.GetFields(Flags))
                        if (!fi.IsLiteral && !typeof(Delegate).IsAssignableFrom(fi.FieldType)) list.Add(fi);
                f = list.ToArray();
                _fields[t] = f;
                return f;
            }
        }

        /// <summary>A snapshot of <paramref name="obj"/> (null for null).</summary>
        public static object Capture(object obj)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            var fs = Fields(t);
            var vals = new object[fs.Length];
            for (int i = 0; i < fs.Length; i++) vals[i] = CopyValue(fs[i].GetValue(obj));
            return new Captured { type = t, values = vals };
        }

        /// <summary>As <see cref="Capture(object)"/>, refilled into <paramref name="reuse"/> (an earlier snapshot of an object of the
        /// same type, no longer needed) when it is one: the snapshot's own containers (the value array, the deep Rng copies, the
        /// array and list copies) are reused, so a pooled snapshot makes no new containers per fixed step (the value fields are
        /// still boxed: reflection reads them as objects). Returns the snapshot to keep (<paramref name="reuse"/> or a new one).</summary>
        public static object Capture(object obj, object reuse)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            var fs = Fields(t);
            if (!(reuse is Captured c) || c.type != t || c.values == null || c.values.Length != fs.Length) return Capture(obj);
            for (int i = 0; i < fs.Length; i++) c.values[i] = CopyValue(fs[i].GetValue(obj), c.values[i]);
            return c;
        }

        /// <summary>Puts <paramref name="obj"/> back as it was when <paramref name="snapshot"/> was taken of it (or of another object of
        /// the same type). A null or mismatched snapshot does nothing.</summary>
        public static void Apply(object obj, object snapshot)
        {
            if (obj == null || !(snapshot is Captured c) || c.type != obj.GetType()) return;
            var fs = Fields(c.type);
            for (int i = 0; i < fs.Length; i++)
            {
                var f = fs[i];
                object v = c.values[i];
                if (v is Captured deep)
                {
                    var cur = f.GetValue(obj);
                    if (cur != null && cur.GetType() == deep.type) Apply(cur, deep);
                    continue;
                }
                if (v is ArrayCopy ac)
                {
                    var cur = f.GetValue(obj) as Array;
                    if (cur != null && cur.Length == ac.items.Length && cur.GetType() == ac.items.GetType()) Array.Copy(ac.items, cur, cur.Length);
                    else f.SetValue(obj, ac.items.Clone());
                    continue;
                }
                if (v is ListCopy lc)
                {
                    var cur = f.GetValue(obj) as IList;
                    if (cur == null || cur.GetType() != lc.listType)
                    {
                        cur = (IList)Activator.CreateInstance(lc.listType);
                        f.SetValue(obj, cur);
                    }
                    cur.Clear();
                    for (int k = 0; k < lc.items.Count; k++) cur.Add(lc.items[k]);
                    continue;
                }
                // a readonly field never changes after construction: left alone (no write to an init-only field)
                if (f.IsInitOnly) continue;
                f.SetValue(obj, v);
            }
        }

        static object CopyValue(object v, object reuse)
        {
            if (v == null || reuse == null) return CopyValue(v);
            if (v is Rng) return Capture(v, reuse);
            if (v is Array a && reuse is ArrayCopy ac && ac.items.GetType() == a.GetType() && ac.items.Length == a.Length)
            {
                Array.Copy(a, ac.items, a.Length);
                return ac;
            }
            if (v is IList l && reuse is ListCopy lc && lc.listType == v.GetType())
            {
                lc.items.Clear();
                for (int k = 0; k < l.Count; k++) lc.items.Add(l[k]);
                return lc;
            }
            return CopyValue(v);
        }

        static object CopyValue(object v)
        {
            if (v == null) return null;
            var t = v.GetType();
            if (t.IsValueType || v is string) return v;
            if (v is Rng) return Capture(v);
            if (v is Array a)
            {
                var et = t.GetElementType();
                if (et != null && et.IsValueType) return new ArrayCopy { items = (Array)a.Clone() };
                return v;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) && t.GetGenericArguments()[0].IsValueType)
            {
                // a typed copy (List<T>(IEnumerable<T>)): no boxing per element on the hot path
                return new ListCopy { items = (IList)Activator.CreateInstance(t, v), listType = t };
            }
            return v;
        }
    }
}
