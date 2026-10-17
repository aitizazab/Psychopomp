using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A collapsing platform (on the editor's box for the hazard <see cref="hazardId"/>). It shakes for the room's shake time
    /// (about 0.8 s) after the player lands on it, falls (its collider goes at once, the block drops and sinks), and comes back
    /// after the room's return delay (never, when negative), unless the player is standing where it would be. A timeline tile
    /// (The Collapse's floor) starts shaking at its room-clock time instead. Rewindable: never destroyed, its state is restored.
    /// </summary>
    public sealed class HellCollapsePlatform : MonoBehaviour, IHellRewindable
    {
        public string hazardId = "";

        public const float ShakeAmplitude = 0.06f, FallDistance = 6f;

        HellRoom _room;
        HellHazard _h;
        CollapseCycle _cycle;
        Collider _collider;
        Renderer _renderer;
        Vector3 _home;

        public bool Solid => _cycle == null || _cycle.Solid;

        public void Bind(HellRoom room)
        {
            _room = room;
            _h = room.Def.Hazard(hazardId);
            _cycle = new CollapseCycle(room.Def.collapseShakeSeconds, _h != null && _h.Timeline ? -1f : room.Def.collapseReturnSeconds);
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();
            _home = transform.position;
        }

        void OnEnable() { HellRewindRegistry.Register(this); }
        void OnDisable() { HellRewindRegistry.Unregister(this); }

        /// <summary>The player is standing on this platform.</summary>
        public bool Under(NVec3 feet, bool grounded) => _h != null && Solid && CollapseCycle.StandingOn(_h.box, feet, grounded);

        public void ResetPlatform()
        {
            if (_cycle == null) return;
            _cycle.Reset();
            Apply();
        }

        void FixedUpdate()
        {
            if (_room == null || _h == null || !_room.Running || _room.Player == null) return;
            var p = _room.Player;
            var feet = HellRoom.N(p.transform.position);
            var sim = p.Movement != null ? p.Movement.Sim : null;
            bool grounded = sim != null && sim.Grounded;
            float height = sim != null ? sim.Height : 1.7f;
            if (_h.Timeline && _room.Clock >= _h.timelineSeconds) _cycle.Trigger();
            bool standing = !_h.Timeline && CollapseCycle.StandingOn(_h.box, feet, grounded);
            _cycle.Tick(Time.fixedDeltaTime, standing, CollapseCycle.BodyIn(_h.box, feet, height));
            Apply();
        }

        void Apply()
        {
            bool solid = _cycle.Solid;
            if (_collider != null && _collider.enabled != solid) _collider.enabled = solid;
            if (_renderer != null) _renderer.enabled = solid || _cycle.Fall < 1f;
            Vector3 at = _home;
            float shake = _cycle.Shake;
            if (shake > 0f)
            {
                float a = ShakeAmplitude * (0.4f + shake);
                at += new Vector3(Mathf.Sin(Time.time * 61f) * a, Mathf.Sin(Time.time * 47f) * a * 0.5f, Mathf.Cos(Time.time * 53f) * a);
            }
            if (!solid) at += Vector3.down * FallDistance * _cycle.Fall * _cycle.Fall;
            transform.position = at;
        }

        readonly HellSnapPool<HellSnapBox<CollapseCycle.State>> _snaps = new HellSnapPool<HellSnapBox<CollapseCycle.State>>();

        public object Snapshot()
        {
            var b = _snaps.Next();
            b.value = _cycle != null ? _cycle.S : default(CollapseCycle.State);
            return b;
        }

        public void Restore(object snapshot)
        {
            if (_cycle == null) return;
            _cycle.S = HellSnapBox<CollapseCycle.State>.From(snapshot);
            Apply();
        }
    }
}
