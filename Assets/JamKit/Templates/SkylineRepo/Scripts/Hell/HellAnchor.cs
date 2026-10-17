using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;

namespace JamKit.SkylineRepo
{
    /// <summary>A chain anchor of Break the Chains: shootable (a <see cref="SkylineHitZone"/> on its collider points here). A shot
    /// bites only from within the ward (12 m); from farther it is deflected (no hit marker). The room keeps the state; the look
    /// is <see cref="HellChainViews"/>.</summary>
    public sealed class HellAnchor : MonoBehaviour, ISkylineHittable
    {
        public HellRoom room;
        public int index;

        public SkylineHitResult TakeHit(in SkylineHitInfo hit)
        {
            if (room == null) return default(SkylineHitResult);
            var r = room.HitAnchor(index, hit.damage);
            return new SkylineHitResult
            {
                hit = r == ChainAnchors.HitResult.Damaged || r == ChainAnchors.HitResult.Broke,
                killed = r == ChainAnchors.HitResult.Broke,
            };
        }
    }

    /// <summary>
    /// The look of Break the Chains' anchors (the builder's red boxes and box links are hidden; the boxes' colliders stay the
    /// hit zones). Each anchor is a massive iron chain from a bracket on the wall to the exit gate, held by one straining link
    /// at the anchor's spot: it glows ember to white-hot, cracked and spitting sparks, inside a faint rune ring (the ward: it
    /// burns bright while you stand inside it, where a shot bites). A hit pulses the glow; the cracks widen and the sparks
    /// thicken as its health falls. When it breaks the link shatters (glowing pieces fly, a spark burst, a light flash), the
    /// chain whips and falls slack into a heap at the gate's foot, and the gate shudders. The view follows
    /// <see cref="ChainAnchors"/> every frame, so a Rewind or a restart puts it back (an anchor whole again is whole at once).
    /// Made once in the room's Awake; nothing is allocated per frame.
    /// </summary>
    public sealed class HellChainViews
    {
        // the link mesh (local: long axis z, flat in xz): half width R, half straight h, wire radius; links interlock at pitch
        const float LinkR = 0.2f, LinkH = 0.2f, LinkWire = 0.075f, LinkPitch = 0.5f;
        const float ChainScale = 1.35f, StrainScale = 1.75f, WardRadius = 1.35f;
        const float WhipSeconds = 2.6f, ShardSeconds = 3f, SparkSeconds = 1.4f, FlashSeconds = 1.2f, ShakeSeconds = 0.7f;
        const int AmbientSparks = 10, BurstSparks = 28, Shards = 7, Runes = 8, Cracks = 6;

        static readonly Quaternion Roll90 = Quaternion.Euler(0f, 0f, 90f);
        static readonly Color Ember = new Color(1.5f, 0.24f, 0.05f), WhiteHot = new Color(3.4f, 1.3f, 0.35f);
        static readonly Color CrackHot = new Color(5.5f, 3.8f, 2.2f), WardCol = new Color(1.3f, 0.32f, 0.85f);
        static readonly Color Cold = new Color(0.07f, 0.05f, 0.045f);
        static readonly Vector3 Gravity = new Vector3(0f, -14f, 0f);
        static Mesh _link, _ring, _cube;

        sealed class Chain
        {
            public Transform root, strain, ward, stub;
            public Transform[] links, cracks, shards, ambient, burst;
            public Vector3[] taut, slack, pos, shardVel, shardSpin, ambientVel, burstVel, crackScale;
            public bool[] hanging;
            public Renderer strainR, stubR;
            public Renderer[] shardR;
            public Material strainMat, crackMat, wardMat, heatMat0, heatMat1, stubMat, shardMat;
            public Light light;
            public Vector3 s, g, a, side, strainScale;
            public Quaternion wardRot;
            public float maxHp, lastHp, hit, phase, breakT = -1f, floorY;
            public bool settled, hidden;
        }

        readonly HellRoom _room;
        readonly Chain[] _chains;
        readonly Transform _gate, _gateTwin;
        readonly Renderer _gateR, _gateTwinR;
        readonly Vector3 _shakeAxis = new Vector3(1f, 0f, 0.25f).normalized;
        float _shakeT = -1f;
        Material _iron, _spark;

