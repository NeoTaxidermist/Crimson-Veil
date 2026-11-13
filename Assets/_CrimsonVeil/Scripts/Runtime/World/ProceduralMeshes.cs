using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// Code-generated meshes for the grey-box-to-art pass: round arches, grass clumps,
    /// tattered banners and branching trees. Deterministic per seed.
    /// </summary>
    public static class ProceduralMeshes
    {
        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int>[] tris;

            public Builder(int submeshes)
            {
                tris = new List<int>[submeshes];
                for (int i = 0; i < submeshes; i++) tris[i] = new List<int>();
            }

            public int Vert(Vector3 p, Vector3 normal, Vector2 texcoord)
            {
                v.Add(p); n.Add(normal); uv.Add(texcoord);
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

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(v);
                mesh.SetNormals(n);
                mesh.SetUVs(0, uv);
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
        {
            float r = span * 0.5f;
            float R = r + pillarWidth;
            float H = springHeight + r + crown;
            float hz = depth * 0.5f;
            var b = new Builder(1);

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
                b.Quad(new Vector3(p.x, p.y, -hz), new Vector3(q.x, q.y, -hz), new Vector3(q.x, q.y, hz), new Vector3(p.x, p.y, hz),
                    normal, UV(p), UV(q), UV(q), UV(p));
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
                    b.Quad(new Vector3(i0.x, i0.y, z), new Vector3(o0.x, o0.y, z), new Vector3(o1.x, o1.y, z), new Vector3(i1.x, i1.y, z),
                        normal, UV(i0), UV(o0), UV(o1), UV(i1));
                }

                void Rect(float x0, float y0, float x1, float y1)
                {
                    b.Quad(new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z),
                        normal, UV(new Vector2(x0, y0)), UV(new Vector2(x1, y0)), UV(new Vector2(x1, y1)), UV(new Vector2(x0, y1)));
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

        // ------------------------------------------------------------------ Grass

        /// <summary>Clump of curved, tapering blades. uv.y = 0 at the root, 1 at the tip (for wind).</summary>
        public static Mesh Grass(int seed, int blades, float width, Vector2 heightRange, float bladeWidth = 0.05f)
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
                float lean = R(-0.45f, 0.45f) * h;
                float w = bladeWidth * R(0.6f, 1.4f);
                int prevL = -1, prevR = -1;
                for (int s = 0; s <= segs; s++)
                {
                    float t = (float)s / segs;
                    var center = new Vector3(x0 + lean * t * t, h * t, z);
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
                    // Maple-ish diamond leaf card (4 tris), facing the camera (-z).
                    Vector3 ax = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot), 0f) * s;
                    Vector3 ay = new Vector3(-Mathf.Sin(rot), Mathf.Cos(rot), 0f) * s * 0.75f;
                    int m = b.Vert(c, Vector3.back, new Vector2(0.5f, 1f));
                    int p0 = b.Vert(c + ax, Vector3.back, new Vector2(1f, 1f));
                    int p1 = b.Vert(c + ay, Vector3.back, new Vector2(0.5f, 1f));
                    int p2 = b.Vert(c - ax * 0.6f, Vector3.back, new Vector2(0f, 1f));
                    int p3 = b.Vert(c - ay, Vector3.back, new Vector2(0.5f, 1f));
                    b.Tri(m, p0, p1, 1); b.Tri(m, p1, p2, 1); b.Tri(m, p2, p3, 1); b.Tri(m, p3, p0, 1);
                }
            }

            Branch(Vector3.zero, new Vector3(0.15f, 1f, 0f), height * 0.38f, trunkRadius, depth);
            return b.ToMesh("Tree");
        }
    }
}
