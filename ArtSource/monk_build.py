# Builds the shield monk (ShieldMonk.blend -> Assets/_CrimsonVeil/Art/Enemy/ShieldMonk.fbx), rebuilt to stand beside the
# hero (v0.101): a broad, heavy warrior monk - a deep hood over a weathered face (a heavy brow, deep sockets the crimson
# eyes burn in, a broken nose, a braided beard with a crimson bead), a closed shoulder mantle over a heavy robe falling
# in deep folds to the ground and torn into strips there, a twisted rope belt with crimson cords, wide folded sleeves,
# forearms bound in leather straps, big hands of jointed fingers and a thumb, a ring of crimson beads round the left
# wrist. Apart from the body, as objects the game moves itself: his kite shield ("Shield": planks under an iron rim,
# rivets, a boss, a crimson eye weeping down its face) and his flail ("Flail" the haft, "FlailLink.<n>" the chain's
# links, "FlailHead" the spiked head) - the game puts the flail in whichever hand is free and swings the chain itself.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\monk_build.py").read())
# Blender space: Z up, the monk faces -Y, his left is +X (as the hero). Body bones keep the old names (ShieldMonk.cs
# poses Spine / Chest / Head / arms by name); fingers are Finger.<side>.<i>.<k> and Thumb.<side>.<k> (HeroHands), the
# cloth strips Tatter.<n>.<k> (SpringBones, prefix "Tatter."). The cloth gets a thickness, the whole is subdivided
# once and thinned (as the wraith, wraith_build.py, whose helpers these are).
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
    """sections: [(centre, rx, ry)]; weight(p, s) -> {bone: w}; disp(i, k, p, c) -> offset of ring point k of section i."""
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
    """Horizontal rings round a leaning axis: rows [(z, rx, ry, cy)]. th: 0 = left (+x), pi/2 = the very front (-y).
    disp(z, th, p, out) -> offset. keep(z, th) -> False drops the face there (an opening)."""
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
    out = []
    for j in range(len(keys) - 1):
        for s in range(steps):
            u = s / steps
            u = u * u * (3 - 2 * u) * 0.5 + u * 0.5
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


def folds(n_folds, amp, z_top, z_full, seed):
    """Folds deepening downwards, each its own depth and a little off its place."""
    def f(z, th, p, out):
        g = max(0.0, min(1.0, (z_top - z) / max(1e-3, z_top - z_full)))
        depth = 0.55 + 0.9 * (0.5 + 0.5 * noise.noise(Vector((th * 3.0 + seed, z * 2.0, seed * 0.3))))
        shift = 0.8 * noise.noise(Vector((th * 1.1, seed + 3.1, z * 1.3)))
        return out * (amp * g * depth * math.sin(n_folds * th + shift))
    return f


def piecewise(sw):
    return lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))]


# ---------------------------------------------------------------- the skeleton
BONES = []


def bone(name, head, tail, parent):
    BONES.append((name, V(*head), V(*tail), parent))


bone("Hips", (0, 0.02, 0.85), (0, 0.0, 1.15), None)
bone("Spine", (0, 0.0, 1.15), (0, -0.03, 1.42), "Hips")
bone("Chest", (0, -0.03, 1.42), (0, -0.08, 1.72), "Spine")
bone("Neck", (0, -0.08, 1.72), (0, -0.12, 1.84), "Chest")
bone("Head", (0, -0.12, 1.84), (0, -0.14, 2.08), "Neck")
for side, sx in (("L", 1.0), ("R", -1.0)):
    bone("Shoulder." + side, (0.08 * sx, -0.06, 1.66), (0.27 * sx, -0.05, 1.66), "Chest")
# the left arm forward to the shield's grip; the right hanging, its fist round the flail's haft
bone("UpperArm.L", (0.27, -0.05, 1.66), (0.33, -0.16, 1.36), "Shoulder.L")
bone("LowerArm.L", (0.33, -0.16, 1.36), (0.23, -0.44, 1.22), "UpperArm.L")
bone("Hand.L", (0.23, -0.44, 1.22), (0.19, -0.55, 1.20), "LowerArm.L")
bone("UpperArm.R", (-0.27, -0.05, 1.66), (-0.33, -0.08, 1.34), "Shoulder.R")
bone("LowerArm.R", (-0.33, -0.08, 1.34), (-0.33, -0.20, 1.08), "UpperArm.R")
bone("Hand.R", (-0.33, -0.20, 1.08), (-0.32, -0.25, 0.97), "LowerArm.R")
B = {n: (h, t) for n, h, t, p in BONES}


def head_of(n):
    return B[n][0]


def tail_of(n):
    return B[n][1]


