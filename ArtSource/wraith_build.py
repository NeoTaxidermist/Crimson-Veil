# Builds the ink wraith (Wraith.blend -> Assets/_CrimsonVeil/Art/Enemy/Wraith.fbx), rebuilt to stand beside the hero
# (v0.96): not a bundle of tubes in a sheet but a starved body under a torn shroud - a skull in the hood (sockets the
# eyes burn in, cheekbones, the jaw), a long neck of vertebrae, a ribcage with every rib, collarbones and a sunken belly,
# the spine and shoulder blades behind; long bony arms bare from the elbow (knobbed elbow, the two bones of the forearm,
# a knuckled wrist), hands of three long jointed claws and a thumb. Over it a deep hood, a mantle open at the chest, a
# shroud bound at the waist by a cord with a crimson rag, torn below into strips; sleeves torn off at the elbow.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\wraith_build.py").read())
# Blender space: Z up, the wraith faces -Y, its left is +X (as the hero). Bone names keep the hero's for the body
# (EnemyAnimator poses Spine / Chest / Head / arms by name); claws are Finger.<side>.<i>.<k> (HeroHands), the strips
# Tatter.<n>.<k> (SpringBones, prefix "Tatter."). The cloth gets a thickness and the whole is subdivided once.
import math
import bpy
from mathutils import Vector, noise

ROOT = r"C:\Dev\CrimsonVeil"
DECIMATE_RATIO = globals().get("DECIMATE_RATIO", 0.4)
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
    return rings


def ring_loft(rows, weight, mat=INK, n=48, disp=None, arc=None, closed_top=False, cap_end=False, keep=None):
    """A body of horizontal rings round a leaning axis: rows [(z, rx, ry, cy)] (half width, half depth, centre y).
    Ring angle th: 0 = left (+x), pi/2 = the very front (-y). disp(z, th, p, out) -> offset. arc=(a0, a1): only that
    part of the ring (an open sheet, e.g. a mantle open in front)."""
    rings, angles = [], []
    span = arc if arc else (0.0, 2 * math.pi)
    cnt = n if not arc else n + 1
    for (z, rx, ry, cy) in rows:
        ring = []
        for k in range(cnt):
            th = span[0] + (span[1] - span[0]) * k / n
            if len(rings) == 0:
                angles.append(th)
            out = V(math.cos(th) * rx, -math.sin(th) * ry, 0)
            p = V(0, cy, z) + out
            o = V(math.cos(th) / max(rx, 1e-4), -math.sin(th) / max(ry, 1e-4), 0).normalized()
            if disp is not None:
                p = p + disp(z, th, p, o)
            ring.append(add_vert(p, weight(p, 0)))
        rings.append(ring)
    for i in range(len(rows) - 1):
        for k in range(n):
            k1 = (k + 1) % cnt if not arc else k + 1
            if keep is not None and not keep((rows[i][0] + rows[i + 1][0]) / 2, (angles[k] + angles[k1 % len(angles)]) / 2 if k1 < len(angles) else angles[k]):
                continue
            faces.append((rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k])); fmat.append(mat)
    if closed_top:
        ring = rings[0]
        cen = sum((verts[j] for j in ring), Vector()) / len(ring)
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[(k + 1) % n], ring[k])); fmat.append(mat)
    if cap_end:
        ring = rings[-1]
        cen = sum((verts[j] for j in ring), Vector()) / len(ring)
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[k], ring[(k + 1) % n])); fmat.append(mat)
    return rings


def dense(keys, steps):
    """Key rows [(z, rx, ry, cy)] interpolated into `steps` rows between each pair."""
    out = []
    for j in range(len(keys) - 1):
        for s in range(steps):
            u = s / steps
            u = u * u * (3 - 2 * u) * 0.5 + u * 0.5      # (eased a little: no kinks at the keys)
            out.append(tuple(keys[j][q] + (keys[j + 1][q] - keys[j][q]) * u for q in range(4)))
    out.append(keys[-1])
    return out


