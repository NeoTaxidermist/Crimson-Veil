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
TORSO = [  # z, half width, half depth, centre y (forward is -y); depths measured off the concept's side profile
    (0.88, 0.150, 0.110, 0.004), (0.97, 0.156, 0.112, 0.006), (1.05, 0.142, 0.1, 0.004),
    (1.13, 0.132, 0.094, -0.004), (1.22, 0.150, 0.104, -0.006), (1.31, 0.176, 0.116, -0.006),
    (1.39, 0.192, 0.12, -0.006), (1.45, 0.190, 0.11, -0.006), (1.50, 0.160, 0.094, -0.006),
    (1.54, 0.085, 0.066, -0.004),
]


def torso_at(z):
    for (z0, *a), (z1, *b) in zip(TORSO, TORSO[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0)
            return [x + (y - x) * t for x, y in zip(a, b)]
    return list(TORSO[0][1:] if z < TORSO[0][0] else TORSO[-1][1:])


torso_w = blend_z([(1.0, "Hips"), (1.1, "Spine"), (1.26, "Spine"), (1.34, "Chest")])
loft([((0, cy, z), rx, ry, {"back": 0.92 if 1.2 < z < 1.48 else 1.0})
      for z, rx, ry, cy in TORSO], torso_w, n=28)

# Cuirass: layered bands over the belly and a raised breastplate edge — the stepped profile of the concept.
for z, grow in ((1.17, 0.016), (1.235, 0.018), (1.30, 0.02)):
    rx, ry, cy = torso_at(z)
    flush = {"back": ry / (ry + grow)}  # plates on the front and sides only: the back stays smooth (concept)
    loft([((0, cy, z - 0.03), rx + 0.004, ry + 0.004, {"back": ry / (ry + 0.004)}), ((0, cy - 0.004, z - 0.008), rx + grow, ry + grow, flush),
          ((0, cy, z + 0.03), rx + 0.004, ry + 0.004, {"back": ry / (ry + 0.004)})], torso_w, n=28)
rx, ry, cy = torso_at(1.40)
loft([((0, cy, 1.34), rx + 0.004, ry + 0.004, {"back": 0.9}), ((0, cy - 0.006, 1.40), rx + 0.016, ry + 0.02, {"back": 0.9}),
      ((0, cy, 1.47), rx - 0.01, ry + 0.006, {"back": 0.9})], torso_w, n=28, square=0.25)
# A crimson line across the chest (the stitched trim in the art).
loft([((0, cy, 1.392), rx + 0.019, ry + 0.023, {"back": 0.9}), ((0, cy, 1.408), rx + 0.019, ry + 0.023, {"back": 0.9})],
     torso_w, mat=CRIMSON, n=28, cap0=False, cap1=False, square=0.25)

# Belt (concept: z 0.89-0.93) with a crimson sash under it and a tail hanging at the left hip.
rx, ry, cy = torso_at(0.91)
loft([((0, cy, 0.875), rx + 0.008, ry + 0.008), ((0, cy, 0.905), rx + 0.01, ry + 0.01)], W("Hips"), mat=CRIMSON, n=28)
loft([((0, cy, 0.9), rx + 0.012, ry + 0.012), ((0, cy, 0.94), rx + 0.012, ry + 0.012)], W("Hips"), n=28)
box((0, cy - ry - 0.014, 0.92), (0.05, 0.012, 0.042), W("Hips"))  # buckle
loft([((0.16, -0.02, 0.885), 0.03, 0.02), ((0.175, -0.005, 0.8), 0.022, 0.012), ((0.17, 0.01, 0.71), 0.03, 0.01)],
     W("Hips"), mat=CRIMSON, n=10)  # sash tail

# ---------------------------------------------------------------- neck, scarf, head
loft([((0, 0.0, 1.50), 0.05, 0.048), ((0, -0.025, 1.58), 0.042, 0.042), ((0, -0.05, 1.65), 0.044, 0.044)],
     blend_z([(1.53, "Chest"), (1.58, "Neck"), (1.62, "Neck"), (1.66, "Head")]), n=16)
# Crimson scarf wound round the neck: two thick turns.
for z, r, t, yc in ((1.545, 0.088, 0.03, -0.012), (1.585, 0.078, 0.026, -0.03)):
    loft([((0, yc + 0.004, z - t), r - 0.012, r - 0.016), ((0, yc, z), r + 0.012, r + 0.006),
          ((0, yc + 0.004, z + t), r - 0.012, r - 0.016)], blend_z([(1.53, "Chest"), (1.58, "Neck")]), mat=CRIMSON, n=20)

HEAD = W("Head")
HF = -0.05  # the head juts forward on a long neck: the face is ahead of the chest line, as in the concept

# ---------------------------------------------------------------- arms and legs
# Measured from the concept art (side view, ~1.66 mm/px at the rig's 1.84 m to the crown). Values are half
# thicknesses (rx sideways, ry front/back); the concept's side profile gives the front/back ones:
#   upper arm 0.10 m, forearm 0.055-0.065, fist 0.076; thigh 0.15, knee with guard 0.15-0.16, calf 0.138,
#   boot shin 0.075-0.09, ankle 0.067, foot 0.26 long and 0.075 high; breeches end at z 0.66, hem ~0.2 wide.
for side, sx in (("L", 1), ("R", -1)):
    sh, el, wr = head_of("UpperArm." + side), head_of("LowerArm." + side), head_of("Hand." + side)
    ua, la = "UpperArm." + side, "LowerArm." + side
    # Deltoid and upper arm.
    loft([(sh + Vector((0, 0, 0.02)), 0.046, 0.046), (sh.lerp(el, 0.18), 0.054, 0.05), (sh.lerp(el, 0.5), 0.05, 0.047),
          (sh.lerp(el, 0.85), 0.04, 0.038), (el, 0.035, 0.034)], along(sh, el, ua, la), n=14)
    # Forearm, wrapped in a bracer from mid-forearm to the wrist.
    loft([(el, 0.035, 0.034), (el.lerp(wr, 0.3), 0.034, 0.031), (wr, 0.026, 0.024)], along(el, wr, la, "Hand." + side), n=14)
    loft([(el.lerp(wr, 0.3), 0.038, 0.034), (el.lerp(wr, 0.36), 0.036, 0.032), (el.lerp(wr, 0.86), 0.031, 0.028),
          (el.lerp(wr, 0.96), 0.034, 0.031)], W(la), n=14, square=0.3)
    # Gauntleted fist (0.076 deep).
    hand = head_of("Hand." + side)
    blob(hand + Vector((sx * 0.004, -0.008, -0.042)), (0.032, 0.038, 0.05), W("Hand." + side), seg=12, rings=8)
    blob(hand + Vector((sx * -0.016, -0.026, -0.036)), (0.012, 0.014, 0.026), W("Hand." + side), seg=8, rings=6)  # thumb
    # Stacked pauldron: three overlapping plates over the shoulder cap, each a little lower and further out.
    for k, (r, d) in enumerate(((0.09, 0.03), (0.08, 0.026), (0.07, 0.022))):
        out = Vector((sx * (0.55 + 0.3 * k), 0.0, 1.0 - 0.25 * k)).normalized()
        c = sh + Vector((sx * (0.0 + 0.02 * k), 0.005, 0.03 - 0.04 * k))
        w = (lambda b: (lambda p, s: {b: 1.0}))(ua) if k else (lambda p, s, a=ua, sd=side: {a: 0.6, "Shoulder." + sd: 0.4})
        dome(c, out, r, d, w, squash=0.9, up=(0, 1, 0))

for side, sx in (("L", 1), ("R", -1)):
    hp, kn, an = head_of("UpperLeg." + side), head_of("LowerLeg." + side), head_of("Foot." + side)
    ul, ll, ft = "UpperLeg." + side, "LowerLeg." + side, "Foot." + side
    thigh = lambda p, s, a=hp, b=kn, ul=ul, ll=ll: (
        {"Hips": 0.35, ul: 0.65} if p.z > a.z + 0.02 else along(a, b, ul, ll)(p, s))
    loft([(hp + Vector((0, 0, 0.04)), 0.08, 0.08), (hp.lerp(kn, 0.3), 0.077, 0.077), (hp.lerp(kn, 0.7), 0.068, 0.072),
          (kn, 0.058, 0.062)], thigh, n=16)
    # Loose short breeches, ending above the knee (z 0.66) in a wide hem.
    loft([(hp + Vector((0, 0, 0.05)), 0.094, 0.1), (hp.lerp(kn, 0.3), 0.098, 0.104), (hp.lerp(kn, 0.58), 0.1, 0.104),
          (hp.lerp(kn, 0.65), 0.103, 0.106)], thigh, n=16)
    # Calf: a strong muscle at the back (0.138 deep), narrowing into the slim boot.
    calf = along(kn, an, ll, ft, blend_from=0.9)
    loft([(kn, 0.056, 0.06), (kn.lerp(an, 0.22), 0.06, 0.062, {"back": 1.15}), (kn.lerp(an, 0.42), 0.05, 0.05, {"back": 1.1}),
          (kn.lerp(an, 0.62), 0.04, 0.04), (an, 0.032, 0.034)], calf, n=16)
    # Tall slim boot from mid-shin to the ankle (0.075-0.09 deep), with a turned rim.
    loft([(kn.lerp(an, 0.5), 0.047, 0.048), (kn.lerp(an, 0.55), 0.044, 0.045), (kn.lerp(an, 0.85), 0.039, 0.04),
          (an + Vector((0, 0, -0.01)), 0.038, 0.042)], calf, n=16, square=0.2)
    # Knee guard: a plate over the kneecap and a smaller one over the shin top.
    dome(kn + Vector((0, -0.05, 0.0)), (0, -1, 0.15), 0.05, 0.025, W(ll), squash=1.1, up=(0, 0, 1))
    dome(kn + Vector((0, -0.045, -0.06)), (0, -1, 0.05), 0.038, 0.018, W(ll), squash=1.3, up=(0, 0, 1))
    # Boot foot: heel to toe, 0.26 long, 0.075 high.
    loft([(an + Vector((0, 0.05, -0.03)), 0.036, 0.028), (an + Vector((0, 0.0, -0.045)), 0.042, 0.04),
          (an + Vector((0, -0.09, -0.058)), 0.04, 0.028), (an + Vector((0, -0.17, -0.068)), 0.032, 0.018),
          (an + Vector((0, -0.21, -0.074)), 0.018, 0.01)], along(an + Vector((0, 0.05, 0)), an + Vector((0, -0.21, 0)), ft, None),
         n=12)
    box(an + Vector((0, -0.075, -0.083)), (0.085, 0.27, 0.018), W(ft))  # sole

# ---------------------------------------------------------------- head and hair, traced from the concept
# The head and its mop of hair are built from the concept's side silhouette itself (ArtSource/ref/hero_concept.jpg):
# the outline is traced at 3.3 mm and "inflated" into a rounded solid (the half-width grows with the distance from
# the outline, up to HEAD_HALF), so seen from the side - as the game sees him - it is exactly the drawing. The spikes
# of hair get spring chains (Hair.N.0..2) found at the outline's tips, so they still sway.
import numpy as np
REF = ROOT + r"\ArtSource\ref\hero_concept.jpg"
K = 0.00166                       # metres per concept pixel (crown at the rig's 1.84 m)
GROUND_ROW, AXIS_COL = 1875, 720  # concept pixel row of the ground; column of the neck's axis
NECK_Y = HF                       # model y of that axis (the neck top leans forward)
HEAD_HALF = 0.085                 # half-width of the head and hair (side to side)
STEP = 2                          # trace every 2nd pixel


def concept_mask():
    im = bpy.data.images.load(REF, check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    red = (r > g * 1.8) & (r > 0.25)
    return ((0.3 * r + 0.59 * g + 0.11 * b) < 0.12) & ~red


def to_model(row, col, x=0.0):
    return Vector((x, NECK_Y - (col - AXIS_COL) * K, (GROUND_ROW - row) * K))


mask = concept_mask()
R0, R1, C0, C1 = HEAD_BOX = (705, 909, 560, 860)  # the head in the concept (the jaw ends at row ~905)
sub = mask[R0:R1:STEP, C0:C1:STEP].copy()
sub[(885 - R0) // STEP:, :(700 - C0) // STEP] = False  # the scarf behind the nape is not the head
nr, nc = sub.shape
# keep only the head: the blob connected to the middle of the skull
seed = ((770 - R0) // STEP, (720 - C0) // STEP)
inside = np.zeros_like(sub)
stack = [seed]
while stack:
    i, j = stack.pop()
    if 0 <= i < nr and 0 <= j < nc and sub[i, j] and not inside[i, j]:
        inside[i, j] = True
        stack += [(i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)]
pad = np.pad(inside, 1)
edge = inside & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
ii, jj = np.nonzero(inside)
ei, ej = np.nonzero(edge)
oi, oj = [a.astype(np.float32) for a in np.nonzero(~inside)]
# distance (m) of every inside node to the nearest outside node
dist = np.zeros(inside.shape)
for n in range(0, len(ii), 512):
    a_i, a_j = ii[n:n + 512, None].astype(np.float32), jj[n:n + 512, None].astype(np.float32)
    d2 = (a_i - oi[None]) ** 2 + (a_j - oj[None]) ** 2
    dist[ii[n:n + 512], jj[n:n + 512]] = np.sqrt(d2.min(1)) * STEP * K
dist[edge] = 0.0

# Spring chains for the hair: the outline's tips far from the skull's middle, one per direction.
pts_m = {(i, j): to_model(R0 + i * STEP, C0 + j * STEP) for i, j in zip(ii, jj)}
centre = sum(pts_m.values(), Vector()) / len(pts_m)
centre = Vector((0, centre.y + 0.01, centre.z + 0.005))
SKULL_R = 0.1
bins = {}
for i, j in zip(ei, ej):
    p = pts_m[(i, j)]
    rel = p - centre
    ang = math.degrees(math.atan2(rel.z, -rel.y))  # 0 = forward, 90 = up, 180 = back
    if -150 < ang < -20:  # the face and the jaw: no hair there
        continue
    key = int((ang + 360) % 360 // 16)
    if rel.length > SKULL_R + 0.03 and (key not in bins or rel.length > bins[key][1]):
        bins[key] = (p, rel.length, ang)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
_win = bpy.context.window_manager.windows[0]
_ctx = dict(window=_win, screen=_win.screen, active_object=rig, object=rig, selected_objects=[rig])
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='EDIT')
eb = rig.data.edit_bones
for bn in [bn for bn in eb if bn.name.startswith("Hair.")]:
    eb.remove(bn)
chains = []  # (angle, root radius, tip radius, [bone names])
for n, (key, (tip, r_tip, ang)) in enumerate(sorted(bins.items())):
    d = (tip - centre).normalized()
    root = centre + d * (SKULL_R * 0.8)
    parent, names = eb["Head"], []
    for k in range(3):
        bn = eb.new("Hair.%d.%d" % (n, k))
        bn.head, bn.tail = root.lerp(tip, k / 3), root.lerp(tip, (k + 1) / 3)
        bn.parent, bn.use_connect = parent, k > 0
        parent = bn
        names.append(bn.name)
    chains.append((ang, SKULL_R * 0.8, r_tip, names))
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='OBJECT')


def head_hair_w(p, s):
    rel = p - centre
    rel.x = 0
    r = rel.length
    if r < SKULL_R or not chains:
        return {"Head": 1.0}
    ang = math.degrees(math.atan2(rel.z, -rel.y))
    best = min(chains, key=lambda c: abs((ang - c[0] + 180) % 360 - 180))
    if abs((ang - best[0] + 180) % 360 - 180) > 24:
        return {"Head": 1.0}
    t = max(0.0, min(0.999, (r - best[1]) / max(best[2] - best[1], 1e-3)))
    w = max(0.0, min(1.0, (r - SKULL_R) / 0.03))
    bone = best[3][int(t * 3)]
    return {bone: 1.0} if w >= 1 else {"Head": 1 - w, bone: w}


# The solid: a front and a back surface over the traced nodes, meeting along the outline.
front, back = {}, {}
for i, j in zip(ii, jj):
    d = min(dist[i, j], HEAD_HALF)
    hw = HEAD_HALF * math.sqrt(max(0.0, 1 - (1 - d / HEAD_HALF) ** 2))
    p = pts_m[(i, j)]
    if hw < 1e-4:
        front[(i, j)] = back[(i, j)] = add_vert(p, head_hair_w(p, 0))
    else:
        q0, q1 = p + Vector((hw, 0, 0)), p - Vector((hw, 0, 0))
        front[(i, j)] = add_vert(q0, head_hair_w(q0, 0))
        back[(i, j)] = add_vert(q1, head_hair_w(q1, 0))
for i, j in zip(ii, jj):
    quad = [(i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j)]
    if all(q in front for q in quad):
        faces.append(tuple(front[q] for q in quad)); fmat.append(INK)
        faces.append(tuple(back[q] for q in reversed(quad))); fmat.append(INK)

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