def body_w(p, s):
    z = p.z
    if z > 1.6 and abs(p.x) > 0.13:
        sb = "Shoulder.L" if p.x > 0 else "Shoulder.R"
        t = min(1.0, (abs(p.x) - 0.13) / 0.08)
        return {"Chest": 1 - 0.7 * t, sb: 0.7 * t}
    if z > 1.45:
        return {"Chest": 1.0}
    if z > 1.25:
        t = (z - 1.25) / 0.2
        return {"Spine": 1 - t, "Chest": t}
    if z > 1.05:
        t = (z - 1.05) / 0.2
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


# ---------------------------------------------------------------- the robe: heavy, broad, a bell at the hem
ROBE_KEYS = [(1.745, 0.10, 0.09, -0.09), (1.71, 0.20, 0.15, -0.075), (1.64, 0.285, 0.19, -0.055), (1.52, 0.30, 0.205, -0.035),
             (1.38, 0.285, 0.205, -0.015), (1.24, 0.265, 0.195, 0.0), (1.13, 0.25, 0.185, 0.0), (1.02, 0.285, 0.21, 0.01),
             (0.84, 0.33, 0.24, 0.02), (0.64, 0.37, 0.265, 0.03), (0.46, 0.395, 0.28, 0.04), (0.34, 0.41, 0.29, 0.045)]
_rf = folds(13, 0.05, 1.05, 0.4, 2.0)
_rf_top = folds(7, 0.012, 1.6, 1.3, 7.0)


def robe_disp(z, th, p, out):
    fy, side = math.sin(th), math.cos(th)
    d = _rf(z, th, p, out) + _rf_top(z, th, p, out) * (1.0 if z > 1.2 else 0.0)
    # the chest broad and heavy under the cloth, the shoulders' muscle, the back's breadth
    d = d + out * (0.022 * g2(z - 1.5, 0.09) * max(0.0, fy) * g2(abs(side) - 0.45, 0.35)
                   + 0.018 * g2(z - 1.62, 0.05) * g2(abs(side) - 0.92, 0.2)
                   + 0.015 * g2(z - 1.48, 0.12) * max(0.0, -fy))
    # gathered in under the belt (folds bunched above and below it)
    d = d - out * 0.03 * g2(z - 1.13, 0.03)
    d = d + out * 0.012 * g2(z - 1.07, 0.03) * math.sin(17 * th)
    # the seam down the front, where the robe closes
    d = d - out * 0.006 * g2(side, 0.04) * max(0.0, fy) * (1.0 if z < 1.7 else 0.0)
    if z < 0.341:   # torn at the hem: ragged, uneven
        tear = 0.5 + 0.5 * noise.noise(Vector((th * 5.0, 1.3, 0.0)))
        d = d + V(0, 0, -0.06 * tear - 0.02 * math.sin(th * 11))
    return d


ring_loft(dense(ROBE_KEYS, 5), body_w, n=64, disp=robe_disp)

# the neck under the hood (seen in its shadow): thick, a sinew either side
loft([(V(0, -0.09, 1.70), 0.075, 0.07), (V(0, -0.11, 1.80), 0.068, 0.066), (V(0, -0.13, 1.87), 0.07, 0.07)],
     lambda p, s: {"Chest": 0.4, "Neck": 0.6} if s < 0.3 else ({"Neck": 0.5, "Head": 0.5} if s > 0.8 else {"Neck": 1.0}),
     n=24, cap0=False, cap1=False)

# ---------------------------------------------------------------- the hood: deep, its peak drooping over the brow
# (its peak runs forward and droops over the brow - a cowl, not a pipe)
HOOD_KEYS = [(1.66, 0.25, 0.20, -0.08), (1.72, 0.226, 0.20, -0.10), (1.80, 0.2, 0.205, -0.125), (1.88, 0.186, 0.21, -0.15),
             (1.96, 0.178, 0.212, -0.172), (2.03, 0.168, 0.206, -0.19), (2.08, 0.15, 0.19, -0.205), (2.12, 0.122, 0.162, -0.222),
             (2.15, 0.088, 0.122, -0.245), (2.165, 0.05, 0.075, -0.272), (2.165, 0.018, 0.03, -0.295), (2.155, 0.004, 0.006, -0.305)]
HOOD = dense(HOOD_KEYS, 3)
FACE_Z = 1.94


def hood_w(p, s):
    if p.z < 1.78:
        return {"Chest": 0.5, "Neck": 0.5}
    if p.z < 1.86:
        return {"Neck": 0.5, "Head": 0.5}
    return {"Head": 1.0}


def cowl(z, th, p, out):
    fy, side = math.sin(th), math.cos(th)
    face = g2(z - FACE_Z, 0.12) * max(0.0, fy) ** 1.6 * g2(side, 0.62)
    rim = g2(z - 2.07, 0.05) * max(0.0, fy) ** 2
    back = g2(z - 1.74, 0.07) * max(0.0, -fy) * math.sin(7 * th)
    crease = noise.noise(Vector((side * 2.5, fy * 2.5, z * 9.0))) + 1.6 * g2(z - 1.85, 0.12) * math.sin(5 * th + 0.6 * noise.noise(Vector((th, z * 3, 1.0)))) * (1 - max(0.0, fy))
    return V(0, 0.06 * face - 0.03 * rim, -0.022 * rim) + out * (0.014 * back + 0.008 * crease * (1 - face))


