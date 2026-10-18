using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;

namespace JamKit.Tests.Skyline
{
    /// <summary>The fast lines' tricky legs as input scripts (docs/HELL-ROOMS.md, "The traversal simulator": how to add a leg).
    /// Points are in the base layout's frame (<see cref="HellRoomLayouts"/>).</summary>
    public static class HellFastLines
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        public static IEnumerable<HellLeg> All()
        {
            foreach (var l in Sigils()) yield return l;
            foreach (var l in Lantern()) yield return l;
            foreach (var l in Chains()) yield return l;
            foreach (var l in Wells()) yield return l;
            foreach (var l in Brand()) yield return l;
            foreach (var l in Collapse()) yield return l;
        }

        // ------------------------------------------------------------------ The Six Sigils

        public static IEnumerable<HellLeg> Sigils()
        {
            const HellRoomKind K = HellRoomKind.SixSigils;
            // the start terrace's edge (z 22) to island A: an 8 m lava gap, a sprint jump and a double jump
            yield return new HellLeg(K, "terrace to A", V(-12, 0, 12), V(-16, 0, 30))
                .Run(V(-16, 0, 30), p => p.Pos.Z > 21.2f)
                .Jump(V(-17, 0, 33), p => p.Falling)
                .Jump(V(-18, 0, 34), p => p.Grounded)
                .Goal(V(-18, 0, 34), 3.5f);
            // A to B: jump and double jump onto the wall standing in the lava, run it, off its end onto B's low landing, then a
            // hop and a mantle up onto B (+3 m)
            yield return new HellLeg(K, "A-B wall-run onto B", V(-18, 0, 33), V(-12, 0, 41))
                .Run(V(-12, 0, 41), p => p.Pos.Z > 37.4f)
                .Jump(V(-9, 1, 42.6f), p => p.Falling)
                .Jump(V(-6, 1, 42.6f), p => p.Wallrunning || p.BeatTime > 1.2f)
                .Run(V(4, 1, 42.6f), p => p.Pos.X > -2f && p.Grounded)
                .Run(V(15, 0.5f, 47.5f), p => p.Pos.Z > 46.3f)
                .Jump(V(16, 3, 52), p => p.Grounded)
                .Goal(V(16, 3, 52), 3.5f);
            // B to C: the rising chain of five collapsing platforms, a hop on each as you land
            var chain = new HellLeg(K, "B-C collapse chain", V(16, 3, 52), V(8, 3.5f, 59.33f));
            var stones = new List<Vector3>();
            for (int i = 1; i <= 5; i++) stones.Add(V(12f - 24f * i / 6f, 3f + 0.5f * i, 56f + 20f * i / 6f));
            stones.Add(V(-16, 6, 80));
            stones.Add(V(-20, 6, 84));
            // on each platform run on toward the next and jump from its far half (a player resets on every platform, so the
            // errors don't add up), a double jump up onto C
            chain.Run(stones[0], p => p.Pos.Z > 55.2f || p.Pos.X < 12.6f);
            chain.Jump(stones[0], p => p.Grounded && p.BeatTime > 0.05f);
            for (int i = 0; i < 5; i++)
            {
                var next = stones[i + 1];
                float at = i < 4 ? 4f : 7f;
                chain.Run(next, p => p.HorizontalDistance(next) < at);
                chain.Jump(next, p => i < 4 ? p.Grounded && p.BeatTime > 0.05f : p.Falling);
            }
            chain.Jump(stones[6], p => p.Grounded).Goal(V(-16, 6, 80), 3.5f);
            yield return chain;
            // C to D: run off C and jump down onto the first dark perch, Shadow Step to the second keeping your speed, hop to D
            yield return new HellLeg(K, "C-D perch, Shadow Step, hop", V(-15, 6, 81), V(-4, 1.5f, 89))
                .Run(V(-4, 1.5f, 89), p => p.Pos.X > -12.6f)
                .Jump(V(-4, 1.5f, 89), p => p.Grounded)
                .Step(V(6.5f, 1, 98.5f), V(18, 0, 105), p => p.BeatTime > 0.12f)
                .Hop(V(20, 0, 106), p => p.Grounded)
                .Goal(V(16, 0, 104), 3.5f);
            // D to E: jump to the beam platform, slide under the beam, jump the lava into the pit
            yield return new HellLeg(K, "D-E beam slide into the pit", V(16, 0, 102), V(13, 0, 110.5f))
                .Run(V(13, 0, 110.5f), p => p.Pos.Z > 107.3f)
                .Jump(V(8, 0, 124), p => p.Grounded)
                .Slide(V(5, 0, 127), p => p.Pos.Z > 119.2f)
                .Jump(V(2, -2, 134), p => p.Falling)
                .Jump(V(2, -2, 132), p => p.Grounded)
                .Goal(V(2, -2, 132), 3.5f);
            // E to F: into the corridor west of the pit's wall, wall-run its west face north, wall-jump west over the pit wall onto
            // the low block in the lava, jump and mantle up onto F's ledge
            yield return new HellLeg(K, "E-F wall-run out of the pit", V(2, -2, 132), V(-7, -2, 134.5f))
                .Run(V(-8f, -2, 134), p => p.Pos.X < -7f)
                .Run(V(-3.4f, -2, 139.5f), p => p.Pos.Z > 136.5f)
                .Jump(V(-3.8f, 0, 146), p => p.Wallrunning || p.BeatTime > 1.2f)
                .Run(V(-4.4f, 0, 160), p => p.BeatTime > 0.25f || !p.Wallrunning)
                .Jump(V(-16, 0.5f, 158), p => p.Falling)
                .Jump(V(-16, 0.5f, 156), p => p.Grounded)
                .Hop(V(-24, 4, 154), p => p.Falling)
                .Jump(V(-24, 4, 154), p => p.Grounded)
                .Goal(V(-22, 4, 154), 4f);
        }

