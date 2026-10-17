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
    /// exit gate when the objective is done. <see cref="Restart"/> after a death. Rewindable: the clock, the objective and the
    /// gate (the hazards and the bots are rewindables of their own).
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
        readonly List<Renderer> _wellCaps = new List<Renderer>();
        readonly List<List<SkylineBot>> _wellBots = new List<List<SkylineBot>>();
        readonly List<int> _living = new List<int>();
        readonly List<int> _spawnAt = new List<int>();
        readonly List<Bounds> _dark = new List<Bounds>();
        SafeSpot _safe;
        bool _beamsMade, _lightsDone, _registered, _doneOpened;
        int _maxWave, _spawned;
        Material _lit, _unlit, _sealed;

        struct Snap
        {
            public float clock;
            public object objective;
            public bool running;
        }

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
            _unlit = MakeUnlit(new Color(0.35f, 0.12f, 0.45f));
            _sealed = MakeUnlit(new Color(0.08f, 0.07f, 0.09f));
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
            if (_registered)
            {
                foreach (var bd in _dark) HellDarkness.UnregisterZone(bd);
                _registered = false;
            }
            if (Objective is BrandHold && SkylineHud.Instance != null) SkylineHud.Instance.Objective.Bind(null, null);
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
                    int n = brand.Tick(dt, feet);
                    for (int w = brand.WavesReleased - n + 1; w <= brand.WavesReleased; w++)
                        SpawnWave(_maxWave <= 0 ? 0 : ((w - 1) % _maxWave) + 1);
                    break;
                case CollapseEscape escape:
                    escape.Tick(feet);
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
            }

            bool onHazard = Lava != null && Lava.Touching;
            foreach (var p in _platforms) if (p.Under(feet, grounded)) { onHazard = true; break; }
            _safe.Tick(dt, feet, _player.Yaw, grounded && !onHazard);

            if (feet.Y < Def.bounds.min.Y - 3f) Sank("fell");
        }

        /// <summary>A sink in lava (or a fall out of the room): back to the last safe spot; the lantern goes home. In The
        /// Collapse the floor behind is gone, so the room starts over instead.</summary>
        public void Sank(string why)
        {
            if (_player == null) return;
            Debug.Log("HELL_ROOM sank (" + why + ") in " + Def.name);
            if (Objective is LanternCarry lantern && lantern.Drop()) { CarryLantern(false); SyncViews(); }
            if (kind == HellRoomKind.Collapse)
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

        public object Snapshot() => new Snap { clock = Clock, objective = Objective.Snapshot(), running = Running };

        public void Restore(object snapshot)
        {
            var s = (Snap)snapshot;
            Clock = s.clock;
            Running = s.running;
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
