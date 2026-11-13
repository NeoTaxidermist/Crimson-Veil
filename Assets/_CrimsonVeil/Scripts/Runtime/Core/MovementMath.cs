using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// Pure helpers for designer-friendly platformer tuning.
    /// Jumps are authored as "height + time to apex" instead of raw gravity/velocity numbers.
    /// </summary>
    public static class MovementMath
    {
        /// <summary>Gravity (positive number) needed to reach <paramref name="height"/> in <paramref name="timeToApex"/>.</summary>
        public static float GravityForJump(float height, float timeToApex)
        {
            timeToApex = Mathf.Max(0.0001f, timeToApex);
            return 2f * height / (timeToApex * timeToApex);
        }

        /// <summary>Initial vertical velocity needed to reach <paramref name="height"/> in <paramref name="timeToApex"/>.</summary>
        public static float JumpVelocity(float height, float timeToApex)
        {
            timeToApex = Mathf.Max(0.0001f, timeToApex);
            return 2f * height / timeToApex;
        }

        /// <summary>Apex height of a ballistic jump with constant gravity.</summary>
        public static float ApexHeight(float jumpVelocity, float gravity)
        {
            return jumpVelocity * jumpVelocity / (2f * Mathf.Max(0.0001f, gravity));
        }

        /// <summary>Frame-rate independent exponential smoothing factor (0..1).</summary>
        public static float Damp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float EaseInOutSine(float t)
        {
            t = Mathf.Clamp01(t);
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        /// <summary>Ease-out that overshoots a little and settles (snappy, alive).</summary>
        public static float EaseOutBack(float t, float overshoot = 1.4f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
        }
    }
}
