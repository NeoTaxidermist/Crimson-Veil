# Builds the hanged man (HangedMan.blend -> Assets/_CrimsonVeil/Art/Enemy/HangedMan.fbx), rebuilt to stand beside the
# hero (v0.102): a starved corpse - every rib under the skin, the collarbones, the sunken belly, the hip bones and the
# shoulder blades; a long neck stretched by the rope, the head fallen to his left (the eyelids half shut over sunken
# eyes, hollow cheeks, the jaw hanging slack with the crimson tongue in it, lank hair in clumps, the ears); a shirt
# torn open down the front and off one shoulder, ragged into tails; breeches torn off below the knee; wasted arms
# with knobbed elbows, hands of four long fingers and a thumb hanging limp; bare shins, ankle bones, bony feet with
# their toes; the crimson noose - a twisted rope with the hangman's coils behind his ear; an iron shackle on the left
# ankle with a broken chain.
# Built standing, feet at the origin, the neck at 1.7 m (the game hangs him there). The skeleton and its names are
# the old one's (HangedMan.cs, SpringBones): limbs and head are loose bone chains Tatter.<n>.<k> (0 / 1 the arms,
# 2 / 3 the legs, 4 the neck and head) that trail the swing and flop when he falls; 5..10 the shirt's tails.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\hanged_build.py", encoding="utf-8").read())
exec(open(r"C:\Dev\CrimsonVeil\ArtSource\sculpt_kit.py", encoding="utf-8").read())
from mathutils import Matrix

BONES = []


def bone(name, head, tail, parent):
    BONES.append((name, V(*head), V(*tail), parent))


