# Turns the fitted walk-sheet poses (ArtSource/ref/walk_poses.json, from hero_fit_walk.py) into the idle-cycle table
# of the game (Assets/_CrimsonVeil/Scripts/Runtime/Player/HeroIdleCycle.cs). The sheet's legs keep one wide stance in
# all 12 frames while the arms and the head sway: it is the hero's standing loop.
# Run with any Python 3:  python ArtSource/walk_to_idle_table.py
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
data = json.load(open(os.path.join(ROOT, "ArtSource", "ref", "walk_poses.json")))
frames = data["frames"]
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


# Fitted angles: rotation about the rig's X, + = the bone swings back. HeroPose: + = towards the hero's front
# (thigh, upper arm, elbow, spine, head), knee + = bend, foot + = toes up.
cols = {
    "spine": smooth([-x for x in series("Spine")]),
    "head": smooth([-x for x in series("Neck")]),
    "bob": smooth([-x for x in series("drop")]),
    # the legs hold one stance: their mean (the frame-to-frame jitter is fitting noise)
    "thighL": [-mean(series("UpperLeg.L"))] * n,
    "kneeL": [max(0.0, mean(series("LowerLeg.L")))] * n,
    "footL": None,  # set below: flat on the ground
    "thighR": [-mean(series("UpperLeg.R"))] * n,
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
    /// The hero's standing loop, from the walk-cycle sheet (ArtSource/ref/walk_cycle.png): the rig was fitted to each
    /// of its 12 side-view silhouettes (ArtSource/hero_fit_walk.py) and the angles tabulated
    /// (ArtSource/walk_to_idle_table.py - generated, do not edit by hand). He stands in the concept's wide stance - the
    /// near leg forward with the knee eased, the far leg back and straight - and his arms and head sway gently.
    /// </summary>
    public static class HeroIdleCycle
    {
        /// <summary>Seconds for one loop of the 12 frames.</summary>
        public const float Duration = 3.6f;

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
''' % "\n".join(rows)
out = os.path.join(ROOT, "Assets", "_CrimsonVeil", "Scripts", "Runtime", "Player", "HeroIdleCycle.cs")
open(out, "w", encoding="utf-8").write(cs)
print("wrote", out)
for k in order:
    print(k.ljust(7), " ".join("%6.1f" % (x * (100 if k == "bob" else 1)) for x in cols[k]))
