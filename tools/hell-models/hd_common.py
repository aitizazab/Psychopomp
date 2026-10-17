# Shared helpers for the HellDescent enemy models (Blender 4.5, run headless).
# Materials are named by role: M_* lit, G_* glow (HDR, unlit in the game). The Unity editor script
# (HellModelsBuild.cs) maps these names onto the game's palette (HellLook), so the colours here are only for the preview.
import bpy, bmesh, math, os, random
from mathutils import Vector, Matrix, Euler

# the preview's textures: the repo's own (tools/hell-models sits two levels under the repo root), else the main checkout
_REPO_TEX = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "JamKit", "Templates", "SkylineRepo", "Resources", "HellLook", "Textures")
TEX = os.environ.get("HELL_TEX") or (_REPO_TEX if os.path.isdir(_REPO_TEX) else r"C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\Assets\JamKit\Templates\SkylineRepo\Resources\HellLook\Textures")

PALETTE = {
    "M_Char": (0.08, 0.035, 0.03), "M_Bone": (0.86, 0.5, 0.2), "M_Rust": (0.46, 0.13, 0.05),
    "M_Steel": (0.22, 0.2, 0.2), "M_Crust": (0.12, 0.085, 0.075), "M_Hollow": (0.02, 0.01, 0.01),
    "M_Flesh": (0.36, 0.06, 0.04), "M_Gown": (0.05, 0.025, 0.07), "M_Skin": (0.2, 0.06, 0.04),
    "M_Gold": (0.75, 0.45, 0.12), "M_Veil": (0.12, 0.06, 0.16),
    "M_Robe": (0.045, 0.035, 0.04), "M_Ash": (0.4, 0.37, 0.38), "M_Hair": (0.2, 0.05, 0.03),
    "G_Core": (5, 3.6, 1.3), "G_Eye": (5, 3, 0.4), "G_Flame": (3.4, 1.0, 0.15), "G_FlameDeep": (2.6, 0.35, 0.06),
    "G_Edge": (5, 1.5, 0.35), "G_Lava": (2.2, 0.5, 0.07), "G_LavaHot": (3.6, 1.4, 0.25), "G_Ember": (2.6, 0.75, 0.12),
    "G_Violet": (1.6, 0.6, 3.6), "G_Hair": (3.2, 0.9, 0.12), "G_HairHot": (4.5, 2.2, 0.5),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    c = PALETTE.get(name, (0.5, 0.5, 0.5))
    if name.startswith("G_"):
        k = max(c)
        bsdf.inputs["Base Color"].default_value = (c[0] / k, c[1] / k, c[2] / k, 1)
        bsdf.inputs["Emission Color"].default_value = (c[0] / k, c[1] / k, c[2] / k, 1)
        bsdf.inputs["Emission Strength"].default_value = k * 1.6
        if name == "G_Lava":
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(os.path.join(TEX, "lava_emis.jpg"))
            mix = nt.nodes.new("ShaderNodeMixRGB"); mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1
            mix.inputs[2].default_value = (1, 0.16, 0.01, 1)
            nt.links.new(tex.outputs["Color"], mix.inputs[1])
            add = nt.nodes.new("ShaderNodeMixRGB"); add.blend_type = 'ADD'; add.inputs[0].default_value = 1
            add.inputs[2].default_value = (0.35, 0.03, 0.0, 1)
            nt.links.new(mix.outputs[0], add.inputs[1])
            nt.links.new(add.outputs[0], bsdf.inputs["Emission Color"])
            nt.links.new(add.outputs[0], bsdf.inputs["Base Color"])
            bsdf.inputs["Emission Strength"].default_value = 3.0
    else:
        bsdf.inputs["Base Color"].default_value = (*c, 1)
        bsdf.inputs["Roughness"].default_value = 0.75
        if name in ("M_Gold", "M_Steel"):
            bsdf.inputs["Metallic"].default_value = 0.8
            bsdf.inputs["Roughness"].default_value = 0.4
        if name in ("M_Char", "M_Crust", "M_Skin", "M_Hair"):
            # the in-game re-dress: a dark rock base with glowing ember cracks (Resources/HellLook textures)
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(os.path.join(TEX, "basalt_diff.jpg"))
            mix = nt.nodes.new("ShaderNodeMixRGB"); mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1
            mix.inputs[2].default_value = (c[0] * 4, c[1] * 4, c[2] * 4, 1)
            nt.links.new(tex.outputs["Color"], mix.inputs[1])
            nt.links.new(mix.outputs[0], bsdf.inputs["Base Color"])
            em = nt.nodes.new("ShaderNodeTexImage")
            em.image = bpy.data.images.load(os.path.join(TEX, "lava_emis.jpg"))
            ramp = nt.nodes.new("ShaderNodeValToRGB")
            ramp.color_ramp.elements[0].position = 0.35 if name == "M_Hair" else 0.55
            ramp.color_ramp.elements[1].position = 0.7 if name == "M_Hair" else 0.85
            nt.links.new(em.outputs["Color"], ramp.inputs[0])
            tint = nt.nodes.new("ShaderNodeMixRGB"); tint.blend_type = 'MULTIPLY'; tint.inputs[0].default_value = 1
            tint.inputs[2].default_value = (1, 0.28, 0.04, 1)
            nt.links.new(ramp.outputs[0], tint.inputs[1])
            nt.links.new(tint.outputs[0], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 6.0 if name == "M_Hair" else (2.5 if name != "M_Skin" else 3.5)
    return m


def link(obj, parent=None):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def empty(name, loc, parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.1
    link(e, parent)
    e.location = loc
    return e


def mesh_obj(name, bm, material_names, parent=None, smooth=True):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for mn in material_names:
        me.materials.append(mat(mn))
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    o = bpy.data.objects.new(name, me)
    link(o, parent)
    return o


def prim(kind, name, loc, scale, material, parent=None, rot=(0, 0, 0), segs=24, rings=12, smooth=True):
    bm = bmesh.new()
    if kind == "sphere":
        bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=0.5)
    elif kind == "cube":
        bmesh.ops.create_cube(bm, size=1.0)
    elif kind == "cyl":
        bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=0.5, radius2=0.5, depth=1.0)
    elif kind == "cone":
        bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=0.5, radius2=0.0, depth=1.0)
    elif kind == "ico":
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.5)
    o = mesh_obj(name, bm, [material], parent, smooth)
    o.location = loc
    o.scale = scale
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    return o


