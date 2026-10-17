# The scythe demon: a gliding robed reaper, about 2.8 m to its horn tips. Blender: front -Y, its right side -X, feet at the origin.
# Run: blender -b --factory-startup --python scythe_demon.py -- <out.fbx> <preview.png>
import bpy, bmesh, math, random, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector, Matrix
from hd_common import reset, mat, empty, mesh_obj, prim, noise_displace, uv_unwrap, import_glb, export_fbx, render_turntable
from hd_parts import place, tube, flame, solidify, subsurf, finalize, parent_keep

DL = r"C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\hell-pass\art\downloads\polypizza-models"
args = sys.argv[sys.argv.index("--") + 1:]
OUT_FBX, OUT_PNG = args[0], args[1]
reset()
random.seed(7)
root = empty("scythe-demon", (0, 0, 0))
parts = []


def keep(o, pivot=None, parent=root):
    bpy.context.view_layer.update()
    if pivot is None:
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        pivot = sum(bb, Vector()) / 8
    place(o, pivot, parent)
    parts.append(o)
    return o


# ---- the robe: a flared, folded cone from the waist to a tattered, ember-lit hem that floats off the floor
bm = bmesh.new()
segs, rings = 40, 14
z_top, z_bot = 1.42, 0.12
rows = []
for i in range(rings + 1):
    t = i / rings
    z = z_top + (z_bot - z_top) * t
    rx = 0.36 + 0.38 * t ** 1.3
    ry = 0.27 + 0.36 * t ** 1.3
    row = []
    for j in range(segs):
        a = j / segs * math.tau
        fold = 1 + (0.02 + 0.07 * t) * math.sin(a * 7 + 0.6) + 0.03 * t * math.sin(a * 13)
        zz = z
        if i == rings:
            # tatters: every other point hangs lower, jittered
            zz = z - (0.16 if j % 2 == 0 else 0.0) - random.uniform(0, 0.1)
        # a sweep back: the hem trails behind as it glides
        yy = math.sin(a) * ry * fold + 0.16 * t * t
        row.append(bm.verts.new((math.cos(a) * rx * fold, yy, zz)))
    rows.append(row)
for i in range(rings):
    for j in range(segs):
        bm.faces.new((rows[i][j], rows[i][(j + 1) % segs], rows[i + 1][(j + 1) % segs], rows[i + 1][j]))
robe = mesh_obj("Robe", bm, ["M_Char", "G_Ember"])
# the hem's last ring of faces glows like smouldering cloth
for p in robe.data.polygons:
    if p.center.z < z_bot + 0.13:
        p.material_index = 1
solidify(robe, 0.03)
keep(robe)

# ---- belt: a rust band with a bone clasp
belt = prim("cyl", "Belt_nc", (0, 0.0, 1.42), (0.78, 0.6, 0.12), "M_Rust", segs=32)
keep(belt)
clasp = prim("sphere", "Clasp_nc", (0, -0.31, 1.42), (0.16, 0.08, 0.14), "M_Bone")
keep(clasp)

# ---- torso: a gaunt charred chest, broad at the shoulders
torso = prim("sphere", "Torso", (0, 0.02, 1.76), (0.92, 0.5, 0.82), "M_Char", segs=32, rings=16)
finalize(torso)
for v in torso.data.vertices:
    if v.co.z > 1.8:
        v.co.x *= 1 + (v.co.z - 1.8) * 0.9   # shoulders broaden
    if v.co.y < 0 and abs(v.co.x) < 0.2 and abs(v.co.z - 1.72) < 0.3:
        v.co.y += 0.08                         # the split: the chest parts round the core
noise_displace(torso, 0.025, seed=3, scale=6)
keep(torso)

# ---- the core (the weak point) bulging out of the split, framed by bone ribs
core = prim("sphere", "WeakPoint", (0, -0.27, 1.70), (0.5, 0.5, 0.5), "G_Core", segs=24, rings=12)
core.scale = (0.5, 0.5, 0.5)
keep(core, pivot=(0, -0.27, 1.70))
for side in (-1, 1):
    for k, (z, r) in enumerate([(1.92, 0.3), (1.78, 0.34), (1.62, 0.33), (1.48, 0.27)]):
        pts = [(side * 0.2, -0.36, z + 0.03), (side * (r + 0.02), -0.27, z), (side * (r + 0.1), -0.06, z - 0.05), (side * (r + 0.06), 0.12, z - 0.08)]
        rib = tube("Rib%s%d_nc" % ("L" if side > 0 else "R", k), pts, [0.022, 0.03, 0.03, 0.02], "M_Bone")
        keep(rib)

# ---- the head: a horned skull (KayKit Skull, CC0) in a ragged hood, eyes burning
before = set(bpy.data.objects)
skull_objs = import_glb(os.path.join(DL, "Skull__KayLousberg__7clFQGz5jH.glb"))
skull = [o for o in skull_objs if o.type == 'MESH'][0]
skull.parent = None
for o in skull_objs:
    if o is not skull:
        bpy.data.objects.remove(o)
