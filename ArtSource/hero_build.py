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


# ---------------------------------------------------------------- body traced from the concept
# The body is measured off the concept's own side silhouette (ArtSource/ref/hero_concept.jpg): the rig is posed as in
# the drawing (ArtSource/ref/concept_pose.json, found by hero_fit_pose.py) and, every 1.2 cm along each bone, the
# outline's reach in front of and behind the bone is read. Each part is then a smooth loft along its rest bone with
# exactly that profile - so the parts meet at the joints, and seen from the side the boots, breeches, knee guards,
# chest, bracers and fists are the drawing's. The front leg and the near arm of the drawing are used for both sides
# (the far arm holds the sword).
import json
import numpy as np

REF = ROOT + r"\ArtSource\ref\hero_concept.jpg"
K = 0.00166                       # metres per concept pixel (crown at the rig's 1.84 m)
GROUND_ROW, AXIS_COL = 1875, 720  # concept pixel row of the ground; column of the neck's axis
NECK_Y = -0.05                    # model y of that axis
BODY_STEP = 4                     # trace every 4th pixel (6.6 mm)


def concept_body_mask():
    im = bpy.data.images.load(REF, check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    red = (r > g * 1.8) & (r > 0.25)
    m = ((0.3 * r + 0.59 * g + 0.11 * b) < 0.12) | red  # the crimson trim belongs to the body
    rows, cols = np.mgrid[0:h, 0:w]
    cy, cx = rows - 680, cols - 250  # the measuring crop
    m &= ~((np.abs((cy - 880) + cx * (180.0 / 285.0)) < 22) & (cx < 300))  # the sword's blade
    # The scarf streaming behind hides the back of the shoulders: there the back follows the line of the visible
    # back below it (from x 350 at row 445 up to 382 at the shoulder top).
    m &= ~((cy < 445) & (cx < 382 - (cy - 260) * (32.0 / 185.0)))
    m &= rows < GROUND_ROW + 2
    # Close the little highlights inside the outline (a clasp's glint on the bracer): they are not holes.
    for grow in (True, True, True, False, False, False):
        pad = np.pad(m, 1)
        nb = pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:]
        m = (m | nb[0] | nb[1] | nb[2] | nb[3]) if grow else (m & nb[0] & nb[1] & nb[2] & nb[3])
    return m


def px_to_model(row, col):
    return NECK_Y - (col - AXIS_COL) * K, (GROUND_ROW - row) * K


def model_to_px(y, z):
    return GROUND_ROW - z / K, AXIS_COL + (NECK_Y - y) / K


# Pose the rig as in the drawing.
CONCEPT_POSE = json.load(open(ROOT + r"\ArtSource\ref\concept_pose.json"))
for pb in rig.pose.bones:
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)
for name, deg in CONCEPT_POSE.items():
    if name in rig.pose.bones:
        pb = rig.pose.bones[name]
        M3 = pb.bone.matrix_local.to_3x3()
        pb.rotation_quaternion = (M3.inverted() @ Matrix.Rotation(math.radians(deg), 3, 'X') @ M3).to_quaternion()
hips_pb = rig.pose.bones["Hips"]
hips_pb.location = hips_pb.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, -CONCEPT_POSE.get("drop", 0.0)))
bpy.context.view_layer.update()
UNPOSE = {pb.name: pb.bone.matrix_local @ pb.matrix.inverted() for pb in rig.pose.bones}
POSED = {pb.name: (pb.head.copy(), pb.tail.copy()) for pb in rig.pose.bones}

BMASK = concept_body_mask()
B_R0, B_R1, B_C0, B_C1 = 905, GROUND_ROW + 2, 380, 1080
bsub = BMASK[B_R0:B_R1:BODY_STEP, B_C0:B_C1:BODY_STEP]
# Distance (m) from the outline, by repeated erosion (alternating 4- and 8-neighbourhoods ~ octagonal distance).
bdist = np.zeros(bsub.shape, np.float32)
cur = bsub.copy()
for it in range(60):
    pad = np.pad(cur, 1)
    er = cur & pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
    if it % 2:
        er &= pad[:-2, :-2] & pad[:-2, 2:] & pad[2:, :-2] & pad[2:, 2:]
    bdist[er] += BODY_STEP * K
    cur = er
    if not cur.any():
        break


