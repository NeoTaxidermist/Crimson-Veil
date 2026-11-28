using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// Life in the top-left corner as leaves of the crimson tree (v0.114, the user's choice): a twig painted in ink, a
    /// leaf on it for every point of life, alternately hanging below it and standing above it as on a real twig.
    /// Read as heart containers: every leaf keeps its place as a faint outline, crimson fills what is left of it, and
    /// three side veins split it into quarters. Every hit shows its own size: exactly the piece it took breaks off,
    /// glows a moment and falls away grey. Life drains from the twig's tip back to its base; the mend binding is wound
    /// round the twig's base, and while a wound can be bound the leaves show faintly what binding would give back.
    /// The shards of the next life swell a bud at the twig's tip; the fourth opens it into a new leaf.
    /// v0.114.5: drawn from smooth textures made in code (<see cref="LeafAtlas"/>), every edge anti-aliased - bare
    /// UI triangles were jagged ("je to odporný a zubatý"). Redrawn only on a change or while something moves.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LifeLeaves : MaskableGraphic
    {
        public float maxHealth = 5f, health = 5f;
        /// <summary>Life a moment ago (kept for the HUD; the falling pieces show what was lost).</summary>
        public float trailingHealth = 5f;
        [Range(0f, 1f)] public float hurtFlash;
        /// <summary>Shards towards the next life (0..3): the bud at the twig's tip.</summary>
        public int shards;
        /// <summary>The mend knot (0..1), how much one binding gives back, and how far the binding now held is (0..1).</summary>
        public float mend, mendAmount = 1f, mending;

        /// <summary>True when a wound can be bound now: the knot whole and life missing.</summary>
        public bool CanMend => mend >= 1f - 1e-4f && health < maxHealth - 1e-3f && health > 0f;

        /// <summary>The twig's length per leaf (px) and a leaf's length.</summary>
        public const float Spacing = 36f, LeafLength = 44f, LeafWidth = 22f;
        public const float FallSeconds = 1.3f, MendGlowSeconds = 0.7f;

        static readonly Color InkC = new Color(0.04f, 0.035f, 0.035f, 1f);
        static readonly Color CutC = new Color(0.16f, 0f, 0.02f, 0.9f);
        static readonly Color Wrap = new Color(0.6f, 0.07f, 0.09f, 1f);
        static readonly Color WrapLit = new Color(0.86f, 0.3f, 0.26f, 1f);
        static readonly Color Untied = new Color(0.5f, 0.47f, 0.46f, 0.6f);
        static readonly Color Ember = new Color(1f, 0.5f, 0.3f, 0.95f);
        static readonly Color Ashen = new Color(0.5f, 0.48f, 0.47f, 0.9f);

        public override Texture mainTexture => LeafAtlas.Texture;

        /// <summary>How many times it has been drawn (for tests).</summary>
        public int Redraws { get; private set; }
        /// <summary>Pieces falling now (for tests).</summary>
        public int Falling => pieces.Count;
        /// <summary>How much life the pieces falling now add up to (for tests: a hit's size).</summary>
        public float FallingAmount { get { float s = 0f; foreach (var p in pieces) s += p.f1 - p.f0; return s; } }

        struct Piece { public int leaf; public float f0, f1, start; }
        readonly System.Collections.Generic.List<Piece> pieces = new System.Collections.Generic.List<Piece>();
        readonly System.Collections.Generic.List<Piece> mends = new System.Collections.Generic.List<Piece>();

        float dHealth = -1f, dFlash, dMax, dMend = -1f, dMending; int dShards = -1;
        float time, lastHealth = -1f, lastMax = -1f;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            if (lastHealth >= 0f && maxHealth == lastMax && health != lastHealth)
            {
                // exactly what changed, leaf by leaf: lost pieces break off, mended ones glow as they grow back
                float lo = Mathf.Min(health, lastHealth), hi = Mathf.Max(health, lastHealth);
                var into = health < lastHealth ? pieces : mends;
                for (int i = Mathf.FloorToInt(lo); i < Mathf.CeilToInt(hi) && i < 64; i++)
                {
                    float f0 = Mathf.Clamp01(lo - i), f1 = Mathf.Clamp01(hi - i);
                    if (f1 - f0 > 1e-3f) into.Add(new Piece { leaf = i, f0 = f0, f1 = f1, start = time });
                }
            }
            lastHealth = health; lastMax = maxHealth;
            pieces.RemoveAll(p => time - p.start > FallSeconds);
            mends.RemoveAll(p => time - p.start > MendGlowSeconds);
            bool low = health > 0f && health <= 1.01f;
            if (health != dHealth || hurtFlash != dFlash || maxHealth != dMax || shards != dShards || mend != dMend || mending != dMending
                || low || CanMend || pieces.Count > 0 || mends.Count > 0)
                SetVerticesDirty();
        }

        /// <summary>Where leaf <paramref name="i"/> grows on the twig, and whether it hangs below (true) or stands above.</summary>
        public static Vector2 Node(int i, Rect r, out bool below)
        {
            below = i % 2 == 0;
            return new Vector2(r.xMin + 22f + i * Spacing, TwigY(r, i * Spacing));
        }

        static float TwigY(Rect r, float x) => r.yMax - 40f + Mathf.Sin(x * 0.02f + 0.7f) * 3f;

        static Vector2 LeafDir(int i, bool below)
        {
            float angle = (below ? -58f : 46f) * Mathf.Deg2Rad + 0.1f * Mathf.Sin(i * 1.7f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>A leaf's half-width (px) a fraction <paramref name="f"/> along it, as the texture draws it.</summary>
        public static float HalfWidthAt(float f, float size = 1f) => LeafWidth * 0.5f * size * LeafAtlas.Profile(f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Redraws++;
            dHealth = health; dFlash = hurtFlash; dMax = maxHealth; dShards = shards; dMend = mend; dMending = mending;
            vh.Clear();
            var r = rectTransform.rect;
            int n = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
            float low = health > 0f && health <= 1.01f ? 0.5f + 0.5f * Mathf.Sin(time * 5f) : 0f;
            float ready = CanMend ? 0.5f + 0.5f * Mathf.Sin(time * 3.5f) : 0f;

            // the twig, the bud at its tip, the binding round its base
            float length = 22f + (n - 1) * Spacing + 34f;
            Twig(vh, r, length);
            Bud(vh, new Vector2(r.xMin + length, TwigY(r, length)), shards / 4f);
            Binding(vh, r, mend, ready);

            float lift = Mathf.Clamp01(0.4f * low + 0.6f * hurtFlash);
            float promised = Mathf.Min(n, health + mendAmount), grown = health + mendAmount * Mathf.Clamp01(mending);
            for (int i = 0; i < n; i++)
            {
                var at = Node(i, r, out bool below);
                var dir = LeafDir(i, below);
                var stalkEnd = at + dir * 5f;
                Stroke(vh, at - dir * 1.5f, stalkEnd + dir * 1.5f, 2.6f, 1.8f, Color.white, true);
                float full = Mathf.Clamp01(health - i);
                if (full < 0.999f) Leaf(vh, stalkEnd, dir, full, 1f, LeafAtlas.Empty, Color.white);
                if (full > 0f)
                {
                    Leaf(vh, stalkEnd, dir, 0f, full, LeafAtlas.Full, Color.white);
                    if (lift > 0.01f) Leaf(vh, stalkEnd, dir, 0f, full, LeafAtlas.Pale, new Color(1f, 0.55f, 0.45f, 0.4f * lift));
                    if (full < 0.999f) Cut(vh, stalkEnd, dir, full);
                }
                if (CanMend && full < 0.999f)
                {
                    // what binding would give back, faint; held, it fills from the stalk
                    float p1 = Mathf.Clamp01(promised - i), g1 = Mathf.Clamp01(Mathf.Min(grown, promised) - i);
                    if (p1 > Mathf.Max(full, g1)) Leaf(vh, stalkEnd, dir, Mathf.Max(full, g1), p1, LeafAtlas.Full, new Color(1f, 1f, 1f, 0.28f + 0.17f * ready));
                    if (g1 > full) Leaf(vh, stalkEnd, dir, full, g1, LeafAtlas.Full, Color.white);
                }
            }
            // a mended piece glows a moment as it grows back
            foreach (var m in mends)
            {
                if (m.leaf >= n) continue;
                float t = (time - m.start) / MendGlowSeconds;
                var at = Node(m.leaf, r, out bool below);
                var dir = LeafDir(m.leaf, below);
                Leaf(vh, at + dir * 5f, dir, m.f0, m.f1, LeafAtlas.Pale, new Color(1f, 0.62f, 0.52f, 0.5f * (1f - t)));
            }
            // the pieces a hit took: a moment glowing where they were, then grey, falling and turning
            foreach (var p in pieces)
            {
                if (p.leaf >= n) continue;
                float t = (time - p.start) / FallSeconds;
                var at = Node(p.leaf, r, out bool below);
                var dir0 = LeafDir(p.leaf, below);
                var baseAt = at + dir0 * 5f;
                float mid = (p.f0 + p.f1) * 0.5f * LeafLength;
                var centre = baseAt + dir0 * mid;
                float fall = Mathf.Max(0f, t - 0.12f);
                float turn = fall * 2.4f * (below ? 1f : -1f);
                var dir = new Vector2(dir0.x * Mathf.Cos(turn) - dir0.y * Mathf.Sin(turn), dir0.x * Mathf.Sin(turn) + dir0.y * Mathf.Cos(turn));
                centre += new Vector2(Mathf.Sin(fall * 5f) * 7f + fall * 12f, -fall * fall * 70f - fall * 10f);
                var c = Color.Lerp(Ember, Ashen, Mathf.Clamp01(t * 4f)); c.a *= 1f - t * t;
                Leaf(vh, centre - dir * mid, dir, p.f0, p.f1, LeafAtlas.Pale, c);
            }
        }

        /// <summary>
        /// A leaf (or the part of it from <paramref name="f0"/> to <paramref name="f1"/> of its length) from its stalk
        /// along <paramref name="dir"/>: one quad over the leaf's picture in the atlas, so its edge is smooth.
        /// </summary>
        static void Leaf(VertexHelper vh, Vector2 baseAt, Vector2 dir, float f0, float f1, Rect cell, Color tint, float size = 1f)
        {
            if (f1 - f0 < 1e-3f) return;
            float L = LeafLength * size, half = L * LeafAtlas.CellHalfWidth;
            var side = new Vector2(-dir.y, dir.x);
            Vector2 a = baseAt + dir * (f0 * L), b = baseAt + dir * (f1 * L);
            float v0 = LeafAtlas.V(cell, f0), v1 = LeafAtlas.V(cell, f1);
            int i = vh.currentVertCount;
            vh.AddVert(a - side * half, tint, new Vector4(cell.xMin, v0));
            vh.AddVert(a + side * half, tint, new Vector4(cell.xMax, v0));
            vh.AddVert(b + side * half, tint, new Vector4(cell.xMax, v1));
            vh.AddVert(b - side * half, tint, new Vector4(cell.xMin, v1));
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>The clean edge where a leaf was torn: a thin dark line across it.</summary>
        static void Cut(VertexHelper vh, Vector2 baseAt, Vector2 dir, float f)
        {
            var side = new Vector2(-dir.y, dir.x);
            var p = baseAt + dir * (f * LeafLength);
            float h = HalfWidthAt(f) * 0.92f;
            Stroke(vh, p - side * h, p + side * h, 1.6f, 1.6f, CutC);
        }

        /// <summary>
        /// A line of ink with smooth edges: a quad over the soft stroke in the atlas, wide enough to hold its fringe.
        /// </summary>
        static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float w0, float w1, Color col, bool bark = false)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            if (bark && n.y < 0f) n = -n;   // (its lit side up)
            float k = LeafAtlas.StrokeSpread;
            var s = bark ? LeafAtlas.BarkCell : LeafAtlas.StrokeCell;
            float u = s.center.x;
            int i = vh.currentVertCount;
            vh.AddVert(a + n * w0 * 0.5f * k, col, new Vector4(u, s.yMax));
            vh.AddVert(a - n * w0 * 0.5f * k, col, new Vector4(u, s.yMin));
            vh.AddVert(b - n * w1 * 0.5f * k, col, new Vector4(u, s.yMin));
            vh.AddVert(b + n * w1 * 0.5f * k, col, new Vector4(u, s.yMax));
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>The twig: an ink stroke tapering from the corner out, a little uneven, its edges smooth.</summary>
        static void Twig(VertexHelper vh, Rect r, float length)
        {
            const int seg = 40;
            var s = LeafAtlas.BarkCell;
            float k = LeafAtlas.StrokeSpread, u0 = s.center.x;
            var white = Color.white;
            int start = vh.currentVertCount;
            for (int j = 0; j <= seg; j++)
            {
                float u = j / (float)seg;
                float x = r.xMin + 2f + u * (length - 2f);
                float y = TwigY(r, x - r.xMin);
                float w = Mathf.Lerp(6.5f, 2.4f, Mathf.Pow(u, 0.8f)) * (1f + 0.1f * Mathf.Sin(u * 31f));
                vh.AddVert(new Vector3(x, y + w * 0.5f * k), white, new Vector4(u0, s.yMax));
                vh.AddVert(new Vector3(x, y - w * 0.5f * k), white, new Vector4(u0, s.yMin));
                if (j > 0)
                {
                    int i0 = start + (j - 1) * 2, i1 = start + j * 2;
                    vh.AddTriangle(i0, i1, i0 + 1); vh.AddTriangle(i0 + 1, i1, i1 + 1);
                }
            }
        }

        /// <summary>
        /// The mend knot as a crimson binding wound round the twig's base: four wraps, one tied by every blow that lands;
        /// whole, two loose ends hang from it and it breathes while a wound waits to be bound.
        /// </summary>
        static void Binding(VertexHelper vh, Rect r, float mend, float ready)
        {
            for (int q = 0; q < 4; q++)
            {
                float x = r.xMin + 4f + q * 4.6f, y = TwigY(r, x - r.xMin);
                bool tied = mend >= (q + 1) / 4f - 1e-4f;
                var c = tied ? Color.Lerp(Wrap, WrapLit, 0.25f + 0.45f * ready) : Untied;
                Stroke(vh, new Vector2(x - 2.2f, y + 7f), new Vector2(x + 2.2f, y - 7f), tied ? 3.2f : 1.6f, tied ? 3.2f : 1.6f, c);
            }
            if (mend < 1f - 1e-4f) return;
            float y0 = TwigY(r, 12f);
            var end = Color.Lerp(Wrap, WrapLit, 0.25f + 0.45f * ready);
            Stroke(vh, new Vector2(r.xMin + 11f, y0 - 4f), new Vector2(r.xMin + 8f, y0 - 15f - 2f * ready), 2.6f, 1f, end);
            Stroke(vh, new Vector2(r.xMin + 13f, y0 - 4f), new Vector2(r.xMin + 17f, y0 - 13f - 2f * ready), 2.6f, 1f, end);
        }

        /// <summary>The bud for the next life: a small leaf still folded at the tip, unfolding with every shard.</summary>
        static void Bud(VertexHelper vh, Vector2 at, float grown)
        {
            float angle = 18f * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (grown <= 0f) { Leaf(vh, at - dir * 1f, dir, 0f, 1f, LeafAtlas.Pale, new Color(0.34f, 0.31f, 0.29f, 1f), 0.2f); return; }
            float size = 0.24f + 0.4f * grown;
            Leaf(vh, at, dir, 0f, 1f, LeafAtlas.Full, Color.white, size);
            var side = new Vector2(-dir.y, dir.x);
            float s = LeafLength * size;
            Stroke(vh, at, at + dir * (s * 0.32f) + side * (s * 0.2f), 2f, 0.8f, Color.white, true);
            Stroke(vh, at, at + dir * (s * 0.32f) - side * (s * 0.2f), 2f, 0.8f, Color.white, true);
        }
    }

    /// <summary>
    /// The pictures the life leaves are drawn from, painted in code once into one small texture with mipmaps
    /// (v0.114.5): the crimson leaf (layered as the scarf: dark at its edge and underside, lit along one half, a dark
    /// midrib and three side veins splitting it in quarters), the empty place of a leaf (a faint fill and a pale edge,
    /// seen on dark rock and pale mist alike), a pale leaf to tint (falling ash, embers, a mend's glow) and a soft stroke
    /// for every line of ink. Every edge is anti-aliased in the texture, so nothing on screen is jagged.
    /// </summary>
    public static class LeafAtlas
    {
        const int Size = 256, CellW = 64, CellH = 128, Base = 4, Len = 120;
        /// <summary>A leaf's half-width in the texture at its broadest, px.</summary>
        const float HalfW = 30f;

        /// <summary>UV cells: the crimson leaf, its empty place, a pale leaf to tint, the soft stroke.</summary>
        public static readonly Rect Full = Cell(0), Empty = Cell(1), Pale = Cell(2);
        public static readonly Rect StrokeCell = new Rect(208f / Size, 0f, 32f / Size, 32f / Size);
        /// <summary>The same stroke as bark: a dry branch lit from above (its top at the cell's top).</summary>
        public static readonly Rect BarkCell = new Rect(208f / Size, 32f / Size, 32f / Size, 32f / Size);
        /// <summary>A quad over a leaf cell is this wide (each side of the midrib) per unit of the leaf's length.</summary>
        public const float CellHalfWidth = (CellW * 0.5f) / Len;
        /// <summary>The stroke cell is this much wider than the solid ink it holds (its soft fringe).</summary>
        public const float StrokeSpread = 32f / 20f;

        static Texture2D texture;
        public static Texture2D Texture { get { if (texture == null) texture = Paint(); return texture; } }

        static Rect Cell(int k) => new Rect(k * CellW / (float)Size, 0f, CellW / (float)Size, CellH / (float)Size);

        /// <summary>The v coordinate a fraction <paramref name="f"/> along a leaf in <paramref name="cell"/>.</summary>
        public static float V(Rect cell, float f) => cell.yMin + (Base + f * Len) / Size;

        /// <summary>A leaf's outline: 0 at the stalk and the tip, 1 at its broadest a third along.</summary>
        public static float Profile(float f) => f <= 0f || f >= 1f ? 0f : Mathf.Sin(Mathf.Pow(f, 0.72f) * Mathf.PI);

        static Texture2D Paint()
        {
            var px = new Color32[Size * Size];
            var deep = new Color(0.27f, 0.015f, 0.035f);
            var crimson = new Color(0.62f, 0.06f, 0.085f);
            var lit = new Color(0.9f, 0.32f, 0.28f);
            var vein = new Color(0.15f, 0f, 0.02f);
            var paper = new Color(0.88f, 0.86f, 0.82f);
            for (int y = 0; y < CellH; y++)
                for (int x = 0; x < CellW; x++)
                {
                    float t = (y + 0.5f - Base) / Len;
                    float xs = x + 0.5f - CellW * 0.5f;
                    float hw = HalfW * Profile(t);
                    float d = Mathf.Abs(xs) - hw;                           // < 0 inside, px (good enough near the sides)
                    float tipD = (t - 1f) * Len, baseD = -t * Len;          // past the tip or the stalk
                    float edge = Mathf.Max(d, Mathf.Max(tipD, baseD));
                    float cover = Mathf.Clamp01(0.5f - edge);              // 1 px anti-aliased edge
                    if (cover <= 0f) continue;
                    float s = hw > 0.01f ? Mathf.Clamp(xs / hw, -1f, 1f) : 0f;   // across: -1 .. 1

                    // the crimson leaf
                    float light = 0.5f + 0.5f * s;
                    var c = Color.Lerp(deep, crimson, 0.45f + 0.4f * light * (1f - 0.35f * t));
                    float gloss = Mathf.Exp(-Mathf.Pow((s - 0.45f) / 0.25f, 2f)) * Mathf.Exp(-Mathf.Pow((t - 0.4f) / 0.25f, 2f));
                    c = Color.Lerp(c, lit, 0.4f * gloss);
                    float rim = Mathf.Clamp01((edge + 3f) / 3f);            // darker towards the edge
                    c = Color.Lerp(c, deep, 0.55f * rim);
                    float rib = Mathf.Clamp01(1.3f - Mathf.Abs(xs)) * (t < 0.92f ? 1f : 0f);
                    float veins = VeinCover(xs, y + 0.5f - Base, hw);
                    c = Color.Lerp(c, vein, Mathf.Max(0.75f * rib, 0.6f * veins));
                    Put(px, 0, x, y, c, 0.97f * cover);

                    // its empty place: a faint ink fill, a pale edge, the quarter veins faintly
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(edge + 1.2f) / 1.2f);
                    var e = Color.Lerp(new Color(0.05f, 0.04f, 0.04f), paper, Mathf.Max(ring, 0.5f * veins));
                    float ea = Mathf.Max(0.16f, Mathf.Max(0.5f * ring, 0.25f * veins));
                    Put(px, 1, x, y, e, ea * cover);

                    // a pale leaf to tint
                    Put(px, 2, x, y, Color.Lerp(Color.white, paper, 0.3f * rim), cover);
                }
            // the soft stroke: solid in its middle 20 px of 32, a smooth fringe either side
            for (int y = 0; y < 32; y++)
            {
                float d = Mathf.Abs(y + 0.5f - 16f) - 10f;
                float a = Mathf.Clamp01(0.5f - d / 1.6f);
                for (int x = 200; x < 248; x++) px[y * Size + x] = new Color(1f, 1f, 1f, a);
                // the bark (v0.114.6): the same stroke as a dry branch lit from above, charcoal below, ash-grey along
                // its top, so the twig shows on black rock and stays dark on pale mist
                float t = Mathf.Clamp01((y + 0.5f - 6f) / 20f);
                var bark = Color.Lerp(new Color(0.07f, 0.06f, 0.06f), new Color(0.5f, 0.46f, 0.43f), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t)));
                bark.a = a;
                for (int x = 200; x < 248; x++) px[(y + 32) * Size + x] = bark;
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "LifeLeaves", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Three side veins from the midrib out and forward to the edge, a quarter, a half and three quarters along.</summary>
        static float VeinCover(float xs, float yAlong, float hw)
        {
            float best = 0f, ax = Mathf.Abs(xs);
            if (ax > hw * 0.85f) return 0f;
            for (int q = 1; q <= 3; q++)
            {
                float line = q * 0.25f * Len + ax * 0.6f;
                float dist = Mathf.Abs(yAlong - line) / 1.17f;
                best = Mathf.Max(best, Mathf.Clamp01(1.1f - dist) * Mathf.Clamp01((hw * 0.85f - ax) / 3f));
            }
            return best;
        }

        static void Put(Color32[] px, int cell, int x, int y, Color c, float a)
        {
            c.a = a;
            px[y * Size + cell * CellW + x] = c;
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
