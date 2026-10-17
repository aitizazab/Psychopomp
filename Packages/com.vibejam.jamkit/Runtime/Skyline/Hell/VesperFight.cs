using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>What Vesper is doing right now (docs/HELL-BOSS.md, "Phases").</summary>
    public enum VesperAction
    {
        /// <summary>Before <see cref="VesperFight.Begin"/>: nothing ticks.</summary>
        Waiting,
        /// <summary>Standing in her darkness between attacks.</summary>
        Idle,
        /// <summary>Her hand flares violet: a volley of shadow bolts is coming.</summary>
        VolleyTell,
        Volley,
        /// <summary>She dissolves into smoke; her destination is marked. Then she is there.</summary>
        BlinkTell,
        /// <summary>Phase 2: she raises her lantern-heart; imps come out of the dark at the end.</summary>
        SummonTell,
        /// <summary>Phase 2: her own Rewind winds up (her heart turns cold cyan, a clock ring spins). Break it by shooting the heart.</summary>
        RewindTell,
        /// <summary>Her Rewind broke: she reels, open (more damage), no attacks.</summary>
        Stagger,
        /// <summary>Phase 3: gone into the dark (cannot be hit).</summary>
        Hidden,
        /// <summary>Phase 3: a violet flare marks where she will step out, near you.</summary>
        HuntTell,
        /// <summary>Phase 3: she is out, her arm drawn back; the cut lands when it ends.</summary>
        StrikeWindup,
        /// <summary>Phase 3: after the cut, her heart blazes and she is open.</summary>
        Exposed,
        /// <summary>She pulled a killing blow short and holds back while you get up.</summary>
        Mercy,
        /// <summary>Between phases: she speaks, cannot be hit.</summary>
        Shift,
        /// <summary>Beaten: on her knees.</summary>
        Kneel,
        /// <summary>The fight is over.</summary>
        Done,
    }

    public enum VesperEventKind
    {
        PhaseBegan, Line, VolleyTell, Bolt, BlinkTell, Blinked, SummonTell, Summoned, RewindTell, Rewound, RewindBroken,
        NightfallBegan, HuntTell, Appeared, StrikeWindup, StrikeHit, StrikeMissed, Exposed, Mercy, Shift, Kneel, Defeated, Restarted,
    }

    /// <summary>One thing the fight did this tick, for the Unity side to show and to act on (fire a bolt, spawn imps, move her).</summary>
    public struct VesperEvent
    {
        public VesperEventKind kind;
        /// <summary>Bolt: the bolt's place in the fan (0 = straight at you). Blink: the perch. PhaseBegan: the phase (1-3). Summoned: how many.</summary>
        public int index;
        /// <summary>Where it happens (her new place, the marked spot).</summary>
        public Vector3 position;
        /// <summary>Damage (StrikeHit), health given back (Rewound), health she will take back (RewindTell).</summary>
        public float amount;
        /// <summary>Line: the line's id in <see cref="VesperLines"/>.</summary>
        public string line;
    }

    /// <summary>Every number of the fight (docs/HELL-BOSS.md, "Tuning"), tuned for 2-4 minutes on a first try with the Writ.</summary>
    [Serializable]
    public sealed class VesperTuning
    {
        [Serializable]
        public sealed class PhaseTuning
        {
            public float idleSeconds = 1.1f, volleyTellSeconds = 0.55f;
            public int volleyBolts = 3, volleysPerPerch = 2;
            public float volleyGap = 0.3f;
            /// <summary>Degrees between the bolts of a fan (0: all at you).</summary>
            public float fanDegrees = 0f;
        }

        public float maxHealth = 1600f, phase2At = 1100f, phase3At = 600f;
        /// <summary>The heart's multiplier when the gun gives none.</summary>
        public float heartMultiplier = 2f;
        public float staggerMultiplier = 1.5f, exposedMultiplier = 1.5f;
        public float shiftSeconds = 3f, kneelSeconds = 3f;

        public PhaseTuning duel = new PhaseTuning();
        public PhaseTuning undo = new PhaseTuning { idleSeconds = 1.2f, volleyBolts = 4, fanDegrees = 6f, volleysPerPerch = 2 };
        public PhaseTuning night = new PhaseTuning { idleSeconds = 0.5f, volleyBolts = 5, fanDegrees = 9f, volleysPerPerch = 1, volleyGap = 0.18f };

        public float boltDamage = 14f, boltSpeed = 36f;
        public float blinkTellSeconds = 0.9f;
        /// <summary>She leaves a perch early after taking this much there.</summary>
        public float pressureBlink = 160f;
        /// <summary>A perch she blinks to is at least this far from you when one is.</summary>
        public float blinkMinDistance = 10f;

        // phase 2
        public float summonTellSeconds = 1.2f, resummonSeconds = 16f;
        public int summonCount = 2, impCap = 3;
        /// <summary>Her Rewind: how far back, its wind-up, the least damage worth undoing, its cooldown, and the heart damage
        /// during the wind-up that breaks it.</summary>
        public float rewindSeconds = 4f, rewindTellSeconds = 1.6f, rewindMinUndo = 70f, rewindCooldown = 10f, rewindBreak = 80f;
        public float staggerSeconds = 3f;

        // phase 3
        public float hiddenSeconds = 1.5f, huntTellSeconds = 1.0f, huntDistance = 6f, huntSpreadDegrees = 45f;
        /// <summary>Her cut: a lunge along the line she drew when she stepped out (locked: sidestep it or step away), this long and
        /// this wide either side; she ends the lunge this far along it, open.</summary>
        public float strikeWindupSeconds = 0.6f, strikeReach = 8f, strikeHalfWidth = 1.4f, strikeDamage = 32f, lungeEnd = 4.5f;
        public float exposedSeconds = 1.8f;
        /// <summary>Every this many hunts she volleys from a perch instead.</summary>
        public int volleyEveryHunts = 3;

        // she never quite goes for the kill
        /// <summary>A blow of hers that would kill you is pulled short (you are left standing) at most once per this many seconds.</summary>
        public float mercyCooldown = 30f, mercySeconds = 2.5f;
        /// <summary>The health she leaves you on a pulled blow.</summary>
        public float mercySliver = 10f;
        /// <summary>While your health (plating + chassis) is under this, she waits longer between attacks (x <see cref="lowHealthPace"/>).</summary>
        public float lowHealth = 50f, lowHealthPace = 1.6f;

        /// <summary>Her health history for her Rewind: one sample every <see cref="historyStep"/> s, <see cref="historySamples"/> kept.</summary>
        public float historyStep = 0.25f;
        public int historySamples = 32;

        public PhaseTuning For(int phase) => phase <= 1 ? duel : phase == 2 ? undo : night;
    }

    /// <summary>Her lines. Every one is a PLACEHOLDER for the creator to rewrite (docs/HELL-BOSS.md, "Lines").</summary>
    public static class VesperLines
    {
        public const string Intro = "intro", Phase2 = "phase2", Phase3 = "phase3", RewindBroken = "rewind-broken", Rewound = "rewound",
            Mercy = "mercy", Mercy2 = "mercy2", Defeat = "defeat", Restart = "restart";

        /// <summary>The text of a line (PLACEHOLDER: the creator rewrites them).</summary>
        public static string Text(string id)
        {
            switch (id)
            {
                case Intro: return "[PLACEHOLDER] So she sent you. I made a deal once too. Let me see what yours bought you.";
                case Phase2: return "[PLACEHOLDER] Time? I have had nothing BUT time. Watch.";
                case Phase3: return "[PLACEHOLDER] He grew bored of the light. So did I. Come and find me.";
                case Rewound: return "[PLACEHOLDER] Again. I have all of it to spare.";
                case RewindBroken: return "[PLACEHOLDER] ...You broke it. No one has done that in a long time.";
                case Mercy: return "[PLACEHOLDER] Get up.";
                case Mercy2: return "[PLACEHOLDER] Not like this. Get up.";
                case Defeat: return "[PLACEHOLDER] ...There. Something new, at last.";
                case Restart: return "[PLACEHOLDER] Again, then.";
                default: return "";
            }
        }
    }

    /// <summary>
    /// The fight with Vesper, the Evening Star, as a pure state machine (docs/HELL-BOSS.md). She fights with the player's
    /// own powers mirrored: she Shadow Steps between dark perches (phase 1, the duel), winds up her own Rewind that undoes the
    /// damage you dealt in the last seconds unless you break it by shooting her heart (phase 2, with imps), and in phase 3
    /// (Nightfall) the arena goes dark and she hunts you, stepping out of the dark beside you. She never quite goes for the
    /// kill: a blow of hers that would kill you is pulled short once per <see cref="VesperTuning.mercyCooldown"/>, and she
    /// waits longer while you are low.
    /// <para>The Unity side (<c>HellVesperFight</c>) feeds <see cref="Tick"/> the player's state and turns the events into
    /// bolts, imps, movement and effects. Everything is in <see cref="S"/> plus the history array, so a snapshot
    /// (<see cref="SaveTo"/> / <see cref="LoadFrom"/>) puts the fight back exactly (the global Rewind).</para>
    /// </summary>
    public sealed class VesperFight
    {
        /// <summary>The whole state but the health history (a value: copying it is a snapshot).</summary>
        public struct State
        {
            public int phase;
            public VesperAction action;
            /// <summary>Seconds left in the action (or to the next bolt in a volley).</summary>
            public float timer;
            public float clock, health;
            public int perch, volleysLeft, boltsLeft, boltIndex, hunts;
            public float damageAtPerch, rewindCooldown, rewindTo, breakDamage, resummonTimer, mercyCooldown;
            public int mercies;
            public Vector3 position, marked;
            /// <summary>Phase 3: the flat direction of her cut, locked when she steps out.</summary>
            public Vector3 aim;
            public int historyHead, historyCount;
            public float historyTimer;
            public ulong random;
            public bool defeatedRaised;
        }

        /// <summary>A snapshot (the global Rewind keeps one per fixed step: reuse them).</summary>
        public sealed class Snapshot
        {
            public State s;
            public float[] history;
        }

        public struct Input
        {
            public float dt;
            public Vector3 player, playerForward;
            /// <summary>Plating + chassis.</summary>
            public float playerHealth;
            public int impsAlive;
        }

        public readonly VesperTuning T;
        public State S;
        float[] _history;
        Vector3[] _perches = { Vector3.Zero };
        CoverBox _bounds;
        bool _hasBounds;

        public VesperFight(VesperTuning tuning = null, ulong seed = 0x5EB7E4UL)
        {
            T = tuning ?? new VesperTuning();
            _history = new float[Math.Max(2, T.historySamples)];
            S.random = seed != 0 ? seed : 0x5EB7E4UL;
            S.action = VesperAction.Waiting;
            S.health = T.maxHealth;
        }

        /// <summary>Her dark perches (feet positions: phase 1 and 2 she stands in these; perch 0 is where she starts) and the
        /// arena's bounds (phase 3's hunt spots stay inside them, 1.5 m in).</summary>
        public void SetArena(IReadOnlyList<Vector3> perches, CoverBox bounds)
        {
            _perches = new Vector3[Math.Max(1, perches.Count)];
            for (int i = 0; i < perches.Count; i++) _perches[i] = perches[i];
            _bounds = bounds;
            _hasBounds = true;
        }

        public int PerchCount => _perches.Length;
        public Vector3 Perch(int i) => _perches[Math.Max(0, Math.Min(_perches.Length - 1, i))];
        public bool Running => S.action != VesperAction.Waiting && S.action != VesperAction.Done;
        public bool Defeated => S.action == VesperAction.Kneel || S.action == VesperAction.Done;
        /// <summary>The health of the phase's floor (she cannot drop under it until the phase changes).</summary>
        public float PhaseFloor => S.phase <= 1 ? T.phase2At : S.phase == 2 ? T.phase3At : 0f;
        /// <summary>The health she started the phase on (her Rewind never gives back more).</summary>
        public float PhaseCap => S.phase <= 1 ? T.maxHealth : S.phase == 2 ? T.phase2At : T.phase3At;

        /// <summary>Can a shot hurt her now? Not while she is in the dark (hidden, the hunt's flare), between phases, or beaten.</summary>
        public bool Vulnerable
        {
            get
            {
                switch (S.action)
                {
                    case VesperAction.Waiting: case VesperAction.Hidden: case VesperAction.HuntTell: case VesperAction.Shift:
                    case VesperAction.Kneel: case VesperAction.Done: return false;
                    default: return true;
                }
            }
        }

        /// <summary>Is she visible (standing somewhere)? Hidden and the hunt's flare: no.</summary>
        public bool Visible => S.action != VesperAction.Hidden && S.action != VesperAction.HuntTell && S.action != VesperAction.Waiting;

        // ------------------------------------------------------------------ start and restart

        /// <summary>The fight starts: phase 1, full health, on perch 0.</summary>
        public void Begin(List<VesperEvent> events)
        {
            var seed = S.random;
            S = default(State);
            S.random = seed;
            S.health = T.maxHealth;
            S.perch = 0;
            S.position = Perch(0);
            ClearHistory();
            EnterPhase(1, events);
            Line(VesperLines.Intro, events);
        }

        /// <summary>The player died: the fight starts over (from phase 1, or from the current phase's start when
        /// <paramref name="keepPhase"/>).</summary>
        public void Restart(bool keepPhase, List<VesperEvent> events)
        {
            int phase = keepPhase ? Math.Max(1, S.phase) : 1;
            int mercies = S.mercies;
            var seed = S.random;
            S = default(State);
            S.random = seed;
            S.mercies = mercies;
            S.perch = 0;
            S.position = Perch(0);
            S.health = phase == 1 ? T.maxHealth : phase == 2 ? T.phase2At : T.phase3At;
            ClearHistory();
            Emit(events, VesperEventKind.Restarted, phase, S.position, 0f);
            EnterPhase(phase, events);
            Line(VesperLines.Restart, events);
        }

        void EnterPhase(int phase, List<VesperEvent> events)
        {
            S.phase = phase;
            S.damageAtPerch = 0f;
            S.rewindCooldown = phase == 2 ? T.rewindCooldown * 0.5f : 0f;
            S.hunts = 0;
            Emit(events, VesperEventKind.PhaseBegan, phase, S.position, 0f);
            if (phase == 1)
            {
                S.volleysLeft = T.duel.volleysPerPerch;
                To(VesperAction.Idle, T.duel.idleSeconds);
            }
            else if (phase == 2)
            {
                S.volleysLeft = T.undo.volleysPerPerch;
                StartSummon(events);
            }
            else
            {
                S.volleysLeft = 0;
                S.perch = -1;
                Emit(events, VesperEventKind.NightfallBegan, 3, S.position, 0f);
                To(VesperAction.Hidden, T.hiddenSeconds);
            }
        }

        // ------------------------------------------------------------------ the step

        public void Tick(in Input input, List<VesperEvent> events)
        {
            if (S.action == VesperAction.Waiting || S.action == VesperAction.Done) return;
            float dt = input.dt;
            if (!(dt > 0f)) return;
            S.clock += dt;
            if (S.rewindCooldown > 0f) S.rewindCooldown = Math.Max(0f, S.rewindCooldown - dt);
            if (S.resummonTimer > 0f) S.resummonTimer = Math.Max(0f, S.resummonTimer - dt);
            if (S.mercyCooldown > 0f) S.mercyCooldown = Math.Max(0f, S.mercyCooldown - dt);
            Record(dt);

            if (S.action == VesperAction.Volley)
            {
                S.timer -= dt;
                while (S.timer <= 0f && S.boltsLeft > 0)
                {
                    FireBolt(events);
                    if (S.boltsLeft > 0) S.timer += T.For(S.phase).volleyGap;
                }
                if (S.boltsLeft <= 0)
                {
                    S.volleysLeft--;
                    To(VesperAction.Idle, Pace(T.For(S.phase).idleSeconds, input));
                }
                return;
            }

            S.timer -= dt;
            if (S.timer > 0f) return;
            switch (S.action)
            {
                case VesperAction.Idle: Decide(input, events); break;
                case VesperAction.VolleyTell:
                    var pt = T.For(S.phase);
                    S.boltsLeft = Math.Max(1, pt.volleyBolts);
                    S.boltIndex = 0;
                    S.action = VesperAction.Volley;
                    S.timer = 0f;
                    FireBolt(events);
                    S.timer = pt.volleyGap;
                    if (S.boltsLeft <= 0)
                    {
                        S.volleysLeft--;
                        To(VesperAction.Idle, Pace(pt.idleSeconds, input));
                    }
                    break;
                case VesperAction.BlinkTell:
                    S.perch = PerchIndexOf(S.marked);
                    S.position = S.marked;
                    S.damageAtPerch = 0f;
                    S.volleysLeft = T.For(S.phase).volleysPerPerch;
                    Emit(events, VesperEventKind.Blinked, S.perch, S.position, 0f);
                    To(VesperAction.Idle, Pace(0.45f, input));
                    break;
                case VesperAction.SummonTell:
                    int n = Math.Max(0, Math.Min(T.summonCount, T.impCap - input.impsAlive));
                    Emit(events, VesperEventKind.Summoned, n, S.position, 0f);
                    S.resummonTimer = T.resummonSeconds;
                    To(VesperAction.Idle, Pace(T.undo.idleSeconds, input));
                    break;
                case VesperAction.RewindTell:
                    float back = Math.Max(0f, Math.Min(S.rewindTo, PhaseCap) - S.health);
                    S.health += back;
                    S.rewindCooldown = T.rewindCooldown;
                    ClearHistory();
                    Emit(events, VesperEventKind.Rewound, 0, S.position, back);
                    Line(VesperLines.Rewound, events);
                    To(VesperAction.Idle, Pace(T.undo.idleSeconds, input));
                    break;
                case VesperAction.Stagger:
                case VesperAction.Mercy:
                case VesperAction.Exposed:
                    To(VesperAction.Idle, Pace(S.phase == 3 ? T.night.idleSeconds : T.For(S.phase).idleSeconds, input));
                    break;
                case VesperAction.Hidden:
                    S.marked = HuntSpot(input);
                    Emit(events, VesperEventKind.HuntTell, 0, S.marked, 0f);
                    To(VesperAction.HuntTell, T.huntTellSeconds);
                    break;
                case VesperAction.HuntTell:
                    S.position = S.marked;
                    var toPlayer = input.player - S.position;
                    toPlayer.Y = 0f;
                    S.aim = toPlayer.LengthSquared() > 1e-6f ? Vector3.Normalize(toPlayer) : new Vector3(0, 0, 1);
                    Emit(events, VesperEventKind.Appeared, 0, S.position, 0f);
                    // the windup event carries the cut's far end (the line to draw)
                    Emit(events, VesperEventKind.StrikeWindup, 0, S.position + S.aim * T.strikeReach, T.strikeReach);
                    To(VesperAction.StrikeWindup, T.strikeWindupSeconds);
                    break;
                case VesperAction.StrikeWindup:
                    bool cut = VesperStrike.Hits(S.position, S.aim, input.player, T.strikeReach, T.strikeHalfWidth);
                    S.position += S.aim * T.lungeEnd;
                    if (_hasBounds)
                    {
                        S.position.X = Math.Max(_bounds.min.X + 1f, Math.Min(_bounds.max.X - 1f, S.position.X));
                        S.position.Z = Math.Max(_bounds.min.Z + 1f, Math.Min(_bounds.max.Z - 1f, S.position.Z));
                    }
                    if (cut) Emit(events, VesperEventKind.StrikeHit, 0, S.position, T.strikeDamage);
                    else Emit(events, VesperEventKind.StrikeMissed, 0, S.position, 0f);
                    Emit(events, VesperEventKind.Exposed, 0, S.position, 0f);
                    To(VesperAction.Exposed, T.exposedSeconds);
                    break;
                case VesperAction.Shift:
                    EnterPhase(S.phase + 1, events);
                    break;
                case VesperAction.Kneel:
                    S.action = VesperAction.Done;
                    if (!S.defeatedRaised)
                    {
                        S.defeatedRaised = true;
                        Emit(events, VesperEventKind.Defeated, 0, S.position, 0f);
                    }
                    break;
            }
        }

        /// <summary>The next move after a pause.</summary>
        void Decide(in Input input, List<VesperEvent> events)
        {
            var pt = T.For(S.phase);
            if (S.phase == 3)
            {
                // the hunt; every few hunts a volley from a perch (she blinks there first)
                if (S.volleysLeft > 0 && S.perch >= 0) { StartVolleyTell(events); return; }
                S.hunts++;
                if (T.volleyEveryHunts > 0 && S.hunts % T.volleyEveryHunts == 0) { StartBlink(input, events); return; }
                To(VesperAction.Hidden, Pace(T.hiddenSeconds, input));
                S.perch = -1;
                return;
            }
            if (S.phase == 2)
            {
                float undo = HealthAgo(T.rewindSeconds) - S.health;
                if (S.rewindCooldown <= 0f && undo >= T.rewindMinUndo)
                {
                    S.rewindTo = Math.Min(PhaseCap, HealthAgo(T.rewindSeconds));
                    S.breakDamage = 0f;
                    Emit(events, VesperEventKind.RewindTell, 0, S.position, S.rewindTo - S.health);
                    To(VesperAction.RewindTell, T.rewindTellSeconds);
                    return;
                }
                if (S.resummonTimer <= 0f && input.impsAlive < T.summonCount) { StartSummon(events); return; }
            }
            if (S.volleysLeft > 0 && S.damageAtPerch < T.pressureBlink) StartVolleyTell(events);
            else StartBlink(input, events);
        }

        void StartVolleyTell(List<VesperEvent> events)
        {
            Emit(events, VesperEventKind.VolleyTell, 0, S.position, 0f);
            To(VesperAction.VolleyTell, T.For(S.phase).volleyTellSeconds);
        }

        void StartSummon(List<VesperEvent> events)
        {
            Emit(events, VesperEventKind.SummonTell, 0, S.position, 0f);
            To(VesperAction.SummonTell, T.summonTellSeconds);
        }

        void StartBlink(in Input input, List<VesperEvent> events)
        {
            int to = PickPerch(input.player);
            S.marked = Perch(to);
            Emit(events, VesperEventKind.BlinkTell, to, S.marked, 0f);
            To(VesperAction.BlinkTell, T.blinkTellSeconds);
        }

        void FireBolt(List<VesperEvent> events)
        {
            var pt = T.For(S.phase);
            // the fan: 0, +1, -1, +2, -2 ... steps of fanDegrees
            int i = S.boltIndex++;
            int side = i == 0 ? 0 : (i % 2 == 1 ? (i + 1) / 2 : -(i / 2));
            Emit(events, VesperEventKind.Bolt, side, S.position, pt.fanDegrees * side);
            S.boltsLeft--;
        }

        float Pace(float seconds, in Input input) => input.playerHealth < T.lowHealth ? seconds * T.lowHealthPace : seconds;

        void To(VesperAction a, float seconds)
        {
            S.action = a;
            S.timer = seconds;
        }

        // ------------------------------------------------------------------ damage

        /// <summary>
        /// A shot hit her: returns the damage done (0 when she cannot be hurt now). The heart takes
        /// <paramref name="heartMultiplier"/> (the gun's weak-point multiplier; 0 or less: <see cref="VesperTuning.heartMultiplier"/>);
        /// a staggered or exposed Vesper takes more. Health never drops under the phase's floor: reaching it starts the shift to
        /// the next phase (or, in phase 3, her defeat). Heart damage during her Rewind's wind-up breaks it at
        /// <see cref="VesperTuning.rewindBreak"/>.
        /// </summary>
        public float Hit(float damage, bool heart, float heartMultiplier, List<VesperEvent> events)
        {
            if (!Vulnerable || !(damage > 0f)) return 0f;
            float d = VesperDamage.Scale(damage, heart, heartMultiplier > 0f ? heartMultiplier : T.heartMultiplier, S.action, T);
            float floor = PhaseFloor;
            float applied = Math.Min(d, Math.Max(0f, S.health - floor));
            S.health -= applied;
            S.damageAtPerch += applied;
            if (S.action == VesperAction.RewindTell && heart)
            {
                S.breakDamage += d;
                if (S.breakDamage >= T.rewindBreak)
                {
                    S.rewindCooldown = T.rewindCooldown;
                    ClearHistory();
                    Emit(events, VesperEventKind.RewindBroken, 0, S.position, 0f);
                    Line(VesperLines.RewindBroken, events);
                    To(VesperAction.Stagger, T.staggerSeconds);
                }
            }
            if (S.health <= floor + 1e-4f)
            {
                S.health = floor;
                if (S.phase >= 3)
                {
                    S.health = 0f;
                    Emit(events, VesperEventKind.Kneel, 0, S.position, 0f);
                    Line(VesperLines.Defeat, events);
                    To(VesperAction.Kneel, T.kneelSeconds);
                }
                else
                {
                    // she stands in her darkness again for the shift
                    if (S.phase == 2 || S.perch < 0) { S.perch = 0; S.position = Perch(0); }
                    S.boltsLeft = 0;
                    Emit(events, VesperEventKind.Shift, S.phase + 1, S.position, 0f);
                    Line(S.phase == 1 ? VesperLines.Phase2 : VesperLines.Phase3, events);
                    To(VesperAction.Shift, T.shiftSeconds);
                }
            }
            return applied;
        }

        /// <summary>
        /// One of her blows is about to kill the player (the Unity side asks from the player's wreck intercept): true when she
        /// pulls it short (once per <see cref="VesperTuning.mercyCooldown"/>; the player is left on <see cref="VesperTuning.mercySliver"/>),
        /// and she holds back for <see cref="VesperTuning.mercySeconds"/>. False: the blow kills.
        /// </summary>
        public bool TryMercy(List<VesperEvent> events)
        {
            if (!Running || Defeated || S.mercyCooldown > 0f) return false;
            S.mercyCooldown = T.mercyCooldown;
            S.mercies++;
            Emit(events, VesperEventKind.Mercy, S.mercies, S.position, T.mercySliver);
            Line(S.mercies % 2 == 1 ? VesperLines.Mercy : VesperLines.Mercy2, events);
            // a volley in progress stops; she stays where she is (hidden in phase 3 she steps out to look at you)
            if (S.action == VesperAction.Hidden || S.action == VesperAction.HuntTell) { S.position = S.marked; S.perch = -1; }
            S.boltsLeft = 0;
            To(VesperAction.Mercy, T.mercySeconds);
            return true;
        }

        // ------------------------------------------------------------------ her Rewind's memory

        void ClearHistory()
        {
            S.historyHead = 0;
            S.historyCount = 0;
            S.historyTimer = 0f;
            Push(S.health);
        }

        void Record(float dt)
        {
            S.historyTimer += dt;
            while (S.historyTimer >= T.historyStep)
            {
                S.historyTimer -= T.historyStep;
                Push(S.health);
            }
        }

        void Push(float h)
        {
            _history[S.historyHead] = h;
            S.historyHead = (S.historyHead + 1) % _history.Length;
            if (S.historyCount < _history.Length) S.historyCount++;
        }

        /// <summary>Her health about <paramref name="seconds"/> ago (the oldest kept when the history is shorter).</summary>
        public float HealthAgo(float seconds)
        {
            if (S.historyCount == 0) return S.health;
            int back = (int)Math.Round(seconds / Math.Max(1e-3f, T.historyStep));
            back = Math.Max(0, Math.Min(back, S.historyCount - 1));
            int newest = (S.historyHead - 1 + _history.Length) % _history.Length;
            return _history[(newest - back + _history.Length * 2) % _history.Length];
        }

        // ------------------------------------------------------------------ places

        int PickPerch(Vector3 player)
        {
            int n = _perches.Length;
            if (n <= 1) return 0;
            // a different perch, far enough from the player when one is (seeded)
            int far = 0;
            for (int i = 0; i < n; i++) if (i != S.perch && Flat(_perches[i] - player) >= T.blinkMinDistance) far++;
            int pick = (int)(Next() % (ulong)Math.Max(1, far > 0 ? far : n - 1));
            for (int i = 0; i < n; i++)
            {
                if (i == S.perch) continue;
                if (far > 0 && Flat(_perches[i] - player) < T.blinkMinDistance) continue;
                if (pick-- == 0) return i;
            }
            return (S.perch + 1) % n;
        }

        int PerchIndexOf(Vector3 p)
        {
            for (int i = 0; i < _perches.Length; i++) if (Vector3.DistanceSquared(_perches[i], p) < 1e-4f) return i;
            return -1;
        }

        /// <summary>Where she steps out in phase 3: behind the player (against their facing) at the hunt distance, turned a
        /// seeded amount either way, kept inside the arena.</summary>
        Vector3 HuntSpot(in Input input)
        {
            var f = input.playerForward;
            f.Y = 0f;
            if (f.LengthSquared() < 1e-6f) f = new Vector3(0, 0, 1);
            f = Vector3.Normalize(f);
            float spread = T.huntSpreadDegrees * (float)Math.PI / 180f;
            float a = ((Next() % 2001UL) / 1000f - 1f) * spread;
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            var back = new Vector3(-(f.X * c - f.Z * s), 0f, -(f.X * s + f.Z * c));
            var p = input.player + back * T.huntDistance;
            if (_hasBounds)
            {
                p.X = Math.Max(_bounds.min.X + 1.5f, Math.Min(_bounds.max.X - 1.5f, p.X));
                p.Z = Math.Max(_bounds.min.Z + 1.5f, Math.Min(_bounds.max.Z - 1.5f, p.Z));
            }
            p.Y = input.player.Y;
            return p;
        }

        static float Flat(Vector3 v) => (float)Math.Sqrt(v.X * v.X + v.Z * v.Z);

        ulong Next()
        {
            // xorshift64*: the stream lives in S, so a snapshot rewinds her choices too
            ulong x = S.random;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            S.random = x;
            return x * 2685821657736338717UL;
        }

        // ------------------------------------------------------------------ snapshots

        public Snapshot SaveTo(Snapshot into = null)
        {
            var s = into ?? new Snapshot();
            s.s = S;
            if (s.history == null || s.history.Length != _history.Length) s.history = new float[_history.Length];
            Array.Copy(_history, s.history, _history.Length);
            return s;
        }

        public void LoadFrom(Snapshot s)
        {
            if (s == null) return;
            S = s.s;
            if (s.history != null && s.history.Length == _history.Length) Array.Copy(s.history, _history, _history.Length);
        }

        // ------------------------------------------------------------------ events

        static void Emit(List<VesperEvent> events, VesperEventKind kind, int index, Vector3 at, float amount)
        {
            if (events != null) events.Add(new VesperEvent { kind = kind, index = index, position = at, amount = amount });
        }

        static void Line(string id, List<VesperEvent> events)
        {
            if (events != null) events.Add(new VesperEvent { kind = VesperEventKind.Line, line = id });
        }
    }

    /// <summary>Her phase 3 cut: a lunge along a locked line.</summary>
    public static class VesperStrike
    {
        /// <summary>Is <paramref name="player"/> (feet) inside the cut: ahead of <paramref name="from"/> along <paramref name="aim"/>
        /// (flat, unit) up to <paramref name="reach"/>, within <paramref name="halfWidth"/> of the line, and within 2.5 m in height.</summary>
        public static bool Hits(Vector3 from, Vector3 aim, Vector3 player, float reach, float halfWidth)
        {
            var d = player - from;
            if (Math.Abs(d.Y) > 2.5f) return false;
            d.Y = 0f;
            float along = d.X * aim.X + d.Z * aim.Z;
            if (along < -0.5f || along > reach) return false;
            float side = Math.Abs(d.X * aim.Z - d.Z * aim.X);
            return side <= halfWidth;
        }
    }

    /// <summary>The damage rules of the fight (pure).</summary>
    public static class VesperDamage
    {
        /// <summary>A shot's damage on her: the heart multiplies; staggered (her Rewind broken) or exposed (after her cut in
        /// phase 3) she takes more.</summary>
        public static float Scale(float damage, bool heart, float heartMultiplier, VesperAction action, VesperTuning t)
        {
            float d = damage * (heart ? heartMultiplier : 1f);
            if (action == VesperAction.Stagger) d *= t.staggerMultiplier;
            else if (action == VesperAction.Exposed) d *= t.exposedMultiplier;
            return d;
        }
    }
}