def measure_profile(bone, ext0, ext1, max_r, step=0.012, head=None, tail=None):
    """The drawing's outline across the posed bone: every `step` metres along it (from ext0 before its head to ext1
    past its tail), how far the outline reaches in front of and behind the bone. Only the run of the outline the
    bone passes through counts (not the other leg, not the forearm in front of the chest). Returns
    [(s metres from the head, front m, back m)], lightly smoothed."""
    h, t = (head, tail) if head is not None else POSED[bone]
    a = Vector(model_to_px(h.y, h.z))
    b = Vector(model_to_px(t.y, t.z))
    L = (b - a).length
    d = (b - a) / L
    n = Vector((-d.y, d.x))  # front (for a bone hanging down: forward; for the foot: up)
    out = []
    s = -ext0
    reach = int(max_r / K)
    H, Wd = BMASK.shape
    while s <= L * K + ext1 + 1e-6:
        c = a + d * (s / K)
        vals = []
        for k in range(-reach, reach + 1):
            p = c + n * k
            r_, c_ = int(round(p.x)), int(round(p.y))
            vals.append(0 <= r_ < H and 0 <= c_ < Wd and bool(BMASK[r_, c_]))
        runs, start = [], None
        for k, v in enumerate(vals + [False]):
            if v and start is None:
                start = k
            if not v and start is not None:
                runs.append((start - reach, k - 1 - reach))
                start = None
        if runs:
            lo, hi = min(runs, key=lambda r: 0 if r[0] <= 0 <= r[1] else min(abs(r[0]), abs(r[1])))
            out.append([s, max(hi, 1) * K, max(-lo, 1) * K])
        s += step
    for _ in range(2):  # smooth the pixel noise
        out = [[out[i][0]] + [(out[max(i - 1, 0)][j] + 2 * out[i][j] + out[min(i + 1, len(out) - 1)][j]) / 4 for j in (1, 2)]
               for i in range(len(out))]
    return out


def front_normal(dvec):
    """Rest-space side-plane normal of a bone direction pointing to its front (matching measure_profile)."""
    return Vector((0.0, dvec.z, -dvec.y)).normalized() if abs(dvec.y) + abs(dvec.z) > 1e-6 else Vector((0, -1, 0))


def loft_bone(bone, profile, lateral, weight, cap_r=None, mirror=None, n=18, head=None, tail=None, flip_front=False):
    """Smooth loft along the rest bone with the drawing's profile: section centre shifted by (front - back) / 2,
    depth (front + back) / 2, side half-width lateral(s, depth) (or lateral * depth). cap_r clamps the depth."""
    rh = head if head is not None else head_of(bone)
    rt = tail if tail is not None else tail_of(bone)
    L = (rt - rh).length
    dvec = (rt - rh) / L
    nf = front_normal(dvec)
    if flip_front:
        nf = -nf
    for side_map in [None] + ([mirror] if mirror else []):
        secs = []
        for s, f, bk in profile:
            if cap_r is not None:
                f, bk = min(f, cap_r), min(bk, cap_r)
            depth = (f + bk) / 2
            c = rh + dvec * s + nf * ((f - bk) / 2)
            rx = lateral(s, depth, c) if callable(lateral) else lateral * depth
            if side_map is not None:
                c = Vector((-c.x, c.y, c.z))
            secs.append((c, rx, depth))
        if side_map is None:
            w = weight
        else:
            w = (lambda p, s_, wf=weight, m=side_map: {m.get(k, k): v for k, v in wf(Vector((-p.x, p.y, p.z)), s_).items()})
        loft(secs, w, n=n)


L2R = {"UpperLeg.L": "UpperLeg.R", "LowerLeg.L": "LowerLeg.R", "Foot.L": "Foot.R", "Hips": "Hips"}
R2L = {"UpperArm.R": "UpperArm.L", "LowerArm.R": "LowerArm.L", "Hand.R": "Hand.L", "Shoulder.R": "Shoulder.L"}

