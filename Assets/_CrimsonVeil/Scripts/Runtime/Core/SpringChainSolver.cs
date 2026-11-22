using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// Verlet "jiggle" chain for secondary motion (hair locks, tassels). Particle 0 is pinned to the
    /// animated root; the others swing with inertia and gravity and are pulled back towards the
    /// animated rest pose with a stiffness that softens towards the tip.
    /// </summary>
    public sealed class SpringChainSolver
    {
        public readonly Vector3[] Positions;
        readonly Vector3[] previous;
        readonly float[] lengths;

        public float Stiffness = 9f;
        public float TipStiffness = 2.5f;
        public float Damping = 0.88f;
        public Vector3 Gravity = new Vector3(0f, -4f, 0f);
        /// <summary>Furthest (degrees) a segment may swing from its rest direction; 0 = no limit. Cloth stays cloth.</summary>
        public float MaxBend = 0f;
        /// <summary>The same limit at the chain's tip: it eases from <see cref="MaxBend"/> at the first segment to this
        /// (0 = MaxBend all along). A lock of hair is firm where it grows and loose at its end.</summary>
        public float TipBend = 0f;

        /// <summary>The furthest (degrees) segment <paramref name="i"/> (1 = the first) of a chain of <paramref name="count"/>
        /// points may swing from its rest direction.</summary>
        public static float BendLimit(int i, int count, float maxBend, float tipBend)
            => tipBend > 0f && count > 2 ? Mathf.Lerp(maxBend, tipBend, (i - 1f) / (count - 2f)) : maxBend;

        public int Count => Positions.Length;

        /// <summary>Capsules the chain is kept out of (a cloth panel off the legs): ends a, b and radius.</summary>
        public readonly System.Collections.Generic.List<Capsule> Colliders = new System.Collections.Generic.List<Capsule>();

        public struct Capsule
        {
            public Vector3 a, b;
            public float radius;
            public Capsule(Vector3 a, Vector3 b, float radius) { this.a = a; this.b = b; this.radius = radius; }
        }

        /// <summary>The point pushed straight out of the capsule if it is inside it (unchanged otherwise).</summary>
        public static Vector3 PushOut(Vector3 p, in Capsule c)
        {
            Vector3 ab = c.b - c.a;
            float t = ab.sqrMagnitude > 1e-10f ? Mathf.Clamp01(Vector3.Dot(p - c.a, ab) / ab.sqrMagnitude) : 0f;
            Vector3 closest = c.a + ab * t;
            Vector3 d = p - closest;
            float len = d.magnitude;
            if (len >= c.radius) return p;
            if (len < 1e-6f) return closest + Vector3.forward * c.radius;
            return closest + d * (c.radius / len);
        }

        void Collide()
        {
            if (Colliders.Count == 0) return;
            for (int i = 1; i < Count; i++)
                foreach (var c in Colliders) Positions[i] = PushOut(Positions[i], c);
        }

        public SpringChainSolver(Vector3[] restPositions)
        {
            int n = restPositions.Length;
            Positions = (Vector3[])restPositions.Clone();
            previous = (Vector3[])restPositions.Clone();
            lengths = new float[Mathf.Max(0, n - 1)];
            for (int i = 0; i < n - 1; i++) lengths[i] = Vector3.Distance(restPositions[i], restPositions[i + 1]);
        }

        public float SegmentLength(int i) => lengths[i];

        public void Reset(Vector3[] rest)
        {
            for (int i = 0; i < Count; i++) Positions[i] = previous[i] = rest[i];
        }

        /// <param name="rest">Where the chain would be if it were rigid (follows the animated head).</param>
        public void Step(float dt, Vector3[] rest)
        {
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 1f / 30f);
            int n = Count;
            Positions[0] = previous[0] = rest[0];

            for (int i = 1; i < n; i++)
            {
                Vector3 p = Positions[i];
                Vector3 v = (p - previous[i]) * Damping;
                previous[i] = p;
                p += v + Gravity * (dt * dt);

                float t = (float)i / (n - 1);
                float k = Mathf.Lerp(Stiffness, TipStiffness, t);
                p = Vector3.Lerp(p, rest[i], 1f - Mathf.Exp(-k * dt));
                Positions[i] = p;
            }

            Collide();
            // Keep segment lengths (parent wins, so the root never stretches).
            for (int i = 1; i < n; i++)
            {
                Vector3 d = Positions[i] - Positions[i - 1];
                float len = d.magnitude;
                if (len <= 1e-6f) continue;
                Vector3 dir = d / len;
                if (MaxBend > 0f)
                {
                    Vector3 r = rest[i] - rest[i - 1];
                    if (r.sqrMagnitude > 1e-10f)
                        dir = Vector3.RotateTowards(r.normalized, dir, BendLimit(i, n, MaxBend, TipBend) * Mathf.Deg2Rad, 0f);
                }
                Positions[i] = Positions[i - 1] + dir * lengths[i - 1];
            }
            Collide();
        }
    }
}
