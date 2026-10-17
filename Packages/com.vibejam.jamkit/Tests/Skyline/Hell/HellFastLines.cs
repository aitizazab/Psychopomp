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
    }
}