bone("Hips", (0, 0.0, 0.92), (0, 0.0, 1.12), None)
bone("Spine", (0, 0.0, 1.12), (0, 0.01, 1.34), "Hips")
bone("Chest", (0, 0.01, 1.34), (0, 0.02, 1.58), "Spine")
ARM = {"L": [V(0.19, 0.02, 1.53), V(0.22, 0.03, 1.24), V(0.23, 0.0, 0.98), V(0.235, -0.01, 0.84)]}
ARM["R"] = [V(-p.x, p.y, p.z) for p in ARM["L"]]
LEG = {"L": [V(0.09, 0.0, 0.93), V(0.1, 0.0, 0.5), V(0.105, 0.02, 0.09), V(0.11, -0.12, 0.03)]}
LEG["R"] = [V(-0.09, 0.0, 0.93), V(-0.085, 0.01, 0.51), V(-0.08, 0.04, 0.1), V(-0.08, -0.1, 0.02)]
HEAD = [V(0, 0.02, 1.58), V(0.03, 0.0, 1.70), V(0.10, -0.03, 1.86)]   # the neck broken, the head fallen to his left
for n, pts in ((0, ARM["L"]), (1, ARM["R"]), (2, LEG["L"]), (3, LEG["R"])):
    parent = "Chest" if n < 2 else "Hips"
    for k in range(3):
        bone("Tatter.%d.%d" % (n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = "Tatter.%d.%d" % (n, k)
bone("Tatter.4.0", tuple(HEAD[0]), tuple(HEAD[1]), "Chest")
bone("Tatter.4.1", tuple(HEAD[1]), tuple(HEAD[2]), "Tatter.4.0")


def T(n, k):
    return "Tatter.%d.%d" % (n, k)


def seg_w(n, k):
    return lambda p, s: {T(n, k): 1.0}


def zone(p, s):
    z = p.z
    if z > 1.5 and abs(p.x) > 0.12:   # the shoulders' points go with the arms
        n = 0 if p.x > 0 else 1
        t = min(1.0, (abs(p.x) - 0.12) / 0.06)
        return {"Chest": 1 - 0.6 * t, T(n, 0): 0.6 * t}
    if z > 1.36:
        return {"Chest": 1.0}
    if z > 1.18:
        t = (z - 1.18) / 0.18
        return {"Spine": 1 - t, "Chest": t}
    if z > 1.0:
        t = (z - 1.0) / 0.18
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


def rotate_from(i0, pivot, frm, to):
    """Turns the vertices built since index i0 about pivot, from direction frm to direction to (a tilted part)."""
    q = frm.rotation_difference(to)
    m = q.to_matrix()
    for i in range(i0, len(verts)):
        verts[i] = pivot + m @ (verts[i] - pivot)


# ---------------------------------------------------------------- the starved body
# (z, half width, half depth, centre y): a narrow waist under a ribcage that flares, the shoulders thin and sloping
TRUNK = [(0.88, 0.14, 0.098, 0.01), (0.95, 0.128, 0.09, 0.01), (1.02, 0.106, 0.078, 0.01), (1.08, 0.1, 0.072, 0.008),
         (1.14, 0.118, 0.088, 0.008), (1.22, 0.135, 0.1, 0.01), (1.30, 0.148, 0.106, 0.012), (1.38, 0.157, 0.108, 0.014),
         (1.45, 0.166, 0.1, 0.016), (1.51, 0.172, 0.09, 0.018), (1.55, 0.15, 0.075, 0.02), (1.585, 0.09, 0.058, 0.02),
         (1.61, 0.05, 0.045, 0.02)]
RIBS = [1.475, 1.435, 1.395, 1.355, 1.315, 1.275, 1.235, 1.2]


def trunk_disp(z, th, p, out):
    fy, side = math.sin(th), math.cos(th)
    d = 0.0
    front = max(0.0, fy + 0.35) / 1.35
    for n, zr in enumerate(RIBS):
        zc = zr - 0.07 * front ** 1.3 - 0.01 * n * front
        if fy > 0.45 and abs(side) < 0.05 + 0.04 * max(0, n - 2):
            continue
        k = 0.011 * front ** 0.7 * (1.0 - 0.06 * n) * (0.85 + 0.3 * noise.noise(Vector((n * 1.3, th * 0.7, 0.4))))
        d += k * g2(z - zc, 0.0075) - 0.45 * k * g2(z - zc - 0.019, 0.0085)
    d *= 1.0 if z > 1.17 else 0.0
    d += 0.007 * g2(side, 0.07) * max(0.0, fy) * g2(z - 1.38, 0.09)                    # the breastbone
    d += 0.01 * g2(z - (1.535 - 0.012 * abs(side)), 0.011) * max(0.0, fy)                # the collarbones
    d -= 0.011 * g2(z - 1.56, 0.012) * max(0.0, fy) * g2(abs(side) - 0.5, 0.25)          # the hollows over them
    d -= 0.024 * g2(z - 1.09, 0.05) * max(0.0, fy) ** 1.2                                 # the belly sunk in
    d += 0.013 * g2(z - 0.99, 0.03) * g2(abs(side) - 0.75, 0.2)                           # the hip bones
    back = max(0.0, -fy)
    d += 0.008 * g2(side, 0.09) * back * (0.5 + 0.5 * math.cos(2 * math.pi * z / 0.036))  # the spine's knuckles
    for sx in (1, -1):
        d += 0.015 * g2(z - 1.44, 0.06) * g2(side - 0.45 * sx, 0.22) * back              # the shoulder blades
    return out * d


ring_loft(dense(TRUNK, 6), zone, n=56, disp=trunk_disp)

# the neck: long, stretched by the rope, the windpipe and the sinews standing out (tilted with the head's fall)
_n0 = len(verts)
NECK = [(0.0, 0.048, 0.044), (0.3, 0.038, 0.037), (0.6, 0.034, 0.034), (0.85, 0.036, 0.036), (1.05, 0.04, 0.04)]


def neck_disp(i, k, p, c):
    th = 2 * math.pi * k / 24
    fy, side = math.sin(th), math.cos(th)
    r = (p - c).normalized()
    return r * (0.006 * g2(side, 0.25) * max(0.0, fy) + 0.007 * g2(abs(side) - 0.6, 0.18) + 0.005 * g2(side, 0.2) * max(0.0, -fy) * (0.5 + 0.5 * math.cos(i * 2.6)))


loft([(HEAD[0].lerp(HEAD[1], u), rx, ry) for u, rx, ry in NECK],
     lambda p, s: {"Chest": 0.4, T(4, 0): 0.6} if s < 0.25 else ({T(4, 0): 0.5, T(4, 1): 0.5} if s > 0.85 else {T(4, 0): 1.0}),
     n=24, disp=neck_disp, cap0=False)

# ---------------------------------------------------------------- the head, fallen to his left
# Built upright about its centre, then turned to lie along the head bone.
HC = V(0, -0.005, 0.0)        # (local: its centre at the origin, z up, the face towards -y)
SK = (0.074, 0.088, 0.1)
_h0 = len(verts)
EYE_UP, EYE_LAT = 0.13, 0.36


def dead_face(d):
    fwd = max(0.0, -d.y)
    lat, up = d.x, d.z
    f = -0.15 * fwd ** 3
    for sx in (1, -1):
        sock = g2(lat - EYE_LAT * sx, 0.15) * g2(up - EYE_UP, 0.13) * fwd
        f -= 0.22 * sock                                                       # sunken sockets
        f += 0.06 * g2(lat - EYE_LAT * sx, 0.12) * g2(up - EYE_UP + 0.02, 0.06) * fwd   # the half-shut lids in them
        f += 0.14 * g2(lat - 0.6 * sx, 0.13) * g2(up + 0.04, 0.09) * fwd     # cheekbones, sharp
        f -= 0.13 * g2(lat - 0.5 * sx, 0.15) * g2(up + 0.32, 0.13) * fwd     # the cheeks sucked in
        f -= 0.1 * g2(lat - 0.9 * sx, 0.16) * g2(up - 0.18, 0.2)             # the temples
        f += 0.05 * g2(lat - 0.62 * sx, 0.1) * g2(up + 0.7, 0.08) * fwd      # the jaw's corners
    f += 0.1 * g2(up - EYE_UP - 0.15, 0.06) * fwd * g2(lat, 0.62)             # the brow
    nose = g2(lat, 0.1) * fwd ** 2
    f += 0.24 * nose * g2(up + 0.05, 0.13)                                     # a thin nose
    f -= 0.05 * g2(lat - 0.1, 0.04) * g2(up + 0.17, 0.03) * fwd - 0.0         # a nostril
    f -= 0.05 * g2(lat + 0.1, 0.04) * g2(up + 0.17, 0.03) * fwd
    mouth = g2(up + 0.52, 0.09) * fwd * g2(lat, 0.38)
    f -= 0.3 * mouth                                                            # the mouth hanging open
    if up < -0.45:
        f -= 0.25 * (-0.45 - up) * (1 - fwd)                                   # the jaw narrow, dropped
    f += 0.02 * noise.noise(Vector((d.x * 6, d.y * 6, d.z * 6)))               # the skin drawn and uneven
    return f


blob(HC, 1.0, seg_w(4, 1), seg=56, rings=40, scale=SK, disp=dead_face)
# the lids' closed slits (thin dark lines in the sockets: half shut, nothing shines out of a dead man)
for sx in (1, -1):
    d = V(EYE_LAT * sx, -0.93, EYE_UP).normalized()
    e = HC + V(SK[0] * d.x, SK[1] * d.y, SK[2] * d.z) * 0.92
    loft([(e + V(sx * 0.011 * u, -0.001, -0.002 * u * u), 0.0035, 0.0025) for u in (-1.0, -0.5, 0.0, 0.5, 1.0)], seg_w(4, 1), n=6)
# the crimson tongue lolling in the open mouth
_m = HC + V(0, -SK[1] * 0.78, -SK[2] * 0.5)
loft([(_m + V(0, -0.012 * u, -0.012 * u * u), 0.016 - 0.004 * u, 0.006) for u in (0.0, 0.4, 0.8, 1.1)], seg_w(4, 1), mat=EYES, n=10)
# the ears
for sx in (1, -1):
    blob(HC + V(SK[0] * 1.0 * sx, 0.005, 0.0), 0.024, seg_w(4, 1), seg=12, rings=8, scale=(0.35, 0.8, 1.3),
         disp=lambda d, sx=sx: -0.25 * g2(d.x * sx - 1.0, 0.5) * g2(d.z, 0.5))
_hc_world = HEAD[1].lerp(HEAD[2], 0.55)
for i in range(_h0, len(verts)):
    verts[i] = verts[i] + _hc_world
_hup = (HEAD[2] - HEAD[1]).normalized()
rotate_from(_h0, _hc_world, V(0, 0, 1), _hup)
_hrot = V(0, 0, 1).rotation_difference(_hup).to_matrix()
# lank hair in clumps: rooted on the tilted skull's crown and back, falling straight down (gravity, not the skull's
# down), flat and heavy, not pointed
for k in range(19):
    a = math.radians(-100 + k * 20 + 6 * noise.noise(Vector((k * 0.7, 0.2, 0))))
    el = 0.55 + 0.25 * (0.5 + 0.5 * noise.noise(Vector((k * 1.9, 0.7, 0))))
    loc = V(SK[0] * 0.98 * math.cos(a) * math.sqrt(1 - el * el), (SK[1] * 0.9 * math.sin(a) + 0.01) * math.sqrt(1 - el * el), SK[2] * el)
    top = _hc_world + _hrot @ loc
    outw = (top - _hc_world)
    outw = V(outw.x, outw.y, 0).normalized() if V(outw.x, outw.y, 0).length > 1e-4 else V(0, 1, 0)
    L = 0.16 + 0.09 * (0.5 + 0.5 * noise.noise(Vector((k * 1.3, 1.0, 0))))
    pts = [top + outw * (0.025 * math.sin(math.pi * min(1.0, u * 1.6))) + V(0, 0, -L * u) for u in (0.0, 0.2, 0.45, 0.7, 1.0)]

    def clump(i, kk, p, c, k=k):
        return (p - c) * 0.3 * noise.noise(Vector((i * 1.1, kk * 0.8, k * 2.0)))
    loft([(q, 0.014 * (1 - 0.55 * u), 0.007 * (1 - 0.5 * u)) for q, u in zip(pts, (0.0, 0.2, 0.45, 0.7, 1.0))], seg_w(4, 1), n=8, disp=clump)

# ---------------------------------------------------------------- the noose: crimson, twisted, the coils behind his ear
_nc = HEAD[0].lerp(HEAD[1], 0.62)
_naxis = (HEAD[1] - HEAD[0]).normalized()
_nu, _nv = frame(_naxis)
for strand in range(2):
    ph = math.pi * strand
    pts = []
    for k in range(48):
        a = 2 * math.pi * k / 48
        tw = 0.006 * math.cos(3 * a + ph)
        pts.append(_nc + (_nu * math.cos(a) + _nv * math.sin(a)) * (0.05 + tw) + _naxis * (0.006 * math.sin(3 * a + ph) - 0.01 * math.cos(a)))
    tube_loop(pts, 0.0085, seg_w(4, 0), mat=EYES, n=8, normal=_naxis)
# the hangman's knot: coils wound one over another, behind his right ear (the head falls the other way)
_kn = _nc + _nu * (-0.055) + _nv * 0.02 + _naxis * 0.02
for j in range(7):
    c = _kn + _naxis * (0.0105 * j - 0.03)
    tube_loop([c + (_nu * math.cos(2 * math.pi * q / 16) + _nv * math.sin(2 * math.pi * q / 16)) * 0.015 for q in range(16)], 0.0058,
              seg_w(4, 0), mat=EYES, n=8, normal=_naxis)
# its standing end, up towards the beam (the game draws the rope on from there)
loft([(_kn + _naxis * (0.06 + 0.05 * u) + _nv * (0.01 * u), 0.012, 0.012) for u in (0.0, 0.5, 1.0)], seg_w(4, 0), mat=EYES, n=10)

# ---------------------------------------------------------------- the shirt: torn open down the front, ragged
SHIRT = dense([(1.6, 0.075, 0.062, 0.02), (1.565, 0.165, 0.1, 0.02), (1.52, 0.188, 0.112, 0.018), (1.42, 0.183, 0.12, 0.015),
               (1.28, 0.168, 0.118, 0.012), (1.14, 0.152, 0.11, 0.01), (1.04, 0.16, 0.114, 0.01), (0.96, 0.176, 0.122, 0.01)], 4)
_sf = folds(8, 0.022, 1.45, 1.0, 3.0)


def shirt_disp(z, th, p, out):
    d = _sf(z, th, p, out)
    if z < 0.961:
        d = d + V(0, 0, -0.05 * (0.5 + 0.5 * noise.noise(Vector((th * 5.0, 2.0, 0.0)))) - 0.02 * math.sin(th * 7))
    return d


def shirt_keep(z, th):
    fy, side = math.sin(th), math.cos(th)
    # torn open down the front, the rip widening towards the belly, its edges ragged
    width = 0.24 + 0.3 * max(0.0, (1.5 - z) / 0.5) + 0.07 * noise.noise(Vector((z * 18.0, 1.3, 0.0)))
    if fy > 0.3 and abs(side) < width and z < 1.53:
        return False
    # holes worn through at the back and the side
    if noise.noise(Vector((th * 3.0, z * 9.0, 4.0))) > 0.62:
        return False
    return True


ring_loft(SHIRT, zone, n=56, disp=shirt_disp, keep=shirt_keep)
# the left sleeve torn off at the shoulder; the right one hanging in tatters to the elbow
for n, side in ((0, "L"), (1, "R")):
    a, b = ARM[side][0], ARM[side][1]
    ln = 0.35 if n == 0 else 0.85

    def sleeve(i, k, p, c, n=n):
        th = 2 * math.pi * k / 28
        tear = 0.0
        if i == 3:
            tear = 0.03 + 0.05 * (0.5 + 0.5 * noise.noise(Vector((k * 0.6, 3.0 + n, 0.0))))
        return (p - c) * (0.14 * math.sin(5 * th + i)) + V(0, 0, -tear)
    a = a + V(-0.03 if n == 0 else 0.03, 0, 0.005)   # (rising out of the shirt, not a box round the shoulder)
    loft([(a.lerp(b, u * ln), r, r) for u, r in ((0.0, 0.05), (0.35, 0.054), (0.7, 0.055), (1.0, 0.058))],
         lambda p, s, n=n: {T(n, 0): 1.0} if s > 0.15 else {"Chest": 0.4, T(n, 0): 0.6}, n=28, cap0=False, cap1=False, disp=sleeve)

# ---------------------------------------------------------------- the breeches: belted with a cord, torn off below the knee
BREECH = dense([(1.03, 0.146, 0.104, 0.01), (0.96, 0.155, 0.11, 0.01), (0.88, 0.17, 0.118, 0.01), (0.8, 0.175, 0.12, 0.012)], 3)
_bf = folds(9, 0.014, 0.98, 0.82, 6.0)
ring_loft(BREECH, zone, n=48, disp=lambda z, th, p, out: _bf(z, th, p, out))
tube_loop([V(0.15 * math.cos(2 * math.pi * k / 40), 0.01 - 0.107 * math.sin(2 * math.pi * k / 40), 1.02) for k in range(40)], 0.008, zone, n=8)
for n, side in ((2, "L"), (3, "R")):
    p = LEG[side]
    a, b = p[0] + V(0, 0, -0.04), p[1]

    def leg_cloth(i, k, pp, c, n=n):
        th = 2 * math.pi * k / 28
        tear = 0.0
        if i == 4:
            tear = 0.04 + 0.08 * (0.5 + 0.5 * noise.noise(Vector((k * 0.5, 6.0 + n, 0.0))))
        return (pp - c) * (0.1 * math.sin(6 * th + 2 * i)) + V(0, 0, -tear)
    loft([(a.lerp(b, u), r, r * 0.95) for u, r in ((0.0, 0.1), (0.3, 0.094), (0.6, 0.086), (0.9, 0.082), (1.1, 0.08))],
         along(a, b, T(n, 0), T(n, 1), 0.85), n=28, cap0=False, cap1=False, disp=leg_cloth)

# ---------------------------------------------------------------- arms: wasted, knobbed elbows, limp hands
for n, side in ((0, "L"), (1, "R")):
    p = ARM[side]
    sx = 1 if side == "L" else -1
    a, b = p[0] + V(-0.02 * sx, 0, -0.01), p[1]   # (from inside the shoulder: no knob standing out of the shirt)
    loft([(a.lerp(b, u), r, r * 0.95) for u, r in ((0.0, 0.044), (0.2, 0.041), (0.5, 0.035), (0.8, 0.031), (1.0, 0.034))],
         along(a, b, T(n, 0), T(n, 1), 0.8), n=20)
    blob(b + V(0, 0.012, 0), 0.034, lambda q, s, n=n: {T(n, 0): 0.5, T(n, 1): 0.5}, seg=16, rings=12, scale=(0.9, 1.2, 1.0))
    a, b = p[1], p[2]

    def fore(i, k, pp, c):
        th = 2 * math.pi * k / 20
        return (pp - c).normalized() * (0.004 * math.cos(2 * th) + 0.004 * g2(math.sin(th) + 1, 0.4) * (1 - i / 6))
    loft([(a.lerp(b, u), r, r * 0.85) for u, r in ((0.0, 0.032), (0.15, 0.034), (0.4, 0.029), (0.7, 0.024), (0.9, 0.02), (1.0, 0.022))],
         along(a, b, T(n, 1), T(n, 2), 0.9), n=20, disp=fore)
    blob(b + V(0.006 * sx, 0.004, 0), 0.013, seg_w(n, 1), seg=12, rings=8)     # the wrist's knob
    # the hand hanging: a narrow palm, four long fingers curled a little, the thumb along them
    a, b = p[2], p[3]
    d = (b - a).normalized()
    palm_in = V(-sx, 0, 0)
    spread = d.cross(palm_in).normalized()
    loft([(a.lerp(b, u), rx, r) for u, rx, r in ((-0.05, 0.016, 0.022), (0.5, 0.018, 0.032), (1.0, 0.016, 0.031))], seg_w(n, 2), n=16,
         disp=lambda i, k, pp, c: (pp - c).normalized() * 0.003 * math.cos(3 * 2 * math.pi * k / 16))
    for f, (off, L) in enumerate(((-0.022, 0.08), (-0.007, 0.09), (0.008, 0.086), (0.022, 0.07))):
        base = b + spread * off
        pts, dirn = [base], d.copy()
        for j, frac in enumerate((0.45, 0.32, 0.23)):
            dirn = (dirn + palm_in * math.tan(math.radians(12 + 8 * j))).normalized()
            pts.append(pts[-1] + dirn * L * frac)
        secs = []
        for j in range(3):
            for u in (0.0, 0.35, 0.7, 1.0):
                r0 = 0.0085 * (1.0 - 0.15 * j)
                knuckle = 1.0 + 0.3 * g2(u, 0.14)
                taper = (1.0 - 0.45 * u ** 2) if j == 2 else 1.0
                secs.append((pts[j].lerp(pts[j + 1], u), r0 * knuckle * taper, r0 * knuckle * taper * 0.85))
        loft(secs, seg_w(n, 2), n=10)
    tb = a.lerp(b, 0.7) + spread * (-0.025) + palm_in * 0.008
    tdir = (d * 0.8 + spread * (-0.3) + palm_in * 0.3).normalized()
    loft([(tb, 0.01, 0.01), (tb + tdir * 0.03, 0.009, 0.009), (tb + tdir * 0.055, 0.008, 0.008), (tb + tdir * 0.07, 0.004, 0.004)], seg_w(n, 2), n=10)

# ---------------------------------------------------------------- legs below the breeches: bare shins, bony feet
for n, side in ((2, "L"), (3, "R")):
    p = LEG[side]
    a, b = p[0], p[1]
    loft([(a.lerp(b, u), r, r * 0.95) for u, r in ((0.0, 0.075), (0.4, 0.066), (0.8, 0.055), (1.0, 0.05))], along(a, b, T(n, 0), T(n, 1), 0.85), n=20)
    blob(b + V(0, -0.012, 0), 0.05, lambda q, s, n=n: {T(n, 0): 0.5, T(n, 1): 0.5}, seg=16, rings=12, scale=(1.0, 0.9, 1.1))   # the knee
    a, b = p[1], p[2]

    def shin(i, k, pp, c):
        th = 2 * math.pi * k / 22
        fy = math.sin(th)
        return (pp - c).normalized() * (0.007 * g2(math.cos(th), 0.25) * max(0.0, fy) - 0.004 * g2(fy + 1, 0.5) * (1 - abs(i - 2.5) / 3) + 0.01 * max(0.0, -fy) * g2(i - 1.5, 1.2))
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.046), (0.2, 0.045), (0.4, 0.04), (0.65, 0.031), (0.85, 0.026), (1.0, 0.027))],
         along(a, b, T(n, 1), T(n, 2), 0.9), n=22, disp=shin)
    for sx in (1, -1):
        blob(b + V(0.02 * sx, 0, 0.008), 0.011, seg_w(n, 1), seg=10, rings=8, scale=(0.7, 1.0, 1.0))   # the ankle bones, under the skin
    # the foot: one piece from the heel through a bony arch to the ball, hanging toes-down; five toes out of it
    a, b = p[2], p[3]
    d = (b - a).normalized()
    heel = a + V(0, 0.03, -0.022)
    sp = d.cross(V(0, 0, 1)).normalized()

    def foot(i, k, pp, c):
        th = 2 * math.pi * k / 18
        return (pp - c).normalized() * (0.004 * math.cos(5 * th) * (i / 6) + 0.006 * max(0.0, math.sin(th)) * g2(i - 2.5, 1.5))
    loft([(heel.lerp(a, 0.4), 0.022, 0.024), (a, 0.03, 0.034), (a.lerp(b, 0.3), 0.034, 0.03), (a.lerp(b, 0.6), 0.038, 0.025),
          (a.lerp(b, 0.85), 0.041, 0.022), (a.lerp(b, 1.0), 0.04, 0.02), (a.lerp(b, 1.08), 0.034, 0.016)], seg_w(n, 2), n=18, disp=foot)
    blob(heel, 0.026, seg_w(n, 2), seg=14, rings=10, scale=(0.95, 1.1, 0.9))
    for t in range(5):
        b0 = b + sp * (-0.026 + 0.013 * t) - d * 0.012
        L = (0.042, 0.036, 0.032, 0.028, 0.024)[t]
        r = (0.0125, 0.0095, 0.009, 0.0085, 0.008)[t]
        loft([(b0, r, r * 0.9), (b0 + d * L * 0.45 + V(0, 0, -0.004), r, r * 0.85), (b0 + d * L * 0.8 + V(0, 0, -0.01), r * 0.9, r * 0.8),
              (b0 + d * L + V(0, 0, -0.016), r * 0.55, r * 0.5)], seg_w(n, 2), n=10)