        public HellChainViews(HellRoom room, List<GameObject> anchors, GameObject gate)
        {
            _room = room;
            var chains = (ChainAnchors)room.Objective;
            MakeMeshes();
            var root = new GameObject("Anchor Chains").transform;
            root.SetParent(room.transform, false);

            // the gate (world box), and its twin for the shudder (the gate's own collider never moves)
            bool hasGate = false;
            Bounds gb = default;
            if (gate != null)
            {
                _gate = gate.transform;
                _gateR = gate.GetComponent<Renderer>();
                var mf = gate.GetComponent<MeshFilter>();
                if (_gateR != null && mf != null)
                {
                    hasGate = true;
                    gb = _gateR.bounds;
                    var twin = new GameObject("Gate Shudder View");
                    twin.transform.SetParent(_gate, false);
                    twin.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    _gateTwinR = twin.AddComponent<MeshRenderer>();
                    _gateTwinR.enabled = false;
                    _gateTwin = twin.transform;
                }
            }

            _spark = Unlit(new Color(6f, 2.6f, 0.6f));
            // the iron: the builder's chain material (the look restyles it like any other SK_ material)
            for (int i = 0; i < anchors.Count && _iron == null; i++)
                if (anchors[i] != null)
                    foreach (var r in anchors[i].GetComponentsInChildren<Renderer>(true))
                        if (r.gameObject != anchors[i]) { _iron = r.sharedMaterial; break; }
            if (_iron == null) _iron = Lit(new Color(0.2f, 0.19f, 0.2f));

            var def = room.Def;
            _chains = new Chain[anchors.Count];
            for (int i = 0; i < anchors.Count && i < def.stops.Count; i++)
            {
                if (anchors[i] == null) continue;
                foreach (var r in anchors[i].GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                var at = anchors[i].transform.position;
                var c = new Chain();
                c.maxHp = c.lastHp = Mathf.Max(1f, chains.Health(i));
                c.floorY = hasGate ? gb.min.y + 0.12f : at.y - 4f;
                var wallN = WallNormal(def.bounds, def.stops[i].position, out float wallDist);
                Build(c, i, root, at, hasGate ? gb : new Bounds(at + Vector3.down * 3f + wallN * 6f, Vector3.one), wallN, Mathf.Min(wallDist, 2f));
                _chains[i] = c;
            }
        }

        /// <summary>The normal (into the room) of the room wall nearest an anchor, and the distance to it.</summary>
        static Vector3 WallNormal(CoverBox b, System.Numerics.Vector3 p, out float dist)
        {
            float w = p.X - b.min.X, e = b.max.X - p.X, s = p.Z - b.min.Z, n = b.max.Z - p.Z;
            dist = Mathf.Min(Mathf.Min(w, e), Mathf.Min(s, n));
            if (dist == w) return Vector3.right;
            if (dist == e) return Vector3.left;
            if (dist == s) return Vector3.forward;
            return Vector3.back;
        }

        void Build(Chain c, int i, Transform root, Vector3 at, Bounds gate, Vector3 wallN, float wallDist)
        {
            c.root = new GameObject("Anchor Chain " + i).transform;
            c.root.SetParent(root, true);
            // where the chain meets the gate: its near face, pulled a little toward the middle
            var g = Vector3.Lerp(gate.ClosestPoint(at), gate.center, 0.35f);
            c.s = at; c.g = g;
            c.a = (g - at).normalized;
            var dh = new Vector3(at.x - g.x, 0f, at.z - g.z);
            if (dh.sqrMagnitude < 1f) dh = wallN;
            dh.Normalize();
            c.side = Vector3.Cross(Vector3.up, dh).normalized;

            c.strainMat = Unlit(Ember);
            c.crackMat = Unlit(CrackHot);
            c.wardMat = Unlit(WardCol * 0.3f);
            c.heatMat0 = Unlit(Ember * 0.45f);
            c.heatMat1 = Unlit(Ember * 0.18f);
            c.stubMat = Unlit(Ember);
            c.shardMat = Unlit(WhiteHot);

            // the wall bracket: a riveted plate, a bar and an eye link
            var wall = at - wallN * wallDist;
            var plateRot = Quaternion.LookRotation(wallN);
            var plate = wall + wallN * 0.12f;
            Part(c.root, "Bracket Plate", _cube, _iron, plate, plateRot, new Vector3(1.5f, 1.5f, 0.24f), true);
            for (int k = 0; k < 4; k++)
            {
                var off = plateRot * new Vector3((k & 1) == 0 ? -0.55f : 0.55f, (k & 2) == 0 ? -0.55f : 0.55f, 0f);
                Part(c.root, "Bracket Rivet", _cube, _iron, plate + wallN * 0.14f + off, plateRot * Quaternion.Euler(0f, 0f, 45f), Vector3.one * 0.16f, false);
            }
            float strainPitch = 0.5f * LinkPitch * (StrainScale + ChainScale);
            var eye = at - c.a * strainPitch;
            Part(c.root, "Bracket Eye", _link, _iron, eye, Quaternion.LookRotation(c.a, c.side) * Roll90, Vector3.one * ChainScale * 1.1f, true);
            var barFrom = wall + wallN * 0.2f;
            var barTo = eye - c.a * ((LinkH + LinkR) * ChainScale);
            var bar = barTo - barFrom;
            if (bar.sqrMagnitude > 1e-4f)
                Part(c.root, "Bracket Bar", _cube, _iron, (barFrom + barTo) * 0.5f, Quaternion.LookRotation(bar.normalized), new Vector3(0.26f, 0.26f, bar.magnitude + 0.2f), true);

            // the straining link (the target), its cracks, the ward ring
            var strainRot = Quaternion.LookRotation(c.a, c.side);
            c.strainScale = Vector3.one * StrainScale;
            c.strain = Part(c.root, "Straining Link", _link, c.strainMat, at, strainRot, c.strainScale, false);
            c.strainR = c.strain.GetComponent<Renderer>();
            c.cracks = new Transform[Cracks];
            c.crackScale = new Vector3[Cracks];
            for (int k = 0; k < Cracks; k++)
            {
                // jagged hairlines across the wire on both straight sides, and two along the end that bears the pull
                float sx = (k & 1) == 0 ? 1f : -1f;
                Vector3 lp; Quaternion lr;
                if (k < 4) { lp = new Vector3(sx * (LinkR + LinkWire * 0.8f), k < 2 ? 0.02f : -0.03f, k < 2 ? 0.05f : -0.12f); lr = Quaternion.Euler(k < 2 ? 25f : -35f, 0f, k * 30f); }
                else { lp = new Vector3(sx * 0.07f, 0f, LinkH + LinkR + LinkWire * 0.7f); lr = Quaternion.Euler(0f, sx * 50f, 0f); }
                var t = Part(c.strain, "Crack", _cube, c.crackMat, Vector3.zero, Quaternion.identity, Vector3.one, false);
                t.localPosition = lp; t.localRotation = lr;
                c.crackScale[k] = k < 4 ? new Vector3(0.03f, 0.17f, 0.03f) : new Vector3(0.12f, 0.025f, 0.03f);
                t.localScale = c.crackScale[k];
                c.cracks[k] = t;
            }
            c.wardRot = Quaternion.LookRotation(c.a, c.side);
            c.ward = Part(c.root, "Ward Ring", _ring, c.wardMat, at, c.wardRot, Vector3.one, false);
            for (int k = 0; k < Runes; k++)
            {
                float ang = k * 360f / Runes;
                var t = Part(c.ward, "Rune", _cube, c.wardMat, Vector3.zero, Quaternion.identity, Vector3.one, false);
                t.localPosition = Quaternion.Euler(0f, 0f, ang) * new Vector3(0f, WardRadius, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, ang + ((k & 1) == 0 ? 45f : 0f));
                t.localScale = (k & 1) == 0 ? new Vector3(0.13f, 0.13f, 0.02f) : new Vector3(0.04f, 0.26f, 0.02f);
            }
            // the half left on the bracket once it breaks
            c.stub = Part(c.root, "Broken Half", _link, c.stubMat, at - c.a * (LinkH * StrainScale * 0.8f), strainRot * Quaternion.Euler(18f, 0f, 0f),
                          new Vector3(StrainScale, StrainScale, StrainScale * 0.5f), false);
            c.stubR = c.stub.GetComponent<Renderer>();
            c.stubR.enabled = false;

            // the chain: taut (a slight sag) from the straining link to the gate; slack, it hangs from the gate and heaps at its foot
            var p0 = at + c.a * strainPitch;
            float pitch = LinkPitch * ChainScale;
            float len = Vector3.Distance(p0, g);
            int n = Mathf.Max(2, Mathf.FloorToInt(len / pitch) + 1);
            c.links = new Transform[n];
            c.taut = new Vector3[n]; c.slack = new Vector3[n]; c.pos = new Vector3[n]; c.hanging = new bool[n];
            float sag = Mathf.Min(1.2f, 0.025f * len);
            float drop = Mathf.Max(0.5f, g.y - c.floorY);
            var heap = new Vector3(g.x, c.floorY, g.z) + dh * 2.2f;
            for (int k = 0; k < n; k++)
            {
                float u = k / (float)(n - 1);
                c.taut[k] = Vector3.Lerp(p0, g, u) + Vector3.down * (4f * u * (1f - u) * sag);
                float fromGate = (n - 1 - k) * pitch;
                if (fromGate <= drop)
                {
                    float f = fromGate / drop;
                    c.slack[k] = g + Vector3.down * fromGate + dh * (1.4f * f * f);
                    c.hanging[k] = true;
                }
                else
                {
                    float laid = fromGate - drop;
                    float rad = 0.7f + 0.02f * laid;
                    float th = laid / rad;
                    c.slack[k] = heap + new Vector3(Mathf.Cos(th) * rad, 0.06f + 0.02f * laid + 0.08f * Mathf.Sin(th * 2.3f), Mathf.Sin(th) * rad);
                }
                var mat = k == 0 ? c.heatMat0 : k == 1 ? c.heatMat1 : _iron;
                c.links[k] = Part(c.root, "Chain Link", _link, mat, c.taut[k], Quaternion.identity, Vector3.one * ChainScale, mat == _iron);
            }
            SetChain(c, c.taut);

            // the shatter: pieces, sparks, light
            c.shards = new Transform[Shards]; c.shardR = new Renderer[Shards];
            c.shardVel = new Vector3[Shards]; c.shardSpin = new Vector3[Shards];
            var rng = new System.Random(9137 + i * 31);
            for (int k = 0; k < Shards; k++)
            {
                bool big = k < 2;
                var size = big ? new Vector3(0.2f, 0.2f, 0.62f) : new Vector3(0.12f + 0.08f * (float)rng.NextDouble(), 0.12f, 0.2f + 0.2f * (float)rng.NextDouble());
                var t = Part(c.root, "Link Shard", _cube, c.shardMat, at, Quaternion.identity, size, false);
                c.shards[k] = t; c.shardR[k] = t.GetComponent<Renderer>(); c.shardR[k].enabled = false;
                c.shardVel[k] = -c.a * (big ? 1.5f : 3f) + RandDir(rng) * (big ? 3f : 6.5f) + Vector3.up * (2.5f + 3f * (float)rng.NextDouble());
                c.shardSpin[k] = RandDir(rng) * (300f + 500f * (float)rng.NextDouble());
            }
            c.ambient = new Transform[AmbientSparks]; c.ambientVel = new Vector3[AmbientSparks];
            for (int k = 0; k < AmbientSparks; k++)
            {
                c.ambient[k] = Part(c.root, "Spark", _cube, _spark, at, Quaternion.identity, Vector3.zero, false);
                c.ambientVel[k] = RandDir(rng) * 1.6f + wallN * 1.2f + Vector3.up * (1.2f + 1.5f * (float)rng.NextDouble());
            }
            c.burst = new Transform[BurstSparks]; c.burstVel = new Vector3[BurstSparks];
            for (int k = 0; k < BurstSparks; k++)
            {
                c.burst[k] = Part(c.root, "Burst Spark", _cube, _spark, at, Quaternion.identity, Vector3.zero, false);
                c.burst[k].gameObject.SetActive(false);
                c.burstVel[k] = RandDir(rng) * (5f + 9f * (float)rng.NextDouble()) + Vector3.up * 3f;
            }
            var lgo = new GameObject("Anchor Glow");
            lgo.transform.SetParent(c.root, false);
            lgo.transform.position = at + wallN * 0.8f;
            c.light = lgo.AddComponent<Light>();
            c.light.type = LightType.Point; c.light.shadows = LightShadows.None;
            c.light.color = new Color(1f, 0.45f, 0.15f); c.light.range = 7f; c.light.intensity = 1.5f;
        }

        // ------------------------------------------------------------------ every frame

        /// <summary>Follow the objective's state (called from <see cref="HellRoom"/>'s Update).</summary>
        public void Tick()
        {
            if (!(_room.Objective is ChainAnchors c)) return;
            float now = Time.time, dt = Time.deltaTime;
            var player = _room.Player;
            for (int i = 0; i < _chains.Length; i++)
            {
                var ch = _chains[i];
                if (ch == null || i >= c.Count) continue;
                float hp = c.Health(i);
                if (!c.Broken(i))
                {
                    if (ch.breakT >= 0f) MakeWhole(ch);
                    if (hp > ch.maxHp) ch.maxHp = hp;
                    if (hp < ch.lastHp - 1e-4f) ch.hit = 1f;
                    ch.lastHp = hp;
                    ch.hit = Mathf.MoveTowards(ch.hit, 0f, dt * 3.5f);
                    bool inside = player != null && c.InWard(i, HellRoom.N(player.EyePosition));
                    TickWhole(ch, 1f - Mathf.Clamp01(hp / ch.maxHp), inside, now, dt, i);
                }
                else
                {
                    if (ch.breakT < 0f) StartBreak(ch, now);
                    ch.lastHp = hp;
                    TickBroken(ch, now - ch.breakT, c.Done);
                }
            }
            TickShake(now);
        }

        void TickWhole(Chain ch, float dmg, bool inside, float now, float dt, int i)
        {
            ch.phase = (ch.phase + dt * (0.7f + 2.6f * dmg)) % 1f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(ch.phase * Mathf.PI * 2f);
            float k = 0.7f + 0.4f * pulse + 1.4f * ch.hit;
            SetCol(ch.strainMat, Color.Lerp(Ember, WhiteHot, dmg) * k);
            SetCol(ch.crackMat, CrackHot * (0.5f + 0.4f * pulse + 0.25f * dmg + 1.2f * ch.hit));
            SetCol(ch.heatMat0, Ember * ((0.35f + 0.35f * dmg) * k));
            SetCol(ch.heatMat1, Ember * ((0.12f + 0.2f * dmg) * k));
            // the strain: a tremble growing with damage, a swell on each hit
            float tr = 0.012f + 0.035f * dmg + 0.05f * ch.hit;
            var jit = ch.side * (Mathf.Sin(now * 47f + i) * tr) + Vector3.up * (Mathf.Sin(now * 39f + 2f * i) * tr * 0.7f);
            ch.strain.position = ch.s + jit;
            ch.strain.localScale = ch.strainScale * (1f + 0.07f * ch.hit + 0.02f * pulse * dmg);
            // the cracks widen and lengthen; the two along the end open only past 40% damage
            for (int j = 0; j < ch.cracks.Length; j++)
            {
                float open = j < 4 ? 0.35f + 0.65f * dmg : Mathf.Clamp01((dmg - 0.4f) * 2.5f);
                var sc = ch.crackScale[j];
                float wide = 1f + 1.6f * dmg;
                ch.cracks[j].localScale = j < 4
                    ? new Vector3(sc.x * wide, sc.y * open * (1f + 0.6f * dmg), sc.z * wide)
                    : new Vector3(sc.x * open, sc.y * wide, sc.z * wide);
            }
            for (int j = 0; j < 3 && j < ch.links.Length; j++) ch.links[j].position = ch.taut[j] + jit * (1f - j / 3f);
            // the ward: faint outside it, bright while you are inside (where a shot bites); a slow turn
            SetCol(ch.wardMat, WardCol * (inside ? 0.85f + 0.35f * pulse : 0.22f + 0.06f * pulse));
            ch.ward.rotation = ch.wardRot * Quaternion.Euler(0f, 0f, now * (inside ? 40f : 12f));
            // sparks spitting from the cracks: more of them the more it is hurt
            int live = 2 + Mathf.RoundToInt(dmg * (AmbientSparks - 2));
            for (int j = 0; j < ch.ambient.Length; j++)
            {
                var t = ch.ambient[j];
                if (j >= live) { t.localScale = Vector3.zero; continue; }
                float period = 0.6f + 0.05f * j;
                float u = ((now + j * 0.137f + i * 0.31f) / period) % 1f;
                float st = u * period;
                var v = ch.ambientVel[j] + Gravity * st;
                t.SetPositionAndRotation(ch.s + ch.ambientVel[j] * st + Gravity * (0.5f * st * st), Quaternion.LookRotation(v.sqrMagnitude > 1e-4f ? v : Vector3.up));
                float s = (1f - u) * (0.6f + 0.6f * dmg);
                t.localScale = new Vector3(0.03f * s, 0.03f * s, 0.2f * s);
            }
            ch.light.color = new Color(1f, 0.45f + 0.25f * dmg, 0.15f + 0.15f * dmg);
            ch.light.range = 7f;
            ch.light.intensity = 1.2f + 1.6f * dmg * pulse + 3f * ch.hit;
        }

        void StartBreak(Chain ch, float now)
        {
            ch.breakT = now; ch.settled = false;
            ch.strainR.enabled = false;
            foreach (var t in ch.cracks) t.gameObject.SetActive(false);
            ch.ward.gameObject.SetActive(false);
            foreach (var t in ch.ambient) t.localScale = Vector3.zero;
            ch.stubR.enabled = true;
            foreach (var r in ch.shardR) r.enabled = true;
            foreach (var t in ch.burst) t.gameObject.SetActive(true);
            ch.light.enabled = true;
            ch.light.color = new Color(1f, 0.75f, 0.45f);
            _shakeT = now;
        }

        void TickBroken(Chain ch, float t, bool done)
        {
            if (ch.settled)
            {
                // once the gate is open the hanging part goes with it (the heap stays on the floor)
                if (done != ch.hidden)
                {
                    ch.hidden = done;
                    for (int k = 0; k < ch.links.Length; k++) if (ch.hanging[k]) ch.links[k].gameObject.SetActive(!done);
                }
                return;
            }
            // the chain: the free end goes first, whips sideways, falls in an arc and gathers at the gate's foot
            int n = ch.links.Length;
            bool end = t >= Mathf.Max(WhipSeconds, ShardSeconds);
            for (int k = 0; k < n; k++)
            {
                if (t >= WhipSeconds) { ch.pos[k] = ch.slack[k]; continue; }
                float u = Mathf.Max(0f, t - 0.3f * (k / (float)n));
                float e = 1f - Mathf.Exp(-3.4f * u) * Mathf.Cos(6.2f * u);
                float free = 1f - k / (float)n;
                var p = Vector3.LerpUnclamped(ch.taut[k], ch.slack[k], e);
                float d = Vector3.Distance(ch.taut[k], ch.slack[k]);
                p += Vector3.down * (Mathf.Sin(Mathf.PI * Mathf.Clamp01(e)) * 0.25f * d);
                p += ch.side * (1.8f * free * Mathf.Exp(-2.4f * u) * Mathf.Sin(10f * u + k * 0.35f));
                if (p.y < ch.floorY) p.y = ch.floorY;
                ch.pos[k] = p;
            }
            SetChain(ch, ch.pos);
            // the shattered link's pieces
            for (int k = 0; k < ch.shards.Length; k++)
            {
                if (t >= ShardSeconds) { ch.shardR[k].enabled = false; continue; }
                var p = ch.s + ch.shardVel[k] * t + Gravity * (0.5f * t * t);
                ch.shardR[k].enabled = p.y > ch.floorY - 3f;
                ch.shards[k].SetPositionAndRotation(p, Quaternion.Euler(ch.shardSpin[k] * t));
            }
            SetCol(ch.shardMat, Color.Lerp(WhiteHot * 1.4f, Cold, Mathf.Clamp01(t / 1.8f)));
            SetCol(ch.stubMat, Color.Lerp(WhiteHot, Ember * 0.25f, Mathf.Clamp01(t / 2.5f)));
            SetCol(ch.heatMat0, Color.Lerp(Ember * 0.8f, Cold, Mathf.Clamp01(t / 2f)));
            SetCol(ch.heatMat1, Color.Lerp(Ember * 0.4f, Cold, Mathf.Clamp01(t / 2f)));
            // the burst of sparks
            for (int k = 0; k < ch.burst.Length; k++)
            {
                var b = ch.burst[k];
                if (t >= SparkSeconds) { if (b.gameObject.activeSelf) b.gameObject.SetActive(false); continue; }
                float st = t * (0.7f + 0.1f * (k % 4));
                float drag = (1f - Mathf.Exp(-1.5f * st)) / 1.5f;
                var v = ch.burstVel[k] * Mathf.Exp(-1.5f * st) + Gravity * st;
                b.SetPositionAndRotation(ch.s + ch.burstVel[k] * drag + Gravity * (0.5f * st * st), Quaternion.LookRotation(v.sqrMagnitude > 1e-4f ? v : Vector3.up));
                float sc = 1f - t / SparkSeconds;
                b.localScale = new Vector3(0.04f * sc, 0.04f * sc, 0.35f * sc);
            }
            // the flash
            if (t < FlashSeconds) { ch.light.range = 14f; ch.light.intensity = 12f * Mathf.Exp(-5f * t) + 0.6f; }
            else if (ch.light.enabled) ch.light.enabled = false;

            if (end)
            {
                ch.settled = true;
                ch.hidden = false;
                foreach (var r in ch.shardR) r.enabled = false;
                foreach (var b in ch.burst) b.gameObject.SetActive(false);
            }
        }

        void MakeWhole(Chain ch)
        {
            ch.breakT = -1f; ch.settled = false; ch.hidden = false; ch.hit = 0f;
            ch.strainR.enabled = true;
            foreach (var t in ch.cracks) t.gameObject.SetActive(true);
            ch.ward.gameObject.SetActive(true);
            ch.stubR.enabled = false;
            foreach (var r in ch.shardR) r.enabled = false;
            foreach (var b in ch.burst) b.gameObject.SetActive(false);
            foreach (var l in ch.links) l.gameObject.SetActive(true);
            ch.light.enabled = true;
            SetChain(ch, ch.taut);
        }

        void TickShake(float now)
        {
            if (_shakeT < 0f || _gateTwin == null) return;
            float t = now - _shakeT;
            if (t >= ShakeSeconds || !_gate.gameObject.activeInHierarchy)
            {
                _shakeT = -1f;
                _gateTwin.localPosition = Vector3.zero;
                _gateTwinR.enabled = false;
                _gateR.enabled = true;
                return;
            }
            if (!_gateTwinR.enabled)
            {
                _gateTwinR.sharedMaterial = _gateR.sharedMaterial;   // the look may have restyled the gate since
                _gateTwinR.shadowCastingMode = _gateR.shadowCastingMode;
                _gateTwinR.enabled = true;
                _gateR.enabled = false;
            }
            float f = 1f - t / ShakeSeconds;
            _gateTwin.position = _gate.position + _shakeAxis * (Mathf.Sin(t * 58f) * 0.1f * f) + Vector3.up * (Mathf.Sin(t * 41f) * 0.04f * f);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Place the links on the points, each along its neighbours, every other one turned a quarter.</summary>
        static void SetChain(Chain ch, Vector3[] pts)
        {
            int n = pts.Length;
            for (int k = 0; k < n; k++)
            {
                var prev = k == 0 ? ch.s : pts[k - 1];
                var next = k == n - 1 ? ch.g : pts[k + 1];
                var f = next - prev;
                if (f.sqrMagnitude < 1e-6f) f = ch.a;
                var up = ch.side;
                if (Mathf.Abs(Vector3.Dot(f.normalized, up)) > 0.98f) up = Vector3.up;
                var rot = Quaternion.LookRotation(f, up);
                ch.links[k].SetPositionAndRotation(pts[k], (k & 1) == 0 ? rot * Roll90 : rot);
            }
        }

        static Vector3 RandDir(System.Random r)
        {
            var v = new Vector3((float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f);
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.up;
        }

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, bool shadow)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(pos, rot);
            t.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = shadow;
            return t;
        }

        static void SetCol(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else m.color = c;
        }

        static Material Unlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh != null ? sh : Shader.Find("Sprites/Default")) { color = c, name = "Hell Anchor Glow" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Material Lit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh != null ? sh : Shader.Find("Standard")) { color = c, name = "Hell Anchor Iron" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static void MakeMeshes()
        {
            if (_link != null) return;
            _link = Loop("Hell Chain Link", LinkR, LinkH, LinkWire, 10, 8);
            // the ward ring: a thin loop turned to face along z (y and z swapped, which mirrors it: the winding flips back)
            _ring = Loop("Hell Ward Ring", WardRadius, 0f, 0.022f, 24, 4);
            var v = _ring.vertices; var nn = _ring.normals;
            for (int i = 0; i < v.Length; i++) { v[i] = new Vector3(v[i].x, v[i].z, v[i].y); nn[i] = new Vector3(nn[i].x, nn[i].z, nn[i].y); }
            var tris = _ring.triangles;
            for (int i = 0; i < tris.Length; i += 3) { int x = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = x; }
            _ring.vertices = v; _ring.normals = nn; _ring.triangles = tris;
            _ring.RecalculateBounds();
            var prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube = prim.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(prim); else Object.DestroyImmediate(prim);
        }

        /// <summary>A stadium loop in xz (two half circles of radius <paramref name="R"/> at z = +-<paramref name="h"/>, joined
        /// by straights along z) swept with a round wire: a chain link, or with h = 0 a ring.</summary>
        static Mesh Loop(string name, float R, float h, float wire, int halfSegs, int around)
        {
            int pathN = 2 * (halfSegs + 1);
            var verts = new Vector3[pathN * around];
            var norms = new Vector3[verts.Length];
            var uv = new Vector2[verts.Length];
            for (int p = 0; p < pathN; p++)
            {
                int half = p / (halfSegs + 1);
                float th = Mathf.PI * (half + (p % (halfSegs + 1)) / (float)halfSegs) - Mathf.PI * 0.5f;
                var outN = new Vector3(Mathf.Sin(th), 0f, Mathf.Cos(th));
                var centre = outN * R + new Vector3(0f, 0f, half == 0 ? h : -h);
                for (int q = 0; q < around; q++)
                {
                    float ph = q * Mathf.PI * 2f / around;
                    var nrm = outN * Mathf.Cos(ph) + Vector3.up * Mathf.Sin(ph);
                    int idx = p * around + q;
                    verts[idx] = centre + nrm * wire;
                    norms[idx] = nrm;
                    uv[idx] = new Vector2(p / (float)pathN, q / (float)around);
                }
            }
            var tris = new int[pathN * around * 6];
            int ti = 0;
            for (int p = 0; p < pathN; p++)
            {
                int pn = (p + 1) % pathN;
                for (int q = 0; q < around; q++)
                {
                    int qn = (q + 1) % around;
                    int a = p * around + q, b = pn * around + q, c = pn * around + qn, d = p * around + qn;
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = d;
                    tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
                }
            }
            var m = new Mesh { name = name, vertices = verts, normals = norms, uv = uv, triangles = tris };
            m.RecalculateBounds();
            return m;
        }
    }
}