def open_shape(v):
    """The opening's half height above / below the face's centre and its half width (in side) at height offset v:
    taller below than above, narrowing to a point at the top (a pointed arch, as cloth falls from a peak)."""
    return 0.12, 0.15, 0.6 * (1 - 0.55 * max(0.0, v / 0.12) ** 1.5)


def hood_open(z, th):
    side, fy = math.cos(th), math.sin(th)
    v = z - FACE_Z
    up, dn, half = open_shape(v)
    return not (fy > 0.5 and (v / (up if v > 0 else dn)) ** 2 + (side / max(half, 1e-3)) ** 2 < 1.0)


_hood0 = len(verts)
ring_loft(HOOD, hood_w, n=48, disp=cowl, keep=hood_open, cap_end=True)
_hood = verts[_hood0:]


def opening_edge(x, z):
    near = [v for v in _hood if abs(v.x - x) < 0.025 and abs(v.z - z) < 0.03]
    return min(v.y for v in near) if near else -0.4


_rim = []
for k in range(64):
    a = 2 * math.pi * k / 64
    up, dn, _ = open_shape(0.0)
    v = (up if math.cos(a) > 0 else dn) * math.cos(a)
    half = open_shape(v)[2]
    x, z = 0.112 * (half / 0.6) * math.sin(a), FACE_Z + v
    _rim.append(V(x, opening_edge(x, z) + 0.006, z))
_ys = [v.y for v in _rim]
for _ in range(6):
    _ys = [(_ys[i - 2] + 2 * _ys[i - 1] + 3 * _ys[i] + 2 * _ys[(i + 1) % 64] + _ys[(i + 2) % 64]) / 9 for i in range(64)]
_rim = [V(v.x, y, v.z) for v, y in zip(_rim, _ys)]
# the edge turned back: thin and uneven (worn cloth), heaviest where the peak droops over the brow
tube_loop(_rim, lambda i: 0.006 + 0.007 * max(0.0, math.cos(2 * math.pi * i / 64)) ** 2 + 0.003 * (0.5 + 0.5 * noise.noise(Vector((i * 0.4, 2.0, 0.0)))),
          hood_w, n=10, normal=V(0, 1, 0))

# ---------------------------------------------------------------- the face in the hood: weathered, stern
_front = sum(opening_edge(x, FACE_Z) for x in (-0.09, 0.09)) / 2
FC_R = (0.082, 0.095, 0.112)
FC_C = V(0, _front + 0.045 + FC_R[1], FACE_Z - 0.01)
EYE_UP, EYE_LAT = 0.16, 0.36


def face(d):
    fwd = max(0.0, -d.y)
    lat, up = d.x, d.z
    f = -0.12 * fwd ** 3
    for sx in (1, -1):
        f -= 0.3 * g2(lat - EYE_LAT * sx, 0.15) * g2(up - EYE_UP, 0.12) * fwd      # sockets, deep under the brow
        f += 0.12 * g2(lat - 0.58 * sx, 0.14) * g2(up + 0.02, 0.1) * fwd          # cheekbones
        f -= 0.08 * g2(lat - 0.5 * sx, 0.15) * g2(up + 0.32, 0.14) * fwd          # hollow cheeks
        f += 0.05 * g2(lat - 0.7 * sx, 0.12) * g2(up + 0.62, 0.12) * fwd          # the jaw's corners
        f += 0.02 * g2(lat - 0.36 * sx, 0.06) * g2(up - 0.46, 0.05) * fwd         # a scar's ridge over the brow
    f += 0.16 * g2(up - EYE_UP - 0.15, 0.065) * fwd * g2(lat, 0.68)               # the heavy brow
    f -= 0.05 * g2(lat, 0.08) * g2(up - EYE_UP - 0.12, 0.05) * fwd                # a frown's furrow between
    nose = g2(lat, 0.11) * fwd ** 2
    f += 0.32 * nose * g2(up + 0.05, 0.16) + 0.08 * nose * g2(up - 0.12, 0.08)    # the nose, broad, broken high
    f += 0.03 * g2(lat - 0.05, 0.04) * g2(up - 0.05, 0.04) * fwd                  # the break's knot
    f -= 0.07 * g2(up + 0.42, 0.025) * fwd * g2(lat, 0.3)                         # the mouth, a hard line
    f += 0.06 * g2(up + 0.66, 0.1) * fwd * g2(lat, 0.3)                           # the chin
    return f


blob(FC_C, 1.0, W("Head"), seg=56, rings=40, scale=FC_R, disp=face)
for sx in (1, -1):
    d = V(EYE_LAT * sx, -0.94, EYE_UP).normalized()
    e = FC_C + V(FC_R[0] * d.x, FC_R[1] * d.y, FC_R[2] * d.z) * (1.0 - 0.2)
    blob(e, 0.014, W("Head"), mat=EYES, seg=14, rings=10, scale=(1.25, 0.7, 0.75))

