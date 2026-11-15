# Shared kit for the rigged, skinned characters of the bestiary (the wraith, the shield monk, the hanged man): lofted
# shapes with per-vertex bone weights, a skeleton, cloth / limb chains for SpringBones, export like the hero.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\rig_kit.py").read())   (then build, then finish(...))
# Blender space: Z up, the character faces -Y, its left is +X (as the hero).
import math
import bpy
from mathutils import Vector

ROOT = r"C:\Dev\CrimsonVeil"
bpy.ops.wm.read_homefile(use_empty=True)
INK, EYES = 0, 1
verts, faces, fmat, vweights = [], [], [], []


def V(x, y, z):
    return Vector((x, y, z))


def add_vert(p, w):
    verts.append(Vector(p))
    vweights.append(w)
    return len(verts) - 1


def frame(t):
    ref = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 0, 1))
    u = (ref - t * ref.dot(t)).normalized()
    return u, t.cross(u).normalized()


def loft(sections, weight, mat=INK, n=16, cap0=True, cap1=True, disp=None):
    """sections: [(centre, rx, ry)]; rx along the lateral axis, ry front/back. weight(p, s) -> {bone: w}.
    disp(i, k, p, c) -> extra offset vector for ring point k of section i."""
    rings, m = [], len(sections)
    for i, (c, rx, ry) in enumerate(sections):
        c = Vector(c)
        a, b = Vector(sections[max(i - 1, 0)][0]), Vector(sections[min(i + 1, m - 1)][0])
        t = (b - a).normalized()
        u, v = frame(t)
        ring = []
        for k in range(n):
            th = 2 * math.pi * k / n
            p = c + u * (rx * math.cos(th)) + v * (ry * math.sin(th))
            if disp is not None:
                p = p + disp(i, k, p, c)
            ring.append(add_vert(p, weight(p, i / max(m - 1, 1))))
        rings.append(ring)
    for i in range(m - 1):
        for k in range(n):
            faces.append((rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k])); fmat.append(mat)
    for ring, flip, cap in ((rings[0], True, cap0), (rings[-1], False, cap1)):
        if not cap:
            continue
        cen = sum((verts[j] for j in ring), Vector()) / n
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[(k + 1) % n], ring[k]) if flip else (ci, ring[k], ring[(k + 1) % n])); fmat.append(mat)


def blob(c, r, weight, mat=INK, seg=12, rings=8, scale=(1, 1, 1)):
    c = Vector(c)
    top = add_vert(c + V(0, 0, r * scale[2]), weight(c, 0))
    idx = []
    for i in range(1, rings):
        ph = math.pi * i / rings
        row = []
        for k in range(seg):
            th = 2 * math.pi * k / seg
            p = c + V(r * scale[0] * math.sin(ph) * math.cos(th), r * scale[1] * math.sin(ph) * math.sin(th), r * scale[2] * math.cos(ph))
            row.append(add_vert(p, weight(p, 0)))
        idx.append(row)
    bot = add_vert(c - V(0, 0, r * scale[2]), weight(c, 0))
    for k in range(seg):
        faces.append((top, idx[0][(k + 1) % seg], idx[0][k])); fmat.append(mat)
        faces.append((bot, idx[-1][k], idx[-1][(k + 1) % seg])); fmat.append(mat)
    for i in range(len(idx) - 1):
        for k in range(seg):
            faces.append((idx[i][k], idx[i][(k + 1) % seg], idx[i + 1][(k + 1) % seg], idx[i + 1][k])); fmat.append(mat)


def W(b):
    return lambda p, s: {b: 1.0}


def blend(a, b, za, zb):
    """a below za, b above zb, blended between (by height)."""
    def f(p, s):
        t = max(0.0, min(1.0, (p.z - za) / (zb - za)))
        return {a: 1 - t, b: t} if 0 < t < 1 else ({a: 1.0} if t <= 0 else {b: 1.0})
    return f


def along(a, b, bone_a, bone_b, from_=0.75):
    a, b = Vector(a), Vector(b)
    d = b - a

    def f(p, s):
        t = max(0.0, min(1.0, (p - a).dot(d) / d.length_squared))
        if t <= from_:
            return {bone_a: 1.0}
        k = (t - from_) / (1 - from_) * 0.5
        return {bone_a: 1 - k, bone_b: k}
    return f


# ---------------------------------------------------------------- skeleton
BONES = []  # (name, head, tail, parent)


def bone(name, head, tail, parent):
    BONES.append((name, V(*head), V(*tail), parent))


def bone_head(n):
    return next(h for b, h, t, p in BONES if b == n)


def bone_tail(n):
    return next(t for b, h, t, p in BONES if b == n)


