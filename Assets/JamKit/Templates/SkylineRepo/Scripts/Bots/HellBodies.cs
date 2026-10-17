using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell enemies' placeholder bodies (docs/HELL-ENEMIES.md, "Bodies"), one builder per enemy, each easy to swap for final
    /// art: drop a prefab at <c>Resources/HellModels/&lt;fire-imp | scythe-demon | lava-ooze&gt;</c> and it is used instead, as long as it
    /// keeps the part names below (the same contract as the Skyline bots' models):
    /// <list type="bullet">
    /// <item><c>Plate_&lt;plateId&gt;</c> per plate of the def (the imp's <c>Plate_horns</c>; the demon's <c>Plate_pauldron-left</c> and
    /// <c>Plate_pauldron-right</c>; the ooze's <c>Plate_crust</c>), shed when broken;</item>
    /// <item><c>WeakPoint</c>, the bright core (the bot weak-point ink role, the weak-point hit zone);</item>
    /// <item>the imp's <c>Muzzle</c> (where bolts leave), <c>WingL</c>/<c>WingR</c> (flapped about their local Z);</item>
    /// <item>the demon's <c>ArmR</c> (raised for the wind-up), <c>LegL</c>/<c>LegR</c> (swung in its stride);</item>
    /// <item>the ooze's <c>Blob</c> (scaled and its glow throbbed by the pulse);</item>
    /// <item>any <c>Flame*</c> (flickered), on any of them.</item>
    /// </list>
    /// The imp's origin is its centre (it hovers there); the demon's and the ooze's are their feet. Every collider becomes a hit zone;
    /// everything is in the hostile ink role, the core in the bot weak-point role. Colours: <see cref="HellLook"/>.
    /// </summary>
    public static class HellBodies
    {
        public const string ModelFolder = "HellModels/";

        /// <summary>The enemy's name for its prefab and its sounds: fire-imp, scythe-demon, lava-ooze.</summary>
        public static string Name(HellKind k) =>
            k == HellKind.FireImp ? "fire-imp" : k == HellKind.ScytheDemon ? "scythe-demon" : k == HellKind.LavaOoze ? "lava-ooze" : "";

        public static SkylineBotParts Build(EnemyDef def, Transform root, MonoBehaviour owner)
        {
            var p = new SkylineBotParts();
            var model = Resources.Load<GameObject>(ModelFolder + Name(def.hell));
            if (model != null)
            {
                p.fromModel = true;
                var go = Object.Instantiate(model, root, false);
                go.name = "Rig";
                p.rig = go.transform;
                foreach (var r in p.rig.GetComponentsInChildren<Renderer>(true))
                    if (r.GetComponent<Collider>() == null && r is MeshRenderer && !r.name.StartsWith("Flame")) r.gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                p.rig = SkylineBotLook.Empty("Rig", root, Vector3.zero);
                switch (def.hell)
                {
                    case HellKind.FireImp: FireImp(p.rig); break;
                    case HellKind.ScytheDemon: ScytheDemon(p.rig); break;
                    default: LavaOoze(p.rig); break;
                }
            }
            int n = def.plates != null ? def.plates.Length : 0;
            p.plates = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                p.plates[i] = SkylineBotBodies.Find(p.rig, "Plate_" + def.plates[i].id);
                if (p.plates[i] == null) SkylineBotBodies.LogOnce("HELL_ENEMY body " + Name(def.hell) + " lacks Plate_" + def.plates[i].id);
            }
            p.weakPoint = SkylineBotBodies.Find(p.rig, "WeakPoint");
            if (p.weakPoint == null)
            {
                SkylineBotBodies.LogOnce("HELL_ENEMY body " + Name(def.hell) + " lacks WeakPoint");
                p.weakPoint = SkylineBotLook.Empty("WeakPoint", p.rig, Vector3.zero);
            }
            p.muzzle = SkylineBotBodies.Find(p.rig, "Muzzle");
            if (p.muzzle == null) p.muzzle = SkylineBotLook.Empty("Muzzle", p.rig, Vector3.zero);
            p.centerLocal = def.hell == HellKind.FireImp ? new Vector3(0f, 0.05f, 0f)
                : def.hell == HellKind.ScytheDemon ? new Vector3(0f, 1.65f, 0f) : new Vector3(0f, 0.5f, 0f);
            SkylineInkRoles.Apply(p.rig.gameObject, SkylineInkRoles.Hostile);
            SkylineInkRoles.Apply(p.weakPoint.gameObject, SkylineInkRoles.BotWeakPoint);
            SkylineBotBodies.AssignZones(def, p, owner);
            return p;
        }

        // ---------------------------------------------------------------- builders (primitives; one per enemy)

        /// <summary>A small horned imp, about 1 m from horn tip to tail: a charred round body and head, bone horns (the plate), two
        /// bat wings, flames licking off its head and tail, yellow eyes, and its core (the weak point) glowing white-yellow out of
        /// its chest. Origin: its centre.</summary>
        public static void FireImp(Transform rig)
        {
            var body = HellLook.Lit(HellLook.Char);
            P(PrimitiveType.Sphere, "Body", rig, new Vector3(0f, -0.02f, 0f), new Vector3(0.56f, 0.62f, 0.5f), body, true);
            P(PrimitiveType.Sphere, "Head", rig, new Vector3(0f, 0.4f, 0.05f), new Vector3(0.42f, 0.38f, 0.4f), body, true);
            // the core bulges out of its chest (front 0.43 m, clear of the body's collider at 0.31 m)
            P(PrimitiveType.Sphere, "WeakPoint", rig, new Vector3(0f, -0.03f, 0.23f), Vector3.one * 0.4f, HellLook.Glow(HellLook.CoreHot), true);
            // a ring of cracked char round the core, so it reads as a wound in the body
            var crack = HellLook.Lit(HellLook.Flesh);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                P(PrimitiveType.Cube, "Crack" + i, rig, new Vector3(Mathf.Cos(a) * 0.2f, -0.03f + Mathf.Sin(a) * 0.2f, 0.2f), new Vector3(0.05f, 0.05f, 0.08f), crack, false,
                    new Vector3(0f, 0f, a * Mathf.Rad2Deg + 45f));
            }
            var horns = SkylineBotLook.Empty("Plate_horns", rig, new Vector3(0f, 0.52f, 0.04f));
            var bone = HellLook.Lit(HellLook.Bone);
            P(PrimitiveType.Capsule, "HornL", horns, new Vector3(-0.12f, 0.1f, 0f), new Vector3(0.08f, 0.15f, 0.08f), bone, true, new Vector3(-15f, 0f, 30f));
            P(PrimitiveType.Capsule, "HornR", horns, new Vector3(0.12f, 0.1f, 0f), new Vector3(0.08f, 0.15f, 0.08f), bone, true, new Vector3(-15f, 0f, -30f));
            P(PrimitiveType.Capsule, "HornTipL", horns, new Vector3(-0.2f, 0.25f, -0.05f), new Vector3(0.05f, 0.08f, 0.05f), bone, false, new Vector3(-35f, 0f, 10f));
            P(PrimitiveType.Capsule, "HornTipR", horns, new Vector3(0.2f, 0.25f, -0.05f), new Vector3(0.05f, 0.08f, 0.05f), bone, false, new Vector3(-35f, 0f, -10f));
            var eye = HellLook.Glow(HellLook.EyeHot);
            P(PrimitiveType.Sphere, "EyeL", rig, new Vector3(-0.09f, 0.44f, 0.22f), new Vector3(0.08f, 0.05f, 0.04f), eye, false, new Vector3(0f, 0f, -15f));
            P(PrimitiveType.Sphere, "EyeR", rig, new Vector3(0.09f, 0.44f, 0.22f), new Vector3(0.08f, 0.05f, 0.04f), eye, false, new Vector3(0f, 0f, 15f));
            SkylineBotLook.Empty("Muzzle", rig, new Vector3(0f, 0.3f, 0.3f));
            // wings: a bone along the top edge and a dark membrane, hinged at the shoulder (flapped about local Z)
            var membrane = HellLook.Lit(HellLook.Flesh);
            for (int side = -1; side <= 1; side += 2)
            {
                var hinge = SkylineBotLook.Empty(side < 0 ? "WingL" : "WingR", rig, new Vector3(side * 0.2f, 0.14f, -0.14f));
                P(PrimitiveType.Cube, "Membrane", hinge, new Vector3(side * 0.34f, -0.06f, -0.04f), new Vector3(0.62f, 0.025f, 0.36f), membrane, true, new Vector3(0f, side * 12f, 0f));
                P(PrimitiveType.Capsule, "WingBone", hinge, new Vector3(side * 0.34f, 0.02f, 0.1f), new Vector3(0.04f, 0.34f, 0.04f), bone, false, new Vector3(0f, 0f, 90f));
                P(PrimitiveType.Capsule, "Claw", hinge, new Vector3(side * 0.66f, 0.02f, 0.12f), new Vector3(0.03f, 0.06f, 0.03f), bone, false, new Vector3(0f, 0f, side * -40f));
            }
            P(PrimitiveType.Capsule, "Tail", rig, new Vector3(0f, -0.3f, -0.2f), new Vector3(0.06f, 0.2f, 0.06f), body, false, new Vector3(50f, 0f, 0f));
            var flame = HellLook.Glow(HellLook.Flame);
            var deep = HellLook.Glow(HellLook.FlameDeep);
            P(PrimitiveType.Sphere, "Flame0", rig, new Vector3(0f, 0.62f, -0.1f), new Vector3(0.14f, 0.28f, 0.14f), flame, false, new Vector3(-25f, 0f, 0f));
            P(PrimitiveType.Sphere, "Flame1", rig, new Vector3(-0.08f, 0.56f, -0.14f), new Vector3(0.1f, 0.2f, 0.1f), deep, false, new Vector3(-30f, 0f, 20f));
            P(PrimitiveType.Sphere, "Flame2", rig, new Vector3(0.08f, 0.56f, -0.14f), new Vector3(0.1f, 0.2f, 0.1f), deep, false, new Vector3(-30f, 0f, -20f));
            P(PrimitiveType.Sphere, "Flame3", rig, new Vector3(0f, -0.4f, -0.36f), new Vector3(0.12f, 0.12f, 0.24f), flame, false, new Vector3(30f, 0f, 0f));
        }

        /// <summary>A big demon, about 2.8 m to its horn tips: charred legs and torso, rust-and-bone pauldrons (the two plates), a
        /// horned head with burning eyes, a long scythe in its right hand (raised for the wind-up), flames at its shoulders, and its
        /// core glowing out of a split in its chest (the weak point). Origin: its feet.</summary>
        public static void ScytheDemon(Transform rig)
        {
            var body = HellLook.Lit(HellLook.Char);
            var bone = HellLook.Lit(HellLook.Bone);
            var rust = HellLook.Lit(HellLook.Rust);
            for (int side = -1; side <= 1; side += 2)
            {
                var hip = SkylineBotLook.Empty(side < 0 ? "LegL" : "LegR", rig, new Vector3(side * 0.24f, 1.15f, 0f));
                P(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.32f, 0.02f), new Vector3(0.32f, 0.36f, 0.32f), body, true);
                P(PrimitiveType.Capsule, "Shin", hip, new Vector3(0f, -0.86f, -0.04f), new Vector3(0.26f, 0.32f, 0.26f), body, true);
                P(PrimitiveType.Cube, "Hoof", hip, new Vector3(0f, -1.1f, 0.06f), new Vector3(0.24f, 0.1f, 0.34f), bone, false);
            }
            P(PrimitiveType.Cube, "Hips", rig, new Vector3(0f, 1.15f, 0f), new Vector3(0.7f, 0.3f, 0.42f), rust, true);
            P(PrimitiveType.Cube, "Torso", rig, new Vector3(0f, 1.65f, 0f), new Vector3(0.95f, 0.85f, 0.5f), body, true);
            // the core in a split chest (front 0.53 m, clear of the torso's box at 0.25 m), framed by two ribs of bone
            P(PrimitiveType.Sphere, "WeakPoint", rig, new Vector3(0f, 1.68f, 0.27f), Vector3.one * 0.52f, HellLook.Glow(HellLook.CoreHot), true);
            P(PrimitiveType.Cube, "RibL", rig, new Vector3(-0.31f, 1.68f, 0.27f), new Vector3(0.08f, 0.56f, 0.1f), bone, false, new Vector3(0f, 0f, -8f));
            P(PrimitiveType.Cube, "RibR", rig, new Vector3(0.31f, 1.68f, 0.27f), new Vector3(0.08f, 0.56f, 0.1f), bone, false, new Vector3(0f, 0f, 8f));
            P(PrimitiveType.Sphere, "Head", rig, new Vector3(0f, 2.3f, 0.04f), new Vector3(0.42f, 0.46f, 0.42f), body, true);
            var eye = HellLook.Glow(HellLook.EyeHot);
            P(PrimitiveType.Sphere, "EyeL", rig, new Vector3(-0.1f, 2.33f, 0.21f), new Vector3(0.09f, 0.05f, 0.04f), eye, false, new Vector3(0f, 0f, -18f));
            P(PrimitiveType.Sphere, "EyeR", rig, new Vector3(0.1f, 2.33f, 0.21f), new Vector3(0.09f, 0.05f, 0.04f), eye, false, new Vector3(0f, 0f, 18f));
            for (int side = -1; side <= 1; side += 2)
            {
                P(PrimitiveType.Capsule, side < 0 ? "HornL" : "HornR", rig, new Vector3(side * 0.2f, 2.56f, -0.02f), new Vector3(0.11f, 0.2f, 0.11f), bone, true, new Vector3(-20f, 0f, side * -35f));
                P(PrimitiveType.Capsule, side < 0 ? "HornL2" : "HornR2", rig, new Vector3(side * 0.36f, 2.74f, -0.14f), new Vector3(0.07f, 0.16f, 0.07f), bone, false, new Vector3(-65f, 0f, side * -15f));
                var pauldron = SkylineBotLook.Empty(side < 0 ? "Plate_pauldron-left" : "Plate_pauldron-right", rig, new Vector3(side * 0.62f, 2.0f, 0f));
                P(PrimitiveType.Sphere, "Shell", pauldron, Vector3.zero, new Vector3(0.52f, 0.34f, 0.52f), rust, true);
                P(PrimitiveType.Capsule, "Spike", pauldron, new Vector3(side * 0.1f, 0.22f, 0f), new Vector3(0.07f, 0.14f, 0.07f), bone, true, new Vector3(0f, 0f, side * -25f));
                P(PrimitiveType.Sphere, "Flame" + (side < 0 ? "L" : "R"), pauldron, new Vector3(0f, 0.3f, -0.12f), new Vector3(0.18f, 0.34f, 0.18f), HellLook.Glow(HellLook.Flame), false, new Vector3(-20f, 0f, 0f));
                var arm = SkylineBotLook.Empty(side < 0 ? "ArmL" : "ArmR", rig, new Vector3(side * 0.66f, 1.92f, 0f));
                P(PrimitiveType.Capsule, "Upper", arm, new Vector3(side * 0.04f, -0.32f, 0f), new Vector3(0.24f, 0.32f, 0.24f), body, true);
                P(PrimitiveType.Capsule, "Fore", arm, new Vector3(side * 0.06f, -0.86f, 0.08f), new Vector3(0.2f, 0.28f, 0.2f), body, true, new Vector3(-15f, 0f, 0f));
                P(PrimitiveType.Sphere, "Claw", arm, new Vector3(side * 0.06f, -1.16f, 0.16f), new Vector3(0.18f, 0.14f, 0.2f), bone, false);
                if (side > 0)
                {
                    // the scythe in its right hand: a long dark haft and a curved blade with a glowing edge (no collider: the hit zone
                    // stays the demon's body)
                    var scythe = SkylineBotLook.Empty("HeldScythe", arm, new Vector3(0.06f, -1.16f, 0.16f));
                    var steel = HellLook.Lit(HellLook.Steel);
                    P(PrimitiveType.Cylinder, "Haft", scythe, new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 1.2f, 0.06f), steel, false);
                    BuildBlade(scythe, new Vector3(0f, 1.7f, 0f), 1f, HellLook.Lit(HellLook.Bone), HellLook.Glow(HellLook.BeamHot));
                }
            }
            P(PrimitiveType.Sphere, "FlameHead", rig, new Vector3(0f, 2.62f, -0.12f), new Vector3(0.16f, 0.3f, 0.16f), HellLook.Glow(HellLook.FlameDeep), false, new Vector3(-30f, 0f, 0f));
        }

        /// <summary>A throbbing blob of lava, about 1.3 m across and 1 m high: an orange-glowing body (scaled and throbbed by the
        /// pulse) with plates of black cooled crust on its back and sides (the plate), two dark hollows for eyes, glowing drips, and
        /// its white-hot core bulging out of its top (the weak point). Origin: its feet.</summary>
        public static void LavaOoze(Transform rig)
        {
            var blob = P(PrimitiveType.Sphere, "Blob", rig, new Vector3(0f, 0.46f, 0f), new Vector3(1.3f, 0.95f, 1.3f), HellLook.Glow(HellLook.Lava), true);
            // a smaller collider than the visible blob, so the core on top stands clear of it
            var sc = blob.GetComponent<SphereCollider>();
            if (sc != null) sc.radius = 0.45f;
            P(PrimitiveType.Sphere, "WeakPoint", rig, new Vector3(0f, 0.98f, 0.18f), Vector3.one * 0.4f, HellLook.Glow(HellLook.CoreHot), true);
            var crust = SkylineBotLook.Empty("Plate_crust", rig, new Vector3(0f, 0.46f, 0f));
            var rock = HellLook.Lit(HellLook.Crust);
            Vector3[] at = { new Vector3(0f, 0.18f, -0.52f), new Vector3(-0.5f, 0.12f, -0.2f), new Vector3(0.5f, 0.12f, -0.2f), new Vector3(-0.42f, 0.05f, 0.3f),
                new Vector3(0.42f, 0.05f, 0.3f), new Vector3(0f, 0.38f, -0.25f) };
            for (int i = 0; i < at.Length; i++)
            {
                Vector3 n = at[i].normalized;
                var q = Quaternion.LookRotation(n) * Quaternion.Euler(90f, i * 37f, 0f);
                var go = P(PrimitiveType.Cube, "Rock" + i, crust, at[i], new Vector3(0.36f, 0.08f, 0.3f), rock, true);
                go.transform.localRotation = q;
            }
            var hollow = HellLook.Lit(new Color(0.02f, 0.01f, 0.01f));
            P(PrimitiveType.Sphere, "EyeL", rig, new Vector3(-0.16f, 0.66f, 0.56f), new Vector3(0.13f, 0.09f, 0.06f), hollow, false, new Vector3(0f, 0f, 20f));
            P(PrimitiveType.Sphere, "EyeR", rig, new Vector3(0.16f, 0.66f, 0.56f), new Vector3(0.13f, 0.09f, 0.06f), hollow, false, new Vector3(0f, 0f, -20f));
            var drip = HellLook.Glow(HellLook.LavaHot);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.3f + 0.4f;
                P(PrimitiveType.Sphere, "Flame" + i, rig, new Vector3(Mathf.Sin(a) * 0.62f, 0.08f, Mathf.Cos(a) * 0.62f), new Vector3(0.14f, 0.1f, 0.14f), drip, false);
            }
        }

        /// <summary>A scythe blade: a curved bone-coloured blade of five segments with a glowing cutting edge, about
        /// <paramref name="size"/> * 1.1 m from heel to tip, curving down from <paramref name="at"/> toward +Z.</summary>
        public static Transform BuildBlade(Transform parent, Vector3 at, float size, Material blade, Material edge)
        {
            var root = SkylineBotLook.Empty("Blade", parent, at);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 22f;
                float r = 0.55f * size;
                var pos = new Vector3(0f, Mathf.Cos(a * Mathf.Deg2Rad) * r - r, Mathf.Sin(a * Mathf.Deg2Rad) * r);
                float w = (0.2f - i * 0.03f) * size;
                P(PrimitiveType.Cube, "Seg" + i, root, pos, new Vector3(0.04f * size, w, 0.24f * size), blade, false, new Vector3(a, 0f, 0f));
                P(PrimitiveType.Cube, "Edge" + i, root, pos + new Vector3(0f, -w * 0.5f, 0f), new Vector3(0.05f * size, 0.035f * size, 0.25f * size), edge, false, new Vector3(a, 0f, 0f));
            }
            return root;
        }

        /// <summary>A primitive part with <paramref name="m"/>; with a collider (a hit zone) only when <paramref name="collider"/>.
        /// Glowing parts cast no shadow.</summary>
        static GameObject P(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider, Vector3 euler = default(Vector3))
        {
            GameObject go;
            if (collider)
            {
                go = GameObject.CreatePrimitive(t);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.GetComponent<Renderer>().sharedMaterial = m;
            }
            else go = SkylineBotLook.Visual(t, name, parent, m);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (m != null && m.HasProperty("_BaseColor") && m.GetColor("_BaseColor").maxColorComponent > 1.01f)
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