        /// <summary>Stepping stones the way a player crosses them: on each one, run on toward the next and jump from its far
        /// half (<paramref name="jumpAt"/>: the distance to the next stone's centre; <paramref name="lastJumpAt"/> for the
        /// last, a pier, aimed at a point inside it), so the errors don't add up. In the air the view looks past the landing
        /// (the stone after it), as a player's does: looking at the landing itself swings the view round as you reach it.</summary>
        static HellLeg Stones(HellLeg leg, Vector3 from, IList<Vector3> stones, float jumpAt, float lastJumpAt)
        {
            var prev = from;
            for (int i = 0; i < stones.Count; i++)
            {
                var next = stones[i];
                var dir = next - prev;
                dir.Y = 0f;
                var beyond = i + 1 < stones.Count ? stones[i + 1] : next + Vector3.Normalize(dir) * 8f;
                float at = i + 1 < stones.Count ? jumpAt : lastJumpAt;
                leg.Run(next, p => p.HorizontalDistance(next) < at);
                leg.Jump(beyond, p => p.Grounded && p.BeatTime > 0.05f);
                prev = next;
            }
            return leg;
        }

        // ------------------------------------------------------------------ The Soul Lantern

        public static IEnumerable<HellLeg> Lantern()
        {
            // carrying the lantern (0.8x, no Shadow Step) along the bridge of nine collapsing segments: no jump, the segments'
            // shake against the carry speed
            yield return new HellLeg(HellRoomKind.SoulLantern, "the collapsing bridge, carrying", V(0, 0, 8), V(0, 0, 54)).Carrying()
                .Run(V(0, 0, 60), p => p.Pos.Z > 50f)
                .Goal(V(0, 0, 54), 2.5f);
        }

        // ------------------------------------------------------------------ Break the Chains

        public static IEnumerable<HellLeg> Chains()
        {
            const HellRoomKind K = HellRoomKind.BreakChains;
            // off the start terrace onto the west wall (anchor 1 in range), a wall jump onto the middle pillar, a jump onto the
            // east wall (anchor 2 in range), a wall jump onto the north deck (anchor 3 from there)
            yield return new HellLeg(K, "the two walls", V(-3, 0, 4), V(-8, 0, 12))
                .Run(V(-8, 0, 12), p => p.Pos.Z > 9.3f)
                .Jump(V(-9.6f, 1, 18), p => p.Falling)
                .Jump(V(-9.6f, 1, 24), p => p.Wallrunning || p.BeatTime > 1.2f)
                .Run(V(-9.6f, 1, 40), p => p.Pos.Z > 19.5f || !p.Wallrunning)
                .Jump(V(-1, 1, 35), p => p.Falling)
                .Jump(V(-1, 1, 34), p => p.Grounded)
                .Run(V(9.6f, 2, 39), p => p.Pos.X > -0.5f)
                .Jump(V(9.6f, 2, 37), p => p.Falling)
                .Jump(V(9.6f, 3, 42), p => p.Wallrunning || p.BeatTime > 1.2f)
                .Run(V(9.6f, 3, 60), p => p.Pos.Z > 45.5f || !p.Wallrunning)
                .Jump(V(0, 0, 64), p => p.Falling)
                .Jump(V(0, 0, 64), p => p.Grounded)
                .Goal(V(2, 0, 58), 5f);
        }