def blob(c, r, weight, mat=INK, seg=12, rings=8, scale=(1, 1, 1), disp=None):
    """A sphere (scaled); disp(d) -> radial factor for its unit direction d (x lateral, y back, z up)."""
    c = Vector(c)

    def at(d):
        f = 1.0 + (disp(d) if disp else 0.0)
        return c + V(r * scale[0] * d.x, r * scale[1] * d.y, r * scale[2] * d.z) * f
    top = add_vert(at(V(0, 0, 1)), weight(c, 0))
    idx = []
    for i in range(1, rings):
        ph = math.pi * i / rings
        row = []
        for k in range(seg):
            th = 2 * math.pi * k / seg
            d = V(math.sin(ph) * math.cos(th), math.sin(ph) * math.sin(th), math.cos(ph))
            p = at(d)
            row.append(add_vert(p, weight(p, 0)))
        idx.append(row)
    bot = add_vert(at(V(0, 0, -1)), weight(c, 0))
    for k in range(seg):
        faces.append((top, idx[0][(k + 1) % seg], idx[0][k])); fmat.append(mat)
        faces.append((bot, idx[-1][k], idx[-1][(k + 1) % seg])); fmat.append(mat)
    for i in range(len(idx) - 1):
        for k in range(seg):
            faces.append((idx[i][k], idx[i][(k + 1) % seg], idx[i + 1][(k + 1) % seg], idx[i + 1][k])); fmat.append(mat)


def tube_loop(points, r, weight, mat=INK, n=10, normal=V(0, 0, 1)):
    """A tube round a closed path (a cord, a rolled hem): one smooth piece, no beads. `normal`: the path's plane's
    normal, so the rings keep one orientation all round (a frame that flipped pinched the rim at the top)."""
    m = len(points)
    rings = []
    for i, c in enumerate(points):
        t = (points[(i + 1) % m] - points[i - 1]).normalized()
        u = t.cross(normal).normalized()
        v = t.cross(u).normalized()
        rr = r(i) if callable(r) else r
        rings.append([add_vert(c + u * (rr * math.cos(2 * math.pi * k / n)) + v * (rr * math.sin(2 * math.pi * k / n)), weight(c, 0)) for k in range(n)])
    for i in range(m):
        a, b = rings[i], rings[(i + 1) % m]
        for k in range(n):
            faces.append((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k])); fmat.append(mat)


def W(b):
    return lambda p, s: {b: 1.0}


def g2(x, w):
    return math.exp(-(x / w) ** 2)


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


def body_w(p, s):
    """The trunk: hips, spine, chest by height; the shoulders' edges with the shoulder bones; the neck above."""
    z = p.z
    if z > 1.5:
        return {"Chest": 0.4, "Neck": 0.6}
    if z > 1.36 and abs(p.x) > 0.11:
        sb = "Shoulder.L" if p.x > 0 else "Shoulder.R"
        t = min(1.0, (abs(p.x) - 0.11) / 0.06)
        return {"Chest": 1 - 0.7 * t, sb: 0.7 * t}
    if z > 1.15:
        return {"Chest": 1.0}
    if z > 0.95:
        t = (z - 0.95) / 0.2
        return {"Spine": 1 - t, "Chest": t}
    if z > 0.75:
        t = (z - 0.75) / 0.2
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


# ---------------------------------------------------------------- the starved body
# The trunk, leaning forward with the spine: (z, half width, half depth, centre y). Gaunt: a narrow waist under a
# ribcage that flares, the shoulders sharp and high.
TRUNK = [(0.92, 0.11, 0.09, -0.02), (1.00, 0.09, 0.075, -0.03), (1.06, 0.083, 0.068, -0.04), (1.12, 0.118, 0.098, -0.055),
         (1.19, 0.132, 0.112, -0.07), (1.27, 0.145, 0.12, -0.085), (1.35, 0.155, 0.12, -0.10), (1.42, 0.168, 0.108, -0.115),
         (1.465, 0.17, 0.094, -0.128), (1.50, 0.09, 0.062, -0.14), (1.53, 0.05, 0.045, -0.148)]
