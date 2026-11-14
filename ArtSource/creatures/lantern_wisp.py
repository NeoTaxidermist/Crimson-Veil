# Lantern wisp — a wrought-iron lantern with a crimson flame (the "Flame" glows), wisps curling below.
# The light itself is added by the game.
import math
from mathutils import Vector
import creature_kit as K

L = K.Part("Lantern")
sides = 6
# Base: a stepped plate with little feet.
L.lathe([(0.0, -0.24), (0.13, -0.24), (0.15, -0.22), (0.15, -0.2), (0.12, -0.19), (0.12, -0.17), (0.0, -0.17)], sides=sides, sx=1, sz=1)
for k in range(sides):
    a = 2 * math.pi * k / sides
    L.tube([(math.cos(a) * 0.13, -0.24, math.sin(a) * 0.13), (math.cos(a) * 0.16, -0.29, math.sin(a) * 0.16)], [0.012, 0.006], sides=5)
# Cage posts, slightly bulging, with crimson-rimmed edges so the ironwork reads against dark backgrounds.
for k in range(sides):
    a = 2 * math.pi * k / sides
    pts = [(math.cos(a) * r, y, math.sin(a) * r) for r, y in [(0.115, -0.17), (0.135, -0.05), (0.135, 0.08), (0.115, 0.18)]]
    L.tube(pts, 0.012, sides=6)
    L.tube([Vector(p) * 1.0 + Vector((0, 0, -0.004)) for p in pts], 0.005, sides=4, mat=K.CRIMSON)
# Decorative scrolls between the posts (curls of iron).
for k in range(sides):
    a = 2 * math.pi * (k + 0.5) / sides
    c = Vector((math.cos(a) * 0.13, 0.0, math.sin(a) * 0.13))
    t_dir = Vector((-math.sin(a), 0, math.cos(a)))
    curl = [c + t_dir * (0.03 * math.cos(s) * (1 - s / 9)) + Vector((0, 0.03 * math.sin(s) * (1 - s / 9) + 0.06, 0)) for s in [j * 0.6 for j in range(12)]]
    L.tube(curl, 0.005, sides=4)
# Roof: a pointed hexagonal dome with a finial and a hanging ring.
L.lathe([(0.0, 0.18), (0.16, 0.18), (0.17, 0.2), (0.12, 0.25), (0.07, 0.31), (0.03, 0.36), (0.0, 0.38)], sides=sides)
L.blob((0, 0.4, 0), (0.025, 0.03, 0.025), segs=8, rings=6)
ring = [(math.cos(2 * math.pi * k / 16) * 0.035, 0.46 + math.sin(2 * math.pi * k / 16) * 0.04, 0) for k in range(17)]
L.tube(ring, 0.008, sides=5, cap=False)
# Rivets along the roof's rim, a stub of broken chain on the hanging ring, a crimson rune plate on the door.
for k in range(12):
    a = 2 * math.pi * k / 12
    L.blob((math.cos(a) * 0.165, 0.19, math.sin(a) * 0.165), (0.008, 0.008, 0.008), segs=6, rings=4)
y = 0.5
for k in range(2):
    link = [(math.cos(2 * math.pi * j / 10) * 0.012 if k % 2 == 0 else 0, y + 0.03 + math.sin(2 * math.pi * j / 10) * 0.03,
             0 if k % 2 == 0 else math.cos(2 * math.pi * j / 10) * 0.012) for j in range(11)]
    L.tube(link, 0.005, sides=4, cap=False)
    y += 0.05
L.prism([(-0.04, -0.12), (0.04, -0.12), (0.04, -0.07), (0.0, -0.05), (-0.04, -0.07)], -0.14, -0.13, mat=K.CRIMSON)
L.tube([(0.0, -0.115, -0.142), (0.0, -0.065, -0.142)], 0.004, sides=3)                       # the rune: a stroke
L.tube([(-0.025, -0.095, -0.142), (0.025, -0.085, -0.142)], 0.004, sides=3)                  # and a cross-stroke
# Wax drips frozen down the posts.
for k in range(3):
    a = 2 * math.pi * k / 3 + 0.4
    L.tube([(math.cos(a) * 0.12, -0.16, math.sin(a) * 0.12), (math.cos(a) * 0.125, -0.21 - 0.02 * k, math.sin(a) * 0.125)], [0.01, 0.004], sides=4)
L.build()

F = K.Part("Flame")
# A teardrop flame with a flickering tongue, and an inner core.
F.lathe([(0.0, -0.13), (0.05, -0.1), (0.07, -0.04), (0.06, 0.03), (0.04, 0.08), (0.02, 0.12), (0.0, 0.16)], sides=14, mat=K.CRIMSON,
        rfunc=lambda th, i, r, y: 1 + 0.12 * math.sin(3 * th + i))
F.build()

S = K.Part("Wisps")
# Smoke wisps curling from the bottom of the lantern: the spirit trailing behind it.
for k in range(3):
    pts = []
    for j in range(14):
        t = j / 13
        ang = t * 4.5 + k * 2.1
        pts.append((math.cos(ang) * 0.05 * t - 0.15 * t * (k - 1), -0.3 - 0.35 * t, math.sin(ang) * 0.04))
    S.tube(pts, [0.02 * (1 - t) + 0.002 for t in [j / 13 for j in range(14)]], sides=5, mat=K.CRIMSON)
S.build()