finalize(skull)
skull.name = "Head"
bb = [v.co.copy() for v in skull.data.vertices]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
k = 0.52 / (mx.z - mn.z)
ctr = (mn + mx) / 2
skull.data.transform(Matrix.Translation(-ctr))
skull.data.transform(Matrix.Diagonal((k, k * 1.05, k * 1.1, 1)))
skull.data.transform(Matrix.Translation((0, -0.12, 2.34)))
skull.data.materials.clear()
skull.data.materials.append(mat("M_Bone"))
SKULL_FRONT = -0.12 - (mx.y - mn.y) * k * 1.05 / 2
keep(skull)
for side in (-1, 1):
    e = prim("sphere", "Eye%s_nc" % ("L" if side > 0 else "R"), (side * 0.088, SKULL_FRONT + 0.09, 2.31), (0.11, 0.07, 0.085), "G_Eye", rot=(0, side * 15, 0))
    keep(e)
# horns: thick bone, sweeping out, back, then up to 2.8 m
for side in (-1, 1):
    pts = [(side * 0.11, -0.02, 2.46), (side * 0.27, 0.06, 2.56), (side * 0.38, 0.17, 2.66), (side * 0.36, 0.22, 2.8)]
    h = tube("Horn%s" % ("L" if side > 0 else "R"), pts, [0.075, 0.06, 0.04, 0.008], "M_Bone", res=6)
    keep(h)
# hood: a ragged cowl over the skull, open at the face, falling to the shoulders
hood = prim("sphere", "Hood_nc", (0, 0.1, 2.32), (0.7, 0.66, 0.8), "M_Char", segs=32, rings=16)
finalize(hood)
bmh = bmesh.new(); bmh.from_mesh(hood.data)
kill = [f for f in bmh.faces if (f.calc_center_median().y < 0.0 and f.calc_center_median().z < 2.55) or f.calc_center_median().z < 2.02]
bmesh.ops.delete(bmh, geom=kill, context='FACES')
for v in bmh.verts:
    if v.co.z < 2.12:
        v.co.z -= random.uniform(0.0, 0.12)   # ragged edge
bmh.to_mesh(hood.data); bmh.free()
noise_displace(hood, 0.02, seed=5, scale=8)
solidify(hood, 0.025)
keep(hood)
head_flame = flame("FlameHead", (0, 0.12, 2.56), 0.42, 0.1, "G_FlameDeep", lean=(0, 0.5), seed=2)
keep(head_flame, pivot=(0, 0.12, 2.56))

# ---- pauldrons: rust and bone shells (the two plates, shed when broken), each burning
for side, pname in ((1, "Plate_pauldron-left"), (-1, "Plate_pauldron-right")):
    pe = empty(pname, (side * 0.6, 0.02, 2.02), root)
    tag = "L" if side > 0 else "R"
    shell = prim("sphere", "Pauldron%s_Shell" % tag, (side * 0.6, 0.02, 1.98), (0.5, 0.56, 0.5), "M_Rust", segs=24, rings=12, rot=(0, side * 22, 0))
    finalize(shell)
    bms = bmesh.new(); bms.from_mesh(shell.data)
    bmesh.ops.delete(bms, geom=[f for f in bms.faces if f.calc_center_median().z < 1.86 - side * 0.0], context='FACES')
    bms.to_mesh(shell.data); bms.free()
    solidify(shell, 0.05)
    keep(shell, parent=pe)
    rim = prim("cyl", "Pauldron%s_Rim_nc" % tag, (side * 0.66, 0.02, 1.88), (0.5, 0.56, 0.04), "M_Bone", segs=24)
    keep(rim, parent=pe)
    for n, (dx, dy, ang) in enumerate([(0.12, -0.1, 25), (0.18, 0.08, 35), (0.02, 0.12, 15)]):
        sp = tube("Pauldron%s_Spike%d" % (tag, n), [(side * (0.62 + dx * 0.5), dy, 2.12), (side * (0.62 + dx), dy, 2.28), (side * (0.66 + dx * 1.6), dy + 0.03, 2.42 - n * 0.04)],
                  [0.05, 0.03, 0.004], "M_Bone")
        keep(sp, parent=pe)
    fl = flame("Flame%s" % tag, (side * 0.6, 0.1, 2.12), 0.4, 0.11, "G_Flame", lean=(side * 0.1, 0.4), seed=side + 4)
    keep(fl, pivot=(side * 0.6, 0.1, 2.12), parent=pe)

