using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// A room's objective (pure). The Unity room feeds it the player's feet (and hits, for the chains) each fixed step, reads
    /// <see cref="Done"/> to open the exit and <see cref="Line"/> for the HUD, and saves and restores it for the Rewind
    /// (<see cref="Snapshot"/> returns a copy that never changes afterwards).
    /// </summary>
    public abstract class HellObjective
    {
        public abstract HellRoomKind Kind { get; }
        public abstract bool Done { get; }
        /// <summary>0..1.</summary>
        public abstract float Progress { get; }
        /// <summary>The HUD's objective line (draft wording for the creator to rewrite).</summary>
        public abstract string Line { get; }
        public abstract object Snapshot();
        public abstract void Restore(object snapshot);
        public abstract void Reset();
        /// <summary>The objective's next point the HUD marker points at (null when there is none).</summary>
        public abstract Vector3? Target { get; }

        /// <summary>The objective for a room.</summary>
        public static HellObjective For(HellRoomDef r)
        {
            var pts = new Vector3[r.stops.Count];
            for (int i = 0; i < pts.Length; i++) pts[i] = r.stops[i].position;
            var t = r.tuning;
            switch (r.kind)
            {
                case HellRoomKind.SixSigils: return new SigilChain(pts, t.radius);
                case HellRoomKind.SoulLantern: return new LanternCarry(pts[0], pts[1], t.radius);
                case HellRoomKind.BreakChains: return new ChainAnchors(pts, t.anchorHealth, t.wardRadius);
                case HellRoomKind.HoldBrand:
                    return r.hold != null ? new BrandHold(pts[0], t.seconds, r.hold.radius, r.hold.halfHeight) : new BrandHold(pts[0], t.seconds);
                case HellRoomKind.Collapse:
                {
                    int waves = 0;
                    foreach (var sp in r.spawns) waves = Math.Max(waves, sp.wave);
                    return new CollapseEscape(pts[0], r.stops[0].reach, t.seconds, waves);
                }
                case HellRoomKind.OozeWells: return new OozeWells(pts, t.radius, t.seconds, t.spawnEvery, t.spawnCap);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Within <paramref name="r"/> across and 2.5 m up or down (a sigil on a ledge does not count from below).</summary>
        protected static bool Near(Vector3 feet, Vector3 p, float r) => Flat(feet, p) <= r && Math.Abs(feet.Y - p.Y) <= 2.5f;
    }

    /// <summary>The Six Sigils: touch A to F in order. A touch out of order does nothing.</summary>
    public sealed class SigilChain : HellObjective
    {
        readonly Vector3[] _p;
        readonly float _r;
        int _next;

        public SigilChain(Vector3[] points, float radius) { _p = points; _r = radius; }
        public override HellRoomKind Kind => HellRoomKind.SixSigils;
        public int Next => _next;
        public int Count => _p.Length;
        public override bool Done => _next >= _p.Length;
        public override float Progress => _p.Length == 0 ? 1f : _next / (float)_p.Length;
        public override string Line => Done ? "SIGILS LIT - GET OUT" : "SIGIL " + (char)('A' + _next) + "   " + _next + " / " + _p.Length;
        public override Vector3? Target => Done ? (Vector3?)null : _p[_next];
        public static char Letter(int i) => (char)('A' + i);

        /// <summary>The sigil touched this step, or -1.</summary>
        public int Tick(Vector3 feet)
        {
            if (Done || !Near(feet, _p[_next], _r)) return -1;
            return _next++;
        }

        public override object Snapshot() => _next;
        public override void Restore(object s) { _next = (int)s; }
        public override void Reset() { _next = 0; }
    }

    public enum LanternState { OnPedestal, Carried, Placed }

    /// <summary>The Soul Lantern: walk into the pedestal to take it, into the altar to place it. While carried the player is
    /// slower and cannot Shadow Step (the Unity side sets the flag). A sink in lava or a death drops it back on its pedestal.</summary>
    public sealed class LanternCarry : HellObjective
    {
        readonly Vector3 _pedestal, _altar;
        readonly float _r;
        public LanternState State { get; private set; }

        public LanternCarry(Vector3 pedestal, Vector3 altar, float radius) { _pedestal = pedestal; _altar = altar; _r = radius; }
        public override HellRoomKind Kind => HellRoomKind.SoulLantern;
        public bool Carrying => State == LanternState.Carried;
        public override bool Done => State == LanternState.Placed;
        public override float Progress => State == LanternState.Placed ? 1f : State == LanternState.Carried ? 0.5f : 0f;
        public override string Line => State == LanternState.OnPedestal ? "TAKE THE SOUL LANTERN" : State == LanternState.Carried ? "CARRY THE LANTERN TO THE ALTAR" : "THE LANTERN BURNS - GET OUT";
        public override Vector3? Target => State == LanternState.OnPedestal ? _pedestal : State == LanternState.Carried ? _altar : (Vector3?)null;

        /// <summary>Returns the new state when it changed this step.</summary>
        public LanternState? Tick(Vector3 feet)
        {
            if (State == LanternState.OnPedestal && Near(feet, _pedestal, _r)) { State = LanternState.Carried; return State; }
            if (State == LanternState.Carried && Near(feet, _altar, _r + 0.5f)) { State = LanternState.Placed; return State; }
            return null;
        }

        /// <summary>A sink or a death: a carried lantern goes back to its pedestal.</summary>
        public bool Drop()
        {
            if (State != LanternState.Carried) return false;
            State = LanternState.OnPedestal;
            return true;
        }

        public override object Snapshot() => State;
        public override void Restore(object s) { State = (LanternState)s; }
        public override void Reset() { State = LanternState.OnPedestal; }
    }

    /// <summary>Break the Chains: three warded anchors; a shot only bites from within the ward radius (measured from the eye).</summary>
    public sealed class ChainAnchors : HellObjective
    {
        readonly Vector3[] _p;
        readonly float _max, _ward;
        float[] _hp;

        public ChainAnchors(Vector3[] points, float health, float ward)
        {
            _p = points; _max = health; _ward = ward;
            _hp = new float[points.Length];
            Reset();
        }

        public override HellRoomKind Kind => HellRoomKind.BreakChains;
        public int Count => _p.Length;
        public float Ward => _ward;
        public bool Broken(int i) => _hp[i] <= 0f;
        public float Health(int i) => _hp[i];
        public int BrokenCount { get { int n = 0; foreach (var h in _hp) if (h <= 0f) n++; return n; } }
        public override bool Done => BrokenCount == _p.Length;
        public override float Progress => _p.Length == 0 ? 1f : BrokenCount / (float)_p.Length;
        public override string Line => Done ? "THE GATE IS FREE - GET OUT" : "BREAK THE CHAINS   " + BrokenCount + " / " + _p.Length;
        public override Vector3? Target
        {
            get
            {
                for (int i = 0; i < _p.Length; i++) if (!Broken(i)) return _p[i];
                return null;
            }
        }

        public bool InWard(int i, Vector3 eye) => Vector3.Distance(eye, _p[i]) <= _ward;

        public enum HitResult { Deflected, Damaged, Broke, AlreadyBroken }

        public HitResult Hit(int i, float damage, Vector3 eye)
        {
            if (Broken(i)) return HitResult.AlreadyBroken;
            if (!InWard(i, eye)) return HitResult.Deflected;
            _hp[i] -= Math.Max(0f, damage);
            return _hp[i] <= 0f ? HitResult.Broke : HitResult.Damaged;
        }

        public override object Snapshot() => (float[])_hp.Clone();
        public override void Restore(object s) { _hp = (float[])((float[])s).Clone(); }
        public override void Reset() { for (int i = 0; i < _hp.Length; i++) _hp[i] = _max; }
    }

    /// <summary>
    /// Hold the Brand's numbers, all in one place (docs/HELL-ROOMS.md, "Tuning"). Charge from speed, not from standing (the
    /// creator, 2026-10-17): inside the ring the brand charges at <see cref="Rate"/>, which grows with your horizontal speed
    /// from <see cref="SlowSpeed"/> (standing, crouching behind cover: <see cref="SlowSeconds"/> for a full charge) to
    /// <see cref="FullSpeed"/> and above (sprint 9.5, slide, wall-run 8.64: <see cref="FastRate"/> times as fast). Waves come
    /// every <see cref="WaveEvery"/> seconds from the first touch whatever you do, so a slow charge costs more fights.
    /// </summary>
    public static class HellBrandTuning
    {
        /// <summary>The ring: 20 m across, and how far above or below its floor the feet may be (a wall-run's height).</summary>
        public const float RingRadius = 10f, RingHalfHeight = 4.5f;
        /// <summary>Seconds for a full charge at a standstill; the fast rate's multiple.</summary>
        public const float SlowSeconds = 30f, FastRate = 3f;
        /// <summary>The speed (m/s) at and below which the rate is the slow one, and at and above which it is the fast one.</summary>
        public const float SlowSpeed = 3f, FullSpeed = 8.5f;
        /// <summary>Seconds between waves once the charge has started.</summary>
        public const float WaveEvery = 5f;

        /// <summary>Seconds for a full charge at full speed (10).</summary>
        public static float FastSeconds => SlowSeconds / FastRate;

        /// <summary>0 at <see cref="SlowSpeed"/> or less, 1 at <see cref="FullSpeed"/> or more: the share of the speed bonus
        /// (the glow's speed-up follows it).</summary>
        public static float SpeedShare(float speed)
        {
            if (!(speed > SlowSpeed)) return 0f;
            return Math.Min(1f, (speed - SlowSpeed) / (FullSpeed - SlowSpeed));
        }

        /// <summary>The rate's multiple of the slow rate at this speed (1..<see cref="FastRate"/>).</summary>
        public static float Multiplier(float speed) => 1f + (FastRate - 1f) * SpeedShare(speed);

        /// <summary>Charge (0..1) per second inside the ring at this horizontal speed.</summary>
        public static float Rate(float speed) => Multiplier(speed) / SlowSeconds;
    }

    /// <summary>
    /// Hold the Brand: a wide ring round a lava pool (<see cref="HellHoldRing"/>) that charges while your feet are inside it, at
    /// a rate that grows with your speed (<see cref="HellBrandTuning"/>): stand behind a stone and it takes 30 s, run laps
    /// round the pool (sprint, slide, wall-run) and it takes 10. Stepping out pauses it; it never drains. From the first moment
    /// of charge a wave is due at once and every <see cref="WaveEvery"/> seconds until it is charged. The kit's
    /// <see cref="Repossession"/> (one job on one socket) is kept only as the HUD's view: its meter mirrors the charge and its
    /// active circle mirrors "inside the ring", so the HUD's tow ring shows the charge.
    /// </summary>
    public sealed class BrandHold : HellObjective
    {
        public struct State
        {
            public float charge, sinceStart, share;
            public int waves;
            public bool started, inRing;
        }

        public readonly Repossession Circle;
        public readonly float WaveEvery, Radius, HalfHeight;
        readonly Vector3 _at;
        State _s;

        public static readonly AssetDef Brand = new AssetDef { kind = AssetKind.ArcadeCabinet, id = "brand", jobSheetName = "The Brand", description = "Hold it until it burns." };

        public BrandHold(Vector3 at, float waveEvery, float radius = HellBrandTuning.RingRadius, float halfHeight = HellBrandTuning.RingHalfHeight)
        {
            _at = at;
            WaveEvery = waveEvery;
            Radius = radius;
            HalfHeight = halfHeight;
            var sockets = new[]
            {
                new SocketDef { id = "brand", group = SocketGroup.Pit, position = at },
                new SocketDef { id = "brand", group = SocketGroup.Inside, position = at },
                new SocketDef { id = "brand", group = SocketGroup.High, position = at },
            };
            Circle = new Repossession(new[] { Brand }, 1UL, sockets);
        }

        JobEntry Entry => Circle.Sheet[0];
        public override HellRoomKind Kind => HellRoomKind.HoldBrand;
        public bool Started => _s.started;
        public int WavesReleased => _s.waves;
        /// <summary>Inside the ring on the last tick.</summary>
        public bool Inside => _s.inRing;
        /// <summary>0..1: the speed bonus on the last tick (0 outside the ring): the glow's speed-up.</summary>
        public float RateShare => _s.share;
        /// <summary>The rate's multiple on the last tick (1..3; 0 outside the ring).</summary>
        public float RateMultiplier => _s.inRing ? 1f + (HellBrandTuning.FastRate - 1f) * _s.share : 0f;

        public bool InCircle(Vector3 feet)
        {
            if (float.IsNaN(feet.X) || float.IsNaN(feet.Y) || float.IsNaN(feet.Z)) return false;
            return Flat(feet, _at) <= Radius && Math.Abs(feet.Y - _at.Y) <= HalfHeight;
        }

        public override bool Done => _s.charge >= 1f;
        public override float Progress => Math.Min(1f, _s.charge);
        public override string Line => Done ? "THE BRAND IS SET - GET OUT"
            : "HOLD THE BRAND   " + (int)Math.Floor(Progress * 100f) + "%" + (_s.inRing ? "   x" + RateMultiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "");
        public override Vector3? Target => Done ? (Vector3?)null : _at;

        /// <summary>Charges while the feet are in the ring, at the rate for <paramref name="speed"/> (horizontal, m/s); returns
        /// how many waves are due this step (numbered from 1: <see cref="WavesReleased"/> after the call is the last one's number).</summary>
        public int Tick(float dt, Vector3 feet, float speed)
        {
            if (Done || !(dt > 0f) || float.IsInfinity(dt)) return 0;
            bool wasStarted = _s.started;
            _s.inRing = InCircle(feet);
            _s.share = _s.inRing ? HellBrandTuning.SpeedShare(speed) : 0f;
            if (_s.inRing)
            {
                _s.charge += HellBrandTuning.Rate(speed) * dt;
                if (_s.charge >= 1f - 1e-5f) _s.charge = 1f;
                _s.started = true;
            }
            SyncCircle();
            if (!_s.started) return 0;
            if (wasStarted) _s.sinceStart += dt;
            if (Done) return 0;
            int due = 1 + (int)Math.Floor(_s.sinceStart / WaveEvery);
            int n = Math.Max(0, due - _s.waves);
            _s.waves += n;
            return n;
        }

        /// <summary>The HUD's view: the kit circle's meter shows the charge, its active circle "inside the ring".</summary>
        void SyncCircle()
        {
            if (Done)
            {
                if (!Entry.meter.Done) Circle.Tick(FloorRules.TowSeconds + 1f, _at);
                return;
            }
            Circle.Reset();
            Entry.meter.RestoreSeconds(_s.charge * FloorRules.TowSeconds);
            Circle.Tick(0f, _s.inRing ? _at : _at + new Vector3(1000f, 0f, 0f));
        }

        public override object Snapshot() => _s;

        public override void Restore(object snapshot)
        {
            _s = (State)snapshot;
            Circle.Reset();
            SyncCircle();
        }

        public override void Reset() { _s = default(State); Circle.Reset(); }
    }

    /// <summary>The Collapse: reach the exit (the floor's fall is the room's timeline, not this). Imps come at the walls on
    /// the clock: wave <c>n</c> (1..<see cref="WaveCount"/>) every <see cref="WaveEvery"/> seconds.</summary>
    public sealed class CollapseEscape : HellObjective
    {
        public struct State
        {
            public bool done;
            public float clock;
            public int waves;
        }

        readonly Vector3 _exit;
        readonly float _r;
        public readonly float WaveEvery;
        public readonly int WaveCount;
        State _s;
        float _distance = -1f;

        public CollapseEscape(Vector3 exit, float reach, float waveEvery = 0f, int waveCount = 0)
        {
            _exit = exit; _r = reach; WaveEvery = waveEvery; WaveCount = waveCount;
        }

        public override HellRoomKind Kind => HellRoomKind.Collapse;
        public override bool Done => _s.done;
        public override float Progress => _s.done ? 1f : 0f;
        public int WavesReleased => _s.waves;
        public override string Line => _s.done ? "OUT!" : "RUN - THE FLOOR IS FALLING" + (_distance >= 0f ? "   " + (int)_distance + " m" : "");
        public override Vector3? Target => _s.done ? (Vector3?)null : _exit;

        public bool Tick(Vector3 feet)
        {
            _distance = Vector3.Distance(feet, _exit);
            if (_s.done || !Near(feet, _exit, _r)) return false;
            _s.done = true;
            return true;
        }

        /// <summary>Advances the wave clock; returns how many waves are due this step (numbered from 1).</summary>
        public int TickWaves(float dt)
        {
            if (_s.done || !(dt > 0f) || WaveEvery <= 0f) return 0;
            _s.clock += dt;
            int due = Math.Min(WaveCount, (int)Math.Floor(_s.clock / WaveEvery));
            int n = Math.Max(0, due - _s.waves);
            _s.waves += n;
            return n;
        }

        public override object Snapshot() => _s;
        public override void Restore(object s) { _s = (State)s; }
        public override void Reset() { _s = default(State); _distance = -1f; }
    }

    /// <summary>
    /// The Ooze Wells: stand within the radius of a well for <c>sealSeconds</c> to seal it (the time pauses when you step
    /// away, it never drains). Each open well asks for an ooze every <c>spawnEvery</c> seconds while it has fewer than its cap alive.
    /// </summary>
    public sealed class OozeWells : HellObjective
    {
        public struct State
        {
            public float[] sealing, sinceSpawn;
        }

        readonly Vector3[] _p;
        readonly float _r, _seal, _every;
        readonly int _cap;
        State _s;

        public OozeWells(Vector3[] wells, float radius, float sealSeconds, float spawnEvery, int cap)
        {
            _p = wells; _r = radius; _seal = sealSeconds; _every = spawnEvery; _cap = cap;
            Reset();
        }

        public override HellRoomKind Kind => HellRoomKind.OozeWells;
        public int Count => _p.Length;
        public bool Sealed(int i) => _s.sealing[i] >= _seal;
        public float SealProgress(int i) => Math.Min(1f, _s.sealing[i] / _seal);
        public int SealedCount { get { int n = 0; for (int i = 0; i < _p.Length; i++) if (Sealed(i)) n++; return n; } }
        public override bool Done => SealedCount == _p.Length;
        public override float Progress { get { float t = 0f; for (int i = 0; i < _p.Length; i++) t += SealProgress(i); return _p.Length == 0 ? 1f : t / _p.Length; } }
        public override string Line => Done ? "THE WELLS ARE SEALED - GET OUT" : "SEAL THE WELLS   " + SealedCount + " / " + _p.Length;
        public override Vector3? Target
        {
            get
            {
                for (int i = 0; i < _p.Length; i++) if (!Sealed(i)) return _p[i];
                return null;
            }
        }

        /// <summary>Advances sealing and spawning; <paramref name="spawnAt"/> gets the wells that spawn an ooze this step;
        /// <paramref name="living"/> is each well's living oozes. Returns the well sealed this step, or -1.</summary>
        public int Tick(float dt, Vector3 feet, IReadOnlyList<int> living, List<int> spawnAt)
        {
            spawnAt?.Clear();
            if (!(dt > 0f)) return -1;
            int sealedNow = -1;
            for (int i = 0; i < _p.Length; i++)
            {
                if (Sealed(i)) continue;
                if (Near(feet, _p[i], _r))
                {
                    _s.sealing[i] += dt;
                    if (Sealed(i)) { sealedNow = i; continue; }
                }
                _s.sinceSpawn[i] += dt;
                int alive = living != null && i < living.Count ? living[i] : 0;
                if (_s.sinceSpawn[i] >= _every)
                {
                    _s.sinceSpawn[i] = 0f;
                    if (alive < _cap) spawnAt?.Add(i);
                }
            }
            return sealedNow;
        }

        public override object Snapshot() => new State { sealing = (float[])_s.sealing.Clone(), sinceSpawn = (float[])_s.sinceSpawn.Clone() };

        public override void Restore(object snapshot)
        {
            var s = (State)snapshot;
            _s = new State { sealing = (float[])s.sealing.Clone(), sinceSpawn = (float[])s.sinceSpawn.Clone() };
        }

        public override void Reset()
        {
            _s = new State { sealing = new float[_p.Length], sinceSpawn = new float[_p.Length] };
            // the first ooze comes a few seconds in, not at once
            for (int i = 0; i < _p.Length; i++) _s.sinceSpawn[i] = _every * 0.5f;
        }
    }
}
