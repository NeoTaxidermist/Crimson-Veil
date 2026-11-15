# Paper kite wraith — a torn diamond of paper on bamboo, a painted face. Faces the camera (in the picture plane).
# The tail and the thread are drawn by the game.
# The paper billows towards the camera between the struts, crumpled along old folds; torn corners curl over; the
# bamboo is jointed and the bow bent; prayer slips and paper tassels hang off the cross-strut. Everything painted on
# the paper (the face, patches, brushed characters) lies on the billowed surface.
import math
import random
from mathutils import Vector, noise
import creature_kit as K

k = K.Part("Sail")
top, right, bottom, left = (0, 0.62), (0.42, 0.08), (0, -0.62), (-0.42, 0.08)


def billow(x, y):
    """Depth of the paper: bellied out towards the camera between the struts, flat along them, crumpled."""
    fx = max(0.0, 1 - abs(x) / 0.44)
    fy = max(0.0, 1 - abs(y - 0.0) / 0.66)
    belly = -0.11 * math.sin(math.pi * fx) * fx * math.sin(math.pi * min(1.0, fy * 1.1))
    near_spine = math.exp(-(x / 0.03) ** 2)
    near_bow = math.exp(-((y - 0.1 - 0.04 * (1 - (x / 0.46) ** 2)) / 0.03) ** 2)
    crumple = 0.012 * noise.noise(Vector((x * 7, y * 7, 0.3))) + 0.006 * abs(math.sin(x * 31 + y * 12))
    return (belly + crumple) * (1 - 0.85 * near_spine) * (1 - 0.6 * near_bow)


def warp(p):
    return Vector((p.x, p.y, p.z + billow(p.x, p.y)))


def on(x, y, dz):
    return (x, y, billow(x, y) + dz)


def torn(a, b, n, depth, seed):
    out = []
    for i in range(n):
        t = i / n
        x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
        nx, ny = (b[1] - a[1]), -(b[0] - a[0])
        ln = math.hypot(nx, ny)
        bite = depth * (0.5 + 0.5 * math.sin(i * 2.3 + seed)) * (1.6 if i % 4 == 2 else 0.5)
        out.append((x - nx / ln * bite, y - ny / ln * bite))
    return out


def subdivide(outline, step=0.03):
    """More points along the outline, so its edge follows the billow smoothly."""
    out = []
    for i in range(len(outline)):
        a, b = outline[i], outline[(i + 1) % len(outline)]
        n = max(1, int(math.hypot(b[0] - a[0], b[1] - a[1]) / step))
        out += [(a[0] + (b[0] - a[0]) * j / n, a[1] + (b[1] - a[1]) * j / n) for j in range(n)]
    return out


# The paper: a thin skin over the billow, its outline torn. (Its interior is filled flat by the prism, so the
# sail is built as a grid of small quads clipped to the outline instead — it then follows the billow everywhere.)
outline = torn(top, right, 9, 0.025, 1) + torn(right, bottom, 11, 0.03, 2) + torn(bottom, left, 11, 0.035, 3) + torn(left, top, 9, 0.02, 4)


def inside(x, y):
    hit = False
    for i in range(len(outline)):
        (ax, ay), (bx, by) = outline[i], outline[i - 1]
        if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
            hit = not hit
    return hit


# The painted face (slanted eyes with a pupil slit, a long jagged grin) is painted into the paper's own cells.
EYES = []
for sx in (-1, 1):
    EYES.append(([(sx * 0.07, 0.22), (sx * 0.2, 0.28), (sx * 0.22, 0.24), (sx * 0.1, 0.17)],
                 [(sx * 0.14, 0.205), (sx * 0.155, 0.255), (sx * 0.165, 0.25), (sx * 0.15, 0.2)]))
grin = []
for i in range(11):
    t = i / 10
    grin.append((-0.24 + 0.48 * t, -0.12 - 0.08 * math.sin(t * math.pi) + (0.03 if i % 2 else 0.0)))
