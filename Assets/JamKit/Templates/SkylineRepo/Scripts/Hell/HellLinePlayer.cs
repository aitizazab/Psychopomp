using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Says the story beats' and tutorials' lines (docs/HELL-BEATS.md): the subtitle through the HUD's
    /// <see cref="SkylineHudSubtitles"/>, and the voice clip <c>Resources/SkylineAudio/hell-voice/&lt;speaker&gt;/&lt;key&gt;</c>
    /// when one exists (else text only). Also draws the tutorial's hint card (under the crosshair) and the beats' letterbox
    /// bars. Everything is built inside the HUD, so the capture lane's clean-picture switch hides it with the HUD, and the
    /// voice takes the player's Voices volume (<see cref="SkylineMix.UserGain"/>). One per scene, made on first use.
    /// </summary>
    public sealed class HellLinePlayer : MonoBehaviour
    {
        public const string VoiceFolder = "SkylineAudio/hell-voice/";
        public const float HintPx = 30f, LetterboxPercent = HellHudPlaces.LetterboxShare * 100f, LetterboxSeconds = 0.35f;
        /// <summary>The hint card's top, in percent of the screen (the layout's hint box, under the decision prompt; docs/HELL-UI.md).</summary>
        public static float HintTopPercent => HellHudPlaces.HintTopPercent;
        static readonly Color HintColour = new Color(1f, 0.86f, 0.62f);

        static HellLinePlayer _instance;
        public static HellLinePlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HellLinePlayer");
                    _instance = go.AddComponent<HellLinePlayer>();
                }
                return _instance;
            }
        }

        AudioSource _voice;
        /// <summary>One source per speaker for the voiced cards and barks (<see cref="PlayVoice"/>): a new line of a speaker
        /// stops that speaker's last one.</summary>
        readonly System.Collections.Generic.Dictionary<string, AudioSource> _speakerVoice = new System.Collections.Generic.Dictionary<string, AudioSource>();
        readonly System.Collections.Generic.Dictionary<string, float> _speakerUntil = new System.Collections.Generic.Dictionary<string, float>();
        VisualElement _hintBox, _barTop, _barBottom;
        KeylinedLabel _hint;
        float _letterbox, _letterboxTarget, _voiceUntil;
        string _hintText = "", _voiceSpeaker = "";

        void Awake()
        {
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            _voice.ignoreListenerPause = false;
        }

        void OnDestroy() { if (_instance == this) _instance = null; }

        // ------------------------------------------------------------------ lines

        /// <summary>Says <paramref name="key"/>: subtitle, plus its voice clip if there is one. Returns how long it lasts
        /// (unscaled seconds; 0 when the key is unknown).</summary>
        public float Say(string key)
        {
            var line = HellLines.Get(key);
            if (line == null)
            {
                Debug.LogWarning("HELL_LINES no line " + key);
                return 0f;
            }
            var clip = line.speaker != HellSpeakers.Hint ? LoadVoice(line.speaker, key) : null;
            float seconds = HellLines.Seconds(line.text, clip != null ? clip.length : 0f);
            if (clip != null)
            {
                if (_speakerVoice.TryGetValue(line.speaker, out var other) && other != null) other.Stop();
                _voiceSpeaker = line.speaker;
                _voice.Stop();
                _voice.clip = clip;
                _voice.volume = VoiceGain();
                _voice.Play();
                _voiceUntil = Time.unscaledTime + clip.length;
                SkylineSounds.NoteSpeech(_voice, clip.length);
            }
            var hud = SkylineHud.Instance;
            if (hud != null) hud.Subtitles.Say(HellSpeakers.Display(line.speaker), line.text, false, seconds);
            Debug.Log("HELL_LINE " + key + (clip != null ? " (voiced)" : ""));
            return seconds;
        }

        /// <summary>Plays the voice clip <c>hell-voice/&lt;speaker&gt;/&lt;key&gt;</c> with no subtitle (the caller shows its own
        /// text: the job cards, Vesper's fight barks). Stops the speaker's previous line first (here and in <see cref="Say"/>).
        /// A missing clip plays nothing. Returns the clip's length (0 when there is none).</summary>
        public float PlayVoice(string speaker, string key)
        {
            var clip = LoadVoice(speaker, key);
            if (clip == null) return 0f;
            if (!_speakerVoice.TryGetValue(speaker, out var src) || src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.ignoreListenerPause = false;
                _speakerVoice[speaker] = src;
            }
            if (_voice != null && _voice.isPlaying && _voiceSpeaker == speaker) _voice.Stop();
            src.Stop();
            src.clip = clip;
            src.volume = VoiceGain();
            src.Play();
            _speakerUntil[speaker] = Time.unscaledTime + clip.length;
            SkylineSounds.NoteSpeech(src, clip.length);
            Debug.Log("HELL_VOICE " + speaker + "/" + key);
            return clip.length;
        }

        /// <summary>Stops a voice line now (its subtitle too).</summary>
        public void Hush()
        {
            foreach (var kv in _speakerVoice) if (kv.Value != null) kv.Value.Stop();
            if (_voice != null) _voice.Stop();
            if (SkylineHud.Instance != null) SkylineHud.Instance.EndSay();
        }

        public static AudioClip LoadVoice(string speaker, string key)
        {
            string folder = VoiceFolder + speaker + "/";
            var c = Resources.Load<AudioClip>(folder + key);
            if (c == null) c = Resources.Load<AudioClip>(folder + key.Replace('.', '_'));
            if (c == null) c = Resources.Load<AudioClip>(folder + key.Replace('.', '-'));
            return c;
        }

        static float VoiceGain() =>
            SkylineMix.UserGain(SkylineSoundCategory.Voices) * SkylineMix.CategoryGain[(int)SkylineSoundCategory.Voices];

        // ------------------------------------------------------------------ the hint card and the letterbox

        public void ShowHint(string text)
        {
            if (!Build()) return;
            text = text ?? "";
            if (text != _hintText) { _hintText = text; _hint.SetText(text); }
            _hintBox.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void HideHint() => ShowHint("");

        public void Letterbox(bool on) { _letterboxTarget = on ? 1f : 0f; Build(); }

        bool Build()
        {
            if (_hint != null) return true;
            var hud = SkylineHud.Instance;
            if (hud == null || hud.Safe == null || hud.Root == null) return false;
            _hintBox = SkylineHudStyle.Layer(hud.Safe, "hell-tutorial-hint");
            _hintBox.style.left = 0;
            _hintBox.style.right = 0;
            _hintBox.style.top = Length.Percent(HintTopPercent);
            _hintBox.style.alignItems = Align.Center;
            _hint = new KeylinedLabel(HintPx, 2.5f, SkylineHudStyle.AmmoFontDefault);
            _hint.SetColor(HintColour);
            _hintBox.Add(_hint);
            _hintBox.style.display = DisplayStyle.None;

            _barTop = Bar(hud.Root, "hell-letterbox-top", true);
            _barBottom = Bar(hud.Root, "hell-letterbox-bottom", false);
            return true;
        }

        static VisualElement Bar(VisualElement root, string name, bool top)
        {
            var b = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            b.style.position = Position.Absolute;
            b.style.left = 0;
            b.style.right = 0;
            if (top) b.style.top = 0; else b.style.bottom = 0;
            b.style.height = Length.Percent(0);
            b.style.backgroundColor = Color.black;
            root.Add(b);
            return b;
        }

        void Update()
        {
            if (_voice != null && _voice.isPlaying)
            {
                _voice.volume = VoiceGain();
                if (Time.unscaledTime > _voiceUntil + 0.5f) _voice.Stop();
            }
            foreach (var kv in _speakerVoice)
            {
                var src = kv.Value;
                if (src == null || !src.isPlaying) continue;
                src.volume = VoiceGain();
                if (_speakerUntil.TryGetValue(kv.Key, out var until) && Time.unscaledTime > until + 0.5f) src.Stop();
            }
            if (_hintBox != null)
            {
                float o = HellAbilityWheel.IsOpen ? 0f : 1f;   // the wheel has the middle of the screen
                if (_hintBox.style.opacity != o) _hintBox.style.opacity = o;
            }
            if (_barTop == null) { if (_letterboxTarget > 0f) Build(); return; }
            if (Mathf.Approximately(_letterbox, _letterboxTarget)) return;
            _letterbox = Mathf.MoveTowards(_letterbox, _letterboxTarget, Time.unscaledDeltaTime / LetterboxSeconds);
            float e = _letterbox * _letterbox * (3f - 2f * _letterbox);
            _barTop.style.height = Length.Percent(LetterboxPercent * e);
            _barBottom.style.height = Length.Percent(LetterboxPercent * e);
        }
    }
}
