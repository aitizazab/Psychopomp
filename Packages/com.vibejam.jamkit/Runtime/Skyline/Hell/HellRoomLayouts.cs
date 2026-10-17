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
        /// <summary>A lava flare's time up (s): a moment (docs/HELL-SHADOW.md).</summary>
        public const float FlareSeconds = 1.2f;
        /// <summary>The exit corridor's length before its drop hole, the hole's length, and how far the drop falls.</summary>
        public const float CorridorLength = 8f, HoleLength = 4f, DropDepth = 12f;
        /// <summary>The open rim (docs/HELL-ROOMS.md, "Open edges"): a parapet's top, how near the safe path brings one, the tall
        /// wall kept at a job pocket's door and behind a chain anchor.</summary>
        public const float ParapetHeight = 1.0f, RimGuard = 3.5f, PocketWall = 24f, AnchorWall = 8f;
        /// <summary>The lava sea far below the rooms (the builder's abyss under the open rims).</summary>
        public const float AbyssSeaY = -40f;

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
        /// edge); the sigils stand far apart (legs of 30 to 47 m) on islands, a pillar, a sunken pit and a high ledge, joined to
        /// the ring and to stone walkways kept off the fast lines (the safe path, 414 m; docs/HELL-ROOMS.md). Every straight
        /// line is blocked by something that needs a movement tool, a different one each leg:
        /// <list type="bullet">
        /// <item>A (island, y 0) to B (island, y 3): a lava gulf with a 9 m runnable wall standing in it: wall-run off its end onto B's landing.</item>
        /// <item>B to C (pillar top, y 6): a rising chain of five collapsing platforms (4 m).</item>
        /// <item>C to D (island, y 0): a 40 m gulf: jump down onto a dark perch, one Shadow Step to another, a hop.</item>
        /// <item>D to E (pit floor, y -2): a platform with a beam lane across it (slide), then a drop into the pit.</item>
        /// <item>E to F (ledge, y 4): a tall runnable wall in the pit: wall-run, wall-jump onto a low block, up to the ledge.</item>
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
            // A-B: the wall-run wall (9 m, so a wall-run sinking from a double jump's height leaves it above the landing), running off its end onto B's low landing (a hop and a
            // mantle up onto B). Simulated: HellFastLines "A-B wall-run onto B".
            k.Wall(-12, 43, -3, 43.6f, LavaSurface - LavaDepth, 7f, true, "wallrun");
            k.Deck(-3, 41, 20, 48, LavaSurface - LavaDepth, 0.5f);
            // B: a raised island, a ramp from the east ring
            k.Deck(12, 48, 20, 56, LavaSurface - LavaDepth, 3f);
            k.Ramp(20, 50, Ring, 54, LavaSurface - LavaDepth, 3f, 0f, true);
            // B-C: the rising collapse chain
            for (int i = 1; i <= 5; i++)
            {
                float t = i / 6f;
                float cx = 12f + (-12f - 12f) * t, cz = 56f + (76f - 56f) * t;
                k.Collapse(cx - 2f, cz - 2f, cx + 2f, cz + 2f, 3f + 0.5f * i);
            }
            // C: a pillar, a ramp from the west ring
            k.Deck(-20, 76, -12, 84, LavaSurface - LavaDepth, 6f);
            k.Ramp(-Ring, 78, -20, 82, LavaSurface - LavaDepth, 0f, 6f, true);
            // C-D: two dark perches: jump down onto the first, Shadow Step to the second (keeping your speed), hop to D
            Pillar(k, -6.25f, 86.75f, -1.75f, 91.25f, 1.5f);
            Pillar(k, 5f, 97f, 9f, 101f, 1f);
            // D: an island, bridged from the east ring
            k.Deck(12, 100, 20, 108, LavaSurface - LavaDepth, 0f);
            k.Floor(20, 102, Ring, 106);
            // D-E: the beam platform
            k.Deck(2, 110, 20, 120, LavaSurface - LavaDepth, 0f);
            k.Beam(2, 116.8f, 20, 117.2f, 0f, 2.2f, 0.8f, 0f);
            // E: the pit (floor y -2), walled, a ramp up to the north terrace
            k.Floor(-10, 128, 6, 144, -2f);
            k.Ramp(-10, 144, 6, 160, -2.5f, -2f, 0f, false);
            k.Wall(-10.6f, 127.4f, -10f, 160, -2.5f, 0.5f, false, "pit");
            k.Wall(6f, 127.4f, 6.6f, 130, -2.5f, 0.5f, false, "pit");
            k.Wall(6f, 133, 6.6f, 160, -2.5f, 0.5f, false, "pit");
            k.Wall(-10f, 127.4f, 6f, 128, -2.5f, 0.5f, false, "pit");
            // E-F: a tall runnable wall in the pit: run its west face north, wall-jump west over the pit wall onto a low block
            // in the lava, then jump and mantle up onto F's ledge
            k.Wall(-4f, 138, -3.4f, 150, -2.5f, 9f, true, "wallrun");
            k.Deck(-14f, 146, -10.6f, 160, LavaSurface - LavaDepth, 0.5f);
            // F: the high ledge, a ramp up from the north terrace
            k.Deck(-Ring, 148, -14, 166, LavaSurface - LavaDepth, 4f);
            k.Ramp(-14, 162, 4, 166, 0f, 4f, 0f, true);
            k.Dark(-18, 4f, 148, -14, 7f, 152);
            // the safe crossings (2026-10-17, the safe path was 931 m): stone walkways in the lava, kept off the fast lines
            k.Floor(-20, 22, -16, 30);                       // the start terrace to A
            k.Floor(-16, 26, 23, 29);                        // the south walkway east...
            k.Floor(18, 29, 21, 41);                         // ...north onto B's landing...
            k.Ramp(14, 44, 18, 48, 0f, 0.5f, 3f, false);      // ...and a ramp up onto B
            k.Ramp(14, 56, 18, 62, 0f, 3f, 0f, false);        // B's north ramp down to the walkway to C
            k.Floor(14, 62, 18, 78.5f);
            k.Floor(-2, 78.5f, 25, 81.5f);                   // the crossing in front of C, and C's east ramp
            k.Ramp(-12, 78.5f, -2, 81.5f, LavaSurface - LavaDepth, 6f, 0f, true);
            k.Floor(22, 81.5f, 25, 130);                     // the east spine, past D's bridge to the pit
            k.Floor(6.6f, 130, 25, 133);                     // the bridge into the pit (through a gap in its east wall)
            k.Ramp(3, 130, 6.6f, 133, -2.5f, -2f, 0f, true);
            // cover on the ring's lava edge, gaps at the bridges and ramps
            for (float z = 26; z < 156; z += 14f)
            {
                if (!Overlaps(z, z + 4, 31, 37) && !Overlaps(z, z + 4, 77, 83) && !Overlaps(z, z + 4, 147, 167)) k.Cover(-Ring - 0.6f, z, -Ring, z + 4);
                if (!Overlaps(z, z + 4, 49, 55) && !Overlaps(z, z + 4, 101, 107)) k.Cover(Ring, z, Ring + 0.6f, z + 4);
            }

            k.Stop("sigil-A", V(-18, 0, 34), 2f);
            k.Stop("sigil-B", V(16, 3, 52), 2f);
            k.Stop("sigil-C", V(-16, 6, 80), 2f);
            k.Stop("sigil-D", V(16, 0, 104), 2f);
            k.Stop("sigil-E", V(2, -2, 132), 2f);
            k.Stop("sigil-F", V(-22, 4, 154), 2f);

            V3(k.r.safePath, k.r.entry, 0, 0, 0, -18, 0, 21, -18, 0, 33,                                     // A
                -18, 0, 27.5f, 19.5f, 0, 27.5f, 19.5f, 0, 40.8f, 19.5f, 0.5f, 41.4f, 16, 0.5f, 44.2f, 16, 3, 48, 16, 3, 51, // B
                16, 3, 56, 16, 0, 62, 16, 0, 80, -2, 0, 80, -12, 6, 80, -14.5f, 6, 80,                     // C
                -12, 6, 80, -2, 0, 80, 23.5f, 0, 80, 23.5f, 0, 104, 17.5f, 0, 104,                          // D
                23.5f, 0, 104, 23.5f, 0, 131.5f, 6.6f, 0, 131.5f, 3, -2, 131.5f,                            // E
                -2, -2, 144, -2, 0, 160, -2, 0, 161, 5, 0, 161, 5, 0, 164, -14, 4, 164, -20, 4, 160, -22, 4, 155.5f, // F
                -20, 4, 160, -14, 4, 164, 5, 0, 164, 5, 0, 168, 0, 0, 168);
            k.r.safePath.Add(k.r.exit);
            // the fast line, leg by leg as simulated (HellFastLines.Sigils)
            V3(k.r.fastPath, k.r.entry, -14, 0, 22, -18, 0, 34,                                         // A
                -14.7f, 0, 37.4f, -10.7f, 2, 41.8f, -3, 0.5f, 42.4f, 10, 0.5f, 45.5f, 16, 3, 52,           // B: the wall-run
                8, 3.5f, 59.3f, 4, 4, 62.7f, 0, 4.5f, 66, -4, 5, 69.3f, -8, 5.5f, 72.7f, -16, 6, 80,       // C: the collapse chain
                -12.6f, 6, 83, -4, 1.5f, 89, 7, 1, 99, 16, 0, 104,                                          // D: perch, Shadow Step, hop
                13, 0, 110.5f, 9, 0, 117, 5, -2, 128, 2, -2, 132,                                           // E: the beam slide, the pit
                -7, -2, 134.5f, -4.4f, -1, 140, -4.4f, -0.5f, 143, -12, 0.5f, 152, -14, 4, 154, -22, 4, 154, // F: the wall-run out
                -20, 4, 165.9f, -14, 0, 168);
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
            Pillar(k, -4.5f, 29f, 0.5f, 35f, 1f);     // where the west wall's wall jump lands (simulated)
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

        /// <summary>The brand's centre (the ring's middle, over the lava pool) and the racing line's half size and corner cut.</summary>
        public static readonly Vector3 BrandCentre = new Vector3(0f, 0f, 38f);
        public const float BrandLoopHalf = 8f, BrandLoopCut = 4.5f;

        /// <summary>The brand: a ring 20 m across (<see cref="HellBrandTuning.RingRadius"/>) on an island in a lava lake, round a
        /// lava pool. Inside the ring it charges faster the faster you move (<see cref="HellBrandTuning"/>): stand behind a stone
        /// and it takes 30 s, run laps round the pool (a racetrack with a wall-run wall on each long side) and it takes 10, while
        /// the waves come every 5 s from the first touch. Fast in: collapsing stepping stones straight over the lake (or two dark
        /// pillars for the Shadow Step); safe in: the west walkway and the open bridge, then a stone to stand behind.</summary>
        static HellRoomDef HoldBrand()
        {
            var k = new Kit(HellRoomKind.HoldBrand, "Hold the Brand");
            const float W = 26f, L = 70f;
            var c = BrandCentre;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 10);
            k.Floor(-W, 10, -20, 56);               // the west walkway
            // the island: a 24 m square track round the 8 m lava pool
            k.Floor(-12, 26, 12, 34);
            k.Floor(-12, 42, 12, 50);
            k.Floor(-12, 34, -4, 42);
            k.Floor(4, 34, 12, 42);
            k.Floor(-20, 44, -12, 48);              // the open bridge from the walkway
            k.Floor(-2, 50, 2, 56);                 // the north bridge
            k.Floor(-W, 56, W, L);
            k.Lava(-4, 34, 4, 42);                  // the pool inside the ring
            k.Lava(-20, 10, W, 26);                 // the lake: entry to island
            k.Lava(-20, 26, -12, 44);
            k.Lava(-20, 48, -12, 56);
            k.Lava(-12, 50, -2, 56);
            k.Lava(2, 50, W, 56);
            k.Lava(12, 26, W, 50);
            // inside the ring: a wall-run wall on each long side of the track (the racing line's fast cue) and three stones
            k.Wall(9.4f, 34f, 10f, 42f, 0f, 3.5f, true, "wallrun");
            k.Wall(-10f, 34f, -9.4f, 42f, 0f, 3.5f, true, "wallrun");
            k.Cover(-1.5f, c.Z - 10f, 1.5f, c.Z - 9.2f);            // south, on the ring's edge
            k.Cover(4.6f, c.Z + 4.6f, 5.8f, c.Z + 5.8f);            // the pool's north-east corner
            k.Cover(-5.8f, c.Z - 5.8f, -4.6f, c.Z - 4.6f);          // the pool's south-west corner
            // the fast way in: stepping stones over the lake, or two dark pillars
            foreach (var z in new[] { 11f, 15.5f, 20f }) k.Collapse(-1.5f, z, 1.5f, z + 3f, 0f);
            Pillar(k, 8, 13, 11, 16, 0.5f);
            Pillar(k, 7, 19, 10, 22, 0.5f);
            k.Stop("brand", c, HellBrandTuning.RingRadius);
            k.r.hold = new HellHoldRing { center = c, radius = HellBrandTuning.RingRadius, halfHeight = HellBrandTuning.RingHalfHeight };
            float h = BrandLoopHalf, q = BrandLoopCut;
            foreach (var p in new[] { V(h, 0, -q), V(h, 0, q), V(q, 0, h), V(-q, 0, h), V(-h, 0, q), V(-h, 0, -q), V(-q, 0, -h), V(q, 0, -h), V(h, 0, -q) })
                k.r.hold.loop.Add(c + p);
            k.r.safePath.AddRange(new[]
            {
                k.r.entry, V(0, 0, 5), V(-23, 0, 8), V(-23, 0, 46), V(-12, 0, 46), V(-7, 0, 44),
                V(-6.6f, 0, 33), // behind the south-west stone, inside the ring: the 30 s charge
                V(-7, 0, 44), V(0, 0, 48), V(0, 0, 56), k.r.exit,
            });
            k.r.fastPath.AddRange(new[] { k.r.entry, V(0, 0, 30), V(8, 0, 34), V(8, 0, 42), V(4.5f, 0, 46.5f), V(0, 0, 50), k.r.exit });
            k.Spawn(HellEnemyRole.Imp, V(12, 6, 20));
            k.Spawn(HellEnemyRole.Imp, V(16, 5, 38), wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(-16, 6, 30), wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(0, 6, 53), wave: 2);
            k.Spawn(HellEnemyRole.Scythe, V(8, 0, 62), 180f, wave: 2, minFloor: 2);
            k.Spawn(HellEnemyRole.Imp, V(12, 5, 22), wave: 3);
            k.Spawn(HellEnemyRole.Scythe, V(-8, 0, 62), 180f, wave: 3, minFloor: 2);
            k.Spawn(HellEnemyRole.Ooze, V(6, 0, 46), wave: 3, minFloor: 3);
            k.r.tuning.radius = HellBrandTuning.RingRadius;
            k.r.tuning.seconds = HellBrandTuning.WaveEvery;     // a wave every 5 s once the brand is first touched
            k.r.collapseReturnSeconds = 5f;
            return k.Done();
        }

        // ------------------------------------------------------------------ 5. The Collapse

        /// <summary>The run-up hall's hanging stalactites (the speed route: wall-kick from one to the next): each is a thin
        /// rock slab along z, its face this far either side of the centre line, hanging from the hall's rock roof down to
        /// <see cref="CollapseStalactiteBottom"/> (a runner, 1.7 m, passes below), the first starting at
        /// <see cref="CollapseStalactiteFirstZ"/>, one every <see cref="CollapseStalactiteSpacing"/> m, alternating sides.</summary>
        public const float CollapseStalactiteHalfGap = 1.5f, CollapseStalactiteBottom = 2.1f, CollapseStalactiteLength = 7.5f;
        public const float CollapseStalactiteFirstZ = 30f, CollapseStalactiteSpacing = 8f, CollapseStalactiteStagger = 2f, CollapseRoofY = 12f;
        public const int CollapseStalactiteCount = 2;   // per side
        /// <summary>The safe lane's fins on the bridge: their z (the near face), the gap each leaves, and the running line
        /// through the gaps (alternating east and west).</summary>
        public const float CollapseBaffleFirstZ = 60f, CollapseBaffleSpacing = 9f;
        public const int CollapseBaffleCount = 6;
        public const float CollapseSafeLineEast = -9.5f, CollapseSafeLineWest = -18.5f;
        /// <summary>The fast lane's lava gap on the bridge (z range): a 4 m jump.</summary>
        public static readonly float[][] CollapseFastGaps = { new[] { 76f, 80f } };

        /// <summary>
        /// Reach the exit before the bridge falls away behind you (the creator, 2026-10-18: "only half of the room"). The first
        /// half is a stone run-up hall with two ways through, the same length: hanging stalactites over its middle that you can
        /// wall-kick between (each kick adds speed along the wall and keeps the entry speed: MoveDef.wallSpeedGrace and
        /// wallJumpAlong), or just run below them (no speed gained). The second half is the collapsing bridge, its tiles falling
        /// on the room clock row by row from the hall's end: the central lane is straight (one 4 m lava gap) and its front comes
        /// at 10.5 m/s soon after you could arrive, so only the kick route's speed outruns it; the west lane winds between stone
        /// fins (standing on the lava bed) and its front comes at 5.5 m/s along the room: survivable at a sprint, with no room
        /// for a fumble. Debris falls on both lanes on a seeded timer (its shadow glows 1 s before), and imps come at the walls
        /// every few seconds. The bridge's lava has no floor: a fallen tile drops you in (a sink restarts the room).
        /// </summary>
        static HellRoomDef Collapse()
        {
            var k = new Kit(HellRoomKind.Collapse, "The Collapse");
            const float W = 20f, L = 120f;
            k.Shell(-W, W, L, -2.5f, WallHeight, 0f, 0f);
            k.Floor(-W, 0, W, 8);
            // the run-up hall (stone, never falls): the stalactites hang over its middle from a rock roof
            k.Floor(-W, 8, W, CollapseStartZ);
            float roofZ0 = CollapseStalactiteFirstZ - 2f, roofZ1 = CollapseStalactiteFirstZ + CollapseStalactiteStagger + CollapseStalactiteSpacing * (CollapseStalactiteCount - 1) + CollapseStalactiteLength + 2f;
            k.Add("roof", "wall", -6f, CollapseRoofY, roofZ0, 6f, CollapseRoofY + 1.5f, roofZ1);
            for (int i = 0; i < CollapseStalactiteCount; i++)
            {
                for (int side = 0; side < 2; side++)
                {
                    float z = CollapseStalactiteFirstZ + CollapseStalactiteSpacing * i + side * CollapseStalactiteStagger;
                    float x = side == 0 ? CollapseStalactiteHalfGap : -CollapseStalactiteHalfGap - 0.6f;
                    k.Wall(x, z, x + 0.6f, z + CollapseStalactiteLength, CollapseStalactiteBottom, CollapseRoofY, true, "stalactite");
                }
            }
            k.Dark(-6f, 0f, roofZ0, 6f, CollapseStalactiteBottom, roofZ1);   // the hall's shade under the roof
            // the bridge: lava under it all (no floor: a fallen tile drops you in), the end floor beyond
            k.Floor(-W, CollapseEndZ, W, L);
            k.Lava(-W, CollapseStartZ, W, CollapseEndZ);
            k.r.lavaFloor = false;
            k.r.lavaSinkSeconds = 0.25f;
            // the central lane's tiles (the gap at 76-80)
            float[][] segs = { new[] { CollapseStartZ, CollapseFastGaps[0][0] }, new[] { CollapseFastGaps[0][1], CollapseEndZ } };
            foreach (var s in segs)
                for (float z = s[0]; z < s[1] - 0.01f; z += 4f)
                    k.Tile(-3, z, 3, Math.Min(z + 4f, s[1]), false);
            // the west lane's tiles (3 m rows) and its winding fins (on the lava bed: they stand when their tiles fall)
            for (float z = CollapseStartZ; z < CollapseEndZ - 0.01f; z += 3f) k.Tile(-W, z, -8, Math.Min(z + 3f, CollapseEndZ), true);
            var safe = new List<Vector3> { k.r.entry, V(0, 0, 2), V(CollapseSafeLineEast, 0, 10), V(CollapseSafeLineEast, 0, CollapseStartZ) };
            for (int i = 0; i < CollapseBaffleCount; i++)
            {
                float z = CollapseBaffleFirstZ + CollapseBaffleSpacing * i;
                bool even = i % 2 == 0;
                if (even) k.Wall(-W, z, -11f, z + 0.6f, LavaSurface - LavaDepth, 3f, false, "baffle");
                else k.Wall(-17f, z, -8f, z + 0.6f, LavaSurface - LavaDepth, 3f, false, "baffle");
                safe.Add(V(even ? CollapseSafeLineEast : CollapseSafeLineWest, 0, z + 0.3f));
            }
            // off the tiles, the safe way rounds a wall on the end floor (the long way to the exit; the front no longer matters here)
            k.Wall(-16f, 114.5f, -3.5f, 115.1f, 0f, 3f, false, "end-wall");
            safe.Add(V(-18.5f, 0, 112.5f));
            safe.Add(V(-18.5f, 0, 117));
            safe.Add(V(-4, 0, 118.5f));
            safe.Add(k.r.exit);
            k.r.safePath.AddRange(safe);
            // the fast line: up the hall kicking between the stalactites (HellFastLines.Collapse), down the central lane
            k.r.fastPath.AddRange(new[] { k.r.entry, V(0.6f, 0, 26), V(0, 2, 40), V(0, 0, 56), V(0, 0, 78), k.r.exit });
            k.Stop("exit", V(0, 0, L - 2f), 2.5f);
            // the exit gate stays the shell's "exit-gate": HellRoom opens it when you reach the exit (rooms3 left it unnamed, so
            // nothing opened it and its collider shut the doorway to the drop hole: the creator's softlock, 2026-10-18)
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 40));
            k.Spawn(HellEnemyRole.Imp, V(8, 6, 66));
            k.Spawn(HellEnemyRole.Imp, V(-12, 6, 92), minFloor: 2);
            k.Spawn(HellEnemyRole.Scythe, V(0, 0, 116), 180f, minFloor: 2);
            k.Spawn(HellEnemyRole.Ooze, V(12, 0, 116), minFloor: 3);
            // imps at the walls on the clock (every CollapseWaveEvery s), over the bridge ahead of a runner on pace
            k.Spawn(HellEnemyRole.Imp, V(W - 1.5f, 5, 62), 270f, wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(-W + 1.5f, 5, 66), 90f, wave: 1);
            k.Spawn(HellEnemyRole.Imp, V(W - 1.5f, 5, 84), 270f, wave: 2);
            k.Spawn(HellEnemyRole.Imp, V(-W + 1.5f, 5, 88), 90f, wave: 2);
            k.Spawn(HellEnemyRole.Imp, V(W - 1.5f, 5, 104), 270f, wave: 3);
            k.Spawn(HellEnemyRole.Imp, V(-W + 1.5f, 5, 108), 90f, wave: 3);
            k.r.tuning.seconds = CollapseWaveEvery;
            k.r.collapseReturnSeconds = -1f;
            k.r.debris = CollapseDebris(k.r, 1UL, 1f);
            return k.Done();
        }

        /// <summary>The Collapse's numbers (docs/HELL-ROOMS.md, "Tuning"): each lane's front speed (m/s, along the room) and its
        /// delay (s): the front sets off from the bridge's start (<see cref="CollapseFrontOriginZ"/>, the hall's end) this long
        /// after the room starts, and a row starts to shake when the front reaches its near edge. The tiles run from
        /// <see cref="CollapseStartZ"/> to <see cref="CollapseEndZ"/>. The wall imps' wave interval.</summary>
        public const float CollapseFastLaneSpeed = 13f, CollapseFastDelay = 5.0f;
        public const float CollapseSafeLaneSpeed = 5.5f, CollapseSafeDelay = 7.3f;
        public const float CollapseStartZ = 56f, CollapseFrontOriginZ = CollapseStartZ, CollapseEndZ = 112f, CollapseWaveEvery = 3f;

        /// <summary>The Collapse's timeline: the room second a tile in a lane starts to shake, from its near edge.</summary>
        public static float CollapseTileSeconds(float nearZ, bool safeLane, float speedScale = 1f) =>
            CollapseTileSeconds(nearZ, safeLane ? CollapseSafeDelay : CollapseFastDelay, (safeLane ? CollapseSafeLaneSpeed : CollapseFastLaneSpeed) * speedScale);

        /// <summary>The same for any front: <paramref name="delay"/> s after the start from the entry, at <paramref name="speed"/>.</summary>
        public static float CollapseTileSeconds(float nearZ, float delay, float speed) =>
            delay + Math.Max(0f, nearZ - CollapseFrontOriginZ) / speed;

        /// <summary>The z a lane's front (the row starting to shake) has reached at room second <paramref name="t"/>.</summary>
        public static float CollapseFrontZ(float t, bool safeLane, float speedScale = 1f)
        {
            float v = (safeLane ? CollapseSafeLaneSpeed : CollapseFastLaneSpeed) * speedScale;
            return CollapseFrontOriginZ + Math.Max(0f, t - (safeLane ? CollapseSafeDelay : CollapseFastDelay)) * v;
        }

        /// <summary>
        /// The Collapse's falling debris, seeded (<see cref="HellDebris"/> has the numbers): in each lane a drop every
        /// <see cref="HellDebris.MinGap"/>..<see cref="HellDebris.MaxGap"/> s, landing <see cref="HellDebris.MinLead"/>..
        /// <see cref="HellDebris.MaxLead"/> m ahead of that lane's front (where a runner on pace is), on the lane's running line
        /// (the fast lane's middle, the safe path's line) give or take <see cref="HellDebris.SideJitter"/> m, so it forces a
        /// sidestep. Never on a lava gap or a fin. In the base frame (call before mirroring or turning the room).
        /// </summary>
        public static List<HellDebrisDrop> CollapseDebris(HellRoomDef r, ulong seed, float speedScale)
        {
            var all = new List<HellDebrisDrop>();
            var rng = new Rng(seed * 0x9E3779B97F4A7C15UL + 0xD1B54A32D192ED03UL);
            for (int lane = 0; lane < 2; lane++)
            {
                bool safeLane = lane == 1;
                float t = Math.Max(1.2f, (safeLane ? CollapseSafeDelay : CollapseFastDelay) - 3f);
                for (int guard = 0; guard < 64; guard++)
                {
                    t += HellDebris.MinGap + (HellDebris.MaxGap - HellDebris.MinGap) * rng.NextFloat();
                    float front = CollapseFrontZ(t, safeLane, speedScale);
                    if (front + HellDebris.MinLead > CollapseEndZ - 3f) break;
                    float z = front + HellDebris.MinLead + (HellDebris.MaxLead - HellDebris.MinLead) * rng.NextFloat();
                    float side = (rng.NextFloat() * 2f - 1f) * HellDebris.SideJitter;
                    if (z > CollapseEndZ - 2f) continue;
                    float x;
                    if (safeLane)
                    {
                        bool onFin = false;
                        for (int i = 0; i < CollapseBaffleCount; i++)
                        {
                            float bz = CollapseBaffleFirstZ + CollapseBaffleSpacing * i;
                            if (z > bz - HellDebris.Radius && z < bz + 0.6f + HellDebris.Radius) onFin = true;
                        }
                        if (onFin) continue;
                        x = LineXAt(r.safePath, z) + side;
                    }
                    else
                    {
                        bool onGap = false;
                        foreach (var g in CollapseFastGaps)
                            if (z > g[0] - HellDebris.Radius && z < g[1] + HellDebris.Radius) onGap = true;
                        if (onGap) continue;
                        x = side;
                    }
                    all.Add(new HellDebrisDrop { time = t, position = V(x, 0f, z), radius = HellDebris.Radius });
                }
            }
            all.Sort((a, b) => a.time.CompareTo(b.time));
            return all;
        }

        /// <summary>The x of a path (a polyline running north) where it crosses <paramref name="z"/>.</summary>
        public static float LineXAt(List<Vector3> path, float z)
        {
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                if ((z >= a.Z && z <= b.Z) && b.Z > a.Z) return a.X + (b.X - a.X) * (z - a.Z) / (b.Z - a.Z);
            }
            return path.Count > 0 ? path[path.Count - 1].X : 0f;
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
            k.Floor(-20, 22, -13, 31.5f);           // pier W1 (out to the row of stones)
            k.Floor(13, 22, 20, 31.5f);             // pier W3
            k.Floor(-4, 47.5f, 4, 56);              // pier W2 (out to the well)
            k.Lava(-20, 8, 20, 22);
            k.Lava(-13, 22, 13, 28);
            k.Lava(-20, 28, 20, 47.5f);
            k.Lava(-20, 47.5f, -4, 56);
            k.Lava(4, 47.5f, 20, 56);
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
            // stepping stones: entry to W1, W1 to W3 (north of the wells), W3 to W2; 4.5 m stones a sprint hop (6 m) apart
            // (simulated: HellFastLines.Wells)
            StonesAlong(k, V(-3, 0, 8), V(-13, 0, 22), 2, 2.25f);
            foreach (var x in new[] { -9f, -3f, 3f, 9f }) k.Collapse(x - 2.25f, 27.25f, x + 2.25f, 31.75f, 0f);
            StonesAlong(k, V(13, 0, 28), V(3, 0, 49), 3, 2.25f);
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

        /// <summary><paramref name="n"/> collapsing stones (<paramref name="half"/> x 2 metres square) evenly between a and b (exclusive).</summary>
        static void StonesAlong(Kit k, Vector3 a, Vector3 b, int n, float half = 1.5f)
        {
            for (int i = 1; i <= n; i++)
            {
                var c = Vector3.Lerp(a, b, i / (float)(n + 1));
                k.Collapse(c.X - half, c.Z - half, c.X + half, c.Z + half, 0f);
            }
        }

        // ------------------------------------------------------------------ the kit

        sealed class Kit
        {
            public readonly HellRoomDef r;
            readonly Dictionary<string, int> _n = new Dictionary<string, int>();
            bool _rim;
            float _minX, _maxX, _len, _baseY, _topY;

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

            /// <summary>Finishes the room: the fire lights over its lava (every <see cref="LightSpacing"/> m, none reaching a dark zone),
            /// and the lava flares (docs/HELL-SHADOW.md): a light held back by a dark zone off the fast line spurts out to its full
            /// radius for <see cref="FlareSeconds"/> every 6-8 s, lighting that dark for a moment.</summary>
            public HellRoomDef Done()
            {
                var onFast = new bool[r.darkZones.Count];
                for (int z = 0; z < onFast.Length; z++) onFast[z] = HellRoomLint.OnFastLine(r, r.darkZones[z]);
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
                            if (radius >= MinLightRadius) r.lights.Add(Flare(new HellLight { position = p, radius = radius }, onFast));
                        }
                }
                Rim();
                KeepAHidingPlace();
                return r;
            }

            /// <summary>
            /// The open rim (the creator, 2026-10-18: "random walls in an odd box shape that prevent the player from seeing
            /// outside"): the west, east and south sides are no longer 8-14 m walls. Along each, metre by metre: a low parapet
            /// (<see cref="ParapetHeight"/>) wherever the safe path comes within <see cref="RimGuard"/> m, so the safe way stays
            /// off the drop; a tall wall section only where something needs one (the job pocket's door, a chain anchor on the
            /// wall); elsewhere an open cliff edge, the cavern's sky and spires beyond (a fall is a sink: back to the last safe spot).
            /// </summary>
            void Rim()
            {
                if (!_rim) return;
                const float t = 0.6f;
                HellJobPockets.Site(r.kind, out int pocketSide, out float doorZ);
                float p0 = Math.Max(0f, Math.Min(_len - PocketWall, doorZ - PocketWall * 0.5f)), p1 = p0 + PocketWall;
                // the west and east sides (x fixed, along z), then the south side either side of the vestibule (along x)
                Side(_minX - t, _minX, 0f, _len, true, -1, p0, p1, pocketSide == -1);
                Side(_maxX, _maxX + t, 0f, _len, true, 1, p0, p1, pocketSide == 1);
                Side(_minX - t, -3f, -t, 0f, false, 0, 0f, 0f, false);
                Side(3f, _maxX + t, -t, 0f, false, 0, 0f, 0f, false);
            }

            /// <summary>One side of the rim: a box x0..x1 by z0..z1 cut into runs of tall wall, parapet and open edge.</summary>
            void Side(float x0, float x1, float z0, float z1, bool alongZ, int side, float pocket0, float pocket1, bool pocketHere)
            {
                float a0 = alongZ ? z0 : x0, a1 = alongZ ? z1 : x1;
                int n = Math.Max(1, (int)Math.Ceiling((a1 - a0) - 1e-3f));
                float step = (a1 - a0) / n;
                int run = -1;
                float runStart = a0;
                for (int i = 0; i <= n; i++)
                {
                    int kind = -1;
                    if (i < n)
                    {
                        float m = a0 + step * (i + 0.5f);
                        var at = alongZ ? V((x0 + x1) * 0.5f, 0f, m) : V(m, 0f, (z0 + z1) * 0.5f);
                        float pathY = NearPathY(at);
                        kind = Tall(at, alongZ, side, pocket0, pocket1, pocketHere) || pathY > 0.15f ? 2 : !float.IsNaN(pathY) ? 1 : 0;
                    }
                    if (i > 0 && kind == run) continue;
                    if (i > 0 && run > 0)
                    {
                        float b0 = runStart, b1 = a0 + step * i;
                        float bx0 = alongZ ? x0 : b0, bx1 = alongZ ? x1 : b1, bz0 = alongZ ? b0 : z0, bz1 = alongZ ? b1 : z1;
                        if (run == 2) Wall(bx0, bz0, bx1, bz1, _baseY, _topY, true);
                        else Add(Id("parapet"), "wall", bx0, _baseY, bz0, bx1, ParapetHeight, bz1);
                    }
                    run = kind;
                    runStart = a0 + step * i;
                }
            }

            /// <summary>A tall wall section stays: the job pocket's door (its frame needs the wall), a chain anchor on this wall.</summary>
            bool Tall(Vector3 at, bool alongZ, int side, float pocket0, float pocket1, bool pocketHere)
            {
                if (!alongZ) return false;
                if (pocketHere && at.Z >= pocket0 && at.Z <= pocket1) return true;
                foreach (var s in r.stops)
                    if (Math.Abs(s.position.X - at.X) < 1.5f && Math.Abs(s.position.Z - at.Z) <= AnchorWall * 0.5f && s.position.Y > 2f) return true;
                return false;
            }

            /// <summary>The highest the safe path is where it comes within <see cref="RimGuard"/> m of this rim point (NaN: it doesn't). A
            /// raised stretch (a ramp or balcony along the side) keeps a tall wall: a parapet only guards the ground.</summary>
            float NearPathY(Vector3 at)
            {
                float y = float.NaN;
                for (int i = 1; i < r.safePath.Count; i++)
                {
                    var a = r.safePath[i - 1];
                    var b = r.safePath[i];
                    int n = Math.Max(1, (int)(Vector3.Distance(a, b) / 0.5f));
                    for (int j = 0; j <= n; j++)
                    {
                        var q = Vector3.Lerp(a, b, j / (float)n);
                        float d = (float)Math.Sqrt((q.X - at.X) * (q.X - at.X) + (q.Z - at.Z) * (q.Z - at.Z));
                        if (d < RimGuard && (float.IsNaN(y) || q.Y > y)) y = q.Y;
                    }
                }
                return y;
            }

            /// <summary>The safe path comes within <see cref="RimGuard"/> m of this rim point.</summary>
            bool Guarded(Vector3 at) => HellRoomLint.FlatDistanceToPath(r.safePath, at) < RimGuard;

            /// <summary>The lint wants one dark zone no flare reaches: if every one is reached, the zone the fewest flares reach is
            /// kept clear (those flares are pulled back from it, or dropped).</summary>
            void KeepAHidingPlace()
            {
                if (r.darkZones.Count == 0) return;
                int best = -1, fewest = int.MaxValue;
                for (int z = 0; z < r.darkZones.Count; z++)
                {
                    int n = 0;
                    foreach (var l in r.lights)
                        if (l.Flares && HellRoomLint.DistanceToBox(l.position, r.darkZones[z]) < l.flareRadius) n++;
                    if (n < fewest) { fewest = n; best = z; }
                }
                if (fewest == 0) return;
                var keep = r.darkZones[best];
                for (int i = 0; i < r.lights.Count; i++)
                {
                    var l = r.lights[i];
                    if (!l.Flares) continue;
                    float d = HellRoomLint.DistanceToBox(l.position, keep);
                    if (d >= l.flareRadius) continue;
                    l.flareRadius = d - LightClearance;
                    if (l.flareRadius < l.radius + 1f) l.flareRadius = l.flarePeriod = l.flareSeconds = l.flarePhase = 0f;
                    r.lights[i] = l;
                }
            }

            /// <summary>A lava flare for light <paramref name="l"/>: out to <see cref="LightRadius"/>, but never into a dark zone on
            /// the fast line (a Shadow Step landing stays dependable); only if it then reaches some other dark zone.</summary>
            HellLight Flare(HellLight l, bool[] onFast)
            {
                float flare = LightRadius;
                for (int z = 0; z < r.darkZones.Count; z++)
                    if (onFast[z]) flare = Math.Min(flare, HellRoomLint.DistanceToBox(l.position, r.darkZones[z]) - LightClearance);
                bool reaches = false;
                for (int z = 0; z < r.darkZones.Count; z++)
                    if (!onFast[z] && HellRoomLint.DistanceToBox(l.position, r.darkZones[z]) < flare) reaches = true;
                if (!reaches || flare < l.radius + 1f) return l;
                // a seeded phase and period from the position, so neighbouring flares don't spurt together
                float h = Math.Abs(l.position.X * 0.371f + l.position.Z * 0.613f);
                h -= (float)Math.Floor(h);
                l.flareRadius = flare;
                l.flarePeriod = 6f + 2f * h;
                l.flareSeconds = FlareSeconds;
                l.flarePhase = h;
                return l;
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
                // the outer walls: the north wall (the exit's gatehouse) stays tall; the west, east and south sides are an open
                // rim (docs/HELL-ROOMS.md, "Open edges"), built by Rim() once the paths are known
                _rim = true; _minX = minX; _maxX = maxX; _len = length; _baseY = baseY; _topY = topY;
                r.rim = new CoverBox { min = V(minX, baseY, 0f), max = V(maxX, topY, length) };
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
