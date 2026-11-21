using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// The scarf's strands as cloth of their own: each tongue that comes off the band (VeilRenderer.Strands) is a chain
    /// of nodes rooted on the scarf where it branches, carried by the same air (the vortices), held loosely in its
    /// place in the bundle - a place that winds round the band in a slow helix, so the strands wrap round it and
    /// round each other - rubbing against the others (they slide round each other instead of passing through) and
    /// falling against the body. Where the scarf hardens into a weapon the strands are laid back on their places.
    /// Pure C#, stepped after the scarf (<see cref="VeilSimulation"/>).
    /// </summary>
    public sealed class VeilStrands
    {
        /// <summary>A strand: where it runs along the scarf (0 neck .. 1 tip), which side of the band and how far
        /// out, its phase, how far behind the band (m) it lies.</summary>
        public struct Def
        {
            public float start, end, side, spread, phase, depth;
        }

        /// <summary>Nodes in each strand.</summary>
        public const int Nodes = 14;
        /// <summary>How far (m) a strand reaches out from the band at most (times its side).</summary>
        public const float Reach = 0.55f;
        /// <summary>Turns a strand's place makes round the band over its length (a gentle twist: the tongues fan out like a flame
        /// rather than winding into a tangle), how fast (rad/s) the fan sways round the band and how far (rad).</summary>
        public const float BraidTurns = 0.15f, BraidSpeed = 0.35f, BraidSway = 0.45f;
        /// <summary>Pull (1/s²) towards its place in the bundle, at the root and at the tail (the tail swirls freely).</summary>
        public const float HoldRoot = 60f, HoldTail = 16f;
        /// <summary>Air drag (1/s) and gravity's share on a strand.</summary>
        public const float Drag = 5f, GravityScale = 0.06f;
        /// <summary>How close (m) two strands come before they push apart (they rub, slide round each other).</summary>
        public const float Rub = 0.05f;
        /// <summary>How strongly (per iteration) a strand is straightened at each node: its stiffness against bending.</summary>
        public const float Bend = 0.3f;
        /// <summary>How far (m) the band's strands hang below its centre line (as drawn).</summary>
        public const float HangBelow = 0.08f;
        /// <summary>The scarf fraction a strand starts at, at least: off the wrap round the neck.</summary>
        public const float MinStart = 0.12f;

        public Def[] Defs { get; }
        public int Count => Defs.Length;
        /// <summary>Node positions, by strand, then node.</summary>
        public Vector3[][] Positions { get; }
        /// <summary>Where each node would lie in its place in the bundle (this step).</summary>
        public Vector3[][] Homes { get; }

        readonly Vector3[][] previous;
        readonly float[] rest;
        float time;
        bool started;

        public VeilStrands(Def[] defs)
        {
            Defs = defs;
            Positions = new Vector3[defs.Length][];
            Homes = new Vector3[defs.Length][];
            previous = new Vector3[defs.Length][];
            rest = new float[defs.Length];
            for (int s = 0; s < defs.Length; s++)
            {
                Positions[s] = new Vector3[Nodes];
                Homes[s] = new Vector3[Nodes];
                previous[s] = new Vector3[Nodes];
            }
        }

        /// <summary>The strands of the drawing (all but the band itself, which is the scarf).</summary>
        public static Def[] FromDrawing()
        {
            var all = VeilRenderer.Strands;
            var list = new System.Collections.Generic.List<Def>();
            for (int s = 0; s < all.Length; s++)
            {
                if (s == VeilRenderer.MainStrand) continue;
                var st = all[s];
                list.Add(new Def
                {
                    start = Mathf.Max(MinStart, st.start), end = st.end, side = st.side, spread = st.spread, phase = st.phase,
                    depth = (s - VeilRenderer.MainStrand) * 0.03f,
                });
            }
            return list.ToArray();
        }

        /// <summary>The scarf's centre line at fraction <paramref name="f"/>, and its direction there.</summary>
        public static Vector3 Along(VeilSimulation sim, float f, out Vector3 tangent, out float hardness)
        {
            int n = sim.Count;
            float fi = Mathf.Clamp01(f) * (n - 1);
            int i0 = Mathf.Min(Mathf.FloorToInt(fi), n - 2);
            float fr = fi - i0;
            var p = sim.Positions;
            tangent = p[Mathf.Min(i0 + 2, n - 1)] - p[Mathf.Max(i0 - 1, 0)];
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.down;
            tangent.Normalize();
            float h0 = sim.UseNodeHardness ? sim.NodeHardness[i0] : sim.Hardness;
            float h1 = sim.UseNodeHardness ? sim.NodeHardness[i0 + 1] : sim.Hardness;
            hardness = Mathf.Clamp01(Mathf.Lerp(h0, h1, fr));
            return Vector3.Lerp(p[i0], p[i0 + 1], fr);
        }

        /// <summary>
        /// A strand's place in the bundle: out from the band (on its side, growing from nothing where it branches) and
        /// turned round it by <paramref name="angle"/> (0: in the picture plane, as drawn flat; then behind the band,
        /// out on the other side, in front of it).
        /// </summary>
        public static Vector3 Place(Vector3 centre, Vector3 tangent, float side, float u, float angle, float depth)
        {
            var normal = new Vector3(-tangent.y, tangent.x, 0f);
            if (normal.sqrMagnitude < 1e-6f) normal = Vector3.up;
            normal.Normalize();
            // mirror-true: a positive side is below the band whichever way it streams
            if (normal.y > 0f) normal = -normal;
            Vector3 bi = Vector3.Cross(tangent, normal).normalized;
            float r = side * Reach * Mathf.SmoothStep(0f, 1f, u);
            Vector3 off = (normal * Mathf.Cos(angle) + bi * Mathf.Sin(angle)) * r;
            return centre + off + Vector3.down * (HangBelow * Mathf.SmoothStep(0f, 1f, u * 3f)) + new Vector3(0f, 0f, depth * Mathf.SmoothStep(0f, 1f, u * 3f));
        }

        /// <summary>The helix turn of strand node <paramref name="u"/> (0 root .. 1 tail) at <paramref name="t"/>.</summary>
        public static float BraidAngle(float u, float t, float phase) => BraidTurns * Mathf.PI * 2f * u + BraidSway * Mathf.Sin(BraidSpeed * t + phase) + phase * 0.05f;

        public void Step(float dt, VeilSimulation sim)
        {
            if (dt <= 0f || float.IsNaN(dt)) return;
            dt = Mathf.Min(dt, 0.1f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / Mathf.Max(0.001f, sim.Settings.maxSubstep)));
            float h = dt / steps;
            for (int k = 0; k < steps; k++) SubStep(h, sim);
        }

        /// <summary>Lays every strand on its place (a start, a teleport).</summary>
        public void Reset(VeilSimulation sim)
        {
            UpdateHomes(sim, out _);
            for (int s = 0; s < Count; s++)
                for (int j = 0; j < Nodes; j++) Positions[s][j] = previous[s][j] = Homes[s][j];
            started = true;
        }

        float[][] hard;

        void UpdateHomes(VeilSimulation sim, out float length)
        {
            length = sim.Settings.length * sim.LengthScale;
            if (hard == null || hard.Length != Count) { hard = new float[Count][]; for (int s = 0; s < Count; s++) hard[s] = new float[Nodes]; }
            for (int s = 0; s < Count; s++)
            {
                var d = Defs[s];
                rest[s] = (d.end - d.start) * length / (Nodes - 1);
                for (int j = 0; j < Nodes; j++)
                {
                    float u = (float)j / (Nodes - 1);
                    float f = Mathf.Lerp(d.start, d.end, u);
                    Vector3 c = Along(sim, f, out var tan, out float hd);
                    hard[s][j] = hd;
                    Homes[s][j] = Place(c, tan, d.side, u, BraidAngle(u, time, d.phase), d.depth);
                }
            }
        }

        void SubStep(float h, VeilSimulation sim)
        {
            time += h;
            if (!started || (Positions.Length > 0 && (Positions[0][0] - Along(sim, Defs[0].start, out _, out _)).sqrMagnitude > 25f)) { Reset(sim); return; }
            UpdateHomes(sim, out _);
            var settings = sim.Settings;
            Vector3 g = new Vector3(0f, -9.81f * GravityScale, 0f);
            Vector3 anchorVel = sim.AnchorVelocity;
            float zRest = sim.Positions[0].z + settings.depthOffset;

            for (int s = 0; s < Count; s++)
            {
                var pos = Positions[s];
                var prev = previous[s];
                var home = Homes[s];
                // the root rides on the scarf where the strand branches off it
                pos[0] = home[0];
                prev[0] = home[0] - anchorVel * h;
                for (int j = 1; j < Nodes; j++)
                {
                    float u = (float)j / (Nodes - 1);
                    Vector3 p = pos[j];
                    Vector3 vel = (p - prev[j]) / h;
                    float loose = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.7f, hard[s][j]));
                    // carried by the scarf's air (its vortices: felt relative to the hero only up to a point, as the
                    // band feels them) and its breeze streaming it back, held in its place in the bundle
                    float f = Mathf.Lerp(Defs[s].start, Defs[s].end, u);
                    Vector2 air = sim.SampleAirVelocity(p) - new Vector2(anchorVel.x, anchorVel.y);
                    if (air.sqrMagnitude > settings.maxRelativeWind * settings.maxRelativeWind) air = air.normalized * settings.maxRelativeWind;
                    Vector3 airV = anchorVel + new Vector3(air.x, air.y, 0f);
                    if (sim.DriftDirection != 0f) airV += new Vector3(sim.DriftDirection * settings.drift, settings.driftLift, 0f) * Mathf.SmoothStep(0f, 1f, f * 4f);
                    float hold = Mathf.Lerp(HoldRoot, HoldTail, u);
                    Vector3 acc = (airV - vel) * Drag + (home[j] - p) * hold + g;
                    acc.z += (zRest - p.z) * settings.depthStiffness * 0.3f;
                    Vector3 next = p + (p - prev[j]) * 0.985f + acc * (h * h);
                    // hardening into a weapon, the strand is laid back on its place
                    next = Vector3.Lerp(home[j], next, loose);
                    prev[j] = Vector3.Lerp(home[j] - anchorVel * h, p, loose);
                    pos[j] = next;
                }
            }

            // they hang together: each segment its length; strands rub against each other and the body
            for (int it = 0; it < 4; it++)
            {
                for (int s = 0; s < Count; s++)
                {
                    var pos = Positions[s];
                    float r = rest[s];
                    for (int j = 0; j < Nodes - 1; j++)
                    {
                        Vector3 d = pos[j + 1] - pos[j];
                        float len = d.magnitude;
                        if (len < 1e-6f) continue;
                        Vector3 corr = d * ((len - r) / len);
                        if (j == 0) pos[1] -= corr;
                        else { pos[j] += corr * 0.5f; pos[j + 1] -= corr * 0.5f; }
                    }
                    // cloth resists bending: each node is drawn towards the middle of its neighbours, so a strand
                    // curves instead of folding into a sharp V (it broke into pieces that didn't join up)
                    for (int j = 1; j < Nodes - 1; j++)
                        pos[j] += ((pos[j - 1] + pos[j + 1]) * 0.5f - pos[j]) * Bend;
                }
                RubTogether();
            }
            CollideWithBody(sim);
            ClampDepth(zRest, settings.maxDepth);
            // hardening into a weapon, the strands are laid on their places after all the rest
            for (int s = 0; s < Count; s++)
                for (int j = 1; j < Nodes; j++)
                {
                    float set = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.7f, hard[s][j]));
                    if (set <= 0f) continue;
                    Vector3 shift = (Homes[s][j] - Positions[s][j]) * set;
                    Positions[s][j] += shift;
                    previous[s][j] += shift;
                }
        }

        /// <summary>
        /// Two strands closer than <see cref="Rub"/> push apart (nodes at about the same place along them): they slide
        /// round each other, so as their places turn round the band they wind over and under, not through each other.
        /// </summary>
        void RubTogether()
        {
            float rub2 = Rub * Rub;
            for (int a = 0; a < Count; a++)
                for (int b = a + 1; b < Count; b++)
                {
                    var pa = Positions[a]; var pb = Positions[b];
                    for (int j = 1; j < Nodes; j++)
                        for (int k = Mathf.Max(1, j - 1); k <= Mathf.Min(Nodes - 1, j + 1); k++)
                        {
                            Vector3 d = pb[k] - pa[j];
                            float d2 = d.sqrMagnitude;
                            if (d2 >= rub2 || d2 < 1e-12f) continue;
                            float dist = Mathf.Sqrt(d2);
                            Vector3 push = d * ((Rub - dist) / dist * 0.5f);
                            pa[j] -= push;
                            pb[k] += push;
                        }
                }
        }

        void CollideWithBody(VeilSimulation sim)
        {
            var body = sim.Body;
            if (body.Count == 0) return;
            float skin = sim.Settings.bodySkin;
            for (int s = 0; s < Count; s++)
            {
                var pos = Positions[s];
                for (int j = 1; j < Nodes; j++)
                    for (int c = 0; c < body.Count; c++)
                    {
                        var cap = body[c];
                        Vector3 axis = VeilSimulation.ClosestOnSegment(cap.a, cap.b, pos[j]);
                        Vector3 off = pos[j] - axis;
                        float dist = off.magnitude, reach = cap.radius + skin;
                        if (dist >= reach) continue;
                        Vector3 n = dist > 1e-5f ? off / dist : Vector3.back;
                        pos[j] += n * (reach - dist);
                    }
            }
        }

        void ClampDepth(float zRest, float maxDepth)
        {
            for (int s = 0; s < Count; s++)
            {
                var pos = Positions[s];
                for (int j = 1; j < Nodes; j++) pos[j].z = Mathf.Clamp(pos[j].z, zRest - maxDepth, zRest + maxDepth);
            }
        }

        /// <summary>Strand <paramref name="s"/> at <paramref name="u"/> (0 root .. 1 tail), smoothly between its nodes.</summary>
        public Vector3 At(int s, float u)
        {
            var pos = Positions[s];
            float fi = Mathf.Clamp01(u) * (Nodes - 1);
            int i1 = Mathf.Min(Mathf.FloorToInt(fi), Nodes - 2);
            float t = fi - i1;
            return VeilRenderer.CatmullRom(pos[Mathf.Max(i1 - 1, 0)], pos[i1], pos[i1 + 1], pos[Mathf.Min(i1 + 2, Nodes - 1)], t);
        }
    }
}
