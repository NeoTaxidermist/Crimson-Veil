using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// Code-generated meshes for the grey-box-to-art pass: round arches, grass clumps,
    /// tattered banners and branching trees. Deterministic per seed.
    /// </summary>
    public static partial class ProceduralMeshes
    {
        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            /// <summary>uv2.y is the wind weight read by the Silhouette shader (defaults to uv).</summary>
            public readonly List<Vector2> uv2 = new List<Vector2>();
            public readonly List<int>[] tris;

            public Builder(int submeshes)
            {
                tris = new List<int>[submeshes];
                for (int i = 0; i < submeshes; i++) tris[i] = new List<int>();
            }

            public int Vert(Vector3 p, Vector3 normal, Vector2 texcoord) => Vert(p, normal, texcoord, texcoord);

            public int Vert(Vector3 p, Vector3 normal, Vector2 texcoord, Vector2 wind)
            {
                v.Add(p); n.Add(normal); uv.Add(texcoord); uv2.Add(wind);
                return v.Count - 1;
            }

            /// <summary>Adds a quad, fixing the winding so it faces <paramref name="normal"/>.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, int sub = 0)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                {
                    (b, d) = (d, b);
                    (ub, ud) = (ud, ub);
                }
                int i0 = Vert(a, normal, ua), i1 = Vert(b, normal, ub), i2 = Vert(c, normal, uc), i3 = Vert(d, normal, ud);
                var t = tris[sub];
                t.Add(i0); t.Add(i1); t.Add(i2);
                t.Add(i0); t.Add(i2); t.Add(i3);
            }

            public void Tri(int a, int b, int c, int sub = 0)
            {
                var t = tris[sub];
                t.Add(a); t.Add(b); t.Add(c);
            }

            /// <summary>Removes every triangle whose centroid satisfies <paramref name="remove"/>.</summary>
            public void Clip(System.Func<Vector3, bool> remove)
            {
                foreach (var t in tris)
                {
                    var kept = new List<int>(t.Count);
                    for (int i = 0; i < t.Count; i += 3)
                    {
                        var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                        if (remove(c)) continue;
                        kept.Add(t[i]); kept.Add(t[i + 1]); kept.Add(t[i + 2]);
                    }
                    t.Clear();
                    t.AddRange(kept);
                }
            }

            public void Box(Vector3 center, Vector3 size, Quaternion rotation, int sub = 0)
            {
                Vector3 h = size * 0.5f;
                Vector3 P(float x, float y, float z) => center + rotation * new Vector3(x * h.x, y * h.y, z * h.z);
                Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (var dir in n)
                {
                    Vector3 a = Mathf.Abs(dir.y) > 0.5f ? Vector3.right : Vector3.up;
                    Vector3 bAxis = Vector3.Cross(dir, a);
                    Vector3 c0 = dir + a + bAxis, c1 = dir + a - bAxis, c2 = dir - a - bAxis, c3 = dir - a + bAxis;
                    Quad(P(c0.x, c0.y, c0.z), P(c1.x, c1.y, c1.z), P(c2.x, c2.y, c2.z), P(c3.x, c3.y, c3.z), rotation * dir,
                        new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), sub);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(v);
                mesh.SetNormals(n);
                mesh.SetUVs(0, uv);
                mesh.SetUVs(1, uv2);
                mesh.subMeshCount = tris.Length;
                for (int i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // ------------------------------------------------------------------ Arch

        /// <summary>
        /// Round arch: two pillars and a semicircular opening. Base at y = 0, centred on x, depth along z.
        /// </summary>
        public static Mesh Arch(float span, float pillarWidth, float springHeight, float crown, float depth, int segments = 16)
            => Arch(span, pillarWidth, springHeight, crown, depth, 0f, 0, segments);

        /// <summary>
        /// Round arch, optionally ruined: <paramref name="breakAmount"/> (0..1) removes a jagged chunk from
        /// the top corner (side chosen by <paramref name="seed"/>). Geometry is finely subdivided so the
        /// break follows a ragged line.
        /// </summary>
        public static Mesh Arch(float span, float pillarWidth, float springHeight, float crown, float depth, float breakAmount, int seed, int segments = 16)
        {
            var mesh = BuildArch(span, pillarWidth, springHeight, crown, depth, segments, out var builder);
            if (breakAmount <= 0.001f) return mesh;

            float r = span * 0.5f, R = r + pillarWidth, H = springHeight + r + crown;
            float side = (seed & 1) == 0 ? 1f : -1f;
            var n = new Vector2(side, 1.3f).normalized;
            var corner = new Vector2(side * R, H);
            var p0 = corner - n * (Mathf.Clamp01(breakAmount) * (R + H) * 0.55f);
            float ox = seed * 3.17f, oy = seed * 1.91f;
            builder.Clip(c =>
            {
                float jag = (Mathf.PerlinNoise(c.x * 1.7f + ox, c.y * 1.7f + oy) - 0.5f) * 1.1f;
                return Vector2.Dot(new Vector2(c.x, c.y) - p0, n) + jag > 0f;
            });
            return builder.ToMesh("ArchRuined");
        }

        static Mesh BuildArch(float span, float pillarWidth, float springHeight, float crown, float depth, int segments, out Builder b)
        {
            float r = span * 0.5f;
            float R = r + pillarWidth;
            float H = springHeight + r + crown;
            float hz = depth * 0.5f;
            b = new Builder(1);
            var bb = b;
            const float piece = 0.45f; // subdivision length so ruins can break raggedly

            // Outline with the solid on the left (counter-clockwise).
            var loop = new List<Vector2> { new Vector2(-R, 0f), new Vector2(-r, 0f), new Vector2(-r, springHeight) };
            for (int k = 1; k < segments; k++)
            {
                float a = Mathf.PI * (1f - (float)k / segments);
                loop.Add(new Vector2(Mathf.Cos(a) * r, springHeight + Mathf.Sin(a) * r));
            }
            loop.Add(new Vector2(r, springHeight));
            loop.Add(new Vector2(r, 0f));
            loop.Add(new Vector2(R, 0f));
            loop.Add(new Vector2(R, H));
            loop.Add(new Vector2(-R, H));

            Vector2 UV(Vector2 p) => new Vector2((p.x + R) / (2f * R), p.y / H);

            // Side walls (extrude every outline edge).
            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 p = loop[i], q = loop[(i + 1) % loop.Count];
                Vector2 d = q - p;
                if (d.sqrMagnitude < 1e-8f) continue;
                var normal = new Vector3(d.y, -d.x, 0f).normalized;
                int parts = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / piece));
                for (int k = 0; k < parts; k++)
                {
                    Vector2 s0 = Vector2.Lerp(p, q, (float)k / parts), s1 = Vector2.Lerp(p, q, (float)(k + 1) / parts);
                    b.Quad(new Vector3(s0.x, s0.y, -hz), new Vector3(s1.x, s1.y, -hz), new Vector3(s1.x, s1.y, hz), new Vector3(s0.x, s0.y, hz),
                        normal, UV(s0), UV(s1), UV(s1), UV(s0));
                }
            }

            // Front and back faces: pillars + ring between intrados and outer rectangle.
            void Face(float z, Vector3 normal)
            {
                Rect(-R, 0f, -r, springHeight);
                Rect(r, 0f, R, springHeight);

                float cornerAngle = Mathf.Atan2(H - springHeight, R);
                var angles = new List<float>();
                for (int k = 0; k <= segments; k++) angles.Add(Mathf.PI * (1f - (float)k / segments));
                angles.Add(cornerAngle);
                angles.Add(Mathf.PI - cornerAngle);
                angles.Sort((x, y) => y.CompareTo(x));

                for (int k = 0; k < angles.Count - 1; k++)
                {
                    Vector2 i0 = Inner(angles[k]), i1 = Inner(angles[k + 1]);
                    Vector2 o0 = Outer(angles[k]), o1 = Outer(angles[k + 1]);
                    int parts = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Vector2.Distance(i0, o0), Vector2.Distance(i1, o1)) / piece));
                    for (int m = 0; m < parts; m++)
                    {
                        float t0 = (float)m / parts, t1 = (float)(m + 1) / parts;
                        Vector2 a0 = Vector2.Lerp(i0, o0, t0), a1 = Vector2.Lerp(i0, o0, t1);
                        Vector2 c0 = Vector2.Lerp(i1, o1, t0), c1 = Vector2.Lerp(i1, o1, t1);
                        bb.Quad(new Vector3(a0.x, a0.y, z), new Vector3(a1.x, a1.y, z), new Vector3(c1.x, c1.y, z), new Vector3(c0.x, c0.y, z),
                            normal, UV(a0), UV(a1), UV(c1), UV(c0));
                    }
                }

                void Rect(float x0, float y0, float x1, float y1)
                {
                    int rows = Mathf.Max(1, Mathf.CeilToInt((y1 - y0) / piece));
                    for (int m = 0; m < rows; m++)
                    {
                        float ya = Mathf.Lerp(y0, y1, (float)m / rows), yb = Mathf.Lerp(y0, y1, (float)(m + 1) / rows);
                        bb.Quad(new Vector3(x0, ya, z), new Vector3(x1, ya, z), new Vector3(x1, yb, z), new Vector3(x0, yb, z),
                            normal, UV(new Vector2(x0, ya)), UV(new Vector2(x1, ya)), UV(new Vector2(x1, yb)), UV(new Vector2(x0, yb)));
                    }
                }
            }

            Vector2 Inner(float a) => new Vector2(Mathf.Cos(a) * r, springHeight + Mathf.Sin(a) * r);
            Vector2 Outer(float a)
            {
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                float tx = Mathf.Abs(c) > 1e-5f ? R / Mathf.Abs(c) : float.MaxValue;
                float ty = s > 1e-5f ? (H - springHeight) / s : float.MaxValue;
                float t = Mathf.Min(tx, ty);
                return new Vector2(c * t, springHeight + s * t);
            }

            Face(-hz, Vector3.back);
            Face(hz, Vector3.forward);
            return b.ToMesh("Arch");
        }

        // ------------------------------------------------------------------ Rubble & urns

        /// <summary>Pile of tumbled stone blocks resting on y = 0.</summary>
        public static Mesh Rubble(int seed, int blocks, float width, float maxSize)
        {
            var rnd = new System.Random(seed);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var b = new Builder(1);
            for (int i = 0; i < blocks; i++)
            {
                float x = R(-0.5f, 0.5f) * width;
                // bigger blocks in the middle, smaller towards the edges
                float s = maxSize * R(0.35f, 1f) * (1f - Mathf.Abs(x) / width);
                var size = new Vector3(s * R(0.8f, 1.6f), s * R(0.5f, 1f), s * R(0.7f, 1.2f));
                var rot = Quaternion.Euler(R(-12f, 12f), R(-30f, 30f), R(-35f, 35f));
                b.Box(new Vector3(x, size.y * 0.35f, R(-0.3f, 0.3f)), size, rot);
            }
            return b.ToMesh("Rubble");
        }

        /// <summary>Surface of revolution around +y from a (radius, height) profile.</summary>
        public static Mesh Lathe(IReadOnlyList<Vector2> profile, int sides = 16, string name = "Lathe")
        {
            var b = new Builder(1);
            for (int i = 0; i < profile.Count - 1; i++)
            {
                Vector2 p = profile[i], q = profile[i + 1];
                Vector2 edge = q - p;
                var n2 = new Vector2(edge.y, -edge.x).normalized; // outward in the (r, y) plane
                for (int k = 0; k < sides; k++)
                {
                    float a0 = Mathf.PI * 2f * k / sides, a1 = Mathf.PI * 2f * (k + 1) / sides;
                    Vector3 D(float a) => new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Vector3 v0 = D(a0) * p.x + Vector3.up * p.y, v1 = D(a1) * p.x + Vector3.up * p.y;
                    Vector3 v2 = D(a1) * q.x + Vector3.up * q.y, v3 = D(a0) * q.x + Vector3.up * q.y;
                    Vector3 normal = (D((a0 + a1) * 0.5f) * n2.x + Vector3.up * n2.y).normalized;
                    b.Quad(v0, v1, v2, v3, normal, new Vector2(0, p.y), new Vector2(1, p.y), new Vector2(1, q.y), new Vector2(0, q.y));
                }
            }
            return b.ToMesh(name);
        }

        public static Mesh Urn(int seed, float height = 0.8f)
        {
            var rnd = new System.Random(seed);
            float w = 0.85f + (float)rnd.NextDouble() * 0.4f;
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.16f, 0f), new Vector2(0.24f, 0.12f), new Vector2(0.27f, 0.32f),
                new Vector2(0.21f, 0.52f), new Vector2(0.11f, 0.62f), new Vector2(0.1f, 0.7f), new Vector2(0.15f, 0.76f),
                new Vector2(0.13f, 0.8f), new Vector2(0f, 0.8f),
            };
            for (int i = 0; i < profile.Count; i++)
                profile[i] = new Vector2(profile[i].x * w, profile[i].y) * (height / 0.8f);
            return Lathe(profile, 14, "Urn");
        }

        // ------------------------------------------------------------------ Grass

        /// <summary>Clump of curved, tapering blades. uv.y = 0 at the root, 1 at the tip (for wind).</summary>
        /// <param name="edgeTaper">Distance over which blades shrink towards both ends of the clump (0 = hard ends),
        /// so grass thins out naturally at a platform's corners instead of stopping in a straight cut.</param>
        /// <param name="rootDepth">Roots start up to this far below y = 0, so the clump's base is ragged and
        /// overlaps the edge it grows from instead of sitting on it in one straight line.</param>
        public static Mesh Grass(int seed, int blades, float width, Vector2 heightRange, float bladeWidth = 0.05f, float edgeTaper = 0f, float rootDepth = 0f)
        {
            var rnd = new System.Random(seed);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var b = new Builder(1);
            const int segs = 5;

            for (int i = 0; i < blades; i++)
            {
                float x0 = R(-width * 0.5f, width * 0.5f);
                float z = R(-0.15f, 0.15f);
                float h = R(heightRange.x, heightRange.y);
                float w = bladeWidth * R(0.6f, 1.4f);
                float root = -R(0f, rootDepth);
                if (edgeTaper > 0f)
                {
                    float edge = Mathf.Clamp01((width * 0.5f - Mathf.Abs(x0)) / edgeTaper);
                    h *= Mathf.Lerp(0.2f, 1f, edge * edge * (3f - 2f * edge));
                }
                h -= root; // a sunk root still reaches the same tip height
                // Blades may lean, but never past the clump's edges (a clump on a platform must not overhang it).
                x0 = Mathf.Clamp(x0, -width * 0.5f + w, width * 0.5f - w);
                float lean = Mathf.Clamp(R(-0.45f, 0.45f) * h, -width * 0.5f - x0 + w, width * 0.5f - x0 - w);
                int prevL = -1, prevR = -1;
                for (int s = 0; s <= segs; s++)
                {
                    float t = (float)s / segs;
                    var center = new Vector3(x0 + lean * t * t, root + h * t, z);
                    float half = w * 0.5f * (1f - t * 0.95f);
                    int l = b.Vert(center + Vector3.left * half, Vector3.back, new Vector2(0f, t));
                    int r = b.Vert(center + Vector3.right * half, Vector3.back, new Vector2(1f, t));
                    if (s > 0)
                    {
                        b.Tri(prevL, l, r);
                        b.Tri(prevL, r, prevR);
                    }
                    prevL = l; prevR = r;
                }
            }
            return b.ToMesh("Grass");
        }

        // ------------------------------------------------------------------ Banner

        /// <summary>Hanging cloth with a torn bottom edge. Top edge at y = 0, uv.y = 1 at the top (pinned).</summary>
        public static Mesh Banner(int seed, float width, float height, int columns = 8, int rows = 10)
        {
            var rnd = new System.Random(seed);
            var b = new Builder(1);
            var lengths = new float[columns + 1];
            for (int c = 0; c <= columns; c++)
            {
                lengths[c] = height * (0.62f + 0.38f * (float)rnd.NextDouble());
                if (c == columns / 2) lengths[c] = height; // pointed middle
            }

            var idx = new int[columns + 1, rows + 1];
            for (int c = 0; c <= columns; c++)
            for (int r = 0; r <= rows; r++)
            {
                float u = (float)c / columns;
                float t = (float)r / rows;
                var p = new Vector3((u - 0.5f) * width * (1f - 0.12f * t), -lengths[c] * t, 0f);
                idx[c, r] = b.Vert(p, Vector3.back, new Vector2(u, 1f - t * lengths[c] / height));
            }
            for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
            {
                b.Tri(idx[c, r], idx[c + 1, r], idx[c + 1, r + 1]);
                b.Tri(idx[c, r], idx[c + 1, r + 1], idx[c, r + 1]);
            }
            return b.ToMesh("Banner");
        }

        // ------------------------------------------------------------------ Tree

        /// <summary>
        /// Gnarled branching tree. Submesh 0 = trunk/branches, submesh 1 = leaf cards.
        /// Leaves carry uv.y = 1 so they sway in the wind shader.
        /// </summary>
        public static Mesh Tree(int seed, float height, float trunkRadius, int depth = 5, int leavesPerTip = 14, float leafSize = 0.22f)
        {
            var rnd = new System.Random(seed);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var b = new Builder(2);

            void Branch(Vector3 start, Vector3 dir, float length, float radius, int level)
            {
                const int sides = 7;
                const int segs = 4;
                Vector3 pos = start;
                Vector3 d = dir.normalized;
                var ringPrev = new int[sides + 1];
                float endRadius = radius * 0.62f;

                for (int s = 0; s <= segs; s++)
                {
                    float t = (float)s / segs;
                    if (s > 0)
                    {
                        // Gnarl: bend the branch a little each segment, slight droop for thin limbs.
                        d = (d + new Vector3(R(-0.35f, 0.35f), R(-0.15f, 0.25f) - (level < 2 ? 0.12f : 0f), R(-0.2f, 0.2f))).normalized;
                        pos += d * (length / segs);
                    }
                    float rad = Mathf.Lerp(radius, endRadius, t);
                    Vector3 side = Vector3.Cross(d, Vector3.forward);
                    if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                    side.Normalize();
                    Vector3 up = Vector3.Cross(side, d).normalized;
                    var ring = new int[sides + 1];
                    for (int k = 0; k <= sides; k++)
                    {
                        float a = Mathf.PI * 2f * k / sides;
                        Vector3 nrm = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                        ring[k] = b.Vert(pos + nrm * rad, nrm, new Vector2((float)k / sides, 0f));
                    }
                    if (s > 0)
                    {
                        for (int k = 0; k < sides; k++)
                        {
                            b.Tri(ringPrev[k], ring[k], ring[k + 1], 0);
                            b.Tri(ringPrev[k], ring[k + 1], ringPrev[k + 1], 0);
                        }
                    }
                    ringPrev = ring;
                }

                if (level <= 0)
                {
                    Leaves(pos, length * 1.6f);
                    return;
                }

                int children = level > 3 ? 2 : (rnd.NextDouble() < 0.5 ? 2 : 3);
                for (int c = 0; c < children; c++)
                {
                    float spread = R(25f, 55f) * (c % 2 == 0 ? 1f : -1f) + R(-10f, 10f);
                    Vector3 cd = Quaternion.AngleAxis(spread, Vector3.forward) * d;
                    cd = (cd + Vector3.right * 0.25f + Vector3.forward * R(-0.3f, 0.3f)).normalized; // lean towards +x like the reference tree
                    Branch(pos, cd, length * R(0.62f, 0.8f), endRadius, level - 1);
                }
                if (level <= 2) Leaves(pos, length);
            }

            void Leaves(Vector3 center, float radius)
            {
                for (int i = 0; i < leavesPerTip; i++)
                {
                    var c = center + new Vector3(R(-1f, 1f) * radius, R(-0.6f, 0.7f) * radius * 0.7f, R(-0.5f, 0.5f) * radius);
                    float s = leafSize * R(0.6f, 1.3f);
                    float rot = R(0f, Mathf.PI * 2f);
                    // Maple leaf card facing the camera: uv = leaf texture (alpha-clipped), uv2.y = 1 so it sways.
                    Vector3 ax = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot), 0f) * s;
                    Vector3 ay = new Vector3(-Mathf.Sin(rot), Mathf.Cos(rot), 0f) * s;
                    var wind = new Vector2(0f, 1f);
                    int q0 = b.Vert(c - ax - ay, Vector3.back, new Vector2(0f, 0f), wind);
                    int q1 = b.Vert(c + ax - ay, Vector3.back, new Vector2(1f, 0f), wind);
                    int q2 = b.Vert(c + ax + ay, Vector3.back, new Vector2(1f, 1f), wind);
                    int q3 = b.Vert(c - ax + ay, Vector3.back, new Vector2(0f, 1f), wind);
                    b.Tri(q0, q2, q1, 1); b.Tri(q0, q3, q2, 1);
                }
            }

            Branch(Vector3.zero, new Vector3(0.15f, 1f, 0f), height * 0.38f, trunkRadius, depth);
            return b.ToMesh("Tree");
        }
    }
}