RIBS = [1.405, 1.365, 1.325, 1.285, 1.245, 1.205, 1.165, 1.13]


def trunk_disp(z, th, p, out):
    fy = math.sin(th)                    # +1 at the very front, -1 at the back
    side = math.cos(th)                  # +1 left, -1 right
    d = 0.0
    # ribs: thin arcs sloping down towards the front, standing out at the sides and the front, sunk into the back
    # under the muscle there; the lower ones stop short of the sternum (the costal arch opens under it)
    front = max(0.0, fy + 0.35) / 1.35
    for n, zr in enumerate(RIBS):
        zc = zr - 0.075 * front ** 1.3 - 0.01 * n * front
        if fy > 0.45 and abs(side) < 0.05 + 0.04 * max(0, n - 2):
            continue
        k = 0.012 * front ** 0.7 * (1.0 - 0.06 * n) * (0.85 + 0.3 * noise.noise(Vector((n * 1.3, th * 0.7, 0.4))))
        d += k * g2(z - zc, 0.0075) - 0.45 * k * g2(z - zc - 0.019, 0.0085)
    d *= 1.0 if z > 1.08 else 0.0
    # the sternum down the middle of the chest, the collarbones across the top, the hollows over them
    d += 0.008 * g2(side, 0.07) * max(0.0, fy) * g2(z - 1.3, 0.09)
    for sx in (1, -1):
        cl = g2(z - (1.45 - 0.012 * abs(side)), 0.011) * max(0.0, fy) * (0.25 < sx * side + 1 < 1.95)
        d += 0.011 * cl
    d -= 0.012 * g2(z - 1.478, 0.012) * max(0.0, fy) * g2(abs(side) - 0.5, 0.25)
    # the belly sunk in under the ribs; the hip bones jutting at the waist
    d -= 0.022 * g2(z - 1.035, 0.045) * max(0.0, fy) ** 1.2
    d += 0.012 * g2(z - 0.97, 0.03) * g2(abs(side) - 0.75, 0.2)
    # behind: the spine's knuckles, the shoulder blades standing out
    back = max(0.0, -fy)
    d += 0.008 * g2(side, 0.09) * back * (0.5 + 0.5 * math.cos(2 * math.pi * z / 0.036))
    for sx in (1, -1):
        d += 0.014 * g2(z - 1.36, 0.06) * g2(side - 0.45 * sx, 0.22) * back
    return out * d


ring_loft(dense(TRUNK, 6), body_w, n=56, disp=trunk_disp)

# the neck: long, the windpipe in front and the vertebrae behind, sinews either side
NECK = [(1.50, 0.048, 0.044), (1.55, 0.04, 0.038), (1.60, 0.036, 0.035), (1.65, 0.038, 0.038), (1.69, 0.042, 0.042)]
_nk = [head_of("Neck").lerp(tail_of("Neck") + V(0, 0, 0.05), (z - 1.50) / 0.19) for z, _, _ in NECK]


def neck_disp(i, k, p, c):
    th = 2 * math.pi * k / 24
    r = (p - c).normalized()
    fy, side = math.sin(th), math.cos(th)
    return r * (0.006 * g2(side, 0.25) * max(0.0, fy) + 0.006 * g2(abs(side) - 0.6, 0.18) + 0.005 * g2(side, 0.2) * max(0.0, -fy) * (0.5 + 0.5 * math.cos(i * 2.6)))


loft([(c, rx, ry) for c, (_, rx, ry) in zip(_nk, NECK)], lambda p, s: {"Chest": 0.3, "Neck": 0.7} if s < 0.3 else ({"Neck": 0.5, "Head": 0.5} if s > 0.8 else {"Neck": 1.0}),
     n=24, disp=neck_disp, cap0=False)

