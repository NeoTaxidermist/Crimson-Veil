# Fits the hero rig to every frame of the walk-cycle sheet (ArtSource/ref/walk_cycle.png: 12 side-view silhouettes,
# 4 columns x 3 rows, facing right) and stores the joint angles per frame in ArtSource/ref/walk_poses.json.
# Side-plane angles (degrees about the rig's X, + = the bone swings back), searched until the rendered silhouette of
# the model overlaps the frame's the most (IoU). Each frame starts from the previous one, so the legs keep their sides.
# Run in Blender after Hero.blend is open (the built model). Globals: FRAMES (indices to fit, default all),
# STEPS (search steps in degrees), ITER.
import json
import math
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = r"C:\Dev\CrimsonVeil"
SHEET = ROOT + r"\ArtSource\ref\walk_cycle.png"
OUT = ROOT + r"\ArtSource\ref\walk_poses.json"
ROW_BANDS = [(35, 372), (463, 804), (901, 1242)]
COL_BANDS = [(60, 300), (350, 590), (640, 880), (930, 1170)]
MODEL_H = 1.887              # the model's height to the hair's top
W_, H_ = 220, 300            # render size
SCALE = 0.0075               # metres per render pixel
BONES = ["Spine", "Neck", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
         "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R"]
LIMITS = {"LowerLeg.L": (-3, 120), "LowerLeg.R": (-3, 120),   # knees bend one way
          "LowerArm.L": (-120, 3), "LowerArm.R": (-120, 3),   # elbows bend forward
          "Spine": (-20, 20), "Neck": (-25, 25), "Foot.L": (-40, 40), "Foot.R": (-40, 40)}
rig = bpy.data.objects["HeroRig"]


def sheet_frames():
    im = bpy.data.images.load(SHEET, check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1]
    dark = (0.3 * px[..., 0] + 0.59 * px[..., 1] + 0.11 * px[..., 2]) < 0.3
    frames = []
    for r0, r1 in ROW_BANDS:
        for c0, c1 in COL_BANDS:
            m = dark[r0 - 5:r1 + 3, c0:c1]
            rows = np.nonzero(m.any(1))[0]
            top, ground = rows.min(), rows.max()
            k = MODEL_H / (ground - top)       # metres per sheet pixel for this frame
            head = m[top:top + int(0.12 * (ground - top))]
            head_col = np.nonzero(head.any(0))[0].mean()
            frames.append(dict(mask=m, ground=ground, k=k, head_col=head_col))
    return frames


def target_for(f, shift):
    """The frame's silhouette sampled on the render grid (model y = 0 at the render's centre column)."""
    rr, cc = np.mgrid[0:H_, 0:W_]
    z = (H_ - 1 - rr) * SCALE - 0.02
    y = (W_ / 2 - cc) * SCALE                     # image right = forward = -y
    srow = np.round(f["ground"] - z / f["k"]).astype(int)
    # the head's middle sits at model y = -0.05 + shift
    scol = np.round(f["head_col"] + (-0.05 + shift - y) / f["k"]).astype(int)
    m = f["mask"]
    ok = (srow >= 0) & (srow < m.shape[0]) & (scol >= 0) & (scol < m.shape[1])
    t = np.zeros((H_, W_), bool)
    t[ok] = m[srow[ok], scol[ok]]
    return t


def apply_pose(angles):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for name in BONES:
        pb = rig.pose.bones[name]
        M = pb.bone.matrix_local.to_3x3()
        pb.rotation_quaternion = (M.inverted() @ Matrix.Rotation(math.radians(angles.get(name, 0.0)), 3, 'X') @ M).to_quaternion()
    hips = rig.pose.bones["Hips"]
    hips.location = hips.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, -angles.get("drop", 0.0)))
    bpy.context.view_layer.update()


def setup_camera():
    scn = bpy.context.scene
    cam = bpy.data.objects["PreviewCam"]
    scn.camera = cam
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = H_ * SCALE
    scn.render.resolution_x, scn.render.resolution_y = W_, H_
    cam.location = (-4, 0, H_ * SCALE / 2 - 0.02)
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    scn.render.engine = 'BLENDER_WORKBENCH'
    sh = scn.display.shading
    sh.light, sh.color_type, sh.single_color = 'FLAT', 'SINGLE', (0, 0, 0)
    scn.world.color = (1, 1, 1)


TMP = bpy.app.tempdir + "walkfit.png"


def render_mask():
    bpy.context.scene.render.filepath = TMP
    bpy.ops.render.render(write_still=True)
    im = bpy.data.images.load(TMP, check_existing=False)
    m = np.array(im.pixels[:], np.float32).reshape(H_, W_, 4)[::-1][..., 0] < 0.5
    bpy.data.images.remove(im)
    return m


def iou(a, b):
    return (a & b).sum() / max((a | b).sum(), 1)


def fit_frame(f, pose):
    pose = dict(pose)
    for k, (lo, hi) in LIMITS.items():  # start inside the joints' limits
        if k in pose:
            pose[k] = min(max(pose[k], lo), hi)
    target = target_for(f, pose.get("shift", 0.0))
    apply_pose(pose)
    best = iou(render_mask(), target)
    for step in globals().get("STEPS", (8, 4, 2, 1)):
        for _ in range(globals().get("ITER", 2)):
            for key in BONES + ["drop", "shift"]:
                d = step if key in BONES else step * 0.004
                for sgn in (1, -1):
                    trial = dict(pose)
                    trial[key] = pose.get(key, 0.0) + sgn * d
                    lo, hi = LIMITS.get(key, (-90, 90)) if key in BONES else (-0.2, 0.2)
                    if not lo <= trial[key] <= hi:
                        continue
                    if key == "shift":
                        target_t = target_for(f, trial["shift"])
                        apply_pose(trial)
                        sc = iou(render_mask(), target_t)
                    else:
                        apply_pose(trial)
                        sc = iou(render_mask(), target)
                    if sc > best:
                        best, pose = sc, trial
                        if key == "shift":
                            target = target_t
                        break
    return pose, float(best)


setup_camera()
FR = sheet_frames()
try:
    data = json.load(open(OUT))
except Exception:
    data = {"frames": [None] * len(FR), "iou": [0.0] * len(FR)}
start = json.load(open(ROOT + r"\ArtSource\ref\concept_pose.json"))
start = {k: v for k, v in start.items() if k in BONES or k == "drop"}
for i in globals().get("FRAMES", range(len(FR))):
    prev = data["frames"][i] or (data["frames"][i - 1] if i > 0 and data["frames"][i - 1] else start)
    pose, sc = fit_frame(FR[i], prev)
    data["frames"][i] = {k: round(v, 3) for k, v in pose.items()}
    data["iou"][i] = round(sc, 4)
    json.dump(data, open(OUT, "w"), indent=1)
apply_pose({})
result = {"iou": data["iou"]}
