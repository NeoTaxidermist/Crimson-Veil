# Turns the fitted walk-sheet poses (ArtSource/ref/walk_poses.json, from hero_fit_walk.py) into the idle-cycle table
# of the game (Assets/_CrimsonVeil/Scripts/Runtime/Player/HeroIdleCycle.cs). The sheet's legs keep one wide stance in
# all 12 frames while the arms and the head sway: it is the hero's standing loop.
# Run with any Python 3:  python ArtSource/walk_to_idle_table.py
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# The animation sheet's idle row (ArtSource/ref/anim_sheet.png, fitted by hero_fit_anim.py); the walk sheet's
# frames (walk_poses.json) if it hasn't been fitted.
_anim = os.path.join(ROOT, "ArtSource", "ref", "anim_poses.json")
if os.path.exists(_anim) and json.load(open(_anim)).get("idle"):
    frames = json.load(open(_anim))["idle"]["frames"]
    SOURCE = "ArtSource/ref/anim_sheet.png (idle row, 8 frames; fitted by ArtSource/hero_fit_anim.py)"
else:
    frames = json.load(open(os.path.join(ROOT, "ArtSource", "ref", "walk_poses.json")))["frames"]
    SOURCE = "ArtSource/ref/walk_cycle.png (12 frames; fitted by ArtSource/hero_fit_walk.py)"
n = len(frames)


def series(key):
    return [f.get(key, 0.0) for f in frames]


def smooth(v, passes=2):
    """Circular 1-2-1 smoothing (the loop wraps)."""
    for _ in range(passes):
        v = [(v[i - 1] + 2 * v[i] + v[(i + 1) % n]) / 4 for i in range(n)]
    return v


def mean(v):
    return sum(v) / len(v)


# Fitted angles: rotation about the rig's X. For a bone pointing down (legs, arms) + swings it back; for one pointing
# up (spine, neck) + tips it forward. HeroPose: + = towards the hero's front (thigh, upper arm, elbow, spine, head),
# knee + = bend, foot + = toes up. The game doesn't tip the hips, so the fitted hips' tilt is folded into the spine
# and the thighs.
hips = series("Hips")
cols = {
    "spine": smooth([x + hp for x, hp in zip(series("Spine"), hips)]),
    "head": smooth(series("Neck")),
    "bob": smooth([-x for x in series("drop")]),
    # the legs hold one stance: their mean (the frame-to-frame jitter is fitting noise)
    "thighL": [-mean([x + hp for x, hp in zip(series("UpperLeg.L"), hips)])] * n,
    "kneeL": [max(0.0, mean(series("LowerLeg.L")))] * n,
    "footL": None,  # set below: flat on the ground
    "thighR": [-mean([x + hp for x, hp in zip(series("UpperLeg.R"), hips)])] * n,
    "kneeR": [max(0.0, mean(series("LowerLeg.R")))] * n,
    "footR": None,
    "armL": smooth([-x for x in series("UpperArm.L")], 3),
    "elbowL": smooth([8.0 + max(0.0, -x) for x in series("LowerArm.L")], 3),  # the sheet's arms hang a little bent
    "armR": smooth([-x for x in series("UpperArm.R")], 3),
    "elbowR": smooth([8.0 + max(0.0, -x) for x in series("LowerArm.R")], 3),
}
# Feet flat on the ground: the fitted foot angles only chased the outline's noise, so the foot turns against the
# shin's lean instead (shin lean forward = thigh - knee).
cols["footL"] = [-(cols["thighL"][i] - cols["kneeL"][i]) for i in range(n)]
cols["footR"] = [-(cols["thighR"][i] - cols["kneeR"][i]) for i in range(n)]
order = ["spine", "head", "bob", "thighL", "kneeL", "footL", "thighR", "kneeR", "footR", "armL", "elbowL", "armR", "elbowR"]
rows = []
for i in range(n):
    rows.append("            new HeroPose { " + ", ".join(
        "%s = %sf" % (k, ("%.4f" % cols[k][i]) if k == "bob" else ("%.1f" % cols[k][i])) for k in order) + " },")

cs = '''using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's standing loop, from %s: the rig was fitted to each side-view silhouette and the angles tabulated
    /// (ArtSource/walk_to_idle_table.py - generated, do not edit by hand). He stands in the concept's wide stance - the
    /// near leg forward with the knee eased, the far leg back and straight - and his arms and head sway gently.
    /// </summary>
    public static class HeroIdleCycle
    {
        /// <summary>Seconds for one loop of the 12 frames.</summary>
        public const float Duration = 3.2f;

        public static readonly HeroPose[] Frames =
        {
%s
        };

        /// <summary>The pose at <paramref name="time"/> seconds into the loop (frames blended smoothly, the loop wraps).</summary>
        public static HeroPose Sample(float time)
        {
            int n = Frames.Length;
            float f = Mathf.Repeat(time / Duration, 1f) * n;
            int i = Mathf.FloorToInt(f) %% n;
            float u = f - Mathf.Floor(f);
            u = u * u * (3f - 2f * u);
            return HeroPose.Lerp(Frames[i], Frames[(i + 1) %% n], u);
        }
    }
}
''' % (SOURCE, "\n".join(rows))
out = os.path.join(ROOT, "Assets", "_CrimsonVeil", "Scripts", "Runtime", "Player", "HeroIdleCycle.cs")
open(out, "w", encoding="utf-8").write(cs)
print("wrote", out)
for k in order:
    print(k.ljust(7), " ".join("%6.1f" % (x * (100 if k == "bob" else 1)) for x in cols[k]))
