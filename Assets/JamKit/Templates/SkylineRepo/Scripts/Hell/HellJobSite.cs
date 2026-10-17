using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// One job at run time, in its room's pocket (docs/HELL-JOBS.md). Made by <see cref="HellJobDirector"/> when the room is
    /// entered: builds the job's figures and props (primitives, the enemies' own bodies for the imps and the demon), feeds the
    /// pure <see cref="HellJob"/> every fixed step (reach, the pocket, sight, the keeper), takes the shots, spawns the guards
    /// and the squatter's friends as ordinary bots, carries the hourglass, opens the cage. Rewindable: the job's state and the
    /// keeper's health (the figures follow from them; a done job stays done).
    /// </summary>
    public sealed class HellJobSite : MonoBehaviour, IHellRewindable
    {
        public HellJob Job { get; private set; }
        public HellJobPocket Pocket { get; private set; }
        public HellRoom Room { get; private set; }
        /// <summary>Every event the job raised (the director shows them).</summary>
        public event Action<HellJobEvent> Raised;

        SkylinePlayer _player;
        Transform _figure, _keeper, _bots, _carryView, _rift, _seal, _beacon;
        readonly List<GameObject> _chains = new List<GameObject>();
        float _keeperHp;
        int _spawned;
        Vector3 _lastFeet;
        bool _carryShown;

        struct Snap
        {
            public object job;
            public float keeperHp;
        }

        public void Bind(HellRoom room, HellJobDef def, HellJobPocket pocket, SkylinePlayer player)
        {
            Room = room;
            Pocket = pocket;
            Job = new HellJob(def);
            _player = player;
            BuildProps();
            ResetFigures();
            Job.Begin();
            _lastFeet = player != null ? player.transform.position : Vector3.zero;
        }

        void OnEnable() { HellRewindRegistry.Register(this); }

        void OnDisable()
        {
            HellRewindRegistry.Unregister(this);
            Carry(false);
        }

        /// <summary>A death restarted the room: the job starts over too.</summary>
        public void Restart()
        {
            Job.Restart();
            ResetFigures();
        }

        /// <summary>The room is being left: settle the job (an unfinished one fails; the hourglass carried out is done).</summary>
        public HellJobEvent Leave(bool throughDrop)
        {
            var ev = Job.Leave(throughDrop);
            Carry(false);
            Raise(ev);
            return ev;
        }

        void ResetFigures()
        {
            _keeperHp = Job.def.keeperHealth;
            if (_bots != null) Destroy(_bots.gameObject);
            _bots = new GameObject("Job Bots").transform;
            _bots.SetParent(transform, false);
            _spawned = 0;
            Carry(false);
            SyncFigures();
        }

        // ------------------------------------------------------------------ the step

        void FixedUpdate()
        {
            if (Job == null || Room == null || !Room.Running || _player == null || Job.Settled) { SyncFigures(); return; }
            var feet = _player.transform.position;
            // a sink or a fall puts you back at a safe spot: the hourglass goes home
            if (Job.Carried && (feet - _lastFeet).sqrMagnitude > 64f) Raise(Job.Drop());
            _lastFeet = feet;

            var spot = SpotNow();
            var input = new HellJobInput
            {
                inReach = Flat(feet - spot) <= Job.def.reach && Mathf.Abs(feet.y - spot.y) < 2.6f,
                inPocket = HellRoomLint.Contains(Pocket.area, HellRoom.N(feet + Vector3.up * 0.5f)),
                seen = Seen(),
                keeperAlive = _keeperHp > 0f,
            };
            var ev = Job.Tick(Time.fixedDeltaTime, input);
            if ((ev & HellJobEvent.SpawnDue) != 0 && Pocket.points.Count > 0) SpawnBot(Pocket.points[(Job.State.spawned - 1) % Pocket.points.Count]);
            Raise(ev);
            SyncFigures();
        }

        /// <summary>Could the target notice the player now: within its radius, in its sight, not hidden by darkness (the
        /// Poacher), and for the Poacher only while he glances round (the machine checks the glance).</summary>
        bool Seen()
        {
            if (_figure == null || !_figure.gameObject.activeInHierarchy) return false;
            var eye = _player.EyePosition;
            var from = _figure.position + Vector3.up * (Job.def.id == HellJobCatalog.Poacher ? 2.3f : 0.4f);
            if ((eye - from).sqrMagnitude > Job.def.alertRadius * Job.def.alertRadius) return false;
            if (Job.def.darkHides && HellDarkness.IsDark(_player.transform.position + Vector3.up * 1.0f)) return false;
            var dir = eye - from;
            if (Physics.Raycast(from, dir.normalized, out var hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(_player.transform) && !hit.collider.transform.IsChildOf(_figure)) return false;
            return true;
        }

        /// <summary>A shot on one of the job's parts.</summary>
        public SkylineHitResult Hit(HellJobTarget t, float damage)
        {
            if (Job == null || Job.Settled || _player == null) return default(SkylineHitResult);
            float dist = Vector3.Distance(_player.EyePosition, t.transform.position);
            HellJobEvent ev;
            switch (t.part)
            {
                case HellJobTarget.Part.Keeper:
                    if (_keeperHp <= 0f) return default(SkylineHitResult);
                    _keeperHp -= damage;
                    ev = Job.Alert() | HellJobEvent.Damaged;
                    if (_keeperHp <= 0f) Debug.Log("HELL_JOB " + Job.def.id + " keeper killed");
                    Raise(ev);
                    SyncFigures();
                    return new SkylineHitResult { hit = true, killed = _keeperHp <= 0f };
                case HellJobTarget.Part.Chain:
                    ev = Job.HitChain(t.index, damage, dist);
                    break;
                default:
                    ev = Job.Hit(damage, dist);
                    break;
            }
            Raise(ev);
            SyncFigures();
            bool landed = (ev & HellJobEvent.Damaged) != 0;
            bool killed = (ev & HellJobEvent.Done) != 0 || (t.part == HellJobTarget.Part.Chain && Job.ChainBroken(t.index));
            return new SkylineHitResult { hit = landed, killed = landed && killed };
        }

        void Raise(HellJobEvent ev)
        {
            if (ev == HellJobEvent.None) return;
            if ((ev & HellJobEvent.Alerted) != 0) OnAlerted();
            if ((ev & HellJobEvent.PickedUp) != 0) Carry(true);
            if ((ev & HellJobEvent.Dropped) != 0) Carry(false);
            if ((ev & (HellJobEvent.Done | HellJobEvent.Failed)) != 0) Carry(false);
            if ((ev & HellJobEvent.Done) != 0) OpenOnDone();
            HellAbilityAudio.Play(Cue(ev), _figure != null ? _figure.position : transform.position);
            Raised?.Invoke(ev);
        }

        static string Cue(HellJobEvent ev)
        {
            if ((ev & HellJobEvent.Done) != 0) return "job_done";
            if ((ev & HellJobEvent.Failed) != 0) return "job_failed";
            if ((ev & HellJobEvent.Alerted) != 0) return "job_alerted";
            if ((ev & HellJobEvent.Undone) != 0) return "job_undone";
            if ((ev & HellJobEvent.Reforged) != 0) return "job_reforged";
            if ((ev & HellJobEvent.PickedUp) != 0) return "job_pickup";
            return "job_hit";
        }

        /// <summary>The target noticed you: the Viscount's and the clerk's guards come.</summary>
        void OnAlerted()
        {
            var id = Job.def.id;
            if (id == HellJobCatalog.Viscount && Pocket.path.Count > 2)
            {
                SpawnBot(HellRoom.U(Pocket.path[1]) + Vector3.up * 1.5f);
                SpawnBot(HellRoom.U(Pocket.path[2]) + Vector3.up * 1.5f);
            }
            else if (id == HellJobCatalog.Ledger)
            {
                SpawnBot(HellRoom.U(Pocket.keeper) + Vector3.up * 1.5f);
                SpawnBot(HellRoom.U(Pocket.stand) + Vector3.up * 3.5f);
            }
        }

        void SpawnBot(NVec3 at) => SpawnBot(HellRoom.U(at));

        void SpawnBot(Vector3 at)
        {
            _spawned++;
            ulong seed = (ulong)(777000 + Room.floor * 1000 + _spawned);
            var bot = SkylineBot.Spawn(HellRoomEnemies.IdFor(HellEnemyRole.Imp), at, 180f, seed, _player, _player.Writ, _bots);
            if (bot != null) Debug.Log("HELL_JOB " + Job.def.id + " spawn imp at " + at);
        }

        void OpenOnDone()
        {
            foreach (var id in Pocket.openOnDone)
            {
                var t = HellRoom.Find(Room.transform, id);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        void Carry(bool on)
        {
            if (on == _carryShown) return;
            _carryShown = on;
            HellRoomFlags.Carry(on);
            if (_player != null) _player.MoveScale = on ? 0.85f : 1f;
        }

        // ------------------------------------------------------------------ where things are

        /// <summary>The job's spot now: the Viscount along his flee path, the thief on his perch, the Bather drifting.</summary>
        Vector3 SpotNow()
        {
            var d = Job.def;
            switch (d.id)
            {
                case HellJobCatalog.Viscount:
                    return Along(Pocket.path, Job.Progress01);
                case HellJobCatalog.SecondsThief:
                    return (Pocket.points.Count > 0 ? HellRoom.U(Pocket.points[Job.Perch % Pocket.points.Count]) : HellRoom.U(Pocket.spot)) + Vector3.up * 1.3f;   // hovering over the perch
                case HellJobCatalog.Bather:
                    float a = Job.State.clock * Mathf.PI * 2f / 8f;
                    return HellRoom.U(Pocket.spot) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.5f;
                default:
                    return HellRoom.U(Pocket.spot);
            }
        }

        static Vector3 Along(List<NVec3> path, float t)
        {
            if (path.Count == 0) return Vector3.zero;
            float total = 0f;
            for (int i = 1; i < path.Count; i++) total += NVec3.Distance(path[i - 1], path[i]);
            float want = Mathf.Clamp01(t) * total;
            for (int i = 1; i < path.Count; i++)
            {
                float l = NVec3.Distance(path[i - 1], path[i]);
                if (want <= l || i == path.Count - 1) return Vector3.Lerp(HellRoom.U(path[i - 1]), HellRoom.U(path[i]), l > 0f ? Mathf.Clamp01(want / l) : 1f);
                want -= l;
            }
            return HellRoom.U(path[path.Count - 1]);
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        void SyncFigures()
        {
            if (Job == null) return;
            var st = Job.Status;
            bool settled = Job.Settled;
            if (_figure != null)
            {
                bool show = st != HellJobStatus.Done && st != HellJobStatus.Failed;
                if (Job.def.verb == HellJobVerb.Retrieve) show = !Job.Carried && st != HellJobStatus.Done;
                if (Job.def.verb == HellJobVerb.Hold || Job.def.verb == HellJobVerb.Free) show = true;   // the seal's door, the whelp
                _figure.gameObject.SetActive(show);
                if (show && Job.def.verb != HellJobVerb.Free && Job.def.verb != HellJobVerb.Hold)
                {
                    var p = SpotNow();
                    float bob = Job.def.id == HellJobCatalog.Viscount || Job.def.id == HellJobCatalog.SecondsThief ? Mathf.Sin(Time.time * 2.2f) * 0.12f : 0f;
                    _figure.position = p + Vector3.up * bob;
                    if (Job.def.id == HellJobCatalog.Poacher && _player != null)
                    {
                        // back to the door at his rack; he turns round while he glances, and faces you once he knows
                        var look = Job.Glancing || Job.Alerted ? _player.transform.position - p : HellRoom.U(Pocket.prop) - p;
                        look.y = 0f;
                        if (look.sqrMagnitude > 0.01f) _figure.rotation = Quaternion.Slerp(_figure.rotation, Quaternion.LookRotation(look), 0.25f);
                    }
                    else if (Job.def.verb == HellJobVerb.Reap)
                    {
                        float k = 1f - 0.6f * Job.Progress01;   // the soul thins as it is reaped
                        _figure.localScale = new Vector3(k, k, k);
                    }
                }
                if (Job.def.id == HellJobCatalog.Whelp && st == HellJobStatus.Done) _figure.gameObject.SetActive(false);
            }
            if (_keeper != null) _keeper.gameObject.SetActive(_keeperHp > 0f && !settled);
            for (int i = 0; i < _chains.Count; i++) if (_chains[i] != null) _chains[i].SetActive(!Job.ChainBroken(i) && st != HellJobStatus.Done);
            if (_rift != null)
            {
                bool on = Job.Alerted && Job.def.verb == HellJobVerb.Escape;
                _rift.gameObject.SetActive(on);
                if (on) _rift.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.4f, Job.Progress01);
            }
            if (_carryView != null)
            {
                bool carried = Job.Carried && _player != null && _player.Camera != null;
                _carryView.gameObject.SetActive(carried);
            }
            if (_seal != null)
            {
                var r = _seal.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = st == HellJobStatus.Done ? _sealDone : (Job.Progress01 > 0f ? _sealHot : _sealCold);
            }
            if (_beacon != null) _beacon.gameObject.SetActive(!settled);
        }

        // ------------------------------------------------------------------ the props (primitives; placeholders for the art pass)

        Material _sealCold, _sealHot, _sealDone;

        void BuildProps()
        {
            var d = Job.def;
            var soul = HellLook.Glow(new Color(0.6f, 1.6f, 2.2f));
            var gold = HellLook.Glow(new Color(2.6f, 1.8f, 0.4f));
            var robe = HellLook.Lit(new Color(0.45f, 0.04f, 0.08f));
            var dark = HellLook.Lit(HellLook.Char);
            var paper = HellLook.Lit(new Color(0.75f, 0.68f, 0.5f));
            var iron = HellLook.Lit(HellLook.Steel);
            var chainMat = HellLook.HostileGlow(new Color(2.2f, 0.4f, 0.15f));

            // the beacon at the door: a tall violet thread, seen from across the room (the HUD marker points here too)
            _beacon = Prim(PrimitiveType.Cylinder, "Job Beacon", transform, HellRoom.U(Pocket.door) + Vector3.up * 6f, new Vector3(0.12f, 6f, 0.12f),
                HellLook.Glow(new Color(1.6f, 0.5f, 2.6f)), false).transform;

            switch (d.id)
            {
                case HellJobCatalog.Viscount:
                {
                    _figure = Rig("The Viscount");
                    HellBodies.FireImp(Rig("Body", _figure));
                    Prim(PrimitiveType.Cylinder, "Crown", _figure, _figure.position + new Vector3(0f, 0.68f, 0f), new Vector3(0.36f, 0.06f, 0.36f), gold, false);
                    Prim(PrimitiveType.Cube, "Robe", _figure, _figure.position + new Vector3(0f, -0.15f, -0.25f), new Vector3(0.7f, 0.7f, 0.08f), robe, false);
                    // the palanquin of chained souls under him
                    Prim(PrimitiveType.Cube, "Palanquin", _figure, _figure.position + new Vector3(0f, -0.7f, 0f), new Vector3(1.6f, 0.15f, 1.2f), gold, false);
                    for (int i = 0; i < 4; i++)
                        Prim(PrimitiveType.Capsule, "Bearer", _figure, _figure.position + new Vector3(i < 2 ? -0.6f : 0.6f, -1.3f, i % 2 == 0 ? -0.45f : 0.45f), new Vector3(0.3f, 0.5f, 0.3f), soul, false);
                    Hook(_figure, HellJobTarget.Part.Target, 0);
                    var door = Prim(PrimitiveType.Cube, "Viscount's Door", transform, HellRoom.U(Pocket.prop) + Vector3.up * 1.6f, new Vector3(2.2f, 3.2f, 0.3f), gold, false);
                    door.transform.rotation = Quaternion.LookRotation(Flatten(HellRoom.U(Pocket.door) - HellRoom.U(Pocket.prop)));
                    break;
                }
                case HellJobCatalog.OverdueSoul:
                case HellJobCatalog.Bather:
                {
                    _figure = Rig(d.id == HellJobCatalog.Bather ? "The Bather" : "Albrecht Huss");
                    Prim(PrimitiveType.Capsule, "Soul", _figure, _figure.position + Vector3.up * 0.9f, new Vector3(0.6f, 0.9f, 0.6f), soul, false);
                    Prim(PrimitiveType.Sphere, "Head", _figure, _figure.position + Vector3.up * 1.95f, Vector3.one * 0.45f, soul, false);
                    break;
                }
                case HellJobCatalog.Hourglass:
                {
                    var spot = HellRoom.U(Pocket.spot);
                    Prim(PrimitiveType.Cube, "Pedestal", transform, spot - Vector3.up * 0.55f, new Vector3(0.6f, 1.1f, 0.6f), dark, true);
                    _figure = Rig("The Hourglass");
                    BuildHourglass(_figure, gold, soul);
                    _carryView = Rig("Carried Hourglass");
                    BuildHourglass(_carryView, gold, soul);
                    if (_player != null && _player.Camera != null)
                    {
                        _carryView.SetParent(_player.Camera.transform, false);
                        _carryView.localPosition = new Vector3(0.42f, -0.38f, 0.85f);
                        _carryView.localScale = Vector3.one * 0.6f;
                    }
                    break;
                }
                case HellJobCatalog.Ledger:
                {
                    _figure = Rig("The Ledger");
                    Prim(PrimitiveType.Cube, "Book", _figure, _figure.position, new Vector3(0.7f, 0.18f, 0.5f), paper, true);
                    Prim(PrimitiveType.Cube, "Cover", _figure, _figure.position + Vector3.up * 0.1f, new Vector3(0.74f, 0.04f, 0.54f), robe, false);
                    Hook(_figure, HellJobTarget.Part.Target, 0);
                    _keeper = Rig("The Clerk", HellRoom.U(Pocket.keeper));
                    HellBodies.FireImp(Rig("Body", _keeper));
                    Prim(PrimitiveType.Cylinder, "Hat", _keeper, _keeper.position + new Vector3(0f, 0.85f, 0f), new Vector3(0.3f, 0.25f, 0.3f), dark, false);
                    Prim(PrimitiveType.Cube, "Stamp", _keeper, _keeper.position + new Vector3(0.35f, -0.2f, 0.25f), new Vector3(0.15f, 0.2f, 0.15f), robe, false);
                    Hook(_keeper, HellJobTarget.Part.Keeper, 0);
                    break;
                }
                case HellJobCatalog.Poacher:
                {
                    _figure = Rig("The Poacher");
                    HellBodies.ScytheDemon(Rig("Body", _figure));
                    Hook(_figure, HellJobTarget.Part.Target, 0);
                    var rack = HellRoom.U(Pocket.prop);
                    for (int i = -3; i <= 3; i++)
                        Prim(PrimitiveType.Cylinder, "Soul Jar", transform, rack + new Vector3(i * 0.9f, 2.7f, 0f), new Vector3(0.3f, 0.25f, 0.3f), soul, false);
                    _rift = Prim(PrimitiveType.Cylinder, "Rift", transform, HellRoom.U(Pocket.prop) + Vector3.up * 1.6f + Flatten(HellRoom.U(Pocket.door) - rack).normalized * 1.2f,
                        new Vector3(1f, 0.02f, 1f), HellLook.Glow(new Color(1.4f, 0.2f, 2.4f)), false).transform;
                    _rift.rotation = Quaternion.LookRotation(Vector3.up, Flatten(HellRoom.U(Pocket.door) - rack));
                    break;
                }
                case HellJobCatalog.Whelp:
                {
                    _figure = Rig("The Whelp");
                    var p = _figure.position;
                    Prim(PrimitiveType.Capsule, "Body", _figure, p + Vector3.up * 0.5f, new Vector3(0.5f, 0.45f, 0.5f), dark, false).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Prim(PrimitiveType.Sphere, "Head", _figure, p + new Vector3(0f, 0.75f, 0.45f), Vector3.one * 0.4f, dark, false);
                    Prim(PrimitiveType.Sphere, "Eyes", _figure, p + new Vector3(0f, 0.8f, 0.64f), new Vector3(0.25f, 0.06f, 0.06f), HellLook.Glow(HellLook.EyeHot), false);
                    for (int i = 0; i < Pocket.points.Count; i++)
                    {
                        var at = HellRoom.U(Pocket.points[i]);
                        var anchor = Prim(PrimitiveType.Cube, "Chain Anchor " + i, transform, at, Vector3.one * 0.9f, iron, true);
                        int links = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(at, p + Vector3.up * 0.6f) / 0.8f));
                        for (int k = 1; k < links; k++)
                            Prim(PrimitiveType.Cube, "Link", anchor.transform, Vector3.Lerp(at, p + Vector3.up * 0.6f, k / (float)links), Vector3.one * 0.16f, chainMat, false);
                        var t = anchor.AddComponent<HellJobTarget>();
                        t.site = this; t.part = HellJobTarget.Part.Chain; t.index = i;
                        t.HookColliders();
                        _chains.Add(anchor);
                    }
                    _keeper = Rig("The Smith", HellRoom.U(Pocket.keeper));
                    HellBodies.FireImp(Rig("Body", _keeper));
                    Prim(PrimitiveType.Cube, "Hammer", _keeper, _keeper.position + new Vector3(0.4f, 0f, 0.2f), new Vector3(0.12f, 0.5f, 0.12f), iron, false);
                    Hook(_keeper, HellJobTarget.Part.Keeper, 0);
                    break;
                }
                case HellJobCatalog.SecondsThief:
                {
                    _figure = Rig("The Seconds Thief");
                    HellBodies.FireImp(Rig("Body", _figure));
                    Prim(PrimitiveType.Cylinder, "Stolen Dial", _figure, _figure.position + new Vector3(0f, 0.2f, -0.35f), new Vector3(0.6f, 0.03f, 0.6f), soul, false)
                        .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Hook(_figure, HellJobTarget.Part.Target, 0);
                    break;
                }
                case HellJobCatalog.Notice:
                {
                    var door = Prim(PrimitiveType.Cube, "Squatter's Door", transform, HellRoom.U(Pocket.prop) + Vector3.up * 1.4f, new Vector3(1.8f, 2.8f, 0.25f), robe, false);
                    door.transform.rotation = Quaternion.LookRotation(Flatten(HellRoom.U(Pocket.door) - HellRoom.U(Pocket.prop)));
                    _figure = door.transform;
                    _sealCold = HellLook.Glow(new Color(0.5f, 0.15f, 0.6f));
                    _sealHot = HellLook.Glow(new Color(2.4f, 0.6f, 3f));
                    _sealDone = HellLook.Lit(HellLook.Char);
                    var seal = Prim(PrimitiveType.Cylinder, "Seal", transform, HellRoom.U(Pocket.spot) + Vector3.up * 0.03f, new Vector3(d.reach * 2f, 0.01f, d.reach * 2f), _sealCold, false);
                    _seal = seal.transform;
                    break;
                }
            }
        }

        static void BuildHourglass(Transform root, Material frame, Material sand)
        {
            var p = root.position;
            Prim(PrimitiveType.Cylinder, "Top", root, p + Vector3.up * 0.32f, new Vector3(0.36f, 0.03f, 0.36f), frame, false);
            Prim(PrimitiveType.Cylinder, "Base", root, p - Vector3.up * 0.32f, new Vector3(0.36f, 0.03f, 0.36f), frame, false);
            Prim(PrimitiveType.Sphere, "Upper", root, p + Vector3.up * 0.15f, new Vector3(0.26f, 0.3f, 0.26f), sand, false);
            Prim(PrimitiveType.Sphere, "Lower", root, p - Vector3.up * 0.15f, new Vector3(0.26f, 0.3f, 0.26f), sand, false);
        }

        Transform Rig(string name) => Rig(name, SpotNowSafe());

        Vector3 SpotNowSafe() => Pocket != null ? HellRoom.U(Pocket.spot) : transform.position;

        Transform Rig(string name, Vector3 at)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            t.position = at;
            return t;
        }

        static Transform Rig(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        void Hook(Transform root, HellJobTarget.Part part, int index)
        {
            var t = root.gameObject.AddComponent<HellJobTarget>();
            t.site = this;
            t.part = part;
            t.index = index;
            t.HookColliders();
        }

        static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v : Vector3.forward;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        // ------------------------------------------------------------------ rewind

        public object Snapshot() => new Snap { job = Job != null ? Job.Snapshot() : null, keeperHp = _keeperHp };

        public void Restore(object snapshot)
        {
            if (Job == null || Job.Status == HellJobStatus.Done) return;
            var s = (Snap)snapshot;
            Job.Restore(s.job);
            _keeperHp = s.keeperHp;
            Carry(Job.Carried);
            SyncFigures();
        }
    }
}
