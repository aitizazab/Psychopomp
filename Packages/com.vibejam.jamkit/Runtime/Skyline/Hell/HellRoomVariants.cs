using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>One floor's dressing and difficulty (event/HELL-DESIGN.md, "Floors").</summary>
    public sealed class HellFloorTuning
    {
        public string name = "";
        /// <summary>Lava damage per second while touching it, and the hit for sinking.</summary>
        public float lavaDps, lavaSinkDamage;
        /// <summary>Seconds a platform shakes before it falls; the multiplier on each room's return delay.</summary>
        public float shakeSeconds, returnScale;
        /// <summary>Beam lanes: the multiplier on the on time (and the inverse on the off time).</summary>
        public float beamOnScale;
        /// <summary>The Collapse's fronts: the multiplier on both lanes' speeds.</summary>
        public float collapseSpeedScale;
        /// <summary>The room layouts are mirrored (x to -x) and turned this many quarter turns on this floor.</summary>
        public bool mirror;
        public int quarterTurns;
        /// <summary>Which enemies this floor fields; a spawn of another role becomes an imp.</summary>
        public HellEnemyRole[] roles = new HellEnemyRole[0];

        public bool Fields(HellEnemyRole r) => Array.IndexOf(roles, r) >= 0;
    }

    /// <summary>
    /// The per-floor variants: 3 floors x 6 layouts = 18 rooms. A variant takes the base layout
    /// (<see cref="HellRoomLayouts"/>), sets its hazards' numbers from the floor (<see cref="Floors"/>), keeps the spawns the
    /// floor has reached (<see cref="HellSpawn.minFloor"/>) and swaps roles the floor does not field for imps, then mirrors and
    /// turns it. The Ember Gate is unmirrored, the Molten Works mirrored, the Black Glass unmirrored and turned a quarter (its
    /// world orientation differs; its difficulty is in the numbers and the enemy mix).
    /// </summary>
    public static class HellRoomVariants
    {
        public const int FloorCount = 3;

        public static readonly HellFloorTuning[] Floors =
        {
            new HellFloorTuning
            {
                name = "The Ember Gate", lavaDps = 10f, lavaSinkDamage = 30f, shakeSeconds = 0.9f, returnScale = 0.8f,
                beamOnScale = 0.8f, collapseSpeedScale = 0.9f, mirror = false, quarterTurns = 0,
                roles = new[] { HellEnemyRole.Imp },
            },
            new HellFloorTuning
            {
                name = "The Molten Works", lavaDps = 15f, lavaSinkDamage = 45f, shakeSeconds = 0.8f, returnScale = 1f,
                beamOnScale = 1f, collapseSpeedScale = 1f, mirror = true, quarterTurns = 0,
                roles = new[] { HellEnemyRole.Imp, HellEnemyRole.Scythe },
            },
            new HellFloorTuning
            {
                name = "The Black Glass", lavaDps = 22f, lavaSinkDamage = 60f, shakeSeconds = 0.65f, returnScale = 1.5f,
                beamOnScale = 1.25f, collapseSpeedScale = 1.12f, mirror = false, quarterTurns = 1,
                roles = new[] { HellEnemyRole.Imp, HellEnemyRole.Scythe, HellEnemyRole.Ooze },
            },
        };

        /// <summary>The variant of <paramref name="kind"/> for <paramref name="floor"/> (0-based), in its own frame turned and
        /// mirrored (the origin stays the entry vestibule's middle; place it in the world with <see cref="HellRoomTransform.offset"/>).</summary>
        public static HellRoomDef Build(HellRoomKind kind, int floor)
        {
            if (floor < 0 || floor >= FloorCount) throw new ArgumentOutOfRangeException(nameof(floor));
            var b = HellRoomLayouts.Build(kind);
            HellJobPockets.Append(b, floor);   // the God of Death's job pocket, if this room has one on this floor (docs/HELL-JOBS.md)
            var t = Floors[floor];
            b.floor = floor;
            b.lavaDps = t.lavaDps;
            b.lavaSinkDamage = t.lavaSinkDamage;
            b.collapseShakeSeconds = t.shakeSeconds;
            if (b.collapseReturnSeconds >= 0f) b.collapseReturnSeconds *= t.returnScale;
            foreach (var h in b.hazards)
            {
                if (h.kind == HellHazardKind.Beam)
                {
                    h.onSeconds *= t.beamOnScale;
                    h.offSeconds /= t.beamOnScale;
                }
                else if (h.Timeline)
                    h.timelineSeconds = HellRoomLayouts.CollapseTileSeconds(h.box.min.Z, h.id.StartsWith("tile-safe", StringComparison.Ordinal), t.collapseSpeedScale);
            }
            var kept = new List<HellSpawn>();
            foreach (var s in b.spawns)
            {
                if (s.minFloor > floor + 1) continue;
                if (t.Fields(s.role)) { kept.Add(s); continue; }
                bool ground = s.role != HellEnemyRole.Imp;
                kept.Add(new HellSpawn
                {
                    role = HellEnemyRole.Imp, position = ground ? s.position + new Vector3(0, HellRoomEnemies.ImpHover, 0) : s.position,
                    yawDeg = s.yawDeg, wave = s.wave, minFloor = s.minFloor,
                });
            }
            b.spawns = kept;
            b.name = b.name + " (" + t.name + ")";
            return new HellRoomTransform { mirror = t.mirror, quarterTurns = t.quarterTurns }.Room(b);
        }

        /// <summary>All 18 rooms, floor by floor in <see cref="HellRoomLayouts.Kinds"/> order.</summary>
        public static List<HellRoomDef> All()
        {
            var all = new List<HellRoomDef>();
            for (int f = 0; f < FloorCount; f++)
                foreach (var k in HellRoomLayouts.Kinds) all.Add(Build(k, f));
            return all;
        }
    }
}
