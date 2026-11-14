# Builds the hero model on the existing rig (Hero.blend), after the concept art: a lean young warrior in layered
# light armour — stacked pauldrons, bracers, a belt with a crimson sash, short breeches, knee guards and tall
# boots — wind-swept hair streaming back, a crimson scarf wound at the neck (the veil flows from it).
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\hero_build.py").read())
# Blender space: Z up, the hero faces -Y, his left is +X. Bone names are kept (the game poses them by name);
# the hair chains are rebuilt (SpringBones finds any "Hair.N.0..2").
import math
import random
import bpy
from mathutils import Vector, Matrix

ROOT = r"C:\Dev\CrimsonVeil"
bpy.ops.wm.open_mainfile(filepath=ROOT + r"\ArtSource\Hero.blend")
rig = bpy.data.objects["HeroRig"]
INK, CRIMSON = 0, 1

verts, faces, fmat, vweights = [], [], [], []
THICK = 1.0  # girth multiplier for lofted parts (limbs are built a little sturdier)


def add_vert(p, w):
    verts.append(Vector(p))
    vweights.append(w)
    return len(verts) - 1


def frame(t):
    """Two unit axes perpendicular to direction t: u ~ lateral (X), v ~ front/back."""
    ref = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 0, 1))
    u = (ref - t * ref.dot(t)).normalized()
    return u, t.cross(u).normalized()


def loft(sections, weight, mat=INK, n=20, cap0=True, cap1=True, square=0.0):
    """sections: [(centre, rx, ry, (optional) dict(dy=, front=))]. rx along the lateral axis, ry front/back.
    square > 0 makes the cross-section boxier (armour). weight(point, s) -> {bone: w}."""
    rings = []
    m = len(sections)
    for i, sec in enumerate(sections):
        c = Vector(sec[0])
        a = Vector(sections[max(i - 1, 0)][0])
        b = Vector(sections[min(i + 1, m - 1)][0])
        t = (b - a).normalized()
        u, v = frame(t)
        rx, ry = sec[1] * THICK, sec[2] * THICK
        extra = sec[3] if len(sec) > 3 else {}
        ring = []
        for k in range(n):
            th = 2 * math.pi * k / n
            cx, cy = math.cos(th), math.sin(th)
            if square > 0:
                e = 2.0 / (2.0 + square * 6.0)
                cx = math.copysign(abs(cx) ** e, cx)
                cy = math.copysign(abs(cy) ** e, cy)
            r_front = extra.get("front", 1.0) if cy < 0 else extra.get("back", 1.0)
            p = c + u * (rx * cx) + v * (ry * cy * r_front)
            ring.append(add_vert(p, weight(p, i / max(m - 1, 1))))
        rings.append(ring)
    for i in range(m - 1):
        for k in range(n):
            a, b = rings[i][k], rings[i][(k + 1) % n]
            c, d = rings[i + 1][(k + 1) % n], rings[i + 1][k]
            faces.append((a, b, c, d)); fmat.append(mat)
    for ring, flip, cap in ((rings[0], True, cap0), (rings[-1], False, cap1)):
        if not cap:
            continue
        cen = sum((verts[j] for j in ring), Vector()) / n
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            f = (ci, ring[(k + 1) % n], ring[k]) if flip else (ci, ring[k], ring[(k + 1) % n])
            faces.append(f); fmat.append(mat)


def blob(centre, r, weight, mat=INK, seg=16, rings=10, rot=None):
    """Ellipsoid (r = (rx, ry, rz)), optionally rotated by matrix rot."""
    c = Vector(centre)
    idx = []
    for j in range(1, rings):
        ph = math.pi * j / rings
        row = []
        for k in range(seg):
            th = 2 * math.pi * k / seg
            d = Vector((math.sin(ph) * math.cos(th) * r[0], math.sin(ph) * math.sin(th) * r[1], math.cos(ph) * r[2]))
            if rot is not None:
                d = rot @ d
            p = c + d
            row.append(add_vert(p, weight(p, 0)))
        idx.append(row)
    top = add_vert(c + (rot @ Vector((0, 0, r[2])) if rot else Vector((0, 0, r[2]))), weight(c, 0))
    bot = add_vert(c + (rot @ Vector((0, 0, -r[2])) if rot else Vector((0, 0, -r[2]))), weight(c, 0))
    for k in range(seg):
        k1 = (k + 1) % seg
        faces.append((top, idx[0][k], idx[0][k1])); fmat.append(mat)
        faces.append((bot, idx[-1][k1], idx[-1][k])); fmat.append(mat)
        for j in range(len(idx) - 1):
            faces.append((idx[j][k], idx[j + 1][k], idx[j + 1][k1], idx[j][k1])); fmat.append(mat)


