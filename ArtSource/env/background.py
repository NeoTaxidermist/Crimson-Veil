# Background set pieces for the depth layers (key art density):
#   DeadTree – a gnarled dead tree with roots and bark ridges (~7 m)
#   Statue   – a hooded statue holding a broken sword on a pedestal, a crimson rag tied to the blade (~5.5 m)
#   Cage     – a hanging gibbet cage with bones, on a chain (~4 m, hangs from the origin)
#   Spires   – a far skyline of gothic spires, pinnacles and flying buttresses (~34 m wide)
# build_env.py passes the piece name as VEG_PART.
import math
import random
from mathutils import Vector
import creature_kit as K

PART = globals().get("VEG_PART", "DeadTree")
rnd = random.Random(sum(map(ord, PART)) * 7)


def branch(p, start, direction, length, radius, depth, seed):
    """Recursive gnarled branch: a bent tapering tube with bark ridges, splitting into thinner ones."""
    r = random.Random(seed)
    pts, radii = [], []
    pos, d = Vector(start), Vector(direction).normalized()
    steps = 7
    for i in range(steps + 1):
        t = i / steps
        pts.append(pos.copy())
        radii.append(radius * (1 - 0.75 * t) + 0.01)
        bend = Vector((r.uniform(-0.35, 0.35), r.uniform(-0.1, 0.25), r.uniform(-0.15, 0.15)))
        d = (d + bend * 0.5).normalized()
        pos = pos + d * (length / steps)
    p.tube(pts, radii, sides=7 if radius > 0.08 else 5, jitter=0.25, seed=seed)
    if radius > 0.12:  # bark ridges: thin strands running along the thick parts
        for k in range(3):
            off = Vector((math.cos(k * 2.1) * radius * 0.9, 0, math.sin(k * 2.1) * radius * 0.9 - 0.02))
            p.tube([q + off * (1 - 0.7 * j / steps) for j, q in enumerate(pts[:5])], radius * 0.12, sides=3)
    if depth <= 0:
        return
    for k in range(r.randint(2, 3)):
        at = r.randint(3, steps)
        side = 1 if k % 2 == 0 else -1
        nd = (d * 0.6 + Vector((side * r.uniform(0.7, 1.6), r.uniform(-0.1, 0.6), r.uniform(-0.3, 0.3)))).normalized()
        branch(p, pts[at], nd, length * r.uniform(0.5, 0.75), radii[at] * 0.7, depth - 1, seed * 3 + k + 1)


if PART == "DeadTree":
    t = K.Part("DeadTree")
    branch(t, (0, 0, 0), (0.15, 1, 0), 4.2, 0.42, 3, 11)
    # Roots clawing into the ground.
    for k in range(6):
        a = (k / 6) * math.pi * 2 + rnd.uniform(-0.3, 0.3)
        root = [Vector((math.cos(a) * 0.15, 0.4, math.sin(a) * 0.15))]
        for j in range(1, 6):
            root.append(Vector((math.cos(a) * 0.35 * j, 0.4 - 0.12 * j - rnd.uniform(0, 0.05), math.sin(a) * 0.25 * j)))
        t.tube(root, [0.22 * (1 - j / 6) + 0.02 for j in range(6)], sides=6)
    t.build()

