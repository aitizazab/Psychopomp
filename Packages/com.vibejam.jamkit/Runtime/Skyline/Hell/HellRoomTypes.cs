using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>The six room types of the descent (event/HELL-DESIGN.md, "The rooms"). Each is one layout, varied per floor.</summary>
    public enum HellRoomKind { SixSigils, SoulLantern, BreakChains, HoldBrand, Collapse, OozeWells }

    /// <summary>The three enemies of Hell. They are the kit's bots under new skins (the enemies branch reskins them):
    /// the fire imp is the drone, the scythe demon is the spotlight, the lava ooze is the floor-care unit.</summary>
    public enum HellEnemyRole { Imp, Scythe, Ooze }

    /// <summary>Lava (damage over time, sinking respawns you), a collapsing platform, a beam lane (a hot line at chest height:
    /// slide under it).</summary>
    public enum HellHazardKind { Lava, Collapse, Beam }

    public static class HellRoomEnemies
    {
        /// <summary>The kit's enemy id for a role (the existing type ids, unchanged).</summary>
        public static string IdFor(HellEnemyRole r)
        {
            switch (r)
            {
                case HellEnemyRole.Scythe: return SkylineEnemies.SpotlightId;
                case HellEnemyRole.Ooze: return SkylineEnemies.FloorCareId;
                default: return SkylineEnemies.DroneId;
            }
        }

        /// <summary>The height above the floor an imp (a hovering drone) is spawned at when a ground spawn becomes an imp.</summary>
        public const float ImpHover = 3.5f;
    }

    /// <summary>One hazard as data. <see cref="box"/> is world (or room-local) space: for lava its top is the lava surface; for a
    /// collapsing platform it is the platform; for a beam it is the thin hot volume.</summary>
    public sealed class HellHazard
    {
        public string id = "";
        public HellHazardKind kind;
        public CoverBox box;
        /// <summary>Beam: seconds on and off per cycle, and the cycle's offset in seconds.</summary>
        public float onSeconds = 1.6f, offSeconds = 1.6f, phase;
        /// <summary>Collapse driven by the room clock (The Collapse room's floor): the room time it starts shaking. Negative:
        /// it shakes when the player lands on it (the usual platform).</summary>
        public float timelineSeconds = -1f;
        /// <summary>A timeline tile is the escape's floor (the safe lane too): the path lint treats it as ground, not a hazard.</summary>
        public bool Timeline => kind == HellHazardKind.Collapse && timelineSeconds >= 0f;
    }

    /// <summary>An enemy spawn point. <see cref="wave"/> 0 spawns when the room starts; higher waves are released by the
    /// objective (Hold the Brand's waves). <see cref="minFloor"/> (1-based, like the floor names) leaves it out on earlier floors.</summary>
    public sealed class HellSpawn
    {
        public HellEnemyRole role;
        public Vector3 position;
        public float yawDeg = 180f;
        public int wave;
        public int minFloor = 1;
    }

    /// <summary>An objective point: a sigil, the lantern's pedestal, the altar, a chain anchor, the brand circle, a well. The
    /// path lint needs a path point within <see cref="reach"/> of it (for an anchor that is the ward radius you shoot from).</summary>
    public sealed class HellStop
    {
        public string id = "";
        public Vector3 position;
        public float reach = 2.5f;
    }

    /// <summary>A fire light (lava glow, a brazier): the darkness rule's light sources (HellDarkness.RegisterLight). Its steady
    /// <see cref="radius"/> never reaches a dark zone; a lava light near one may also <b>flare</b> (a lava spurt,
    /// docs/HELL-SHADOW.md): for <see cref="flareSeconds"/> every <see cref="flarePeriod"/> s it lights out to
    /// <see cref="flareRadius"/>, into the dark beside it (fire kills shadow, live). 0 flare radius: it never flares.</summary>
    public struct HellLight
    {
        public Vector3 position;
        public float radius;
        public float flareRadius, flarePeriod, flareSeconds, flarePhase;

        public bool Flares => flareRadius > radius && flarePeriod > 0f && flareSeconds > 0f;

        /// <summary>Is the flare up at game time <paramref name="t"/> (s)? The cycle starts at <see cref="flarePhase"/> (0..1).</summary>
        public bool FlareUp(double t)
        {
            if (!Flares) return false;
            double c = t / flarePeriod + flarePhase;
            double f = c - Math.Floor(c);
            return f * flarePeriod < flareSeconds;
        }
    }

    /// <summary>Hold the Brand's ring (docs/HELL-ROOMS.md, "Hold the Brand"): a wide circle round a lava pool that charges
    /// faster the faster you move inside it (<see cref="HellBrandTuning"/>). <see cref="loop"/> is the racing line round the
    /// pool (closed: its last point is its first), which the lint checks lies inside the ring on solid ground.</summary>
    public sealed class HellHoldRing
    {
        public Vector3 center;
        public float radius = HellBrandTuning.RingRadius, halfHeight = HellBrandTuning.RingHalfHeight;
        public List<Vector3> loop = new List<Vector3>();
    }

    /// <summary>One piece of falling debris (The Collapse): it lands at room second <see cref="time"/> on
    /// <see cref="position"/> (a floor point) and hits a body within <see cref="radius"/>; its shadow glows on the floor for
    /// <see cref="HellDebris.TelegraphSeconds"/> before. Pure data: where everything is follows from the room clock, so the
    /// Rewind of the clock rewinds the debris.</summary>
    public struct HellDebrisDrop
    {
        public float time;
        public Vector3 position;
        public float radius;
    }

    /// <summary>The objective's numbers for one room (a floor's variant may change them).</summary>
    public sealed class HellObjectiveTuning
    {
        /// <summary>Sigils: touch radius. Lantern: pick-up and place radius. Wells: the sealing radius. Brand: the circle (the
        /// repossess circle's own radius is <see cref="FloorRules.CircleRadius"/>).</summary>
        public float radius = 2f;
        /// <summary>Wells: seconds to seal one. Brand: seconds between waves once the charge has started.</summary>
        public float seconds = 3f;
        /// <summary>Wells: seconds between oozes per open well; the most living oozes per well.</summary>
        public float spawnEvery = 7f;
        public int spawnCap = 2;
        /// <summary>Chains: hit points per anchor, and the ward radius (shots from farther are deflected).</summary>
        public float anchorHealth = 120f, wardRadius = 12f;
        /// <summary>Lantern: the carrier's move scale.</summary>
        public float carryMoveScale = 0.8f;
    }

    /// <summary>
    /// One room as data, in its own frame: the entry vestibule is at z &lt; 0 with <see cref="entry"/> at its middle, facing
    /// +Z; the room runs north; <see cref="exit"/> is the doorway in the north wall, the exit corridor and its drop hole lie
    /// beyond it. Everything the Unity builder, the run director and the lint need: solids (one collider each), hazards,
    /// enemy spawns, the objective's points, the gates, and the two paths (safe and fast) the lint checks.
    /// </summary>
    public sealed class HellRoomDef
    {
        public HellRoomKind kind;
        public string name = "";
        /// <summary>The floor this variant is for (0-based: 0 the Ember Gate, 1 the Molten Works, 2 the Black Glass); -1 for the base layout.</summary>
        public int floor = -1;
        public bool mirrored;
        /// <summary>Quarter turns clockwise seen from above.</summary>
        public int quarterTurns;

        public List<FloorSolid> solids = new List<FloorSolid>();
        public List<HellHazard> hazards = new List<HellHazard>();
        public List<HellSpawn> spawns = new List<HellSpawn>();
        public List<HellStop> stops = new List<HellStop>();
        /// <summary>The objective's stops must be reached in order (sigils, lantern then altar); wells and anchors in any order.</summary>
        public bool ordered = true;
        /// <summary>The shortest objective leg allowed (stop to stop, metres): the scale rule (the Six Sigils: 30).</summary>
        public float minLeg = 20f;
        /// <summary>Per stop, where the objective spawns its own enemies (the Ooze Wells' oozes); empty elsewhere.</summary>
        public List<Vector3> stopSpawns = new List<Vector3>();
        /// <summary>Authored darkness (the Shadow Step lands only in darkness): volumes, kept clear of every fire light. Chains of
        /// them across lava are the skilled player's fast lines; the safe path never needs them.</summary>
        public List<CoverBox> darkZones = new List<CoverBox>();
        /// <summary>The fire lights (made from the lava by <see cref="HellRoomLayouts"/>, none reaching a dark zone).</summary>
        public List<HellLight> lights = new List<HellLight>();

        public Vector3 entry;
        public float entryYaw;
        public Vector3 exit;
        public float exitYaw;
        /// <summary>The box under the exit corridor's drop hole: the player's feet in it means "left the room".</summary>
        public CoverBox dropZone;
        /// <summary>The room's footprint (for the bots' search area and the nav).</summary>
        public CoverBox bounds;

        /// <summary>The id of the solid that closes the exit until the objective is done (empty: no gate, as in The Collapse).</summary>
        public string exitGateId = "";
        /// <summary>The Six Sigils' gate before F (opened by a shot at <see cref="gateTarget"/>); empty elsewhere.</summary>
        public string innerGateId = "";
        public Vector3 gateTarget;

        /// <summary>Seconds a fallen platform takes to come back in this room (negative: never).</summary>
        public float collapseReturnSeconds = 4f;
        /// <summary>Seconds a platform shakes before it falls.</summary>
        public float collapseShakeSeconds = 0.8f;
        /// <summary>Lava: damage per second while touching it, the damage of sinking, and seconds of contact that sink you.</summary>
        public float lavaDps = 12f, lavaSinkDamage = 35f, lavaSinkSeconds = 1.0f;
        /// <summary>The lava's top is a floor you wade on (true: the builder gives the basins a collider at the surface, a
        /// jump and a mantle get you out). False (The Collapse's bridge, docs/HELL-ROOMS.md): no collider, a fallen tile drops
        /// you into it and <see cref="LavaContact.Under"/> sinks you as soon as your feet are under its surface.</summary>
        public bool lavaFloor = true;
        /// <summary>The open rim's inner edge (the room's west, east, south and north wall lines; docs/HELL-ROOMS.md, "Open
        /// edges"): the lint keeps the safe path off it where no parapet stands. Zero size: not checked.</summary>
        public CoverBox rim;

        public HellObjectiveTuning tuning = new HellObjectiveTuning();
        /// <summary>Hold the Brand's ring (null in the other rooms).</summary>
        public HellHoldRing hold;
        /// <summary>The Collapse's falling debris, in landing order (empty elsewhere).</summary>
        public List<HellDebrisDrop> debris = new List<HellDebrisDrop>();

        /// <summary>The two routes the lint checks, from <see cref="entry"/> to <see cref="exit"/> through the stops.</summary>
        public List<Vector3> safePath = new List<Vector3>();
        public List<Vector3> fastPath = new List<Vector3>();

        public FloorSolid Solid(string id)
        {
            foreach (var s in solids) if (s.id == id) return s;
            return null;
        }

        public HellHazard Hazard(string id)
        {
            foreach (var h in hazards) if (h.id == id) return h;
            return null;
        }
    }

    /// <summary>
    /// Mirror (x to -x) then quarter turns clockwise (seen from above, yaw 90 = +X), about the room's own origin. Boxes stay
    /// axis-aligned; a ramp's slope axis and its start and end heights follow. Pure.
    /// </summary>
    public struct HellRoomTransform
    {
        public bool mirror;
        public int quarterTurns;
        public Vector3 offset;

        public static HellRoomTransform Identity => default(HellRoomTransform);

        int Q => ((quarterTurns % 4) + 4) % 4;

        public Vector3 Point(Vector3 p)
        {
            if (mirror) p.X = -p.X;
            float x = p.X, z = p.Z;
            switch (Q)
            {
                case 1: p.X = z; p.Z = -x; break;
                case 2: p.X = -x; p.Z = -z; break;
                case 3: p.X = -z; p.Z = x; break;
            }
            return p + offset;
        }

        public float Yaw(float yawDeg)
        {
            float y = mirror ? -yawDeg : yawDeg;
            y += 90f * Q;
            y %= 360f;
            if (y < 0f) y += 360f;
            return y;
        }

        public CoverBox Box(CoverBox b)
        {
            var a = Point(b.min);
            var c = Point(b.max);
            return new CoverBox { min = Vector3.Min(a, c), max = Vector3.Max(a, c) };
        }

        public FloorSolid Solid(FloorSolid s)
        {
            var o = new FloorSolid
            {
                id = s.id, role = s.role, box = Box(s.box), runnable = s.runnable, glass = s.glass, ramp = s.ramp,
                rampAlongX = s.rampAlongX, rampTopStart = s.rampTopStart, rampTopEnd = s.rampTopEnd,
            };
            if (!s.ramp) return o;
            // Where does the old start edge (the box's minimum on the slope axis) land?
            var c = (s.box.min + s.box.max) * 0.5f;
            var startPoint = s.rampAlongX ? new Vector3(s.box.min.X, c.Y, c.Z) : new Vector3(c.X, c.Y, s.box.min.Z);
            var sp = Point(startPoint);
            var nc = (o.box.min + o.box.max) * 0.5f;
            bool alongX = Math.Abs(sp.X - nc.X) > Math.Abs(sp.Z - nc.Z);
            o.rampAlongX = alongX;
            bool startIsMin = alongX ? sp.X < nc.X : sp.Z < nc.Z;
            o.rampTopStart = (startIsMin ? s.rampTopStart : s.rampTopEnd) + offset.Y;
            o.rampTopEnd = (startIsMin ? s.rampTopEnd : s.rampTopStart) + offset.Y;
            return o;
        }

        public HellHazard Hazard(HellHazard h) => new HellHazard
        {
            id = h.id, kind = h.kind, box = Box(h.box), onSeconds = h.onSeconds, offSeconds = h.offSeconds, phase = h.phase,
            timelineSeconds = h.timelineSeconds,
        };

        /// <summary>A copy of the room in this transform (the def's own numbers and tuning are copied as they are).</summary>
        public HellRoomDef Room(HellRoomDef r)
        {
            var o = new HellRoomDef
            {
                kind = r.kind, name = r.name, floor = r.floor, mirrored = r.mirrored ^ mirror, minLeg = r.minLeg,
                quarterTurns = (r.quarterTurns + Q) % 4, ordered = r.ordered,
                entry = Point(r.entry), entryYaw = Yaw(r.entryYaw), exit = Point(r.exit), exitYaw = Yaw(r.exitYaw),
                dropZone = Box(r.dropZone), bounds = Box(r.bounds), rim = Box(r.rim), exitGateId = r.exitGateId, innerGateId = r.innerGateId,
                gateTarget = Point(r.gateTarget), collapseReturnSeconds = r.collapseReturnSeconds,
                collapseShakeSeconds = r.collapseShakeSeconds, lavaDps = r.lavaDps, lavaSinkDamage = r.lavaSinkDamage,
                lavaSinkSeconds = r.lavaSinkSeconds, lavaFloor = r.lavaFloor, tuning = r.tuning,
            };
            foreach (var s in r.solids) o.solids.Add(Solid(s));
            foreach (var h in r.hazards) o.hazards.Add(Hazard(h));
            foreach (var s in r.spawns)
                o.spawns.Add(new HellSpawn { role = s.role, position = Point(s.position), yawDeg = Yaw(s.yawDeg), wave = s.wave, minFloor = s.minFloor });
            foreach (var s in r.stops) o.stops.Add(new HellStop { id = s.id, position = Point(s.position), reach = s.reach });
            foreach (var p in r.stopSpawns) o.stopSpawns.Add(Point(p));
            foreach (var d in r.darkZones) o.darkZones.Add(Box(d));
            foreach (var l in r.lights) { var c = l; c.position = Point(l.position); o.lights.Add(c); }
            foreach (var p in r.safePath) o.safePath.Add(Point(p));
            foreach (var p in r.fastPath) o.fastPath.Add(Point(p));
            if (r.hold != null)
            {
                o.hold = new HellHoldRing { center = Point(r.hold.center), radius = r.hold.radius, halfHeight = r.hold.halfHeight };
                foreach (var p in r.hold.loop) o.hold.loop.Add(Point(p));
            }
            foreach (var d in r.debris) o.debris.Add(new HellDebrisDrop { time = d.time, position = Point(d.position), radius = d.radius });
            return o;
        }
    }
}
