using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell LOOK (docs/HELL-LOOK-TEST.md, docs/HELL-LIGHTING.md): applies one of the three art directions to the built Hell
    /// scene at run time, per floor if asked (<c>-hellLookFloors B,A,C</c>), so the creator judges real frames of the game.
    /// Nothing changes unless it is asked for: <c>-hellLook A|B|C</c>, <c>-hellLookFloors</c>, or this component baked into the scene.
    /// <list type="bullet">
    /// <item>Every direction now shares one surface shader and one light rig, so they compare fairly: <b>Hell/Surface</b>
    /// (world triplanar CC0 textures, walls and floors from different sets, edge trims, normal maps, lava-heated veins) and
    /// <see cref="HellLightRig"/> (lava lights that reach, braziers, flicker, shadows, a gradient ambient), plus
    /// <see cref="HellBeyond"/> (Hell's sky and the spires and chains beyond the rooms).</item>
    /// <item><b>A Inked Inferno:</b> the textures painted (posterized), comic-ink light bands and ink lines, neutral tonemap.</item>
    /// <item><b>B Molten Grit:</b> the photo textures under URP's PBR light, ACES, bloom, warm fog.</item>
    /// <item><b>C Black and Ember:</b> colour-free near-black stone, only fire coloured, black fog; the fire bright enough to navigate by.</item>
    /// </list>
    /// The hostile outline (<c>-hellOutline</c>) and the safe-path studs (<c>-hellStuds</c>) are switched here too; the pulse's
    /// wind-up is fed to each bot's renderers every frame; every enemy is re-dressed as it spawns (charred hide with glowing
    /// ember cracks, molten ooze). Everything is made from build-included materials (Resources/HellLook, Resources/SkylineFx).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class HellLookSwitch : MonoBehaviour
    {
        [Tooltip("The baked direction (the command line's -hellLook wins).")]
        public HellLookDirection look = HellLookDirection.A;
        [Tooltip("Per floor (empty or None: the look above). The command line's -hellLookFloors wins.")]
        public HellLookDirection[] floorLooks = new HellLookDirection[3];
        public HellOutlineVariant outline = HellOutlineVariant.SoulfirePulse;
        public HellStudColour studs = HellStudColour.Bone;

        public const string ResourceFolder = "HellLook/";

        /// <summary>The game's look per floor with no switches (the creator's pick, 2026-10-18): floor 1 A Inked Inferno, floors 2
        /// and 3 B Molten Grit (floor 2's floor re-textured: scorched cracked ground, corroded walkways). Baked into the scene by
        /// <c>HellLookBuild.BakeDefault</c>; a scene without a switch gets it at run time too.</summary>
        public static readonly HellLookDirection[] DefaultFloorLooks = { HellLookDirection.A, HellLookDirection.B, HellLookDirection.B };

        /// <summary>The look in force on floor 1 (None outside the look test): the imp's model swap reads it.</summary>
        public static HellLookDirection ActiveLook { get; private set; }
        public static HellOutlineVariant ActiveOutline { get; private set; }
        public static HellLookSwitch Instance { get; private set; }

        static readonly int HostileId = Shader.PropertyToID("_SkHellHostile");
        static readonly int InkWidthId = Shader.PropertyToID("_InkWidth");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");
        static readonly int UnlitId = Shader.PropertyToID("_Unlit");
        static readonly int SkHellLightId = Shader.PropertyToID("_SkHellLight");

        bool _applied;
        readonly List<Material> _scrolling = new List<Material>();
        readonly Dictionary<(Material, int), Material> _swap = new Dictionary<(Material, int), Material>();
        readonly HellFloorLook[] _L = new HellFloorLook[3];
        readonly HellLookDirection[] _dir = new HellLookDirection[3];
        readonly Dictionary<HellLookDirection, Volume> _volumes = new Dictionary<HellLookDirection, Volume>();
        HellLightRig _rig;
        HellBeyond _beyond;
        MaterialPropertyBlock _mpb;

        public HellLookDirection DirectionOn(int floor) => _dir[Mathf.Clamp(floor, 0, 2)];
        public HellFloorLook LookOn(int floor) => _L[Mathf.Clamp(floor, 0, 2)];
        /// <summary>True when the floors don't all share one direction.</summary>
        public bool Mixed => _dir[0] != _dir[1] || _dir[1] != _dir[2];

        // ------------------------------------------------------------------ boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            ActiveLook = HellLookDirection.None;
            ActiveOutline = HellOutlineVariant.Kit;
            Shader.SetGlobalVector(HostileId, Vector4.zero);
            Shader.SetGlobalVector(SkHellLightId, Vector4.zero);
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
                // Always in the Hell scene. Since the creator's pick (2026-10-18) the game's default is DefaultFloorLooks (floor 1
                // Inked Inferno, floors 2 and 3 Molten Grit); -hellLook / -hellLookFloors override it for comparison and capture.
                sw = new GameObject("Hell Look (switch)").AddComponent<HellLookSwitch>();
                sw.look = DefaultFloorLooks[0];
                sw.floorLooks = (HellLookDirection[])DefaultFloorLooks.Clone();
                sw.outline = opts.outline;
                sw.studs = opts.studs;
            }
            if (opts.look != HellLookDirection.None) sw.look = opts.look;
            if (sw.floorLooks == null || sw.floorLooks.Length < 3) sw.floorLooks = new HellLookDirection[3];
            for (int f = 0; f < 3; f++)
            {
                if (opts.floors != null) sw.floorLooks[f] = opts.ForFloor(f);
                else if (opts.look != HellLookDirection.None) sw.floorLooks[f] = opts.look;
            }
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
            Shader.SetGlobalVector(SkHellLightId, Vector4.zero);
        }

        /// <summary>The imp's model under a look (the Quaternius "Demon", CC0, made by the look-test builder), or null.</summary>
        public static GameObject ModelOverride(string enemyName)
        {
            if (ActiveLook == HellLookDirection.None || enemyName != "fire-imp") return null;
            // a real drop-in model (Resources/HellModels/fire-imp, the enemy-models lane) wins over the look test's stand-in
            if (Resources.Load<GameObject>(HellBodies.ModelFolder + enemyName) != null) return null;
            return Resources.Load<GameObject>(ResourceFolder + "fire-imp");
        }

        // ------------------------------------------------------------------ apply

        /// <summary>Applies the directions, <see cref="outline"/> and <see cref="studs"/> to the open scene (once).</summary>
        public void Apply()
        {
            if (_applied) { SetOutline(outline); SetStuds(studs); return; }
            _applied = true;
            for (int f = 0; f < 3; f++)
            {
                var d = floorLooks != null && f < floorLooks.Length && floorLooks[f] != HellLookDirection.None ? floorLooks[f] : look;
                _dir[f] = d;
            }
            if (look == HellLookDirection.None) look = _dir[0];
            ActiveLook = look;
            Debug.Log("HELL_LOOK apply " + look + " (floors " + _dir[0] + "," + _dir[1] + "," + _dir[2] + "), outline " + outline + ", studs " + studs);
            if (look == HellLookDirection.None)
            {
                // the plain scene: only the outline (an outlines-only ink pass) and the studs
                if (outline != HellOutlineVariant.Off)
                {
                    PostNone();
                    foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) PrepareCamera(cam);
                }
                SetOutline(outline);
                SetStuds(studs);
                return;
            }
            for (int f = 0; f < 3; f++) _L[f] = HellLookPalette.For(_dir[f], f);
            HellFxPalette.Use(look == HellLookDirection.B ? HellFxLook.MoltenGrit : look == HellLookDirection.C ? HellFxLook.BlackAndEmber : HellFxLook.InkedInferno);

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int swapped = 0;
            var lavaRenderers = new List<(Renderer, int)>();
            foreach (var r in renderers)
            {
                if (r.GetComponentInParent<SkylineBot>() != null) continue;
                var m = r.sharedMaterial;
                if (m == null || !m.name.StartsWith("SK_")) continue;
                string key = Key(m.name);
                int floor = FloorOf(m.name);
                if (floor < 0) floor = HellLookOptions.FloorAt(r.bounds.center.z);
                bool lava = key == "Lava" || key == "Lava_Well" || key == "WellLava";
                if (lava) { lavaRenderers.Add((r, floor)); BoxUv(r, 7f); }
                var mf = r.GetComponent<MeshFilter>();
                bool ramp = mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Wedge");
                var made = Swap(m, floor, ramp);
                if (made != null && made != m) { r.sharedMaterial = made; swapped++; }
            }
            foreach (var (r, f) in lavaRenderers) if (_dir[f] != HellLookDirection.B) ShoreRim(r);
            SigilBeams();

            _rig = gameObject.AddComponent<HellLightRig>();
            _rig.lookFor = f => _L[Mathf.Clamp(f, 0, 2)];
            _rig.Build();
            _beyond = gameObject.AddComponent<HellBeyond>();
            _beyond.lookFor = f => _L[Mathf.Clamp(f, 0, 2)];
            _beyond.Build();
            Post();
            SetOutline(outline);
            SetStuds(studs);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) PrepareCamera(cam);
            AtmosphereFor(0, true);
            Debug.Log("HELL_LOOK " + look + ": " + swapped + " renderers restyled, " + lavaRenderers.Count + " lava surfaces");
            Debug.Log("HELL_LIGHT ambient (floor 1): " + HellLightRig.AmbientReport());
            LogPipeline();
        }

        /// <summary>The light rig the look built (null before <see cref="Apply"/> or without a look).</summary>
        public HellLightRig Rig => _rig;

        /// <summary>Restyles geometry built at run time after <see cref="Apply"/> (Vesper's arena, built when her fight begins):
        /// every renderer under <paramref name="root"/> with a builder-named material (SK_Hell_Stone_F2, SK_Emissive_Lava_F2, ...)
        /// gets the floor's look exactly as the scene's rooms got it. Returns the renderers restyled.</summary>
        public int Restyle(Transform root, int floor)
        {
            if (!_applied || look == HellLookDirection.None || root == null) return 0;
            floor = Mathf.Clamp(floor, 0, 2);
            if (_L[floor] == null) return 0;
            int swapped = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || !m.name.StartsWith("SK_")) continue;
                string key = Key(m.name);
                bool lava = key == "Lava" || key == "Lava_Well" || key == "WellLava";
                if (lava) BoxUv(r, 7f);
                var mf = r.GetComponent<MeshFilter>();
                bool ramp = mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Wedge");
                var made = Swap(m, floor, ramp);
                if (made != null && made != m) { r.sharedMaterial = made; swapped++; }
                if (lava && _dir[floor] != HellLookDirection.B) ShoreRim(r);
            }
            return swapped;
        }

        /// <summary>The proof lines for docs/HELL-LIGHTING.md: the pipeline as the player runs it.</summary>
        static void LogPipeline()
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) { Debug.Log("HELL_LIGHT pipeline: not URP"); return; }
            int lights = 0, shadowed = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) { lights++; if (l.shadows != LightShadows.None) shadowed++; }
            Debug.Log("HELL_LIGHT pipeline " + asset.name + ": quality " + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                      ", HDR " + asset.supportsHDR + ", additional lights " + asset.additionalLightsRenderingMode + " (per object limit " + asset.maxAdditionalLightsCount +
                      "), additional shadows " + asset.supportsAdditionalLightShadows + ", shadow distance " + asset.shadowDistance +
                      ", main shadows " + asset.supportsMainLightShadows + "; active lights now " + lights + " (" + shadowed + " shadowed)");
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

        Material Swap(Material src, int floor, bool ramp)
        {
            var k = (src, floor * 2 + (ramp ? 1 : 0));
            if (_swap.TryGetValue(k, out var done)) return done;
            var made = Scenery(src, floor, ramp);
            _swap[k] = made ?? src;
            return made ?? src;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ------------------------------------------------------------------ surfaces (Hell/Surface)

        static Texture2D Tex(string name, bool normal)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Resources.Load<Texture2D>(ResourceFolder + "Textures/" + name + (normal ? "_nor" : "_diff"));
        }

        /// <summary>A Hell/Surface material: <paramref name="topTex"/> on up-facing surfaces, <paramref name="sideTex"/> on the rest,
        /// lit in the floor's direction (bands for A, PBR for B and C), with the trim on cube-shaped boxes.</summary>
        public static Material Surface(string name, string topTex, string sideTex, Color topTint, Color sideTint, Color trimTint, HellFloorLook L, bool trim)
        {
            var t = Resources.Load<Material>(ResourceFolder + "HL_Surface");
            if (t == null) { Debug.LogWarning("HELL_LOOK Resources/HellLook/HL_Surface missing (run HellLookBuild.PrepareAssets)"); return null; }
            var m = new Material(t) { name = "HellLook " + name };
            m.SetTexture("_TopMap", Tex(topTex, false));
            m.SetTexture("_TopNormal", Tex(topTex, true));
            m.SetTexture("_SideMap", Tex(sideTex, false));
            m.SetTexture("_SideNormal", Tex(sideTex, true));
            m.SetColor("_TopTint", topTint);
            m.SetColor("_SideTint", sideTint);
            m.SetColor("_TrimTint", trimTint);
            m.SetFloat("_TopTile", topTex == L.floorTex ? L.floorTile : topTex == L.deckTex ? L.deckTile : L.wallTile);
            m.SetFloat("_SideTile", L.wallTile);
            m.SetFloat("_TopSmooth", L.floorSmooth);
            m.SetFloat("_SideSmooth", L.wallSmooth);
            m.SetFloat("_Desaturate", L.desaturate);
            m.SetFloat("_Posterize", L.posterize);
            m.SetFloat("_Bands", L.bands);
            m.SetFloat("_Contrast", L.contrast);
            m.SetFloat("_NormalStrength", L.normal);
            m.SetFloat("_TrimOn", trim ? 1f : 0f);
            m.SetColor("_VeinColor", L.veinColor);
            m.SetFloat("_VeinTop", -0.3f);
            m.SetFloat("_VeinFade", 0.9f);
            return m;
        }

        Material Scenery(Material src, int floor, bool ramp)
        {
            string key = Key(src.name);
            var L = _L[floor];
            var dir = _dir[floor];
            bool trim = !ramp;
            Material m;
            switch (key)
            {
                case "Stone": m = Surface(key, L.floorTex, L.wallTex, L.floorTint, L.wallTint * 0.85f, L.trimTint, L, trim); break;
                case "Deck": m = Surface(key, L.deckTex, L.wallTex, L.deckTint, L.wallTint * 0.85f, L.trimTint * 1.2f, L, trim); break;
                case "Ramp": m = Surface(key, L.deckTex, L.wallTex, L.deckTint, L.wallTint * 0.85f, L.trimTint, L, false); break;
                case "Wall": m = Surface(key, L.wallTex, L.wallTex, L.wallTint * 0.9f, L.wallTint, L.trimTint, L, trim); break;
                case "WallRun": m = Surface(key, L.wallTex, L.wallTex, L.wallTint * 0.9f, L.wallTint * 1.1f, L.trimTint * 1.3f, L, trim); break;
                case "Cover": m = Surface(key, L.coverTex, L.coverTex, L.coverTint, L.coverTint, L.trimTint, L, trim); break;
                case "Collapse":
                    m = Surface(key, "basalt", "basalt", L.floorTint * 0.8f, L.wallTint * 0.7f, L.trimTint, L, trim);
                    if (m != null) { m.SetFloat("_VeinTop", 4f); m.SetFloat("_VeinFade", 6f); m.SetColor("_VeinColor", L.veinColor * 0.7f); }
                    break;
                case "Gate": m = Surface(key, "rust", "rust", Hex("#B0583A"), Hex("#B0583A"), L.trimTint, L, trim); if (m != null && dir != HellLookDirection.C) m.SetFloat("_Desaturate", 0f); break;
                case "Obsidian":
                    m = Surface(key, "basalt", "basalt", Hex("#4A4658"), Hex("#4A4658"), Hex("#2A2633"), L, false);
                    if (m != null) { m.SetFloat("_TopSmooth", 0.85f); m.SetFloat("_TopTile", 3f); }
                    break;
                case "Shaft": m = Surface(key, L.wallTex, L.wallTex, L.wallTint * 0.5f, L.wallTint * 0.55f, L.trimTint, L, trim); break;
                case "Well": m = Surface(key, L.wallTex, L.coverTex, L.wallTint * 0.7f, L.coverTint * 0.8f, L.trimTint, L, trim); break;
                case "Chain": case "BeamPost": m = Surface(key, "rust", "rust", Hex("#7A5A50"), Hex("#7A5A50"), L.trimTint, L, false); break;
                case "Altar": m = Surface(key, "tiles", "tiles", Hex("#C8C0D0"), Hex("#A8A0B0"), Hex("#E0D8E8"), L, trim); break;
                case "Lava": case "Lava_Well": case "WellLava": return Lava(src, key, dir);
                default: return null;   // the glows (ember, sigil, lantern, anchor, brand, studs) and the pall keep their own material
            }
            if (m != null) m.name = src.name + " (look " + dir + ")";
            return m;
        }

        // ---- lava

        Material Lava(Material src, string key, HellLookDirection dir)
        {
            if (dir == HellLookDirection.B)
            {
                var emisT = Resources.Load<Material>(ResourceFolder + "HL_LitEmission");
                if (emisT == null) return null;
                var lava = new Material(emisT) { name = src.name + " (look B)" };
                var ld = Resources.Load<Texture2D>(ResourceFolder + "Textures/lava_diff");
                var le = Resources.Load<Texture2D>(ResourceFolder + "Textures/lava_emis");
                lava.SetTexture(BaseMapId, ld);
                lava.SetColor(BaseColorId, Hex("#5A2A1A"));
                lava.SetTexture(EmissionMapId, le != null ? le : ld);
                lava.SetColor(EmissionColorId, key == "Lava" ? new Color(4.6f, 1.45f, 0.22f) : new Color(3.4f, 1.0f, 0.15f));
                _scrolling.Add(lava);
                return lava;
            }
            // A and C: the cel lava (flat bands, Hades) on InkLit unlit, HDR so the bloom catches it
            var ink = SkylineInkRoles.FromLit(src, SkylineInkRoles.None);
            if (ink == null) return null;
            ink = new Material(ink) { name = src.name + " (look " + dir + ")" };
            ink.SetTexture(BaseMapId, LavaTexture());
            ink.SetTextureScale(BaseMapId, Vector2.one);
            float glow = key == "Lava" ? 1.7f : 1.4f;
            if (dir == HellLookDirection.C) glow *= 1.15f;
            ink.SetColor(BaseColorId, new Color(glow, glow, glow, 1f));
            ink.SetFloat(UnlitId, 1f);
            if (ink.HasProperty(DesaturateId)) ink.SetFloat(DesaturateId, 0f);
            _scrolling.Add(ink);
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
                    float n = 0.55f * HellLookTextures.TileNoise(u, v, 4) + 0.3f * HellLookTextures.TileNoise(u, v, 8) + 0.15f * HellLookTextures.TileNoise(u, v, 16);
                    px[y * N + x] = n < 0.38f ? crust : n < 0.52f ? deep : n < 0.66f ? orange : pale;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _lavaTex = tex;
            return tex;
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
                _rimMat.SetFloat(UnlitId, 1f);
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

        // ---- world-space box UVs for the lava (the room boxes are scaled cubes: their own UVs stretch one texture over 180 m)

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

        // ---- the sigils' beams: light pillars that fade upward (they were solid 9 m violet posts that just stopped)

        void SigilBeams()
        {
            var t = Resources.Load<Material>(ResourceFolder + "HL_Haze");
            if (t == null) return;
            var mats = new Dictionary<int, Material>();
            int n = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.name != "Sigil Beam") continue;
                var b = r.bounds;
                int key = Mathf.RoundToInt(b.min.y * 10f);
                if (!mats.TryGetValue(key, out var m))
                {
                    m = new Material(t) { name = "HellLook_SigilBeam" };
                    m.SetColor("_Color", new Color(0.55f, 0.18f, 1.1f));
                    m.SetFloat("_Base", b.min.y);
                    m.SetFloat("_Height", 14f);
                    m.SetFloat("_Power", 1.3f);
                    m.SetFloat("_Shimmer", 0.25f);
                    mats[key] = m;
                }
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                var s = r.transform.localScale;
                r.transform.localScale = new Vector3(s.x * 2.2f, s.y * 1.5f, s.z * 2.2f);
                r.transform.position += Vector3.up * (b.size.y * 0.25f);
                n++;
            }
            if (n > 0) Debug.Log("HELL_LOOK sigil beams as light pillars: " + n);
        }

        // ------------------------------------------------------------------ atmosphere and post, per floor

        int _atmosFloor = -1;
        /// <summary>The capture's shot floor (-1: follow the main camera).</summary>
        [System.NonSerialized] public int floorOverride = -1;

        public void AtmosphereFor(int floor) => AtmosphereFor(floor, false);

        public void AtmosphereFor(int floor, bool force)
        {
            floor = Mathf.Clamp(floor, 0, 2);
            if (floor == _atmosFloor && !force) return;
            _atmosFloor = floor;
            var L = _L[floor];
            if (L == null) return;
            RenderSettings.fog = true;
            RenderSettings.fogMode = L.fogMode;
            RenderSettings.fogColor = L.fog;
            RenderSettings.fogDensity = L.fogDensity;
            RenderSettings.fogStartDistance = L.fogStart;
            RenderSettings.fogEndDistance = L.fogEnd;
            if (_rig != null) _rig.ApplyFloor(L);
            if (_beyond != null) _beyond.ApplyFloor(L);
            foreach (var kv in _volumes) if (kv.Value != null) kv.Value.weight = kv.Key == _dir[floor] ? 1f : 0f;
        }

        void PostNone()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "HellLook none";
            var ink = profile.Add<SkylineInk>(true);
            ink.inkEnabled.Override(true);
            ink.fadeStart.Override(0f); ink.fadeEnd.Override(0.05f); ink.farInk.Override(0f);
            Volume(profile, "none");
        }

        /// <summary>One post volume per direction in use; <see cref="AtmosphereFor"/> weights in the floor's.</summary>
        void Post()
        {
            for (int f = 0; f < 3; f++)
            {
                var d = _dir[f];
                if (_volumes.ContainsKey(d)) continue;
                var L = _L[f];
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "HellLook " + d;
                var ink = profile.Add<SkylineInk>(true);
                ink.inkEnabled.Override(true);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(L.aces ? TonemappingMode.ACES : TonemappingMode.Neutral);
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(L.bloomThreshold); bloom.intensity.Override(L.bloomIntensity); bloom.scatter.Override(0.68f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.Override(L.vignette);
                var ca = profile.Add<ColorAdjustments>(true);
                ca.postExposure.Override(L.exposure);
                switch (d)
                {
                    case HellLookDirection.A:
                        ink.inkColor.Override(Hex("#16141F"));
                        ca.contrast.Override(8f);
                        break;
                    case HellLookDirection.B:
                        // outlines only: the scenery lines fade to nothing from the first metre (the hued outlines don't fade)
                        ink.fadeStart.Override(0f); ink.fadeEnd.Override(0.05f); ink.farInk.Override(0f);
                        ca.contrast.Override(14f);
                        break;
                    case HellLookDirection.C:
                        ink.inkColor.Override(Hex("#050406"));
                        ink.farInk.Override(0.15f);
                        ca.contrast.Override(18f);
                        break;
                }
                _volumes[d] = Volume(profile, d.ToString());
            }
        }

        Volume Volume(VolumeProfile profile, string name)
        {
            var go = new GameObject("Hell Look (post " + name + ")");
            go.transform.SetParent(transform, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 100f;
            v.sharedProfile = profile;
            return v;
        }

        public static void PrepareCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && data.renderType == CameraRenderType.Base) data.renderPostProcessing = true;
            cam.allowHDR = true;
            if (ActiveLook != HellLookDirection.None) HellBeyond.SkyCamera(cam);
        }

        // ------------------------------------------------------------------ outline and studs

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

        /// <summary>The safe path's studs. They were 24 cm squares 6 cm high (a pixel or two at 20 m: the creator never saw them):
        /// now 60 cm bone diamonds, raised, glowing enough for the bloom.</summary>
        public void SetStuds(HellStudColour c)
        {
            studs = c;
            if (_studOriginal.Count == 0)
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (r.name == "Guide" && r.transform.parent != null && r.transform.parent.name == "Safe Guides")
                    {
                        _studOriginal[r] = r.sharedMaterial;
                        var ps = r.transform.parent.lossyScale;
                        r.transform.localScale = new Vector3(0.6f / Mathf.Max(ps.x, 1e-4f), 0.09f / Mathf.Max(ps.y, 1e-4f), 0.6f / Mathf.Max(ps.z, 1e-4f));
                        r.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                        r.transform.position += Vector3.up * 0.015f;
                    }
            if (_bone == null)
            {
                var t = Resources.Load<Material>(SkylineInkRoles.TemplateFolder + SkylineInkRoles.TemplatePlain);
                if (t == null) return;
                _bone = new Material(t) { name = "HellLook_Studs_Bone" };
                _bone.SetColor(BaseColorId, Hex("#F2E8D4") * 1.9f);
                _bone.SetFloat(UnlitId, 1f);
                _stone = new Material(t) { name = "HellLook_Studs_Stone" };
                _stone.SetColor(BaseColorId, Hex("#6E6A66"));
                _stone.SetFloat(UnlitId, 1f);
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
            DressNewBots();
            var cam = Camera.main;
            if (floorOverride >= 0) AtmosphereFor(floorOverride);
            else if (cam != null) AtmosphereFor(HellLookOptions.FloorAt(cam.transform.position.z));
            float t = Time.time;
            foreach (var m in _scrolling)
                if (m != null) m.SetTextureOffset(BaseMapId, new Vector2(t * 0.012f, t * 0.02f));
        }

        // ---- the enemies: charred hide with ember cracks, molten ooze

        readonly HashSet<int> _dressed = new HashSet<int>();
        readonly Dictionary<(Material, int), Material> _botMats = new Dictionary<(Material, int), Material>();

        /// <summary>Dresses every enemy not yet dressed (they spawn through the run; the capture calls this right after its spawns).</summary>
        public void DressNewBots()
        {
            foreach (var bot in SkylineBot.Live)
            {
                if (bot == null || !_dressed.Add(bot.GetInstanceID())) continue;
                int floor = HellLookOptions.FloorAt(bot.transform.position.z);
                Dress(bot.gameObject, _dir[floor]);
            }
        }

        /// <summary>Re-dresses one enemy for a direction: every lit (non-glow) InkLit part gets a hide texture, and the dark ones
        /// (charred skin, flesh, crust) glowing ember cracks; the ooze's molten body gets the lava. Roles (the outline) are kept.</summary>
        public void Dress(GameObject root, HellLookDirection dir)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null || !SkylineInkRoles.IsInk(src)) continue;
                    bool blob = r.name.StartsWith("Blob");
                    var k = (src, (int)dir * 2 + (blob ? 1 : 0));
                    if (!_botMats.TryGetValue(k, out var made))
                    {
                        made = DressMaterial(src, blob, dir);
                        _botMats[k] = made;
                    }
                    if (made != null && made != src) { mats[i] = made; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        Material DressMaterial(Material src, bool blob, HellLookDirection dir)
        {
            bool glow = src.HasProperty(UnlitId) && src.GetFloat(UnlitId) > 0.5f;
            Color c = src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : Color.white;
            if (glow)
            {
                // the ooze's body: molten lava, scrolling (its core, eyes and flames stay the hot glows they are)
                if (!blob) return src;
                var m = new Material(src) { name = src.name + " (molten " + dir + ")" };
                m.SetTexture(BaseMapId, LavaTexture());
                m.SetTextureScale(BaseMapId, new Vector2(0.6f, 0.6f));
                m.SetColor(BaseColorId, new Color(1.5f, 1.5f, 1.5f));
                _scrolling.Add(m);
                return m;
            }
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            var d = new Material(src) { name = src.name + " (dressed " + dir + ")" };
            d.SetTexture(BaseMapId, HellLookTextures.Hide());
            d.SetTextureScale(BaseMapId, Vector2.one);
            if (lum < 0.2f)
            {
                // charred: lift the colour a touch (the texture darkens it), and the cracks glow
                d.SetColor(BaseColorId, c * 1.5f + new Color(0.02f, 0.01f, 0.01f));
                d.EnableKeyword("_EMISSION");
                d.SetTexture(EmissionMapId, HellLookTextures.Cracks());
                float k = dir == HellLookDirection.C ? 1.5f : dir == HellLookDirection.A ? 1.1f : 1.25f;
                d.SetColor(EmissionColorId, new Color(2.6f, 0.75f, 0.12f) * k);
            }
            return d;
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