# ---------------------------------------------------------------- the shackle on the left ankle and its broken chain
_a0 = LEG["L"][2] + V(0, 0, 0.05)
tube_loop([_a0 + V(0.048 * math.cos(2 * math.pi * k / 28), 0.045 * math.sin(2 * math.pi * k / 28), 0.004 * math.sin(4 * math.pi * k / 28)) for k in range(28)],
          lambda i: 0.011 + 0.003 * (i % 7 == 0), seg_w(2, 1), n=8)
for j in range(6):
    c = _a0 + V(0.05 + 0.004 * j, 0.004 * j, -0.06 * (j + 1))
    rot = j % 2
    tube_loop([c + (V(0.018 * math.cos(2 * math.pi * q / 16), 0.0, 0.028 * math.sin(2 * math.pi * q / 16)) if rot else
                    V(0.0, 0.018 * math.cos(2 * math.pi * q / 16), 0.028 * math.sin(2 * math.pi * q / 16))) for q in range(16)],
              0.0065, seg_w(2, 2), n=6, normal=V(0, 1, 0) if rot else V(1, 0, 0))

# ---------------------------------------------------------------- the shirt's tails (SpringBones swings them)
for j in range(6):
    n = 5 + j
    ang = math.radians(-160 + j * 60 + 8 * noise.noise(Vector((j * 0.9, 0.3, 0))))
    if -0.3 < math.sin(ang) and math.sin(ang) > 0.85:
        ang += 0.5   # (not in the rip at the front)
    top = V(0.18 * math.cos(ang), 0.01 + 0.122 * math.sin(ang), 0.93)
    length = 0.2 + 0.08 * (0.5 + 0.5 * noise.noise(Vector((j * 1.7, 2.0, 0))))
    bottom = top + V(0.015 * math.cos(ang), 0.02, -length)
    pts = [top.lerp(bottom, k / 3.0) for k in range(4)]
    parent = "Hips"
    for k in range(3):
        bone(T(n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = T(n, k)
    secs, sw = [], []
    al = (math.sin(ang), math.cos(ang))
    for k in range(3):
        for u in ((0.0, 0.5, 1.0) if k == 0 else (0.5, 1.0)):
            f = (k + u) / 3.0
            w = 0.07 * (1.0 - 0.55 * f) * (0.15 if k == 2 and u == 1.0 else 1.0)
            secs.append((pts[k].lerp(pts[k + 1], u), max(0.01, w * abs(al[0])), max(0.01, w * abs(al[1]))))
            sw.append({"Hips": 0.6, T(n, 0): 0.4} if (k == 0 and u == 0.0) else {T(n, k): 1.0})
    loft(secs, piecewise(sw), n=10, disp=lambda i, k, p, c, n=n: (p - c) * 0.25 * noise.noise(Vector((i * 0.8, k * 0.9, n * 1.3))))

# ---------------------------------------------------------------- armature + mesh
arm = bpy.data.armatures.new("HangedManRig")
rig = bpy.data.objects.new("HangedManRig", arm)
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
        b.use_connect = parent.startswith("Tatter.") and (eb[parent].tail - h).length < 1e-5
with bpy.context.temp_override(**_ctx):
    bpy.ops.object.mode_set(mode='OBJECT')

me = bpy.data.meshes.new("HangedMan")
me.from_pydata([tuple(v) for v in verts], [], faces)
me.validate()
me.update()
me.shade_smooth()
for name, col in (("HangedInk", (0.07, 0.07, 0.08, 1)), ("HangedCrimson", (0.9, 0.02, 0.04, 1))):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = col
    me.materials.append(m)
for poly, m in zip(me.polygons, fmat):
    poly.material_index = m
ob = bpy.data.objects.new("HangedMan", me)
bpy.context.scene.collection.objects.link(ob)
ob.parent = rig
groups = {}
for vi, w in enumerate(vweights):
    tot = sum(w.values())
    for bn, x in w.items():
        g = groups.get(bn) or ob.vertex_groups.new(name=bn)
        groups[bn] = g
        g.add([vi], x / tot, 'REPLACE')
bpy.context.view_layer.objects.active = ob
for o in bpy.context.scene.objects:
    o.select_set(o == ob)
sol = ob.modifiers.new("Thick", 'SOLIDIFY'); sol.thickness = 0.006; sol.offset = -1.0; sol.use_even_offset = False
sub = ob.modifiers.new("Smooth", 'SUBSURF'); sub.levels = 1; sub.render_levels = 1
dec = ob.modifiers.new("Thin", 'DECIMATE'); dec.ratio = DECIMATE_RATIO
if not globals().get("RAW", False):
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=ob, object=ob, selected_objects=[ob]):
        for name in ("Thick", "Smooth", "Thin"):
            bpy.ops.object.modifier_apply(modifier=name)
mod = ob.modifiers.new("Armature", 'ARMATURE')
mod.object = rig

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\HangedMan.blend")
result = {"verts": len(me.vertices), "faces": len(me.polygons), "bones": len(BONES), "dims": [round(x, 3) for x in ob.dimensions]}
if globals().get("EXPORT", True):
    for o in bpy.context.scene.objects:
        o.select_set(o in (rig, ob))
    with bpy.context.temp_override(window=_win, screen=_win.screen, active_object=rig, selected_objects=[rig, ob]):
        bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Enemy\HangedMan.fbx", use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                 bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
