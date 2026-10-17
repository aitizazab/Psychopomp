# Vesper, Lucifer's bride, the Evening Star. Round 2 (2026-10-18): a trophy wife neglected for centuries: withdrawn, proud but
# hollow. A concealing high-collared robe with embroidered stars, gold clasps and chains and a violet brooch; molten hair
# hanging like a curtain round her head past the collar, only her eyes showing. (Round 1: vesper_r1.py in the art folder.)
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
remap = {"Dress": "M_Robe", "Skin": "M_Ash", "Shoes": "M_Gold", "Hair": "M_Hair", "Eyes": "G_Eye"}
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

# ---- round 2 (the creator's brief, 2026-10-18): a trophy wife neglected for centuries; withdrawn, proud but hollow.
# A concealing high-collared robe, stylish in its details (embroidered stars, gold clasps and chains, a violet brooch), not
# its shape; long molten hair hanging round her head like a curtain past the collar, only her eyes showing.


def star_mesh(name, centre, normal, size, material, points=4):
    """A small flat star (embroidery), facing along normal."""
    bm = bmesh.new()
    ring = []
    for i in range(points * 2):
        a = i / (points * 2) * math.tau
        r = size if i % 2 == 0 else size * 0.35
        ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0)))
    c = bm.verts.new((0, 0, size * 0.15))
    for i in range(len(ring)):
        bm.faces.new((ring[i], ring[(i + 1) % len(ring)], c))
    o = mesh_obj(name, bm, [material], smooth=False)
    o.rotation_euler = Vector(normal).to_track_quat('Z', 'Y').to_euler()
    o.location = centre
    return o


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for s in objs:
        s.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    return o


# ---- the robe's skirt: floor-length, falling straight with heavy folds; a gold hem band, stars scattered on it
bm = bmesh.new()
segs, rings = 36, 12
z_top, z_bot = 2.7, 0.02
rows = []
for i in range(rings + 1):
    t = i / rings
    z = z_top + (z_bot - z_top) * t
    r = 0.5 + 0.38 * t ** 1.4
    row = []
    for j in range(segs):
        a = j / segs * math.tau
        fold = 1 + (0.02 + 0.05 * t) * math.sin(a * 8)
        row.append(bm.verts.new((math.cos(a) * r * fold, math.sin(a) * r * 0.9 * fold, z)))
    rows.append(row)
for i in range(rings):
    for j in range(segs):
        bm.faces.new((rows[i][j], rows[i][(j + 1) % segs], rows[i + 1][(j + 1) % segs], rows[i + 1][j]))
gown = mesh_obj("Gown_nc", bm, ["M_Robe", "M_Gold"])
for p in gown.data.polygons:
    if p.center.z < z_bot + 0.16:
        p.material_index = 1
solidify(gown, 0.04)
part(gown, "Hips")
stars = []
for k in range(26):
    a = random.uniform(0, math.tau)
    t = random.uniform(0.1, 0.92)
    z = z_top + (z_bot - z_top) * t
    r = (0.5 + 0.38 * t ** 1.4) + 0.03
    p = Vector((math.cos(a) * r, math.sin(a) * r * 0.9, z))
    stars.append(star_mesh("s%d" % k, p, Vector((math.cos(a), math.sin(a) * 0.9, 0)), random.uniform(0.035, 0.06), "M_Gold" if k % 3 else "G_Ember"))
part(join(stars, "SkirtStars_nc"), "Hips")

# ---- the bodice: a closed robe front from the waist to the throat, over her dress
bm = bmesh.new()
segs, rings = 28, 8
rows = []
for i in range(rings + 1):
    t = i / rings
    z = 2.65 + t * 1.3                       # waist .. base of the neck
    rx = 0.42 + 0.14 * math.sin(t * math.pi * 0.9) - (0.18 * (t - 0.85) / 0.15 if t > 0.85 else 0)
    ry = 0.3 + 0.04 * math.sin(t * math.pi)
    row = []
    for j in range(segs):
        a = j / segs * math.tau
        row.append(bm.verts.new((math.cos(a) * rx, 0.02 + math.sin(a) * ry, z)))
    rows.append(row)
for i in range(rings):
    for j in range(segs):
        bm.faces.new((rows[i][j], rows[i][(j + 1) % segs], rows[i + 1][(j + 1) % segs], rows[i + 1][j]))
bod = mesh_obj("Bodice_nc", bm, ["M_Robe"])
solidify(bod, 0.03)
part(bod, "Torso")
belt = prim("cyl", "Belt_nc", (0, 0.0, 2.68), (0.98, 0.78, 0.08), "M_Gold", segs=32)
part(belt, "Hips")

