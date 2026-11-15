# Builds the ink wraith (Wraith.blend -> Assets/_CrimsonVeil/Art/Enemy/Wraith.fbx): a gaunt, hunched spectre that
# hovers above the ground - a deep hood with a peak jutting over the face and two crimson eyes in its shadow, a long
# neck, a ribcage standing out under the shroud, bony shoulders, very long thin arms with knobbly elbows and hands of
# three long claws; below the waist the shroud tears into strips of ink, two more trail from the shoulders.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\wraith_build.py").read())
# Blender space: Z up, the wraith faces -Y, its left is +X (as the hero). Bone names keep the hero's for the body
# (EnemyAnimator poses Spine / Chest / Head / arms by name); claws are Finger.<side>.<i>.<k> (HeroHands), the strips
# Tatter.<n>.<k> (SpringBones, prefix "Tatter.").
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


# ---------------------------------------------------------------- the skeleton (positions)
BONES = []  # (name, head, tail, parent)


def bone(name, head, tail, parent):
    BONES.append((name, V(*head), V(*tail), parent))


bone("Hips", (0, 0.02, 0.30), (0, 0.0, 0.72), None)
bone("Spine", (0, 0.0, 0.72), (0, -0.03, 1.08), "Hips")
bone("Chest", (0, -0.03, 1.08), (0, -0.13, 1.47), "Spine")
bone("Neck", (0, -0.13, 1.47), (0, -0.24, 1.64), "Chest")
bone("Head", (0, -0.24, 1.64), (0, -0.28, 1.92), "Neck")
for side, sx in (("L", 1.0), ("R", -1.0)):
    bone("Shoulder." + side, (0.05 * sx, -0.09, 1.41), (0.21 * sx, -0.08, 1.44), "Chest")
    bone("UpperArm." + side, (0.21 * sx, -0.08, 1.44), (0.29 * sx, -0.16, 1.01), "Shoulder." + side)
    bone("LowerArm." + side, (0.29 * sx, -0.16, 1.01), (0.32 * sx, -0.25, 0.61), "UpperArm." + side)
    bone("Hand." + side, (0.32 * sx, -0.25, 0.61), (0.33 * sx, -0.29, 0.49), "LowerArm." + side)
B = {n: (h, t) for n, h, t, p in BONES}


def head_of(n):
    return B[n][0]


def tail_of(n):
    return B[n][1]


# ---------------------------------------------------------------- body
# Seen from the side (as the game sees it): a rounded cowl with its peak drooping forward over a deep, dark hollow
# where the face should be; a ragged mantle over the shoulders; below it a wide shroud falling in deep folds that
# trails back as the wraith hovers, its hem torn. (z, half width, half depth, centre y; forward is -y)
SHROUD = [(1.50, 0.15, 0.12, -0.12), (1.44, 0.235, 0.15, -0.10), (1.38, 0.24, 0.16, -0.08), (1.28, 0.21, 0.16, -0.06),
          (1.15, 0.20, 0.17, -0.03), (1.00, 0.22, 0.185, 0.00), (0.86, 0.25, 0.21, 0.035), (0.72, 0.28, 0.235, 0.07),
          (0.60, 0.30, 0.25, 0.10)]


def shroud_w(p, s):
    z = p.z
    if z > 1.15:
        return {"Chest": 1.0}
    if z > 0.95:
        t = (z - 0.95) / 0.2
        return {"Spine": 1 - t, "Chest": t}
    if z > 0.75:
        t = (z - 0.75) / 0.2
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


def folds(n_folds, amp, start, phase=0.0):
    """Cloth falling in folds: the ring rippled in and out, more and more towards its lower end."""
    def f(i, k, p, c, m):
        g = max(0.0, (i - start) / max(1.0, m - 1 - start))
        th = 2 * math.pi * k / RING
        r = (p - c)
        r.z = 0.0
        return r.normalized() * (amp * g * math.sin(n_folds * th + phase + 0.7 * math.sin(3 * th))) if r.length > 1e-6 else V(0, 0, 0)
    return f


RING = 40


def shroud_disp(i, k, p, c):
    d = folds(9, 0.035, 2)(i, k, p, c, len(SHROUD))
    if i == len(SHROUD) - 1:
        d = d + V(0, 0, -0.09 * ((k * 7) % 4) / 3.0 - 0.03 * math.sin(k * 1.3))   # the hem torn into tongues
    return d


loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in SHROUD], shroud_w, n=RING, cap0=False, cap1=False, disp=shroud_disp)

# Neck (hidden in the hood, it moves the head).
loft([(head_of("Neck").lerp(tail_of("Neck"), u), 0.04, 0.04) for u in (0.0, 1.0)],
     lambda p, s: {"Chest": 0.5, "Neck": 0.5} if s < 0.5 else {"Neck": 1.0}, n=10)

# The cowl: round at the back of the skull, its peak drooping forward at the top. (z, half width, half depth, centre y)
HOOD_KEYS = [(1.46, 0.17, 0.15, -0.10), (1.53, 0.165, 0.165, -0.13), (1.60, 0.165, 0.185, -0.16), (1.68, 0.16, 0.2, -0.19),
             (1.76, 0.155, 0.205, -0.21), (1.83, 0.145, 0.195, -0.225), (1.89, 0.125, 0.17, -0.245), (1.93, 0.095, 0.135, -0.27),
             (1.955, 0.06, 0.09, -0.30), (1.965, 0.02, 0.035, -0.33)]
HOOD = []
for j in range(len(HOOD_KEYS) - 1):
    for u in (0.0, 0.5):
        a0, b0 = HOOD_KEYS[j], HOOD_KEYS[j + 1]
        HOOD.append(tuple(a0[q] + (b0[q] - a0[q]) * u for q in range(4)))
HOOD.append(HOOD_KEYS[-1])


def hood_w(p, s):
    if p.z < 1.58:
        return {"Chest": 0.5, "Neck": 0.5}
    if p.z < 1.66:
        return {"Neck": 0.5, "Head": 0.5}
    return {"Head": 1.0}


def cowl(i, k, p, c):
    th = 2 * math.pi * k / 32
    fy = -math.sin(th)                                   # +1 at the very front (towards -y)
    side = math.cos(th)
    z = p.z
    # the face: a deep hollow, wide and tall as a face, sunk far back into the cowl
    face = math.exp(-((z - 1.71) / 0.1) ** 2) * max(0.0, fy) ** 1.8 * math.exp(-(side / 0.62) ** 2)
    # its rim: rolled forward around the opening, heaviest at the drooping peak above it
    rim = math.exp(-((z - 1.84) / 0.05) ** 2) * max(0.0, fy) ** 2
    cheek = math.exp(-((z - 1.66) / 0.1) ** 2) * max(0.0, fy) * (abs(side) > 0.45) * 0.4
    # soft folds where the cowl gathers at the back of the neck
    back = math.exp(-((z - 1.52) / 0.06) ** 2) * max(0.0, -fy) * math.sin(7 * th)
    return V(0, 0.2 * face - 0.08 * rim - 0.02 * cheek, -0.03 * rim) + (p - c).normalized() * 0.012 * back


_hood0 = len(verts)
loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in HOOD], hood_w, n=32, disp=cowl, cap0=False)
_hood = verts[_hood0:]


def hood_front(x, z):
    """The hood's face at (x, z): the foremost point of its surface there (measured, not guessed)."""
    near = [v.y for v in _hood if abs(v.x - x) < 0.035 and abs(v.z - z) < 0.045]
    return min(near) if near else -0.4

# The void inside the hood: a dark skull-like mass far back in the hollow; the eyes burn on it.
blob(V(0, -0.21, 1.70), 0.075, W("Head"), seg=14, rings=10, scale=(1.0, 0.8, 1.15))
for sx in (1, -1):
    ey = hood_front(0.045 * sx, 1.72)
    blob(V(0.045 * sx, ey - 0.006, 1.72), 0.02, W("Head"), mat=EYES, seg=10, rings=8, scale=(1.3, 0.6, 0.7))

# The mantle: a short ragged cape over the shoulders, falling in folds, its edge torn.
MANTLE = [(1.56, 0.17, 0.15, -0.13), (1.50, 0.23, 0.19, -0.11), (1.43, 0.29, 0.23, -0.08), (1.35, 0.32, 0.25, -0.05),
          (1.27, 0.33, 0.26, -0.03)]


def mantle_disp(i, k, p, c):
    d = folds(7, 0.03, 1, 0.8)(i, k, p, c, len(MANTLE))
    if i == len(MANTLE) - 1:
        th = 2 * math.pi * k / RING
        d = d + V(0, 0, -0.07 * ((k * 5) % 3) / 2.0 - 0.06 * max(0.0, math.sin(th)))   # longer at the back
    return d


loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in MANTLE], lambda p, s: {"Chest": 1.0}, n=RING, cap0=False, cap1=False, disp=mantle_disp)

# ---------------------------------------------------------------- arms and claws
for side, sx in (("L", 1.0), ("R", -1.0)):
    sh, ua, la, ha = "Shoulder." + side, "UpperArm." + side, "LowerArm." + side, "Hand." + side
    blob(tail_of(sh) + V(0, 0, 0.01), 0.045, W(ua), seg=10, rings=8)                    # bony shoulder
    loft([(head_of(sh).lerp(tail_of(sh), u), 0.035, 0.035) for u in (0.0, 1.0)], W(sh), n=10)
    # the sleeve: narrow at the shoulder, a bony elbow showing through, flaring into a torn bell at the wrist
    a, b = head_of(ua), tail_of(ua)
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.08), (0.3, 0.075), (0.6, 0.07), (1.0, 0.066))], along(a, b, ua, la, 0.8), n=24,
         disp=lambda i, k, p, c: (p - c) * (0.14 * math.sin(k * 6 * math.pi / 24 + i)))   # loose cloth, creased
    blob(b, 0.062, lambda p, s, ua=ua, la=la: {ua: 0.5, la: 0.5}, seg=10, rings=8)
    a, b = head_of(la), tail_of(la)
    cuff = lambda i, k, p, c: ((p - c) * (0.16 * (i / 4.0) * math.sin(k * 7 * math.pi / 28))
                              + V(0, 0, -(0.16 * max(0.0, math.sin(2 * math.pi * k / 28)) + 0.05 * ((k * 5) % 3) / 2.0) * (1.0 if i == 4 else 0.35 * i / 4.0)))   # an angel sleeve: folds, a long torn point hanging behind the forearm
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.068), (0.3, 0.078), (0.6, 0.1), (0.85, 0.13), (1.0, 0.15))],
         along(a, b, la, ha, 0.9), n=28, cap1=False, disp=cuff)
    # the bare bony forearm inside the sleeve, coming out at the wrist
    loft([(a.lerp(b, u), r, r) for u, r in ((0.5, 0.02), (1.0, 0.017))], W(la), n=8)
    # the hand: a narrow palm, three long claws spread front to back, a thumb-claw
    a, b = head_of(ha), tail_of(ha)
    d = (b - a).normalized()
    loft([(a.lerp(b, u), 0.016, r) for u, r in ((0.0, 0.022), (0.6, 0.032), (1.0, 0.03))], W(ha), n=10)
    palm_in = V(-sx, 0, 0)
    fwd = V(0, -1, 0)
    for i, (off, lens) in enumerate(((-0.02, (0.075, 0.07, 0.085)), (0.0, (0.085, 0.08, 0.095)), (0.02, (0.07, 0.065, 0.08)))):
        base = b + fwd * off
        name = "Finger.%s.%d" % (side, i)
        pts, dirn = [base], d.copy()
        for k, ln in enumerate(lens):
            dirn = (dirn + palm_in * math.tan(math.radians(10 + 9 * k))).normalized()
            pts.append(pts[-1] + dirn * ln)
        parent = ha
        for k in range(3):
            bone("%s.%d" % (name, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
            parent = "%s.%d" % (name, k)
        secs, sw = [], []
        for k in range(3):
            for u in ((0.0, 0.5, 1.0) if k == 0 else (0.5, 1.0)):
                r = 0.011 * (1.0 - 0.28 * k) * (1.0 - 0.75 * u if k == 2 else 1.0)  # each claw tapers to a point
                secs.append((pts[k].lerp(pts[k + 1], u), max(r, 0.0015), max(r, 0.0015)))
                sw.append({ha: 0.5, "%s.%d" % (name, k): 0.5} if u == 0.0 else {"%s.%d" % (name, k): 1.0})
        loft(secs, lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))], n=8)
    tb = a + d * 0.05 + fwd * 0.03
    tdir = (d * 0.6 + fwd * 0.7 + palm_in * 0.2).normalized()
    t1 = tb + tdir * 0.05
    t2 = t1 + (tdir + palm_in * 0.4).normalized() * 0.05
    bone("Thumb.%s.0" % side, tuple(tb), tuple(t1), ha)
    bone("Thumb.%s.1" % side, tuple(t1), tuple(t2), "Thumb.%s.0" % side)
    loft([(tb, 0.011, 0.011), (t1, 0.009, 0.009), (t2, 0.002, 0.002)],
         lambda p, s, side=side: {"Thumb.%s.0" % side: 1.0} if s < 0.5 else {"Thumb.%s.1" % side: 1.0}, n=8)

