# Vesper, Lucifer's bride, the Evening Star: a molten, fiery, long-haired, willful woman with a stylish veneer.
# Base: Quaternius "Woman in Dress" (CC0, Poly Pizza zMyPlQXBzq, rigged, with Female_Idle). Re-dressed: obsidian skin with
# lava cracks, a floor-length black-violet gown with an ember hem, long fire hair, a gold crown with the Evening Star on her
# brow, earrings, a choker, a high standing collar, gold cuffs and a belt. Accessories ride their bones (bone-parented).
# Actions: Idle (hers), Cast, Blink, Reel, Kneel (posed here on her bones).
# Run: blender -b --factory-startup --python vesper.py -- <out.fbx> <preview.png> [pose-for-preview]
import bpy, bmesh, math, random, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector, Matrix, Quaternion
from hd_common import reset, mat, mesh_obj, prim, noise_displace, uv_unwrap, import_glb, export_fbx, render_turntable, link
from hd_parts import tube, flame, solidify, finalize

SRC = r"C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\hell-pass\art\downloads\polypizza-vesper\zMyPlQXBzq.glb"
args = sys.argv[sys.argv.index("--") + 1:]
OUT_FBX, OUT_PNG = args[0], args[1]
PREVIEW = args[2] if len(args) > 2 else "Idle"
reset()
random.seed(11)
objs = import_glb(SRC)
arm = [o for o in objs if o.type == 'ARMATURE'][0]
body = [o for o in objs if o.type == 'MESH' and o.name == 'Female'][0]
rootnode = [o for o in objs if o.name == 'RootNode'][0]
for o in objs:
    if o.type == 'MESH' and o is not body:
        bpy.data.objects.remove(o)
for o in list(bpy.data.objects):
    if o.type == 'EMPTY' and o is not rootnode:
        bpy.data.objects.remove(o)
arm.name = "VesperRig"
if arm.animation_data is not None:
    arm.animation_data.action = None
for pb in arm.pose.bones:
    pb.matrix_basis = Matrix.Identity(4)
arm.data.pose_position = "REST"
body.name = "VesperBody"
bpy.context.view_layer.update()

# ---- her skin and clothes, by role
remap = {"Dress": "M_Gown", "Skin": "M_Skin", "Shoes": "M_Gold", "Hair": "G_Hair", "Eyes": "G_Eye"}
for i, m in enumerate(body.data.materials):
    body.data.materials[i] = mat(remap.get(m.name.split(".")[0], "M_Gown"))

MW = arm.matrix_world


def bone_world(name):
    return MW @ arm.data.bones[name].head_local


def attach(o, bone):
    """Bone-parent a world-space part, keeping it where it is."""
    bpy.context.view_layer.update()
    world = o.matrix_world.copy()
    o.parent = arm
    o.parent_type = 'BONE'
    o.parent_bone = bone
    bpy.context.view_layer.update()
    o.matrix_world = world
    return o


parts = []


def part(o, bone):
    finalize(o)
    # pivot at the part's centre
    vs = [v.co for v in o.data.vertices]
    c = sum(vs, Vector()) / max(1, len(vs))
    o.data.transform(Matrix.Translation(-c))
    o.location = c
    attach(o, bone)
    parts.append(o)
    return o


head = bone_world("Head")       # the neck's top
H = 4.67                        # her source height (T-pose), before the final scale
hc = Vector((0, 0.02, 4.38))    # the head's centre (source units)

# ---- the gown: floor-length, flared, folds, an ember-lit hem; rides the hips
bm = bmesh.new()
segs, rings = 36, 12
z_top, z_bot = 2.55, 0.02
rows = []
for i in range(rings + 1):
    t = i / rings
    z = z_top + (z_bot - z_top) * t
    r = 0.47 + 0.55 * t ** 1.6
    row = []
    for j in range(segs):
        a = j / segs * math.tau
        fold = 1 + (0.015 + 0.06 * t) * math.sin(a * 9)
        # a trailing train behind (+Y is her back)
        yy = math.sin(a) * r * 0.9 * fold + (0.35 * t ** 2 if math.sin(a) > 0 else 0) * math.sin(a)
        zz = z if i < rings else z + (0.03 if j % 2 else 0)
        row.append(bm.verts.new((math.cos(a) * r * fold, yy, zz)))
    rows.append(row)
