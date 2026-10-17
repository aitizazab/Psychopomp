using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell sound pass (docs/HELL-SOUND-PASS.md, "Installed"): the cue table is complete (every cue the code raises has a
    /// row, every row's file is shipped), every shipped file is credited, and the binder's small rules.</summary>
    public class HellSoundTests
    {
        const string Scripts = "Assets/JamKit/Templates/SkylineRepo/Scripts";
        const string Audio = "Assets/JamKit/Templates/SkylineRepo/Resources/SkylineAudio";

        static readonly string[] EnemyKinds = { "fire-imp", "scythe-demon", "lava-ooze" };
        /// <summary>The names SkylineBot.HellSound is asked for (the brain's cues).</summary>
        static readonly string[] EnemyNames = { "alert", "hurt", "telegraph", "attack", "plate_shed", "wreck" };

        static string RepoPath(string relative)
        {
            foreach (var start in new[] { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory() })
            {
                var dir = new DirectoryInfo(start);
                for (int i = 0; dir != null && i < 10; i++, dir = dir.Parent)
                {
                    string p = Path.Combine(dir.FullName, relative);
                    if (Directory.Exists(p) || File.Exists(p)) return p;
                }
            }
            return null;
        }

        static List<string> BinderCues()
        {
            var list = new List<string>();
            foreach (var f in typeof(HellSoundCues).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.FieldType == typeof(string)) list.Add((string)f.GetRawConstantValue());
            for (int floor = 0; floor < 3; floor++) list.Add(HellSoundCues.Bed(floor));
            return list;
        }

        // ---------------------------------------------------------------- the table

        [Test]
        public void EveryCueTheBinderRaisesHasARow()
        {
            foreach (var cue in BinderCues())
                Assert.That(HellSoundTable.Find(cue) != null || HellSoundTable.Variants.ContainsKey(cue), Is.True, cue);
        }

        [Test]
        public void EveryEnemyCueHasARow()
        {
            foreach (var kind in EnemyKinds)
                foreach (var name in EnemyNames)
                    Assert.That(HellSoundTable.Find("hell/" + kind + "/" + name), Is.Not.Null, kind + "/" + name);
        }

        [Test]
        public void EveryAbilityCueInTheCodeHasARow()
        {
            string root = RepoPath(Scripts);
            if (root == null) Assert.Ignore("the scripts are not reachable from here");
            var found = new List<string>();
            var re = new Regex("HellAbilityAudio\\.Play\\(\"([a-z_]+)\"");
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                foreach (Match m in re.Matches(File.ReadAllText(file)))
                    found.Add("hell/" + m.Groups[1].Value);
            Assert.That(found, Is.Not.Empty);
            foreach (var cue in found) Assert.That(HellSoundTable.Find(cue), Is.Not.Null, cue);
        }

        [Test]
        public void EveryRowHasAUniqueCueAndAnAllowedFileName()
        {
            var seen = new HashSet<string>();
            foreach (var r in HellSoundTable.Rows)
            {
                Assert.That(seen.Add(r.Cue), Is.True, "duplicate " + r.Cue);
                Assert.That(r.Cue.StartsWith("hell/", StringComparison.Ordinal), Is.True, r.Cue);
                Assert.That(r.File == "" || r.File.StartsWith("hell/", StringComparison.Ordinal), Is.True, r.Cue + " -> " + r.File);
            }
        }

        [Test]
        public void EveryFileTheTableCanPlayIsShipped()
        {
            string root = RepoPath(Audio);
            if (root == null) Assert.Ignore("the audio folder is not reachable from here");
            foreach (var f in HellSoundTable.AllFiles())
            {
                string p = Path.Combine(root, f.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(p + ".wav") || File.Exists(p + ".ogg"), Is.True, f);
            }
        }

        [Test]
        public void BusesFollowTheRows()
        {
            Assert.That(HellSoundTable.BusOf("hell/shadowstep_out"), Is.EqualTo(HellSoundBus.Ability));
            Assert.That(HellSoundTable.BusOf("hell/some_new_ability"), Is.EqualTo(HellSoundBus.Ability));
            Assert.That(HellSoundTable.BusOf("hell/fire-imp/alert"), Is.EqualTo(HellSoundBus.Enemy));
            Assert.That(HellSoundTable.BusOf("hell/world/lava_bubble_2"), Is.EqualTo(HellSoundBus.World));
            Assert.That(HellSoundTable.BusOf("hell/bed/1"), Is.EqualTo(HellSoundBus.Bed));
            Assert.That(HellSoundTable.BusOf("hell/death"), Is.EqualTo(HellSoundBus.Ui));
            Assert.That(HellSoundTable.Dilated(HellSoundBus.Ability), Is.False);
            Assert.That(HellSoundTable.Dilated(HellSoundBus.Ui), Is.False);
            Assert.That(HellSoundTable.Dilated(HellSoundBus.Enemy), Is.True);
            Assert.That(HellSoundTable.FileFor("hell/rewind_denied"), Is.EqualTo("hell/abilities/denied"));
            Assert.That(HellSoundTable.FileFor("hell/not/in/table"), Is.EqualTo("hell/not/in/table"));
        }

        [Test]
        public void TheMixByImportanceKeepsItsOrder()
        {
            // the per-cue gains (docs/HELL-SOUND-PASS.md, "The mix by importance"): in range, never pushing a bus past full,
            // and a bed always under a warning
            var busDb = new Dictionary<HellSoundBus, float>
                { { HellSoundBus.Ability, -2f }, { HellSoundBus.Ui, -2f }, { HellSoundBus.World, -6f }, { HellSoundBus.Enemy, -12f }, { HellSoundBus.Bed, -8f } };
            foreach (var r in HellSoundTable.Rows)
            {
                Assert.That(r.GainDb, Is.InRange(-15f, 12f), r.Cue);
                Assert.That(busDb[r.Bus] + r.GainDb, Is.LessThanOrEqualTo(0.01f), r.Cue + ": bus x gain over 1");
            }
            Assert.That(HellSoundTable.GainOf("hell/not/in/table"), Is.EqualTo(1f));
            Assert.That(HellSoundTable.GainOf("hell/bed/1"), Is.LessThan(1f));
            Assert.That(HellSoundTable.GainOf("hell/scythe/warn"), Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void OneBedForAllSwapsEveryFloorToTheFirst()
        {
            try
            {
                HellSoundTable.OneBedForAll = true;
                Assert.That(HellSoundTable.FileFor(HellSoundCues.Bed(2)), Is.EqualTo(HellSoundTable.FileFor(HellSoundCues.Bed(0))));
            }
            finally { HellSoundTable.OneBedForAll = false; }
            Assert.That(HellSoundTable.FileFor(HellSoundCues.Bed(2)), Is.Not.EqualTo(HellSoundTable.FileFor(HellSoundCues.Bed(0))));
        }

        // ---------------------------------------------------------------- the credits

        [Test]
        public void EveryShippedFileIsCredited()
        {
            Assert.That(HellAudioCredits.Uncredited(HellSoundTable.AllFiles()), Is.Empty);
        }

        [Test]
        public void EverySourceIsUsedAndItsCreditLineIsComplete()
        {
            var used = new HashSet<string>();
            foreach (var row in HellAudioCredits.FileSources) for (int i = 1; i < row.Length; i++) used.Add(row[i]);
            foreach (var s in HellAudioCredits.Sources)
            {
                Assert.That(used.Contains(s.Id), Is.True, "unused source " + s.Id);
                Assert.That(s.Licence, Does.StartWith("CC"), s.Id);
                Assert.That(s.Licence, Does.Not.Contain("NC"), s.Id);
                if (s.Id == "synth") continue;
                Assert.That(s.Credit, Does.Contain(s.Author), s.Id);
                Assert.That(s.Credit, Does.Contain(s.Url), s.Id);
                if (s.CreditRequired) Assert.That(s.Credit, Does.Contain("licensed under " + s.Licence), s.Id);
            }
            foreach (var row in HellAudioCredits.FileSources)
                for (int i = 1; i < row.Length; i++) Assert.That(HellAudioCredits.Source(row[i]), Is.Not.Null, row[0] + ": " + row[i]);
        }

        [Test]
        public void TheCreditLinesPutTheRequiredOnesFirst()
        {
            var lines = HellAudioCredits.Lines();
            Assert.That(lines.Count, Is.EqualTo(HellAudioCredits.Sources.Length));
            int firstCc0 = lines.FindIndex(l => l.Contains("CC0"));
            int lastBy = lines.FindLastIndex(l => l.Contains("licensed under CC BY"));
            Assert.That(lastBy, Is.LessThan(firstCc0));
        }

        [Test]
        public void TheCreditsFilesHoldEveryLine()
        {
            string md = RepoPath("CREDITS.md"), txt = RepoPath(Audio + "/../HellAudioCredits.txt");
            if (md == null || txt == null) Assert.Ignore("the credits files are not reachable from here");
            string a = File.ReadAllText(md), b = File.ReadAllText(txt);
            foreach (var s in HellAudioCredits.Sources)
            {
                Assert.That(a, Does.Contain(s.Credit), "CREDITS.md: " + s.Id);
                Assert.That(b, Does.Contain(s.Credit), "HellAudioCredits.txt: " + s.Id);
            }
        }

        // ---------------------------------------------------------------- the rules

        [Test]
        public void TheMuffleClosesWithTheDilation()
        {
            Assert.That(HellSoundRules.MuffleCutoff(0f), Is.EqualTo(HellSoundRules.MuffleOpenHz).Within(1f));
            Assert.That(HellSoundRules.MuffleCutoff(1f), Is.EqualTo(HellSoundRules.MuffleMinHz).Within(1f));
            float last = float.MaxValue;
            for (int i = 0; i <= 10; i++)
            {
                float c = HellSoundRules.MuffleCutoff(i / 10f);
                Assert.That(c, Is.LessThanOrEqualTo(last));
                last = c;
            }
            Assert.That(HellSoundRules.DilationDepth(1f, 0.35f), Is.EqualTo(0f));
            Assert.That(HellSoundRules.DilationDepth(0.35f, 0.35f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(HellSoundRules.DilationDepth(0.675f, 0.35f), Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(HellSoundRules.WorldPitch(1f), Is.EqualTo(1f));
            Assert.That(HellSoundRules.WorldPitch(0.3f), Is.EqualTo(HellSoundRules.PitchFloor));
        }

        [Test]
        public void TheThrobFollowsTheBeatAndTheStepsTheStride()
        {
            Assert.That(HellSoundRules.BeatWrapped(0.95f, 0.02f), Is.True);
            Assert.That(HellSoundRules.BeatWrapped(0.2f, 0.4f), Is.False);
            Assert.That(HellSoundRules.BeatWrapped(-1f, 0.1f), Is.False);   // no ooze phase yet
            Assert.That(HellSoundRules.StepsBetween(0.4f, 0.6f), Is.EqualTo(1));
            Assert.That(HellSoundRules.StepsBetween(0.6f, 0.9f), Is.EqualTo(0));
            Assert.That(HellSoundRules.StepsBetween(0.9f, 1.1f), Is.EqualTo(1));
            Assert.That(HellSoundRules.StepsBetween(1.1f, 1.0f), Is.EqualTo(0));   // a Rewind turned the stride back: no step
        }

        [Test]
        public void AWatchedCountPlaysOnlyWhenItRisesInPlay()
        {
            var w = new HellSoundRules.Watch();
            Assert.That(w.Rose(0), Is.False);   // the first feed bases it
            Assert.That(w.Rose(1), Is.True);
            Assert.That(w.Rose(1), Is.False);
            w.Rebase(0);                        // a Rewind took the sigil back: silent
            Assert.That(w.Rose(0), Is.False);
            Assert.That(w.Rose(1), Is.True);    // lit again for real: it plays again
            w.Rebase(3);                        // a restore that RAISES it (rewinding into a later state) stays silent too
            Assert.That(w.Rose(3), Is.False);
            w.Clear();
            Assert.That(w.Rose(5), Is.False);
        }

        [Test]
        public void AWarningIsPlacedToEndOnTheBeat()
        {
            Assert.That(HellSoundRules.WarnOffset(0.9f, 0.9f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(HellSoundRules.WarnOffset(0.9f, 0.4f), Is.EqualTo(0.5f).Within(1e-6f));   // restored late in the telegraph: start inside the clip
            Assert.That(HellSoundRules.WarnOffset(0.9f, 1.4f), Is.EqualTo(-0.5f).Within(1e-6f));  // a longer telegraph: wait, then play
            Assert.That(HellSoundRules.ChargePitch(0f), Is.LessThan(HellSoundRules.ChargePitch(1f)));
            Assert.That(HellSoundRules.WindLevel(0f), Is.LessThan(HellSoundRules.WindLevel(3f)));
        }
    }
}
