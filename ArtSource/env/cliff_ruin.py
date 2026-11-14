# Cliff with ruins on top: a stratified rock mass with ledges, a waterfall notch, and a crumbled village above.
# Origin at the cliff foot centre; x -15..15, y 0..16 (+ buildings), depth 0..6.
import math
import random
import creature_kit as K

r = K.Part("CliffRuin")
rnd = random.Random(9)
Z0, Z1 = 0.0, 6.0
# Rock mass: ragged sides stepping in, a notch on the right where water falls.
outline = [(-15, 0)]
for k in range(1, 9):  # left face, stepping back in strata
    y = 16 * k / 8
    outline.append((-15 + 1.2 * k + rnd.uniform(-0.6, 0.4), y - rnd.uniform(0, 0.4)))
top_pts = []
for k in range(1, 20):
    x = -5.4 + 18.4 * k / 20
    y = 16 + rnd.uniform(-0.3, 0.3)
    if 6.0 < x < 7.4:  # the notch the waterfall pours from
        y -= 1.6
    top_pts.append((x, y))
outline += top_pts
for k in range(8, 0, -1):  # right face
    y = 16 * k / 8
    outline.append((14 - 0.7 * (k % 3) + rnd.uniform(-0.4, 0.4), y))
outline.append((15, 0))
r.prism(outline, Z0, Z1)
# Strata: protruding ledges across the face.
for k in range(7):
    y = 1.6 + k * 2.1 + rnd.uniform(-0.3, 0.3)
    x0 = -14 + k * 1.1 + rnd.uniform(0, 2)
    x1 = x0 + rnd.uniform(4, 12)
    r.prism([(x0, y), (x1, y + rnd.uniform(-0.2, 0.2)), (x1 - 0.4, y + 0.35), (x0 + 0.3, y + 0.3)], Z0 - 0.35, Z0 + 0.5)
# Boulders at the foot.
for k in range(10):
    x = rnd.uniform(-15, 15)
    s = rnd.uniform(0.6, 1.8)
    r.blob((x, s * 0.4, Z0 - 0.8), (s, s * 0.6, s * 0.7), segs=10, rings=6, bumps=0.3, seed=k)
# Village on top: small houses with gabled roofs, some roofless, a little bell tower.
houses = [(-4.5, 3.4, 3.0, False), (-0.6, 2.8, 2.4, True), (2.6, 3.8, 3.4, False), (9.0, 3.0, 2.6, True)]
for (hx, hw, hh, ruined) in houses:
    base = 16.0
    r.box(hx - hw / 2, base - 0.2, hx + hw / 2, base + hh, Z0 + 1, Z0 + 3.5)
    if ruined:
        r.prism([(hx - hw / 2, base + hh), (hx - hw / 2 + 0.5, base + hh + 0.9), (hx, base + hh + 0.3), (hx + 0.3, base + hh + 1.1), (hx + hw / 2, base + hh)], Z0 + 1, Z0 + 3.5)
    else:
        r.prism([(hx - hw / 2 - 0.3, base + hh), (hx + hw / 2 + 0.3, base + hh), (hx, base + hh + hw * 0.6)], Z0 + 0.9, Z0 + 3.6)
    # Window holes as dark inset frames would not read in a single tone: cut-outs via small bays instead.
r.bay(4.8, 7.0, 16.0, 22.5, 0.35, 3.2, Z0 + 1.2, Z0 + 2.4, segs=10, pointed=True, ragged=0.2, seed=3)  # bell tower
r.prism([(4.6, 22.5), (7.2, 22.5), (5.9, 24.6)], Z0 + 1.2, Z0 + 2.4)
# Window holes punched through the intact houses (small arched bays in the gable walls).
for (hx, hw, hh, ruined) in houses:
    if not ruined:
        r.bay(hx - 0.7, hx + 0.7, 16.0 + hh * 0.35, 16.0 + hh * 0.85, 0.3, 0.5, Z0 + 0.95, Z0 + 1.05, segs=8, seed=int(hx * 7))
# Dead trees leaning over the rim and roots hanging down the faces.
for (tx, lean) in [(-7.6, -0.5), (12.2, 0.6), (8.0, 0.2)]:
    base = (tx, 15.6 if abs(tx) > 9 else 16.0, Z0 + 0.6)
    pts = [(base[0] + lean * 2.2 * t * t, base[1] + 3.4 * t, base[2]) for t in [j / 6 for j in range(7)]]
    r.tube(pts, [0.22 * (1 - 0.8 * j / 6) + 0.02 for j in range(7)], sides=6)
    for b in range(5):  # crooked branches
        px, py, pz = pts[2 + b % 4]
        d = 1 if b % 2 else -1
        r.tube([(px, py, pz), (px + d * 0.8, py + 0.5, pz), (px + d * 1.4, py + 0.4 + 0.4 * rnd.random(), pz)],
               [0.07, 0.04, 0.01], sides=4)
for k in range(9):
    x = rnd.choice([rnd.uniform(-9, -5), rnd.uniform(10.5, 13.5)])
    y0 = rnd.uniform(9, 15.5)
    L = rnd.uniform(1.5, 4.0)
    pts = [(x + 0.2 * math.sin(t * 4 + k), y0 - L * t, Z0 - 0.1) for t in [j / 8 for j in range(9)]]
    r.tube(pts, [0.08 * (1 - 0.8 * j / 8) + 0.01 for j in range(9)], sides=4)
r.displace(0.12, scale=0.6, seed=4)
r.build()
