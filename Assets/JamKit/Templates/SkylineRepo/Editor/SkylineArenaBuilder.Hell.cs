using System.Collections.Generic;
using System.IO;
using System.Linq;
using JamKit.Movement;
using JamKit.Skyline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo.EditorTools
{
    /// <summary>
    /// The Hell scene, built from data (the B1 pattern; docs/HELL-ROOMS.md): all 18 rooms (<see cref="HellWorld.Room"/>, 6
    /// layouts x 3 floors) each in its own slot, one collider per <see cref="FloorSolid"/> (named by its id), the hazards (lava
    /// basins with <see cref="HellLava"/>, collapsing platforms with <see cref="HellCollapsePlatform"/>, beam-lane emitter posts;
    /// the beams themselves are the enemies branch's <c>HellBeamLane</c>, made at run time), the objective's pieces (sigils,
    /// the lantern, its pedestal and the altar, the chain anchors, the brand's ring, the wells' caps), the dark zones (a shadow
    /// pall, obsidian tops), the fire lights, the safe path's cold guide studs, the three floor shafts, the player, the HUD and
    /// the <see cref="HellRunDirector"/>. Grey-box with clear materials: stone per floor (ash, iron-brown, obsidian), glowing
    /// lava, collapse platforms with ember edges, wall-run walls with an ember stripe. Nothing is hand-placed.
    /// </summary>
    public static partial class SkylineArenaBuilder
    {
        public const string HellScenePath = SkylineScenes.Folder + "/HellDescent.unity";

        sealed class HellMats
        {
            public Material stone, wall, cover, deck, ramp, gate, lava, lavaDim, collapse, ember, wallrun, obsidian, pall, sigil, lantern,
                altar, anchor, chain, brand, wellCap, well, guide, post, shaft;
        }

        static readonly Color[] HellStone = { new Color(0.29f, 0.27f, 0.25f), new Color(0.24f, 0.18f, 0.16f), new Color(0.09f, 0.085f, 0.11f) };
        static readonly Color[] HellWall = { new Color(0.36f, 0.33f, 0.31f), new Color(0.30f, 0.22f, 0.19f), new Color(0.13f, 0.12f, 0.16f) };

        static HellMats MakeHellMats(int floor) => new HellMats
        {
            stone = Lit("SK_Hell_Stone_F" + floor, HellStone[floor]),
            wall = Lit("SK_Hell_Wall_F" + floor, HellWall[floor]),
            cover = Lit("SK_Hell_Cover_F" + floor, HellWall[floor] * 1.25f),
            deck = Lit("SK_Hell_Deck_F" + floor, HellStone[floor] * 1.15f),
            ramp = Lit("SK_Hell_Ramp_F" + floor, HellStone[floor] * 1.1f),
            gate = Lit("SK_Hell_Gate", new Color(0.42f, 0.12f, 0.08f)),
            lava = Emissive("SK_Emissive_Lava", new Color(3.2f, 1.05f, 0.18f)),
            lavaDim = Emissive("SK_Emissive_Lava_Well", new Color(2.2f, 0.6f, 0.1f)),
            collapse = Lit("SK_Hell_Collapse", new Color(0.33f, 0.24f, 0.2f)),
            ember = Emissive("SK_Emissive_Ember", new Color(2.6f, 0.7f, 0.12f)),
            wallrun = Lit("SK_Hell_WallRun_F" + floor, HellWall[floor] * 0.9f),
            obsidian = Lit("SK_Hell_Obsidian", new Color(0.03f, 0.03f, 0.045f)),
            pall = Glass("SK_Glass_ShadowPall", new Color(0.01f, 0.0f, 0.03f, 0.55f)),
            sigil = Emissive("SK_Emissive_Sigil", new Color(0.85f, 0.3f, 1.25f)),
            lantern = Emissive("SK_Emissive_Lantern", new Color(0.6f, 2.4f, 2.6f)),
            altar = Lit("SK_Hell_Altar", new Color(0.55f, 0.5f, 0.6f)),
            anchor = Emissive("SK_Emissive_Anchor", new Color(2.8f, 0.25f, 0.2f)),
            chain = Lit("SK_Hell_Chain", new Color(0.2f, 0.19f, 0.2f)),
            brand = Emissive("SK_Emissive_Brand", new Color(1.1f, 0.55f, 2.4f)),
            wellCap = Emissive("SK_Emissive_WellLava", new Color(3.0f, 0.8f, 0.1f)),
            well = Lit("SK_Hell_Well", new Color(0.18f, 0.16f, 0.15f)),
            guide = Emissive("SK_Emissive_SafeGuide", new Color(0.55f, 0.75f, 1.1f)),
            post = Lit("SK_Hell_BeamPost", new Color(0.15f, 0.1f, 0.1f)),
            shaft = Lit("SK_Hell_Shaft", new Color(0.07f, 0.05f, 0.05f)),
        };

        /// <summary>Builds the Hell scene and saves it (and puts it first in the Build Settings).</summary>
        public static void BuildHell(string path)
        {
            var scene = SkylineScenes.NewScene();
            _template = null;
            HellLighting();
            var world = new GameObject("Hell").transform;
            int rooms = 0, solids = 0;
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
            {
                var mats = MakeHellMats(f);
                var floorRoot = Group(world, "Floor " + (f + 1) + " " + HellRoomVariants.Floors[f].name);
                foreach (var kind in HellRoomLayouts.Kinds)
                {
                    solids += BuildHellRoom(floorRoot, HellWorld.Room(f, kind), f, kind, mats);
                    rooms++;
                }
                BuildShaft(floorRoot, f, mats);
            }
            // the abyss under the open rims (docs/HELL-ROOMS.md, "Open edges"): a lava sea far below every room, no collider (a
            // fall off a cliff edge is a sink before you reach it)
            float seaX1 = HellRoomLayouts.Kinds.Length * HellWorld.SlotSpacing + 250f, seaZ1 = (HellRoomVariants.FloorCount - 1) * HellWorld.FloorSpacing + 250f;
            NoShadow(Box(world, "Abyss Sea", new Vector3(-250f, HellRoomLayouts.AbyssSeaY - 1f, -250f), new Vector3(seaX1, HellRoomLayouts.AbyssSeaY, seaZ1), MakeHellMats(0).lava, false, null, true));

            // the player, the HUD, the run
            var start = HellWorld.Room(0, HellRoomLayouts.Kinds[0]);
            var player = SkylinePlayerBuilder.Build(null, U(start.entry), start.entryYaw);
            var hudGo = new GameObject("HUD");
            var doc = hudGo.AddComponent<UIDocument>();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel != null) doc.panelSettings = panel;
            var hud = hudGo.AddComponent<SkylineHud>();
            hud.panelSettings = panel;
            hud.player = player;
            var director = new GameObject("Hell Run").AddComponent<HellRunDirector>();
            director.player = player;
            try { SkylineLook.SetupCameras(scene); }
            catch (System.Exception e) { Debug.LogWarning("HELL_BUILD cameras not set up by the look: " + e.Message); }
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.01f, 0.01f);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
            AddFirstInBuildSettings(path);
            Debug.Log("HELL_BUILD built " + path + ": " + rooms + " rooms, " + solids + " solids");
        }

        static void HellLighting()
        {
            var sun = new GameObject("Hell Glow (directional)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.45f, 0.3f);
            sun.intensity = 0.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.09f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.08f, 0.02f, 0.015f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 220f;
        }

        static void AddFirstInBuildSettings(string path)
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ one room

        static int BuildHellRoom(Transform parent, HellRoomDef d, int floor, HellRoomKind kind, HellMats m)
        {
            var root = Group(parent, "Room " + (floor + 1) + " " + kind);
            var solidsRoot = Group(root, "Solids");
            int n = 0;
            foreach (var s in d.solids)
            {
                Material mat;
                switch (s.role)
                {
                    case "floor": mat = m.stone; break;
                    case "deck": mat = m.deck; break;
                    case "ramp": mat = m.ramp; break;
                    case "cover": mat = m.cover; break;
                    case "gate": mat = m.gate; break;
                    case "well": mat = m.well; break;
                    default: mat = s.runnable && s.id.StartsWith("wallrun") ? m.wallrun : m.wall; break;
                }
                var go = s.ramp ? FloorRamp(solidsRoot, s, mat) : Box(solidsRoot, s.id, U(s.box.min), U(s.box.max), mat, true, s.runnable);
                go.name = s.id;
                n++;
                if (s.id.StartsWith("wallrun"))
                {
                    // an ember stripe along a wall-run wall's top: the fast line's cue
                    var b = s.box;
                    NoShadow(Box(go.transform, "Ember Stripe", U(b.min) + new Vector3(-0.02f, b.max.Y - b.min.Y - 0.25f, -0.02f), U(b.max) + new Vector3(0.02f, 0.02f, 0.02f), m.ember, false, null, true));
                }
                if (s.role == "well")
                {
                    var b = s.box;
                    var cap = Box(go.transform, "Well Cap", new Vector3(b.min.X + 0.3f, b.max.Y, b.min.Z + 0.3f), new Vector3(b.max.X - 0.3f, b.max.Y + 0.05f, b.max.Z - 0.3f), m.wellCap, false, null, true);
                    NoShadow(cap);
                }
            }
            // name the well caps by stop index (the room finds "Well Cap i")
            if (kind == HellRoomKind.OozeWells)
            {
                var caps = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Well Cap").ToList();
                for (int i = 0; i < d.stops.Count; i++)
                {
                    var p = U(d.stops[i].position);
                    var best = caps.OrderBy(c => (Flat(c.position) - Flat(p)).sqrMagnitude).FirstOrDefault();
                    if (best != null) { best.name = "Well Cap " + i; caps.Remove(best); }
                }
            }

            // hazards
            var lavaRoot = Group(root, "Lava");
            lavaRoot.gameObject.AddComponent<HellLava>();
            var platRoot = Group(root, "Collapsing Platforms");
            foreach (var h in d.hazards)
            {
                switch (h.kind)
                {
                    case HellHazardKind.Lava:
                        // a floorless basin (The Collapse's bridge) has no collider: a fallen tile drops you into the lava
                        var lava = Box(lavaRoot, h.id, U(h.box.min), U(h.box.max), m.lava, d.lavaFloor, d.lavaFloor ? false : (bool?)null);
                        NoShadow(lava);
                        break;
                    case HellHazardKind.Collapse:
                        var p = Box(platRoot, h.id, U(h.box.min), U(h.box.max), m.collapse, true, false);
                        p.AddComponent<HellCollapsePlatform>().hazardId = h.id;
                        // ember edges: a thin glowing rim just under the top
                        var b = h.box;
                        NoShadow(Box(p.transform, "Ember Rim", new Vector3(b.min.X - 0.03f, b.max.Y - 0.12f, b.min.Z - 0.03f), new Vector3(b.max.X + 0.03f, b.max.Y - 0.04f, b.max.Z + 0.03f), m.ember, false, null, true));
                        n++;
                        break;
                    case HellHazardKind.Beam:
                        var bb = h.box;
                        bool alongX = bb.max.X - bb.min.X >= bb.max.Z - bb.min.Z;
                        var c = U((bb.min + bb.max) * 0.5f);
                        Vector3 a0 = alongX ? new Vector3(bb.min.X - 0.3f, 0f, c.z) : new Vector3(c.x, 0f, bb.min.Z - 0.3f);
                        Vector3 a1 = alongX ? new Vector3(bb.max.X + 0.3f, 0f, c.z) : new Vector3(c.x, 0f, bb.max.Z + 0.3f);
                        float ground = bb.min.Y - HellRoomLayouts.BeamHeight + 0.1f;
                        foreach (var a in new[] { a0, a1 })
                        {
                            var post = Box(root, "Beam Post " + h.id, new Vector3(a.x - 0.2f, ground, a.z - 0.2f), new Vector3(a.x + 0.2f, ground + 2.2f, a.z + 0.2f), m.post, false, null, true);
                            NoShadow(Box(post.transform, "Emitter", new Vector3(a.x - 0.25f, c.y - 0.12f, a.z - 0.25f), new Vector3(a.x + 0.25f, c.y + 0.12f, a.z + 0.25f), m.anchor, false, null, true));
                        }
                        break;
                }
            }

            // the objective's pieces
            var obj = Group(root, "Objective");
            for (int i = 0; i < d.stops.Count; i++)
            {
                var s = d.stops[i];
                var at = U(s.position);
                var stop = new GameObject("Stop " + s.id).transform;
                stop.SetParent(obj, false);
                stop.position = at;
                switch (kind)
                {
                    case HellRoomKind.SixSigils:
                        NoShadow(Box(stop, "Sigil", at + new Vector3(-0.6f, 0f, -0.6f), at + new Vector3(0.6f, 0.08f, 0.6f), m.sigil, false, null, true));
                        NoShadow(Box(stop, "Sigil Beam", at + new Vector3(-0.15f, 0.08f, -0.15f), at + new Vector3(0.15f, 9f, 0.15f), m.sigil, false, null, true));
                        break;
                    case HellRoomKind.SoulLantern:
                        if (i == 0)
                        {
                            stop.name = "Lantern Pedestal";
                            Box(stop, "Pedestal", at + new Vector3(-0.4f, 0f, -0.4f), at + new Vector3(0.4f, 1.1f, 0.4f), m.altar, false, null, true);
                            var lantern = Box(obj, "Lantern", at + new Vector3(-0.25f, 1.15f, -0.25f), at + new Vector3(0.25f, 1.75f, 0.25f), m.lantern, false, null, true);
                            NoShadow(lantern);
                            var l = lantern.AddComponent<Light>();
                            l.type = LightType.Point; l.color = new Color(0.4f, 0.9f, 1f); l.range = 7f; l.intensity = 2f;
                        }
                        else
                        {
                            stop.name = "Altar";
                            Box(stop, "Altar Block", at + new Vector3(-1.2f, 0f, -0.8f), at + new Vector3(1.2f, 1.0f, 0.8f), m.altar, false, null, true);
                            NoShadow(Box(stop, "Altar Glow", at + new Vector3(-1.25f, 0.95f, -0.85f), at + new Vector3(1.25f, 1.02f, 0.85f), m.lantern, false, null, true));
                        }
                        break;
                    case HellRoomKind.BreakChains:
                        var anchor = Box(obj, "Anchor " + i, at - Vector3.one * 0.6f, at + Vector3.one * 0.6f, m.anchor, true, null);
                        var ha = anchor.AddComponent<HellAnchor>();
                        ha.index = i;
                        var zone = anchor.AddComponent<SkylineHitZone>();
                        zone.owner = ha;
                        // the chain: links from the anchor to the exit gate's middle
                        var gateC = U((d.Solid(d.exitGateId).box.min + d.Solid(d.exitGateId).box.max) * 0.5f);
                        int links = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(at, gateC) / 1.2f));
                        for (int k = 1; k < links; k++)
                        {
                            var q = Vector3.Lerp(at, gateC, k / (float)links);
                            NoShadow(Box(anchor.transform, "Link", q - Vector3.one * 0.12f, q + Vector3.one * 0.12f, m.chain, false, null, true));
                        }
                        break;
                    case HellRoomKind.HoldBrand:
                        // the brand ring: a glowing band 20 m across round the lava pool (HellRoom pulses it faster the
                        // faster you charge it), made of short segments so the track and the pool stay visible inside it
                        var ringRoot = new GameObject("Brand Circle").transform;
                        ringRoot.SetParent(stop, false);
                        float rr = d.hold != null ? d.hold.radius : HellBrandTuning.RingRadius;
                        const int segs = 48;
                        float segLen = 2f * Mathf.PI * rr / segs * 1.04f;
                        for (int k = 0; k < segs; k++)
                        {
                            float a = (k + 0.5f) * 360f / segs;
                            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            seg.name = "Brand Segment";
                            Object.DestroyImmediate(seg.GetComponent<Collider>());
                            seg.AddComponent<SkylineNoCollider>();
                            seg.transform.SetParent(ringRoot, false);
                            var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                            seg.transform.position = at + dir * rr + Vector3.up * 0.04f;
                            seg.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir), Vector3.up);
                            seg.transform.localScale = new Vector3(0.45f, 0.06f, segLen);
                            seg.GetComponent<Renderer>().sharedMaterial = m.brand;
                            NoShadow(seg);
                        }
                        break;
                    case HellRoomKind.Collapse:
                        NoShadow(Box(stop, "Exit Glow", at + new Vector3(-2f, 0f, -0.3f), at + new Vector3(2f, 0.06f, 0.3f), m.lantern, false, null, true));
                        break;
                }
            }
            if (kind == HellRoomKind.BreakChains)
                foreach (var ha in root.GetComponentsInChildren<HellAnchor>(true)) ha.room = null;   // set below once the room exists

            // darkness: a shadow pall over each zone and obsidian under it
            var dark = Group(root, "Dark Zones");
            foreach (var z in d.darkZones)
            {
                var pall = Box(dark, "Shadow Pall", U(z.min), U(z.max), m.pall, false, null, true);
                NoShadow(pall);
                NoShadow(Box(dark, "Obsidian", new Vector3(z.min.X, z.min.Y - 0.02f, z.min.Z), new Vector3(z.max.X, z.min.Y + 0.01f, z.max.Z), m.obsidian, false, null, true));
            }

            // fire lights over the lava (none reaches a dark zone: the lint checks it)
            var lights = Group(root, "Fire Lights");
            foreach (var li in d.lights)
            {
                var go = new GameObject("Lava Light");
                go.transform.SetParent(lights, false);
                go.transform.position = U(li.position);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.42f, 0.12f);
                l.range = li.radius;
                l.intensity = 2.2f;
                l.shadows = LightShadows.None;
            }

            // the safe path's cold guide studs (every ~8 m, on the ground): the slow way reads as the lit stone way
            var guides = Group(root, "Safe Guides");
            float acc = 0f;
            for (int i = 1; i < d.safePath.Count; i++)
            {
                var a = U(d.safePath[i - 1]);
                var b = U(d.safePath[i]);
                float len = Vector3.Distance(a, b);
                for (float t = 8f - acc; t < len; t += 8f)
                {
                    var p = Vector3.Lerp(a, b, t / len);
                    NoShadow(Box(guides, "Guide", p + new Vector3(-0.12f, 0.0f, -0.12f), p + new Vector3(0.12f, 0.06f, 0.12f), m.guide, false, null, true));
                }
                acc = (acc + len) % 8f;
            }

            var room = root.gameObject.AddComponent<HellRoom>();
            room.kind = kind;
            room.floor = floor;
            foreach (var ha in root.GetComponentsInChildren<HellAnchor>(true)) ha.room = room;
            return n;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static void BuildShaft(Transform parent, int floor, HellMats m)
        {
            var c = U(HellWorld.ShaftCentre(floor));
            float h = HellWorld.ShaftHalf, top = HellWorld.ShaftTop + 4f, bottom = HellWorld.ShaftBottom;
            var root = Group(parent, "Shaft " + (floor + 1));
            Box(root, "Shaft Floor", c + new Vector3(-h, bottom - 0.5f, -h), c + new Vector3(h, bottom, h), m.shaft, true, null);
            Box(root, "Shaft Wall W", c + new Vector3(-h - 0.6f, bottom, -h), c + new Vector3(-h, top, h), m.shaft, true, false);
            Box(root, "Shaft Wall E", c + new Vector3(h, bottom, -h), c + new Vector3(h + 0.6f, top, h), m.shaft, true, false);
            Box(root, "Shaft Wall S", c + new Vector3(-h - 0.6f, bottom, -h - 0.6f), c + new Vector3(h + 0.6f, top, -h), m.shaft, true, false);
            Box(root, "Shaft Wall N", c + new Vector3(-h - 0.6f, bottom, h), c + new Vector3(h + 0.6f, top, h + 0.6f), m.shaft, true, false);
            NoShadow(Box(root, "Shaft Glow", c + new Vector3(-h + 0.5f, bottom, -h + 0.5f), c + new Vector3(h - 0.5f, bottom + 0.04f, h - 0.5f), m.lava, false, null, true));
            var lg = new GameObject("Shaft Light");
            lg.transform.SetParent(root, false);
            lg.transform.position = c + new Vector3(0f, bottom + 3f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.35f, 0.1f); l.range = 30f; l.intensity = 3f;
        }
    }

    /// <summary>The Hell scene's editor entry points.</summary>
    public static class HellScene
    {
        [MenuItem("JamKit/Hell/Build the Hell scene")]
        public static void BuildMenu()
        {
            SkylineArenaBuilder.BuildHell(SkylineArenaBuilder.HellScenePath);
            AssetDatabase.Refresh();
        }

        /// <summary><c>Unity.exe -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellScene.BuildFromCommandLine -logFile -</c>:
        /// lints the 18 rooms' data, builds the scene, puts it first in the Build Settings, exits 0 (or 1 on a failure).</summary>
        public static void BuildFromCommandLine()
        {
            int code = 0;
            try
            {
                int problems = 0;
                foreach (var r in HellRoomVariants.All())
                    foreach (var p in HellRoomLint.Check(r).problems) { Debug.LogError("HELL_LINT floor " + r.floor + ": " + p); problems++; }
                Debug.Log("HELL_LINT " + problems + " problems over 18 rooms");
                SkylineArenaBuilder.BuildHell(SkylineArenaBuilder.HellScenePath);
                AssetDatabase.Refresh();
                if (problems > 0) code = 1;
            }
            catch (System.Exception e)
            {
                Debug.LogError("HELL_BUILD failed: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }
    }
}
