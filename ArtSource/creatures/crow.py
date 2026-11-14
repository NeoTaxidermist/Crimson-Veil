# Crow — facing +x. Wings "WingL"/"WingR" spread in the picture plane, pointing up from the shoulder;
# the game flaps them by rotating about x (0° = up, ~150° = down past the body).
import math
from mathutils import Vector
import creature_kit as K

b = K.Part("Body")
# Body: a teardrop along x (chest forward, tapering to the tail).
body_pts = [(-0.2 + 0.4 * t, 0.01 * math.sin(t * 3), 0) for t in [k / 10 for k in range(11)]]
b.tube(body_pts, [0.03, 0.05, 0.07, 0.085, 0.095, 0.1, 0.098, 0.09, 0.08, 0.065, 0.045], sides=12, flat=0.85)
# Head and a heavy beak.
b.blob((0.25, 0.05, 0), (0.07, 0.065, 0.06), segs=14, rings=10)
b.tube([(0.3, 0.05, 0), (0.37, 0.035, 0), (0.42, 0.015, 0)], [0.03, 0.017, 0.002], sides=8, flat=0.8)
b.blob((0.275, 0.075, -0.05), (0.014, 0.014, 0.01), segs=8, rings=5, mat=K.CRIMSON)  # eye (camera side)
# Shaggy throat: a few hackles just breaking the outline under the beak.
for k in range(3):
    x = 0.2 + 0.025 * k
    b.prism([(0, 0), (0.025, -0.01), (0.005, -0.035), (-0.015, -0.01)], -0.025, 0.025, xform=lambda p, x=x: p + Vector((x, -0.015, 0)))
# Tail fan: six long feathers.
for k in range(6):
    a = math.radians(-12 + k * 5)
    L = 0.24 + 0.03 * (k % 2)
    out = [(0, 0.012), (L * 0.9, 0.022), (L, 0.0), (L * 0.95, -0.012), (L * 0.85, 0.0), (0, -0.012)]
    def xf(p, a=a):
        x, y = -p.x, p.y
        return Vector((x * math.cos(a) - y * math.sin(a) - 0.17, x * math.sin(a) + y * math.cos(a) + 0.01, p.z))
    b.prism(out, -0.01 + k * 0.004, 0.0 + k * 0.004, xform=xf)
# Legs tucked, talons.
for z in (-0.03, 0.03):
    b.tube([(0.02, -0.06, z), (0.0, -0.13, z), (0.04, -0.15, z)], [0.01, 0.008, 0.005], sides=5)
    for t in range(3):
        b.tube([(0.04, -0.15, z), (0.07 - 0.02 * t, -0.18, z + (t - 1) * 0.01)], [0.004, 0.002], sides=4)
# Feather scales layered along the back and flanks (they break the smooth body into plumage).
for k in range(12):
    x = 0.17 - 0.03 * k
    yb = 0.06 + 0.015 * math.sin(k)
    sc = [(0.0, 0.0), (-0.045, 0.012), (-0.06, -0.008), (-0.03, -0.022)]
    b.prism(sc, -0.07 + 0.012 * (k % 3), -0.064 + 0.012 * (k % 3), xform=lambda p, x=x, yb=yb: p + Vector((x, yb - 0.03 * (k % 2), 0)))
# Ruffled nape feathers sticking out behind the head.
for k in range(4):
    b.tube([(0.22 - 0.02 * k, 0.09, -0.02 + 0.013 * k), (0.17 - 0.03 * k, 0.12 + 0.01 * (k % 2), -0.02 + 0.013 * k)], [0.008, 0.001], sides=3)
b.displace(0.004, scale=12.0, seed=1)
b.build()


def wing(name, z):
    w = K.Part(name, pivot=(0.0, 0.04, z))
    # Wing in the picture plane, pointing up (+y) from the shoulder, trailing back (-x).
    out = [(0.04, 0.0), (0.06, 0.12), (0.04, 0.24), (0.0, 0.34)]
    # Primaries: separate feather fingers at the tip.
    for k in range(6):
        base_y = 0.34 - k * 0.035
        tip = (-0.05 - k * 0.055, 0.46 - k * 0.04)
        out += [(tip[0] + 0.02, tip[1] - 0.015), tip, (tip[0] - 0.012, tip[1] - 0.04), (-0.03 - k * 0.055, base_y - 0.03)]
    out += [(-0.33, 0.12), (-0.3, 0.05), (-0.22, 0.03), (-0.12, -0.01)]  # secondaries, trailing edge
    w.prism(out, -0.008, 0.008)
    # Coverts: a ridge of small feathers along the leading edge.
    w.tube([(0.03, 0.01, 0), (0.03, 0.15, 0), (0.0, 0.3, 0), (-0.05, 0.36, 0)], [0.025, 0.022, 0.015, 0.008], sides=6, flat=0.6)
    # Rows of covert feathers over the wing, each a little proud of the one below.
    for row in range(2):
        for k in range(5):
            bx, by = -0.02 - k * 0.055 - row * 0.03, 0.26 - k * 0.035 - row * 0.09
            fe = [(0.0, 0.0), (0.03, 0.04), (-0.05, 0.02), (-0.07, -0.03), (-0.02, -0.025)]
            w.prism(fe, -0.012 - 0.003 * row, -0.009 - 0.003 * row, xform=lambda p, bx=bx, by=by: p + Vector((bx, by, 0)))
    # Shafts of the long flight feathers.
    for k in range(6):
        tip = (-0.05 - k * 0.055, 0.46 - k * 0.04)
        w.tube([(-0.03 - k * 0.04, 0.3 - k * 0.04, -0.01), (tip[0], tip[1] - 0.02, -0.01)], 0.003, sides=3)
    w.build()


wing("WingL", -0.03)
wing("WingR", 0.03)
