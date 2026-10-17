using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell LOOK TEST (docs/HELL-LOOK-TEST.md): applies one of the art pass's three directions (docs/HELL-ART-PASS.md section 4)
    /// to the built Hell scene at run time, so the creator judges real frames of the game, not palettes. Nothing changes unless it
    /// is asked for: <c>-hellLook A|B|C</c> on the command line, or this component baked into the scene by the look-test builder.
    /// <list type="bullet">
    /// <item><b>A Inked Inferno:</b> the scenery on InkLit (banded, hard-edged light) in the per-floor palette, the comic ink on,
    /// lava as flat bands with a bright shore rim, the lava lights stronger so they paint the walls, floor 3 colour-free.</item>
    /// <item><b>B Molten Grit:</b> CC0 photo textures (basalt, ash, volcanic tiles, rust) box-projected in world space, photo lava
    /// with HDR emission, bloom, ACES, vignette, thick warm fog. Outlines only (no scenery ink).</item>
    /// <item><b>C Black and Ember:</b> near-black colour-free stone into black fog, almost no ambient; only fire, the objective's
    /// glows and the enemies' cores keep colour.</item>
    /// </list>
    /// The hostile outline (<c>-hellOutline</c>) and the safe-path studs (<c>-hellStuds</c>) are switched here too; the pulse's
    /// wind-up is fed to each bot's renderers every frame. Everything is made from build-included materials (the InkLit templates in
    /// Resources/SkylineFx, the Lit templates and textures in Resources/HellLook), so it works in a player.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class HellLookSwitch : MonoBehaviour
    {
        [Tooltip("The baked direction (the command line's -hellLook wins).")]
        public HellLookDirection look = HellLookDirection.A;
        public HellOutlineVariant outline = HellOutlineVariant.SoulfirePulse;
        public HellStudColour studs = HellStudColour.Bone;

        public const string ResourceFolder = "HellLook/";

        /// <summary>The look in force (None outside the look test): the imp's model swap reads it.</summary>
        public static HellLookDirection ActiveLook { get; private set; }
        public static HellOutlineVariant ActiveOutline { get; private set; }
        public static HellLookSwitch Instance { get; private set; }

        static readonly int HostileId = Shader.PropertyToID("_SkHellHostile");
        static readonly int InkWidthId = Shader.PropertyToID("_InkWidth");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        bool _applied;
        readonly List<Material> _scrolling = new List<Material>();
        readonly Dictionary<Material, Material> _swap = new Dictionary<Material, Material>();
        readonly Color[] _fog = new Color[3], _ambient = new Color[3];
        Vector2 _fogRange = new Vector2(40f, 220f);
        FogMode _fogMode = FogMode.Linear;
        float _fogDensity;
        MaterialPropertyBlock _mpb;

        // ------------------------------------------------------------------ boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            ActiveLook = HellLookDirection.None;
            ActiveOutline = HellOutlineVariant.Kit;
            Shader.SetGlobalVector(HostileId, Vector4.zero);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.name.StartsWith("HellDescent")) return;
            var opts = HellLookOptions.Parse(System.Environment.GetCommandLineArgs());
            var sw = Object.FindAnyObjectByType<HellLookSwitch>();
            if (sw == null)
            {
                // Always in the Hell scene since the creator's outline pick (2026-10-17): with no look it restyles nothing, it only
                // turns on the soulfire pulse outline (an outlines-only ink pass) and the bone-white studs. -hellOutline off skips it.
                if (!opts.Any && opts.outline == HellOutlineVariant.Off) return;
                sw = new GameObject("Hell Look (switch)").AddComponent<HellLookSwitch>();
                sw.look = opts.look;
                sw.outline = opts.outline;
                sw.studs = opts.studs;
            }
            if (opts.look != HellLookDirection.None) sw.look = opts.look;
            if (opts.outlineGiven) sw.outline = opts.outline;
            if (opts.studsGiven) sw.studs = opts.studs;
            sw.Apply();
            if (opts.captureDir != null && sw.GetComponent<HellLookCapture>() == null)
                sw.gameObject.AddComponent<HellLookCapture>().outDir = opts.captureDir;
        }

        void Awake() => Instance = this;
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalVector(HostileId, Vector4.zero);
        }

        /// <summary>The imp's model under the look test (the Quaternius "Demon", CC0, made by the look-test builder), or null.</summary>
        public static GameObject ModelOverride(string enemyName)
        {
            if (ActiveLook == HellLookDirection.None || enemyName != "fire-imp") return null;
            return Resources.Load<GameObject>(ResourceFolder + "fire-imp");
        }

        // ------------------------------------------------------------------ apply

        /// <summary>Applies <see cref="look"/>, <see cref="outline"/> and <see cref="studs"/> to the open scene (once).</summary>
        public void Apply()
        {
            if (_applied) { SetOutline(outline); SetStuds(studs); return; }
            _applied = true;
            ActiveLook = look;
            Debug.Log("HELL_LOOK apply " + look + ", outline " + outline + ", studs " + studs);
            if (look == HellLookDirection.None)
            {
                // the plain scene: only the outline (an outlines-only ink pass) and the studs
                if (outline != HellOutlineVariant.Off)
                {
                    Post();
                    foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) PrepareCamera(cam);
                }
                SetOutline(outline);
                SetStuds(studs);
                return;
            }

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int swapped = 0;
            var lavaRenderers = new List<Renderer>();
            foreach (var r in renderers)
            {
                if (r.GetComponentInParent<SkylineBot>() != null) continue;
                var m = r.sharedMaterial;
                if (m == null || !m.name.StartsWith("SK_")) continue;
                string key = Key(m.name);
                if (key == "Lava" || key == "Lava_Well" || key == "WellLava") lavaRenderers.Add(r);
                bool boxUv = look == HellLookDirection.B || key == "Lava" || key == "Lava_Well" || key == "WellLava";
                if (boxUv) BoxUv(r, key.StartsWith("Lava") || key == "WellLava" ? 7f : 4f);
                var made = Swap(m);
                if (made != null && made != m) { r.sharedMaterial = made; swapped++; }
            }
            if (look != HellLookDirection.B) foreach (var r in lavaRenderers) ShoreRim(r);
            Lights();
            Atmosphere();
            Post();
            SetOutline(outline);
            SetStuds(studs);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) PrepareCamera(cam);
            Debug.Log("HELL_LOOK " + look + ": " + swapped + " renderers restyled, " + lavaRenderers.Count + " lava surfaces");
        }

        /// <summary>The material's kind from its builder name: SK_Hell_Stone_F0 -> Stone, SK_Emissive_Lava -> Lava.</summary>
        static string Key(string name)
        {
            string s = name.Replace(" (Instance)", "");
            if (s.StartsWith("SK_Hell_")) s = s.Substring(8);
            else if (s.StartsWith("SK_Emissive_")) s = s.Substring(12);
            else if (s.StartsWith("SK_Glass_")) s = s.Substring(9);
            int f = s.IndexOf("_F", System.StringComparison.Ordinal);
            if (f > 0 && f + 3 == s.Length) s = s.Substring(0, f);
            return s;
        }

        static int FloorOf(string name)
        {
            int f = name.IndexOf("_F", System.StringComparison.Ordinal);
            if (f > 0 && f + 3 <= name.Length && char.IsDigit(name[f + 2])) return name[f + 2] - '0';
            return -1;
        }

        Material Swap(Material src)
        {
            if (_swap.TryGetValue(src, out var done)) return done;
            Material made = null;
            switch (look)
            {
                case HellLookDirection.A: made = InkScenery(src, false); break;
                case HellLookDirection.C: made = InkScenery(src, true); break;
                case HellLookDirection.B: made = GritScenery(src); break;
            }
            _swap[src] = made ?? src;
            return made ?? src;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ---- A and C: InkLit

        static readonly string[] AWall = { "#4A403C", "#6A3A26", "#1E1A26" };
        static readonly string[] AFloor = { "#6E5D50", "#3B2A24", "#2A2630" };
        const string CWall = "#151315", CFloor = "#1E1B1C";

        Material InkScenery(Material src, bool blackAndEmber)
        {
            string key = Key(src.name);
            int floor = FloorOf(src.name);
            int fi = Mathf.Clamp(floor, 0, 2);
            bool f3 = floor == 2;
            Color wall = blackAndEmber ? Hex(CWall) : Hex(AWall[fi]);
            Color ground = blackAndEmber ? Hex(CFloor) : Hex(AFloor[fi]);
            Color? c = null;
            Texture lavaMap = null;
            bool desat = blackAndEmber || f3;
            switch (key)
            {
                case "Stone": c = ground; break;
                case "Deck": c = ground * (blackAndEmber ? 1.45f : 1.3f); break;   // the safe walkways: lighter laid stone
                case "Ramp": c = ground * 1.12f; break;
                case "Wall": c = wall; break;
                case "Cover": c = wall * 1.2f; break;
                case "WallRun": c = wall * 0.9f; break;
                case "Collapse": c = blackAndEmber ? Hex("#100E0E") : Hex("#2E211C"); desat = blackAndEmber; break;
                case "Gate": c = blackAndEmber ? Hex("#2A0E08") : Hex("#6B1E12"); desat = false; break;
                case "Obsidian": c = Hex("#141218"); desat = true; break;
                case "Shaft": c = Hex("#120D0C"); break;
                case "Well": c = blackAndEmber ? Hex("#121011") : Hex("#2A2422"); break;
                case "Chain": c = Hex("#2A2828"); break;
                case "Altar": c = blackAndEmber ? Hex("#3A3740") : Hex("#8C8494"); break;
                case "BeamPost": c = Hex("#261A18"); break;
                case "Lava": case "Lava_Well": case "WellLava": lavaMap = LavaTexture(); desat = false; break;
                default: return null;   // the glows (ember, sigil, lantern, anchor, brand, studs) and the pall keep their own material
            }
            var ink = SkylineInkRoles.FromLit(src, SkylineInkRoles.None);
            if (ink == null) { Debug.LogWarning("HELL_LOOK no InkLit templates in Resources/SkylineFx: " + src.name + " kept"); return null; }
            ink = new Material(ink) { name = src.name + " (look " + look + ")" };
            if (c.HasValue) ink.SetColor(BaseColorId, c.Value);
            if (ink.HasProperty(DesaturateId)) ink.SetFloat(DesaturateId, desat ? 1f : 0f);
            if (lavaMap != null)
            {
                ink.SetTexture(BaseMapId, lavaMap);
                ink.SetTextureScale(BaseMapId, Vector2.one);
                // HDR so the bloom catches it; the bands themselves are flat colours (Hades' lava)
                float glow = key == "Lava" ? 1.7f : 1.4f;
                ink.SetColor(BaseColorId, new Color(glow, glow, glow, 1f));
                ink.SetFloat("_Unlit", 1f);
                _scrolling.Add(ink);
            }
            return ink;
        }

        static Texture2D _lavaTex;

        /// <summary>The cel lava: tileable value noise cut into four flat bands (crust #4A1008, deep red #E2401C, orange #FF9A2E,
        /// pale yellow #FFE9A8), made once at run time (nothing to import).</summary>
        static Texture2D LavaTexture()
        {
            if (_lavaTex != null) return _lavaTex;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "HellLook_CelLava", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            Color crust = Hex("#4A1008"), deep = Hex("#E2401C"), orange = Hex("#FF9A2E"), pale = Hex("#FFE9A8");
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = 0.55f * TileNoise(u, v, 4) + 0.3f * TileNoise(u, v, 8) + 0.15f * TileNoise(u, v, 16);
                    Color col = n < 0.38f ? crust : n < 0.52f ? deep : n < 0.66f ? orange : pale;
                    px[y * N + x] = col;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _lavaTex = tex;
            return tex;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Value noise on a <paramref name="cells"/> grid that wraps at 1 (so the texture tiles).</summary>
        static float TileNoise(float u, float v, int cells)
        {
            float gx = u * cells, gy = v * cells;
            int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
            float fx = gx - x0, fy = gy - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells;
            x0 %= cells; y0 %= cells;
            float a = Mathf.Lerp(Hash(x0, y0), Hash(x1, y0), fx);
            float b = Mathf.Lerp(Hash(x0, y1), Hash(x1, y1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        Material _rimMat;

        /// <summary>The bright shore rim where stone meets lava (Hades): thin pale-yellow strips along the lava box's top edges.</summary>
        void ShoreRim(Renderer lava)
        {
            if (_rimMat == null)
            {
                var src = Resources.Load<Material>(SkylineInkRoles.TemplateFolder + SkylineInkRoles.TemplatePlain);
                if (src == null) return;
                _rimMat = new Material(src) { name = "HellLook_ShoreRim" };
                _rimMat.SetColor(BaseColorId, Hex("#FFE9A8") * 2.6f);
                _rimMat.SetFloat("_Unlit", 1f);
            }
            var b = lava.bounds;
            float w = 0.35f, y0 = b.max.y - 0.01f, y1 = b.max.y + 0.03f;
            var parent = lava.transform;
            Strip(parent, new Vector3(b.min.x - w * 0.5f, y0, b.min.z), new Vector3(b.max.x + w * 0.5f, y1, b.min.z + w));
            Strip(parent, new Vector3(b.min.x - w * 0.5f, y0, b.max.z - w), new Vector3(b.max.x + w * 0.5f, y1, b.max.z));
            Strip(parent, new Vector3(b.min.x, y0, b.min.z), new Vector3(b.min.x + w, y1, b.max.z));
            Strip(parent, new Vector3(b.max.x - w, y0, b.min.z), new Vector3(b.max.x, y1, b.max.z));
        }

        void Strip(Transform parent, Vector3 min, Vector3 max)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Shore Rim";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = (min + max) * 0.5f;
            go.transform.rotation = Quaternion.identity;
            var size = max - min;
            var ps = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / Mathf.Max(ps.x, 1e-4f), size.y / Mathf.Max(ps.y, 1e-4f), size.z / Mathf.Max(ps.z, 1e-4f));
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = _rimMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---- B: photo textures on URP Lit

        Material GritScenery(Material src)
        {
            string key = Key(src.name);
            int floor = Mathf.Clamp(FloorOf(src.name), 0, 2);
            var normalT = Resources.Load<Material>(ResourceFolder + "HL_LitNormal");
            var emisT = Resources.Load<Material>(ResourceFolder + "HL_LitEmission");
            string tex = null;
            Color tint = Color.white;
            float smooth = 0.2f;
            switch (key)
            {
                case "Stone": tex = floor == 0 ? "ash" : floor == 1 ? "tiles" : "basalt"; tint = floor == 2 ? Hex("#6A6878") : floor == 1 ? Hex("#C89A86") : Color.white; smooth = floor == 2 ? 0.55f : 0.15f; break;
                case "Deck": tex = floor == 1 ? "rust" : "tiles"; tint = floor == 2 ? Hex("#8A8898") : Color.white; break;
                case "Ramp": tex = floor == 1 ? "rust" : "tiles"; break;
                case "Wall": case "Cover": case "WallRun": case "Well": case "Shaft":
                    tex = "basalt"; tint = floor == 0 ? Hex("#D8C8B8") : floor == 1 ? Hex("#C08070") : Hex("#606070"); smooth = floor == 2 ? 0.6f : 0.25f; break;
                case "Collapse": tex = "basalt"; tint = Hex("#8A6A5A"); break;
                case "Gate": case "Chain": case "BeamPost": tex = "rust"; break;
                case "Obsidian": tex = "basalt"; tint = Hex("#303040"); smooth = 0.8f; break;
                case "Altar": tex = "tiles"; tint = Hex("#B0A8B8"); break;
                case "Lava": case "Lava_Well": case "WellLava":
                    if (emisT == null) return null;
                    var lava = new Material(emisT) { name = src.name + " (look B)" };
                    var ld = Resources.Load<Texture2D>(ResourceFolder + "Textures/lava_diff");
                    var le = Resources.Load<Texture2D>(ResourceFolder + "Textures/lava_emis");
                    lava.SetTexture(BaseMapId, ld);
                    lava.SetColor(BaseColorId, Hex("#5A2A1A"));
                    lava.SetTexture(EmissionMapId, le != null ? le : ld);
                    lava.SetColor(EmissionColorId, key == "Lava" ? new Color(5.0f, 1.9f, 0.45f) : new Color(3.6f, 1.2f, 0.25f));
                    _scrolling.Add(lava);
                    return lava;
                default: return null;   // glows keep their Unlit material (HDR: bloom catches them)
            }
            if (normalT == null) { Debug.LogWarning("HELL_LOOK Resources/HellLook/HL_LitNormal missing (run the look-test builder): " + src.name + " kept"); return null; }
            var m = new Material(normalT) { name = src.name + " (look B)" };
            m.SetTexture(BaseMapId, Resources.Load<Texture2D>(ResourceFolder + "Textures/" + tex + "_diff"));
            m.SetTexture(BumpMapId, Resources.Load<Texture2D>(ResourceFolder + "Textures/" + tex + "_nor"));
            m.SetColor(BaseColorId, tint);
            if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, smooth);
            return m;
        }

        // ---- world-space box UVs (the room boxes are scaled cubes: their own UVs stretch one texture over 180 m)

        static readonly Dictionary<Mesh, bool> _ownMeshes = new Dictionary<Mesh, bool>();

        static void BoxUv(Renderer r, float tile)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || _ownMeshes.ContainsKey(mf.sharedMesh)) return;
            var src = mf.sharedMesh;
            Mesh mesh;
            if (src.name.StartsWith("Cube") && src.vertexCount == 24) mesh = UnitCube();
            else if (src.isReadable) mesh = Object.Instantiate(src);
            else return;
            var verts = mesh.vertices;
            var normals = mesh.normals;
            var uv = new Vector2[verts.Length];
            var l2w = r.transform.localToWorldMatrix;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p = l2w.MultiplyPoint3x4(verts[i]);
                Vector3 n = l2w.MultiplyVector(normals[i]);
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                if (ay >= ax && ay >= az) uv[i] = new Vector2(p.x, p.z) / tile;
                else if (ax >= az) uv[i] = new Vector2(p.z, p.y) / tile;
                else uv[i] = new Vector2(p.x, p.y) / tile;
            }
            mesh.uv = uv;
            mesh.name = "HellLook box-uv";
            _ownMeshes[mesh] = true;
            mf.sharedMesh = mesh;
        }

        /// <summary>A unit cube (24 vertices, a face per axis direction), centred, like Unity's primitive.</summary>
        static Mesh UnitCube()
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var d in dirs)
            {
                Vector3 a = d.y != 0f ? Vector3.forward : Vector3.up;
                Vector3 b = Vector3.Cross(d, a);
                int s = v.Count;
                v.Add((d - a - b) * 0.5f); v.Add((d - a + b) * 0.5f); v.Add((d + a + b) * 0.5f); v.Add((d + a - b) * 0.5f);
                for (int k = 0; k < 4; k++) n.Add(d);
                // wound clockwise seen from outside (Unity's front face)
                t.Add(s); t.Add(s + 2); t.Add(s + 1);
                t.Add(s); t.Add(s + 3); t.Add(s + 2);
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }

        // ---- lights, atmosphere, post

        void Lights()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = look == HellLookDirection.C ? 0.06f : look == HellLookDirection.B ? 0.45f : 0.3f;
                    continue;
                }
                if (l.name != "Lava Light" && l.name != "Shaft Light") continue;
                switch (look)
                {
                    case HellLookDirection.A: l.intensity = 4.5f; l.range *= 1.35f; l.color = new Color(1f, 0.5f, 0.16f); break;
                    case HellLookDirection.B: l.intensity = 3.4f; l.range *= 1.3f; l.color = new Color(1f, 0.45f, 0.15f); break;
                    case HellLookDirection.C: l.intensity = 3.2f; l.range *= 0.85f; l.color = new Color(1f, 0.38f, 0.1f); break;
                }
            }
        }

        void Atmosphere()
        {
            switch (look)
            {
                case HellLookDirection.A:
                    // per floor: warm ash-brown, deep red, violet-black (the art pass's fog plan)
                    _fog[0] = Hex("#2A1C16"); _fog[1] = Hex("#3A0C06"); _fog[2] = Hex("#0E0914");
                    _ambient[0] = new Color(0.20f, 0.15f, 0.13f); _ambient[1] = new Color(0.20f, 0.08f, 0.06f); _ambient[2] = new Color(0.08f, 0.07f, 0.10f);
                    _fogMode = FogMode.Linear; _fogRange = new Vector2(35f, 200f);
                    break;
                case HellLookDirection.B:
                    _fog[0] = Hex("#4A2A1A"); _fog[1] = Hex("#5A1E0E"); _fog[2] = Hex("#1A1218");
                    _ambient[0] = new Color(0.16f, 0.11f, 0.09f); _ambient[1] = new Color(0.17f, 0.08f, 0.05f); _ambient[2] = new Color(0.07f, 0.06f, 0.08f);
                    _fogMode = FogMode.ExponentialSquared; _fogDensity = 0.011f;
                    break;
                case HellLookDirection.C:
                    for (int i = 0; i < 3; i++) { _fog[i] = new Color(0.006f, 0.005f, 0.005f); _ambient[i] = new Color(0.05f, 0.045f, 0.045f); }
                    _fogMode = FogMode.Linear; _fogRange = new Vector2(10f, 95f);
                    break;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = _fogMode;
            RenderSettings.fogStartDistance = _fogRange.x;
            RenderSettings.fogEndDistance = _fogRange.y;
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            AtmosphereFor(0);
        }

        int _atmosFloor = -1;
        /// <summary>The capture's shot floor (-1: follow the main camera).</summary>
        [System.NonSerialized] public int floorOverride = -1;

        public void AtmosphereFor(int floor)
        {
            floor = Mathf.Clamp(floor, 0, 2);
            if (floor == _atmosFloor) return;
            _atmosFloor = floor;
            RenderSettings.fogColor = _fog[floor];
            RenderSettings.ambientLight = _ambient[floor];
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = _fog[floor];
        }

        Volume _volume;

        void Post()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "HellLook " + look;
            var ink = profile.Add<SkylineInk>(true);
            ink.inkEnabled.Override(true);
            var tone = profile.Add<Tonemapping>(true);
            var bloom = profile.Add<Bloom>(true);
            var vig = profile.Add<Vignette>(true);
            switch (look)
            {
                case HellLookDirection.None:
                    // outlines only, nothing else changes (no tonemapping, bloom or vignette)
                    ink.fadeStart.Override(0f); ink.fadeEnd.Override(0.05f); ink.farInk.Override(0f);
                    tone.active = false; bloom.active = false; vig.active = false;
                    break;
                case HellLookDirection.A:
                    ink.inkColor.Override(Hex("#16141F"));
                    tone.mode.Override(TonemappingMode.Neutral);
                    bloom.threshold.Override(1.1f); bloom.intensity.Override(0.55f); bloom.scatter.Override(0.6f);
                    vig.intensity.Override(0.22f);
                    break;
                case HellLookDirection.B:
                    // outlines only: the scenery lines fade to nothing from the first metre (the hued outlines don't fade)
                    ink.fadeStart.Override(0f); ink.fadeEnd.Override(0.05f); ink.farInk.Override(0f);
                    tone.mode.Override(TonemappingMode.ACES);
                    bloom.threshold.Override(0.95f); bloom.intensity.Override(1.3f); bloom.scatter.Override(0.72f);
                    vig.intensity.Override(0.34f);
                    var ca = profile.Add<ColorAdjustments>(true);
                    ca.postExposure.Override(0.35f); ca.contrast.Override(12f);
                    break;
                case HellLookDirection.C:
                    ink.inkColor.Override(Hex("#050406"));
                    ink.farInk.Override(0.15f);
                    tone.mode.Override(TonemappingMode.Neutral);
                    bloom.threshold.Override(1.0f); bloom.intensity.Override(0.8f); bloom.scatter.Override(0.65f);
                    vig.intensity.Override(0.4f);
                    break;
            }
            var go = new GameObject("Hell Look (post " + look + ")");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 100f;
            _volume.sharedProfile = profile;
        }

        public static void PrepareCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && data.renderType == CameraRenderType.Base) data.renderPostProcessing = true;
            cam.allowHDR = true;
        }

        // ---- outline and studs

        public void SetOutline(HellOutlineVariant v)
        {
            outline = v;
            ActiveOutline = v;
            var h = HellLookOptions.Hue(v == HellOutlineVariant.Off ? HellOutlineVariant.Kit : v);
            var lin = new Color(h.r, h.g, h.b).linear;
            Shader.SetGlobalVector(HostileId, new Vector4(lin.r, lin.g, lin.b, h.mode));
        }

        readonly Dictionary<Renderer, Material> _studOriginal = new Dictionary<Renderer, Material>();
        Material _bone, _stone;

        public void SetStuds(HellStudColour c)
        {
            studs = c;
            if (_studOriginal.Count == 0)
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (r.name == "Guide" && r.transform.parent != null && r.transform.parent.name == "Safe Guides") _studOriginal[r] = r.sharedMaterial;
            if (_bone == null)
            {
                var t = Resources.Load<Material>(SkylineInkRoles.TemplateFolder + SkylineInkRoles.TemplatePlain);
                if (t == null) return;
                _bone = new Material(t) { name = "HellLook_Studs_Bone" };
                _bone.SetColor(BaseColorId, Hex("#EDE3D0") * 1.3f);
                _bone.SetFloat("_Unlit", 1f);
                _stone = new Material(t) { name = "HellLook_Studs_Stone" };
                _stone.SetColor(BaseColorId, Hex("#4E4A46"));
                _stone.SetFloat("_Unlit", 1f);
            }
            foreach (var kv in _studOriginal)
                if (kv.Key != null) kv.Key.sharedMaterial = c == HellStudColour.Bone ? _bone : c == HellStudColour.Stone ? _stone : kv.Value;
        }

        // ------------------------------------------------------------------ per frame

        /// <summary>Wind-ups forced by the capture (bot -> 0..1), so a still shows idle, alert and about-to-fire imps.</summary>
        public readonly Dictionary<SkylineBot, float> forcedWindup = new Dictionary<SkylineBot, float>();

        void Update()
        {
            if (HellLookOptions.Pulses(outline)) FeedWindups();
            if (look == HellLookDirection.None) return;
            var cam = Camera.main;
            if (floorOverride >= 0) AtmosphereFor(floorOverride);
            else if (cam != null) AtmosphereFor(HellLookOptions.FloorAt(cam.transform.position.z));
            float t = Time.time;
            foreach (var m in _scrolling)
                if (m != null) m.SetTextureOffset(BaseMapId, new Vector2(t * 0.012f, t * 0.02f));
        }

        void FeedWindups()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var bot in SkylineBot.Live)
            {
                if (bot == null) continue;
                float w;
                if (!forcedWindup.TryGetValue(bot, out w))
                {
                    w = 0f;
                    if (bot.Brain != null)
                    {
                        float tel = bot.Def != null ? Mathf.Max(0.2f, bot.Def.telegraphSeconds) : 0.5f;
                        // idle 0 (a slow breath); alert 0.3; through the telegraph 0.5 rising to 1 at the moment it fires
                        if (bot.Brain.State == BotState.Telegraph) w = 0.5f + 0.5f * Mathf.Clamp01(bot.Brain.StateTime / tel);
                        else if (bot.Brain.State == BotState.Alert) w = 0.3f;
                    }
                }
                foreach (var r in bot.GetComponentsInChildren<Renderer>())
                {
                    var m = r.sharedMaterial;
                    if (SkylineInkRoles.GetRole(m) != SkylineInkRoles.Hostile) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(InkWidthId, 1f - w);
                    r.SetPropertyBlock(_mpb);
                }
            }
        }
    }
}