torso_w = blend_z([(1.0, "Hips"), (1.1, "Spine"), (1.26, "Spine"), (1.34, "Chest")])
# ---------------------------------------------------------------- the walk sheet: the whole figure, arms clear
# ArtSource/ref/walk_cycle.png shows the hero 12 times from the side with his arms away from the body; the rig was
# fitted to every frame (hero_fit_walk.py -> ref/walk_poses.json). Posed as in each frame, every bone reads the
# frame's outline across it; the 12 readings are combined per point along the bone, taking a low percentile so the
# frames where something lies over the bone (an arm across the chest, the other leg) don't widen it.
SHEET = ROOT + r"\ArtSource\ref\walk_cycle.png"
WALK = json.load(open(ROOT + r"\ArtSource\ref\walk_poses.json"))["frames"]
S_ROWS = [(35, 372), (463, 804), (901, 1242)]
S_COLS = [(60, 300), (350, 590), (640, 880), (930, 1170)]
WALK_BONES = ["Spine", "Neck", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
              "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R"]


def sheet_frames():
    im = bpy.data.images.load(SHEET, check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1]
    dark = (0.3 * px[..., 0] + 0.59 * px[..., 1] + 0.11 * px[..., 2]) < 0.3
    out = []
    for r0, r1 in S_ROWS:
        for c0, c1 in S_COLS:
            m = dark[r0 - 5:r1 + 3, c0:c1]
            rows = np.nonzero(m.any(1))[0]
            top, ground = rows.min(), rows.max()
            k = 1.887 / (ground - top)
            head = m[top:top + int(0.12 * (ground - top))]
            out.append(dict(mask=m, ground=ground, k=k, head_col=np.nonzero(head.any(0))[0].mean()))
    return out


def pose_walk(angles):
    for pb in rig.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for name in WALK_BONES:
        pb = rig.pose.bones[name]
        M3 = pb.bone.matrix_local.to_3x3()
        pb.rotation_quaternion = (M3.inverted() @ Matrix.Rotation(math.radians(angles.get(name, 0.0)), 3, 'X') @ M3).to_quaternion()
    hp = rig.pose.bones["Hips"]
    hp.location = hp.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, -angles.get("drop", 0.0)))
    bpy.context.view_layer.update()


def measure_px(mask, to_px, k, h, t, ext0, ext1, max_r, step):
    """Outline across the line h->t (model points) in one silhouette: [front m, back m] or None every `step` m."""
    a = Vector(to_px(h.y, h.z))
    b = Vector(to_px(t.y, t.z))
    L = (b - a).length * k
    d = (b - a).normalized()
    n = Vector((-d.y, d.x))
    reach = int(max_r / k)
    H, Wd = mask.shape
    out, s = [], -ext0
    while s <= L + ext1 + 1e-6:
        c = a + d * (s / k)
        vals = []
        for q in range(-reach, reach + 1):
            p = c + n * q
            r_, c_ = int(round(p.x)), int(round(p.y))
            vals.append(0 <= r_ < H and 0 <= c_ < Wd and bool(mask[r_, c_]))
        runs, start = [], None
        for q, v in enumerate(vals + [False]):
            if v and start is None:
                start = q
            if not v and start is not None:
                runs.append((start - reach, q - 1 - reach))
                start = None
        if runs:
            lo, hi = min(runs, key=lambda r: 0 if r[0] <= 0 <= r[1] else min(abs(r[0]), abs(r[1])))
            out.append([s, max(hi, 1) * k, max(-lo, 1) * k])
        else:
            out.append([s, None, None])
        s += step
    return out


SFR = sheet_frames()


def sheet_profile(bones, ext0, ext1, max_r, pct=35, step=0.012, line=None):
    """Combined reading of the sheet for one or more (symmetric) bones; line(): custom posed (head, tail)."""
    readings = []
    for f, ang in zip(SFR, WALK):
        pose_walk(ang)
        to_px = (lambda y, z, f=f, sh=ang.get("shift", 0.0):
                 (f["ground"] - z / f["k"], f["head_col"] + (-0.05 + sh - y) / f["k"]))
        for bone in (bones or [None]):
            h, t = line() if line else (rig.pose.bones[bone].head.copy(), rig.pose.bones[bone].tail.copy())
            readings.append(measure_px(f["mask"], to_px, f["k"], h, t, ext0, ext1, max_r, step))
    n = min(len(r) for r in readings)
    out = []
    for i in range(n):
        fr = [r[i][1] for r in readings if r[i][1] is not None]
        bk = [r[i][2] for r in readings if r[i][2] is not None]
        if fr and bk:
            out.append([readings[0][i][0], float(np.percentile(fr, pct)), float(np.percentile(bk, pct))])
    for _ in range(3):  # smooth along the bone
        out = [[out[i][0]] + [(out[max(i - 1, 0)][j] + 2 * out[i][j] + out[min(i + 1, len(out) - 1)][j]) / 4 for j in (1, 2)]
               for i in range(len(out))]
    return out