# ---------------------------------------------------------------- the torn shroud
# Strips of ink hanging from the bell of the shroud (and two from the shoulders behind), each a chain of four bones
# the game swings like hair. Ragged: different lengths and widths, twisted a little.
STRIPS, STRIP_ANG = [], []
for n in range(10):
    ang = math.radians(-170 + n * 38)
    STRIP_ANG.append((math.sin(ang), math.cos(ang)))   # the ribbon lies along the hem: its width along the tangent
    top = V(0.26 * math.cos(ang), 0.10 + 0.22 * math.sin(ang), 0.58)
    length = 0.48 + 0.2 * ((n * 37) % 5) / 4.0
    bottom = top + V(0.05 * math.cos(ang), 0.16 + 0.04 * (n % 3), -length)
    STRIPS.append((top, bottom, 0.10 - 0.015 * (n % 3)))
for sx in (1, -1):
    top = V(0.15 * sx, 0.03, 1.40)
    STRIPS.append((top, top + V(0.05 * sx, 0.30, -0.70), 0.09))
    STRIP_ANG.append((1.0, 0.25))
for n, (top, bottom, width) in enumerate(STRIPS):
    root = "Hips" if top.z < 1.0 else "Chest"
    sag = V(0, -0.04, 0)  # a gentle belly to each ribbon
    pts = [top.lerp(bottom, k / 4.0) + sag * math.sin(math.pi * k / 4.0) for k in range(5)]
    parent = root
    for k in range(4):
        bone("Tatter.%d.%d" % (n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = "Tatter.%d.%d" % (n, k)
    secs, sw = [], []
    for k in range(4):
        for u in ((0.0, 0.5, 1.0) if k == 0 else (0.5, 1.0)):
            f = (k + u) / 4.0
            w = width * (1.0 - 0.55 * f) * (0.12 if k == 3 and u == 1.0 else 1.0)  # torn to a point
            secs.append((pts[k].lerp(pts[k + 1], u), max(0.012, w * abs(STRIP_ANG[n][0])), max(0.012, w * abs(STRIP_ANG[n][1]))))
            sw.append({root: 0.6, "Tatter.%d.0" % n: 0.4} if (k == 0 and u == 0.0) else {"Tatter.%d.%d" % (n, k): 1.0})
    loft(secs, lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))], n=8)

# ---------------------------------------------------------------- armature + mesh
arm = bpy.data.armatures.new("WraithRig")
rig = bpy.data.objects.new("WraithRig", arm)
bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
_win = bpy.context.window_manager.windows[0]
_ctx = dict(window=_win, screen=_win.screen, active_object=rig, object=rig, selected_objects=[rig])
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='EDIT')
eb = arm.edit_bones
for name, h, t, parent in BONES:
    b = eb.new(name)
    b.head, b.tail = h, t
    if parent:
        b.parent = eb[parent]
        b.use_connect = (parent.startswith("Finger.") or parent.startswith("Tatter.") or parent.startswith("Thumb.")) \
            and (eb[parent].tail - h).length < 1e-5
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='OBJECT')

me = bpy.data.meshes.new("Wraith")
me.from_pydata([tuple(v) for v in verts], [], faces)
me.update()
me.shade_smooth()
for name, col in (("WraithInk", (0.07, 0.07, 0.08, 1)), ("WraithEyes", (1, 0.02, 0.04, 1))):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = col
    me.materials.append(m)
for poly, m in zip(me.polygons, fmat):
    poly.material_index = m
ob = bpy.data.objects.new("Wraith", me)
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

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\Wraith.blend")
result = {"verts": len(verts), "faces": len(faces), "bones": len(BONES), "dims": [round(x, 3) for x in ob.dimensions]}

if globals().get("EXPORT", True):
    for o in bpy.context.scene.objects:
        o.select_set(o in (rig, ob))
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=rig, selected_objects=[rig, ob]):
        bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Enemy\Wraith.fbx", use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                 bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
