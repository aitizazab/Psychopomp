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
    /// The 1-of-3 floor skill pick (docs/HELL-ABILITIES.md): on <see cref="HellRunEvents.FloorCleared"/> the game stops
    /// (time 0 through <see cref="HellTime"/>, the player's input off) and three cards offer skills the player doesn't own
    /// (<see cref="HellSkillPool.Offer"/>). Pick with the mouse, 1 / 2 / 3, or the pad (D-pad left/right, A / Cross); the
    /// skill is granted and <see cref="HellRunEvents.ContinueAfterPick"/> called. Each card: the flavour's colour, the icon
    /// text big, the name, the description. In the editor F9 opens a pick, to try it without a floor.
    /// </summary>
    public sealed class HellSkillPick : MonoBehaviour
    {
        const string TimeKey = "skillpick";

        public static bool Open { get; private set; }

        HellAbilities _a;
        VisualElement _layer;
        readonly List<VisualElement> _cards = new List<VisualElement>();
        List<HellSkillDef> _offer;
        int _focus;
        System.Random _rng;
        CursorLockMode _cursorWas;

        void Awake()
        {
            _a = GetComponent<HellAbilities>();
            _rng = new System.Random(Environment.TickCount);
        }

        void OnEnable() => HellRunEvents.FloorCleared += OnFloorCleared;

        void OnDisable()
        {
            HellRunEvents.FloorCleared -= OnFloorCleared;
            if (Open) Close();
        }

        void OnFloorCleared(int floor) => Show(floor);

        /// <summary>Opens the pick (a floor was cleared). With nothing left to offer the run goes straight on.</summary>
        public void Show(int floor)
        {
            if (Open || _a == null) return;
            var owned = new List<string>(_a.Owned);
            _offer = HellSkillPool.Offer(HellSkills.All, owned, 3, _rng.Next);
            if (_offer.Count == 0) { HellRunEvents.ContinueAfterPick(); return; }
            var hud = SkylineHud.Instance;
            if (hud == null || hud.Root == null)
            {
                // no HUD to draw on: take the first offer so the run never stalls
                Debug.LogWarning("HELL_SKILL no HUD for the pick screen; granting " + _offer[0].id);
                _a.Grant(_offer[0].id);
                HellRunEvents.ContinueAfterPick();
                return;
            }
            Open = true;
            HellTime.Request(TimeKey, 0f);
            SkylineDeviceInput.Paused = true;
            _cursorWas = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Build(hud.Root, floor);
            _focus = 0;
            Focus();
            Debug.Log("HELL_SKILL pick after floor " + floor + ": " + string.Join(", ", _offer.ConvertAll(s => s.id)));
        }

        void Update()
        {
            if (!Open)
            {
                if (Application.isEditor && Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame && !SkylineDeviceInput.Paused) Show(0);
                return;
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) { Pick(0); return; }
                if (kb.digit2Key.wasPressedThisFrame) { Pick(1); return; }
                if (kb.digit3Key.wasPressedThisFrame) { Pick(2); return; }
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Move(-1);
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Move(1);
                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) { Pick(_focus); return; }
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) Move(-1);
                if (pad.dpad.right.wasPressedThisFrame) Move(1);
                if (pad.buttonSouth.wasPressedThisFrame) Pick(_focus);
            }
        }

        void Move(int d)
        {
            _focus = (_focus + d + _cards.Count) % _cards.Count;
            Focus();
        }

        void Focus()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var s = _cards[i].style;
                bool on = i == _focus;
                s.scale = new Scale(on ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one);
                s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = on ? Color.white : SkylineHudStyle.Ink;
            }
        }

        void Pick(int i)
        {
            if (!Open || _offer == null || i < 0 || i >= _offer.Count) return;
            var id = _offer[i].id;
            HellAbilityAudio.Play("skill_pick", transform.position);
            Close();
            _a.Grant(id);
            HellRunEvents.ContinueAfterPick();
        }

        void Close()
        {
            Open = false;
            if (_layer != null) _layer.RemoveFromHierarchy();
            _layer = null;
            _cards.Clear();
            SkylineDeviceInput.Paused = false;
            HellTime.Clear(TimeKey);
            Cursor.lockState = _cursorWas;
            Cursor.visible = _cursorWas != CursorLockMode.Locked;
        }

        void Build(VisualElement root, int floor)
        {
            _layer = new VisualElement { name = "hell-skill-pick" };
            var s = _layer.style;
            s.position = Position.Absolute;
            s.left = 0; s.top = 0; s.right = 0; s.bottom = 0;
            s.backgroundColor = new Color(0.03f, 0.01f, 0.05f, 0.82f);
            s.flexDirection = FlexDirection.Column;
            s.alignItems = Align.Center;
            s.justifyContent = Justify.Center;
            root.Add(_layer);

            var title = Text(floor > 0 ? "FLOOR " + floor + " CLEARED" : "CHOOSE A POWER", 54f, SkylineHudStyle.Paper, true);
            title.style.marginBottom = 36f;
            _layer.Add(title);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            _layer.Add(row);
            for (int i = 0; i < _offer.Count; i++) row.Add(Card(_offer[i], i));
        }

        VisualElement Card(HellSkillDef d, int index)
        {
            var hue = HellSkills.FlavourColour(d.flavour);
            var c = new VisualElement { pickingMode = PickingMode.Position };
            var s = c.style;
            s.width = 330f;
            s.height = 430f;
            s.marginLeft = s.marginRight = 18f;
            s.paddingLeft = s.paddingRight = 22f;
            s.paddingTop = 18f;
            s.backgroundColor = new Color(0.08f, 0.07f, 0.11f, 0.97f);
            s.borderTopWidth = 10f;
            s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 4f;
            s.borderTopColor = hue;
            s.alignItems = Align.Center;
            s.rotate = new Rotate(index == 0 ? -1.5f : index == 2 ? 1.5f : 0f);   // a hand-placed look, not a grid

            var key = Text((index + 1).ToString(), 22f, new Color(1f, 1f, 1f, 0.5f), false);
            key.style.alignSelf = Align.FlexStart;
            c.Add(key);
            var icon = Text(d.icon, 78f, hue, true);
            icon.style.marginTop = 6f;
            icon.style.marginBottom = 10f;
            c.Add(icon);
            var name = Text(d.name.ToUpperInvariant(), 34f, SkylineHudStyle.Paper, true);
            c.Add(name);
            var flav = Text(d.flavour.ToString().ToUpperInvariant(), 18f, hue, false);
            flav.style.marginBottom = 16f;
            c.Add(flav);
            var desc = Text(d.description, 21f, new Color(0.9f, 0.88f, 0.84f), false);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.unityTextAlign = TextAnchor.UpperCenter;
            c.Add(desc);

            c.RegisterCallback<PointerEnterEvent>(e => { _focus = index; Focus(); });
            c.RegisterCallback<PointerDownEvent>(e => Pick(index));
            _cards.Add(c);
            return c;
        }

        static Label Text(string text, float size, Color col, bool display)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = col;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            SkylineHudStyle.ZeroBox(l.style);
            if (display) SkylineHudStyle.ApplyFace(l.style, SkylineHudStyle.AmmoFontName);
            return l;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Open = false;
    }
}
