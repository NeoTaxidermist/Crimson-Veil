using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The scarf as cloth in three dimensions: wound round the neck, free to swing in depth (pulled back towards its
    /// place behind the hero, within reach of it), falling against the body instead of through it.
    /// </summary>
    [Category("Veil")]
    public class Veil3DTests
    {
        const float Dt = 1f / 60f;

        static VeilSimulation Make(out VeilSettings s)
        {
            s = new VeilSettings();
            return new VeilSimulation(s, Vector3.zero) { DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f };
        }

        [Test]
        public void TheScarf_IsWoundRoundTheNeck_FromTheThroatRoundTheNearSideToTheBack()
        {
            var sim = Make(out var s);
            sim.NeckForward = Vector3.right;   // facing right
            for (int i = 0; i < 120; i++) sim.Step(Dt, Vector3.zero);
            for (int i = 1; i <= s.wrapNodes; i++)
            {
                var p = sim.Positions[i];
                Assert.AreEqual(s.neckRadius, new Vector2(p.x, p.z).magnitude, 1e-3f, $"node {i} on the ring round the neck");
                Assert.AreEqual(0f, p.y, 1e-3f, $"node {i} level round it");
                Assert.IsTrue(sim.IsWrapped(i));
            }
            Assert.Greater(sim.Positions[1].x, s.neckRadius * 0.99f, "it starts at the throat");
            Assert.Less(sim.Positions[2].z, -0.05f, "goes round the side towards the camera");
            Assert.Less(sim.Positions[s.wrapNodes].x, 0f, "and on to the back of the neck");
            Assert.IsFalse(sim.IsWrapped(s.wrapNodes + 1), "the rest streams off the wrap");
        }

        [Test]
        public void TheLooseCloth_MovesInDepth_YetStaysNearItsPlaceBehindTheHero()
        {
            var sim = Make(out var s);
            float min = 9f, max = -9f, moved = 0f;
            var last = new float[sim.Count];
            for (int f = 0; f < 600; f++)
            {
                sim.Step(Dt, Vector3.zero);
                if (f < 120) continue;
                for (int i = s.wrapNodes + 1; i < sim.Count; i++)
                {
                    float z = sim.Positions[i].z;
                    min = Mathf.Min(min, z); max = Mathf.Max(max, z);
                    if (f > 120) moved += Mathf.Abs(z - last[i]);
                    last[i] = z;
                }
            }
            Assert.Greater(max - min, 0.05f, "the cloth lives in depth, not flat in a plane");
            Assert.Greater(moved, 1f, "and moves in it");
            Assert.Less(max, s.depthOffset + s.maxDepth + 1e-3f, "within reach of its place behind the hero");
            Assert.Greater(min, s.depthOffset - s.maxDepth - 1e-3f);
        }

        [Test]
        public void TheLooseCloth_FallsAgainstTheBody_NotThroughIt()
        {
            // no breeze: the cloth hangs down from the neck - through a trunk standing in its way
            var s = new VeilSettings();
            var sim = new VeilSimulation(s, Vector3.zero);
            var trunk = new VeilSimulation.BodyCapsule(new Vector3(-0.05f, -1.2f, 0.1f), new Vector3(-0.05f, -0.15f, 0.1f), 0.16f);
            sim.Body.Add(trunk);
            for (int f = 0; f < 300; f++) sim.Step(Dt, Vector3.zero);
            float worst = 9f;
            for (int i = s.wrapNodes + 1; i < sim.Count; i++)
            {
                var axis = VeilSimulation.ClosestOnSegment(trunk.a, trunk.b, sim.Positions[i]);
                worst = Mathf.Min(worst, Vector3.Distance(sim.Positions[i], axis));
            }
            Assert.Greater(worst, trunk.radius, "every node outside the trunk (with the cloth's skin off it)");

            // without the body the same cloth hangs right through where the trunk would be
            var free = new VeilSimulation(new VeilSettings(), Vector3.zero);
            for (int f = 0; f < 300; f++) free.Step(Dt, Vector3.zero);
            float inside = 9f;
            for (int i = s.wrapNodes + 1; i < free.Count; i++)
                inside = Mathf.Min(inside, Vector3.Distance(free.Positions[i], VeilSimulation.ClosestOnSegment(trunk.a, trunk.b, free.Positions[i])));
            Assert.Less(inside, trunk.radius, "(the test's trunk is in the cloth's way)");
        }

        [Test]
        public void ASetWeapon_StaysInFrontOfTheBody_AndOffTheNeck()
        {
            var s = new VeilSettings();
            var sim = new VeilSimulation(s, Vector3.zero) { Hardness = 1f };
            for (int i = 0; i < sim.Count; i++) sim.Targets[i] = new Vector3(i * 0.06f, -0.2f, 0f);
            sim.RecomputeTargetRest();
            for (int f = 0; f < 120; f++) sim.Step(Dt, Vector3.zero);
            for (int i = 1; i < sim.Count; i++)
            {
                Assert.AreEqual(-s.weaponDepth, sim.Positions[i].z, 1e-3f, $"node {i} held in front of the body");
                Assert.IsFalse(sim.IsWrapped(i), $"node {i}: a weapon is not wound round the neck");
            }
        }

        [Test]
        public void TheWrapRing_StepsASegmentAlongItself()
        {
            float r = 0.085f, seg = 0.1f;
            var p1 = VeilSimulation.WrapRingPoint(Vector3.zero, Vector3.up, Vector3.right, Vector3.back, r, seg, 1);
            var p2 = VeilSimulation.WrapRingPoint(Vector3.zero, Vector3.up, Vector3.right, Vector3.back, r, seg, 2);
            Assert.AreEqual(new Vector3(r, 0f, 0f).x, p1.x, 1e-5f);
            float arc = Mathf.Acos(Vector3.Dot(p1.normalized, p2.normalized)) * r;
            Assert.AreEqual(seg, arc, 1e-4f, "a segment's length along the ring");
            var flipped = VeilSimulation.WrapRingPoint(Vector3.zero, Vector3.up, Vector3.left, Vector3.back, r, seg, 2);
            Assert.Less(flipped.z, 0f, "facing left it still goes round the side towards the camera");
        }
    }
}
