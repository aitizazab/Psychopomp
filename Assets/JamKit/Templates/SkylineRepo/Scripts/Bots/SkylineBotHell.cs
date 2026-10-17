using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The event game's enemies on the Skyline bot (docs/HELL-ENEMIES.md). Same ids, spawn calls, brain, sight, groups, plates, damage
    /// states and hit zones; this partial adds what is new:
    /// <list type="bullet">
    /// <item><b>Fire imp</b> (the drone's id): the drone's hover patrol, search and lagged aim; its bolt is an ember at three times the
    /// speed (<see cref="SkylineBolt.MakeEmber"/>); an ember swells in its mouth through the telegraph; its wings flap and its flames
    /// flicker.</item>
    /// <item><b>Scythe demon</b> (the spotlight's id): a ground demon that strides in at <c>walkSpeed</c> while further than
    /// <c>walkKeepDistance</c>, raises its scythe through the telegraph, and on the attack calls a volley of demon scythes
    /// (<see cref="ScytheVolley"/>, <see cref="HellScythe"/>).</item>
    /// <item><b>Lava ooze</b> (the floor-care unit's id): the floor-care unit's drive, idle crawl and reach; it throbs
    /// (<see cref="OozePulse"/>), faster through its wind-up, and its attack slams a lava pool round itself (<see cref="HellLavaPool"/>),
    /// at most <c>poolMax</c> burning at once.</item>
    /// <item><b>Death</b>: a burst of fire and smoke (<see cref="HellFx.DeathBurst"/>), plates thrown off, then the bot is DEACTIVATED,
    /// never destroyed, so the Rewind can bring it back.</item>
    /// <item><b>Rewind</b>: <see cref="IHellRewindable"/>; registered while active. The snapshot holds the position, facing, velocity
    /// (knock and patrol), health and plates, alive, the brain, patrol, search, arrival, idle work, aim trail, random stream, damage
    /// stage and its own attack state (the ooze's throb and pools, the demon's arm, the imp's charge). Restore puts all of it back,
    /// reactivating a bot that died since.</item>
    /// </list>
    /// </summary>
    public sealed partial class SkylineBot : IHellRewindSpawned
    {
        // ---- the rig's animated parts (found by name: HellBodies' contract)
        Transform _wingL, _wingR, _armR, _legL, _legR, _blob;
        readonly List<Transform> _flames = new List<Transform>();
        readonly List<Vector3> _flameScale = new List<Vector3>();
        Vector3 _blobScale = Vector3.one, _coreScale = Vector3.one;
        MeshRenderer _blobR;
        Color _blobColor;
        static MaterialPropertyBlock _hellBlock;
        static readonly int HellBaseColorId = Shader.PropertyToID("_BaseColor"), HellColorId = Shader.PropertyToID("_Color");

        // ---- Hell state (all in the rewind snapshot)
        OozePulse _pulse;
        readonly List<HellLavaPool> _pools = new List<HellLavaPool>();
        float _armRaise, _armTarget, _walkPhase, _flap, _slam;
        bool _striding;
        readonly List<ScythePath> _volley = new List<ScythePath>();

        /// <summary>The Hell enemy this is (None: a Skyline bot).</summary>
        public HellKind Hell => Def != null ? Def.hell : HellKind.None;

        void HellInit()
        {
            var rig = Parts.rig;
            _wingL = SkylineBotBodies.Find(rig, "WingL");
            _wingR = SkylineBotBodies.Find(rig, "WingR");
            _armR = SkylineBotBodies.Find(rig, "ArmR");
            _legL = SkylineBotBodies.Find(rig, "LegL");
            _legR = SkylineBotBodies.Find(rig, "LegR");
            _blob = SkylineBotBodies.Find(rig, "Blob");
            if (_blob != null)
            {
                _blobScale = _blob.localScale;
                _blobR = _blob.GetComponent<MeshRenderer>();
                var m = _blobR != null ? _blobR.sharedMaterial : null;
                _blobColor = m != null && m.HasProperty(HellBaseColorId) ? m.GetColor(HellBaseColorId) : HellLook.Lava;
            }
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Flame"))
                {
                    _flames.Add(t);
                    _flameScale.Add(t.localScale);
                }
            if (Parts.weakPoint != null) _coreScale = Parts.weakPoint.localScale;
            if (Def.hell == HellKind.LavaOoze) _pulse = new OozePulse(Def, (float)(_seed % 1000UL) / 1000f);
            HellEnabled();
        }

        void HellEnabled()
        {
            if (Alive && !Live.Contains(this)) Live.Add(this);
            HellRewindRegistry.Register(this);
            HellRewindLedger.Enabled(this);
        }

        void HellDisabled()
        {
            Live.Remove(this);
            HellRewindRegistry.Unregister(this);
            HellRewindLedger.Disabled(this);
        }

        // ------------------------------------------------------------------ every frame

        void HellTick(float dt, bool live)
        {
            // flames lick: each flickers on its own beat
            for (int i = 0; i < _flames.Count; i++)
            {
                var f = _flames[i];
                if (f == null) continue;
                float k = 1f + 0.22f * Mathf.Sin(_clock * (17f + i * 3.1f) + i) + 0.12f * Mathf.Sin(_clock * (29f + i * 5.3f));
                Vector3 s = _flameScale[i];
                f.localScale = new Vector3(s.x * (2f - k) * 0.5f + s.x * 0.5f, s.y * k, s.z * (2f - k) * 0.5f + s.z * 0.5f);
            }
            switch (Def.hell)
            {
                case HellKind.FireImp:
                {
                    // wings beat (faster in a dash), folded a little while it charges a shot
                    float rate = _patrol != null && _patrol.Dashing ? 14f : 9f;
                    _flap += dt * rate;
                    float a = Mathf.Sin(_flap * Mathf.PI * 2f) * 38f + (_charging ? -12f : 0f);
                    if (_wingL != null) _wingL.localRotation = Quaternion.Euler(0f, 0f, a);
                    if (_wingR != null) _wingR.localRotation = Quaternion.Euler(0f, 0f, -a);
                    break;
                }
                case HellKind.LavaOoze:
                {
                    float windUp = Brain.State == BotState.Telegraph && Def.telegraphSeconds > 0f ? Brain.StateTime / Def.telegraphSeconds : 0f;
                    _pulse.Tick(dt, IsAwake, windUp);
                    _slam = Mathf.MoveTowards(_slam, 0f, dt * 3f);
                    if (_blob != null)
                    {
                        Vector3 sc = U(_pulse.Scale);
                        // the slam: a hard squash that springs back
                        float squash = _slam * _slam;
                        _blob.localScale = Vector3.Scale(_blobScale, new Vector3(sc.x * (1f + 0.35f * squash), sc.y * (1f - 0.45f * squash), sc.z * (1f + 0.35f * squash)));
                        _blob.localPosition = new Vector3(0f, _blobScale.y * 0.5f * sc.y * (1f - 0.45f * squash) - 0.015f, 0f);
                        if (_blobR != null && _flashLeft <= 0f)
                        {
                            if (_hellBlock == null) _hellBlock = new MaterialPropertyBlock();
                            float wear = Mathf.Lerp(1f, 0.55f, 1f - DamageStates.Integrity(Body));
                            Color c = _blobColor * (_pulse.Glow * 0.7f * wear);
                            c.a = 1f;
                            _hellBlock.SetColor(HellBaseColorId, c);
                            _hellBlock.SetColor(HellColorId, c);
                            _blobR.SetPropertyBlock(_hellBlock);
                        }
                    }
                    if (Parts.weakPoint != null) Parts.weakPoint.localScale = _coreScale * (1f + 0.15f * _pulse.Throb);
                    for (int i = _pools.Count - 1; i >= 0; i--)
                        if (_pools[i] == null || !_pools[i].gameObject.activeSelf || _pools[i].Lava == null || _pools[i].Lava.Gone) _pools.RemoveAt(i);
                    break;
                }
                case HellKind.ScytheDemon:
                    _armRaise = Mathf.MoveTowards(_armRaise, _armTarget, dt * (_armTarget > _armRaise ? 3.2f : 2f));
                    if (_armTarget > 0f && Brain.State != BotState.Telegraph && _armRaise >= 0.99f) _armTarget = 0f;
                    break;
            }
        }

        /// <summary>The demon strides in while it is further than its keep distance (not while winding up).</summary>
        void DemonStride(float dt, bool live)
        {
            _striding = false;
            if (!IsAwake || !live || practice || Brain.State == BotState.Telegraph || !(Def.walkSpeed > 0f)) return;
            Vector3 feet = target.Root.position;
            float dx = feet.x - _pos.x, dz = feet.z - _pos.z;
            if (dx * dx + dz * dz <= Def.walkKeepDistance * Def.walkKeepDistance) return;
            Vector3 before = _pos;
            Drive(feet, Def.walkSpeed * dt);
            _striding = (_pos - before).sqrMagnitude > 1e-8f;
            if (_striding) _walkPhase += dt * Def.walkSpeed * 0.9f;
        }

        void PoseDemon(float dt)
        {
            transform.position = _pos;
            var shake = _shake > 0f ? Quaternion.Euler(_rng.Range(-1f, 1f) * _shake * 4f, _rng.Range(-1f, 1f) * _shake * 4f, 0f) : Quaternion.identity;
            float bob = _striding ? Mathf.Abs(Mathf.Sin(_walkPhase * Mathf.PI * 2f)) * 0.05f : 0f;
            Parts.rig.localPosition = new Vector3(0f, bob, 0f);
            Parts.rig.rotation = Quaternion.Euler(0f, _yaw, 0f) * shake;
            float swing = _striding ? Mathf.Sin(_walkPhase * Mathf.PI * 2f) * 28f : 0f;
            if (_legL != null) _legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            // the wind-up: the scythe arm swings up and back over its head, then down with the volley
            if (_armR != null) _armR.localRotation = Quaternion.Euler(-150f * _armRaise + swing * 0.4f, 0f, -12f * _armRaise);
        }

        // ------------------------------------------------------------------ attacks

        void DemonWindUp() => _armTarget = 1f;

        /// <summary>The demon's attack: the volley's scythes appear and project their paths (<see cref="ScytheVolley"/>).</summary>
        void DemonVolley(bool sees)
        {
            _armTarget = 0f;
            if (!TargetLive(target) || target.Root == null) return;
            ScytheVolley.Plan(N(_pos), N(target.Root.position), Def, _rng, _volley);
            for (int i = 0; i < _volley.Count; i++) HellScythe.Spawn(_volley[i], Def, target, Def.id);
            Sound("attack");
            Attacked?.Invoke(this);
        }

        /// <summary>The ooze's attack: it slams down and a lava pool spreads round it; one more than <c>poolMax</c> cools the oldest.</summary>
        void OozeSlam()
        {
            for (int i = _pools.Count - 1; i >= 0; i--)
                if (_pools[i] == null || !_pools[i].gameObject.activeSelf || _pools[i].Lava == null || !_pools[i].Lava.Burning) _pools.RemoveAt(i);
            while (_pools.Count >= Mathf.Max(1, Def.poolMax))
            {
                _pools[0].Lava.CoolNow();
                _pools.RemoveAt(0);
            }
            Vector3 ground = _pos + Vector3.up * 0.01f;
            if (CastClear(_pos + Vector3.up * 0.5f, Vector3.down, 2f, transform, out var hit)) ground = hit.point + Vector3.up * 0.01f;
            _pools.Add(HellLavaPool.Spawn(ground, Def, target, Def.id));
            _slam = 1f;
            Sound("attack");
            Attacked?.Invoke(this);
        }

        // ------------------------------------------------------------------ sound

        /// <summary>Hell's own sounds at <c>Resources/SkylineAudio/hell/&lt;fire-imp | scythe-demon | lava-ooze&gt;/&lt;name&gt;</c> once they
        /// exist; until then only the cues that warn or confirm (telegraph, attack, plate shed, wreck) borrow the old bot's sound, and
        /// the robot's hum, chirps and cries stay off.</summary>
        void HellSound(string name)
        {
            string key = "hell/" + HellBodies.Name(Def.hell) + "/" + name;
            if (SkylineSounds.Get(key) != null) { SkylineSounds.PlayAt(key, Center); return; }
            if (name == "telegraph" || name == "attack" || name == "plate_shed" || name == "wreck") SkylineSounds.PlayAt(Def.id + "/" + name, Center);
        }

        // ------------------------------------------------------------------ death

        Vector3 HellDamagePoint(bool one)
        {
            switch (Def.hell)
            {
                case HellKind.FireImp: return one ? new Vector3(-0.22f, 0.1f, 0.12f) : new Vector3(0.2f, -0.12f, 0.1f);
                case HellKind.ScytheDemon: return one ? new Vector3(-0.4f, 1.9f, 0.2f) : new Vector3(0.3f, 1.35f, 0.26f);
                default: return one ? new Vector3(0.4f, 0.6f, 0.3f) : new Vector3(-0.35f, 0.7f, -0.2f);
            }
        }

        float HellBodySize => Def.hell == HellKind.ScytheDemon ? 1.1f : Def.hell == HellKind.LavaOoze ? 0.75f : 0.5f;

        /// <summary>A Hell death: the brain wrecked (its cue and the Wrecked event as before), a burst of fire and smoke, the plates
        /// still on thrown off, then the bot switched off (kept for the Rewind).</summary>
        void HellDie(Vector3 normal)
        {
            _hitCues.Clear();
            Brain.OnWrecked(_hitCues);
            HandleCues(_hitCues, false, Center, false);
            Sound("wreck");
            _charging = false;
            if (_chargeBall != null) _chargeBall.SetActive(false);
            ClearFlash();
            Vector3 c = Center;
            HellFx.DeathBurst(c, HellBodySize);
            for (int i = 0; i < Parts.plates.Length; i++)
            {
                var t = Parts.plates[i];
                if (t == null || !t.gameObject.activeSelf) continue;
                Fling(t.gameObject, (t.position - c).sqrMagnitude > 1e-4f ? (t.position - c).normalized : Vector3.up, 1.3f, NoColliders);
                t.gameObject.SetActive(false);
            }
            foreach (var m in _markers.Values) if (m != null) m.Close();
            _markers.Clear();
            Spots.Clear();
            _pendingStages.Clear();
            Wrecked?.Invoke(this);
            Debug.Log("HELL_ENEMY " + Id + " died (deactivated, kept for the Rewind)");
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ rewind

        sealed class HellSnap
        {
            public bool active, awake, sees, everSeen, sawLast, hasGoal, returning, charging, striding;
            public Vector3 pos, home, kick, lastKnown, goal, rootPos;
            public Quaternion rootRot;
            public float yaw, pitch, shake, sway, swayVel, clock, lastSeenAt, eightPhase, eightWeight, charge, spin;
            public float armRaise, armTarget, walkPhase, flap, slam;
            public Vector3[] trail;
            public HellLavaPool[] pools;
            public int lookStage;
            public GunDef killedBy;
            public object body, brain, patrol, rng, chestTrail, arrival, cleaning, damageStates, pulse;
        }

        public object Snapshot()
        {
            if (!_initialized) return null;
            return new HellSnap
            {
                active = gameObject.activeSelf, awake = IsAwake, sees = SeesTarget, everSeen = _everSeen, sawLast = _sawLastStep,
                hasGoal = _hasGoal, returning = _returning, charging = _charging, striding = _striding,
                pos = _pos, home = _home, kick = _kick, lastKnown = _lastKnown, goal = _goal, rootPos = transform.position, rootRot = transform.rotation,
                yaw = _yaw, pitch = _pitch, shake = _shake, sway = _sway, swayVel = _swayVel, clock = _clock, lastSeenAt = _lastSeenAt,
                eightPhase = _eightPhase, eightWeight = _eightWeight, charge = _charge, spin = _spin,
                armRaise = _armRaise, armTarget = _armTarget, walkPhase = _walkPhase, flap = _flap, slam = _slam,
                trail = _trail.Count > 0 ? _trail.ToArray() : null,
                pools = _pools.Count > 0 ? _pools.ToArray() : null,
                lookStage = _lookStage, killedBy = KilledBy,
                body = StateCopy.Capture(Body), brain = StateCopy.Capture(Brain), patrol = StateCopy.Capture(_patrol), rng = StateCopy.Capture(_rng),
                chestTrail = Def.aimLagSeconds > 0f ? StateCopy.Capture(_chestTrail) : null, arrival = StateCopy.Capture(_arrival),
                cleaning = StateCopy.Capture(_cleaning), damageStates = StateCopy.Capture(_damageStates), pulse = StateCopy.Capture(_pulse),
            };
        }

        public void Restore(object snapshot)
        {
            if (!(snapshot is HellSnap s) || !_initialized) return;
            if (!s.active)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
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
            if (s.trail != null) _trail.AddRange(s.trail);
            _pools.Clear();
            if (s.pools != null) _pools.AddRange(s.pools);
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
            if (!_charging && _chargeBall != null) _chargeBall.SetActive(false);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);   // a bot that died since comes back (OnEnable re-lists it)
                Debug.Log("HELL_ENEMY " + Id + " restored alive by the Rewind");
            }
            else if (!Live.Contains(this) && Alive) Live.Add(this);
            _face.Show(Brain.Face);
            Pose(0f);
        }

        public void VanishForRewind()
        {
            // the Rewind went back to before this bot was spawned: it never was (no burst, no events)
            if (_chargeBall != null) _chargeBall.SetActive(false);
            gameObject.SetActive(false);
        }

    }
}