# ---------------------------------------------------------------- the hood (built before the skull: the skull is set in it)
HOOD_KEYS = [(1.44, 0.205, 0.17, -0.10), (1.50, 0.18, 0.172, -0.12), (1.57, 0.156, 0.172, -0.15), (1.64, 0.146, 0.176, -0.18),
             (1.71, 0.143, 0.178, -0.20), (1.77, 0.136, 0.172, -0.215), (1.82, 0.12, 0.155, -0.226), (1.855, 0.092, 0.124, -0.236),
             (1.876, 0.056, 0.078, -0.246), (1.886, 0.02, 0.03, -0.252)]
HOOD = dense(HOOD_KEYS, 3)


def hood_w(p, s):
    if p.z < 1.58:
        return {"Chest": 0.5, "Neck": 0.5}
    if p.z < 1.66:
        return {"Neck": 0.5, "Head": 0.5}
    return {"Head": 1.0}


FACE_Z = 1.715


def cowl(z, th, p, out):
    fy = math.sin(th)
    side = math.cos(th)
    # the opening: a deep hollow, wide and tall as a face - the cloth turned back into it
    face = g2(z - FACE_Z, 0.105) * max(0.0, fy) ** 1.6 * g2(side, 0.6)   # (round the opening the cloth turns inwards)
    rim = g2(z - 1.835, 0.05) * max(0.0, fy) ** 2           # the peak drooping over the brow
    back = g2(z - 1.52, 0.06) * max(0.0, -fy) * math.sin(7 * th)
    crease = noise.noise(Vector((side * 2.5, fy * 2.5, z * 9.0))) + 1.6 * g2(z - 1.62, 0.12) * math.sin(5 * th + 0.6 * noise.noise(Vector((th, z * 3, 1.0)))) * (1 - max(0.0, fy))
    return V(0, 0.06 * face - 0.025 * rim, -0.02 * rim) + out * (0.012 * back + 0.007 * crease * (1 - face))


_hood0 = len(verts)
def hood_open(z, th):
    """The face's opening: an oval in the front of the hood."""
    side, fy = math.cos(th), math.sin(th)
    return not (fy > 0.55 and ((z - FACE_Z) / 0.118) ** 2 + (side / 0.62) ** 2 < 1.0)


ring_loft([(z, rx, ry, cy) for z, rx, ry, cy in HOOD], hood_w, n=48, disp=cowl, keep=hood_open, cap_end=True)
_hood = verts[_hood0:]


def hood_front(x, z):
    """The hood's surface at (x, z), the foremost point there (measured, not guessed)."""
    near = [v.y for v in _hood if abs(v.x - x) < 0.03 and abs(v.z - z) < 0.04]
    return min(near) if near else -0.4


def opening_edge(x, z):
    """Where the opening's edge is (front of the hood near (x, z))."""
    near = [v for v in _hood if abs(v.x - x) < 0.025 and abs(v.z - z) < 0.03]
    return min(v.y for v in near) if near else -0.38


# The opening's edge rolled thick, heaviest over the brow: one smooth loop round the face.
_rim = []
for k in range(64):
    a = 2 * math.pi * k / 64
    x, z = 0.098 * math.sin(a), FACE_Z + 0.118 * math.cos(a)
    _rim.append(V(x, opening_edge(x, z) + 0.006, z))
_ys = [v.y for v in _rim]
for _ in range(6):
    _ys = [(_ys[i - 2] + 2 * _ys[i - 1] + 3 * _ys[i] + 2 * _ys[(i + 1) % 64] + _ys[(i + 2) % 64]) / 9 for i in range(64)]
_rim = [V(v.x, y, v.z) for v, y in zip(_rim, _ys)]
tube_loop(_rim, lambda i: 0.011 + 0.009 * max(0.0, math.cos(2 * math.pi * i / 64)), hood_w, n=12, normal=V(0, 1, 0))