def dome(centre, axis, radius, depth, weight, mat=INK, seg=18, rings=6, squash=1.0, up=None):
    """A shallow shell cap (armour plate) bulging along axis, with a little thickness."""
    axis = Vector(axis).normalized()
    u, v = frame(axis)
    if up is not None:  # orient the squash
        v = (Vector(up) - axis * Vector(up).dot(axis)).normalized()
        u = v.cross(axis).normalized()
    c = Vector(centre)
    outer, inner = [], []
    for j in range(rings + 1):
        a = (math.pi / 2) * j / rings  # 0 = rim, pi/2 = crown
        row_o, row_i = [], []
        for k in range(seg):
            th = 2 * math.pi * k / seg
            rr = radius * math.cos(a)
            base = u * (rr * math.cos(th)) + v * (rr * math.sin(th) * squash)
            p_o = c + base + axis * (depth * math.sin(a))
            p_i = c + base * 0.92 + axis * (depth * math.sin(a) - 0.012)
            row_o.append(add_vert(p_o, weight(p_o, 0)))
            row_i.append(add_vert(p_i, weight(p_i, 0)))
        outer.append(row_o); inner.append(row_i)
    for j in range(rings):
        for k in range(seg):
            k1 = (k + 1) % seg
            faces.append((outer[j][k], outer[j][k1], outer[j + 1][k1], outer[j + 1][k])); fmat.append(mat)
            faces.append((inner[j][k], inner[j + 1][k], inner[j + 1][k1], inner[j][k1])); fmat.append(mat)
    for k in range(seg):  # rim
        k1 = (k + 1) % seg
        faces.append((outer[0][k], inner[0][k], inner[0][k1], outer[0][k1])); fmat.append(mat)


def box(centre, size, weight, mat=INK, rot=None):
    c = Vector(centre)
    hx, hy, hz = (s / 2 for s in size)
    idx = []
    for z in (-hz, hz):
        for y in (-hy, hy):
            for x in (-hx, hx):
                d = Vector((x, y, z))
                if rot is not None:
                    d = rot @ d
                idx.append(add_vert(c + d, weight(c + d, 0)))
    for f in ((0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)):
        faces.append(tuple(idx[i] for i in f)); fmat.append(mat)


# ---------------------------------------------------------------- weights
def W(bone):
    return lambda p, s: {bone: 1.0}


def blend_z(bands):
    """bands: [(z, bone)] ascending: weight blends linearly between neighbouring bands."""
    def f(p, s):
        z = p.z
        if z <= bands[0][0]:
            return {bands[0][1]: 1.0}
        for (z0, b0), (z1, b1) in zip(bands, bands[1:]):
            if z <= z1:
                t = (z - z0) / (z1 - z0)
                return {b0: 1 - t, b1: t} if b0 != b1 else {b0: 1.0}
        return {bands[-1][1]: 1.0}
    return f


def along(a, b, bone_a, bone_b, blend_from=0.8):
    """Weight by projection on the segment a->b: bone_a, blending into bone_b over the last part."""
    a, b = Vector(a), Vector(b)
    d = b - a
    L2 = d.length_squared

    def f(p, s):
        t = max(0.0, min(1.0, (p - a).dot(d) / L2))
        if t <= blend_from or bone_b is None:
            return {bone_a: 1.0}
        k = (t - blend_from) / (1 - blend_from) * 0.5
        return {bone_a: 1 - k, bone_b: k}
    return f


def head_of(name):
    return rig.data.bones[name].head_local.copy()


def tail_of(name):
    return rig.data.bones[name].tail_local.copy()


