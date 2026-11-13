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
    }
}
