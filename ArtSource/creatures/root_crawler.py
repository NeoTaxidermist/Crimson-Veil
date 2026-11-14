# Root crawler — a knot of black roots with a cluster of crimson eyes.
# Each "Root{i}" is its own object pointing up (+y) from the centre: the game rotates it to i*40° and wiggles it.
import math
import random
from mathutils import Vector
import creature_kit as K

core = K.Part("Knot")
core.blob((0, 0, 0), (0.24, 0.2, 0.2), segs=24, rings=16, bumps=0.35, seed=7)
# Gnarled rootlets wrapped around the knot.
rnd = random.Random(4)
for i in range(14):
    a = rnd.uniform(0, 2 * math.pi)
    b = rnd.uniform(-0.8, 0.8)
    d = Vector((math.cos(a) * math.cos(b), math.sin(b), math.sin(a) * math.cos(b) * 0.8))
    side = d.cross(Vector((0, 0, 1))).normalized() if abs(d.z) < 0.9 else Vector((1, 0, 0))
    pts = [d * (0.17 + 0.02 * k) + side * (0.06 * math.sin(k * 1.3)) for k in range(6)]
    core.tube(pts, [0.03 - 0.004 * k for k in range(6)], sides=6, seed=i)
# Eyes: a spider-like cluster of four, the front pair bigger.
for (x, y, z, r) in [(0.17, 0.05, -0.12, 0.045), (0.2, 0.0, -0.05, 0.04), (0.15, 0.11, -0.06, 0.025), (0.12, -0.05, -0.15, 0.022)]:
    core.blob((x, y, z), (r, r, r * 0.6), segs=10, rings=6, mat=K.CRIMSON)
# A maw: thorny teeth under the eyes.
for k in range(5):
    x = 0.12 + 0.03 * k
    core.tube([(x, -0.12, -0.12), (x + 0.01, -0.2, -0.14)], [0.012, 0.002], sides=5)
# Thorns bristling from the knot, pale bracket fungus growing on it, more small eyes waking up.
for i in range(16):
    a = rnd.uniform(0, 2 * math.pi)
    b2 = rnd.uniform(-1.0, 1.0)
    d = Vector((math.cos(a) * math.cos(b2), math.sin(b2), math.sin(a) * math.cos(b2) * 0.8))
    base = Vector((d.x * 0.22, d.y * 0.18, d.z * 0.18))
    core.tube([base, base + d * rnd.uniform(0.06, 0.12)], [0.014, 0.001], sides=4)
for (x, y, z) in [(-0.15, 0.12, -0.1), (-0.2, 0.0, -0.06), (-0.08, 0.17, -0.08)]:
    core.lathe([(0.0, 0.0), (0.05, 0.0), (0.045, 0.015), (0.0, 0.02)], sides=10, center=(x, y, z), sz=0.6)
for (x, y, z, r) in [(0.08, 0.14, -0.13, 0.014), (0.21, 0.08, 0.02, 0.012), (0.06, -0.02, -0.19, 0.012), (0.18, -0.08, -0.08, 0.01)]:
    core.blob((x, y, z), (r, r, r * 0.6), segs=8, rings=5, mat=K.CRIMSON)
core.build()

lengths = [0.55, 0.72, 0.5, 0.8, 0.62, 0.7, 0.48, 0.75, 0.6]
for i, L in enumerate(lengths):
    root = K.Part("Root%d" % i)
    r = random.Random(100 + i)
    pts = []
    for k in range(10):
        t = k / 9
        bend = 0.12 * math.sin(t * 3.0 + i) + 0.1 * t * t * (1 if i % 2 else -1)
        pts.append((bend, 0.12 + L * t, 0.03 * math.sin(t * 5 + i)))
    root.tube(pts, [0.045 * (1 - 0.85 * (k / 9)) + 0.004 for k in range(10)], sides=7, jitter=0.35, seed=i)
    # Side twigs and a hooked, clawed tip.
    for s in range(2):
        k0 = 3 + s * 3
        base = Vector(pts[k0])
        dirx = 1 if (i + s) % 2 else -1
        twig = [base + Vector((dirx * 0.04 * j, 0.05 * j, 0)) + Vector((dirx * 0.02 * j * j, 0, 0)) for j in range(4)]
        root.tube(twig, [0.018, 0.013, 0.008, 0.003], sides=5)
    tip = Vector(pts[-1])
    hook = [tip + Vector((0.05 * math.sin(j * 0.9) * (1 if i % 2 else -1), 0.04 * math.cos(j * 0.9), 0)) for j in range(5)]
    root.tube(hook, [0.008, 0.007, 0.005, 0.003, 0.001], sides=5)
    root.build()
