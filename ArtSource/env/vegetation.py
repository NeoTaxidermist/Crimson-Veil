# Bramble, dry weeds, a hanging chain and a pile of crimson leaves — small props that give the key art its density.
# Each object is exported as its own FBX by build_env.py (VEG_PART selects which).
import math
import random
from mathutils import Vector
import creature_kit as K

PART = globals().get("VEG_PART", "Bramble")
rnd = random.Random(sum(map(ord, PART)))  # deterministic per prop

if PART == "Bramble":
    b = K.Part("Bramble")
    # Arching thorny canes rooted at the origin, spreading 2.4 m wide, 1.6 m tall.
    for k in range(11):
        ang = math.radians(rnd.uniform(25, 155))
        L = rnd.uniform(0.9, 2.0)
        bend = rnd.uniform(0.5, 1.2) * (1 if math.cos(ang) > 0 else -1)
        pts = []
        for j in range(12):
            t = j / 11
            x = math.cos(ang) * L * t + bend * 0.5 * t * t
            y = max(0.03, math.sin(ang) * L * 1.3 * t * (1 - 0.92 * t * t))  # arch back down to the ground
            pts.append(Vector((x, y, rnd.uniform(-0.05, 0.05))))
        b.tube(pts, [0.035 * (1 - 0.85 * j / 11) + 0.004 for j in range(12)], sides=5)
        for j in range(2, 11):  # thorns
            p = pts[j]
            tdir = (pts[j] - pts[j - 1]).normalized()
            n = Vector((-tdir.y, tdir.x, 0)) * (1 if j % 2 else -1)
            b.tube([p, p + n * 0.07 + tdir * 0.03], [0.012, 0.001], sides=3)
        if rnd.random() < 0.5:  # a curled dead leaf
            p = pts[7]
            b.prism([(0, 0), (0.09, 0.04), (0.13, -0.02), (0.05, -0.05)], p.z - 0.01, p.z + 0.01,
                    xform=lambda q, p=p: Vector((q.x + p.x, q.y + p.y, q.z)))
    b.build()

elif PART == "Weeds":
    w = K.Part("Weeds")
    # Dry stalks with seed heads and drooping blades, 1.8 m wide.
    for k in range(16):
        x = rnd.uniform(-0.9, 0.9)
        h = rnd.uniform(0.6, 1.6)
        lean = rnd.uniform(-0.25, 0.25)
        pts = [Vector((x + lean * t * t, h * t, rnd.uniform(-0.1, 0.1) * t)) for t in [j / 8 for j in range(9)]]
        w.tube(pts, [0.016 * (1 - 0.6 * j / 8) for j in range(9)], sides=4)
        top = pts[-1]
        kind = k % 3
        if kind == 0:  # umbel: spokes with seed dots
            for s in range(7):
                a = math.radians(30 + s * 20)
                tip = top + Vector((math.cos(a) * 0.14, math.sin(a) * 0.1 + 0.02, 0))
                w.tube([top, tip], 0.004, sides=3)
                w.blob(tip, (0.012, 0.012, 0.012), segs=6, rings=4)
        elif kind == 1:  # seed spike
            w.blob(top + Vector((0, 0.08, 0)), (0.025, 0.11, 0.025), segs=8, rings=6, bumps=0.3, seed=k)
        else:  # drooping blade
            blade = [top, top + Vector((0.12 * (1 if lean > 0 else -1), 0.05, 0)), top + Vector((0.25 * (1 if lean > 0 else -1), -0.12, 0))]
            w.tube(blade, [0.012, 0.01, 0.002], sides=3, flat=0.3)
    w.build()

elif PART == "Chain":
    c = K.Part("Chain")
    # 2.4 m of chain hanging from the origin, links alternating in orientation, a hook at the end.
    y = 0.0
    for k in range(18):
        L, Wd = 0.12, 0.06
        ring = []
        for j in range(17):
            a = 2 * math.pi * j / 16
            px, py = math.cos(a) * Wd, math.sin(a) * L
            ring.append(Vector((px, y - L + py, 0)) if k % 2 == 0 else Vector((0, y - L + py, px)))
        c.tube(ring, 0.014, sides=5, cap=False)
        y -= L * 1.6
    hook = [Vector((0, y - 0.05, 0)) + Vector((0.12 * math.sin(t), -0.12 * (1 - math.cos(t)) - 0.08, 0)) for t in [j * 0.45 for j in range(9)]]
    c.tube(hook, [0.025, 0.024, 0.022, 0.02, 0.018, 0.015, 0.012, 0.008, 0.003], sides=6)
    c.build()

elif PART == "LeafPile":
    p = K.Part("LeafPile")
    # A drift of fallen maple leaves, 3.5 m wide, thickest in the middle.
    for k in range(140):
        x = rnd.gauss(0, 0.9)
        if abs(x) > 1.75:
            continue
        y = max(0.02, 0.32 * math.exp(-x * x * 1.4) + rnd.uniform(-0.03, 0.06))
        z = rnd.uniform(-0.6, 0.6)
        s = rnd.uniform(0.06, 0.12)
        rot = rnd.uniform(0, 2 * math.pi)
        tilt = rnd.uniform(0.35, 1.3)  # leaves propped up so the side-on camera sees their outline
        leaf = []
        for j in range(10):  # a five-lobed maple outline
            a = 2 * math.pi * j / 10
            rr = s * (1.0 if j % 2 == 0 else 0.45)
            leaf.append((math.cos(a + rot) * rr, math.sin(a + rot) * rr))
        p.prism(leaf, -0.004, 0.004, mat=K.CRIMSON,
                xform=lambda q, x=x, y=y, z=z, tilt=tilt: Vector((q.x + x, q.y * math.sin(tilt) + y, q.y * math.cos(tilt) + z + q.z)))
    p.build()