# ---------------------------------------------------------------- the skull in the hood
# One closed surface, shaped: the cranium long and high, the face flattened, the sockets deep under a heavy brow, the
# cheekbones sharp over hollow cheeks, a nose's hole, the jaw narrow and hanging a little open, the temples sunk.
_front = sum(opening_edge(x, FACE_Z) for x in (-0.09, 0.09)) / 2      # (the opening's edge either side of the face)
SK_R = (0.06, 0.08, 0.084)
SK_C = V(0, _front + 0.035 + SK_R[1], FACE_Z - 0.006)
EYE_UP, EYE_LAT = 0.12, 0.37


def skull(d):
    fwd = max(0.0, -d.y)                  # +1 at the face
    lat, up = d.x, d.z
    f = 0.0
    f -= 0.22 * fwd ** 3                                                   # the face flat, the cranium round
    for sx in (1, -1):
        sock = g2(lat - EYE_LAT * sx, 0.16) * g2(up - EYE_UP, 0.15) * fwd
        f -= 0.55 * sock                                                   # the sockets, deep
        f += 0.16 * g2(lat - 0.6 * sx, 0.14) * g2(up + 0.12, 0.1) * fwd   # the cheekbones, sharp
        f -= 0.16 * g2(lat - 0.5 * sx, 0.16) * g2(up + 0.4, 0.13) * fwd   # the cheeks sunk under them
        f -= 0.12 * g2(lat - 0.9 * sx, 0.16) * g2(up - 0.18, 0.2)         # the temples
        f += 0.05 * g2(lat - 0.62 * sx, 0.1) * g2(up + 0.72, 0.08) * fwd  # the corners of the jaw
    f += 0.12 * g2(up - EYE_UP - 0.16, 0.06) * fwd * g2(lat, 0.62)         # the brow ridge over them
    f -= 0.26 * g2(lat, 0.075) * g2(up + 0.12, 0.085) * fwd               # the nose's hole
    f += 0.07 * g2(lat, 0.06) * g2(up - 0.0, 0.06) * fwd                   # the bridge between the sockets
    mouth = g2(up + 0.58, 0.035) * fwd * g2(lat, 0.42)
    f -= 0.16 * mouth                                                      # the jaw hanging a little open
    teeth = g2(up + 0.5, 0.045) * fwd * g2(lat, 0.38) + g2(up + 0.66, 0.04) * fwd * g2(lat, 0.34)
    f += 0.035 * teeth * (math.cos(lat * 42) ** 2)                         # the teeth, a row above and below
    if up < -0.5:
        f -= 0.32 * (-0.5 - up) * (1 - fwd)                               # the jaw narrow, the chin forward
    return f


blob(SK_C, 1.0, W("Head"), seg=56, rings=40, scale=SK_R, disp=skull)
for sx in (1, -1):
    # the eyes burn deep in the sockets (on the socket's floor, not on the face)
    d = V(EYE_LAT * sx, -0.92, EYE_UP).normalized()
    e = SK_C + V(SK_R[0] * d.x, SK_R[1] * d.y, SK_R[2] * d.z) * (1.0 - 0.42)
    blob(e, 0.016, W("Head"), mat=EYES, seg=14, rings=10, scale=(1.15, 0.8, 0.8))

