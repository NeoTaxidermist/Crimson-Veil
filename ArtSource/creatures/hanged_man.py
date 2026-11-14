# Hanged man — built standing, feet at the origin, neck at y = 1.7 (the game hangs him from there).
import math
from mathutils import Vector
import creature_kit as K

b = K.Part("Body")


def rag(th, i, y):
    if i > 0:
        return 0.0
    return -0.12 * max(0.0, math.sin(6 * th + 0.5)) ** 2 - 0.05 * abs(math.sin(17 * th))


# Torso in a torn shirt (ragged hem, slack folds), narrow and starved.
b.lathe([(0.19, 0.92), (0.16, 1.0), (0.13, 1.15), (0.14, 1.35), (0.165, 1.5), (0.16, 1.58), (0.09, 1.64), (0.05, 1.68)],
        sides=28, sz=0.65, yfunc=rag, rfunc=lambda th, i, r, y: 1 + 0.06 * math.sin(5 * th + y * 9), cap_bottom=False)
# Pelvis and ragged trousers.
b.lathe([(0.16, 0.86), (0.17, 0.95), (0.15, 1.02)], sides=20, sz=0.7)
for side, z in ((-1, -0.07), (1, 0.07)):
    # Legs hang apart a little, feet point down (toes first: the weight of a dead man).
    knee = (0.06 * side, 0.5, z)
    leg = [(0.0, 0.9, z), (0.03 * side, 0.7, z), knee, (0.05 * side, 0.28, z), (0.03 * side, 0.1, z)]
    b.tube(leg, [0.085, 0.075, 0.065, 0.055, 0.04], sides=10)
    # Torn trouser cuff.
    sx = 0.04 * side
    cuff = [(0.055 * side, 0.42 - 0.02 * side, z), (0.055 * side, 0.32 - 0.03 * side, z)]
    b.tube(cuff, [0.075, 0.07], sides=8, jitter=0.6, seed=3 + side)
    b.tube([(sx - 0.01, 0.1, z), (sx + 0.04, 0.03, z), (sx + 0.09, -0.02, z)], [0.04, 0.035, 0.02], sides=8)  # foot, toes down
    for t in range(3):
        b.tube([(sx + 0.08, -0.01, z - 0.02 + t * 0.02), (sx + 0.11, -0.05, z - 0.02 + t * 0.02)], [0.01, 0.006], sides=5)
# Arms hang limp — one a little in front, one behind, so both read in the silhouette. Long fingers.
for z, sx in ((-0.2, 1), (0.2, -1)):
    arm = [(0.05 * sx, 1.56, z), (0.17 * sx, 1.3, z * 1.05), (0.21 * sx, 1.05, z * 1.08), (0.22 * sx, 0.9, z * 1.08)]
    b.tube(arm, [0.058, 0.048, 0.043, 0.038], sides=10)
    b.tube([(0.05 * sx, 1.57, z), (0.12 * sx, 1.4, z * 1.03)], [0.075, 0.068], sides=10, jitter=0.5, seed=7)  # torn sleeve
    hand = Vector((0.225 * sx, 0.85, z * 1.08))
    b.blob(hand, (0.035, 0.05, 0.03), segs=8, rings=6)
    for f in range(4):
        b.tube([hand + Vector((0.012 * f - 0.018, -0.03, 0)), hand + Vector((0.012 * f - 0.02 + 0.01 * sx, -0.13 + 0.012 * abs(f - 1.5), 0))],
               [0.009, 0.004], sides=5)

# Head: chin sunk on the chest, turned a little away; lank hair hanging straight down.
neck = Vector((0.03, 1.68, 0.0))
start = len(b.bm.verts)
b.blob((0.03, 1.82, -0.01), (0.115, 0.135, 0.105), segs=18, rings=12, bumps=0.06, seed=2)
b.blob((0.12, 1.77, -0.01), (0.04, 0.045, 0.03), segs=8, rings=6)  # nose / brow ridge
b.blob((0.09, 1.71, -0.01), (0.06, 0.035, 0.06), segs=10, rings=6)  # slack jaw
head_verts = list(b.bm.verts)[start:]
tilt = math.radians(-38)  # forward = clockwise in the picture plane
for v in head_verts:
    g = Vector((-v.co.x, v.co.z, -v.co.y)) - neck
    g = Vector((g.x * math.cos(tilt) - g.y * math.sin(tilt), g.x * math.sin(tilt) + g.y * math.cos(tilt), g.z)) + neck
    v.co = K.G(g.x, g.y, g.z)
