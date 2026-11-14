using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The life gauge, drawn like the hero's scarf: a crimson ribbon tied in a knot at the top-left corner,
    /// fluttering in the wind, streaked like a flame. One torn piece per point of life; lost pieces are left
    /// as grey ash so the player always sees how much there was. The living end burns with a small flame
    /// tongue. Underneath runs a thin crimson thread — the scarf's strength for casting (glinting when full:
    /// the scarf will take the next blow). Built procedurally (no textures), animated every frame.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ScarfHealthBar : MaskableGraphic
    {
        [Tooltip("Points of life (one ribbon piece each).")]
        public float maxHealth = 5f;
        [Tooltip("Life left (fractions burn part of a piece).")]
        public float health = 5f;
        [Tooltip("The scarf's strength for casting, 0..1 (the thread under the ribbon).")]
        [Range(0f, 1f)] public float charge = 1f;
        [Tooltip("0..1 flash of a fresh wound.")]
        [Range(0f, 1f)] public float hurtFlash;
        /// <summary>Health a moment ago — the burnt-away part glows on as embers for a while.</summary>
        public float trailingHealth = 5f;

        public float thickness = 24f;
        public float waveAmplitude = 7f;

        static readonly Color Deep = new Color(0.32f, 0.0f, 0.02f, 1f);
        static readonly Color Mid = new Color(0.78f, 0.03f, 0.05f, 1f);
        static readonly Color Glow = new Color(1.0f, 0.22f, 0.12f, 1f);
        static readonly Color Ash = new Color(0.36f, 0.34f, 0.34f, 0.75f); // lost life: visible on black rock too
        static readonly Color Ink = new Color(0f, 0f, 0f, 0.45f);

        float time;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float x0 = r.xMin + 46f, x1 = r.xMax - 22f, y = r.yMax - 36f;
            float len = x1 - x0;
            float lowPulse = health <= 1.01f && health > 0f ? 0.5f + 0.5f * Mathf.Sin(time * 7f) : 0f;

            // Ink smear behind everything, so the gauge reads on the pale mist as well as on black rock.
            Ribbon(vh, x0 - 30f, x1 + 10f, y, 0f, 1f, thickness * 2.2f, waveAmplitude * 0.4f, (u, a) => Ink, 22);

            // One piece per point of life: ash where it burnt away, embers where it just did, crimson flame where alive.
            var pieces = ScarfHudLayout.Pieces(maxHealth, health);
            var trail = ScarfHudLayout.Pieces(maxHealth, Mathf.Max(trailingHealth, health));
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                float liveEnd = p.FillEnd, trailEnd = trail[i].FillEnd;
                if (liveEnd < p.end - 1e-3f)
                    Ribbon(vh, x0, x1, y, liveEnd, p.end, thickness * 0.9f, waveAmplitude, (u, a) => Ash, 10);
                if (trailEnd > liveEnd + 1e-3f)
                {
                    float ember = Mathf.Clamp01(trailingHealth - health);
                    Ribbon(vh, x0, x1, y, liveEnd, trailEnd, thickness, waveAmplitude,
                        (u, a) => Color.Lerp(Ash, Glow, 0.4f + 0.6f * hurtFlash) * new Color(1f, 1f, 1f, 0.5f + 0.5f * ember), 10);
                }
                if (p.fill > 0f)
                    Ribbon(vh, x0, x1, y, p.start, liveEnd, thickness, waveAmplitude, (u, a) => Flame(u, a, lowPulse), 16);
            }

            // The burning tip of the living part: a flickering flame tongue.
            float tip = ScarfHudLayout.LivingEnd(maxHealth, health);
            if (tip > 0f) Tongue(vh, x0 + tip * len, y + ScarfHudLayout.Wave(tip, time, waveAmplitude), thickness, lowPulse);

            // The knot at the corner, with two short tails (the scarf tied at the neck).
            Knot(vh, new Vector2(r.xMin + 26f, y + 2f), 13f);

            // The scarf's strength: a thin thread under the ribbon.
            float ty = y - thickness * 0.95f;
            Ribbon(vh, x0, x1, ty, 0f, 0.85f, 3f, waveAmplitude * 0.35f, (u, a) => new Color(0.15f, 0.12f, 0.12f, 0.55f), 24);
            if (charge > 0.005f)
                Ribbon(vh, x0, x1, ty, 0f, 0.85f * Mathf.Clamp01(charge), 4.2f, waveAmplitude * 0.35f,
                    (u, a) => Color.Lerp(Mid, Glow, 0.35f + 0.25f * Mathf.Sin(u * 30f - time * 4f)), 24);
            if (charge >= 0.999f)
            {
                // Full: a glint — the scarf will take the next blow for the hero.
                float gx = x0 + 0.85f * len, gy = ty + ScarfHudLayout.Wave(0.85f, time, waveAmplitude * 0.35f);
                float s = 5f + 2f * Mathf.Sin(time * 5f);
                Diamond(vh, new Vector2(gx, gy), s, new Color(1f, 0.75f, 0.7f, 0.9f));
            }
        }

        Color Flame(float u, float across, float lowPulse)
        {
            // Flame streaks running along the ribbon, brighter at the core, hotter towards the burning end.
            float streak = 0.5f + 0.5f * Mathf.Sin(u * 46f - time * 6.5f + across * 2.2f + Mathf.Sin(u * 9f + time) * 1.5f);
            float core = 1f - Mathf.Abs(across);
            var c = Color.Lerp(Deep, Mid, 0.25f + 0.75f * core);
            c = Color.Lerp(c, Deep, 0.45f * (1f - streak) * (1f - streak));      // dark flame stripes
            c = Color.Lerp(c, Glow, 0.35f * streak * streak * core + 0.25f * lowPulse); // hot streaks at the core
            c = Color.Lerp(c, Color.white, 0.6f * hurtFlash * core);
            c.a = 1f;
            return c;
        }

        /// <summary>A strip along the waving centre line between u0 and u1 (0..1 of x0..x1).</summary>
        void Ribbon(VertexHelper vh, float x0, float x1, float y, float u0, float u1, float thick, float amp,
                    System.Func<float, float, Color> color, int samples)
        {
            if (u1 - u0 < 1e-4f) return;
            int n = Mathf.Max(2, Mathf.CeilToInt(samples * (u1 - u0)) + 2);
            int start = vh.currentVertCount;
            // Torn ends: each cut runs on a slant (the top edge starts later and ends later than the bottom).
            float slant = Mathf.Min(0.012f, (u1 - u0) * 0.3f);
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / (n - 1);
                float uT = Mathf.Lerp(u0 + slant, u1, k), uM = Mathf.Lerp(u0 + slant * 0.5f, u1 - slant * 0.5f, k), uB = Mathf.Lerp(u0, u1 - slant, k);
                Vector3 Edge(float u, float side)
                {
                    float cy = y + ScarfHudLayout.Wave(u, time, amp);
                    float hw = ScarfHudLayout.HalfWidth(u, thick);
                    // A frayed lower edge, rippling a little as the cloth moves.
                    if (side < 0f) hw *= 1f - 0.34f * Mathf.Abs(Mathf.Sin(u * 61f + Mathf.Sin(u * 13f) * 2f)) * Mathf.SmoothStep(0.1f, 1f, u);
                    hw *= 1f + 0.06f * Mathf.Sin(u * 23f - time * 4f);
                    return new Vector3(Mathf.Lerp(x0, x1, u), cy + hw * side);
                }
                vh.AddVert(Edge(uT, 1f), color(uT, 1f), Vector4.zero);
                vh.AddVert(Edge(uM, 0f), color(uM, 0f), Vector4.zero);
                vh.AddVert(Edge(uB, -1f), color(uB, -1f), Vector4.zero);
                if (i > 0)
                {
                    int a = start + (i - 1) * 3, b = start + i * 3;
                    vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
                    vh.AddTriangle(a + 1, b + 1, a + 2); vh.AddTriangle(a + 2, b + 1, b + 2);
                }
            }
        }

        void Tongue(VertexHelper vh, float x, float y, float thick, float lowPulse)
        {
            float flicker = 0.75f + 0.25f * Mathf.Sin(time * 17f) * Mathf.Sin(time * 5.3f);
            float l = thick * (0.9f + 0.4f * lowPulse) * flicker;
            int i = vh.currentVertCount;
            var hot = Color.Lerp(Glow, Color.white, 0.25f);
            var tipC = new Color(Glow.r, Glow.g, Glow.b, 0f);
            vh.AddVert(new Vector3(x - 2f, y + thick * 0.4f), Mid, Vector4.zero);
            vh.AddVert(new Vector3(x - 2f, y - thick * 0.4f), Mid, Vector4.zero);
            vh.AddVert(new Vector3(x + l * 0.45f, y + thick * 0.1f), hot, Vector4.zero);
            vh.AddVert(new Vector3(x + l, y + thick * 0.35f * Mathf.Sin(time * 4f)), tipC, Vector4.zero);
            vh.AddTriangle(i, i + 2, i + 1);
            vh.AddTriangle(i, i + 3, i + 2);
            vh.AddTriangle(i + 1, i + 2, i + 3);
        }

        void Knot(VertexHelper vh, Vector2 c, float radius)
        {
            // Two short tails hanging from the knot.
            for (int s = 0; s < 2; s++)
            {
                float sway = Mathf.Sin(time * 2.6f + s) * 3f;
                int i = vh.currentVertCount;
                float dx = s == 0 ? -4f : 5f, w = s == 0 ? 6f : 5f, l = s == 0 ? 26f : 20f;
                vh.AddVert(new Vector3(c.x + dx - w, c.y - 4f), Deep, Vector4.zero);
                vh.AddVert(new Vector3(c.x + dx + w, c.y - 4f), Mid, Vector4.zero);
                vh.AddVert(new Vector3(c.x + dx + sway - 1f, c.y - l), new Color(Deep.r, Deep.g, Deep.b, 0.4f), Vector4.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
            int centre = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x, c.y), Mid, Vector4.zero);
            const int seg = 18;
            for (int k = 0; k <= seg; k++)
            {
                float a = Mathf.PI * 2f * k / seg;
                float rr = radius * (1f + 0.08f * Mathf.Sin(a * 3f + time * 2f));
                vh.AddVert(new Vector3(c.x + Mathf.Cos(a) * rr * 1.15f, c.y + Mathf.Sin(a) * rr), Deep, Vector4.zero);
                if (k > 0) vh.AddTriangle(centre, centre + k, centre + k + 1);
            }
        }

        void Diamond(VertexHelper vh, Vector2 c, float s, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x, c.y + s), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x + s * 0.6f, c.y), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x, c.y - s), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x - s * 0.6f, c.y), col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
