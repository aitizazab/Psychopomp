using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>
    /// The Hell capture tools' data reader (docs/HELL-CAPTURE.md; ported from the Skyline trailer capture): a small JSON parser and a mapper onto
    /// <c>[Serializable]</c> classes with public fields (float, int, bool, string, enums by name, float[] / string[], List&lt;T&gt;,
    /// nested classes), the same shape JsonUtility reads. Pure C#, so the same loader runs in the Unity editor and in core-tests.
    /// Keys the class has no field for are reported (a typo in a hand-written shot or preset must not pass silently).
    /// </summary>
    public static class HellCaptureJson
    {
        /// <summary>Reads <paramref name="json"/> into a new <typeparamref name="T"/>; unknown keys are added to
        /// <paramref name="problems"/> (when given) as "unknown key 'path'". Throws FormatException on malformed JSON.</summary>
        public static T FromJson<T>(string json, List<string> problems = null) where T : class, new()
        {
            object tree = Parse(json);
            if (!(tree is Dictionary<string, object> obj)) throw new FormatException("the top level is not an object");
            return (T)Map(obj, typeof(T), "", problems);
        }

        // ------------------------------------------------------------------ mapping

        static object Map(object value, Type type, string path, List<string> problems)
        {
            if (value == null) return type.IsValueType ? Activator.CreateInstance(type) : null;
            if (type == typeof(string)) return value is string s ? s : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (type == typeof(float)) return (float)ToDouble(value, path);
            if (type == typeof(double)) return ToDouble(value, path);
            if (type == typeof(int)) return (int)Math.Round(ToDouble(value, path));
            if (type == typeof(long)) return (long)Math.Round(ToDouble(value, path));
            if (type == typeof(bool))
            {
                if (value is bool b) return b;
                throw new FormatException(path + ": expected true or false");
            }
            if (type.IsEnum)
            {
                string name = (value as string ?? "").Replace("-", "").Replace("_", "");
                foreach (string n in Enum.GetNames(type))
                    if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return Enum.Parse(type, n);
                throw new FormatException(path + ": '" + value + "' is not one of " + string.Join(", ", Enum.GetNames(type)));
            }
            if (type.IsArray)
            {
                if (!(value is List<object> list)) throw new FormatException(path + ": expected an array");
                var et = type.GetElementType();
                var arr = Array.CreateInstance(et, list.Count);
                for (int i = 0; i < list.Count; i++) arr.SetValue(Map(list[i], et, path + "[" + i + "]", problems), i);
                return arr;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (!(value is List<object> list)) throw new FormatException(path + ": expected an array");
                var et = type.GetGenericArguments()[0];
                var result = (IList)Activator.CreateInstance(type);
                for (int i = 0; i < list.Count; i++) result.Add(Map(list[i], et, path + "[" + i + "]", problems));
                return result;
            }
            if (!(value is Dictionary<string, object> obj)) throw new FormatException(path + ": expected an object");
            object target = Activator.CreateInstance(type);
            foreach (var kv in obj)
            {
                var field = type.GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                string p = path.Length == 0 ? kv.Key : path + "." + kv.Key;
                if (field == null)
                {
                    // "//" keys are free comments in hand-written files
                    if (!kv.Key.StartsWith("//", StringComparison.Ordinal) && problems != null) problems.Add("unknown key '" + p + "'");
                    continue;
                }
                field.SetValue(target, Map(kv.Value, field.FieldType, p, problems));
            }
            return target;
        }

        static double ToDouble(object v, string path)
        {
            if (v is double d) return d;
            throw new FormatException(path + ": expected a number");
        }

        // ------------------------------------------------------------------ parsing

        /// <summary>Parses JSON into Dictionary&lt;string, object&gt;, List&lt;object&gt;, double, string, bool and null.</summary>
        public static object Parse(string json)
        {
            if (json == null) throw new FormatException("no text");
            int i = 0;
            object v = Value(json, ref i);
            Skip(json, ref i);
            if (i < json.Length) throw Error(json, i, "text after the value");
            return v;
        }

        static FormatException Error(string s, int i, string what)
        {
            int line = 1;
            for (int k = 0; k < i && k < s.Length; k++) if (s[k] == '\n') line++;
            return new FormatException("JSON line " + line + ": " + what);
        }

        static void Skip(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') { i++; continue; }
                break;
            }
        }

        static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw Error(s, i, "unexpected end");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Word(s, ref i, "true")) return true;
            if (Word(s, ref i, "false")) return false;
            if (Word(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;
            Skip(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "expected a key");
                string key = Str(s, ref i);
                Skip(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "expected ':'");
                i++;
                d[key] = Value(s, ref i);
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++;
            Skip(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return l; }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            throw Error(s, i, "unterminated string");
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start) throw Error(s, i, "unexpected '" + s[i] + "'");
            if (!double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                throw Error(s, start, "bad number");
            return d;
        }
    }
}
