# Shared sculpting helpers for the bestiary build scripts (the wraith's method, see wraith_build.py / monk_build.py):
# lofts, ring lofts with openings, blobs with displacement, tube loops, folds, the skeleton list and the finishing
# pipeline (solidify, subdivide once, decimate). Exec it at the top of a build script:
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\sculpt_kit.py", encoding="utf-8").read())
# Blender space: Z up, the creature faces -Y, its left is +X.
import math
import bpy
from mathutils import Vector, noise

ROOT = r"C:\Dev\CrimsonVeil"
DECIMATE_RATIO = globals().get("DECIMATE_RATIO", 0.4)
bpy.ops.wm.read_homefile(use_empty=True)
INK, EYES = 0, 1
verts, faces, fmat, vweights = [], [], [], []


def V(x, y, z):
    return Vector((x, y, z))


def add_vert(p, w):
    verts.append(Vector(p))
    vweights.append(w)
    return len(verts) - 1


def frame(t):
    ref = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 0, 1))
    u = (ref - t * ref.dot(t)).normalized()
    return u, t.cross(u).normalized()


def loft(sections, weight, mat=INK, n=16, cap0=True, cap1=True, disp=None):
    """sections: [(centre, rx, ry)]; weight(p, s) -> {bone: w}; disp(i, k, p, c) -> offset of ring point k of section i."""
    rings, m = [], len(sections)
    for i, (c, rx, ry) in enumerate(sections):
        c = Vector(c)
        a, b = Vector(sections[max(i - 1, 0)][0]), Vector(sections[min(i + 1, m - 1)][0])
        t = (b - a).normalized()
        u, v = frame(t)
        ring = []
        for k in range(n):
            th = 2 * math.pi * k / n
            p = c + u * (rx * math.cos(th)) + v * (ry * math.sin(th))
            if disp is not None:
                p = p + disp(i, k, p, c)
            ring.append(add_vert(p, weight(p, i / max(m - 1, 1))))
        rings.append(ring)
    for i in range(m - 1):
        for k in range(n):
            faces.append((rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k])); fmat.append(mat)
    for ring, flip, cap in ((rings[0], True, cap0), (rings[-1], False, cap1)):
        if not cap:
            continue
        cen = sum((verts[j] for j in ring), Vector()) / n
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[(k + 1) % n], ring[k]) if flip else (ci, ring[k], ring[(k + 1) % n])); fmat.append(mat)
    return rings


def ring_loft(rows, weight, mat=INK, n=48, disp=None, arc=None, closed_top=False, cap_end=False, keep=None):
    """Horizontal rings round a leaning axis: rows [(z, rx, ry, cy)]. th: 0 = left (+x), pi/2 = the very front (-y).
    disp(z, th, p, out) -> offset. keep(z, th) -> False drops the face there (an opening)."""
    rings, angles = [], []
    span = arc if arc else (0.0, 2 * math.pi)
    cnt = n if not arc else n + 1
    for (z, rx, ry, cy) in rows:
        ring = []
        for k in range(cnt):
            th = span[0] + (span[1] - span[0]) * k / n
            if len(rings) == 0:
                angles.append(th)
            out = V(math.cos(th) * rx, -math.sin(th) * ry, 0)
            p = V(0, cy, z) + out
            o = V(math.cos(th) / max(rx, 1e-4), -math.sin(th) / max(ry, 1e-4), 0).normalized()
            if disp is not None:
                p = p + disp(z, th, p, o)
            ring.append(add_vert(p, weight(p, 0)))
        rings.append(ring)
    for i in range(len(rows) - 1):
        for k in range(n):
            k1 = (k + 1) % cnt if not arc else k + 1
            if keep is not None and not keep((rows[i][0] + rows[i + 1][0]) / 2, (angles[k] + angles[k1 % len(angles)]) / 2 if k1 < len(angles) else angles[k]):
                continue
            faces.append((rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k])); fmat.append(mat)
    if closed_top:
        ring = rings[0]
        cen = sum((verts[j] for j in ring), Vector()) / len(ring)
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[(k + 1) % n], ring[k])); fmat.append(mat)
    if cap_end:
        ring = rings[-1]
        cen = sum((verts[j] for j in ring), Vector()) / len(ring)
        ci = add_vert(cen, vweights[ring[0]])
        for k in range(n):
            faces.append((ci, ring[k], ring[(k + 1) % n])); fmat.append(mat)
    return rings


