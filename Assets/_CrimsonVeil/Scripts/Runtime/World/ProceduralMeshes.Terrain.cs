using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    public enum TerrainStyle
    {
        /// <summary>Natural cliff: ragged sides, chipped corners, jagged underside.</summary>
        Rock,
        /// <summary>Masonry: straighter, with cut stone steps on the sides.</summary>
        Ruin,
    }

    public static partial class ProceduralMeshes
    {
        /// <summary>
        /// Silhouette outline (counter-clockwise, solid on the left) of a terrain block whose walkable top
        /// spans x ∈ [-w/2, w/2] at y = 0 and which extends down to y = -h. The top stays almost flat
        /// (feet must match the box collider); sides, corners and the underside get the character.
        /// </summary>
        public static List<Vector2> TerrainOutline(float width, float height, TerrainStyle style, float roughness, int seed)
        {
            var rnd = new System.Random(seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            float hw = width * 0.5f;
            float rough = Mathf.Clamp01(roughness);
            var pts = new List<Vector2>();

            // Top edge, left → right is clockwise for the top; we walk the outline CCW: bottom-left → bottom-right → top-right → top-left.
            int bottomSteps = Mathf.Max(3, Mathf.CeilToInt(width / 0.6f));
            int sideSteps = Mathf.Max(2, Mathf.CeilToInt(height / 0.5f));
            int topSteps = Mathf.Max(3, Mathf.CeilToInt(width / 0.5f));
            bool floating = height < 3.5f;

            // Bottom: jagged and tapering for floating ledges, roughly flat for tall cliffs (usually off-screen).
            for (int i = 0; i <= bottomSteps; i++)
            {
                float t = (float)i / bottomSteps;
                float x = Mathf.Lerp(-hw, hw, t);
                float taper = floating ? Mathf.Sin(t * Mathf.PI) * height * 0.45f * rough : 0f;
                float jag = style == TerrainStyle.Rock ? R(-0.25f, 0.25f) * rough * Mathf.Min(1f, height) : 0f;
                float inset = (i == 0 || i == bottomSteps) ? 0.25f * rough * Mathf.Min(1f, width * 0.2f) : 0f;
                pts.Add(new Vector2(x + (i == 0 ? inset : i == bottomSteps ? -inset : 0f), -height - taper + jag));
            }

            // Right side, bottom → top.
            for (int i = 1; i < sideSteps; i++)
            {
                float t = (float)i / sideSteps;
                float y = Mathf.Lerp(-height, 0f, t);
                float n = style == TerrainStyle.Rock ? R(-0.18f, 0.12f) : (i % 2 == 0 ? -0.06f : 0f);
                float lip = t > 0.85f ? 0.08f * rough : 0f; // slight overhanging lip under the top
                pts.Add(new Vector2(hw + (n * rough) + lip, y));
            }

            // Top, right → left: nearly flat, with a chipped corner on each end.
            float chip = 0.12f * rough;
            pts.Add(new Vector2(hw, -chip));
            for (int i = 0; i <= topSteps; i++)
            {
                float t = (float)i / topSteps;
                float x = Mathf.Lerp(hw - chip, -hw + chip, t);
                float bump = (i == 0 || i == topSteps) ? 0f : R(-0.025f, 0.025f) * rough;
                pts.Add(new Vector2(x, bump));
            }
            pts.Add(new Vector2(-hw, -chip));

            // Left side, top → bottom.
            for (int i = sideSteps - 1; i >= 1; i--)
            {
                float t = (float)i / sideSteps;
                float y = Mathf.Lerp(-height, 0f, t);
                float n = style == TerrainStyle.Rock ? R(-0.18f, 0.12f) : (i % 2 == 0 ? -0.06f : 0f);
                float lip = t > 0.85f ? 0.08f * rough : 0f;
                pts.Add(new Vector2(-hw - (n * rough) - lip, y));
            }
            return pts;
        }

        /// <summary>Extruded terrain block mesh (front, back and side walls) from <see cref="TerrainOutline"/>.</summary>
        public static Mesh Terrain(float width, float height, float depth, TerrainStyle style, float roughness, int seed)
        {
            var outline = TerrainOutline(width, height, style, roughness, seed);
            var b = new Builder(1);
            float hz = depth * 0.5f;
            float minX = -width * 0.5f - 1f, maxX = width * 0.5f + 1f;
            Vector2 UV(Vector2 p) => new Vector2((p.x - minX) / (maxX - minX), p.y / Mathf.Max(0.01f, height) + 1f);

            // Side walls.
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % outline.Count];
                Vector2 d = q - p;
                if (d.sqrMagnitude < 1e-8f) continue;
                var normal = new Vector3(d.y, -d.x, 0f).normalized;
                b.Quad(new Vector3(p.x, p.y, -hz), new Vector3(q.x, q.y, -hz), new Vector3(q.x, q.y, hz), new Vector3(p.x, p.y, hz),
                    normal, UV(p), UV(q), UV(q), UV(p));
            }

            // Front & back caps.
            var tris = Triangulate(outline);
            foreach (var (z, normal) in new[] { (-hz, Vector3.back), (hz, Vector3.forward) })
            {
                int start = b.v.Count;
                foreach (var p in outline) b.Vert(new Vector3(p.x, p.y, z), normal, UV(p));
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = start + tris[i], c1 = start + tris[i + 1], c2 = start + tris[i + 2];
                    var face = Vector3.Cross(b.v[c1] - b.v[a], b.v[c2] - b.v[a]);
                    if (Vector3.Dot(face, normal) >= 0f) b.Tri(a, c1, c2);
                    else b.Tri(a, c2, c1);
                }
            }
            TerrainRelief(b, outline, width, height, -hz, style, roughness, seed);
            return b.ToMesh("Terrain");
        }

        /// <summary>
        /// Carved front face: rock gets protruding strata slabs (tilted a little, moonlit on top), ruin
        /// masonry gets staggered ashlar courses. Everything stays inside the outline and below the grass line.
        /// </summary>
        static void TerrainRelief(Builder b, List<Vector2> outline, float width, float height, float z, TerrainStyle style, float roughness, int seed)
        {
            var rnd = new System.Random(seed * 4241 + 7);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            float hw = width * 0.5f;
            float bottom = -height + 0.25f;
            bool Inside(float x, float y) => PointInPolygon(new Vector2(x, y), outline);

            if (style == TerrainStyle.Ruin)
            {
                float ch = 0.48f;
                int row = 0;
                for (float y = -0.4f; y - ch > bottom; y -= ch, row++)
                {
                    float x = -hw + (row % 2 == 1 ? 0.35f : 0f);
                    while (x < hw)
                    {
                        float w = R(0.7f, 1.3f), xe = Mathf.Min(hw, x + w);
                        float cx = (x + xe) * 0.5f, cy = y - ch * 0.5f, sw = xe - x - 0.06f, sh = ch - 0.06f;
                        if (sw > 0.2f && rnd.NextDouble() > 0.08 && Inside(cx - sw * 0.5f, cy) && Inside(cx + sw * 0.5f, cy) && Inside(cx, cy - sh * 0.5f))
                        {
                            Stone(b, new Vector2(cx, cy), sw, sh, z, R(0.06f, 0.11f), 0.07f, R(-1.5f, 1.5f));
                        }
                        x = xe;
                    }
                }
                return;
            }

            // Rock strata: slabs in rough bands, some missing, the band spacing varies.
            float rough = Mathf.Clamp01(roughness);
            for (float y = -0.38f; y > bottom; y -= R(0.42f, 0.75f))
            {
                float x = -hw + R(0f, 0.5f);
                while (x < hw)
                {
                    float w = R(0.5f, 1.7f), h = R(0.16f, 0.4f);
                    float cx = x + w * 0.5f, cy = y - h * 0.5f;
                    x += w + R(0.04f, 0.3f);
                    if (rnd.NextDouble() < 0.3) continue; // gap: the raw rock behind shows
                    float tilt = R(-6f, 6f) * rough;
                    var rot = Quaternion.Euler(0f, 0f, tilt);
                    Vector3 ex = rot * new Vector3(w * 0.5f, 0f, 0f), ey = rot * new Vector3(0f, h * 0.5f, 0f);
                    if (!Inside(cx - ex.x - ey.x, cy - ex.y - ey.y) || !Inside(cx + ex.x - ey.x, cy + ex.y - ey.y) ||
                        !Inside(cx - ex.x + ey.x, cy - ex.y + ey.y) || !Inside(cx + ex.x + ey.x, cy + ex.y + ey.y)) continue;
                    float proud = R(0.06f, 0.16f) * (0.5f + rough);
                    Stone(b, new Vector2(cx, cy), w, h, z, proud, R(0.05f, 0.1f), tilt);
                    // A chipped smaller stone sitting on some slabs.
                    if (rnd.NextDouble() < 0.25 && Inside(cx, cy + h * 0.5f + 0.12f) && cy + h * 0.5f + 0.2f < -0.32f)
                        b.Box(new Vector3(cx + R(-w, w) * 0.3f, cy + h * 0.5f + 0.08f, z - proud * 0.4f), new Vector3(R(0.15f, 0.35f), R(0.1f, 0.18f), proud * 0.8f), Quaternion.Euler(0f, 0f, R(-15f, 15f)));
                }
            }
        }

        public static bool PointInPolygon(Vector2 p, IReadOnlyList<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 a = poly[i], c = poly[j];
                if ((a.y > p.y) != (c.y > p.y) && p.x < (c.x - a.x) * (p.y - a.y) / (c.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Ear-clipping triangulation of a simple polygon (any winding). Returns index triples.</summary>
        public static List<int> Triangulate(IReadOnlyList<Vector2> poly)
        {
            var result = new List<int>();
            int n = poly.Count;
            if (n < 3) return result;

            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % n];
                area += p.x * q.y - q.x * p.y;
            }
            bool ccw = area > 0f;

            var idx = new List<int>(n);
            for (int i = 0; i < n; i++) idx.Add(i);

            int guard = 0;
            while (idx.Count > 3 && guard++ < n * n)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (ccw ? cross <= 1e-9f : cross >= -1e-9f) continue; // reflex
                    bool inside = false;
                    for (int k = 0; k < idx.Count && !inside; k++)
                    {
                        int ip = idx[k];
                        if (ip == ia || ip == ib || ip == ic) continue;
                        inside = PointInTriangle(poly[ip], a, b, c);
                    }
                    if (inside) continue;
                    result.Add(ia); result.Add(ib); result.Add(ic);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break; // degenerate polygon: stop rather than loop forever
            }
            if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
            return result;
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0;
            bool pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        /// <summary>Hanging roots / dead grass, growing downward from y = 0 (uv2.y = wind weight, 1 at the tip).</summary>
        public static Mesh HangingRoots(int seed, int strands, float width, Vector2 lengthRange)
        {
            var rnd = new System.Random(seed);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var b = new Builder(1);
            const int segs = 5;
            for (int i = 0; i < strands; i++)
            {
                float x0 = R(-width * 0.5f, width * 0.5f);
                float len = R(lengthRange.x, lengthRange.y);
                float curl = R(-0.3f, 0.3f) * len;
                float w = R(0.015f, 0.04f);
                int pl = -1, pr = -1;
                for (int s = 0; s <= segs; s++)
                {
                    float t = (float)s / segs;
                    var c = new Vector3(x0 + curl * t * t, -len * t, R(-0.05f, 0.05f));
                    float half = w * 0.5f * (1f - t * 0.9f);
                    int l = b.Vert(c + Vector3.left * half, Vector3.back, new Vector2(0f, t), new Vector2(0f, t));
                    int r = b.Vert(c + Vector3.right * half, Vector3.back, new Vector2(1f, t), new Vector2(0f, t));
                    if (s > 0) { b.Tri(pl, l, r); b.Tri(pl, r, pr); }
                    pl = l; pr = r;
                }
            }
            return b.ToMesh("HangingRoots");
        }

        /// <summary>
        /// Fern / bush silhouette: arching fronds with alternating leaflets, rooted at y = 0.
        /// uv2.y grows along each frond so the tips sway most.
        /// </summary>
        public static Mesh Fern(int seed, int fronds, float size)
        {
            var rnd = new System.Random(seed);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var b = new Builder(1);
            for (int f = 0; f < fronds; f++)
            {
                float side = R(-1f, 1f);
                float len = size * R(0.6f, 1f);
                // quadratic arch: up first, then over and down
                Vector2 p0 = new Vector2(R(-0.1f, 0.1f) * size, 0f);
                Vector2 p1 = p0 + new Vector2(side * len * 0.35f, len * R(0.6f, 0.9f));
                Vector2 p2 = p0 + new Vector2(side * len * R(0.8f, 1.1f), len * R(0.15f, 0.45f));
                float z = R(-0.2f, 0.2f);
                Vector2 Q(float t) => (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;

                const int segs = 10;
                int pl = -1, pr = -1;
                for (int s = 0; s <= segs; s++)
                {
                    float t = (float)s / segs;
                    Vector2 c = Q(t);
                    Vector2 tan = (Q(Mathf.Min(1f, t + 0.02f)) - Q(Mathf.Max(0f, t - 0.02f))).normalized;
                    Vector2 n = new Vector2(-tan.y, tan.x);
                    float stem = size * 0.018f * (1f - t * 0.8f);
                    var w = new Vector2(0f, t);
                    int l = b.Vert(new Vector3(c.x + n.x * stem, c.y + n.y * stem, z), Vector3.back, new Vector2(0f, t), w);
                    int r = b.Vert(new Vector3(c.x - n.x * stem, c.y - n.y * stem, z), Vector3.back, new Vector2(1f, t), w);
                    if (s > 0) { b.Tri(pl, l, r); b.Tri(pl, r, pr); }
                    pl = l; pr = r;

                    // leaflets, alternating, shrinking towards the tip
                    if (s > 1 && s < segs)
                    {
                        float leaf = size * 0.16f * (1f - t) + size * 0.03f;
                        float dir = s % 2 == 0 ? 1f : -1f;
                        Vector2 baseA = c - tan * leaf * 0.12f, baseB = c + tan * leaf * 0.12f;
                        Vector2 tip = c + (n * dir * 0.85f + tan * 0.5f).normalized * leaf;
                        int a0 = b.Vert(new Vector3(baseA.x, baseA.y, z), Vector3.back, Vector2.zero, w);
                        int a1 = b.Vert(new Vector3(baseB.x, baseB.y, z), Vector3.back, Vector2.zero, w);
                        int a2 = b.Vert(new Vector3(tip.x, tip.y, z), Vector3.back, Vector2.one, new Vector2(0f, Mathf.Min(1f, t + 0.15f)));
                        b.Tri(a0, a1, a2);
                    }
                }
            }
            return b.ToMesh("Fern");
        }

        /// <summary>Classical column with base and capital, standing on y = 0.</summary>
        public static Mesh Column(float height, float radius, bool broken, int seed)
        {
            var rnd = new System.Random(seed);
            float top = broken ? height * (0.45f + 0.35f * (float)rnd.NextDouble()) : height;
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(radius * 1.45f, 0f), new Vector2(radius * 1.45f, radius * 0.35f),
                new Vector2(radius * 1.2f, radius * 0.5f), new Vector2(radius * 1.05f, radius * 0.7f),
                new Vector2(radius, radius * 0.9f), new Vector2(radius * 0.88f, top - radius * 0.9f),
            };
            if (!broken)
            {
                profile.Add(new Vector2(radius * 1.05f, top - radius * 0.6f));
                profile.Add(new Vector2(radius * 1.5f, top - radius * 0.35f));
                profile.Add(new Vector2(radius * 1.6f, top));
            }
            else profile.Add(new Vector2(radius * 0.8f, top));
            profile.Add(new Vector2(0f, top));
            return Lathe(profile, 12, broken ? "ColumnBroken" : "Column");
        }
    }
}