b.tube([(0.0, 1.62, 0), (0.04, 1.7, -0.005)], [0.045, 0.048], sides=8)  # neck, stretched
for s in range(9):
    a = -1.6 + s * 0.4
    root = Vector((0.0 + math.cos(a) * 0.05, 1.86 - 0.02 * abs(s - 4), math.sin(a) * 0.09))
    length = 0.24 + 0.05 * ((s * 7) % 3)
    b.tube([root, root + Vector((0.03, -length * 0.5, 0)), root + Vector((0.035, -length, 0))], [0.012, 0.007, 0.002], sides=4)

# Crimson noose biting into the neck; the hangman's knot under the ear; the rope going up.
ring = []
for k in range(25):
    a = 2 * math.pi * k / 24
    ring.append((0.035 + math.cos(a) * 0.07, 1.67 + 0.035 * math.sin(a), math.sin(a) * 0.065))
b.tube(ring, 0.022, sides=7, mat=K.CRIMSON, cap=False)
knot = Vector((-0.05, 1.7, 0.05))
for k in range(6):  # neat coils around the standing part
    coil = [knot + Vector((math.cos(2 * math.pi * j / 12) * 0.032, 0.028 * k, math.sin(2 * math.pi * j / 12) * 0.032)) for j in range(13)]
    b.tube(coil, 0.012, sides=5, mat=K.CRIMSON, cap=False)
b.tube([knot, knot + Vector((0.0, 0.42, -0.01))], 0.018, sides=6, mat=K.CRIMSON)
# Rags of the shirt hanging from its torn hem, swaying with him.
for s, (x, l, z) in enumerate([(-0.1, 0.22, -0.08), (0.02, 0.3, -0.11), (0.12, 0.18, -0.06), (-0.02, 0.16, 0.1)]):
    pts = [(x + 0.02 * math.sin(t * 5 + s), 0.95 - l * t, z) for t in [j / 6 for j in range(7)]]
    b.tube(pts, [0.03 * (1 - 0.7 * j / 6) + 0.004 for j in range(7)], sides=5, flat=0.3, seed=s)
# Buttons down the shirt front (most torn off: three left).
for y in (1.45, 1.32, 1.2):
    b.blob((0.02, y, -0.105), (0.014, 0.014, 0.008), segs=8, rings=5)
# An iron shackle on one ankle, its broken chain dangling.
ank = Vector((-0.06, 0.18, -0.07))
cuff = [ank + Vector((math.cos(2 * math.pi * k / 16) * 0.06, 0.012 * math.sin(4 * math.pi * k / 16), math.sin(2 * math.pi * k / 16) * 0.05)) for k in range(17)]
b.tube(cuff, 0.014, sides=5, cap=False)
y = 0.12
for k in range(4):
    Ln, W = 0.04, 0.02
    link = [(ank.x - 0.05 + (math.cos(2 * math.pi * j / 10) * W if k % 2 == 0 else 0), y - Ln + math.sin(2 * math.pi * j / 10) * Ln,
             ank.z - 0.02 + (0 if k % 2 == 0 else math.cos(2 * math.pi * j / 10) * W)) for j in range(11)]
    b.tube(link, 0.006, sides=4, cap=False)
    y -= Ln * 1.5
# A rope belt cinching the trousers, knotted at the hip.
belt = [(math.cos(2 * math.pi * k / 20) * 0.165, 0.97, math.sin(2 * math.pi * k / 20) * 0.165 * 0.7) for k in range(21)]
b.tube(belt, 0.012, sides=5, cap=False)
b.blob((0.12, 0.96, -0.1), (0.025, 0.022, 0.02), segs=8, rings=5, bumps=0.3, seed=9)
b.displace(0.008, scale=8.0, seed=4)
b.build()
