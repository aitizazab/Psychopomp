# The lava ooze: a molten blob about 1.35 m across and 1 m high, with cooled-crust plates, hollow eyes and a grin, and its
# white-hot core bulging out of the top. Blender: front -Y, feet at the origin.
# The Blob is unit-height and centred on its own origin (the bot code throbs it and sets its height from its scale).
# Run: blender -b --factory-startup --python lava_ooze.py -- <out.fbx> <preview.png>
import bpy, bmesh, math, random, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector, Matrix, noise
from hd_common import reset, mat, empty, mesh_obj, prim, noise_displace, uv_unwrap, export_fbx, render_turntable
from hd_parts import place, flame, finalize, parent_keep, set_origin

args = sys.argv[sys.argv.index("--") + 1:]
OUT_FBX, OUT_PNG = args[0], args[1]
reset()
random.seed(3)
root = empty("lava-ooze", (0, 0, 0))
parts = []
W = 1.35   # width over height


def keep(o, pivot=None, parent=root):
    bpy.context.view_layer.update()
    if pivot is None:
        vs = [o.matrix_world @ v.co for v in o.data.vertices]
        pivot = sum(vs, Vector()) / max(1, len(vs))
    place(o, pivot, parent)
    parts.append(o)
    return o


# ---- the blob: a slumped molten dome, its base spreading like a puddle, lumpy, with drips running down
bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=40, v_segments=24, radius=0.5)
for v in bm.verts:
    x, y, z = v.co
    t = (z + 0.5)                       # 0 bottom .. 1 top
    # slump: wide low body, the base spreading out
    spread = 1.0 + 0.32 * max(0.0, 0.42 - t) / 0.42
    v.co.x, v.co.y = x * W * spread, y * W * spread
    if t < 0.08:
        v.co.z = -0.5 + t * 0.4         # a flat sole
    # lumps
    n = noise.noise(Vector((x * 3.1, y * 3.1, z * 3.1)) + Vector((4.2, 1.3, 7.7)))
    v.co += v.normal * n * 0.06
    # drips: vertical runnels on the sides
    a = math.atan2(y, x)
    if 0.12 < t < 0.75:
        d = max(0.0, math.sin(a * 5 + 0.7)) ** 6
        v.co.x += math.cos(a) * d * 0.05
        v.co.y += math.sin(a) * d * 0.05
# centre it on its own origin, unit height
zs = [v.co.z for v in bm.verts]
lo, hi = min(zs), max(zs)
for v in bm.verts:
    v.co.z = (v.co.z - lo) / (hi - lo) - 0.5
blob = mesh_obj("Blob", bm, ["G_Lava"])
blob.location = (0, 0, 0.5)
keep(blob, pivot=(0, 0, 0.5))


def surface(dirv):
    """A point on the blob's surface (world) along a direction from its centre, by ray against the mesh."""
    bpy.context.view_layer.update()
    o = blob
    origin = Vector((0, 0, 0.5))
    d = Vector(dirv).normalized()
    ok, loc, nrm, idx = o.ray_cast(o.matrix_world.inverted() @ (origin + d * 2.0), -(o.matrix_world.inverted().to_3x3() @ d))
    if ok:
        return o.matrix_world @ loc, (o.matrix_world.to_3x3() @ nrm).normalized()
    return origin + d * 0.5, d


# ---- the crust: cooled black plates on its back and sides (the plate "crust", shed when broken), riding the throb
crust = empty("Plate_crust", (0, 0, 0.5), blob)
crust.location = (0, 0, 0)
dirs = [(0, 1, 0.5), (-0.8, 0.6, 0.35), (0.8, 0.6, 0.35), (-1, -0.1, 0.15), (1, -0.1, 0.15), (0, 0.6, 1.0), (-0.5, 0.95, -0.1), (0.5, 0.95, -0.1),
        (-0.7, 0.2, 0.7), (0.7, 0.2, 0.7), (-0.75, -0.45, 0.75), (0.75, -0.45, 0.75), (-0.95, -0.6, -0.05), (0.95, -0.6, -0.05), (-0.35, 0.3, 1.0), (0.4, 0.35, 1.0)]
