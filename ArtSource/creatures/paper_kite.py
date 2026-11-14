# Paper kite wraith — a torn diamond of paper on bamboo, a painted face. Faces the camera (in the picture plane).
# The tail and the thread are drawn by the game.
import math
from mathutils import Vector
import creature_kit as K

k = K.Part("Sail")
# Diamond sail with torn edges and bites.
top, right, bottom, left = (0, 0.62), (0.42, 0.08), (0, -0.62), (-0.42, 0.08)


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


outline = torn(top, right, 9, 0.025, 1) + torn(right, bottom, 11, 0.03, 2) + torn(bottom, left, 11, 0.035, 3) + torn(left, top, 9, 0.02, 4)
k.prism(outline, -0.006, 0.006)
# Bamboo struts (spine and cross bow), bound with crimson cord.
k.tube([(0, 0.66, -0.02), (0, -0.66, -0.02)], 0.016, sides=6)
k.tube([(-0.46, 0.06, -0.02), (-0.2, 0.12, -0.02), (0.2, 0.12, -0.02), (0.46, 0.06, -0.02)], 0.014, sides=6)
for (x, y) in [(0, 0.11), (0, 0.6), (0, -0.6), (-0.43, 0.07), (0.43, 0.07)]:
    for j in range(3):
        k.tube([(x - 0.025, y - 0.015 + j * 0.015, -0.03), (x + 0.025, y + 0.005 + j * 0.015, -0.03)], 0.006, sides=4, mat=K.CRIMSON)
# The painted face: slanted eyes, a long jagged grin.
for sx in (-1, 1):
    eye = [(sx * 0.07, 0.22), (sx * 0.2, 0.28), (sx * 0.22, 0.24), (sx * 0.1, 0.17)]
    if sx < 0:
        eye = list(reversed(eye))
    k.prism(eye, -0.012, -0.006, mat=K.CRIMSON)
grin = []
for i in range(11):
    t = i / 10
    x = -0.24 + 0.48 * t
    y = -0.12 - 0.08 * math.sin(t * math.pi)
    grin.append((x, y + (0.03 if i % 2 else 0.0)))
for i in range(10, -1, -1):
    t = i / 10
    x = -0.24 + 0.48 * t
    grin.append((x, -0.15 - 0.1 * math.sin(t * math.pi) - (0.03 if i % 2 else 0.0)))
k.prism(grin, -0.012, -0.006, mat=K.CRIMSON)
# Paper tassels hanging from the side corners.
for sx in (-1, 1):
    for j in range(3):
        x0 = sx * (0.42 - 0.02 * j)
        k.prism([(x0 - 0.012, 0.06), (x0 + 0.012, 0.06), (x0 + 0.02 + 0.02 * sx, -0.18 - 0.04 * j), (x0 + 0.005 * sx, -0.2 - 0.04 * j)], -0.004, 0.004)
k.build()
