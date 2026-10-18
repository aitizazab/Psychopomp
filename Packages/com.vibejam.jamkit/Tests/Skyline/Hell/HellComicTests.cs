using System;
using System.Collections.Generic;
using System.Linq;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The comic flip-through (docs/HELL-COMIC.md): the sequences follow the script, the pages lay out, and every
    /// caption, balloon and splash title fits its box at 16:9 and 16:10 from 1080p to 4K (the ending's name clipped before).</summary>
    public class HellComicTests
    {
        static readonly string[] EndingIds =
        {
            HellEndings.Kill, HellEndings.SpareKept, HellEndings.SpareCost, HellEndings.Damned, HellEndings.KillLucifer, HellEndings.SpareLucifer,
        };

        /// <summary>Screens (pixels) to check: 16:9 and 16:10, 1080p to 4K.</summary>
        static readonly (int w, int h)[] Screens = { (1920, 1080), (1920, 1200), (2560, 1440), (2560, 1600), (3840, 2160), (3840, 2400), (1280, 720), (1280, 800) };

        /// <summary>The page's size in panel units on a screen (the HUD's panel: 1920x1080 reference, match 0.5), less the
        /// comic's margins (2.2% each side, 3.6% top, 7.5% bottom).</summary>
        static (float w, float h) Stage((int w, int h) screen)
        {
            double scale = Math.Pow(2, 0.5 * Math.Log(screen.w / 1920.0, 2) + 0.5 * Math.Log(screen.h / 1080.0, 2));
            float pw = (float)(screen.w / scale), ph = (float)(screen.h / scale);
            return (pw * (1f - 0.044f), ph * (1f - 0.111f));
        }

        static IEnumerable<HellComicSequence> All()
        {
            yield return HellComics.Opening();
            foreach (var id in EndingIds) yield return HellComics.Ending(id);
        }

        [Test]
        public void OpeningFollowsThePlaceholderCardsWithTheirVoices()
        {
            var s = HellComics.Opening();
            Assert.That(s.id, Is.EqualTo(HellComics.OpeningId));
            var texts = s.pages.SelectMany(p => p.panels).SelectMany(p => p.lines).Select(l => l.text).ToList();
            Assert.That(texts, Is.EqualTo(HellFlowText.Opening.Select(c => c.text).ToList()), "every opening card, in order");
            var voices = HellComics.Voices(s).Select(v => v.speaker + "/" + v.key).ToList();
            Assert.That(voices, Is.EqualTo(new[] { "narrator/open-01", "narrator/open-02", "god/open-03", "god/open-04", "god/open-05", "god/open-06" }));
            Assert.That(s.Images().Distinct().Count(), Is.EqualTo(6), "the six premise panels");
            Assert.That(s.pages.Select(p => p.music), Is.EqualTo(new[] { "sister", "hall", "wait", "idea" }));
            Assert.That(HellComics.OpeningMusic(1), Is.EqualTo("hall"));
            Assert.That(HellComics.OpeningMusic(9), Is.Null);
        }

        [TestCase("kill")]
        [TestCase("spare-kept")]
        [TestCase("spare-cost")]
        [TestCase("damned")]
        [TestCase("kill-lucifer")]
        [TestCase("spare-lucifer")]
        public void EndingPlaysEveryLineOnceThenItsNameAsASplash(string id)
        {
            var s = HellComics.Ending(id);
            Assert.That(s.id, Is.EqualTo("ending-" + id));
            var keys = HellHourEndings.Sequence(id);
            var texts = s.pages.SelectMany(p => p.panels).SelectMany(p => p.lines).Select(l => l.text).ToList();
            foreach (var k in keys) Assert.That(texts.Count(t => t == HellEndingLines.Get(k).text), Is.EqualTo(1), k);
            Assert.That(texts.Count, Is.EqualTo(keys.Length));
            var last = s.pages.Last();
            Assert.That(last.panels.Count, Is.EqualTo(1));
            Assert.That(last.panels[0].Splash, Is.True);
            Assert.That(last.panels[0].title, Is.EqualTo(HellEndings.Title(id)));
            Assert.That(last.panels[0].fallbackImage, Does.StartWith("endings/"));
            Assert.That(s.pages.Take(s.pages.Count - 1).All(p => p.panels.Count == 2), "staggered two-panel pages before the splash");
            foreach (var l in s.pages.SelectMany(p => p.panels).SelectMany(p => p.lines))
                if (l.kind == HellComicLineKind.Balloon) Assert.That(l.Voiced, Is.True, "Baja's balloons are voiced: " + l.voice);
        }

        [Test]
        public void EndingImagesFollowTheImportNames()
        {
            var names = HellComics.Ending(HellEndings.KillLucifer).Images().ToList();
            Assert.That(names, Does.Contain("endings/kill-1-1"));
            Assert.That(names, Does.Contain("endings/kill-1-4"));
            Assert.That(names, Does.Contain("endings/lucifer-1-4"));
            Assert.That(names, Does.Contain("endings/lucifer-title"));
            foreach (var art in HellComics.EndingArt)
                for (int p = 0; p <= 4; p++) Assert.That(HellComics.Describe(art, p), Is.Not.EqualTo("An ending panel."), art + " " + p);
        }

        [Test]
        public void PanelsStayOnThePageAndOnlyTheStaggerOverlaps()
        {
            for (int n = 1; n <= 4; n++)
            {
                var r = HellComicLayout.Rects(n);
                Assert.That(r.Length, Is.EqualTo(n));
                foreach (var a in r) Assert.That(a.x >= 0f && a.y >= 0f && a.Right <= 1.0001f && a.Bottom <= 1.0001f, "inside the page: " + n);
                for (int i = 0; i < r.Length; i++)
                    for (int j = i + 1; j < r.Length; j++)
                        Assert.That(r[i].Overlaps(r[j]), Is.EqualTo(n == 2), n + " panels: " + i + "/" + j);
            }
            Assert.That(HellComicLayout.Rects(7).Length, Is.EqualTo(HellComicLayout.MaxPanels));
            // the covered corner never takes a line
            var b = HellComicLine.Balloon("god", "BAJA", "x", null);
            var c = HellComicLine.Caption("x");
            Assert.That(HellComicLayout.Spot(b, 0, true, HellComicLayout.CornerCovered(0, 2)), Is.Not.EqualTo(HellComicSpot.BottomRight));
            Assert.That(HellComicLayout.Spot(b, 0, true, false), Is.EqualTo(HellComicSpot.BottomRight));
            Assert.That(HellComicLayout.Spot(c, 0, true), Is.EqualTo(HellComicSpot.TopLeft));
        }

        [Test]
        public void PicturesCoverTheirPanelsAndKeepTheFocusInFrame()
        {
            foreach (var (pw, ph) in new[] { (1000f, 560f), (600f, 900f), (1800f, 400f) })
            {
                var c = HellComicLayout.Cover(pw, ph, 16f / 9f, 0.5f, 0.5f, 1.1f);
                Assert.That(c.w, Is.GreaterThanOrEqualTo(pw * 1.099f));
                Assert.That(c.h, Is.GreaterThanOrEqualTo(ph * 1.099f));
                Assert.That(c.left, Is.LessThanOrEqualTo(0f));
                Assert.That(c.top, Is.LessThanOrEqualTo(0f));
                Assert.That(c.left + c.w, Is.GreaterThanOrEqualTo(pw));
                Assert.That(c.top + c.h, Is.GreaterThanOrEqualTo(ph));
            }
        }

        [Test]
        public void EverySplashTitleFitsTheScreen()
        {
            var titles = EndingIds.Select(HellEndings.Title).Concat(new[] { "SPARED, AT A PRICE" }).ToList();
            foreach (var screen in Screens)
            {
                var (w, h) = Stage(screen);
                foreach (var t in titles)
                {
                    // the player's numbers: 84% of the panel's width, 60% of its height, up to 120 (or 16% of the height)
                    float size = HellComicFit.Size(t, w * 0.84f, h * 0.6f, Math.Min(120f, h * 0.16f), 16f, 0.06f);
                    int lines = HellComicFit.Lines(t, w * 0.84f, size, size * 0.06f);
                    Assert.That(lines, Is.GreaterThan(0), t + " at " + screen);
                    Assert.That(lines * size * HellComicFit.LineHeight, Is.LessThanOrEqualTo(h * 0.6f), t + " at " + screen);
                    Assert.That(size, Is.GreaterThanOrEqualTo(40f), "still big: " + t + " at " + screen);
                }
            }
        }

        [Test]
        public void EveryCaptionAndBalloonFitsItsPanel()
        {
            foreach (var screen in Screens)
            {
                var (sw, sh) = Stage(screen);
                foreach (var s in All())
                    foreach (var page in s.pages)
                    {
                        var rects = HellComicLayout.Rects(page.panels.Count);
                        for (int i = 0; i < page.panels.Count; i++)
                        {
                            float w = rects[i].w * sw, h = rects[i].h * sh;
                            var p = page.panels[i];
                            int balloons = p.lines.Count(l => l.kind == HellComicLineKind.Balloon);
                            foreach (var l in p.lines)
                            {
                                // the player's numbers (HellComicPlayer.FitLine)
                                bool caption = l.kind == HellComicLineKind.Caption;
                                float maxW = w * (caption ? 0.62f : 0.48f);
                                float maxH = h * (caption ? 0.3f : balloons > 1 ? 0.38f : 0.44f);
                                float textW = maxW - (caption ? 36f : 52f), textH = maxH - (caption ? 24f : 34f) - (caption ? 0f : 30f);
                                float size = HellComicFit.Size(l.text, textW, textH, Math.Max(18f, Math.Min(40f, h * 0.062f)), 11f);
                                int lines = HellComicFit.Lines(l.text, textW, size);
                                string where = s.id + " page " + s.pages.IndexOf(page) + " panel " + i + " at " + screen + ": " + l.text;
                                Assert.That(lines, Is.GreaterThan(0), where);
                                Assert.That(lines * size * HellComicFit.LineHeight, Is.LessThanOrEqualTo(textH), where);
                                Assert.That(size, Is.GreaterThanOrEqualTo(14f), "readable: " + where);
                            }
                        }
                    }
            }
        }

        [Test]
        public void FitShrinksLongWordsInsteadOfOverflowing()
        {
            Assert.That(HellComicFit.Lines("ANTIDISESTABLISHMENTARIANISM", 200f, 40f), Is.EqualTo(-1));
            float s = HellComicFit.Size("ANTIDISESTABLISHMENTARIANISM", 400f, 200f, 60f, 8f);
            Assert.That(HellComicFit.Lines("ANTIDISESTABLISHMENTARIANISM", 400f, s), Is.EqualTo(1));
            Assert.That(HellComicFit.Lines("a b\nc", 1000f, 20f), Is.EqualTo(2));
        }

        [Test]
        public void LinesLastTheirVoiceOrTheirReadingTime()
        {
            var l = HellComicLine.Balloon("god", "BAJA", "Short.", "x");
            Assert.That(HellComics.LineSeconds(l, 3f), Is.EqualTo(3.6f).Within(1e-4));
            Assert.That(HellComics.LineSeconds(l, 0f), Is.EqualTo(2.2f).Within(1e-4));
            Assert.That(HellComicLine.Caption("x").Voiced, Is.False);
            Assert.That(HellComicLine.Caption("x", "narrator", "open-01").Voiced, Is.True);
            Assert.That(HellComicLine.Balloon("you", "YOU", "x", "k").Voiced, Is.False);
        }
    }
}