# Torso: along the spine line, from below the belt to the base of the neck. Its side-to-side breadth (not in the side
# view) keeps the earlier front-view table.
T_TOP, T_BOT = POSED["Chest"][1], POSED["Hips"][0] - Vector((0, 0, 0.12))
# (The torso comes from the concept: on the walk sheet the near arm hangs over its front and the far arm over its back
# in nearly every frame.)
TORSO_PROFILE = measure_profile(None, 0.0, 0.0, 0.2, step=0.012, head=T_TOP, tail=T_BOT)
# Where the concept's arms lie over the torso it can't be read: the near elbow over the belly (front, z 0.95-1.2), the
# far hand and the sword's hilt over the hips (back, below z 1.0), the scarf's knot under the chin (front, z 1.38-1.5).
for e in TORSO_PROFILE:
    z = T_TOP.z - e[0]
    if 0.95 < z < 1.2:
        e[1] = min(e[1], 0.118)
    if z < 1.0:
        e[2] = min(e[2], 0.15)
    if 1.38 < z < 1.5:
        e[1] = max(e[1], 0.1)
T_REST_TOP, T_REST_BOT = tail_of("Chest"), head_of("Hips") - Vector((0, 0, 0.12))
loft_bone(None, TORSO_PROFILE, lambda s, dep, c: torso_at(c.z)[0], lambda p, s: torso_w(p, s), cap_r=0.17,
          head=T_REST_TOP, tail=T_REST_BOT, n=28)
_tz = [(T_REST_TOP.z - s, f, bk) for s, f, bk in TORSO_PROFILE]

# Legs (both legs of the walk sheet) - tunic, knee guard, calf, boot - and the foot (the concept's); mirrored.
leg_l = lambda p, s: along(head_of("UpperLeg.L"), head_of("LowerLeg.L"), "UpperLeg.L", "LowerLeg.L", 0.85)(p, s) \
    if p.z < head_of("UpperLeg.L").z - 0.02 else {"Hips": 0.35, "UpperLeg.L": 0.65}
# (the breeches stay narrow at the belt and balloon out below it, as drawn - side to side as well)
loft_bone("UpperLeg.L", sheet_profile(["UpperLeg.L", "UpperLeg.R"], 0.06, 0.02, 0.13, pct=55),
          lambda s, dep, c: dep * 0.85 * min(1.0, 0.55 + max(s + 0.06, 0.0) / 0.3), leg_l, cap_r=0.12, mirror=L2R)
loft_bone("LowerLeg.L", sheet_profile(["LowerLeg.L", "LowerLeg.R"], 0.02, 0.0, 0.1, pct=55),
          0.9, along(head_of("LowerLeg.L"), head_of("Foot.L"), "LowerLeg.L", "Foot.L", 0.9), cap_r=0.09, mirror=L2R)
# The foot stands flat in the drawing: read it along the ground, 4.5 cm up, from the heel (col 755) to the toe
# (col 918) of the front boot; its top above that line, the sole below. It lies flat under the ankle at rest.
_heel_y, _toe_y = px_to_model(0, 755)[0], px_to_model(0, 918)[0]
FOOT_PROFILE = measure_profile(None, 0.0, 0.0, 0.08, step=0.01, head=Vector((0, _heel_y, 0.045)), tail=Vector((0, _toe_y, 0.045)))
_ank = head_of("Foot.L")
_foot_len = _heel_y - _toe_y
loft_bone("Foot.L", FOOT_PROFILE, 0.95, lambda p, s: {"Foot.L": 1.0}, cap_r=0.07, mirror=L2R,
          head=Vector((_ank.x, _ank.y + 0.075, 0.045)), tail=Vector((_ank.x, _ank.y + 0.075 - _foot_len, 0.045)))