# ---- capelets over the shoulders, embroidered
for side in (-1, 1):
    tag = "L" if side > 0 else "R"
    cap = prim("sphere", "Capelet%s_nc" % tag, (side * 0.46, 0.03, 3.62), (0.62, 0.66, 0.42), "M_Robe", segs=20, rings=10, rot=(0, side * 28, 0))
    finalize(cap)
    bmc = bmesh.new(); bmc.from_mesh(cap.data)
    bmesh.ops.delete(bmc, geom=[f for f in bmc.faces if f.calc_center_median().z < 3.5], context='FACES')
    bmc.to_mesh(cap.data); bmc.free()
    solidify(cap, 0.03)
    part(cap, "Shoulder." + tag)
    st = [star_mesh("cs%d" % n, (side * (0.42 + 0.1 * n), -0.24 + 0.05 * n, 3.72 - 0.05 * n), (side * 0.3, -1, 0.4), 0.04, "M_Gold") for n in range(3)]
    part(join(st, "CapeletStars%s_nc" % tag), "Shoulder." + tag)

# ---- sleeves: long, concealing, flaring at the wrist with embroidered stars (T-pose: the arms run along X)
for side in (-1, 1):
    tag = "L" if side > 0 else "R"
    up = bone_world("UpperArm." + tag)
    lo = bone_world("LowerArm." + tag)
    pa = bone_world("Palm." + tag)
    s1 = tube("SleeveUpper%s_nc" % tag, [tuple(up + Vector((-side * 0.05, 0, 0))), tuple(lo)], [0.19, 0.17], "M_Robe", res=4, bevel_res=4)
    part(s1, "UpperArm." + tag)
    s2 = tube("SleeveLower%s_nc" % tag, [tuple(lo - Vector((side * 0.04, 0, 0))), tuple(lo + (pa - lo) * 0.7), tuple(pa + Vector((-side * 0.02, 0, 0)))], [0.17, 0.19, 0.25], "M_Robe", res=4, bevel_res=4)
    part(s2, "LowerArm." + tag)
    cuff = prim("cyl", "Cuff%s_nc" % tag, tuple(lo + (pa - lo) * 0.93), (0.46, 0.46, 0.05), "M_Gold", segs=16, rot=(0, 90, 0))
    part(cuff, "LowerArm." + tag)
    st = []
    for n in range(5):
        u = 0.25 + 0.15 * n
        p = lo + (pa - lo) * u
        ang = n * 1.3
        nrm = Vector((0, math.sin(ang), math.cos(ang)))
        st.append(star_mesh("ss%d" % n, p + nrm * (0.14 + 0.05 * u), nrm, 0.035, "M_Gold"))
    part(join(st, "SleeveStars%s_nc" % tag), "LowerArm." + tag)

# ---- the high collar: tall, flared, pointed at the tips, its lining embroidered with stars
bm = bmesh.new()
cols, rws = 16, 5
grid = []
for i in range(rws + 1):
    t = i / rws
    row = []
    for j in range(cols + 1):
        u = j / cols
        a = math.radians(-35 + 250 * u)               # from her front-left round the back to her front-right
        r = 0.32 + 0.45 * t
        z = 3.88 + 0.78 * t
        edge = min(u, 1 - u)
        if edge < 0.1:
            z += 0.08 * t * (1 - edge / 0.1)           # the two points rise at the front
        row.append(bm.verts.new((math.cos(a) * r * (1 if u > 0.5 else 1), 0.06 + math.sin(a) * r * 0.82, z)))
    grid.append(row)
for i in range(rws):
    for j in range(cols):
        bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
trim_pts = [tuple(grid[rws][j].co) for j in range(0, cols + 1, 2)]
lining_pts = [(grid[i][j].co.copy(), i, j) for i in range(1, rws) for j in range(1, cols, 2)]
collar = mesh_obj("Collar_nc", bm, ["M_Robe"])
solidify(collar, 0.035)
part(collar, "Torso")
trim = tube("CollarTrim_nc", trim_pts, [0.022] * len(trim_pts), "M_Gold", res=4)
part(trim, "Torso")
st = []
for k, (p, i, j) in enumerate(lining_pts):
    if (i + j) % 2:
        continue
    inward = Vector((-p.x, 0.06 - p.y, 0)).normalized()
    st.append(star_mesh("ls%d" % k, p + inward * 0.03, inward, 0.04, "G_Ember" if k % 4 == 0 else "M_Gold"))
part(join(st, "CollarStars_nc"), "Torso")

# ---- gold clasps at the collar's front corners, chains draped between them, the violet brooch at her throat
clasps = []
for side in (-1, 1):
    c = Vector((side * 0.3, -0.24, 3.78))
    d = prim("cyl", "Clasp%s_nc" % ("L" if side > 0 else "R"), tuple(c), (0.16, 0.16, 0.04), "M_Gold", segs=20, rot=(90, 0, 0))
    part(d, "Torso")
    g = prim("sphere", "ClaspGem%s_nc" % ("L" if side > 0 else "R"), tuple(c + Vector((0, -0.025, 0))), (0.07, 0.04, 0.07), "G_Ember", segs=10, rings=6)
    part(g, "Torso")
    clasps.append(c)
