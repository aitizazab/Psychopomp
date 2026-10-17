using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// The six room layouts as data (event/HELL-DESIGN.md, "The rooms"), in the room's own frame (entry vestibule at z &lt; 0,
    /// the room running north, the exit doorway in the north wall). Ground is y 0; lava lies in 1.2 m deep basins (surface
    /// y -1.2), so lava reads as a glowing trench and the stone walkways stand above it. Every room has a FAST path (the
    /// straight line: over lava, across collapsing platforms, through beam lanes) and a SAFE path (stone walkways with cover,
    /// round the edge, longer); <see cref="HellRoomLint"/> checks both. <see cref="HellRoomVariants"/> makes the per-floor
    /// variants (mirror, hazard intensity, enemy mix) from these.
    /// </summary>
    public static class HellRoomLayouts
    {
        public const float WallHeight = 8f, LavaSurface = -1.2f, LavaDepth = 1.3f, PlatformThickness = 0.6f;
        public const float BeamHeight = 1.3f;
        /// <summary>Lava lights: one every this many metres over a basin, this radius, kept this far from a dark zone, dropped below the minimum.</summary>
        public const float LightSpacing = 12f, LightRadius = 9f, LightClearance = 1f, MinLightRadius = 3f;
        /// <summary>The exit corridor's length before its drop hole, the hole's length, and how far the drop falls.</summary>
        public const float CorridorLength = 8f, HoleLength = 4f, DropDepth = 12f;

        public static readonly HellRoomKind[] Kinds =
        {
            HellRoomKind.SixSigils, HellRoomKind.SoulLantern, HellRoomKind.BreakChains,
            HellRoomKind.HoldBrand, HellRoomKind.Collapse, HellRoomKind.OozeWells,
        };

        public static HellRoomDef Build(HellRoomKind kind)
        {
            switch (kind)
            {
                case HellRoomKind.SixSigils: return SixSigils();
                case HellRoomKind.SoulLantern: return SoulLantern();
                case HellRoomKind.BreakChains: return BreakChains();
                case HellRoomKind.HoldBrand: return HoldBrand();
                case HellRoomKind.Collapse: return Collapse();
                case HellRoomKind.OozeWells: return OozeWells();
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ------------------------------------------------------------------ 1. The Six Sigils

        /// <summary>
        /// A 72 x 170 m lava cavern, built for Hell from scratch. A stone ring walkway runs round the walls (cover on its lava
        /// edge); the sigils stand far apart (legs of 30 to 47 m) on islands, a pillar, a sunken pit and a high ledge, each
        /// joined to the ring by ONE bridge or ramp, so the safe way to any sigil is the long way round the ring. Every straight
        /// line is blocked by something that needs a movement tool, a different one each leg:
        /// <list type="bullet">
        /// <item>A (island, y 0) to B (island, y 3): a lava gulf with a 22 m runnable wall standing in it: wall-run.</item>
        /// <item>B to C (pillar top, y 6): a rising chain of five collapsing platforms.</item>
        /// <item>C to D (island, y 0): a 40 m gulf crossed by two dark perches: the Shadow Step chain.</item>
        /// <item>D to E (pit floor, y -2): a platform with a beam lane across it (slide), then a drop into the pit.</item>
        /// <item>E to F (ledge, y 4): two tall runnable walls, pit to lava to ledge: chained wall-runs up and out.</item>
        /// </list>
        /// </summary>
        static HellRoomDef SixSigils()
        {
            var k = new Kit(HellRoomKind.SixSigils, "The Six Sigils");
            const float W = 36f, L = 170f, Ring = 30f;
            k.Shell(-W, W, L, -3f, 12f, 0f, 0f);
            k.r.minLeg = 30f;
            // the start terrace, the ring walkways, the north terrace (cut round F's ledge)
            k.Floor(-W, 0, W, 22);
            k.Floor(-W, 22, -Ring, 160);
            k.Floor(Ring, 22, W, 160);
            k.Floor(-W, 160, -Ring, L);
            k.Floor(-Ring, 166, -14, L);
            k.Floor(-14, 160, W, L);
            // the lava sea, cut round the pit
            k.Lava(-Ring, 22, Ring, 127.4f);
            k.Lava(-Ring, 127.4f, -10.6f, 160);
            k.Lava(6.6f, 127.4f, Ring, 160);
            // A: an island, bridged from the west ring
            k.Deck(-22, 30, -14, 38, LavaSurface - LavaDepth, 0f);
            k.Floor(-Ring, 32, -22, 36);
            // A-B: the wall-run wall
            k.Wall(-12, 43, 10, 43.6f, LavaSurface - LavaDepth, 7f, true, "wallrun");
            // B: a raised island, a ramp from the east ring
            k.Deck(12, 48, 20, 56, LavaSurface - LavaDepth, 3f);
            k.Ramp(20, 50, Ring, 54, LavaSurface - LavaDepth, 3f, 0f, true);
            // B-C: the rising collapse chain
            for (int i = 1; i <= 5; i++)
            {
                float t = i / 6f;
                float cx = 12f + (-12f - 12f) * t, cz = 56f + (76f - 56f) * t;
                k.Collapse(cx - 1.5f, cz - 1.5f, cx + 1.5f, cz + 1.5f, 3f + 0.5f * i);
            }
            // C: a pillar, a ramp from the west ring
            k.Deck(-20, 76, -12, 84, LavaSurface - LavaDepth, 6f);
            k.Ramp(-Ring, 78, -20, 82, LavaSurface - LavaDepth, 0f, 6f, true);
            // C-D: two dark perches
            Pillar(k, -5.35f, 86.65f, -2.85f, 89.15f, 1f);
            Pillar(k, 6.85f, 94.85f, 9.35f, 97.35f, 1f);
            // D: an island, bridged from the east ring
            k.Deck(16, 100, 24, 108, LavaSurface - LavaDepth, 0f);
            k.Floor(24, 102, Ring, 106);
            // D-E: the beam platform
            k.Deck(2, 114, 20, 120, LavaSurface - LavaDepth, 0f);
            k.Beam(2, 116.8f, 20, 117.2f, 0f, 2.2f, 0.8f, 0f);
            // E: the pit (floor y -2), walled, a ramp up to the north terrace
            k.Floor(-10, 128, 6, 144, -2f);
            k.Ramp(-10, 144, 6, 160, -2.5f, -2f, 0f, false);
            k.Wall(-10.6f, 127.4f, -10f, 160, -2.5f, 0.5f, false, "pit");
            k.Wall(6f, 127.4f, 6.6f, 160, -2.5f, 0.5f, false, "pit");
            k.Wall(-10f, 127.4f, 6f, 128, -2.5f, 0.5f, false, "pit");
            // E-F: the two tall runnable walls
            k.Wall(-6f, 136, -5.4f, 146, -2.5f, 9f, true, "wallrun");
            k.Wall(-13f, 144, -12.4f, 152, LavaSurface - LavaDepth, 9f, true, "wallrun");
            // F: the high ledge, a ramp up from the north terrace
            k.Deck(-Ring, 148, -14, 166, LavaSurface - LavaDepth, 4f);
            k.Ramp(-14, 162, 4, 166, 0f, 4f, 0f, true);
            k.Dark(-18, 4f, 148, -14, 7f, 152);
            // cover on the ring's lava edge, gaps at the bridges and ramps
            for (float z = 26; z < 156; z += 14f)
            {
                if (!Overlaps(z, z + 4, 31, 37) && !Overlaps(z, z + 4, 77, 83) && !Overlaps(z, z + 4, 147, 167)) k.Cover(-Ring - 0.6f, z, -Ring, z + 4);
                if (!Overlaps(z, z + 4, 49, 55) && !Overlaps(z, z + 4, 101, 107)) k.Cover(Ring, z, Ring + 0.6f, z + 4);
            }

            k.Stop("sigil-A", V(-18, 0, 34), 2f);
            k.Stop("sigil-B", V(16, 3, 52), 2f);
            k.Stop("sigil-C", V(-16, 6, 80), 2f);
            k.Stop("sigil-D", V(20, 0, 104), 2f);
            k.Stop("sigil-E", V(2, -2, 132), 2f);
            k.Stop("sigil-F", V(-22, 4, 154), 2f);

            V3(k.r.safePath, k.r.entry, 0, 0, 10, -33, 0, 20, -33, 0, 34, -18, 0, 34,            // A
                -33, 0, 34, -33, 0, 20, 33, 0, 20, 33, 0, 52, 30, 0, 52, 20, 3, 52, 16, 3, 52,   // B
                20, 3, 52, 30, 0, 52, 33, 0, 52, 33, 0, 20, -33, 0, 20, -33, 0, 80, -30, 0, 80, -20, 6, 80, -16, 6, 80, // C
                -20, 6, 80, -30, 0, 80, -33, 0, 80, -33, 0, 158, -33, 0, 168, 33, 0, 168, 33, 0, 104, 20, 0, 104,     // D
                33, 0, 104, 33, 0, 168, 8, 0, 168, 8, 0, 161, -2, 0, 161, -2, 0, 160, -2, -2, 144, 2, -2, 132,       // E
                -2, -2, 144, -2, 0, 160, -2, 0, 161, 5, 0, 161, 5, 0, 164, -14, 4, 164, -20, 4, 160, -22, 4, 154,   // F
                -20, 4, 160, -14, 4, 164, 5, 0, 164, 5, 0, 168, 0, 0, 168);
            k.r.safePath.Add(k.r.exit);
            V3(k.r.fastPath, k.r.entry, -14, 0, 22, -18, 0, 34, -12, 1, 42.4f, 0, 2, 42.4f, 10, 2, 42.4f, 16, 3, 52,
                8, 3.5f, 59.3f, 4, 4, 62.7f, 0, 4.5f, 66, -4, 5, 69.3f, -8, 5.5f, 72.7f, -16, 6, 80,
                -4.1f, 1, 87.9f, 8.1f, 1, 96.1f, 20, 0, 104, 11, 0, 117, 4, -2, 130, 2, -2, 132,
                -5.7f, 0, 140, -5.7f, 2, 145, -12.1f, 3, 148, -12.1f, 3.5f, 151, -22, 4, 154, -20, 4, 165.9f, -14, 0, 168);
            k.r.fastPath.Add(k.r.exit);

            k.Spawn(HellEnemyRole.Imp, V(0, 6, 34));
            k.Spawn(HellEnemyRole.Imp, V(-2, 7, 64));
            k.Spawn(HellEnemyRole.Imp, V(4, 6, 98));
            k.Spawn(HellEnemyRole.Imp, V(-16, 8, 130), minFloor: 2);
            k.Spawn(HellEnemyRole.Imp, V(20, 7, 140), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(18, 0, 118), 270f, minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(33, 0, 80), 270f, minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(-33, 0, 120), 90f, minFloor: 3);
            k.Spawn(HellEnemyRole.Ooze, V(-2, -2, 140), minFloor: 3);
            k.Spawn(HellEnemyRole.Ooze, V(20, 0, 166), minFloor: 3);
            k.r.tuning.radius = 2f;
            return k.Done();
        }

        static bool Overlaps(float a0, float a1, float b0, float b1) => a0 < b1 && b0 < a1;

        static void V3(List<Vector3> into, Vector3 first, params float[] xyz)
        {
            into.Add(first);
            for (int i = 0; i + 2 < xyz.Length; i += 3) into.Add(new Vector3(xyz[i], xyz[i + 1], xyz[i + 2]));
        }

        // ------------------------------------------------------------------ 2. The Soul Lantern

        /// <summary>Pick the lantern up by the entry and carry it to the altar across a lava chasm. Carrying, you are slower and
        /// cannot Shadow Step. Fast: a collapsing bridge straight across; safe: the wall ledge (up a ramp, along the west wall
        /// behind low cover, down a ramp).</summary>
        static HellRoomDef SoulLantern()
        {
            var k = new Kit(HellRoomKind.SoulLantern, "The Soul Lantern");
            const float W = 22f, L = 60f;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 12);
            k.Floor(-W, 48, W, L);
            k.Lava(-16, 12, W, 48);
            k.Dark(-W, 3f, 28f, -19f, 6f, 32f);      // an alcove on the ledge
            k.Dark(18f, 0f, 56f, W, 3f, L);          // the north-east corner
            k.Ramp(-W, 12, -16, 24, -2.5f, 0f, 3f, false);
            k.Deck(-W, 24, -16, 36, -2.5f, 3f);
            k.Ramp(-W, 36, -16, 48, -2.5f, 3f, 0f, false);
            k.Cover(-16.6f, 25, -16f, 29, 3f);
            k.Cover(-16.6f, 31, -16f, 35, 3f);
            for (float z = 12; z < 48; z += 4f) k.Collapse(-1.5f, z, 1.5f, z + 4f, 0f);
            k.Stop("lantern", V(0, 0, 7), 2f);
            k.Stop("altar", V(0, 0, 54), 2.5f);
            k.r.safePath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 7), V(-19, 0, 10), V(-19, 0, 12), V(-19, 3, 24), V(-19, 3, 36), V(-19, 0, 48),
                V(-17, 0, 52), V(0, 0, 54), k.r.exit,
            });
            k.r.fastPath.AddRange(new[] { k.r.entry, V(0, 0, 7), V(0, 0, 30), V(0, 0, 54), k.r.exit });
            k.Spawn(HellEnemyRole.Imp, V(8, 5, 30));
            k.Spawn(HellEnemyRole.Imp, V(-6, 6, 20));
            k.Spawn(HellEnemyRole.Imp, V(10, 6, 44), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(14, 0, 56), 225f, minFloor: 2);
            k.Spawn(HellEnemyRole.Ooze, V(-14, 0, 56), minFloor: 3);
            k.r.tuning.radius = 2f;
            k.r.collapseReturnSeconds = 4f;
            return k.Done();
        }

        // ------------------------------------------------------------------ 3. Break the Chains

        /// <summary>Three chain anchors hold the exit gate, warded: a shot only bites within 12 m. Anchor 1 is low on the west
        /// wall, 2 higher on the east wall, 3 high over the exit. Fast: wall-run the walls standing in the lava and shoot in
        /// mid-air (and a jump off the north deck for the third); safe: climb the ramps to each anchor's balcony.</summary>
        static HellRoomDef BreakChains()
        {
            var k = new Kit(HellRoomKind.BreakChains, "Break the Chains");
            const float W = 20f, L = 64f;
            k.Shell(-W, W, L, -2.5f, 14f, 0f, 0f);
            k.Floor(-W, 0, W, 10);
            k.Floor(-W, 10, -12, 54);
            k.Floor(12, 10, W, 54);
            k.Floor(-W, 54, W, L);
            k.Lava(-12, 10, 12, 54);
            // west balcony (anchor 1): up from the south, down to the north
            k.Ramp(-W, 10, -15, 20, 0f, 0f, 3f, false);
            k.Deck(-W, 20, -15, 26, 0f, 3f);
            k.Ramp(-W, 26, -15, 38, 0f, 3f, 0f, false);
            // east balcony (anchor 2): up from the north
            k.Deck(15, 34, W, 42, 0f, 6f);
            k.Ramp(15, 42, W, 54, 0f, 6f, 0f, false);
            // the gallery over the exit (anchor 3), up a ramp along the north wall
            k.Ramp(-W, 61, -6, L, 0f, 0f, 8f, true);
            k.Deck(-6, 61, 6, L, 7.5f, 8f);
            // wall-run walls standing in the lava
            k.Wall(-10.6f, 14, -10f, 32, -2.5f, 6f, true, "wallrun");
            k.Wall(10f, 30, 10.6f, 48, -2.5f, 9f, true, "wallrun");
            Pillar(k, -6, 20, -3, 23, 1f);
            Pillar(k, -1, 28, 2, 31, 1f);
            Pillar(k, 3, 38, 6, 41, 1f);
            k.Stop("anchor-1", V(-19.5f, 4.5f, 23), 12f);
            k.Stop("anchor-2", V(19.5f, 7.5f, 38), 12f);
            k.Stop("anchor-3", V(0, 12f, 63.5f), 12f);
            k.r.ordered = false;
            k.r.safePath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 1), V(-17.5f, 0, 6), V(-17.5f, 0, 10), V(-17.5f, 3, 20), V(-17.5f, 3, 23), V(-17.5f, 3, 26),
                V(-17.5f, 0, 38), V(-13.5f, 0, 40), V(-13.5f, 0, 54), V(13.5f, 0, 56), V(17.5f, 0, 54.5f), V(17.5f, 0, 54),
                V(17.5f, 6, 42), V(17.5f, 6, 38), V(17.5f, 6, 42), V(17.5f, 0, 54), V(17.5f, 0, 55), V(-19.5f, 0, 59), V(-19.5f, 0, 62.5f),
                V(-6, 8, 62.5f), V(-2, 8, 62.5f), V(-6, 8, 62.5f), V(-19.5f, 0, 62.5f), V(-19.5f, 0, 59), V(-10, 0, 58), V(0, 0, 58), k.r.exit,
            });
            k.r.fastPath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 10), V(-9.7f, 3, 20), V(-9.7f, 3, 26), V(0, 1, 34), V(9.7f, 4, 38), V(9.7f, 3, 46),
                V(0, 0, 54), V(0, 2.5f, 58), k.r.exit,
            });
            k.Spawn(HellEnemyRole.Imp, V(0, 6, 24));
            k.Spawn(HellEnemyRole.Imp, V(0, 7, 44));
            k.Spawn(HellEnemyRole.Imp, V(-8, 8, 50), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(16, 0, 20), 270f, minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(-16, 0, 48), 90f, minFloor: 3);
            k.Spawn(HellEnemyRole.Ooze, V(8, 0, 58), minFloor: 3);
            k.r.tuning.wardRadius = 12f;
            return k.Done();
        }

        // ------------------------------------------------------------------ 4. Hold the Brand

        /// <summary>The brand circle on an island in a lava lake: stand in it until it is charged (the kit's tow circle, 10 s,
        /// pausing when you step out), waves arriving every few seconds. Fast: collapsing stepping stones straight to the
        /// island; safe: the west walkway and a long open bridge the imps can see all of.</summary>
        static HellRoomDef HoldBrand()
        {
            var k = new Kit(HellRoomKind.HoldBrand, "Hold the Brand");
            const float W = 26f, L = 70f;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 10);
            k.Floor(-W, 10, -20, 56);
            k.Floor(-6, 30, 6, 42);                 // the island
            k.Floor(-20, 34, -6, 38);               // the long bridge
            k.Floor(-2, 42, 2, 56);                 // the north bridge
            k.Floor(-W, 56, W, L);
            k.Lava(-20, 10, W, 30);
            k.Lava(-20, 30, -6, 34);
            k.Lava(-20, 38, -6, 56);
            k.Lava(-6, 42, -2, 56);
            k.Lava(2, 42, W, 56);
            k.Lava(6, 30, W, 42);
            foreach (var z in new[] { 11.5f, 16f, 20.5f, 25f }) k.Collapse(-1.5f, z, 1.5f, z + 3f, 0f);
            Pillar(k, 8, 15, 11, 18, 0.5f);
            Pillar(k, 7, 24, 10, 27, 0.5f);
            k.Stop("brand", V(0, 0, 36), FloorRules.CircleRadius);
            k.r.safePath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 5), V(-23, 0, 8), V(-23, 0, 36), V(-6, 0, 36), V(0, 0, 36), V(0, 0, 42), V(0, 0, 56), k.r.exit,
            });
            k.r.fastPath.AddRange(new[] { k.r.entry, V(0, 0, 36), k.r.exit });
            k.Spawn(HellEnemyRole.Imp, V(12, 6, 20));
            k.Spawn(HellEnemyRole.Imp, V(14, 5, 36), wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 24), wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(0, 6, 50), wave: 2);
            k.Spawn(HellEnemyRole.Scythe, V(8, 0, 62), 180f, wave: 2, minFloor: 2);
            k.Spawn(HellEnemyRole.Imp, V(12, 5, 22), wave: 3);
            k.Spawn(HellEnemyRole.Scythe, V(-8, 0, 62), 180f, wave: 3, minFloor: 2);
            k.Spawn(HellEnemyRole.Ooze, V(4, 0, 40), wave: 3, minFloor: 3);
            k.r.tuning.radius = FloorRules.CircleRadius;
            k.r.tuning.seconds = 5f;              // a wave every 5 s once the brand is first touched
            k.r.collapseReturnSeconds = 5f;
            return k.Done();
        }

        // ------------------------------------------------------------------ 5. The Collapse

        /// <summary>Reach the exit before the floor falls away behind you. The floor tiles fall on the room clock, row by row from
        /// the entry. The central lane is straight (a lava gap to jump, a beam to slide under, a 12 m gap for the Shadow Step)
        /// and its front comes fast; the west lane is solid stone that winds between walls, and its front comes slower.</summary>
        static HellRoomDef Collapse()
        {
            var k = new Kit(HellRoomKind.Collapse, "The Collapse");
            const float W = 20f, L = 120f;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 8);
            k.Floor(-W, 112, W, L);
            k.Lava(-W, 8, W, 112);
            // the central lane's tiles (gaps at 30-34 and 70-82)
            float[][] segs = { new[] { 8f, 30f }, new[] { 34f, 70f }, new[] { 82f, 112f } };
            foreach (var s in segs)
                for (float z = s[0]; z < s[1] - 0.01f; z += 4f)
                    k.Tile(-3, z, 3, Math.Min(z + 4f, s[1]), false);
            k.Beam(-3, 49.8f, 3, 50.2f, 0f, 2.5f, 0.5f, 0f);
            k.Dark(-3f, 0f, 82f, 3f, 3f, 90f);       // the landing past the 12 m gap
            // the west lane's tiles and its winding walls
            for (float z = 8; z < 112 - 0.01f; z += 4f) k.Tile(-W, z, -8, Math.Min(z + 4f, 112f), true);
            var safe = new List<Vector3> { k.r.entry, V(0, 0, 4), V(-13, 0, 6) };
            for (int i = 0; i < 9; i++)
            {
                float z = 16f + 10f * i;
                bool even = i % 2 == 0;
                if (even) k.Wall(-W, z, -11f, z + 0.6f, 0f, 3f, false, "baffle");
                else k.Wall(-15f, z, -8f, z + 0.6f, 0f, 3f, false, "baffle");
                float gx = even ? -9.5f : -17.5f;
                safe.Add(V(gx, 0, z - 1.5f));
                safe.Add(V(gx, 0, z + 2.1f));
            }
            safe.Add(V(-13, 0, 110));
            safe.Add(V(-6, 0, 114));
            safe.Add(V(0, 0, 116));
            safe.Add(k.r.exit);
            k.r.safePath.AddRange(safe);
            k.r.fastPath.AddRange(new[] { k.r.entry, V(0, 0, 32), V(0, 0, 50), V(0, 0.5f, 76), k.r.exit });
            k.Stop("exit", V(0, 0, L - 2f), 2.5f);
            k.r.exitGateId = "";
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 40));
            k.Spawn(HellEnemyRole.Imp, V(6, 6, 60));
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 90), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(0, 0, 116), 180f, minFloor: 2);
            k.Spawn(HellEnemyRole.Ooze, V(12, 0, 116), minFloor: 3);
            k.r.collapseReturnSeconds = -1f;
            return k.Done();
        }

        /// <summary>The Collapse's timeline: the room second a tile in a lane starts to shake, from its near edge.</summary>
        public const float CollapseDelay = 4f, CollapseFastLaneSpeed = 7f, CollapseSafeLaneSpeed = 4f, CollapseStartZ = 8f;

        public static float CollapseTileSeconds(float nearZ, bool safeLane, float speedScale = 1f)
        {
            float v = (safeLane ? CollapseSafeLaneSpeed : CollapseFastLaneSpeed) * speedScale;
            return CollapseDelay + Math.Max(0f, nearZ - CollapseStartZ) / v;
        }

        // ------------------------------------------------------------------ 6. The Ooze Wells

        /// <summary>Three wells in a lava lake keep spawning oozes; stand by each for 3 s to seal it (any order). Fast: hop the
        /// collapsing stepping stones across the lake between the wells; safe: the ring walkway round the edge and the piers.</summary>
        static HellRoomDef OozeWells()
        {
            var k = new Kit(HellRoomKind.OozeWells, "The Ooze Wells");
            const float W = 26f, L = 64f;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 8);
            k.Floor(-W, 8, -20, 56);
            k.Floor(20, 8, W, 56);
            k.Floor(-W, 56, W, L);
            k.Floor(-20, 22, -13, 28);              // pier W1
            k.Floor(13, 22, 20, 28);                // pier W3
            k.Floor(-3, 49, 3, 56);                 // pier W2
            k.Lava(-20, 8, 20, 22);
            k.Lava(-13, 22, 13, 28);
            k.Lava(-20, 28, 20, 49);
            k.Lava(-20, 49, -3, 56);
            k.Lava(3, 49, 20, 56);
            k.Well(-12.5f, 23.5f, -9.5f, 26.5f);
            k.Well(-1.5f, 44.5f, 1.5f, 47.5f);
            k.Well(9.5f, 23.5f, 12.5f, 26.5f);
            Pillar(k, 3, 14, 6, 17, 0.5f);
            Pillar(k, -6, 38, -3, 41, 0.5f);
            k.Stop("well-1", V(-11, 0, 25), 3.5f);
            k.Stop("well-2", V(0, 0, 46), 3.5f);
            k.Stop("well-3", V(11, 0, 25), 3.5f);
            k.r.stopSpawns.AddRange(new[] { V(-17, 0, 25), V(0, 0, 53), V(17, 0, 25) });
            k.r.ordered = false;
            // stepping stones: entry to W1, W1 to W3 (north of the wells), W3 to W2
            StonesAlong(k, V(-3, 0, 8), V(-13, 0, 22), 3);
            foreach (var x in new[] { -9f, -4.5f, 0f, 4.5f, 9f }) k.Collapse(x - 1.5f, 28f, x + 1.5f, 31f, 0f);
            StonesAlong(k, V(13, 0, 28), V(3, 0, 49), 3);
            k.r.safePath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 4), V(-23, 0, 6), V(-23, 0, 25), V(-14, 0, 25), V(-23, 0, 25), V(-23, 0, 58), V(0, 0, 58),
                V(0, 0, 49.3f), V(0, 0, 58), V(23, 0, 58), V(23, 0, 25), V(14, 0, 25), V(23, 0, 25), V(23, 0, 58), V(0, 0, 60),
                k.r.exit,
            });
            k.r.fastPath.AddRange(new[]
            {
                k.r.entry, V(-3, 0, 8), V(-13, 0, 22), V(-13, 0, 26), V(-13, 0, 29.5f), V(13, 0, 29.5f), V(13, 0, 25),
                V(13, 0, 28), V(3, 0, 49), V(0, 0, 49.3f), k.r.exit,
            });
            k.Spawn(HellEnemyRole.Imp, V(0, 6, 30));
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 45), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(14, 0, 60), 180f, minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(-14, 0, 60), 180f, minFloor: 3);
            k.r.tuning.radius = 3.5f;
            k.r.tuning.seconds = 3f;
            k.r.collapseReturnSeconds = 4f;
            return k.Done();
        }

        /// <summary>A dark stone pillar standing in the lava (a Shadow Step perch): the solid and its dark zone.</summary>
        static void Pillar(Kit k, float x0, float z0, float x1, float z1, float top)
        {
            k.Deck(x0, z0, x1, z1, LavaSurface - LavaDepth, top);
            k.Dark(x0, top, z0, x1, top + 3f, z1);
        }

        /// <summary><paramref name="n"/> 3 x 3 m collapsing stones evenly between a and b (exclusive).</summary>
        static void StonesAlong(Kit k, Vector3 a, Vector3 b, int n)
        {
            for (int i = 1; i <= n; i++)
            {
                var c = Vector3.Lerp(a, b, i / (float)(n + 1));
                k.Collapse(c.X - 1.5f, c.Z - 1.5f, c.X + 1.5f, c.Z + 1.5f, 0f);
            }
        }

        // ------------------------------------------------------------------ the kit

        sealed class Kit
        {
            public readonly HellRoomDef r;
            readonly Dictionary<string, int> _n = new Dictionary<string, int>();

            public Kit(HellRoomKind kind, string name)
            {
                r = new HellRoomDef { kind = kind, name = name };
            }

            string Id(string p)
            {
                _n.TryGetValue(p, out int n);
                _n[p] = n + 1;
                return p + "-" + n;
            }

            public FloorSolid Add(string id, string role, float x0, float y0, float z0, float x1, float y1, float z1, bool runnable = false)
            {
                var s = new FloorSolid { id = id, role = role, box = new CoverBox { min = V(x0, y0, z0), max = V(x1, y1, z1) }, runnable = runnable };
                r.solids.Add(s);
                return s;
            }

            /// <summary>A floor slab 0.5 m thick with its top at <paramref name="y"/>.</summary>
            public void Floor(float x0, float z0, float x1, float z1, float y = 0f) => Add(Id("floor"), "floor", x0, y - 0.5f, z0, x1, y, z1);

            /// <summary>A raised block you stand on (a ledge, a balcony).</summary>
            public void Deck(float x0, float z0, float x1, float z1, float yBottom, float top) => Add(Id("deck"), "deck", x0, yBottom, z0, x1, top, z1);

            public void Wall(float x0, float z0, float x1, float z1, float yb, float yt, bool runnable, string prefix = "wall") =>
                Add(Id(prefix), "wall", x0, yb, z0, x1, yt, z1, runnable);

            /// <summary>Low cover (1.2 m), standing on <paramref name="y"/>.</summary>
            public void Cover(float x0, float z0, float x1, float z1, float y = 0f) => Add(Id("cover"), "cover", x0, y, z0, x1, y + 1.2f, z1);

            public void Ramp(float x0, float z0, float x1, float z1, float yBottom, float topStart, float topEnd, bool alongX)
            {
                var s = Add(Id("ramp"), "ramp", x0, yBottom, z0, x1, Math.Max(topStart, topEnd), z1);
                s.ramp = true;
                s.rampAlongX = alongX;
                s.rampTopStart = topStart;
                s.rampTopEnd = topEnd;
            }

            public void Gate(string id, float x0, float z0, float x1, float z1, float yb, float yt) => Add(id, "gate", x0, yb, z0, x1, yt, z1);

            /// <summary>A lava well's stone body (not walkable; the builder caps it with glowing lava).</summary>
            public void Well(float x0, float z0, float x1, float z1) => Add(Id("well"), "well", x0, LavaSurface - LavaDepth, z0, x1, 0.8f, z1);

            public void Lava(float x0, float z0, float x1, float z1)
            {
                r.hazards.Add(new HellHazard
                {
                    id = Id("lava"), kind = HellHazardKind.Lava,
                    box = new CoverBox { min = V(x0, LavaSurface - LavaDepth, z0), max = V(x1, LavaSurface, z1) },
                });
            }

            public void Collapse(float x0, float z0, float x1, float z1, float top)
            {
                r.hazards.Add(new HellHazard
                {
                    id = Id("collapse"), kind = HellHazardKind.Collapse,
                    box = new CoverBox { min = V(x0, top - PlatformThickness, z0), max = V(x1, top, z1) },
                });
            }

            /// <summary>A floor tile of The Collapse, falling on the room clock.</summary>
            public void Tile(float x0, float z0, float x1, float z1, bool safeLane)
            {
                r.hazards.Add(new HellHazard
                {
                    id = Id(safeLane ? "tile-safe" : "tile-fast"), kind = HellHazardKind.Collapse,
                    box = new CoverBox { min = V(x0, -PlatformThickness, z0), max = V(x1, 0f, z1) },
                    timelineSeconds = CollapseTileSeconds(z0, safeLane),
                });
            }

            public void Beam(float x0, float z0, float x1, float z1, float groundY, float on, float off, float phase)
            {
                r.hazards.Add(new HellHazard
                {
                    id = Id("beam"), kind = HellHazardKind.Beam,
                    box = new CoverBox { min = V(x0, groundY + BeamHeight - 0.1f, z0), max = V(x1, groundY + BeamHeight + 0.1f, z1) },
                    onSeconds = on, offSeconds = off, phase = phase,
                });
            }

            public void Dark(float x0, float y0, float z0, float x1, float y1, float z1) =>
                r.darkZones.Add(new CoverBox { min = V(x0, y0, z0), max = V(x1, y1, z1) });

            /// <summary>Finishes the room: the fire lights over its lava (every <see cref="LightSpacing"/> m, none reaching a dark zone).</summary>
            public HellRoomDef Done()
            {
                foreach (var h in r.hazards)
                {
                    if (h.kind != HellHazardKind.Lava) continue;
                    float sx = h.box.max.X - h.box.min.X, sz = h.box.max.Z - h.box.min.Z;
                    int nx = Math.Max(1, (int)Math.Round(sx / LightSpacing)), nz = Math.Max(1, (int)Math.Round(sz / LightSpacing));
                    for (int i = 0; i < nx; i++)
                        for (int j = 0; j < nz; j++)
                        {
                            var p = V(h.box.min.X + sx * (i + 0.5f) / nx, LavaSurface + 0.8f, h.box.min.Z + sz * (j + 0.5f) / nz);
                            float radius = LightRadius;
                            foreach (var d in r.darkZones) radius = Math.Min(radius, HellRoomLint.DistanceToBox(p, d) - LightClearance);
                            if (radius >= MinLightRadius) r.lights.Add(new HellLight { position = p, radius = radius });
                        }
                }
                return r;
            }

            public void Stop(string id, Vector3 p, float reach) => r.stops.Add(new HellStop { id = id, position = p, reach = reach });

            public void Spawn(HellEnemyRole role, Vector3 p, float yaw = 180f, int wave = 0, int minFloor = 1) =>
                r.spawns.Add(new HellSpawn { role = role, position = p, yawDeg = yaw, wave = wave, minFloor = minFloor });

            /// <summary>The room's shell: the entry vestibule (z -8..0, 6 m wide, the player at its middle), the outer walls from
            /// <paramref name="baseY"/> to <paramref name="topY"/> with a doorway south (6 m) and north (4 m, at
            /// <paramref name="exitX"/>, floor <paramref name="exitY"/>) closed by the exit gate, the exit corridor and its drop hole.</summary>
            public void Shell(float minX, float maxX, float length, float baseY, float topY, float exitX, float exitY)
            {
                const float t = 0.6f;
                // the vestibule
                Floor(-3f, -8f, 3f, 0f);
                Wall(-3f - t, -8f, -3f, 0f, 0f, 5f, false, "vestibule");
                Wall(3f, -8f, 3f + t, 0f, 0f, 5f, false, "vestibule");
                Wall(-3f - t, -8f - t, 3f + t, -8f, 0f, 5f, false, "vestibule");
                // the outer walls
                Wall(minX - t, -t, -3f, 0f, baseY, topY, true);
                Wall(3f, -t, maxX + t, 0f, baseY, topY, true);
                Wall(minX - t, 0f, minX, length, baseY, topY, true);
                Wall(maxX, 0f, maxX + t, length, baseY, topY, true);
                Wall(minX - t, length, exitX - 2f, length + t, baseY, topY, true);
                Wall(exitX + 2f, length, maxX + t, length + t, baseY, topY, true);
                Wall(exitX - 2f, length, exitX + 2f, length + t, exitY + 4f, topY, false, "lintel");
                Gate("exit-gate", exitX - 2f, length, exitX + 2f, length + t, exitY, exitY + 4f);
                r.exitGateId = "exit-gate";
                // the exit corridor, its drop hole and the pit under it
                float c0 = length, c1 = length + CorridorLength, h1 = c1 + HoleLength;
                Floor(exitX - 2f, c0, exitX + 2f, c1, exitY);
                Wall(exitX - 2f - t, c0 + t, exitX - 2f, h1, exitY - DropDepth, exitY + 4f, false, "corridor");
                Wall(exitX + 2f, c0 + t, exitX + 2f + t, h1, exitY - DropDepth, exitY + 4f, false, "corridor");
                Wall(exitX - 2f - t, h1, exitX + 2f + t, h1 + t, exitY - DropDepth, exitY + 4f, false, "corridor");
                Wall(exitX - 2f, c1 - t, exitX + 2f, c1, exitY - DropDepth, exitY - 0.5f, false, "corridor");
                Floor(exitX - 2f, c1, exitX + 2f, h1, exitY - DropDepth);
                r.dropZone = new CoverBox { min = V(exitX - 2f, exitY - DropDepth, c1), max = V(exitX + 2f, exitY - 4f, h1) };
                r.entry = V(0, 0, -4f);
                r.entryYaw = 0f;
                r.exit = V(exitX, exitY, length);
                r.exitYaw = 0f;
                r.bounds = new CoverBox { min = V(minX - t, Math.Min(baseY, exitY - DropDepth), -8f - t), max = V(maxX + t, topY, h1 + t) };
            }
        }
    }
}
