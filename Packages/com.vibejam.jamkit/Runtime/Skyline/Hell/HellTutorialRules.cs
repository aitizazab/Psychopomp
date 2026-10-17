using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>One first-time prompt from the God of Death: her spoken line, the on-screen hint (input tokens allowed) and,
    /// when the player does the thing, a short reaction. Keys: <c>tut.&lt;id&gt;.1</c>, <c>.hint</c>, <c>.done</c>.</summary>
    public sealed class HellTutorialDef
    {
        public string id = "";
        /// <summary>Lower shows first when several wait.</summary>
        public int priority;
        /// <summary>The hint stays this long (unscaled seconds) unless the player does it first.</summary>
        public float hintSeconds = 9f;
        /// <summary>A request not shown within this long is dropped (the moment passed; it may be asked again later).</summary>
        public float staleSeconds = 6f;
        /// <summary>True: the prompt ends when the player does it (<see cref="HellTutorialQueue.Complete"/>), and doing it
        /// before it shows means it never needs to.</summary>
        public bool asksToDo;

        public string LineKey => "tut." + id + ".1";
        public string HintKey => "tut." + id + ".hint";
        public string DoneKey => "tut." + id + ".done";
    }

    /// <summary>The prompts (docs/HELL-BEATS.md has the trigger of each).</summary>
    public static class HellTutorials
    {
        public const string ShadowStep = "shadowstep", Rewind = "rewind", Dilation = "dilation", Lava = "lava",
            Collapse = "collapse", Paths = "paths", SkillPick = "skillpick", Job = "job";

        /// <summary>The objective prompt's id for a room kind ("obj.SixSigils").</summary>
        public static string Objective(string kind) => "obj." + kind;

        public static readonly HellTutorialDef[] All = Build();

        static HellTutorialDef[] Build()
        {
            var l = new List<HellTutorialDef>
            {
                new HellTutorialDef { id = Rewind, priority = 0, hintSeconds = 6f, staleSeconds = 3f, asksToDo = true },
                new HellTutorialDef { id = SkillPick, priority = 1, hintSeconds = 12f, staleSeconds = 4f },
                new HellTutorialDef { id = Lava, priority = 2, hintSeconds = 6f, staleSeconds = 4f },
                new HellTutorialDef { id = Collapse, priority = 2, hintSeconds = 5f, staleSeconds = 2.5f },
                new HellTutorialDef { id = ShadowStep, priority = 3, hintSeconds = 14f, staleSeconds = 20f, asksToDo = true },
                new HellTutorialDef { id = Dilation, priority = 4, hintSeconds = 8f, staleSeconds = 3f, asksToDo = true },
                new HellTutorialDef { id = Job, priority = 5, hintSeconds = 7f, staleSeconds = 10f },
                new HellTutorialDef { id = Paths, priority = 7, hintSeconds = 8f, staleSeconds = 40f },
            };
            foreach (var k in HellRoomLayouts.Kinds)
                l.Add(new HellTutorialDef { id = Objective(k.ToString()), priority = 6, hintSeconds = 8f, staleSeconds = 30f });
            return l.ToArray();
        }

        public static HellTutorialDef Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return null;
        }

        /// <summary>Which dark zone the first room's Shadow Step prompt points at: the nearest to the entry whose centre is
        /// at least <paramref name="minDistance"/> away (a step worth taking) and within <paramref name="maxDistance"/>;
        /// failing that, the nearest of all. -1 when there are none.</summary>
        public static int PickDarkZone(Vector3 entry, IReadOnlyList<CoverBox> zones, float minDistance = 4f, float maxDistance = 18f)
        {
            int best = -1, any = -1;
            float bestD = float.MaxValue, anyD = float.MaxValue;
            for (int i = 0; i < zones.Count; i++)
            {
                var c = (zones[i].min + zones[i].max) * 0.5f;
                float d = Vector2.Distance(new Vector2(c.X, c.Z), new Vector2(entry.X, entry.Z));
                if (d < anyD) { anyD = d; any = i; }
                if (d >= minDistance && d <= maxDistance && d < bestD) { bestD = d; best = i; }
            }
            return best >= 0 ? best : any;
        }
    }

    /// <summary>
    /// The prompts' queue (pure; unscaled time in seconds). Each prompt shows once: once shown (or done by the player before
    /// it showed) it is <see cref="Seen"/>, and the seen set is saved between runs (<see cref="SaveSeen"/> /
    /// <see cref="LoadSeen"/>) until <see cref="ReplayAll"/>. One shows at a time; others wait, best priority first, then
    /// oldest; a request goes stale after its <see cref="HellTutorialDef.staleSeconds"/>. Nothing starts while
    /// <c>blocked</c> (a story beat, a pause), and two prompts are at least <see cref="GapSeconds"/> apart.
    /// </summary>
    public sealed class HellTutorialQueue
    {
        public const float GapSeconds = 1.5f;

        sealed class Pending { public HellTutorialDef def; public float at, notBefore; }

        readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        readonly List<Pending> _pending = new List<Pending>();
        HellTutorialDef _current;
        float _shownAt, _endedAt = float.MinValue / 2f;

        /// <summary>False: nothing is requested or shown (the player turned the tutorials off).</summary>
        public bool Enabled = true;

        public HellTutorialDef Current => _current;
        public bool Seen(string id) => _seen.Contains(id);
        public int PendingCount => _pending.Count;
        public bool IsPending(string id)
        {
            foreach (var p in _pending) if (p.def.id == id) return true;
            return false;
        }

        /// <summary>Asks for a prompt (shown no earlier than <paramref name="delay"/> seconds from now). False when it is
        /// unknown, seen, showing, already waiting, or the tutorials are off.</summary>
        public bool Request(string id, float now, float delay = 0f)
        {
            if (!Enabled) return false;
            var def = HellTutorials.Get(id);
            if (def == null || _seen.Contains(id) || (_current != null && _current.id == id) || IsPending(id)) return false;
            _pending.Add(new Pending { def = def, at = now, notBefore = now + Math.Max(0f, delay) });
            return true;
        }

        /// <summary>The player did it. Returns true when the prompt for it is showing now (the caller says the reaction
        /// line and takes the hint down). A waiting or never-asked "do it" prompt is marked seen: they know already.</summary>
        public bool Complete(string id, float now)
        {
            var def = HellTutorials.Get(id);
            if (def == null) return false;
            if (_current != null && _current.id == id)
            {
                _current = null;
                _endedAt = now;
                return true;
            }
            if (def.asksToDo)
            {
                _pending.RemoveAll(p => p.def.id == id);
                _seen.Add(id);
            }
            return false;
        }

        /// <summary>Advances the queue. Returns the prompt that starts now (it is marked seen), or null. A showing prompt
        /// ends after its hint time.</summary>
        public HellTutorialDef Tick(float now, bool blocked)
        {
            if (_current != null && now - _shownAt >= _current.hintSeconds)
            {
                _current = null;
                _endedAt = now;
            }
            for (int i = _pending.Count - 1; i >= 0; i--)
                if (now - _pending[i].notBefore > _pending[i].def.staleSeconds) _pending.RemoveAt(i);
            if (!Enabled) { _pending.Clear(); return null; }
            if (blocked || _current != null || now - _endedAt < GapSeconds) return null;
            Pending best = null;
            foreach (var p in _pending)
            {
                if (now < p.notBefore) continue;
                if (best == null || p.def.priority < best.def.priority || (p.def.priority == best.def.priority && p.at < best.at)) best = p;
            }
            if (best == null) return null;
            _pending.Remove(best);
            _current = best.def;
            _shownAt = now;
            _seen.Add(best.def.id);
            return best.def;
        }

        /// <summary>Takes the showing prompt down now (a restart, a beat that must speak).</summary>
        public void Dismiss(float now)
        {
            if (_current == null) return;
            _current = null;
            _endedAt = now;
        }

        /// <summary>Forget what was seen: every prompt shows again (the settings toggle).</summary>
        public void ReplayAll()
        {
            _seen.Clear();
            _pending.Clear();
        }

        /// <summary>The seen ids, comma-separated (for PlayerPrefs).</summary>
        public string SaveSeen()
        {
            var ids = new List<string>(_seen);
            ids.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            foreach (var id in ids) { if (sb.Length > 0) sb.Append(','); sb.Append(id); }
            return sb.ToString();
        }

        public void LoadSeen(string saved)
        {
            _seen.Clear();
            if (string.IsNullOrEmpty(saved)) return;
            foreach (var id in saved.Split(','))
                if (id.Length > 0) _seen.Add(id.Trim());
        }
    }
}
