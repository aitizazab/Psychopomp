using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Rewind on the kit's bots: the ONE snapshot path for every bot, Skyline or Hell (docs/HELL-INTEGRATION.md; it merges the
    /// enemies branch's full snapshot with the abilities branch's revive-or-retire). Kept in its own file so edits to SkylineBot.cs
    /// don't collide.
    /// <para><b>Registration</b>: a bot registers with <see cref="HellRewindRegistry"/> while it is active and initialised
    /// (SkylineBot.cs: Init, OnEnable, OnDisable call <see cref="HellRewindListed"/>), so a dead Hell enemy (deactivated) is
    /// unregistered, but the frames taken while it lived still hold it, and <see cref="HellRewind"/> restores a frame's own
    /// objects whether registered or not.</para>
    /// <para><b>The snapshot</b>: position, facing, velocity (knock and patrol), health and plates, alive, the brain, patrol,
    /// search, arrival, idle work, aim trail, random stream, damage stage and the Hell attack state (the ooze's throb and pools,
    /// the demon's arm and stride, the imp's charge).</para>
    /// <para><b>Restore</b>: puts all of it back. A Hell enemy that died since (deactivated, no wreck) is reactivated. A Skyline
    /// bot wrecked since can't un-break in place (its wreck took the body), so it is <b>revived</b>: a fresh bot of the same kind,
    /// seed, group and target takes the snapshot, the old one and its wreck are retired, and <see cref="HellRevived"/> tells the
    /// directors to swap references. A bot that appeared after the snapshot is <b>retired</b>
    /// (<see cref="IHellRewindSpawnAware.RewoundBeforeSpawn"/>; <see cref="HellRetired"/> tells the directors).</para>
    /// <para>Not restored: the drone's hover offset, a Skyline beam or acid sweep in progress (they restart from the brain's
    /// state), the gondola's rail and mines, Writ spots, burns.</para>
    /// </summary>
    public sealed partial class SkylineBot : IHellRewindable, IHellRewindSpawnAware
    {
        /// <summary>A rewind replaced a wrecked bot (old) with a fresh one (new). Directors swap their references.</summary>
        public static event Action<SkylineBot, SkylineBot> HellRevived;
        /// <summary>A rewind switched this bot off for good (it came after the snapshot, or it was revived as another).</summary>
        public static event Action<SkylineBot> HellRetired;

        sealed class HellSnap
        {
            public bool active, wrecked, awake, sees, everSeen, sawLast, hasGoal, returning, charging, striding;
            public Vector3 pos, home, kick, lastKnown, goal, rootPos;
            public Quaternion rootRot;
            public float yaw, pitch, shake, sway, swayVel, clock, lastSeenAt, eightPhase, eightWeight, charge, spin;
            public float armRaise, armTarget, walkPhase, flap, slam;
            public readonly List<Vector3> trail = new List<Vector3>();
            public readonly List<HellLavaPool> pools = new List<HellLavaPool>();
            public int lookStage;
            public GunDef killedBy;
            public object body, brain, patrol, rng, chestTrail, arrival, cleaning, damageStates, pulse;
        }

        bool _hellRetired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHellStatics()
        {
            HellRevived = null;
            HellRetired = null;
        }

        /// <summary>Listed with the Rewind while active (called from Init, OnEnable and OnDisable in SkylineBot.cs).</summary>
        void HellRewindListed(bool on)
        {
            if (on && _initialized && !_hellRetired) HellRewindRegistry.Register(this);
            else if (!on) HellRewindRegistry.Unregister(this);
        }

        object IHellRewindable.Snapshot() => SnapshotNow();

        HellSnap SnapshotNow()
        {
            if (!_initialized || Body == null) return null;
            var s = new HellSnap();
            s.active = gameObject.activeSelf; s.wrecked = Body.Wrecked; s.awake = IsAwake; s.sees = SeesTarget; s.everSeen = _everSeen;
            s.sawLast = _sawLastStep; s.hasGoal = _hasGoal; s.returning = _returning; s.charging = _charging; s.striding = _striding;
            s.pos = _pos; s.home = _home; s.kick = _kick; s.lastKnown = _lastKnown; s.goal = _goal;
            s.rootPos = transform.position; s.rootRot = transform.rotation;
            s.yaw = _yaw; s.pitch = _pitch; s.shake = _shake; s.sway = _sway; s.swayVel = _swayVel; s.clock = _clock; s.lastSeenAt = _lastSeenAt;
            s.eightPhase = _eightPhase; s.eightWeight = _eightWeight; s.charge = _charge; s.spin = _spin;
            s.armRaise = _armRaise; s.armTarget = _armTarget; s.walkPhase = _walkPhase; s.flap = _flap; s.slam = _slam;
            s.trail.Clear(); s.trail.AddRange(_trail);
            s.pools.Clear(); s.pools.AddRange(_pools);
            s.lookStage = _lookStage; s.killedBy = KilledBy;
            // the pure parts: reflection copies, each refilled in place when the snapshot object is reused
            s.body = StateCopy.Capture(Body, s.body);
            s.brain = StateCopy.Capture(Brain, s.brain);
            s.patrol = StateCopy.Capture(_patrol, s.patrol);
            s.rng = StateCopy.Capture(_rng, s.rng);
            s.chestTrail = Def.aimLagSeconds > 0f ? StateCopy.Capture(_chestTrail, s.chestTrail) : null;
            s.arrival = StateCopy.Capture(_arrival, s.arrival);
            s.cleaning = StateCopy.Capture(_cleaning, s.cleaning);
            s.damageStates = StateCopy.Capture(_damageStates, s.damageStates);
            s.pulse = StateCopy.Capture(_pulse, s.pulse);
            return s;
        }

        void IHellRewindable.Restore(object snapshot)
        {
            if (!(snapshot is HellSnap s) || !_initialized || _hellRetired) return;
            if (!s.active || s.wrecked) return;   // already off or wrecked then: nothing to undo
            if (Body.Wrecked && WreckObject != null) { Revive(s); return; }   // a Skyline wreck: a fresh bot takes over
            ApplyHellSnap(s);
        }

        void IHellRewindSpawnAware.RewoundBeforeSpawn() => Retire();

        void ApplyHellSnap(HellSnap s)
        {
            StateCopy.Apply(Body, s.body);
            StateCopy.Apply(Brain, s.brain);
            StateCopy.Apply(_patrol, s.patrol);
            StateCopy.Apply(_rng, s.rng);
            if (s.chestTrail != null) StateCopy.Apply(_chestTrail, s.chestTrail);
            StateCopy.Apply(_arrival, s.arrival);
            StateCopy.Apply(_cleaning, s.cleaning);
            StateCopy.Apply(_pulse, s.pulse);
            IsAwake = s.awake;
            SeesTarget = s.sees;
            _everSeen = s.everSeen;
            _sawLastStep = s.sawLast;
            _hasGoal = s.hasGoal;
            _returning = s.returning;
            _charging = s.charging;
            _striding = s.striding;
            _pos = s.pos; _home = s.home; _kick = s.kick; _lastKnown = s.lastKnown; _goal = s.goal;
            _yaw = s.yaw; _pitch = s.pitch; _shake = s.shake; _sway = s.sway; _swayVel = s.swayVel;
            _clock = s.clock; _lastSeenAt = s.lastSeenAt; _eightPhase = s.eightPhase; _eightWeight = s.eightWeight; _charge = s.charge; _spin = s.spin;
            _armRaise = s.armRaise; _armTarget = s.armTarget; _walkPhase = s.walkPhase; _flap = s.flap; _slam = s.slam;
            _trail.Clear();
            _trail.AddRange(s.trail);
            _pools.Clear();
            _pools.AddRange(s.pools);
            KilledBy = s.killedBy;
            transform.SetPositionAndRotation(s.rootPos, s.rootRot);

            // the look: plates on or off, wear, the damage stage (without its animation), the face, the charge
            for (int i = 0; i < Parts.plates.Length; i++)
                if (Parts.plates[i] != null) Parts.plates[i].gameObject.SetActive(Body.PlateOn(i));
            ClearFlash();
            if (_lookStage != s.lookStage) ForceDamageStage(s.lookStage);
            _pendingStages.Clear();
            StateCopy.Apply(_damageStates, s.damageStates);
            ApplyWear(true);
            if (_acid != null) _acid.Stop();
            if (_beamView != null) _beamView.Hide();
            if (!_charging && _chargeBall != null) _chargeBall.SetActive(false);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);   // a Hell enemy that died since comes back (OnEnable lists it again)
                Debug.Log("HELL_ENEMY " + Id + " restored alive by the Rewind");
            }
            if (Alive && !Live.Contains(this)) Live.Add(this);
            if (Alive && !SkylineTargets.Aimables.Contains(this)) SkylineTargets.Aimables.Add(this);
            _face.Show(Brain.Face);
            Pose(0f);
        }

        void Revive(HellSnap s)
        {
            var parent = transform.parent;
            Vector3 railEnd = Rail != null ? new Vector3(Rail.End.X, Rail.End.Y, Rail.End.Z) : s.pos;
            Vector3 at = Rail != null ? new Vector3(Rail.Start.X, Rail.Start.Y, Rail.Start.Z) : s.pos;
            var fresh = Spawn(Def.id, at, _spawnYaw, _seed, target, _writ, parent, group, railEnd);
            if (fresh == null) return;
            fresh.practice = practice;
            fresh.Door = Door;
            fresh.gameObject.name = gameObject.name;
            fresh.ApplyHellSnap(s);
            if (WreckObject != null) WreckObject.gameObject.SetActive(false);
            Debug.Log("HELL_REWIND revived " + Id + " as " + fresh.Id);
            HellRevived?.Invoke(this, fresh);
            Retire();
        }

        /// <summary>Off for good (a rewind went to before it, or it was revived as another): not live, not aimed at, not
        /// recorded; the object stays (inactive; no burst, no events but <see cref="HellRetired"/>) so nothing that references
        /// it breaks.</summary>
        void Retire()
        {
            if (_hellRetired) return;
            _hellRetired = true;
            Live.Remove(this);
            SkylineTargets.Aimables.Remove(this);
            HellRewindRegistry.Unregister(this);
            if (WreckObject != null) WreckObject.gameObject.SetActive(false);
            if (_railBar != null) _railBar.SetActive(false);
            foreach (var v in _mineViews.Values) v.Destroy();
            _mineViews.Clear();
            _charging = false;
            if (_chargeBall != null) _chargeBall.SetActive(false);
            HellRetired?.Invoke(this);
            gameObject.SetActive(false);
        }

        /// <summary>Switched off by a rewind (see <see cref="Retire"/>).</summary>
        public bool HellRetiredByRewind => _hellRetired;
    }
}
