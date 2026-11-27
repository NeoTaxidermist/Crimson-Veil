# Root crawler — a knot of black roots with a cluster of crimson eyes (rebuilt to the hero's level, v0.103).
# Each "Root{i}" is its own object pointing up (+y) from the centre: the game rotates it to i*40° and wiggles it.
# The knot is a gnarled burl of bark: thick strands wound round a core ridged with bark furrows, a split maw of
# layered thorn teeth over a dark throat that weeps crimson sap, six eyes sunk in knotholes under bark lids, bracket
# fungus, moss threads hanging. Each root is three strands twisted into a rope, swelling at knuckles, furrowed, thorned,
# with rootlet hairs, and ends in a hand of three hooked claws.
import math
import random
from mathutils import Vector, noise
import creature_kit as K

rnd = random.Random(4)
core = K.Part("Knot")
# the burl itself: lumpy, furrowed bark running round it
core.blob((0, 0, 0), (0.205, 0.175, 0.175), segs=40, rings=26, bumps=0.32, seed=7)
# Strands wound round the core like a clenched fist of roots (rounder, more of them, swelling as they cross).
for i in range(24):
    tilt = rnd.uniform(-1.25, 1.25)
    phase = rnd.uniform(0, 2 * math.pi)
    ax = Vector((math.cos(tilt), math.sin(tilt), rnd.uniform(-0.4, 0.4))).normalized()
    u = ax.orthogonal().normalized()
    v = ax.cross(u)
    pts, rr = [], []
    for k in range(18):
        a = phase + k * 0.25
        r = 0.18 + 0.016 * math.sin(k * 1.7 + i)
        p = u * math.cos(a) * r + v * math.sin(a) * r + ax * (0.04 * math.sin(k * 0.6 + i))
        pts.append(Vector((p.x * 1.15, p.y, p.z * 0.95)))
        swell = 1.0 + 0.25 * math.exp(-((k - 6 - (i % 5)) / 1.6) ** 2)
        rr.append((0.03 * math.sin(math.pi * (k + 0.5) / 18) + 0.006) * swell)
    core.tube(pts, rr, sides=10, jitter=0.18, seed=i)
# The maw: split bark gaping low on the front, a dark throat, two rows of hooked thorn teeth, sap running from it.
mc = Vector((0.15, -0.07, -0.1))
core.blob(mc + Vector((-0.02, 0, 0)), (0.06, 0.05, 0.045), segs=18, rings=12)
for row, (rr0, n_t, L) in enumerate(((0.075, 14, 0.05), (0.055, 10, 0.035))):
    for k in range(n_t):
        a = 2 * math.pi * (k + 0.5 * row) / n_t
        base = mc + Vector((0.0, math.sin(a) * rr0 * 0.85, math.cos(a) * rr0)) + Vector((0.03 - 0.012 * row, 0, -0.03))
        inward = (mc - base).normalized()
        tip = base + inward * L + Vector((0.03, 0, -0.02))
        core.tube([base, base.lerp(tip, 0.55) + Vector((0.01, 0, 0)), tip], [0.012 - 0.003 * row, 0.006, 0.0008], sides=7)
# the bark's lips round the split, thick and rough
lip = [mc + Vector((0.035, math.sin(2 * math.pi * j / 24) * 0.085, math.cos(2 * math.pi * j / 24) * 0.095)) for j in range(25)]
core.tube(lip, [0.016 + 0.006 * abs(math.sin(j * 1.3)) for j in range(25)], sides=8, jitter=0.2, seed=91, cap=False)
# crimson sap weeping from the maw down the bark
for s, (dz, ln) in enumerate(((-0.03, 0.12), (0.025, 0.08), (0.0, 0.16))):
    st = mc + Vector((0.05, -0.08, dz))
    pts = [st + Vector((0.01 * math.sin(j), -ln * j / 6, 0.004 * math.sin(j * 2 + s))) for j in range(7)]
    core.tube(pts, [0.009, 0.008, 0.007, 0.006, 0.006, 0.007, 0.0015], sides=8, mat=K.CRIMSON)
    core.blob(pts[-2], (0.011, 0.014, 0.011), segs=10, rings=8, mat=K.CRIMSON)
