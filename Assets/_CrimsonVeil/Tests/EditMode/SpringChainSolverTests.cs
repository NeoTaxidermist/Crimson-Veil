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
    }
}