        // ------------------------------------------------------------------ The Ooze Wells

        public static IEnumerable<HellLeg> Wells()
        {
            const HellRoomKind K = HellRoomKind.OozeWells;
            var toW1 = new List<Vector3>();
            for (int i = 1; i <= 2; i++) toW1.Add(Vector3.Lerp(V(-3, 0, 8), V(-13, 0, 22), i / 3f));
            toW1.Add(V(-15, 0, 24.5f));
            yield return Stones(new HellLeg(K, "stones to well 1", V(-1, 0, 2), toW1[0]).Run(toW1[0], p => p.Pos.Z > 4.8f).Jump(toW1[1], p => p.Grounded), toW1[0], toW1.GetRange(1, 2), 5.6f, 6.5f)
                .Goal(V(-15, 0, 24.5f), 3f);
            // from a standstill by well 1 (sealing it), across the row north of the wells to well 3's pier
            var row = new List<Vector3> { V(-3, 0, 29.5f), V(3, 0, 29.5f), V(9, 0, 29.5f), V(16.5f, 0, 29.5f) };
            yield return Stones(new HellLeg(K, "the row, well 1 to well 3", V(-18, 0, 29.5f), V(-9, 0, 29.5f)).StartSpeed(0f)
                    .Run(V(-9, 0, 29.5f), p => p.Pos.X > -13.6f).Jump(row[0], p => p.Grounded), V(-9, 0, 29.5f), row, 5.6f, 6.5f)
                .Goal(V(16.5f, 0, 29.5f), 3f);
            // from a standstill by well 3, the three stones to well 2's pier
            var toW2 = new List<Vector3>();
            for (int i = 2; i <= 3; i++) toW2.Add(Vector3.Lerp(V(13, 0, 28), V(3, 0, 49), i / 4f));
            toW2.Add(V(1, 0, 52));
            yield return Stones(new HellLeg(K, "stones, well 3 to well 2", V(16, 0, 24), V(10.5f, 0, 33.25f)).StartSpeed(0f)
                    .Run(V(10.5f, 0, 33.25f), p => p.Pos.Z > 27.4f).Jump(toW2[0], p => p.Grounded), V(10.5f, 0, 33.25f), toW2, 5.6f, 6.5f)
                .Goal(V(1, 0, 52), 3f);
        }

        // ------------------------------------------------------------------ Hold the Brand (hell-rooms2's layout: simulated only)

        public static IEnumerable<HellLeg> Brand()
        {
            const HellRoomKind K = HellRoomKind.HoldBrand;
            // the stepping stones over the lake: 1-1.5 m gaps, sprinted straight over
            yield return new HellLeg(K, "the stones in, at a sprint", V(0, 0, 4), V(0, 0, 30))
                .Run(V(0, 0, 40), p => p.Pos.Z > 25f)
                .Goal(V(0, 0, 29), 3f);
            // the dark pillars: a Shadow Step onto the first at a run (you keep your speed), a hop to the second, a hop onto the island
            yield return new HellLeg(K, "the dark pillars in", V(9.5f, 0, 2), V(9.5f, 0, 14.5f))
                .Run(V(9.5f, 0, 14.5f), p => p.Pos.Z > 8.5f)
                .Step(V(9.5f, 0.5f, 14.5f), V(8.5f, 0.5f, 26), p => p.Grounded && p.BeatTime > 0.02f)
                .Hop(V(8.5f, 0.5f, 26), p => p.Grounded)
                .Hop(V(8, 0, 34), p => p.Grounded)
                .Goal(V(8, 0, 29), 3f);
        }

        // ------------------------------------------------------------------ The Collapse (rooms3: the run-up hall, then the bridge)

