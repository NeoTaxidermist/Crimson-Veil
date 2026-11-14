# Small modular ruin pieces for the near layers, rebuilt with full masonry detail (key art):
#   RuinWall   – a crumbling wall with two arched windows, x -4.7..4.7, y 0..7.4, depth -0.65..0.65
#   Tower      – a square tower with gothic windows and a broken crown, x -2.7..2.7, y 0..15.7
#   RuinStairs – a broken stone stair rising to the right, x -5..0.26, y 0..2.45
# build_env.py passes the piece name as VEG_PART.
import math
import random
import creature_kit as K

PART = globals().get("VEG_PART", "RuinWall")
rnd = random.Random(sum(map(ord, PART)) + 3)


def ivy(p, x, y, length, z, k):
    pts = [(x + 0.18 * math.sin(t * 4 + k), y - length * t, z) for t in [j / 9 for j in range(10)]]
    p.tube(pts, [0.06 * (1 - 0.7 * j / 9) + 0.012 for j in range(10)], sides=4)
    for j in range(2, 10, 2):
        px, py, pz = pts[j]
        s = 1 if j % 4 else -1
        p.prism([(0, 0), (0.13 * s, 0.06), (0.2 * s, -0.03), (0.07 * s, -0.08)], pz - 0.02, pz + 0.02,
                xform=lambda q, px=px, py=py: K.Vector((q.x + px, q.y + py, q.z)))


def rubble(p, x0, x1, z, n):
    for k in range(n):
        x = rnd.uniform(x0, x1)
        s = rnd.uniform(0.2, 0.55)
        p.stone(x, s * 0.35, s * rnd.uniform(1.0, 1.6), s * 0.7, z, s * 0.6, 0.06, rnd.uniform(-20, 20))


if PART == "RuinWall":
    w = K.Part("RuinWall")
    Z0, Z1 = -0.65, 0.65
    # Two window bays; the top steps down raggedly to the right where the wall has fallen.
    w.bay(-4.7, -0.2, 0.0, 7.4, 1.0, 3.6, Z0, Z1, segs=14, ragged=0.15, seed=1)
    w.bay(-0.2, 4.7, 0.0, 5.6, 1.0, 3.0, Z0, Z1, segs=14, ragged=0.45, seed=2)
    # Broken stepped edge between the two heights.
    w.prism([(-0.2, 5.6), (-0.2, 7.4), (-0.9, 7.4), (-0.6, 6.8), (-0.3, 6.3)], Z0, Z1)
    # Plinth course, string course and a cornice remnant.
    w.box(-4.85, 0.0, 4.85, 0.55, Z0 - 0.15, Z1 + 0.1)
    w.box(-4.9, 7.2, -1.2, 7.45, Z0 - 0.16, Z1 + 0.1)
    for x in [-4.6 + 0.45 * k for k in range(8)]:  # dentils under the cornice
        w.box(x, 7.0, x + 0.2, 7.2, Z0 - 0.1, Z0 + 0.1)
    # Buttress on the left.
    w.prism([(-5.3, 0), (-4.7, 0), (-4.7, 5.2), (-4.9, 5.2)], Z0 - 0.05, Z0 + 0.6)
    for k in range(5):
        ivy(w, rnd.uniform(-4.3, 3.6), rnd.choice([7.2, 5.5, 3.3]), rnd.uniform(1.0, 2.8), Z0 - 0.15, k)
    rubble(w, -4.5, 5.2, Z0 - 0.1, 12)
    w.displace(0.025, scale=1.5, seed=5)
    w.build()

elif PART == "Tower":
    t = K.Part("Tower")
    Z0, Z1 = -2.2, 2.2
    y = 0.0
    # Battered base, then four storeys with gothic windows separated by string courses.
    t.prism([(-2.7, 0), (2.7, 0), (2.35, 2.2), (-2.35, 2.2)], Z0 - 0.3, Z1 + 0.3)
    t.ashlar(-2.6, 2.6, 0.05, 2.15, Z0 - 0.3, course=0.55, proud=(0.07, 0.14), bevel=0.08, seed=9)
    y = 2.2
    for s, h in enumerate([3.2, 3.0, 2.8, 2.4]):
        last = s == 3
        t.bay(-2.2, 2.2, y, y + h, 1.25, h * 0.45, Z0, Z1, segs=12, pointed=True, ragged=0.5 if last else 0.0, seed=20 + s)
        if not last:
            t.box(-2.4, y + h - 0.12, 2.4, y + h + 0.1, Z0 - 0.15, Z1 + 0.15)
        y += h
    # The crown's broken stump on the left corner.
    t.prism([(-2.45, y - 2.4), (-1.0, y - 2.4), (-1.0, y - 1.0), (-1.2, y - 0.6), (-1.5, y + 0.6), (-2.0, y + 0.2), (-2.45, y + 1.2)], Z0 - 0.1, Z1)
    # Corner quoins down both edges.
    for k in range(int(y / 0.6)):
        for side in (-1, 1):
            yy = 2.3 + k * 0.6
            if yy > y - 2.6:
                break
            wq = 0.5 if k % 2 else 0.3
            t.stone(side * (2.2 - wq / 2), yy + 0.27, wq, 0.5, Z0, 0.1, 0.06)
    for k in range(4):
        ivy(t, rnd.uniform(-2.0, 2.0), rnd.uniform(6, 12), rnd.uniform(1.5, 3.5), Z0 - 0.2, k)
    rubble(t, -2.8, 3.0, Z0 - 0.35, 8)
    t.displace(0.025, scale=1.4, seed=6)
    t.build()

elif PART == "RuinStairs":
    s = K.Part("RuinStairs")
    Z0, Z1 = -0.8, 0.8
    # Eight steps rising to the right, a few broken off; each tread has a nosing; the stringer wall below.
    steps = 8
    for k in range(steps):
        x0 = -5.0 + k * 0.6
        top = 0.3 * (k + 1)
        broken = k in (2, 5)
        zf = Z0 + (0.5 if broken else 0.0)
        s.box(x0, 0.0, x0 + 0.62, top - (0.12 if broken else 0.0), zf, Z1)
        if not broken:
            s.box(x0 - 0.05, top - 0.08, x0 + 0.62, top + 0.02, Z0 - 0.07, Z1)  # nosing
    s.ashlar(-5.0, -0.2, 0.05, 1.0, Z0, course=0.32, proud=(0.04, 0.08), bevel=0.05, seed=4,
             skip=lambda cx, cy, w, h: cy + h / 2 > 0.3 * ((cx + 5.0) / 0.6) + 0.25)
    # Newel post with a broken cap at the top.
    s.box(0.0, 0.0, 0.26, 2.45, Z0 - 0.05, Z0 + 0.4)
    s.prism([(-0.05, 2.45), (0.31, 2.45), (0.25, 2.6), (0.05, 2.55)], Z0 - 0.1, Z0 + 0.45)
    rubble(s, -5.2, -1.0, Z0 - 0.1, 7)
    s.displace(0.015, scale=2.0, seed=2)
    s.build()
