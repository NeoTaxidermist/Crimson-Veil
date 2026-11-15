# Root crawler — a knot of black roots with a cluster of crimson eyes.
# Each "Root{i}" is its own object pointing up (+y) from the centre: the game rotates it to i*40° and wiggles it.
# The knot is woven: thick strands wind round a gnarled core; a thorn-ringed maw gapes under six eyes sunk in
# sockets. Each root is three strands twisted into a rope that thins into a hooked claw, with rootlet hairs and thorns.
import math
import random
from mathutils import Vector
import creature_kit as K

rnd = random.Random(4)
core = K.Part("Knot")
core.blob((0, 0, 0), (0.2, 0.17, 0.17), segs=28, rings=18, bumps=0.3, seed=7)
# Strands wound round the core like a clenched fist of roots.
for i in range(18):
    tilt = rnd.uniform(-1.2, 1.2)
    phase = rnd.uniform(0, 2 * math.pi)
    ax = Vector((math.cos(tilt), math.sin(tilt), rnd.uniform(-0.4, 0.4))).normalized()
    u = ax.orthogonal().normalized()
    v = ax.cross(u)
    pts, rr = [], []
    for k in range(14):
        a = phase + k * 0.32
        r = 0.18 + 0.015 * math.sin(k * 1.7 + i)
        p = u * math.cos(a) * r + v * math.sin(a) * r + ax * (0.04 * math.sin(k * 0.6 + i))
        pts.append(Vector((p.x * 1.15, p.y, p.z * 0.95)))
        rr.append(0.03 * math.sin(math.pi * (k + 0.5) / 14) + 0.006)
    core.tube(pts, rr, sides=7, jitter=0.25, seed=i)
# The maw: a dark pit ringed by hooked thorn teeth, low on the front.
mc = Vector((0.15, -0.07, -0.1))
core.blob(mc, (0.06, 0.045, 0.04), segs=14, rings=8)
for k in range(12):
    a = 2 * math.pi * k / 12
    base = mc + Vector((0.0, math.sin(a) * 0.06, math.cos(a) * 0.07)) + Vector((0.03, 0, -0.03))
    inward = (mc - base).normalized()
    tip = base + inward * 0.045 + Vector((0.03, 0, -0.02))
    core.tube([base, base.lerp(tip, 0.6) + Vector((0.01, 0, 0)), tip], [0.011, 0.006, 0.0008], sides=5)
# Eyes: six, sunk in ridged sockets, the front pair biggest.
for (x, y, z, r) in [(0.13, 0.06, -0.15, 0.04), (0.18, 0.02, -0.11, 0.034), (0.08, 0.12, -0.13, 0.022),
                     (0.2, 0.08, -0.06, 0.02), (0.05, 0.03, -0.18, 0.018), (0.15, 0.13, -0.08, 0.015)]:
    c = Vector((x, y, z))
    n = c.normalized()
    t1 = n.orthogonal().normalized()
    t2 = n.cross(t1)
    lid = [c + (t1 * math.cos(2 * math.pi * j / 16) + t2 * math.sin(2 * math.pi * j / 16)) * r * 1.15 for j in range(17)]
    core.tube(lid, r * 0.35, sides=6, cap=False)
    core.blob(c, (r, r, r * 0.75), segs=12, rings=8, mat=K.CRIMSON)
    core.blob(c + n * r * 0.6, (r * 0.3, r * 0.3, r * 0.2), segs=6, rings=4)            # a slit of pupil
# Thorns, pale bracket fungus, rootlet hairs.
for i in range(22):
    a = rnd.uniform(0, 2 * math.pi)
    b2 = rnd.uniform(-1.0, 1.0)
    d = Vector((math.cos(a) * math.cos(b2), math.sin(b2), math.sin(a) * math.cos(b2) * 0.8))
    base = Vector((d.x * 0.22, d.y * 0.19, d.z * 0.19))
    bend = d.orthogonal().normalized() * 0.02
    L = rnd.uniform(0.06, 0.13)
    core.tube([base, base + d * L * 0.5 + bend, base + d * L + bend * 2.2], [0.016, 0.007, 0.0008], sides=5)
