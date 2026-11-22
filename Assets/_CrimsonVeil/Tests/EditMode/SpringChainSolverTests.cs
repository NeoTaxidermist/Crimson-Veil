using CrimsonVeil.Core;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class SpringChainSolverTests
    {
        static Vector3[] Rest(Vector3 root) => new[] { root, root + new Vector3(0f, -0.1f, 0.0f), root + new Vector3(0f, -0.2f, 0f), root + new Vector3(0f, -0.3f, 0f) };

        [Test]
        public void StillAnchor_StaysNearRest()
        {
            var s = new SpringChainSolver(Rest(Vector3.zero)) { Gravity = Vector3.zero };
            for (int i = 0; i < 120; i++) s.Step(1f / 60f, Rest(Vector3.zero));
            Assert.Less(Vector3.Distance(s.Positions[3], new Vector3(0f, -0.3f, 0f)), 1e-3f);
        }

        [Test]
        public void MovingAnchor_TipLagsBehindThenCatchesUp()
        {
            var s = new SpringChainSolver(Rest(Vector3.zero)) { Gravity = Vector3.zero };
            Vector3 root = Vector3.zero;
            for (int i = 0; i < 10; i++)
            {
                root += new Vector3(0.08f, 0f, 0f);
                s.Step(1f / 60f, Rest(root));
            }
            Assert.Less(s.Positions[3].x, root.x - 0.02f, "the tip trails behind the motion");
            for (int i = 0; i < 180; i++) s.Step(1f / 60f, Rest(root));
            Assert.Less(Mathf.Abs(s.Positions[3].x - root.x), 0.01f, "and settles back");
        }

        [Test]
        public void SegmentLengths_ArePreserved()
        {
            var s = new SpringChainSolver(Rest(Vector3.zero));
            Vector3 root = Vector3.zero;
            for (int i = 0; i < 60; i++)
            {
                root += new Vector3(Mathf.Sin(i * 0.4f) * 0.2f, Mathf.Cos(i * 0.3f) * 0.2f, 0f);
                s.Step(1f / 60f, Rest(root));
            }
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(s.SegmentLength(i), Vector3.Distance(s.Positions[i], s.Positions[i + 1]), 1e-4f);
            Assert.AreEqual(root, s.Positions[0], "root is pinned");
        }

        [Test]
        public void PushOut_LeavesAPointOutside_AndPutsOneInsideOnTheSurface()
        {
            var c = new SpringChainSolver.Capsule(Vector3.zero, Vector3.down, 0.1f);
            Assert.AreEqual(new Vector3(0.3f, -0.5f, 0f), SpringChainSolver.PushOut(new Vector3(0.3f, -0.5f, 0f), c));
            var p = SpringChainSolver.PushOut(new Vector3(0.04f, -0.5f, 0f), c);
            Assert.AreEqual(0.1f, p.x, 1e-4f, "pushed straight out sideways");
            Assert.AreEqual(-0.5f, p.y, 1e-4f);
            var cap = SpringChainSolver.PushOut(new Vector3(0f, 0.05f, 0f), c);
            Assert.AreEqual(0.1f, cap.y, 1e-4f, "past the end, out of its round cap");
        }

        [Test]
        public void AChainSwungIntoALeg_StaysOutsideIt()
        {
            var s = new SpringChainSolver(Rest(Vector3.zero)) { Gravity = new Vector3(0f, 0f, 20f) };   // blown backwards
            var leg = new SpringChainSolver.Capsule(new Vector3(0f, -0.05f, 0.15f), new Vector3(0f, -0.6f, 0.15f), 0.1f);
            s.Colliders.Add(leg);
            for (int i = 0; i < 120; i++) s.Step(1f / 60f, Rest(Vector3.zero));
            for (int k = 1; k < s.Count; k++)
                Assert.AreEqual(s.Positions[k], SpringChainSolver.PushOut(s.Positions[k], leg), "point " + k + " is not inside the leg");
            Assert.Greater(s.Positions[3].z, 0.01f, "(it was blown back, against the leg)");
        }

        [Test]
        public void MaxBend_KeepsEverySegmentWithinItsAngleOfRest()
        {
            var s = new SpringChainSolver(Rest(Vector3.zero)) { Gravity = new Vector3(60f, 0f, 0f), MaxBend = 30f };
            for (int i = 0; i < 120; i++) s.Step(1f / 60f, Rest(Vector3.zero));
            for (int k = 1; k < s.Count; k++)
            {
                float a = Vector3.Angle(Vector3.down, s.Positions[k] - s.Positions[k - 1]);
                Assert.LessOrEqual(a, 30.5f, "segment " + k + " swung past its limit");
            }
            Assert.Greater(Vector3.Angle(Vector3.down, s.Positions[1] - s.Positions[0]), 20f, "(it was blown hard to the side)");
        }
            [Test]
        public void ALockOfHair_IsFirmWhereItGrows_AndLooseAtItsTip()
        {
            Assert.AreEqual(12f, SpringChainSolver.BendLimit(1, 4, 12f, 40f), 1e-4f, "the first segment: the root's limit");
            Assert.AreEqual(40f, SpringChainSolver.BendLimit(3, 4, 12f, 40f), 1e-4f, "the last: the tip's");
            Assert.AreEqual(26f, SpringChainSolver.BendLimit(2, 4, 12f, 40f), 1e-4f, "eased between them");
            Assert.AreEqual(12f, SpringChainSolver.BendLimit(3, 4, 12f, 0f), 1e-4f, "no limit of its own at the tip: the same all along");

            // a lock standing up, thrown hard sideways: each segment stays within its own limit of its rest direction
            var rest = new[] { Vector3.zero, new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.15f, 0f) };
            var chain = new SpringChainSolver(rest) { Stiffness = 2f, TipStiffness = 1f, Damping = 0.95f, Gravity = new Vector3(400f, 0f, 0f), MaxBend = 12f, TipBend = 40f };
            for (int f = 0; f < 60; f++) chain.Step(1f / 60f, rest);
            for (int i = 1; i < chain.Count; i++)
            {
                float bent = Vector3.Angle(rest[i] - rest[i - 1], chain.Positions[i] - chain.Positions[i - 1]);
                Assert.LessOrEqual(bent, SpringChainSolver.BendLimit(i, chain.Count, 12f, 40f) + 0.1f, "segment " + i);
            }
            Assert.Greater(Vector3.Angle(rest[3] - rest[2], chain.Positions[3] - chain.Positions[2]), 30f, "the tip swings far");
            Assert.Less(Vector3.Angle(rest[1] - rest[0], chain.Positions[1] - chain.Positions[0]), 12.2f, "the root hardly at all");
        }
    }
}
