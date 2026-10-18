using System;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// HellDescent's music (docs/HELL-MUSIC-DIRECTOR.md). Reads the composer's manifest
    /// (<see cref="HellMusicManifest.ResourcePath"/>), WATCHES the game (stage, ending phase, Vesper's phase, the pause menu, the
    /// skill pick, the player, the room's objective, the hour, darkness, Time Dilation) and the run's events (floor cards, jobs),
    /// and plays:
    /// <list type="bullet">
    /// <item><b>The in-run set:</b> every stem of the layered loop scheduled on ONE dspTime (<c>PlayScheduled</c>, clip.loop: the
    /// stems are the same length, so they stay sample-locked); mixing is per-stem gain only. The cell (mood x speed) comes from
    /// <see cref="HellMusicCellRules"/>; mood changes fade on the next bar line, speed stems on the next beat.</item>
    /// <item><b>Modifiers:</b> the Dark modifier (<see cref="HellShadowAudio.Depth"/>: drums x <see cref="DarkHush.MusicDrums"/>,
    /// drone = <see cref="DarkHush.MusicDrone"/>, low-pass <see cref="DarkHush.MusicCutoff"/>), Time Dilation
    /// (<see cref="HellAbilityAudio.Pitch"/>: a slight pitch drop and a muffle), the pause menu (Baja's hold loop over the game
    /// music, muffled to 600 Hz and -14 dB), the skill pick (the set out, Baja's gift loop in), stingers ducking the loops.</item>
    /// <item><b>By stage:</b> title, opening, boss (Vesper's phase parts on one clock, switched on bar lines; the kneel), the
    /// decision (hold loop, kill/spare stingers), the escape (the in-run set, Danger at least), the ending cue, credits, stats.
    /// A cue still "pending" in the manifest falls back (<see cref="HellMusicStageRules.Resolve"/>) or stays silent.</item>
    /// </list>
    /// Volume: the Music bus (<see cref="SkylineMix.CategoryGain"/>) times <see cref="SkylineMix.UserGain"/> (the pause menu's Music
    /// slider and the clean-picture switch). The music does NOT rewind (a Rewind is game time; the music is the player's time).
    /// No allocations per frame: the sources are made once, the manifest is read at boot and on returning to the title.
    /// </summary>
    [DefaultExecutionOrder(910)]
    public sealed class HellMusicDirector : MonoBehaviour
    {
        static HellMusicDirector _instance;
        public static HellMusicDirector Instance => _instance;

        public readonly HellMusicCellRules rules = new HellMusicCellRules();
        public HellMusicManifest Manifest { get; private set; }
        public HellMusicScene Scene { get; private set; } = HellMusicScene.Silence;

        const double StartLead = 0.12, SceneFade = 1.0, StopAfter = 0.1;

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
            if (FindAnyObjectByType<HellRunDirector>() == null && FindAnyObjectByType<HellVesperTestBed>() == null
                && FindAnyObjectByType<HellEndingTestBed>() == null) return;
            new GameObject("HellMusicDirector").AddComponent<HellMusicDirector>();
        }

        // ------------------------------------------------------------------ voices

        /// <summary>One music voice: its own GameObject so its low-pass filter is its own.</summary>
        sealed class Voice
        {
            public AudioSource src;
            public AudioLowPassFilter lp;
            public HellMusicStem stem;
            public HellMusicFade fade;
            public double stopAt = -1;
            public bool playing;
        }

        sealed class Group
        {
            public Voice[] voices;
            public HellMusicCue cue;
            public double clockStart, loopSeconds, barSeconds, beatSeconds;
            public HellMusicFade gain = HellMusicFade.Hold(0f);
            public bool running;
        }

        Group _inrun, _boss;
        Voice _bedA, _bedB, _overlay;
        Voice[] _stingers;
        int _nextStinger;
        bool _bedAOn;
        string _bedCue, _overlayCue;

        HellMusicMood _appliedMood = HellMusicMood.Quiet;
        HellMusicSpeed _appliedSpeed = HellMusicSpeed.Still;
        int _bossPhase, _lastFloorCard = -1;
        bool _bossKneel, _wasPaused, _wasPick, _decided;
        double _duckUntil, _fallUntil;
        float _duck = 1f;
        string _endingId;
        HellRunDirector _run;
        SkylinePlayer _player;
        static readonly string[] EndingPhaseNames = Enum.GetNames(typeof(HellEndingDirector.Phase));

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadManifest();
            _bedA = MakeVoice("bedA"); _bedB = MakeVoice("bedB"); _overlay = MakeVoice("overlay");
            _stingers = new[] { MakeVoice("sting0"), MakeVoice("sting1") };
            HellRunEvents.FloorStarted += OnFloorStarted;
            HellRunEvents.JobOffered += OnJobOffered;
            HellRunEvents.JobDone += OnJobDone;
            HellRunEvents.JobFailed += OnJobFailed;
            HellRunEvents.EndingReached += OnEndingReached;
            HellRunEvents.RunStarted += OnRunStarted;
            HellRunEvents.OpeningPage += OnOpeningPage;
        }

        void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            HellRunEvents.FloorStarted -= OnFloorStarted;
            HellRunEvents.JobOffered -= OnJobOffered;
            HellRunEvents.JobDone -= OnJobDone;
            HellRunEvents.JobFailed -= OnJobFailed;
            HellRunEvents.EndingReached -= OnEndingReached;
            HellRunEvents.RunStarted -= OnRunStarted;
            HellRunEvents.OpeningPage -= OnOpeningPage;
        }

        /// <summary>(Re)reads the manifest: the composer flips cues from pending to ready as the files land.</summary>
        public void LoadManifest()
        {
            var ta = Resources.Load<TextAsset>(HellMusicManifest.ResourcePath);
            Manifest = ta != null ? HellMusicManifest.Parse(ta.text) : new HellMusicManifest();
            _inrun = BuildGroup(Manifest.Ready(HellMusicManifest.InRun), _inrun, true);
            _boss = BuildGroup(Manifest.Ready(HellMusicManifest.Boss), _boss, false);
        }

        Voice MakeVoice(string name)
        {
            var go = new GameObject("music-" + name);
            go.transform.SetParent(transform, false);
            var v = new Voice { src = go.AddComponent<AudioSource>(), lp = go.AddComponent<AudioLowPassFilter>(), fade = HellMusicFade.Hold(0f) };
            v.src.playOnAwake = false;
            v.src.spatialBlend = 0f;
            v.src.ignoreListenerPause = true;      // the pause menu pauses the listener; the music plays on under Baja's hold loop
            v.src.priority = 0;
            v.src.volume = 0f;
            v.lp.cutoffFrequency = HellMusicMods.OpenHz;
            return v;
        }

        Group BuildGroup(HellMusicCue cue, Group old, bool stems)
        {
            if (old != null) foreach (var v in old.voices) { v.src.Stop(); Destroy(v.src.gameObject); }
            if (cue == null) return null;
            var g = new Group { cue = cue, barSeconds = cue.BarSeconds, beatSeconds = cue.BeatSeconds, loopSeconds = cue.LoopSeconds };
            var list = new System.Collections.Generic.List<Voice>();
            if (stems) foreach (var s in cue.stems) AddVoice(list, s);
            else foreach (var kv in cue.parts) if (kv.Value.kind == "loop") AddVoice(list, kv.Value);
            g.voices = list.ToArray();
            return g;
        }

        void AddVoice(System.Collections.Generic.List<Voice> list, HellMusicStem s)
        {
            var clip = s.resource != null ? Resources.Load<AudioClip>(s.resource) : null;
            if (clip == null) return;                                  // a stem not landed yet: the set plays without it
            var v = MakeVoice(s.id);
            v.stem = s;
            v.src.clip = clip;
            v.src.loop = true;
            list.Add(v);
        }

        // ------------------------------------------------------------------ events

        void OnRunStarted() { _lastFloorCard = -1; }
        void OnFloorStarted(int floor)
        {
            if (floor == _lastFloorCard) return;
            _lastFloorCard = floor;
            // the opening's fall stinger is floor 1's arrival: it blows up into the in-run set (same frame as the hand-over, or
            // while it still rings)
            if (Scene == HellMusicScene.Opening && HellMusicStageRules.OpeningFall(Manifest) != null) return;
            if (AudioSettings.dspTime < _fallUntil) return;
            Sting(HellMusicStageRules.FloorCard(floor));
        }
        void OnJobOffered(string id) => Sting(HellMusicManifest.JobBrief);
        void OnJobDone(string id) => Sting(HellMusicManifest.JobDone);
        void OnJobFailed(string id) => Sting(HellMusicManifest.JobFailed);
        void OnEndingReached(string id) { _endingId = id; }

        string _openingPart;

        /// <summary>The opening's comic turned a page (docs/HELL-COMIC.md): its music part (sister, hall, wait then idea). A
        /// loop part crossfades in; the "wait" stinger stops the loop dead and the "idea" loop follows it when it ends.</summary>
        void OnOpeningPage(int page)
        {
            if (Scene != HellMusicScene.Opening) return;
            string part = HellComics.OpeningMusic(page);
            var cue = Manifest.Ready(HellMusicManifest.Opening);
            if (part == null || cue == null || part == _openingPart) return;
            if (page == 0 && _openingPart == null) { _openingPart = part; return; }   // the scene's bed is already its first loop (sister)
            _openingPart = part;
            double dsp = AudioSettings.dspTime;
            var p = cue.Part(part);
            if (p == null || p.resource == null) return;
            if (p.kind == "stinger")
            {
                var clip = Resources.Load<AudioClip>(p.resource);
                if (clip == null) return;
                FadeOutVoice(_bedA, dsp, 0.03);
                FadeOutVoice(_bedB, dsp, 0.03);
                PlayStingerAt(clip, 0, dsp + StartLead);
                var idea = cue.Part(HellComics.MusicIdea);
                if (idea != null && idea.resource != null) PlayBedResource(idea.resource, dsp + StartLead + clip.length, 0.05, false);
                return;
            }
            PlayBedResource(p.resource, dsp + StartLead, SceneFade);
        }

        /// <summary>A looping bed from a resource at <paramref name="at"/> (dsp), crossfading from the one that plays.</summary>
        void PlayBedResource(string res, double at, double fade, bool fadeOthers = true)
        {
            var clip = Resources.Load<AudioClip>(res);
            if (clip == null) return;
            _bedCue = HellMusicManifest.Opening + ":" + res;
            if (fadeOthers)
            {
                FadeOutVoice(_bedA, at, fade);
                FadeOutVoice(_bedB, at, fade);
            }
            var v = _bedAOn ? _bedB : _bedA;
            _bedAOn = !_bedAOn;
            v.src.Stop();
            v.src.clip = clip;
            v.src.loop = true;
            v.src.timeSamples = 0;
            v.src.PlayScheduled(at);
            v.playing = true; v.stopAt = -1;
            v.fade = HellMusicFade.Hold(0f);
            v.fade.Retarget(1f, at, fade);
        }

        /// <summary>Plays a stinger cue now (skipped while pending); the loops duck by its duck_db for its length.</summary>
        public void Sting(string cueId)
        {
            var c = Manifest != null ? Manifest.Ready(cueId) : null;
            if (c == null || c.resource == null) return;
            PlayStinger(Resources.Load<AudioClip>(c.resource), c.duckDb);
        }

        void StingPart(HellMusicCue cue, string part)
        {
            var p = cue != null ? cue.Part(part) : null;
            if (p != null && p.resource != null) PlayStinger(Resources.Load<AudioClip>(p.resource), cue.duckDb);
        }

        void PlayStinger(AudioClip clip, double duckDb) => PlayStingerAt(clip, duckDb, -1);

        /// <summary>A stinger now (<paramref name="at"/> &lt; 0) or scheduled on the audio clock at <paramref name="at"/>.</summary>
        void PlayStingerAt(AudioClip clip, double duckDb, double at)
        {
            if (clip == null) return;
            var v = _stingers[_nextStinger];
            _nextStinger = (_nextStinger + 1) % _stingers.Length;
            v.src.Stop();
            v.src.clip = clip;
            v.src.loop = false;
            v.fade = HellMusicFade.Hold(1f);
            if (at >= 0) v.src.PlayScheduled(at); else v.src.Play();
            v.playing = true;
            if (duckDb < 0) { _duck = HellMusicMods.Db((float)duckDb); _duckUntil = Math.Max(at, AudioSettings.dspTime) + clip.length; }
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            double dsp = AudioSettings.dspTime;
            float dt = Time.unscaledDeltaTime;
            if (_run == null) _run = FindAnyObjectByType<HellRunDirector>();
            if (_player == null) _player = _run != null && _run.player != null ? _run.player : FindAnyObjectByType<SkylinePlayer>();

            var ending = HellEndingDirector.Instance;
            var state = new HellMusicState
            {
                stage = HellRunEvents.Stage,
                bossActive = HellVesperFight.Active,
                endingPhase = ending != null && ending.Stage != HellEndingDirector.Phase.Idle ? EndingPhaseNames[(int)ending.Stage] : null,
            };
            if (_run == null && HellVesperFight.Active) state.stage = HellFlowStage.Boss;   // the boss test bed
            var scene = HellMusicStageRules.SceneFor(state);
            if (scene != Scene) EnterScene(scene, dsp);

            TickInRun(dsp, dt);
            TickBoss(dsp);
            TickDecision(ending);
            TickOverlays(dsp);
            ApplyVolumes(dsp);
        }

        void EnterScene(HellMusicScene scene, double dsp)
        {
            var from = Scene;
            Scene = scene;
            if (scene == HellMusicScene.Title && from != HellMusicScene.Silence) LoadManifest();
            if (scene == HellMusicScene.Opening) _openingPart = null;
            if (from == HellMusicScene.Opening && scene == HellMusicScene.InRun && FallIntoInRun(dsp)) return;
            string cue = HellMusicStageRules.Resolve(Manifest, HellMusicStageRules.CueFor(scene, _endingId));

            bool inrun = cue == HellMusicManifest.InRun;
            bool boss = cue == HellMusicManifest.Boss && _boss != null && _boss.voices.Length > 0;
            if (inrun) StartGroup(_inrun, dsp); else StopGroup(_inrun, dsp);
            if (boss) { _bossPhase = 0; _bossKneel = false; StartGroup(_boss, dsp); } else StopGroup(_boss, dsp);
            _decided = false;
            if (scene == HellMusicScene.InRun && from != HellMusicScene.InRun && from != HellMusicScene.Escape) rules.Reset();
            PlayBed(inrun || boss ? null : cue, dsp);
        }

        /// <summary>The opening hands over to play: the "fall" stinger at T, the in-run set's clock at T + downbeat_sample / rate
        /// (Quiet or Normal, Still), so the stinger's hit lands on the set's first downbeat. The opening's loop stops at T.</summary>
        bool FallIntoInRun(double dsp)
        {
            var fall = HellMusicStageRules.OpeningFall(Manifest);
            if (fall == null || _inrun == null || _inrun.voices.Length == 0 || _inrun.running) return false;
            var clip = Resources.Load<AudioClip>(fall.resource);
            if (clip == null) return false;
            double t = dsp + StartLead;
            FadeOutVoice(_bedA, t, 0.03);
            FadeOutVoice(_bedB, t, 0.03);
            _bedCue = null;
            StopGroup(_boss, dsp);
            PlayStingerAt(clip, 0, t);
            _fallUntil = t + clip.length;
            rules.Reset();
            StartGroup(_inrun, dsp, HellMusicClock.IntoCueStart(t, fall.downbeatSample, Manifest.sampleRate), true);
            return true;
        }

        // ------------------------------------------------------------------ groups (one clock each)

        void StartGroup(Group g, double dsp) => StartGroup(g, dsp, dsp + StartLead, false);

        /// <summary>Starts a group's clock at <paramref name="clockAt"/> (dsp); <paramref name="full"/>: at full gain from its first
        /// sample (no scene fade: a stinger hands over onto its downbeat).</summary>
        void StartGroup(Group g, double dsp, double clockAt, bool full)
        {
            if (g == null || g.voices.Length == 0) return;
            if (full) g.gain = HellMusicFade.Hold(1f); else g.gain.Retarget(1f, dsp, SceneFade);
            if (g.running) { foreach (var v in g.voices) v.stopAt = -1; return; }
            g.running = true;
            g.clockStart = clockAt;
            foreach (var v in g.voices)
            {
                v.src.Stop();
                v.src.timeSamples = 0;
                v.src.PlayScheduled(g.clockStart);
                v.playing = true;
                v.stopAt = -1;
                v.fade = HellMusicFade.Hold(0f);
            }
            if (g == _inrun) { _appliedMood = (HellMusicMood)(-1); }
        }

        void StopGroup(Group g, double dsp)
        {
            if (g == null || !g.running) return;
            g.gain.Retarget(0f, dsp, SceneFade);
            foreach (var v in g.voices) v.stopAt = dsp + SceneFade + StopAfter;
        }

        void TickGroupStops(Group g, double dsp)
        {
            if (g == null || !g.running) return;
            bool any = false;
            foreach (var v in g.voices)
            {
                if (v.stopAt >= 0 && dsp >= v.stopAt) { v.src.Stop(); v.playing = false; v.stopAt = -1; }
                any |= v.playing;
            }
            if (!any) g.running = false;
        }

        void TickInRun(double dsp, float dt)
        {
            TickGroupStops(_inrun, dsp);
            if (_inrun == null || !_inrun.running) return;
            var i = Inputs();
            rules.Step(i, dt, _inrun.barSeconds);
            var mood = rules.Mood; var speed = rules.Speed;
            if (mood == _appliedMood && speed == _appliedSpeed) return;
            bool first = (int)_appliedMood < 0;
            double bar = first ? _inrun.clockStart : HellMusicClock.NextLine(_inrun.clockStart, _inrun.barSeconds, dsp);
            double beat = first ? _inrun.clockStart : HellMusicClock.NextLine(_inrun.clockStart, _inrun.beatSeconds, dsp);
            double moodFade = first ? 0.0 : HellMusicCellRules.MoodFadeSeconds(_appliedMood, mood);
            foreach (var v in _inrun.voices)
            {
                float target = HellMusicCellRules.StemTarget(v.stem, mood, speed);
                if (Math.Abs(target - v.fade.Target) < 1e-4f) continue;
                bool moodChange = v.stem.mood != (int)_appliedMood || v.stem.mood != (int)mood;
                if (moodChange || first) v.fade.Retarget(target, bar, moodFade);
                else v.fade.Retarget(target, beat, first ? 0.0 : target > 0f ? HellMusicCellRules.SpeedInSeconds : HellMusicCellRules.SpeedOutSeconds);
            }
            _appliedMood = mood; _appliedSpeed = speed;
        }

        HellMusicInputs Inputs()
        {
            var i = new HellMusicInputs { health01 = 1f, hourLeft = -1f, forceDanger = Scene == HellMusicScene.Escape || Scene == HellMusicScene.Boss };
            for (int n = 0; n < SkylineBot.Live.Count; n++)
            {
                var b = SkylineBot.Live[n];
                if (b == null || !b.IsAwake || b.Brain == null) continue;
                var s = b.Brain.State;
                if (s != BotState.Idle && s != BotState.Wrecked) i.enemiesAwake++;
            }
            if (_player != null)
            {
                var a = _player.Armor;
                if (a != null) i.health01 = (a.Plating + a.Chassis) / Mathf.Max(1f, a.PlatingMax + a.ChassisMax);
                var v = _player.Velocity; v.y = 0f;
                i.speed = v.magnitude;
            }
            var room = _run != null ? _run.Current : null;
            var obj = room != null ? room.Objective : null;
            if (obj != null && !obj.Done)
            {
                i.objectiveRunning = true;
                i.objectiveImminent = (obj.Kind == HellRoomKind.HoldBrand || obj.Kind == HellRoomKind.Collapse) && obj.Progress >= 0.85f;
            }
            var run = _run != null ? _run.Run : null;
            if (run != null) i.falling = run.Phase == HellRunPhase.Dropping || run.Phase == HellRunPhase.Shaft;
            var hour = HellHour.Current;
            if (hour != null && !hour.Stopped) i.hourLeft = Mathf.Max(0f, hour.Left);
            return i;
        }

        void TickBoss(double dsp)
        {
            TickGroupStops(_boss, dsp);
            if (_boss == null || !_boss.running || Scene != HellMusicScene.Boss) return;
            var f = HellVesperFight.Instance != null ? HellVesperFight.Instance.Fight : null;
            if (f == null) return;
            if (f.Defeated)
            {
                if (_bossKneel) return;
                _bossKneel = true;
                StopGroup(_boss, dsp);
                StingPart(_boss.cue, "kneel");
                return;
            }
            int phase = Mathf.Clamp(f.S.phase, 1, 3);
            if (phase == _bossPhase) return;
            bool first = _bossPhase == 0;
            double at = first ? _boss.clockStart : HellMusicClock.NextLine(_boss.clockStart, _boss.barSeconds, dsp);
            _bossPhase = phase;
            string bed = HellMusicStageRules.BossBed(phase), drums = HellMusicStageRules.BossDrums(phase);
            foreach (var v in _boss.voices)
                v.fade.Retarget(v.stem.id == bed || v.stem.id == drums ? 1f : 0f, at, first ? 0.0 : SceneFade);
        }

        void TickDecision(HellEndingDirector ending)
        {
            if (Scene != HellMusicScene.Decision || _decided || ending == null || ending.Decision == null) return;
            var st = ending.Decision.State;
            if (st == HellDecisionState.Waiting) return;
            _decided = true;
            var cue = Manifest.Ready(HellMusicManifest.Decision);
            if (st == HellDecisionState.Killed) { StopBed(AudioSettings.dspTime, 0.02); StingPart(cue, "kill"); }
            else { StopBed(AudioSettings.dspTime, SceneFade); StingPart(cue, "spare"); }
        }

        // ------------------------------------------------------------------ beds and overlays

        void PlayBed(string cueId, double dsp)
        {
            if (cueId == _bedCue && cueId != null) return;
            _bedCue = cueId;
            StopBed(dsp, SceneFade);
            var cue = Manifest.Ready(cueId);
            string res = cue == null ? null : cue.kind == "parts" ? FirstLoopPart(cue) : cue.resource;
            var clip = res != null ? Resources.Load<AudioClip>(res) : null;
            if (clip == null) return;
            var v = _bedAOn ? _bedB : _bedA;
            _bedAOn = !_bedAOn;
            v.src.Stop();
            v.src.clip = clip;
            v.src.loop = cue.kind != "oneshot";
            v.src.timeSamples = 0;
            v.src.PlayScheduled(dsp + StartLead);
            v.playing = true; v.stopAt = -1;
            v.fade = HellMusicFade.Hold(0f);
            v.fade.Retarget(1f, dsp + StartLead, SceneFade);
        }

        static string FirstLoopPart(HellMusicCue cue)
        {
            foreach (var kv in cue.parts) if (kv.Value.kind == "loop" && kv.Value.resource != null) return kv.Value.resource;
            return null;
        }

        void StopBed(double dsp, double fade)
        {
            FadeOutVoice(_bedA, dsp, fade);
            FadeOutVoice(_bedB, dsp, fade);
        }

        static void FadeOutVoice(Voice v, double dsp, double fade)
        {
            if (!v.playing) return;
            v.fade.Retarget(0f, dsp, fade);
            v.stopAt = dsp + fade + StopAfter;
        }

        void TickOverlays(double dsp)
        {
            bool paused = SkylinePauseMenu.IsOpen;
            bool pick = HellSkillPick.Open && !paused;
            string want = paused ? HellMusicManifest.BajaHold : pick ? HellMusicManifest.BajaGift : null;
            if (want != _overlayCue)
            {
                _overlayCue = want;
                FadeOutVoice(_overlay, dsp, 0.3);
                var cue = Manifest.Ready(want);
                var clip = cue != null && cue.resource != null ? Resources.Load<AudioClip>(cue.resource) : null;
                if (clip != null)
                {
                    _overlay.src.Stop();
                    _overlay.src.clip = clip; _overlay.src.loop = true; _overlay.src.timeSamples = 0;   // restart from 0 at each open
                    _overlay.src.Play();
                    _overlay.playing = true; _overlay.stopAt = -1;
                    _overlay.fade = HellMusicFade.Hold(0f);
                    _overlay.fade.Retarget(1f, dsp, paused ? 0.15 : 0.5);
                }
            }
            if (pick != _wasPick && _inrun != null) _inrun.gain.Retarget(pick ? 0f : 1f, dsp, 0.5);
            _wasPick = pick; _wasPaused = paused;
            foreach (var v in _stingers) if (v.playing && !v.src.isPlaying) v.playing = false;
            TickStop(_bedA, dsp); TickStop(_bedB, dsp); TickStop(_overlay, dsp);
        }

        static void TickStop(Voice v, double dsp)
        {
            if (v.stopAt >= 0 && dsp >= v.stopAt) { v.src.Stop(); v.playing = false; v.stopAt = -1; }
        }

        // ------------------------------------------------------------------ the mix

        void ApplyVolumes(double dsp)
        {
            float bus = SkylineMix.CategoryGain[(int)SkylineSoundCategory.Music] * SkylineMix.UserGain(SkylineSoundCategory.Music);
            float dark = HellShadowAudio.Depth;
            float worldPitch = HellAbilityAudio.Pitch;
            bool paused = _wasPaused;
            float under = paused ? HellMusicMods.Db(HellMusicMods.PauseDb) : 1f;
            if (dsp > _duckUntil) _duck = 1f;
            float cutoff = HellMusicMods.Cutoff(dark, worldPitch, paused);
            float pitch = paused ? 1f : HellMusicMods.DilationPitch(worldPitch);
            float drums = DarkHush.MusicDrums(dark), drone = DarkHush.MusicDrone(dark);

            if (_inrun != null && _inrun.running)
            {
                float g = _inrun.gain.At(dsp) * under * _duck * bus;
                foreach (var v in _inrun.voices)
                {
                    float role = v.stem.role == HellMusicRole.Drums ? drums : 1f;
                    float stem = v.stem.role == HellMusicRole.Drone ? drone : v.fade.At(dsp);
                    Set(v, g * role * stem, pitch, cutoff);
                }
            }
            if (_boss != null && _boss.running)
            {
                float g = _boss.gain.At(dsp) * under * _duck * bus;
                foreach (var v in _boss.voices)
                    Set(v, g * v.fade.At(dsp) * (v.stem.role == HellMusicRole.Drums ? drums : 1f), pitch, cutoff);
            }
            Set(_bedA, _bedA.fade.At(dsp) * under * _duck * bus, pitch, cutoff);
            Set(_bedB, _bedB.fade.At(dsp) * under * _duck * bus, pitch, cutoff);
            Set(_overlay, _overlay.fade.At(dsp) * bus, 1f, HellMusicMods.OpenHz);
            foreach (var v in _stingers) Set(v, v.fade.At(dsp) * bus, 1f, HellMusicMods.OpenHz);
        }

        static void Set(Voice v, float volume, float pitch, float cutoff)
        {
            if (!v.playing) return;
            v.src.volume = volume;
            if (Mathf.Abs(v.src.pitch - pitch) > 1e-4f) v.src.pitch = pitch;
            if (Mathf.Abs(v.lp.cutoffFrequency - cutoff) > 1f) v.lp.cutoffFrequency = cutoff;
        }

        /// <summary>A one-line state for the perf/debug overlays.</summary>
        public override string ToString() => $"music {Scene} {rules.Mood}/{rules.Speed} bed={_bedCue} overlay={_overlayCue}";
    }
}