# Arms, after the walk sheet, where the far arm swings clear behind the body in every frame. Measured there: the upper
# arm ~0.12 m deep at the elbow and ~0.13 at the shoulder guard, the bracer 0.08-0.095 m tapering to ~0.066 at the
# wrist, the open hand ~0.08 m. Its
# top half is lost in the shoulder in the side view, so the shoulder end is completed to taper smoothly into it.
# (t: 0..1 along the bone; depth in metres)
UPPER_ARM = [(-0.1, 0.1), (0.0, 0.128), (0.2, 0.134), (0.5, 0.122), (0.8, 0.112), (1.0, 0.102), (1.06, 0.098)]  # (the guard at the shoulder)
FOREARM = [(-0.06, 0.098), (0.0, 0.096), (0.15, 0.092), (0.25, 0.09), (0.6, 0.082), (0.85, 0.074), (1.0, 0.066), (1.04, 0.063)]
HAND = [(-0.12, 0.062), (0.15, 0.076), (0.5, 0.082), (0.85, 0.074), (1.2, 0.045)]  # (the sheet's hands are open, broad)


def arm_profile(points, bone, step=0.012):
    L = (tail_of(bone) - head_of(bone)).length
    out = []
    t = points[0][0]
    while t <= points[-1][0] + 1e-6:
        for (t0, d0), (t1, d1) in zip(points, points[1:]):
            if t0 <= t <= t1:
                u = (t - t0) / (t1 - t0)
                u = u * u * (3 - 2 * u)
                dep = d0 + (d1 - d0) * u
                break
        out.append([t * L, dep / 2, dep / 2])
        t += step / L
    return out


loft_bone("UpperArm.R", arm_profile(UPPER_ARM, "UpperArm.R"), 0.95,
          along(head_of("UpperArm.R"), head_of("LowerArm.R"), "UpperArm.R", "LowerArm.R", 0.85), mirror=R2L, n=16)
loft_bone("LowerArm.R", arm_profile(FOREARM, "LowerArm.R"), 0.9,
          along(head_of("LowerArm.R"), head_of("Hand.R"), "LowerArm.R", "Hand.R", 0.9), mirror=R2L, n=16)
loft_bone("Hand.R", arm_profile(HAND, "Hand.R", step=0.01), 0.8, lambda p, s: {"Hand.R": 1.0}, mirror=R2L, n=14)

# Back to the rest pose.
for pb in rig.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)
bpy.context.view_layer.update()

_old_torso_at = torso_at


def torso_at(z):
    """Half width (front-view table), half depth and centre y of the torso at height z, from the drawing."""
    best = min(_tz, key=lambda e: abs(e[0] - z))
    f, bk = min(best[1], 0.17), min(best[2], 0.17)
    return [_old_torso_at(z)[0], (f + bk) / 2, -(f - bk) / 2]


# A crimson line across the chest (the stitched trim in the art).
rx, ry, cy = torso_at(1.40)
loft([((0, cy, 1.392), rx + 0.006, ry + 0.006), ((0, cy, 1.408), rx + 0.006, ry + 0.006)],
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

# ---------------------------------------------------------------- pauldrons
# Stacked over each shoulder cap (seen from the front; from the side they stay within the drawing's shoulder).
for side, sx in (("L", 1), ("R", -1)):
    sh = head_of("UpperArm." + side)
    ua = "UpperArm." + side
    for k, (r, d) in enumerate(((0.085, 0.026), (0.075, 0.022), (0.065, 0.019))):
        out = Vector((sx * (0.55 + 0.3 * k), 0.0, 1.0 - 0.25 * k)).normalized()
        c = sh + Vector((sx * (0.0 + 0.02 * k), 0.005, 0.03 - 0.04 * k))
        w = (lambda b: (lambda p, s: {b: 1.0}))(ua) if k else (lambda p, s, a=ua, sd=side: {a: 0.6, "Shoulder." + sd: 0.4})
        dome(c, out, r, d, w, squash=0.9, up=(0, 1, 0))

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
    quad = [q for q in ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j)) if q in front]
    if len(quad) >= 3:  # a full cell, or a step of the outline (three corners): no holes between the rows
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