for i in range(rings):
    for j in range(segs):
        bm.faces.new((rows[i][j], rows[i][(j + 1) % segs], rows[i + 1][(j + 1) % segs], rows[i + 1][j]))
gown = mesh_obj("Gown_nc", bm, ["M_Gown", "G_Ember"])
for p in gown.data.polygons:
    if p.center.z < z_bot + 0.2:
        p.material_index = 1
solidify(gown, 0.04)
part(gown, "Hips")

# belt with the star clasp
belt = prim("cyl", "Belt_nc", (0, -0.0, 2.6), (1.0, 0.82, 0.1), "M_Gold", segs=32)
part(belt, "Hips")

# ---- long fire hair: strands from the crown down her back to the hips, licking out at the tips
strands = []
for k in range(11):
    u = (k - 5) / 5.0                 # -1 .. 1 across the back of the head
    x0 = u * 0.26
    start = (x0, 0.12 + 0.06 * (1 - abs(u)), 4.6 - 0.12 * abs(u))
    lenk = 1.0 - 0.25 * abs(u) + random.uniform(-0.08, 0.08)
    pts = [start,
           (x0 * 1.25, 0.34, 4.32),
           (x0 * 1.45 + random.uniform(-0.04, 0.04), 0.42, 3.8),
           (x0 * 1.55 + random.uniform(-0.06, 0.06), 0.46 + 0.05 * abs(u), 3.8 - 0.9 * lenk),
           (x0 * 1.7 + random.uniform(-0.12, 0.12), 0.56 + random.uniform(0, 0.12), 3.8 - 1.55 * lenk)]
    radii = [0.11, 0.13, 0.12, 0.08, 0.005]
    s = tube("HairStrand%d" % k, pts, radii, "G_Hair" if k % 3 else "G_HairHot", res=5)
    strands.append(s)
# two front locks framing her face, over the shoulders
for side in (-1, 1):
    pts = [(side * 0.24, -0.02, 4.55), (side * 0.33, -0.12, 4.25), (side * 0.36, -0.2, 3.85), (side * 0.32, -0.26, 3.45)]
    strands.append(tube("HairLock%s" % ("L" if side > 0 else "R"), pts, [0.08, 0.08, 0.06, 0.004], "G_Hair", res=5))
# join them into one hair piece
bpy.ops.object.select_all(action='DESELECT')
for s in strands:
    s.select_set(True)
bpy.context.view_layer.objects.active = strands[0]
bpy.ops.object.join()
hair = bpy.context.view_layer.objects.active
hair.name = "Hair_nc"
noise_displace(hair, 0.02, seed=4, scale=7)
part(hair, "Head")
# hair flames at the crown (flicker as Flame*: HellVesperBody leaves them; the bot code is not hers)
for n, (x, y) in enumerate([(-0.12, 0.2), (0.12, 0.2), (0, 0.28)]):
    f = flame("FlameHair%d_nc" % n, (x, y, 4.6), 0.35, 0.09, "G_HairHot", lean=(x * 1.5, 0.6), seed=n + 20)
    part(f, "Head")

# ---- the crown: a gold band with ember-tipped points, and the Evening Star on her brow
band = prim("cyl", "Crown_nc", (0, 0.03, 4.58), (0.6, 0.62, 0.06), "M_Gold", segs=32)
part(band, "Head")
for n in range(7):
    a = math.radians(-90 + (n - 3) * 26)          # the front arc (-Y)
    base = Vector((math.cos(a) * 0.3, 0.03 + math.sin(a) * 0.31, 4.6))
    tip = base + Vector((math.cos(a) * 0.04, math.sin(a) * 0.04, 0.22 - abs(n - 3) * 0.035))
    sp = tube("CrownPoint%d_nc" % n, [tuple(base), tuple(tip)], [0.035, 0.004], "M_Gold")
    part(sp, "Head")
    gem = prim("sphere", "CrownGem%d_nc" % n, tuple(base + Vector((0, 0, 0.03))), (0.05, 0.05, 0.05), "G_Ember", segs=10, rings=6)
    part(gem, "Head")
# the Evening Star: an eight-point star, violet-white
bm = bmesh.new()
cverts = []
for i in range(16):
    a = i / 16 * math.tau
    r = 0.16 if i % 2 == 0 else 0.05
    if i % 4 == 0:
        r = 0.2
    cverts.append(bm.verts.new((math.cos(a) * r, 0, math.sin(a) * r)))
