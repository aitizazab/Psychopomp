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
    /// The full-screen screens of the Hell flow (docs/HELL-FLOW.md): the title (Start, Settings, Quit), the opening's text
    /// cards, a floor's intro card, the credits and the stats. Dark and ember: a black-to-ember gradient, drifting embers, the
    /// HUD's keylined display faces. Words come from <see cref="HellFlowText"/>. While a screen shows, the game holds still
    /// (the time scale is 0, the player's input is empty, the cursor is free) and the pause menu's binding is left to the
    /// screen (the title's Settings opens the pause menu, and Escape / Start on the title's main list do too; in a sub-list,
    /// the difficulty question, the chapters, the errands question, Escape / B / Start go back a list instead, and the
    /// difficulty and the chapters have a Back row for the mouse; 2026-10-18). Keyboard and mouse
    /// (arrows / W S, Enter / Space, click; Escape skips) and pad (D-pad or stick, A, B or Start skips). Made by the
    /// <see cref="HellRunDirector"/> at run time (no scene setup), on the HUD's panel settings.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HellScreens : MonoBehaviour
    {
        public enum Screen { None, Title, Opening, FloorCard, Credits, Stats }

        /// <summary>A screen finished: (which, the title's choice: <see cref="ChoiceStart"/>, <see cref="ChoiceQuit"/>, or
        /// <see cref="ChoiceChapter"/> + chapter 0-3; -1 otherwise). Settings and the chapter menu are handled here.</summary>
        public event Action<Screen, int> Done;

        public const int ChoiceStart = 0, ChoiceQuit = 2, ChoiceChapter = 10;

        public Screen Showing { get; private set; }
        public bool Visible => Showing != Screen.None;

        // look (reference 1920 x 1080 panel units)
        static readonly Color Top = new Color(0.012f, 0.006f, 0.008f, 1f);
        static readonly Color Mid = new Color(0.05f, 0.012f, 0.008f, 1f);
        static readonly Color Bottom = new Color(0.26f, 0.06f, 0.015f, 1f);
        static readonly Color Ember = new Color(1f, 0.42f, 0.13f, 1f);
        static readonly Color EmberHot = new Color(1f, 0.78f, 0.42f, 1f);
        static readonly Color Ash = new Color(0.97f, 0.93f, 0.86f, 1f);
        static readonly Color Dim = new Color(0.97f, 0.93f, 0.86f, 0.45f);
        // the faces by role (docs/HELL-UI.md): the title Cinzel Decorative, headings and menu items Metal Mania, small print
        // and hints Grenze Gotisch, numbers Luckiest Guy; the opening cards Metal Mania; other story prose keeps the project face (it is read, not glanced at)
        const string Display = HellFaces.Heading, Numbers = HellFaces.Numbers, TitleFace = HellFaces.Title, Fine = HellFaces.Fine;
        const int EmberCount = 46;
        static readonly Color SplashYellow = new Color(1f, 0.93f, 0.25f, 1f);
        KeylinedLabel _splash;
        readonly System.Random _splashRng = new System.Random();
        const float Fade = 0.45f, CreditsSpeed = 70f;

        UIDocument _doc;
        VisualElement _root, _back, _embersLayer, _title, _card, _credits, _creditsColumn, _stats;
        readonly List<VisualElement> _embers = new List<VisualElement>();
        readonly List<Vector4> _emberState = new List<Vector4>();      // x, y (0..1), speed, phase
        readonly List<KeylinedLabel> _menu = new List<KeylinedLabel>();
        readonly List<KeylinedLabel> _titleMenu = new List<KeylinedLabel>();
        readonly List<KeylinedLabel> _chapterMenu = new List<KeylinedLabel>();
        readonly List<KeylinedLabel> _errandMenu = new List<KeylinedLabel>();
        VisualElement _mainBox, _chapterBox, _errandBox;
        bool _chaptersOpen, _errandsOpen;

        /// <summary>The answer to the Vesper chapter's question (true: ran her errands), set just before the title finishes
        /// with <see cref="ChoiceChapter"/> + <see cref="HellFlow.VesperChapter"/>; null until answered.</summary>
        public bool? VesperErrands { get; private set; }
        KeylinedLabel _cardSpeaker, _cardText, _floorNumber, _floorName, _floorTagline;
        Label _hint;
        int _sel;
        HellCard[] _cards;
        int _cardIndex;
        float _shownAt, _cardAt, _creditsY, _lastMouseMove, _floorSeconds;
        bool _menuWasOpen, _releasePending, _stickLatched;
        Vector2 _lastMouse;
        System.Random _rng = new System.Random(7);
        // the stats' count-up: (label, target, format)
        readonly List<(KeylinedLabel label, float target, Func<float, string> format)> _countUps = new List<(KeylinedLabel, float, Func<float, string>)>();

        // ------------------------------------------------------------------ creating

        public static HellScreens Create(PanelSettings panel)
        {
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1920, 1080);
                panel.match = 0.5f;
            }
            var go = new GameObject("Hell Screens");
            go.SetActive(false);                      // the panel must be set before the document is enabled
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = 900f;                  // over the HUD, under the pause menu (1000)
            var s = go.AddComponent<HellScreens>();
            s._doc = doc;
            go.SetActive(true);
            return s;
        }

        void OnEnable()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_root == null && _doc != null && _doc.rootVisualElement != null) Build(_doc.rootVisualElement);
        }

        void OnDestroy()
        {
            if (Visible) Release();
            SkylinePauseMenu.Blocked = false;
        }

        void Build(VisualElement docRoot)
        {
            _root = Full(new VisualElement { name = "hell-screens", pickingMode = PickingMode.Ignore });
            docRoot.Add(_root);
            _back = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _back.generateVisualContent += DrawGradient;
            _root.Add(_back);
            _embersLayer = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _root.Add(_embersLayer);
            for (int i = 0; i < EmberCount; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                float size = 3f + (float)_rng.NextDouble() * 5f;
                e.style.position = Position.Absolute;
                e.style.width = size;
                e.style.height = size;
                e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = size * 0.5f;
                _embersLayer.Add(e);
                _embers.Add(e);
                _emberState.Add(new Vector4((float)_rng.NextDouble(), (float)_rng.NextDouble(), 0.03f + (float)_rng.NextDouble() * 0.07f, (float)_rng.NextDouble() * 6.28f));
            }

            BuildTitle();
            BuildCard();
            BuildCredits();
            _stats = Column(_root);
            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.style.position = Position.Absolute;
            _hint.style.bottom = 36f;
            _hint.style.left = 0;
            _hint.style.right = 0;
            _hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _hint.style.fontSize = 24f;   // Grenze Gotisch reads small: 24 at 1080p
            _hint.style.color = Dim;
            _hint.style.letterSpacing = 3f;
            SkylineHudStyle.ApplyFace(_hint.style, Fine);
            _root.Add(_hint);
            _root.style.display = DisplayStyle.None;
        }

        void BuildTitle()
        {
            _title = Column(_root);
            var name = new KeylinedLabel(124f, 6f, TitleFace);
            name.SetText(HellFlowText.Title);
            name.SetColor(Ember);
            foreach (var c in name.Children()) c.style.letterSpacing = 10f;
            var nameBox = new VisualElement { pickingMode = PickingMode.Ignore };
            nameBox.Add(name);
            _title.Add(nameBox);
            // the splash line (Minecraft-style; HellFlowText.Splashes): tilted, yellow, pulsing at the title's lower right
            _splash = new KeylinedLabel(34f, 3f, Numbers);
            _splash.SetColor(SplashYellow);
            _splash.style.position = Position.Absolute;
            _splash.style.right = -70f;
            _splash.style.bottom = -12f;
            _splash.style.rotate = new Rotate(new Angle(-12f));
            _splash.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            nameBox.Add(_splash);
            var rule = new VisualElement { pickingMode = PickingMode.Ignore };
            rule.style.width = 520f;
            rule.style.height = 3f;
            rule.style.marginTop = 10f;
            rule.style.marginBottom = 70f;
            rule.style.backgroundColor = new Color(1f, 0.42f, 0.13f, 0.55f);
            _title.Add(rule);
            _mainBox = MenuBox(_title);
            foreach (var word in new[] { HellFlowText.Start, HellFlowText.Chapters, HellFlowText.Settings, HellFlowText.Quit })
                _titleMenu.Add(MenuItem(_mainBox, word, 54f));
            _chapterBox = MenuBox(_title);
            foreach (var word in HellFlowText.ChapterNames) _chapterMenu.Add(MenuItem(_chapterBox, word, 44f));
            _chapterMenu.Add(MenuItem(_chapterBox, HellFlowText.Back, 40f));
            _chapterBox.style.display = DisplayStyle.None;

            // the Vesper chapter's question (no jobs are offered there: the answer decides the spare ending)
            _errandBox = MenuBox(_title);
            var q = new KeylinedLabel(44f, 3f, Display);
            q.SetText(HellFlowText.ErrandsTitle);
            q.SetColor(Ember);
            foreach (var c in q.Children()) c.style.letterSpacing = 12f;
            _errandBox.Add(q);
            var body = new KeylinedLabel(34f, 2.5f, null);
            body.SetWrap(true, TextAnchor.MiddleCenter);
            body.SetText(HellFlowText.ErrandsBody);
            body.SetColor(Ash);
            body.style.maxWidth = 1100f;
            body.style.marginTop = 14f;
            body.style.marginBottom = 30f;
            _errandBox.Add(body);
            _errandMenu.Add(MenuItem(_errandBox, HellFlowText.ErrandsYes, 40f));
            _errandMenu.Add(MenuItem(_errandBox, HellFlowText.ErrandsNo, 40f));
            _errandBox.style.display = DisplayStyle.None;
            BuildDifficulty();
        }

        // ---- Start asks the difficulty first: how long Baja's hour is (docs/HELL-HOUR.md); saved in SkylineOptions.difficulty
        readonly List<KeylinedLabel> _difficultyMenu = new List<KeylinedLabel>();
        VisualElement _difficultyBox;
        bool _difficultyOpen;

        void BuildDifficulty()
        {
            _difficultyBox = MenuBox(_title);
            var q = new KeylinedLabel(44f, 3f, Display);
            q.SetText(HellHourLines.DifficultyTitle);
            q.SetColor(Ember);
            foreach (var c in q.Children()) c.style.letterSpacing = 12f;
            _difficultyBox.Add(q);
            var body = new KeylinedLabel(34f, 2.5f, null);
            body.SetWrap(true, TextAnchor.MiddleCenter);
            body.SetText(HellHourLines.DifficultyBody);
            body.SetColor(Ash);
            body.style.maxWidth = 1100f;
            body.style.marginTop = 14f;
            body.style.marginBottom = 30f;
            _difficultyBox.Add(body);
            foreach (var word in HellHourLines.DifficultyChoices) _difficultyMenu.Add(MenuItem(_difficultyBox, word, 40f));
            _difficultyMenu.Add(MenuItem(_difficultyBox, HellFlowText.Back, 36f));   // a way back for the mouse (Start by accident)
            _difficultyBox.style.display = DisplayStyle.None;
        }

        void OpenDifficulty(bool open)
        {
            _difficultyOpen = open;
            _mainBox.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            _difficultyBox.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _menu.Clear();
            _menu.AddRange(open ? _difficultyMenu : _titleMenu);
            _sel = open ? (int)HellHourRules.FromInt(SkylineSettings.Options.difficulty) : 0;
            _hint.text = open ? HellFlowText.MenuHint + "      ESC / B  back" : HellFlowText.MenuHint;
            SyncMenu();
        }

        static VisualElement MenuBox(VisualElement parent)
        {
            var b = new VisualElement { pickingMode = PickingMode.Ignore };
            b.style.alignItems = Align.Center;
            parent.Add(b);
            return b;
        }

        static KeylinedLabel MenuItem(VisualElement box, string word, float size)
        {
            var item = new KeylinedLabel(size, 3.5f, Display);
            item.SetText(word);
            item.style.marginTop = 6f;
            item.style.marginBottom = 6f;
            foreach (var c in item.Children()) c.style.letterSpacing = 8f;
            box.Add(item);
            return item;
        }

        void OpenChapters(bool open)
        {
            _chaptersOpen = open;
            _errandsOpen = false;
            _difficultyOpen = false;
            if (_difficultyBox != null) _difficultyBox.style.display = DisplayStyle.None;
            _mainBox.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            _chapterBox.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _errandBox.style.display = DisplayStyle.None;
            _menu.Clear();
            _menu.AddRange(open ? _chapterMenu : _titleMenu);
            _sel = open ? 0 : 1;
            _hint.text = open ? HellFlowText.MenuHint + "      ESC / B  back" : HellFlowText.MenuHint;
            SyncMenu();
        }

        /// <summary>The Vesper chapter's question over the chapter list (Escape / B goes back to the list, on Vesper).</summary>
        void OpenErrands()
        {
            _chaptersOpen = false;
            _errandsOpen = true;
            _mainBox.style.display = _chapterBox.style.display = DisplayStyle.None;
            _errandBox.style.display = DisplayStyle.Flex;
            _menu.Clear();
            _menu.AddRange(_errandMenu);
            _sel = 0;
            _hint.text = HellFlowText.MenuHint + "      ESC / B  back";
            SyncMenu();
        }

        void BuildCard()
        {
            _card = Column(_root);
            _card.style.paddingLeft = _card.style.paddingRight = 260f;
            _cardSpeaker = new KeylinedLabel(30f, 2.5f, Display);
            _cardSpeaker.SetColor(Ember);
            foreach (var c in _cardSpeaker.Children()) c.style.letterSpacing = 10f;
            _cardSpeaker.style.marginBottom = 26f;
            _card.Add(_cardSpeaker);
            _cardText = new KeylinedLabel(50f, 3.5f, Display);   // the opening cards: the subheading face (the creator, 2026-10-18)
            _cardText.SetWrap(true, TextAnchor.MiddleCenter);
            _cardText.style.maxWidth = 1300f;
            _card.Add(_cardText);
            _floorNumber = new KeylinedLabel(34f, 2.5f, Display);
            _floorNumber.SetColor(Ember);
            foreach (var c in _floorNumber.Children()) c.style.letterSpacing = 14f;
            _card.Add(_floorNumber);
            _floorName = new KeylinedLabel(120f, 6f, Display);
            foreach (var c in _floorName.Children()) c.style.letterSpacing = 12f;
            _floorName.style.marginTop = 8f;
            _floorName.style.marginBottom = 18f;
            _card.Add(_floorName);
            _floorTagline = new KeylinedLabel(34f, 2.5f, null);
            _floorTagline.SetColor(EmberHot);
            _card.Add(_floorTagline);
        }

        void BuildCredits()
        {
            _credits = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _credits.style.overflow = Overflow.Hidden;
            _root.Add(_credits);
            _creditsColumn = new VisualElement { pickingMode = PickingMode.Ignore };
            _creditsColumn.style.position = Position.Absolute;
            _creditsColumn.style.left = 0;
            _creditsColumn.style.right = 0;
            _creditsColumn.style.alignItems = Align.Center;
            _credits.Add(_creditsColumn);
            foreach (var line in HellFlowText.Credits)
            {
                bool isTitle = line.heading && line.text == HellFlowText.Title;
                var l = new KeylinedLabel(isTitle ? 64f : line.heading ? 44f : 32f, line.heading ? 3f : 2.5f, isTitle ? TitleFace : line.heading ? Display : Fine);
                l.SetText(line.text.Length == 0 ? " " : line.text);
                l.SetColor(line.heading ? Ember : Ash);
                l.style.marginTop = line.heading ? 26f : 4f;
                l.style.marginBottom = 4f;
                if (line.heading) foreach (var c in l.Children()) c.style.letterSpacing = 8f;
                _creditsColumn.Add(l);
            }
        }

        // ------------------------------------------------------------------ showing

        public void ShowTitle()
        {
            Show(Screen.Title);
            _title.style.display = DisplayStyle.Flex;
            if (_splash != null) _splash.SetText(HellFlowText.Splash(_splashRng.Next()));
            OpenChapters(false);
            VesperErrands = null;
            _sel = 0;
            SyncMenu();
        }

        public void ShowOpening(HellCard[] cards)
        {
            Show(Screen.Opening);
            _cards = cards ?? new HellCard[0];
            _cardIndex = -1;
            _card.style.display = DisplayStyle.Flex;
            _hint.text = HellFlowText.SkipHint;
            NextCard();
        }

        public void ShowFloorCard(int floor)
        {
            Show(Screen.FloorCard);
            var f = HellFlowText.Floor(floor);
            _card.style.display = DisplayStyle.Flex;
            _cardSpeaker.style.display = DisplayStyle.None;
            _cardText.style.display = DisplayStyle.None;
            _floorNumber.style.display = _floorName.style.display = _floorTagline.style.display = DisplayStyle.Flex;
            _floorNumber.SetText(f.number);
            _floorName.SetText(f.name);
            _floorTagline.SetText(f.tagline ?? "");
            _floorSeconds = f.seconds;
            _cardAt = Time.unscaledTime;
            _hint.text = "";
        }

        public void ShowCredits()
        {
            Show(Screen.Credits);
            _credits.style.display = DisplayStyle.Flex;
            _creditsY = 1080f;
            _creditsColumn.style.top = _creditsY;
            _hint.text = "HOLD ENTER / A  faster      ESC / B  skip";
        }

        public void ShowStats(HellRunStats s, string endingId)
        {
            Show(Screen.Stats);
            _stats.Clear();
            _countUps.Clear();
            _stats.style.display = DisplayStyle.Flex;
            s = s ?? new HellRunStats();

            var head = new KeylinedLabel(40f, 3f, Display);
            head.SetText(HellFlowText.StatsTitle);
            head.SetColor(Ember);
            foreach (var c in head.Children()) c.style.letterSpacing = 14f;
            _stats.Add(head);
            var ending = new KeylinedLabel(64f, 4f, Display);
            ending.SetText(HellEndings.Title(endingId ?? s.EndingId));
            ending.style.marginTop = 6f;
            ending.style.marginBottom = 24f;
            _stats.Add(ending);

            var big = Row(_stats, 30f);
            BigStat(big, "TIME", s.TotalSeconds, HellRunStats.Clock, 1.6f);
            BigStat(big, "DEATHS", s.Deaths, Whole, 1f);
            BigStat(big, "ENEMIES", s.EnemiesKilled, Whole, 1f);
            if (HellHour.Current != null) Word(big, HellHourLines.StatsLabel(HellHour.Current), HellHourLines.StatsValue(HellHour.Current));   // Baja's hour

            var mid = Row(_stats, 30f);
            BigStat(mid, "REWINDS", s.Rewinds, Whole, 0.8f);
            BigStat(mid, "SHADOW STEPS", s.ShadowSteps, Whole, 0.8f);
            OutOf(mid, "FAST PATHS", s.FastPathsTaken, s.FastPathsOffered);
            if (HellJobs.AssumedErrands.HasValue)     // the Vesper chapter: the answer, not counts it never had
                Word(mid, HellFlowText.StatsErrandsLabel, HellJobs.AssumedErrands.Value ? HellFlowText.StatsErrandsYes : HellFlowText.StatsErrandsNo);
            else OutOf(mid, "JOBS DONE", s.JobsDone, s.JobsOffered);

            var floors = Row(_stats, 10f);
            for (int f = 0; f < HellRunStats.Floors; f++)
                BigStat(floors, HellFlowText.Floor(f).name, s.FloorSeconds[f], HellRunStats.Clock, 0.62f);
            if (s.BossSeconds > 0f) BigStat(floors, "VESPER", s.BossSeconds, HellRunStats.Clock, 0.62f);

            var cont = new KeylinedLabel(46f, 3f, Display);
            cont.SetText(HellFlowText.Continue);
            cont.style.marginTop = 46f;
            foreach (var c in cont.Children()) c.style.letterSpacing = 8f;
            _stats.Add(cont);
            _menu.Clear();
            _menu.Add(cont);
            _sel = 0;
            SyncMenu();
            _hint.text = "ENTER / A  continue";
        }

        static string Whole(float v) => ((int)Math.Round(v)).ToString();

        void BigStat(VisualElement row, string label, float value, Func<float, string> format, float scale)
        {
            var cell = Cell(row);
            var n = new KeylinedLabel(96f * scale, 5f * scale, Numbers);
            n.SetText(format(0f));
            n.SetColor(Ash);
            cell.Add(n);
            var l = new KeylinedLabel(27f, 2f, Fine);
            l.SetText(label);
            l.SetColor(Ember);
            foreach (var c in l.Children()) c.style.letterSpacing = 6f;
            cell.Add(l);
            _countUps.Add((n, value, format));
        }

        /// <summary>A stat cell that is a word (no count-up), sized like <see cref="OutOf"/>.</summary>
        static void Word(VisualElement row, string label, string word)
        {
            var cell = Cell(row);
            var n = new KeylinedLabel(77f, 4f, Numbers);
            n.SetText(word);
            n.SetColor(Ash);
            cell.Add(n);
            var l = new KeylinedLabel(27f, 2f, Fine);
            l.SetText(label);
            l.SetColor(Ember);
            foreach (var c in l.Children()) c.style.letterSpacing = 6f;
            cell.Add(l);
        }

        void OutOf(VisualElement row, string label, int done, int offered)
        {
            var cell = Cell(row);
            var n = new KeylinedLabel(77f, 4f, Numbers);
            n.SetText("0 / " + offered);
            n.SetColor(Ash);
            cell.Add(n);
            var l = new KeylinedLabel(27f, 2f, Fine);
            l.SetText(label);
            l.SetColor(Ember);
            foreach (var c in l.Children()) c.style.letterSpacing = 6f;
            cell.Add(l);
            _countUps.Add((n, done, v => ((int)Math.Round(v)) + " / " + offered));
        }

        /// <summary>Hides every screen; the game runs again (from the end of this frame, so the press isn't also a jump).</summary>
        public void Hide()
        {
            if (!Visible) return;
            Showing = Screen.None;
            if (_root != null) _root.style.display = DisplayStyle.None;
            SkylinePauseMenu.Blocked = false;
            _releasePending = true;
        }

        void Show(Screen s)
        {
            if (_root == null && _doc != null && _doc.rootVisualElement != null) Build(_doc.rootVisualElement);
            Showing = s;
            _releasePending = false;
            _shownAt = Time.unscaledTime;
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 0f;
            _title.style.display = _card.style.display = _credits.style.display = _stats.style.display = DisplayStyle.None;
            _cardSpeaker.style.display = _cardText.style.display = DisplayStyle.Flex;
            _floorNumber.style.display = _floorName.style.display = _floorTagline.style.display = DisplayStyle.None;
            Hold();
            // Escape skips a card; on the title the screen itself opens the pause menu (the settings) from its main list only,
            // so a sub-list's Escape / B / Start goes back instead of also opening the menu
            SkylinePauseMenu.Blocked = true;
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            if (!Visible || _root == null) return;
            Hold();
            float now = Time.unscaledTime;
            _root.style.opacity = Mathf.Clamp01((now - _shownAt) / Fade);
            TickEmbers(Time.unscaledDeltaTime);

            bool menuOpen = SkylinePauseMenu.IsOpen;
            bool skipInput = menuOpen || _menuWasOpen || now - _shownAt < 0.2f;
            _menuWasOpen = menuOpen;

            switch (Showing)
            {
                case Screen.Title:
                    if (_splash != null)
                    {
                        float pulse = 1f + 0.06f * Mathf.Sin(now * Mathf.PI * 2f * 1.1f);   // a gentle pulse
                        _splash.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
                    }
                    if (!skipInput) TitleInput();
                    break;
                case Screen.Opening:
                    if (!skipInput && SkipPressed()) { Finish(Screen.Opening, -1); break; }
                    var card = _cardIndex >= 0 && _cardIndex < _cards.Length ? _cards[_cardIndex] : null;
                    float shown = now - _cardAt;
                    _card.style.opacity = CardAlpha(shown, card != null ? card.seconds : 1f);
                    if ((!skipInput && ConfirmPressed() && shown > 0.25f) || (card != null && shown >= card.seconds)) NextCard();
                    break;
                case Screen.FloorCard:
                    float t = now - _cardAt;
                    _card.style.opacity = CardAlpha(t, _floorSeconds);
                    if ((!skipInput && (ConfirmPressed() || SkipPressed()) && t > 0.3f) || t >= _floorSeconds) Finish(Screen.FloorCard, -1);
                    break;
                case Screen.Credits:
                    bool fast = ConfirmHeld();
                    _creditsY -= CreditsSpeed * (fast ? 5f : 1f) * Time.unscaledDeltaTime;
                    _creditsColumn.style.top = _creditsY;
                    float h = _creditsColumn.layout.height;
                    if ((!skipInput && SkipPressed()) || (!float.IsNaN(h) && h > 0f && _creditsY < -h - 40f)) Finish(Screen.Credits, -1);
                    break;
                case Screen.Stats:
                    float k = Mathf.Clamp01((now - _shownAt - 0.3f) / 1.6f);
                    k = 1f - (1f - k) * (1f - k) * (1f - k);
                    foreach (var c in _countUps) c.label.SetText(c.format(c.target * k));
                    if (!skipInput && now - _shownAt > 0.8f && (ConfirmPressed() || ClickedOn(_menu.Count > 0 ? _menu[0] : null))) Finish(Screen.Stats, -1);
                    break;
            }
        }

        void LateUpdate()
        {
            if (_releasePending && !Visible && !SkylinePauseMenu.IsOpen)
            {
                _releasePending = false;
                Release();
            }
        }

        /// <summary>Holds the game still under a screen (re-asserted each frame: the pause menu lets go when it closes).</summary>
        void Hold()
        {
            if (SkylinePauseMenu.IsOpen) return;
            SkylineDeviceInput.Paused = true;
            Time.timeScale = 0f;
            if (!Application.isBatchMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = Showing == Screen.Title || Showing == Screen.Stats;
            }
        }

        void Release()
        {
            SkylineDeviceInput.Paused = false;
            Time.timeScale = 1f;
            HellTime.Apply();                         // the requests' scale (Time Dilation, a pick) back
            if (!Application.isBatchMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Finish(Screen s, int choice)
        {
            if (Showing != s) return;
            Done?.Invoke(s, choice);
        }

        static float CardAlpha(float t, float seconds)
        {
            float a = Mathf.Clamp01(t / 0.35f);
            float b = Mathf.Clamp01((seconds - t) / 0.35f);
            return Mathf.Min(a, b);
        }

        void NextCard()
        {
            _cardIndex++;
            if (_cards == null || _cardIndex >= _cards.Length) { Finish(Screen.Opening, -1); return; }
            var c = _cards[_cardIndex];
            _cardAt = Time.unscaledTime;
            bool spoken = !string.IsNullOrEmpty(c.speaker);
            _cardSpeaker.style.display = spoken ? DisplayStyle.Flex : DisplayStyle.None;
            _cardSpeaker.SetText(spoken ? c.speaker : "");
            _cardText.SetText(c.text);
            _cardText.SetColor(spoken ? EmberHot : Ash);
            _cardText.SetItalic(!spoken);
            _card.style.opacity = 0f;
        }

        // ------------------------------------------------------------------ the title's menu

        void TitleInput()
        {
            int move = NavMove();
            if (move != 0) { _sel = (_sel + move + _menu.Count) % _menu.Count; SyncMenu(); }
            int hovered = Hovered();
            if (hovered >= 0 && hovered != _sel) { _sel = hovered; SyncMenu(); }
            var mouse = Mouse.current;
            bool click = mouse != null && mouse.leftButton.wasPressedThisFrame && hovered >= 0;
            if (ConfirmPressed() || click) Activate(_sel);
            else if (_errandsOpen && SkipPressed()) { OpenChapters(true); _sel = HellFlow.VesperChapter; SyncMenu(); }
            else if (_chaptersOpen && SkipPressed()) OpenChapters(false);
            else if (_difficultyOpen && SkipPressed()) OpenDifficulty(false);
            else if (!_chaptersOpen && !_errandsOpen && !_difficultyOpen && MenuPressed()) SkylinePauseMenu.OpenMenu();
        }

        /// <summary>Escape or the pad's Start / Menu (or whatever the Pause binding is): the title's main list opens the settings.</summary>
        static bool MenuPressed()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame)
                || SkylineBindings.Pressed(BindDevice.KeyboardMouse, BindAction.Pause, true) || SkylineBindings.Pressed(BindDevice.Pad, BindAction.Pause, true);
        }

        void Activate(int i)
        {
            if (_difficultyOpen)
            {
                if (i >= HellHourLines.DifficultyChoices.Length) { OpenDifficulty(false); return; }   // Back
                SkylineSettings.Options.difficulty = (int)HellHourRules.FromInt(i);
                SkylineSettings.Commit();
                Debug.Log("HELL_FLOW difficulty " + HellHourRules.FromInt(i));
                Finish(Screen.Title, ChoiceStart);
                return;
            }
            if (_errandsOpen)
            {
                VesperErrands = i == 0;
                Finish(Screen.Title, ChoiceChapter + HellFlow.VesperChapter);
                return;
            }
            if (_chaptersOpen)
            {
                if (i == HellFlow.VesperChapter) OpenErrands();           // no jobs in this chapter: ask instead
                else if (i < HellFlowText.ChapterNames.Length) Finish(Screen.Title, ChoiceChapter + i);
                else OpenChapters(false);
                return;
            }
            switch (i)
            {
                case 0: OpenDifficulty(true); break;      // Start asks how long Baja's hour is first
                case 1: OpenChapters(true); break;
                case 2:
                    if (!SkylinePauseMenu.OpenMenu()) Debug.LogWarning("HELL_FLOW no pause menu in this scene: Settings unavailable");
                    break;
                case 3: Finish(Screen.Title, ChoiceQuit); break;
            }
        }

        void SyncMenu()
        {
            for (int i = 0; i < _menu.Count; i++)
            {
                bool on = i == _sel;
                _menu[i].SetColor(on ? EmberHot : Dim);
                _menu[i].style.scale = new Scale(on ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one);
            }
        }

        int Hovered()
        {
            var mouse = Mouse.current;
            if (mouse == null || _root.panel == null) return -1;
            var pos = mouse.position.ReadValue();
            if ((pos - _lastMouse).sqrMagnitude < 0.5f && Time.unscaledTime - _lastMouseMove > 0.05f) return -1;   // only a moving mouse hovers
            if ((pos - _lastMouse).sqrMagnitude >= 0.5f) _lastMouseMove = Time.unscaledTime;
            _lastMouse = pos;
            var p = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(pos.x, UnityEngine.Screen.height - pos.y));
            for (int i = 0; i < _menu.Count; i++)
            {
                var r = _menu[i].worldBound;
                r.xMin -= 60f; r.xMax += 60f;
                if (r.Contains(p)) return i;
            }
            return -1;
        }

        bool ClickedOn(VisualElement e)
        {
            var mouse = Mouse.current;
            if (e == null || mouse == null || !mouse.leftButton.wasPressedThisFrame || _root.panel == null) return false;
            var pos = mouse.position.ReadValue();
            var p = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(pos.x, UnityEngine.Screen.height - pos.y));
            var r = e.worldBound;
            r.xMin -= 60f; r.xMax += 60f;
            return r.Contains(p);
        }

        // ------------------------------------------------------------------ input (devices read directly: these screens live
        // outside the game's bindings)

        static bool ConfirmPressed()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }

        static bool ConfirmHeld()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.enterKey.isPressed || kb.spaceKey.isPressed)) || (pad != null && pad.buttonSouth.isPressed);
        }

        static bool SkipPressed()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame))
                || (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
        }

        int NavMove()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            int m = 0;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) m = -1;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) m = 1;
            }
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame) m = -1;
                if (pad.dpad.down.wasPressedThisFrame) m = 1;
                float y = pad.leftStick.ReadValue().y;
                if (Mathf.Abs(y) < 0.3f) _stickLatched = false;
                else if (!_stickLatched && Mathf.Abs(y) > 0.6f) { _stickLatched = true; m = y > 0f ? -1 : 1; }
            }
            return m;
        }

        // ------------------------------------------------------------------ the look

        void TickEmbers(float dt)
        {
            float w = _root.layout.width, h = _root.layout.height;
            if (float.IsNaN(w) || w <= 0f) { w = 1920f; h = 1080f; }
            float now = Time.unscaledTime;
            for (int i = 0; i < _embers.Count; i++)
            {
                var s = _emberState[i];
                s.y -= s.z * dt;
                s.x += Mathf.Sin(now * 0.7f + s.w) * 0.004f * dt;
                if (s.y < -0.05f) { s.y = 1.05f; s.x = (float)_rng.NextDouble(); }
                _emberState[i] = s;
                var e = _embers[i];
                e.style.left = s.x * w;
                e.style.top = s.y * h;
                float flicker = 0.55f + 0.45f * Mathf.Sin(now * (3f + s.z * 30f) + s.w);
                float heat = Mathf.Clamp01(s.y);              // brighter low, fading as they rise
                var c = Color.Lerp(Ember, EmberHot, flicker * 0.5f);
                c.a = flicker * (0.25f + 0.75f * heat);
                e.style.backgroundColor = c;
            }
        }

        void DrawGradient(MeshGenerationContext ctx)
        {
            var r = _back.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var mesh = ctx.Allocate(6, 12);
            float midY = r.height * 0.62f;
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, 0f, Vertex.nearZ), tint = Top });
            mesh.SetNextVertex(new Vertex { position = new Vector3(r.width, 0f, Vertex.nearZ), tint = Top });
            mesh.SetNextVertex(new Vertex { position = new Vector3(r.width, midY, Vertex.nearZ), tint = Mid });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, midY, Vertex.nearZ), tint = Mid });
            mesh.SetNextVertex(new Vertex { position = new Vector3(r.width, r.height, Vertex.nearZ), tint = Bottom });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, r.height, Vertex.nearZ), tint = Bottom });
            // clockwise (UI Toolkit's front face): top band 0-1-2, 0-2-3; bottom band 3-2-4, 3-4-5
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
            mesh.SetNextIndex(3); mesh.SetNextIndex(2); mesh.SetNextIndex(4);
            mesh.SetNextIndex(3); mesh.SetNextIndex(4); mesh.SetNextIndex(5);
        }

        // ------------------------------------------------------------------ helpers

        static VisualElement Full(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = e.style.top = e.style.right = e.style.bottom = 0;
            return e;
        }

        static VisualElement Column(VisualElement parent)
        {
            var c = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            c.style.alignItems = Align.Center;
            c.style.justifyContent = Justify.Center;
            c.style.display = DisplayStyle.None;
            parent.Add(c);
            return c;
        }

        static VisualElement Row(VisualElement parent, float marginBottom)
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore };
            r.style.flexDirection = FlexDirection.Row;
            r.style.justifyContent = Justify.Center;
            r.style.alignItems = Align.FlexEnd;
            r.style.marginBottom = marginBottom;
            parent.Add(r);
            return r;
        }

        static VisualElement Cell(VisualElement row)
        {
            var c = new VisualElement { pickingMode = PickingMode.Ignore };
            c.style.alignItems = Align.Center;
            c.style.marginLeft = c.style.marginRight = 34f;
            row.Add(c);
            return c;
        }
    }
}