# ---------------------------------------------------------------- torso
TORSO = [  # z, half width, half depth, centre y (forward is -y): deep chest, narrow waist, a straight back
    (0.88, 0.150, 0.112, 0.000), (0.97, 0.158, 0.118, 0.004), (1.05, 0.144, 0.104, 0.000),
    (1.13, 0.134, 0.098, -0.008), (1.22, 0.152, 0.116, -0.020), (1.31, 0.178, 0.136, -0.030),
    (1.39, 0.194, 0.142, -0.034), (1.45, 0.192, 0.128, -0.024), (1.50, 0.162, 0.104, -0.010),
    (1.54, 0.085, 0.068, 0.000),
]


def torso_at(z):
    for (z0, *a), (z1, *b) in zip(TORSO, TORSO[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0)
            return [x + (y - x) * t for x, y in zip(a, b)]
    return list(TORSO[0][1:] if z < TORSO[0][0] else TORSO[-1][1:])


torso_w = blend_z([(1.0, "Hips"), (1.1, "Spine"), (1.26, "Spine"), (1.34, "Chest")])
loft([((0, cy, z), rx, ry, {"front": 1.12 if 1.28 < z < 1.48 else 1.0, "back": 0.92 if 1.2 < z < 1.48 else 1.0})
      for z, rx, ry, cy in TORSO], torso_w, n=28)

# Cuirass: layered bands over the belly and a raised breastplate edge — the stepped profile of the concept.
for z, grow in ((1.17, 0.016), (1.235, 0.018), (1.30, 0.02)):
    rx, ry, cy = torso_at(z)
    loft([((0, cy, z - 0.03), rx + 0.004, ry + 0.004), ((0, cy - 0.004, z - 0.008), rx + grow, ry + grow),
          ((0, cy, z + 0.03), rx + 0.004, ry + 0.004)], torso_w, n=28)
rx, ry, cy = torso_at(1.40)
loft([((0, cy, 1.34), rx + 0.004, ry + 0.004, {"back": 0.9}), ((0, cy - 0.006, 1.40), rx + 0.016, ry + 0.02, {"back": 0.9}),
      ((0, cy, 1.47), rx - 0.01, ry + 0.006, {"back": 0.9})], torso_w, n=28, square=0.25)
# A crimson line across the chest (the stitched trim in the art).
loft([((0, cy, 1.392), rx + 0.019, ry + 0.023, {"back": 0.9}), ((0, cy, 1.408), rx + 0.019, ry + 0.023, {"back": 0.9})],
     torso_w, mat=CRIMSON, n=28, cap0=False, cap1=False, square=0.25)

# Belt with a crimson sash under it and a knot hanging at the left hip.
rx, ry, cy = torso_at(0.97)
loft([((0, cy, 0.935), rx + 0.022, ry + 0.022), ((0, cy, 0.985), rx + 0.024, ry + 0.024)], W("Hips"), mat=CRIMSON, n=28)
loft([((0, cy, 0.975), rx + 0.026, ry + 0.026), ((0, cy, 1.015), rx + 0.026, ry + 0.026)], W("Hips"), n=28, square=0.4)
box((0, cy - ry - 0.03, 0.995), (0.06, 0.02, 0.05), W("Hips"))  # buckle
loft([((0.155, -0.02, 0.95), 0.03, 0.02), ((0.17, -0.005, 0.86), 0.022, 0.012), ((0.165, 0.01, 0.76), 0.03, 0.01)],
     W("Hips"), mat=CRIMSON, n=10)  # sash tail

# ---------------------------------------------------------------- neck, scarf, head
loft([((0, 0.0, 1.50), 0.05, 0.048), ((0, -0.01, 1.58), 0.043, 0.043), ((0, -0.015, 1.65), 0.045, 0.045)],
     blend_z([(1.53, "Chest"), (1.58, "Neck"), (1.62, "Neck"), (1.66, "Head")]), n=16)
# Crimson scarf wound round the neck: two thick turns.
for z, r, t in ((1.545, 0.088, 0.03), (1.585, 0.078, 0.026)):
    loft([((0, -0.008, z - t), r - 0.012, r - 0.016), ((0, -0.012, z), r + 0.012, r + 0.006),
          ((0, -0.008, z + t), r - 0.012, r - 0.016)], blend_z([(1.53, "Chest"), (1.58, "Neck")]), mat=CRIMSON, n=20)

HEAD = W("Head")
blob((0, 0.005, 1.725), (0.083, 0.098, 0.105), HEAD, seg=20, rings=12)  # skull
blob((0, -0.045, 1.655), (0.055, 0.05, 0.045), HEAD)  # jaw and chin
blob((0, -0.06, 1.64), (0.03, 0.03, 0.025), HEAD)  # chin point (strong profile)
box((0, -0.098, 1.705), (0.022, 0.03, 0.045), HEAD, rot=Matrix.Rotation(math.radians(-20), 3, 'X'))  # nose
for sx in (-1, 1):
    blob((sx * 0.083, 0.01, 1.71), (0.012, 0.025, 0.03), HEAD, seg=10, rings=6)  # ears

# ---------------------------------------------------------------- arms
THICK = 1.15
for side, sx in (("L", 1), ("R", -1)):
    sh, el, wr = head_of("UpperArm." + side), head_of("LowerArm." + side), head_of("Hand." + side)
    ua, la = "UpperArm." + side, "LowerArm." + side
    # Deltoid and upper arm.
    loft([(sh + Vector((0, 0, 0.02)), 0.05, 0.05), (sh.lerp(el, 0.18), 0.062, 0.058), (sh.lerp(el, 0.5), 0.056, 0.052),
          (sh.lerp(el, 0.85), 0.046, 0.044), (el, 0.042, 0.042)], along(sh, el, ua, la), n=14)
    # Forearm with a bracer from mid-forearm to the wrist (flared cuff).
    loft([(el, 0.043, 0.043), (el.lerp(wr, 0.3), 0.046, 0.042), (wr, 0.034, 0.032)], along(el, wr, la, "Hand." + side), n=14)
    loft([(el.lerp(wr, 0.28), 0.056, 0.052), (el.lerp(wr, 0.36), 0.05, 0.047), (el.lerp(wr, 0.85), 0.043, 0.04),
          (el.lerp(wr, 0.96), 0.049, 0.046)], W(la), n=14, square=0.3)
    # Gauntleted fist.
    hand = head_of("Hand." + side)
    blob(hand + Vector((sx * 0.004, -0.008, -0.045)), (0.038, 0.048, 0.058), W("Hand." + side), seg=12, rings=8)
    blob(hand + Vector((sx * -0.018, -0.03, -0.04)), (0.014, 0.016, 0.03), W("Hand." + side), seg=8, rings=6)  # thumb
    # Stacked pauldron: three overlapping plates, the top one biggest, each a little lower and further out.
    for k, (r, d) in enumerate(((0.112, 0.038), (0.1, 0.034), (0.088, 0.03))):
        out = Vector((sx * (0.55 + 0.3 * k), 0.0, 1.0 - 0.25 * k)).normalized()
        c = sh + Vector((sx * (0.0 + 0.022 * k), 0.005, 0.035 - 0.045 * k))
        w = (lambda b: (lambda p, s: {b: 1.0}))(ua) if k else (lambda p, s, a=ua, sd=side: {a: 0.6, "Shoulder." + sd: 0.4})
        dome(c, out, r, d, w, squash=0.9, up=(0, 1, 0))

# ---------------------------------------------------------------- legs
for side, sx in (("L", 1), ("R", -1)):
    hp, kn, an = head_of("UpperLeg." + side), head_of("LowerLeg." + side), head_of("Foot." + side)
    ul, ll, ft = "UpperLeg." + side, "LowerLeg." + side, "Foot." + side
    thigh = lambda p, s, a=hp, b=kn, ul=ul, ll=ll: (
        {"Hips": 0.35, ul: 0.65} if p.z > a.z + 0.02 else along(a, b, ul, ll)(p, s))
    loft([(hp + Vector((0, 0, 0.04)), 0.088, 0.09), (hp.lerp(kn, 0.3), 0.084, 0.085), (hp.lerp(kn, 0.7), 0.068, 0.07),
          (kn, 0.054, 0.056)], thigh, n=16)
    # Short breeches: a looser tube over the thigh, ending above the knee in a hem.
    loft([(hp + Vector((0, 0, 0.05)), 0.097, 0.1), (hp.lerp(kn, 0.3), 0.096, 0.095), (hp.lerp(kn, 0.5), 0.088, 0.086),
          (hp.lerp(kn, 0.54), 0.093, 0.091)], thigh, n=16)
    # Calf with the muscle at the back, then the tall boot.
    calf = along(kn, an, ll, ft, blend_from=0.9)
    loft([(kn, 0.052, 0.054), (kn.lerp(an, 0.3), 0.056, 0.06, {"back": 1.2}), (kn.lerp(an, 0.6), 0.046, 0.046),
          (an, 0.038, 0.04)], calf, n=16)
    loft([(kn.lerp(an, 0.36), 0.068, 0.07), (kn.lerp(an, 0.42), 0.064, 0.066), (kn.lerp(an, 0.8), 0.052, 0.054),
          (an + Vector((0, 0, -0.01)), 0.05, 0.056)], calf, n=16, square=0.2)
    # Knee guard: a plate over the kneecap with a smaller one over the shin top.
    dome(kn + Vector((0, -0.045, 0.0)), (0, -1, 0.15), 0.06, 0.035, W(ll), squash=1.1, up=(0, 0, 1))
    dome(kn + Vector((0, -0.04, -0.07)), (0, -1, 0.05), 0.045, 0.025, W(ll), squash=1.3, up=(0, 0, 1))
    # Boot foot: heel to toe.
    loft([(an + Vector((0, 0.05, -0.03)), 0.042, 0.03), (an + Vector((0, 0.0, -0.045)), 0.05, 0.045),
          (an + Vector((0, -0.09, -0.06)), 0.046, 0.032), (an + Vector((0, -0.17, -0.07)), 0.036, 0.02),
          (an + Vector((0, -0.2, -0.075)), 0.02, 0.012)], along(an + Vector((0, 0.05, 0)), an + Vector((0, -0.2, 0)), ft, None),
         n=12)
    box(an + Vector((0, -0.06, -0.085)), (0.1, 0.27, 0.02), W(ft))  # sole

THICK = 1.0
# ---------------------------------------------------------------- hair: rebuilt chains, swept back by the wind
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
_win = bpy.context.window_manager.windows[0]
_ctx = dict(window=_win, screen=_win.screen, active_object=rig, object=rig, selected_objects=[rig])
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='EDIT')
eb = rig.data.edit_bones
for b in [b for b in eb if b.name.startswith("Hair.")]:
    eb.remove(b)
