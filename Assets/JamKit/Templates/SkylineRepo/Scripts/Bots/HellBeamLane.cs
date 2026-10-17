using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A room's "beam lane" (docs/HELL-ENEMIES.md): a demon scythe on a fixed dangerous path, no demon needed, fired on a clock
    /// (<see cref="BeamLane"/>: every <see cref="period"/> seconds from <see cref="offset"/>). The path is <see cref="from"/> to
    /// <see cref="to"/> in this object's local space (put the object on the floor and the points at waist height, about 1 m up).
    /// The target is the scene's <see cref="SkylinePlayer"/> unless a room sets one. Rewindable (its clock).
    /// </summary>
    public sealed class HellBeamLane : MonoBehaviour, IHellRewindable
    {
        public Vector3 from = new Vector3(-6f, 1f, 0f), to = new Vector3(6f, 1f, 0f);
        public float period = 3f, offset = 0.5f;
        public float telegraph = 0.9f, beamSpeed = 45f, hold = 0.2f, hitRadius = 0.55f, damage = 30f;
        public ISkylineTarget target;

        BeamLane _clock;

        /// <summary>A lane made in code (a room's builder): world points, its clock and the demon's beam tuning by default.</summary>
        public static HellBeamLane Create(Vector3 worldFrom, Vector3 worldTo, float period, float offset, Transform parent = null, ISkylineTarget target = null)
        {
            var go = new GameObject("Hell beam lane");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            var lane = go.AddComponent<HellBeamLane>();
            var d = HellEnemies.ScytheDemon();
            lane.from = go.transform.InverseTransformPoint(worldFrom);
            lane.to = go.transform.InverseTransformPoint(worldTo);
            lane.period = period;
            lane.offset = offset;
            lane.telegraph = d.scytheTelegraph;
            lane.beamSpeed = d.scytheBeamSpeed;
            lane.hold = d.scytheHold;
            lane.hitRadius = d.scytheHitRadius;
            lane.damage = d.scytheDamage;
            lane.target = target;
            return lane;
        }

        void OnEnable() => HellRewindRegistry.Register(this);
        void OnDisable() => HellRewindRegistry.Unregister(this);

        void Update()
        {
            if (_clock == null) _clock = new BeamLane(period, offset);
            float dt = Time.deltaTime;
            if (!(dt > 0f)) return;
            int fire = _clock.Tick(dt);
            if (fire <= 0) return;
            if (target == null) target = Object.FindAnyObjectByType<SkylinePlayer>();
            HellScythe.Spawn(transform.TransformPoint(from), transform.TransformPoint(to), telegraph, beamSpeed, hold, hitRadius, damage, target, "beam-lane");
        }

        public object Snapshot() => _clock != null ? StateCopy.Capture(_clock) : null;

        public void Restore(object snapshot)
        {
            if (snapshot == null) { _clock = null; return; }
            if (_clock == null) _clock = new BeamLane(period, offset);
            StateCopy.Apply(_clock, snapshot);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f);
            Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
        }
    }
}
