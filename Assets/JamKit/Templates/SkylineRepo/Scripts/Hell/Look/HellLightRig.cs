using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Hell's lights (docs/HELL-LIGHTING.md), Deep Rock Galactic's rule: dark rock lit by its sources. Replaces the builder's
    /// lava lights (they sat 0.4 m BELOW the walking floor, inside the basins, so every floor's top faced away from them, and at
    /// intensity 2-4.5 an inverse-square light reached 1-2 m) with lights 1.6 m above each lava surface at intensity ~16 (a
    /// 10-14 m pool); adds braziers along every tall outer wall (a bowl of fire on a bracket and its light, grazing the
    /// textured wall); a heat haze curtain rising off each basin; flicker on every fire; hard shadows on the braziers nearest the
    /// camera (a budget of <see cref="ShadowedBraziers"/>); and the ambient as a gradient (sky glow above, lava glow below) as
    /// spherical harmonics. Nothing here changes play: the darkness the Shadow Step reads is the rooms' data
    /// (<see cref="HellDarkness"/>), and every light here is kept clear of the dark zones' palls so they still look dark.
    /// </summary>
    public sealed class HellLightRig : MonoBehaviour
    {
        public System.Func<int, HellFloorLook> lookFor;
        public const int ShadowedBraziers = 3;
        /// <summary>The camera the shadow budget follows (the capture's own camera); null: the main camera.</summary>
        public static Camera Focus;

        static readonly int SkHellLightId = Shader.PropertyToID("_SkHellLight");

        struct Flame
        {
            public Light light;
            public float baseIntensity, amp, speed, seed;
            public Transform flame;
            public Vector3 flameScale;
        }

        readonly List<Flame> _flames = new List<Flame>();
        readonly List<Light> _braziers = new List<Light>();
        readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        float _shadowClock;
        public int lavaLights, braziers, removed, hazes;

        public void Build()
        {
            foreach (var room in Object.FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                BuildRoom(room);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.name != "Shaft Light") continue;
                var L = lookFor(HellLookOptions.FloorAt(l.transform.position.z));
                l.color = L.lavaLight; l.intensity = L.lavaIntensity * 2.5f; l.range = 30f;
            }
            Debug.Log("HELL_LIGHT rig: " + lavaLights + " lava lights (replacing " + removed + " builder lights), " + braziers + " braziers, " + hazes + " heat hazes");
        }

        void BuildRoom(HellRoom room)
        {
            var def = HellWorld.Room(room.floor, room.kind);
            var L = lookFor(room.floor);
            var palls = new List<Bounds>();
            foreach (var r in room.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Shadow Pall") palls.Add(r.bounds);

            // the builder's lava lights: gone (see the summary)
            var fire = room.transform.Find("Fire Lights");
            if (fire == null) { fire = new GameObject("Fire Lights").transform; fire.SetParent(room.transform, false); }
            for (int i = fire.childCount - 1; i >= 0; i--) { Destroy(fire.GetChild(i).gameObject); removed++; }

            var lava = room.transform.Find("Lava");
            if (lava != null)
                foreach (Transform t in lava)
                {
                    var r = t.GetComponent<Renderer>();
                    if (r == null) continue;
                    var b = r.bounds;
                    float sx = b.size.x, sz = b.size.z;
                    int nx = Mathf.Max(1, Mathf.RoundToInt(sx / L.lavaSpacing)), nz = Mathf.Max(1, Mathf.RoundToInt(sz / L.lavaSpacing));
                    for (int i = 0; i < nx; i++)
                        for (int j = 0; j < nz; j++)
                        {
                            var p = new Vector3(b.min.x + sx * (i + 0.5f) / nx, b.max.y + L.lavaHeight, b.min.z + sz * (j + 0.5f) / nz);
                            float range = ClearOf(p, L.lavaRange, palls);
                            if (range < 4f) continue;
                            var l = MakeLight(fire, "Lava Light", p, L.lavaLight, L.lavaIntensity * Mathf.Lerp(0.6f, 1f, range / L.lavaRange), range);
                            _flames.Add(new Flame { light = l, baseIntensity = l.intensity, amp = L.lavaFlicker, speed = 0.9f, seed = Random.value * 100f });
                            lavaLights++;
                        }
                    Haze(t, b, L, room.floor);
                }

            // braziers on the tall walls' inner faces and on the rim's parapets
            var group = new GameObject("Braziers").transform;
            group.SetParent(room.transform, false);
            var c = (V(def.bounds.min) + V(def.bounds.max)) * 0.5f;
            foreach (var s in def.solids)
            {
                if (s.ramp || s.role == "floor" || s.role == "deck" || s.role == "ramp" || s.role == "cover" || s.role == "gate" || s.role == "well") continue;
                Vector3 mn = V(s.box.min), mx = V(s.box.max);
                // the open rim's parapets (docs/HELL-ROOMS.md, "Open edges") carry braziers on their tops; other walls need height
                bool parapet = s.id.StartsWith("parapet");
                if (!parapet && mx.y - mn.y < 6f) continue;
                bool alongZ = mx.z - mn.z >= mx.x - mn.x;
                float len = alongZ ? mx.z - mn.z : mx.x - mn.x;
                if (len < 8f) continue;
                Vector3 normal; float face;
                if (alongZ) { bool minSide = c.x < (mn.x + mx.x) * 0.5f; face = minSide ? mn.x : mx.x; normal = minSide ? Vector3.left : Vector3.right; }
                else { bool minSide = c.z < (mn.z + mx.z) * 0.5f; face = minSide ? mn.z : mx.z; normal = minSide ? Vector3.back : Vector3.forward; }
                int n = Mathf.Max(1, Mathf.FloorToInt(len / L.brazierSpacing));
                float y = parapet ? mx.y + 0.75f : Mathf.Min(mx.y - 1.2f, 4.2f);
                for (int k = 0; k < n; k++)
                {
                    float along = (alongZ ? mn.z : mn.x) + len * (k + 0.5f) / n;
                    var at = alongZ ? new Vector3(face, y, along) : new Vector3(along, y, face);
                    at += normal * 0.45f;
                    float range = ClearOf(at, L.brazierRange, palls);
                    if (range < 6f) continue;
                    Brazier(group, at, normal, L, range);
                    braziers++;
                }
            }
        }

        static Vector3 V(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        /// <summary>The light's range, cut so it stops a metre short of every dark zone's pall.</summary>
        static float ClearOf(Vector3 p, float range, List<Bounds> palls)
        {
            foreach (var b in palls)
            {
                float d = Mathf.Sqrt(b.SqrDistance(p));
                range = Mathf.Min(range, d - 1f);
            }
            return range;
        }

        static Light MakeLight(Transform parent, string name, Vector3 p, Color c, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        Material Mat(string key, System.Func<Material> make)
        {
            if (!_mats.TryGetValue(key, out var m) || m == null) { m = make(); _mats[key] = m; }
            return m;
        }

        void Brazier(Transform group, Vector3 at, Vector3 normal, HellFloorLook L, float range)
        {
            var root = new GameObject("Brazier").transform;
            root.SetParent(group, false);
            root.position = at;
            root.rotation = Quaternion.LookRotation(normal, Vector3.up);
            var iron = Mat("iron", () => SkylineBotLook.Lit(new Color(0.07f, 0.055f, 0.05f)));
            var hot = Mat("flame" + L.brazierLight, () => SkylineBotLook.Glow(new Color(L.brazierLight.r * 4f, L.brazierLight.g * 3f, L.brazierLight.b * 2.4f)));
            var core = Mat("core" + L.brazierLight, () => SkylineBotLook.Glow(new Color(5f, 3.4f, 1.6f)));
            Part(PrimitiveType.Cube, root, new Vector3(0f, -0.22f, -0.3f), new Vector3(0.12f, 0.12f, 0.6f), iron);          // the bracket
            Part(PrimitiveType.Cube, root, new Vector3(0f, -0.55f, -0.42f), new Vector3(0.1f, 0.6f, 0.1f), iron);           // its strut
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, -0.1f, 0f), new Vector3(0.62f, 0.09f, 0.62f), iron);         // the bowl
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, -0.2f, 0f), new Vector3(0.4f, 0.06f, 0.4f), iron);
            var flame = Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.22f, 0f), new Vector3(0.42f, 0.6f, 0.42f), hot);
            Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.3f, 0.3f), core);
            Halo(root, new Vector3(0f, 0.3f, 0.1f), 2.6f, L.brazierLight * 0.9f);
            var l = MakeLight(root, "Brazier Light", at + Vector3.up * 0.55f + normal * 0.25f, L.brazierLight, L.brazierIntensity, range);
            l.transform.SetParent(root, true);
            _braziers.Add(l);
            _flames.Add(new Flame { light = l, baseIntensity = l.intensity, amp = L.brazierFlicker, speed = 2.6f, seed = Random.value * 100f, flame = flame.transform, flameScale = flame.transform.localScale });
        }

        static GameObject Part(PrimitiveType t, Transform parent, Vector3 local, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(t);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        Mesh _quad;

        /// <summary>The glow round a fire in the smoky air (Hell/Halo, a camera-facing additive quad).</summary>
        void Halo(Transform parent, Vector3 local, float size, Color c)
        {
            var t = Resources.Load<Material>(HellLookSwitch.ResourceFolder + "HL_Halo");
            if (t == null) return;
            var mat = Mat("halo" + c, () => { var m = new Material(t) { name = "HellLook_Halo" }; m.SetColor("_Color", c); return m; });
            if (_quad == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _quad = tmp.GetComponent<MeshFilter>().sharedMesh;
                Destroy(tmp);
            }
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        static Mesh _curtain;

        /// <summary>The heat haze: a curtain round the basin's rim rising from the lava surface (Hell/Haze, additive).</summary>
        void Haze(Transform lava, Bounds b, HellFloorLook L, int floor)
        {
            var shader = Resources.Load<Material>(HellLookSwitch.ResourceFolder + "HL_Haze");
            if (shader == null) return;
            float baseY = b.max.y;
            var mat = Mat("haze" + floor + "_" + Mathf.RoundToInt(baseY * 10f), () =>
            {
                var m = new Material(shader) { name = "HellLook_Haze" };
                m.SetColor("_Color", L.haze);
                m.SetFloat("_Base", baseY);
                m.SetFloat("_Height", L.hazeHeight);
                return m;
            });
            if (_curtain == null) _curtain = CurtainMesh();
            var go = new GameObject("Heat Haze");
            go.transform.SetParent(lava, true);
            go.transform.position = new Vector3(b.center.x, baseY, b.center.z);
            go.transform.rotation = Quaternion.identity;
            var ls = lava.lossyScale;
            go.transform.localScale = new Vector3((b.size.x - 0.1f) / Mathf.Max(ls.x, 1e-4f), L.hazeHeight / Mathf.Max(ls.y, 1e-4f), (b.size.z - 0.1f) / Mathf.Max(ls.z, 1e-4f));
            go.AddComponent<MeshFilter>().sharedMesh = _curtain;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            hazes++;
        }

        /// <summary>A unit open box: four sides from y 0 to 1 round x, z in -0.5..0.5, normals outwards.</summary>
        static Mesh CurtainMesh()
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var d in dirs)
            {
                var side = Vector3.Cross(Vector3.up, d);
                int s = v.Count;
                var c = d * 0.5f;
                v.Add(c - side * 0.5f); v.Add(c + side * 0.5f); v.Add(c + side * 0.5f + Vector3.up); v.Add(c - side * 0.5f + Vector3.up);
                for (int k = 0; k < 4; k++) n.Add(d);
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
            }
            var m = new Mesh { name = "HellLook curtain" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------ per floor: sun, ambient, globals

        public void ApplyFloor(HellFloorLook L)
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.color = L.sun;
                l.intensity = L.sunIntensity;
                l.shadows = LightShadows.Soft;
                l.transform.rotation = Quaternion.Euler(68f, -35f, 0f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = L.ambientSky;
            RenderSettings.ambientEquatorColor = L.ambientEquator;
            RenderSettings.ambientGroundColor = L.ambientGround;
            RenderSettings.ambientProbe = Gradient(L.ambientSky, L.ambientEquator, L.ambientGround);
            Shader.SetGlobalVector(SkHellLightId, new Vector4(L.poolStrength, 1f, 0f, 0f));
            _carry = L.carryLight;
        }

        static bool? _fromConvention;

        /// <summary>The ambient gradient as spherical harmonics: the equator all round, the sky's colour on up-facing normals and
        /// the lava glow's on down-facing ones. (The direction convention of AddDirectionalLight is checked once by evaluating.)</summary>
        public static SphericalHarmonicsL2 Gradient(Color sky, Color equator, Color ground)
        {
            if (!_fromConvention.HasValue)
            {
                var test = new SphericalHarmonicsL2();
                test.AddDirectionalLight(Vector3.up, Color.white, 1f);
                var res = new Color[2];
                test.Evaluate(new[] { Vector3.up, Vector3.down }, res);
                _fromConvention = res[0].r > res[1].r;
            }
            Vector3 upDir = _fromConvention.Value ? Vector3.up : Vector3.down;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(equator);
            sh.AddDirectionalLight(upDir, Positive(sky - equator * 0.5f), 1.6f);
            sh.AddDirectionalLight(-upDir, Positive(ground - equator * 0.5f), 1.6f);
            return sh;
        }

        static Color Positive(Color c) => new Color(Mathf.Max(0f, c.r), Mathf.Max(0f, c.g), Mathf.Max(0f, c.b), 1f);

        /// <summary>The ambient as it now reaches an up-facing, a side-facing and a down-facing surface (for the proof log).</summary>
        public static string AmbientReport()
        {
            var res = new Color[3];
            RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up, Vector3.forward, Vector3.down }, res);
            string F(Color c) => "(" + c.r.ToString("0.000") + ", " + c.g.ToString("0.000") + ", " + c.b.ToString("0.000") + ")";
            return "up " + F(res[0]) + " side " + F(res[1]) + " down " + F(res[2]);
        }

        // ------------------------------------------------------------------ per frame

        float _carry;
        Light _carried;

        /// <summary>The ember the player carries (only where the floor's look asks for one): a warm point light riding the camera.</summary>
        void Carry()
        {
            var cam = Focus != null ? Focus : Camera.main;
            if (_carry <= 0f || cam == null) { if (_carried != null) _carried.enabled = false; return; }
            if (_carried == null)
            {
                _carried = new GameObject("Carried Ember").AddComponent<Light>();
                _carried.type = LightType.Point;
                _carried.color = new Color(1f, 0.62f, 0.32f);
                _carried.range = 11f;
                _carried.shadows = LightShadows.None;
            }
            _carried.enabled = true;
            _carried.intensity = _carry;
            _carried.transform.position = cam.transform.position + cam.transform.forward * 0.6f + Vector3.up * 0.3f;
        }

        void LateUpdate() => Carry();

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < _flames.Count; i++)
            {
                var f = _flames[i];
                if (f.light == null || !f.light.isActiveAndEnabled) continue;
                float n = Mathf.PerlinNoise(f.seed, t * f.speed) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(f.seed + 31.7f, t * f.speed * 2.3f) * 2f - 1f;
                float k = 1f + f.amp * (0.7f * n + 0.3f * n2);
                f.light.intensity = f.baseIntensity * k;
                if (f.flame != null) f.flame.localScale = new Vector3(f.flameScale.x * (1f + 0.1f * n2), f.flameScale.y * (0.85f + 0.3f * (k - 1f + 0.5f)), f.flameScale.z * (1f + 0.1f * n2));
            }
            _shadowClock -= Time.unscaledDeltaTime;
            if (_shadowClock <= 0f) { _shadowClock = 0.3f; ShadowBudget(); }
        }

        readonly List<Light> _near = new List<Light>();

        /// <summary>Hard shadows on the <see cref="ShadowedBraziers"/> braziers nearest the camera (point-light shadows cost six
        /// renders each, so only a few; URP scales their tiles down to share the 2048 atlas).</summary>
        public void ShadowBudget()
        {
            var cam = Focus != null ? Focus : Camera.main;
            if (cam == null) return;
            var p = cam.transform.position;
            _near.Clear();
            foreach (var l in _braziers) if (l != null && l.isActiveAndEnabled) _near.Add(l);
            _near.Sort((a, b) => (a.transform.position - p).sqrMagnitude.CompareTo((b.transform.position - p).sqrMagnitude));
            for (int i = 0; i < _near.Count; i++)
            {
                bool on = i < ShadowedBraziers;
                var want = on ? LightShadows.Hard : LightShadows.None;
                if (_near[i].shadows == want) continue;
                _near[i].shadows = want;
            }
        }
    }
}