head_bone = eb["Head"]
SK = Vector((0, 0.005, 1.725))
SKR = (0.083, 0.098, 0.105)
# (azimuth deg: 0 = straight back, + towards his left; elevation deg on the skull; length; width; rise deg; spread)
LOCKS = [  # a shaggy mop blown back: short jagged spikes, the longest streaming behind
    (0, 80, 0.17, 0.06, 32, 0.0), (28, 74, 0.16, 0.055, 26, 0.3), (-28, 74, 0.16, 0.055, 26, -0.3),
    (0, 62, 0.22, 0.065, 12, 0.0), (40, 58, 0.19, 0.055, 10, 0.5), (-40, 58, 0.19, 0.055, 10, -0.5),
    (12, 42, 0.24, 0.06, 0, 0.15), (-12, 42, 0.24, 0.06, 0, -0.15), (30, 30, 0.2, 0.05, -8, 0.35), (-30, 30, 0.2, 0.05, -8, -0.35),
    (0, 22, 0.17, 0.06, -22, 0.0), (65, 38, 0.15, 0.045, 0, 0.7), (-65, 38, 0.15, 0.045, 0, -0.7),
    (95, 50, 0.13, 0.04, 8, 0.8), (-95, 50, 0.13, 0.04, 8, -0.8), (100, 25, 0.11, 0.035, -30, 0.6), (-100, 25, 0.11, 0.035, -30, -0.6),
    (150, 66, 0.15, 0.05, 48, 0.2), (-150, 66, 0.15, 0.05, 48, -0.2), (175, 58, 0.13, 0.045, 58, 0.05), (-175, 58, 0.13, 0.045, 58, -0.05),
    (125, 78, 0.16, 0.05, 40, 0.35), (-125, 78, 0.16, 0.05, 40, -0.35), (60, 82, 0.15, 0.05, 34, 0.4), (-60, 82, 0.15, 0.05, 34, -0.4),
]
rnd = random.Random(7)
chains = []
for i, (az, el, length, width, rise, spread) in enumerate(LOCKS):
    a, e = math.radians(az), math.radians(el)
    root = SK + Vector((math.sin(a) * math.cos(e) * SKR[0], math.cos(a) * math.cos(e) * SKR[1], math.sin(e) * SKR[2])) * 0.9
    pts = [root]
    heading = Vector((spread, 1.0, 0)).normalized()  # streaming back (+y), fanned to the side
    pitch = math.radians(rise)
    for k in range(3):
        pitch += math.radians(rnd.uniform(2, 9))  # the wind lifts the ends
        d = Vector((heading.x * math.cos(pitch), heading.y * math.cos(pitch), math.sin(pitch)))
        pts.append(pts[-1] + d * (length / 3) * (1.0 - 0.08 * k))
    parent = head_bone
    names = []
    for k in range(3):
        b = eb.new("Hair.%d.%d" % (i, k))
        b.head, b.tail = pts[k], pts[k + 1]
        b.parent = parent
        b.use_connect = k > 0
        parent = b
        names.append(b.name)
    chains.append((pts, width, names))
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='OBJECT')