for i in range(10, -1, -1):
    t = i / 10
    grin.append((-0.24 + 0.48 * t, -0.15 - 0.1 * math.sin(t * math.pi) - (0.03 if i % 2 else 0.0)))


def in_poly(poly, x, y):
    hit = False
    for i in range(len(poly)):
        (ax, ay), (bx, by) = poly[i], poly[i - 1]
        if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
            hit = not hit
    return hit


def paint(x, y):
    for eye, pup in EYES:
        if in_poly(eye, x, y) and not in_poly(pup, x, y):
            return K.CRIMSON
    return K.CRIMSON if in_poly(grin, x, y) else K.INK


step = 0.02
xs = [-0.5 + step * i for i in range(int(1.0 / step) + 1)]
ys = [-0.7 + step * j for j in range(int(1.4 / step) + 1)]


def snap(x, y):
    """The nearest point on the torn outline (grid points just outside it are pulled onto the edge)."""
    best, bd = (x, y), 1e9
    for i in range(len(outline)):
        (ax, ay), (bx, by) = outline[i - 1], outline[i]
        dx, dy = bx - ax, by - ay
        t = max(0.0, min(1.0, ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy or 1)))
        px, py = ax + dx * t, ay + dy * t
        d = (px - x) ** 2 + (py - y) ** 2
        if d < bd:
            best, bd = (px, py), d
    return best


IN = {(i, j): inside(x, y) for i, x in enumerate(xs) for j, y in enumerate(ys)}
fr, bk = {}, {}


def vert(i, j):
    if (i, j) not in fr:
        x, y = (xs[i], ys[j]) if IN[i, j] else snap(xs[i], ys[j])
        fr[i, j] = k._v(on(x, y, -0.002))
        bk[i, j] = k._v(on(x, y, 0.002))
    return fr[i, j], bk[i, j]


for i in range(len(xs) - 1):
    for j in range(len(ys) - 1):
        cell = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
        if any(IN[c] for c in cell):
            f = [vert(*c)[0] for c in cell]
            b = [vert(*c)[1] for c in reversed(cell)]
            k._face(f, K.INK)
            k._face(b, K.INK)
# ...and the torn margin as a ribbon round it, joined to the grid's ragged edge visually by overlap.
edge = subdivide(outline, 0.02)
ring = [Vector(on(x * 0.985, y * 0.985, 0)) for x, y in edge]
k.tube(ring + [ring[0]], 0.0035, sides=4, cap=False, flat=0.6)
# Curled torn corners: flaps of paper peeling over at the side corners and the bottom.
for (cx, cy, dx, dy) in [(0.36, 0.1, -1, 0.3), (-0.37, 0.05, 1, 0.5), (0.04, -0.56, -0.4, 1)]:
    d = Vector((dx, dy, 0)).normalized()
    pts = [Vector(on(cx, cy, 0)) + d * (0.06 * s) + Vector((0, 0, -0.05 * s * s)) for s in (0.0, 0.35, 0.7, 1.0)]
    k.blade(pts, [0.04, 0.038, 0.03, 0.012], thick=0.0015, normal=(0, 0, -1), camber=-0.15, cols=5)
# Bamboo: a jointed spine and a bent bow, behind the paper, bound with crimson cord.
spine = [Vector((0, 0.68 - 1.36 * t, 0.012)) for t in [j / 20 for j in range(21)]]
k.tube(spine, 0.015, sides=8)
bow = [Vector((-0.47 + 0.94 * t, 0.06 + 0.07 * math.sin(math.pi * t), 0.012 + 0.03 * math.sin(math.pi * t))) for t in [j / 20 for j in range(21)]]
k.tube(bow, 0.012, sides=8)
for strut, rad in ((spine, 0.015), (bow, 0.012)):
    for j in (4, 8, 12, 16):
        p, d = strut[j], (strut[j + 1] - strut[j - 1]).normalized()
        k.tube([p - d * 0.006, p, p + d * 0.006], [rad * 1.05, rad * 1.3, rad * 1.05], sides=8)     # the node rings