elif PART == "Statue":
    s = K.Part("Statue")
    # Pedestal: plinth, die with an inscription band of carved marks, cornice.
    s.box(-1.2, 0, 1.2, 0.35, -0.9, 0.9)
    s.ashlar(-1.0, 1.0, 0.35, 1.9, -0.75, course=0.38, proud=(0.03, 0.06), bevel=0.04, seed=5)
    s.box(-1.0, 0.35, 1.0, 1.9, -0.72, 0.72)
    for k in range(9):  # worn glyphs
        x = -0.8 + k * 0.2
        s.box(x, 1.55, x + rnd.uniform(0.06, 0.12), 1.55 + rnd.uniform(0.08, 0.16), -0.78, -0.74)
    s.box(-1.15, 1.9, 1.15, 2.1, -0.85, 0.85)
    # Robed figure: a lathe robe with deep folds, hood, a shoulder cape.
    base = 2.1
    prof = [(0.0, base), (0.62, base), (0.6, base + 0.4), (0.5, base + 1.3), (0.44, base + 2.0), (0.47, base + 2.3),
            (0.3, base + 2.55), (0.24, base + 2.75), (0.27, base + 3.05), (0.18, base + 3.3), (0.0, base + 3.38)]
    s.lathe([(r, y) for r, y in prof], sides=28, center=(0, 0, 0), sz=0.7,
            rfunc=lambda th, i, r, y: 1 + (0.09 * abs(math.sin(th * 5)) if y < base + 2.0 else 0.03 * math.sin(th * 3)))
    # The face is shadow under the hood: a dark recess.
    s.blob((0.12, base + 2.92, -0.16), (0.12, 0.15, 0.06))
    # A cape flaring from the shoulders behind, its hem torn.
    hem = [(-0.95, base + 0.35), (-0.7, base + 0.15), (-0.55, base + 0.4), (-0.35, base + 0.1), (0.0, base + 0.3), (0.35, base + 0.05),
           (0.6, base + 0.35), (0.85, base + 0.2)]
    s.prism([(-0.42, base + 2.35), (0.42, base + 2.35)] + list(reversed(hem)), 0.15, 0.3)
    # Right arm raised, holding a broken sword aloft (the blade outside the silhouette, against the sky).
    sh = (0.38, base + 2.3, -0.05)
    s.tube([sh, (0.62, base + 2.75, -0.12), (0.7, base + 3.3, -0.15)], [0.12, 0.1, 0.09], sides=8)
    hx, hy = 0.72, base + 3.38
    s.tube([(hx, hy - 0.12, -0.15), (hx, hy + 0.12, -0.15)], 0.045, sides=6)                    # grip
    s.box(hx - 0.3, hy + 0.12, hx + 0.3, hy + 0.2, -0.2, -0.1)                                 # cross-guard
    s.prism([(hx - 0.06, hy + 0.2), (hx + 0.06, hy + 0.2), (hx + 0.05, hy + 1.3), (hx + 0.1, hy + 1.45),
             (hx - 0.02, hy + 1.38), (hx - 0.06, hy + 1.5)], -0.18, -0.12)                       # blade, snapped
    # Left arm broken off at the elbow: a stump reaching out.
    s.tube([(-0.38, base + 2.3, -0.05), (-0.62, base + 1.95, -0.15), (-0.74, base + 1.85, -0.18)], [0.12, 0.1, 0.1], sides=8)
    s.prism([(-0.8, base + 1.95), (-0.68, base + 1.98), (-0.72, base + 1.78), (-0.82, base + 1.8)], -0.27, -0.1)
    # A crimson rag knotted under the sword's guard, its ends streaming in the wind.
    s.blob((hx, hy + 0.05, -0.2), (0.08, 0.06, 0.05), mat=K.CRIMSON)
    for k, (l, lift) in enumerate([(1.1, 0.25), (0.8, -0.05)]):
        pts = [(hx + l * j / 6, hy + 0.05 + lift * math.sin(j * 0.6) - 0.08 * j / 6, -0.22 - 0.01 * k) for j in range(7)]
        s.tube(pts, [0.06 * (1 - 0.6 * j / 6) + 0.01 for j in range(7)], sides=4, flat=0.3, mat=K.CRIMSON)
    # Weathering: chips off the hood and the pedestal corner, moss strands.
    for k in range(6):
        x = rnd.uniform(-0.9, 0.9)
        s.tube([(x, 1.9, -0.76), (x + rnd.uniform(-0.1, 0.1), 1.9 - rnd.uniform(0.3, 0.9), -0.78)], 0.025, sides=3)
    s.displace(0.012, scale=4.0, seed=3)
    s.build()