def noise_displace(obj, amount, seed=1, scale=2.0):
    random.seed(seed)
    off = Vector((random.random() * 100, random.random() * 100, random.random() * 100))
    from mathutils import noise
    for v in obj.data.vertices:
        n = noise.noise((v.co * scale) + off)
        v.co += v.normal * n * amount


def apply_all_transforms(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def uv_unwrap(objs):
    # box projection in world metres (each face by its dominant normal axis): the tiling hide/crack textures read evenly
    for o in objs:
        if o.type != 'MESH':
            continue
        bm = bmesh.new(); bm.from_mesh(o.data)
        uvl = bm.loops.layers.uv.verify()
        mw = o.matrix_world
        for f in bm.faces:
            n = f.normal
            ax = max(range(3), key=lambda i: abs(n[i]))
            for l in f.loops:
                w = mw @ l.vert.co
                if ax == 0:
                    l[uvl].uv = (w.y * 0.8, w.z * 0.8)
                elif ax == 1:
                    l[uvl].uv = (w.x * 0.8, w.z * 0.8)
                else:
                    l[uvl].uv = (w.x * 0.8, w.y * 0.8)
        bm.to_mesh(o.data); bm.free()


def import_glb(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def export_fbx(path, objs, armature=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    kw = dict(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
              object_types={'EMPTY', 'MESH', 'ARMATURE'}, mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True)
    if armature:
        kw.update(bake_space_transform=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                  bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, primary_bone_axis='Y', secondary_bone_axis='X')
    else:
        kw.update(bake_space_transform=True, bake_anim=False)
    bpy.ops.export_scene.fbx(**kw)


def render_turntable(out_png, target_height, focus_z, yaw_deg=-35, dist_k=2.3, label=None, frame=None):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE_NEXT'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.render.film_transparent = False
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    if frame is not None:
        sc.frame_set(frame)
    w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
    bg = w.node_tree.nodes["Background"]; bg.inputs[0].default_value = (0.05, 0.012, 0.01, 1); bg.inputs[1].default_value = 0.6
    # floor: dark ash
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
    fl = bpy.context.active_object; fl.name = "PreviewFloor"
    fm = bpy.data.materials.new("PreviewFloorMat"); fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.03, 0.02, 0.02, 1)
    fl.data.materials.append(fm)

    def light(name, kind, loc, energy, color, size=1.0):
        ld = bpy.data.lights.new(name, kind); ld.energy = energy; ld.color = color
        if kind == 'AREA':
            ld.size = size
        lo = bpy.data.objects.new(name, ld); sc.collection.objects.link(lo); lo.location = loc
        d = Vector((0, 0, focus_z)) - Vector(loc)
        lo.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        return lo
    h = target_height
    light("Key", 'AREA', (-2.5 * h, -2.5 * h, 2.0 * h), 300 * h, (1.0, 0.75, 0.6), 2 * h)
    light("Rim", 'AREA', (2.0 * h, 2.5 * h, 1.6 * h), 500 * h, (1.0, 0.35, 0.1), 2 * h)
    light("Fill", 'AREA', (3.0 * h, -2.0 * h, 0.6 * h), 60 * h, (0.6, 0.8, 1.0), 2 * h)
    light("Under", 'POINT', (0, -1.0 * h, 0.1), 25 * h, (1.0, 0.3, 0.05))
    cam_d = bpy.data.cameras.new("Cam"); cam_d.lens = 35
    cam = bpy.data.objects.new("Cam", cam_d); sc.collection.objects.link(cam); sc.camera = cam
    yaw = math.radians(yaw_deg)
    dist = h * dist_k
    cam.location = (math.sin(yaw) * dist, -math.cos(yaw) * dist, focus_z + h * 0.08)
    d = Vector((0, 0, focus_z)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    # bloom: compositor glare
    sc.use_nodes = True
    nt = sc.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    rl = nt.nodes.new("CompositorNodeRLayers")
    gl = nt.nodes.new("CompositorNodeGlare"); gl.glare_type = 'FOG_GLOW'; gl.quality = 'HIGH'; gl.threshold = 1.0; gl.size = 7
    comp = nt.nodes.new("CompositorNodeComposite")
    nt.links.new(rl.outputs["Image"], gl.inputs["Image"])
    nt.links.new(gl.outputs["Image"], comp.inputs["Image"])
    sc.render.filepath = out_png
    os.makedirs(os.path.dirname(out_png), exist_ok=True)
    bpy.ops.render.render(write_still=True)