def dense(keys, steps):
    out = []
    for j in range(len(keys) - 1):
        for s in range(steps):
            u = s / steps
            u = u * u * (3 - 2 * u) * 0.5 + u * 0.5
            out.append(tuple(keys[j][q] + (keys[j + 1][q] - keys[j][q]) * u for q in range(4)))
    out.append(keys[-1])
    return out


def blob(c, r, weight, mat=INK, seg=12, rings=8, scale=(1, 1, 1), disp=None):
    """A sphere (scaled); disp(d) -> radial factor for its unit direction d (x lateral, y back, z up)."""
    c = Vector(c)

    def at(d):
        f = 1.0 + (disp(d) if disp else 0.0)
        return c + V(r * scale[0] * d.x, r * scale[1] * d.y, r * scale[2] * d.z) * f
    top = add_vert(at(V(0, 0, 1)), weight(c, 0))
    idx = []
    for i in range(1, rings):
        ph = math.pi * i / rings
        row = []
        for k in range(seg):
            th = 2 * math.pi * k / seg
            d = V(math.sin(ph) * math.cos(th), math.sin(ph) * math.sin(th), math.cos(ph))
            p = at(d)
            row.append(add_vert(p, weight(p, 0)))
        idx.append(row)
    bot = add_vert(at(V(0, 0, -1)), weight(c, 0))
    for k in range(seg):
        faces.append((top, idx[0][(k + 1) % seg], idx[0][k])); fmat.append(mat)
        faces.append((bot, idx[-1][k], idx[-1][(k + 1) % seg])); fmat.append(mat)
    for i in range(len(idx) - 1):
        for k in range(seg):
            faces.append((idx[i][k], idx[i][(k + 1) % seg], idx[i + 1][(k + 1) % seg], idx[i + 1][k])); fmat.append(mat)


def tube_loop(points, r, weight, mat=INK, n=10, normal=V(0, 0, 1)):
    m = len(points)
    rings = []
    for i, c in enumerate(points):
        t = (points[(i + 1) % m] - points[i - 1]).normalized()
        u = t.cross(normal).normalized()
        v = t.cross(u).normalized()
        rr = r(i) if callable(r) else r
        rings.append([add_vert(c + u * (rr * math.cos(2 * math.pi * k / n)) + v * (rr * math.sin(2 * math.pi * k / n)), weight(c, 0)) for k in range(n)])
    for i in range(m):
        a, b = rings[i], rings[(i + 1) % m]
        for k in range(n):
            faces.append((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k])); fmat.append(mat)


def W(b):
    return lambda p, s: {b: 1.0}


def g2(x, w):
    return math.exp(-(x / w) ** 2)


def along(a, b, bone_a, bone_b, from_=0.75):
    a, b = Vector(a), Vector(b)
    d = b - a

    def f(p, s):
        t = max(0.0, min(1.0, (p - a).dot(d) / d.length_squared))
        if t <= from_:
            return {bone_a: 1.0}
        k = (t - from_) / (1 - from_) * 0.5
        return {bone_a: 1 - k, bone_b: k}
    return f


def folds(n_folds, amp, z_top, z_full, seed):
    """Folds deepening downwards, each its own depth and a little off its place."""
    def f(z, th, p, out):
        g = max(0.0, min(1.0, (z_top - z) / max(1e-3, z_top - z_full)))
        depth = 0.55 + 0.9 * (0.5 + 0.5 * noise.noise(Vector((th * 3.0 + seed, z * 2.0, seed * 0.3))))
        shift = 0.8 * noise.noise(Vector((th * 1.1, seed + 3.1, z * 1.3)))
        return out * (amp * g * depth * math.sin(n_folds * th + shift))
    return f


def piecewise(sw):
    return lambda p, s, sw=sw: sw[min(len(sw) - 1, int(round(s * (len(sw) - 1))))]


