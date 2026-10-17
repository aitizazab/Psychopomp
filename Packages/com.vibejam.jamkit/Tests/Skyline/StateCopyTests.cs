using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Rewind's snapshots of the pure objects (docs/HELL-ENEMIES.md, "Rewind"): restore puts each back exactly, so
    /// replaying the same inputs after a restore gives the same outputs, random choices included.</summary>
    public class StateCopyTests
    {
        const float H = 1f / 50f;   // a fixed step

        static bool Sees(int i) => i % 97 < 70;   // in and out of sight

        static string Run(BotBrain b, int from, int to)
        {
            var cues = new List<BotCue>();
            var log = new System.Text.StringBuilder();
            for (int i = from; i < to; i++)
            {
                cues.Clear();
                b.Tick(H, Sees(i), cues);
                if (i % 37 == 0) b.OnDamaged(cues);
                foreach (var c in cues) log.Append(i).Append(':').Append(c.kind).Append(c.state).Append(c.bark).Append(' ');
                log.Append(b.State).Append(b.StateTime.ToString("F4")).Append('|');
            }
            return log.ToString();
        }

        [Test]
        public void ABrainRestoredReplaysExactly()
        {
            foreach (var d in HellEnemies.All())
            {
                var b = new BotBrain(d, 42);
                Run(b, 0, 150);
                var snap = StateCopy.Capture(b);
                string first = Run(b, 150, 400);
                StateCopy.Apply(b, snap);
                string again = Run(b, 150, 400);
                Assert.That(again, Is.EqualTo(first), d.id);
            }
        }

        [Test]
        public void AReusedSnapshotIsRefilledInPlaceAndStillRestoresExactly()
        {
            foreach (var d in HellEnemies.All())
            {
                var b = new BotBrain(d, 42);
                Run(b, 0, 60);
                var old = StateCopy.Capture(b);
                Run(b, 60, 150);
                var snap = StateCopy.Capture(b, old);
                Assert.That(snap, Is.SameAs(old), d.id);
                string first = Run(b, 150, 400);
                StateCopy.Apply(b, snap);
                string again = Run(b, 150, 400);
                Assert.That(again, Is.EqualTo(first), d.id);
            }
            Assert.That(StateCopy.Capture(new BotBrain(HellEnemies.FireImp(), 1), new object()), Is.Not.Null);
        }

        [Test]
        public void AWreckedBrainComesBackAlive()
        {
            var d = HellEnemies.FireImp();
            var b = new BotBrain(d, 7);
            Run(b, 0, 100);
            var state = b.State;
            var snap = StateCopy.Capture(b);
            b.OnWrecked(null);
            Assert.That(b.State, Is.EqualTo(BotState.Wrecked));
            StateCopy.Apply(b, snap);
            Assert.That(b.State, Is.EqualTo(state));
        }

        [Test]
        public void AHoverPatrolRestoredFliesTheSamePath()
        {
            var d = HellEnemies.FireImp();
            var p = new HoverPatrol(d, 99);
            var post = new Vector3(0f, 4f, 0f);
            var to = new Vector3(5f, -3f, 12f);
            for (int i = 0; i < 100; i++) { p.Tick(H, post, true, false, to, null); if (i % 40 == 0) p.OnVolley(); }
            var snap = StateCopy.Capture(p);
            var a = new List<Vector3>();
            for (int i = 0; i < 200; i++) { p.Tick(H, post, true, i % 50 < 15, to, null); if (i % 50 == 15) p.OnVolley(); if (i == 120) p.OnHit(); a.Add(p.Offset); }
            StateCopy.Apply(p, snap);
            for (int i = 0; i < 200; i++)
            {
                p.Tick(H, post, true, i % 50 < 15, to, null);
                if (i % 50 == 15) p.OnVolley();
                if (i == 120) p.OnHit();
                Assert.That(p.Offset, Is.EqualTo(a[i]), "step " + i);
            }
        }

        [Test]
        public void ABodyRestoredHasItsHealthAndPlatesBack()
        {
            var d = HellEnemies.ScytheDemon();
            var b = new EnemyBody(d);
            b.Damage(50f, 0);   // the left pauldron off, 10 into the chassis
            var snap = StateCopy.Capture(b);
            float chassis = b.Chassis;
            b.Damage(1000f, -1);
            Assert.That(b.Wrecked, Is.True);
            StateCopy.Apply(b, snap);
            Assert.That(b.Wrecked, Is.False);
            Assert.That(b.Chassis, Is.EqualTo(chassis));
            Assert.That(b.PlateOn(0), Is.False);
            Assert.That(b.PlateHp(1), Is.EqualTo(40f));
            // a snapshot can be applied again (it is never changed by a restore)
            b.Damage(1000f, -1);
            StateCopy.Apply(b, snap);
            Assert.That(b.Chassis, Is.EqualTo(chassis));
        }

        sealed class Holder
        {
            public readonly List<Vector3> route = new List<Vector3>();
            public List<Vector3> path;
            public readonly float[] ring = new float[4];
            public readonly EnemyDef def = HellEnemies.LavaOoze();
            public int index;
        }

        [Test]
        public void ListsAndArraysOfValuesAreCopiedAndSharedReferencesKept()
        {
            var h = new Holder { path = new List<Vector3> { Vector3.One } };
            h.route.Add(new Vector3(1f, 2f, 3f));
            h.ring[2] = 5f;
            h.index = 3;
            var snap = StateCopy.Capture(h);
            var def = h.def;
            h.route.Add(Vector3.Zero);
            h.path = new List<Vector3>();
            h.ring[2] = 9f;
            h.index = 7;
            StateCopy.Apply(h, snap);
            Assert.That(h.route, Is.EqualTo(new[] { new Vector3(1f, 2f, 3f) }));
            Assert.That(h.path, Is.EqualTo(new[] { Vector3.One }));
            Assert.That(h.ring[2], Is.EqualTo(5f));
            Assert.That(h.index, Is.EqualTo(3));
            Assert.That(h.def, Is.SameAs(def));
            h.route.Clear();
            StateCopy.Apply(h, snap);   // the snapshot was not changed by the first restore
            Assert.That(h.route.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheRandomStreamRewinds()
        {
            var r = new Rng(5);
            r.NextUInt();
            var snap = StateCopy.Capture(r);
            uint a = r.NextUInt(), b = r.NextUInt();
            StateCopy.Apply(r, snap);
            Assert.That(r.NextUInt(), Is.EqualTo(a));
            Assert.That(r.NextUInt(), Is.EqualTo(b));
        }

        [Test]
        public void TheAimTrailAndTheHellTimelinesRewind()
        {
            var trail = new PositionTrail();
            for (int i = 0; i < 50; i++) trail.Record(i * H, new Vector3(i, 0f, 0f));
            var ts = StateCopy.Capture(trail);
            for (int i = 50; i < 300; i++) trail.Record(i * H, new Vector3(i, 5f, 0f));
            StateCopy.Apply(trail, ts);
            Assert.That(trail.Count, Is.EqualTo(50));
            Assert.That(Aim.LaggedPoint(trail, 49 * H, 0.2f, Vector3.Zero), Is.EqualTo(new Vector3(39f, 0f, 0f)));

            var d = HellEnemies.ScytheDemon();
            var s = new ScytheBeam(new ScythePath { from = Vector3.Zero, to = new Vector3(10f, 0f, 0f) }, d);
            for (int i = 0; i < 60; i++) s.Tick(H);
            var ss = StateCopy.Capture(s);
            for (int i = 0; i < 60; i++) { s.Tick(H); s.TryHit(new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), 0.4f); }
            Assert.That(s.HasHit, Is.True);
            StateCopy.Apply(s, ss);
            Assert.That(s.HasHit, Is.False);
            Assert.That(s.T, Is.EqualTo(60 * H).Within(1e-5f));

            var o = HellEnemies.LavaOoze();
            var pool = new LavaPool(Vector3.Zero, o);
            pool.Tick(0.5f, true);
            var ps = StateCopy.Capture(pool);
            pool.Tick(5f, true);
            Assert.That(pool.Gone, Is.True);
            StateCopy.Apply(pool, ps);
            Assert.That(pool.Burning, Is.True);
            Assert.That(pool.T, Is.EqualTo(0.5f));
        }
    }
}
