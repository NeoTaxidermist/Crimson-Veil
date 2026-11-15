# Lantern wisp — a wrought-iron lantern with a crimson flame (the "Flame" glows), wisps curling below.
# The light itself is added by the game.
# Gothic ironwork: twisted posts, pointed arches and tracery under a ribbed, flared roof with a spiked finial and a
# broken chain; a thorned crown under the base. The spirit is the flame — tongues of fire round a hollow-eyed face —
# and ribbons of crimson smoke twisting down from the lantern as it drifts.
import math
from mathutils import Vector
import creature_kit as K

L = K.Part("Lantern")
S6 = 6


def ang(k, off=0.0):
    return 2 * math.pi * (k + off) / S6


# Base: a stepped, moulded plate; under it a crown of hooked thorns.
L.lathe([(0.0, -0.24), (0.12, -0.24), (0.15, -0.225), (0.155, -0.21), (0.14, -0.2), (0.125, -0.19), (0.125, -0.17), (0.0, -0.17)], sides=S6 * 4)
for k in range(S6 * 2):
    a = ang(k / 2.0)
    p0 = Vector((math.cos(a) * 0.12, -0.24, math.sin(a) * 0.12))
    out = Vector((math.cos(a), 0, math.sin(a)))
    L.tube([p0, p0 + out * 0.03 + Vector((0, -0.04, 0)), p0 + out * 0.07 + Vector((0, -0.05 - 0.02 * (k % 2), 0))],
           [0.011, 0.007, 0.0008], sides=5)
# Posts: two iron rods twisted together, bulging, between the base and the roof.
for k in range(S6):
    a = ang(k)
    prof = [(0.115, -0.17), (0.13, -0.1), (0.137, -0.02), (0.135, 0.06), (0.125, 0.13), (0.115, 0.18)]
    for s in range(2):
        pts = []
        for j in range(25):
            t = j / 24
            i0 = min(int(t * 5), 4)
            f = t * 5 - i0
            r = prof[i0][0] * (1 - f) + prof[i0 + 1][0] * f
            y = prof[i0][1] * (1 - f) + prof[i0 + 1][1] * f
            tw = t * 6 * math.pi + s * math.pi
            c = Vector((math.cos(a) * r, y, math.sin(a) * r))
            side = Vector((-math.sin(a), 0, math.cos(a)))
            radial = Vector((math.cos(a), 0, math.sin(a)))
            pts.append(c + side * math.cos(tw) * 0.007 + radial * math.sin(tw) * 0.007)
        L.tube(pts, 0.0065, sides=5)
    # a crimson-hot inner edge, so the ironwork reads against dark backgrounds
    L.tube([(math.cos(a) * r * 0.93, y, math.sin(a) * r * 0.93) for r, y in prof], 0.004, sides=4, mat=K.CRIMSON)
# Pointed arches and tracery between the posts, under the roof; a band of scrolls at mid height.
for k in range(S6):
    a0, a1 = ang(k), ang(k + 1)
    p0 = Vector((math.cos(a0) * 0.125, 0.12, math.sin(a0) * 0.125))
    p1 = Vector((math.cos(a1) * 0.125, 0.12, math.sin(a1) * 0.125))
    arch = []
    for j in range(9):
        t = j / 8
        q = p0.lerp(p1, t)
        h = 0.055 * (1 - abs(2 * t - 1)) ** 0.6
        arch.append(q + Vector((0, h, 0)))
    L.tube(arch, 0.005, sides=4)
    # trefoil: three small rings in the arch
    for (u, v, r) in ((0.5, 0.025, 0.012), (0.36, 0.008, 0.009), (0.64, 0.008, 0.009)):
        c = p0.lerp(p1, u) + Vector((0, v, 0))
        side = (p1 - p0).normalized()
        L.tube([c + side * math.cos(2 * math.pi * j / 12) * r + Vector((0, math.sin(2 * math.pi * j / 12) * r, 0)) for j in range(13)],
               0.0025, sides=4, cap=False)
    # a scroll at mid height
    c = Vector((math.cos(ang(k + 0.5)) * 0.13, -0.03, math.sin(ang(k + 0.5)) * 0.13))
    tdir = Vector((-math.sin(ang(k + 0.5)), 0, math.cos(ang(k + 0.5))))
    for s in (-1, 1):
        curl = [c + tdir * s * (0.018 + 0.02 * math.cos(q) * (1 - q / 9)) + Vector((0, 0.02 * math.sin(q) * (1 - q / 9), 0))
                for q in [j * 0.55 for j in range(13)]]
        L.tube(curl, 0.004, sides=4)
# Roof: flared, ribbed, with a crimson-lit drip edge, rivets, a spiked finial, a ring and a broken chain.
L.lathe([(0.0, 0.18), (0.17, 0.18), (0.185, 0.195), (0.15, 0.22), (0.11, 0.26), (0.075, 0.3), (0.045, 0.34), (0.02, 0.37), (0.0, 0.385)],
        sides=S6 * 4, rfunc=lambda th, i, r, y: 1 + (0.06 if i > 1 else 0.0) * max(0.0, math.cos(th * S6)) ** 8)