# the braided beard: from the jaw down onto the chest, a crimson bead binding its end
_chin = FC_C + V(0, -FC_R[1] * 0.75, -FC_R[2] * 0.72)
_beard = [_chin + V(0, 0.012 * u, -0.2 * u) + V(0, -0.025 * math.sin(math.pi * u), 0) for u in [j / 10.0 for j in range(11)]]


def braid(i, k, p, c):
    th = 2 * math.pi * k / 16
    return (p - c) * 0.22 * math.sin(2 * th + i * 1.9)


loft([(c, 0.045 * (1 - 0.6 * (j / 10.0)) + 0.012 * (j == 0), 0.03 * (1 - 0.55 * (j / 10.0))) for j, c in enumerate(_beard)],
     lambda p, s: {"Head": 1.0} if s < 0.5 else {"Head": 0.6, "Neck": 0.4}, n=16, disp=braid, cap0=False)
blob(_beard[-2] + V(0, 0, -0.004), 0.017, lambda p, s: {"Head": 0.6, "Neck": 0.4}, mat=EYES, seg=12, rings=8, scale=(1, 1, 1.3))
loft([(FC_C + V(sx * FC_R[0] * 0.92, -FC_R[1] * 0.3, -FC_R[2] * (0.15 + 0.5 * u)), 0.016, 0.012) for sx in (1,) for u in (0.0, 0.5, 1.0)], W("Head"), n=10)
loft([(FC_C + V(-FC_R[0] * 0.92, -FC_R[1] * 0.3, -FC_R[2] * (0.15 + 0.5 * u)), 0.016, 0.012) for u in (0.0, 0.5, 1.0)], W("Head"), n=10)

# ---------------------------------------------------------------- the mantle: closed over the shoulders, longer behind
MANTLE = dense([(1.78, 0.15, 0.13, -0.085), (1.73, 0.27, 0.19, -0.07), (1.66, 0.34, 0.235, -0.05), (1.57, 0.365, 0.255, -0.03),
                (1.48, 0.375, 0.265, -0.015), (1.42, 0.38, 0.27, -0.005)], 4)
_mf = folds(10, 0.03, 1.66, 1.45, 5.0)


def mantle_disp(z, th, p, out):
    d = _mf(z, th, p, out)
    if z < 1.421:
        d = d + V(0, 0, -0.05 - 0.07 * (0.5 + 0.5 * noise.noise(Vector((th * 4.0, 1.7, 0.0)))) - 0.12 * max(0.0, -math.sin(th)) ** 2)
    return d


ring_loft(MANTLE, lambda p, s: {"Chest": 1.0} if abs(p.x) < 0.15 or p.z > 1.72 else {"Chest": 0.6, ("Shoulder.L" if p.x > 0 else "Shoulder.R"): 0.4},
          n=64, disp=mantle_disp)

# ---------------------------------------------------------------- the rope belt, twisted, a knot and crimson cords
_belt = [V(0.262 * math.cos(2 * math.pi * k / 72), 0.0 - 0.196 * math.sin(2 * math.pi * k / 72), 1.13 + 0.008 * math.sin(4 * math.pi * k / 72)) for k in range(72)]
tube_loop(_belt, lambda i: 0.022 + 0.005 * abs(math.sin(i * 1.6)), lambda p, s: {"Spine": 0.5, "Hips": 0.5}, n=10)
blob(V(0.16, -0.205, 1.12), 0.04, lambda p, s: {"Spine": 0.5, "Hips": 0.5}, seg=14, rings=10, scale=(1.2, 0.8, 1.0),
     disp=lambda d: 0.12 * math.sin(5 * math.atan2(d.y, d.x)) * math.sin(3 * math.acos(max(-1, min(1, d.z)))))

