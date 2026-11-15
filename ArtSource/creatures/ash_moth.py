# Ash moth — facing +x. Wings "WingL"/"WingR" in the picture plane pointing up; the game beats them about x.
# A furred body (thorax coat, banded abdomen with tufts, compound eyes, a curled proboscis, comb antennae,
# six jointed hairy legs) and wings that are cupped membranes: every vein, band, speck and the eye-spot follows the
# wing's curve, and a fringe of scales hangs off the torn trailing margin.
import math
import random
from mathutils import Vector
import creature_kit as K

rnd = random.Random(5)
b = K.Part("Body")
# Abdomen: banded segments trailing back and down, each ring a little proud, ending in a tuft.
abd = [Vector((-0.03 - 0.05 * k, -0.01 - 0.005 * k * k, 0)) for k in range(8)]
radii = [0.06, 0.068, 0.07, 0.066, 0.058, 0.046, 0.032, 0.012]
b.tube(abd, radii, sides=14, flat=0.9)
for k in range(1, 7):
    p = abd[k]
    d = (abd[k + 1] - abd[k - 1]).normalized()
    b.tube([p + d * 0.012, p, p - d * 0.014], [radii[k] * 1.02, radii[k] * 1.1, radii[k] * 1.0], sides=14, flat=0.9)
for k in range(10):                                   # the anal tuft
    a = 2 * math.pi * k / 10
    root = abd[-2] + Vector((0, math.cos(a) * 0.015, math.sin(a) * 0.015))
    b.tube([root, root + Vector((-0.06, -0.02 + 0.02 * math.cos(a), 0.02 * math.sin(a)))], [0.007, 0.0008], sides=4)
# Thorax and its thick coat of hair.
b.blob((0.06, 0.02, 0), (0.1, 0.09, 0.08), segs=18, rings=12)
for k in range(70):
    a = rnd.uniform(0, 2 * math.pi)
    e = rnd.uniform(-1.3, 1.3)
    d = Vector((math.cos(a) * math.cos(e) * 0.7 - 0.25, math.sin(e), math.sin(a) * math.cos(e) * 0.8)).normalized()
    root = Vector((0.06, 0.02, 0)) + Vector((d.x * 0.09, d.y * 0.08, d.z * 0.07))
    tip = root + d * rnd.uniform(0.025, 0.045) + Vector((-0.035, -0.008, 0))
    b.tube([root, root.lerp(tip, 0.5) + Vector((0, -0.006, 0)), tip], [0.01, 0.006, 0.001], sides=4)
# A collar of hair between thorax and head.
for k in range(14):
    a = 2 * math.pi * k / 14
    root = Vector((0.14, 0.02 + math.cos(a) * 0.055, math.sin(a) * 0.05))
    b.tube([root, root + Vector((0.04, math.cos(a) * 0.03, math.sin(a) * 0.03))], [0.009, 0.001], sides=4)
# Head, with two big faceted compound eyes and a curled proboscis.
b.blob((0.18, 0.02, 0), (0.05, 0.05, 0.048), segs=14, rings=10)
for zs in (-1, 1):
    b.blob((0.205, 0.035, zs * 0.036), (0.03, 0.032, 0.022), segs=7, rings=5, mat=K.CRIMSON)   # low-poly: facets
spiral = [Vector((0.215 + 0.03 * math.cos(t) * (1 - t / 12), -0.03 - 0.03 * math.sin(t) * (1 - t / 12), 0)) for t in [j * 0.5 for j in range(20)]]
b.tube(spiral, [0.006 * (1 - j / 22) for j in range(20)], sides=5)
# Comb antennae: a spine with paired side teeth, swept back.
for zs in (-1, 1):
    spine = [Vector((0.2, 0.065, zs * 0.02)) + Vector((0.07 * t + 0.03 * t * t, 0.2 * t, zs * 0.04 * t)) for t in [k / 8 for k in range(9)]]
    b.tube(spine, [0.006 - 0.0004 * k for k in range(9)], sides=5)
    for k in range(1, 9):
        p = spine[k]
        L = 0.035 * math.sin(math.pi * k / 9) + 0.008
        for s in (-1, 1):
            b.tube([p, p + Vector((s * L * 0.6 - 0.008, -L * 0.4, s * L * 0.5))], [0.002, 0.0008], sides=3)
