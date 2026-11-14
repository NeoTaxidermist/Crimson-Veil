# Fits the hero rig to every frame of the animation sheet (ArtSource/ref/anim_sheet.png: Idle 8, Run 12,
# Jump 8, Attack 8 side-view silhouettes, facing right) and stores the joint angles per frame in
# ArtSource/ref/anim_poses.json. Side-plane angles (degrees about the rig's X, + = the bone swings back) plus the
# body's lift, searched until the rendered silhouette overlaps the frame's the most (IoU); every frame starts from
# the previous one. Run in Blender with Hero.blend open. Globals: ANIM ("idle"/"run"/"jump"/"attack"), FRAMES,
# STEPS, ITER.
import json
import math
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = r"C:\Dev\CrimsonVeil"
SHEET = ROOT + r"\ArtSource\ref\anim_sheet.png"
OUT = ROOT + r"\ArtSource\ref\anim_poses.json"
ROWS = {"idle": (20, 212), "run": (296, 449), "jump": (528, 737), "attack": (838, 987)}
COUNT = {"idle": 8, "run": 12, "jump": 8, "attack": 8}
TITLE_ROWS, TITLE_COLS = {"idle": 20, "jump": 12}, 420
MODEL_H = 1.887
W_, H_ = 240, 300
SCALE = 0.0085
BONES = ["Hips", "Spine", "Neck", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
         "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R"]
LIMITS = {"Hips": (-45, 45), "Spine": (-35, 35), "Neck": (-30, 30),
          "UpperLeg.L": (-120, 80), "UpperLeg.R": (-120, 80), "LowerLeg.L": (-3, 150), "LowerLeg.R": (-3, 150),
          "Foot.L": (-45, 45), "Foot.R": (-45, 45), "UpperArm.L": (-130, 120), "UpperArm.R": (-130, 120),
          "LowerArm.L": (-140, 3), "LowerArm.R": (-140, 3)}
rig = bpy.data.objects["HeroRig"]


def load_sheet():
    im = bpy.data.images.load(SHEET, check_existing=True)
    w, h = im.size
    px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1]
    return (0.3 * px[..., 0] + 0.59 * px[..., 1] + 0.11 * px[..., 2]) < 0.35


def frames_of(anim, dark):
    r0, r1 = ROWS[anim]
    band = dark[r0:r1].copy()
    band[:TITLE_ROWS.get(anim, 0), :TITLE_COLS] = False  # the row's title (only where it is)
    # the ground line: thin and long; find its row, then drop every thin horizontal streak
    vert = band.copy()
    up, dn = np.roll(band, 2, 0), np.roll(band, -2, 0)
    thin = band & ~(up & dn)
    ground_row = int(np.argmax(thin.sum(1)))
    fig = band & (up | dn)
    fig[ground_row - 1:ground_row + 3] &= np.roll(fig, -4, 0)[ground_row - 1:ground_row + 3]
    fig[ground_row + 3:] = False
    cols = fig.sum(0) > 0
    spans, s = [], None
    for c, v in enumerate(list(cols) + [False]):
        if v and s is None:
            s = c
        if not v and s is not None:
            if c - s > 15:
                spans.append([s, c])
            s = None
    # merge pieces of one figure (a hand clear of the body)
    merged = []
    for sp in spans:
        if merged and sp[0] - merged[-1][1] < 12:
            merged[-1][1] = sp[1]
        else:
            merged.append(sp)
    out = []
    for c0, c1 in merged[:COUNT[anim]]:
        m = fig[:, max(c0 - 6, 0):c1 + 6]
        rows = np.nonzero(m.any(1))[0]
        top = rows.min()
        head = m[top:top + 20]
        out.append(dict(mask=m, ground=ground_row, top=top, head_col=float(np.nonzero(head.any(0))[0].mean())))
    return out


def sheet_scale(dark):
    """Metres per sheet pixel, from the standing (idle) frames: their height is the model's."""
    idle = frames_of("idle", dark)
    return MODEL_H / np.mean([f["ground"] - f["top"] for f in idle])


def target_for(f, k, shift, zoom=1.0):
    k = k / zoom  # the sheet draws some rows smaller (zoom < 1)
    rr, cc = np.mgrid[0:H_, 0:W_]
    z = (H_ - 1 - rr) * SCALE - 0.02
    y = (W_ / 2 - cc) * SCALE
    srow = np.round(f["ground"] - z / k).astype(int)
    scol = np.round(f["head_col"] + (-0.05 + shift - y) / k).astype(int)
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
    cam.data.ortho_scale = max(W_, H_) * SCALE
    scn.render.resolution_x, scn.render.resolution_y = W_, H_
    cam.location = (-4, 0, H_ * SCALE / 2 - 0.02)
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    scn.render.engine = 'BLENDER_WORKBENCH'
    sh = scn.display.shading
    sh.light, sh.color_type, sh.single_color = 'FLAT', 'SINGLE', (0, 0, 0)
    scn.world.color = (1, 1, 1)


