using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    // =====================================================================================================================
    // THE ENDING (docs/HELL-ENDING.md): the decision over the kneeling Vesper, the escape up through the Collapse, and the
    // three endings. Pure (no engine); the Unity side is Scripts/Hell/HellEndingDirector.cs.
    // =====================================================================================================================

    /// <summary>One line of the ending: its key (docs/HELL-SCRIPT.md sections 10, 10.1 and 11; the voice file is
    /// <c>Resources/SkylineAudio/hell-voice/&lt;speaker&gt;/&lt;key&gt;.wav</c>), who says it and the words.</summary>
    public sealed class HellEndingLine
    {
        public readonly string key, speaker, text;

        public HellEndingLine(string key, string speaker, string text)
        {
            this.key = key; this.speaker = speaker; this.text = text;
        }

        /// <summary>A spoken line (it has a voice folder): not the player's own text, not a card.</summary>
        public bool Voiced => speaker == HellEndingLines.God || speaker == HellEndingLines.Vesper;
    }

    /// <summary>
    /// EVERY line of the ending. ALL ARE PLACEHOLDERS (the creator writes all dialogue; keys are docs/HELL-SCRIPT.md's, so the
    /// placeholder voices in hell-voice/god and hell-voice/vesper already match). Change the words here; keep the keys.
    /// </summary>
    public static class HellEndingLines
    {
        /// <summary>Speaker ids: <see cref="God"/> and <see cref="Vesper"/> name their voice folders; <see cref="You"/> is the
        /// player (text only), <see cref="Card"/> an epilogue card (text only).</summary>
        public const string God = "god", Vesper = "vesper", You = "you", Card = "card";

        public static readonly HellEndingLine[] All =
        {
            // ---- 10. the decision
            new HellEndingLine("dec-01", Vesper, "[PLACEHOLDER] Go on. She's waiting for my soul. I'd be waiting too."),
            new HellEndingLine("dec-god-01", God, "[PLACEHOLDER] Well? Chop chop. I have a lunch."),
            new HellEndingLine("dec-god-02", God, "[PLACEHOLDER] It's one shot. You've done a thousand today."),
            new HellEndingLine("dec-you-spare", You, "[PLACEHOLDER] Help me get my sister out. Then find something to stay for."),
            new HellEndingLine("dec-spare-01", Vesper, "[PLACEHOLDER] ...Something to do. It's been so long since anyone asked."),
            new HellEndingLine("dec-spare-02", Vesper, "[PLACEHOLDER] All right. Run, then. I know the way up."),
            new HellEndingLine("dec-kill-01", Vesper, "[PLACEHOLDER] Thank you. Tell her she took her time."),
            // ---- 10.1 the escape
            new HellEndingLine("esc-vesper-01", Vesper, "[PLACEHOLDER] Left! The bridge still remembers me."),
            new HellEndingLine("esc-vesper-02", Vesper, "[PLACEHOLDER] Don't look back. It's the oldest rule down here."),
            new HellEndingLine("esc-god-01", God, "[PLACEHOLDER] Lovely. Now run. The whole place is sulking."),
            // ---- 11. the three endings
            new HellEndingLine("end-kill-01", God, "[PLACEHOLDER] Lucifer is sulking. Delightful. A deal's a deal: one sister, slightly singed."),
            new HellEndingLine("end-kill-02", God, "[PLACEHOLDER] Do come by again. Not soon."),
            new HellEndingLine("end-kill-card", Card, "[PLACEHOLDER] You brought your sister home. Some nights you look for the evening star, and it isn't there."),
            new HellEndingLine("end-favour-01", God, "[PLACEHOLDER] You spared her. You disobeyed me. ...And you did my chores. Ha!"),
            new HellEndingLine("end-favour-02", God, "[PLACEHOLDER] Fine. Keep her. Something new for her to live for. You'll owe me one."),
            new HellEndingLine("end-favour-card", Card, "[PLACEHOLDER] Three walked out of Hell: you, your sister, and the Evening Star, who finally had somewhere to be."),
            new HellEndingLine("end-few-01", God, "[PLACEHOLDER] You spared her, and you did nothing I asked. Someone pays for that, darling. Time pays."),
            new HellEndingLine("end-few-02", God, "[PLACEHOLDER] Your sister keeps her years. I'll take mine out of yours."),
            new HellEndingLine("end-few-card", Card, "[PLACEHOLDER] Your sister went home. You stayed below, paying off the hours. Vesper waits with you. She has the time."),
        };

        /// <summary>The on-screen prompts (UI words, not dialogue). {Use}, {Fire}, {Reload} are the controls' names.</summary>
        public const string PromptApproach = "WALK TO HER TO OFFER YOUR HAND      {Fire}  END IT",
            PromptLower = "LOWER YOUR GUN: HOLD {Reload}, OR HOLD YOUR FIRE",
            PromptOffer = "HOLD {Use}  OFFER YOUR HAND      {Fire}  END IT",
            PromptHolding = "OFFERING YOUR HAND...",
            EscapeLine = "GET OUT", EscapeSoulLine = "GET OUT  (YOUR SISTER'S SOUL: ON THE WAY)";

        static Dictionary<string, HellEndingLine> _byKey;

        public static HellEndingLine Get(string key)
        {
            if (key == null) return null;
            if (_byKey == null)
            {
                var d = new Dictionary<string, HellEndingLine>(StringComparer.Ordinal);
                foreach (var l in All) d[l.key] = l;
                _byKey = d;
            }
            return _byKey.TryGetValue(key, out var found) ? found : null;
        }

        /// <summary>The subtitle's speaker name.</summary>
        public static string Display(string speaker)
        {
            switch (speaker)
            {
                case God: return "BAJA";
                case Vesper: return "VESPER";
                case You: return "YOU";
                default: return "";
            }
        }

        /// <summary>How long a line stays: its clip plus a tail, else reading time (about 15 characters a second).</summary>
        public static float Seconds(HellEndingLine line, float clipSeconds = 0f)
        {
            int n = line != null && line.text != null ? line.text.Length : 0;
            float read = Math.Min(8f, Math.Max(2.4f, 1.2f + n / 15f));
            return clipSeconds > 0f ? Math.Max(2.4f, clipSeconds + 0.5f) : read;
        }
    }

    /// <summary>The ending's sound cues (rows in <see cref="HellSoundTable"/>).</summary>
    public static class HellEndingCues
    {
        public const string Rumble = "hell/ending/rumble", Hold = "hell/ending/hold", Spare = "hell/ending/spare", Kill = "hell/ending/kill",
            Soul = "hell/ending/soul", Step = "hell/ending/step", Escape = "hell/ending/escape", Out = "hell/ending/out", Card = "hell/ending/card";

        public static readonly string[] All = { Rumble, Hold, Spare, Kill, Soul, Step, Escape, Out, Card };
    }

    /// <summary>Which ending, and its card sequence.</summary>
    public static class HellEndingRules
    {
        /// <summary>The ending: a kill is <see cref="HellEndings.Kill"/>; a spare is <see cref="HellEndings.SpareKept"/> when the God of
        /// Death's jobs were mostly done (<see cref="HellJobLedger.Favoured"/>'s rule: at least two-thirds of those offered, and at
        /// least 2), else <see cref="HellEndings.SpareCost"/>.</summary>
        public static string Pick(bool killed, int jobsOffered, int jobsDone) => Pick(killed, jobsOffered, jobsDone, null);

        /// <summary>As <see cref="Pick(bool, int, int)"/>, but with the Vesper chapter's answer (no jobs are offered there; the
        /// title asks whether you ran her errands on the way down): a non-null <paramref name="assumedErrands"/> decides the
        /// favour instead of the counts (true = favoured, false = not).</summary>
        public static string Pick(bool killed, int jobsOffered, int jobsDone, bool? assumedErrands)
        {
            if (killed) return HellEndings.Kill;
            bool favoured = assumedErrands ?? (jobsOffered > 0 && jobsDone >= HellJobLedger.Needed(jobsOffered));
            return favoured ? HellEndings.SpareKept : HellEndings.SpareCost;
        }

        /// <summary>The ending's sequence after the escape: two voiced lines of the God of Death, then the epilogue card.</summary>
        public static string[] Sequence(string endingId)
        {
            switch (endingId)
            {
                case HellEndings.Kill: return new[] { "end-kill-01", "end-kill-02", "end-kill-card" };
                case HellEndings.SpareKept: return new[] { "end-favour-01", "end-favour-02", "end-favour-card" };
                case HellEndings.SpareCost: return new[] { "end-few-01", "end-few-02", "end-few-card" };
                default: return new string[0];
            }
        }

        /// <summary>The lines said once the choice is made, before the escape.</summary>
        public static string[] AfterChoice(bool killed) =>
            killed ? new[] { "dec-kill-01" } : new[] { "dec-you-spare", "dec-spare-01", "dec-spare-02" };

        /// <summary>The escape's first bark (killed: the God of Death; spared: Vesper beside you) and the halfway bark (spared
        /// only: when your sister's soul is picked up).</summary>
        public static string EscapeStart(bool killed) => killed ? "esc-god-01" : "esc-vesper-01";
        public static string EscapeHalfway(bool killed) => killed ? null : "esc-vesper-02";
    }

    // ----------------------------------------------------------------------------------------------------------- the decision

    public enum HellDecisionState { Waiting, Killed, Spared }

    public enum HellDecisionEventKind { Line, Lowered, Raised, HoldStarted, HoldCancelled, Killed, Spared }

    public struct HellDecisionEvent
    {
        public HellDecisionEventKind kind;
        public string line;
    }

    /// <summary>What the player does this frame, as the decision sees it.</summary>
    public struct HellDecisionInput
    {
        public float dt;
        /// <summary>Horizontal distance from the player's feet to Vesper.</summary>
        public float distance;
        /// <summary>Use (offer your hand) held.</summary>
        public bool useHeld;
        /// <summary>The holster input (Reload) held.</summary>
        public bool holsterHeld;
        /// <summary>A shot fired this frame (that did not hit her).</summary>
        public bool fired;
        /// <summary>A shot hit her this frame.</summary>
        public bool hitHer;
    }

    public sealed class HellDecisionTuning
    {
        /// <summary>How close you must stand to offer your hand (m, feet to her, horizontal).</summary>
        public float reach = 2.8f;
        /// <summary>How long Use is held to offer your hand.</summary>
        public float holdSeconds = 1.6f;
        /// <summary>How fast the hold ring empties when you let go (share of the ring a second).</summary>
        public float holdDecayPerSecond = 1.5f;
        /// <summary>The gun counts as lowered this long after your last shot...</summary>
        public float lowerAfterSeconds = 1.5f;
        /// <summary>...or once the holster input (Reload) is held this long.</summary>
        public float holsterHoldSeconds = 0.35f;
        /// <summary>The God of Death presses you at these seconds with no choice made (docs/HELL-SCRIPT.md: every 8 s).</summary>
        public float[] nagSeconds = { 8f, 16f };
        public string[] nagLines = { "dec-god-01", "dec-god-02" };
        /// <summary>Hell's shaking: this share at the start, rising to full over <see cref="quakeFullSeconds"/>. No timer ends the
        /// choice: the shaking only builds.</summary>
        public float quakeStart = 0.2f, quakeFullSeconds = 45f;
    }

    /// <summary>
    /// The decision over the kneeling Vesper (the creator's design, docs/HELL-ENDING.md). <b>Kill:</b> any shot that hits her
    /// ends it (<see cref="Kill"/>, or <c>hitHer</c> in the input). <b>Spare</b> is deliberate: the gun lowered (no shot for
    /// <see cref="HellDecisionTuning.lowerAfterSeconds"/>, or the holster input held), within reach of her, Use held for
    /// <see cref="HellDecisionTuning.holdSeconds"/> (the hold ring). Letting go drains the ring; a shot that misses her empties it
    /// and raises the gun. No timer: the shaking (<see cref="Quake"/>) only builds. Once made, the choice is final: nothing
    /// changes it (the Unity side keeps this out of the Rewind).
    /// </summary>
    public sealed class HellDecision
    {
        public readonly HellDecisionTuning T;
        public HellDecisionState State { get; private set; }
        public float Elapsed { get; private set; }
        /// <summary>The hold ring, 0..1.</summary>
        public float Progress { get; private set; }
        public bool Lowered { get; private set; }
        public bool InReach { get; private set; }
        public bool Holding { get; private set; }
        public bool Decided => State != HellDecisionState.Waiting;
        float _sinceShot, _holster;
        int _nag;

        public HellDecision(HellDecisionTuning tuning = null) { T = tuning ?? new HellDecisionTuning(); }

        /// <summary>She has just knelt: her line, and the clock starts.</summary>
        public void Begin(List<HellDecisionEvent> ev)
        {
            State = HellDecisionState.Waiting;
            Elapsed = 0f;
            Progress = 0f;
            Lowered = false;
            InReach = false;
            Holding = false;
            _sinceShot = 0f;
            _holster = 0f;
            _nag = 0;
            Line(ev, "dec-01");
        }

        /// <summary>Hell's shaking, 0..1: builds while you decide.</summary>
        public float Quake => Math.Min(1f, T.quakeStart + (1f - T.quakeStart) * Elapsed / Math.Max(0.01f, T.quakeFullSeconds));

        public void Tick(in HellDecisionInput i, List<HellDecisionEvent> ev)
        {
            if (Decided) return;
            Elapsed += i.dt;
            while (_nag < T.nagSeconds.Length && Elapsed >= T.nagSeconds[_nag])
            {
                if (_nag < T.nagLines.Length && Progress <= 0f) Line(ev, T.nagLines[_nag]);
                _nag++;
            }
            if (i.hitHer) { Kill(ev); return; }

            if (i.fired) _sinceShot = 0f; else _sinceShot += i.dt;
            _holster = i.holsterHeld ? _holster + i.dt : 0f;
            bool lowered = !i.fired && (_sinceShot >= T.lowerAfterSeconds || _holster >= T.holsterHoldSeconds);
            if (lowered != Lowered)
            {
                Lowered = lowered;
                ev?.Add(new HellDecisionEvent { kind = lowered ? HellDecisionEventKind.Lowered : HellDecisionEventKind.Raised });
            }
            InReach = i.distance <= T.reach;

            bool hold = i.useHeld && InReach && Lowered;
            if (i.fired && Progress > 0f)
            {
                Progress = 0f;
                Holding = false;
                ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.HoldCancelled });
                return;
            }
            if (hold)
            {
                if (!Holding) ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.HoldStarted });
                Holding = true;
                Progress = Math.Min(1f, Progress + i.dt / Math.Max(0.01f, T.holdSeconds));
                if (Progress >= 1f)
                {
                    State = HellDecisionState.Spared;
                    Holding = false;
                    ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.Spared });
                }
                return;
            }
            if (Holding)
            {
                Holding = false;
                ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.HoldCancelled });
            }
            if (Progress > 0f) Progress = Math.Max(0f, Progress - T.holdDecayPerSecond * i.dt);
        }

        /// <summary>A shot hit her: the choice is made. False when it was already made.</summary>
        public bool Kill(List<HellDecisionEvent> ev)
        {
            if (Decided) return false;
            State = HellDecisionState.Killed;
            Holding = false;
            Progress = 0f;
            ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.Killed });
            return true;
        }

        /// <summary>Which prompt shows (<see cref="HellEndingLines"/>'s Prompt* words).</summary>
        public string Prompt
        {
            get
            {
                if (Decided) return "";
                if (!InReach) return HellEndingLines.PromptApproach;
                if (!Lowered) return HellEndingLines.PromptLower;
                return Holding ? HellEndingLines.PromptHolding : HellEndingLines.PromptOffer;
            }
        }

        static void Line(List<HellDecisionEvent> ev, string key) => ev?.Add(new HellDecisionEvent { kind = HellDecisionEventKind.Line, line = key });
    }

    // ------------------------------------------------------------------------------------------------------------- the escape

    /// <summary>
    /// The escape (docs/HELL-ENDING.md): The Collapse of the Black Glass (floor 3's Collapse room, its existing slot and
    /// geometry, re-run from its start; <see cref="HellRoomLayouts"/> is untouched), with your sister's soul hovering halfway
    /// along each lane's running line.
    /// </summary>
    public static class HellEscapeRoute
    {
        /// <summary>The room the escape runs in: floor 3's (0-based 2) Collapse.</summary>
        public const int Floor = 2;
        public const HellRoomKind Kind = HellRoomKind.Collapse;
        /// <summary>Where along each lane's running line the soul hovers (share of the path's length), how near you pass to
        /// take it (swept, so a slide or a Shadow Step through it counts) and its height over the floor.</summary>
        public const float SoulAlong = 0.5f, SoulRadius = 3f, SoulHeight = 1.2f;

        public static HellRoomDef Room() => HellWorld.Room(Floor, Kind);

        /// <summary>Your sister's soul: one on the fast lane, one on the safe lane (take either; both go).</summary>
        public static Vector3[] SoulPoints(HellRoomDef d) => new[] { PointAlong(d.fastPath, SoulAlong), PointAlong(d.safePath, SoulAlong) };

        /// <summary>The point <paramref name="share"/> of the way along a polyline (by length).</summary>
        public static Vector3 PointAlong(List<Vector3> path, float share)
        {
            if (path == null || path.Count == 0) return Vector3.Zero;
            float total = Length(path), want = total * Math.Max(0f, Math.Min(1f, share)), run = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                if (run + seg >= want && seg > 1e-5f) return Vector3.Lerp(path[i - 1], path[i], (want - run) / seg);
                run += seg;
            }
            return path[path.Count - 1];
        }

        public static float Length(List<Vector3> path)
        {
            float t = 0f;
            for (int i = 1; i < path.Count; i++) t += Vector3.Distance(path[i - 1], path[i]);
            return t;
        }

        /// <summary>The room's running direction (entry to exit, flat): the Collapse's base +z in this floor's frame.</summary>
        public static Vector3 Forward(HellRoomDef d)
        {
            var f = d.exit - d.entry;
            f.Y = 0f;
            return f.LengthSquared() > 1e-6f ? Vector3.Normalize(f) : Vector3.UnitZ;
        }
    }

    /// <summary>Your sister's soul in the escape: taken when your path this step passes within the radius of any of its
    /// points (swept, so speed and Shadow Steps can't skip it). Rewindable by value (<see cref="Collected"/>).</summary>
    public sealed class HellSoulPickup
    {
        public readonly Vector3[] Points;
        public readonly float Radius;
        /// <summary>Taken (the Rewind writes it back).</summary>
        public bool Collected;

        public HellSoulPickup(Vector3[] points, float radius = HellEscapeRoute.SoulRadius)
        {
            Points = points ?? new Vector3[0];
            Radius = radius;
        }

        /// <summary>The feet moved from <paramref name="from"/> to <paramref name="to"/>: true on the step the soul is taken.
        /// Height is checked loosely (within 3 m), so a hop over it still counts.</summary>
        public bool Tick(Vector3 from, Vector3 to)
        {
            if (Collected) return false;
            foreach (var p in Points)
            {
                if (Math.Abs(to.Y - p.Y) > 3f && Math.Abs(from.Y - p.Y) > 3f) continue;
                if (FlatDistanceToSegment(p, from, to) <= Radius)
                {
                    Collected = true;
                    return true;
                }
            }
            return false;
        }

        public void Reset() => Collected = false;

        public static float FlatDistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ap = new Vector2(p.X - a.X, p.Z - a.Z);
            var ab = new Vector2(b.X - a.X, b.Z - a.Z);
            float len2 = ab.LengthSquared();
            float t = len2 > 1e-8f ? Math.Max(0f, Math.Min(1f, Vector2.Dot(ap, ab) / len2)) : 0f;
            return (ap - ab * t).Length();
        }
    }

    public sealed class HellFollowerTuning
    {
        /// <summary>Where she runs: this far to your left and this far behind.</summary>
        public float side = 1.8f, behind = 0.8f;
        /// <summary>Her top speed (m/s): above the Collapse's fastest front (11.1 on floor 3) and a slide.</summary>
        public float maxSpeed = 16f;
        /// <summary>Farther than this from her place (you Shadow Stepped, fell, sprinted off): she Shadow Steps to it.</summary>
        public float catchUp = 9f;
        /// <summary>Every this many seconds while you run (at least <see cref="blinkMinSpeed"/>) she Shadow Steps ahead of you
        /// (<see cref="blinkAhead"/> m) and waits up to <see cref="waitSeconds"/> for you to pass.</summary>
        public float blinkEvery = 7f, blinkMinSpeed = 4f, blinkAhead = 7f, waitSeconds = 1.4f;
    }

    /// <summary>
    /// Vesper running beside you in the spared escape (docs/HELL-ENDING.md): she glides to a place at your left shoulder at up
    /// to <see cref="HellFollowerTuning.maxSpeed"/>, Shadow Steps to it when she falls behind, and now and then Shadow Steps
    /// ahead of you and waits there. She never touches the floor rules (she is Vesper: the lava and the falling floor don't
    /// take her). The state is a struct: the Rewind copies it.
    /// </summary>
    public sealed class HellFollower
    {
        public struct State
        {
            public Vector3 position, facing;
            public float sinceBlink, wait;
        }

        public readonly HellFollowerTuning T;
        public State S;

        public HellFollower(Vector3 start, Vector3 facing, HellFollowerTuning tuning = null)
        {
            T = tuning ?? new HellFollowerTuning();
            S = new State { position = start, facing = Flat(facing, Vector3.UnitZ) };
        }

        /// <summary>Her place beside a player at <paramref name="player"/> running along <paramref name="dir"/>.</summary>
        public Vector3 Place(Vector3 player, Vector3 dir)
        {
            var left = new Vector3(-dir.Z, 0f, dir.X);
            return player + left * T.side - dir * T.behind;
        }

        /// <summary>One step. True when she Shadow Stepped (<paramref name="blinkFrom"/> is where she left).</summary>
        public bool Tick(float dt, Vector3 player, Vector3 playerVelocity, out Vector3 blinkFrom)
        {
            blinkFrom = S.position;
            var flatV = new Vector3(playerVelocity.X, 0f, playerVelocity.Z);
            float speed = flatV.Length();
            if (speed > 1f) S.facing = flatV / speed;
            var dir = S.facing;
            var place = Place(player, dir);
            S.sinceBlink += dt;

            if (Vector3.Distance(S.position, place) > T.catchUp)
            {
                S.position = place;
                S.sinceBlink = 0f;
                S.wait = 0f;
                return true;
            }
            if (S.sinceBlink >= T.blinkEvery && speed >= T.blinkMinSpeed && S.wait <= 0f)
            {
                S.position = Place(player + dir * T.blinkAhead, dir);
                S.sinceBlink = 0f;
                S.wait = T.waitSeconds;
                return true;
            }
            if (S.wait > 0f)
            {
                S.wait -= dt;
                // she waits ahead until you draw level (or the wait runs out)
                if (Vector3.Dot(S.position - player, dir) <= 0.5f) S.wait = 0f;
                S.position = new Vector3(S.position.X, player.Y, S.position.Z);
                return false;
            }
            var to = place - S.position;
            float d = to.Length();
            float step = T.maxSpeed * dt;
            S.position = d <= step ? place : S.position + to * (step / d);
            return false;
        }

        static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.Y = 0f;
            return v.LengthSquared() > 1e-6f ? Vector3.Normalize(v) : fallback;
        }
    }
}
