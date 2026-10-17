using System.Collections.Generic;
using System.Linq;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The God of Death's jobs (docs/HELL-JOBS.md): the catalogue, the pockets and their lint, the state machines, the
    /// ledger and the rewards.</summary>
    public class HellJobTests
    {
        static readonly HellJobInput Nothing = new HellJobInput { keeperAlive = true };

        static HellJob Begun(string id)
        {
            var j = new HellJob(HellJobCatalog.Find(id));
            j.Begin();
            return j;
        }

        static HellJobEvent Run(HellJob j, float seconds, HellJobInput input, float dt = 0.02f)
        {
            var ev = HellJobEvent.None;
            for (float t = 0f; t < seconds - 1e-4f; t += dt) ev |= j.Tick(dt, input);
            return ev;
        }

        // ------------------------------------------------------------------ the catalogue

        [Test]
        public void NineJobsThreePerFloorEachInADifferentRoom()
        {
            Assert.That(HellJobCatalog.All.Length, Is.EqualTo(9));
            for (int f = 0; f < 3; f++)
            {
                var rooms = HellJobCatalog.All.Where(j => j.floor == f).Select(j => j.room).ToList();
                Assert.That(rooms.Count, Is.EqualTo(3), "floor " + f);
                Assert.That(rooms.Distinct().Count(), Is.EqualTo(3), "floor " + f);
            }
            Assert.That(HellJobCatalog.All.Select(j => j.id).Distinct().Count(), Is.EqualTo(9));
            foreach (var j in HellJobCatalog.All)
            {
                Assert.That(j.name, Is.Not.Empty);
                Assert.That(j.briefing, Is.Not.Empty);
                Assert.That(j.objective, Is.Not.Empty);
                Assert.That(HellJobCatalog.For(j.room, j.floor), Is.SameAs(j));
            }
        }

        [Test]
        public void TheVerbsVaryAndThereIsOneGiftPerFloor()
        {
            Assert.That(HellJobCatalog.All.Select(j => j.verb).Distinct().Count(), Is.GreaterThanOrEqualTo(7));
            for (int f = 0; f < 3; f++)
                Assert.That(HellJobCatalog.All.Count(j => j.floor == f && j.rewardKind == HellJobRewardKind.Gift), Is.EqualTo(1), "floor " + f);
            Assert.That(HellJobCatalog.All.Where(j => j.rewardKind == HellJobRewardKind.Gift).Select(j => j.giftFlavour).Distinct().Count(), Is.EqualTo(3));
            foreach (var j in HellJobCatalog.All.Where(j => j.rewardKind == HellJobRewardKind.Boon))
                Assert.That(HellJobBoons.All, Does.Contain(j.boon), j.id);
        }

        [Test]
        public void EveryDealtRunOffersAtLeastOneJobPerFloor()
        {
            int total = 0, runs = 300;
            for (ulong seed = 1; seed <= (ulong)runs; seed++)
            {
                var deck = HellDeck.Deal(seed);
                for (int f = 0; f < deck.Length; f++)
                    Assert.That(deck[f].Count(k => HellJobCatalog.For(k, f) != null), Is.GreaterThanOrEqualTo(1), "seed " + seed + " floor " + f);
                total += HellJobCatalog.Offered(deck);
            }
            float mean = total / (float)runs;
            TestContext.WriteLine("jobs offered per run: " + mean);
            Assert.That(mean, Is.InRange(5f, 7f));
        }

        // ------------------------------------------------------------------ the pockets

        [Test]
        public void EveryPocketPassesThePocketLintAndItsRoomStillPassesTheRoomLint()
        {
            var problems = new List<string>();
            foreach (var j in HellJobCatalog.All)
            {
                var room = HellRoomVariants.Build(j.room, j.floor);
                var pocket = HellJobPockets.Variant(j.room, j.floor);
                Assert.That(pocket, Is.Not.Null, j.id);
                problems.AddRange(HellJobPocketLint.Check(room, pocket));
                problems.AddRange(HellRoomLint.Check(room).problems.Select(p => j.id + " room: " + p));
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void ThePocketLintBites()
        {
            var room = HellRoomVariants.Build(HellRoomKind.BreakChains, 0);
            var pocket = HellJobPockets.Variant(HellRoomKind.BreakChains, 0);
            room.solids.RemoveAll(s => pocket.openOnDone.Contains(s.id));
            Assert.That(HellJobPocketLint.Check(room, pocket), Has.Some.Contains("walked into"), "an open cage needs no Shadow Step");

            var sigils = HellRoomVariants.Build(HellRoomKind.SixSigils, 0);
            var vp = HellJobPockets.Variant(HellRoomKind.SixSigils, 0);
            sigils.safePath.Insert(sigils.safePath.Count - 1, vp.spot);
            Assert.That(HellJobPocketLint.Check(sigils, vp), Has.Some.Contains("runs through the pocket"));
        }

        [Test]
        public void RoomsWithoutAJobOnTheirFloorHaveNoPocket()
        {
            foreach (var k in HellRoomLayouts.Kinds)
                for (int f = 0; f < 3; f++)
                {
                    var room = HellRoomVariants.Build(k, f);
                    bool has = room.solids.Any(s => s.id.StartsWith("job-"));
                    Assert.That(has, Is.EqualTo(HellJobCatalog.For(k, f) != null), k + " floor " + f);
                }
        }

        [Test]
        public void ThePocketSitsOutsideTheRoomsOriginalFootprint()
        {
            foreach (var j in HellJobCatalog.All)
            {
                var plain = HellRoomLayouts.Build(j.room);
                var pocket = HellJobPockets.Append(HellRoomLayouts.Build(j.room), j.floor);
                bool outside = pocket.area.min.X >= plain.bounds.max.X - 0.01f || pocket.area.max.X <= plain.bounds.min.X + 0.01f;
                Assert.That(outside, Is.True, j.id);
            }
        }

        [Test]
        public void TheWorldPocketFollowsItsRoom()
        {
            foreach (var j in HellJobCatalog.All)
            {
                var room = HellWorld.Room(j.floor, j.room);
                var pocket = HellJobPockets.World(j.floor, j.room);
                Assert.That(HellRoomLint.Contains(room.bounds, pocket.door), Is.True, j.id);
                Assert.That(HellRoomLint.Contains(room.bounds, pocket.spot), Is.True, j.id);
            }
        }

        // ------------------------------------------------------------------ the state machines

        [Test]
        public void TheViscountEscapesUnlessKilledInTime()
        {
            var j = Begun(HellJobCatalog.Viscount);
            Run(j, 3f, Nothing);
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Active));
            var ev = j.Tick(0.02f, new HellJobInput { seen = true });
            Assert.That(ev & HellJobEvent.Alerted, Is.EqualTo(HellJobEvent.Alerted));
            Run(j, j.def.escapeSeconds + 0.1f, Nothing);
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Failed));
            Assert.That(j.State.failReason, Is.EqualTo("escaped"));

            var k = Begun(HellJobCatalog.Viscount);
            Assert.That(k.Hit(30f, 10f) & HellJobEvent.Alerted, Is.EqualTo(HellJobEvent.Alerted), "a shot alerts him");
            Run(k, 2f, Nothing);
            k.Hit(30f, 10f);
            Assert.That(k.Hit(30f, 10f) & HellJobEvent.Done, Is.EqualTo(HellJobEvent.Done));
            Assert.That(k.Status, Is.EqualTo(HellJobStatus.Done));
        }

        [Test]
        public void ThePoacherOnlyNoticesWhileGlancingAndShotsFromTheDarkHitTriple()
        {
            var j = Begun(HellJobCatalog.Poacher);
            var seen = new HellJobInput { seen = true };
            j.Tick(0.1f, seen);
            Assert.That(j.Glancing, Is.False);
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Active), "back turned");
            Run(j, j.def.glanceEvery - j.def.glanceSeconds, Nothing, 0.1f);
            j.Tick(0.1f, seen);
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Alerted), "glancing round");

            var k = Begun(HellJobCatalog.Poacher);
            k.Hit(22f, 10f);
            Assert.That(k.Health, Is.EqualTo(150f - 66f).Within(1e-3f));
            Assert.That(k.Alerted, Is.True);
            k.Hit(22f, 10f);
            Assert.That(k.Health, Is.EqualTo(150f - 88f).Within(1e-3f), "once he knows, plain damage");
            Run(k, k.def.escapeSeconds + 0.05f, Nothing);
            Assert.That(k.Status, Is.EqualTo(HellJobStatus.Failed));
        }

        [Test]
        public void ReapingAccumulatesAndDecaysOutOfReach()
        {
            var j = Begun(HellJobCatalog.Bather);
            var near = new HellJobInput { inReach = true };
            Run(j, 1f, near);
            Assert.That(j.State.progress, Is.EqualTo(1f).Within(0.03f));
            Run(j, 1f, Nothing);
            Assert.That(j.State.progress, Is.EqualTo(0.7f).Within(0.03f));
            Run(j, 1.9f, near);
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Done));
        }

        [Test]
        public void TheHourglassIsDoneOnlyWhenCarriedOutThroughTheDrop()
        {
            var j = Begun(HellJobCatalog.Hourglass);
            Assert.That(j.Tick(0.02f, new HellJobInput { inReach = true }) & HellJobEvent.PickedUp, Is.EqualTo(HellJobEvent.PickedUp));
            Assert.That(j.Drop(), Is.EqualTo(HellJobEvent.Dropped));
            j.Tick(0.02f, new HellJobInput { inReach = true });
            Assert.That(j.Leave(true), Is.EqualTo(HellJobEvent.Done));

            var k = Begun(HellJobCatalog.Hourglass);
            Assert.That(k.Leave(true), Is.EqualTo(HellJobEvent.Failed), "left it behind");
            var m = Begun(HellJobCatalog.Hourglass);
            m.Tick(0.02f, new HellJobInput { inReach = true });
            Assert.That(m.Leave(false), Is.EqualTo(HellJobEvent.Failed), "carried, but not out through the drop (a shaft or the run's end)");
        }

        [Test]
        public void TheLedgerIsWardedAndTheClerkFilesItUnlessDead()
        {
            var j = Begun(HellJobCatalog.Ledger);
            Assert.That(j.Hit(30f, 20f) & HellJobEvent.Deflected, Is.EqualTo(HellJobEvent.Deflected));
            Assert.That(j.Health, Is.EqualTo(60f));
            Assert.That(j.Alerted, Is.True, "a shot from afar still alerts the clerk");
            Run(j, 5f, new HellJobInput { keeperAlive = true });
            Run(j, 20f, new HellJobInput { keeperAlive = false });
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Alerted), "a dead clerk files nothing");
            j.Hit(30f, 5f);
            Assert.That(j.Hit(30f, 5f) & HellJobEvent.Done, Is.EqualTo(HellJobEvent.Done));

            var k = Begun(HellJobCatalog.Ledger);
            k.Tick(0.02f, new HellJobInput { inPocket = true, keeperAlive = true });
            Assert.That(k.Alerted, Is.True, "walking in alerts him");
            Run(k, k.def.timerSeconds + 0.1f, new HellJobInput { keeperAlive = true });
            Assert.That(k.Status, Is.EqualTo(HellJobStatus.Failed));
            Assert.That(k.State.failReason, Is.EqualTo("filed"));
        }

        [Test]
        public void TheSmithReforgesChainsWhileHeLives()
        {
            var j = Begun(HellJobCatalog.Whelp);
            Assert.That(j.HitChain(0, 50f, 20f), Is.EqualTo(HellJobEvent.Deflected));
            j.HitChain(0, 50f, 5f);
            Assert.That(j.ChainBroken(0), Is.True);
            var ev = Run(j, j.def.reforgeSeconds + 0.05f, new HellJobInput { keeperAlive = true });
            Assert.That(ev & HellJobEvent.Reforged, Is.EqualTo(HellJobEvent.Reforged));
            Assert.That(j.ChainBroken(0), Is.False);
            j.HitChain(0, 50f, 5f);
            Run(j, 30f, new HellJobInput { keeperAlive = false });
            Assert.That(j.ChainBroken(0), Is.True, "a dead smith forges nothing");
            j.HitChain(1, 50f, 5f);
            Assert.That(j.HitChain(2, 50f, 5f) & HellJobEvent.Done, Is.EqualTo(HellJobEvent.Done));
        }

        [Test]
        public void TheSecondsThiefUndoesWoundsThatDontKillInTheWindow()
        {
            var j = Begun(HellJobCatalog.SecondsThief);
            j.Hit(40f, 10f);
            var ev = Run(j, j.def.burstWindow + 0.05f, Nothing);
            Assert.That(ev & HellJobEvent.Undone, Is.EqualTo(HellJobEvent.Undone));
            Assert.That(j.Health, Is.EqualTo(80f));
            j.Hit(40f, 10f);
            Run(j, 0.5f, Nothing);
            Assert.That(j.Hit(40f, 10f) & HellJobEvent.Done, Is.EqualTo(HellJobEvent.Done));
        }

        [Test]
        public void TheSecondsThiefHopsButNotWithAWoundOpen()
        {
            var j = Begun(HellJobCatalog.SecondsThief);
            var ev = Run(j, j.def.hopSeconds + 0.05f, Nothing);
            Assert.That(ev & HellJobEvent.Hopped, Is.EqualTo(HellJobEvent.Hopped));
            Assert.That(j.Perch, Is.EqualTo(1));
            j.Hit(10f, 5f);
            Run(j, j.def.hopSeconds * 0.4f, Nothing);
            Assert.That(j.Perch, Is.EqualTo(1));
        }

        [Test]
        public void HoldingTheSealSpawnsUpToTheCap()
        {
            var j = Begun(HellJobCatalog.Notice);
            int spawns = 0;
            for (float t = 0; t < j.def.seconds + 0.1f; t += 0.02f)
                if ((j.Tick(0.02f, new HellJobInput { inReach = true }) & HellJobEvent.SpawnDue) != 0) spawns++;
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Done));
            Assert.That(spawns, Is.EqualTo(2), "6 s held, one every 3 s");
            Assert.That(spawns, Is.LessThanOrEqualTo(j.def.spawnCap));
        }

        [Test]
        public void LeavingFailsADeathRestartsAndARewindUndoesOnlyFailures()
        {
            var j = Begun(HellJobCatalog.OverdueSoul);
            Run(j, 1f, new HellJobInput { inReach = true });
            j.Restart();
            Assert.That(j.State.progress, Is.EqualTo(0f));
            Assert.That(j.Status, Is.EqualTo(HellJobStatus.Active));
            Assert.That(j.Leave(false), Is.EqualTo(HellJobEvent.Failed));

            var v = Begun(HellJobCatalog.Viscount);
            v.Alert();
            var before = v.Snapshot();
            Run(v, v.def.escapeSeconds + 0.1f, Nothing);
            Assert.That(v.Status, Is.EqualTo(HellJobStatus.Failed));
            v.Restore(before);
            Assert.That(v.Status, Is.EqualTo(HellJobStatus.Alerted), "a Rewind can undo his escape");

            var w = Begun(HellJobCatalog.Viscount);
            var early = w.Snapshot();
            w.Hit(500f, 5f);
            w.Restore(early);
            Assert.That(w.Status, Is.EqualTo(HellJobStatus.Done), "but never a success");
            Assert.That(w.Leave(false), Is.EqualTo(HellJobEvent.None));
        }

        [Test]
        public void ASnapshotIsACopy()
        {
            var j = Begun(HellJobCatalog.Bather);
            var s = j.Snapshot();
            Run(j, 1f, new HellJobInput { inReach = true });
            Assert.That(((HellJobState)s).progress, Is.EqualTo(0f));
            j.Restore(s);
            Assert.That(j.State.progress, Is.EqualTo(0f));
            Assert.That(j.State, Is.Not.SameAs(s));
        }

        // ------------------------------------------------------------------ the ledger and the rewards

        [Test]
        public void TheLedgerCountsOnceAndFavoursMost()
        {
            var l = new HellJobLedger();
            foreach (var id in new[] { "a", "b", "c", "d", "e", "f" }) l.Offer(id);
            Assert.That(HellJobLedger.Needed(6), Is.EqualTo(4));
            Assert.That(HellJobLedger.Needed(3), Is.EqualTo(2));
            Assert.That(HellJobLedger.Needed(1), Is.EqualTo(1));
            l.MarkDone("a"); l.MarkDone("b"); l.MarkDone("c");
            Assert.That(l.MarkDone("a"), Is.False);
            Assert.That(l.MarkFailed("a"), Is.False, "settled once");
            Assert.That(l.Favoured, Is.False);
            l.MarkDone("d");
            Assert.That(l.Favoured, Is.True);
            Assert.That(l.DoneCount, Is.EqualTo(4));
            Assert.That(new HellJobLedger().Favoured, Is.False);
        }

        static readonly HellSkillDef[] Pool =
        {
            new HellSkillDef { id = "s1", flavour = HellFlavour.Shadow }, new HellSkillDef { id = "s2", flavour = HellFlavour.Shadow },
            new HellSkillDef { id = "t1", flavour = HellFlavour.Time }, new HellSkillDef { id = "f1", flavour = HellFlavour.Fire },
        };

        [Test]
        public void AGiftIsAnUnownedSkillOfItsFlavourElseThatFlavoursBoon()
        {
            var viscount = HellJobCatalog.Find(HellJobCatalog.Viscount);
            var r = HellJobRewards.Resolve(viscount, Pool, new List<string> { "s1" }, n => 0);
            Assert.That(r.skill, Is.EqualTo("s2"));
            Assert.That(r.boon, Is.Null);
            var none = HellJobRewards.Resolve(viscount, Pool, new List<string> { "s1", "s2" }, n => 0);
            Assert.That(none.skill, Is.Null);
            Assert.That(none.boon, Is.EqualTo(HellJobBoons.QuickStep));
            var boon = HellJobRewards.Resolve(HellJobCatalog.Find(HellJobCatalog.Notice), Pool, new List<string>(), n => 0);
            Assert.That(boon.boon, Is.EqualTo(HellJobBoons.Mend));
        }

        [Test]
        public void BoonsHaveFloors()
        {
            Assert.That(HellJobBoons.StepRecharge(6f), Is.EqualTo(5f));
            Assert.That(HellJobBoons.StepRecharge(3.5f), Is.EqualTo(3f));
            Assert.That(HellJobBoons.RewindCooldown(20f), Is.EqualTo(17f));
            Assert.That(HellJobBoons.RewindCooldown(12f), Is.EqualTo(11f));
        }
    }
}
