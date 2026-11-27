# Crow — facing +x (rebuilt to the hero's level, v0.103: more and finer feathers with split barbs, a finer beak and eye,
# scaled legs; kept light without smoothing - a swarm of them wheels at once). Wings "WingL"/"WingR" spread in the picture plane, pointing up from the shoulder;
# the game flaps them by rotating about x (0° = up, ~150° = down past the body).
# Modelled feather by feather: every contour, covert, flight and tail feather is a cambered blade with a shaft,
# so the bird keeps its volume when lit and turned (the swarm wheels round in depth).
import math
import random
from mathutils import Vector
import creature_kit as K

rnd = random.Random(11)
b = K.Part("Body")
# Body: a deep-chested teardrop running into a thick neck and the head.
body = [(-0.19 + 0.4 * t, 0.012 * math.sin(t * 3.0) + 0.02 * t * t, 0) for t in [k / 12 for k in range(13)]]
b.tube(body, [0.025, 0.045, 0.062, 0.076, 0.086, 0.094, 0.098, 0.097, 0.092, 0.084, 0.074, 0.066, 0.06], sides=22, flat=0.82)
# Head: domed crown, flat brow over the eye, heavy jaw line.
b.blob((0.25, 0.055, 0), (0.072, 0.066, 0.058), segs=26, rings=16)
b.blob((0.265, 0.082, 0), (0.05, 0.03, 0.05), segs=12, rings=8)      # the brow ridge
# Beak: two mandibles, the upper curved over the tip, a slit of gape between them, bristles over the nostril.
up = [(0.3, 0.068, 0), (0.35, 0.062, 0), (0.39, 0.048, 0), (0.425, 0.028, 0), (0.44, 0.012, 0)]
b.tube(up, [0.026, 0.02, 0.014, 0.007, 0.0015], sides=16, flat=0.75)
for zs in (-1, 1):   # the nostril, a slit under the bristles
    b.blob((0.33, 0.066, zs * 0.016), (0.008, 0.003, 0.002), segs=8, rings=4)
lo = [(0.3, 0.035, 0), (0.35, 0.03, 0), (0.39, 0.022, 0), (0.42, 0.016, 0)]
b.tube(lo, [0.02, 0.014, 0.008, 0.002], sides=16, flat=0.7)
for k in range(11):
    a = -0.5 + k * 0.11
    root = Vector((0.31, 0.07, 0.03 * math.sin(a * 3)))
    b.tube([root, root + Vector((0.05, 0.004 - 0.003 * k, 0.008 * math.sin(a * 3)))], [0.004, 0.0008], sides=4)
# Eyes (both sides, crimson) in a dark socket ring.
for zs in (-1, 1):
    c = Vector((0.272, 0.066, zs * 0.052))
    b.tube([c + Vector((math.cos(2 * math.pi * j / 14) * 0.017, math.sin(2 * math.pi * j / 14) * 0.016, 0)) for j in range(15)],
           0.004, sides=5, cap=False)
    b.blob(c + Vector((0, 0, zs * 0.002)), (0.013, 0.013, 0.008), segs=16, rings=10, mat=K.CRIMSON)
    b.blob(c + Vector((0.002, 0.001, zs * 0.008)), (0.005, 0.006, 0.003), segs=10, rings=6)            # the pupil
# Contour feathers: overlapping rows of small blades laid back along the body, from the nape to the rump.
for row in range(9):
    x = 0.2 - row * 0.04
    for k in range(12):
        a = math.radians(-160 + k * 29 + (row % 2) * 14 + rnd.uniform(-6, 6))   # staggered round the body
        ry = 0.094 * (1 - 0.4 * max(0, abs(row - 2.5) - 1) / 5)
        n = Vector((0, math.cos(a), math.sin(a) * 0.84)).normalized()
        L = 0.06 + 0.015 * rnd.random()
        root = Vector((x + rnd.uniform(-0.01, 0.01), 0.012 + math.cos(a) * ry * 0.93, math.sin(a) * ry * 0.78))
        tip = Vector((root.x - L, 0.012 + math.cos(a) * ry * 0.96, math.sin(a) * ry * 0.82))
        b.blade([root, root.lerp(tip, 0.35) + n * 0.002, root.lerp(tip, 0.7) + n * 0.003, tip], [0.02, 0.022, 0.017, 0.004], thick=0.0012, normal=n, camber=0.08, cols=4,
                edge=lambda i, u, s=rnd.random(): 1.0 - 0.25 * (i >= 2) * abs(math.sin(u * 7 + s * 6)))   # barbs split at the tip
# Throat hackles: long, shaggy, pointed feathers fanning under the beak.
for k in range(7):
    a = math.radians(-60 + k * 20)
    root = Vector((0.24 - 0.01 * k, -0.005, 0.045 * math.sin(a)))
    tip = root + Vector((-0.02 - 0.006 * k, -0.06 - 0.004 * (k % 3), 0.02 * math.sin(a)))
    b.blade([root, root.lerp(tip, 0.5) + Vector((0.01, 0, 0)), tip], [0.01, 0.009, 0.001], thick=0.0015,
            normal=(0.3, -0.2, math.sin(a)), camber=0.3, cols=4)
# Ruffled nape.
for k in range(6):
    root = Vector((0.23 - 0.012 * k, 0.1 - 0.006 * k, -0.035 + 0.014 * k))
    b.blade([root, root + Vector((-0.04, 0.02 + 0.006 * (k % 2), 0))], [0.009, 0.002], thick=0.0015, normal=(0, 1, 0), camber=0.2, cols=4)
