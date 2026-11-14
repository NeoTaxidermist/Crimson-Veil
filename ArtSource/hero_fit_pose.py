# Poses the hero rig like the concept art (ArtSource/ref/hero_concept.jpg) so the model can be compared with the
# drawing silhouette-to-silhouette: joint angles in the side plane are searched until the rendered side silhouette
# overlaps the concept's the most (IoU). The concept's sword and scarf are left out of the comparison.
# exec after hero_build.py (EXPORT=False). Globals: ITER (search passes), START (dict of angles to start from).
# Result: CONCEPT_POSE (degrees per bone, + = swings back) is stored in ArtSource/ref/concept_pose.json.
import json
import math
import bpy
import numpy as np
from mathutils import Matrix

ROOT = r"C:\Dev\CrimsonVeil"
K = 0.00166
GROUND_ROW, AXIS_COL = 1875, 720
W_, H_ = 400, 620            # render size; 2 concept px per render px
SCALE = 2 * K                # metres per render pixel
TOP_ROW = GROUND_ROW - H_ * 2 + 2 * 16  # concept row at the render's top (ground 16 px above the bottom)
rig = bpy.data.objects["HeroRig"]


def concept_target():
    im = bpy.data.images.load(ROOT + r"\ArtSource\ref\hero_concept.jpg", check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1]
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    red = (r > g * 1.8) & (r > 0.25)
    dark = ((0.3 * r + 0.59 * g + 0.11 * b) < 0.12) & ~red
    c0 = int(round(AXIS_COL - 0.05 / K)) - W_  # render column 0 in concept px (model y = 0 is 0.05 m behind the neck axis)
    m = dark[TOP_ROW:TOP_ROW + 2 * H_:2, c0:c0 + 2 * W_:2].copy()
    care = np.ones_like(m)
    rows, cols = np.mgrid[0:H_, 0:W_]
    crow, ccol = TOP_ROW + rows * 2 - 680, c0 + cols * 2 - 250  # in the crop coordinates used while measuring
    sword = np.abs((crow - 880) + (ccol * (180.0 / 285.0))) < 22  # the blade from (0, 880) up to the hilt (285, 700)
    care &= ~(sword & (ccol < 300))
    care &= ~((crow < 445) & (ccol < 330))  # the scarf streaming behind
    care &= ~(red[TOP_ROW:TOP_ROW + 2 * H_:2, c0:c0 + 2 * W_:2])
    care &= crow < 1196  # above the ground
    return m, care


TARGET, CARE = concept_target()
BONES = ["Spine", "Neck", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
         "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R"]


def apply_pose(angles, drop=0.0):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for name, deg in angles.items():
        if name == "drop":
            continue
        pb = rig.pose.bones[name]
        M = pb.bone.matrix_local.to_3x3()
        pb.rotation_quaternion = (M.inverted() @ Matrix.Rotation(math.radians(deg), 3, 'X') @ M).to_quaternion()
    hips = rig.pose.bones["Hips"]
    hips.location = hips.bone.matrix_local.to_3x3().inverted() @ Matrix.Identity(3).col[2] * -angles.get("drop", 0.0)
    bpy.context.view_layer.update()


def render_mask(path):
    scn = bpy.context.scene
    cam = bpy.data.objects["PreviewCam"]
    scn.camera = cam
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = H_ * SCALE
    scn.render.resolution_x, scn.render.resolution_y = W_, H_
    top_z = (GROUND_ROW - TOP_ROW) * K
    cam.location = (-4, 0, top_z - H_ * SCALE / 2)
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    scn.render.engine = 'BLENDER_WORKBENCH'
    sh = scn.display.shading
    sh.light, sh.color_type, sh.single_color = 'FLAT', 'SINGLE', (0, 0, 0)
    scn.world.color = (1, 1, 1)
    scn.render.filepath = path
    bpy.ops.render.render(write_still=True)
    im = bpy.data.images.load(path, check_existing=False)
    m = np.array(im.pixels[:], np.float32).reshape(H_, W_, 4)[::-1][..., 0] < 0.5
    bpy.data.images.remove(im)
    return m


def score(m):
    a, b = m & CARE, TARGET & CARE
    return (a & b).sum() / max((a | b).sum(), 1)


TMP = bpy.app.tempdir + "fit.png"
pose_file = ROOT + r"\ArtSource\ref\concept_pose.json"
try:
    pose = json.load(open(pose_file))
except Exception:
    pose = {}
pose.update(globals().get("START", {}))
for b in BONES:
    pose.setdefault(b, 0.0)
pose.setdefault("drop", 0.0)
apply_pose(pose)
best = score(render_mask(TMP))
for step in globals().get("STEPS", (12, 6, 3)):
    for _ in range(globals().get("ITER", 1)):
        for key in BONES + ["drop"]:
            d = step if key != "drop" else step * 0.004
            for sgn in (1, -1):
                trial = dict(pose)
                trial[key] = pose[key] + sgn * d
                apply_pose(trial)
                sc = score(render_mask(TMP))
                if sc > best:
                    best, pose = sc, trial
                    break
apply_pose(pose)
json.dump(pose, open(pose_file, "w"), indent=1)
CONCEPT_POSE = pose
result = {"iou": round(float(best), 4), "pose": {k: round(v, 3) for k, v in pose.items()}}
