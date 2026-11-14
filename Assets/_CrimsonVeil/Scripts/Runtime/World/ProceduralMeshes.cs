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
            var mesh = BuildArch(span, pillarWidth, springHeight, crown, depth, segments, out var builder, seed);
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

        static Mesh BuildArch(float span, float pillarWidth, float springHeight, float crown, float depth, int segments, out Builder b, int seed = 0)
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
            ArchMasonry(b, r, pillarWidth, springHeight, H, hz, seed);
            return b.ToMesh("Arch");
        }

        /// <summary>
        /// Carved detail on the camera side of an arch (key art): a ring of wedge voussoirs with a keystone,
        /// impost cornices and plinths on the piers, staggered ashlar courses with recessed joints, a top
        /// cornice and ivy hanging from the ring and the crown. All of it stands proud of the front face, so
        /// the stone relief shading draws every block.
        /// </summary>
        static void ArchMasonry(Builder b, float r, float pillarWidth, float spring, float H, float hz, int seed)
        {
            var rnd = new System.Random(seed * 7919 + 13);
            float Rn(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            float u = pillarWidth / 0.8f;              // unit: 1 at Near scale
            float R = r + pillarWidth;
            float z = -hz;                             // front face
            float ring = Mathf.Max(0.12f, Mathf.Min(Mathf.Min(pillarWidth * 0.8f, r * 0.45f), H - spring - r - 0.06f));

            // Voussoirs: wedges between r and r + ring, small angular joints, the keystone taller and prouder.
            int count = Mathf.Max(9, Mathf.RoundToInt(Mathf.PI * (r + ring * 0.5f) / (0.42f * u)) | 1);
            float gap = 0.035f * u / (r + ring * 0.5f);
            for (int k = 0; k < count; k++)
            {
                float a0 = Mathf.PI * k / count + gap, a1 = Mathf.PI * (k + 1) / count - gap;
                bool key = k == count / 2;
                float outer = Mathf.Min(r + ring * (key ? 1.35f : Rn(0.92f, 1.06f)), H - spring - 0.02f); // never above the wall
                float proud = (key ? 0.16f : Rn(0.05f, 0.1f)) * u;
                Wedge(b, spring, r, outer, Mathf.PI - a1, Mathf.PI - a0, z, proud);
            }

            // Piers: plinth, ashlar courses, impost cornice.
            foreach (float side in new[] { -1f, 1f })
            {
                float x0 = side < 0 ? -R : r, x1 = side < 0 ? -r : R, cx = (x0 + x1) * 0.5f, w = x1 - x0;
                Slab(b, cx, 0.25f * u, w + 0.22f * u, 0.5f * u, z, 0.14f * u);                          // plinth
                Slab(b, cx, 0.58f * u, w + 0.12f * u, 0.12f * u, z, 0.09f * u);                          // plinth moulding
                Slab(b, cx, spring - 0.05f * u, w + 0.3f * u, 0.18f * u, z, 0.16f * u);                  // impost
                Slab(b, cx, spring - 0.22f * u, w + 0.14f * u, 0.12f * u, z, 0.1f * u);
                Courses(b, rnd, x0, x1, 0.7f * u, spring - 0.3f * u, z, u);
            }

            // Spandrels above the ring: courses that stop at the voussoirs, then a top cornice.
            float y = spring + 0.1f * u, ch = 0.42f * u;
            int row = 0;
            while (y + ch < H - 0.35f * u)
            {
                float yc = y + ch * 0.5f;
                float dy = yc - spring;
                float reach = dy < r + ring ? Mathf.Sqrt(Mathf.Max(0f, (r + ring) * (r + ring) - dy * dy)) + 0.06f * u : -1f;
                if (reach < 0f) StoneRow(b, rnd, -R, R, y, ch, z, u, row);
                else
                {
                    if (R - reach > 0.3f * u) StoneRow(b, rnd, -R, -reach, y, ch, z, u, row);
                    if (R - reach > 0.3f * u) StoneRow(b, rnd, reach, R, y, ch, z, u, row);
                }
                y += ch; row++;
            }
            Slab(b, 0f, H - 0.12f * u, 2f * R + 0.3f * u, 0.24f * u, z, 0.18f * u);                      // cornice
            for (float x = -R; x < R - 0.2f * u; x += 0.5f * u)                                           // dentils
                Slab(b, x + 0.2f * u, H - 0.32f * u, 0.2f * u, 0.14f * u, z, 0.1f * u);

            // Ivy: strands with leaves hanging from the ring and the cornice.
            int strands = 3 + rnd.Next(4);
            for (int s = 0; s < strands; s++)
            {
                bool fromRing = rnd.NextDouble() < 0.6;
                float x = fromRing ? Rn(-r * 0.8f, r * 0.8f) : Rn(-R, R);
                float top = fromRing ? spring + Mathf.Sqrt(Mathf.Max(0f, r * r - x * x)) : H - 0.2f * u;
                float len = Rn(0.8f, fromRing ? 2.6f : 3.4f) * u;
                Vine(b, rnd, new Vector3(x, top, z - 0.2f * u), len, u);
            }
        }

        /// <summary>Wedge stone between radii r0..r1 and angles a0..a1 around (0, spring), standing proud by <paramref name="proud"/>.</summary>
        static void Wedge(Builder b, float spring, float r0, float r1, float a0, float a1, float z, float proud)
        {
            Vector3 P(float rad, float a, float zz) => new Vector3(Mathf.Cos(a) * rad, spring + Mathf.Sin(a) * rad, zz);
            float zf = z - proud;
            Vector3 i0 = P(r0, a0, zf), i1 = P(r0, a1, zf), o1 = P(r1, a1, zf), o0 = P(r1, a0, zf);
            b.Quad(i0, i1, o1, o0, Vector3.back, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            // Sides back to the wall, each facing away from the stone's centre.
            var centre = (i0 + i1 + o1 + o0) * 0.25f;
            void Side(Vector3 p, Vector3 q)
            {
                var pb = new Vector3(p.x, p.y, z); var qb = new Vector3(q.x, q.y, z);
                var e = q - p;
                var nrm = new Vector3(e.y, -e.x, 0f).normalized;
                var outward = (p + q) * 0.5f - centre;
                if (Vector3.Dot(nrm, new Vector3(outward.x, outward.y, 0f)) < 0f) nrm = -nrm;
                b.Quad(p, q, qb, pb, nrm, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            Side(i0, i1); Side(i1, o1); Side(o1, o0); Side(o0, i0);
        }

        /// <summary>A box standing proud of the wall plane z, centred at (cx, cy).</summary>
        static void Slab(Builder b, float cx, float cy, float w, float h, float z, float proud)
            => b.Box(new Vector3(cx, cy, z - proud * 0.5f + 0.01f), new Vector3(w, h, proud + 0.02f), Quaternion.identity);

        /// <summary>
        /// A dressed stone standing proud of the wall plane z with chamfered front edges: the 45° chamfers face
        /// the camera, so the top one catches the moon and the bottom/left ones fall into shade — each block
        /// reads clearly inside a flat silhouette.
        /// </summary>
        static void Stone(Builder b, Vector2 c, float w, float h, float z, float proud, float bevel, float tiltDeg = 0f)
        {
            bevel = Mathf.Min(bevel, proud * 0.9f, w * 0.3f, h * 0.3f);
            var rot = Quaternion.Euler(0f, 0f, tiltDeg);
            Vector3 P(float x, float y, float zz) => new Vector3(c.x, c.y, 0f) + rot * new Vector3(x, y, 0f) + new Vector3(0f, 0f, zz);
            float hx = w * 0.5f, hy = h * 0.5f, ix = hx - bevel, iy = hy - bevel;
            float zf = z - proud, zb = zf + bevel, zw = z + 0.01f;
            Vector3 f0 = P(-ix, -iy, zf), f1 = P(ix, -iy, zf), f2 = P(ix, iy, zf), f3 = P(-ix, iy, zf);
            Vector3 o0 = P(-hx, -hy, zb), o1 = P(hx, -hy, zb), o2 = P(hx, hy, zb), o3 = P(-hx, hy, zb);
            Vector3 w0 = P(-hx, -hy, zw), w1 = P(hx, -hy, zw), w2 = P(hx, hy, zw), w3 = P(-hx, hy, zw);
            Vector2 U = Vector2.zero;
            b.Quad(f0, f1, f2, f3, Vector3.back, U, U, U, U);
            Vector3 up = rot * Vector3.up, right = rot * Vector3.right;
            b.Quad(f3, f2, o2, o3, (up + Vector3.back).normalized, U, U, U, U);      // top chamfer
            b.Quad(f0, f1, o1, o0, (-up + Vector3.back).normalized, U, U, U, U);     // bottom chamfer
            b.Quad(f1, f2, o2, o1, (right + Vector3.back).normalized, U, U, U, U);   // right chamfer
            b.Quad(f0, f3, o3, o0, (-right + Vector3.back).normalized, U, U, U, U);  // left chamfer
            b.Quad(o3, o2, w2, w3, up, U, U, U, U);
            b.Quad(o0, o1, w1, w0, -up, U, U, U, U);
            b.Quad(o1, o2, w2, w1, right, U, U, U, U);
            b.Quad(o0, o3, w3, w0, -right, U, U, U, U);
        }

        static void Courses(Builder b, System.Random rnd, float x0, float x1, float y0, float y1, float z, float u)
        {
            float ch = 0.42f * u;
            int row = 0;
            for (float y = y0; y + ch * 0.6f < y1; y += ch, row++)
                StoneRow(b, rnd, x0, x1, y, Mathf.Min(ch, y1 - y), z, u, row);
        }

        /// <summary>One course of ashlar between x0..x1, joints staggered every other row, stones slightly uneven.</summary>
        static void StoneRow(Builder b, System.Random rnd, float x0, float x1, float y, float ch, float z, float u, int row)
        {
            float joint = 0.04f * u;
            float x = x0 + (row % 2 == 1 ? 0.3f * u : 0f);
            if (x > x0 + 0.05f) Lay(x0, x);
            while (x < x1 - 0.05f)
            {
                float w = (0.55f + (float)rnd.NextDouble() * 0.5f) * u;
                float xe = Mathf.Min(x1, x + w);
                if (x1 - xe < 0.25f * u) xe = x1;
                Lay(x, xe);
                x = xe;
            }
            void Lay(float a, float c)
            {
                if (c - a < joint * 2f) return;
                if (rnd.NextDouble() < 0.07) return;   // a missing block
                float proud = (0.06f + (float)rnd.NextDouble() * 0.05f) * u;
                float sag = (float)rnd.NextDouble() * 0.03f * u;
                Stone(b, new Vector2((a + c) * 0.5f, y + ch * 0.5f - sag), c - a - joint, ch - joint, z, proud, 0.07f * u,
                    ((float)rnd.NextDouble() - 0.5f) * 2.5f);
            }
        }

        /// <summary>Ivy strand hanging from <paramref name="top"/>: a thin swaying ribbon with alternating leaves.</summary>
        static void Vine(Builder b, System.Random rnd, Vector3 top, float len, float u)
        {
            const int segs = 8;
            float curl = ((float)rnd.NextDouble() - 0.5f) * 0.6f * u;
            float w = 0.035f * u;
            int pl = -1, pr = -1;
            for (int s = 0; s <= segs; s++)
            {
                float t = (float)s / segs;
                var c = top + new Vector3(curl * Mathf.Sin(t * 3f), -len * t, 0f);
                float half = w * (1f - t * 0.7f);
                int l = b.Vert(c + Vector3.left * half, Vector3.back, new Vector2(0f, t));
                int rr = b.Vert(c + Vector3.right * half, Vector3.back, new Vector2(1f, t));
                // Hanging downwards, so the winding is the reverse of an upright grass blade (front faces -z).
                if (s > 0) { b.Tri(pl, rr, l); b.Tri(pl, pr, rr); }
                pl = l; pr = rr;
                if (s > 0 && s < segs)
                {
                    float dir = s % 2 == 0 ? 1f : -1f, leaf = (0.16f - 0.06f * t) * u;
                    var a = b.Vert(c, Vector3.back, Vector2.zero);
                    var p1 = b.Vert(c + new Vector3(dir * leaf * 0.5f, leaf * 0.45f, 0f), Vector3.back, Vector2.zero);
                    var p2 = b.Vert(c + new Vector3(dir * leaf, -leaf * 0.1f, 0f), Vector3.back, Vector2.zero);
                    var p3 = b.Vert(c + new Vector3(dir * leaf * 0.45f, -leaf * 0.4f, 0f), Vector3.back, Vector2.zero);
                    if (dir > 0f) { b.Tri(a, p1, p2); b.Tri(a, p2, p3); }
                    else { b.Tri(a, p2, p1); b.Tri(a, p3, p2); } // mirrored leaf: flip to keep facing the camera
                }
            }
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
