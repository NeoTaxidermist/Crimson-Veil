using System.Linq;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class VeilSimulationTests
    {
        const float Dt = 1f / 60f;

        static VeilSimulation Run(VeilSimulation sim, float seconds, Vector3 start, Vector3 velocity)
        {
            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 1; i <= frames; i++) sim.Step(Dt, start + velocity * (i * Dt));
            return sim;
        }

        [Test]
        public void Root_IsPinnedToAnchor()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            Run(sim, 1f, Vector3.zero, new Vector3(6f, 0f, 0f));
            Assert.AreEqual(6f, sim.Positions[0].x, 1e-3f);
            Assert.AreEqual(0f, sim.Positions[0].y, 1e-3f);
        }

        [Test]
        public void Segments_KeepRestLength()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            Run(sim, 2f, Vector3.zero, new Vector3(8f, 0f, 0f));
            for (int i = 0; i < sim.Count - 1; i++)
            {
                var d = sim.Positions[i + 1] - sim.Positions[i];
                d.z = 0f; // depth flutter is cosmetic
                Assert.AreEqual(sim.FlowRestLength, d.magnitude, sim.FlowRestLength * 0.2f, $"segment {i}");
            }
        }

        [Test]
        public void Running_VeilTrailsBehind()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            Run(sim, 1.5f, Vector3.zero, new Vector3(8f, 0f, 0f));
            Assert.Less(sim.Positions[sim.Count - 1].x, sim.Positions[0].x - 1f);
        }

        [Test]
        public void Running_ScarfUndulatesInsteadOfStretchingIntoASheet()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            float sum = 0f, tipDistance = 0f;
            int frames = 0;
            for (int i = 1; i <= 150; i++)
            {
                sim.Step(Dt, new Vector3(8f * i * Dt, 0f, 0f));
                if (i < 60) continue;
                // Largest distance of any node from the straight neck → tip line.
                Vector3 a = sim.Positions[0], b = sim.Positions[sim.Count - 1];
                float maxDev = 0f;
                for (int k = 1; k < sim.Count - 1; k++)
                    maxDev = Mathf.Max(maxDev, CrimsonVeil.Combat.VeilCombatProfile.SegmentDistance(a, b, sim.Positions[k]));
                sum += maxDev;
                tipDistance += Vector3.Distance(a, b);
                frames++;
            }
            Assert.Greater(sum / frames, 0.2f, "a living scarf curves; a cape is a straight line");
            Assert.Less(tipDistance / frames, sim.Settings.length * 0.97f, "not pulled taut to full length");
        }

        [Test]
        public void Vortices_AlternateSign()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            Run(sim, 2f, Vector3.zero, new Vector3(5f, 0f, 0f));
            var signs = sim.Vortices.Select(v => Mathf.Sign(v.circulation)).ToList();
            Assert.GreaterOrEqual(signs.Count, 4);
            for (int i = 1; i < signs.Count; i++) Assert.AreNotEqual(signs[i - 1], signs[i]);
        }

        [Test]
        public void SheddingFrequency_GrowsWithSpeed()
        {
            var still = Run(new VeilSimulation(new VeilSettings(), Vector3.zero), 2f, Vector3.zero, Vector3.zero);
            var fast = Run(new VeilSimulation(new VeilSettings(), Vector3.zero), 2f, Vector3.zero, new Vector3(9f, 0f, 0f));
            Assert.Greater(still.ShedCount, 0, "the veil should swirl even when idle");
            Assert.Greater(fast.ShedCount, still.ShedCount * 2);
        }

        [Test]
        public void LambOseen_IsTangentialAndFinite()
        {
            Assert.AreEqual(Vector2.zero, VeilSimulation.InducedVelocity(Vector2.zero, Vector2.zero, 3f, 0.2f));

            var p = new Vector2(0.5f, 0f);
            var v = VeilSimulation.InducedVelocity(p, Vector2.zero, 3f, 0.2f);
            Assert.AreEqual(0f, Vector2.Dot(v, p), 1e-5f, "velocity must be perpendicular to radius");
            Assert.Greater(v.y, 0f, "positive circulation spins counter-clockwise");

            float far = VeilSimulation.InducedVelocity(new Vector2(10f, 0f), Vector2.zero, 3f, 0.2f).magnitude;
            Assert.AreEqual(3f / (2f * Mathf.PI * 10f), far, 1e-4f, "far field is a point vortex");

            float nearCore = VeilSimulation.InducedVelocity(new Vector2(0.001f, 0f), Vector2.zero, 3f, 0.2f).magnitude;
            Assert.Less(nearCore, 1f, "core is regularised (no singularity)");
        }

        [Test]
        public void Vortices_DieAfterLifetime()
        {
            var settings = new VeilSettings { vortexLifetime = 0.5f };
            var sim = new VeilSimulation(settings, Vector3.zero);
            Run(sim, 2f, Vector3.zero, Vector3.zero);
            Assert.IsTrue(sim.Vortices.All(v => v.age < 0.5f));
        }

        [Test]
        public void FullHardness_ConvergesToTargetShape()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            for (int i = 0; i < sim.Count; i++) sim.Targets[i] = new Vector3(i * 0.1f, 0f, 0f);
            sim.RecomputeTargetRest();
            sim.Hardness = 1f;
            Run(sim, 2f, Vector3.zero, Vector3.zero);

            float worst = 0f;
            for (int i = 0; i < sim.Count; i++)
            {
                var d = sim.Positions[i] - sim.Targets[i];
                d.z = 0f;
                worst = Mathf.Max(worst, d.magnitude);
            }
            Assert.Less(worst, 0.12f);
        }

        [Test]
        public void HugeTimeStep_StaysFinite()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            sim.Step(10f, new Vector3(1f, 0f, 0f));
            sim.Step(float.NaN, Vector3.zero);
            Assert.IsTrue(sim.Positions.All(p => !float.IsNaN(p.x) && !float.IsInfinity(p.x)));
        }

        [Test]
        public void Teleport_ResetsChainAtNewAnchor()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            sim.Step(Dt, new Vector3(100f, 0f, 0f));
            Assert.IsTrue(sim.Positions.All(p => Mathf.Abs(p.x - 100f) < sim.Settings.length + 0.5f));
        }
    }
}
