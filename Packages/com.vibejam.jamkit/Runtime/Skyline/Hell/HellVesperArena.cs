using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// Vesper's arena, the Evening Star's Hall, at the bottom of floor 3 (docs/HELL-BOSS.md, "The arena"), as a
    /// <see cref="HellRoomDef"/> in the same style as the six rooms (its own file: <see cref="HellRoomLayouts"/> belongs to the
    /// rooms lane). 52 x 64 m, walls 14 m high. A lava pit fills the middle (28 x 32 m) with her dais standing in it; stone
    /// walkways run round it with low cover on their lava edge; the east gallery (5 m up, a ramp from its north end) and the
    /// throne balcony over the exit (4 m up, a ramp from the east) are the high ground; four dark pillars stand in the lava.
    /// Her darkness: the dais, the pillars, a gallery alcove and the throne balcony are dark zones; she Shadow Steps between
    /// them (<see cref="Perches"/>) and so can you. The room rule is adapted for a fight (the lint passes): the stops are the
    /// high ground (the gallery, then the balcony with the exit door), the safe path walks round to them, the fast path is the
    /// Shadow Step line over the lava.
    /// <para>Its world slot: floor 3's row, the column after the shaft (<see cref="Offset"/>): a slot of
    /// <see cref="HellWorld"/>'s grid kept in this file so <c>HellWorld.cs</c> stays untouched.</para>
    /// </summary>
    public static class HellVesperArena
    {
        public const float HalfWidth = 26f, Length = 64f, WallTop = 14f;
        /// <summary>The lava pit (x and z extents) and the dais in it.</summary>
        public const float PitHalf = 14f, PitZ0 = 16f, PitZ1 = 48f;
        public const float DaisTop = 2f;
        /// <summary>The floor (0-based) the arena belongs to.</summary>
        public const int Floor = 2;

        /// <summary>The arena's world offset: floor 3's row (z), one column past the floor's shaft (x).</summary>
        public static Vector3 Offset =>
            new Vector3((HellRoomLayouts.Kinds.Length + 1) * HellWorld.SlotSpacing, 0f, Floor * HellWorld.FloorSpacing);

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>The arena in world space (floor 3's lava numbers).</summary>
        public static HellRoomDef World() => new HellRoomTransform { offset = Offset }.Room(Build());

        /// <summary>Her perches (feet positions on top of each dark zone's floor), in the def's own space: perch 0 is the dais.</summary>
        public static List<Vector3> Perches(HellRoomDef d)
        {
            var list = new List<Vector3>();
            foreach (var z in d.darkZones) list.Add(new Vector3((z.min.X + z.max.X) * 0.5f, z.min.Y, (z.min.Z + z.max.Z) * 0.5f));
            return list;
        }

        /// <summary>The arena in its own frame (entry vestibule south at z &lt; 0, the exit door north over the balcony).</summary>
        public static HellRoomDef Build()
        {
            var b = new Builder("The Evening Star's Hall");
            var r = b.r;
            const float W = HalfWidth, L = Length, bottom = HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth;
            b.Shell(W, L, -3f, WallTop, 0f, 4f);
            // walkways round the pit
            b.Floor(-W, 0, W, PitZ0);
            b.Floor(-W, PitZ0, -PitHalf, PitZ1);
            b.Floor(PitHalf, PitZ0, W, PitZ1);
            b.Floor(-W, PitZ1, W, L);
            b.Lava(-PitHalf, PitZ0, PitHalf, PitZ1);

            // her dark places, in order: the dais (perch 0), four pillars, the gallery alcove, the throne balcony
            b.Deck(-4, 28, 4, 36, bottom, DaisTop);
            b.Dark(-4, DaisTop, 28, 4, DaisTop + 3.5f, 36);
            Pillar(b, -10.25f, 20.75f, -7.75f, 23.25f, 1f);
            Pillar(b, 7.75f, 20.75f, 10.25f, 23.25f, 1f);
            Pillar(b, -10.25f, 40.75f, -7.75f, 43.25f, 1f);
            Pillar(b, 9.75f, 38.75f, 12.25f, 41.25f, 1f);
            // the east gallery (high ground, 5 m), its ramp from the north walkway, its dark alcove
            b.Deck(20, 22, W, 42, 0f, 5f);
            b.Ramp(20, 42, W, 52, 0f, 5f, 0f, false);
            b.Dark(22, 5f, 23, W, 8.5f, 28);
            // the throne balcony over the exit (4 m), its ramp from the east, its darkness (the throne)
            b.Deck(-10, 56, 10, L, 0f, 4f);
            b.Ramp(10, 56, 16, L, 0f, 4f, 0f, true);
            b.Dark(-9, 4f, 58, -3, 7.5f, L - 0.5f);
            // low cover on the walkways' lava edge (gaps where the ramps and the gallery meet the pit)
            for (float z = 18; z < 46; z += 7f)
            {
                b.Cover(-PitHalf - 0.6f, z, -PitHalf, z + 3f);
                if (z + 3f < 22f || z > 42f) b.Cover(PitHalf, z, PitHalf + 0.6f, z + 3f);
            }
            b.Cover(-9, 10, -5, 10.6f);
            b.Cover(5, 10, 9, 10.6f);
            b.Cover(-6, 51, -2, 51.6f);
            // tall standing stones on the walkways (cover from her bolts, and a wall to run)
            b.Wall(-24, 24, -23.4f, 34, 0f, 6f, true, "wallrun");
            b.Wall(-24, 4, -18, 4.6f, 0f, 3f, false, "stone");
            b.Wall(18, 4, 24, 4.6f, 0f, 3f, false, "stone");

            b.Stop("gallery", V(23, 5, 32), 2.5f);
            b.Stop("balcony", V(0, 4, 60), 2.5f);

            // the safe way: round the east walkway, up the gallery ramp from the north, back down, up the balcony ramp
            r.safePath.AddRange(new[]
            {
                r.entry, V(0, 0, 6), V(17, 0, 8), V(17, 0, 54), V(23, 0, 54), V(23, 0, 52), V(23, 5, 42), V(23, 5, 32),
                V(23, 5, 42), V(23, 0, 52), V(23, 0, 54), V(17, 0, 60), V(16, 0, 60), V(10, 4, 60), V(0, 4, 60), r.exit,
            });
            // the fast way: Shadow Steps over the lava (pillar, gallery alcove; pillar, throne)
            r.fastPath.AddRange(new[] { r.entry, V(0, 0, 6), V(9, 1, 22), V(23, 5, 26), V(23, 5, 32), V(11, 1, 40), V(0, 4, 58), V(0, 4, 60), r.exit });

            // the imps she calls in phase 2 (wave 1: the fight spawns them; the arena never spawns on its own)
            b.Spawn(HellEnemyRole.Imp, V(-8, 6.5f, 30), 1);
            b.Spawn(HellEnemyRole.Imp, V(8, 6.5f, 30), 1);
            b.Spawn(HellEnemyRole.Imp, V(0, 7.5f, 44), 1);

            r.minLeg = 20f;
            r.lavaDps = 22f;
            r.lavaSinkDamage = 45f;
            r.lavaSinkSeconds = 1f;
            r.floor = Floor;
            return b.Done();
        }

        static void Pillar(Builder b, float x0, float z0, float x1, float z1, float top)
        {
            b.Deck(x0, z0, x1, z1, HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth, top);
            b.Dark(x0, top, z0, x1, top + 3f, z1);
        }

        /// <summary>The rooms' kit, in small (the rooms lane's own is private to <see cref="HellRoomLayouts"/>).</summary>
        sealed class Builder
        {
            public readonly HellRoomDef r;
            readonly Dictionary<string, int> _n = new Dictionary<string, int>();

            public Builder(string name) { r = new HellRoomDef { name = name, ordered = true }; }

            string Id(string p)
            {
                _n.TryGetValue(p, out int n);
                _n[p] = n + 1;
                return p + "-" + n;
            }

            FloorSolid Add(string id, string role, float x0, float y0, float z0, float x1, float y1, float z1, bool runnable = false)
            {
                var s = new FloorSolid { id = id, role = role, box = new CoverBox { min = V(x0, y0, z0), max = V(x1, y1, z1) }, runnable = runnable };
                r.solids.Add(s);
                return s;
            }

            public void Floor(float x0, float z0, float x1, float z1, float y = 0f) => Add(Id("floor"), "floor", x0, y - 0.5f, z0, x1, y, z1);
            public void Deck(float x0, float z0, float x1, float z1, float yBottom, float top) => Add(Id("deck"), "deck", x0, yBottom, z0, x1, top, z1);
            public void Wall(float x0, float z0, float x1, float z1, float yb, float yt, bool runnable, string prefix = "wall") =>
                Add(Id(prefix), "wall", x0, yb, z0, x1, yt, z1, runnable);
            public void Cover(float x0, float z0, float x1, float z1, float y = 0f) => Add(Id("cover"), "cover", x0, y, z0, x1, y + 1.2f, z1);

            public void Ramp(float x0, float z0, float x1, float z1, float yBottom, float topStart, float topEnd, bool alongX)
            {
                var s = Add(Id("ramp"), "ramp", x0, yBottom, z0, x1, Math.Max(topStart, topEnd), z1);
                s.ramp = true;
                s.rampAlongX = alongX;
                s.rampTopStart = topStart;
                s.rampTopEnd = topEnd;
            }

            public void Lava(float x0, float z0, float x1, float z1) => r.hazards.Add(new HellHazard
            {
                id = Id("lava"), kind = HellHazardKind.Lava,
                box = new CoverBox { min = V(x0, HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth, z0), max = V(x1, HellRoomLayouts.LavaSurface, z1) },
            });

            public void Dark(float x0, float y0, float z0, float x1, float y1, float z1) =>
                r.darkZones.Add(new CoverBox { min = V(x0, y0, z0), max = V(x1, y1, z1) });

            public void Stop(string id, Vector3 p, float reach) => r.stops.Add(new HellStop { id = id, position = p, reach = reach });

            public void Spawn(HellEnemyRole role, Vector3 p, int wave) =>
                r.spawns.Add(new HellSpawn { role = role, position = p, yawDeg = 180f, wave = wave, minFloor = 1 });

            /// <summary>The entry vestibule (south, z -8..0), the outer walls with the south doorway and the north exit door (4 m
            /// wide at <paramref name="exitX"/>, its sill at <paramref name="exitY"/>: the balcony) closed by the exit gate, and a
            /// short corridor behind it (no drop hole: the escape starts there).</summary>
            public void Shell(float half, float length, float baseY, float topY, float exitX, float exitY)
            {
                const float t = 0.6f;
                Floor(-3f, -8f, 3f, 0f);
                Wall(-3f - t, -8f, -3f, 0f, 0f, 5f, false, "vestibule");
                Wall(3f, -8f, 3f + t, 0f, 0f, 5f, false, "vestibule");
                Wall(-3f - t, -8f - t, 3f + t, -8f, 0f, 5f, false, "vestibule");
                Wall(-half - t, -t, -3f, 0f, baseY, topY, false);
                Wall(3f, -t, half + t, 0f, baseY, topY, false);
                Wall(-3f, -t, 3f, 0f, 5f, topY, false, "lintel");
                Wall(-half - t, 0f, -half, length, baseY, topY, true);
                Wall(half, 0f, half + t, length, baseY, topY, true);
                Wall(-half - t, length, exitX - 2f, length + t, baseY, topY, false);
                Wall(exitX + 2f, length, half + t, length + t, baseY, topY, false);
                Wall(exitX - 2f, length, exitX + 2f, length + t, exitY + 4f, topY, false, "lintel");
                Add("exit-gate", "gate", exitX - 2f, exitY, length, exitX + 2f, exitY + 4f, length + t);
                r.exitGateId = "exit-gate";
                float c1 = length + HellRoomLayouts.CorridorLength;
                Floor(exitX - 2f, length, exitX + 2f, c1, exitY);
                Wall(exitX - 2f - t, length + t, exitX - 2f, c1, exitY, exitY + 4f, false, "corridor");
                Wall(exitX + 2f, length + t, exitX + 2f + t, c1, exitY, exitY + 4f, false, "corridor");
                Wall(exitX - 2f - t, c1, exitX + 2f + t, c1 + t, exitY, exitY + 4f, false, "corridor");
                r.entry = V(0, 0, -4f);
                r.entryYaw = 0f;
                r.exit = V(exitX, exitY, length);
                r.exitYaw = 0f;
                r.dropZone = new CoverBox { min = V(exitX - 2f, exitY, c1 - 2f), max = V(exitX + 2f, exitY + 3f, c1) };
                r.bounds = new CoverBox { min = V(-half - t, baseY, -8f - t), max = V(half + t, topY, c1 + t) };
            }

            /// <summary>The lava's fire lights (as the rooms do: every <see cref="HellRoomLayouts.LightSpacing"/> m, none reaching a dark zone).</summary>
            public HellRoomDef Done()
            {
                foreach (var h in r.hazards)
                {
                    if (h.kind != HellHazardKind.Lava) continue;
                    float sx = h.box.max.X - h.box.min.X, sz = h.box.max.Z - h.box.min.Z;
                    int nx = Math.Max(1, (int)Math.Round(sx / HellRoomLayouts.LightSpacing)), nz = Math.Max(1, (int)Math.Round(sz / HellRoomLayouts.LightSpacing));
                    for (int i = 0; i < nx; i++)
                        for (int j = 0; j < nz; j++)
                        {
                            var p = V(h.box.min.X + sx * (i + 0.5f) / nx, HellRoomLayouts.LavaSurface + 0.8f, h.box.min.Z + sz * (j + 0.5f) / nz);
                            float radius = HellRoomLayouts.LightRadius;
                            foreach (var d in r.darkZones) radius = Math.Min(radius, HellRoomLint.DistanceToBox(p, d) - HellRoomLayouts.LightClearance);
                            if (radius >= HellRoomLayouts.MinLightRadius) r.lights.Add(new HellLight { position = p, radius = radius });
                        }
                }
                return r;
            }
        }
    }
}
