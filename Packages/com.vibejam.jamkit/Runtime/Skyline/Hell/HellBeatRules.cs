using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>What can set a story beat off (docs/HELL-BEATS.md). The Unity side reports each as a <see cref="HellBeatEvent"/>.</summary>
    public enum HellBeatTrigger
    {
        /// <summary>A room was entered (floor, room index, kind).</summary>
        RoomEntered,
        /// <summary>A room's objective finished (floor, room, kind).</summary>
        ObjectiveDone,
        /// <summary>The objective's progress reached <see cref="HellBeatWhen.atLeast"/> (0..1; e.g. 0.5 of the Sigils).</summary>
        ObjectiveProgress,
        /// <summary>The Soul Lantern was picked up.</summary>
        LanternTaken,
        /// <summary>Seconds in the current room (<see cref="HellBeatWhen.seconds"/>, the room's clock).</summary>
        RoomTimer,
        /// <summary>The player stood in a dark zone (the Shadow Step's darkness).</summary>
        DarkZoneEntered,
        /// <summary>A floor's shaft was entered (after its fourth room).</summary>
        ShaftEntered,
        /// <summary>A job was done (<see cref="HellBeatEvent.count"/> = jobs done so far).</summary>
        JobDone,
        /// <summary>The final boss was requested (straight before Vesper's fight).</summary>
        BeforeBoss,
        /// <summary>The run finished (a build without a boss).</summary>
        RunFinished,
    }

    /// <summary>Something that happened, reported by the Unity side. Unused fields stay at their defaults.</summary>
    public struct HellBeatEvent
    {
        public HellBeatTrigger trigger;
        /// <summary>0-based floor and room index (-1 when not in a room).</summary>
        public int floor, room;
        /// <summary>The room's kind (<see cref="HellRoomKind"/>'s name), or null.</summary>
        public string kind;
        /// <summary>Seconds (<see cref="HellBeatTrigger.RoomTimer"/>), progress 0..1 (<see cref="HellBeatTrigger.ObjectiveProgress"/>).</summary>
        public float value;
        /// <summary>A count (<see cref="HellBeatTrigger.JobDone"/>: jobs done so far).</summary>
        public int count;

        public static HellBeatEvent Of(HellBeatTrigger t, int floor = -1, int room = -1, string kind = null, float value = 0f, int count = 0) =>
            new HellBeatEvent { trigger = t, floor = floor, room = room, kind = kind, value = value, count = count };
    }

    /// <summary>One way a beat can start. Every filter left at its default matches anything.</summary>
    [Serializable]
    public sealed class HellBeatWhen
    {
        public HellBeatTrigger trigger;
        /// <summary>0-based floor (-1 any), room index in the floor (-1 any), room kind ("" any).</summary>
        public int floor = -1, room = -1;
        public string kind = "";
        /// <summary><see cref="HellBeatTrigger.RoomTimer"/>: at least this many seconds in the room.</summary>
        public float seconds;
        /// <summary><see cref="HellBeatTrigger.ObjectiveProgress"/>: at least this share done (0..1).</summary>
        public float atLeast;
        /// <summary><see cref="HellBeatTrigger.JobDone"/>: at least this many jobs done (0 = the first).</summary>
        public int count;
        /// <summary>Only if no room of this kind was entered on this trigger's floor before (the fallback for "the Soul
        /// Lantern wasn't dealt"). "" = no condition.</summary>
        public string unlessKindSeen = "";

        public HellBeatWhen() { }
        public HellBeatWhen(HellBeatTrigger t) { trigger = t; }
    }

    /// <summary>The in-world staging of a beat (on top of the voice and subtitles).</summary>
    public enum HellBeatStaging
    {
        None,
        /// <summary>A figure seen at a distance ahead of the player (<see cref="HellBeatDef.figureDistance"/> m), for
        /// <see cref="HellBeatDef.figureSeconds"/>; it fades when the beat ends.</summary>
        FigureAhead,
    }

    /// <summary>A story beat: when (any of <see cref="when"/>, so later entries are fallbacks), what is said (keys into
    /// <see cref="HellLines"/>), its staging, and whether it holds the player (control paused, world slowed, letterbox; never
    /// over <see cref="HellBeatRules.MaxHoldSeconds"/>). Plays once per run.</summary>
    [Serializable]
    public sealed class HellBeatDef
    {
        public string id = "";
        public List<HellBeatWhen> when = new List<HellBeatWhen>();
        public string[] lines = new string[0];
        /// <summary>Only after this beat has played ("" = no order).</summary>
        public string after = "";
        public HellBeatStaging staging;
        public float figureDistance = 28f, figureSeconds = 6f;
        public bool letterbox;
        /// <summary>Seconds of held control (0 = play goes on: the voice and subtitles only).</summary>
        public float holdSeconds;
        /// <summary>How slow the world runs while held (a HellTime request; 1 = not slowed).</summary>
        public float holdScale = 0.15f;
    }

    /// <summary>
    /// The beats' trigger rules (pure, data-driven). <see cref="Report"/> an event: the first beat (in the set's order)
    /// that has not played, whose <see cref="HellBeatDef.after"/> has played, and one of whose <see cref="HellBeatWhen"/>
    /// matches, is returned and marked played; otherwise null. <see cref="RunStarted"/> forgets what played. The beats
    /// themselves are data (<see cref="HellBeatSets"/>): swapping a set is a data edit.
    /// </summary>
    public sealed class HellBeatRules
    {
        public const float MaxHoldSeconds = 4f;

        readonly List<HellBeatDef> _defs;
        readonly HashSet<string> _played = new HashSet<string>(StringComparer.Ordinal);
        // "floor:kind" for every room entered this run (for unlessKindSeen)
        readonly HashSet<string> _seenKinds = new HashSet<string>(StringComparer.Ordinal);

        public HellBeatRules(IEnumerable<HellBeatDef> defs) { _defs = new List<HellBeatDef>(defs); }
        public HellBeatRules() : this(HellBeatSets.Active) { }

        public IReadOnlyList<HellBeatDef> Defs => _defs;
        public bool Played(string id) => _played.Contains(id);

        /// <summary>The hold actually used: never negative, never over <see cref="MaxHoldSeconds"/>.</summary>
        public static float HoldFor(HellBeatDef d) => d == null ? 0f : Math.Max(0f, Math.Min(MaxHoldSeconds, d.holdSeconds));

        public void RunStarted()
        {
            _played.Clear();
            _seenKinds.Clear();
        }

        /// <summary>Something happened: the beat to play now, or null.</summary>
        public HellBeatDef Report(HellBeatEvent e)
        {
            HellBeatDef hit = null;
            foreach (var d in _defs)
            {
                if (_played.Contains(d.id)) continue;
                if (!string.IsNullOrEmpty(d.after) && !_played.Contains(d.after)) continue;
                foreach (var w in d.when)
                    if (Matches(w, e)) { hit = d; break; }
                if (hit != null) break;
            }
            // the room counts as seen after this event's own check (a fallback in the lantern room itself never fires)
            if (e.trigger == HellBeatTrigger.RoomEntered && e.kind != null) _seenKinds.Add(e.floor + ":" + e.kind);
            if (hit != null) _played.Add(hit.id);
            return hit;
        }

        bool Matches(HellBeatWhen w, HellBeatEvent e)
        {
            if (w.trigger != e.trigger) return false;
            if (w.floor >= 0 && w.floor != e.floor) return false;
            if (w.room >= 0 && w.room != e.room) return false;
            if (!string.IsNullOrEmpty(w.kind) && w.kind != e.kind) return false;
            if (!string.IsNullOrEmpty(w.unlessKindSeen))
            {
                if (_seenKinds.Contains(e.floor + ":" + w.unlessKindSeen)) return false;
                if (e.trigger == HellBeatTrigger.RoomEntered && e.kind == w.unlessKindSeen) return false;
            }
            switch (w.trigger)
            {
                case HellBeatTrigger.RoomTimer: return e.value >= w.seconds;
                case HellBeatTrigger.ObjectiveProgress: return e.value >= w.atLeast;
                case HellBeatTrigger.JobDone: return e.count >= Math.Max(1, w.count);
                default: return true;
            }
        }
    }
}
