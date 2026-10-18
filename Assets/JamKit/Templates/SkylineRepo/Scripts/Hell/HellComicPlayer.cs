using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The comic flip-through (docs/HELL-COMIC.md): plays a <see cref="HellComicSequence"/> as comic pages over everything
    /// but the pause menu. Each panel slides in, its caption and balloons appear one by one with their voices, the current
    /// panel drifts slowly, and pages turn with a flip. Click, Enter, Space or A moves on (or it moves on by itself after the
    /// voice); holding Escape or B skips the whole sequence. A missing picture is a dark ink panel with its description, so a
    /// sequence always plays. Every caption, balloon and splash title is sized to fit its box (no clipping at any aspect).
    /// Raises <see cref="HellRunEvents.ComicPage"/> on every page (the music follows it) and
    /// <see cref="HellRunEvents.ComicFinished"/> at the end. Unscaled time throughout.
    /// </summary>
    public sealed class HellComicPlayer : MonoBehaviour
    {
        static readonly Color PageColor = new Color(0.045f, 0.022f, 0.016f, 1f);
        static readonly Color InkColor = new Color(0.02f, 0.012f, 0.012f, 1f);
        static readonly Color EmberLine = new Color(1f, 0.42f, 0.13f, 0.38f);
        static readonly Color Ember = new Color(1f, 0.42f, 0.13f, 1f);
        static readonly Color EmberHot = new Color(1f, 0.78f, 0.42f, 1f);
        static readonly Color BalloonPaper = new Color(0.98f, 0.95f, 0.88f, 1f);
        static readonly Color CaptionPaper = new Color(0.86f, 0.74f, 0.52f, 1f);
        static readonly Color TextInk = new Color(0.08f, 0.04f, 0.035f, 1f);
        static readonly Color NameRed = new Color(0.55f, 0.08f, 0.04f, 1f);
        static readonly Color Ash = new Color(0.97f, 0.93f, 0.86f, 0.6f);
        static readonly Color PlaceholderInk = new Color(0.075f, 0.045f, 0.04f, 1f);
        static readonly Color Hatch = new Color(0.32f, 0.09f, 0.05f, 0.28f);
        const string Display = HellFaces.Heading, Fine = HellFaces.Fine;
        const float ImageAspect = 16f / 9f, Overscan = 1.1f, RevealSeconds = 0.42f, FlipSeconds = 0.6f, SkipHoldSeconds = 0.6f;

        public static HellComicPlayer Current { get; private set; }
        public static bool Playing => Current != null && !Current._finished;
        public string SequenceId => _seq != null ? _seq.id : null;
        /// <summary>The page on screen (0-based), -1 before the first.</summary>
        public int PageIndex => _pageIndex;

        // ---------------------------------------------------------------- per-element state
        sealed class LineView
        {
            public HellComicLine line;
            public VisualElement box;
            public Label name, text;
            public HellComicSpot spot;
            public float size, shownAt = -1f;
            public int tries;
        }

        sealed class PanelView
        {
            public HellComicPanel panel;
            public VisualElement root, image, frame, shade;
            public KeylinedLabel title;
            public Label placeholder;
            public readonly List<LineView> lines = new List<LineView>();
            public float revealedAt = -1f, drift, titleSize;
            public int titleTries;
            public bool hasPicture;
        }

        struct Step { public int page, panel, line; }

        UIDocument _doc;
        VisualElement _root, _backdrop, _stage, _hintBar, _skipFill;
        Label _hint;
        VisualElement _page, _oldPage;
        readonly List<PanelView> _panels = new List<PanelView>();
        readonly List<Step> _steps = new List<Step>();
        HellComicSequence _seq;
        Action _onDone;
        AudioSource _voice;
        bool _holdGame, _finished, _scripted, _pad, _wasBlocked;
        int _pageIndex = -1, _step = -1;
        float _stepAt, _stepDur, _flipAt = -1f, _skipHeld, _endAt = -1f, _rootAlpha;

        // ---------------------------------------------------------------- start

        /// <summary>Plays a sequence. <paramref name="holdGame"/>: hold the game still under it (the opening; the ending's
        /// director holds the game itself). <paramref name="onDone"/> runs when it ends or is skipped (the comic then fades
        /// out over what comes next).</summary>
        public static HellComicPlayer Play(HellComicSequence seq, Action onDone, bool holdGame)
        {
            if (Current != null) Current.Stop();
            var go = new GameObject("Hell Comic");
            go.SetActive(false);                       // the panel must be set before the document is enabled
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = Panel();
            doc.sortingOrder = 950f;                   // over the HUD, the ending cards (880) and the flow's screens (900); under the pause menu (1000)
            var p = go.AddComponent<HellComicPlayer>();
            p._doc = doc;
            p._seq = seq ?? new HellComicSequence("empty");
            p._onDone = onDone;
            p._holdGame = holdGame;
            p._scripted = Application.isBatchMode || SkylineRunArgs.Demo;
            Current = p;
            go.SetActive(true);
            return p;
        }

        static PanelSettings Panel()
        {
            var hud = SkylineHud.Instance != null ? SkylineHud.Instance : FindFirstObjectByType<SkylineHud>();
            if (hud != null)
            {
                var hd = hud.GetComponent<UIDocument>();
                if (hd != null && hd.panelSettings != null) return hd.panelSettings;
            }
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.match = 0.5f;
            return panel;
        }

        void OnEnable()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_root != null || _doc == null || _doc.rootVisualElement == null) return;
            Build(_doc.rootVisualElement);
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            _voice.ignoreListenerPause = true;
            _wasBlocked = SkylinePauseMenu.Blocked;
            SkylinePauseMenu.Blocked = true;           // Escape is the skip here
            foreach (var pair in Flatten(_seq)) _steps.Add(pair);
            Debug.Log("HELL_COMIC start " + _seq.id + " (" + _seq.pages.Count + " pages, " + _seq.PanelCount + " panels)");
            if (_steps.Count == 0) Finish(false);
            else NextStep();
        }

        static IEnumerable<Step> Flatten(HellComicSequence s)
        {
            for (int p = 0; p < s.pages.Count; p++)
                for (int n = 0; n < s.pages[p].panels.Count && n < HellComicLayout.MaxPanels; n++)
                {
                    yield return new Step { page = p, panel = n, line = -1 };
                    var lines = s.pages[p].panels[n].lines;
                    for (int l = 0; l < lines.Count; l++) yield return new Step { page = p, panel = n, line = l };
                }
        }

        /// <summary>Ends it now without the callback (another sequence replaces it, or the scene goes).</summary>
        public void Stop()
        {
            _onDone = null;
            if (!_finished) Finish(true);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (!_finished) SkylinePauseMenu.Blocked = _wasBlocked;
        }

        // ---------------------------------------------------------------- building

        static VisualElement Full(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        void Build(VisualElement docRoot)
        {
            _root = Full(new VisualElement { name = "hell-comic", pickingMode = PickingMode.Ignore });
            docRoot.Add(_root);
            _backdrop = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _backdrop.style.backgroundColor = PageColor;
            _backdrop.generateVisualContent += DrawVignette;
            _root.Add(_backdrop);
            _stage = new VisualElement { pickingMode = PickingMode.Ignore };
            _stage.style.position = Position.Absolute;
            _stage.style.left = Length.Percent(2.2f);
            _stage.style.right = Length.Percent(2.2f);
            _stage.style.top = Length.Percent(3.6f);
            _stage.style.bottom = Length.Percent(7.5f);
            _root.Add(_stage);

            _hintBar = new VisualElement { pickingMode = PickingMode.Ignore };
            _hintBar.style.position = Position.Absolute;
            _hintBar.style.right = Length.Percent(2.2f);
            _hintBar.style.bottom = Length.Percent(1.8f);
            _hintBar.style.alignItems = Align.FlexEnd;
            _root.Add(_hintBar);
            _hint = new Label { pickingMode = PickingMode.Ignore };
            SkylineHudStyle.ZeroBox(_hint.style);
            SkylineHudStyle.ApplyFace(_hint.style, Fine);
            _hint.style.fontSize = 22f;
            _hint.style.color = Ash;
            _hint.style.whiteSpace = WhiteSpace.NoWrap;
            _hintBar.Add(_hint);
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.height = 3f;
            track.style.width = Length.Percent(100f);
            track.style.marginTop = 4f;
            track.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            _hintBar.Add(track);
            _skipFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _skipFill.style.height = 3f;
            _skipFill.style.width = Length.Percent(0f);
            _skipFill.style.backgroundColor = Ember;
            track.Add(_skipFill);
            SetHint();
            _root.style.opacity = 0f;
        }

        void SetHint() => _hint.text = _pad ? "A  NEXT      HOLD B  SKIP" : "CLICK / ENTER  NEXT      HOLD ESC  SKIP";

        void BuildPage(int index)
        {
            if (_page != null)
            {
                if (_oldPage != null) _oldPage.RemoveFromHierarchy();
                _oldPage = _page;
                _oldPage.BringToFront();
                _flipAt = Time.unscaledTime;
                var shade = Full(new VisualElement { name = "flip-shade", pickingMode = PickingMode.Ignore });
                shade.style.backgroundColor = Color.black;
                shade.style.opacity = 0f;
                _oldPage.Add(shade);
                _oldPage.style.transformOrigin = new TransformOrigin(Length.Percent(0f), Length.Percent(50f));
            }
            _panels.Clear();
            _lineByBox.Clear();
            _page = Full(new VisualElement { name = "comic-page", pickingMode = PickingMode.Ignore });
            _page.style.backgroundColor = PageColor;
            _stage.Add(_page);
            if (_oldPage != null) _oldPage.BringToFront();
            var page = _seq.pages[index];
            int count = Mathf.Min(page.panels.Count, HellComicLayout.MaxPanels);
            var rects = HellComicLayout.Rects(count);
            for (int i = 0; i < count; i++) _panels.Add(BuildPanel(page.panels[i], rects[i], i, count));
        }

        PanelView BuildPanel(HellComicPanel panel, HellComicRect r, int index, int count)
        {
            var v = new PanelView { panel = panel };
            var root = v.root = new VisualElement { name = "comic-panel", pickingMode = PickingMode.Ignore };
            root.style.position = Position.Absolute;
            root.style.left = Length.Percent(r.x * 100f);
            root.style.top = Length.Percent(r.y * 100f);
            root.style.width = Length.Percent(r.w * 100f);
            root.style.height = Length.Percent(r.h * 100f);
            root.style.overflow = Overflow.Hidden;
            root.style.backgroundColor = PlaceholderInk;
            root.style.opacity = 0f;
            _page.Add(root);

            var tex = string.IsNullOrEmpty(panel.image) ? null : Resources.Load<Texture2D>(HellComics.Root + panel.image);
            if (tex == null && !string.IsNullOrEmpty(panel.fallbackImage)) tex = Resources.Load<Texture2D>(HellComics.Root + panel.fallbackImage);
            v.hasPicture = tex != null;
            if (tex != null)
            {
                v.image = new VisualElement { pickingMode = PickingMode.Ignore };
                v.image.style.position = Position.Absolute;
                v.image.style.backgroundImage = new StyleBackground(tex);
                root.Add(v.image);
            }
            else
            {
                if (!string.IsNullOrEmpty(panel.image)) Debug.Log("HELL_COMIC placeholder " + panel.image + " (no Resources/" + HellComics.Root + panel.image + ")");
                var hatch = Full(new VisualElement { pickingMode = PickingMode.Ignore });
                int seed = (panel.image ?? "").GetHashCode();
                hatch.generateVisualContent += mgc => DrawHatch(mgc, seed);
                root.Add(hatch);
                if (!panel.Splash)
                {
                    v.placeholder = new Label(panel.description ?? "") { pickingMode = PickingMode.Ignore };
                    SkylineHudStyle.ZeroBox(v.placeholder.style);
                    SkylineHudStyle.ApplyFace(v.placeholder.style, Fine);
                    v.placeholder.style.position = Position.Absolute;
                    v.placeholder.style.left = Length.Percent(12f);
                    v.placeholder.style.right = Length.Percent(12f);
                    v.placeholder.style.top = Length.Percent(38f);
                    v.placeholder.style.whiteSpace = WhiteSpace.Normal;
                    v.placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                    v.placeholder.style.color = new Color(0.72f, 0.56f, 0.46f, 0.75f);
                    v.placeholder.style.fontSize = 26f;
                    root.Add(v.placeholder);
                }
            }

            if (panel.Splash)
            {
                v.shade = Full(new VisualElement { pickingMode = PickingMode.Ignore });
                v.shade.style.backgroundColor = new Color(0f, 0f, 0f, v.hasPicture ? 0.5f : 0.15f);
                root.Add(v.shade);
                var column = Full(new VisualElement { pickingMode = PickingMode.Ignore });
                column.style.alignItems = Align.Center;
                column.style.justifyContent = Justify.Center;
                root.Add(column);
                v.title = new KeylinedLabel(96f, 5f, Display);
                v.title.SetColor(Ember);
                v.title.SetWrap(true, TextAnchor.MiddleCenter);
                v.title.SetText(panel.title);
                column.Add(v.title);
            }

            bool hasCaption = false;
            foreach (var l in panel.lines) if (l.kind == HellComicLineKind.Caption) hasCaption = true;
            int balloon = 0;
            foreach (var l in panel.lines)
            {
                var spot = HellComicLayout.Spot(l, l.kind == HellComicLineKind.Balloon ? balloon : 0, hasCaption, HellComicLayout.CornerCovered(index, count));
                if (l.kind == HellComicLineKind.Balloon) balloon++;
                v.lines.Add(BuildLine(root, l, spot));
            }

            v.frame = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            int frameSeed = ((panel.image ?? "") + index).GetHashCode();
            v.frame.generateVisualContent += mgc => DrawInkFrame(mgc, frameSeed);
            root.Add(v.frame);

            root.RegisterCallback<GeometryChangedEvent>(_ => Fit(v));
            return v;
        }

        LineView BuildLine(VisualElement panelRoot, HellComicLine line, HellComicSpot spot)
        {
            var lv = new LineView { line = line, spot = spot };
            bool caption = line.kind == HellComicLineKind.Caption;
            var box = lv.box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.paddingLeft = box.style.paddingRight = caption ? 18f : 26f;
            box.style.paddingTop = caption ? 12f : 16f;
            box.style.paddingBottom = caption ? 12f : 18f;
            box.style.opacity = 0f;
            box.generateVisualContent += mgc => DrawBox(mgc, lv);
            if (!caption && !string.IsNullOrEmpty(line.name))
            {
                lv.name = new Label(line.name) { pickingMode = PickingMode.Ignore };
                SkylineHudStyle.ZeroBox(lv.name.style);
                SkylineHudStyle.ApplyFace(lv.name.style, Display);
                lv.name.style.color = NameRed;
                lv.name.style.letterSpacing = 3f;
                lv.name.style.marginBottom = 2f;
                box.Add(lv.name);
            }
            lv.text = new Label(line.text ?? "") { pickingMode = PickingMode.Ignore };
            SkylineHudStyle.ZeroBox(lv.text.style);
            SkylineHudStyle.ApplyFace(lv.text.style, Display);
            lv.text.style.whiteSpace = WhiteSpace.Normal;
            lv.text.style.color = TextInk;
            lv.text.style.unityTextAlign = caption ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            box.Add(lv.text);
            Anchor(lv);
            panelRoot.Add(box);
            return lv;
        }

        static void Anchor(LineView lv)
        {
            var s = lv.box.style;
            const float m = 3.5f;   // percent of the panel from its edge
            bool top = lv.spot == HellComicSpot.TopLeft || lv.spot == HellComicSpot.TopRight;
            bool left = lv.spot == HellComicSpot.TopLeft || lv.spot == HellComicSpot.BottomLeft;
            if (top) { s.top = Length.Percent(m + 1.5f); s.bottom = StyleKeyword.Auto; } else { s.bottom = Length.Percent(m + 1.5f); s.top = StyleKeyword.Auto; }
            if (left) { s.left = Length.Percent(m); s.right = StyleKeyword.Auto; } else { s.right = Length.Percent(m); s.left = StyleKeyword.Auto; }
        }

        // ---------------------------------------------------------------- fitting (no clipping at any aspect or size)

        void Fit(PanelView v)
        {
            float w = v.root.layout.width, h = v.root.layout.height;
            if (float.IsNaN(w) || w < 10f || h < 10f) return;
            if (v.image != null)
            {
                var c = HellComicLayout.Cover(w, h, ImageAspect, v.panel.focusX, v.panel.focusY, Overscan);
                v.image.style.width = c.w;
                v.image.style.height = c.h;
                v.image.style.left = c.left;
                v.image.style.top = c.top;
                v.drift = Mathf.Max(0f, (c.w - w) * 0.5f);
            }
            if (v.placeholder != null) v.placeholder.style.fontSize = HellComicFit.Size(v.placeholder.text, w * 0.76f, h * 0.5f, Mathf.Min(30f, h * 0.06f), 12f);
            if (v.title != null)
            {
                float size = HellComicFit.Size(v.panel.title, w * 0.84f, h * 0.6f, Mathf.Min(120f, h * 0.16f), 16f, 0.06f);
                if (v.titleTries > 0 && v.titleSize > 0f) size = Mathf.Min(size, v.titleSize);
                v.titleSize = size;
                SetTitleSize(v.title, size, w * 0.84f);
                v.title.UnregisterCallback<GeometryChangedEvent>(OnTitleGeometry);
                v.title.userData = v;
                v.title.RegisterCallback<GeometryChangedEvent>(OnTitleGeometry);
            }
            int balloons = 0;
            foreach (var lv in v.lines) if (lv.line.kind == HellComicLineKind.Balloon) balloons++;
            foreach (var lv in v.lines) FitLine(lv, w, h, balloons);
        }

        static void SetTitleSize(KeylinedLabel t, float size, float maxWidth)
        {
            t.style.maxWidth = maxWidth;
            foreach (var ch in t.Children())
            {
                ch.style.fontSize = size;
                ch.style.letterSpacing = size * 0.06f;
                ch.style.maxWidth = maxWidth;
            }
        }

        void OnTitleGeometry(GeometryChangedEvent e)
        {
            var t = e.target as KeylinedLabel;
            if (t == null || !(t.userData is PanelView v) || v.titleTries > 8) return;
            float w = v.root.layout.width, h = v.root.layout.height;
            var f = t.Front.layout;
            if (f.height > h * 0.7f || f.width > w * 0.9f)
            {
                v.titleTries++;
                v.titleSize = Mathf.Max(12f, v.titleSize * 0.88f);
                SetTitleSize(t, v.titleSize, w * 0.84f);
            }
        }

        void FitLine(LineView lv, float w, float h, int balloons)
        {
            bool caption = lv.line.kind == HellComicLineKind.Caption;
            float maxW = w * (caption ? 0.62f : 0.48f);
            float maxH = h * (caption ? 0.3f : balloons > 1 ? 0.38f : 0.44f);
            float padX = caption ? 36f : 52f, padY = caption ? 24f : 34f;
            float nameH = lv.name != null ? 30f : 0f;
            float textW = Mathf.Max(40f, maxW - padX), textH = Mathf.Max(20f, maxH - padY - nameH);
            float size = HellComicFit.Size(lv.line.text, textW, textH, Mathf.Clamp(h * 0.062f, 18f, 40f), 11f);
            if (lv.tries > 0 && lv.size > 0f) size = Mathf.Min(size, lv.size);
            lv.size = size;
            lv.box.style.maxWidth = maxW;
            lv.text.style.fontSize = size;
            lv.text.style.maxWidth = textW;
            if (lv.name != null) lv.name.style.fontSize = Mathf.Max(11f, size * 0.62f);
            lv.box.UnregisterCallback<GeometryChangedEvent>(OnLineGeometry);
            lv.box.userData = new Vector3(w, h, maxH);
            lv.box.RegisterCallback<GeometryChangedEvent>(OnLineGeometry);
            lv.box.MarkDirtyRepaint();
            _lineByBox[lv.box] = lv;
        }

        readonly Dictionary<VisualElement, LineView> _lineByBox = new Dictionary<VisualElement, LineView>();

        void OnLineGeometry(GeometryChangedEvent e)
        {
            var box = e.target as VisualElement;
            if (box == null || !_lineByBox.TryGetValue(box, out var lv) || !(box.userData is Vector3 room)) return;
            box.MarkDirtyRepaint();
            if (lv.tries > 8) return;
            var b = box.layout;
            if (b.height > room.z || b.width > room.x * 0.9f)
            {
                lv.tries++;
                lv.size = Mathf.Max(10f, lv.size * 0.88f);
                lv.text.style.fontSize = lv.size;
                if (lv.name != null) lv.name.style.fontSize = Mathf.Max(10f, lv.size * 0.62f);
            }
        }

        // ---------------------------------------------------------------- drawing

        static void DrawVignette(MeshGenerationContext mgc)
        {
            var r = mgc.visualElement.contentRect;
            var p = mgc.painter2D;
            p.strokeColor = new Color(0f, 0f, 0f, 0.35f);
            p.lineWidth = Mathf.Min(r.width, r.height) * 0.08f;
            p.BeginPath();
            p.MoveTo(new Vector2(0f, 0f)); p.LineTo(new Vector2(r.width, 0f)); p.LineTo(new Vector2(r.width, r.height)); p.LineTo(new Vector2(0f, r.height));
            p.ClosePath();
            p.Stroke();
        }

        static void DrawHatch(MeshGenerationContext mgc, int seed)
        {
            var r = mgc.visualElement.contentRect;
            if (r.width < 2f) return;
            var p = mgc.painter2D;
            var rng = new System.Random(seed);
            p.strokeColor = Hatch;
            p.lineWidth = 2f;
            float step = 26f;
            for (float x = -r.height; x < r.width; x += step)
            {
                float j = (float)rng.NextDouble() * 6f;
                p.BeginPath();
                p.MoveTo(new Vector2(x + j, r.height));
                p.LineTo(new Vector2(x + r.height + j, 0f));
                p.Stroke();
            }
            p.strokeColor = new Color(Hatch.r, Hatch.g, Hatch.b, 0.12f);
            for (float x = 0f; x < r.width + r.height; x += step * 1.7f)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(x, r.height));
                p.LineTo(new Vector2(x - r.height, 0f));
                p.Stroke();
            }
        }

        /// <summary>A hand-inked border: a thick, slightly wobbling black line, and a faint ember keyline inside it.</summary>
        static void DrawInkFrame(MeshGenerationContext mgc, int seed)
        {
            var r = mgc.visualElement.contentRect;
            if (r.width < 4f) return;
            var p = mgc.painter2D;
            var rng = new System.Random(seed);
            float ink = 7f;
            p.strokeColor = InkColor;
            p.lineWidth = ink * 2f;
            p.lineJoin = LineJoin.Miter;
            p.BeginPath();
            Wobble(p, rng, new Vector2(0f, 0f), new Vector2(r.width, 0f), true);
            Wobble(p, rng, new Vector2(r.width, 0f), new Vector2(r.width, r.height), false);
            Wobble(p, rng, new Vector2(r.width, r.height), new Vector2(0f, r.height), false);
            Wobble(p, rng, new Vector2(0f, r.height), new Vector2(0f, 0f), false);
            p.ClosePath();
            p.Stroke();
            p.strokeColor = EmberLine;
            p.lineWidth = 1.5f;
            p.BeginPath();
            p.MoveTo(new Vector2(ink + 2f, ink + 2f)); p.LineTo(new Vector2(r.width - ink - 2f, ink + 2f));
            p.LineTo(new Vector2(r.width - ink - 2f, r.height - ink - 2f)); p.LineTo(new Vector2(ink + 2f, r.height - ink - 2f));
            p.ClosePath();
            p.Stroke();
        }

        static void Wobble(Painter2D p, System.Random rng, Vector2 a, Vector2 b, bool first)
        {
            if (first) p.MoveTo(a);
            var n = new Vector2(-(b - a).y, (b - a).x).normalized;
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f;
                float j = i == 6 ? 0f : ((float)rng.NextDouble() - 0.5f) * 3.2f;
                p.LineTo(Vector2.Lerp(a, b, t) + n * j);
            }
        }

        /// <summary>A caption's box (parchment, square, inked) or a balloon (white, rounded, a tail toward the panel's middle).</summary>
        static void DrawBox(MeshGenerationContext mgc, LineView lv)
        {
            var r = mgc.visualElement.layout;
            float w = r.width, h = r.height;
            if (float.IsNaN(w) || w < 4f || h < 4f) return;
            var p = mgc.painter2D;
            p.lineWidth = 3f;
            p.strokeColor = InkColor;
            if (lv.line.kind == HellComicLineKind.Caption)
            {
                p.fillColor = CaptionPaper;
                p.BeginPath();
                p.MoveTo(new Vector2(0f, 0f)); p.LineTo(new Vector2(w, 1.5f)); p.LineTo(new Vector2(w - 1f, h)); p.LineTo(new Vector2(1f, h - 1f));
                p.ClosePath();
                p.Fill();
                p.Stroke();
                return;
            }
            float rad = Mathf.Min(28f, h * 0.45f, w * 0.25f);
            bool top = lv.spot == HellComicSpot.TopLeft || lv.spot == HellComicSpot.TopRight;
            bool left = lv.spot == HellComicSpot.TopLeft || lv.spot == HellComicSpot.BottomLeft;
            // the tail: from the edge facing the panel's middle, toward it
            float baseX = left ? w * 0.7f : w * 0.3f;
            float tailLen = Mathf.Min(40f, h * 0.5f);
            Vector2 b0 = new Vector2(baseX - 14f, top ? h : 0f), b1 = new Vector2(baseX + 14f, top ? h : 0f);
            Vector2 tip = new Vector2(baseX + (left ? 26f : -26f), top ? h + tailLen : -tailLen);
            p.fillColor = BalloonPaper;
            p.BeginPath();
            p.MoveTo(new Vector2(rad, 0f));
            if (!top) { p.LineTo(b0); p.LineTo(tip); p.LineTo(b1); }
            p.ArcTo(new Vector2(w, 0f), new Vector2(w, h), rad);
            p.ArcTo(new Vector2(w, h), new Vector2(0f, h), rad);
            if (top) { p.LineTo(b1); p.LineTo(tip); p.LineTo(b0); }
            p.ArcTo(new Vector2(0f, h), new Vector2(0f, 0f), rad);
            p.ArcTo(new Vector2(0f, 0f), new Vector2(w, 0f), rad);
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }

        // ---------------------------------------------------------------- the frame

        void Update()
        {
            if (_root == null) return;
            if (_holdGame && !_finished) Hold();
            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            if (_finished)
            {
                _rootAlpha = Mathf.MoveTowards(_rootAlpha, 0f, dt / 0.45f);
                _root.style.opacity = _rootAlpha;
                if (_rootAlpha <= 0f) Destroy(gameObject);
                return;
            }
            _rootAlpha = Mathf.MoveTowards(_rootAlpha, 1f, dt / 0.35f);
            _root.style.opacity = _rootAlpha;

            TrackDevice();
            bool menu = SkylinePauseMenu.IsOpen;
            if (!menu && SkipHeld()) _skipHeld += dt; else _skipHeld = Mathf.Max(0f, _skipHeld - dt * 3f);
            _skipFill.style.width = Length.Percent(Mathf.Clamp01(_skipHeld / SkipHoldSeconds) * 100f);
            if (_skipHeld >= SkipHoldSeconds) { Debug.Log("HELL_COMIC skipped " + _seq.id); Finish(false); return; }

            TickFlip(now);
            TickPanels(now);

            if (menu) { _stepAt += dt; return; }      // the pause menu holds the comic's clock
            bool flipping = _flipAt >= 0f && now - _flipAt < Flip;
            if (flipping) { _stepAt = now; return; }
            float shown = now - _stepAt;
            if ((shown > 0.25f && AdvancePressed()) || shown >= _stepDur) NextStep();
        }

        float Flip => _scripted ? 0.1f : FlipSeconds;

        void LateUpdate()
        {
            if (_holdGame && !_finished) Hold();
        }

        void Hold()
        {
            if (SkylinePauseMenu.IsOpen) return;
            SkylineDeviceInput.Paused = true;
            Time.timeScale = 0f;
            if (!Application.isBatchMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
            }
        }

        void TickFlip(float now)
        {
            if (_oldPage == null) return;
            float k = Mathf.Clamp01((now - _flipAt) / Flip);
            float e = k * k * (3f - 2f * k);
            _oldPage.style.scale = new Scale(new Vector3(Mathf.Max(0.001f, 1f - e), 1f, 1f));
            _oldPage.style.translate = new Translate(Length.Percent(-2f * e), 0f);
            var shade = _oldPage.Q("flip-shade");
            if (shade != null) shade.style.opacity = 0.85f * e;
            if (k >= 1f)
            {
                _oldPage.RemoveFromHierarchy();
                _oldPage = null;
            }
        }

        void TickPanels(float now)
        {
            var cur = _step >= 0 && _step < _steps.Count ? _steps[_step] : default;
            for (int i = 0; i < _panels.Count; i++)
            {
                var v = _panels[i];
                if (v.revealedAt < 0f) { v.root.style.opacity = 0f; continue; }
                float t = now - v.revealedAt;
                float k = Mathf.Clamp01(t / RevealSeconds);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                v.root.style.opacity = e;
                float sc = 1.045f - 0.045f * e;
                v.root.style.scale = new Scale(new Vector3(sc, sc, 1f));
                v.root.style.translate = new Translate(28f * (1f - e), 10f * (1f - e));
                if (v.image != null && i == cur.panel)
                {
                    // the parallax drift: the current panel's picture slides slowly across its overscan
                    float d = Mathf.Clamp01(t / 14f);
                    d = d * d * (3f - 2f * d);
                    float dx = Mathf.Lerp(v.drift * 0.7f, -v.drift * 0.7f, d);
                    v.image.style.translate = new Translate(dx, -v.drift * 0.15f * d);
                }
                foreach (var lv in v.lines)
                {
                    if (lv.shownAt < 0f) { lv.box.style.opacity = 0f; continue; }
                    float lk = Mathf.Clamp01((now - lv.shownAt) / 0.18f);
                    lv.box.style.opacity = lk;
                    float ls = 0.86f + 0.14f * (1f - (1f - lk) * (1f - lk));
                    lv.box.style.scale = new Scale(new Vector3(ls, ls, 1f));
                }
            }
        }

        // ---------------------------------------------------------------- the steps

        void NextStep()
        {
            HushVoice();
            _step++;
            if (_step >= _steps.Count) { Finish(false); return; }
            var s = _steps[_step];
            float now = Time.unscaledTime;
            if (s.page != _pageIndex)
            {
                _pageIndex = s.page;
                BuildPage(s.page);
                if (_oldPage != null) SkylineSounds.PlayViewmodel(HellEndingCues.Card);
                Debug.Log("HELL_COMIC page " + _seq.id + " " + s.page);
                HellRunEvents.RaiseComicPage(_seq.id, s.page);
            }
            _stepAt = now;
            var panel = _seq.pages[s.page].panels[s.panel];
            var view = s.panel < _panels.Count ? _panels[s.panel] : null;
            if (s.line < 0)
            {
                if (view != null) view.revealedAt = now + (_oldPage != null ? Flip : 0f);
                if (!panel.Splash && _oldPage == null && s.panel > 0) SkylineSounds.PlayViewmodel(HellEndingCues.Card, 0.5f);
                _stepDur = panel.lines.Count > 0 ? RevealSeconds + 0.15f : RevealSeconds + Mathf.Max(panel.Splash ? 2.4f : 1.6f, panel.hold);
            }
            else
            {
                var line = panel.lines[s.line];
                if (view != null && s.line < view.lines.Count) view.lines[s.line].shownAt = now;
                float clip = PlayVoice(line);
                _stepDur = HellComics.LineSeconds(line, clip) + (s.line == panel.lines.Count - 1 ? panel.hold : 0f);
            }
            if (_scripted) _stepDur = Mathf.Min(_stepDur, 0.3f);
        }

        void Finish(bool silent)
        {
            if (_finished) return;
            _finished = true;
            HushVoice();
            SkylinePauseMenu.Blocked = _wasBlocked;
            if (_holdGame)
            {
                SkylineDeviceInput.Paused = false;
                Time.timeScale = 1f;
                HellTime.Apply();
                if (!Application.isBatchMode)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            if (_seq != null) Debug.Log("HELL_COMIC done " + _seq.id);
            if (silent) return;
            if (_seq != null) HellRunEvents.RaiseComicFinished(_seq.id);
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        // ---------------------------------------------------------------- voices and input

        float PlayVoice(HellComicLine line)
        {
            if (line == null || !line.Voiced || _voice == null) return 0f;
            var clip = HellLinePlayer.LoadVoice(line.speaker, line.voice);
            if (clip == null) return 0f;
            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = SkylineMix.UserGain(SkylineSoundCategory.Voices) * SkylineMix.CategoryGain[(int)SkylineSoundCategory.Voices];
            _voice.Play();
            SkylineSounds.NoteSpeech(_voice, clip.length);
            return clip.length;
        }

        void HushVoice()
        {
            if (_voice != null) _voice.Stop();
        }

        void TrackDevice()
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool padNow = pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
            bool kbmNow = (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (padNow && !_pad) { _pad = true; SetHint(); }
            else if (kbmNow && _pad) { _pad = false; SetHint(); }
        }

        static bool AdvancePressed()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        static bool SkipHeld()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.escapeKey.isPressed || kb.backspaceKey.isPressed))
                || (pad != null && (pad.buttonEast.isPressed || pad.startButton.isPressed));
        }
    }
}