# Six legs: coxa, femur, tibia with a fringe of hair, segmented tarsus, hooked tip.
for k in range(3):
    x = 0.12 - 0.045 * k
    for zs in (-1, 1):
        hip = Vector((x, -0.035, zs * 0.03))
        knee = hip + Vector((0.035 - 0.025 * k, -0.04, zs * 0.05))
        ankle = knee + Vector((0.04 - 0.045 * k, -0.08, zs * 0.015))
        b.tube([hip, knee], [0.009, 0.007], sides=6)
        b.tube([knee, ankle], [0.006, 0.004], sides=6)
        for j in range(5):
            q = knee.lerp(ankle, 0.15 + 0.17 * j)
            b.tube([q, q + Vector((-0.012, -0.004, zs * 0.008))], [0.0025, 0.0005], sides=3)
        prev = ankle
        for j in range(4):
            nxt = prev + Vector((0.012 - 0.01 * k, -0.012, 0))
            b.tube([prev, nxt], [0.0032, 0.0028], sides=5)
            prev = nxt
        b.tube([prev, prev + Vector((0.006, -0.006, 0)), prev + Vector((0.012, -0.003, 0))], [0.002, 0.0015, 0.0003], sides=4)
b.build()


def scallop(points, depth, n):
    """Adds a ragged, scalloped trailing edge between consecutive outline points."""
    out = []
    for i in range(len(points) - 1):
        (x0, y0), (x1, y1) = points[i], points[i + 1]
        for k in range(n):
            t = k / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            nx, ny = -(y1 - y0), (x1 - x0)
            ln = math.hypot(nx, ny) or 1
            bite = depth * (math.sin(t * math.pi) ** 2) * (1 if k % 2 else 0.4)
            out.append((x - nx / ln * bite, y - ny / ln * bite))
    out.append(points[-1])
    return out