def chain_ribbon(prefix, n, pts, width, thick, root, mat=None, taper=0.6, point=0.12):
    """A ribbon of cloth (or a limb of rope) along pts (len k+1) with a bone chain <prefix><n>.<j> parented to root,
    wide (width) in the front-back plane, thin (thick) across. Swings on SpringBones."""
    mat = INK if mat is None else mat
    parent = root
    k = len(pts) - 1
    for j in range(k):
        bone("%s%d.%d" % (prefix, n, j), tuple(pts[j]), tuple(pts[j + 1]), parent)
        parent = "%s%d.%d" % (prefix, n, j)
    secs, sw = [], []
    for j in range(k):
        for u in ((0.0, 0.5, 1.0) if j == 0 else (0.5, 1.0)):
            f = (j + u) / k
            w = width * (1.0 - taper * f) * (point if j == k - 1 and u == 1.0 else 1.0)
            secs.append((pts[j].lerp(pts[j + 1], u), thick, w))
            sw.append({root: 0.6, "%s%d.0" % (prefix, n): 0.4} if (j == 0 and u == 0.0) else {"%s%d.%d" % (prefix, n, j): 1.0})
    loft(secs, lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))], mat=mat, n=8)


def finish(name, materials, fbx, blend, export=True):
    """Armature from BONES, skinned mesh from the lofts, saved to blend and exported (hero-style FBX)."""
    arm = bpy.data.armatures.new(name + "Rig")
    rig = bpy.data.objects.new(name + "Rig", arm)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    win = bpy.context.window_manager.windows[0]
    ctx = dict(window=win, screen=win.screen, active_object=rig, object=rig, selected_objects=[rig])
    with bpy.context.temp_override(**ctx):
        bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.edit_bones
    for bn, h, t, parent in BONES:
        b = eb.new(bn)
        b.head, b.tail = h, t
        if parent:
            b.parent = eb[parent]
            b.use_connect = (eb[parent].tail - h).length < 1e-5 and not parent in ("Hips", "Chest", "Spine", "Head")
    with bpy.context.temp_override(**ctx):
        bpy.ops.object.mode_set(mode='OBJECT')
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    me.shade_smooth()
    for mname, col in materials:
        m = bpy.data.materials.get(mname) or bpy.data.materials.new(mname)
        m.diffuse_color = col
        me.materials.append(m)
    for poly, m in zip(me.polygons, fmat):
        poly.material_index = m
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = rig
    groups = {}
    for vi, w in enumerate(vweights):
        tot = sum(w.values())
        for bn, x in w.items():
            g = groups.get(bn) or ob.vertex_groups.new(name=bn)
            groups[bn] = g
            g.add([vi], x / tot, 'REPLACE')
    mod = ob.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    bpy.ops.wm.save_as_mainfile(filepath=ROOT + "\\ArtSource\\" + blend)
    if export:
        for o in bpy.context.scene.objects:
            o.select_set(o in (rig, ob))
        with bpy.context.temp_override(window=win, screen=win.screen, active_object=rig, selected_objects=[rig, ob]):
            bpy.ops.export_scene.fbx(filepath=ROOT + "\\Assets\\_CrimsonVeil\\Art\\Enemy\\" + fbx, use_selection=True,
                                     object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                     secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                     bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
    return {"verts": len(verts), "faces": len(faces), "bones": len(BONES), "dims": [round(x, 3) for x in ob.dimensions]}


def preview(path, size=2.6, zc=1.0):
    """Side (as the game sees it), three-quarter and front renders side by side on a light ground."""
    import numpy as np
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'
    sc.display.shading.color_type = 'MATERIAL'
    sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = 600, 800
    cd = bpy.data.cameras.new("PC"); cam = bpy.data.objects.new("PC", cd); sc.collection.objects.link(cam)
    cd.type = 'ORTHO'; cd.ortho_scale = size
    sc.camera = cam
    tiles = []
    for yaw in (90, 45, 0):
        a = math.radians(yaw)
        cam.location = Vector((8 * math.sin(a), -8 * math.cos(a), zc))
        cam.rotation_euler = (math.radians(90), 0, a)
        sc.render.filepath = path + ".tmp.png"
        bpy.ops.render.render(write_still=True)
        im = bpy.data.images.load(sc.render.filepath); w, h = im.size
        tiles.append(np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)); bpy.data.images.remove(im)
    s = np.concatenate(tiles, axis=1)
    al = s[..., 3:4]
    s = np.concatenate([s[..., :3] * al + 0.85 * (1 - al), np.ones_like(al)], axis=2)
    H, W2 = s.shape[:2]
    out = bpy.data.images.new("pv", W2, H, alpha=True); out.pixels[:] = s.ravel()
    out.filepath_raw = path; out.file_format = "PNG"; out.save(); bpy.data.images.remove(out)
    bpy.data.objects.remove(cam)
