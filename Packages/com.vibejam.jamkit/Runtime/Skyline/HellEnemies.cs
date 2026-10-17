namespace JamKit.Skyline
{
    /// <summary>Which Hell enemy a def is (docs/HELL-ENEMIES.md). None: a Skyline bot.</summary>
    public enum HellKind { None, FireImp, ScytheDemon, LavaOoze }

    /// <summary>
    /// The event game's three enemies (FableAnimationTests/event/HELL-DESIGN.md, "Enemies"). Each one keeps the type id, the spawn
    /// API and the functions of the Skyline bot it replaces (another branch spawns them by those ids), so
    /// <c>SkylineBot.Spawn(SkylineEnemies.DroneId, ...)</c> makes a fire imp while the Hell roster is on (the Unity side's default).
    /// The Skyline defs (<see cref="SkylineEnemies"/>) are untouched, so the headless sims and their tests still balance the old bots.
    /// Each call returns a new instance.
    /// </summary>
    public static class HellEnemies
    {
        /// <summary>How much faster the imp's fire bolt flies than the drone's taser bolt (30 m/s): the creator's "FAST fire bolts".</summary>
        public const float ImpBoltSpeedFactor = 3f;

        /// <summary>The fire imp (replaces the mall-cop drone): the drone's hover patrol and lagged aim, a small horned body with
        /// wings and a bright core (the weak point), and an ember bolt three times as fast. Still dodgeable: it aims where your chest
        /// was 0.2 s ago, and the lag alone puts a walker across its line over a metre off (<see cref="HellImpAim"/>).</summary>
        public static EnemyDef FireImp() => new EnemyDef
        {
            id = SkylineEnemies.DroneId, displayName = "Fire imp", hell = HellKind.FireImp, aim = AimModel.LeadsYou, layer = ThreatLayer.Air,
            chassisHp = 60f, plates = new[] { new PlateDef("horns", 15f) }, bodyRadius = 0.6f,
            bodyScale = 1f, weakPointScale = 1f, weakPointRadius = 0.24f,
            alertSeconds = 0.5f, telegraphSeconds = 0.4f, fireInterval = 1.1f, loseSightSeconds = 3f, rememberSeconds = 8f, searchSpeed = 2.5f,
            patrolSpeed = 3f, patrolLeash = 3f, patrolBob = 0.3f, patrolDashSpeed = 5.5f,
            projectileSpeed = 30f * ImpBoltSpeedFactor, projectileRadius = 0.18f, aimLagSeconds = 0.2f, aimSpreadDeg = 0.5f,
            attackDamage = 18f, range = 45f, scrapPlating = 15f,
        };

        /// <summary>The scythe demon (replaces the spotlight): a big ground demon that strides slowly in and calls demon scythes, each
        /// projecting its path for <see cref="EnemyDef.scytheTelegraph"/> seconds and then beaming along it. One lane starts at the
        /// demon and runs at you; the rest are parallel lanes across where you stand. Its weak point is the core in its chest.</summary>
        public static EnemyDef ScytheDemon() => new EnemyDef
        {
            id = SkylineEnemies.SpotlightId, displayName = "Scythe demon", hell = HellKind.ScytheDemon, aim = AimModel.FiresAtWhereYouAre,
            layer = ThreatLayer.Floor,
            chassisHp = 140f, plates = new[] { new PlateDef("pauldron-left", 40f), new PlateDef("pauldron-right", 40f) }, bodyRadius = 0.7f,
            bodyScale = 1f, weakPointScale = 1f, weakPointRadius = 0.26f,
            alertSeconds = 0.6f, telegraphSeconds = 0.6f, fireInterval = 3.4f, loseSightSeconds = 4f, rememberSeconds = 6f,
            projectileSpeed = 0f, attackDamage = 30f, range = 40f, scrapPlating = 25f,
            walkSpeed = 1.6f, walkKeepDistance = 16f,
            scytheCount = 3, scytheTelegraph = 0.9f, scytheBeamSpeed = 45f, scytheLength = 14f, scytheHitRadius = 0.55f, scytheHold = 0.2f,
            scytheHeight = 1.0f, scytheSpacing = 3.6f, scytheDamage = 30f,
        };

        /// <summary>The lava ooze (replaces the floor-care unit): a throbbing blob that crawls at you, throbs faster through its
        /// wind-up and slams a lava pool round itself that burns while you stand in it, then cools and fades. Its weak point is the
        /// bright core on top.</summary>
        public static EnemyDef LavaOoze() => new EnemyDef
        {
            id = SkylineEnemies.FloorCareId, displayName = "Lava ooze", hell = HellKind.LavaOoze, aim = AimModel.Area, layer = ThreatLayer.Floor,
            chassisHp = 90f, plates = new[] { new PlateDef("crust", 35f) }, bodyRadius = 0.65f,
            bodyScale = 1f, weakPointScale = 1f, weakPointRadius = 0.2f,
            alertSeconds = 0.6f, telegraphSeconds = 0.8f, fireInterval = 3.2f, loseSightSeconds = 3f,
            projectileSpeed = 0f, attackDamage = 22f, range = 12f, moveSpeed = 3.2f,
            // the reach that starts its wind-up (and it stops driving at half of it); the pool is what hurts
            sweepRadius = 3.6f, ankleHeight = 0.35f, scrapPlating = 25f,
            idleSpeed = 0.4f, idleLeash = 1.5f,
            poolRadius = 3.2f, poolGrowSeconds = 0.3f, poolBurnSeconds = 3f, poolCoolSeconds = 1.5f, poolDps = 22f, poolTick = 0.25f, poolMax = 3,
            pulseIdleHz = 1.1f, pulseAwakeHz = 1.7f, pulseTelegraphHz = 5f, pulseAmount = 0.12f,
        };

        public static EnemyDef[] All() => new[] { FireImp(), ScytheDemon(), LavaOoze() };

        /// <summary>The Hell enemy of <paramref name="id"/> (the Skyline ids); the gondola, which Hell has no stand-in for, is the
        /// Skyline one. Null for an unknown id.</summary>
        public static EnemyDef Find(string id) =>
            id == SkylineEnemies.DroneId ? FireImp() : id == SkylineEnemies.SpotlightId ? ScytheDemon() :
            id == SkylineEnemies.FloorCareId ? LavaOoze() : SkylineEnemies.Find(id);
    }
}