def wing(name, z, zs):
    w = K.Part(name, pivot=(0.02, 0.06, z))

    def cup(x, y):
        """The membrane's depth: it cups away from the body towards the tip, and ripples a little."""
        r = math.hypot(x, y)
        return zs * (0.16 * r * r + 0.008 * math.sin(x * 22 + y * 9) * r)

    def warp(p):
        return Vector((p.x, p.y, p.z + cup(p.x, p.y)))

    def on(x, y, dz):
        return Vector((x, y, cup(x, y) + dz))

    face = -0.0075   # the camera side of the membrane, before the cup (WingR's camera side is the far one: it is hidden)
    # Forewing: broad, angular, swept back, with a hooked apex; the outer margin torn like burnt paper.
    lead = [(0.02, 0.0), (0.07, 0.18), (0.12, 0.42), (0.17, 0.66), (0.14, 0.72)]
    margin = [(0.14, 0.72), (0.0, 0.66), (-0.14, 0.56), (-0.26, 0.42), (-0.34, 0.26), (-0.3, 0.16)]
    inner = [(-0.3, 0.16), (-0.18, 0.08), (-0.06, 0.02)]
    fore = lead[:-1] + scallop(margin, 0.045, 7)[:-1] + inner
    w.prism(fore, -0.005, 0.005, xform=warp)
    # Hindwing: rounder, behind and below, with a short tail and a crimson rim band.
    hind = [(-0.06, 0.0), (-0.2, 0.1), (-0.34, 0.14), (-0.44, 0.08), (-0.5, -0.05), (-0.56, -0.12), (-0.44, -0.08), (-0.28, -0.06), (-0.12, -0.02)]
    hind_o = scallop(hind + [hind[0]], 0.025, 5)[:-1]
    w.prism(hind_o, 0.005, 0.013, xform=warp)
    band = [Vector((x * 0.93 - 0.02, y * 0.9 + 0.004, 0)) for (x, y) in hind[1:7]]
    w.tube([on(p.x, p.y, 0.003 - 0.0065) for p in band], 0.007, sides=5, mat=K.CRIMSON, flat=0.4)
    # Veins: raised ridges branching from the root, lying on the cupped surface.
    for (x, y) in [(0.14, 0.66), (0.0, 0.62), (-0.14, 0.52), (-0.26, 0.38), (-0.33, 0.22), (-0.4, 0.08), (-0.48, -0.04)]:
        pts = [on(x * s, y * s, face - 0.0012) for s in (0.02, 0.25, 0.5, 0.72, 0.92)]
        w.tube(pts, [0.006, 0.005, 0.004, 0.003, 0.0015], sides=5, flat=0.5)
        if y > 0.3:
            m = Vector((x * 0.6, y * 0.6, 0))
            fork = [on(m.x, m.y, face - 0.001), on(m.x * 1.25 - 0.06, m.y * 1.3, face - 0.001)]
            w.tube(fork, [0.003, 0.0012], sides=4, flat=0.5)
    # Crimson eye-spot: a raised ring, an iris and a dark pupil with a pale glint.
    c = Vector((-0.04, 0.42, 0))
    ring = [on(c.x + math.cos(2 * math.pi * k / 24) * 0.085, c.y + math.sin(2 * math.pi * k / 24) * 0.09, face - 0.002) for k in range(25)]
    w.tube(ring, 0.022, sides=6, mat=K.CRIMSON, cap=False, flat=0.4)
    w.blob(on(c.x, c.y, face - 0.002), (0.045, 0.05, 0.005), segs=14, rings=6, mat=K.CRIMSON)
    w.blob(on(c.x + 0.01, c.y + 0.01, face - 0.006), (0.02, 0.022, 0.004), segs=10, rings=5)
    # Ash bands (a burnt page's tide lines) and soot specks — kept inside the forewing.
    poly = lead[:-1] + margin + inner

    def inside(x, y, m=0.03):
        hit = False
        for i in range(len(poly)):
            (ax, ay), (bx, by) = poly[i], poly[i - 1]
            if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
                hit = not hit
        return hit and all(math.hypot(x - px, y - py) > m for px, py in poly)
    for rr in (0.3, 0.45, 0.58):
        arc = [(math.cos(a) * rr * 0.75 - 0.02, math.sin(a) * rr) for a in [0.5 + 2.2 * j / 30 for j in range(31)]]
        run = []
        for (x, y) in arc + [(99, 99)]:
            if inside(x, y) and math.hypot(x - c.x, y - c.y) > 0.12:
                run.append(on(x, y, face - 0.001))
            elif len(run) > 2:
                w.tube(run, 0.006, sides=3, flat=0.4)
                run = []
            else:
                run = []
    rs = random.Random(31 + int(z * 100))
    for k in range(50):
        x, y = rs.uniform(-0.34, 0.17), rs.uniform(0.05, 0.7)
        if inside(x, y) and math.hypot(x - c.x, y - c.y) > 0.11:
            w.blob(on(x, y, face - 0.0015), (0.008, 0.008, 0.002), segs=6, rings=3)
    # A fringe of scales hanging off the torn outer margin of both wings.
    for (outline, step) in ((scallop(margin, 0.045, 7), 1), (hind_o[2:9], 1)):
        for i in range(0, len(outline) - 1, step):
            (x0, y0), (x1, y1) = outline[i], outline[i + 1]
            nx, ny = (y1 - y0), -(x1 - x0)
            ln = math.hypot(nx, ny) or 1
            nx, ny = nx / ln, ny / ln
            if nx * x0 + ny * (y0 - 0.2) < 0:      # only on the outer side
                nx, ny = -nx, -ny
            L = 0.01 + 0.006 * ((i * 7) % 3)
            w.tube([on(x0, y0, 0), on(x0 + nx * L, y0 + ny * L - 0.004, 0)], [0.004, 0.0006], sides=3)
    w.build()


wing("WingL", -0.03, -1)
wing("WingR", 0.03, 1)
