using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell effects' pure parts (docs/HELL-FX.md): the ghost track, the curves, the emission accumulator, the screen
    /// grade mix, and the screen-shake option.</summary>
    public class HellFxTests
    {
        // ------------------------------------------------------------------ GhostTrack

        [Test]
        public void GhostTrackKeepsTheNewestWhenFull()
        {
            var t = new GhostTrack(4);
            for (int i = 0; i < 6; i++) t.Push(i, new Vector3(i, 0f, 0f));
            Assert.That(t.Count, Is.EqualTo(4));
            Assert.That(t.OldestTime, Is.EqualTo(2.0));
            Assert.That(t.NewestTime, Is.EqualTo(5.0));
            Assert.That(t[0].X, Is.EqualTo(2f));
            Assert.That(t.Newest.X, Is.EqualTo(5f));
        }

        [Test]
        public void GhostTrackInterpolatesAndClampsToItsEnds()
        {
            var t = new GhostTrack(8);
            t.Push(1.0, new Vector3(0f, 0f, 0f));
            t.Push(2.0, new Vector3(10f, 0f, 0f));
            t.Push(3.0, new Vector3(10f, 4f, 0f));
            Assert.That(t.At(1.5).X, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(t.At(2.5).Y, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(t.At(0.0).X, Is.EqualTo(0f), "before the first sample: the oldest");
            Assert.That(t.At(9.0).Y, Is.EqualTo(4f), "after the last: the newest");
        }

        [Test]
        public void GhostTrackFindsTheFirstSampleAtOrAfterATime()
        {
            var t = new GhostTrack(8);
            for (int i = 0; i < 5; i++) t.Push(i * 0.5, Vector3.Zero);
            Assert.That(t.FirstAtOrAfter(-1.0), Is.EqualTo(0));
            Assert.That(t.FirstAtOrAfter(1.0), Is.EqualTo(2));
            Assert.That(t.FirstAtOrAfter(1.1), Is.EqualTo(3));
            Assert.That(t.FirstAtOrAfter(5.0), Is.EqualTo(5));
        }

        [Test]
        public void GhostTrackClearsWhenTimeGoesBackwards()
        {
            var t = new GhostTrack(8);
            t.Push(5.0, Vector3.One);
            t.Push(6.0, Vector3.One);
            t.Push(1.0, Vector3.Zero);
            Assert.That(t.Count, Is.EqualTo(1));
            Assert.That(t.OldestTime, Is.EqualTo(1.0));
        }

        [Test]
        public void GhostTrackMeasuresThePathBetweenTwoTimes()
        {
            var t = new GhostTrack(8);
            t.Push(0.0, new Vector3(0f, 0f, 0f));
            t.Push(1.0, new Vector3(3f, 0f, 0f));
            t.Push(2.0, new Vector3(3f, 4f, 0f));
            Assert.That(t.LengthBetween(0.0, 2.0), Is.EqualTo(7f).Within(1e-4f));
            Assert.That(t.LengthBetween(0.5, 1.5), Is.EqualTo(3.5f).Within(1e-4f));
            Assert.That(t.LengthBetween(2.0, 1.0), Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ curves

        [Test]
        public void ScrubRunsFromThePresentToTheRestoredMomentAndNeverBacktracks()
        {
            Assert.That(HellFxCurves.Scrub(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(HellFxCurves.Scrub(1f), Is.EqualTo(1f).Within(1e-5f));
            float prev = 0f;
            for (int i = 1; i <= 50; i++)
            {
                float v = HellFxCurves.Scrub(i / 50f);
                Assert.That(v, Is.GreaterThanOrEqualTo(prev));
                prev = v;
            }
        }

        [Test]
        public void EnvelopeRisesHoldsAndFalls()
        {
            Assert.That(HellFxCurves.Envelope(-0.1f, 1f, 0.1f, 0.2f), Is.EqualTo(0f));
            Assert.That(HellFxCurves.Envelope(0.05f, 1f, 0.1f, 0.2f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(HellFxCurves.Envelope(0.5f, 1f, 0.1f, 0.2f), Is.EqualTo(1f));
            Assert.That(HellFxCurves.Envelope(0.9f, 1f, 0.1f, 0.2f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(HellFxCurves.Envelope(1.1f, 1f, 0.1f, 0.2f), Is.EqualTo(0f));
        }

        [Test]
        public void DecayStartsAtOneAndEndsAtZero()
        {
            Assert.That(HellFxCurves.Decay(0f, 0.3f), Is.EqualTo(1f));
            Assert.That(HellFxCurves.Decay(0.15f, 0.3f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(HellFxCurves.Decay(0.3f, 0.3f), Is.EqualTo(0f));
            Assert.That(HellFxCurves.Decay(-1f, 0.3f), Is.EqualTo(0f), "not started");
        }

        [Test]
        public void FovKickPunchesOutAndComesBackToOne()
        {
            Assert.That(HellFxCurves.FovKick(-1f, 0.12f, 0.05f, 0.3f), Is.EqualTo(1f));
            Assert.That(HellFxCurves.FovKick(0.05f, 0.12f, 0.05f, 0.3f), Is.EqualTo(1.12f).Within(1e-4f));
            float mid = HellFxCurves.FovKick(0.2f, 0.12f, 0.05f, 0.3f);
            Assert.That(mid, Is.GreaterThan(1f).And.LessThan(1.12f));
            Assert.That(HellFxCurves.FovKick(0.36f, 0.12f, 0.05f, 0.3f), Is.EqualTo(1f));
            Assert.That(HellFxCurves.FovKick(0.05f, -0.08f, 0.05f, 0.3f), Is.EqualTo(0.92f).Within(1e-4f), "a negative kick narrows");
        }

        [Test]
        public void LandingTraumaOnlyForHardLandings()
        {
            Assert.That(HellFxCurves.LandingTrauma(5f, 9f, 22f, 0.4f), Is.EqualTo(0f));
            Assert.That(HellFxCurves.LandingTrauma(15.5f, 9f, 22f, 0.4f), Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(HellFxCurves.LandingTrauma(60f, 9f, 22f, 0.4f), Is.EqualTo(0.4f));
        }

        [Test]
        public void FalloffIsFullNearAndNothingFar()
        {
            Assert.That(HellFxCurves.Falloff(2f, 4f, 20f), Is.EqualTo(1f));
            Assert.That(HellFxCurves.Falloff(12f, 4f, 20f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(HellFxCurves.Falloff(25f, 4f, 20f), Is.EqualTo(0f));
        }

        [Test]
        public void StaggerStartsEachItemLater()
        {
            Assert.That(HellFxCurves.Stagger(0.05f, 0, 0.1f, 0.2f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(HellFxCurves.Stagger(0.05f, 1, 0.1f, 0.2f), Is.EqualTo(0f));
            Assert.That(HellFxCurves.Stagger(0.2f, 1, 0.1f, 0.2f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(HellFxCurves.Stagger(9f, 2, 0.1f, 0.2f), Is.EqualTo(1f));
        }

        [Test]
        public void BackOutOvershootsThenSettles()
        {
            Assert.That(HellFxCurves.BackOut(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(HellFxCurves.BackOut(1f), Is.EqualTo(1f).Within(1e-5f));
            float peak = 0f;
            for (int i = 0; i <= 100; i++) peak = System.Math.Max(peak, HellFxCurves.BackOut(i / 100f));
            Assert.That(peak, Is.GreaterThan(1.05f).And.LessThan(1.15f));
        }

        [Test]
        public void BreatheStaysBetweenItsLowAndOne()
        {
            for (int i = 0; i < 40; i++)
            {
                float v = HellFxCurves.Breathe(i * 0.037f, 2.2f, 0.85f);
                Assert.That(v, Is.InRange(0.85f, 1f));
            }
        }

        // ------------------------------------------------------------------ the emission accumulator

        [Test]
        public void EmitAccumulatorKeepsFractionsAcrossFrames()
        {
            var a = new EmitAccumulator();
            int total = 0;
            for (int i = 0; i < 120; i++) total += a.Take(10f, 1f / 120f, 100);   // 10/s for 1 s
            Assert.That(total, Is.InRange(9, 10));
        }

        [Test]
        public void EmitAccumulatorCapsALongFrame()
        {
            var a = new EmitAccumulator();
            Assert.That(a.Take(100f, 1f, 6), Is.EqualTo(6));
            Assert.That(a.Take(100f, 0f, 6), Is.EqualTo(0), "no time, nothing");
            Assert.That(a.Take(0f, 1f, 6), Is.EqualTo(0), "no rate, nothing");
        }

        // ------------------------------------------------------------------ the screen grade mix

        [Test]
        public void GradeMixOneChannelReachesItsOwnLook()
        {
            var m = new ScreenGradeMix();
            var t = new float[ScreenGradeMix.ParamCount];
            t[ScreenGradeMix.Saturation] = -60f;
            t[ScreenGradeMix.Chroma] = 0.8f;
            m.Begin();
            m.Add(0.3f, t);
            Assert.That(m.Weight, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(m.Get(ScreenGradeMix.Saturation), Is.EqualTo(-60f).Within(1e-4f));
            Assert.That(m.Get(ScreenGradeMix.Chroma), Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void GradeMixBlendsChannelsByStrength()
        {
            var m = new ScreenGradeMix();
            var a = new float[ScreenGradeMix.ParamCount];
            var b = new float[ScreenGradeMix.ParamCount];
            a[ScreenGradeMix.Saturation] = -100f;
            b[ScreenGradeMix.Saturation] = 20f;
            m.Begin();
            m.Add(1f, a);
            m.Add(0.5f, b);
            Assert.That(m.Weight, Is.EqualTo(1f));
            Assert.That(m.Get(ScreenGradeMix.Saturation), Is.EqualTo((-100f + 10f) / 1.5f).Within(1e-3f));
        }

        [Test]
        public void GradeMixWithNothingIsOff()
        {
            var m = new ScreenGradeMix();
            m.Begin();
            m.Add(0f, new float[ScreenGradeMix.ParamCount]);
            Assert.That(m.Weight, Is.EqualTo(0f));
            Assert.That(m.Get(ScreenGradeMix.Lens), Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ the screen-shake option

        [Test]
        public void ScreenShakeOptionRoundTripsAndClamps()
        {
            var o = new SkylineOptions();
            Assert.That(o.screenShake, Is.EqualTo(1f), "on by default");
            o.screenShake = 0.35f;
            var back = SkylineOptions.Deserialize(o.Serialize());
            Assert.That(back.screenShake, Is.EqualTo(0.35f).Within(1e-4f));
            o.screenShake = 4f;
            o.Clamp();
            Assert.That(o.screenShake, Is.EqualTo(1f));
            o.screenShake = float.NaN;
            o.Clamp();
            Assert.That(o.screenShake, Is.EqualTo(1f));
            Assert.That(SkylineOptions.Deserialize("skyline-options 1\nmaster=1\n").screenShake, Is.EqualTo(1f), "old saves keep shake on");
        }
    }
}
