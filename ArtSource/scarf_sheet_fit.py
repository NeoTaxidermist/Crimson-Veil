# Reads the scarf's animation sheet (ArtSource/ref/scarf_anim_sheet.png: 12 side-view frames of a seamless loop, the
# hero standing, the scarf streaming back in an S) and writes the game's table of its centre line
# (Assets/_CrimsonVeil/Scripts/Runtime/Veil/VeilRestShape.cs - generated, do not edit by hand).
# In every frame the crimson is masked; its weighted centre in each pixel column gives the centre line, measured
# from the neck in metres (the hero is ~170 px tall = 1.9 m with hair), smoothed along the scarf and sampled
# every 0.2 m. Run in Blender (numpy + image loading):
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\scarf_sheet_fit.py").read())
import bpy
import numpy as np

ROOT = r"C:\Dev\CrimsonVeil"
SHEET = ROOT + r"\ArtSource\ref\scarf_anim_sheet.png"
ROWS = [(200, 412), (428, 642), (655, 872)]             # the three rows of frames
COLS = [(5, 440), (448, 884), (892, 1328), (1336, 1772)]  # the four columns
NECK = (352.0, 66.0)                                     # the scarf's root (frame pixels): the hero's neck
K = 1.9 / 170.0                                          # metres per pixel
GRID = np.round(np.arange(0.0, 3.21, 0.2), 2)            # along the scarf, from the neck (m)

im = bpy.data.images.load(SHEET, check_existing=True)
w, h = im.size
px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1]
redness = np.clip(px[..., 0] - np.maximum(px[..., 1], px[..., 2]), 0, 1)
table = []
for r0, r1 in ROWS:
    for c0, c1 in COLS:
        R = redness[r0:r1, c0:c1]
        pts = []
        for x in range(0, int(NECK[0])):
            col = R[:, x]
            m = col > 0.25
            if m.sum() >= 2:
                ys = np.nonzero(m)[0]
                pts.append(((NECK[0] - x) * K, (NECK[1] - float((ys * col[m]).sum() / col[m].sum())) * K))
        pts.sort()
        d = np.array([p[0] for p in pts]); y = np.array([p[1] for p in pts])
        ys = np.array([np.average(y, weights=np.exp(-((d - di) / 0.12) ** 2)) for di in d])
        table.append([0.0] + [round(float(np.interp(g, d, ys)), 3) for g in GRID[1:]])

rows = "\n".join("            { " + ", ".join("%.3ff" % v for v in r) + " }," for r in table)
cs = '''using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// The loose scarf's shape at rest, frame by frame, from its animation sheet (ArtSource/ref/scarf_anim_sheet.png,
    /// 12 frames of a seamless loop; measured by ArtSource/scarf_sheet_fit.py - generated, do not edit by hand): the
    /// height (m, + up) of its centre line below the neck, every <see cref="Step"/> m back from the neck. It streams
    /// back level in an S - down to ~0.4 m below the neck at 1.2 m, up to ~0.1 m at 2 m, down again past 2.6 m -
    /// and breathes gently through the loop.
    /// </summary>
    public static class VeilRestShape
    {
        /// <summary>Seconds for one loop of the sheet's frames.</summary>
        public const float Duration = 2.4f;
        /// <summary>Spacing (m) of the samples along the scarf.</summary>
        public const float Step = 0.2f;

        public static readonly float[,] Heights =
        {
%s
        };

        public static int Frames => Heights.GetLength(0);
        public static int Samples => Heights.GetLength(1);
        /// <summary>Metres from the neck the table covers.</summary>
        public static float Reach => Step * (Samples - 1);

        /// <summary>Height (m) of the centre line <paramref name="d"/> m back from the neck at <paramref name="time"/> s
        /// (smoothly between frames and samples; beyond the table it keeps its last height).</summary>
        public static float Height(float d, float time)
        {
            int n = Frames;
            float f = Mathf.Repeat(time / Duration, 1f) * n;
            int i0 = Mathf.FloorToInt(f) %% n, i1 = (i0 + 1) %% n;
            float u = f - Mathf.Floor(f);
            u = u * u * (3f - 2f * u);
            return Mathf.Lerp(At(i0, d), At(i1, d), u);
        }

        static float At(int frame, float d)
        {
            float s = Mathf.Clamp(d / Step, 0f, Samples - 1);
            int j = Mathf.Min(Mathf.FloorToInt(s), Samples - 2);
            float v = s - j;
            return Mathf.Lerp(Heights[frame, j], Heights[frame, j + 1], v);
        }
    }
}
''' % rows
out = ROOT + r"\Assets\_CrimsonVeil\Scripts\Runtime\Veil\VeilRestShape.cs"
open(out, "w", encoding="utf-8").write(cs)
result = {"written": out, "mean": [round(float(x), 2) for x in np.array(table).mean(0)]}
