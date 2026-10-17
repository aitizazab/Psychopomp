using System.IO;
using System.Linq;
using JamKit.Skyline;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace JamKit.SkylineRepo.EditorTools
{
    /// <summary>
    /// The Hell look test's editor side (docs/HELL-LOOK-TEST.md): makes the build-included assets the run-time switch
    /// (<see cref="HellLookSwitch"/>) needs, and builds the scene and the player with a look baked in.
    /// <list type="bullet">
    /// <item><see cref="PrepareAssets"/>: marks the CC0 normal maps in Resources/HellLook/Textures as normal maps; makes the two URP
    /// Lit templates (normal-mapped; emissive) whose keywords the build must keep; makes the imp prefab
    /// Resources/HellLook/fire-imp.prefab from the Quaternius "Demon" (CC0, converted to Look/HellLook/Models/fire-imp-quaternius.obj),
    /// repainted char black with glowing eyes and a white-hot core, with the part names HellBodies expects.</item>
    /// <item><c>-executeMethod JamKit.SkylineRepo.EditorTools.HellLookBuild.SceneFromCommandLine [-hellLook A|B|C]</c>: the assets,
    /// then the Hell scene (the rooms' own builder), with the look baked in when one is named. Logs HELL_LOOK_BUILD.</item>
    /// <item><c>-executeMethod JamKit.SkylineRepo.EditorTools.HellLookBuild.PlayerFromCommandLine [-hellLook A|B|C] [-skylineOut &lt;dir&gt;]</c>:
    /// the same, then the Windows player at &lt;dir&gt;/build/HellDescent.exe (default out/hell-look). The look is still switchable in
    /// the player with -hellLook. Logs HELL_LOOK_PLAYER_BUILD OK|FAILED.</item>
    /// </list>
    /// </summary>
    public static class HellLookBuild
    {
        const string ResRoot = "Assets/JamKit/Templates/SkylineRepo/Resources/HellLook/";
        const string ModelObj = "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Models/fire-imp-quaternius.obj";
        const string ModelMatFolder = "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Models/";
        /// <summary>The imp's size: the Quaternius demon's 5.5 m wingspan brought to about 2 m (the stand-in's is 1.6 m).</summary>
        const float ImpScale = 0.37f;

        [MenuItem("JamKit/Hell/Look test/Prepare the assets")]
        public static void PrepareAssetsMenu() => PrepareAssets();

        [MenuItem("JamKit/Hell/Look test/Build the scene with look A (Inked Inferno)")]
        public static void BuildA() => BuildScene(HellLookDirection.A);
        [MenuItem("JamKit/Hell/Look test/Build the scene with look B (Molten Grit)")]
        public static void BuildB() => BuildScene(HellLookDirection.B);
        [MenuItem("JamKit/Hell/Look test/Build the scene with look C (Black and Ember)")]
        public static void BuildC() => BuildScene(HellLookDirection.C);

        public static void PrepareAssets()
        {
            AssetDatabase.Refresh();
            NormalMaps();
            LitTemplates();
            ImpPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void NormalMaps()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ResRoot.TrimEnd('/') + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool normal = Path.GetFileNameWithoutExtension(path).EndsWith("_nor");
                bool changed = false;
                if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; changed = true; }
                if (imp.maxTextureSize != 1024) { imp.maxTextureSize = 1024; changed = true; }
                if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
                if (changed) imp.SaveAndReimport();
            }
        }

        static Shader LitShader() => Shader.Find("Universal Render Pipeline/Lit");

        static void LitTemplates()
        {
            var lit = LitShader();
            if (lit == null) { Debug.LogError("HELL_LOOK_BUILD no URP Lit shader"); return; }
            Directory.CreateDirectory(ResRoot);
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(ResRoot + "Textures/basalt_nor.jpg");
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(ResRoot + "Textures/basalt_diff.jpg");
            var m = LoadOrMake(ResRoot + "HL_LitNormal.mat", lit);
            m.SetTexture("_BaseMap", diff);
            m.SetTexture("_BumpMap", nor);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);

            var lavaDiff = AssetDatabase.LoadAssetAtPath<Texture2D>(ResRoot + "Textures/lava_diff.jpg");
            var lavaEmis = AssetDatabase.LoadAssetAtPath<Texture2D>(ResRoot + "Textures/lava_emis.jpg");
            var e = LoadOrMake(ResRoot + "HL_LitEmission.mat", lit);
            e.SetTexture("_BaseMap", lavaDiff);
            e.SetTexture("_EmissionMap", lavaEmis != null ? lavaEmis : lavaDiff);
            e.SetColor("_EmissionColor", new Color(4f, 1.6f, 0.4f));
            e.EnableKeyword("_EMISSION");
            e.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            e.SetFloat("_Smoothness", 0.5f);
            EditorUtility.SetDirty(e);
        }

        static Material LoadOrMake(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>The imp prefab from the Quaternius "Demon" OBJ: one child per source material, repainted; the weak point (a
        /// white-hot core on its chest), Muzzle, Plate_horns, WingL/WingR (empty: the model's wings are part of its mesh) and a
        /// head flame. Logs and leaves the stand-in in place if the OBJ didn't import.</summary>
        static void ImpPrefab()
        {
            var meshes = AssetDatabase.LoadAllAssetsAtPath(ModelObj).OfType<Mesh>().ToList();
            if (meshes.Count == 0)
            {
                Debug.LogWarning("HELL_LOOK_BUILD the imp OBJ has no meshes (" + ModelObj + "): the look test keeps the stand-in imp");
                return;
            }
            var lit = LitShader();
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Material Mat(string name, Color c, bool glow)
            {
                string path = ModelMatFolder + name + ".mat";
                var m = LoadOrMake(path, glow && unlit != null ? unlit : lit);
                m.SetColor("_BaseColor", c);
                EditorUtility.SetDirty(m);
                return m;
            }
            var charMat = Mat("HL_Imp_Char", HellLook.Char, false);
            var skinMat = Mat("HL_Imp_Skin", new Color(0.2f, 0.035f, 0.025f), false);   // dark ember-red hide
            var eyeMat = Mat("HL_Imp_Eye", HellLook.EyeHot, true);
            var coreMat = Mat("HL_Imp_Core", HellLook.CoreHot, true);
            var flameMat = Mat("HL_Imp_Flame", HellLook.Flame, true);

            var root = new GameObject("fire-imp");
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localScale = Vector3.one * ImpScale;
            Bounds main = new Bounds();
            bool haveMain = false;
            foreach (var mesh in meshes)
            {
                string n = mesh.name;
                Material mat = n.Contains("Eye_White") ? eyeMat : n.Contains("Eye_Black") ? charMat : n.Contains("Demon_Main") ? skinMat : charMat;
                var go = new GameObject(n.Contains("Demon_Main") ? "Body" : n.Contains("Eye_White") ? "Eyes" : "Char " + n);
                go.transform.SetParent(model, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                if (n.Contains("Demon_Main")) { main = mesh.bounds; haveMain = true; }
            }
            if (!haveMain) main = meshes[0].bounds;
            // the rig's origin is the imp's centre (it hovers there); the OBJ is centred on its bounds already
            Vector3 S(Vector3 v) => v * ImpScale;
            var chest = S(new Vector3(main.center.x, main.center.y, main.max.z - main.size.z * 0.12f));
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "WeakPoint";
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = chest;
            core.transform.localScale = Vector3.one * Mathf.Clamp(main.size.y * ImpScale * 0.35f, 0.18f, 0.4f);
            core.GetComponent<Renderer>().sharedMaterial = coreMat;
            Empty("Muzzle", root.transform, chest + new Vector3(0f, 0.1f, 0.15f));
            Empty("Plate_horns", root.transform, S(new Vector3(main.center.x, main.max.y, main.center.z)));
            Empty("WingL", root.transform, new Vector3(-0.25f, 0.1f, 0f));
            Empty("WingR", root.transform, new Vector3(0.25f, 0.1f, 0f));
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame0";
            Object.DestroyImmediate(flame.GetComponent<Collider>());
            flame.transform.SetParent(root.transform, false);
            flame.transform.localPosition = S(new Vector3(main.center.x, main.max.y, main.center.z)) + new Vector3(0f, 0.05f, -0.05f);
            flame.transform.localScale = new Vector3(0.12f, 0.24f, 0.12f);
            flame.GetComponent<Renderer>().sharedMaterial = flameMat;

            Directory.CreateDirectory(ResRoot);
            PrefabUtility.SaveAsPrefabAsset(root, ResRoot + "fire-imp.prefab");
            Object.DestroyImmediate(root);
            Debug.Log("HELL_LOOK_BUILD imp prefab " + ResRoot + "fire-imp.prefab from " + meshes.Count + " meshes (" + string.Join(", ", meshes.Select(m => m.name)) + ")");
        }

        static void Empty(string name, Transform parent, Vector3 at)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = at;
        }

        /// <summary>Builds the Hell scene from data (the rooms' builder) and, for a look, adds a <see cref="HellLookSwitch"/> with it baked in.</summary>
        public static void BuildScene(HellLookDirection look)
        {
            PrepareAssets();
            SkylineArenaBuilder.BuildHell(SkylineArenaBuilder.HellScenePath);
            if (look != HellLookDirection.None)
            {
                var scene = EditorSceneManager.OpenScene(SkylineArenaBuilder.HellScenePath, OpenSceneMode.Single);
                var sw = new GameObject("Hell Look (switch)").AddComponent<HellLookSwitch>();
                sw.look = look;
                sw.outline = HellLookOptions.DefaultOutline(look);
                sw.studs = look == HellLookDirection.C ? HellStudColour.Stone : HellStudColour.Blue;
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.Refresh();
            Debug.Log("HELL_LOOK_BUILD scene " + SkylineArenaBuilder.HellScenePath + " look " + look);
        }

        static HellLookDirection LookArg() => HellLookOptions.Parse(System.Environment.GetCommandLineArgs()).look;

        public static void SceneFromCommandLine()
        {
            int code = 0;
            try { BuildScene(LookArg()); }
            catch (System.Exception e) { Debug.LogError("HELL_LOOK_BUILD failed: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void PlayerFromCommandLine()
        {
            string outDir = SkylinePassBatch.Arg("-skylineOut") ?? Path.Combine("out", "hell-look");
            string exe = Path.GetFullPath(Path.Combine(outDir, "build", "HellDescent.exe"));
            bool ok = false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exe));
                BuildScene(LookArg());
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { SkylineArenaBuilder.HellScenePath },
                    locationPathName = exe,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                });
                ok = report.summary.result == BuildResult.Succeeded && File.Exists(exe);
            }
            catch (System.Exception e) { Debug.LogError("HELL_LOOK_PLAYER_BUILD exception: " + e); }
            Debug.Log("HELL_LOOK_PLAYER_BUILD " + (ok ? "OK " : "FAILED ") + exe);
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
