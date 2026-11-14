# Ash moth — facing +x. Wings "WingL"/"WingR" in the picture plane pointing up; the game beats them about x.
import math
import random
from mathutils import Vector
import creature_kit as K

b = K.Part("Body")
# Segmented, furry abdomen trailing back and down; a thick thorax; a small head.
abd = [(-0.04 - 0.05 * k, -0.01 - 0.005 * k * k, 0) for k in range(8)]
radii = [0.06, 0.068, 0.07, 0.066, 0.058, 0.046, 0.032, 0.012]
b.tube(abd, radii, sides=12, flat=0.9)
for k in range(1, 7):  # segment rings
    p = Vector(abd[k])
    b.tube([p + Vector((0.006, 0.0, 0)), p + Vector((-0.006, 0, 0))], radii[k] * 1.1, sides=12, flat=0.9)
b.blob((0.06, 0.02, 0), (0.1, 0.09, 0.08), segs=16, rings=10)
b.blob((0.17, 0.02, 0), (0.05, 0.05, 0.045), segs=12, rings=8)
# Fur: tufts bristling from the thorax.
rnd = random.Random(5)
for k in range(26):
    a = rnd.uniform(0, 2 * math.pi)
    e = rnd.uniform(-1.2, 1.2)
    d = Vector((math.cos(a) * 0.6 + 0.2, math.sin(e), math.sin(a) * 0.4))
    root = Vector((0.06, 0.02, 0)) + Vector((d.x * 0.08, d.y * 0.08, d.z * 0.07))
    b.tube([root, root + d.normalized() * 0.07], [0.012, 0.0015], sides=4)
# Eyes: two glinting crimson beads.
b.blob((0.2, 0.04, -0.035), (0.016, 0.016, 0.012), segs=8, rings=5, mat=K.CRIMSON)
# Feathery antennae (comb-like).
for z in (-0.02, 0.02):
    spine = [Vector((0.19, 0.06, z)) + Vector((0.07 * t + 0.03 * t * t, 0.18 * t, 0)) for t in [k / 6 for k in range(7)]]
    b.tube(spine, [0.006, 0.005, 0.004, 0.0035, 0.003, 0.0025, 0.002], sides=4)
    for k in range(1, 7):
        p = spine[k]
        for s in (-1, 1):
            b.tube([p, p + Vector((0.025 * s + 0.01, -0.01, 0))], [0.002, 0.001], sides=3)
# Six thin legs.
for k in range(3):
    x = 0.12 - 0.05 * k
    for z in (-0.04, 0.04):
        b.tube([(x, -0.03, z), (x + 0.03, -0.1, z * 1.5), (x + 0.05 - 0.02 * k, -0.18, z * 1.5)], [0.006, 0.004, 0.002], sides=4)
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


def wing(name, z, spot_z):
    w = K.Part(name, pivot=(0.02, 0.06, z))
    # Forewing: broad, angular, swept back; ragged edges like burnt paper.
    # Leading edge rises forward to a hooked apex; the outer margin falls back, torn like burnt paper.
    lead = [(0.02, 0.0), (0.07, 0.18), (0.12, 0.42), (0.17, 0.66), (0.14, 0.72)]
    margin = [(0.14, 0.72), (0.0, 0.66), (-0.14, 0.56), (-0.26, 0.42), (-0.34, 0.26), (-0.3, 0.16)]
    inner = [(-0.3, 0.16), (-0.18, 0.08), (-0.06, 0.02)]
    w.prism(lead[:-1] + scallop(margin, 0.045, 6)[:-1] + inner, -0.006, 0.006)
    # Hindwing: rounder, behind and below, ragged too, with a short tail.
    hind = [(-0.06, 0.0), (-0.2, 0.1), (-0.34, 0.14), (-0.44, 0.08), (-0.5, -0.05), (-0.56, -0.12), (-0.44, -0.08), (-0.28, -0.06), (-0.12, -0.02)]
    w.prism(scallop(hind + [hind[0]], 0.025, 4)[:-1], 0.004, 0.014)
    # Veins: raised ridges from the root.
    for (x, y) in [(0.14, 0.66), (0.0, 0.62), (-0.14, 0.52), (-0.26, 0.38), (-0.4, 0.08), (-0.48, -0.04)]:
        w.tube([(0.0, 0.02, -0.008), (x * 0.5, y * 0.5, -0.009), (x * 0.92, y * 0.92, -0.008)], [0.006, 0.004, 0.0015], sides=4, flat=0.5)
    # Crimson eye-spot: a ring with a dark pupil, on the camera side.
    c = Vector((-0.04, 0.42, spot_z))
    ring = [c + Vector((math.cos(2 * math.pi * k / 20) * 0.085, math.sin(2 * math.pi * k / 20) * 0.09, 0)) for k in range(21)]
    w.tube(ring, 0.022, sides=6, mat=K.CRIMSON, cap=False, flat=0.4)
    w.blob(c + Vector((0, 0, -0.002)), (0.04, 0.045, 0.006), segs=12, rings=6, mat=K.CRIMSON)
    w.blob(c + Vector((0.01, 0.01, -0.006)), (0.018, 0.02, 0.004), segs=10, rings=5)
    # Ash bands across the forewing (like a burnt page's tide lines) and scattered soot specks — kept inside it.
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
            if inside(x, y):
                run.append((x, y, -0.0085))
            elif len(run) > 2:
                w.tube(run, 0.006, sides=3, flat=0.4)
                run = []
            else:
                run = []
    rs = random.Random(31 + int(z * 100))
    for k in range(40):
        x, y = rs.uniform(-0.34, 0.17), rs.uniform(0.05, 0.7)
        if inside(x, y):
            w.blob((x, y, -0.009), (0.008, 0.008, 0.002), segs=6, rings=3)
    w.build()


wing("WingL", -0.03, -0.012)
wing("WingR", 0.03, -0.012)