for k in range(S6):
    a = ang(k)
    L.tube([(math.cos(a) * 0.18, 0.19, math.sin(a) * 0.18), (math.cos(a) * 0.12, 0.25, math.sin(a) * 0.12),
            (math.cos(a) * 0.05, 0.33, math.sin(a) * 0.05), (0, 0.38, 0)], [0.008, 0.007, 0.005, 0.004], sides=5)   # ribs
    tip = Vector((math.cos(a) * 0.2, 0.17, math.sin(a) * 0.2))
    L.tube([tip + Vector((-math.cos(a) * 0.03, 0.025, -math.sin(a) * 0.03)), tip, tip + Vector((math.cos(a) * 0.01, -0.025, math.sin(a) * 0.01))],
           [0.008, 0.006, 0.001], sides=5)                                                                             # spiked eaves
for k in range(18):
    a = 2 * math.pi * k / 18
    L.blob((math.cos(a) * 0.172, 0.19, math.sin(a) * 0.172), (0.007, 0.007, 0.007), segs=6, rings=4)
L.lathe([(0.0, 0.38), (0.025, 0.39), (0.03, 0.41), (0.015, 0.43), (0.006, 0.47), (0.0, 0.5)], sides=8)
ring = [(math.cos(2 * math.pi * k / 18) * 0.035, 0.53 + math.sin(2 * math.pi * k / 18) * 0.04, 0) for k in range(19)]
L.tube(ring, 0.008, sides=5, cap=False)
y = 0.57
for k in range(3):
    link = [(math.cos(2 * math.pi * j / 12) * 0.012 if k % 2 == 0 else 0, y + 0.03 + math.sin(2 * math.pi * j / 12) * 0.03,
             0 if k % 2 == 0 else math.cos(2 * math.pi * j / 12) * 0.012) for j in range(13 if k < 2 else 9)]
    L.tube(link, 0.005, sides=4, cap=False)
    y += 0.05
# The door plate with its rune, wax drips frozen down the posts.
L.prism([(-0.04, -0.12), (0.04, -0.12), (0.04, -0.07), (0.0, -0.05), (-0.04, -0.07)], -0.145, -0.135, mat=K.CRIMSON)
L.tube([(0.0, -0.115, -0.148), (0.0, -0.065, -0.148)], 0.004, sides=3)
L.tube([(-0.025, -0.095, -0.148), (0.025, -0.085, -0.148)], 0.004, sides=3)
for k in range(4):
    a = ang(k * 1.5 + 0.2)
    L.tube([(math.cos(a) * 0.12, -0.16, math.sin(a) * 0.12), (math.cos(a) * 0.126, -0.2 - 0.015 * k, math.sin(a) * 0.126)],
           [0.01, 0.004], sides=5)
L.build()

F = K.Part("Flame")
# Tongues of fire licking up round a core, and a hollow-eyed face in it (the dark holes are ink).
F.lathe([(0.0, -0.13), (0.05, -0.1), (0.07, -0.04), (0.06, 0.03), (0.04, 0.08), (0.02, 0.12), (0.0, 0.16)], sides=16, mat=K.CRIMSON,
        rfunc=lambda th, i, r, y: 1 + 0.12 * math.sin(3 * th + i))
for k in range(5):
    a = 2 * math.pi * k / 5 + 0.3
    root = Vector((math.cos(a) * 0.04, -0.02, math.sin(a) * 0.04))
    pts = [root + Vector((math.cos(a) * 0.02 * math.sin(t * 3), 0.07 * t + 0.03 * t * t, math.sin(a) * 0.02 * math.sin(t * 3))) * (1.0 + 0.3 * (k % 2))
           for t in [j / 6 for j in range(7)]]
    F.tube(pts, [0.03 * (1 - j / 6.5) for j in range(7)], sides=6, mat=K.CRIMSON)
for zs in (-1, 1):
    F.blob((0.022 * zs, 0.02, -0.058), (0.013, 0.018, 0.008), segs=8, rings=6)            # the eye holes
F.blob((0.0, -0.035, -0.062), (0.018, 0.012, 0.006), segs=8, rings=6)                    # the open mouth
F.build()

S = K.Part("Wisps")
# Ribbons of crimson smoke twisting down from under the lantern: the spirit trailing behind it.
for k in range(4):
    pts, ws = [], []
    for j in range(16):
        t = j / 15
        a = t * 5.0 + k * 1.6
        pts.append(Vector((math.cos(a) * 0.04 * t - 0.12 * t * (k - 1.5), -0.27 - 0.4 * t, math.sin(a) * 0.04)))
        ws.append(0.022 * (1 - t) ** 0.7 + 0.002)
    nrm = (math.sin(k * 2.0), 0.1, -math.cos(k * 2.0))
    S.blade(pts, ws, thick=0.003, normal=nrm, camber=0.4, cols=5, mat=K.CRIMSON)
S.build()
