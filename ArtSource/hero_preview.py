# Renders silhouettes of the hero (side, front, three-quarter) into OUT_DIR — exec after hero_build.py.
import math
import bpy

OUT_DIR = globals().get("OUT_DIR", r"C:\Dev\CrimsonVeil\Captures")
POSE = globals().get("POSE", None)  # optional callable(rig) to pose the rig first
scn = bpy.context.scene
cam = bpy.data.objects.get("PreviewCam")
if cam is None:
    cam = bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam"))
    scn.collection.objects.link(cam)
scn.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.4
scn.render.resolution_x, scn.render.resolution_y = 700, 800
scn.render.engine = 'BLENDER_WORKBENCH'
sh = scn.display.shading
sh.light = 'FLAT'
sh.color_type = 'MATERIAL'
for m in bpy.data.materials:
    m.diffuse_color = (0.7, 0.02, 0.035, 1) if "Crimson" in m.name else (0.01, 0.01, 0.012, 1)
w = scn.world or bpy.data.worlds.new("W")
scn.world = w
w.color = (0.82, 0.82, 0.82)
rig = bpy.data.objects["HeroRig"]
if POSE:
    POSE(rig)
out = {}
for name, loc, rot in (("side", (4, 0, 1.02), (90, 0, 90)), ("front", (0, -4, 1.02), (90, 0, 0)),
                       ("three", (2.8, -2.8, 1.02), (90, 0, 45))):
    cam.location = loc
    cam.rotation_euler = tuple(math.radians(a) for a in rot)
    p = OUT_DIR + "\\hero_" + name + ".png"
    scn.render.filepath = p
    bpy.ops.render.render(write_still=True)
    out[name] = p
result = out