# ---------------------------------------------------------------- arms: wide sleeves, strapped forearms, big hands
for side, sx in (("L", 1.0), ("R", -1.0)):
    sh, ua, la, ha = "Shoulder." + side, "UpperArm." + side, "LowerArm." + side, "Hand." + side
    a, b = head_of(ua), tail_of(ua)

    def up_sleeve(i, k, p, c, sx=sx):
        th = 2 * math.pi * k / 32
        return (p - c) * (0.12 * math.sin(5 * th + i * 1.3) * (0.3 + 0.7 * i / 4.0))
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.105), (0.3, 0.1), (0.65, 0.095), (1.0, 0.095))], along(a, b, ua, la, 0.8), n=32, disp=up_sleeve)
    blob(b, 0.095, lambda p, s, ua=ua, la=la: {ua: 0.5, la: 0.5}, seg=20, rings=14,
         disp=lambda d: 0.1 * math.sin(6 * math.atan2(d.y, d.x)))
    a, b = head_of(la), tail_of(la)

    def low_sleeve(i, k, p, c, sx=sx):
        th = 2 * math.pi * k / 36
        hang = (0.05 * max(0.0, math.sin(th)) + 0.03 * (0.5 + 0.5 * noise.noise(Vector((k * 0.7, sx * 3.0, 0.0))))) * (1.0 if i == 5 else 0.0)
        return (p - c) * (0.14 * (i / 5.0) * math.sin(6 * th + i)) + V(0, 0, -hang)
    # (ending half way down the forearm: the strapped forearm and the fist show below it)
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.09), (0.15, 0.094), (0.3, 0.1), (0.42, 0.108), (0.52, 0.116), (0.6, 0.124))],
         along(a, b, la, ha, 0.9), n=36, cap1=False, disp=low_sleeve)
    # the forearm inside: thick, bound in leather straps crossing round it
    def strap(i, k, p, c):
        th = 2 * math.pi * k / 20
        u = i / 7.0
        return (p - c).normalized() * (0.006 * g2(math.sin(math.pi * (u * 7 + th / math.pi)), 0.35))
    loft([(a.lerp(b, u), r, r * 0.9) for u, r in ((0.35, 0.052), (0.45, 0.05), (0.55, 0.049), (0.65, 0.047), (0.75, 0.045), (0.85, 0.043), (0.93, 0.042), (1.0, 0.044), (1.04, 0.045))],
         along(a, b, la, ha, 0.9), n=20, disp=strap)
    # the hand: a broad palm, four thick jointed fingers, a thumb
    a, b = head_of(ha), tail_of(ha)
    d = (b - a).normalized()
    palm_in = V(-sx, 0, 0)
    spread = d.cross(palm_in).normalized()

    def palm(i, k, p, c):
        th = 2 * math.pi * k / 16
        return (p - c).normalized() * 0.004 * math.cos(4 * th)
    loft([(a.lerp(b, u), 0.026, r) for u, r in ((-0.1, 0.035), (0.4, 0.045), (1.0, 0.046))], W(ha), n=16, disp=palm)
    for i, (off, lens) in enumerate(((-0.033, (0.044, 0.03, 0.025)), (-0.011, (0.05, 0.034, 0.027)), (0.011, (0.048, 0.033, 0.026)), (0.031, (0.038, 0.026, 0.022)))):
        base = b + spread * off
        name = "Finger.%s.%d" % (side, i)
        pts, dirn = [base], d.copy()
        for k, ln in enumerate(lens):
            dirn = (dirn + palm_in * math.tan(math.radians(14 + 10 * k))).normalized()
            pts.append(pts[-1] + dirn * ln)
        parent = ha
        for k in range(3):
            bone("%s.%d" % (name, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
            parent = "%s.%d" % (name, k)
        secs, sw = [], []
        for k in range(3):
            for u in (0.0, 0.25, 0.5, 0.8, 1.0):
                r0 = 0.0125 * (1.0 - 0.12 * k) * (0.85 if i == 3 else 1.0)
                knuckle = 1.0 + 0.22 * g2(u, 0.14) + (0.15 * g2(u - 1.0, 0.12) if k < 2 else 0.0)
                taper = (1.0 - 0.35 * u ** 2) if k == 2 else 1.0
                r = r0 * knuckle * taper
                secs.append((pts[k].lerp(pts[k + 1], u), r, r * 0.88))
                sw.append({ha: 0.5, "%s.%d" % (name, k): 0.5} if (k == 0 and u == 0.0) else {"%s.%d" % (name, k): 1.0})
        loft(secs, piecewise(sw), n=10)
    tb = a + d * 0.03 + spread * (-0.035) + palm_in * 0.01
    tdir = (d * 0.55 + spread * (-0.5) + palm_in * 0.45).normalized()
    t1 = tb + tdir * 0.045
    t2 = t1 + (tdir + palm_in * 0.5).normalized() * 0.038
    bone("Thumb.%s.0" % side, tuple(tb), tuple(t1), ha)
    bone("Thumb.%s.1" % side, tuple(t1), tuple(t2), "Thumb.%s.0" % side)
    loft([(tb, 0.016, 0.016), (tb.lerp(t1, 0.5), 0.014, 0.014), (t1, 0.015, 0.015), (t1.lerp(t2, 0.6), 0.012, 0.012), (t2, 0.008, 0.008)],
         lambda p, s, side=side: {"Thumb.%s.0" % side: 1.0} if s < 0.5 else {"Thumb.%s.1" % side: 1.0}, n=10)

# crimson prayer beads wound round the left wrist (the rosary of old, now only that)
_w = tail_of("LowerArm.L")
_wd = (tail_of("LowerArm.L") - head_of("LowerArm.L")).normalized()
_wu, _wv = frame(_wd)
for k in range(11):
    a = 2 * math.pi * k / 11
    blob(_w - _wd * 0.04 + (_wu * math.cos(a) + _wv * math.sin(a)) * 0.052, 0.012, W("LowerArm.L"), mat=EYES, seg=10, rings=6)

# ---------------------------------------------------------------- the torn strips (SpringBones swings them)
STRIPS = []   # (top, bottom, width, root, mat, along)
for k in range(12):
    ang = math.radians(-180 + k * 30 + 9 * noise.noise(Vector((k * 0.9, 0.3, 0.0))))
    top = V(0.40 * math.cos(ang), 0.045 + 0.285 * math.sin(ang), 0.36)
    length = 0.22 + 0.12 * (0.5 + 0.5 * noise.noise(Vector((k * 1.7, 2.0, 0.0))))
    STRIPS.append((top, top + V(0.03 * math.cos(ang), 0.05, -length), 0.11 - 0.015 * (k % 3), "Hips", INK, (math.sin(ang), math.cos(ang))))
for s in (1, -1):   # the mantle's torn tails behind
    top = V(0.12 * s, 0.265, 1.36)
    STRIPS.append((top, top + V(0.03 * s, 0.08, -0.45), 0.085, "Chest", INK, (0.95, 0.3)))
for dx in (0.0, 0.06):   # the belt's crimson cords
    top = V(0.17 + dx, -0.21, 1.1)
    STRIPS.append((top, top + V(dx * 0.4, -0.04, -0.5), 0.03, "Hips", EYES, (0.7, 0.7)))
for n, (top, bottom, width, root, mat, al) in enumerate(STRIPS):
    sag = V(0, -0.03, 0)
    pts = [top.lerp(bottom, k / 4.0) + sag * math.sin(math.pi * k / 4.0) for k in range(5)]
    parent = root
    for k in range(4):
        bone("Tatter.%d.%d" % (n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = "Tatter.%d.%d" % (n, k)
    secs, sw = [], []
    for k in range(4):
        for u in ((0.0, 0.5, 1.0) if k == 0 else (0.5, 1.0)):
            f = (k + u) / 4.0
            w = width * (1.0 - 0.5 * f) * (0.15 if k == 3 and u == 1.0 else 1.0)
            if mat == EYES:
                secs.append((pts[k].lerp(pts[k + 1], u), max(0.008, w * 0.5), max(0.008, w * 0.5)))
            else:
                secs.append((pts[k].lerp(pts[k + 1], u), max(0.012, w * abs(al[0])), max(0.012, w * abs(al[1]))))
            sw.append({root: 0.6, "Tatter.%d.0" % n: 0.4} if (k == 0 and u == 0.0) else {"Tatter.%d.%d" % (n, k): 1.0})

    def strip_disp(i, k, p, c, n=n):
        return (p - c) * 0.25 * noise.noise(Vector((i * 0.8, k * 0.9, n * 1.3)))
    loft(secs, piecewise(sw), mat=mat, n=10, disp=strip_disp)

# ---------------------------------------------------------------- armature + body mesh
arm = bpy.data.armatures.new("ShieldMonkRig")
rig = bpy.data.objects.new("ShieldMonkRig", arm)
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

MATS = (("MonkInk", (0.07, 0.07, 0.08, 1)), ("MonkCrimson", (0.9, 0.02, 0.04, 1)))
for name, col in MATS:
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = col


def make_mesh(name):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.validate()
    me.update()
    me.shade_smooth()
    for mname, _ in MATS:
        me.materials.append(bpy.data.materials[mname])
    for poly, m in zip(me.polygons, fmat):
        poly.material_index = m
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = rig
    return ob


def apply_mods(ob, thick=None, smooth=1, ratio=None):
    if thick:
        s = ob.modifiers.new("Thick", 'SOLIDIFY'); s.thickness = thick; s.offset = -1.0; s.use_even_offset = False
    if smooth:
        s = ob.modifiers.new("Smooth", 'SUBSURF'); s.levels = smooth; s.render_levels = smooth
    if ratio:
        s = ob.modifiers.new("Thin", 'DECIMATE'); s.ratio = ratio
    if globals().get("RAW", False):
        return
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=ob, object=ob, selected_objects=[ob]):
        for m in list(ob.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)


body = make_mesh("ShieldMonk")
groups = {}
for vi, w in enumerate(vweights):
    tot = sum(w.values())
    for bn, x in w.items():
        g = groups.get(bn) or body.vertex_groups.new(name=bn)
        groups[bn] = g
        g.add([vi], x / tot, 'REPLACE')
apply_mods(body, thick=0.007, smooth=1, ratio=DECIMATE_RATIO)
mod = body.modifiers.new("Armature", 'ARMATURE')
mod.object = rig


def reset():
    verts.clear(); faces.clear(); fmat.clear(); vweights.clear()


NOW = lambda p, s: {}

# ---------------------------------------------------------------- the kite shield (front -y; outline in x, z about its centre)
reset()
ROWS = 34
COLS = 28


def kite_half(z):
    """The kite's half width at height z (top 0.44, point -0.58): a curved top, straight sides, then tapering."""
    if z > 0.3:
        return 0.30 * math.sqrt(max(0.0, 1 - ((z - 0.3) / 0.16) ** 2)) * 0.25 + 0.30 * 0.75
    if z > 0.08:
        return 0.30
    return max(0.012, 0.30 * (z + 0.58) / 0.66)


_kz = [0.44 - 1.02 * j / (ROWS - 1) for j in range(ROWS)]


def kite_pt(j, i, front):
    z = _kz[j]
    h = kite_half(z)
    x = -h + 2 * h * i / COLS
    u = x / max(h, 1e-3)
    bulge = 0.055 * (1 - u * u)
    if not front:
        return V(x, 0.028 - 0.4 * bulge, z)
    plank = -0.004 * g2(math.sin(math.pi * (x / 0.1 + 0.5)), 0.12)                       # the grooves between planks
    grain = 0.0015 * noise.noise(Vector((x * 40, z * 6, 1.0)))
    dent = -0.006 * g2(x - 0.12, 0.04) * g2(z + 0.2, 0.05) - 0.005 * g2(x + 0.15, 0.03) * g2(z - 0.25, 0.04)
    return V(x, -(0.03 + bulge) + plank + grain + dent, z)


for front in (True, False):
    idx = [[add_vert(kite_pt(j, i, front), {}) for i in range(COLS + 1)] for j in range(ROWS)]
    for j in range(ROWS - 1):
        for i in range(COLS):
            q = (idx[j][i], idx[j][i + 1], idx[j + 1][i + 1], idx[j + 1][i])
            faces.append(q if front else tuple(reversed(q))); fmat.append(INK)
# its edge closed between front and back
_edge = [(j, 0) for j in range(ROWS)] + [(ROWS - 1, i) for i in range(COLS + 1)] + [(j, COLS) for j in reversed(range(ROWS))] + [(0, i) for i in reversed(range(COLS + 1))]
# the iron rim: a tube along the outline
_rimpts = []
for j in range(ROWS):
    _rimpts.append(V(-kite_half(_kz[j]), -0.035, _kz[j]))
for j in reversed(range(ROWS)):
    _rimpts.append(V(kite_half(_kz[j]), -0.035, _kz[j]))
tube_loop(_rimpts, 0.02, NOW, n=8, normal=V(0, 1, 0))
for j in range(2, ROWS - 3, 3):   # rivets along the rim
    for sx in (1, -1):
        blob(V(sx * (kite_half(_kz[j]) - 0.03), -0.075 + 0.04 * (abs(_kz[j]) > 0.3) * 0, _kz[j]), 0.011, NOW, seg=8, rings=6, scale=(1, 0.6, 1))
# the boss: a domed iron centre with a short spike
_bc = V(0, -0.085, 0.16)
blob(_bc, 0.075, NOW, seg=20, rings=12, scale=(1, 0.55, 1))
loft([(_bc + V(0, -0.04, 0), 0.018, 0.018), (_bc + V(0, -0.08, 0), 0.008, 0.008), (_bc + V(0, -0.1, 0), 0.001, 0.001)], NOW, n=10)
# the crimson eye round it, weeping down the face in three runs
tube_loop([V(0.105 * math.cos(2 * math.pi * k / 40), -0.09, 0.16 + 0.085 * math.sin(2 * math.pi * k / 40)) for k in range(40)], 0.011, NOW, mat=EYES, n=8, normal=V(0, 1, 0))
for dx, ln in ((-0.04, 0.34), (0.0, 0.52), (0.045, 0.27)):
    pts = [V(dx + 0.006 * math.sin(u * 9), 0, 0.07 - ln * u) for u in [j / 8.0 for j in range(9)]]
    secs = []
    for u, p in zip([j / 8.0 for j in range(9)], pts):
        z = p.z
        h = kite_half(z)
        uu = p.x / max(h, 1e-3)
        y = -(0.03 + 0.055 * (1 - uu * uu)) - 0.006
        secs.append((V(p.x, y, z), 0.012 * (1 - 0.55 * u) + 0.004 * g2(u - 1.0, 0.08), 0.006))
    loft(secs, NOW, mat=EYES, n=8)
# behind: the grip and an arm strap
loft([(V(-0.08, 0.06, 0.12), 0.016, 0.016), (V(0.0, 0.085, 0.12), 0.018, 0.018), (V(0.08, 0.06, 0.12), 0.016, 0.016)], NOW, n=10)
loft([(V(-0.12, 0.05, -0.12), 0.012, 0.03), (V(0.0, 0.07, -0.12), 0.012, 0.03), (V(0.12, 0.05, -0.12), 0.012, 0.03)], NOW, n=10)
shield = make_mesh("Shield")
apply_mods(shield, smooth=1)
shield.location = (0.36, -0.62, 1.12)          # held on the left arm, before him
shield.rotation_euler = (0, 0, 0)

# ---------------------------------------------------------------- the flail: a haft, six links, a spiked head
# Each is its own object with its origin where the game holds it: the haft at the fist's grip (its length up +z),
# a link at its centre (long along z), the head at its centre (the chain's eye on top, +z).
reset()
HAFT_TOP = 0.42


def haft_disp(i, k, p, c):
    th = 2 * math.pi * k / 16
    z = c.z
    wrap = 0.003 * g2(math.sin(math.pi * z / 0.025), 0.4) if -0.08 < z < 0.1 else 0.0       # leather wound round the grip
    grain = 0.0012 * noise.noise(Vector((math.cos(th) * 2, math.sin(th) * 2, z * 20)))
    return (p - c).normalized() * (wrap + grain)


loft([(V(0, 0, z), r, r) for z, r in ((-0.13, 0.026), (-0.11, 0.03), (-0.09, 0.024), (0.0, 0.024), (0.1, 0.024), (0.12, 0.021), (0.3, 0.023), (0.38, 0.025))],
     NOW, n=16, disp=haft_disp)
loft([(V(0, 0, z), r, r) for z, r in ((0.36, 0.031), (0.4, 0.033), (0.42, 0.028), (0.43, 0.012))], NOW, n=16)   # the iron cap
tube_loop([V(0, 0.022 * math.cos(2 * math.pi * k / 16), HAFT_TOP + 0.03 + 0.022 * math.sin(2 * math.pi * k / 16)) for k in range(16)], 0.007, NOW, n=8, normal=V(1, 0, 0))
for z in (0.12, 0.3):   # iron bands
    loft([(V(0, 0, z - 0.008), 0.027, 0.027), (V(0, 0, z + 0.008), 0.027, 0.027)], NOW, n=16)
flail = make_mesh("Flail")
apply_mods(flail, smooth=1)

LINK_LEN = 0.075
links = []
for n in range(6):
    reset()
    pts = []
    for k in range(24):
        a = 2 * math.pi * k / 24
        pts.append(V(0.02 * math.cos(a), 0, (LINK_LEN * 0.5) * math.sin(a)))
    tube_loop(pts, 0.007, NOW, n=8, normal=V(0, 1, 0))
    lk = make_mesh("FlailLink.%d" % n)
    apply_mods(lk, smooth=0)
    links.append(lk)

reset()
HEAD_R = 0.085


def ball(d):
    return 0.03 * noise.noise(Vector((d.x * 3, d.y * 3, d.z * 3)))   # hammered, not turned


blob(V(0, 0, 0), HEAD_R, NOW, seg=28, rings=18, disp=ball)
_sp = []
golden = math.pi * (3 - math.sqrt(5))
for k in range(16):   # spikes spread evenly round it (not on the top, where the chain's eye is)
    z = 0.8 - 1.7 * (k + 0.5) / 16
    r = math.sqrt(max(0.0, 1 - z * z))
    d = V(r * math.cos(golden * k), r * math.sin(golden * k), z)
    base = d * HEAD_R * 0.92
    loft([(base, 0.024, 0.024), (base + d * 0.035, 0.014, 0.014), (base + d * 0.07, 0.002, 0.002)], NOW, n=10)
tube_loop([V(HEAD_R * 1.02 * math.cos(2 * math.pi * k / 40), HEAD_R * 1.02 * math.sin(2 * math.pi * k / 40), -0.005) for k in range(40)], 0.009, NOW, mat=EYES, n=8)
tube_loop([V(0, 0.022 * math.cos(2 * math.pi * k / 16), HEAD_R + 0.025 + 0.022 * math.sin(2 * math.pi * k / 16)) for k in range(16)], 0.007, NOW, n=8, normal=V(1, 0, 0))
head = make_mesh("FlailHead")
apply_mods(head, smooth=1)

# rest: the haft upright in the right fist, the chain hanging down along it, the head at his knee
_grip = tail_of("Hand.R") + V(0.0, -0.01, 0.0)
flail.location = _grip
_top = _grip + V(0, 0, HAFT_TOP + 0.03)
for n, lk in enumerate(links):
    lk.location = _top + V(0, -0.03, -LINK_LEN * 0.8 * (n + 0.5))
    lk.rotation_euler = (0, 0, math.radians(90 * (n % 2)))
head.location = _top + V(0, -0.03, -LINK_LEN * 0.8 * 6 - HEAD_R - 0.03)

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\ShieldMonk.blend")
_parts = [shield, flail, head] + links
result = {"verts": len(body.data.vertices), "faces": len(body.data.polygons), "bones": len(BONES),
          "dims": [round(x, 3) for x in body.dimensions], "parts": {o.name: len(o.data.vertices) for o in _parts}}

if globals().get("EXPORT", True):
    sel = [rig, body] + _parts
    for o in bpy.context.scene.objects:
        o.select_set(o in sel)
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=rig, selected_objects=sel):
        bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Enemy\ShieldMonk.fbx", use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                 bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
