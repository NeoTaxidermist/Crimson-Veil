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

        /// <summary>Morph time of a form: the big shapes (the shield's ring, the bow) need a few frames more to
        /// visibly form out of the scarf; the hardening (and so the gameplay) keeps its own pace.</summary>
        public static float FormMorphTime(VeilForm form, float baseTime)
        {
            switch (form)
            {
                case VeilForm.Shield: return Mathf.Max(baseTime, 0.3f);
                case VeilForm.Bow: return Mathf.Max(baseTime, 0.24f);
                default: return baseTime;
            }
        }

        /// <summary>Seconds the cloth is led from a released weapon's shape back into the streaming scarf.</summary>
        public const float MeltGuideTime = 0.5f;

        /// <summary>Per-node progress of the melt (0..1 overall): the neck lets go first, the tip follows.</summary>
        public static float MeltProgress(float melt, float f)
        {
            float x = Mathf.Clamp01(melt) * 1.7f - Mathf.Clamp01(f) * 0.7f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
        }

        /// <summary>How firmly a melting node is led along its path: fully until it nearly reaches the scarf, then
        /// handed over to the cloth (which is pulled to the same shape), so nothing jumps or bunches up.</summary>
        public static float MeltGuide(float m) => 1f - Mathf.SmoothStep(0.7f, 1f, m);

        /// <summary>The streaming scarf's centre line in facing-normalised local space (behind the hero = -x),
        /// as the cloth is pulled to it (see VeilSimulation.RestReach and VeilRestShape).</summary>
        public static Vector2 ClothRestPoint(float f, float length, float time)
        {
            float d = Mathf.Clamp01(f) * length;
            return new Vector2(-d * VeilSimulation.RestReach, VeilRestShape.Height(d, time));
        }

        /// <summary>
        /// The melting weapon as a chain unrolling into the scarf: every segment turns from its direction in the
        /// weapon to its direction in the streaming scarf (neck first) and keeps its length in between, and the
        /// chain is laid out from the neck. Nothing is squeezed together, so the cloth can't crumple into a lump.
        /// <paramref name="from"/> is the weapon's shape, <paramref name="pts"/> receives the path (local space).
        /// </summary>
        public static void MeltChain(Vector2[] from, float melt, float length, float time, Vector2[] pts, float[] guide)
        {
            int n = from.Length;
            Vector2 r0 = ClothRestPoint(0f, length, time);
            float m0 = MeltProgress(melt, 0f);
            pts[0] = Vector2.Lerp(from[0], r0, m0);
            guide[0] = 0f;
            float prevF = 0f, prevR = 0f, offset = 0f;
            Vector2 rPrev = r0;
            for (int i = 0; i < n - 1; i++)
            {
                float fa = (float)i / (n - 1), fb = (float)(i + 1) / (n - 1);
                Vector2 df = from[i + 1] - from[i];
                Vector2 rNext = ClothRestPoint(fb, length, time);
                Vector2 dr = rNext - rPrev;
                rPrev = rNext;
                float af = df.sqrMagnitude > 1e-8f ? Mathf.Atan2(df.y, df.x) * Mathf.Rad2Deg : prevF;
                float ar = Mathf.Atan2(dr.y, dr.x) * Mathf.Rad2Deg;
                // unwrap both along the chain, so a ring unwinds turn by turn instead of flipping
                af = i == 0 ? af : prevF + Mathf.DeltaAngle(prevF, af);
                ar = i == 0 ? ar : prevR + Mathf.DeltaAngle(prevR, ar);
                if (i == 0) offset = 360f * Mathf.Round((af - ar) / 360f); // start the scarf's angles nearest the weapon's
                prevF = af; prevR = ar;
                float m = MeltProgress(melt, 0.5f * (fa + fb));
                float ang = Mathf.Lerp(af, ar + offset, m) * Mathf.Deg2Rad;
                float len = Mathf.Lerp(df.magnitude, dr.magnitude, m);
                pts[i + 1] = pts[i] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * len;
                guide[i + 1] = MeltGuide(MeltProgress(melt, fb));
            }
        }

        /// <summary>
        /// The shield ring unfurls from the hand like a thrown loop: at <paramref name="s"/> = 0 the whole ring is
        /// gathered at the palm, at 1 it is closed round the body. <paramref name="points"/>[from..] is the ring
        /// (in order along it); it is swept in place round <paramref name="center"/>.
        /// </summary>
        public static void SweepRing(Vector2[] points, int from, Vector2 center, Vector2 grip, float s)
        {
            s = Mathf.Clamp01(s);
            if (s >= 1f || from < 0 || from >= points.Length) return;
            Vector2 g = grip - center;
            float a0 = Mathf.Atan2(g.y, g.x) * Mathf.Rad2Deg, r0 = g.magnitude;
            float along = 0f, prev = a0;
            for (int i = from; i < points.Length; i++)
            {
                Vector2 p = points[i] - center;
                float a = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                along += Mathf.DeltaAngle(prev, a); // unwrapped angle along the ring from the hand
                prev = a;
                float r = Mathf.Lerp(r0, p.magnitude, s);
                float ang = (a0 + along * s) * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
        }
    }
}
