using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// How flying things move, engine independent: wings beat with a quick power stroke down and a slower recovery,
    /// faster climbing and held open gliding down; the body pitches into its flight; a hanging lantern swings behind
    /// the way it is carried.
    /// </summary>
    public static class FlyerAnimation
    {
        /// <summary>Share of the wing cycle spent in the downstroke (the power stroke is quick).</summary>
        public const float DownstrokeShare = 0.38f;

        /// <summary>
        /// Wing position through a beat, <paramref name="phase"/> 0..1: 0 = up, 1 = fully down. The downstroke is
        /// quick (<see cref="DownstrokeShare"/> of the cycle), the recovery back up slower.
        /// </summary>
        public static float Stroke(float phase)
        {
            phase = Mathf.Repeat(phase, 1f);
            if (phase < DownstrokeShare) return Mathf.SmoothStep(0f, 1f, phase / DownstrokeShare);
            return 1f - Mathf.SmoothStep(0f, 1f, (phase - DownstrokeShare) / (1f - DownstrokeShare));
        }

        /// <summary>
        /// Beats per second for a flyer climbing (+) or sinking (-) at <paramref name="vy"/> m/s: it labours up and
        /// all but stops beating gliding down.
        /// </summary>
        public static float BeatRate(float baseRate, float vy)
            => baseRate * Mathf.Clamp(1f + vy * 0.18f, 0.35f, 1.7f);

        /// <summary>How wide the stroke is (0..1): full climbing or hovering, shallow gliding down.</summary>
        public static float StrokeWidth(float vy) => Mathf.Clamp(1f + vy * 0.15f, 0.3f, 1f);

        /// <summary>Degrees the body pitches nose-up (+) / down (-) flying at <paramref name="v"/> (along x it faces).</summary>
        public static float Pitch(Vector2 v, float max)
        {
            if (v.sqrMagnitude < 0.04f) return 0f;
            float a = Mathf.Atan2(v.y, Mathf.Abs(v.x)) * Mathf.Rad2Deg;
            // slow drifts pitch only a little: the lean grows with speed
            return Mathf.Clamp(a, -max, max) * Mathf.Clamp01(v.magnitude / 3f);
        }

        /// <summary>
        /// A lantern hanging from its ring: swings back against the way it is pushed (<paramref name="accelX"/> m/s²),
        /// pulled straight by gravity, damped. Angle in degrees (+ = bottom swung to -x).
        /// </summary>
        public static void Pendulum(ref float angle, ref float velocity, float accelX, float dt, float length = 0.35f)
        {
            float rad = angle * Mathf.Deg2Rad;
            float acc = (-9.81f * Mathf.Sin(rad) + accelX * Mathf.Cos(rad)) / length;   // carried: the bottom lags behind
            velocity += acc * Mathf.Rad2Deg * dt;
            velocity *= Mathf.Exp(-2.2f * dt);
            angle = Mathf.Clamp(angle + velocity * dt, -50f, 50f);
        }
    }
}