# Eyes: six, sunk in knotholes under bark lids, the front pair biggest.
for (x, y, z, r) in [(0.13, 0.06, -0.15, 0.04), (0.18, 0.02, -0.11, 0.034), (0.08, 0.12, -0.13, 0.022),
                     (0.2, 0.08, -0.06, 0.02), (0.05, 0.03, -0.18, 0.018), (0.15, 0.13, -0.08, 0.015)]:
    c = Vector((x, y, z))
    n = c.normalized()
    t1 = n.orthogonal().normalized()
    t2 = n.cross(t1)
    # the knothole: a ring of bark swelling round it, thickest above (a heavy lid)
    lid = [c + (t1 * math.cos(2 * math.pi * j / 24) + t2 * math.sin(2 * math.pi * j / 24)) * r * 1.2 + n * r * 0.15 for j in range(25)]
    core.tube(lid, [r * (0.32 + 0.18 * max(0.0, math.sin(2 * math.pi * j / 24))) for j in range(25)], sides=8, cap=False)
    core.blob(c, (r, r, r * 0.78), segs=18, rings=12, mat=K.CRIMSON)
    core.blob(c + n * r * 0.62, (r * 0.18, r * 0.42, r * 0.12), segs=10, rings=6)     # a slit of pupil
# Thorns (curved, rooted in bark), pale bracket fungus in shelves, moss threads hanging, rootlet hairs.
for i in range(30):
    a = rnd.uniform(0, 2 * math.pi)
    b2 = rnd.uniform(-1.0, 1.0)
    d = Vector((math.cos(a) * math.cos(b2), math.sin(b2), math.sin(a) * math.cos(b2) * 0.8))
    base = Vector((d.x * 0.22, d.y * 0.19, d.z * 0.19))
    bend = d.orthogonal().normalized() * 0.02
    L = rnd.uniform(0.06, 0.14)
    core.tube([base - d * 0.01, base + d * L * 0.5 + bend, base + d * L + bend * 2.2], [0.018, 0.008, 0.0008], sides=7)
for j, (x, y, z) in enumerate([(-0.15, 0.12, -0.1), (-0.2, 0.0, -0.06), (-0.08, 0.17, -0.08), (-0.17, 0.07, -0.12), (-0.12, -0.05, -0.15)]):
    for tier in range(2):
        core.lathe([(0.0, 0.0), (0.06 - 0.015 * tier, 0.0), (0.056 - 0.015 * tier, 0.01), (0.034, 0.018), (0.0, 0.021)], sides=20,
                   center=(x, y - 0.03 * tier, z), sz=0.6,
                   rfunc=lambda th, i, r, y, j=j: 1 + 0.1 * math.sin(th * 5 + j) + 0.04 * math.sin(th * 13))
for i in range(14):   # moss threads hanging from under the burl
    a = rnd.uniform(0, 2 * math.pi)
    base = Vector((math.cos(a) * 0.15, -0.12, math.sin(a) * 0.13))
    L = rnd.uniform(0.08, 0.16)
    pts = [base + Vector((0.01 * math.sin(j * 1.7 + i), -L * j / 4, 0.008 * math.cos(j + i))) for j in range(5)]
    core.tube(pts, [0.006, 0.005, 0.004, 0.003, 0.0006], sides=5)
for i in range(30):
    a = rnd.uniform(0, 2 * math.pi)
    d = Vector((math.cos(a), -abs(math.sin(a)) - 0.3, rnd.uniform(-0.6, 0.6))).normalized()
    base = Vector((d.x * 0.18, d.y * 0.16, d.z * 0.16))
    tip = base + d * rnd.uniform(0.05, 0.1) + Vector((rnd.uniform(-0.03, 0.03), -0.03, 0))
    core.tube([base, base.lerp(tip, 0.5) + Vector((0.01, 0, 0)), tip], [0.006, 0.0035, 0.0005], sides=5)
