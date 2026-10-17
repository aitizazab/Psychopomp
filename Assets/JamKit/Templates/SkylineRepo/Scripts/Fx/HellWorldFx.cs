using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell rooms' living hazards (docs/HELL-FX.md), drawn over the rooms without touching their materials:
    /// <list type="bullet">
    /// <item><b>Lava:</b> embers rising off every basin of the running rooms (a rate per square metre, capped, only near the
    /// camera) and glow pulses: hot blobs heaving up out of the surface and sinking back, like bubbles of magma.</item>
    /// <item><b>Collapsing platforms:</b> while one shakes, cracks open on its top and grit trickles from under it; when it
    /// falls, a ring of dust and a spray of grit, and the camera shakes by distance.</item>
    /// </list>
    /// Rooms are found when one is entered or the run starts (never per frame). Everything is <see cref="HellMotes"/>, so a
    /// Rewind clears it, and the platforms' remembered states are re-read after a Rewind without emitting.
    /// </summary>
    public sealed class HellWorldFx : MonoBehaviour
    {
        [Header("Lava")]
        public float embersPerSquareMetre = 0.5f, maxEmbersPerSecond = 40f;
        public float pulsesPerSquareMetre = 0.06f, maxPulsesPerSecond = 4f;
        public float lavaRange = 45f;
        [Header("Collapse")]
        public float crackEvery = 0.18f, gritEvery = 0.07f;
        public float fallTrauma = 0.28f;

        sealed class Basin { public HellRoom room; public CoverBox box; public float area; public EmitAccumulator embers, pulses; }
        sealed class Plat { public HellCollapsePlatform p; public Renderer r; public Vector3 home; public bool solid = true, shaking; public float crackLeft, gritLeft; public int cracks; }

        readonly List<Basin> _basins = new List<Basin>();
        readonly List<Plat> _plats = new List<Plat>();
        float _rescanLeft;

        void OnEnable()
        {
            HellRunEvents.RoomEntered += OnRoomEntered;
            HellRunEvents.RunStarted += Rescan;
            HellRewind.Rewound += OnRewound;
            Rescan();
        }

        void OnDisable()
        {
            HellRunEvents.RoomEntered -= OnRoomEntered;
            HellRunEvents.RunStarted -= Rescan;
            HellRewind.Rewound -= OnRewound;
        }

        void OnRoomEntered(int floor, int room, string kind) => _rescanLeft = 0.1f;   // the room binds itself this frame: look next

        /// <summary>Finds every room's lava basins and collapsing platforms (allocates; only on room entry and run start).</summary>
        public void Rescan()
        {
            _basins.Clear();
            _plats.Clear();
            foreach (var room in FindObjectsByType<HellRoom>(FindObjectsSortMode.None))
            {
                if (room.Def == null) continue;
                foreach (var h in room.Def.hazards)
                {
                    if (h.kind != HellHazardKind.Lava) continue;
                    var b = h.box;
                    float area = Mathf.Max(0f, (b.max.X - b.min.X) * (b.max.Z - b.min.Z));
                    if (area > 0.01f) _basins.Add(new Basin { room = room, box = b, area = area });
                }
                foreach (var p in room.Platforms)
                    if (p != null) _plats.Add(new Plat { p = p, r = p.GetComponent<Renderer>(), home = p.transform.position, solid = p.Solid });
            }
        }

        void OnRewound()
        {
            foreach (var pl in _plats)
            {
                if (pl.p == null) continue;
                pl.solid = pl.p.Solid;
                pl.shaking = false;
                pl.cracks = 0;
            }
        }

        void Update()
        {
            if (_rescanLeft > 0f)
            {
                _rescanLeft -= Time.unscaledDeltaTime;
                if (_rescanLeft <= 0f) Rescan();
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            TickLava(dt, eye);
            TickPlatforms(dt);
        }

        // ------------------------------------------------------------------ lava

        void TickLava(float dt, Vector3 eye)
        {
            if (_basins.Count == 0) return;
            var ember = HellFx.Glow(HellFxPalette.Ember, 1f);
            var glow = HellFx.Glow(HellFxPalette.LavaGlow, 1f);
            float dens = HellFxPalette.Density;
            for (int i = 0; i < _basins.Count; i++)
            {
                var b = _basins[i];
                if (b.room == null || !b.room.Running) continue;
                var bx = b.box;
                Vector3 c = new Vector3((bx.min.X + bx.max.X) * 0.5f, bx.max.Y, (bx.min.Z + bx.max.Z) * 0.5f);
                float half = 0.5f * Mathf.Sqrt(b.area);
                if ((c - eye).sqrMagnitude > (lavaRange + half) * (lavaRange + half)) continue;
                int n = b.embers.Take(Mathf.Min(maxEmbersPerSecond, b.area * embersPerSquareMetre) * dens, dt, 6);
                for (int k = 0; k < n; k++)
                {
                    HellMotes.Emit(new MoteSpec
                    {
                        shape = MoteShape.Streak, material = ember, position = OnTop(bx, 0.05f),
                        velocity = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.5f, 3.6f), Random.Range(-0.5f, 0.5f)),
                        life = Random.Range(1.1f, 2.2f), size = Random.Range(0.035f, 0.06f), gravity = -0.6f, drag = 0.3f, swell = 0.08f, stretch = 0.03f,
                    });
                }
                int m = b.pulses.Take(Mathf.Min(maxPulsesPerSecond, b.area * pulsesPerSquareMetre) * dens, dt, 2);
                for (int k = 0; k < m; k++)
                {
                    float size = Random.Range(0.5f, 1.2f);
                    HellMotes.Emit(new MoteSpec
                    {
                        shape = MoteShape.Blob, material = glow, position = OnTop(bx, -size * 0.35f),
                        velocity = new Vector3(0f, Random.Range(0.15f, 0.35f), 0f),
                        life = Random.Range(0.8f, 1.3f), size = size, swell = 0.45f,
                    });
                }
            }
        }

        static Vector3 OnTop(CoverBox b, float lift) =>
            new Vector3(Random.Range(b.min.X, b.max.X), b.max.Y + lift, Random.Range(b.min.Z, b.max.Z));

        // ------------------------------------------------------------------ collapsing platforms

        void TickPlatforms(float dt)
        {
            if (_plats.Count == 0) return;
            Material dust = null, crack = null;
            for (int i = 0; i < _plats.Count; i++)
            {
                var pl = _plats[i];
                if (pl.p == null) continue;
                bool solid = pl.p.Solid;
                Vector3 pos = pl.p.transform.position;
                bool shaking = solid && (pos - pl.home).sqrMagnitude > 1e-5f;
                if (dust == null) { dust = HellFx.Lit(HellFxPalette.Dust); crack = HellFx.Lit(HellFxPalette.Crack); }
                Bounds bb = pl.r != null ? pl.r.bounds : new Bounds(pos, Vector3.one * 2f);
                if (shaking)
                {
                    if (!pl.shaking) { pl.cracks = 0; pl.crackLeft = 0f; }
                    pl.crackLeft -= dt;
                    if (pl.crackLeft <= 0f && pl.cracks < 7)
                    {
                        pl.crackLeft = crackEvery;
                        pl.cracks++;
                        Vector3 top = new Vector3(Random.Range(bb.min.x, bb.max.x), bb.max.y + 0.01f, Random.Range(bb.min.z, bb.max.z));
                        HellMotes.Emit(new MoteSpec
                        {
                            shape = MoteShape.Splat, material = crack, position = top, life = 1.6f,
                            size = Random.Range(0.6f, 1.3f), stretch = 0.03f, swell = 0.12f, yaw = Random.Range(0f, 180f),
                        });
                        HellMotes.Spray(MoteShape.Streak, HellFx.Glow(HellFxPalette.Ember, 0.8f), top, Vector3.up, 0.5f, 2, 1f, 2.5f, 0.35f, 0.03f, 4f, 1f);
                    }
                    pl.gritLeft -= dt;
                    if (pl.gritLeft <= 0f)
                    {
                        pl.gritLeft = gritEvery;
                        Vector3 under = new Vector3(Random.Range(bb.min.x, bb.max.x), bb.min.y, Random.Range(bb.min.z, bb.max.z));
                        HellMotes.Emit(new MoteSpec
                        {
                            shape = MoteShape.Blob, material = dust, position = under, velocity = Vector3.down * 0.5f,
                            life = 0.7f, size = Random.Range(0.08f, 0.16f), gravity = 9f, drag = 0.2f,
                        });
                    }
                }
                if (pl.solid && !solid)
                {
                    Vector3 c = new Vector3(bb.center.x, bb.max.y, bb.center.z);
                    float spread = Mathf.Max(bb.extents.x, bb.extents.z);
                    HellMotes.Ring(MoteShape.Blob, dust, c, 16, 3f + spread, 0.7f, 0.45f, 0.4f, 3f);
                    HellMotes.Spray(MoteShape.Blob, dust, c, Vector3.down, 1f, 14, 1f, 4f, 0.9f, 0.14f, 9f, 0.3f);
                    HellFeelFx.ShakeAt(c, fallTrauma, 5f, 25f);
                }
                pl.shaking = shaking;
                pl.solid = solid;
            }
        }
    }
}
