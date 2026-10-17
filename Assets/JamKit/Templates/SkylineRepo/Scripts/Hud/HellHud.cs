using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell abilities on the HUD (docs/HELL-UI.md, "The abilities"): small icons LEFT of the ammo counter and gun icon,
    /// on its baseline (<see cref="HellHudPlaces.Abilities"/>; the creator, 2026-10-18), and the slot for Baja's hour, top
    /// right (<see cref="HellHudPlaces.Hourglass"/>).
    /// <list type="bullet">
    /// <item>A row of icons, right to left (nearest the gun first): <b>Rewind</b> (a clock face inside a counter-clockwise
    /// arrow; the arrow fills round as it cools down), <b>Shadow Step</b> (a violet footprint trailing ink; its ring fills as
    /// the next charge comes back, a pip per charge under it; grey while blocked: carrying the lantern), <b>Time
    /// Dilation</b> (a fuel gauge: an hourglass inside a sand ring that drains while the world is slowed and refills, with a
    /// tick at the share it needs to start; dim under it), and <b>Nightfall</b> when owned (a violet dial). Under each, its
    /// control for the device in use ("E", "RB", ...). An icon coming ready flashes.</item>
    /// <item>Over the ammo counter, a chip per floor skill you own (its glyph in its flavour's hue;
    /// <see cref="HellHudPlaces.SkillChips"/>).</item>
    /// <item>The hourglass slot: another lane's run clock calls <see cref="SetHourglass"/>; a plain hourglass is drawn until
    /// it builds its own visual into <see cref="HourglassSlot"/> (and sets <see cref="DrawDefaultHourglass"/> false).</item>
    /// </list>
    /// No text but the control labels and the hour's digits. Repaints only when a value it draws changes; no garbage a frame.
    /// The <see cref="HellAbilityWheel"/> is added beside it.
    /// </summary>
    public sealed class HellHud : MonoBehaviour
    {
        // ---- the icons, in the abilities box (4 x 60 wide, 88 high): icon, pips, control label
        const float TileW = HellHudPlaces.AbilityTileW, IconCy = 30f, IconR = 18f, RingPx = 4f, LabelTop = 64f;
        const float ChipH = 24f, ChipW = 34f, ChipGap = 2f;
        // tile order, right to left (nearest the gun first)
        const int TRewind = 0, TStep = 1, TDilation = 2, TNight = 3;
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
        bool _dilationWasReady = true;
        float _dilationFlash;
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
            bool dilReady = HellAbilityIcons.CanStartDilation(DilationFuel01(_a));
            if (dilReady && !_dilationWasReady) _dilationFlash = FlashSeconds;
            _dilationWasReady = dilReady;
            _dilationFlash = Mathf.Max(0f, _dilationFlash - dt);
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
                + DilationFuel01(_a) * 53f + (_a.Dilation.Active ? 7000f : 0f) + _dilationFlash * 211f
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

            _chipRow = SkylineHudStyle.Layer(hud.Safe, "hell-skill-chips");
            HellHudStyle.Place(_chipRow, HellHudPlaces.SkillChips);
            _chipRow.style.flexDirection = FlexDirection.RowReverse;
            _chipRow.style.alignItems = Align.FlexEnd;

            for (int i = 0; i < _keys.Length; i++)
            {
                var l = new Label { pickingMode = PickingMode.Ignore };
                SkylineHudStyle.ZeroBox(l.style);
                l.style.position = Position.Absolute;
                l.style.width = TileW;
                l.style.top = LabelTop;
                l.style.right = i * TileW;
                l.style.fontSize = 18f;   // Grenze Gotisch reads small; the icons are small now
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
            _keys[TRewind].text = HellInputDevice.Short(BindAction.Rewind, d);
            _keys[TStep].text = HellInputDevice.Short(BindAction.ShadowStep, d);
            _keys[TDilation].text = HellInputDevice.Short(BindAction.Aim, d);   // Time Dilation (the aim button)
            _keys[TNight].text = HellInputDevice.Short(BindAction.Skill, d);
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

        /// <summary>Time Dilation's fuel, 1 full to 0 empty.
        /// TODO(dilation fuel): read <c>HellAbilities.DilationFuel01</c> here once the fuel-gauge change (drains in 4 s,
        /// recharges at half rate, needs 25% to start) is on main. Until then this stub reads the old hold: it drains while
        /// the world is slowed and reads full otherwise.</summary>
        static float DilationFuel01(HellAbilities a)
        {
            float hold = a.Dilation.HoldLeft01;
            return hold > 0f ? hold : 1f;
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (_a == null) return;
            var p = ctx.painter2D;
            float w = HellHudPlaces.Abilities.w;

            // Rewind: a clock face inside a counter-clockwise arrow; the arrow fills round as it cools down
            var cd = _a.Rewind != null ? _a.Rewind.Cooldown : null;
            float rf = cd != null ? cd.Fraction : 1f;
            DrawRewind(p, new Vector2(Cx(w, TRewind), IconCy), rf, _rewindFlash > 0f);

            // Shadow Step: a footprint trailing ink; the ring fills as the next charge comes back; a pip per charge
            var c = _a.StepCharges;
            DrawStep(p, new Vector2(Cx(w, TStep), IconCy), c.Charges, Mathf.Clamp(c.Max, 1, 4), c.Fraction,
                HellAbilities.ShadowStepBlocked, _stepFlash > 0f, _a.StepReady);

            // Time Dilation: a fuel gauge round an hourglass, a tick at the share it needs to start
            DrawDilation(p, new Vector2(Cx(w, TDilation), IconCy), DilationFuel01(_a), _a.Dilation.Active, _dilationFlash > 0f);

            // Nightfall, when owned
            if (_night != null)
            {
                var nc = new Vector2(Cx(w, TNight), IconCy);
                var ncol = _nightFlash > 0f ? Color.white : HellHudStyle.Shadow;
                Disc(p, nc, IconR + RingPx * 0.5f + 2f);
                Ring(p, nc, IconR, _night.Cooldown.Fraction, ncol, false);
                p.BeginPath();
                p.Arc(nc, 8f, new Angle(0f), new Angle(360f));
                p.fillColor = HellHudStyle.Ink;
                p.Fill();
                p.strokeColor = _night.Cooldown.Ready ? ncol : HellHudStyle.Dim(ncol, 0.6f);
                p.lineWidth = 2f;
                p.Stroke();
            }
            if (_keys[TNight] != null)
            {
                var show = _night != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (_keys[TNight].style.display != show) _keys[TNight].style.display = show;
            }
        }

        /// <summary>Tile <paramref name="i"/>'s centre x, counted from the right edge of a box <paramref name="w"/> wide.</summary>
        static float Cx(float w, int i) => w - TileW * (i + 0.5f);

        /// <summary>The icon's backing: an obsidian disc with an ink rim.</summary>
        static void Disc(Painter2D p, Vector2 c, float r)
        {
            p.BeginPath();
            p.Arc(c, r, new Angle(0f), new Angle(360f));
            p.fillColor = HellHudStyle.Plate;
            p.Fill();
            p.strokeColor = HellHudStyle.Ink;
            p.lineWidth = 2f;
            p.Stroke();
        }

        /// <summary>A meter ring: an ink track, then <paramref name="fill01"/> of the circle from twelve o'clock (clockwise, or
        /// counter-clockwise), full colour when full, dimmer while filling.</summary>
        static void Ring(Painter2D p, Vector2 c, float r, float fill01, Color col, bool ccw, float sweep = 360f, float startDeg = -90f)
        {
            p.BeginPath();
            if (ccw) p.Arc(c, r, new Angle(startDeg), new Angle(startDeg - sweep), ArcDirection.CounterClockwise);
            else p.Arc(c, r, new Angle(startDeg), new Angle(startDeg + sweep));
            p.strokeColor = new Color(HellHudStyle.Ink.r, HellHudStyle.Ink.g, HellHudStyle.Ink.b, 0.9f);
            p.lineWidth = RingPx + 3f;
            p.lineCap = LineCap.Butt;
            p.Stroke();
            fill01 = Mathf.Clamp01(fill01);
            if (fill01 <= 0.002f) return;
            p.BeginPath();
            if (ccw) p.Arc(c, r, new Angle(startDeg), new Angle(startDeg - sweep * fill01), ArcDirection.CounterClockwise);
            else p.Arc(c, r, new Angle(startDeg), new Angle(startDeg + sweep * fill01));
            p.strokeColor = fill01 >= 0.999f ? col : HellHudStyle.Dim(col, 0.62f);
            p.lineWidth = RingPx;
            p.lineCap = LineCap.Butt;
            p.Stroke();
        }

        static Vector2 OnCircle(Vector2 c, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        /// <summary>Rewind: a counter-clockwise arrow (300 degrees from twelve, its head at about two o'clock pointing back up)
        /// round a clock face whose hands sit at ten to twelve. The arrow's shaft fills as it cools down.</summary>
        static void DrawRewind(Painter2D p, Vector2 c, float fill01, bool flash)
        {
            const float Sweep = 300f;
            bool ready = fill01 >= 1f;
            var col = flash ? Color.white : HellHudStyle.Sand;
            var dim = HellHudStyle.Dim(HellHudStyle.Sand, 0.55f);
            Disc(p, c, IconR + RingPx * 0.5f + 2f);
            Ring(p, c, IconR - 2f, fill01, col, true, Sweep);

            // the arrowhead at the shaft's end, pointing on round (counter-clockwise; screen angles grow clockwise)
            float endDeg = -90f - Sweep;
            var tip = OnCircle(c, IconR - 2f, endDeg);
            float a = endDeg * Mathf.Deg2Rad;
            var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var along = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));   // the counter-clockwise direction on screen
            for (int pass = 0; pass < 2; pass++)
            {
                p.BeginPath();
                p.MoveTo(tip + along * 6f);
                p.LineTo(tip - along * 2f + radial * 6f);
                p.LineTo(tip - along * 2f - radial * 6f);
                p.ClosePath();
                if (pass == 0)
                {
                    p.strokeColor = HellHudStyle.Ink;
                    p.lineWidth = 3f;
                    p.lineJoin = LineJoin.Miter;
                    p.Stroke();
                }
                else
                {
                    p.fillColor = ready ? col : dim;
                    p.Fill();
                }
            }

            // the clock face and its hands
            var face = ready ? col : dim;
            p.BeginPath();
            p.Arc(c, 8.5f, new Angle(0f), new Angle(360f));
            p.strokeColor = face;
            p.lineWidth = 1.6f;
            p.Stroke();
            for (int pass = 0; pass < 2; pass++)
            {
                p.BeginPath();
                p.MoveTo(OnCircle(c, 6.5f, -90f));
                p.LineTo(c);
                p.LineTo(OnCircle(c, 4.8f, -150f));   // the short hand at ten
                p.strokeColor = pass == 0 ? HellHudStyle.Ink : face;
                p.lineWidth = pass == 0 ? 3.6f : 1.8f;
                p.lineCap = LineCap.Round;
                p.lineJoin = LineJoin.Round;
                p.Stroke();
            }
        }

        /// <summary>Shadow Step: a violet footprint mid-stride (a tilted sole and four toes) with three ink streaks trailing
        /// behind it, inside a ring that fills as the next charge comes back; under it a pip per charge. Grey while blocked.</summary>
        static void DrawStep(Painter2D p, Vector2 c, int charges, int max, float fraction, bool blocked, bool flash, bool ready)
        {
            var col = blocked ? HellHudStyle.Spent : flash ? Color.white : HellHudStyle.Shadow;
            Disc(p, c, IconR + RingPx * 0.5f + 2f);
            Ring(p, c, IconR, charges >= max ? 1f : fraction, col, false);

            // the ink streaks behind the foot (to the left: it steps right, toward the gun)
            for (int i = 0; i < 3; i++)
            {
                float y = c.y - 4f + i * 4.5f, len = 7f - i * 1.5f;
                p.BeginPath();
                p.MoveTo(new Vector2(c.x - 12f, y));
                p.LineTo(new Vector2(c.x - 12f + len, y));
                p.strokeColor = HellHudStyle.Dim(col, 0.7f);
                p.lineWidth = 1.6f;
                p.lineCap = LineCap.Round;
                p.Stroke();
            }

            // the sole: an ellipse tilted forward, as polygon points (no garbage)
            var foot = ready ? col : HellHudStyle.Dim(col, 0.6f);
            var sc = c + new Vector2(3f, 3f);
            const float Rx = 4.6f, Ry = 7.4f, Tilt = 20f * Mathf.Deg2Rad;
            float ct = Mathf.Cos(Tilt), st = Mathf.Sin(Tilt);
            p.BeginPath();
            for (int i = 0; i < 18; i++)
            {
                float t = i / 18f * Mathf.PI * 2f;
                float ex = Mathf.Cos(t) * Rx, ey = Mathf.Sin(t) * Ry;
                // narrower heel: squeeze the lower half
                if (ey > 0f) ex *= 0.8f;
                var q = sc + new Vector2(ex * ct - ey * st, ex * st + ey * ct);
                if (i == 0) p.MoveTo(q); else p.LineTo(q);
            }
            p.ClosePath();
            p.fillColor = foot;
            p.Fill();
            p.strokeColor = HellHudStyle.Ink;
            p.lineWidth = 1.2f;
            p.Stroke();

            // four toes over the sole's front, biggest first
            for (int i = 0; i < 4; i++)
            {
                float tx = -3.6f + i * 2.6f, ty = -Ry - 2.6f + i * 0.9f, tr = 1.9f - i * 0.25f;
                var q = sc + new Vector2(tx * ct - ty * st, tx * st + ty * ct);
                p.BeginPath();
                p.Arc(q, tr, new Angle(0f), new Angle(360f));
                p.fillColor = foot;
                p.Fill();
            }

            // a pip per charge, under the icon
            const float PipR = 4f, PipGap = 11f;
            float x0 = c.x - (max - 1) * PipGap * 0.5f;
            for (int i = 0; i < max; i++)
                HellHudStyle.Diamond(p, new Vector2(x0 + i * PipGap, c.y + IconR + 9f), PipR,
                    HellAbilityIcons.Pip(i, charges, fraction), col, HellHudStyle.Plate);
        }

        /// <summary>Time Dilation as a fuel gauge: a sand ring (clockwise from twelve) holding what is left, an hourglass in the
        /// middle with that much sand on top, and a bone tick at the share it needs to start. Ember while the world is slowed;
        /// dim under the start share.</summary>
        static void DrawDilation(Painter2D p, Vector2 c, float fuel01, bool active, bool flash)
        {
            bool canStart = HellAbilityIcons.CanStartDilation(fuel01);
            var col = flash ? Color.white : active ? HellHudStyle.EmberHot : canStart ? HellHudStyle.Sand : HellHudStyle.Dim(HellHudStyle.Ember, 0.8f);
            Disc(p, c, IconR + RingPx * 0.5f + 2f);
            // the fuel never reads "dim while filling" here: what is in the tank is usable, so draw the arc at full colour
            Ring(p, c, IconR, 1f, HellHudStyle.Dim(HellHudStyle.Sand, 0.25f), false);
            fuel01 = Mathf.Clamp01(fuel01);
            if (fuel01 > 0.002f)
            {
                p.BeginPath();
                p.Arc(c, IconR, new Angle(-90f), new Angle(-90f + 360f * fuel01));
                p.strokeColor = col;
                p.lineWidth = RingPx;
                p.lineCap = LineCap.Butt;
                p.Stroke();
            }
            // the start mark
            float m = -90f + 360f * HellAbilityIcons.DilationStartShare;
            p.BeginPath();
            p.MoveTo(OnCircle(c, IconR - 4f, m));
            p.LineTo(OnCircle(c, IconR + 4f, m));
            p.strokeColor = HellHudStyle.Bone;
            p.lineWidth = 2f;
            p.lineCap = LineCap.Butt;
            p.Stroke();
            Hourglass(p, c, 9f, fuel01, col);
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
            if (_chipRow != null) _chipRow.RemoveFromHierarchy();
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
