using System;

namespace JamKit.Skyline
{
    /// <summary>
    /// Shadow hides you (docs/HELL-SHADOW.md): the numbers. Darkness at your chest, unlit, beyond <see cref="hideBeyond"/> of
    /// an enemy, after <see cref="settleSeconds"/> in it, hides you from that enemy. Moving fast (sprint, slide, wall-run)
    /// shrinks the cover to <see cref="fastHideBeyond"/>: stealth or speed. Firing, or fire light on you, reveals you for
    /// <see cref="revealSeconds"/> / <see cref="litRevealSeconds"/>.
    /// </summary>
    public sealed class ShadowTuning
    {
        /// <summary>An enemy this close (m) sees you in any darkness.</summary>
        public float hideBeyond = 4f;
        /// <summary>Moving fast in darkness, an enemy this close (m) still sees you.</summary>
        public float fastHideBeyond = 12f;
        /// <summary>Faster than this (m/s) counts as moving fast (walk 5, sprint 9.5, slide 13); a slide or a wall-run always does.</summary>
        public float fastSpeed = 7f;
        /// <summary>Time in darkness (s) before it hides you: diving through a dark patch at a run doesn't.</summary>
        public float settleSeconds = 0.3f;
        /// <summary>Your shot reveals you for this long (s).</summary>
        public float revealSeconds = 1.5f;
        /// <summary>Fire light on you while in a dark zone reveals you for this long after it passes (s).</summary>
        public float litRevealSeconds = 1.5f;

        public float HideRange(bool fast) => fast ? fastHideBeyond : hideBeyond;
        public bool IsFast(float speed, bool slidingOrWallrunning) => slidingOrWallrunning || speed > fastSpeed;
    }

    /// <summary>The stealth state as a value (a Rewind snapshot copies it).</summary>
    public struct StealthState
    {
        public float revealLeft, darkTime;
        public bool inDarkVolume, lit, fast, shotFromShadow;
    }

    /// <summary>
    /// The player's hidden/revealed state machine (pure; the Unity side ticks it every frame from <c>HellDarkness</c> and the
    /// movement, and asks it per enemy). Concealed = in a dark volume, not lit, settled, not revealed.
    /// </summary>
    public sealed class PlayerStealth
    {
        public ShadowTuning tuning;
        StealthState _s;

        public PlayerStealth(ShadowTuning t = null) { tuning = t ?? new ShadowTuning(); }

        /// <summary>In a dark zone or a dark sphere (lit or not).</summary>
        public bool InDarkVolume => _s.inDarkVolume;
        /// <summary>In a dark zone and a fire light is on you.</summary>
        public bool Lit => _s.lit;
        public bool Fast => _s.fast;
        /// <summary>Seconds of reveal left (a shot, a light).</summary>
        public float RevealLeft => _s.revealLeft;
        public bool Revealed => _s.revealLeft > 0f;
        public float DarkTime => _s.darkTime;
        /// <summary>Hidden from every enemy beyond <see cref="Range"/>.</summary>
        public bool Concealed => _s.inDarkVolume && !_s.lit && _s.revealLeft <= 0f && _s.darkTime >= tuning.settleSeconds;
        /// <summary>The distance inside which an enemy still sees you now.</summary>
        public float Range => tuning.HideRange(_s.fast);

        /// <summary>One frame: <paramref name="inDarkVolume"/> at your chest (zones and dark spheres, lights ignored),
        /// <paramref name="lit"/> (any fire light, static or passing, at your chest), <paramref name="fast"/>.</summary>
        public void Tick(float dt, bool inDarkVolume, bool lit, bool fast)
        {
            if (!(dt >= 0f) || float.IsInfinity(dt)) dt = 0f;
            _s.revealLeft = Math.Max(0f, _s.revealLeft - dt);
            _s.inDarkVolume = inDarkVolume;
            _s.lit = inDarkVolume && lit;
            _s.fast = fast;
            if (_s.lit) _s.revealLeft = Math.Max(_s.revealLeft, tuning.litRevealSeconds);
            bool dark = inDarkVolume && !lit;
            _s.darkTime = dark ? _s.darkTime + dt : 0f;
        }

        /// <summary>You fired: remembers whether the shot came from hiding (Ambush), then reveals you.</summary>
        public void Fired()
        {
            _s.shotFromShadow = Concealed;
            Reveal(tuning.revealSeconds);
        }

        /// <summary>Was the last shot fired from hiding? Clears it (one Ambush per shot).</summary>
        public bool TakeShotFromShadow()
        {
            bool b = _s.shotFromShadow;
            _s.shotFromShadow = false;
            return b;
        }

        public void Reveal(float seconds) { if (seconds > _s.revealLeft) _s.revealLeft = seconds; }

        /// <summary>An enemy <paramref name="distance"/> m away can't see you.</summary>
        public bool HiddenFrom(float distance) => Concealed && distance > Range;

