using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// Life in the top-left corner as leaves of the crimson tree (v0.113.4, the user's choice): a twig painted in ink,
    /// a crimson leaf on it for every point of life, alternately hanging below it and standing above it as on a real
    /// twig, so many fit on a short one. A wound tears a leaf a quarter at a time (from its tip back to the stalk); a
    /// leaf lost turns grey and falls away, the bare stalk left. More life is grown, not added: the shards of the next
    /// life swell a bud at the twig's tip, a quarter each; the fourth opens it into a new leaf, and the twig grows on
    /// to carry the next bud. Translucent layered crimson as the scarf. Built in code; redrawn only on a change or
    /// while a leaf falls.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LifeLeaves : MaskableGraphic
    {
        public float maxHealth = 5f, health = 5f;
        /// <summary>Life a moment ago: the part just lost glows on a moment.</summary>
        public float trailingHealth = 5f;
        [Range(0f, 1f)] public float hurtFlash;
        /// <summary>Shards towards the next life (0..3): the bud at the twig's tip.</summary>
        public int shards;

        /// <summary>The twig's length per leaf (px) and a leaf's length.</summary>
        public const float Spacing = 36f, LeafLength = 44f, LeafWidth = 22f;
        public const float FallSeconds = 1.3f;

        static readonly Color InkC = new Color(0.03f, 0.03f, 0.035f, 1f);
        static readonly Color Dark = new Color(0.32f, 0.0f, 0.03f, 0.92f);
        static readonly Color Mid = new Color(0.72f, 0.03f, 0.06f, 0.85f);
        static readonly Color Bright = new Color(1f, 0.2f, 0.16f, 0.55f);
        static readonly Color Ember = new Color(1f, 0.35f, 0.18f, 0.9f);
        static readonly Color Ashen = new Color(0.45f, 0.43f, 0.43f, 0.85f);

        /// <summary>How many times it has been drawn (for tests).</summary>
        public int Redraws { get; private set; }
        /// <summary>Leaves falling now (for tests).</summary>
        public int Falling => falls.Count;

        float dHealth = -1f, dTrail, dFlash, dMax; int dShards = -1;
        float time, lastHealth = -1f;
        readonly System.Collections.Generic.List<Vector2> falls = new System.Collections.Generic.List<Vector2>();   // (leaf index, start time)

        void Update()
        {
            time += Time.unscaledDeltaTime;
            // a whole leaf lost: it falls away
            if (lastHealth >= 0f)
                for (int i = Mathf.CeilToInt(health); i < Mathf.CeilToInt(lastHealth) && i < 64; i++)
                    if (health <= i) falls.Add(new Vector2(i, time));
            lastHealth = health;
            falls.RemoveAll(f => time - f.y > FallSeconds);
            bool low = health > 0f && health <= 1.01f;
            if (health != dHealth || trailingHealth != dTrail || hurtFlash != dFlash || maxHealth != dMax || shards != dShards || low || falls.Count > 0)
                SetVerticesDirty();
        }

        /// <summary>Where leaf <paramref name="i"/> grows on the twig, and whether it hangs below (true) or stands above.</summary>
        public static Vector2 Node(int i, Rect r, out bool below)
        {
            below = i % 2 == 0;
            return new Vector2(r.xMin + 22f + i * Spacing, TwigY(r, i * Spacing));
        }

        static float TwigY(Rect r, float x) => r.yMax - 40f + Mathf.Sin(x * 0.02f + 0.7f) * 3f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Redraws++;
            dHealth = health; dTrail = trailingHealth; dFlash = hurtFlash; dMax = maxHealth; dShards = shards;
            vh.Clear();
            var r = rectTransform.rect;
            int n = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
            float low = health > 0f && health <= 1.01f ? 0.5f + 0.5f * Mathf.Sin(time * 6f) : 0f;

            // the twig: from its base at the corner to beyond the last leaf, where the bud is; thick at the base
            float length = 22f + (n - 1) * Spacing + 34f;
            Twig(vh, r, length);
            // the bud for the next life at the tip, swelling with the shards (where the next leaf will grow)
            var tip = new Vector2(r.xMin + length, TwigY(r, length));
            Bud(vh, tip, shards / 4f, n);

            for (int i = 0; i < n; i++)
            {
                var at = Node(i, r, out bool below);
                float full = Mathf.Clamp01(health - i), trail = Mathf.Clamp01(Mathf.Max(trailingHealth, health) - i);
                float angle = (below ? -58f : 46f) * Mathf.Deg2Rad + 0.1f * Mathf.Sin(i * 1.7f);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                // the stalk, always (a bare one where the leaf has gone)
                var stalkEnd = at + dir * 6f;
                Line(vh, at, stalkEnd, 2.2f, 1.4f, InkC);
                if (trail > full + 1e-3f) Leaf(vh, stalkEnd, dir, full, trail, Ember, Ember, i, 1f);
                if (full > 0f)
                {
                    var glow = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.5f, 1f), 0.5f * low + 0.6f * hurtFlash);
                    Leaf(vh, stalkEnd, dir, 0f, full, Dark * glow, Mid * glow, i, 1f);
                }
            }
            // leaves lost a moment ago, grey, falling and turning
            foreach (var f in falls)
            {
                int i = (int)f.x;
                float t = (time - f.y) / FallSeconds;
                var at = Node(i, r, out bool below);
                float angle = (below ? -58f : 46f) * Mathf.Deg2Rad - t * 2.2f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var pos = at + new Vector2(Mathf.Sin(t * 5f) * 8f + t * 10f, -t * 46f);
                var c = Ashen; c.a *= 1f - t;
                Leaf(vh, pos, dir, 0f, 1f, c, c, i, 1f - 0.2f * t);
            }
        }

        /// <summary>The twig: an ink stroke tapering from the corner out, a little uneven, with a knot at each leaf.</summary>
        void Twig(VertexHelper vh, Rect r, float length)
        {
            const int seg = 40;
            int start = vh.currentVertCount;
            for (int k = 0; k <= seg; k++)
            {
                float u = k / (float)seg;
                float x = r.xMin + u * length;
                float y = TwigY(r, u * length);
                float w = Mathf.Lerp(7f, 2.6f, u) * (1f + 0.12f * Mathf.Sin(u * 37f));
                vh.AddVert(new Vector3(x, y + w * 0.5f), InkC, Vector4.zero);
                vh.AddVert(new Vector3(x, y - w * 0.5f), InkC, Vector4.zero);
                if (k > 0)
                {
                    int i0 = start + (k - 1) * 2, i1 = start + k * 2;
                    vh.AddTriangle(i0, i1, i0 + 1); vh.AddTriangle(i0 + 1, i1, i1 + 1);
                }
            }
            // a dark wash under it, so it reads on the pale mist
        }

        /// <summary>
        /// A crimson leaf from its stalk along <paramref name="dir"/>, as the scarf's cloth: a dark underside, the crimson
        /// over it, a bright gloss along one half, a dark midrib. Only from <paramref name="f0"/> to <paramref name="f1"/>
        /// of its length is drawn (a torn leaf: the rest is gone, its edge ragged).
        /// </summary>
        static void Leaf(VertexHelper vh, Vector2 baseAt, Vector2 dir, float f0, float f1, Color under, Color over, int seed, float size)
        {
            if (f1 - f0 < 1e-3f) return;
            float L = LeafLength * size, W = LeafWidth * size;
            var side = new Vector2(-dir.y, dir.x);
            const int seg = 14;
            // the outline's half-width along the leaf: round at the base, broadest a third along, pointed at the tip
            float Half(float u) => W * 0.5f * Mathf.Sin(Mathf.Pow(Mathf.Clamp01(u), 0.75f) * Mathf.PI) * (1f + 0.05f * Mathf.Sin(u * 20f + seed));
            for (int layer = 0; layer < 3; layer++)
            {
                var col = layer == 0 ? under : layer == 1 ? over : Bright;
                if (layer == 2) col.a *= over.a;   // (the gloss fades with the leaf)
                float shrink = layer == 0 ? 1f : layer == 1 ? 0.86f : 0.5f;
                float shift = layer == 2 ? 0.1f : 0f;   // the gloss on one half
                int start = vh.currentVertCount;
                for (int k = 0; k <= seg; k++)
                {
                    float u = Mathf.Lerp(f0, f1, k / (float)seg);
                    float h = Half(u) * shrink;
                    var p = baseAt + dir * (u * L);
                    vh.AddVert(p + side * (h + shift * W), col, Vector4.zero);
                    vh.AddVert(p - side * (h * (layer == 2 ? 0.1f : 1f) - shift * W), col, Vector4.zero);
                    if (k > 0)
                    {
                        int i0 = start + (k - 1) * 2, i1 = start + k * 2;
                        vh.AddTriangle(i0, i1, i0 + 1); vh.AddTriangle(i0 + 1, i1, i1 + 1);
                    }
                }
            }
            // a torn end: ragged teeth of the leaf left standing past the cut
            if (f1 < 0.999f)
            {
                float h = Half(f1);
                var cut = baseAt + dir * (f1 * L);
                const int teeth = 4;
                for (int j = 0; j < teeth; j++)
                {
                    var a = cut + side * Mathf.Lerp(h, -h, j / (float)teeth);
                    var b = cut + side * Mathf.Lerp(h, -h, (j + 1f) / teeth);
                    float reach = L * (((j + seed) & 1) == 0 ? 0.09f : 0.035f);
                    var tipAt = (a + b) * 0.5f + dir * reach;
                    int t0 = vh.currentVertCount;
                    vh.AddVert(a, under, Vector4.zero); vh.AddVert(b, under, Vector4.zero); vh.AddVert(tipAt, under, Vector4.zero);
                    vh.AddTriangle(t0, t0 + 1, t0 + 2);
                }
            }
            // the midrib
            Line(vh, baseAt + dir * (f0 * L), baseAt + dir * (Mathf.Min(f1, 0.92f) * L), 1.4f, 0.5f, new Color(0.2f, 0f, 0.02f, 0.85f * over.a));
        }

        /// <summary>The bud for the next life: two dark sepals and the crimson swelling between, bigger with every shard.</summary>
        static void Bud(VertexHelper vh, Vector2 at, float grown, int seed)
        {
            // a small leaf still folded, pointing on along the twig; it unfolds into the next leaf on the fourth shard
            float angle = 18f * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (grown <= 0f) { Ellipse(vh, at + dir * 3f, 4f, 2.6f, InkC); return; }
            float size = 0.22f + 0.4f * grown;
            Leaf(vh, at, dir, 0f, 1f, Dark, Mid, seed, size);
            // the sepals holding it, dark
            var side = new Vector2(-dir.y, dir.x);
            float s = LeafLength * size;
            Line(vh, at, at + dir * (s * 0.35f) + side * (s * 0.22f), 2f, 0.5f, InkC);
            Line(vh, at, at + dir * (s * 0.35f) - side * (s * 0.22f), 2f, 0.5f, InkC);
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float w0, float w1, Color col)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            int i = vh.currentVertCount;
            vh.AddVert(a + n * w0 * 0.5f, col, Vector4.zero); vh.AddVert(a - n * w0 * 0.5f, col, Vector4.zero);
            vh.AddVert(b - n * w1 * 0.5f, col, Vector4.zero); vh.AddVert(b + n * w1 * 0.5f, col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, Color col) => Ellipse(vh, c, radius, radius, col);

        static void Ellipse(VertexHelper vh, Vector2 c, float rx, float ry, Color col)
        {
            const int seg = 16;
            int centre = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            for (int k = 0; k <= seg; k++)
            {
                float a = Mathf.PI * 2f * k / seg;
                vh.AddVert(c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), col, Vector4.zero);
                if (k > 0) vh.AddTriangle(centre, centre + k, centre + k + 1);
            }
        }
    }

    /// <summary>
    /// The scarf's strength at the bottom in the middle (v0.113): the scarf itself, painted as a fluttering ribbon of
    /// crimson flame whose length is the strength - where the eyes are in a fight. A faint ink track shows its whole
    /// length; small light notches mark what each weapon costs (the whip, the shield, the spear, the bow - closer
    /// together on a longer scarf, bought at an altar), so it is seen at a glance which weapon there is scarf for. At its
    /// start the knot a wound is bound with, tied tighter as blows land; full, it glows. Full strength, a glint at the
    /// end: the scarf will take the next blow. Redrawn about thirty times a second for its flutter, at once on a change.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EnergyScarf : MaskableGraphic
    {
        [Range(0f, 1f)] public float charge = 1f;
        /// <summary>The scarf's length bought at altars: the weapons' notches lie at their cost divided by it.</summary>
        public float capacity = 1f;
        /// <summary>The mend knot, 0..1.</summary>
        [Range(0f, 1f)] public float mend;
        [Range(0f, 1f)] public float hurtFlash;

        public float thickness = 28f, waveAmplitude = 6f;
        public const float FlutterStep = 1f / 30f;

        static readonly Color Deep = new Color(0.32f, 0.0f, 0.02f, 1f);
        static readonly Color Mid = new Color(0.78f, 0.03f, 0.05f, 1f);
        static readonly Color Glow = new Color(1.0f, 0.22f, 0.12f, 1f);
        static readonly Color Track = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color Ash = new Color(0.4f, 0.38f, 0.38f, 0.8f);

        /// <summary>The weapons whose cost is marked, in order of cost.</summary>
        public static readonly VeilForm[] Marked = { VeilForm.Whip, VeilForm.Shield, VeilForm.Spear, VeilForm.Bow };

        /// <summary>Where (0..1 of the scarf) the notch of <paramref name="form"/> lies: its cost on a scarf this long.</summary>
        public static float NotchAt(VeilForm form, float capacity) => Mathf.Clamp01(VeilCasting.Cost(form) / Mathf.Max(0.1f, capacity));

        public int Redraws { get; private set; }
        float time, drawnAt = -1f, dCharge = -1f, dMend = -1f, dFlash, dCap;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            bool changed = charge != dCharge || mend != dMend || hurtFlash != dFlash || capacity != dCap;
            if (changed || time - drawnAt >= FlutterStep) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Redraws++;
            drawnAt = time; dCharge = charge; dMend = mend; dFlash = hurtFlash; dCap = capacity;
            vh.Clear();
            var r = rectTransform.rect;
            float x0 = r.xMin + 64f, x1 = r.xMax - 20f, y = r.center.y;
            // the whole length, faint: a thin ash thread under it (seen on black rock and pale mist alike)
            Ribbon(vh, x0, x1, y, 0f, 1f, 9f, waveAmplitude, (u, a) => Track, 28);
            Ribbon(vh, x0, x1, y, Mathf.Clamp01(charge), 1f, 4f, waveAmplitude, (u, a) => Ash, 28);
            // the strength: the scarf itself (v0.113.1 - "it should look like the scarf")
            if (charge > 0.003f) Scarf(vh, x0, x1, y, Mathf.Clamp01(charge));
            // the weapons' costs
            foreach (var f in Marked)
            {
                float u = NotchAt(f, capacity);
                float nx = Mathf.Lerp(x0, x1, u), ny = y + ScarfHudLayout.Wave(u, time, waveAmplitude);
                bool affordable = charge + 1e-3f >= Mathf.Min(u, VeilCasting.MinToCast);
                // (a small mark over the scarf, not across it)
                Quad(vh, new Vector2(nx, ny + thickness * 0.95f), 1.2f, 5f, affordable ? new Color(1f, 0.9f, 0.85f, 0.75f) : new Color(0.5f, 0.48f, 0.46f, 0.5f));
            }
            if (charge >= 0.999f)
            {
                float gx = x0 + (x1 - x0), gy = y + ScarfHudLayout.Wave(1f, time, waveAmplitude);
                Diamond(vh, new Vector2(gx, gy), 7f + 2f * Mathf.Sin(time * 5f), new Color(1f, 0.75f, 0.7f, 0.95f));
            }
            Knot(vh, new Vector2(r.xMin + 30f, y), 18f);
        }

        /// <summary>
        /// The scarf as it is in the game: several strands of translucent crimson cloth, dark carmine to bright, laid
        /// over one another and twisting round each other as they stream; a gloss along the brighter ones; towards its
        /// end the strands part, thin and curl, and a few threads of smoke trail on past it. Its length is the strength.
        /// </summary>
        void Scarf(VertexHelper vh, float x0, float x1, float y, float len)
        {
            float L = (x1 - x0) * len;
            const int strands = 7;
            for (int s = 0; s < strands; s++)
            {
                float k = s / (strands - 1f);                 // 0 the dark under-strand .. 1 the bright top one
                float phase = s * 1.37f, speed = 1.6f + 0.35f * s;
                var col = Color.Lerp(new Color(0.28f, 0.0f, 0.03f, 0.85f), new Color(0.9f, 0.06f, 0.08f, 0.62f), k);
                col = Color.Lerp(col, new Color(1f, 0.6f, 0.5f, col.a), 0.5f * hurtFlash);
                float width = thickness * (0.95f - 0.35f * Mathf.Abs(k - 0.5f));
                Strand(vh, x0, y, L, len, phase, speed, width, col, k, s);
            }
            // threads of smoke trailing past the end
            for (int t = 0; t < 3; t++)
            {
                float ph = 2.1f + t * 2.3f;
                Thread(vh, x0 + L * 0.55f, y, L * 0.45f + 40f + 18f * t, ph, new Color(0.55f, 0.03f, 0.05f, 0.45f - 0.1f * t));
            }
        }

        /// <summary>The scarf's centre line at <paramref name="u"/> (0..1 of its length): a long S that streams.</summary>
        float Centre(float u, float L) => ScarfHudLayout.Wave(u, time, waveAmplitude) + Mathf.Sin(u * 5.2f - time * 1.3f) * 3.5f * Mathf.Clamp01(L / 200f);

        void Strand(VertexHelper vh, float x0, float y, float L, float len, float phase, float speed, float width, Color col, float k, int seed)
        {
            int n = Mathf.Max(6, Mathf.CeilToInt(L / 6f));
            int start = vh.currentVertCount;
            var gloss = new Color(1f, 0.55f, 0.5f, 0.55f * k * k);
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                // strands cross over one another as the cloth twists; nearer the end they part and fan out
                float part = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, u));
                float twist = Mathf.Sin(u * (6f + seed * 0.6f) - time * speed + phase);
                float off = (k - 0.5f) * thickness * 0.55f * twist + (k - 0.5f) * thickness * 1.6f * part
                            + Mathf.Sin(u * 11f - time * 3f + phase) * 4f * part;
                // thick at the neck, a little narrower along, thin and curling at the very end
                float w = width * (1f - 0.55f * part) * (0.75f + 0.25f * Mathf.Sin(u * 17f + phase - time * 2f)) * Mathf.Clamp01((1f - u) * 14f + 0.15f);
                float cy = y + Centre(u, L) + off;
                // the end curls up or down a little, strand by strand
                float curl = part * part * Mathf.Sin(phase + time * 0.9f) * 10f;
                float x = x0 + u * L;
                // the twist turns the strand's face: narrow where it is edge-on, with a gloss where it faces the light
                float face = 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(u * (6f + seed * 0.6f) - time * speed + phase));
                float hw = w * 0.5f * face;
                var c = col; c.a *= 0.7f + 0.3f * face;
                vh.AddVert(new Vector3(x, cy + curl + hw), Color.Lerp(c, gloss, gloss.a * face), Vector4.zero);
                vh.AddVert(new Vector3(x, cy + curl - hw), c, Vector4.zero);
                if (i > 0)
                {
                    int a = start + (i - 1) * 2, b = start + i * 2;
                    vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
                }
            }
        }

        /// <summary>A thread of smoke: a thin line rippling more than the cloth, fading out.</summary>
        void Thread(VertexHelper vh, float xs, float y, float length, float phase, Color col)
        {
            const int n = 24;
            int start = vh.currentVertCount;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                float x = xs + u * length;
                float cy = y + Mathf.Sin(u * 7f - time * 2.4f + phase) * 7f * u + Mathf.Sin(phase) * 6f * u;
                float hw = 1.3f * (1f - u * 0.6f);
                var c = col; c.a *= Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                vh.AddVert(new Vector3(x, cy + hw), c, Vector4.zero);
                vh.AddVert(new Vector3(x, cy - hw), c, Vector4.zero);
                if (i > 0)
                {
                    int a = start + (i - 1) * 2, b = start + i * 2;
                    vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
                }
            }
        }

        Color Flame(float u, float across)
        {
            float streak = 0.5f + 0.5f * Mathf.Sin(u * 46f - time * 6.5f + across * 2.2f + Mathf.Sin(u * 9f + time) * 1.5f);
            float core = 1f - Mathf.Abs(across);
            var c = Color.Lerp(Deep, Mid, 0.25f + 0.75f * core);
            c = Color.Lerp(c, Deep, 0.45f * (1f - streak) * (1f - streak));
            c = Color.Lerp(c, Glow, 0.35f * streak * streak * core);
            c = Color.Lerp(c, Color.white, 0.5f * hurtFlash * core);
            c.a = 1f;
            return c;
        }

        /// <summary>The knot at the scarf's start: ink, wound round by crimson as far as the knot is tied; full, it glows.</summary>
        void Knot(VertexHelper vh, Vector2 c, float radius)
        {
            bool full = mend >= 0.999f;
            float pulse = full ? 0.5f + 0.5f * Mathf.Sin(time * 4f) : 0f;
            Disc(vh, c, radius + 6f, 1f, new Color(0f, 0f, 0f, 0.55f));
            Disc(vh, c, radius, 1f, new Color(0.06f, 0.05f, 0.05f, 1f));
            if (mend > 0.001f) ArcBand(vh, c, radius - 4f, 6f, mend, Color.Lerp(Mid, Glow, 0.3f * pulse));
            if (full) Disc(vh, c, radius * (0.45f + 0.08f * pulse), 1f, Color.Lerp(Mid, Glow, 0.4f + 0.4f * pulse));
            // two short tails
            for (int s = 0; s < 2; s++)
            {
                float sway = Mathf.Sin(time * 2.6f + s) * 3f;
                int i = vh.currentVertCount;
                float dy = s == 0 ? 5f : -5f;
                vh.AddVert(new Vector3(c.x - radius * 0.6f, c.y + dy + 4f), Deep, Vector4.zero);
                vh.AddVert(new Vector3(c.x - radius * 0.6f, c.y + dy - 4f), Mid, Vector4.zero);
                vh.AddVert(new Vector3(c.x - radius - 22f, c.y + dy * 2.2f + sway), new Color(Deep.r, Deep.g, Deep.b, 0.3f), Vector4.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
        }

        void Ribbon(VertexHelper vh, float x0, float x1, float y, float u0, float u1, float thick, float amp,
                    System.Func<float, float, Color> color, int samples)
        {
            if (u1 - u0 < 1e-4f) return;
            int n = Mathf.Max(2, Mathf.CeilToInt(samples * (u1 - u0)) + 2);
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                float u = Mathf.Lerp(u0, u1, (float)i / (n - 1));
                float cy = y + ScarfHudLayout.Wave(u, time, amp);
                // the living end tapers and frays, as the scarf's tip
                float tipFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u1 - u) / 0.06f));
                float hw = thick * 0.5f * (0.55f + 0.45f * tipFade) * (1f + 0.06f * Mathf.Sin(u * 23f - time * 4f));
                float fray = 1f - 0.3f * Mathf.Abs(Mathf.Sin(u * 61f + Mathf.Sin(u * 13f) * 2f));
                float x = Mathf.Lerp(x0, x1, u);
                vh.AddVert(new Vector3(x, cy + hw), color(u, 1f), Vector4.zero);
                vh.AddVert(new Vector3(x, cy), color(u, 0f), Vector4.zero);
                vh.AddVert(new Vector3(x, cy - hw * fray), color(u, -1f), Vector4.zero);
                if (i > 0)
                {
                    int a = start + (i - 1) * 3, b = start + i * 3;
                    vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
                    vh.AddTriangle(a + 1, b + 1, a + 2); vh.AddTriangle(a + 2, b + 1, b + 2);
                }
            }
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, float fraction, Color col)
        {
            const int seg = 28;
            int centre = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            int n = Mathf.Max(2, Mathf.CeilToInt(seg * fraction));
            for (int k = 0; k <= n; k++)
            {
                float a = Mathf.PI * 0.5f - fraction * Mathf.PI * 2f * k / n;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, col, Vector4.zero);
                if (k > 0) vh.AddTriangle(centre, centre + k, centre + k + 1);
            }
        }

        static void ArcBand(VertexHelper vh, Vector2 c, float radius, float w, float fraction, Color col)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(32 * fraction));
            int start = vh.currentVertCount;
            for (int k = 0; k <= n; k++)
            {
                float a = Mathf.PI * 0.5f - fraction * Mathf.PI * 2f * k / n;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(c + d * (radius + w * 0.5f), col, Vector4.zero);
                vh.AddVert(c + d * (radius - w * 0.5f), col, Vector4.zero);
                if (k > 0)
                {
                    int i0 = start + (k - 1) * 2, i1 = start + k * 2;
                    vh.AddTriangle(i0, i1, i0 + 1); vh.AddTriangle(i0 + 1, i1, i1 + 1);
                }
            }
        }

        static void Quad(VertexHelper vh, Vector2 c, float halfW, float halfH, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x - halfW, c.y - halfH), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x - halfW, c.y + halfH), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x + halfW, c.y + halfH), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x + halfW, c.y - halfH), col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Diamond(VertexHelper vh, Vector2 c, float s, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x, c.y + s), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x + s * 0.6f, c.y), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x, c.y - s), col, Vector4.zero);
            vh.AddVert(new Vector3(c.x - s * 0.6f, c.y), col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
