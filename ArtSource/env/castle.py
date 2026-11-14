# Castle keep: a tall hall with rows of arched windows (open to the sky), two towers with gothic windows,
# battlements, a broken roof and a collapsed corner. Origin at the ground centre; x -12..12, depth 0..5.
import math
import random
import creature_kit as K

c = K.Part("Castle")
rnd = random.Random(5)
Z0, Z1 = 0.0, 5.0


def wall_with_windows(x0, x1, y0, rows, row_h, bay_w, pier, spring, z0, z1, pointed=False, ragged_top=0.0, seed=0):
    n = max(1, int(round((x1 - x0) / bay_w)))
    w = (x1 - x0) / n
    y = y0
    for r in range(rows):
        for i in range(n):
            # Ruin: the top storey has lost its right half; some windows were bricked up long ago.
            if ragged_top and r == rows - 1 and i >= n - 3:
                continue
            if rnd.random() < 0.18:
                c.box(x0 + i * w, y, x0 + (i + 1) * w, y + row_h, z0, z1)
                continue
            c.bay(x0 + i * w, x0 + (i + 1) * w, y, y + row_h, pier, spring, z0, z1, segs=12, pointed=pointed,
                  ragged=ragged_top if r == rows - 1 else 0.0, seed=seed + r * 13 + i)
        y += row_h
        # A string course between the storeys (not over the fallen part).
        course_end = x0 + (n - 3) * w if ragged_top and r == rows - 1 else x1 + 0.15
        c.box(x0 - 0.15, y - 0.2, course_end, y + 0.05, z0 - 0.2, z1 + 0.2)
    return y


# Plinth: a solid battered base of big rusticated blocks.
c.prism([(-9.5, 0), (9.5, 0), (9.1, 3.0), (-9.1, 3.0)], Z0 - 0.4, Z1 + 0.4)
c.ashlar(-9.3, 9.3, 0.05, 2.95, Z0 - 0.4, course=0.6, proud=(0.08, 0.16), bevel=0.09, seed=77)
# Main hall: three storeys of round-arched windows; the top storey crumbles.
top = wall_with_windows(-8.6, 8.6, 3.0, 3, 3.6, 2.4, 0.55, 1.4, Z0, Z1, ragged_top=0.5, seed=1)
c.crenellate(-8.6, 5.5, top, 0.6, 0.45, 0.6, Z0, Z0 + 0.6, seed=2, broken=0.3)
# Collapsed corner: a jagged stump of wall stepping down where the top storey fell.
base = top - 3.6
c.prism([(3.4, base), (8.6, base), (8.6, base + 0.9), (7.6, base + 0.5), (6.9, base + 1.4), (5.9, base + 1.0),
         (5.2, base + 2.3), (4.4, base + 1.9), (3.9, base + 3.1), (3.4, base + 3.6)], Z0, Z1)
# Towers on both sides: taller, gothic windows, conical roofs (one broken).
for side, (tx, broken_roof) in enumerate([(-11.0, False), (10.6, True)]):
    tw = 3.6
    y = wall_with_windows(tx - tw / 2, tx + tw / 2, 0.0, 6, 3.4, tw, 0.9, 1.2, Z0 + 0.5, Z1 - 0.5, pointed=True, seed=50 + side * 20)
    c.crenellate(tx - tw / 2 - 0.2, tx + tw / 2 + 0.2, y, 0.5, 0.35, 0.5, Z0 + 0.5, Z0 + 1.0, seed=60 + side, broken=0.15)
    if broken_roof:
        c.prism([(tx - 2.0, y), (tx + 1.0, y), (tx + 0.2, y + 2.4), (tx - 0.2, y + 1.8), (tx - 0.8, y + 3.4)], Z0 + 0.6, Z1 - 0.6)
    else:
        c.prism([(tx - 2.1, y), (tx + 2.1, y), (tx, y + 5.5)], Z0 + 0.6, Z1 - 0.6)
        c.tube([(tx, y + 5.4, Z0 + 2.0), (tx, y + 6.6, Z0 + 2.0)], 0.06, sides=5)  # spire rod
    # Arrow-slit buttresses.
    c.prism([(tx - tw / 2 - 0.6, 0), (tx - tw / 2, 0), (tx - tw / 2, 6), (tx - tw / 2 - 0.15, 6)], Z0 + 1, Z0 + 2)
# Rubble heaped against the hall where the corner fell.
for k in range(14):
    x = rnd.uniform(5.5, 10.0)
    s = rnd.uniform(0.3, 1.0)
    c.box(x, 0, x + s, s * rnd.uniform(0.5, 1.0) + max(0.0, (x - 5.5) * 0.25), Z0 - 0.6, Z0 + 0.2)
# Ivy creeping up the plinth and the left tower.
for k in range(10):
    x = rnd.uniform(-12.5, -6.0)
    pts = [(x + 0.3 * math.sin(t * 5 + k), t * rnd.uniform(4, 9), Z0 - 0.05) for t in [j / 10 for j in range(11)]]
    c.tube(pts, 0.05, sides=4)
c.displace(0.04, scale=1.0, seed=7)
c.build()
