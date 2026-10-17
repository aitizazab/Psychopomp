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
            Assert.That(o.outline, Is.EqualTo(HellOutlineVariant.Kit));
            Assert.That(o.Any, Is.False);
        }

        [Test]
        public void ALookDefaultsToTheLimeOutline()
        {
            var o = HellLookOptions.Parse(new[] { "x", "-hellLook", "b" });
            Assert.That(o.look, Is.EqualTo(HellLookDirection.B));
            Assert.That(o.outline, Is.EqualTo(HellOutlineVariant.Lime));
            Assert.That(o.outlineGiven, Is.False);
        }

        [TestCase("lime", HellOutlineVariant.Lime)]
        [TestCase("soulfire", HellOutlineVariant.Soulfire)]
        [TestCase("whitehot", HellOutlineVariant.WhiteHot)]
        [TestCase("white-hot", HellOutlineVariant.WhiteHot)]
        [TestCase("pulse", HellOutlineVariant.Pulse)]
        [TestCase("kit", HellOutlineVariant.Kit)]
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
            Assert.That(HellLookOptions.PulseHz(0f), Is.EqualTo(1f));
            Assert.That(HellLookOptions.PulseHz(1f), Is.EqualTo(6f));
            Assert.That(HellLookOptions.PulseHz(0.5f), Is.GreaterThan(HellLookOptions.PulseHz(0.2f)));
            Assert.That(HellLookOptions.PulseHz(3f), Is.EqualTo(6f));
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
    }
}