# ---- arms: long and gaunt, clawed; the right one (ArmR, pivot at the shoulder) holds the scythe
def arm(side, parent, hand, elbow, shoulder):
    tag = "L" if side > 0 else "R"
    up = tube("Arm%s_Upper" % tag, [shoulder, elbow], [0.11, 0.08], "M_Char", res=4)
    keep(up, parent=parent)
    fo = tube("Arm%s_Fore" % tag, [elbow, hand], [0.085, 0.055], "M_Char", res=4)
    keep(fo, parent=parent)
    spur = tube("Arm%s_Spur_nc" % tag, [elbow, (elbow[0] + side * 0.02, elbow[1] + 0.12, elbow[2] - 0.02)], [0.04, 0.003], "M_Bone")
    keep(spur, parent=parent)
    for f in range(4):
        a = (f - 1.5) * 0.35
        base = Vector(hand)
        tip = base + Vector((side * 0.03 + math.sin(a) * 0.05, -0.08 + math.cos(a) * -0.05, -0.2))
        mid = base + Vector((math.sin(a) * 0.06, -0.12, -0.1))
        c = tube("Arm%s_Claw%d_nc" % (tag, f), [tuple(base), tuple(mid), tuple(tip)], [0.03, 0.022, 0.003], "M_Bone")
        keep(c, parent=parent)


armL = empty("ArmL", (0.66, 0.0, 1.95), root)
arm(1, armL, (0.8, -0.1, 1.02), (0.78, 0.06, 1.45), (0.66, 0.0, 1.95))
armR = empty("ArmR", (-0.66, 0.0, 1.95), root)
arm(-1, armR, (-0.74, -0.32, 1.12), (-0.8, -0.05, 1.5), (-0.66, 0.0, 1.95))

# the scythe (Quaternius "Scythe", CC0): haft through the right hand, the blade high and curving forward
sc_objs = import_glb(os.path.join(DL, "Scythe__Quaternius__yE9TKCewO8.glb"))
scy = [o for o in sc_objs if o.type == 'MESH'][0]
scy.parent = None
for o in sc_objs:
    if o is not scy:
        bpy.data.objects.remove(o)
finalize(scy)
scy.name = "Scythe_nc"
# source: haft along +Z (-2.22..3.37), blade out along +X. Scale to ~2.5 m, blade forward (-Y) and a little inward.
s = 0.45
scy.data.transform(Matrix.Diagonal((s, s, s, 1)))
scy.data.transform(Matrix.Rotation(math.radians(-125), 4, "Z"))
scy.data.transform(Matrix.Rotation(math.radians(8), 4, 'X'))
scy.data.transform(Matrix.Translation((-0.74, -0.32, 1.12 + 0.25)))
remap = {"DarkWood": "M_Char", "LightWood": "M_Bone", "Steel": "M_Steel", "LightSteel": "G_Edge"}
for i, m in enumerate(scy.data.materials):
    nm = m.name.split(".")[0]
    scy.data.materials[i] = mat(remap.get(nm, "M_Steel"))
keep(scy, parent=armR)

# ---- front cloth flaps (LegL/LegR, pivots at the belt): they flutter as it glides in
for side, nm in ((1, "LegL"), (-1, "LegR")):
    le = empty(nm, (side * 0.15, -0.3, 1.38), root)
    bmf = bmesh.new()
    cols, rws = 4, 8
    grid = []
    for i in range(rws + 1):
        t = i / rws
        row = []
        for j in range(cols + 1):
            u = j / cols
            x = side * (0.03 + u * 0.26 + t * 0.06)
            y = -0.31 - t * 0.33 - 0.03 * math.sin(u * math.pi)
            z = 1.38 - t * 1.2
            if i == rws:
                z -= (0.14 if j % 2 == 0 else 0.0) + random.uniform(0, 0.06)
            row.append(bmf.verts.new((x, y, z)))
        grid.append(row)
    for i in range(rws):
        for j in range(cols):
            bmf.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
    flap = mesh_obj("Flap%s_nc" % nm[-1], bmf, ["M_Char"])
    noise_displace(flap, 0.02, seed=side + 9, scale=5)
    solidify(flap, 0.025)
    keep(flap, parent=le)

# smouldering embers rising off the hem's back (flicker as Flame*)
for n in range(3):
    a = math.radians(140 + n * 40)
    fb = (math.cos(a) * 0.55, 0.35 + math.sin(a) * 0.1, 0.18)
    f = flame("FlameHem%d" % n, fb, 0.32, 0.07, "G_Flame", lean=(0, 0.3), seed=11 + n)
    keep(f, pivot=fb)

uv_unwrap(parts)
export_fbx(OUT_FBX, [root] + [o for o in bpy.data.objects if o.parent is not None or o is root])
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__), os.path.splitext(os.path.basename(OUT_FBX))[0] + ".blend"))
if len(args) > 2:
    render_turntable(OUT_PNG, 2.8, 2.3, yaw_deg=-20, dist_k=0.5)
else:
    render_turntable(OUT_PNG, 2.8, 1.4, yaw_deg=-30, dist_k=2.4)
print("DEMON_OK", len(parts), "parts")
