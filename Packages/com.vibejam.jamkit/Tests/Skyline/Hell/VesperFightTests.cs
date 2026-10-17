using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>Vesper's fight (docs/HELL-BOSS.md): the arena passes the room lint, the phase machine, her Rewind and how to
    /// break it, the damage rules, her mercy, and the snapshot the global Rewind uses.</summary>
    public class VesperFightTests
    {
        const float Dt = 0.02f;
        readonly List<VesperEvent> _ev = new List<VesperEvent>();

        [SetUp]
        public void Clear() => _ev.Clear();

        static VesperFight NewFight(VesperTuning t = null)
        {
            var f = new VesperFight(t);
            var d = HellVesperArena.Build();
            f.SetArena(HellVesperArena.Perches(d), d.bounds);
            return f;
        }

        static VesperFight.Input In(float health = 200f, int imps = 0, Vector3? player = null) => new VesperFight.Input
        {
            dt = Dt, player = player ?? new Vector3(0, 0, 8), playerForward = new Vector3(0, 0, 1), playerHealth = health, impsAlive = imps,
        };

        void Run(VesperFight f, float seconds, VesperFight.Input input)
        {
            for (float t = 0f; t < seconds; t += Dt) f.Tick(input, _ev);
        }

        /// <summary>Ticks until an event of <paramref name="k"/> shows (at most <paramref name="max"/> s).</summary>
        bool RunUntil(VesperFight f, VesperEventKind k, float max, VesperFight.Input input)
        {
            for (float t = 0f; t < max; t += Dt)
            {
                if (Saw(k)) return true;
                f.Tick(input, _ev);
            }
            return Saw(k);
        }

        bool Saw(VesperEventKind k) => _ev.Any(e => e.kind == k);

        /// <summary>Damage straight to the phase's floor: the shift (or the defeat) starts.</summary>
        void DropToFloor(VesperFight f)
        {
            for (int i = 0; i < 400 && f.Vulnerable && f.S.health > f.PhaseFloor; i++) f.Hit(50f, false, 2f, _ev);
        }

        // ------------------------------------------------------------------ the arena

        [Test]
        public void TheArenaPassesTheRoomLint()
        {
            var r = HellVesperArena.Build();
            var res = HellRoomLint.Check(r);
            TestContext.WriteLine("Vesper's arena: safe " + res.safeLength + " m, fast " + res.fastLength + " m, straight " + res.straightLength + " m");
            Assert.That(res.problems, Is.Empty, string.Join("\n", res.problems));
            var world = HellVesperArena.World();
            Assert.That(HellRoomLint.Check(world).problems, Is.Empty, "in its world slot too");
        }

        [Test]
        public void TheArenaHasLavaDarknessAndHighGround()
        {
            var r = HellVesperArena.Build();
            Assert.That(r.hazards.Any(h => h.kind == HellHazardKind.Lava), Is.True);
            var perches = HellVesperArena.Perches(r);
            Assert.That(perches.Count, Is.GreaterThanOrEqualTo(5), "her perches: dais, pillars, gallery, throne");
            Assert.That(perches.Max(p => p.Y), Is.GreaterThanOrEqualTo(4f), "a perch on the high ground");
            Assert.That(r.stops.All(s => s.position.Y >= 4f), Is.True, "the stops are the high ground");
            Assert.That(r.bounds.max.Y - 0f, Is.GreaterThanOrEqualTo(12f), "tall walls (verticality)");
        }

        [Test]
        public void TheArenaSlotIsClearOfTheRoomsAndTheShafts()
        {
            var a = HellVesperArena.World();
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
            {
                foreach (var kind in HellRoomLayouts.Kinds)
                {
                    var b = HellWorld.Room(f, kind).bounds;
                    bool overlap = a.bounds.min.X < b.max.X && b.min.X < a.bounds.max.X && a.bounds.min.Z < b.max.Z && b.min.Z < a.bounds.max.Z;
                    Assert.That(overlap, Is.False, "overlaps floor " + f + " " + kind);
                }
                var s = HellWorld.ShaftCentre(f);
                Assert.That(HellRoomLint.Contains(a.bounds, new Vector3(s.X, 0f, s.Z)), Is.False, "shaft " + f);
            }
        }

        // ------------------------------------------------------------------ the phase machine

        [Test]
        public void WaitingUntilBegunAndCannotBeHit()
        {
            var f = NewFight();
            Run(f, 2f, In());
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Waiting));
            Assert.That(f.Hit(100f, true, 2f, _ev), Is.EqualTo(0f));
            Assert.That(_ev, Is.Empty);
        }

        [Test]
        public void PhaseOneVolleysThenBlinksToAnotherPerchWithATell()
        {
            var f = NewFight();
            f.Begin(_ev);
            Assert.That(f.S.phase, Is.EqualTo(1));
            Assert.That(_ev.Any(e => e.kind == VesperEventKind.Line && e.line == VesperLines.Intro), Is.True);
            Run(f, 12f, In());
            Assert.That(Saw(VesperEventKind.VolleyTell), Is.True);
            int tells = _ev.Count(e => e.kind == VesperEventKind.VolleyTell);
            int bolts = _ev.Count(e => e.kind == VesperEventKind.Bolt);
            Assert.That(bolts, Is.GreaterThanOrEqualTo(tells * 3 - 3), "three bolts per volley");
            int tell = _ev.FindIndex(e => e.kind == VesperEventKind.BlinkTell);
            int blink = _ev.FindIndex(e => e.kind == VesperEventKind.Blinked);
            Assert.That(tell, Is.GreaterThanOrEqualTo(0));
            Assert.That(blink, Is.GreaterThan(tell), "the blink comes after its tell");
            Assert.That(_ev[blink].position, Is.EqualTo(_ev[tell].position), "she lands where the tell marked");
            Assert.That(_ev[blink].index, Is.Not.EqualTo(0), "a different perch");
        }

        [Test]
        public void EveryBoltComesAfterAVolleyTell()
        {
            var f = NewFight();
            f.Begin(_ev);
            Run(f, 30f, In());
            bool told = false;
            foreach (var e in _ev)
            {
                if (e.kind == VesperEventKind.VolleyTell) told = true;
                if (e.kind == VesperEventKind.Bolt) Assert.That(told, Is.True);
            }
        }

        [Test]
        public void TakingHeavyFireAtAPerchMakesHerLeaveEarly()
        {
            var f = NewFight();
            f.Begin(_ev);
            Run(f, 0.5f, In());
            f.Hit(200f, false, 2f, _ev);
            _ev.Clear();
            Run(f, 1.5f, In());
            Assert.That(Saw(VesperEventKind.BlinkTell), Is.True);
            Assert.That(Saw(VesperEventKind.VolleyTell), Is.False);
        }

        [Test]
        public void HealthStopsAtThePhaseFloorAndTheShiftLeadsToPhaseTwo()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, true, 2f, _ev);
            Assert.That(f.S.health, Is.EqualTo(f.T.phase2At), "one huge hit stops at the floor");
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Shift));
            Assert.That(f.Vulnerable, Is.False);
            Assert.That(f.Hit(100f, false, 2f, _ev), Is.EqualTo(0f), "no damage during the shift");
            Run(f, f.T.shiftSeconds + 0.1f, In());
            Assert.That(f.S.phase, Is.EqualTo(2));
            Assert.That(_ev.Any(e => e.kind == VesperEventKind.PhaseBegan && e.index == 2), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.SummonTell), "phase 2 opens with a summon");
            Run(f, f.T.summonTellSeconds + 0.05f, In());
            var s = _ev.Last(e => e.kind == VesperEventKind.Summoned);
            Assert.That(s.index, Is.EqualTo(f.T.summonCount));
        }

        [Test]
        public void SummonsRespectTheImpCap()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In(imps: 2));
            Run(f, f.T.summonTellSeconds + 0.05f, In(imps: 2));
            Assert.That(_ev.Last(e => e.kind == VesperEventKind.Summoned).index, Is.EqualTo(f.T.impCap - 2));
        }

        [Test]
        public void PhaseThreeIsNightfallAndSheHuntsFromTheDark()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In());
            f.Hit(5000f, false, 2f, _ev);
            Assert.That(f.S.health, Is.EqualTo(f.T.phase3At));
            Run(f, f.T.shiftSeconds + 0.1f, In());
            Assert.That(f.S.phase, Is.EqualTo(3));
            Assert.That(Saw(VesperEventKind.NightfallBegan), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Hidden));
            Assert.That(f.Vulnerable, Is.False, "hidden in the dark");
            _ev.Clear();
            var player = new Vector3(0, 0, 8);
            Run(f, f.T.hiddenSeconds + 0.05f, In(player: player));
            var tell = _ev.Single(e => e.kind == VesperEventKind.HuntTell);
            float dist = Vector3.Distance(new Vector3(tell.position.X, 0, tell.position.Z), new Vector3(player.X, 0, player.Z));
            Assert.That(dist, Is.LessThanOrEqualTo(f.T.huntDistance + 0.01f), "the flare marks a spot near you");
            Assert.That(tell.position.Z, Is.LessThan(player.Z), "behind you (you face +Z)");
            Assert.That(f.Vulnerable, Is.False, "the flare is only a warning");
            Run(f, f.T.huntTellSeconds + 0.05f, In(player: player));
            Assert.That(Saw(VesperEventKind.Appeared), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.StrikeWindup));
            Assert.That(f.Vulnerable, Is.True, "out of the dark she can be shot");
        }

        [Test]
        public void HerCutHitsWhenYouStayAndMissesWhenYouMove()
        {
            foreach (bool stay in new[] { true, false })
            {
                _ev.Clear();
                var f = NewFight();
                f.Begin(_ev);
                f.Hit(5000f, false, 2f, _ev);
                Run(f, f.T.shiftSeconds + 0.1f, In());
                f.Hit(5000f, false, 2f, _ev);
                Run(f, f.T.shiftSeconds + 0.1f, In());
                var player = new Vector3(0, 0, 8);
                Run(f, f.T.hiddenSeconds + f.T.huntTellSeconds + 0.1f, In(player: player));
                var moved = stay ? player : player + new Vector3(3f, 0, 0);   // a sidestep out of the locked line
                Run(f, f.T.strikeWindupSeconds + 0.05f, In(player: moved));
                Assert.That(Saw(VesperEventKind.StrikeHit), Is.EqualTo(stay), stay ? "standing still is cut" : "stepping away dodges");
                Assert.That(f.S.action, Is.EqualTo(VesperAction.Exposed), "after the cut she is open");
            }
        }

        [Test]
        public void TheCutIsALockedLine()
        {
            var from = new Vector3(0, 0, 0);
            var aim = new Vector3(0, 0, 1);
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(0, 0, 6), 8f, 1.4f), Is.True, "in line");
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(1.2f, 0, 3), 8f, 1.4f), Is.True, "at the edge");
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(2f, 0, 3), 8f, 1.4f), Is.False, "beside it");
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(0, 0, 9), 8f, 1.4f), Is.False, "past its reach");
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(0, 0, -2), 8f, 1.4f), Is.False, "behind her");
            Assert.That(VesperStrike.Hits(from, aim, new Vector3(0, 3f, 4), 8f, 1.4f), Is.False, "above her (on a ledge)");
        }

        [Test]
        public void TheLastDamageMakesHerKneelThenTheFightEndsOnce()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In());
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In());
            Run(f, f.T.hiddenSeconds + f.T.huntTellSeconds + 0.1f, In());
            Assert.That(f.Vulnerable, Is.True);
            f.Hit(5000f, false, 2f, _ev);
            Assert.That(f.S.health, Is.EqualTo(0f));
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Kneel));
            Assert.That(f.Defeated, Is.True);
            Assert.That(f.Hit(100f, true, 2f, _ev), Is.EqualTo(0f));
            Run(f, f.T.kneelSeconds + 1f, In());
            Assert.That(_ev.Count(e => e.kind == VesperEventKind.Defeated), Is.EqualTo(1));
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Done));
            Run(f, 2f, In());
            Assert.That(_ev.Count(e => e.kind == VesperEventKind.Defeated), Is.EqualTo(1), "raised once");
        }

        [Test]
        public void ADeathRestartsTheFightFromPhaseOneOrKeepsThePhase()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In());
            f.Hit(100f, false, 2f, _ev);
            f.Restart(false, _ev);
            Assert.That(f.S.phase, Is.EqualTo(1));
            Assert.That(f.S.health, Is.EqualTo(f.T.maxHealth));
            Assert.That(Saw(VesperEventKind.Restarted), Is.True);

            var g = NewFight();
            g.Begin(_ev);
            g.Hit(5000f, false, 2f, _ev);
            Run(g, g.T.shiftSeconds + 0.1f, In());
            g.Hit(100f, false, 2f, _ev);
            g.Restart(true, _ev);
            Assert.That(g.S.phase, Is.EqualTo(2));
            Assert.That(g.S.health, Is.EqualTo(g.T.phase2At));
        }

        // ------------------------------------------------------------------ her Rewind

        VesperFight AtPhaseTwo()
        {
            var f = NewFight();
            f.Begin(_ev);
            f.Hit(5000f, false, 2f, _ev);
            Run(f, f.T.shiftSeconds + 0.1f, In());
            Run(f, f.T.rewindCooldown + 1f, In(imps: 2));   // past the summon and her first cooldown
            _ev.Clear();
            return f;
        }

        [Test]
        public void HerRewindUndoesTheDamageOfTheLastSecondsAfterATell()
        {
            var f = AtPhaseTwo();
            float before = f.S.health;
            f.Hit(150f, false, 2f, _ev);
            Assert.That(RunUntil(f, VesperEventKind.RewindTell, 4f, In(imps: 2)), Is.True);
            Assert.That(Saw(VesperEventKind.RewindTell), Is.True, "a tell first");
            Assert.That(f.S.action, Is.EqualTo(VesperAction.RewindTell));
            Assert.That(f.Vulnerable, Is.True, "she can be shot while it winds up");
            Run(f, f.T.rewindTellSeconds + 0.05f, In(imps: 2));
            var r = _ev.Single(e => e.kind == VesperEventKind.Rewound);
            Assert.That(r.amount, Is.EqualTo(150f).Within(0.01f));
            Assert.That(f.S.health, Is.EqualTo(before).Within(0.01f), "the damage is undone");
        }

        [Test]
        public void ShootingHerHeartDuringTheTellBreaksTheRewindAndStaggersHer()
        {
            var f = AtPhaseTwo();
            f.Hit(150f, false, 2f, _ev);
            Assert.That(RunUntil(f, VesperEventKind.RewindTell, 4f, In(imps: 2)), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.RewindTell));
            float h = f.S.health;
            f.Hit(22f, true, 2f, _ev);   // 44 on the heart
            Assert.That(f.S.action, Is.EqualTo(VesperAction.RewindTell), "one heart shot is not enough");
            f.Hit(22f, true, 2f, _ev);   // 88 >= 80
            Assert.That(Saw(VesperEventKind.RewindBroken), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Stagger));
            Run(f, 0.5f, In(imps: 2));
            Assert.That(Saw(VesperEventKind.Rewound), Is.False, "nothing undone");
            Assert.That(f.S.health, Is.EqualTo(h - 88f).Within(0.01f));
            float staggered = f.Hit(20f, false, 2f, _ev);
            Assert.That(staggered, Is.EqualTo(20f * f.T.staggerMultiplier).Within(0.01f), "staggered, she takes more");
        }

        [Test]
        public void BodyShotsDoNotBreakHerRewind()
        {
            var f = AtPhaseTwo();
            f.Hit(150f, false, 2f, _ev);
            Assert.That(RunUntil(f, VesperEventKind.RewindTell, 4f, In(imps: 2)), Is.True);
            for (int i = 0; i < 5; i++) f.Hit(22f, false, 2f, _ev);
            Run(f, f.T.rewindTellSeconds + 0.05f, In(imps: 2));
            Assert.That(Saw(VesperEventKind.RewindBroken), Is.False);
            Assert.That(Saw(VesperEventKind.Rewound), Is.True, "and the body shots during the tell are undone too");
        }

        [Test]
        public void HerRewindNeverGivesBackMoreThanThePhaseStartedWith()
        {
            var f = AtPhaseTwo();
            Assert.That(f.S.health, Is.EqualTo(f.T.phase2At));
            f.Hit(120f, false, 2f, _ev);
            Assert.That(RunUntil(f, VesperEventKind.RewindTell, 4f, In(imps: 2)), Is.True);
            Run(f, f.T.rewindTellSeconds + 0.05f, In(imps: 2));
            Assert.That(f.S.health, Is.LessThanOrEqualTo(f.T.phase2At));
        }

        [Test]
        public void HerRewindWaitsForItsCooldownAndForDamageWorthUndoing()
        {
            var f = AtPhaseTwo();
            f.Hit(30f, false, 2f, _ev);   // under rewindMinUndo
            Run(f, 4f, In(imps: 2));
            Assert.That(Saw(VesperEventKind.RewindTell), Is.False);
            f.Hit(150f, false, 2f, _ev);
            RunUntil(f, VesperEventKind.Rewound, 4f + f.T.rewindTellSeconds, In(imps: 2));
            Assert.That(Saw(VesperEventKind.Rewound), Is.True);
            _ev.Clear();
            f.Hit(150f, false, 2f, _ev);
            Run(f, 3f, In(imps: 2));
            Assert.That(Saw(VesperEventKind.RewindTell), Is.False, "on cooldown");
        }

        [Test]
        public void HealthAgoReadsTheHistory()
        {
            var f = AtPhaseTwo();
            float h0 = f.S.health;
            Run(f, 2f, In(imps: 2));
            f.Hit(40f, false, 2f, _ev);
            Assert.That(f.HealthAgo(0f), Is.EqualTo(h0).Within(0.01f), "the newest sample is from before the hit");
            Run(f, 0.3f, In(imps: 2));
            Assert.That(f.HealthAgo(0f), Is.EqualTo(h0 - 40f).Within(0.01f));
            Assert.That(f.HealthAgo(1.5f), Is.EqualTo(h0).Within(0.01f));
        }

        // ------------------------------------------------------------------ damage rules

        [Test]
        public void TheHeartMultipliesAndTheGunsMultiplierWins()
        {
            var t = new VesperTuning();
            Assert.That(VesperDamage.Scale(22f, true, 2f, VesperAction.Idle, t), Is.EqualTo(44f));
            Assert.That(VesperDamage.Scale(9f, true, 1.25f, VesperAction.Idle, t), Is.EqualTo(11.25f));
            Assert.That(VesperDamage.Scale(22f, false, 2f, VesperAction.Idle, t), Is.EqualTo(22f));
            Assert.That(VesperDamage.Scale(22f, false, 2f, VesperAction.Exposed, t), Is.EqualTo(33f));
            Assert.That(VesperDamage.Scale(22f, true, 2f, VesperAction.Stagger, t), Is.EqualTo(66f));
            var f = NewFight();
            f.Begin(_ev);
            Assert.That(f.Hit(10f, true, 0f, _ev), Is.EqualTo(10f * t.heartMultiplier), "no gun multiplier: the tuning's");
        }

        /// <summary>A scripted shooter with the Writ (8 rounds, 1.75 s reload) against the whole fight: a shot every
        /// <paramref name="interval"/> s while she can be hurt, <paramref name="hitPct"/> % of shots land, every
        /// <paramref name="heartEvery"/>th hit on the heart; <paramref name="breaksRewinds"/>: during her Rewind's wind-up every
        /// hit goes to the heart. Returns the fight's length in seconds (the player's own health is not modelled).</summary>
        float SimulateFight(float interval, int hitPct, int heartEvery, bool breaksRewinds)
        {
            var f = NewFight();
            f.Begin(_ev);
            var player = new Vector3(4, 0, 6);
            float t = 0f, nextShot = 0f;
            int shot = 0, hits = 0, mag = 8;
            while (f.S.action != VesperAction.Done && t < 900f)
            {
                f.Tick(In(imps: 2, player: player), _ev);
                t += Dt;
                if (t < nextShot || !f.Vulnerable) continue;
                nextShot = t + interval;
                if (--mag <= 0) { mag = 8; nextShot = t + 1.75f; }
                if ((shot++ * 37) % 100 >= hitPct) continue;
                bool heart = (breaksRewinds && f.S.action == VesperAction.RewindTell) || hits % heartEvery == 0;
                hits++;
                f.Hit(22f, heart, 2f, _ev);
            }
            return t;
        }

        [Test]
        public void AFirstTryTakesTwoToFourMinutesAndASkilledRunAboutOne()
        {
            float first = SimulateFight(0.5f, 35, 5, false);
            float skilled = SimulateFight(0.28f, 65, 3, true);
            TestContext.WriteLine("first try " + first.ToString("0") + " s, skilled " + skilled.ToString("0") + " s");
            Assert.That(first, Is.InRange(120f, 240f), "a first try");
            Assert.That(skilled, Is.InRange(45f, 120f), "a skilled run");
        }

        // ------------------------------------------------------------------ she never quite goes for the kill

        [Test]
        public void SheSparesTheKillingBlowOncePerCooldown()
        {
            var f = NewFight();
            f.Begin(_ev);
            Run(f, 1f, In());
            Assert.That(f.TryMercy(_ev), Is.True);
            Assert.That(f.S.action, Is.EqualTo(VesperAction.Mercy));
            Assert.That(_ev.Any(e => e.kind == VesperEventKind.Line && e.line == VesperLines.Mercy), Is.True);
            int bolts = _ev.Count(e => e.kind == VesperEventKind.Bolt);
            Run(f, f.T.mercySeconds - 0.1f, In(health: 10f));
            Assert.That(_ev.Count(e => e.kind == VesperEventKind.Bolt), Is.EqualTo(bolts), "she holds fire while you get up");
            Assert.That(f.TryMercy(_ev), Is.False, "not again so soon: the next one kills");
            Run(f, f.T.mercyCooldown + 0.5f, In());
            Assert.That(f.TryMercy(_ev), Is.True, "after the cooldown, again");
        }

        [Test]
        public void SheWaitsLongerWhileYouAreLow()
        {
            int Volleys(float health)
            {
                _ev.Clear();
                var f = NewFight();
                f.Begin(_ev);
                Run(f, 40f, In(health: health));
                return _ev.Count(e => e.kind == VesperEventKind.VolleyTell);
            }
            Assert.That(Volleys(20f), Is.LessThan(Volleys(200f)));
        }

        // ------------------------------------------------------------------ the global Rewind's snapshot

        [Test]
        public void ASnapshotPutsTheFightBackExactlyAndReplaysTheSame()
        {
            var f = AtPhaseTwo();
            f.Hit(60f, false, 2f, _ev);
            Run(f, 0.7f, In(imps: 2));
            var snap = f.SaveTo();
            var before = f.S;
            float ago = f.HealthAgo(2f);
            _ev.Clear();
            f.Hit(150f, true, 2f, _ev);
            Run(f, 6f, In(imps: 1));
            var first = _ev.Select(e => e.kind).ToList();
            f.LoadFrom(snap);
            Assert.That(f.S.health, Is.EqualTo(before.health));
            Assert.That(f.S.action, Is.EqualTo(before.action));
            Assert.That(f.HealthAgo(2f), Is.EqualTo(ago), "her own history rewinds too");
            _ev.Clear();
            f.Hit(150f, true, 2f, _ev);
            Run(f, 6f, In(imps: 1));
            Assert.That(_ev.Select(e => e.kind).ToList(), Is.EqualTo(first), "the same choices again (the random stream rewinds)");
        }

        [Test]
        public void SavingIntoAnOldSnapshotReusesIt()
        {
            var f = NewFight();
            f.Begin(_ev);
            var a = f.SaveTo();
            Run(f, 1f, In());
            var b = f.SaveTo(a);
            Assert.That(b, Is.SameAs(a));
            Assert.That(b.s.clock, Is.EqualTo(f.S.clock));
        }
    }
}
