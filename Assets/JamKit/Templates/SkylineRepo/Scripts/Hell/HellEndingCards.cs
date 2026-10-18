using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The ending's cards over black (docs/HELL-ENDING.md): a full-screen fade (the cut into the escape and out of it), then
    /// the God of Death's two lines as cards (speaker over the words), the epilogue card and the ending's name. A screen like
    /// the flow's opening cards (its own document over the HUD, under the flow's screens and the pause menu), so the clean
    /// picture leaves it alone, as it does the opening. Unscaled time throughout.
    /// </summary>
    public sealed class HellEndingCards : MonoBehaviour
    {
        static readonly Color Ember = new Color(1f, 0.55f, 0.25f);
        static readonly Color Paper = new Color(1f, 0.93f, 0.85f);
        const string Display = JamKit.Skyline.HellFaces.Heading;   // the ending's name and the speakers: important subtext (docs/HELL-UI.md)

        UIDocument _doc;
        VisualElement _root, _black, _column;
        KeylinedLabel _speaker, _text, _title;
        float _alpha, _alphaTarget, _fadeSeconds = 0.4f, _textAlpha;

        public float Alpha => _alpha;
        public bool Black => _alpha >= 0.99f;

        public static HellEndingCards Create()
        {
            PanelSettings panel = null;
            var hud = SkylineHud.Instance != null ? SkylineHud.Instance : FindFirstObjectByType<SkylineHud>();
            if (hud != null)
            {
                var hd = hud.GetComponent<UIDocument>();
                if (hd != null) panel = hd.panelSettings;
            }
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1920, 1080);
                panel.match = 0.5f;
            }
            var go = new GameObject("Hell Ending Cards");
            go.SetActive(false);                      // the panel must be set before the document is enabled
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = 880f;                  // over the HUD, under the flow's screens (900) and the pause menu (1000)
            var c = go.AddComponent<HellEndingCards>();
            c._doc = doc;
            go.SetActive(true);
            return c;
        }

        void OnEnable()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_root == null && _doc != null && _doc.rootVisualElement != null) Build(_doc.rootVisualElement);
        }

        static VisualElement Full(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = 0;
            e.style.right = 0;
            e.style.top = 0;
            e.style.bottom = 0;
            return e;
        }

        void Build(VisualElement docRoot)
        {
            _root = Full(new VisualElement { name = "hell-ending-cards", pickingMode = PickingMode.Ignore });
            docRoot.Add(_root);
            _black = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _black.style.backgroundColor = Color.black;
            _root.Add(_black);
            _column = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _column.style.alignItems = Align.Center;
            _column.style.justifyContent = Justify.Center;
            _column.style.paddingLeft = _column.style.paddingRight = 240f;
            _root.Add(_column);
            _speaker = new KeylinedLabel(30f, 2.5f, Display);
            _speaker.SetColor(Ember);
            foreach (var ch in _speaker.Children()) ch.style.letterSpacing = 10f;
            _speaker.style.marginBottom = 24f;
            _column.Add(_speaker);
            _text = new KeylinedLabel(46f, 3f, null);
            _text.SetColor(Paper);
            _text.SetWrap(true, TextAnchor.MiddleCenter);
            _text.style.maxWidth = 1300f;
            _column.Add(_text);
            _title = new KeylinedLabel(72f, 4f, Display);   // wraps inside the column (the ending's name clipped at 96 px, one line)
            _title.SetColor(Ember);
            _title.SetWrap(true, TextAnchor.MiddleCenter);
            _title.style.maxWidth = 1300f;
            foreach (var ch in _title.Children()) { ch.style.letterSpacing = 6f; ch.style.maxWidth = 1300f; }
            _column.Add(_title);
            Apply();
        }

        /// <summary>Fades the black to <paramref name="target"/> (0 clear, 1 black) over <paramref name="seconds"/>.</summary>
        public void Fade(float target, float seconds)
        {
            _alphaTarget = Mathf.Clamp01(target);
            _fadeSeconds = Mathf.Max(0.01f, seconds);
        }

        /// <summary>A line as a card: the speaker's name over the words (no speaker: an epilogue card).</summary>
        public void Show(string speaker, string text)
        {
            if (_root == null) return;
            _speaker.SetText(speaker ?? "");
            _speaker.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
            _text.SetText(text ?? "");
            _text.style.display = DisplayStyle.Flex;
            _title.style.display = DisplayStyle.None;
            _textAlpha = 0f;
        }

        /// <summary>The ending's name, big.</summary>
        public void ShowTitle(string title)
        {
            if (_root == null) return;
            _speaker.style.display = DisplayStyle.None;
            _text.style.display = DisplayStyle.None;
            _title.SetText(title ?? "");
            _title.style.display = DisplayStyle.Flex;
            _textAlpha = 0f;
        }

        public void ClearText()
        {
            if (_root == null) return;
            _speaker.style.display = DisplayStyle.None;
            _text.style.display = DisplayStyle.None;
            _title.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (_root == null) return;
            _alpha = Mathf.MoveTowards(_alpha, _alphaTarget, Time.unscaledDeltaTime / _fadeSeconds);
            _textAlpha = Mathf.MoveTowards(_textAlpha, 1f, Time.unscaledDeltaTime / 0.5f);
            Apply();
        }

        void Apply()
        {
            _black.style.opacity = _alpha;
            _root.style.display = _alpha > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
            _column.style.opacity = _alpha * _textAlpha;
        }
    }
}
