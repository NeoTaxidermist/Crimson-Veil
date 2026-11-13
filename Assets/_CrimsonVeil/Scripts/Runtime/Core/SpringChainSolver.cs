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

        public int Count => Positions.Length;

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

            // Keep segment lengths (parent wins, so the root never stretches).
            for (int i = 1; i < n; i++)
            {
                Vector3 d = Positions[i] - Positions[i - 1];
                float len = d.magnitude;
                if (len > 1e-6f) Positions[i] = Positions[i - 1] + d * (lengths[i - 1] / len);
            }
        }
    }
}
