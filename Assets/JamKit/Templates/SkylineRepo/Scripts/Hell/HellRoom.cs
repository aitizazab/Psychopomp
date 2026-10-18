using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// One room at run time (docs/HELL-ROOMS.md). Its data is <see cref="HellWorld.Room"/> for its <see cref="kind"/> and
    /// <see cref="floor"/> (the same data the editor built its geometry from). <see cref="Begin"/> when the player drops in:
    /// the clock starts, wave 0 spawns, the darkness registers, the bots' flight area and walk graph are set. Each fixed step
    /// it feeds the objective the player's feet, spawns the objective's enemies (the brand's waves, the wells' oozes), keeps
    /// the last safe spot, handles a sink in lava (respawn at the safe spot; in The Collapse the room restarts) and opens the
    /// exit gate when the objective is done. <see cref="Restart"/> after a death. Rewindable: the clock, the objective, the
    /// spawn counter and the gate (the hazards and the bots are rewindables of their own).
    /// </summary>
    public sealed class HellRoom : MonoBehaviour, IHellRewindable
    {
        public HellRoomKind kind;
        public int floor;

        public HellRoomDef Def { get; private set; }
        public HellObjective Objective { get; private set; }
        public float Clock { get; private set; }
        public bool Running { get; private set; }
        public SkylinePlayer Player => _player;
        public HellLava Lava { get; private set; }
        public IReadOnlyList<HellCollapsePlatform> Platforms => _platforms;

        SkylinePlayer _player;
        Transform _bots;
        GameObject _exitGate, _lanternView;
        Transform _pedestal, _altar;
        readonly List<HellCollapsePlatform> _platforms = new List<HellCollapsePlatform>();
        readonly List<Renderer> _stopViews = new List<Renderer>();
        readonly List<GameObject> _anchorViews = new List<GameObject>();
        HellChainViews _chainViews;   // Break the Chains: the chains, the straining links, the shatter
        readonly List<Renderer> _wellCaps = new List<Renderer>();
        readonly List<List<SkylineBot>> _wellBots = new List<List<SkylineBot>>();
        readonly List<int> _living = new List<int>();
        readonly List<int> _spawnAt = new List<int>();
        readonly List<Bounds> _dark = new List<Bounds>();
        SafeSpot _safe;
        bool _beamsMade, _lightsDone, _registered, _doneOpened;
        int _maxWave, _spawned;
        Material _lit, _unlit, _sealed, _debrisShadow, _debrisRock;
        // Hold the Brand: the ring's glowing pieces (the builder's "Brand Circle"), their colour, the pulse's phase
        readonly List<Renderer> _brandRing = new List<Renderer>();
        MaterialPropertyBlock _brandBlock;
        Color _brandColour = new Color(1.1f, 0.55f, 2.4f);
        float _brandPhase;
        // The Collapse: one shadow (the 1 s telegraph on the floor) and one rock per debris drop, made at run time
        readonly List<Transform> _debrisShadows = new List<Transform>();
        readonly List<Transform> _debrisRocks = new List<Transform>();

        /// <summary>The brand ring's pulse: this many pulses a second at a standstill, plus this many more at full speed.</summary>
        public const float BrandPulseSlowHz = 0.6f, BrandPulseFastHz = 3.4f;

        // pooled (no garbage per step; docs/HELL-INTEGRATION.md): the room's clock, the objective, and the spawn counter (the
        // bots' seeds), so a Rewind past a spawn spawns the same bot again
        sealed class Snap
        {
            public float clock;
            public object objective;
            public bool running;
            public int spawned;
        }

        readonly HellSnapPool<Snap> _snaps = new HellSnapPool<Snap>();

        void Awake()
        {
            Def = HellWorld.Room(floor, kind);
            Objective = HellObjective.For(Def);
            Lava = GetComponentInChildren<HellLava>(true);
            if (Lava != null) Lava.Bind(this);
            GetComponentsInChildren(true, _platforms);
            foreach (var p in _platforms) p.Bind(this);
            var gate = Find(transform, Def.exitGateId);
            if (gate != null && Def.exitGateId.Length > 0) _exitGate = gate.gameObject;
            for (int i = 0; i < Def.stops.Count; i++)
            {
                var v = Find(transform, "Stop " + Def.stops[i].id);
                _stopViews.Add(v != null ? v.GetComponentInChildren<Renderer>() : null);
                var a = Find(transform, "Anchor " + i);
                _anchorViews.Add(a != null ? a.gameObject : null);
                var w = Find(transform, "Well Cap " + i);
                _wellCaps.Add(w != null ? w.GetComponent<Renderer>() : null);
                _wellBots.Add(new List<SkylineBot>());
                _living.Add(0);
            }
            var l = Find(transform, "Lantern");
            if (l != null) _lanternView = l.gameObject;
            _pedestal = Find(transform, "Lantern Pedestal");
            _altar = Find(transform, "Altar");
            foreach (var s in Def.spawns) _maxWave = Mathf.Max(_maxWave, s.wave);
            _lit = MakeUnlit(new Color(2.4f, 0.9f, 3.2f));
            // a pending sigil glows a readable violet (it was 0.35, 0.12, 0.45: a near-black disc under fog and tonemapping)
            _unlit = MakeUnlit(new Color(0.85f, 0.3f, 1.25f));
            _sealed = MakeUnlit(new Color(0.08f, 0.07f, 0.09f));
            var ring = Find(transform, "Brand Circle");
            if (ring != null)
            {
                ring.GetComponentsInChildren(true, _brandRing);
                if (_brandRing.Count > 0 && _brandRing[0].sharedMaterial != null && _brandRing[0].sharedMaterial.HasProperty("_BaseColor"))
                    _brandColour = _brandRing[0].sharedMaterial.GetColor("_BaseColor");
                _brandBlock = new MaterialPropertyBlock();
            }
            if (Def.debris.Count > 0) MakeDebrisViews();
            if (Objective is ChainAnchors) _chainViews = new HellChainViews(this, _anchorViews, _exitGate);
        }

        void OnEnable() { HellRewindRegistry.Register(this); }
        void OnDisable() { HellRewindRegistry.Unregister(this); }

        // ------------------------------------------------------------------ the run's calls

        /// <summary>The player dropped in: start the room.</summary>
        public void Begin(SkylinePlayer player)
        {
            _player = player;
            var b = Def.bounds;
            SkylineBot.SearchArea = Rect.MinMaxRect(b.min.X + 1f, b.min.Z + 1f, b.max.X - 1f, b.max.Z - 1f);
            SkylineBot.Nav = ArenaNav.Build(HellRoomLint.NavSolids(Def));
            if (!_beamsMade) MakeBeams();
            if (!_lightsDone)
            {
                _lightsDone = true;
                foreach (var li in Def.lights) HellDarkness.RegisterLight(U(li.position), li.radius);
            }
            HellShadowFire.SetRoomFlares(Def.lights);   // the lava flares (docs/HELL-SHADOW.md)
            if (!_registered)
            {
                _registered = true;
                _dark.Clear();
                foreach (var d in Def.darkZones)
                {
                    var bd = new Bounds();
                    bd.SetMinMax(U(d.min), U(d.max));
                    _dark.Add(bd);
                    HellDarkness.RegisterZone(bd);
                }
            }
            if (Objective is BrandHold brand && SkylineHud.Instance != null) SkylineHud.Instance.Objective.Bind(brand.Circle, null);
            ResetState();
            Running = true;
            Debug.Log("HELL_ROOM begin " + Def.name + " floor " + floor);
        }

        /// <summary>The player left (dropped out): stop ticking, let the darkness go, put the bots to sleep.</summary>
        public void End()
        {
            Running = false;
            HellShadowFire.SetRoomFlares(null);
            if (_registered)
            {
                foreach (var bd in _dark) HellDarkness.UnregisterZone(bd);
                _registered = false;
            }
            if (Objective is BrandHold && SkylineHud.Instance != null)
            {
                SkylineHud.Instance.Objective.Bind(null, null);
                SkylineHud.Instance.Objective.RingRate = 0f;
            }
            if (_bots != null) _bots.gameObject.SetActive(false);
            CarryLantern(false);
        }

        /// <summary>A death: the room starts over (the run director puts the player back at the entry).</summary>
        public void Restart()
        {
            ResetState();
            Running = true;
            Debug.Log("HELL_ROOM restart " + Def.name);
        }

        void ResetState()
        {
            Clock = 0f;
            _doneOpened = false;
            _spawned = 0;
            Objective.Reset();
            if (Lava != null) Lava.ResetLava();
            foreach (var p in _platforms) p.ResetPlatform();
            if (_bots != null) Destroy(_bots.gameObject);
            _bots = new GameObject("Bots").transform;
            _bots.SetParent(transform, false);
            foreach (var l in _wellBots) l.Clear();
            _safe = new SafeSpot(Def.entry, Def.entryYaw);
            CarryLantern(false);
            SyncViews();
            SpawnWave(0);
        }

        // ------------------------------------------------------------------ the step

        void FixedUpdate()
        {
            if (!Running || _player == null) return;
            float dt = Time.fixedDeltaTime;
            Clock += dt;
            var feet = N(_player.transform.position);
            var sim = _player.Movement != null ? _player.Movement.Sim : null;
            bool grounded = sim != null && sim.Grounded;

            switch (Objective)
            {
                case SigilChain sigils:
                    if (sigils.Tick(feet) >= 0) { SyncViews(); Debug.Log("HELL_ROOM sigil " + sigils.Next + "/" + sigils.Count); }
                    break;
                case LanternCarry lantern:
                    var ch = lantern.Tick(feet);
                    if (ch.HasValue) { CarryLantern(ch.Value == LanternState.Carried); SyncViews(); }
                    break;
                case BrandHold brand:
                    // charge from speed: the faster you move inside the ring, the faster it charges (HellBrandTuning)
                    float speed = sim != null ? sim.HorizontalSpeed : 0f;
                    int n = brand.Tick(dt, feet, speed);
                    for (int w = brand.WavesReleased - n + 1; w <= brand.WavesReleased; w++)
                        SpawnWave(_maxWave <= 0 ? 0 : ((w - 1) % _maxWave) + 1);
                    if (SkylineHud.Instance != null) SkylineHud.Instance.Objective.RingRate = brand.RateShare;
                    break;
                case CollapseEscape escape:
                    escape.Tick(feet);
                    // the wall imps, on the clock (each wave once)
                    int due = escape.TickWaves(dt);
                    for (int w = escape.WavesReleased - due + 1; w <= escape.WavesReleased; w++) SpawnWave(w);
                    // the debris lands on the room clock: a hit within its radius on the step it lands
                    foreach (var d in Def.debris)
                        if (HellDebris.Lands(d, Clock - dt, Clock) && HellDebris.Hits(d, feet))
                        {
                            _player.TakeAttack(HellDebris.Damage, "debris", U(d.position) + Vector3.up * 3f);
                            Debug.Log("HELL_ROOM debris hit at " + U(d.position));
                        }
                    break;
                case OozeWells wells:
                    for (int i = 0; i < _wellBots.Count; i++)
                    {
                        // count, don't drop, the dead: a Rewind can bring a dead ooze back (the same object); a bot it
                        // retired (spawned after its snapshot) is gone for good (docs/HELL-INTEGRATION.md)
                        _wellBots[i].RemoveAll(x => x == null || x.HellRetiredByRewind);
                        int alive = 0;
                        foreach (var x in _wellBots[i]) if (x.Alive) alive++;
                        _living[i] = alive;
                    }
                    int sealedNow = wells.Tick(dt, feet, _living, _spawnAt);
                    foreach (int i in _spawnAt)
                        if (i < Def.stopSpawns.Count)
                        {
                            var bot = Spawn(HellEnemyRole.Ooze, Def.stopSpawns[i], 180f);
                            if (bot != null) _wellBots[i].Add(bot);
                        }
                    if (sealedNow >= 0) SyncViews();
                    break;
            }

            if (Objective.Done && !_doneOpened)
            {
                _doneOpened = true;
                if (_exitGate != null) _exitGate.SetActive(false);
                Debug.Log("HELL_ROOM objective done " + Def.name + " at " + Clock.ToString("0.0") + " s");
                HellRunEvents.RaiseRoomObjectiveDone(floor, kind.ToString(), Clock);
            }

            bool onHazard = Lava != null && Lava.Touching;
            foreach (var p in _platforms) if (p.Under(feet, grounded)) { onHazard = true; break; }
            _safe.Tick(dt, feet, _player.Yaw, grounded && !onHazard);

            if (feet.Y < Def.bounds.min.Y - 3f) Sank("fell");
            // floorless lava (The Collapse's bridge): a fallen tile drops you in, and you sink at once (docs/HELL-ROOMS.md)
            else if (LavaContact.SunkIn(Def, feet) && !HellAbilities.LavaImmune) Sank("lava");
        }

        void Update()
        {
            if (_brandRing.Count > 0 && Objective is BrandHold brand) TickBrandGlow(brand);
            if (_debrisShadows.Count > 0) TickDebrisViews();
            if (_chainViews != null) _chainViews.Tick();
        }

        /// <summary>The brand ring's glow pulses faster the faster you charge it (the charge rate's cue; the HUD ring does the
        /// same), dims while you are outside it, and burns steady once it is set.</summary>
        void TickBrandGlow(BrandHold brand)
        {
            float k;
            if (brand.Done) k = 1.3f;
            else
            {
                float hz = BrandPulseSlowHz + BrandPulseFastHz * brand.RateShare;
                _brandPhase = (_brandPhase + Time.deltaTime * hz) % 1f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(_brandPhase * Mathf.PI * 2f);
                k = brand.Inside && Running ? 0.55f + 0.75f * pulse : 0.35f + 0.15f * pulse;
            }
            _brandBlock.SetColor("_BaseColor", _brandColour * k);
            _brandBlock.SetColor("_EmissionColor", _brandColour * k);
            foreach (var r in _brandRing) if (r != null) r.SetPropertyBlock(_brandBlock);
        }

        void MakeDebrisViews()
        {
            _debrisShadow = MakeUnlit(new Color(2.6f, 0.7f, 0.15f));
            _debrisRock = MakeUnlit(new Color(0.16f, 0.1f, 0.08f));
            var root = new GameObject("Debris").transform;
            root.SetParent(transform, false);
            foreach (var d in Def.debris)
            {
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shadow.name = "Debris Shadow";
                Destroy(shadow.GetComponent<Collider>());
                shadow.transform.SetParent(root, false);
                shadow.transform.position = U(d.position) + Vector3.up * 0.04f;
                shadow.transform.localScale = new Vector3(d.radius * 2f, 0.01f, d.radius * 2f);
                var sr = shadow.GetComponent<Renderer>();
                sr.sharedMaterial = _debrisShadow;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shadow.SetActive(false);
                _debrisShadows.Add(shadow.transform);
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Debris Rock";
                Destroy(rock.GetComponent<Collider>());
                rock.transform.SetParent(root, false);
                rock.transform.localScale = Vector3.one * (d.radius * 1.1f);
                rock.transform.rotation = Quaternion.Euler(23f, 41f * _debrisRocks.Count, 17f);
                rock.GetComponent<Renderer>().sharedMaterial = _debrisRock;
                rock.SetActive(false);
                _debrisRocks.Add(rock.transform);
            }
        }

        /// <summary>The debris as the room clock has it (so a Rewind of the clock shows it rewound): the shadow grows and burns
        /// brighter over its 1 s warning, the rock falls in its last moments, the rubble lies a moment and goes.</summary>
        void TickDebrisViews()
        {
            for (int i = 0; i < Def.debris.Count && i < _debrisShadows.Count; i++)
            {
                var d = Def.debris[i];
                float tel = Running ? HellDebris.Telegraph(d, Clock) : -1f;
                var s = _debrisShadows[i];
                bool showShadow = tel >= 0f;
                if (s.gameObject.activeSelf != showShadow) s.gameObject.SetActive(showShadow);
                if (showShadow)
                {
                    float w = d.radius * 2f * (0.55f + 0.45f * tel);
                    s.localScale = new Vector3(w, 0.01f, w);
                }
                var rock = _debrisRocks[i];
                float h = Running ? HellDebris.RockHeight(d, Clock) : -1f;
                bool rubble = Running && HellDebris.Rubble(d, Clock);
                bool showRock = h >= 0f || rubble;
                if (rock.gameObject.activeSelf != showRock) rock.gameObject.SetActive(showRock);
                if (showRock)
                {
                    float y = h >= 0f ? h + d.radius * 0.55f : d.radius * 0.25f;
                    rock.position = U(d.position) + Vector3.up * y;
                    rock.localScale = h >= 0f ? Vector3.one * (d.radius * 1.1f) : new Vector3(d.radius * 1.6f, d.radius * 0.4f, d.radius * 1.6f);
                }
            }
        }

        /// <summary>A sink in lava (or a fall out of the room): back to the last safe spot; the lantern goes home. In The
        /// Collapse the floor behind is gone, so the room starts over instead.</summary>
        float _cheatSankAt = -10f;

        public void Sank(string why)
        {
            if (_player == null) return;
            Debug.Log("HELL_ROOM sank (" + why + ") in " + Def.name);
            if (Objective is LanternCarry lantern && lantern.Drop()) { CarryLantern(false); SyncViews(); }
            // CAPTURE-ONLY -hellInvulnerable (docs/HELL-CAPTURE.md): The Collapse too goes back to the safe spot, unless that
            // spot has fallen as well (a second sink within 1.5 s): then it starts over as usual
            bool cheatSafe = kind == HellRoomKind.Collapse && HellCaptureCheats.Current.invulnerable && Time.time - _cheatSankAt > 1.5f;
            _cheatSankAt = Time.time;
            if (kind == HellRoomKind.Collapse && !cheatSafe)
            {
                Restart();
                _player.Movement.Teleport(U(Def.entry), Def.entryYaw, Vector3.zero);
                _player.SetAim(Def.entryYaw, 0f);
                HellRunEvents.RaiseRoomRestarted();
                return;
            }
            _player.Movement.Teleport(U(_safe.Spot), _safe.Yaw, Vector3.zero);
            _player.SetAim(_safe.Yaw, 0f);
        }

        /// <summary>A shot on chain anchor <paramref name="i"/> (from <see cref="HellAnchor"/>).</summary>
        public ChainAnchors.HitResult HitAnchor(int i, float damage)
        {
            if (!(Objective is ChainAnchors chains) || _player == null) return ChainAnchors.HitResult.Deflected;
            var r = chains.Hit(i, damage, N(_player.EyePosition));
            if (r == ChainAnchors.HitResult.Broke) { SyncViews(); Debug.Log("HELL_ROOM anchor " + i + " broken"); }
            return r;
        }

        /// <summary>The feet are in this room's drop hole.</summary>
        public bool InDrop(Vector3 feet) => HellRoomLint.Contains(Def.dropZone, N(feet));

        // ------------------------------------------------------------------ enemies

        void SpawnWave(int wave)
        {
            if (_player == null) return;
            foreach (var s in Def.spawns)
                if (s.wave == wave) Spawn(s.role, s.position, s.yawDeg);
        }

        SkylineBot Spawn(HellEnemyRole role, NVec3 at, float yaw)
        {
            _spawned++;
            ulong seed = (ulong)(floor * 100000 + (int)kind * 1000 + _spawned);
            var bot = SkylineBot.Spawn(HellRoomEnemies.IdFor(role), U(at), yaw, seed, _player, _player.Writ, _bots);
            if (bot != null) Debug.Log("HELL_ROOM spawn " + role + " (" + HellRoomEnemies.IdFor(role) + ") at " + U(at));
            return bot;
        }

        void MakeBeams()
        {
            _beamsMade = true;
            foreach (var h in Def.hazards)
            {
                if (h.kind != HellHazardKind.Beam) continue;
                var b = h.box;
                float cy = (b.min.Y + b.max.Y) * 0.5f;
                bool alongX = b.max.X - b.min.X >= b.max.Z - b.min.Z;
                var c = (b.min + b.max) * 0.5f;
                var from = alongX ? new Vector3(b.min.X, cy, c.Z) : new Vector3(c.X, cy, b.min.Z);
                var to = alongX ? new Vector3(b.max.X, cy, c.Z) : new Vector3(c.X, cy, b.max.Z);
                HellBeamLane.Create(from, to, h.onSeconds + h.offSeconds, h.phase, transform, _player);
            }
        }

        // ------------------------------------------------------------------ the lantern and the views

        void CarryLantern(bool carrying)
        {
            HellRoomFlags.Carry(carrying);
            if (_player != null) _player.MoveScale = carrying ? Def.tuning.carryMoveScale : 1f;
        }

        void SyncViews()
        {
            switch (Objective)
            {
                case SigilChain s:
                    for (int i = 0; i < _stopViews.Count; i++)
                        if (_stopViews[i] != null) _stopViews[i].sharedMaterial = i < s.Next ? _lit : (i == s.Next ? _unlit : _unlit);
                    break;
                case ChainAnchors c:
                    for (int i = 0; i < _anchorViews.Count; i++)
                        if (_anchorViews[i] != null) _anchorViews[i].SetActive(!c.Broken(i));
                    break;
                case OozeWells w:
                    for (int i = 0; i < _wellCaps.Count; i++)
                        if (_wellCaps[i] != null && w.Sealed(i)) _wellCaps[i].sharedMaterial = _sealed;
                    break;
                case LanternCarry l:
                    if (_lanternView == null) break;
                    var t = _lanternView.transform;
                    if (l.State == LanternState.Carried && _player != null && _player.Camera != null)
                    {
                        t.SetParent(_player.Camera.transform, false);
                        t.localPosition = new Vector3(0.45f, -0.35f, 0.9f);
                    }
                    else
                    {
                        var home = l.State == LanternState.Placed ? _altar : _pedestal;
                        t.SetParent(transform, true);
                        if (home != null) t.position = home.position + Vector3.up * 1.4f;
                    }
                    break;
            }
            if (_exitGate != null) _exitGate.SetActive(!Objective.Done);
        }

        // ------------------------------------------------------------------ rewind

        public object Snapshot()
        {
            var s = _snaps.Next();
            s.clock = Clock;
            s.objective = Objective.Snapshot();
            s.running = Running;
            s.spawned = _spawned;
            return s;
        }

        public void Restore(object snapshot)
        {
            var s = (Snap)snapshot;
            Clock = s.clock;
            Running = s.running;
            _spawned = s.spawned;
            Objective.Restore(s.objective);
            _doneOpened = Objective.Done;
            if (Objective is LanternCarry l) CarryLantern(l.Carrying);
            SyncViews();
        }

        // ------------------------------------------------------------------ helpers

        static Material MakeUnlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh != null ? sh : Shader.Find("Sprites/Default")) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        public static Transform Find(Transform root, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        public static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
        public static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
    }
}