chains = []
for k, sag in enumerate((0.12, 0.26, 0.42)):
    a, b = clasps[0], clasps[1]
    pts = []
    for n in range(7):
        u = n / 6
        p = a + (b - a) * u
        p = p + Vector((0, -0.05 - 0.06 * math.sin(u * math.pi) * (1 + k * 0.4), -sag * math.sin(u * math.pi)))
        pts.append(tuple(p))
    chains.append(tube("chain%d" % k, pts, [0.016] * 7, "M_Gold", res=6, bevel_res=2))
part(join(chains, "Chains_nc"), "Torso")
bro = prim("sphere", "Brooch_nc", (0, -0.33, 3.72), (0.15, 0.07, 0.19), "G_Violet", segs=12, rings=8)
part(bro, "Neck")
bset = prim("cyl", "BroochSetting_nc", (0, -0.31, 3.72), (0.21, 0.21, 0.03), "M_Gold", segs=16, rot=(90, 0, 0))
part(bset, "Neck")

# ---- the hair: a curtain of molten hair round her head, falling past the collar; only her eyes show through it
bm = bmesh.new()
segs = 36
prof = []                                       # (z, rx, ry): crown .. tips
for i in range(16):
    t = i / 15
    if t < 0.3:                                 # the dome over the crown
        k = t / 0.3
        ang = (1 - k) * math.pi / 2
        z = 4.38 + 0.32 * math.sin(ang) + 0.04
        rr = 0.33 * math.cos(ang) + 0.02
    else:
        k = (t - 0.3) / 0.7
        z = 4.42 - 1.05 * k                     # down to about 3.37: past the collar's base
        rr = 0.35 + 0.08 * k
    prof.append((z, rr * 1.0, rr * 0.98))
rows = []
for i, (z, rx, ry) in enumerate(prof):
    row = []
    for j in range(segs):
        a = j / segs * math.tau - math.pi / 2   # j = 0 is her front (-Y)
        zz = z
        if i == len(prof) - 1:
            zz += (0.1 if j % 2 else -0.06) + random.uniform(-0.05, 0.05)   # ragged ends
        wav = 1 + 0.025 * math.sin(j * 2.3 + i)
        row.append(bm.verts.new((math.cos(a) * rx * wav, 0.04 + math.sin(a) * ry * wav, zz)))
    rows.append(row)
for i in range(len(rows) - 1):
    for j in range(segs):
        v = [rows[i][j], rows[i][(j + 1) % segs], rows[i + 1][(j + 1) % segs], rows[i + 1][j]]
        c = sum((x.co for x in v), Vector()) / 4
        front = c.y < -0.1
        # the eye window, and a thin parting below it
        if front and 4.24 < c.z < 4.37 and abs(c.x) < 0.2:
            continue
        if front and 4.02 < c.z <= 4.24 and abs(c.x) < 0.05:
            continue
        bm.faces.new(v)
hair = mesh_obj("Hair_nc", bm, ["M_Hair", "G_Hair"])
for p in hair.data.polygons:
    if p.center.z < 3.95 - 0.12 * math.sin(p.center.x * 9 + p.center.y * 5) and (p.center.y > -0.12 or abs(p.center.x) > 0.2):
        p.material_index = 1                    # below her jaw the hair burns: a molten curtain past the collar
noise_displace(hair, 0.02, seed=4, scale=9)
solidify(hair, 0.04)
part(hair, "Head")
# a few loose molten strands over the curtain
strands = []
for k in range(7):
    a = -math.pi / 2 + (1.5 + k * 0.55) * (1 if k % 2 else -1)
    x, y = math.cos(a) * 0.37, 0.04 + math.sin(a) * 0.37
    pts = [(x, y, 4.45), (x * 1.08, y * 1.08, 4.0), (x * 1.15, y * 1.12, 3.55), (x * 1.2, y * 1.15, 3.28)]
    strands.append(tube("hs%d" % k, pts, [0.03, 0.035, 0.03, 0.004], "G_Hair", res=4))
part(join(strands, "HairStrands_nc"), "Head")

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


# withdrawn: shoulders in, chin down, arms close with the forearms drawn in front of her
BASE = [("r", "Torso", (1, 0, 0), 5), ("r", "UpperArm.L", (0, 1, 0), 78), ("r", "UpperArm.R", (0, 1, 0), -78),
        ("r", "LowerArm.L", (1, 0, 0), -40), ("r", "LowerArm.R", (1, 0, 0), -40), ("r", "LowerArm.L", (0, 0, 1), -25), ("r", "LowerArm.R", (0, 0, 1), 25),
        ("r", "Head", (1, 0, 0), 10)]


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
apply(BASE)
key_all(1); key_all(61)
apply([("r", "Torso", X, 2), ("r", "Head", X, 3), ("r", "Head", Z, 4)])
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
