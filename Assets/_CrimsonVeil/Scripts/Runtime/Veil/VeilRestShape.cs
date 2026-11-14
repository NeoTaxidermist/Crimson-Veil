using UnityEngine;

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
            { 0.000f, -0.111f, -0.126f, -0.111f, -0.217f, -0.338f, -0.376f, -0.341f, -0.217f, -0.071f, -0.053f, -0.074f, -0.175f, -0.317f, -0.390f, -0.437f, -0.333f },
            { 0.000f, -0.111f, -0.123f, -0.106f, -0.203f, -0.321f, -0.401f, -0.389f, -0.291f, -0.140f, -0.073f, -0.063f, -0.175f, -0.319f, -0.375f, -0.353f, -0.308f },
            { 0.000f, -0.110f, -0.114f, -0.101f, -0.202f, -0.308f, -0.383f, -0.383f, -0.305f, -0.149f, -0.112f, -0.118f, -0.211f, -0.379f, -0.455f, -0.494f, -0.421f },
            { 0.000f, -0.109f, -0.117f, -0.100f, -0.200f, -0.310f, -0.378f, -0.373f, -0.280f, -0.186f, -0.123f, -0.152f, -0.291f, -0.421f, -0.488f, -0.472f, -0.361f },
            { 0.000f, -0.105f, -0.101f, -0.088f, -0.192f, -0.313f, -0.378f, -0.365f, -0.273f, -0.214f, -0.172f, -0.173f, -0.267f, -0.415f, -0.483f, -0.423f, -0.324f },
            { 0.000f, -0.091f, -0.071f, -0.080f, -0.212f, -0.341f, -0.382f, -0.333f, -0.249f, -0.190f, -0.153f, -0.161f, -0.284f, -0.427f, -0.518f, -0.370f, -0.241f },
            { 0.000f, -0.090f, -0.065f, -0.068f, -0.208f, -0.345f, -0.378f, -0.334f, -0.250f, -0.171f, -0.108f, -0.094f, -0.224f, -0.418f, -0.478f, -0.395f, -0.283f },
            { 0.000f, -0.085f, -0.061f, -0.071f, -0.214f, -0.360f, -0.395f, -0.346f, -0.279f, -0.209f, -0.106f, -0.099f, -0.278f, -0.423f, -0.479f, -0.331f, -0.210f },
            { 0.000f, -0.112f, -0.083f, -0.095f, -0.237f, -0.355f, -0.393f, -0.369f, -0.285f, -0.209f, -0.091f, -0.094f, -0.278f, -0.379f, -0.450f, -0.350f, -0.229f },
            { 0.000f, -0.108f, -0.090f, -0.098f, -0.211f, -0.340f, -0.389f, -0.357f, -0.277f, -0.173f, -0.045f, -0.048f, -0.238f, -0.356f, -0.517f, -0.485f, -0.218f },
            { 0.000f, -0.130f, -0.137f, -0.099f, -0.173f, -0.327f, -0.386f, -0.370f, -0.314f, -0.221f, -0.131f, -0.003f, -0.075f, -0.252f, -0.425f, -0.564f, -0.573f },
            { 0.000f, -0.136f, -0.139f, -0.087f, -0.143f, -0.291f, -0.389f, -0.402f, -0.360f, -0.264f, -0.132f, 0.005f, -0.027f, -0.150f, -0.321f, -0.453f, -0.528f },
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
            int i0 = Mathf.FloorToInt(f) % n, i1 = (i0 + 1) % n;
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
