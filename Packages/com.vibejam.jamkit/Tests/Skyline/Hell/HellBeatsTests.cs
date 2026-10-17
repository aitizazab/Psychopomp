using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>Vesper's beats, the God of Death's prompts, the lines table and the input labels (docs/HELL-BEATS.md).</summary>
    public class HellBeatsTests
    {
        // ------------------------------------------------------------------ the beats
        // ------------------------------------------------------------------ the beats (generic rules, placeholder data)

        static HellBeatEvent Ev(HellBeatTrigger t, int floor = -1, int room = -1, string kind = null, float value = 0f, int count = 0) =>
            HellBeatEvent.Of(t, floor, room, kind, value, count);

        static string Id(HellBeatDef d) => d != null ? d.id : null;

        [Test]
        public void TheLanternPickupPlaysFloorOnesBeatOnce()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.RunStarted();
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 1, "SoulLantern")), Is.Null);
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.LanternTaken, 0, 1, "SoulLantern"))), Is.EqualTo("vesper.f1.lantern"));
            Assert.That(r.Report(Ev(HellBeatTrigger.LanternTaken, 0, 1, "SoulLantern")), Is.Null, "once");
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomTimer, 0, 1, "SoulLantern", 99f)), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.ShaftEntered, 0)), Is.Null);
        }

        [Test]
        public void ALanternLeftOnItsPedestalStillGetsTheBeatAfterAWhile()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 0, "SoulLantern"));
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomTimer, 0, 0, "SoulLantern", 24f)), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomTimer, 0, 0, "Collapse", 30f)), Is.Null, "another room's clock");
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.RoomTimer, 0, 0, "SoulLantern", 25f))), Is.EqualTo("vesper.f1.lantern"));
        }

        [Test]
        public void NoLanternDealtOnFloorOneFallsBackToTheLastRoom()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 0, "SixSigils")), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 1, "Collapse")), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 2, "HoldBrand")), Is.Null);
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 3, "OozeWells"))), Is.EqualTo("vesper.f1.lantern"));
        }

        [Test]
        public void TheLanternAsTheLastRoomWaitsForThePickupNotTheEntry()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 3, "SoulLantern")), Is.Null);
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.LanternTaken, 0, 3, "SoulLantern"))), Is.EqualTo("vesper.f1.lantern"));
        }

        [Test]
        public void ALanternSeenEarlierBlocksTheLastRoomFallbackButNotTheShaft()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 0, "SoulLantern"));   // raced out without it, under 25 s
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 0, 3, "Collapse")), Is.Null);
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.ShaftEntered, 0))), Is.EqualTo("vesper.f1.lantern"));
        }

        [Test]
        public void FloorTwosBeatComesHalfwayThroughTheSigilsOrAtTheShaft()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.Report(Ev(HellBeatTrigger.ShaftEntered, 0));
            Assert.That(r.Report(Ev(HellBeatTrigger.ObjectiveProgress, 1, 0, "SixSigils", 0.34f)), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.ObjectiveProgress, 0, 0, "SixSigils", 1f)), Is.Null, "a floor-1 sigil is not floor 2's");
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.ObjectiveProgress, 1, 0, "SixSigils", 0.5f))), Is.EqualTo("vesper.f2.deal"));
            Assert.That(r.Report(Ev(HellBeatTrigger.ShaftEntered, 1)), Is.Null);

            var s = new HellBeatRules(HellBeatSets.Placeholder());
            Assert.That(Id(s.Report(Ev(HellBeatTrigger.ShaftEntered, 1))), Is.EqualTo("vesper.f2.deal"), "no sigils dealt");
        }

        [Test]
        public void FloorThreesBeatComesBeforeTheBossWhicheverWayTheBossIsReached()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.ShaftEntered, 2))), Is.EqualTo("vesper.f3.mirror"));
            Assert.That(r.Report(Ev(HellBeatTrigger.BeforeBoss)), Is.Null);
            Assert.That(r.Report(Ev(HellBeatTrigger.RunFinished)), Is.Null);

            var chapter = new HellBeatRules(HellBeatSets.Placeholder());   // the chapter start straight to Vesper
            Assert.That(Id(chapter.Report(Ev(HellBeatTrigger.BeforeBoss))), Is.EqualTo("vesper.f3.mirror"));
            var noBoss = new HellBeatRules(HellBeatSets.Placeholder());
            Assert.That(Id(noBoss.Report(Ev(HellBeatTrigger.RunFinished))), Is.EqualTo("vesper.f3.mirror"));
        }

        [Test]
        public void AFullRunPlaysThePlaceholderBeatsInFloorOrder()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.RunStarted();
            var got = new List<string>();
            void Add(HellBeatDef b) { if (b != null) got.Add(b.id); }
            string[][] deck = { new[] { "Collapse", "SoulLantern", "BreakChains", "HoldBrand" }, new[] { "SixSigils", "OozeWells", "Collapse", "HoldBrand" }, new[] { "HoldBrand", "SixSigils", "Collapse", "OozeWells" } };
            for (int f = 0; f < 3; f++)
            {
                for (int i = 0; i < 4; i++)
                {
                    string k = deck[f][i];
                    Add(r.Report(Ev(HellBeatTrigger.RoomEntered, f, i, k)));
                    Add(r.Report(Ev(HellBeatTrigger.RoomTimer, f, i, k, 5f)));
                    if (k == "SoulLantern") Add(r.Report(Ev(HellBeatTrigger.LanternTaken, f, i, k)));
                    for (int s = 1; s <= 6; s++) Add(r.Report(Ev(HellBeatTrigger.ObjectiveProgress, f, i, k, s / 6f)));
                    Add(r.Report(Ev(HellBeatTrigger.ObjectiveDone, f, i, k)));
                    Add(r.Report(Ev(HellBeatTrigger.DarkZoneEntered, f, i, k)));
                }
                Add(r.Report(Ev(HellBeatTrigger.ShaftEntered, f)));
            }
            Add(r.Report(Ev(HellBeatTrigger.BeforeBoss)));
            Add(r.Report(Ev(HellBeatTrigger.RunFinished)));
            Assert.That(got, Is.EqualTo(new[] { "vesper.f1.lantern", "vesper.f2.deal", "vesper.f3.mirror" }));
        }

        [Test]
        public void ANewRunPlaysTheBeatsAgain()
        {
            var r = new HellBeatRules(HellBeatSets.Placeholder());
            r.Report(Ev(HellBeatTrigger.LanternTaken, 0));
            Assert.That(r.Played("vesper.f1.lantern"), Is.True);
            r.RunStarted();
            Assert.That(r.Played("vesper.f1.lantern"), Is.False);
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.LanternTaken, 0))), Is.EqualTo("vesper.f1.lantern"));
        }

        [Test]
        public void EveryTriggerKindAndFilterWorksForANewSet()
        {
            var beats = new List<HellBeatDef>
            {
                new HellBeatDef { id = "dark", lines = new[] { "x" }, when = { HellBeatSets.When(HellBeatTrigger.DarkZoneEntered, 1, "Collapse") } },
                new HellBeatDef { id = "done", lines = new[] { "x" }, when = { HellBeatSets.When(HellBeatTrigger.ObjectiveDone, -1, "BreakChains") } },
                new HellBeatDef { id = "job2", lines = new[] { "x" }, when = { new HellBeatWhen(HellBeatTrigger.JobDone) { count = 2 } } },
                new HellBeatDef { id = "room", lines = new[] { "x" }, when = { HellBeatSets.When(HellBeatTrigger.RoomEntered, 2, "", 1) } },
                new HellBeatDef { id = "late", lines = new[] { "x" }, after = "room", when = { HellBeatSets.When(HellBeatTrigger.ShaftEntered) } },
            };
            var r = new HellBeatRules(beats);
            Assert.That(r.Report(Ev(HellBeatTrigger.DarkZoneEntered, 1, 0, "SixSigils")), Is.Null, "kind filter");
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.DarkZoneEntered, 1, 0, "Collapse"))), Is.EqualTo("dark"));
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.ObjectiveDone, 2, 3, "BreakChains"))), Is.EqualTo("done"));
            Assert.That(r.Report(Ev(HellBeatTrigger.JobDone, count: 1)), Is.Null, "count");
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.JobDone, count: 2))), Is.EqualTo("job2"));
            Assert.That(r.Report(Ev(HellBeatTrigger.ShaftEntered, 0)), Is.Null, "after: waits for its beat");
            Assert.That(r.Report(Ev(HellBeatTrigger.RoomEntered, 2, 0, "Collapse")), Is.Null, "room filter");
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.RoomEntered, 2, 1, "Collapse"))), Is.EqualTo("room"));
            Assert.That(Id(r.Report(Ev(HellBeatTrigger.ShaftEntered, 1))), Is.EqualTo("late"));
        }

        [Test]
        public void TheActiveSetIsChosenByName()
        {
            string was = HellBeatSets.ActiveName;
            try
            {
                HellBeatSets.ActiveName = "no such one";
                Assert.That(HellBeatSets.Active.Count, Is.EqualTo(HellBeatSets.Placeholder().Count), "unknown: the placeholder");
                HellBeatSets.Sets["test-one"] = () => new List<HellBeatDef> { new HellBeatDef { id = "only" } };
                HellBeatSets.ActiveName = "test-one";
                Assert.That(new HellBeatRules().Defs[0].id, Is.EqualTo("only"));
            }
            finally
            {
                HellBeatSets.Sets.Remove("test-one");
                HellBeatSets.ActiveName = was;
            }
        }

        [Test]
        public void HoldsAreCappedAndOnlyThePlaceholdersLastBeatHolds()
        {
            var p = HellBeatSets.Placeholder();
            Assert.That(HellBeatRules.HoldFor(p[0]), Is.EqualTo(0f));
            Assert.That(HellBeatRules.HoldFor(p[1]), Is.EqualTo(0f));
            Assert.That(HellBeatRules.HoldFor(p[2]), Is.GreaterThan(0f).And.LessThanOrEqualTo(HellBeatRules.MaxHoldSeconds));
            Assert.That(HellBeatRules.HoldFor(new HellBeatDef { holdSeconds = 30f }), Is.EqualTo(HellBeatRules.MaxHoldSeconds));
            Assert.That(HellBeatRules.HoldFor(new HellBeatDef { holdSeconds = -2f }), Is.EqualTo(0f));
        }

        [Test]
        public void EverySetsLinesExistAndAreShort()
        {
            foreach (var kv in HellBeatSets.Sets)
                foreach (var d in kv.Value())
                {
                    Assert.That(d.lines.Length, Is.InRange(1, 3), kv.Key + "/" + d.id + ": a line or two, not a speech");
                    Assert.That(d.when.Count, Is.GreaterThan(0), d.id + ": never plays");
                    foreach (var k in d.lines) Assert.That(HellLines.Has(k), kv.Key + "/" + d.id + ": " + k);
                }
        }

        // ------------------------------------------------------------------ the lines

        [Test]
        public void EveryBeatAndPromptHasItsLines()
        {
            foreach (var t in HellTutorials.All)
            {
                Assert.That(HellLines.Has(t.LineKey), t.LineKey);
                Assert.That(HellLines.Get(t.LineKey).speaker, Is.EqualTo(HellSpeakers.Death), t.LineKey);
                Assert.That(HellLines.Has(t.HintKey), t.HintKey);
                Assert.That(HellLines.Get(t.HintKey).speaker, Is.EqualTo(HellSpeakers.Hint), t.HintKey);
                if (t.asksToDo) Assert.That(HellLines.Has(t.DoneKey), t.DoneKey + ": a prompt that asks gets a reaction");
            }
        }

        [Test]
        public void TheKeysAreUniqueAndTheTextIsShort()
        {
            var keys = new HashSet<string>();
            foreach (var l in HellLines.All)
            {
                Assert.That(keys.Add(l.key), Is.True, "duplicate " + l.key);
                Assert.That(l.text.Length, Is.InRange(8, 140), l.key + ": subtitle-sized");
                if (l.speaker == HellSpeakers.Hint) Assert.That(l.text.Length, Is.LessThanOrEqualTo(64), l.key + ": a hint, not a wall of text");
            }
        }

        [Test]
        public void SubtitleTimeFollowsTheTextOrTheClip()
        {
            Assert.That(HellLines.Seconds("Hi."), Is.EqualTo(HellLines.MinSeconds));
            Assert.That(HellLines.Seconds(new string('a', 500)), Is.EqualTo(HellLines.MaxSeconds));
            Assert.That(HellLines.Seconds("Hi.", 5f), Is.EqualTo(5.35f).Within(1e-4f));
        }

        // ------------------------------------------------------------------ the input labels

        [Test]
        public void HintsShowTheBoundControlForTheDeviceInUse()
        {
            var t = new BindingTable();
            Assert.That(HellPromptText.Fill("HOLD {ShadowStep} TO AIM", t, BindDevice.KeyboardMouse), Is.EqualTo("HOLD [E] TO AIM"));
            Assert.That(HellPromptText.Fill("HOLD {ShadowStep} TO AIM", t, BindDevice.Pad), Is.EqualTo("HOLD [RB / R1] TO AIM"));
            Assert.That(HellPromptText.Fill("{Rewind}", t, BindDevice.KeyboardMouse), Is.EqualTo("[V]"));
            Assert.That(HellPromptText.Fill("AIM {Aim}", t, BindDevice.KeyboardMouse), Is.EqualTo("AIM [RIGHT MOUSE]"));
            Assert.That(HellPromptText.Fill("AIM {Aim}", t, BindDevice.Pad), Is.EqualTo("AIM [LT / L2]"));
        }

        [Test]
        public void ARebindShowsInTheHint()
        {
            var t = new BindingTable();
            t.Set(BindDevice.KeyboardMouse, BindAction.Rewind, "<Keyboard>/t");
            Assert.That(HellPromptText.Fill("{Rewind}", t, BindDevice.KeyboardMouse), Is.EqualTo("[T]"));
        }

        [Test]
        public void DeviceWordsAndUnknownTokens()
        {
            var t = new BindingTable();
            Assert.That(HellPromptText.Fill("PICK {kbm:CLICK|pad:A}", t, BindDevice.KeyboardMouse), Is.EqualTo("PICK CLICK"));
            Assert.That(HellPromptText.Fill("PICK {kbm:CLICK|pad:A}", t, BindDevice.Pad), Is.EqualTo("PICK A"));
            Assert.That(HellPromptText.Fill("{Nope} and {", t, BindDevice.Pad), Is.EqualTo("{Nope} and {"));
            foreach (var l in HellLines.All)
                if (l.speaker == HellSpeakers.Hint)
                    foreach (BindDevice d in new[] { BindDevice.KeyboardMouse, BindDevice.Pad })
                        Assert.That(HellPromptText.Fill(l.text, t, d), Does.Not.Contain("{"), l.key + " has an unknown token");
        }

        [Test]
        public void TheLastUsedDeviceIsLabelled()
        {
            Assert.That(HellPromptText.DeviceFor(-1, -1), Is.EqualTo(BindDevice.KeyboardMouse));
            Assert.That(HellPromptText.DeviceFor(10, 5), Is.EqualTo(BindDevice.Pad));
            Assert.That(HellPromptText.DeviceFor(5, 10), Is.EqualTo(BindDevice.KeyboardMouse));
        }

        // ------------------------------------------------------------------ the prompts' queue

        [Test]
        public void APromptShowsOnceAndIsRememberedAcrossRuns()
        {
            var q = new HellTutorialQueue();
            Assert.That(q.Request(HellTutorials.Lava, 0f), Is.True);
            Assert.That(q.Tick(0f, false).id, Is.EqualTo(HellTutorials.Lava));
            Assert.That(q.Request(HellTutorials.Lava, 1f), Is.False, "seen");
            string saved = q.SaveSeen();

            var next = new HellTutorialQueue();
            next.LoadSeen(saved);
            Assert.That(next.Request(HellTutorials.Lava, 0f), Is.False, "remembered");
            next.ReplayAll();
            Assert.That(next.Request(HellTutorials.Lava, 0f), Is.True, "the replay toggle");
        }

        [Test]
        public void OneAtATimeBestPriorityFirstWithAGap()
        {
            var q = new HellTutorialQueue();
            q.Request(HellTutorials.Paths, 0f);
            q.Request(HellTutorials.ShadowStep, 0f);
            Assert.That(q.Tick(0f, false).id, Is.EqualTo(HellTutorials.ShadowStep));
            Assert.That(q.Tick(1f, false), Is.Null, "one at a time");
            Assert.That(q.Complete(HellTutorials.ShadowStep, 2f), Is.True, "done while showing: the reaction line");
            Assert.That(q.Tick(2f + HellTutorialQueue.GapSeconds * 0.5f, false), Is.Null, "the gap");
            Assert.That(q.Tick(2f + HellTutorialQueue.GapSeconds, false).id, Is.EqualTo(HellTutorials.Paths));
        }

        [Test]
        public void BlockedWhileABeatPlaysAndDelaysAreKept()
        {
            var q = new HellTutorialQueue();
            q.Request(HellTutorials.ShadowStep, 0f, 2f);
            Assert.That(q.Tick(1f, false), Is.Null, "not before its delay");
            Assert.That(q.Tick(3f, true), Is.Null, "a beat is speaking");
            Assert.That(q.Tick(4f, false).id, Is.EqualTo(HellTutorials.ShadowStep));
        }

        [Test]
        public void AHintEndsOnItsOwnAfterItsTime()
        {
            var q = new HellTutorialQueue();
            q.Request(HellTutorials.Collapse, 0f);
            var d = q.Tick(0f, false);
            Assert.That(q.Current, Is.SameAs(d));
            q.Tick(d.hintSeconds + 0.01f, false);
            Assert.That(q.Current, Is.Null);
        }

        [Test]
        public void StaleRequestsAreDroppedAndMayComeBack()
        {
            var q = new HellTutorialQueue();
            q.Request(HellTutorials.Paths, 0f);
            q.Request(HellTutorials.Rewind, 0f);
            Assert.That(q.Tick(0f, false).id, Is.EqualTo(HellTutorials.Rewind));
            q.Request(HellTutorials.Collapse, 0.5f);
            q.Tick(10f, false);   // rewind's hint ends; the collapse moment passed long ago
            Assert.That(q.IsPending(HellTutorials.Collapse), Is.False);
            Assert.That(q.Seen(HellTutorials.Collapse), Is.False);
            Assert.That(q.Request(HellTutorials.Collapse, 11f), Is.True);
        }

        [Test]
        public void DoingItFirstMeansItNeverNeedsTeaching()
        {
            var q = new HellTutorialQueue();
            q.Request(HellTutorials.Rewind, 0f, 5f);
            Assert.That(q.Complete(HellTutorials.Rewind, 1f), Is.False, "not showing: no reaction");
            Assert.That(q.IsPending(HellTutorials.Rewind), Is.False);
            Assert.That(q.Seen(HellTutorials.Rewind), Is.True);
            Assert.That(q.Complete(HellTutorials.Dilation, 1f), Is.False);
            Assert.That(q.Request(HellTutorials.Dilation, 2f), Is.False, "they already did it");
            Assert.That(q.Complete(HellTutorials.Lava, 1f), Is.False);
            Assert.That(q.Request(HellTutorials.Lava, 2f), Is.True, "lava isn't a thing to do: still taught");
        }

        [Test]
        public void TurnedOffMeansNothing()
        {
            var q = new HellTutorialQueue { Enabled = false };
            Assert.That(q.Request(HellTutorials.Lava, 0f), Is.False);
            Assert.That(q.Tick(0f, false), Is.Null);
        }

        [Test]
        public void EveryRoomKindHasAnObjectivePrompt()
        {
            foreach (var k in HellRoomLayouts.Kinds)
                Assert.That(HellTutorials.Get(HellTutorials.Objective(k.ToString())), Is.Not.Null, k.ToString());
        }

        [Test]
        public void TheShadowPromptPointsAtANearbyDarkZone()
        {
            var zones = new List<CoverBox>
            {
                new CoverBox { min = new Vector3(1, 0, 1), max = new Vector3(2, 3, 2) },      // 1.5 m: too close to be a step
                new CoverBox { min = new Vector3(30, 0, 0), max = new Vector3(32, 3, 2) },    // 31 m: out of reach
                new CoverBox { min = new Vector3(8, 0, 0), max = new Vector3(10, 3, 2) },     // 9 m: this one
            };
            Assert.That(HellTutorials.PickDarkZone(Vector3.Zero, zones), Is.EqualTo(2));
            Assert.That(HellTutorials.PickDarkZone(Vector3.Zero, zones.GetRange(0, 2)), Is.EqualTo(0), "none in the band: the nearest");
            Assert.That(HellTutorials.PickDarkZone(Vector3.Zero, new List<CoverBox>()), Is.EqualTo(-1));
        }
    }
}
