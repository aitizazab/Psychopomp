using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JamKit.SkylineRepo.EditorTools
{
    /// <summary>
    /// The Hell models' prefabs (docs/HELL-ENEMIES.md, "Bodies"; docs/HELL-BOSS.md): turns the Blender-made FBX files in Look/HellLook/Models (scythe-demon,
    /// lava-ooze, vesper; built by tools/hell-models/*.py from CC0 parts, CREDITS.md) into the drop-in prefabs
    /// <c>Resources/HellModels/&lt;name&gt;.prefab</c> that <see cref="HellBodies"/> and <see cref="HellVesperBody"/> load:
    /// <list type="bullet">
    /// <item>materials by role name (M_* lit, G_* glow) onto the game palette (<see cref="HellLook"/>); charred parts get the hide
    /// and the glowing ember cracks (the textures of <see cref="HellLookTextures"/>, saved as PNGs beside the models);</item>
    /// <item>hit zones: a box per part, a sphere on <c>WeakPoint</c>, a disabled box on decorations (names ending <c>_nc</c>) so the
    /// body builder adds none; the part names the bot code drives (Plate_*, WeakPoint, ArmR, LegL/LegR, Blob, Flame*) come from
    /// Blender as they are;</item>
    /// <item>the demon's hover and cloth sway (<see cref="HellModelMotion"/>); Vesper's Animator (Idle, Cast, Blink, Reel, Kneel by
    /// the int <c>Pose</c>), her <c>Heart</c> on the chest bone, body and head colliders on her bones.</item>
    /// </list>
    /// Menu: JamKit/Hell/Models/Build the model prefabs. Command line:
    /// <c>Unity -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellModelsBuild.FromCommandLine</c>.
    /// A missing FBX is skipped (the stand-in body stays), so each model can land on its own.
    /// </summary>
    public static class HellModelsBuild
    {
        const string ModelDir = "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Models/";
        const string PrefabDir = "Assets/JamKit/Templates/SkylineRepo/Resources/HellModels/";
        const string TexDir = "Assets/JamKit/Templates/SkylineRepo/Resources/HellLook/Textures/";

        [MenuItem("JamKit/Hell/Models/Build the model prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
            Demon();
            Ooze();
            Vesper();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void FromCommandLine()
        {
            int code = 0;
            try { BuildAll(); }
            catch (System.Exception e) { Debug.LogError("HELL_MODELS_BUILD failed: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ the three models

        static void Demon()
        {
            var root = Begin("scythe-demon", 2.8f, "WeakPoint");
            if (root == null) return;
            var model = root.transform.Find("Model");
            var motion = root.AddComponent<HellModelMotion>();
            motion.hover = model;
            motion.bobHeight = 0.07f;
            motion.bobHz = 0.55f;
            motion.sway = new[] { Deep(root.transform, "Robe"), Deep(root.transform, "FlapL_nc"), Deep(root.transform, "FlapR_nc"), Deep(root.transform, "Hood_nc") }
                .Where(t => t != null).ToArray();
            motion.swayDegrees = 2.5f;
            motion.swayHz = 0.45f;
            Save(root, "scythe-demon");
        }

        static void Ooze()
        {
            var root = Begin("lava-ooze", 1.0f, "WeakPoint");
            if (root == null) return;
            // the bot code sets Blob's localPosition to half its scaled height above its parent's origin (the feet), so the Blob's
            // mesh must be unit-height and centred: the Blender script makes it so; check it here
            var blob = Deep(root.transform, "Blob");
            if (blob != null)
            {
                var mf = blob.GetComponent<MeshFilter>();
                var b = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds();
                Debug.Log("HELL_MODELS_BUILD ooze Blob mesh bounds " + b + ", localScale " + blob.localScale + ", localPosition " + blob.localPosition +
                          ", parent " + (blob.parent != null ? blob.parent.name + " " + blob.parent.position + " " + blob.parent.rotation.eulerAngles + " " + blob.parent.lossyScale : "none"));
                var sc = blob.GetComponent<BoxCollider>();
                if (sc != null) Object.DestroyImmediate(sc);
                var sph = blob.gameObject.AddComponent<SphereCollider>();
                sph.radius = 0.36f;   // inside the visible body, so the core on top stands clear
            }
            Save(root, "lava-ooze");
        }

        static void Vesper()
        {
            string fbx = ModelDir + "vesper.fbx";
            if (!File.Exists(fbx)) { Debug.Log("HELL_MODELS_BUILD no " + fbx + ": Vesper keeps the stand-in"); return; }
            var clips = ImportClips(fbx);
            var root = Begin("vesper", 2.35f, "FrontMarker");
            if (root == null) return;
            var model = root.transform.Find("Model");
            // the Animator: Pose 0 Stand (idle), 1 Cast, 2 Rewind (cast pose), 3 Reel, 4 Strike (cast pose), 5 Kneel; Blink is a trigger
            var anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = Controller(clips);
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // the heart rides the chest bone; colliders on the bones (the skinned mesh gets no box from the body builder)
            var chest = Deep(root.transform, "Chest") ?? Deep(root.transform, "Torso") ?? Deep(root.transform, "Spine2");
            var hips = Deep(root.transform, "Hips");
            var head = Deep(root.transform, "Head");
            var handR = Deep(root.transform, "Palm.R") ?? Deep(root.transform, "LowerArm.R");
            if (chest != null)
            {
                var heart = new GameObject("Heart").transform;
                heart.SetParent(chest, false);
                // the chest's front, in the model's frame: forward of the bone by about 0.17 m, at the breastbone
                heart.position = new Vector3(chest.position.x, VesperHeartY(root.transform, chest), 0f) + root.transform.forward * 0.19f;
                heart.rotation = root.transform.rotation;
                FixColliderScale(heart);   // the fight builds her heart at metre sizes under it
                var body = new GameObject("BodyCollider").transform;
                body.SetParent(chest, false);
                body.position = chest.position - root.transform.up * 0.15f;
                var cap = body.gameObject.AddComponent<CapsuleCollider>();
                cap.direction = 1; cap.radius = 0.2f; cap.height = 0.9f;
                FixColliderScale(body);
            }
            if (hips != null)
            {
                var legs = new GameObject("SkirtCollider").transform;
                legs.SetParent(hips, false);
                legs.position = hips.position - root.transform.up * 0.45f;
                var cap = legs.gameObject.AddComponent<CapsuleCollider>();
                cap.direction = 1; cap.radius = 0.3f; cap.height = 1.1f;
                FixColliderScale(legs);
            }
            if (head != null)
            {
                var hc = new GameObject("HeadCollider").transform;
                hc.SetParent(head, false);
                hc.position = head.position + root.transform.up * 0.12f;
                var s = hc.gameObject.AddComponent<SphereCollider>();
                s.radius = 0.16f;
                FixColliderScale(hc);
            }
            if (handR != null)
            {
                var a = new GameObject("ArmR").transform;   // where her bolts leave (HellVesperBody.Hand); the code's arm swing moves only this empty
                a.SetParent(handR, false);
            }
            Save(root, "vesper");
        }

        static float VesperHeartY(Transform root, Transform chest) => chest.position.y + 0.15f;

        /// <summary>Colliders under scaled bones: undo the lossy scale so the sizes above are in metres.</summary>
        static void FixColliderScale(Transform t)
        {
            var s = t.lossyScale;
            if (Mathf.Abs(s.x) < 1e-4f) return;
            t.localScale = new Vector3(t.localScale.x / s.x, t.localScale.y / s.y, t.localScale.z / s.z);
        }

        // ------------------------------------------------------------------ shared steps

        /// <summary>Instantiates the FBX under a new root (its origin the model's feet), re-skins its materials, fixes its facing and
        /// size, and gives each part its hit zone. Null (and a log) when the FBX is missing.</summary>
        static GameObject Begin(string name, float height, string frontPart)
        {
            string fbx = ModelDir + name + ".fbx";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (src == null) { Debug.Log("HELL_MODELS_BUILD no " + fbx + ": " + name + " keeps the stand-in"); return null; }
            var root = new GameObject(name);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Model";
            inst.transform.SetParent(root.transform, false);

            // size: the FBX should come in at metres; if not (a unit mix-up), scale the Model to the expected height
            var bounds = RendererBounds(inst);
            float k = bounds.size.y > 1e-4f ? height / bounds.size.y : 1f;
            Debug.Log("HELL_MODELS_BUILD " + name + " imported bounds " + bounds + " (expected height about " + height + " m)");
            if (k < 0.7f || k > 1.4f)
            {
                inst.transform.localScale *= k;
                Debug.LogWarning("HELL_MODELS_BUILD " + name + " rescaled by " + k);
            }
            // facing: the weak point is on its front, which must be +Z
            var front = frontPart != null ? Deep(root.transform, frontPart) : null;
            if (front != null && front.position.z < 0f)
            {
                inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * inst.transform.localRotation;
                Debug.Log("HELL_MODELS_BUILD " + name + " turned to face +Z");
            }
            bounds = RendererBounds(inst);
            Debug.Log("HELL_MODELS_BUILD " + name + " final bounds " + bounds);

            var made = new Dictionary<string, Material>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Palette(mats[i] != null ? mats[i].name : "M_Char", made);
                r.sharedMaterials = mats;
                bool glow = mats.Length > 0 && mats.All(m => m != null && m.shader != null && m.shader.name.Contains("Unlit"));
                if (glow) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!(r is MeshRenderer)) continue;
                var go = r.gameObject;
                if (go.name.EndsWith("_nc")) { go.AddComponent<BoxCollider>().enabled = false; continue; }   // decoration: no hit zone, and none added at run time
                if (go.name.StartsWith("Flame")) continue;   // flames never take hits (the bot body builder skips them too)
                if (go.name == "WeakPoint") { go.AddComponent<SphereCollider>(); continue; }
                go.AddComponent<BoxCollider>();
            }
            return root;
        }

        static void Save(GameObject root, string name)
        {
            string path = PrefabDir + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("HELL_MODELS_BUILD saved " + path);
        }

        static Transform Deep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var f = Deep(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>The game's material for a Blender role name (M_Char, G_Core, ...; Blender may add a ".001").</summary>
        static Material Palette(string blenderName, Dictionary<string, Material> made)
        {
            string role = blenderName.Split('.')[0].Trim();
            if (made.TryGetValue(role, out var have)) return have;
            Color c; bool glow = role.StartsWith("G_"); bool charred = false;
            switch (role)
            {
                case "M_Char": c = HellLook.Char; charred = true; break;
                case "M_Crust": c = HellLook.Crust; charred = true; break;
                case "M_Skin": c = new Color(0.16f, 0.05f, 0.035f); charred = true; break;   // Vesper's molten skin: dark with hot cracks
                case "M_Hair": c = new Color(0.16f, 0.04f, 0.025f); charred = true; break;   // Vesper's hair: dark with molten cracks
                case "M_Robe": c = new Color(0.045f, 0.035f, 0.04f); break;                  // her concealing robe
                case "M_Ash": c = new Color(0.4f, 0.37f, 0.38f); break;                      // her pale ashen skin
                case "M_Bone": c = HellLook.Bone; break;
                case "M_Rust": c = HellLook.Rust; break;
                case "M_Steel": c = HellLook.Steel; break;
                case "M_Flesh": c = HellLook.Flesh; break;
                case "M_Hollow": c = new Color(0.02f, 0.01f, 0.01f); break;
                case "M_Gown": c = new Color(0.05f, 0.025f, 0.07f); break;
                case "M_Veil": c = new Color(0.12f, 0.06f, 0.16f); break;
                case "M_Gold": c = new Color(0.75f, 0.45f, 0.12f); break;
                case "G_Core": c = HellLook.CoreHot; break;
                case "G_Eye": c = HellLook.EyeHot; break;
                case "G_Flame": c = HellLook.Flame; break;
                case "G_FlameDeep": c = HellLook.FlameDeep; break;
                case "G_Edge": c = HellLook.BeamHot; break;
                case "G_Lava": c = HellLook.Lava; break;
                case "G_LavaHot": c = HellLook.LavaHot; break;
                case "G_Ember": c = new Color(2.6f, 0.75f, 0.12f); break;
                case "G_Violet": c = new Color(1.6f, 0.6f, 3.6f); break;
                case "G_Hair": c = new Color(3.2f, 0.9f, 0.12f); break;
                case "G_HairHot": c = new Color(4.5f, 2.2f, 0.5f); break;
                default: c = glow ? HellLook.Flame : HellLook.Char; break;
            }
            string path = ModelDir + "HM_" + role.Substring(2) + ".mat";
            var shader = Shader.Find(glow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            m.SetColor("_BaseColor", c);
            if (glow && role == "G_Lava")
            {
                var lava = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "lava_diff.jpg");
                if (lava != null) { m.SetTexture("_BaseMap", lava); m.SetColor("_BaseColor", new Color(1.5f, 1.5f, 1.5f)); }
            }
            if (!glow)
            {
                m.SetFloat("_Smoothness", role == "M_Gold" || role == "M_Steel" ? 0.55f : 0.15f);
                if (role == "M_Gold" || role == "M_Steel") m.SetFloat("_Metallic", 0.8f);
            }
            if (charred)
            {
                // the hide and the ember cracks (the run-time re-dress uses the same pair under a -hellLook)
                m.SetTexture("_BaseMap", SavedTexture("hm_hide.png", HellLookTextures.Hide()));
                m.SetColor("_BaseColor", c * 1.5f + new Color(0.02f, 0.01f, 0.01f));
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetTexture("_EmissionMap", SavedTexture("hm_cracks.png", HellLookTextures.Cracks()));
                m.SetColor("_EmissionColor", new Color(2.6f, 0.75f, 0.12f) * (role == "M_Hair" ? 2.2f : role == "M_Skin" ? 1.6f : 1.2f));
            }
            EditorUtility.SetDirty(m);
            made[role] = m;
            return m;
        }

        static Texture2D SavedTexture(string file, Texture2D made)
        {
            string path = ModelDir + file;
            var have = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (have != null) return have;
            File.WriteAllBytes(path, made.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.sRGBTexture = true; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ Vesper's animation

        /// <summary>Sets the FBX's clips (Generic rig, Idle looping) and returns them by name (Idle, Cast, Blink, Reel, Kneel).</summary>
        static Dictionary<string, AnimationClip> ImportClips(string fbx)
        {
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp != null)
            {
                bool dirty = false;
                if (imp.animationType != ModelImporterAnimationType.Generic) { imp.animationType = ModelImporterAnimationType.Generic; dirty = true; }
                var defs = imp.defaultClipAnimations;
                if (defs != null && defs.Length > 0)
                {
                    foreach (var d in defs)
                    {
                        string n = d.name.Contains("|") ? d.name.Substring(d.name.LastIndexOf('|') + 1) : d.name;
                        d.name = n;
                        d.loopTime = n == "Idle" || n == "Cast" || n == "Kneel" || n == "Reel";
                    }
                    imp.clipAnimations = defs;
                    dirty = true;
                }
                if (dirty) imp.SaveAndReimport();
            }
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>())
                if (!a.name.StartsWith("__preview__")) clips[a.name] = a;
            Debug.Log("HELL_MODELS_BUILD vesper clips: " + string.Join(", ", clips.Keys));
            return clips;
        }

        static RuntimeAnimatorController Controller(Dictionary<string, AnimationClip> clips)
        {
            string path = ModelDir + "vesper.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Pose", AnimatorControllerParameterType.Int);
            ac.AddParameter("Blink", AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            AnimationClip C(string n) => clips.TryGetValue(n, out var c) ? c : (clips.TryGetValue("Idle", out var i) ? i : null);
            string[] poses = { "Idle", "Cast", "Cast", "Reel", "Cast", "Kneel" };
            var states = new AnimatorState[poses.Length];
            for (int i = 0; i < poses.Length; i++)
            {
                states[i] = sm.AddState(i == 0 ? "Stand" : i == 2 ? "Rewind" : i == 4 ? "Strike" : poses[i]);
                states[i].motion = C(poses[i]);
            }
            // any state into a pose; back to Stand only from a pose (so the blink, played while she stands, runs to its end)
            for (int i = 1; i < poses.Length; i++)
            {
                var t = sm.AddAnyStateTransition(states[i]);
                t.AddCondition(AnimatorConditionMode.Equals, i, "Pose");
                t.duration = i == 5 ? 0.45f : 0.18f;
                t.canTransitionToSelf = false;
                t.hasExitTime = false;
                var back = states[i].AddTransition(states[0]);
                back.AddCondition(AnimatorConditionMode.Equals, 0, "Pose");
                back.duration = 0.25f;
                back.hasExitTime = false;
            }
            sm.defaultState = states[0];
            var blink = sm.AddState("Blink");
            blink.motion = C("Blink");
            var tb = sm.AddAnyStateTransition(blink);
            tb.AddCondition(AnimatorConditionMode.If, 0, "Blink");
            tb.duration = 0.08f; tb.hasExitTime = false;
            var after = blink.AddTransition(states[0]);
            after.hasExitTime = true; after.exitTime = 0.95f; after.duration = 0.2f;
            AssetDatabase.SaveAssets();
            return ac;
        }
    }
}