for (x, y, z) in [(-0.15, 0.12, -0.1), (-0.2, 0.0, -0.06), (-0.08, 0.17, -0.08), (-0.17, 0.07, -0.12)]:
    core.lathe([(0.0, 0.0), (0.055, 0.0), (0.05, 0.012), (0.03, 0.018), (0.0, 0.02)], sides=12, center=(x, y, z), sz=0.6,
               rfunc=lambda th, i, r, y: 1 + 0.08 * math.sin(th * 5))
for i in range(30):
    a = rnd.uniform(0, 2 * math.pi)
    d = Vector((math.cos(a), -abs(math.sin(a)) - 0.3, rnd.uniform(-0.6, 0.6))).normalized()
    base = Vector((d.x * 0.18, d.y * 0.16, d.z * 0.16))
    tip = base + d * rnd.uniform(0.05, 0.1) + Vector((rnd.uniform(-0.03, 0.03), -0.03, 0))
    core.tube([base, base.lerp(tip, 0.5) + Vector((0.01, 0, 0)), tip], [0.005, 0.003, 0.0005], sides=4)
core.displace(0.006, scale=9.0, seed=3)
core.build()

lengths = [0.55, 0.72, 0.5, 0.8, 0.62, 0.7, 0.48, 0.75, 0.6]
for i, L in enumerate(lengths):
    root = K.Part("Root%d" % i)
    sgn = 1 if i % 2 else -1
    N = 16
    axis = []
    for k in range(N):
        t = k / (N - 1)
        bend = 0.12 * math.sin(t * 3.0 + i) + 0.1 * t * t * sgn
        axis.append(Vector((bend, 0.1 + L * t, 0.03 * math.sin(t * 5 + i))))
    # Three strands twisted into a rope; they part near the base (where the root tears out of the knot).
    for s in range(3):
        pts, rr = [], []
        for k in range(N):
            t = k / (N - 1)
            a = 2 * math.pi * s / 3 + t * 7.0 + i
            spread = 0.026 * (1 - 0.8 * t) + 0.02 * max(0.0, 0.2 - t) * 5
            pts.append(axis[k] + Vector((math.cos(a) * spread, 0, math.sin(a) * spread)))
            rr.append(0.028 * (1 - 0.85 * t) + 0.003)
        root.tube(pts, rr, sides=7, jitter=0.3, seed=i * 3 + s)
    # Side twigs, rootlet hairs, thorns.
    for s in range(3):
        k0 = 3 + s * 4
        base = axis[k0]
        dirx = sgn if s % 2 else -sgn
        twig = [base + Vector((dirx * (0.018 * j + 0.005 * j * j), 0.028 * j, 0.006 * j)) for j in range(5)]
        root.tube(twig, [0.016, 0.012, 0.008, 0.004, 0.001], sides=5, jitter=0.2, seed=50 + s)
    r2 = random.Random(100 + i)
    for h in range(14):
        k0 = r2.randint(1, N - 3)
        base = axis[k0]
        d = Vector((r2.uniform(-1, 1), r2.uniform(-0.6, 0.2), r2.uniform(-1, 1))).normalized()
        if h % 2:
            root.tube([base + d * 0.02, base + d * 0.06 + Vector((0, 0.02, 0))], [0.008, 0.0006], sides=4)       # thorn
        else:
            tip = base + d * 0.07 + Vector((0, -0.03, 0))
            root.tube([base + d * 0.015, base.lerp(tip, 0.5) + d * 0.01, tip], [0.003, 0.002, 0.0004], sides=3)  # hair
    # A hooked, clawed tip.
    tip = axis[-1]
    hook = [tip + Vector((0.05 * math.sin(j * 0.45) * sgn, 0.04 * math.sin(j * 0.32 + 0.2), 0)) for j in range(10)]
    root.tube(hook, [0.01 * (1 - j / 10.5) for j in range(10)], sides=6)
    root.build()
