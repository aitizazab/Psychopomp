# Part helpers: tubes from curves, flames, pivots and parenting (world-space build, then pivots).
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from hd_common import mat, link


def finalize(obj):
    """Bake the object's transform into its mesh (object at the origin, identity)."""
    bpy.context.view_layer.update()
    me = obj.data
    me.transform(obj.matrix_world)
    obj.matrix_world = Matrix.Identity(4)
    return obj


def set_origin(obj, point):
    point = Vector(point)
    obj.data.transform(Matrix.Translation(-point))
    obj.location = point
    return obj


def parent_keep(obj, parent):
    bpy.context.view_layer.update()
    world = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_parent_inverse = Matrix.Identity(4)
    obj.matrix_world = world
    return obj


def place(obj, pivot, parent=None):
    finalize(obj)
    set_origin(obj, pivot)
    if parent is not None:
        parent_keep(obj, parent)
    return obj


def tube(name, pts, radii, material, res=3, bevel_res=3, cap=True):
    cu = bpy.data.curves.new(name, 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = 1.0
    cu.bevel_resolution = bevel_res
    cu.resolution_u = res
    cu.use_fill_caps = cap
    sp = cu.splines.new('BEZIER')
    sp.bezier_points.add(len(pts) - 1)
    for i, (p, r) in enumerate(zip(pts, radii)):
        bp = sp.bezier_points[i]
        bp.co = Vector(p)
        bp.radius = r
        bp.handle_left_type = bp.handle_right_type = 'AUTO'
    o = bpy.data.objects.new(name + "_crv", cu)
    link(o)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    bpy.data.objects.remove(o)
    me.materials.clear()
    me.materials.append(mat(material))
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    link(ob)
    return ob


def flame(name, base, height, radius, material, lean=(0, 0), seed=0):
    """A licking flame: a twisted teardrop cone, its pivot at the base."""
    random.seed(seed)
    bm = bmesh.new()
    rings, segs = 7, 10
    verts = []
    for i in range(rings + 1):
        t = i / rings
        r = radius * math.sin(math.pi * min(1.0, t * 1.15 + 0.08)) * (1 - t) ** 0.35 if t < 1 else 0.0
        ring = []
        tw = t * 1.6
        for j in range(segs):
            a = j / segs * math.tau + tw
            wob = 1 + 0.18 * math.sin(a * 3 + t * 5 + seed)
            x = math.cos(a) * r * wob + lean[0] * t * t * height
            y = math.sin(a) * r * wob + lean[1] * t * t * height
            ring.append(bm.verts.new((x, y, t * height)))
        verts.append(ring)
    for i in range(rings):
        for j in range(segs):
            a, b = verts[i][j], verts[i][(j + 1) % segs]
            c, d = verts[i + 1][(j + 1) % segs], verts[i + 1][j]
            bm.faces.new((a, b, c, d))
    bm.faces.new(list(reversed(verts[0])))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat(material))
    for p in me.polygons:
        p.use_smooth = True
    o = bpy.data.objects.new(name, me)
    link(o)
    o.location = Vector(base)
    return o


def solidify(obj, thickness):
    m = obj.modifiers.new("Solid", 'SOLIDIFY')
    m.thickness = thickness
    m.offset = 0
    obj.data.update()
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)
    return obj


def subsurf(obj, levels=1):
    m = obj.modifiers.new("Sub", 'SUBSURF')
    m.levels = levels
    m.render_levels = levels
    obj.data.update()
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)
    return obj
