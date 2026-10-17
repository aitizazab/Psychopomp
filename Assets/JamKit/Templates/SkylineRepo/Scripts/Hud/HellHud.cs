using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell abilities on the HUD (docs/HELL-UI.md, "The abilities"), bottom right above the ammo
    /// (<see cref="HellHudPlaces.Abilities"/>), and the slot for Baja's hour, top right (<see cref="HellHudPlaces.Hourglass"/>).
    /// <list type="bullet">
    /// <item>A row of tiles, right to left: <b>Time Dilation</b> (a sand dial that empties while the world is slowed; ready
    /// otherwise), <b>Rewind</b> (a sand dial with a back-chevron filling as it cools down), <b>Shadow Step</b> (one violet
    /// diamond a charge, the recharging one filling; grey while blocked: carrying the lantern), and <b>Nightfall</b> when owned
    /// (a violet dial). Under each, its control for the device in use ("E", "RB", ...). A tile coming ready flashes.</item>
    /// <item>Above them, a chip per floor skill you own (its glyph in its flavour's hue).</item>
    /// <item>The hourglass slot: another lane's run clock calls <see cref="SetHourglass"/>; a plain hourglass is drawn until
    /// it builds its own visual into <see cref="HourglassSlot"/> (and sets <see cref="DrawDefaultHourglass"/> false).</item>
    /// </list>
    /// No text but the control labels and the hour's digits. Repaints only when a value it draws changes; no garbage a frame.
    /// The <see cref="HellAbilityWheel"/> is added beside it.
    /// </summary>
    public sealed class HellHud : MonoBehaviour
    {
        // ---- the tiles, in the abilities box (330 x 132): chips on top, tiles under them
        const float TileW = 80f, IconCy = 66f, LabelTop = 100f, ChipH = 24f, ChipW = 34f, ChipGap = 2f;
        const float FlashSeconds = 0.3f;

        // ---- the hourglass's state, kept statically so the other lane can call before the HUD exists
        static float _hourLeft01 = -1f, _hourSeconds = -1f;
        static bool _hourUrgent;

        /// <summary>
        /// Baja's hour, for the run-clock lane: <paramref name="remaining01"/> 1 = a full hour glass, 0 = run out (a negative
        /// value hides the slot); <paramref name="secondsLeft"/> shows as "m:ss" under it (negative: no digits);
        /// <paramref name="urgent"/> makes it throb ember. Call it every frame or only on change; it is cheap.
        /// </summary>
        public static void SetHourglass(float remaining01, float secondsLeft = -1f, bool urgent = false)
        {
            _hourLeft01 = remaining01 < 0f ? -1f : Mathf.Clamp01(remaining01);
            _hourSeconds = secondsLeft;
            _hourUrgent = urgent;
        }

        /// <summary>Hides the hourglass slot.</summary>
        public static void HideHourglass() => _hourLeft01 = -1f;

        /// <summary>False: the default hourglass isn't drawn (a lane building its own visual into <see cref="HourglassSlot"/>).</summary>
        public static bool DrawDefaultHourglass = true;

        /// <summary>The top-right element reserved for the hour (null until the HUD is built). Add children to it freely; it
        /// sits under the HUD's root, so the clean-picture switch hides it.</summary>
        public static VisualElement HourglassSlot { get; private set; }

        HellAbilities _a;
        HellSkillNightfall _night;
        VisualElement _el, _chipRow, _hourEl;
        KeylinedLabel _hourDigits;
        readonly Label[] _keys = new Label[4];
        readonly List<KeylinedLabel> _chips = new List<KeylinedLabel>();
        int _chipCount = -1;
        float _stepFlash, _rewindFlash, _nightFlash, _lastKey = float.NaN, _lastHourKey = float.NaN, _t;
        bool _stepWasReady = true, _rewindWasReady = true, _nightWasReady = true;
        BindDevice _keysFor = (BindDevice)(-1);
        string _hourText;

        void Start()
        {
            _a = GetComponent<HellAbilities>();
            if (_a != null) _a.Granted += OnGranted;
            if (GetComponent<HellAbilityWheel>() == null) gameObject.AddComponent<HellAbilityWheel>();
        }

        void OnGranted(string id)
        {
            _night = GetComponent<HellSkillNightfall>();
            _chipCount = -1;   // rebuild the chips
        }

        void Update()
        {
            if (_a == null) return;
            if (_el == null && !Build()) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;

            // the flashes as each comes ready
            bool stepReady = _a.StepReady;
            if (stepReady && !_stepWasReady) _stepFlash = FlashSeconds;
            _stepWasReady = stepReady;
            bool rewindReady = _a.Rewind == null || _a.Rewind.Cooldown.Ready;
            if (rewindReady && !_rewindWasReady) _rewindFlash = FlashSeconds;
            _rewindWasReady = rewindReady;
            bool nightReady = _night == null || _night.Cooldown.Ready;
            if (nightReady && !_nightWasReady) _nightFlash = FlashSeconds;
            _nightWasReady = nightReady;
            _stepFlash = Mathf.Max(0f, _stepFlash - dt);
            _rewindFlash = Mathf.Max(0f, _rewindFlash - dt);
            _nightFlash = Mathf.Max(0f, _nightFlash - dt);

            if (_chipCount != _a.Owned.Count) RebuildChips();
            var device = HellInputDevice.Current;
            if (device != _keysFor) RefreshKeys(device);

            // repaint only on change
            var c = _a.StepCharges;
            float key = c.Charges * 1000f + c.Fraction * 97f
                + (_a.Rewind != null ? _a.Rewind.Cooldown.Fraction * 31f : 0f)
                + _a.Dilation.HoldLeft01 * 53f
                + (_night != null ? _night.Cooldown.Fraction * 17f + 5000f : 0f)
                + (HellAbilities.ShadowStepBlocked ? 20000f : 0f)
                + (_stepFlash + _rewindFlash + _nightFlash) * 311f;
            if (key != _lastKey)
            {
                _lastKey = key;
                _el.MarkDirtyRepaint();
            }
            TickHourglass();
        }

        bool Build()
        {
            var hud = SkylineHud.Instance;
            if (hud == null || hud.Safe == null) return false;
            _el = SkylineHudStyle.Layer(hud.Safe, "hell-abilities");
            HellHudStyle.Place(_el, HellHudPlaces.Abilities);
            _el.generateVisualContent += Draw;

            _chipRow = SkylineHudStyle.Layer(_el, "hell-skill-chips");
            _chipRow.style.right = 0;
            _chipRow.style.top = 0;
            _chipRow.style.flexDirection = FlexDirection.RowReverse;

            for (int i = 0; i < _keys.Length; i++)
            {
                var l = new Label { pickingMode = PickingMode.Ignore };
                SkylineHudStyle.ZeroBox(l.style);
                l.style.position = Position.Absolute;
                l.style.width = TileW;
                l.style.top = LabelTop;
                l.style.right = i * TileW;
                l.style.fontSize = 21f;   // Grenze Gotisch reads small
                l.style.color = HellHudStyle.AshDim;
                l.style.unityTextAlign = TextAnchor.UpperCenter;
                l.style.whiteSpace = WhiteSpace.NoWrap;
                l.style.textShadow = new TextShadow { offset = new Vector2(0f, 1.5f), blurRadius = 2f, color = HellHudStyle.Ink };
                SkylineHudStyle.ApplyFace(l.style, HellFaces.Fine);
                _el.Add(l);
                _keys[i] = l;
            }

            _hourEl = SkylineHudStyle.Layer(hud.Safe, "hell-hourglass");
            HellHudStyle.Place(_hourEl, HellHudPlaces.Hourglass);
            _hourEl.generateVisualContent += DrawHourglass;
            _hourDigits = HellHudStyle.Label(28f, HellFaces.Numbers, HellHudStyle.Ash);
            _hourDigits.style.position = Position.Absolute;
            _hourDigits.style.left = 0;
            _hourDigits.style.right = 0;
            _hourDigits.style.bottom = 0;
            _hourDigits.SetWrap(false, TextAnchor.MiddleCenter);
            _hourEl.Add(_hourDigits);
            _hourEl.style.display = DisplayStyle.None;
            HourglassSlot = _hourEl;
            _night = GetComponent<HellSkillNightfall>();
            return true;
        }

        void RefreshKeys(BindDevice d)
        {
            _keysFor = d;
            _keys[0].text = HellInputDevice.Short(BindAction.Aim, d);         // Time Dilation (the aim button)
            _keys[1].text = HellInputDevice.Short(BindAction.Rewind, d);
            _keys[2].text = HellInputDevice.Short(BindAction.ShadowStep, d);
            _keys[3].text = HellInputDevice.Short(BindAction.Skill, d);
        }

        void RebuildChips()
        {
            _chipCount = _a.Owned.Count;
            foreach (var c in _chips) c.RemoveFromHierarchy();
            _chips.Clear();
            foreach (var def in HellSkills.All)
            {
                if (!_a.Has(def.id)) continue;
                var chip = HellHudStyle.Label(18f, HellFaces.Numbers, HellHudStyle.Flavour(def.flavour), 2f);
                chip.SetText(def.icon);
                chip.SetWrap(false, TextAnchor.MiddleCenter);
                chip.style.width = ChipW;
                chip.style.height = ChipH;
                chip.style.marginLeft = ChipGap;
                chip.style.backgroundColor = HellHudStyle.Plate;
                chip.style.borderBottomWidth = 3f;
                chip.style.borderBottomColor = HellHudStyle.Flavour(def.flavour);
                _chipRow.Add(chip);
                _chips.Add(chip);
            }
        }

        // ------------------------------------------------------------------ drawing

        void Draw(MeshGenerationContext ctx)
        {
            if (_a == null) return;
            var p = ctx.painter2D;
            float w = HellHudPlaces.Abilities.w;

            // 0: Time Dilation: a sand dial, full when ready, emptying while the world is slowed
            float hold = _a.Dilation.HoldLeft01;
            var dc = new Vector2(Cx(w, 0), IconCy);
            var dcol = hold > 0f ? HellHudStyle.EmberHot : HellHudStyle.Sand;
            HellHudStyle.Dial(p, dc, 22f, hold > 0f ? hold : 1f, dcol);
            Hourglass(p, dc, 12f, hold > 0f ? hold : 1f, dcol);

            // 1: Rewind: a sand dial filling as it cools, a back-chevron in the middle
            var cd = _a.Rewind != null ? _a.Rewind.Cooldown : null;
            float rf = cd != null ? cd.Fraction : 1f;
            var rc = new Vector2(Cx(w, 1), IconCy);
            var rcol = _rewindFlash > 0f ? Color.white : HellHudStyle.Sand;
            HellHudStyle.Dial(p, rc, 22f, rf, rcol);
            Chevron(p, rc, 12f, rf >= 1f ? rcol : HellHudStyle.Dim(HellHudStyle.Sand, 0.5f));

            // 2: Shadow Step: a diamond a charge, the recharging one filling
            var c = _a.StepCharges;
            int n = Mathf.Clamp(c.Max, 1, 4);
            bool blocked = HellAbilities.ShadowStepBlocked;
            var scol = blocked ? HellHudStyle.Spent : _stepFlash > 0f ? Color.white : HellHudStyle.Shadow;
            float r = n == 1 ? 22f : 16f, gap = r * 2f + 4f;
            float x0 = Cx(w, 2) - (n - 1) * gap * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float fill = i < c.Charges ? 1f : (i == c.Charges ? c.Fraction : 0f);
                HellHudStyle.Diamond(p, new Vector2(x0 + i * gap, IconCy), r, fill, scol, HellHudStyle.Plate);
            }

            // 3: Nightfall, when owned
            if (_night != null)
            {
                var nc = new Vector2(Cx(w, 3), IconCy);
                HellHudStyle.Dial(p, nc, 20f, _night.Cooldown.Fraction, _nightFlash > 0f ? Color.white : HellHudStyle.Shadow);
                p.BeginPath();
                p.Arc(nc, 9f, new Angle(0f), new Angle(360f));
                p.fillColor = HellHudStyle.Ink;
                p.Fill();
                p.strokeColor = HellHudStyle.Shadow;
                p.lineWidth = 2f;
                p.Stroke();
            }
            if (_keys[3] != null)
            {
                var show = _night != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (_keys[3].style.display != show) _keys[3].style.display = show;
            }
        }

        /// <summary>Tile <paramref name="i"/>'s centre x, counted from the right edge of a box <paramref name="w"/> wide.</summary>
        static float Cx(float w, int i) => w - TileW * (i + 0.5f);

        static void Chevron(Painter2D p, Vector2 c, float h, Color col)
        {
            for (int k = 0; k < 2; k++)
            {
                float dx = k * h * 0.55f - h * 0.35f;
                for (int pass = 0; pass < 2; pass++)
                {
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(dx + h * 0.45f, -h * 0.5f));
                    p.LineTo(c + new Vector2(dx - h * 0.05f, 0f));
                    p.LineTo(c + new Vector2(dx + h * 0.45f, h * 0.5f));
                    p.strokeColor = pass == 0 ? HellHudStyle.Ink : col;
                    p.lineWidth = pass == 0 ? 5f : 2.5f;
                    p.lineJoin = LineJoin.Miter;
                    p.Stroke();
                }
            }
        }

        /// <summary>A small hourglass round <paramref name="c"/>: two triangles, the sand in the top one by
        /// <paramref name="top01"/>, the rest fallen into the bottom one.</summary>
        public static void Hourglass(Painter2D p, Vector2 c, float h, float top01, Color sand)
        {
            float w = h * 0.8f;
            top01 = Mathf.Clamp01(top01);
            if (top01 > 0.01f)
            {
                float y = c.y - h * top01;
                float hw = w * top01;
                p.BeginPath();
                p.MoveTo(new Vector2(c.x - hw, y));
                p.LineTo(new Vector2(c.x + hw, y));
                p.LineTo(c);
                p.ClosePath();
                p.fillColor = sand;
                p.Fill();
            }
            if (top01 < 0.99f)
            {
                float b = 1f - top01;
                float y = c.y + h - h * b;
                float hw = w * (1f - b);
                p.BeginPath();
                p.MoveTo(new Vector2(c.x - w, c.y + h));
                p.LineTo(new Vector2(c.x + w, c.y + h));
                p.LineTo(new Vector2(c.x + hw, y));
                p.LineTo(new Vector2(c.x - hw, y));
                p.ClosePath();
                p.fillColor = sand;
                p.Fill();
            }
            p.BeginPath();
            p.MoveTo(new Vector2(c.x - w, c.y - h));
            p.LineTo(new Vector2(c.x + w, c.y - h));
            p.LineTo(c);
            p.LineTo(new Vector2(c.x + w, c.y + h));
            p.LineTo(new Vector2(c.x - w, c.y + h));
            p.LineTo(c);
            p.ClosePath();
            p.strokeColor = HellHudStyle.Bone;
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Miter;
            p.Stroke();
        }

        // ------------------------------------------------------------------ the hour

        void TickHourglass()
        {
            bool show = _hourLeft01 >= 0f;
            var disp = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_hourEl.style.display != disp) _hourEl.style.display = disp;
            if (!show) return;
            string text = _hourSeconds >= 0f ? HellClockText.Of(_hourSeconds) : "";
            if (!ReferenceEquals(text, _hourText))
            {
                _hourText = text;
                _hourDigits.SetText(text);
            }
            float pulse = _hourUrgent ? 0.5f + 0.5f * Mathf.Sin(_t * 7f) : 0f;
            float key = _hourLeft01 * 1000f + pulse * 7f + (DrawDefaultHourglass ? 1f : 0f);
            if (key != _lastHourKey)
            {
                _lastHourKey = key;
                _hourDigits.SetColor(_hourUrgent ? Color.Lerp(HellHudStyle.Ash, HellHudStyle.Ember, pulse) : HellHudStyle.Ash);
                _hourEl.MarkDirtyRepaint();
            }
        }

        void DrawHourglass(MeshGenerationContext ctx)
        {
            if (!DrawDefaultHourglass || _hourLeft01 < 0f) return;
            var p = ctx.painter2D;
            var c = new Vector2(HellHudPlaces.Hourglass.w * 0.5f, 66f);
            p.BeginPath();
            p.Arc(c, 58f, new Angle(0f), new Angle(360f));
            p.fillColor = HellHudStyle.Plate;
            p.Fill();
            p.strokeColor = _hourUrgent ? HellHudStyle.Ember : HellHudStyle.EmberEdge;
            p.lineWidth = 2f;
            p.Stroke();
            Hourglass(p, c, 44f, _hourLeft01, HellHudStyle.Sand);
        }

        void OnDestroy()
        {
            if (_a != null) _a.Granted -= OnGranted;
            if (_el != null) _el.RemoveFromHierarchy();
            if (_hourEl != null)
            {
                _hourEl.RemoveFromHierarchy();
                if (HourglassSlot == _hourEl) HourglassSlot = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _hourLeft01 = -1f;
            _hourSeconds = -1f;
            _hourUrgent = false;
            DrawDefaultHourglass = true;
            HourglassSlot = null;
        }
    }
}
