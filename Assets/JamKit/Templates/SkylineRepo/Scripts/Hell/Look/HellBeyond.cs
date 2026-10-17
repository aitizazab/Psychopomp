using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The world beyond the rooms (docs/HELL-LIGHTING.md): Hell's sky (Hell/Sky: a smoke ceiling lit from below, a fire glow on
    /// the horizon, ranges of crags and spires in silhouette, eruption columns, embers) per floor, and round every room a ring
    /// of tall rock spires with glowing lava seams and giant chains slung between them, rising above the 8 m walls so the
    /// rooms stand in a vast burning cavern instead of a void. Per room the spires and chains are ONE combined mesh each (two
    /// draw calls), children of the room, so they come and go with it. Seeded per room: the same every run.
    /// </summary>
    public sealed class HellBeyond : MonoBehaviour
    {
        public System.Func<int, HellFloorLook> lookFor;
        Material _sky;
        readonly Dictionary<int, Material> _spireMats = new Dictionary<int, Material>();
        readonly Dictionary<int, Material> _chainMats = new Dictionary<int, Material>();
        public int spires, chains;

        public void Build()
        {
            var t = Resources.Load<Material>(HellLookSwitch.ResourceFolder + "HL_Sky");
            if (t != null)
            {
                _sky = new Material(t) { name = "HellLook_Sky" };
                RenderSettings.skybox = _sky;
            }
            else Debug.LogWarning("HELL_LIGHT no Resources/HellLook/HL_Sky (run HellLookBuild.PrepareAssets): no sky");
            foreach (var room in Object.FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Ring(room);
            Debug.Log("HELL_LIGHT beyond: sky " + (_sky != null) + ", " + spires + " spires, " + chains + " chains");
        }

        /// <summary>The sky's colours for a floor; the cameras clear to the sky.</summary>
        public void ApplyFloor(HellFloorLook L)
        {
            if (_sky == null) return;
            _sky.SetColor("_Zenith", L.zenith);
            _sky.SetColor("_Horizon", L.fog);
            _sky.SetColor("_Glow", L.glow);
            _sky.SetFloat("_GlowHeight", L.glowHeight);
            _sky.SetColor("_Smoke", L.smoke);
            _sky.SetColor("_SmokeLit", L.smokeLit);
            _sky.SetFloat("_SmokeAmount", L.smokeAmount);
            _sky.SetColor("_RidgeFar", L.ridgeFar);
            _sky.SetColor("_RidgeNear", L.ridgeNear);
            _sky.SetColor("_RimGlow", L.rim);
            _sky.SetFloat("_RidgeHeight", L.ridgeHeight);
            _sky.SetFloat("_Spires", L.spires);
            _sky.SetColor("_Pillar", L.pillar);
            _sky.SetFloat("_Embers", L.embers);
            _sky.SetFloat("_Seed", L.seed);
            RenderSettings.skybox = _sky;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) SkyCamera(cam);
        }

        public static void SkyCamera(Camera cam)
        {
            if (cam == null || RenderSettings.skybox == null) return;
            if (cam.clearFlags == CameraClearFlags.SolidColor || cam.clearFlags == CameraClearFlags.Skybox) cam.clearFlags = CameraClearFlags.Skybox;
            if (cam.farClipPlane < 600f) cam.farClipPlane = 600f;
        }

        Material SpireMat(int floor)
        {
            if (_spireMats.TryGetValue(floor, out var m) && m != null) return m;
            var L = lookFor(floor);
            m = HellLookSwitch.Surface("spire F" + floor, L.spireTex, L.spireTex, L.spireTint, L.spireTint, L.trimTint, L, false);
            if (m != null)
            {
                m.SetColor("_VeinColor", L.spireVein);
                m.SetFloat("_VeinTop", -12f);
                m.SetFloat("_VeinFade", 14f);
                m.SetFloat("_SideTile", 14f);
                m.SetFloat("_TopTile", 14f);
                m.SetFloat("_GradBottom", 1f);
            }
            _spireMats[floor] = m;
            return m;
        }

        Material ChainMat(int floor)
        {
            if (_chainMats.TryGetValue(floor, out var m) && m != null) return m;
            var L = lookFor(floor);
            m = HellLookSwitch.Surface("chain F" + floor, "rust", "rust", L.spireTint * 0.8f, L.spireTint * 0.8f, L.trimTint, L, false);
            if (m != null) { m.SetFloat("_SideTile", 3f); m.SetFloat("_TopTile", 3f); m.SetFloat("_GradBottom", 1f); }
            _chainMats[floor] = m;
            return m;
        }

        static Vector3 V(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        void Ring(HellRoom room)
        {
            var def = HellWorld.Room(room.floor, room.kind);
            Vector3 mn = V(def.bounds.min), mx = V(def.bounds.max);
            var c = (mn + mx) * 0.5f;
            float hx = (mx.x - mn.x) * 0.5f, hz = (mx.z - mn.z) * 0.5f;
            var rng = new System.Random(1000 + room.floor * 17 + (int)room.kind * 101);
            float R() => (float)rng.NextDouble();

            var spireParts = new List<CombineInstance>();
            var tops = new List<Vector3>();
            int count = 26;
            for (int i = 0; i < count; i++)
            {
                float a = (i + R() * 0.7f) / count * Mathf.PI * 2f;
                float out1 = 14f + R() * R() * 120f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                // a point on an ellipse round the room's bounds, pushed out
                var p = c + new Vector3(dir.x * (hx + out1), 0f, dir.z * (hz + out1));
                float h = 22f + out1 * 0.55f + R() * 55f;
                float r0 = h * (0.09f + R() * 0.08f);
                var mesh = SpireMesh(rng, r0, h);
                var pos = new Vector3(p.x, -30f, p.z);
                spireParts.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(0f, R() * 360f, 0f), Vector3.one) });
                tops.Add(pos + Vector3.up * (h * 0.72f + 0f));
                spires++;
            }
            Combined(room, "Beyond Spires", spireParts, SpireMat(room.floor));

            // chains slung between spires on either side of the room (they cross the sky above it)
            var chainParts = new List<CombineInstance>();
            var link = LinkMesh();
            for (int k = 0; k < 4; k++)
            {
                int i = rng.Next(tops.Count);
                int j = (i + tops.Count / 2 + rng.Next(-3, 4) + tops.Count) % tops.Count;
                Vector3 a0 = tops[i], a1 = tops[j];
                float span = Vector3.Distance(a0, a1);
                if (span < 30f) continue;
                float sag = span * (0.12f + R() * 0.1f);
                float linkLen = 2.2f;
                int n = Mathf.Clamp(Mathf.RoundToInt(span / (linkLen * 0.8f)), 8, 260);
                Vector3 prev = a0;
                for (int s = 1; s <= n; s++)
                {
                    float u = s / (float)n;
                    var q = Vector3.Lerp(a0, a1, u) + Vector3.down * (sag * 4f * u * (1f - u));
                    var dirL = (q - prev);
                    if (dirL.sqrMagnitude < 1e-4f) continue;
                    var rot = Quaternion.LookRotation(dirL.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, (s & 1) == 0 ? 0f : 90f);
                    chainParts.Add(new CombineInstance { mesh = link, transform = Matrix4x4.TRS((q + prev) * 0.5f, rot, new Vector3(1f, 1f, 1f)) });
                    prev = q;
                }
                chains++;
            }
            if (chainParts.Count > 0) Combined(room, "Beyond Chains", chainParts, ChainMat(room.floor));
        }

        static void Combined(HellRoom room, string name, List<CombineInstance> parts, Material m)
        {
            if (m == null || parts.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            // the combined vertices are in world space: an identity world transform, kept when parented to the room
            var go = new GameObject(name);
            go.transform.SetParent(room.transform, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>A jagged rock spire: 7 sides, 8 rings tapering to a leaning point, every vertex jittered, flat-shaded.</summary>
        static Mesh SpireMesh(System.Random rng, float r0, float h)
        {
            const int sides = 7, rings = 8;
            var ringPts = new Vector3[rings + 1, sides];
            var lean = new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f) * r0 * 1.6f;
            for (int k = 0; k <= rings; k++)
            {
                float u = k / (float)rings;
                float r = r0 * Mathf.Pow(1f - u, 1.25f) * (k == rings ? 0f : 1f);
                var centre = Vector3.up * (h * u) + lean * u * u;
                for (int s = 0; s < sides; s++)
                {
                    float a = (s + (float)rng.NextDouble() * 0.35f) / sides * Mathf.PI * 2f;
                    float jr = r * (0.75f + (float)rng.NextDouble() * 0.5f);
                    ringPts[k, s] = centre + new Vector3(Mathf.Cos(a) * jr, (float)(rng.NextDouble() - 0.5) * h * 0.03f, Mathf.Sin(a) * jr);
                }
            }
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int k = 0; k < rings; k++)
                for (int s = 0; s < sides; s++)
                {
                    int s1 = (s + 1) % sides;
                    Vector3 a = ringPts[k, s], b = ringPts[k, s1], c = ringPts[k + 1, s1], d = ringPts[k + 1, s];
                    int i0 = v.Count;
                    v.Add(a); v.Add(c); v.Add(b);
                    v.Add(a); v.Add(d); v.Add(c);
                    for (int q = 0; q < 6; q++) t.Add(i0 + q);
                }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh _link;

        /// <summary>One chain link: an open rectangle of four bars, 2.2 m long, 1.1 m wide.</summary>
        static Mesh LinkMesh()
        {
            if (_link != null) return _link;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cube = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            var parts = new[]
            {
                new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(new Vector3(0.45f, 0f, 0f), Quaternion.identity, new Vector3(0.24f, 0.24f, 2.2f)) },
                new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(new Vector3(-0.45f, 0f, 0f), Quaternion.identity, new Vector3(0.24f, 0.24f, 2.2f)) },
                new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(new Vector3(0f, 0f, 0.98f), Quaternion.identity, new Vector3(1.14f, 0.24f, 0.24f)) },
                new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(new Vector3(0f, 0f, -0.98f), Quaternion.identity, new Vector3(1.14f, 0.24f, 0.24f)) },
            };
            _link = new Mesh { name = "Hell chain link" };
            _link.CombineMeshes(parts, true, true);
            return _link;
        }
    }
}
