# Grapple ring for the whip: an iron ring (pivot = its centre, where the whip wraps) hanging on a chain from
# a broken timber beam, with a crimson rag tied to it so the player reads it at a glance (key art accent).
import math
import random
from mathutils import Vector
import creature_kit as K

g = K.Part("GrappleRing")
rnd = random.Random(42)

# Ring: a torus in the picture plane.
R, r = 0.34, 0.055
ring = [(math.cos(2 * math.pi * k / 24) * R, math.sin(2 * math.pi * k / 24) * R, 0.0) for k in range(25)]
g.tube(ring, r, sides=8, cap=False)
# A collar where the chain is forged on.
g.tube([(0, R - 0.02, 0), (0, R + 0.12, 0)], [0.08, 0.06], sides=8)

# Chain: alternating links up to the beam.
y = R + 0.12
top = 3.1
k = 0
while y < top - 0.1:
    L, W = 0.13, 0.065
    pts = []
    for j in range(17):
        a = 2 * math.pi * j / 16
        px, py = math.cos(a) * W, math.sin(a) * L
        pts.append(Vector((px, y + L + py, 0)) if k % 2 == 0 else Vector((0, y + L + py, px)))
    g.tube(pts, 0.018, sides=5, cap=False)
    y += L * 1.6
    k += 1

# Iron staple through the beam and the beam itself: a squared timber with broken, splintered ends.
g.box(-0.05, top - 0.1, 0.05, top + 0.25, -0.05, 0.05)
bx0, bx1, by0, by1 = -1.3, 1.1, top + 0.1, top + 0.55
outline = [(bx0, by0), (bx1, by0), (bx1 + 0.12, by0 + 0.12), (bx1 - 0.05, by0 + 0.22), (bx1 + 0.18, by0 + 0.33),
           (bx1, by1), (bx0, by1), (bx0 - 0.2, by1 - 0.1), (bx0 + 0.02, by0 + 0.25), (bx0 - 0.15, by0 + 0.1)]
g.prism(outline, -0.25, 0.25)
for k in range(5):  # grain grooves standing proud on the front face
    yy = by0 + 0.08 + k * 0.085
    g.box(bx0 + 0.1 + rnd.uniform(0, 0.3), yy, bx1 - 0.1 - rnd.uniform(0, 0.3), yy + 0.018, -0.27, -0.24)
# Iron straps round the beam.
for x in (-0.55, 0.45):
    g.box(x, by0 - 0.03, x + 0.1, by1 + 0.03, -0.28, 0.28)

# Crimson rag knotted to the ring, hanging and fluttering a little.
knot = Vector((R * 0.7, -R * 0.7, -0.02))
g.blob(knot, (0.07, 0.06, 0.05), segs=8, rings=5, mat=K.CRIMSON)
for s, (w0, length, sway) in enumerate([(0.11, 0.75, 0.12), (0.07, 0.55, -0.1)]):
    pts = []
    for j in range(8):
        t = j / 7
        pts.append((knot.x + 0.06 * s + sway * math.sin(t * 2.4) * t, knot.y - length * t, knot.z - 0.01 * s))
    left = [(p[0] - w0 * (1 - 0.6 * j / 7), p[1]) for j, p in enumerate(pts)]
    right = [(p[0] + w0 * (1 - 0.6 * j / 7), p[1]) for j, p in enumerate(pts)]
    tail = [(pts[-1][0] + 0.03, pts[-1][1] - 0.08), (pts[-1][0] - 0.02, pts[-1][1] - 0.02)]  # torn tip
    g.prism(left + tail + list(reversed(right)), knot.z - 0.015 - 0.01 * s, knot.z + 0.005 - 0.01 * s, mat=K.CRIMSON)

g.displace(0.008, scale=4.0, seed=5)
g.build()
