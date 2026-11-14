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

        /// <summary>How much faster than average the turn runs through its middle (and slower at its ends).</summary>
        const float Surge = 0.6f;

        static float Sign(int facing) => facing < 0 ? -1f : 1f;

        /// <summary>
        /// Roll (degrees about the view axis): one full forward turn that flows — never stopped, a little quicker
        /// through the middle, easing out so the hero lands upright. Facing right a forward flip turns clockwise.
        /// </summary>
        public static float Angle(float progress, int facing)
        {
            if (!Active(progress)) return 0f;
            float t = progress;
            float e = t - Surge * Mathf.Sin(2f * Mathf.PI * t) / (2f * Mathf.PI);
            return -Sign(facing) * 360f * e;
        }

        /// <summary>Angular velocity of the turn (rad/s, + = anticlockwise) — the scarf whirls round with it.</summary>
        public static float AngularVelocity(float progress, int facing)
        {
            if (!Active(progress)) return 0f;
            float rate = (1f - Surge * Mathf.Cos(2f * Mathf.PI * progress)) * 2f * Mathf.PI / Duration;
            return -Sign(facing) * rate;
        }

        /// <summary>0..1 how tightly the body is tucked (knees to the chest through the middle of the turn).</summary>
        public static float Tuck(float progress)
        {
            if (!Active(progress)) return 0f;
            return Mathf.Sin(Mathf.PI * Mathf.SmoothStep(0f, 1f, progress));
        }

        /// <summary>0..1 how strongly the scarf is wound round the body: eases in with the turn, lets go gently.</summary>
        public static float Wrap(float progress, float sinceEnd)
        {
            if (progress >= 0f && progress < 1f) return Mathf.SmoothStep(0f, 1f, progress / 0.25f);
            if (sinceEnd < 0f) return 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, sinceEnd / 0.45f);
        }
    }
}
