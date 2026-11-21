using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The scarf's strands as cloth of their own: rooted on the band where they come off it, streaming back with it,
    /// winding round it (over and under each other: in depth), rubbing instead of passing through each other, off the
    /// body, and laid back on their places as the scarf hardens into a weapon.
    /// </summary>
    public class VeilStrandsTests
    {
        const float Dt = 1f / 60f;

        static (VeilSimulation sim, VeilStrands strands) Make()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero)
            {
                DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f, UseNodeHardness = true,
            };
            return (sim, new VeilStrands(VeilStrands.FromDrawing()));
        }

        static void Run(VeilSimulation sim, VeilStrands strands, int frames, System.Action each = null)
        {
            for (int f = 0; f < frames; f++) { sim.Step(Dt, Vector3.zero); strands.Step(Dt, sim); each?.Invoke(); }
        }

        [Test]
        public void EveryStrandButTheBand_IsSimulated_RootedOnTheBandOffTheNeck()
        {
            var (sim, strands) = Make();
            Assert.AreEqual(VeilRenderer.Strands.Length - 1, strands.Count, "every tongue of the drawing, the band is the scarf itself");
            Run(sim, strands, 120);
            for (int s = 0; s < strands.Count; s++)
            {
                var d = strands.Defs[s];
                Assert.GreaterOrEqual(d.start, VeilStrands.MinStart, "off the wrap round the neck");
                Vector3 onBand = VeilStrands.Along(sim, d.start, out _, out _);
                Assert.Less(Vector3.Distance(strands.Positions[s][0], onBand), 0.15f, $"strand {s} comes off the band");
            }
        }

        [Test]
        public void TheStrands_StreamBackWithTheScarf_TheirEndsBehindIt()
        {
            var (sim, strands) = Make();
            Run(sim, strands, 300);
            for (int s = 0; s < strands.Count; s++)
            {
                var d = strands.Defs[s];
                float tip = strands.Positions[s][VeilStrands.Nodes - 1].x;
                float bandThere = VeilStrands.Along(sim, d.end, out _, out _).x;
                Assert.Less(tip, -0.8f, $"strand {s} streams back behind the hero");
                Assert.AreEqual(bandThere, tip, 0.9f, $"strand {s} ends about where the band does at its fraction");
            }
        }

        [Test]
        public void TheStrands_WindRoundTheBand_OverAndUnderEachOther()
        {
            var (sim, strands) = Make();
            int front = 0, behind = 0;
            float closest = 9f;
            Run(sim, strands, 1500, () =>
            {
                // the middle of strand 2 against the band beside it: in front of it, then behind it, as its place winds round
                int j = VeilStrands.Nodes / 2;
                var d = strands.Defs[2];
                Vector3 band = VeilStrands.Along(sim, Mathf.Lerp(d.start, d.end, 0.5f), out _, out _);
                float dz = strands.Positions[2][j].z - band.z;
                if (dz < -0.05f) front++;
                if (dz > 0.05f) behind++;
                for (int a = 0; a < strands.Count; a++)
                    for (int b = a + 1; b < strands.Count; b++)
                        for (int k = 2; k < VeilStrands.Nodes; k++)
                            closest = Mathf.Min(closest, Vector3.Distance(strands.Positions[a][k], strands.Positions[b][k]));
            });
            Assert.Greater(front, 50, "the strand comes round in front of the band");
            Assert.Greater(behind, 50, "and goes round behind it");
            Assert.Greater(closest, VeilStrands.Rub * 0.5f, "the strands rub, they don't pass through each other");
        }

        [Test]
        public void TheStrands_FallAgainstTheBody()
        {
            var (sim, strands) = Make();
            sim.DriftDirection = 0f; sim.AmbientFlow = Vector2.zero; sim.RestShapeWeight = 0f;   // hanging down
            var trunk = new VeilSimulation.BodyCapsule(new Vector3(-0.05f, -1.4f, 0.1f), new Vector3(-0.05f, -0.2f, 0.1f), 0.16f);
            sim.Body.Add(trunk);
            Run(sim, strands, 300);
            for (int s = 0; s < strands.Count; s++)
                for (int j = 1; j < VeilStrands.Nodes; j++)
                {
                    var p = strands.Positions[s][j];
                    Assert.GreaterOrEqual(Vector3.Distance(p, VeilSimulation.ClosestOnSegment(trunk.a, trunk.b, p)), trunk.radius - 1e-3f, $"strand {s} node {j}");
                }
        }

        [Test]
        public void HardenedIntoAWeapon_TheStrandsLieOnTheirPlaces()
        {
            var (sim, strands) = Make();
            Run(sim, strands, 120);
            for (int i = 0; i < sim.Count; i++) { sim.NodeHardness[i] = 1f; sim.Targets[i] = new Vector3(i * 0.06f, -0.2f, 0f); }
            sim.Hardness = 1f;
            sim.RecomputeTargetRest();
            Run(sim, strands, 60);
            for (int s = 0; s < strands.Count; s++)
                for (int j = 1; j < VeilStrands.Nodes; j++)
                    Assert.Less(Vector3.Distance(strands.Positions[s][j], strands.Homes[s][j]), VeilStrands.Rub * 1.5f, $"strand {s} node {j} on its place");
        }

        [Test]
        public void APlaceInTheBundle_GrowsFromTheBand_AndTurnsRoundIt()
        {
            Vector3 c = Vector3.zero, tan = Vector3.left;
            Assert.AreEqual(c + Vector3.down * 0f, VeilStrands.Place(c, tan, 1f, 0f, 0f, 0f), "on the band where it branches");
            var flat = VeilStrands.Place(c, tan, 1f, 1f, 0f, 0f);
            var turned = VeilStrands.Place(c, tan, 1f, 1f, Mathf.PI * 0.5f, 0f);
            Assert.AreEqual(0f, flat.z, 1e-5f, "at angle 0 in the picture plane (as drawn flat)");
            Assert.Greater(Mathf.Abs(turned.z), VeilStrands.Reach * 0.9f, "a quarter turn round: out of the plane");
        }
    }
}
