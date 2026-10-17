using System;
using System.Collections.Generic;
using System.Linq;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell UI's pure parts (docs/HELL-UI.md): the layout rules, the ability wheel's maths and binding, the clock
    /// text cache, the vessel's cracks, the dilation's hold, the faces and the title, the wheel's binding defaults.</summary>
    public class HellHudTests
    {
        // ------------------------------------------------------------------ layout

        static IEnumerable<HellHudState> States() => Enum.GetValues(typeof(HellHudState)).Cast<HellHudState>();

        [TestCase(1920f, 1080f)]   // 1080p
        [TestCase(2560f, 1440f)]   // 1440p (the same panel)
        [TestCase(3840f, 2160f)]   // 4K
        [TestCase(1920f, 1200f)]   // 16:10
        [TestCase(2560f, 1080f)]   // 21:9
        [TestCase(3440f, 1440f)]   // 21:9
        [TestCase(1280f, 720f)]
        public void NothingThatShowsTogetherOverlaps(float screenW, float screenH)
        {
            HellHudPlaces.Panel(screenW, screenH, out float w, out float h);
            var bad = new List<string>();
            foreach (var s in States()) bad.AddRange(HellHudPlaces.Problems(s, w, h));
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }

        [Test]
        public void SixteenByNineScreensShareOnePanel()
        {
            foreach (var (sw, sh) in new[] { (1920f, 1080f), (2560f, 1440f), (3840f, 2160f), (1280f, 720f) })
            {
                HellHudPlaces.Panel(sw, sh, out float w, out float h);
                Assert.That(w, Is.EqualTo(1920f).Within(0.5f));
                Assert.That(h, Is.EqualTo(1080f).Within(0.5f));
            }
        }

        [Test]
        public void TheRuleBitesWhenTwoThingsCollide()
        {
            var a = new HellHudBox("a", HellHAlign.Left, HellVAlign.Top, 10f, 10f, 100f, 50f).Resolve(1920f, 1080f);
            var b = new HellHudBox("b", HellHAlign.Left, HellVAlign.Top, 60f, 40f, 100f, 50f).Resolve(1920f, 1080f);
            var c = new HellHudBox("c", HellHAlign.Left, HellVAlign.Top, 110f, 10f, 100f, 50f).Resolve(1920f, 1080f);
            Assert.That(a.Overlaps(b), Is.True);
            Assert.That(a.Overlaps(c), Is.False, "touching edges are not an overlap");
        }

        [Test]
        public void BoxesResolveFromTheirAnchors()
        {
            var r = HellHudPlaces.Ammo.Resolve(1920f, 1080f);
            Assert.That(r.Right, Is.EqualTo(1920f - HellHudPlaces.Margin).Within(0.01f));
            Assert.That(r.Bottom, Is.EqualTo(1080f - HellHudPlaces.Margin).Within(0.01f));
            var o = HellHudPlaces.Objective.Resolve(1920f, 1080f);
            Assert.That(o.x + o.w * 0.5f, Is.EqualTo(960f).Within(0.01f), "centred");
            var hint = HellHudPlaces.Hint.Resolve(1920f, 1080f);
            Assert.That(hint.y, Is.EqualTo(1080f * HellHudPlaces.Hint.yShare).Within(0.01f));
            var sub = HellHudPlaces.Subtitles.Resolve(1920f, 1080f);
            Assert.That(sub.w, Is.EqualTo(1920f * HellHudPlaces.Subtitles.wShare).Within(0.01f));
        }

        [Test]
        public void TheDecisionPromptSitsBetweenTheRingsAndTheHint()
        {
            foreach (var (sw, sh) in new[] { (1920f, 1080f), (2560f, 1080f), (1920f, 1200f) })
            {
                HellHudPlaces.Panel(sw, sh, out float w, out float h);
                var ring = HellHudPlaces.Crosshair.Resolve(w, h);
                var prompt = HellHudPlaces.Prompt.Resolve(w, h);
                var hint = HellHudPlaces.Hint.Resolve(w, h);
                Assert.That(prompt.y, Is.GreaterThanOrEqualTo(ring.Bottom));
                Assert.That(hint.y, Is.GreaterThanOrEqualTo(prompt.Bottom));
            }
        }

        [Test]
        public void SubtitlesClearTheLetterbox()
        {
            foreach (var (sw, sh) in new[] { (1920f, 1080f), (2560f, 1080f), (1920f, 1200f) })
            {
                HellHudPlaces.Panel(sw, sh, out float w, out float h);
                var sub = HellHudPlaces.Subtitles.Resolve(w, h);
                Assert.That(sub.Bottom, Is.LessThanOrEqualTo(h * (1f - HellHudPlaces.LetterboxShare)));
            }
        }

        // ------------------------------------------------------------------ the wheel's maths

        [TestCase(0f, 1f, 4, 0)]       // up
        [TestCase(1f, 0f, 4, 1)]       // right
        [TestCase(0f, -1f, 4, 2)]      // down
        [TestCase(-1f, 0f, 4, 3)]      // left
        [TestCase(0.2f, 1f, 4, 0)]     // a little right of up
        [TestCase(-0.2f, 1f, 4, 0)]    // a little left of up: still slot 0 (wraps round)
        [TestCase(1f, 1f, 8, 1)]       // 45 degrees with 8 slots
        public void PickPointsAtTheSlotUnderTheDirection(float x, float y, int count, int expected)
        {
            Assert.That(HellWheel.Pick(x, y, count, 0.1f), Is.EqualTo(expected));
        }

        [Test]
        public void SlotBoundariesAreHalfWayBetweenMiddles()
        {
            // 12 slots: 30 degrees each, slot 0 spans -15..15
            Assert.That(HellWheel.Pick(Sin(14f), Cos(14f), 12, 0.1f), Is.EqualTo(0));
            Assert.That(HellWheel.Pick(Sin(16f), Cos(16f), 12, 0.1f), Is.EqualTo(1));
            Assert.That(HellWheel.Pick(Sin(-14f), Cos(-14f), 12, 0.1f), Is.EqualTo(0));
            Assert.That(HellWheel.Pick(Sin(-16f), Cos(-16f), 12, 0.1f), Is.EqualTo(11));
        }

        static float Sin(float deg) => (float)Math.Sin(deg * Math.PI / 180.0);
        static float Cos(float deg) => (float)Math.Cos(deg * Math.PI / 180.0);

        [Test]
        public void TheDeadZoneAndNoSlotsPickNothing()
        {
            Assert.That(HellWheel.Pick(0.1f, 0.1f, 6, HellWheel.StickDeadZone), Is.EqualTo(-1));
            Assert.That(HellWheel.Pick(0f, 1f, 0, 0.1f), Is.EqualTo(-1));
            Assert.That(HellWheel.Pick(float.NaN, 1f, 6, 0.1f), Is.EqualTo(-1));
        }

        [Test]
        public void SlotPositionsGoClockwiseFromTwelve()
        {
            HellWheel.SlotPosition(0, 4, 100f, out float x0, out float y0);
            Assert.That(x0, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(y0, Is.EqualTo(-100f).Within(1e-3f), "screen y is down: twelve o'clock is negative");
            HellWheel.SlotPosition(1, 4, 100f, out float x1, out float y1);
            Assert.That(x1, Is.EqualTo(100f).Within(1e-3f));
            Assert.That(y1, Is.EqualTo(0f).Within(1e-3f));
            // the drawn position and the pick agree
            for (int i = 0; i < 7; i++)
            {
                HellWheel.SlotPosition(i, 7, 50f, out float x, out float y);
                Assert.That(HellWheel.Pick(x, -y, 7, 1f), Is.EqualTo(i));
            }
        }

        [Test]
        public void TheMouseMovesAClampedPointerAndKeepsTheLastSlot()
        {
            var s = new HellWheelSelector();
            s.Open(4, 0);
            Assert.That(s.Selected, Is.EqualTo(0));
            s.Mouse(10f, 0f);                                  // inside the dead zone: no change
            Assert.That(s.Selected, Is.EqualTo(0));
            s.Mouse(500f, 0f);                                 // far right: clamped to the reach, slot 1
            Assert.That(s.Selected, Is.EqualTo(1));
            Assert.That(Math.Sqrt(s.CursorX * s.CursorX + s.CursorY * s.CursorY), Is.EqualTo(HellWheel.CursorReach).Within(0.01));
            s.Mouse(-HellWheel.CursorReach, 0f);               // back to the middle: keeps slot 1
            Assert.That(s.Selected, Is.EqualTo(1));
            s.Mouse(0f, -300f);                                // down
            Assert.That(s.Selected, Is.EqualTo(2));
        }

        [Test]
        public void TheStickSpringingBackKeepsItsSlot()
        {
            var s = new HellWheelSelector();
            s.Open(6);
            Assert.That(s.Selected, Is.EqualTo(-1));
            s.Stick(-1f, 0f);
            int left = s.Selected;
            Assert.That(left, Is.GreaterThanOrEqualTo(0));
            s.Stick(0.05f, 0.02f);
            Assert.That(s.Selected, Is.EqualTo(left));
        }

        [Test]
        public void StepsWrapRound()
        {
            var s = new HellWheelSelector();
            s.Open(3);
            Assert.That(s.Step(1), Is.EqualTo(0));
            Assert.That(s.Step(1), Is.EqualTo(1));
            Assert.That(s.Step(1), Is.EqualTo(2));
            Assert.That(s.Step(1), Is.EqualTo(0));
            Assert.That(s.Step(-1), Is.EqualTo(2));
        }

        [Test]
        public void TheSkillButtonBindsActiveSkillsOnly()
        {
            var b = new HellSkillBinding();
            Assert.That(b.Fires("nightfall"), Is.True, "nothing bound: the button fires whatever asks");
            b.Granted("ambush", false);
            Assert.That(b.Bound, Is.Null, "a passive skill never binds");
            b.Granted("nightfall", true);
            Assert.That(b.Bound, Is.EqualTo("nightfall"), "the first active skill binds by itself");
            b.Granted("other", true);
            Assert.That(b.Bound, Is.EqualTo("nightfall"), "a second one waits for the wheel");
            Assert.That(b.Pick("ambush", false), Is.False);
            Assert.That(b.Pick("other", true), Is.True);
            Assert.That(b.Fires("other"), Is.True);
            Assert.That(b.Fires("nightfall"), Is.False);
            Assert.That(b.Pick("other", true), Is.False, "picking the bound one changes nothing");
            b.Reset();
            Assert.That(b.Bound, Is.Null);
        }

        [Test]
        public void TheWheelListsTheGiftsThenOwnedSkillsInCatalogueOrder()
        {
            var order = new[] { "a", "b", "c", "d" };
            var owned = new HashSet<string> { "d", "b" };
            var slots = HellWheelSlots.Build(order, owned.Contains, id => id == "d");
            Assert.That(slots.Select(s => s.id), Is.EqualTo(new[] { HellWheelSlots.ShadowStep, HellWheelSlots.Rewind, HellWheelSlots.Dilation, "b", "d" }));
            Assert.That(slots.Take(3).All(s => s.gift), Is.True);
            Assert.That(slots[3].active, Is.False);
            Assert.That(slots[4].active, Is.True);
            Assert.That(HellWheelSlots.Build(order, id => true, null, max: 5).Count, Is.EqualTo(5));
            foreach (var g in slots.Take(3))
            {
                Assert.That(HellWheelText.GiftName(g.id), Is.Not.Empty);
                Assert.That(HellWheelText.GiftDescription(g.id), Is.Not.Empty);
            }
        }

        [Test]
        public void TheWheelsStateWordsAreMadeOnce()
        {
            Assert.That(HellWheelText.Seconds(8.2f), Is.EqualTo("9 s"));
            Assert.That(ReferenceEquals(HellWheelText.Seconds(8.5f), HellWheelText.Seconds(8.9f)), Is.True);
            Assert.That(HellWheelText.Seconds(-3f), Is.EqualTo("0 s"));
            Assert.That(HellWheelText.Seconds(float.NaN), Is.EqualTo("0 s"));
            Assert.That(HellWheelText.Charges(1, 2), Is.EqualTo("1 / 2"));
            Assert.That(ReferenceEquals(HellWheelText.Charges(1, 2), HellWheelText.Charges(1, 2)), Is.True);
        }

        // ------------------------------------------------------------------ the rest

        [Test]
        public void ClockTextIsMinutesAndSecondsAndCached()
        {
            Assert.That(HellClockText.Of(0), Is.EqualTo("0:00"));
            Assert.That(HellClockText.Of(65), Is.EqualTo("1:05"));
            Assert.That(HellClockText.Of(65.9f), Is.EqualTo("1:05"));
            Assert.That(HellClockText.Of(-4f), Is.EqualTo("0:00"));
            Assert.That(ReferenceEquals(HellClockText.Of(125), HellClockText.Of(125.4f)), Is.True, "no new string for a seen second");
        }

        [TestCase(1f, 0)]
        [TestCase(0.84f, 0)]
        [TestCase(0.83f, 1)]
        [TestCase(0.5f, 3)]
        [TestCase(0.1f, 5)]
        [TestCase(0f, 5)]
        [TestCase(float.NaN, 0)]
        public void TheVesselCracksAsItDrains(float share, int cracks)
        {
            Assert.That(HellVesselModel.Cracks(share), Is.EqualTo(cracks));
        }

        [Test]
        public void TheDilationHoldDrainsFromOneToZero()
        {
            var d = new TimeDilationState { holdSeconds = 3f, easeIn = 0.15f, easeOut = 0.15f };
            Assert.That(d.HoldLeft01, Is.EqualTo(0f));
            d.Press(0.5f);
            Assert.That(d.HoldLeft01, Is.EqualTo(1f).Within(1e-4f));
            d.Tick(1.575f);
            Assert.That(d.HoldLeft01, Is.EqualTo(0.5f).Within(1e-3f));
            d.Tick(2f);
            Assert.That(d.HoldLeft01, Is.EqualTo(0f));
        }

        [Test]
        public void TheFacesAndTheTitle()
        {
            var faces = new[] { HellFaces.Title, HellFaces.Heading, HellFaces.Fine, HellFaces.Numbers, HellFaces.Comic };
            Assert.That(faces.Distinct().Count(), Is.EqualTo(faces.Length));
            Assert.That(HellFlowText.Title, Is.EqualTo("OVERDUE"));
            Assert.That(HellFlowText.Credits[0].text, Is.EqualTo(HellFlowText.Title), "the credits open on the title");
            foreach (var name in new[] { "Cinzel Decorative", "Metal Mania", "Barlow Condensed" })
                Assert.That(HellFlowText.Credits.Any(c => c.text.Contains(name)), Is.True, name + " is credited");
        }

        [Test]
        public void BajaAndVesperHaveTheirOwnSubtitleColours()
        {
            var white = SpeakerColours.Of("someone");
            Assert.That(SpeakerColours.Of("BAJA"), Is.Not.EqualTo(white));
            Assert.That(SpeakerColours.Of("VESPER"), Is.Not.EqualTo(white));
            Assert.That(SpeakerColours.Of("BAJA"), Is.Not.EqualTo(SpeakerColours.Of("VESPER")));
        }

        [Test]
        public void TheWheelHasItsOwnFreeControls()
        {
            var t = new BindingTable();
            Assert.That(t.Get(BindDevice.KeyboardMouse, BindAction.Wheel), Is.EqualTo("<Keyboard>/tab"));
            Assert.That(t.Get(BindDevice.Pad, BindAction.Wheel), Is.EqualTo("<Gamepad>/dpad/left"));
            foreach (BindDevice d in Enum.GetValues(typeof(BindDevice)))
                foreach (var a in BindingTable.AllActions)
                    if (a != BindAction.Wheel)
                        foreach (var p in BindingTable.Split(t.Get(d, a)))
                            Assert.That(BindingTable.Uses(t.Get(d, BindAction.Wheel), p), Is.False, a + " shares the wheel's control");
            Assert.That(BindingTable.AllActions, Does.Contain(BindAction.Wheel));
        }
    }
}