cf = bm.verts.new((0, -0.03, 0))
cb = bm.verts.new((0, 0.02, 0))
for i in range(16):
    bm.faces.new((cverts[i], cverts[(i + 1) % 16], cf))
    bm.faces.new((cverts[(i + 1) % 16], cverts[i], cb))
star = mesh_obj("EveningStar_nc", bm, ["G_Violet"], smooth=False)
star.location = (0, -0.3, 4.66)
part(star, "Head")

# earrings: gold drops with ember gems
for side in (-1, 1):
    e1 = tube("Earring%s_nc" % ("L" if side > 0 else "R"), [(side * 0.27, 0.0, 4.3), (side * 0.28, -0.01, 4.12)], [0.012, 0.012], "M_Gold")
    part(e1, "Head")
    g = prim("sphere", "EarGem%s_nc" % ("L" if side > 0 else "R"), (side * 0.28, -0.01, 4.08), (0.06, 0.06, 0.09), "G_Ember", segs=10, rings=6)
    part(g, "Head")

# ---- choker and a pendant; the high standing collar behind her neck (the stylish veneer)
chok = prim("cyl", "Choker_nc", (0, 0.13, 3.98), (0.34, 0.34, 0.07), "M_Gold", segs=24)
part(chok, "Neck")
pend = prim("sphere", "Pendant_nc", (0, -0.06, 3.86), (0.08, 0.05, 0.11), "G_Violet", segs=10, rings=6)
part(pend, "Neck")
bm = bmesh.new()
cols, rws = 14, 4
grid = []
for i in range(rws + 1):
    t = i / rws
    row = []
    for j in range(cols + 1):
        a = math.radians(-20 + 220 * j / cols)          # wraps round the back (+Y) from shoulder to shoulder
        r = 0.36 + 0.28 * t
        z = 3.72 + 0.78 * t
        row.append(bm.verts.new((math.cos(a) * r, 0.1 + math.sin(a) * r * 0.75, z + (0.12 * t if j in (0, cols) else 0) + (0.08 * t * (j % 2)))))
    grid.append(row)
for i in range(rws):
    for j in range(cols):
        bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