# ---------------------------------------------------------------- arms: bare bone from the elbow
for side, sx in (("L", 1.0), ("R", -1.0)):
    sh, ua, la, ha = "Shoulder." + side, "UpperArm." + side, "LowerArm." + side, "Hand." + side
    # the shoulder's point and the upper arm, thin, the bone showing at both ends
    blob(tail_of(sh) + V(-0.012 * sx, 0, 0.0), 0.034, W(ua), seg=16, rings=12, scale=(1.0, 1.0, 0.85))   # (under the mantle)
    a, b = head_of(ua), tail_of(ua)
    loft([(a.lerp(b, u), r, r * 0.95) for u, r in ((0.0, 0.046), (0.2, 0.042), (0.5, 0.036), (0.8, 0.032), (1.0, 0.036))],
         along(a, b, ua, la, 0.8), n=20)
    # the elbow: the knob of the bone behind, sharp
    blob(b + V(0, 0.015, 0), 0.036, lambda p, s, ua=ua, la=la: {ua: 0.5, la: 0.5}, seg=16, rings=12, scale=(0.9, 1.2, 1.0))
    # the forearm: two bones under thin skin - a ridge down its back, the wrist knuckled and narrow
    a, b = head_of(la), tail_of(la)

    def fore(i, k, p, c, sx=sx):
        th = 2 * math.pi * k / 20
        r = (p - c).normalized()
        return r * (0.004 * math.cos(2 * th) + 0.004 * g2(math.sin(th) + 1, 0.4) * (1 - i / 6))
    loft([(a.lerp(b, u), r, r * 0.85) for u, r in ((0.0, 0.034), (0.15, 0.036), (0.4, 0.031), (0.7, 0.025), (0.9, 0.021), (1.0, 0.023))],
         along(a, b, la, ha, 0.9), n=20, disp=fore)
    blob(b + V(0.006 * sx, 0.004, 0), 0.014, W(la), seg=12, rings=8)             # the knob of the wrist
    # the sleeve: torn off above the elbow, hanging loose and ragged round the upper arm
    a, b = head_of(ua), tail_of(ua)

    def sleeve(i, k, p, c):
        th = 2 * math.pi * k / 32
        r = (p - c)
        crease = 0.12 * math.sin(6 * th + i) * (i / 4.0)
        tear = 0.0
        if i == 4:
            tear = 0.05 + 0.07 * (0.5 + 0.5 * noise.noise(Vector((k * 0.6, 7.0 * sx, 0.0)))) + 0.05 * max(0.0, math.sin(th))
        return r * crease + V(0, 0, -tear)
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.07), (0.25, 0.068), (0.5, 0.07), (0.7, 0.078), (0.82, 0.085))],
         along(a, b, ua, la, 0.85), n=32, cap1=False, disp=sleeve)

    # the hand: a narrow bony palm, three long claws, each of three joints knuckled, and a thumb-claw
    a, b = head_of(ha), tail_of(ha)
    d = (b - a).normalized()

    def palm(i, k, p, c):
        th = 2 * math.pi * k / 16
        return (p - c).normalized() * 0.003 * math.cos(3 * th)              # the tendons over its back
    loft([(a.lerp(b, u), 0.016, r) for u, r in ((0.0, 0.02), (0.5, 0.03), (1.0, 0.029))], W(ha), n=16, disp=palm)
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
            for u in ((0.0, 0.2, 0.5, 0.8, 1.0) if k < 2 else (0.0, 0.2, 0.5, 0.8, 1.0)):
                r0 = 0.0105 * (1.0 - 0.25 * k)
                knuckle = 1.0 + 0.32 * g2(u, 0.13) + (0.25 * g2(u - 1.0, 0.12) if k < 2 else 0.0)
                taper = (1.0 - 0.9 * u ** 1.3) if k == 2 else 1.0                  # the last joint a hooked claw
                r = max(0.0012, r0 * knuckle * taper)
                secs.append((pts[k].lerp(pts[k + 1], u), r, r * 0.9))
                sw.append({ha: 0.5, "%s.%d" % (name, k): 0.5} if (k == 0 and u == 0.0) else {"%s.%d" % (name, k): 1.0})
        loft(secs, lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))], n=10)
    tb = a + d * 0.05 + fwd * 0.03
    tdir = (d * 0.6 + fwd * 0.7 + palm_in * 0.2).normalized()
    t1 = tb + tdir * 0.05
    t2 = t1 + (tdir + palm_in * 0.4).normalized() * 0.05
    bone("Thumb.%s.0" % side, tuple(tb), tuple(t1), ha)
    bone("Thumb.%s.1" % side, tuple(t1), tuple(t2), "Thumb.%s.0" % side)
    loft([(tb, 0.011, 0.011), (tb.lerp(t1, 0.5), 0.009, 0.009), (t1, 0.011, 0.011), (t1.lerp(t2, 0.6), 0.006, 0.006), (t2, 0.0015, 0.0015)],
         lambda p, s, side=side: {"Thumb.%s.0" % side: 1.0} if s < 0.5 else {"Thumb.%s.1" % side: 1.0}, n=10)