elif PART == "Cage":
    c = K.Part("Cage")
    # Chain up from the cage's crown to the origin (it hangs from where it is placed).
    y = -0.25
    k = 0
    while y > -1.6:
        L, W = 0.1, 0.05
        ring = []
        for j in range(13):
            a = 2 * math.pi * j / 12
            px, py = math.cos(a) * W, math.sin(a) * L
            ring.append(Vector((px, y - L + py, 0)) if k % 2 == 0 else Vector((0, y - L + py, px)))
        c.tube(ring, 0.013, sides=4, cap=False)
        y -= L * 1.6
        k += 1
    top, bottom = -1.7, -3.9
    # The cage: a dome of bars, hoops, a floor grid.
    bars = 12
    for i in range(bars):
        a = 2 * math.pi * i / bars
        pts = []
        for j in range(9):
            t = j / 8
            yy = top - (top - bottom) * t
            rr = 0.55 * math.sin(min(1.0, t * 2.2) * math.pi / 2) if t < 0.45 else 0.55
            pts.append(Vector((math.cos(a) * rr, yy, math.sin(a) * rr * 0.7)))
        c.tube(pts, 0.022, sides=4)
    for yy, rr in ((top - 0.55, 0.5), (top - 1.3, 0.55), (bottom + 0.05, 0.55)):
        hoop = [Vector((math.cos(2 * math.pi * j / 24) * rr, yy, math.sin(2 * math.pi * j / 24) * rr * 0.7)) for j in range(25)]
        c.tube(hoop, 0.03, sides=4, cap=False)
    c.box(-0.55, bottom - 0.05, 0.55, bottom + 0.02, -0.4, 0.4)
    c.tube([(0, top + 0.1, 0), (0, top - 0.05, 0)], 0.06, sides=6)  # crown ring
    # Bones of the one who died in it: skull, ribs, a dangling arm through the bars.
    c.blob((0.1, bottom + 0.62, -0.05), (0.13, 0.15, 0.13))
    for k in range(5):
        yy = bottom + 0.2 + k * 0.07
        c.tube([(-0.2, yy, -0.05), (0.0, yy + 0.04, -0.15), (0.2, yy, -0.05)], 0.015, sides=3)
    c.tube([(0.2, bottom + 0.35, -0.2), (0.5, bottom + 0.2, -0.38), (0.62, bottom - 0.25, -0.4)], [0.025, 0.02, 0.015], sides=4)
    # A crimson rag caught on the bars.
    c.prism([(0.35, top - 0.8), (0.5, top - 0.85), (0.55, top - 1.55), (0.46, top - 1.4), (0.4, top - 1.6), (0.33, top - 1.2)], -0.42, -0.4, mat=K.CRIMSON)
    c.build()

elif PART == "Spires":
    s = K.Part("Spires")
    # A far skyline: a cathedral mass with a great spire, lesser towers, pinnacles and flying buttresses.
    def tower(x, w, h, spire, z0=0.0, z1=4.0, seed=0):
        s.bay(x - w / 2, x + w / 2, 0, h * 0.55, w * 0.22, h * 0.28, z0, z1, segs=10, pointed=True, seed=seed, masonry=False)
        s.bay(x - w / 2, x + w / 2, h * 0.55, h, w * 0.28, h * 0.2, z0, z1, segs=10, pointed=True, seed=seed + 1, masonry=False)
        s.crenellate(x - w / 2 - 0.2, x + w / 2 + 0.2, h, 0.45, 0.3, 0.6, z0, z0 + 0.6, seed=seed + 2, broken=0.2)
        if spire > 0:
            s.prism([(x - w * 0.42, h), (x + w * 0.42, h), (x + 0.08, h + spire), (x - 0.08, h + spire)], z0 + 0.5, z1 - 0.5)
            for side in (-1, 1):  # corner pinnacles
                px = x + side * w * 0.45
                s.prism([(px - 0.25, h), (px + 0.25, h), (px, h + spire * 0.25)], z0, z0 + 0.5)
            s.tube([(x, h + spire - 0.1, z0 + 2), (x, h + spire + 1.2, z0 + 2)], 0.06, sides=4)  # finial
    # Nave between the towers, with a rose window and buttresses.
    s.bay(-9, 9, 0, 13, 1.3, 7.5, 0.5, 3.5, segs=12, pointed=True, seed=40, masonry=False)
    s.prism([(-9.5, 13), (9.5, 13), (0, 18)], 0.6, 3.4)
    rose = [(math.cos(2 * math.pi * j / 20) * 1.6, 15 + math.sin(2 * math.pi * j / 20) * 1.6, 0.45) for j in range(21)]
    s.tube(rose, 0.12, sides=4, cap=False)
    for j in range(8):
        a = 2 * math.pi * j / 8
        s.tube([(0, 15, 0.45), (math.cos(a) * 1.6, 15 + math.sin(a) * 1.6, 0.45)], 0.07, sides=3)
    tower(-12, 5, 20, 11, seed=1)
    tower(12, 5, 17, 0, seed=7)          # its spire fell long ago
    tower(0, 3.2, 21, 13, z0=0.8, z1=3.2, seed=13)
    tower(-21, 3.6, 12, 6, seed=19)
    tower(19.5, 3.0, 10, 5, seed=23)
    for x in (-7, -3.5, 3.5, 7):  # flying buttresses
        s.prism([(x - 0.3, 0), (x + 0.3, 0), (x + 0.3, 9), (x - 1.2 * (1 if x < 0 else -1) + 0.3, 12.5), (x - 1.2 * (1 if x < 0 else -1) - 0.3, 12.5), (x - 0.3, 9)], 0.2, 0.6)
        s.prism([(x - 0.35, 9), (x + 0.35, 9), (x, 11)], 0.15, 0.65)  # pinnacle on the pier
    s.build()