trim_pts = [tuple(grid[rws][j].co) for j in range(0, cols + 1, 2)]
collar = mesh_obj("Collar_nc", bm, ["M_Gown"])
solidify(collar, 0.03)
part(collar, "Torso")
trim = tube("CollarTrim_nc", trim_pts, [0.025] * (cols // 2 + 1), "M_Gold", res=4)
part(trim, "Torso")
# shoulder pieces: small gold plates with ember studs
for side in (-1, 1):
    sh = prim("sphere", "Pauldron%s_nc" % ("L" if side > 0 else "R"), (side * 0.52, 0.03, 3.68), (0.36, 0.34, 0.2), "M_Gold", segs=16, rings=8, rot=(0, side * 25, 0))
    part(sh, "Shoulder.L" if side > 0 else "Shoulder.R")

# cuffs on the forearms (T-pose: the arms run along X)
for side in (-1, 1):
    tag = "L" if side > 0 else "R"
    lo = bone_world("LowerArm." + tag)
    pa = bone_world("Palm." + tag)
    c = lo + (pa - lo) * 0.7
    cuff = prim("cyl", "Cuff%s_nc" % tag, tuple(c), (0.24, 0.24, 0.28), "M_Gold", segs=16, rot=(0, 90, 0))
    part(cuff, "LowerArm." + tag)

# a front marker for the prefab builder (her front is -Y here, +Z in Unity)
fm = bpy.data.objects.new("FrontMarker", None)
link(fm)
fm.location = (0, -0.6, 3.3)
attach(fm, "Torso")

uv_unwrap(parts + [body])
arm.data.pose_position = "POSE"
bpy.context.view_layer.update()

# ---- the actions
sc = bpy.context.scene
acts = {a.name: a for a in bpy.data.actions}
for n, a in list(acts.items()):
    bpy.data.actions.remove(a)
if arm.animation_data is None:
    arm.animation_data_create()
sc.frame_set(1)
bpy.context.view_layer.update()
AW = arm.matrix_world
AWi = AW.inverted()
pbs = arm.pose.bones


def rot(bname, axis, deg):
    pb = pbs[bname]
    ax = (AWi.to_3x3() @ Vector(axis)).normalized()
    p = pb.head.copy()
    R = Matrix.Translation(p) @ Quaternion(ax, math.radians(deg)).to_matrix().to_4x4() @ Matrix.Translation(-p)
    pb.matrix = R @ pb.matrix
    bpy.context.view_layer.update()


def move(bname, world_delta):
    pb = pbs[bname]
    d = AWi.to_3x3() @ Vector(world_delta)
    m = pb.matrix.copy()
    m.translation += d
    pb.matrix = m
    bpy.context.view_layer.update()


BASE = [("r", "UpperArm.L", (0, 1, 0), 70), ("r", "UpperArm.R", (0, 1, 0), -70), ("r", "LowerArm.L", (1, 0, 0), -12), ("r", "LowerArm.R", (1, 0, 0), -12),
        ("r", "Head", (1, 0, 0), -6)]


def apply(steps):
    for kind, b, *rest in steps:
        if kind == "r":
            rot(b, rest[0], rest[1])
        else:
            move(b, rest[0])


def key_all(f):
    for pb in pbs:
        pb.keyframe_insert("rotation_quaternion", frame=f)
        pb.keyframe_insert("location", frame=f)
        pb.keyframe_insert("scale", frame=f)


def start(name):
    act = bpy.data.actions.new(name)
    arm.animation_data.action = act
    for pb in pbs:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    act.use_fake_user = True
    return act


def make_pose(name, steps, frames=(1, 24)):
    """A held pose from her standing base (arms down from the T-pose rest), keyed on every bone at each frame."""
    act = start(name)
    apply(BASE + steps)
    for f in frames:
        key_all(f)
    return act


X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
# Idle: chin up, a slow breath, the hands drifting (a 60-frame loop)
idle = start("Idle")
apply(BASE + [("r", "Torso", X, -3)])
key_all(1); key_all(61)
apply([("r", "Torso", X, -2.5), ("r", "UpperArm.L", Y, -4), ("r", "UpperArm.R", Y, 4), ("r", "Head", X, -2)])
key_all(31)
cast = make_pose("Cast", [("r", "Torso", X, -8), ("r", "UpperArm.R", X, -100), ("r", "UpperArm.R", Z, 10), ("r", "LowerArm.R", X, 10),
                          ("r", "UpperArm.L", Y, -20), ("r", "Head", X, -4)])
blink = make_pose("Blink", [("m", "Body", (0, 0, -0.35)), ("r", "Torso", X, 28), ("r", "UpperArm.L", X, 55), ("r", "UpperArm.R", X, 55),
                            ("r", "UpperArm.L", Y, -15), ("r", "UpperArm.R", Y, 15), ("r", "Head", X, -22),
                            ("r", "UpperLeg.L", X, -35), ("r", "UpperLeg.R", X, 25)], frames=(1, 14))
reel = make_pose("Reel", [("m", "Body", (0, 0.05, -0.1)), ("r", "Torso", X, -18), ("r", "Torso", Y, 8), ("r", "Head", X, -22),
                          ("r", "UpperArm.L", Y, -40), ("r", "UpperArm.R", Y, 40), ("r", "UpperArm.L", X, 20), ("r", "UpperArm.R", X, 25)])
kneel = make_pose("Kneel", [("m", "Body", (0, 0.1, -0.95)), ("r", "UpperLeg.L", X, -50), ("r", "LowerLeg.L", X, 60),
                            ("r", "UpperLeg.R", X, 8), ("r", "LowerLeg.R", X, 55),
                            ("r", "Torso", X, 26), ("r", "Head", X, 24),
                            ("r", "UpperArm.L", X, -35), ("r", "UpperArm.R", X, -20), ("r", "UpperArm.R", Y, 12)])
idle.use_fake_user = True
arm.animation_data.action = idle

# ---- size: about 2.25 m tall standing (the crown points and the fire tips a little more)
rootnode.scale = rootnode.scale * (2.25 / H)
bpy.context.view_layer.update()

export_fbx(OUT_FBX, [rootnode, arm, body] + parts + [fm], armature=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__), "vesper.blend"))
arm.animation_data.action = bpy.data.actions[PREVIEW]
sc.frame_set(1)
render_turntable(OUT_PNG, 2.3, 1.2, yaw_deg=-24, dist_k=2.3)
print("VESPER_OK", len(parts), "parts; actions", [a.name for a in bpy.data.actions])