        public static IEnumerable<HellLeg> Collapse()
        {
            const HellRoomKind K = HellRoomKind.Collapse;
            // the speed route: up the hall's stalactites, kicking from one row to the other, out onto the bridge
            yield return Kicks(new HellLeg(K, "the stalactite kicks", V(RunX, 0, 12), V(RunX, 0, 30)))
                .Goal(V(0, 0, 60), 5f, 6f);
            yield return new HellLeg(K, "the 4 m lava gap", V(0, 0, 64), V(0, 0, 90))
                .Run(V(0, 0, 100), p => p.Pos.Z > 75.2f)
                .Jump(V(0, 0, 100), p => p.Grounded)
                .Goal(V(0, 0, 86), 4f);
        }

        /// <summary>The kicks: run the line beside the east row, jump up into its first column (a stalagmite), then kick across
        /// to the other row's next column and back, zig-zagging (each kick within the wall-kick window keeps the speed and adds
        /// along the wall), and drop out of the last one running.</summary>
        static HellLeg Kicks(HellLeg leg)
        {
            const float far = 90f, H = HellRoomLayouts.CollapseStalactiteHalfGap, endZ = 50f;
            var first = HellRoomLayouts.CollapseKickColumns[0];
            return leg
                .Run(V(RunX, 0, 60), p => p.Pos.Z > first.z - RunUp)
                .Jump(V(H, 1.5f, first.z + 3f), p => p.Wallrunning || p.Pos.Z > endZ)
                .Jump(V(-H, 2, far), p => (p.Wallrunning && p.Pos.X < 0) || p.Pos.Z > endZ)
                .Jump(V(H, 2, far), p => (p.Wallrunning && p.Pos.X > 0) || p.Pos.Z > endZ)
                .Jump(V(-H, 2, far), p => (p.Wallrunning && p.Pos.X < 0) || p.Pos.Z > endZ)
                .Jump(V(0, 0, far), p => p.Grounded);
        }

        /// <summary>The kick route's running line (beside the east row) and where it jumps for the first column (this far
        /// before its centre).</summary>
        public const float RunX = HellRoomLayouts.CollapseStalactiteHalfGap - 0.85f, RunUp = 5.5f;

        /// <summary>The whole speed route on the room clock: dropped in at the entry, the kicks, the bridge's central lane and
        /// its gap, to the end floor.</summary>
        public static HellLeg CollapseSpeedRoute() =>
            Bridge(Kicks(new HellLeg(HellRoomKind.Collapse, "the speed route", V(RunX, 4, -4), V(RunX, 0, 20)).StartSpeed(0f)));

        /// <summary>The normal route into the fast lane: a sprint straight below the stalactites, then the central lane.</summary>
        public static HellLeg CollapseNormalRouteFastLane() =>
            Bridge(new HellLeg(HellRoomKind.Collapse, "a sprint below, then the fast lane", V(0, 4, -4), V(0, 0, 20)).StartSpeed(0f)
                .Run(V(0, 0, 130), p => p.Pos.Z > 50f));

        /// <summary>The normal route with the ground's own speed tools: a slide below the stalactites, a hop out of it and hops on
        /// every landing, then the central lane.</summary>
        public static HellLeg CollapseSlideHopFastLane() =>
            Bridge(new HellLeg(HellRoomKind.Collapse, "a slide and hops below, then the fast lane", V(0, 4, -4), V(0, 0, 20)).StartSpeed(0f)
                .Run(V(0, 0, 130), p => p.Pos.Z > 6f)
                .Slide(V(0, 0, 130), p => p.Pos.Z > 12f)
                .Jump(V(0, 0, 130), p => p.Grounded)
                .Hop(V(0, 0, 130), p => p.Grounded && p.BeatTime > 0.1f)
                .Hop(V(0, 0, 130), p => p.Grounded && p.BeatTime > 0.1f)
                .Hop(V(0, 0, 130), p => p.Grounded && p.BeatTime > 0.1f)
                .Run(V(0, 0, 130), p => p.Pos.Z > 50f));

        static HellLeg Bridge(HellLeg leg) => leg
            .Slide(V(0, 0, 130), p => p.Pos.Z > 75.2f || p.Sim.HorizontalSpeed < 9f)
            .Run(V(0, 0, 130), p => p.Pos.Z > 75.2f)
            .Jump(V(0, 0, 130), p => p.Grounded)
            .Run(V(0, 0, 130), p => p.Pos.Z > HellRoomLayouts.CollapseEndZ + 0.5f)
            .Goal(V(0, 0, 116), 4f, 20f);
    }
}
