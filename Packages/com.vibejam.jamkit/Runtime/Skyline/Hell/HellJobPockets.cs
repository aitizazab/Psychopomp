using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// A job's side pocket in a room's frame: a small walled space beyond one of the room's side walls, through a 4 m doorway
    /// cut into that wall (docs/HELL-JOBS.md). The geometry goes into the room's data (<see cref="HellJobPockets.Append"/>), so
    /// the editor builds it with the room and the room lint checks it; this object carries the job's points for the Unity side.
    /// </summary>
    public sealed class HellJobPocket
    {
        public string jobId = "";
        /// <summary>The pocket's footprint (beyond the room wall), for "the player is in the pocket" and the path rule.</summary>
        public CoverBox area;
        /// <summary>The doorway's middle at floor height (the HUD marker points here); a point in the room in front of it.</summary>
        public Vector3 door, approach;
        /// <summary>The target or the thing: the Viscount's balcony, the soul, the hourglass, the ledger, the Poacher, the whelp,
        /// the Bather's spring, the seal.</summary>
        public Vector3 spot;
        /// <summary>Where the player stands to do the job (the lint: walkable from the approach, unless <see cref="stepOnly"/>).</summary>
        public Vector3 stand;
        /// <summary>The stand is reached only by a Shadow Step (the cage): from <see cref="stepFrom"/> to <see cref="stepTo"/>.</summary>
        public bool stepOnly;
        public Vector3 stepFrom, stepTo;
        /// <summary>Escape: the flee path (the Viscount's palanquin); Burst: the perches; Hold: enemy spawns; Free: the chains.</summary>
        public List<Vector3> path = new List<Vector3>();
        public List<Vector3> points = new List<Vector3>();
        /// <summary>A second figure (Destroy's clerk, Free's smith) or a prop (the Viscount's door, the squatter's door).</summary>
        public Vector3 keeper, prop;
        /// <summary>The ids of solids the job removes when it's done (the cage's door bars).</summary>
        public List<string> openOnDone = new List<string>();

        public HellJobPocket Transformed(HellRoomTransform t)
        {
            var o = new HellJobPocket
            {
                jobId = jobId, area = t.Box(area), door = t.Point(door), approach = t.Point(approach), spot = t.Point(spot),
                stand = t.Point(stand), stepOnly = stepOnly, stepFrom = t.Point(stepFrom), stepTo = t.Point(stepTo),
                keeper = t.Point(keeper), prop = t.Point(prop),
            };
            foreach (var p in path) o.path.Add(t.Point(p));
            foreach (var p in points) o.points.Add(t.Point(p));
            o.openOnDone.AddRange(openOnDone);
            return o;
        }
    }

    /// <summary>
    /// Attaches each job's side pocket to its room without touching the layouts (<see cref="HellRoomLayouts"/> belongs to the
    /// rooms lanes): <see cref="HellRoomVariants.Build"/> calls <see cref="Append"/> on the base layout before the floor's mirror
    /// and turn, so the pocket is transformed, built and linted with the room. A pocket cuts a doorway into the room's side wall
    /// (the wall solid is split round it), adds its own floor, walls, lava, platforms and dark zones, keeps the room's fire lights
    /// clear of its dark zones and grows the room's bounds to cover it.
    /// </summary>
    public static class HellJobPockets
    {
        public const float DoorHalf = 2f, DoorHeight = 4f, Half = 9f, T = 0.6f;

        /// <summary>Where each room kind's pocket door is (base layout frame): which side wall (+1 east, -1 west) and the z of the
        /// doorway's middle. Each sits beside the room's safe walkway, off both paths.</summary>
        public static void Site(HellRoomKind kind, out int side, out float doorZ)
        {
            switch (kind)
            {
                case HellRoomKind.SixSigils: side = -1; doorZ = 120f; break;   // off the west ring walkway
                case HellRoomKind.SoulLantern: side = 1; doorZ = 52f; break;   // off the north floor by the altar
                case HellRoomKind.BreakChains: side = 1; doorZ = 22f; break;   // off the east walkway, under no balcony
                case HellRoomKind.HoldBrand: side = -1; doorZ = 46f; break;    // off the west walkway, past the long bridge
                case HellRoomKind.Collapse: side = 1; doorZ = 116f; break;     // off the north floor (the collapse never reaches it)
                default: side = 1; doorZ = 40f; break;                         // the Ooze Wells: off the east walkway
            }
        }

        /// <summary>Adds the pocket of the job in <paramref name="r"/>'s kind on <paramref name="floor"/> (if any) to the room, in the
        /// base layout's frame. Returns the pocket (or null).</summary>
        public static HellJobPocket Append(HellRoomDef r, int floor)
        {
            var job = HellJobCatalog.For(r.kind, floor);
            if (job == null) return null;
            var k = new Kit(r, job);
            k.Build();
            return k.p;
        }

        /// <summary>The pocket in the floor variant's frame (the frame <see cref="HellRoomVariants.Build"/> returns), or null.</summary>
        public static HellJobPocket Variant(HellRoomKind kind, int floor)
        {
            if (HellJobCatalog.For(kind, floor) == null) return null;
            var b = HellRoomLayouts.Build(kind);
            var p = Append(b, floor);
            var t = HellRoomVariants.Floors[floor];
            return p.Transformed(new HellRoomTransform { mirror = t.mirror, quarterTurns = t.quarterTurns });
        }

        /// <summary>The pocket in world space (as <see cref="HellWorld.Room"/> places its room), or null.</summary>
        public static HellJobPocket World(int floor, HellRoomKind kind)
        {
            var p = Variant(kind, floor);
            return p == null ? null : p.Transformed(new HellRoomTransform { offset = HellWorld.SlotOffset(floor, kind) });
        }

        // ------------------------------------------------------------------ the kit: a pocket frame on the room's side wall

        sealed class Kit
        {
            public readonly HellJobPocket p = new HellJobPocket();
            readonly HellRoomDef r;
            readonly HellJobDef job;
            readonly int side;
            readonly float doorZ, innerX, wallTop, baseY;
            readonly List<CoverBox> _dark = new List<CoverBox>();
            int _n;
            float _depth;

            public Kit(HellRoomDef r, HellJobDef job)
            {
                this.r = r;
                this.job = job;
                Site(r.kind, out side, out doorZ);
                innerX = side > 0 ? r.bounds.max.X - T : r.bounds.min.X + T;
                var wall = SideWall(r, innerX, side, doorZ);
                if (wall == null) throw new InvalidOperationException(r.name + ": no side wall at the job pocket's door (z " + doorZ + ")");
                wallTop = wall.box.max.Y;
                baseY = Math.Min(wall.box.min.Y, HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth);
                SplitWall(wall);
                p.jobId = job.id;
            }

            /// <summary>The room's outer side wall at the door (the shell's wall: thin in x, long in z, at the inner face).</summary>
            static FloorSolid SideWall(HellRoomDef r, float innerX, int side, float z)
            {
                float x = innerX + side * T * 0.5f;
                foreach (var s in r.solids)
                {
                    if (s.role != "wall" || s.ramp) continue;
                    var b = s.box;
                    if (x > b.min.X && x < b.max.X && z - DoorHalf > b.min.Z && z + DoorHalf < b.max.Z && b.max.Z - b.min.Z > 20f) return s;
                }
                return null;
            }

            void SplitWall(FloorSolid w)
            {
                var b = w.box;
                var north = new FloorSolid
                {
                    id = w.id + "-job", role = "wall", runnable = w.runnable,
                    box = new CoverBox { min = new Vector3(b.min.X, b.min.Y, doorZ + DoorHalf), max = b.max },
                };
                w.box = new CoverBox { min = b.min, max = new Vector3(b.max.X, b.max.Y, doorZ - DoorHalf) };
                r.solids.Add(north);
                r.solids.Add(new FloorSolid
                {
                    id = "job-lintel", role = "wall",
                    box = new CoverBox { min = new Vector3(b.min.X, DoorHeight, doorZ - DoorHalf), max = new Vector3(b.max.X, b.max.Y, doorZ + DoorHalf) },
                });
                if (b.min.Y < -0.5f)
                    r.solids.Add(new FloorSolid
                    {
                        id = "job-sill", role = "wall",
                        box = new CoverBox { min = new Vector3(b.min.X, b.min.Y, doorZ - DoorHalf), max = new Vector3(b.max.X, -0.5f, doorZ + DoorHalf) },
                    });
            }

            // pocket frame: u along the wall (room +z), v outward through the wall (0 = the wall's inner face), y absolute
            public Vector3 P(float u, float y, float v) => new Vector3(innerX + side * v, y, doorZ + u);

            CoverBox B(float u0, float y0, float v0, float u1, float y1, float v1)
            {
                var a = P(u0, y0, v0);
                var c = P(u1, y1, v1);
                return new CoverBox { min = Vector3.Min(a, c), max = Vector3.Max(a, c) };
            }

            string Id(string what) => "job-" + job.id + "-" + what + "-" + (_n++);

            FloorSolid Add(string what, string role, CoverBox box)
            {
                var s = new FloorSolid { id = Id(what), role = role, box = box };
                r.solids.Add(s);
                return s;
            }

            void Floor(float u0, float v0, float u1, float v1, float y = 0f) => Add("floor", "floor", B(u0, y - 0.5f, v0, u1, y, v1));
            void Deck(float u0, float v0, float u1, float v1, float top) => Add("deck", "deck", B(u0, baseY, v0, u1, top, v1));
            void Cover(float u0, float v0, float u1, float v1, float h = 1.2f) => Add("cover", "cover", B(u0, 0f, v0, u1, h, v1));
            FloorSolid Wall(string what, float u0, float v0, float u1, float v1, float y0, float y1) => Add(what, "wall", B(u0, y0, v0, u1, y1, v1));

            void Lava(float u0, float v0, float u1, float v1)
            {
                r.hazards.Add(new HellHazard
                {
                    id = Id("lava"), kind = HellHazardKind.Lava,
                    box = B(u0, HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth, v0, u1, HellRoomLayouts.LavaSurface, v1),
                });
            }

            void Collapse(float u, float v, float size = 2.4f, float top = 0f)
            {
                float h = size * 0.5f;
                r.hazards.Add(new HellHazard
                {
                    id = Id("collapse"), kind = HellHazardKind.Collapse,
                    box = B(u - h, top - HellRoomLayouts.PlatformThickness, v - h, u + h, top, v + h),
                });
            }

            void Dark(float u0, float y0, float v0, float u1, float y1, float v1) => _dark.Add(B(u0, y0, v0, u1, y1, v1));

            /// <summary>The pocket's walls (two sides and the back, room height), its area and its approach.</summary>
            void Shell(float depth)
            {
                _depth = depth;
                Wall("wall", -Half - T, T, -Half, depth + T, baseY, wallTop);
                Wall("wall", Half, T, Half + T, depth + T, baseY, wallTop);
                Wall("wall", -Half - T, depth, Half + T, depth + T, baseY, wallTop);
                p.area = B(-Half, baseY, T, Half, wallTop, depth);
                p.door = P(0f, 0f, 0f);
                p.approach = P(0f, 0f, -3f);
            }

            public void Build()
            {
                switch (job.id)
                {
                    case HellJobCatalog.Viscount: Viscount(); break;
                    case HellJobCatalog.OverdueSoul: OverdueSoul(); break;
                    case HellJobCatalog.Hourglass: Hourglass(); break;
                    case HellJobCatalog.Ledger: Ledger(); break;
                    case HellJobCatalog.Poacher: Poacher(); break;
                    case HellJobCatalog.Whelp: Whelp(); break;
                    case HellJobCatalog.SecondsThief: SecondsThief(); break;
                    case HellJobCatalog.Bather: Bather(); break;
                    case HellJobCatalog.Notice: Notice(); break;
                    default: throw new ArgumentOutOfRangeException(job.id);
                }
                Finish();
            }

            /// <summary>The Viscount's balcony over a lava inlet; when alerted his palanquin carries him over the lava to his door
            /// in the far corner. A walkway along one side (dark at its end: a Shadow Step from the landing) cuts him off.</summary>
            void Viscount()
            {
                Shell(22f);
                Floor(-Half, 0f, Half, 5f);
                Floor(-Half, 5f, -6.5f, 22f);
                Lava(-6.5f, 5f, Half, 22f);
                Deck(2f, 5.5f, 8f, 9.5f, 2.5f);
                Dark(-Half, 0f, 17f, -6.5f, 3f, 22f);
                p.spot = P(5f, 3.2f, 7.5f);
                p.path.Add(P(5f, 3.2f, 7.5f));
                p.path.Add(P(5f, 3.2f, 18f));
                p.path.Add(P(-4f, 3.2f, 18f));
                p.path.Add(P(-4f, 2f, 21.4f));
                p.prop = P(-4f, 0f, 21.7f);
                p.stand = P(-7.75f, 0f, 20f);
            }

            /// <summary>A barred cage, roofed, dark inside, with Albrecht Huss in it. The bars are too close to squeeze through
            /// but a Shadow Step aimed through a gap lands inside. Reaping him opens the cage's front.</summary>
            void OverdueSoul()
            {
                Shell(18f);
                Floor(-Half, 0f, Half, 18f);
                const float u0 = -3f, u1 = 3f, v0 = 9f, v1 = 15f, h = 3.4f, bar = 0.1f;
                int n = 8;
                for (int i = 0; i <= n; i++)
                {
                    float u = u0 + (u1 - u0) * i / n;
                    var front = Wall("cagedoor", u - bar, v0 - bar, u + bar, v0 + bar, 0f, h);
                    p.openOnDone.Add(front.id);
                    Wall("bar", u - bar, v1 - bar, u + bar, v1 + bar, 0f, h);
                }
                for (int i = 1; i < n; i++)
                {
                    float v = v0 + (v1 - v0) * i / n;
                    Wall("bar", u0 - bar, v - bar, u0 + bar, v + bar, 0f, h);
                    Wall("bar", u1 - bar, v - bar, u1 + bar, v + bar, 0f, h);
                }
                Wall("cageroof", u0 - 0.2f, v0 - 0.2f, u1 + 0.2f, v1 + 0.2f, h, h + 0.2f);
                Dark(u0 + 0.15f, 0f, v0 + 0.15f, u1 - 0.15f, h - 0.1f, v1 - 0.15f);
                float gap = (u1 - u0) / n * 0.5f;   // the middle of a gap between two bars
                p.spot = P(0f, 0f, 12f);
                p.stand = P(0f, 0f, 12f);
                p.stepOnly = true;
                p.stepFrom = P(gap, 0f, 5f);
                p.stepTo = P(gap, 0f, 12.5f);
            }

            /// <summary>The hourglass on a shelf across a lava trench: the stones straight across (they fall), or the walkway
            /// round the side. Carry it out (slower, no Shadow Step).</summary>
            void Hourglass()
            {
                Shell(18f);
                Floor(-Half, 0f, Half, 5f);
                Lava(-Half, 5f, 6.5f, 14f);
                Floor(6.5f, 5f, Half, 14f);
                Floor(-Half, 14f, Half, 18f);
                Collapse(-2f, 7f);
                Collapse(-2f, 10.5f);
                p.spot = P(-2f, 1.1f, 16f);
                p.stand = P(-2f, 0f, 16f);
            }

            /// <summary>The clerk's office: a desk with the ledger, stacks of paperwork for cover, a filing cabinet at the back.</summary>
            void Ledger()
            {
                Shell(16f);
                Floor(-Half, 0f, Half, 16f);
                Cover(-3f, 11f, 3f, 12.5f, 1f);
                Cover(-6f, 6f, -4f, 7f);
                Cover(4f, 6f, 6f, 7f);
                Wall("cabinet", -1.5f, 15f, 1.5f, 16f, 0f, 2.5f);
                p.spot = P(0f, 1.25f, 11.75f);
                p.keeper = P(0f, 2.2f, 14f);
                p.stand = P(0f, 0f, 8f);
            }

            /// <summary>The Poacher's den: he works at his rack of soul jars with his back to the door, glancing round now and
            /// then. The entry and a side gallery are dark.</summary>
            void Poacher()
            {
                Shell(20f);
                Floor(-Half, 0f, Half, 20f);
                Dark(-Half, 0f, T, Half, 4f, 8f);
                Dark(-Half, 0f, 8f, -5f, 4f, 20f);
                Cover(-5.6f, 10f, -5f, 12f);
                Cover(-5.6f, 15f, -5f, 17f);
                Wall("rack", -4f, 18.5f, 4f, 19.5f, 0f, 2.4f);
                p.spot = P(0f, 0f, 16.5f);
                p.prop = P(0f, 0f, 19f);
                p.stand = P(-7f, 0f, 14f);
            }

            /// <summary>The whelp chained in the middle: three chains to anchors on the two side walls and high on the back wall;
            /// the smith hovers by his anvil.</summary>
            void Whelp()
            {
                Shell(16f);
                Floor(-Half, 0f, Half, 16f);
                Cover(3f, 12f, 5f, 13f, 0.9f);
                p.spot = P(0f, 0f, 12f);
                p.points.Add(P(-Half + 0.4f, 1.8f, 11f));
                p.points.Add(P(Half - 0.4f, 4.2f, 7f));
                p.points.Add(P(0f, 6.5f, 15.6f));
                p.keeper = P(4f, 2.5f, 13.5f);
                p.stand = P(0f, 0f, 10f);
            }

            /// <summary>A lava inlet with three dark perches: the thief hops between them; the perches let you Shadow Step close.</summary>
            void SecondsThief()
            {
                Shell(22f);
                Floor(-Half, 0f, Half, 5f);
                Lava(-Half, 5f, Half, 22f);
                foreach (var c in new[] { new Vector2(-5f, 9f), new Vector2(4f, 13f), new Vector2(-3f, 18f) })
                {
                    Deck(c.X - 1.25f, c.Y - 1.25f, c.X + 1.25f, c.Y + 1.25f, 1f);
                    Dark(c.X - 1.25f, 1f, c.Y - 1.25f, c.X + 1.25f, 4f, c.Y + 1.25f);
                    p.points.Add(P(c.X, 1f, c.Y));
                }
                p.spot = p.points[0];
                p.stand = P(0f, 0f, 3f);
            }

            /// <summary>A lava spring; the Bather drifts in its middle. Four stones (they fall) let you get close without wading.</summary>
            void Bather()
            {
                Shell(18f);
                Floor(-Half, 0f, Half, 5f);
                Lava(-Half, 5f, Half, 18f);
                Collapse(-2.2f, 9f);
                Collapse(2.2f, 9f);
                Collapse(-2.2f, 14f);
                Collapse(2.2f, 14f);
                p.spot = P(0f, HellRoomLayouts.LavaSurface + 0.2f, 11.5f);
                p.stand = P(0f, 0f, 3f);
            }

            /// <summary>The squatter's door at the back, the seal in front of it, two pillars; his friends come in from above.</summary>
            void Notice()
            {
                Shell(16f);
                Floor(-Half, 0f, Half, 16f);
                Cover(-6.5f, 7.5f, -5.5f, 8.5f, 2.5f);
                Cover(5.5f, 7.5f, 6.5f, 8.5f, 2.5f);
                p.spot = P(0f, 0f, 13f);
                p.prop = P(0f, 0f, 15.7f);
                p.stand = P(0f, 0f, 13f);
                p.points.Add(P(-6f, 4f, 14f));
                p.points.Add(P(6f, 4f, 14f));
                p.points.Add(P(0f, 4f, 5f));
            }

            /// <summary>The dark zones in; the room's fire lights kept clear of them (and the pocket's own lava lit the same way);
            /// the room's bounds grown over the pocket.</summary>
            void Finish()
            {
                r.darkZones.AddRange(_dark);
                for (int i = r.lights.Count - 1; i >= 0; i--)
                {
                    var l = r.lights[i];
                    foreach (var d in _dark) l.radius = Math.Min(l.radius, HellRoomLint.DistanceToBox(l.position, d) - HellRoomLayouts.LightClearance);
                    if (l.radius < HellRoomLayouts.MinLightRadius) r.lights.RemoveAt(i);
                    else r.lights[i] = l;
                }
                foreach (var h in r.hazards)
                {
                    if (h.kind != HellHazardKind.Lava || !h.id.StartsWith("job-", StringComparison.Ordinal)) continue;
                    var c = (h.box.min + h.box.max) * 0.5f;
                    var at = new Vector3(c.X, HellRoomLayouts.LavaSurface + 0.8f, c.Z);
                    float radius = HellRoomLayouts.LightRadius;
                    foreach (var d in r.darkZones) radius = Math.Min(radius, HellRoomLint.DistanceToBox(at, d) - HellRoomLayouts.LightClearance);
                    if (radius >= HellRoomLayouts.MinLightRadius) r.lights.Add(new HellLight { position = at, radius = radius });
                }
                var a = B(-Half - T, baseY, 0f, Half + T, wallTop, _depth + T);
                r.bounds = new CoverBox { min = Vector3.Min(r.bounds.min, a.min), max = Vector3.Max(r.bounds.max, a.max) };
            }
        }
    }

    /// <summary>
    /// The pocket rule (docs/HELL-JOBS.md), on top of the room lint: the pocket is off both paths (no sample of the safe or the
    /// fast path inside it), its door is reached on foot from the safe path, the job's stand is reached on foot from the door
    /// (or, for the cage, NOT on foot but by a Shadow Step that is in range, in sight and lands in darkness), the pocket's solids
    /// don't run into the room's, and every chain (Free) can be shot from inside its ward.
    /// </summary>
    public static class HellJobPocketLint
    {
        public static List<string> Check(HellRoomDef r, HellJobPocket p)
        {
            var bad = new List<string>();
            string who = r.name + " / " + p.jobId + ": ";
            foreach (var path in new[] { r.safePath, r.fastPath })
                for (int i = 1; i < path.Count; i++)
                {
                    int n = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(path[i - 1], path[i]) / 0.5f));
                    for (int k = 0; k <= n; k++)
                    {
                        var s = Vector3.Lerp(path[i - 1], path[i], k / (float)n);
                        if (InXZ(p.area, s)) { bad.Add(who + "a " + (path == r.safePath ? "safe" : "fast") + " path runs through the pocket"); k = n; i = path.Count; }
                    }
                }

            var nav = ArenaNav.Build(HellRoomLint.NavSolids(r), 1f);
            int door = nav.NodeAt(p.approach);
            if (door < 0) bad.Add(who + "no ground in front of the pocket's door");
            else
            {
                int near = -1;
                float best = float.MaxValue;
                foreach (var v in r.safePath)
                {
                    float d = Vector3.Distance(v, p.approach);
                    int node = nav.NodeAt(v);
                    if (node >= 0 && d < best) { best = d; near = node; }
                }
                if (near < 0 || !nav.Reachable(near, door)) bad.Add(who + "the pocket's door can't be walked to from the safe path");
                int stand = nav.NodeAt(p.stand);
                bool walk = stand >= 0 && nav.Reachable(door, stand) && Vector3.Distance(nav.PositionOf(stand), p.stand) < 1.5f;
                if (!p.stepOnly && !walk) bad.Add(who + "the job's spot can't be walked to from the door");
                if (p.stepOnly)
                {
                    if (walk) bad.Add(who + "the cage can be walked into (it should take a Shadow Step)");
                    int from = nav.NodeAt(p.stepFrom);
                    if (from < 0 || !nav.Reachable(door, from)) bad.Add(who + "the Shadow Step's start can't be walked to");
                    var eye = p.stepFrom + new Vector3(0f, ArenaNav.EyeHeight, 0f);
                    var chest = p.stepTo + new Vector3(0f, 1.2f, 0f);
                    if (Vector3.Distance(eye, p.stepTo) > 18f) bad.Add(who + "the Shadow Step into the cage is out of range");
                    string block = FirstSolidOn(r, eye, chest) ?? FirstSolidOn(r, eye, p.stepTo + new Vector3(0f, 0.05f, 0f));
                    if (block != null) bad.Add(who + "the Shadow Step into the cage is blocked by " + block);
                    bool dark = false;
                    foreach (var d in r.darkZones) if (HellRoomLint.Contains(d, p.stepTo + new Vector3(0f, 0.5f, 0f))) dark = true;
                    if (!dark) bad.Add(who + "the Shadow Step's landing is not dark");
                }
            }

            foreach (var s in r.solids)
            {
                if (s.id.StartsWith("job-", StringComparison.Ordinal) || s.id.EndsWith("-job", StringComparison.Ordinal)) continue;
                if (Overlap(s.box, p.area)) bad.Add(who + "the room's " + s.id + " runs into the pocket");
            }

            var job = HellJobCatalog.Find(p.jobId);
            if (job != null && job.verb == HellJobVerb.Free)
                foreach (var c in p.points)
                {
                    bool ok = false;
                    for (int i = 0; i < nav.Count && !ok; i++)
                    {
                        var q = nav.PositionOf(i);
                        if (!InXZ(p.area, q)) continue;
                        if (Vector3.Distance(q + new Vector3(0f, ArenaNav.EyeHeight, 0f), c) <= job.wardRadius) ok = true;
                    }
                    if (!ok) bad.Add(who + "a chain can't be shot from inside its ward");
                }
            return bad;
        }

        /// <summary>The first solid the segment a-b passes through (gates count: they're shut), else null.</summary>
        public static string FirstSolidOn(HellRoomDef r, Vector3 a, Vector3 b)
        {
            foreach (var s in r.solids)
                if (SegmentHitsBox(a, b, s.box)) return s.id;
            return null;
        }

        public static bool SegmentHitsBox(Vector3 a, Vector3 b, CoverBox box)
        {
            float t0 = 0f, t1 = 1f;
            var d = b - a;
            if (!Slab(a.X, d.X, box.min.X, box.max.X, ref t0, ref t1)) return false;
            if (!Slab(a.Y, d.Y, box.min.Y, box.max.Y, ref t0, ref t1)) return false;
            if (!Slab(a.Z, d.Z, box.min.Z, box.max.Z, ref t0, ref t1)) return false;
            return t1 > t0;
        }

        static bool Slab(float a, float d, float lo, float hi, ref float t0, ref float t1)
        {
            if (Math.Abs(d) < 1e-6f) return a > lo && a < hi;
            float u0 = (lo - a) / d, u1 = (hi - a) / d;
            if (u0 > u1) { float t = u0; u0 = u1; u1 = t; }
            t0 = Math.Max(t0, u0);
            t1 = Math.Min(t1, u1);
            return t1 > t0;
        }

        static bool InXZ(CoverBox b, Vector3 s) => s.X > b.min.X && s.X < b.max.X && s.Z > b.min.Z && s.Z < b.max.Z;

        static bool Overlap(CoverBox a, CoverBox b) =>
            a.min.X < b.max.X - 0.01f && a.max.X > b.min.X + 0.01f && a.min.Z < b.max.Z - 0.01f && a.max.Z > b.min.Z + 0.01f &&
            a.min.Y < b.max.Y && a.max.Y > b.min.Y;
    }
}
