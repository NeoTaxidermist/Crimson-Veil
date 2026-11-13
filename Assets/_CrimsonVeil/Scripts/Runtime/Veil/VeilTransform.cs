using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Choreography of the veil changing shape: hardening travels as a wave from the neck to the tip
    /// (and releases tip-first), and shapes morph node by node with a winding swirl in between.
    /// </summary>
    public static class VeilTransform
    {
        /// <summary>How far ahead the neck is of the tip, 0 = all at once.</summary>
        public const float WaveSpread = 0.9f;

        /// <summary>Hardness factor (0..1) of a node at fraction <paramref name="f"/> for overall progress <paramref name="p"/>.</summary>
        public static float Wave(float p, float f)
        {
            float x = Mathf.Clamp01(p) * (1f + WaveSpread) - Mathf.Clamp01(f) * WaveSpread;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
        }

        /// <summary>The last stretch of a weapon stays a little soft so the tip keeps fluttering.</summary>
        public static float TipSoftness(float f) => Mathf.Lerp(1f, 0.72f, Mathf.SmoothStep(0f, 1f, (f - 0.82f) / 0.18f));

        /// <summary>Per-node morph progress: the neck leads, the tip follows.</summary>
        public static float MorphProgress(float morph, float f)
        {
            float x = Mathf.Clamp01(morph) * 1.6f - Mathf.Clamp01(f) * 0.6f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
        }

        /// <summary>
        /// Turn (degrees) of the cloth around the palm while it winds into a weapon: zero at the start (it leaves
        /// the scarf where it is), strongest mid-way, zero once the weapon has formed — a vortex sucked into the hand.
        /// </summary>
        public static float SpiralIn(float m)
        {
            m = Mathf.Clamp01(m);
            return 320f * Mathf.Sin(m * Mathf.PI) * (1f - m);
        }

        /// <summary>Share of the spiral turn a point at <paramref name="distance"/> (m) from the palm gets.</summary>
        public static float VortexFalloff(float distance) => 1f / (1f + 2.5f * distance * distance);

        /// <summary>
        /// Position of a node while morphing from <paramref name="from"/> to <paramref name="to"/>: it travels
        /// along a lazy curl (strongest mid-morph) so the cloth winds into its new shape.
        /// </summary>
        public static Vector2 MorphNode(Vector2 from, Vector2 to, float m, float f, float time, float swirl = 0.14f)
        {
            Vector2 p = Vector2.Lerp(from, to, m);
            Vector2 d = to - from;
            Vector2 perp = d.sqrMagnitude > 1e-6f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
            float curl = Mathf.Sin(m * Mathf.PI) * Mathf.Sin(f * 11f - time * 9f) * swirl;
            return p + perp * curl;
        }
    }
}
