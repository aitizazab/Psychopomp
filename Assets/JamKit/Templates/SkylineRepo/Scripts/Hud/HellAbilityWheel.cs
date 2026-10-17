using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The ability wheel (docs/HELL-UI.md, "The ability wheel"). <b>Hold</b> the Wheel binding (Tab / D-pad left; on the
    /// Controls page) to open a radial of your powers: the three gifts (Shadow Step at twelve o'clock, Rewind, Time Dilation)
    /// and every floor skill you own, each with its state (charges, cooldown, the slow-down left, PASSIVE). While it is open
    /// <b>time slows to 0.2x</b> (<see cref="HellTime"/> key <c>wheel</c>), the look holds still and the gun is quiet: the
    /// mouse moves a pointer round the wheel (or the right stick points), the middle shows the slot's name, state and what it
    /// does. Moving, jumping and sliding go on.
    /// <para><b>What it selects:</b> the gifts and the passive skills have their own buttons or none, so the wheel only shows
    /// them. Releasing on an <b>active</b> skill binds it to the Skill button (<see cref="Binding"/>). Today Nightfall is the
    /// only active skill and binds by itself when granted, so the wheel is mostly a status and "what does this do" screen; the
    /// binding is there for the next active skill.</para>
    /// <para>Added by <see cref="HellHud"/>. Under the HUD's root (the clean picture hides it; the slow-down still applies).
    /// Replays record the wheel held (<see cref="HellReplayButtons.WheelHeld"/>) so the time scale replays; the pointer is
    /// not recorded.</para>
    /// </summary>
    public sealed class HellAbilityWheel : MonoBehaviour
    {
        public const string TimeKey = "wheel";
        public const float SlowScale = 0.2f, SlotRadius = 166f, SlotSize = 44f, SlotSizeSelected = 52f, OpenSeconds = 0.12f;
        /// <summary>The active floor skills (the Skill button fires the bound one).</summary>
        public static readonly string[] ActiveSkills = { HellSkills.Nightfall };

        /// <summary>True while the wheel is held open (other HUD pieces in the middle of the screen step aside).</summary>
        public static bool IsOpen { get; private set; }
        /// <summary>Which active skill the Skill button fires.</summary>
        public static readonly HellSkillBinding Binding = new HellSkillBinding();

        HellAbilities _a;
        SkylinePlayer _player;
        readonly HellWheelSelector _sel = new HellWheelSelector();
        readonly List<HellWheelSlot> _slots = new List<HellWheelSlot>();
        readonly List<KeylinedLabel> _icons = new List<KeylinedLabel>();
        VisualElement _root, _ring, _iconLayer;
        KeylinedLabel _name;
        Label _state, _desc, _how;
        float _open01, _lastKey = float.NaN;
        int _shown = -2;
        string _stateText;
        bool _wasHeld;

        static bool IsActiveSkill(string id) => System.Array.IndexOf(ActiveSkills, id) >= 0;

        void Start()
        {
            _a = GetComponent<HellAbilities>();
            _player = GetComponent<SkylinePlayer>();
            Binding.Reset();   // a new player (a new run after the title): bind again from what it owns
            if (_a != null)
            {
                _a.Granted += OnGranted;
                foreach (var id in _a.Owned) Binding.Granted(id, IsActiveSkill(id));
            }
        }

        void OnGranted(string id) => Binding.Granted(id, IsActiveSkill(id));

        bool Held()
        {
            if (HellAbilities.ScriptedButtons != null) return HellAbilities.ScriptedButtons().wheelHeld;
            bool mouseOk = Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked;
            return SkylineBindings.Held(BindDevice.KeyboardMouse, BindAction.Wheel, mouseOk) || SkylineBindings.Held(BindDevice.Pad, BindAction.Wheel, mouseOk);
        }

        bool Allowed => _a != null && !SkylineDeviceInput.Paused && !SkylinePauseMenu.IsOpen && !HellSkillPick.Open
            && (_player == null || _player.Armor == null || !_player.Armor.Wrecked);

        /// <summary>For the UI stills (<see cref="HellUiShots"/>): holds the wheel open (true) or lets it go, with slot
        /// <paramref name="select"/> pointed at.</summary>
        public void ShowForShot(bool open, int select = 0)
        {
            _shotHold = open;
            if (open && !IsOpen && _a != null)
            {
                Open();
                _sel.Open(_slots.Count, select);
                _wasHeld = true;
            }
        }
        bool _shotHold;

        void Update()
        {
            bool held = Held() || _shotHold;
            bool allowed = Allowed;
            if (!IsOpen && held && !_wasHeld && allowed && !_a.Aiming) Open();
            else if (IsOpen && (!held || !allowed)) Close(held == false && allowed);
            _wasHeld = held;

            float dt = Time.unscaledDeltaTime;
            _open01 = Mathf.MoveTowards(_open01, IsOpen ? 1f : 0f, dt / OpenSeconds);
            if (_root == null) { if (_open01 > 0f) Build(); else return; }
            if (_root == null) return;
            var disp = _open01 > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (_root.style.display != disp) _root.style.display = disp;
            if (_open01 <= 0f) return;
            _root.style.opacity = _open01;
            float s = 0.86f + 0.14f * _open01;
            _root.style.scale = new Scale(new Vector3(s, s, 1f));

            if (IsOpen && HellAbilities.ScriptedButtons == null) Point(dt);
            if (_sel.Selected != _shown) ShowSelected();
            RefreshState();
        }

        void Point(float dt)
        {
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var d = mouse.delta.ReadValue();
                float k = Screen.height > 0 ? HellHudPlaces.RefH / Screen.height : 1f;
                if (d.sqrMagnitude > 0f) _sel.Mouse(d.x * k, d.y * k);
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                var st = pad.rightStick.ReadValue();
                if (st.sqrMagnitude > 0.01f) _sel.Stick(st.x, st.y);
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) _sel.Step(-1);
                if (kb.eKey.wasPressedThisFrame) _sel.Step(1);
            }
        }

        void Open()
        {
            IsOpen = true;
            _slots.Clear();
            _slots.AddRange(HellWheelSlots.Build(SkillOrder(), _a.Has, IsActiveSkill));
            int start = 0;
            for (int i = 0; i < _slots.Count; i++) if (_slots[i].active && _slots[i].id == Binding.Bound) start = i;
            _sel.Open(_slots.Count, start);
            _shown = -2;
            HellTime.Request(TimeKey, SlowScale);
            SkylineDeviceInput.LookHeld = true;
            if (_root != null) BuildSlots();
            HellAbilityAudio.Play("wheel_open", transform.position);
            Debug.Log("HELL_WHEEL open (" + _slots.Count + " slots)");
        }

        void Close(bool pick)
        {
            IsOpen = false;
            HellTime.Clear(TimeKey);
            SkylineDeviceInput.LookHeld = false;
            if (pick && _sel.Selected >= 0 && _sel.Selected < _slots.Count)
            {
                var s = _slots[_sel.Selected];
                if (Binding.Pick(s.id, s.active))
                {
                    HellAbilityAudio.Play("wheel_bind", transform.position);
                    Debug.Log("HELL_WHEEL bound " + s.id);
                }
            }
            HellAbilityAudio.Play("wheel_close", transform.position);
        }

        IEnumerable<string> SkillOrder()
        {
            foreach (var d in HellSkills.All) yield return d.id;
        }

        static HellSkillDef Def(string id)
        {
            foreach (var d in HellSkills.All) if (d.id == id) return d;
            return null;
        }

        // ------------------------------------------------------------------ building

        void Build()
        {
            var hud = SkylineHud.Instance;
            if (hud == null || hud.Safe == null) return;
            _root = SkylineHudStyle.Layer(hud.Safe, "hell-ability-wheel");
            HellHudStyle.Place(_root, HellHudPlaces.Wheel);
            _root.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            _ring = SkylineHudStyle.Layer(_root, "wheel-ring");
            _ring.style.left = _ring.style.top = _ring.style.right = _ring.style.bottom = 0;
            _ring.generateVisualContent += DrawRing;
            _iconLayer = SkylineHudStyle.Layer(_root, "wheel-icons");
            _iconLayer.style.left = _iconLayer.style.top = _iconLayer.style.right = _iconLayer.style.bottom = 0;

            var mid = SkylineHudStyle.Layer(_root, "wheel-middle");
            mid.style.left = Length.Percent(50);
            mid.style.top = Length.Percent(50);
            mid.style.width = 230f;
            mid.style.marginLeft = -115f;
            mid.style.marginTop = -74f;
            mid.style.alignItems = Align.Center;
            _name = HellHudStyle.Label(30f, HellFaces.Heading, HellHudStyle.Ash, 2.5f, 1f);
            _name.SetWrap(false, TextAnchor.MiddleCenter);
            mid.Add(_name);
            _state = FineLabel(mid, 20f, HellHudStyle.Sand);
            _state.style.marginTop = 2f;
            _desc = FineLabel(mid, 18f, HellHudStyle.Ash);
            _desc.style.whiteSpace = WhiteSpace.Normal;
            _desc.style.marginTop = 8f;
            _how = FineLabel(_root, 18f, HellHudStyle.AshDim);
            _how.style.position = Position.Absolute;
            _how.style.left = 0;
            _how.style.right = 0;
            _how.style.bottom = -30f;
            BuildSlots();
            _root.style.display = DisplayStyle.None;
        }

        static Label FineLabel(VisualElement parent, float px, Color c)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            SkylineHudStyle.ZeroBox(l.style);
            l.style.fontSize = px;
            l.style.color = c;
            l.style.unityTextAlign = TextAnchor.UpperCenter;
            l.style.textShadow = new TextShadow { offset = new Vector2(0f, 1.5f), blurRadius = 2f, color = HellHudStyle.Ink };
            SkylineHudStyle.ApplyFace(l.style, HellFaces.Fine);
            parent.Add(l);
            return l;
        }

        void BuildSlots()
        {
            foreach (var i in _icons) i.RemoveFromHierarchy();
            _icons.Clear();
            float half = HellHudPlaces.Wheel.w * 0.5f;
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                var lab = HellHudStyle.Label(26f, HellFaces.Numbers, SlotColour(s), 2f);
                var def = s.gift ? null : Def(s.id);
                lab.SetText(def != null ? def.icon : "");
                lab.SetWrap(false, TextAnchor.MiddleCenter);
                HellWheel.SlotPosition(i, _slots.Count, SlotRadius, out float x, out float y);
                lab.style.position = Position.Absolute;
                lab.style.width = 90f;
                lab.style.left = half + x - 45f;
                lab.style.top = half + y - 17f;
                _iconLayer.Add(lab);
                _icons.Add(lab);
            }
            _how.text = "MOVE TO CHOOSE    RELEASE " + HellInputDevice.Short(BindAction.Wheel) + " TO CLOSE";
            _ring.MarkDirtyRepaint();
        }

        static Color SlotColour(HellWheelSlot s)
        {
            if (s.gift) return s.id == HellWheelSlots.ShadowStep ? HellHudStyle.Shadow : HellHudStyle.Sand;
            var d = Def(s.id);
            return d != null ? HellHudStyle.Flavour(d.flavour) : HellHudStyle.Ash;
        }

        void ShowSelected()
        {
            _shown = _sel.Selected;
            _stateText = null;
            if (_shown < 0 || _shown >= _slots.Count) { _name.SetText(""); _desc.text = ""; _state.text = ""; return; }
            var s = _slots[_shown];
            var def = s.gift ? null : Def(s.id);
            _name.SetText(s.gift ? HellWheelText.GiftName(s.id) : def != null ? def.name : s.id);
            _name.SetColor(SlotColour(s));
            _desc.text = s.gift ? HellWheelText.GiftDescription(s.id) : def != null ? def.description : "";
            _ring.MarkDirtyRepaint();
        }

        /// <summary>Slot i's readiness 0..1 (1 = ready / passive).</summary>
        float Ready01(HellWheelSlot s)
        {
            switch (s.id)
            {
                case HellWheelSlots.ShadowStep:
                {
                    var c = _a.StepCharges;
                    return c.Max > 0 ? Mathf.Clamp01((c.Charges + (c.Charges < c.Max ? c.Fraction : 0f)) / c.Max) : 1f;
                }
                case HellWheelSlots.Rewind: return _a.Rewind != null ? _a.Rewind.Cooldown.Fraction : 1f;
                case HellWheelSlots.Dilation: return _a.Dilation.Active ? _a.Dilation.HoldLeft01 : 1f;
            }
            if (s.id == HellSkills.Nightfall)
            {
                var n = GetComponent<HellSkillNightfall>();
                return n != null ? n.Cooldown.Fraction : 1f;
            }
            return 1f;
        }

        string StateText(HellWheelSlot s)
        {
            switch (s.id)
            {
                case HellWheelSlots.ShadowStep:
                    return HellAbilities.ShadowStepBlocked ? "BLOCKED" : HellWheelText.Charges(_a.StepCharges.Charges, _a.StepCharges.Max);
                case HellWheelSlots.Rewind:
                {
                    var cd = _a.Rewind != null ? _a.Rewind.Cooldown : null;
                    return cd == null || cd.Ready ? HellWheelText.Ready : HellWheelText.Seconds(cd.Remaining);
                }
                case HellWheelSlots.Dilation:
                    return _a.Dilation.Active ? HellWheelText.Active : HellWheelText.Ready;
            }
            if (s.active)
            {
                if (s.id != Binding.Bound) return HellWheelText.Bind;
                var n = s.id == HellSkills.Nightfall ? GetComponent<HellSkillNightfall>() : null;
                if (n != null && !n.Cooldown.Ready) return HellWheelText.Seconds(n.Cooldown.Remaining);
                return HellWheelText.Bound;
            }
            return HellWheelText.Passive;
        }

        void RefreshState()
        {
            if (_shown >= 0 && _shown < _slots.Count)
            {
                var t = StateText(_slots[_shown]);
                if (!ReferenceEquals(t, _stateText))
                {
                    _stateText = t;
                    _state.text = t;
                }
            }
            float key = _sel.Selected * 1000f;
            for (int i = 0; i < _slots.Count; i++) key += Ready01(_slots[i]) * (i + 1) * 7f;
            if (key != _lastKey)
            {
                _lastKey = key;
                _ring.MarkDirtyRepaint();
            }
        }

        // ------------------------------------------------------------------ drawing

        void DrawRing(MeshGenerationContext ctx)
        {
            if (_a == null) return;
            var p = ctx.painter2D;
            float half = HellHudPlaces.Wheel.w * 0.5f;
            var c = new Vector2(half, half);
            // the backdrop: a dark disc with an ember rim, the middle a deeper well
            p.BeginPath();
            p.Arc(c, half - 6f, new Angle(0f), new Angle(360f));
            p.fillColor = new Color(0.04f, 0.02f, 0.03f, 0.78f);
            p.Fill();
            p.strokeColor = HellHudStyle.EmberEdge;
            p.lineWidth = 2f;
            p.Stroke();
            p.BeginPath();
            p.Arc(c, 120f, new Angle(0f), new Angle(360f));
            p.fillColor = new Color(0.02f, 0.01f, 0.02f, 0.85f);
            p.Fill();

            int n = _slots.Count;
            if (n <= 0) return;
            // the selected sector, a soft wedge pointing at the slot
            if (_sel.Selected >= 0)
            {
                float a = HellWheel.SlotAngle(_sel.Selected, n) - 90f, step = 360f / n;
                var col = SlotColour(_slots[_sel.Selected]);
                p.BeginPath();
                p.Arc(c, half - 8f, new Angle(a - step * 0.5f), new Angle(a + step * 0.5f));
                p.Arc(c, 122f, new Angle(a + step * 0.5f), new Angle(a - step * 0.5f), ArcDirection.CounterClockwise);
                p.ClosePath();
                p.fillColor = new Color(col.r, col.g, col.b, 0.16f);
                p.Fill();
            }
            for (int i = 0; i < n; i++)
            {
                var s = _slots[i];
                HellWheel.SlotPosition(i, n, SlotRadius, out float x, out float y);
                var sc = c + new Vector2(x, y);
                bool sel = i == _sel.Selected;
                var col = SlotColour(s);
                float r = sel ? SlotSizeSelected : SlotSize;
                HellHudStyle.Dial(p, sc, r * 0.82f, Ready01(s), col, sel ? 7f : 5f);
                if (s.gift) DrawGiftIcon(p, s.id, sc, col);
                if (s.active && s.id == Binding.Bound)
                {
                    // a small violet pip: this one is on the Skill button
                    HellHudStyle.Diamond(p, sc + new Vector2(0f, r * 0.82f + 10f), 6f, 1f, HellHudStyle.Shadow, HellHudStyle.Plate);
                }
                if (!s.gift && !s.active)
                {
                    // passive: a thin bone tick under the slot
                    p.BeginPath();
                    p.MoveTo(sc + new Vector2(-8f, r * 0.82f + 9f));
                    p.LineTo(sc + new Vector2(8f, r * 0.82f + 9f));
                    p.strokeColor = HellHudStyle.Bone;
                    p.lineWidth = 2f;
                    p.Stroke();
                }
            }
            // the pointer: a short ember tick on the inner rim toward the cursor
            if (_sel.Selected >= 0)
            {
                float a = (HellWheel.SlotAngle(_sel.Selected, n) - 90f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                p.BeginPath();
                p.MoveTo(c + dir * 104f);
                p.LineTo(c + dir * 118f);
                p.strokeColor = HellHudStyle.Ember;
                p.lineWidth = 4f;
                p.Stroke();
            }
        }

        void DrawGiftIcon(Painter2D p, string id, Vector2 c, Color col)
        {
            if (id == HellWheelSlots.ShadowStep) HellHudStyle.Diamond(p, c, 16f, 1f, col, HellHudStyle.Plate);
            else if (id == HellWheelSlots.Dilation) HellHud.Hourglass(p, c, 15f, _a.Dilation.Active ? _a.Dilation.HoldLeft01 : 1f, col);
            else
            {
                for (int pass = 0; pass < 2; pass++)
                    for (int k = 0; k < 2; k++)
                    {
                        float dx = k * 9f - 6f;
                        p.BeginPath();
                        p.MoveTo(c + new Vector2(dx + 7f, -9f));
                        p.LineTo(c + new Vector2(dx - 2f, 0f));
                        p.LineTo(c + new Vector2(dx + 7f, 9f));
                        p.strokeColor = pass == 0 ? HellHudStyle.Ink : col;
                        p.lineWidth = pass == 0 ? 6f : 3f;
                        p.lineJoin = LineJoin.Miter;
                        p.Stroke();
                    }
            }
        }

        void OnDisable()
        {
            if (IsOpen) Close(false);
            _open01 = 0f;
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        void OnDestroy()
        {
            if (_a != null) _a.Granted -= OnGranted;
            if (_root != null) _root.RemoveFromHierarchy();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsOpen = false;
            Binding.Reset();
            SkylineDeviceInput.LookHeld = false;
        }
    }
}