core.displace(0.006, scale=9.0, seed=3)
core.displace(0.0025, scale=28.0, seed=11)     # the bark's grain
core.build(smooth=1, thin=0.25)

lengths = [0.55, 0.72, 0.5, 0.8, 0.62, 0.7, 0.48, 0.75, 0.6]
for i, L in enumerate(lengths):
    root = K.Part("Root%d" % i)
    sgn = 1 if i % 2 else -1
    N = 22
    axis = []
    for k in range(N):
        t = k / (N - 1)
        bend = 0.12 * math.sin(t * 3.0 + i) + 0.1 * t * t * sgn
        axis.append(Vector((bend, 0.1 + L * t, 0.03 * math.sin(t * 5 + i))))
    knuckles = (0.3, 0.62)
    # Three strands twisted into a rope; they part near the base (where the root tears out of the knot) and bunch at
    # the knuckles; furrows run along each strand.
    for s in range(3):
        pts, rr = [], []
        for k in range(N):
            t = k / (N - 1)
            a = 2 * math.pi * s / 3 + t * 7.0 + i
            spread = 0.026 * (1 - 0.8 * t) + 0.02 * max(0.0, 0.2 - t) * 5
            kn = sum(math.exp(-((t - q) / 0.035) ** 2) for q in knuckles)
            pts.append(axis[k] + Vector((math.cos(a) * spread * (1 + 0.5 * kn), 0, math.sin(a) * spread * (1 + 0.5 * kn))))
            rr.append((0.028 * (1 - 0.85 * t) + 0.003) * (1 + 0.35 * kn))
        root.tube(pts, rr, sides=10, jitter=0.22, seed=i * 3 + s)
    # Side twigs, rootlet hairs, thorns.
    for s in range(3):
        k0 = 4 + s * 5
        base = axis[k0]
        dirx = sgn if s % 2 else -sgn
        # (no longer than this: swung to the edge of its fan a root's twig must stay out of the stone)
        twig = [base + Vector((dirx * (0.016 * j + 0.004 * j * j), 0.03 * j, 0.006 * j)) for j in range(5)]
        root.tube(twig, [0.016, 0.012, 0.008, 0.004, 0.001], sides=7, jitter=0.15, seed=50 + s)
    r2 = random.Random(100 + i)
    for h in range(18):
        k0 = r2.randint(1, N - 4)
        base = axis[k0]
        d = Vector((r2.uniform(-1, 1), r2.uniform(-0.6, 0.2), r2.uniform(-1, 1))).normalized()
        if h % 2:
            root.tube([base + d * 0.018, base + d * 0.04 + Vector((0, 0.008, 0)), base + d * 0.065 + Vector((0, 0.022, 0))], [0.009, 0.005, 0.0006], sides=6)   # thorn
        else:
            tip = base + d * 0.07 + Vector((0, -0.03, 0))
            root.tube([base + d * 0.015, base.lerp(tip, 0.5) + d * 0.01, tip], [0.003, 0.002, 0.0004], sides=4)  # hair
    # The tip: a hand of three hooked claws spread from a gnarled knuckle.
    tip = axis[-1]
    dirn = (axis[-1] - axis[-3]).normalized()
    root.blob(tip, (0.018, 0.022, 0.018), segs=14, rings=10, bumps=0.3, seed=i)
    side = dirn.cross(Vector((0, 0, 1))).normalized()
    for c in range(3):
        sp = (c - 1) * 0.3   # (kept close: a root swung to the edge of its fan must not reach into the stone)
        dd = (dirn + side * sp + Vector((0, 0, 0.25 * (c - 1)))).normalized()
        claw = []
        for j in range(8):
            t = j / 7
            hook = side * (-sgn) * 0.014 * t * t
            claw.append(tip + dd * (0.01 + 0.06 * t) + hook + Vector((0, -0.012 * t * t, 0)))
        root.tube(claw, [0.008 * (1 - j / 8.0) + 0.0006 for j in range(8)], sides=7)
    root.displace(0.003, scale=22.0, seed=20 + i)
    root.build(smooth=1, thin=0.4)