        public StealthState Save() => _s;
        public void Restore(StealthState s) => _s = s;
        public void Clear() => _s = default(StealthState);
    }

    /// <summary>
    /// Fire kills shadow, live (docs/HELL-SHADOW.md): how big and how long each fire source lights the dark. A light makes the
    /// dark under it not dark (no hiding, no Shadow Step landing) for its time.
    /// </summary>
    public static class FireExposure
    {
        /// <summary>An ember bolt in flight: radius (m) round its path, and how long the path stays lit (s).</summary>
        public const float BoltRadius = 2.5f, BoltSeconds = 0.6f;
        /// <summary>An ember bolt striking: it floods the spot.</summary>
        public const float ImpactRadius = 3.5f, ImpactSeconds = 1.5f;
        /// <summary>A burning enemy: radius, refreshed every <see cref="BurnRefresh"/> s while it burns.</summary>
        public const float BurnRadius = 4f, BurnSeconds = 0.5f, BurnRefresh = 0.2f;
        /// <summary>Combustion's blast: its own radius plus this, for this long.</summary>
        public const float BlastExtra = 1f, BlastSeconds = 2f;
        /// <summary>An ooze's burning pool: its radius plus this, refreshed while hot.</summary>
        public const float PoolExtra = 2f, PoolSeconds = 0.5f, PoolRefresh = 0.25f;
        /// <summary>A bolt lights the dark only near it: it stamps light when its path comes within this of a dark volume.</summary>
        public const float BoltNearDark = BoltRadius;
        /// <summary>Bolt stamps along a path at most this far apart (m), so a fast bolt leaves no gaps.</summary>
        public const float BoltStampSpacing = 1.5f;

        /// <summary>How many stamps a path of <paramref name="length"/> m needs (at least 1).</summary>
        public static int StampsFor(float length) => Math.Max(1, (int)Math.Ceiling(length / BoltStampSpacing));
    }

    /// <summary>
    /// Darkness sounds dark (docs/HELL-SHADOW.md): a smoothed depth (0 = open world, 1 = hidden) and what it does to the mix:
    /// the world and the enemies low-passed and dipped, your heartbeat up front, and the music's Dark modifier.
    /// </summary>
    public sealed class DarkHush
    {
        public float attack = 0.35f, release = 0.6f;
        /// <summary>The world/enemy/bed dip at full depth (dB) and the low-pass it closes to (Hz).</summary>
        public float worldDb = -7f, worldCutoffHz = 1200f;
        public const float OpenHz = 22000f;
        /// <summary>The music's Dark modifier at full depth: drums down to this gain, a low-pass to this, the drone up to this.</summary>
        public const float MusicDrumsFloor = 0f, MusicCutoffHz = 900f, MusicDroneGain = 0.8f;

        float _depth;
        public float Depth => _depth;

        /// <summary>The depth to aim for: hidden 1, hidden but moving fast 0.7, in darkness yet exposed 0.35, else 0.</summary>
        public static float TargetFor(bool concealed, bool fast, bool inDarkVolume) =>
            concealed ? (fast ? 0.7f : 1f) : (inDarkVolume ? 0.35f : 0f);

        /// <summary>Eases toward <paramref name="target"/> (exponential, attack in, release out).</summary>
        public float Step(float target, float dt)
        {
            if (!(dt > 0f)) return _depth;
            float tc = target > _depth ? attack : release;
            _depth += (target - _depth) * (1f - (float)Math.Exp(-dt / Math.Max(1e-3f, tc)));
            if (Math.Abs(target - _depth) < 1e-4f) _depth = target;
            return _depth;
        }

        public void Set(float depth) => _depth = Math.Max(0f, Math.Min(1f, depth));

        public float WorldGainDb => worldDb * _depth;
        public float WorldCutoff => LogLerp(OpenHz, worldCutoffHz, _depth);
        /// <summary>The heartbeat's level (0..1): only once the hush is well in.</summary>
        public float Heartbeat => Math.Max(0f, (_depth - 0.3f) / 0.7f) * 0.85f;

        /// <summary>The music's drums stem gain at <paramref name="depth"/> (1 open, <see cref="MusicDrumsFloor"/> hidden).</summary>
        public static float MusicDrums(float depth) => 1f + (MusicDrumsFloor - 1f) * Clamp01(depth);
        /// <summary>The music bus low-pass at <paramref name="depth"/> (Hz).</summary>
        public static float MusicCutoff(float depth) => LogLerp(OpenHz, MusicCutoffHz, depth);
        /// <summary>The Dark drone layer's gain at <paramref name="depth"/>.</summary>
        public static float MusicDrone(float depth) => MusicDroneGain * Clamp01(depth);

        /// <summary>A frequency between <paramref name="a"/> and <paramref name="b"/>, even to the ear (log scale).</summary>
        public static float LogLerp(float a, float b, float t)
        {
            t = Clamp01(t);
            return (float)Math.Exp(Math.Log(a) + (Math.Log(b) - Math.Log(a)) * t);
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
