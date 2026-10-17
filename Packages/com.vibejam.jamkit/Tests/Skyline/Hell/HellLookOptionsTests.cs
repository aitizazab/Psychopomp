using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell look test's switches (docs/HELL-LOOK-TEST.md).</summary>
    public class HellLookOptionsTests
    {
        [Test]
        public void NoSwitchesLeaveTheSceneAlone()
        {
            var o = HellLookOptions.Parse(new[] { "HellDescent.exe" });
            Assert.That(o.look, Is.EqualTo(HellLookDirection.None));
            Assert.That(o.outline, Is.EqualTo(HellOutlineVariant.SoulfirePulse), "the creator's pick is the default");
            Assert.That(o.studs, Is.EqualTo(HellStudColour.Bone));
            Assert.That(o.Any, Is.False);
        }

        [Test]
        public void ALookDefaultsToTheSoulfirePulse()
        {
            var o = HellLookOptions.Parse(new[] { "x", "-hellLook", "b" });
            Assert.That(o.look, Is.EqualTo(HellLookDirection.B));
            Assert.That(o.outline, Is.EqualTo(HellOutlineVariant.SoulfirePulse));
            Assert.That(HellLookOptions.Pulses(o.outline), Is.True);
            Assert.That(o.outlineGiven, Is.False);
        }

        [TestCase("lime", HellOutlineVariant.Lime)]
        [TestCase("soulfire", HellOutlineVariant.Soulfire)]
        [TestCase("whitehot", HellOutlineVariant.WhiteHot)]
        [TestCase("white-hot", HellOutlineVariant.WhiteHot)]
        [TestCase("pulse", HellOutlineVariant.Pulse)]
        [TestCase("kit", HellOutlineVariant.Kit)]
        [TestCase("soulfire-pulse", HellOutlineVariant.SoulfirePulse)]
        [TestCase("off", HellOutlineVariant.Off)]
        public void OutlinesParse(string s, HellOutlineVariant v)
        {
            var o = HellLookOptions.Parse(new[] { "-hellLook", "A", "-hellOutline", s, "-hellStuds", "bone", "-hellLookCapture", "C:/out" });
            Assert.That(o.outline, Is.EqualTo(v));
            Assert.That(o.studs, Is.EqualTo(HellStudColour.Bone));
            Assert.That(o.captureDir, Is.EqualTo("C:/out"));
        }

        [Test]
        public void TheHuesAreTheMeasuredOnes()
        {
            var lime = HellLookOptions.Hue(HellOutlineVariant.Lime);
            Assert.That(lime.r, Is.EqualTo(0xA6 / 255f).Within(1e-4));
            Assert.That(lime.g, Is.EqualTo(1f));
            Assert.That(lime.b, Is.EqualTo(0f));
            Assert.That(lime.mode, Is.EqualTo(1));
            Assert.That(HellLookOptions.Hue(HellOutlineVariant.Pulse).mode, Is.EqualTo(2));
            Assert.That(HellLookOptions.Hue(HellOutlineVariant.Kit).mode, Is.EqualTo(0));
            var soul = HellLookOptions.Hue(HellOutlineVariant.Soulfire);
            Assert.That(soul.b, Is.GreaterThan(soul.r), "soulfire is cold");
        }

        [Test]
        public void ThePulseQuickensWithTheWindUp()
        {
            Assert.That(HellLookOptions.PulseHz(0f), Is.EqualTo(0.6f).Within(1e-5), "a slow breath while idle");
            Assert.That(HellLookOptions.PulseHz(1f), Is.EqualTo(6f).Within(1e-5), "fastest just before it fires");
            Assert.That(HellLookOptions.PulseDepth(1f), Is.GreaterThan(HellLookOptions.PulseDepth(0f)));
            Assert.That(HellLookOptions.Hue(HellOutlineVariant.SoulfirePulse).mode, Is.EqualTo(2));
            Assert.That(HellLookOptions.PulseHz(0.5f), Is.GreaterThan(HellLookOptions.PulseHz(0.2f)));
            Assert.That(HellLookOptions.PulseHz(3f), Is.EqualTo(6f).Within(1e-5));
        }

        [Test]
        public void FloorsAreRowsInZ()
        {
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
            {
                var r = HellWorld.Room(f, HellRoomLayouts.Kinds[0]);
                Assert.That(HellLookOptions.FloorAt(r.entry.Z), Is.EqualTo(f));
            }
        }
    
        [Test]
        public void ADirectionPerFloor()
        {
            var o = HellLookOptions.Parse(new[] { "x.exe", "-hellLookFloors", "B,A,C" });
            Assert.That(o.ForFloor(0), Is.EqualTo(HellLookDirection.B));
            Assert.That(o.ForFloor(1), Is.EqualTo(HellLookDirection.A));
            Assert.That(o.ForFloor(2), Is.EqualTo(HellLookDirection.C));
            Assert.That(o.look, Is.EqualTo(HellLookDirection.B), "the first floor's direction leads when -hellLook is absent");
            Assert.That(o.Any, Is.True);

            var mixed = HellLookOptions.Parse(new[] { "x.exe", "-hellLook", "A", "-hellLookFloors", "c" });
            Assert.That(mixed.ForFloor(0), Is.EqualTo(HellLookDirection.C));
            Assert.That(mixed.ForFloor(1), Is.EqualTo(HellLookDirection.A), "a floor left out takes -hellLook");
            Assert.That(mixed.ForFloor(2), Is.EqualTo(HellLookDirection.A));

            Assert.That(HellLookOptions.ParseFloors("BAC"), Is.EqualTo(new[] { HellLookDirection.B, HellLookDirection.A, HellLookDirection.C }));
            Assert.That(HellLookOptions.ParseFloors("zz"), Is.Null);
            Assert.That(HellLookOptions.Parse(new[] { "x.exe", "-hellLook", "B" }).ForFloor(2), Is.EqualTo(HellLookDirection.B));
        }
    }
}
