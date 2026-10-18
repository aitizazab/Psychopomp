using System.Collections.Generic;
using JamKit.Movement;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Builds Vesper's arena (<see cref="HellVesperArena"/>) at run time, the same way the editor builds the six rooms: one
    /// collider per solid (ramps as wedges, wall-run walls marked runnable with an ember stripe), glowing lava basins, obsidian
    /// under her dark zones with a violet haze, the fire lights over the lava, the exit gate. Built at run time so the fight
    /// works in the Hell scene and in any test scene without rebuilding the scene (the scene builder belongs to the rooms
    /// lane); about 70 primitives, built once.
    /// </summary>
    public static class HellVesperArenaView
    {
        public sealed class Built
        {
            public Transform root;
            public GameObject exitGate;
            /// <summary>The lights Nightfall puts out (the braziers under the look; the builder's lava lights without it).</summary>
            public readonly List<Light> fireLights = new List<Light>();
            public readonly List<Renderer> lavaRenderers = new List<Renderer>();
            public readonly List<Renderer> haze = new List<Renderer>();
        }

        static readonly Color Stone = new Color(0.09f, 0.085f, 0.11f);
        static readonly Color Wall = new Color(0.13f, 0.12f, 0.16f);
        static readonly Color Obsidian = new Color(0.03f, 0.03f, 0.045f);
        static readonly Color LavaHot = new Color(3.2f, 1.05f, 0.18f);
        static readonly Color Ember = new Color(2.6f, 0.7f, 0.12f);
        static readonly Color GateRed = new Color(0.42f, 0.12f, 0.08f);
        static readonly Color Haze = new Color(0.5f, 0.2f, 1.1f);

        public static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);

        public static Built Build(Transform parent, HellRoomDef d)
        {
            var b = new Built();
            b.root = new GameObject("Vesper Arena").transform;
            b.root.SetParent(parent, false);
            // named as the scene builder names the rooms' materials (SK_Hell_<kind>_F<floor>), so the look restyles them like a room's
            string F = "_F" + HellVesperArena.Floor;
            var stone = Named(SkylineBotLook.Lit(Stone), "SK_Hell_Stone" + F);
            var wall = Named(SkylineBotLook.Lit(Wall), "SK_Hell_Wall" + F);
            var wallRun = Named(SkylineBotLook.Lit(Wall), "SK_Hell_WallRun" + F);
            var cover = Named(SkylineBotLook.Lit(Wall * 1.25f), "SK_Hell_Cover" + F);
            var deck = Named(SkylineBotLook.Lit(Stone * 1.2f), "SK_Hell_Deck" + F);
            var ramp = Named(SkylineBotLook.Lit(Stone * 1.2f), "SK_Hell_Ramp" + F);
            var gate = Named(SkylineBotLook.Lit(GateRed), "SK_Hell_Gate" + F);
            var lava = Named(SkylineBotLook.Glow(LavaHot), "SK_Emissive_Lava" + F);
            var ember = SkylineBotLook.Glow(Ember);
            var obsidian = Named(SkylineBotLook.Lit(Obsidian), "SK_Hell_Obsidian" + F);
            var lavaGroup = new GameObject("Lava").transform;
            lavaGroup.SetParent(b.root, false);
            var fireGroup = new GameObject("Fire Lights").transform;
            fireGroup.SetParent(b.root, false);
            var haze = SkylineBotLook.Glow(Haze * 0.6f);

            foreach (var s in d.solids)
            {
                Material m;
                switch (s.role)
                {
                    case "floor": m = stone; break;
                    case "deck": m = deck; break;
                    case "ramp": m = s.ramp ? ramp : deck; break;
                    case "cover": m = cover; break;
                    case "gate": m = gate; break;
                    default: m = s.runnable ? wallRun : wall; break;
                }
                var go = s.ramp ? Ramp(b.root, s, m) : Box(b.root, s.id, U(s.box.min), U(s.box.max), m, true, s.runnable);
                if (s.id == d.exitGateId) b.exitGate = go;
                if (s.runnable && s.id.StartsWith("wallrun"))
                {
                    var bx = s.box;
                    Box(go.transform, "Ember Stripe", U(bx.min) + new Vector3(-0.02f, bx.max.Y - bx.min.Y - 0.25f, -0.02f), U(bx.max) + new Vector3(0.02f, 0.02f, 0.02f), ember, false, false);
                }
            }
            foreach (var h in d.hazards)
            {
                if (h.kind != HellHazardKind.Lava) continue;
                var go = Box(lavaGroup, h.id, U(h.box.min), U(h.box.max), lava, true, false);
                b.lavaRenderers.Add(go.GetComponent<Renderer>());
            }
            // her darkness: an obsidian plate on the floor of each zone, a thin violet haze ring at its rim
            foreach (var z in d.darkZones)
            {
                Box(b.root, "Obsidian", new Vector3(z.min.X, z.min.Y - 0.02f, z.min.Z), new Vector3(z.max.X, z.min.Y + 0.015f, z.max.Z), obsidian, false, false);
                var rim = Box(b.root, "Shadow Haze", new Vector3(z.min.X - 0.05f, z.min.Y + 0.015f, z.min.Z - 0.05f), new Vector3(z.max.X + 0.05f, z.min.Y + 0.05f, z.max.Z + 0.05f), haze, false, false);
                b.haze.Add(rim.GetComponent<Renderer>());
            }
            foreach (var li in d.lights)
            {
                var go = new GameObject("Lava Light");
                go.transform.SetParent(fireGroup, false);
                go.transform.position = U(li.position);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.42f, 0.12f);
                l.range = li.radius;
                l.intensity = 6f;
                l.shadows = LightShadows.None;
                b.fireLights.Add(l);
            }
            ApplyLook(b, d);
            return b;
        }

        static Material Named(Material shared, string name) => new Material(shared) { name = name };

        /// <summary>The look system's dressing (docs/HELL-LIGHTING.md), which the scene's rooms get at load and this arena,
        /// built later, never did (it rendered almost black: dark flat materials, a few short lava lights, no braziers): the
        /// floor's Hell/Surface materials and lava, the rig's lava lights and braziers (kept clear of her dark zones, so they
        /// still read dark). Without a look in force the builder's own lights stay (brighter than before).</summary>
        static void ApplyLook(Built b, HellRoomDef d)
        {
            var look = HellLookSwitch.Instance;
            if (look == null || HellLookSwitch.ActiveLook == HellLookDirection.None) return;
            try
            {
                int restyled = look.Restyle(b.root, HellVesperArena.Floor);
                var rig = look.Rig;
                if (rig == null) { Debug.Log("HELL_VESPER arena look: " + restyled + " restyled, no light rig"); return; }
                var palls = new List<Bounds>();
                foreach (var z in d.darkZones)
                {
                    var pb = new Bounds();
                    pb.SetMinMax(U(z.min), U(z.max));
                    palls.Add(pb);
                }
                var made = rig.BuildArena(b.root, d, HellVesperArena.Floor, palls, out var braziers);
                b.fireLights.Clear();            // the rig replaced the builder's lava lights (destroyed)
                b.fireLights.AddRange(braziers); // Nightfall puts the braziers out; the lava's lights keep burning
                Debug.Log("HELL_VESPER arena look " + look.DirectionOn(HellVesperArena.Floor) + ": " + restyled + " renderers restyled, " + made + " lava lights, " + braziers.Count + " braziers");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HELL_VESPER arena look failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 min, Vector3 max, Material m, bool collider, bool runnable)
        {
            var go = collider ? GameObject.CreatePrimitive(PrimitiveType.Cube) : SkylineBotLook.Visual(PrimitiveType.Cube, name, null, m, false);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation((min + max) * 0.5f, Quaternion.identity);
            var size = max - min;
            var ps = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / ps.x, size.y / ps.y, size.z / ps.z);
            if (collider)
            {
                go.GetComponent<Renderer>().sharedMaterial = m;
                go.AddComponent<MovementSurface>().runnable = runnable;
            }
            return go;
        }

        static GameObject Ramp(Transform parent, FloorSolid s, Material m)
        {
            var wedge = FloorMeshes.Wedge(s);
            var centre = (s.box.min + s.box.max) * 0.5f;
            var go = new GameObject(s.id);
            go.transform.SetParent(parent, false);
            go.transform.position = U(centre);
            var verts = new Vector3[wedge.positions.Length];
            var normals = new Vector3[wedge.normals.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = U(wedge.positions[i] - centre);
                normals[i] = U(wedge.normals[i]);
            }
            var mesh = new Mesh { name = "Wedge " + s.id, vertices = verts, normals = normals, triangles = wedge.triangles };
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.convex = true;
            go.AddComponent<MovementSurface>().runnable = s.runnable;
            return go;
        }
    }
}
