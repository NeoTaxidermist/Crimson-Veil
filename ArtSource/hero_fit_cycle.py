# Fits ONE run cycle to all 12 run frames of the animation sheet at once (ArtSource/ref/anim_sheet.png).
# A frame-by-frame fit can't tell the legs (or the arms) apart in a silhouette and drifts; here the cycle is a few
# parameters - lean, stride, knee lift, foot, arm swing, elbow, bounce, the sheet's scale - with the legs and arms
# alternating by construction, and the parameters are searched to make the model overlap all 12 frames the most.
# Writes the parameters and the per-frame poses to ArtSource/ref/anim_poses.json ("run_cycle", "run").
# Run in Blender with Hero.blend open (it takes a few minutes; it keeps saving its progress).
import json
import math
import bpy
import numpy as np

src = open(r"C:\Dev\CrimsonVeil\ArtSource\hero_fit_anim.py", encoding="utf-8").read()
exec(src[:src.index("setup_camera()\nDARK")])
setup_camera()
DARK = load_sheet()
K_SHEET = sheet_scale(DARK)
FR = frames_of("run", DARK)
N = len(FR)

P0 = dict(phase=0.0, stride=40.0, thigh0=-10.0, knee0=35.0, knee1=60.0, knee_ph=1.2, foot=20.0, arm=40.0,
          elbow=-85.0, lean=22.0, neck=6.0, drop0=0.12, drop1=0.03, zoom=0.8, shift=0.02)
STEP = dict(phase=0.04, stride=6, thigh0=5, knee0=8, knee1=10, knee_ph=0.3, foot=8, arm=8, elbow=10, lean=4,
            neck=4, drop0=0.03, drop1=0.015, zoom=0.03, shift=0.01)
RANGE = dict(phase=(-1, 2), stride=(10, 75), thigh0=(-40, 25), knee0=(0, 90), knee1=(0, 110), knee_ph=(-3.2, 3.2),
             foot=(-30, 45), arm=(0, 80), elbow=(-130, 0), lean=(0, 45), neck=(-25, 30), drop0=(-0.05, 0.35),
             drop1=(-0.1, 0.1), zoom=(0.6, 1.1), shift=(-0.25, 0.25))


def pose_at(P, i):
    """Rig angles (fit convention: + swings back) for frame i of the cycle. Facing right, L leads at phase 0."""
    a = 2 * math.pi * (i / N + P["phase"])
    def leg(sh):
        s = math.sin(a + sh)
        thigh = -(P["stride"] * s) + P["thigh0"]                     # forward = negative
        knee = P["knee0"] + P["knee1"] * (0.5 + 0.5 * math.cos(a + sh + P["knee_ph"]))
        return thigh, max(0.0, knee)
    tl, kl = leg(0.0)
    tr, kr = leg(math.pi)
    return {"Hips": P["lean"], "Spine": 0.0, "Neck": P["neck"],
            "UpperLeg.L": tl, "LowerLeg.L": kl, "Foot.L": P["foot"],
            "UpperLeg.R": tr, "LowerLeg.R": kr, "Foot.R": P["foot"],
            "UpperArm.L": P["arm"] * math.sin(a), "LowerArm.L": P["elbow"],      # arm swings against its leg
            "UpperArm.R": P["arm"] * math.sin(a + math.pi), "LowerArm.R": P["elbow"],
            "drop": P["drop0"] + P["drop1"] * math.cos(2 * a), "shift": P["shift"], "zoom": P["zoom"]}


def score(P):
    tot = 0.0
    for i, f in enumerate(FR):
        pose = pose_at(P, i)
        apply_pose(pose)
        tot += iou(render_mask(), target_for(f, K_SHEET, pose["shift"], pose["zoom"]))
    return tot / N


OUTP = OUT
try:
    data = json.load(open(OUTP))
except Exception:
    data = {}
P = dict(data.get("run_cycle", {}).get("params", P0))
best = score(P)
for scale in globals().get("SCALES", (1.0, 0.5, 0.25)):
    for _ in range(2):
        improved = False
        for key in P0:
            for sgn in (1, -1):
                trial = dict(P)
                trial[key] = P[key] + sgn * STEP[key] * scale
                lo, hi = RANGE[key]
                if not lo <= trial[key] <= hi:
                    continue
                sc = score(trial)
                if sc > best:
                    best, P, improved = sc, trial, True
                    data["run_cycle"] = {"params": P, "iou": round(best, 4)}
                    json.dump(data, open(OUTP, "w"), indent=1)
                    break
        if not improved:
            break
data["run_cycle"] = {"params": P, "iou": round(best, 4)}
data["run"] = {"frames": [{k: round(v, 3) for k, v in pose_at(P, i).items()} for i in range(N)],
               "iou": [round(float(iou((apply_pose(pose_at(P, i)), render_mask())[1],
                                       target_for(FR[i], K_SHEET, P["shift"], P["zoom"]))), 4) for i in range(N)]}
json.dump(data, open(OUTP, "w"), indent=1)
apply_pose({})
result = {"iou": round(best, 4), "params": {k: round(v, 3) for k, v in P.items()}}