# ---------------------------------------------------------------- the mantle: over the shoulders, open at the chest


def folds(n_folds, amp, z_top, z_full, seed):
    """Folds deepening downwards, each its own depth and a little off its place (cloth, not a lampshade)."""
    def f(z, th, p, out):
        g = max(0.0, min(1.0, (z_top - z) / max(1e-3, z_top - z_full)))
        depth = 0.55 + 0.9 * (0.5 + 0.5 * noise.noise(Vector((th * 3.0 + seed, z * 2.0, seed * 0.3))))
        shift = 0.8 * noise.noise(Vector((th * 1.1, seed + 3.1, z * 1.3)))
        return out * (amp * g * depth * math.sin(n_folds * th + shift))
    return f


MANTLE = dense([(1.56, 0.12, 0.095, -0.135), (1.50, 0.215, 0.15, -0.115), (1.43, 0.265, 0.19, -0.09), (1.35, 0.29, 0.21, -0.065),
                (1.27, 0.30, 0.22, -0.045)], 4)
_mf = folds(9, 0.026, 1.48, 1.3, 5.0)


def mantle_disp(z, th, p, out):
    d = _mf(z, th, p, out)
    if z < 1.271:
        d = d + V(0, 0, -0.05 - 0.09 * (0.5 + 0.5 * noise.noise(Vector((th * 4.0, 1.7, 0.0)))) - 0.05 * max(0.0, -math.sin(th)))
    return d


# (the arc leaves the front open: th = pi/2 is the very front; it runs round the back from front-left to front-right)
ring_loft(MANTLE, lambda p, s: {"Chest": 1.0} if abs(p.x) < 0.14 or p.z > 1.45 else {"Chest": 0.6, ("Shoulder.L" if p.x > 0 else "Shoulder.R"): 0.4},
          n=56, disp=mantle_disp, arc=(math.pi / 2 + 0.55, math.pi / 2 + 2 * math.pi - 0.55))

# ---------------------------------------------------------------- the shroud from the waist, the cord, the crimson rag
SHROUD = dense([(1.07, 0.105, 0.088, -0.04), (1.0, 0.13, 0.11, -0.03), (0.9, 0.2, 0.17, 0.0), (0.78, 0.25, 0.21, 0.04),
                (0.66, 0.28, 0.235, 0.075), (0.58, 0.30, 0.25, 0.10)], 5)
_sf = folds(11, 0.042, 1.0, 0.62, 1.0)


def shroud_disp(z, th, p, out):
    d = _sf(z, th, p, out)
    d = d - out * 0.012 * g2(z - 1.02, 0.012)                       # gathered in under the cord
    if z < 0.581:
        tear = 0.5 + 0.5 * noise.noise(Vector((th * 6.0, 4.2, 0.0)))
        d = d + V(0, 0, -0.14 * tear - 0.03 * math.sin(th * 9))
    return d


ring_loft(SHROUD, body_w, n=56, disp=shroud_disp)
_cord = [V(0.112 * math.cos(2 * math.pi * k / 48), -0.04 - 0.094 * math.sin(2 * math.pi * k / 48), 1.02 + 0.006 * math.sin(4 * math.pi * k / 48)) for k in range(48)]
tube_loop(_cord, 0.0085, lambda p, s: {"Spine": 1.0}, n=10)

# ---------------------------------------------------------------- the torn shroud: strips (SpringBones swings them)
STRIPS, STRIP_ANG, STRIP_MAT = [], [], []
for n in range(14):
    ang = math.radians(-172 + n * 26.5 + 9 * noise.noise(Vector((n * 0.9, 0.3, 0.0))))
    STRIP_ANG.append((math.sin(ang), math.cos(ang)))
    top = V(0.26 * math.cos(ang), 0.10 + 0.22 * math.sin(ang), 0.58)
    length = 0.42 + 0.3 * (0.5 + 0.5 * noise.noise(Vector((n * 1.7, 2.0, 0.0))))
    bottom = top + V(0.05 * math.cos(ang), 0.16 + 0.04 * (n % 3), -length)
    STRIPS.append((top, bottom, 0.10 - 0.015 * (n % 3)))
    STRIP_MAT.append(INK)
