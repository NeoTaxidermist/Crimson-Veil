"""
Creature modelling kit for Crimson Veil (run inside Blender).

Everything is authored in GAME coordinates (Unity): x = forward (the way the creature faces),
y = up, z = depth (negative = towards the camera). The FBX export settings used by the project
map Blender (x, y, z) -> Unity (-x, -z, y), so G() converts game -> Blender.

Material slot 0 = "Ink" (black silhouette), slot 1 = "Crimson" (red details).
"""
import math
import random
import bpy
import bmesh
from mathutils import Vector, Matrix, noise

INK, CRIMSON = 0, 1


def G(x, y, z=0.0):
    """Game (Unity) coordinates -> Blender coordinates."""
    return Vector((-x, -z, y))


def reset():
    bpy.ops.wm.read_homefile(use_empty=True)
    for name, col in (("Ink", (0.01, 0.01, 0.012, 1)), ("Crimson", (0.9, 0.02, 0.04, 1))):
        if name not in bpy.data.materials:
            m = bpy.data.materials.new(name)
            m.diffuse_color = col


class Part:
    """One exported object: a mesh built in game coordinates relative to its pivot."""

    def __init__(self, name, pivot=(0, 0, 0)):
        self.name = name
        self.pivot = Vector(pivot)
        self.bm = bmesh.new()
        # Per-corner tone exported as vertex colour: dressed stones vary in shade, mortar behind them is dark.
        self.col = self.bm.loops.layers.float_color.new("Col")
        self.tones = {}

    def paint(self, faces, value):
        for f in faces:
            if f is not None:
                self.tones[f] = value

    MORTAR = 0.5

    # --- primitives (all coordinates in game space, relative to the pivot) -------------------------

    def _v(self, p):
        p = Vector(p)
        return self.bm.verts.new(G(p.x, p.y, p.z))

    def _face(self, verts, mat):
        try:
            f = self.bm.faces.new(verts)
            f.material_index = mat
            f.smooth = True
            return f
        except ValueError:
            return None

    def tube(self, pts, radii, sides=8, mat=INK, cap=True, flat=1.0, twist=0.0, jitter=0.0, seed=0):
        """A tube along game-space points; radii per point (or one value). `flat` squashes it in depth."""
        rnd = random.Random(seed)
        pts = [Vector(p) for p in pts]
        if not isinstance(radii, (list, tuple)):
            radii = [radii] * len(pts)
        rings = []
        prev_n = None
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)])
            if t.length < 1e-6:
                t = Vector((0, 1, 0))
            t.normalize()
            if prev_n is None:
                ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
                n = t.cross(ref).normalized()
            else:
                n = (prev_n - t * prev_n.dot(t)).normalized()
            prev_n = n
            b = t.cross(n).normalized()
            ring = []
            for k in range(sides):
                a = 2 * math.pi * k / sides + twist * i
                r = radii[i] * (1 + jitter * (rnd.random() - 0.5))
                off = n * math.cos(a) * r + b * math.sin(a) * r
                off.z *= flat
                ring.append(self._v(p + off))
            rings.append(ring)
        for i in range(len(rings) - 1):
            for k in range(sides):
                a, b2 = rings[i][k], rings[i][(k + 1) % sides]
                c, d = rings[i + 1][(k + 1) % sides], rings[i + 1][k]
                self._face([a, b2, c, d], mat)
        if cap:
            self._face(list(reversed(rings[0])), mat)
            self._face(rings[-1], mat)
        return rings

    def lathe(self, profile, sides=24, center=(0, 0, 0), sx=1.0, sz=1.0, mat=INK, rfunc=None, yfunc=None, cap_top=True, cap_bottom=True):
        """Surface of revolution around the vertical axis. profile = [(radius, y)], bottom to top.
        rfunc(theta, i, r, y) -> radius multiplier (folds, rags); yfunc(theta, i, y) -> y offset (ragged hems)."""
        c = Vector(center)
        rings = []
        for i, (r, y) in enumerate(profile):
            ring = []
            for k in range(sides):
                th = 2 * math.pi * k / sides
                rr = r * (rfunc(th, i, r, y) if rfunc else 1.0)
                yy = y + (yfunc(th, i, y) if yfunc else 0.0)
                ring.append(self._v(c + Vector((math.cos(th) * rr * sx, yy, math.sin(th) * rr * sz))))
            rings.append(ring)
        for i in range(len(rings) - 1):
            for k in range(sides):
                self._face([rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]], mat)
        if cap_bottom and profile[0][0] > 1e-4:
            self._face(list(reversed(rings[0])), mat)
        if cap_top and profile[-1][0] > 1e-4:
            self._face(rings[-1], mat)
        return rings

    def blob(self, center, radii, segs=16, rings=10, mat=INK, bumps=0.0, seed=0, squash_front=0.0):
        """Ellipsoid, optionally lumpy."""
        c = Vector(center)
        grid = []
        for j in range(rings + 1):
            v = math.pi * j / rings
            row = []
            for k in range(segs):
                u = 2 * math.pi * k / segs
                d = Vector((math.sin(v) * math.cos(u), math.cos(v), math.sin(v) * math.sin(u)))
                s = 1.0
                if bumps:
                    s += bumps * noise.noise(d * 2.7 + Vector((seed, seed * 0.37, seed * 1.3)))
                p = Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * s
                if squash_front and p.x > 0:
                    p.x *= (1 - squash_front)
                row.append(self._v(c + p))
            grid.append(row)
        for j in range(rings):
            for k in range(segs):
                self._face([grid[j][k], grid[j][(k + 1) % segs], grid[j + 1][(k + 1) % segs], grid[j + 1][k]], mat)
        return grid

    def prism(self, outline, z0, z1, mat=INK, xform=None):
        """A flat shape: a 2D outline [(x, y)] in the picture plane, extruded in depth from z0 to z1.
        xform(p) may bend/rotate the resulting game-space points."""
        def P(x, y, z):
            p = Vector((x, y, z))
            return xform(p) if xform else p
        front = [self._v(P(x, y, z0)) for x, y in outline]
        back = [self._v(P(x, y, z1)) for x, y in outline]
        n = len(outline)
        for k in range(n):
            self._face([front[k], front[(k + 1) % n], back[(k + 1) % n], back[k]], mat)
        # Caps: fill concave outlines robustly.
        for ring, rev in ((front, False), (back, True)):
            edges = [self.bm.edges.get((ring[k], ring[(k + 1) % n])) or self.bm.edges.new((ring[k], ring[(k + 1) % n])) for k in range(n)]
            res = bmesh.ops.triangle_fill(self.bm, use_beauty=True, use_dissolve=False, edges=edges)
            for f in res.get("geom", []):
                if isinstance(f, bmesh.types.BMFace):
                    f.material_index = mat
                    f.smooth = False
        return front, back

    def bay(self, x0, x1, y0, top, pier, spring, z0, z1, mat=INK, segs=12, ragged=0.0, seed=0, pointed=False, masonry=True):
        """One bay of an arcade: an inverted U (two piers and the wall over an arched opening)
        between x0 and x1, from y0 up to `top`; the opening springs at y0 + spring.
        ragged > 0 breaks the top edge (crumbling masonry)."""
        rnd = random.Random(seed)
        inner0, inner1 = x0 + pier, x1 - pier
        r = (inner1 - inner0) / 2
        cx = (inner0 + inner1) / 2
        out = [(x0, y0)]
        # top edge, possibly crumbling
        n = max(2, int((x1 - x0) / 0.35))
        tops = []
        for k in range(n + 1):
            x = x0 + (x1 - x0) * k / n
            y = top - (abs(rnd.gauss(0, ragged)) if ragged > 0 and 0 < k < n else 0.0)
            tops.append((x, y))
        out += tops
        out += [(x1, y0), (inner1, y0), (inner1, y0 + spring)]
        ys = y0 + spring
        if pointed:
            # Gothic arch: each half is an arc centred on the opposite springing line (radius 1.6 r).
            R = r * 1.6
            cr, cl = inner1 - R, inner0 + R
            tp = math.acos(max(-1.0, min(1.0, (cx - cr) / R)))
            half = max(2, segs // 2)
            for k in range(1, half + 1):
                t = tp * k / half
                out.append((cr + R * math.cos(t), ys + R * math.sin(t)))
            for k in range(1, half):
                t = math.pi - tp + tp * k / half
                out.append((cl + R * math.cos(t), ys + R * math.sin(t)))
        else:
            for k in range(1, segs):
                a = math.pi * k / segs
                out.append((cx + r * math.cos(a), ys + r * math.sin(a)))
        out += [(inner0, y0 + spring), (inner0, y0)]
        before = set(self.bm.faces)
        self.prism(out, z0, z1, mat=mat)
        if not masonry:
            return out
        # The bay's camera face is the mortar bed under its stones: darker, so every joint reads.
        front_y = -z0  # game z -> Blender -y
        for f in set(self.bm.faces) - before:
            c = f.calc_center_median()
            if abs(c.y - front_y) < 1e-3 and all(abs(v.co.y - front_y) < 1e-3 for v in f.verts):
                self.tones[f] = Part.MORTAR
        # Masonry on the camera face: wedge voussoirs (keystone proud), imposts, ashlar on piers and spandrels.
        u = min(1.0, max(0.35, pier / 0.9))
        ringw = min(pier * 0.85, r * 0.5, 0.75)
        n = max(7, int(math.pi * (r + ringw / 2) / (0.5 * u)) | 1)
        if pointed:
            R = r * 1.6
            cr, cl = inner1 - R, inner0 + R
            tp = math.acos(max(-1.0, min(1.0, (cx - cr) / R)))

            def arc(t, rad):  # t 0..1 from right springer over the apex to the left springer
                if t <= 0.5:
                    a = tp * (t / 0.5)
                    return (cr + rad * math.cos(a), ys + rad * math.sin(a)) if rad == R else \
                        (cr + (rad) * math.cos(a), ys + rad * math.sin(a))
                a = math.pi - tp + tp * ((t - 0.5) / 0.5)
                return (cl + rad * math.cos(a), ys + rad * math.sin(a))
            inner = lambda t: arc(t, R)
            outer = lambda t: arc(t, R + ringw)
            arc_len = 2 * tp * (R + ringw / 2)
        else:
            inner = lambda t: (cx + r * math.cos(math.pi * t), ys + r * math.sin(math.pi * t))
            outer = lambda t: (cx + (r + ringw) * math.cos(math.pi * t), ys + (r + ringw) * math.sin(math.pi * t))
            arc_len = math.pi * (r + ringw / 2)
        # Voussoirs: equal wedges set tight with the same thin joint as the coursed stone, every joint radial;
        # the keystone at the crown a little longer.
        joint = 0.04 * u
        gap = joint * 0.5 / arc_len
        key_k = n // 2
        for k in range(n):
            t0, t1 = k / n + gap, (k + 1) / n - gap
            key = k == key_k
            o = (lambda t: outer(t)) if not key else (lambda t: (lambda p, q: (p[0] + (q[0] - p[0]) * 1.3, p[1] + (q[1] - p[1]) * 1.3))(inner(t), outer(t)))
            self.stone(0, 0, 1, 1, z0, (0.14 if key else rnd.uniform(0.07, 0.09)) * u, 0.025 * u, mat=mat,
                       quad=[inner(t0), inner(t1), o(t1), o(t0)])
        for px0, px1 in ((x0, inner0), (inner1, x1)):
            self.box(px0 - 0.08 * u, ys - 0.22 * u, px1 + 0.08 * u, ys, z0 - 0.14 * u, z0 + 0.01, mat)       # impost
        top_ok = top - (ragged * 2.5 if ragged else 0) - 0.15

        # The extrados (outer curve of the ring, the keystone standing up out of it): spandrel stones are cut to it.
        kp0, kp1 = inner(key_k / n), outer((key_k + 0.5) / n)
        key_half = abs(outer(key_k / n)[0] - outer((key_k + 1) / n)[0]) * 0.5 + joint
        key_top = kp1[1] + (kp1[1] - inner((key_k + 0.5) / n)[1]) * 0.3 + joint

        def extrados(x):
            if pointed:
                c = cr if x >= cx else cl
                re = R + ringw + joint
            else:
                c, re = cx, r + ringw + joint
            y = ys + math.sqrt(re * re - (x - c) ** 2) if abs(x - c) < re else -1e9
            if abs(x - cx) < key_half:
                y = max(y, key_top)
            return y

        def skip(sx, sy, w, h):
            return sy + h / 2 > top_ok
        kw = dict(course=0.45 * u, proud=(0.05 * u, 0.1 * u), bevel=0.035 * u, mat=mat)
        # Piers up to the impost (each on its own, never reaching into the opening), then the spandrels.
        self.ashlar(x0, inner0, y0 + 0.05, min(ys - 0.24 * u, top_ok), z0, seed=seed + 101, **kw)
        self.ashlar(inner1, x1, y0 + 0.05, min(ys - 0.24 * u, top_ok), z0, seed=seed + 102, **kw)
        if top_ok > ys + 0.3:
            self.ashlar(x0, x1, ys + 0.02, top_ok, z0, seed=seed + 103, skip=skip, cut=extrados, **kw)
        return out

    def stone(self, cx, cy, w, h, z, proud, bevel=0.06, tilt=0.0, mat=INK, quad=None, tone=None):
        """A dressed stone standing proud of the wall plane z (towards the camera) with chamfered front
        edges, flat-shaded so the game's relief shading draws a moonlit top and shaded sides.
        quad: optional 4 corner points [(x, y)] (bl, br, tr, tl) for wedge-shaped voussoirs."""
        bevel = min(bevel, proud * 0.9, w * 0.3, h * 0.3)
        ca, sa = math.cos(math.radians(tilt)), math.sin(math.radians(tilt))
        if quad is None:
            hx, hy = w / 2, h / 2
            quad = [(-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy)]
            quad = [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in quad]
        nq = len(quad)
        mx, my = sum(p[0] for p in quad) / nq, sum(p[1] for p in quad) / nq

        def shrink(p, d):
            vx, vy = mx - p[0], my - p[1]
            L = math.hypot(vx, vy) or 1.0
            return (p[0] + vx / L * d * 1.41, p[1] + vy / L * d * 1.41)
        zf, zb, zw = z - proud, z - proud + bevel, z + 0.01
        front = [self._v((*shrink(p, bevel), zf)) for p in quad]
        mid = [self._v((p[0], p[1], zb)) for p in quad]
        wall = [self._v((p[0], p[1], zw)) for p in quad]
        faces = [self._face(front, mat)]
        for k in range(nq):
            j = (k + 1) % nq
            faces.append(self._face([front[k], mid[k], mid[j], front[j]], mat))
            faces.append(self._face([mid[k], wall[k], wall[j], mid[j]], mat))
        for f in faces:
            if f:
                f.smooth = False
        if tone is None:  # deterministic shade per stone (vertex colours cannot exceed 1: stones 0.8..1, mortar darker)
            tone = random.Random(int((mx * 31.7 + my * 17.3 + z * 7.1) * 1000)).uniform(0.8, 1.0)
        self.paint(faces, tone)

    @staticmethod
    def cut_to(cx, cy, w, h, cut):
        """The stone (cx, cy, w, h) trimmed along the curve y = cut(x) (an arch's extrados): None when it does
        not meet it, [] when too little would be left, else the outline."""
        x0, x1, y0, y1 = cx - w / 2, cx + w / 2, cy - h / 2, cy + h / 2
        if all(cut(x0 + (x1 - x0) * i / 8) <= y0 for i in range(9)):
            return None
        pts = [(x0 + (x1 - x0) * i / 10, y0) for i in range(10)] + [(x1, y0 + (y1 - y0) * i / 3) for i in range(3)]
        pts += [(x1, y1), (x0, y1)] + [(x0, y0 + (y1 - y0) * i / 3) for i in (2, 1)]
        out = []
        for (x, y) in pts:
            e = cut(x)
            q = (x, min(e, y1)) if y < e else (x, y)
            if not out or (out[-1][0] - q[0]) ** 2 + (out[-1][1] - q[1]) ** 2 > 1e-6:
                out.append(q)
        if len(out) > 2 and (out[0][0] - out[-1][0]) ** 2 + (out[0][1] - out[-1][1]) ** 2 < 1e-6:
            out.pop()
        area = abs(sum(out[i][0] * out[(i + 1) % len(out)][1] - out[(i + 1) % len(out)][0] * out[i][1] for i in range(len(out)))) / 2
        return [] if len(out) < 3 or area < w * h * 0.3 else out

    def ashlar(self, x0, x1, y0, y1, z, course=0.45, proud=(0.05, 0.1), bevel=0.06, seed=0, skip=None, missing=0.0, mat=INK, cut=None):
        """Staggered courses of dressed stones over the wall face x0..x1, y0..y1 at plane z.
        skip(cx, cy, w, h) -> True leaves a stone out (openings, broken parts);
        cut(x) -> y trims the stones meeting an arch along its extrados, so the coursing closes onto it."""
        rnd = random.Random(seed)
        joint = 0.04
        row = 0
        y = y0
        while y + course * 0.6 < y1:
            ch = min(course, y1 - y)
            x = x0 + (course * 0.7 if row % 2 else 0.0)
            segs = [(x0, x)] if x > x0 + 0.1 else []
            while x < x1 - 0.05:
                xe = min(x1, x + course * rnd.uniform(1.3, 2.4))
                if x1 - xe < course * 0.6:
                    xe = x1
                segs.append((x, xe))
                x = xe
            for a, c in segs:
                w, h = c - a - joint, ch - joint
                cx, cy = (a + c) / 2, y + ch / 2 - rnd.uniform(0, 0.025)
                if w < 0.12 or rnd.random() < missing or (skip and skip(cx, cy, w, h)):
                    continue
                trimmed = Part.cut_to(cx, cy, w, h, cut) if cut else None
                if trimmed is None:
                    self.stone(cx, cy, w, h, z, rnd.uniform(*proud), bevel, rnd.uniform(-1.2, 1.2), mat)
                elif trimmed:
                    self.stone(0, 0, w, h, z, rnd.uniform(*proud), bevel, mat=mat, quad=trimmed)
            y += ch
            row += 1

    def box(self, x0, y0, x1, y1, z0, z1, mat=INK):
        self.prism([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1, mat=mat)

    def crenellate(self, x0, x1, y, merlon=0.5, gap=0.35, h=0.45, z0=0.0, z1=0.5, mat=INK, seed=0, broken=0.2):
        """Battlements along the top of a wall."""
        rnd = random.Random(seed)
        x = x0
        while x + merlon <= x1 + 1e-4:
            if rnd.random() > broken:
                hh = h * (1 - 0.6 * rnd.random() * (rnd.random() < broken))
                self.box(x, y, x + merlon, y + hh, z0, z1, mat)
            x += merlon + gap

    def displace(self, amount, scale=3.0, seed=0, where=None):
        """Organic irregularity: push vertices along noise."""
        off = Vector((seed * 1.7, seed * 0.3, seed * 2.1))
        for v in self.bm.verts:
            g = Vector((-v.co.x, v.co.z, -v.co.y))  # back to game space
            if where and not where(g):
                continue
            d = noise.noise_vector(g * scale + off) * amount
            v.co += G(d.x, d.y, d.z)

    # --- output ----------------------------------------------------------------------------------

    def build(self, parent=None):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        for f in self.bm.faces:
            t = self.tones.get(f, 1.0)
            for loop in f.loops:
                t = min(1.0, t)
                loop[self.col] = (t, t, t, 1.0)
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        me.materials.append(bpy.data.materials["Ink"])
        me.materials.append(bpy.data.materials["Crimson"])
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        ob.location = G(*self.pivot)
        # Geometry was authored relative to the pivot: nothing more to do.
        if parent is not None:
            ob.parent = parent
            ob.location = G(*self.pivot)
        return ob


def export(path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.context.scene.objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             object_types={'EMPTY', 'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             colors_type='LINEAR')


def preview(path, center=(0, 1, 0), size=2.5, angle=0.0):
    """Renders the scene as the game camera sees it (from -z), workbench, light background."""
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.get("PreviewCam") or bpy.data.cameras.new("PreviewCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = size
    cam = bpy.data.objects.get("PreviewCam") or bpy.data.objects.new("PreviewCam", cam_data)
    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)
    c = Vector(center)
    a = math.radians(angle)
    eye = c + Vector((math.sin(a) * 10, 0, -math.cos(a) * 10))
    cam.location = G(eye.x, eye.y, eye.z)
    direction = G(c.x, c.y, c.z) - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'FLAT'
    scene.display.shading.color_type = 'MATERIAL'
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.film_transparent = False
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = (0.75, 0.75, 0.76)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def stats():
    return {o.name: len(o.data.polygons) for o in bpy.context.scene.objects if o.type == 'MESH'}