# Tail: a wedge of ten feathers fanned in a shallow, cambered spread, the middle pair longest.
for k in range(10):
    f = (k - 4.5) / 4.5
    a = math.radians(f * 14)
    L = 0.27 - 0.04 * abs(f) ** 1.5
    root = Vector((-0.16, 0.025, 0.035 * f))
    d = Vector((-math.cos(a), -math.sin(a) * 0.35 - 0.08, math.sin(a) * 0.6)).normalized()
    pts = [root + d * L * t + Vector((0, 0.025 * t * t, 0)) for t in (0.0, 0.3, 0.65, 0.9, 1.0)]
    b.blade(pts, [0.012, 0.02, 0.022, 0.018, 0.004], thick=0.0025, normal=(0, 1, 0), camber=0.12, rachis=0.0015, cols=6)
# Legs: scaled shanks, three toes forward and one back, hooked talons; feathered thighs.
for zs in (-1, 1):
    z = zs * 0.032
    b.blob((0.03, -0.055, z), (0.035, 0.03, 0.022), segs=10, rings=6)
    hock = Vector((0.02, -0.085, z))
    foot = Vector((0.045, -0.16, z))
    b.tube([hock, hock.lerp(foot, 0.5), foot], [0.008, 0.0065, 0.006], sides=10)
    for j in range(9):   # the scutes down the front of the shank
        b.tube([hock.lerp(foot, j / 9) + Vector((0.006, 0, 0)), hock.lerp(foot, j / 9 + 0.06) + Vector((0.0065, 0, 0))], 0.0072, sides=8)
    for t in range(4):
        back = t == 3
        da = math.radians([-25, 0, 25, 0][t])
        dirv = Vector((-1.0, -0.15, 0)) if back else Vector((math.cos(da), -0.25, math.sin(da) * 0.9)).normalized()
        L = 0.035 if back else 0.05 - 0.008 * abs(t - 1)
        toe = [foot, foot + dirv * L * 0.5 + Vector((0, 0.004, 0)), foot + dirv * L]
        b.tube(toe, [0.0045, 0.004, 0.0035], sides=8)
        for j in (0.33, 0.66):   # the toe's knuckles
            b.blob(toe[0].lerp(toe[-1], j), (0.0048, 0.0042, 0.0042), segs=8, rings=5)
        claw = [toe[-1], toe[-1] + dirv * 0.012 + Vector((0, -0.004, 0)), toe[-1] + dirv * 0.018 + Vector((0, -0.014, 0))]
        b.tube(claw, [0.003, 0.002, 0.0004], sides=7)
b.displace(0.0015, scale=20.0, seed=1)
b.build()


def wing(name, z, zs):
    w = K.Part(name, pivot=(0.0, 0.04, z))
    n = (0, 0, zs)        # the upper surface faces out from the body
    # The arm: shoulder, wrist, hand — a fleshy leading edge.
    arm = [Vector((0.02, 0.0, 0)), Vector((0.05, 0.13, 0)), Vector((0.02, 0.26, 0)), Vector((-0.04, 0.35, 0))]
    w.tube(arm, [0.028, 0.022, 0.016, 0.009], sides=8, flat=0.55)
    # Secondaries: nine broad feathers along the forearm, trailing back.
    for k in range(12):
        t = k / 11
        root = arm[0].lerp(arm[2], 0.15 + 0.8 * t)
        a = math.radians(170 - 30 * t)
        L = 0.2 + 0.03 * t
        d = Vector((math.cos(a), math.sin(a), 0))
        pts = [root + d * L * s + Vector((0, -0.02 * s * s, zs * -0.004 * k)) for s in (0.0, 0.35, 0.7, 0.92, 1.0)]
        w.blade(pts, [0.013, 0.021, 0.023, 0.018, 0.006], thick=0.002, normal=n, camber=0.1, rachis=0.0015, cols=5,
                edge=lambda i, u, s=k: 1.0 - 0.2 * (i >= 3) * abs(math.sin(u * 5 + s)))
    # Primaries: seven long fingered feathers from the hand, emarginated (narrowing) towards the tip.
    for k in range(9):
        t = k / 8
        root = arm[2].lerp(arm[3], 0.2 + 0.8 * t)
        a = math.radians(140 - 55 * t)
        L = 0.24 + 0.08 * math.sin(t * math.pi * 0.9)
        d = Vector((math.cos(a), math.sin(a), 0))
        pts = [root + d * L * s + Vector((0.025 * s * s * (1 - t), 0, zs * (-0.0032 * (8 - k)))) for s in (0.0, 0.2, 0.4, 0.55, 0.7, 0.85, 0.95, 1.0)]
        # emarginated: the vane steps in two thirds of the way out (the fingers of a crow's wingtip)
        w.blade(pts, [0.011, 0.019, 0.021, 0.02, 0.012, 0.011, 0.008, 0.002], thick=0.002, normal=n, camber=0.1, rachis=0.0015, cols=5)
    # Coverts: three rows of shorter feathers over the roots of the flight feathers, each row proud of the last.
    for row in range(3):
        cnt = 9 - row * 2
        for k in range(cnt):
            t = k / (cnt - 1)
            root = arm[0].lerp(arm[3], 0.1 + 0.85 * t) + Vector((-0.01 * row, 0, 0))
            a = math.radians(175 - 60 * t)
            L = 0.09 - 0.022 * row
            d = Vector((math.cos(a), math.sin(a), 0))
            pts = [root + d * L * s + Vector((0, 0, zs * (0.006 + 0.005 * row))) for s in (0.0, 0.5, 1.0)]
            w.blade(pts, [0.016, 0.02, 0.006], thick=0.002, normal=n, camber=0.15, cols=4)
    w.build()


wing("WingL", -0.03, -1)
wing("WingR", 0.03, 1)
