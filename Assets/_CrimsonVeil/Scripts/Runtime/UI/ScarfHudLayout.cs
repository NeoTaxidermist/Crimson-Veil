using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// Pure layout of the health scarf: one piece of crimson ribbon per point of life, cut apart by small
    /// torn gaps, the last one partly burnt when the health is fractional. Positions are 0..1 along the ribbon.
    /// </summary>
    public static class ScarfHudLayout
    {
        public struct Piece
        {
            /// <summary>Start / end of the piece along the ribbon (0..1).</summary>
            public float start, end;
            /// <summary>How much of the piece is still alive (0 = burnt to ash, 1 = whole).</summary>
            public float fill;
            /// <summary>End of the living part (start .. end).</summary>
            public float FillEnd => Mathf.Lerp(start, end, Mathf.Clamp01(fill));
        }

        /// <summary>Pieces for <paramref name="maxHealth"/> points of life with <paramref name="health"/> left.</summary>
        public static List<Piece> Pieces(float maxHealth, float health, float gap = 0.012f)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
            float h = Mathf.Clamp(health, 0f, n);
            var list = new List<Piece>(n);
            float width = (1f - gap * (n - 1)) / n;
            for (int i = 0; i < n; i++)
            {
                float s = i * (width + gap);
                list.Add(new Piece { start = s, end = s + width, fill = Mathf.Clamp01(h - i) });
            }
            return list;
        }

        /// <summary>Where along the ribbon the living part ends (0..1) — the burning tip.</summary>
        public static float LivingEnd(float maxHealth, float health, float gap = 0.012f)
        {
            float end = 0f;
            foreach (var p in Pieces(maxHealth, health, gap))
                if (p.fill > 0f) end = p.FillEnd;
            return end;
        }

        /// <summary>The ribbon flutters like the scarf in the wind: more towards its loose end.</summary>
        public static float Wave(float u, float time, float amplitude)
            => Mathf.Sin(u * 7.5f - time * 3.1f) * amplitude * Mathf.SmoothStep(0.05f, 1f, u)
               + Mathf.Sin(u * 15f - time * 5.3f) * amplitude * 0.25f * u;

        /// <summary>Half-thickness along the ribbon: snug at the knot, fuller, then tapering.</summary>
        public static float HalfWidth(float u, float thickness)
            => thickness * 0.5f * Mathf.Lerp(0.75f, 1f, Mathf.SmoothStep(0f, 1f, u * 6f)) * Mathf.Lerp(1f, 0.6f, u * u);
    }
}