# A cap of hair over the skull (the locks grow out of it), slightly ragged at the back.
blob(SK + Vector((0, 0.022, 0.016)), (0.094, 0.118, 0.106), HEAD, seg=20, rings=12)
for pts, width, names in chains:
    def hair_w(p, s, pts=pts, names=names):
        best, bk = 1e9, 0
        for k in range(3):
            a, b = pts[k], pts[k + 1]
            d = b - a
            t = max(0.0, min(1.0, (p - a).dot(d) / d.length_squared))
            dist = (a + d * t - p).length
            if dist < best:
                best, bk = dist, k
        return {names[bk]: 1.0}
    secs = []
    for j in range(9):
        t = j / 8
        seg = min(int(t * 3), 2)
        lt = t * 3 - seg
        p = pts[seg].lerp(pts[seg + 1], lt)
        taper = (1 - t) ** 0.8
        secs.append((p, max(width * taper, 0.002), max(width * 0.55 * taper, 0.0015)))
    loft(secs, hair_w, n=8)

# ---------------------------------------------------------------- mesh object
old = bpy.data.objects.get("Hero")
if old is not None:
    bpy.data.objects.remove(old, do_unlink=True)
me = bpy.data.meshes.new("Hero")
me.from_pydata([tuple(v) for v in verts], [], faces)
me.update()
ink = bpy.data.materials.get("HeroInk") or bpy.data.materials.new("HeroInk")
crimson = bpy.data.materials.get("HeroCrimson") or bpy.data.materials.new("HeroCrimson")
crimson.diffuse_color = (0.7, 0.02, 0.035, 1)
me.materials.append(ink)
me.materials.append(crimson)
for poly, m in zip(me.polygons, fmat):
    poly.material_index = m
ob = bpy.data.objects.new("Hero", me)
bpy.context.scene.collection.objects.link(ob)
ob.parent = rig
groups = {}
for vi, w in enumerate(vweights):
    tot = sum(w.values())
    for bone, x in w.items():
        g = groups.get(bone) or ob.vertex_groups.new(name=bone)
        groups[bone] = g
        g.add([vi], x / tot, 'REPLACE')
mod = ob.modifiers.new("Armature", 'ARMATURE')
mod.object = rig

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\Hero.blend")
result = {"verts": len(verts), "faces": len(faces), "hair_chains": len(chains),
          "dims": [round(x, 3) for x in ob.dimensions]}

# ---------------------------------------------------------------- export to the game
if globals().get("EXPORT", True):
    for o in bpy.context.scene.objects:
        o.select_set(o in (rig, ob))
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=rig, selected_objects=[rig, ob]):
      bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Hero\Hero.fbx", use_selection=True,
                             object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                             secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
