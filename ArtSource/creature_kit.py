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
                             object_types={'EMPTY', 'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True)


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
