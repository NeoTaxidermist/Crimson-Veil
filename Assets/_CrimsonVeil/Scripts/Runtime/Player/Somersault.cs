using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The somersault of the double jump: the body tucks and turns once, head first, round its middle, and the
    /// scarf wraps round it (see <c>VeilController</c>). Pure functions of the flip progress 0..1 (-1 = none).
    /// </summary>
    public static class Somersault
    {
        public const float Duration = 0.5f;
        /// <summary>Height above the feet the body turns round (hips).</summary>
        public const float Pivot = 0.95f;

        /// <summary>Strictly inside the turn (0 = a default/unset value counts as not flipping).</summary>
        public static bool Active(float progress) => progress > 0f && progress < 1f;

        /// <summary>
        /// Roll (degrees about the view axis): one full forward turn — quick through the middle, settling at
        /// the end so the hero lands upright. Facing right a forward flip turns clockwise (negative).
        /// </summary>
        public static float Angle(float progress, int facing)
        {
            if (!Active(progress)) return 0f;
            float t = progress;
            float e = t * t * t * (t * (t * 6f - 15f) + 10f); // smootherstep
            return -Mathf.Sign(facing == 0 ? 1 : facing) * 360f * e;
        }

        /// <summary>0..1 how tightly the body is tucked (knees to the chest through the middle of the turn).</summary>
        public static float Tuck(float progress)
        {
            if (!Active(progress)) return 0f;
            return Mathf.Sin(Mathf.PI * Mathf.SmoothStep(0f, 1f, progress));
        }

        /// <summary>0..1 how strongly the scarf is wound round the body (wraps in fast, unwinds slower).</summary>
        public static float Wrap(float progress, float sinceEnd)
        {
            if (progress >= 0f && progress < 1f) return Mathf.Clamp01(progress / 0.12f);
            if (sinceEnd < 0f) return 0f;
            return 1f - Mathf.Clamp01(sinceEnd / 0.35f);
        }
    }
}
