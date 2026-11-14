# Shield monk — run in Blender after creature_kit.reset() (in a separate call).
import math
import bpy
from mathutils import Vector
import creature_kit as K

# ---------------- body (robe, mantle, hood, arms, belt, beads)
body = K.Part("Robe")


def folds(th, i, r, y):
    f = 1 + 0.07 * (1 - min(y / 1.5, 1)) * math.sin(9 * th + 0.8 * math.sin(3 * th))
    back = max(0.0, -math.cos(th))  # theta = pi is the back (-x): a hunched back
    f += 0.08 * back * math.exp(-((y - 1.3) / 0.18) ** 2)
    return f


def hem(th, i, y):
    if i > 1:
        return 0.0
    return -(0.07 * max(0.0, math.sin(11 * th + 1.3)) ** 3 + 0.04 * max(0.0, math.sin(23 * th)) ** 4)


body.lathe([(0.41, 0.0), (0.43, 0.06), (0.39, 0.4), (0.33, 0.78), (0.29, 0.95), (0.31, 1.15), (0.35, 1.33), (0.37, 1.44),
            (0.31, 1.54), (0.16, 1.61), (0.12, 1.66)], sides=48, sz=0.72, rfunc=folds, yfunc=hem, cap_bottom=False)


def mantle_hem(th, i, y):
    if i > 0:
        return 0.0
    return -0.09 * max(0.0, math.sin(8 * th + 0.4)) ** 2 - 0.03 * abs(math.sin(19 * th))


body.lathe([(0.44, 1.18), (0.42, 1.3), (0.38, 1.45), (0.28, 1.56), (0.16, 1.64)], sides=40, sz=0.74, yfunc=mantle_hem,
           rfunc=lambda th, i, r, y: 1 + 0.05 * math.sin(7 * th), cap_bottom=False)

# Hood: a long peaked cowl whose tip droops back, opening towards +x.
hood_rings = body.lathe([(0.15, 1.58), (0.21, 1.64), (0.24, 1.72), (0.25, 1.8), (0.24, 1.88), (0.21, 1.95), (0.17, 2.01),
                         (0.12, 2.07), (0.08, 2.12), (0.045, 2.17), (0.02, 2.21), (0.004, 2.24)],
                        sides=32, center=(0.03, 0, 0), sx=1.05, sz=0.92)
for ring in hood_rings:
    for v in ring:
        g = Vector((-v.co.x, v.co.z, -v.co.y))
        if g.y > 1.86:  # the peak sweeps back and droops
            k = (g.y - 1.86) / 0.38
            g.x -= 0.42 * k ** 1.6
            g.y -= 0.1 * k ** 3
        if g.x > 0.12 and 1.68 < g.y < 1.9:  # the face is a deep hollow
            g.x -= 0.07
        v.co = K.G(g.x, g.y, g.z)
# Eyes: two crimson slits deep in the hood.
body.blob((0.21, 1.79, -0.07), (0.035, 0.017, 0.02), segs=10, rings=6, mat=K.CRIMSON)
body.blob((0.205, 1.79, 0.06), (0.03, 0.015, 0.02), segs=10, rings=6, mat=K.CRIMSON)

# Front (shield) arm: bell sleeve, a hand gripping the shield.
body.tube([(0.02, 1.46, -0.25), (0.14, 1.28, -0.28), (0.25, 1.14, -0.3), (0.38, 1.1, -0.31), (0.44, 1.09, -0.31)],
          [0.1, 0.1, 0.11, 0.14, 0.17], sides=12)
body.blob((0.5, 1.08, -0.3), (0.06, 0.07, 0.055), segs=10, rings=7)
for k in range(4):
    body.tube([(0.5, 1.12 - k * 0.025, -0.33), (0.56, 1.11 - k * 0.025, -0.33), (0.57, 1.08 - k * 0.025, -0.29)],
              [0.016, 0.014, 0.012], sides=6)

# Back arm: held out behind him, the bony hand telling prayer beads that hang free of the robe.
body.tube([(-0.12, 1.42, -0.18), (-0.3, 1.2, -0.22), (-0.42, 1.0, -0.24), (-0.47, 0.92, -0.24)], [0.1, 0.1, 0.12, 0.155], sides=12)
body.blob((-0.5, 0.86, -0.24), (0.05, 0.065, 0.05), segs=10, rings=7)
for k in range(4):  # long fingers
    a = -0.6 + k * 0.35
    body.tube([(-0.5, 0.84, -0.24 + k * 0.012), (-0.52 + 0.05 * math.sin(a), 0.76, -0.25), (-0.5 + 0.06 * math.sin(a), 0.71, -0.26)],
              [0.013, 0.011, 0.008], sides=5)
beads = []
for k in range(17):
    t = k / 16.0
    beads.append((-0.5 + 0.09 * math.sin(t * math.pi * 2) * 0.5, 0.8 - 0.42 * math.sin(t * math.pi), -0.27 - 0.07 * math.sin(t * math.pi)))
for p in beads:
    body.blob(p, (0.024, 0.024, 0.024), segs=8, rings=5, mat=K.CRIMSON)