for (x, y) in [(0, 0.12), (0, 0.63), (0, -0.63), (-0.44, 0.07), (0.44, 0.07)]:
    for j in range(4):
        a = 0.6 + j * 0.25
        k.tube([(x - 0.026 * math.cos(a), y - 0.02 + j * 0.012 - 0.026 * math.sin(a), -0.004),
                (x + 0.026 * math.cos(a), y - 0.02 + j * 0.012 + 0.026 * math.sin(a), -0.004)], 0.0055, sides=5, mat=K.CRIMSON)
# The paint lies just proud of the paper, laid in narrow pieces so each follows the billow.
for eye, pup in EYES:
    k.prism(subdivide(eye, 0.02), -0.008, -0.004, mat=K.CRIMSON, xform=warp)
    k.prism(pup, -0.01, -0.007, xform=warp)
top_g, bot_g = grin[:11], list(reversed(grin[11:]))
for i in range(10):
    quad = [bot_g[i], bot_g[i + 1], top_g[i + 1], top_g[i]]
    k.prism(quad, -0.008, -0.004, mat=K.CRIMSON, xform=warp)
# Drips of paint running down from the grin.
for (x, L) in [(-0.12, 0.08), (0.03, 0.12), (0.17, 0.06)]:
    y0 = -0.2 - 0.08 * math.sin((x + 0.24) / 0.48 * math.pi)
    k.tube([on(x, y0 - L * t, -0.004) for t in (0.0, 0.5, 1.0)], [0.008, 0.006, 0.004], sides=5, mat=K.CRIMSON, flat=0.4)
# Patches glued over old tears, and brushed prayer characters running down the sail.
for (x, y, w, h, a) in [(-0.1, -0.33, 0.12, 0.09, 0.3), (0.12, 0.35, 0.1, 0.08, -0.4), (-0.22, 0.02, 0.08, 0.07, 0.8)]:
    ca, sa = math.cos(a), math.sin(a)
    quad = [(x + px * ca - py * sa, y + px * sa + py * ca) for px, py in [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]]
    k.prism(subdivide(quad, 0.02), -0.0035, -0.002, xform=warp)
for col, x in enumerate((-0.12, 0.12)):
    for j in range(5):
        y = 0.42 - j * 0.1 if col == 0 else -0.3 - j * 0.06
        if col == 1 and j > 2:
            break
        k.prism([(x - 0.03, y), (x + 0.02, y + 0.01), (x + 0.025, y - 0.008), (x - 0.025, y - 0.015)], -0.005, -0.0035, xform=warp)
        k.prism([(x - 0.004, y + 0.02), (x + 0.006, y + 0.02), (x + 0.004, y - 0.04), (x - 0.006, y - 0.035)], -0.005, -0.0035, xform=warp)
# Prayer slips and torn paper tassels hanging off the ends of the bow, twisting as they hang.
rnd = random.Random(9)
for sx in (-1, 1):
    for j in range(4):
        x0 = sx * (0.44 - 0.025 * j)
        L = 0.2 + 0.05 * j + rnd.uniform(0, 0.04)
        pts = [Vector((x0 + 0.02 * sx * s * s, 0.06 - L * s, 0.01 * math.sin(s * 4 + j))) for s in (0.0, 0.25, 0.5, 0.75, 1.0)]
        nrm = (math.sin(j * 1.3) * 0.6, 0, -1)
        k.blade(pts, [0.008, 0.014 - 0.002 * j, 0.014, 0.012, 0.006], thick=0.0015, normal=nrm, camber=0.2, cols=4,
                mat=K.CRIMSON if j == 1 else K.INK)
k.build()