TMP = bpy.app.tempdir + "animfit.png"


def render_mask():
    bpy.context.scene.render.filepath = TMP
    bpy.ops.render.render(write_still=True)
    im = bpy.data.images.load(TMP, check_existing=False)
    m = np.array(im.pixels[:], np.float32).reshape(H_, W_, 4)[::-1][..., 0] < 0.5
    bpy.data.images.remove(im)
    return m


def iou(a, b):
    return (a & b).sum() / max((a | b).sum(), 1)


def fit_frame(f, k, pose, lift_range):
    pose = dict(pose)
    for key, (lo, hi) in LIMITS.items():
        if key in pose:
            pose[key] = min(max(pose[key], lo), hi)
    target = target_for(f, k, pose.get("shift", 0.0), pose.get("zoom", 1.0))
    apply_pose(pose)
    best = iou(render_mask(), target)
    for step in globals().get("STEPS", (8, 4, 2, 1)):
        for _ in range(globals().get("ITER", 2)):
            for key in BONES + ["drop", "shift", "zoom"]:
                d = step if key in BONES else step * {"drop": 0.01, "shift": 0.004, "zoom": 0.01}[key]
                for sgn in (1, -1):
                    trial = dict(pose)
                    trial[key] = pose.get(key, 0.0) + sgn * d
                    lo, hi = LIMITS[key] if key in BONES else {"drop": lift_range, "shift": (-0.3, 0.3), "zoom": (0.62, 1.15)}[key]
                    if not lo <= trial[key] <= hi:
                        continue
                    tt = target_for(f, k, trial.get("shift", 0.0), trial.get("zoom", 1.0)) if key in ("shift", "zoom") else target
                    apply_pose(trial)
                    sc = iou(render_mask(), tt)
                    if sc > best:
                        best, pose = sc, trial
                        target = tt
                        break
    return pose, float(best)


setup_camera()
DARK = load_sheet()
K_SHEET = sheet_scale(DARK)
anim = globals().get("ANIM", "idle")
FR = frames_of(anim, DARK)
try:
    data = json.load(open(OUT))
except Exception:
    data = {}
entry = data.setdefault(anim, {"frames": [None] * len(FR), "iou": [0.0] * len(FR)})
walk0 = json.load(open(ROOT + r"\ArtSource\ref\walk_poses.json"))["frames"][0]
start = dict(walk0, **globals().get("START", {}))
lift = {"jump": (-1.6, 0.45), "run": (-0.08, 0.45)}.get(anim, (-0.15, 0.4))  # the jump rises well above the ground


def swapped(p, pairs):
    q = dict(p)
    for a, b in pairs:
        q[a], q[b] = p.get(b, 0.0), p.get(a, 0.0)
    return q


LEGS = [("UpperLeg.L", "UpperLeg.R"), ("LowerLeg.L", "LowerLeg.R"), ("Foot.L", "Foot.R")]
ARMS = [("UpperArm.L", "UpperArm.R"), ("LowerArm.L", "LowerArm.R")]
for i in globals().get("FRAMES", range(len(FR))):
    prev = (entry["frames"][i - 1] if i > 0 and entry["frames"][i - 1] else None) or entry["frames"][i] or start
    if globals().get("RESTARTS", False):
        # the silhouette doesn't tell the legs (or the arms) apart: try the previous pose, and with them swapped
        cands = [prev, swapped(prev, LEGS), swapped(prev, ARMS), swapped(swapped(prev, LEGS), ARMS)]
        if entry["frames"][i]:
            cands.append(entry["frames"][i])
        best = None
        for c in cands:
            STEPS_SAVE = globals().get("STEPS", (8, 4, 2, 1))
            globals()["STEPS"] = (8, 4)
            pc, scc = fit_frame(FR[i], K_SHEET, c, lift)
            globals()["STEPS"] = STEPS_SAVE
            if best is None or scc > best[1]:
                best = (pc, scc)
        globals()["STEPS"] = (2, 1)
        pose, sc = fit_frame(FR[i], K_SHEET, best[0], lift)
        globals()["STEPS"] = STEPS_SAVE
    else:
        pose, sc = fit_frame(FR[i], K_SHEET, prev, lift)
    entry["frames"][i] = {kk: round(v, 3) for kk, v in pose.items()}
    entry["iou"][i] = round(sc, 4)
    json.dump(data, open(OUT, "w"), indent=1)
apply_pose({})
result = {"anim": anim, "frames": len(FR), "k": round(float(K_SHEET), 5), "iou": entry["iou"]}