body.tube([beads[8], (-0.49, 0.26, -0.33)], 0.006, sides=5, mat=K.CRIMSON)
body.prism([(0, 0.05), (0.04, 0), (0, -0.07), (-0.04, 0)], -0.345, -0.325, mat=K.CRIMSON,
           xform=lambda p: p + Vector((-0.49, 0.2, 0)))  # a little crimson pendant

# Tattered strips of cloth hanging from the mantle at his back.
for s, (x0, len_, w) in enumerate([(-0.3, 0.55, 0.07), (-0.36, 0.42, 0.05), (-0.24, 0.7, 0.06), (-0.33, 0.3, 0.04)]):
    pts = []
    for k in range(9):
        t = k / 8
        pts.append((x0 - 0.12 * t + 0.03 * math.sin(t * 7 + s), 1.3 - len_ * t, 0.12 - 0.05 * s))
    body.tube(pts, [w * (1 - 0.8 * (k / 8)) for k in range(9)], sides=6, flat=0.25, seed=s)

# Rope belt with a knot and frayed ends.
belt = []
for k in range(33):
    a = 2 * math.pi * k / 32
    belt.append((math.cos(a) * 0.305, 0.96 + 0.015 * math.sin(3 * a), math.sin(a) * 0.305 * 0.74))
body.tube(belt, 0.024, sides=7, cap=False)
body.blob((0.22, 0.95, -0.2), (0.05, 0.045, 0.04), segs=8, rings=6, bumps=0.25, seed=3)
for dx, dz in [(0.0, 0.0), (0.05, 0.03)]:
    body.tube([(0.22 + dx, 0.93, -0.21 + dz), (0.24 + dx, 0.75, -0.22 + dz), (0.23 + dx * 2, 0.58, -0.22 + dz)],
              [0.02, 0.018, 0.014], sides=6)
    for s in range(3):
        body.tube([(0.23 + dx * 2, 0.58, -0.22 + dz), (0.21 + dx * 2 + s * 0.02, 0.5, -0.22 + dz)], 0.006, sides=4)
# Toes peeking out under the hem.
body.blob((0.33, 0.03, -0.1), (0.09, 0.04, 0.05), segs=10, rings=6)
body.displace(0.012, scale=6.0, seed=2)
# Hunched: the upper body leans forward over the shield.
for v in body.bm.verts:
    g = Vector((-v.co.x, v.co.z, -v.co.y))
    lean = 0.14 * max(0.0, (g.y - 0.6) / 1.6) ** 2
    v.co = K.G(g.x + lean, g.y, g.z)
body.build()

# ---------------- shield (its own object: the monk raises it, it is torn away)
sh = K.Part("Shield", pivot=(0.55, 1.05, -0.15))
ang = math.radians(40)
n = Vector((math.cos(ang), 0, -math.sin(ang)))  # face normal: forward, turned towards the camera
h = Vector((math.sin(ang), 0, math.cos(ang)))   # across the face
up = Vector((0, 1, 0))


def X(p):
    return h * p.x + up * p.y + n * p.z


outline = []
for k in range(40):
    t = k / 40.0
    a = 2 * math.pi * t
    x = 0.31 * math.sin(a)
    y = 0.28 + 0.29 * math.cos(a) if math.cos(a) > 0 else 0.28 + 0.9 * math.cos(a) * (1 - 0.35 * abs(math.sin(a)))
    if 0.08 < t < 0.16:  # a chipped bite in the upper edge
        x *= 0.88
        y -= 0.04
    outline.append((x, y))
sh.prism(outline, 0.0, 0.07, xform=X)
rim = [(x, y, 0.075) for x, y in outline] + [(outline[0][0], outline[0][1], 0.075)]
sh.tube([X(Vector(p)) for p in rim], 0.03, sides=6, cap=False)
for k in range(0, 40, 4):  # rivets
    x, y = outline[k]
    sh.blob(X(Vector((x * 0.88, y * 0.92 + 0.02, 0.09))), (0.018, 0.018, 0.018), segs=8, rings=5)
sh.blob(X(Vector((0, 0.15, 0.1))), (0.08, 0.08, 0.05), segs=14, rings=8)  # boss
# Crimson rune: a weeping eye — almond lids, a slit pupil, tears running down the shield.
eye = []
for k in range(33):
    a = 2 * math.pi * k / 32
    s = math.sin(a)
    eye.append(X(Vector((0.18 * math.cos(a), 0.15 + 0.08 * s * abs(s) ** 0.35, 0.095))))
sh.tube(eye, 0.015, sides=6, mat=K.CRIMSON, cap=False)
slit = [(0.0, 0.07), (0.025, 0.0), (0.0, -0.07), (-0.025, 0.0)]
sh.prism([(x, y + 0.15) for x, y in slit], 0.095, 0.115, mat=K.CRIMSON, xform=X)
for i, (x, length) in enumerate([(-0.07, 0.3), (0.0, 0.48), (0.08, 0.22)]):
    pts = [X(Vector((x + 0.01 * math.sin(t * 9), 0.08 - length * t, 0.095))) for t in [k / 6 for k in range(7)]]
    sh.tube(pts, [0.016 * (1 - 0.5 * k / 6) for k in range(7)], sides=6, mat=K.CRIMSON)
    sh.blob(pts[-1], (0.02, 0.026, 0.012), segs=8, rings=5, mat=K.CRIMSON)
sh.displace(0.006, scale=9.0, seed=5)
sh.build()