for sx in (1, -1):
    top = V(0.15 * sx, 0.03, 1.36)
    STRIPS.append((top, top + V(0.05 * sx, 0.30, -0.70), 0.15))
    STRIP_ANG.append((1.0, 0.1))
    STRIP_MAT.append(INK)
# the crimson rag knotted at the cord, hanging at the left hip (the only colour on it but the eyes)
STRIPS.append((V(0.07, -0.115, 1.01), V(0.11, -0.13, 0.62), 0.045))
STRIP_ANG.append((0.9, 0.4))
STRIP_MAT.append(EYES)
for n, (top, bottom, width) in enumerate(STRIPS):
    root = "Hips" if top.z < 0.95 else ("Spine" if top.z < 1.1 else "Chest")
    sag = V(0, -0.04, 0)
    pts = [top.lerp(bottom, k / 4.0) + sag * math.sin(math.pi * k / 4.0) for k in range(5)]
    parent = root
    for k in range(4):
        bone("Tatter.%d.%d" % (n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = "Tatter.%d.%d" % (n, k)
    secs, sw = [], []
    for k in range(4):
        for u in ((0.0, 0.5, 1.0) if k == 0 else (0.5, 1.0)):
            f = (k + u) / 4.0
            w = width * (1.0 - 0.55 * f) * (0.12 if k == 3 and u == 1.0 else 1.0)
            secs.append((pts[k].lerp(pts[k + 1], u), max(0.012, w * abs(STRIP_ANG[n][0])), max(0.012, w * abs(STRIP_ANG[n][1]))))
            sw.append({root: 0.6, "Tatter.%d.0" % n: 0.4} if (k == 0 and u == 0.0) else {"Tatter.%d.%d" % (n, k): 1.0})

    def strip_disp(i, k, p, c, n=n):
        return (p - c) * 0.25 * noise.noise(Vector((i * 0.8, k * 0.9, n * 1.3)))   # (ragged, not a ribbon)
    loft(secs, lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))], mat=STRIP_MAT[n], n=10, disp=strip_disp)

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
me.validate()
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
# Cloth has an inside and an edge (back faces are culled in the game: an open hood was see-through), and the whole is
# subdivided once - smooth, as the hero. Both applied; the weights are carried along.
bpy.context.view_layer.objects.active = ob
for o in bpy.context.scene.objects:
    o.select_set(o == ob)
sol = ob.modifiers.new("Thick", 'SOLIDIFY')
sol.thickness = 0.007
sol.offset = -1.0
sol.use_even_offset = False   # (even offset shot spikes out of the sharp corners round the hood's opening)
sub = ob.modifiers.new("Smooth", 'SUBSURF')
sub.levels = 1
sub.render_levels = 1
# ...then thinned where it is flat: eleven of them haunt the ruins at once (124 000 vertices each was three heroes)
dec = ob.modifiers.new("Thin", 'DECIMATE')
dec.ratio = DECIMATE_RATIO
if not globals().get("RAW", False):
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=ob, object=ob, selected_objects=[ob]):
        bpy.ops.object.modifier_apply(modifier="Thick")
        bpy.ops.object.modifier_apply(modifier="Smooth")
        bpy.ops.object.modifier_apply(modifier="Thin")
mod = ob.modifiers.new("Armature", 'ARMATURE')
mod.object = rig

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\Wraith.blend")
result = {"verts": len(me.vertices), "faces": len(me.polygons), "bones": len(BONES), "dims": [round(x, 3) for x in ob.dimensions]}

if globals().get("EXPORT", True):
    for o in bpy.context.scene.objects:
        o.select_set(o in (rig, ob))
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=rig, selected_objects=[rig, ob]):
        bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Enemy\Wraith.fbx", use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                 bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
