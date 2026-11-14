# Aqueduct: a two-tier arcade, partly fallen, overgrown — a landmark for the mid/far layers (key art).
# Origin at the foot of the first pier; spans x 0..31; depth z 0..2.
import math
import random
import creature_kit as K

a = K.Part("Aqueduct")
rnd = random.Random(11)
Z0, Z1 = 0.0, 2.0
# Lower tier: six tall bays; the last one has fallen to half.
span, pier = 5.2, 0.9
for i in range(6):
    x0 = i * span
    top = 9.0 if i < 5 else 7.4
    a.bay(x0, x0 + span, 0.0, top, pier, 4.5, Z0, Z1, segs=16, ragged=0.0 if i < 5 else 0.6, seed=i)
    # Pier plinths and a few fallen blocks at the feet.
    a.box(x0 - 0.15, 0.0, x0 + pier + 0.15, 0.7, Z0 - 0.15, Z1 + 0.15)
    for j in range(rnd.randint(1, 3)):
        bx = x0 + rnd.uniform(-0.8, 1.5)
        s = rnd.uniform(0.3, 0.7)
        a.box(bx, 0.0, bx + s, s * 0.7, Z0 - 0.3, Z0 + 0.3)
a.box(6 * span - pier, 0.0, 6 * span + 0.15, 0.7, Z0 - 0.15, Z1 + 0.15)
# String course between the tiers.
a.box(-0.2, 9.0, 5 * span + 0.2, 9.35, Z0 - 0.15, Z1 + 0.15)
# Upper tier: small arches; one bay has collapsed, the end is broken off raggedly.
span2, pier2 = 2.6, 0.45
for i in range(10):
    if i == 6:
        continue  # collapsed: a gap in the arcade
    x0 = i * span2
    a.bay(x0, x0 + span2, 9.35, 12.6, pier2, 1.6, Z0 + 0.15, Z1 - 0.15, segs=12, ragged=0.35 if i in (5, 7, 9) else 0.08, seed=20 + i)
# Cornice and the water channel's parapet on top, crumbling.
for i in range(10):
    if i in (6, 7):
        continue
    x0 = i * span2
    a.box(x0 - 0.1, 12.6, x0 + span2 + 0.1, 12.85, Z0, Z1)
    a.box(x0, 12.85, x0 + span2, 13.4 - 0.5 * rnd.random() * (i > 7), Z0 + 0.1, Z0 + 0.4)
# Hanging vines from the arches and the broken edge.
for k in range(14):
    x = rnd.uniform(0.5, 25.5)
    if 15.6 < x < 18.2:
        x -= 3.0  # not in the collapsed bay
    y = 9.0 if rnd.random() < 0.6 else 12.6
    length = rnd.uniform(1.0, 3.2)
    pts = [(x + 0.15 * math.sin(t * 3 + k), y - length * t, Z0 - 0.05) for t in [j / 8 for j in range(9)]]
    a.tube(pts, [0.09 * (1 - 0.7 * j / 8) + 0.015 for j in range(9)], sides=5)
    for leaf in range(4):  # leaves along the vine
        px, py, pz = pts[2 + leaf * 2 if 2 + leaf * 2 < 9 else 8]
        a.prism([(0, 0), (0.12, 0.05), (0.18, -0.03), (0.06, -0.06)], pz - 0.02, pz + 0.02,
                xform=lambda p, px=px, py=py, s=(1 if leaf % 2 else -1): K.Vector((p.x * s + px, p.y + py, p.z)))
a.displace(0.03, scale=1.3, seed=3)
a.build()