for i, dv in enumerate(dirs):
    p, n = surface(dv)
    bmr = bmesh.new()
    bmesh.ops.create_icosphere(bmr, subdivisions=1, radius=0.5)
    rock = mesh_obj("Crust%d" % i, bmr, ["M_Crust"], smooth=False)
    s = random.uniform(0.3, 0.42)
    rock.scale = (s, s * random.uniform(0.7, 1.0), 0.09)
    rock.rotation_euler = n.to_track_quat('Z', 'Y').to_euler()
    rock.rotation_euler.rotate_axis('Z', random.uniform(0, 6.28))
    rock.location = p + n * 0.015
    bpy.context.view_layer.update()
    finalize(rock)
    noise_displace(rock, 0.03, seed=i, scale=9)
    keep(rock, parent=crust)

# ---- the face: two slanted hollows with embers deep inside, and a ragged grin
for side in (-1, 1):
    tag = "L" if side > 0 else "R"
    p, n = surface((side * 0.32, -1, 0.42))
    eye = prim("sphere", "Eye%s_nc" % tag, p, (0.2, 0.09, 0.12), "M_Hollow", rot=(0, side * -26, 0))
    keep(eye, parent=blob)
    em = prim("sphere", "Pupil%s_nc" % tag, p + n * 0.05 + Vector((side * -0.02, 0, -0.01)), (0.07, 0.04, 0.05), "G_Eye")
    keep(em, parent=blob)
bmm = bmesh.new()
pts = []
for i in range(9):
    u = i / 8
    x = (u - 0.5) * 0.62
    pts.append((x, 0.04 * math.sin(u * math.pi) + (0.035 if i % 2 else -0.02)))
for i, (x, z) in enumerate(pts):
    pass
p0, n0 = surface((0, -1, 0.05))
mouth = prim("sphere", "Mouth_nc", p0 + Vector((0, 0.03, 0)), (0.62, 0.1, 0.09), "M_Hollow", segs=24, rings=8)
finalize(mouth)
for v in mouth.data.vertices:
    v.co.z += 0.035 * math.sin((v.co.x + 0.31) / 0.62 * math.pi) * (1 if v.co.z < p0.z else -0.3)   # a grin
    if v.co.z < p0.z:
        v.co.z -= 0.03 * (int((v.co.x + 1) * 18) % 2)   # ragged lower lip
keep(mouth, parent=blob)

# ---- the core (the weak point): white-hot, bulging out of its top, in a collar of cracked crust
core = prim("sphere", "WeakPoint", (0, -0.1, 0.95), (0.42, 0.42, 0.42), "G_Core")
keep(core, pivot=(0, -0.1, 0.95))
for i in range(7):
    a = i / 7 * math.tau
    c = Vector((0, -0.1, 0.9)) + Vector((math.cos(a) * 0.23, math.sin(a) * 0.23, 0))
    bmr = bmesh.new()
    bmesh.ops.create_icosphere(bmr, subdivisions=1, radius=0.5)
    r = mesh_obj("Collar%d_nc" % i, bmr, ["M_Crust"], smooth=False)
    r.scale = (0.2, 0.12, 0.12)
    r.rotation_euler = (random.uniform(-0.4, 0.4), random.uniform(-0.4, 0.4), a)
    r.location = c
    keep(r, parent=blob)

# ---- glowing drips pooling round its base (they flicker as Flame*)
for i in range(6):
    a = i * 1.1 + 0.3
    base = Vector((math.sin(a) * 0.78, math.cos(a) * 0.78, 0.0))
    d = prim("sphere", "Flame%d" % i, base + Vector((0, 0, 0.03)), (0.2, 0.16, 0.07), "G_LavaHot", segs=16, rings=8)
    keep(d, pivot=base)

uv_unwrap(parts)
export_fbx(OUT_FBX, [root] + [o for o in bpy.data.objects if o.parent is not None])
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__), "lava-ooze.blend"))
render_turntable(OUT_PNG, 1.25, 0.5, yaw_deg=-28, dist_k=2.6)
print("OOZE_OK", len(parts), "parts")
