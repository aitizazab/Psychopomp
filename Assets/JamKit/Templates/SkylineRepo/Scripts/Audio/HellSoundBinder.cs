using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// HellDescent's sound, bound from the outside (docs/HELL-SOUND-PASS.md, "Installed"): it WATCHES the game's public state and
    /// events and plays the cues of <see cref="HellSoundTable"/>, so the scripts other lanes edit (Scripts/Hell, the abilities, the
    /// bots) carry no sound code. Created by itself in a scene with a <see cref="HellRunDirector"/>, <see cref="HellAbilities"/> or
    /// <see cref="HellEnemyTestBed"/>.
    /// <list type="bullet">
    /// <item><b>Abilities:</b> the aim hum while E is held, Umbral Chain's free step, the Ambush shot, Borrowed Time's tick, the
    /// Rewind's end tick (0.3 s after it), Ashwalker's lava steps; Time Dilation's muffle and slow-down on the world
    /// (<see cref="SkylineSounds.SetWorldTime"/> from <see cref="HellAbilityAudio.PitchChanged"/>) and its heartbeat.</item>
    /// <item><b>Enemies:</b> the imp's hover loop, the ooze's throb on each beat of its pulse, the demon's footsteps on its stride, the
    /// ooze's pools (burning loop, cooling), the scythes' 0.9 s warning (bent to END on the beam, whatever the time scale) and beam.</item>
    /// <item><b>Rooms:</b> a bed per floor, the nearest braziers, lava bubbles, the lava sizzle, a sink, the collapsing platforms,
    /// every objective's moments and loops, the exit gate and the room-cleared bell, the shaft's wind and landing, the skill pick
    /// (open, hover), the death.</item>
    /// </list>
    /// <para><b>Rewind-safe:</b> one-shots fire only on a change seen in play. A Rewind (<see cref="HellRewind.Rewound"/>), a room
    /// restart and a room change RE-BASE every watch (<see cref="HellSoundRules.Watch"/>) without playing, whichever way the restore
    /// moved it; loops simply follow the restored state; a scythe warning restarts at the offset that still ends on its beam.</para>
    /// <para><b>Music:</b> none, on purpose (the creator composes it with us). The hook: add rows "hell/music/&lt;floor&gt;" (Bus Bed)
    /// to the table and call <c>Loop(this, 2, cue, true, ...)</c> beside the bed in <see cref="TickBeds"/>.</para>
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class HellSoundBinder : MonoBehaviour
    {
        const float LoopFadeSeconds = 0.15f, RewindEndDelay = 0.3f;
        const int MaxLoops = 24, MaxBraziers = 3;
        const float BrazierReach = 14f, TeleportJump = 4f;

        static HellSoundBinder _instance;

        // ------------------------------------------------------------------ boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_instance != null) return;
            if (FindAnyObjectByType<HellRunDirector>() == null && FindAnyObjectByType<HellAbilities>() == null
                && FindAnyObjectByType<HellEnemyTestBed>() == null) return;
            new GameObject("HellSoundBinder").AddComponent<HellSoundBinder>();
        }

        // ------------------------------------------------------------------ loops (own sources: they follow a state every frame)

        sealed class LoopVoice
        {
            public AudioSource src;
            public AudioLowPassFilter lp;
            public object owner;
            public int sub;
            public string cue;
            public SkylineSoundCategory cat;
            public float volume, pitch, fade;
            public bool wanted, live;
        }

        readonly List<LoopVoice> _loops = new List<LoopVoice>();

        /// <summary>Keeps loop <paramref name="cue"/> of (<paramref name="owner"/>, <paramref name="sub"/>) playing this frame (call it every
        /// frame while it should sound; it fades out the first frame it is not called). <paramref name="at"/> null: 2D.</summary>
        void Loop(object owner, int sub, string cue, Vector3? at, float volume = 1f, float pitch = 1f, float maxDistance = 30f)
        {
            LoopVoice v = null;
            for (int i = 0; i < _loops.Count; i++)
            {
                var l = _loops[i];
                if (l.live && l.sub == sub && ReferenceEquals(l.owner, owner) && l.cue == cue) { v = l; break; }
            }
            if (v == null)
            {
                var clip = SkylineSounds.Get(cue);
                if (clip == null) return;
                for (int i = 0; i < _loops.Count; i++) if (!_loops[i].live) { v = _loops[i]; break; }
                if (v == null)
                {
                    if (_loops.Count >= MaxLoops) return;
                    var go = new GameObject("Hell loop " + _loops.Count);
                    go.transform.SetParent(transform, false);
                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.loop = true;
                    s.rolloffMode = AudioRolloffMode.Linear;
                    s.dopplerLevel = 0f;
                    var lp = go.AddComponent<AudioLowPassFilter>();
                    lp.enabled = false;
                    v = new LoopVoice { src = s, lp = lp };
                    _loops.Add(v);
                }
                v.owner = owner; v.sub = sub; v.cue = cue; v.live = true; v.fade = 0f;
                v.cat = SkylineMix.CategoryOf(cue);
                v.src.clip = clip;
                v.src.priority = SkylineMix.Priority[(int)v.cat];
                v.src.time = Random.Range(0f, clip.length * 0.9f);   // two of the same loop never phase together
                v.src.Play();
            }
            v.wanted = true;
            v.volume = volume;
            v.pitch = pitch;
            v.src.spatialBlend = at.HasValue ? 1f : 0f;
            v.src.minDistance = 2f;
            v.src.maxDistance = maxDistance;
            if (at.HasValue) v.src.transform.position = at.Value;
        }

        void TickLoops(float udt)
        {
            float duck = SkylineSounds.DuckDepth, shot = SkylineSounds.ShotDuckDepth;
            foreach (var v in _loops)
            {
                if (!v.live) continue;
                v.fade = Mathf.MoveTowards(v.fade, v.wanted ? 1f : 0f, udt / LoopFadeSeconds);
                v.wanted = false;
                if (v.fade <= 0f) { v.src.Stop(); v.live = false; v.owner = null; continue; }
                Apply(v.src, v.lp, v.cat, v.cue, v.volume * v.fade, v.pitch, duck, shot);
            }
        }

        static void Apply(AudioSource src, AudioLowPassFilter lp, SkylineSoundCategory cat, string cue, float volume, float pitch, float duck, float shot)
        {
            src.volume = Mathf.Clamp01(volume * SkylineMix.GainOf(cue) * SkylineMix.UserGain(cat) * SkylineMix.DuckGain(cat, duck, shot));
            bool dilated = SkylineMix.Dilated(cat);
            float p = pitch * (dilated ? SkylineSounds.WorldPitch : 1f);
            if (!Mathf.Approximately(src.pitch, p)) src.pitch = p;
            bool on = dilated && SkylineSounds.WorldCutoffHz < 21000f;
            if (lp.enabled != on) lp.enabled = on;
            if (on) lp.cutoffFrequency = SkylineSounds.WorldCutoffHz;
        }

        // ------------------------------------------------------------------ the scythes' timed warnings

        sealed class ScytheWatch
        {
            public HellScythe s;
            public ScythePhase last;
            public AudioSource warn;
            public AudioLowPassFilter lp;
        }

        readonly List<ScytheWatch> _scythes = new List<ScytheWatch>();
        readonly Stack<ScytheWatch> _scytheSpare = new Stack<ScytheWatch>();
        int _scytheCount = -1;

        void TickScythes()
        {
            int count = HellScythe.ActiveCount;
            if (count != _scytheCount)
            {
                _scytheCount = count;
                if (count > _scythes.Count)
                    foreach (var s in FindObjectsByType<HellScythe>(FindObjectsSortMode.None))
                    {
                        bool known = false;
                        foreach (var w in _scythes) if (w.s == s) { known = true; break; }
                        if (!known && s.Beam != null) _scythes.Add(NewScythe(s));
                    }
            }
            float duck = SkylineSounds.DuckDepth, shot = SkylineSounds.ShotDuckDepth;
            var clip = SkylineSounds.Get(HellSoundCues.ScytheWarn);
            for (int i = _scythes.Count - 1; i >= 0; i--)
            {
                var w = _scythes[i];
                if (w.s == null || !w.s.gameObject.activeInHierarchy || w.s.Beam == null)
                {
                    w.warn.Stop();
                    _scytheSpare.Push(w);
                    _scythes.RemoveAt(i);
                    continue;
                }
                var b = w.s.Beam;
                var phase = b.Phase;
                Vector3 at = HellRoom.U(b.path.from);
                if (phase == ScythePhase.Telegraph && clip != null)
                {
                    // the warning ENDS on the beam: start it so its remainder equals the time left, and bend its speed to keep it so
                    float scale = Mathf.Max(Time.timeScale, 0.001f);
                    float leftReal = Mathf.Max(0f, b.telegraph - b.T) / scale;
                    w.warn.transform.position = at;
                    if (Time.timeScale < 0.01f) { if (w.warn.isPlaying) w.warn.Pause(); }
                    else if (!w.warn.isPlaying)
                    {
                        float offset = HellSoundRules.WarnOffset(clip.length, leftReal);
                        if (offset >= 0f && offset < clip.length - 0.02f)
                        {
                            w.warn.clip = clip;
                            w.warn.time = offset;
                            w.warn.Play();
                        }
                    }
                    // muffled with the world, but its speed is the beam's (not the world pitch): it must end as the beam fires
                    float bend = w.warn.isPlaying && leftReal > 0.01f ? Mathf.Clamp((clip.length - w.warn.time) / leftReal, 0.5f, 2f) : 1f;
                    Apply(w.warn, w.lp, SkylineSoundCategory.Bots, HellSoundCues.ScytheWarn, 1f, 1f, duck, shot);
                    w.warn.pitch = bend;
                }
                else if (w.warn.isPlaying) w.warn.Stop();
                if (phase == ScythePhase.Sweep && w.last == ScythePhase.Telegraph && !_rebase)
                    SkylineSounds.PlayAt(HellSoundCues.ScytheBeam, at);
                w.last = phase;
            }
        }

        ScytheWatch NewScythe(HellScythe s)
        {
            ScytheWatch w;
            if (_scytheSpare.Count > 0) w = _scytheSpare.Pop();
            else
            {
                var go = new GameObject("Hell scythe warning");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 3f;
                src.maxDistance = 45f;
                src.dopplerLevel = 0f;
                var lp = go.AddComponent<AudioLowPassFilter>();
                lp.enabled = false;
                w = new ScytheWatch { warn = src, lp = lp };
            }
            w.s = s;
            w.last = s.Beam.Phase;   // first seen: a fresh one is in its telegraph (the warning starts below); a restored one is re-based
            return w;
        }

        // ------------------------------------------------------------------ enemies

        sealed class BotWatch
        {
            public float pulse = -1f, walk;
            public int seenFrame;
        }

        readonly Dictionary<SkylineBot, BotWatch> _bots = new Dictionary<SkylineBot, BotWatch>();
        readonly List<SkylineBot> _botScratch = new List<SkylineBot>();

        sealed class PoolWatch
        {
            public HellLavaPool p;
            public bool burning;
        }

        readonly List<PoolWatch> _pools = new List<PoolWatch>();

        void TickEnemies()
        {
            int frame = Time.frameCount;
            var live = SkylineBot.Live;
            for (int i = 0; i < live.Count; i++)
            {
                var bot = live[i];
                if (bot == null || bot.Def == null || bot.Hell == HellKind.None || !bot.Alive) continue;
                if (!_bots.TryGetValue(bot, out var w))
                {
                    w = new BotWatch { pulse = bot.HellPulsePhase, walk = bot.HellWalkPhase };
                    _bots[bot] = w;
                }
                w.seenFrame = frame;
                switch (bot.Hell)
                {
                    case HellKind.FireImp:
                        Loop(bot, 0, HellSoundCues.ImpHover, bot.Center, 1f, 1f, 22f);
                        break;
                    case HellKind.LavaOoze:
                    {
                        float now = bot.HellPulsePhase;
                        if (!_rebase && HellSoundRules.BeatWrapped(w.pulse, now))
                            SkylineSounds.PlayAt(HellSoundCues.OozeThrob, bot.Center, bot.IsAwake ? 1f : 0.55f);
                        w.pulse = now;
                        break;
                    }
                    case HellKind.ScytheDemon:
                    {
                        float now = bot.HellWalkPhase;
                        if (!_rebase && bot.HellStriding && HellSoundRules.StepsBetween(w.walk, now) > 0)
                            SkylineSounds.PlayAt(HellSoundCues.DemonStep, bot.transform.position);
                        w.walk = now;
                        break;
                    }
                }
            }
            if ((frame & 63) == 0)
            {
                _botScratch.Clear();
                foreach (var kv in _bots) if (kv.Key == null || frame - kv.Value.seenFrame > 30) _botScratch.Add(kv.Key);
                foreach (var b in _botScratch) _bots.Remove(b);
            }

            for (int i = _pools.Count - 1; i >= 0; i--)
            {
                var pw = _pools[i];
                if (pw.p == null || !pw.p.gameObject.activeInHierarchy || pw.p.Lava == null) { _pools.RemoveAt(i); continue; }
                bool burning = pw.p.Lava.Burning;
                if (burning) Loop(pw.p, 0, HellSoundCues.PoolLoop, pw.p.transform.position, 1f, 1f, 18f);
                else if (pw.burning && !_rebase) SkylineSounds.PlayAt(HellSoundCues.PoolCool, pw.p.transform.position);
                pw.burning = burning;
            }
        }

        void OnBotAttacked(SkylineBot bot)
        {
            if (bot == null || bot.Hell != HellKind.LavaOoze) return;
            var pools = bot.HellPools;
            if (pools == null || pools.Count == 0) return;
            var p = pools[pools.Count - 1];
            foreach (var pw in _pools) if (pw.p == p) return;
            _pools.Add(new PoolWatch { p = p, burning = false });
        }

        // ------------------------------------------------------------------ abilities

        HellAbilities _abilities;
        SkylinePlayer _player;
        System.Func<float> _ambushInner, _ambushWrap;
        int _chargesBefore;
        float _rewindEndAt = -1f, _lavaStepAt, _lavaTouchAt;

        void BindAbilities()
        {
            var a = HellAbilities.Instance;
            if (a == _abilities) return;
            if (_abilities != null) _abilities.Stepped -= OnStepped;
            _abilities = a;
            if (_abilities != null) _abilities.Stepped += OnStepped;
        }

        void BindPlayer(SkylinePlayer p)
        {
            if (p == _player) return;
            if (_player != null) { _player.Wrecked -= OnWrecked; _player.HitLanded -= OnHitLanded; }
            _player = p;
            if (_player != null) { _player.Wrecked += OnWrecked; _player.HitLanded += OnHitLanded; _lastFeet = _player.transform.position; }
        }

        void OnStepped(Vector3 from, Vector3 to)
        {
            _skipTeleportFrame = Time.frameCount + 1;
            _lastFeet = to;
            // Umbral Chain's free step spent no charge: its brighter layer on top of the step's own sound
            if (_abilities != null && _abilities.Has(HellSkills.UmbralChain) && _abilities.StepCharges.Charges >= _chargesBefore)
                SkylineSounds.PlayAt(HellSoundCues.ShadowStepFree, from);
        }

        void TickAbilities(float udt)
        {
            BindAbilities();
            var a = _abilities;
            if (a == null) return;
            if (a.Player != null) BindPlayer(a.Player);
            _chargesBefore = a.StepCharges != null ? a.StepCharges.Charges : 0;
            if (a.Aiming) Loop(a, 0, HellSoundCues.ShadowStepAim, null);

            // Ambush: the first shot after a step is tripled; its crack rides on that shot
            if (a.Has(HellSkills.Ambush) && a.Player != null && a.Player.ShotDamageScale != null && a.Player.ShotDamageScale != _ambushWrap)
            {
                var inner = a.Player.ShotDamageScale;
                _ambushInner = inner;
                _ambushWrap = () =>
                {
                    float s = _ambushInner != null ? _ambushInner() : 1f;
                    if (s > 1f && _player != null) SkylineSounds.PlayAt(HellSoundCues.Ambush, _player.EyePosition);
                    return s;
                };
                a.Player.ShotDamageScale = _ambushWrap;
            }

            if (_rewindEndAt > 0f && Time.unscaledTime >= _rewindEndAt)
            {
                _rewindEndAt = -1f;
                SkylineSounds.PlayAt(HellSoundCues.RewindEnd, a.transform.position);
            }

            // Time Dilation's heartbeat (the muffle itself is set from PitchChanged)
            float beat = Mathf.Max(_dilationDepth, HellShadowAudio.HeartbeatLevel);   // and in the dark (docs/HELL-SHADOW.md)
            if (HellSoundTable.DilationHeartbeat && beat > 0.05f) Loop(a, 1, HellSoundCues.Heartbeat, null, beat);
        }

        float _dilationDepth;

        void OnPitchChanged(float pitch)
        {
            float slowest = _abilities != null && _abilities.dilationCurve != null ? _abilities.dilationCurve.minScale : 0.35f;
            _dilationDepth = HellSoundRules.DilationDepth(pitch, slowest);
            if (!HellSoundTable.DilationMuffle || _dilationDepth <= 0f) SkylineSounds.SetWorldTime(1f, HellSoundRules.MuffleOpenHz);
            else SkylineSounds.SetWorldTime(HellSoundRules.WorldPitch(pitch), HellSoundRules.MuffleCutoff(_dilationDepth));
        }

        void OnKilled(SkylineBot bot, GunDef gun)
        {
            if (_abilities != null && _abilities.Has(HellSkills.BorrowedTime))
                SkylineSounds.PlayAt(HellSoundCues.BorrowedTime, _abilities.transform.position);
        }

        void OnRewound()
        {
            _rewindEndAt = Time.unscaledTime + RewindEndDelay;
            _skipTeleportFrame = Time.frameCount + 2;
            foreach (var w in _scythes) w.warn.Stop();   // each restarts at the offset that ends on its (restored) beam
            Rebase();
        }

        // ------------------------------------------------------------------ the run and its rooms

        HellRunDirector _director;
        HellRoom _room;
        HellRunPhase _phase = HellRunPhase.NotStarted;
        float _shaftSeconds, _bubbleAt;
        bool _rebase;
        int _skipTeleportFrame;
        Vector3 _lastFeet;

        HellSoundRules.Watch _done, _sigils, _anchorsBroken, _wellsSealed;
        LanternState _lantern;
        float _brandLast, _brandChargingUntil;
        readonly List<float> _anchorHp = new List<float>();
        readonly List<float> _wellLast = new List<float>();
        readonly List<Transform> _anchorViews = new List<Transform>();
        readonly List<Transform> _wellViews = new List<Transform>();
        readonly List<Vector3> _braziers = new List<Vector3>();
        readonly List<CoverBox> _lavaBoxes = new List<CoverBox>();
        Transform _gate;

        sealed class PlatformWatch
        {
            public HellCollapsePlatform p;
            public Vector3 last;
            public bool solid, shaking;
            public float still;
        }

        readonly List<PlatformWatch> _platforms = new List<PlatformWatch>();

        void EnterRoom(HellRoom room)
        {
            _room = room;
            _anchorViews.Clear(); _wellViews.Clear(); _braziers.Clear(); _lavaBoxes.Clear(); _platforms.Clear();
            _gate = null;
            if (room == null || room.Def == null) return;
            for (int i = 0; i < room.Def.stops.Count; i++)
            {
                _anchorViews.Add(HellRoom.Find(room.transform, "Anchor " + i));
                _wellViews.Add(HellRoom.Find(room.transform, "Well Cap " + i));
            }
            _gate = HellRoom.Find(room.transform, room.Def.exitGateId);
            foreach (var l in room.Def.lights) _braziers.Add(HellRoom.U(l.position));
            foreach (var h in room.Def.hazards) if (h.kind == HellHazardKind.Lava) _lavaBoxes.Add(h.box);
            foreach (var p in room.Platforms) _platforms.Add(new PlatformWatch { p = p, last = p.transform.position, solid = p.Solid });
            Rebase();
        }

        /// <summary>Takes every watched state as it is now, silently (a Rewind, a restart, a new room).</summary>
        void Rebase()
        {
            _rebase = true;
            var o = _room != null ? _room.Objective : null;
            _done.Rebase(o != null && o.Done ? 1 : 0);
            _sigils.Rebase(o is SigilChain s ? s.Next : 0);
            _anchorsBroken.Rebase(o is ChainAnchors c ? c.BrokenCount : 0);
            _wellsSealed.Rebase(o is OozeWells w ? w.SealedCount : 0);
            _lantern = o is LanternCarry l ? l.State : LanternState.OnPedestal;
            _brandLast = o is BrandHold b ? b.Progress : 0f;
            _anchorHp.Clear();
            if (o is ChainAnchors ca) for (int i = 0; i < ca.Count; i++) _anchorHp.Add(ca.Health(i));
            _wellLast.Clear();
            if (o is OozeWells ow) for (int i = 0; i < ow.Count; i++) _wellLast.Add(ow.SealProgress(i));
            foreach (var pw in _platforms)
                if (pw.p != null) { pw.solid = pw.p.Solid; pw.last = pw.p.transform.position; pw.shaking = true; pw.still = 0f; }
            foreach (var pool in _pools) if (pool.p != null && pool.p.Lava != null) pool.burning = pool.p.Lava.Burning;
        }

        void TickRun(float udt)
        {
            if (_director == null) _director = FindAnyObjectByType<HellRunDirector>();
            if (_director == null || _director.Run == null) return;
            if (_director.player != null) BindPlayer(_director.player);
            var run = _director.Run;
            if (_director.Current != _room) EnterRoom(_director.Current);

            var phase = run.Phase;
            if (phase != _phase)
            {
                if (phase == HellRunPhase.Shaft) { SkylineSounds.PlayViewmodel(HellSoundCues.ShaftStart); _shaftSeconds = 0f; }
                if (_phase == HellRunPhase.Shaft) SkylineSounds.PlayViewmodel(HellSoundCues.ShaftLand);
                _phase = phase;
            }
            if (phase == HellRunPhase.Shaft)
            {
                _shaftSeconds += Time.deltaTime;
                float k = HellSoundRules.WindLevel(_shaftSeconds);
                Loop(this, 1, HellSoundCues.ShaftWind, null, k, 0.85f + 0.3f * k);
            }
            TickBeds(run);
            if (phase == HellRunPhase.InRoom || phase == HellRunPhase.Dropping) TickRoom(udt);
            if (_player != null) _lastFeet = _player.transform.position;
        }

        void TickBeds(HellRun run)
        {
            if (run.Phase == HellRunPhase.NotStarted) return;
            Loop(this, 0, HellSoundCues.Bed(run.Floor), null);
            // music hook: none yet (see the class summary)
        }

        void TickRoom(float udt)
        {
            var room = _room;
            if (room == null || !room.Running || _player == null) return;
            Vector3 feet = _player.transform.position;
            var o = room.Objective;

            // ---- objectives
            switch (o)
            {
                case SigilChain s:
                    if (_sigils.Rose(s.Next) && !_rebase) SkylineSounds.PlayAt(HellSoundCues.Sigil, feet);
                    break;
                case LanternCarry l:
                    if (l.State != _lantern && !_rebase)
                    {
                        if (l.State == LanternState.Carried && _lantern == LanternState.OnPedestal) SkylineSounds.PlayAt(HellSoundCues.LanternPickup, feet);
                        if (l.State == LanternState.Placed) SkylineSounds.PlayAt(HellSoundCues.LanternPlace, feet);
                    }
                    _lantern = l.State;
                    if (l.Carrying) Loop(l, 0, HellSoundCues.LanternCarry, null);
                    break;
                case ChainAnchors c:
                    for (int i = 0; i < c.Count && i < _anchorHp.Count; i++)
                    {
                        float hp = c.Health(i);
                        if (hp < _anchorHp[i] && hp > 0f && !_rebase) SkylineSounds.PlayAt(HellSoundCues.AnchorHit, AnchorAt(i, feet));
                        _anchorHp[i] = hp;
                    }
                    if (_anchorsBroken.Rose(c.BrokenCount) && !_rebase)
                        for (int i = 0; i < c.Count; i++)
                            if (c.Broken(i) && _anchorViews.Count > i && _anchorViews[i] != null && !_anchorViews[i].gameObject.activeSelf)
                            {
                                SkylineSounds.PlayAt(HellSoundCues.AnchorBreak, _anchorViews[i].position);
                                break;
                            }
                    break;
                case BrandHold b:
                {
                    float p = b.Progress;
                    if (p > _brandLast + 1e-5f) _brandChargingUntil = Time.time + 0.2f;
                    _brandLast = p;
                    if (!b.Done && Time.time < _brandChargingUntil) Loop(b, 0, HellSoundCues.BrandCharge, feet, 1f, HellSoundRules.ChargePitch(p));
                    break;
                }
                case OozeWells w:
                    for (int i = 0; i < w.Count && i < _wellLast.Count; i++)
                    {
                        float p = w.SealProgress(i);
                        if (p > _wellLast[i] + 1e-5f && p < 1f) Loop(w, i, HellSoundCues.WellGrind, WellAt(i, feet));
                        _wellLast[i] = p;
                    }
                    if (_wellsSealed.Rose(w.SealedCount) && !_rebase) SkylineSounds.PlayAt(HellSoundCues.WellSealed, feet);
                    break;
            }
            if (_done.Rose(o.Done ? 1 : 0) && !_rebase)
            {
                if (o is BrandHold) SkylineSounds.PlayAt(HellSoundCues.BrandDone, feet);
                SkylineSounds.PlayAt(HellSoundCues.GateOpen, _gate != null ? _gate.position : feet);
                SkylineSounds.PlayViewmodel(HellSoundCues.RoomCleared);
            }

            // ---- lava: the sizzle while burning, Ashwalker's steps, bubbles round the player
            bool touching = room.Lava != null && room.Lava.Touching;
            if (touching && Time.time >= _lavaTouchAt)
            {
                bool immune = HellAbilities.LavaImmune;
                SkylineSounds.PlayAt(immune ? HellSoundCues.LavaWalk : HellSoundCues.LavaTouch, feet);
                _lavaTouchAt = Time.time + (immune ? 0.35f : HellLava.BurnEvery);
            }
            if (_lavaBoxes.Count > 0 && Time.time >= _bubbleAt)
            {
                _bubbleAt = Time.time + Random.Range(0.8f, 2.4f);
                for (int tries = 0; tries < 4; tries++)
                {
                    var bx = _lavaBoxes[Random.Range(0, _lavaBoxes.Count)];
                    var at = new Vector3(Random.Range(bx.min.X, bx.max.X), bx.max.Y, Random.Range(bx.min.Z, bx.max.Z));
                    if ((at - feet).sqrMagnitude > 18f * 18f) continue;
                    SkylineSounds.PlayAt("hell/world/lava_bubble_" + Random.Range(1, 4), at, Random.Range(0.6f, 1f));
                    break;
                }
            }

            // ---- the collapsing platforms: shake once as it starts to tremble, fall when it gives way
            foreach (var pw in _platforms)
            {
                if (pw.p == null) continue;
                Vector3 pos = pw.p.transform.position;
                bool solid = pw.p.Solid, moved = (pos - pw.last).sqrMagnitude > 1e-8f;
                if (solid && moved)
                {
                    pw.still = 0f;
                    if (!pw.shaking && !_rebase) SkylineSounds.PlayAt(HellSoundCues.PlatformShake, pos);
                    pw.shaking = true;
                }
                else if (solid)
                {
                    pw.still += Time.deltaTime;
                    if (pw.still > 0.15f) pw.shaking = false;
                }
                if (pw.solid && !solid && !_rebase) SkylineSounds.PlayAt(HellSoundCues.PlatformFall, pos);
                pw.solid = solid;
                pw.last = pos;
            }

            // ---- the nearest braziers
            for (int n = 0; n < MaxBraziers; n++)
            {
                int best = -1;
                float bestD = BrazierReach * BrazierReach;
                for (int i = 0; i < _braziers.Count; i++)
                {
                    float d = (_braziers[i] - feet).sqrMagnitude;
                    if (d < bestD && !Taken(i, n)) { bestD = d; best = i; }
                }
                _brazierPick[n] = best;
                if (best >= 0) Loop(room, 100 + best, HellSoundCues.FireLoop, _braziers[best], 1f, 1f, BrazierReach);
            }

            // ---- a sink: the player was put back (a teleport) with no step, rewind, death or room change to explain it
            if ((feet - _lastFeet).sqrMagnitude > TeleportJump * TeleportJump && Time.frameCount > _skipTeleportFrame)
                SkylineSounds.PlayAt(HellSoundCues.Sink, _lastFeet);
        }

        readonly int[] _brazierPick = new int[MaxBraziers];

        bool Taken(int index, int upTo)
        {
            for (int i = 0; i < upTo; i++) if (_brazierPick[i] == index) return true;
            return false;
        }

        Vector3 AnchorAt(int i, Vector3 fallback) => i < _anchorViews.Count && _anchorViews[i] != null ? _anchorViews[i].position : fallback;
        Vector3 WellAt(int i, Vector3 fallback) => i < _wellViews.Count && _wellViews[i] != null ? _wellViews[i].position : fallback;

        void OnHitLanded(SkylineHitInfo info, SkylineHitResult r)
        {
            // a shot on an anchor from outside its ward: no damage (the hp is unchanged), a dull deflect instead
            if (_room == null || !(_room.Objective is ChainAnchors c)) return;
            for (int i = 0; i < c.Count && i < _anchorViews.Count && i < _anchorHp.Count; i++)
            {
                var v = _anchorViews[i];
                if (v == null || !v.gameObject.activeSelf || c.Broken(i)) continue;
                if ((v.position - info.point).sqrMagnitude > 2.5f * 2.5f) continue;
                if (c.Health(i) >= _anchorHp[i]) SkylineSounds.PlayAt(HellSoundCues.AnchorDeflect, info.point);
                return;
            }
        }

        void OnWrecked()
        {
            _skipTeleportFrame = Time.frameCount + 2;
            SkylineSounds.PlayViewmodel(HellSoundCues.Death);
        }

        void OnRoomChanged(int floor, int room, string kind) { _skipTeleportFrame = Time.frameCount + 2; }

        void OnRoomRestarted()
        {
            _skipTeleportFrame = Time.frameCount + 2;
            Rebase();
        }

        // ------------------------------------------------------------------ the skill pick

        bool _pickOpen;

        void TickPick()
        {
            bool open = HellSkillPick.Open;
            if (open && !_pickOpen) SkylineSounds.PlayViewmodel(HellSoundCues.SkillOpen);
            _pickOpen = open;
            if (!open) return;
            bool move = false;
            var kb = Keyboard.current;
            if (kb != null)
                move = kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
            var pad = Gamepad.current;
            if (pad != null) move |= pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
            if (move) SkylineSounds.PlayViewmodel(HellSoundCues.SkillHover);
        }

        // ------------------------------------------------------------------ Unity

        void Awake() { _instance = this; }

        void OnEnable()
        {
            HellAbilityAudio.PitchChanged += OnPitchChanged;
            HellRewind.Rewound += OnRewound;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
            HellRunEvents.RoomEntered += OnRoomChanged;
            SkylineBot.Killed += OnKilled;
            SkylineBot.Attacked += OnBotAttacked;
        }

        void OnDisable()
        {
            HellAbilityAudio.PitchChanged -= OnPitchChanged;
            HellRewind.Rewound -= OnRewound;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
            HellRunEvents.RoomEntered -= OnRoomChanged;
            SkylineBot.Killed -= OnKilled;
            SkylineBot.Attacked -= OnBotAttacked;
            if (_abilities != null) _abilities.Stepped -= OnStepped;
            if (_player != null) { _player.Wrecked -= OnWrecked; _player.HitLanded -= OnHitLanded; }
            if (_player != null && _ambushWrap != null && _player.ShotDamageScale == _ambushWrap) _player.ShotDamageScale = _ambushInner;
            SkylineSounds.SetWorldTime(1f, HellSoundRules.MuffleOpenHz);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Start() { SkylineSounds.Ensure(); }

        void Update()
        {
            float udt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
            TickAbilities(udt);
            TickRun(udt);
            TickEnemies();
            TickScythes();
            TickPick();
            TickLoops(udt);
            _rebase = false;
        }
    }
}
