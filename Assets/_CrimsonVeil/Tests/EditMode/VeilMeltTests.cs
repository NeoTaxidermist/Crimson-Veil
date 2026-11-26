using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The transformations between the scarf and the big weapons (shield, bow) are continuous.</summary>
    [Category("Veil")]
    public class VeilMeltTests
    {
        const int N = 40;
        const float Length = 3.2f;

        static Vector2[] Ring()
        {
            var p = new Vector2[N];
            for (int i = 0; i < N; i++)
            {
                float a = (float)i / (N - 1) * Mathf.PI * 2f * 0.97f;
                p[i] = VeilFormLibrary.ShieldCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * VeilFormLibrary.ShieldRadius;
            }
            return p;
        }

        [Test]
        public void MeltChain_StartsAtTheWeaponAndEndsOnTheScarf()
        {
            var from = Ring();
            var pts = new Vector2[N];
            var guide = new float[N];
            VeilTransform.MeltChain(from, 0f, Length, 0f, pts, guide);
            for (int i = 0; i < N; i++) Assert.Less(Vector2.Distance(pts[i] - pts[0], from[i] - from[0]), 1e-3f, "starts as the weapon");
            VeilTransform.MeltChain(from, 1f, Length, 0f, pts, guide);
            for (int i = 0; i < N; i++)
                Assert.Less(Vector2.Distance(pts[i], VeilTransform.ClothRestPoint((float)i / (N - 1), Length, 0f)), 1e-3f, "ends on the scarf");
            for (int i = 1; i < N; i++) Assert.AreEqual(0f, guide[i], 1e-4f, "handed over to the cloth at the end");
        }

        [Test]
        public void MeltChain_NeverSqueezesTheCloth()
        {
            // The crumpled lump came from nodes travelling in straight lines: segments shrank to nothing.
            var from = Ring();
            var pts = new Vector2[N];
            var guide = new float[N];
            float minCloth = float.MaxValue;
            for (int i = 0; i < N - 1; i++)
                minCloth = Mathf.Min(minCloth, Vector2.Distance(VeilTransform.ClothRestPoint((float)i / (N - 1), Length, 0f), VeilTransform.ClothRestPoint((float)(i + 1) / (N - 1), Length, 0f)));
            for (int k = 0; k <= 20; k++)
            {
                VeilTransform.MeltChain(from, k / 20f, Length, 0f, pts, guide);
                for (int i = 0; i < N - 1; i++)
                    Assert.GreaterOrEqual(Vector2.Distance(pts[i], pts[i + 1]), minCloth * 0.99f, $"segment {i} at melt {k / 20f}");
            }
        }

        [Test]
        public void MeltChain_MovesSmoothly()
        {
            var from = Ring();
            var a = new Vector2[N]; var b = new Vector2[N];
            var g = new float[N];
            for (int k = 0; k < 50; k++)
            {
                VeilTransform.MeltChain(from, k / 50f, Length, 0f, a, g);
                VeilTransform.MeltChain(from, (k + 1) / 50f, Length, 0f, b, g);
                for (int i = 0; i < N; i++) Assert.Less(Vector2.Distance(a[i], b[i]), 0.6f, $"node {i} jumps at melt {k / 50f}");
            }
        }

        [Test]
        public void MeltProgress_NeckLetsGoFirst()
        {
            Assert.Greater(VeilTransform.MeltProgress(0.4f, 0f), VeilTransform.MeltProgress(0.4f, 1f));
            Assert.AreEqual(1f, VeilTransform.MeltProgress(1f, 1f), 1e-4f);
            Assert.AreEqual(0f, VeilTransform.MeltProgress(0f, 0f), 1e-4f);
        }

        [Test]
        public void ShieldRing_UnfurlsFromTheHand()
        {
            var ring = Ring();
            var grip = VeilFormLibrary.Hand;
            var p = (Vector2[])ring.Clone();
            VeilTransform.SweepRing(p, 0, VeilFormLibrary.ShieldCenter, grip, 0f);
            for (int i = 0; i < N; i++) Assert.Less(Vector2.Distance(p[i], grip), 1e-3f, "gathered at the palm");
            p = (Vector2[])ring.Clone();
            VeilTransform.SweepRing(p, 0, VeilFormLibrary.ShieldCenter, grip, 0.5f);
            float reach = 0f;
            Vector2 c = VeilFormLibrary.ShieldCenter;
            for (int i = 1; i < N; i++) reach += Mathf.Abs(Vector2.SignedAngle(p[i - 1] - c, p[i] - c));
            Assert.AreEqual(0.97f * 360f * 0.5f, reach, 10f, "half way round at half way");
            p = (Vector2[])ring.Clone();
            VeilTransform.SweepRing(p, 0, VeilFormLibrary.ShieldCenter, grip, 1f);
            for (int i = 0; i < N; i++) Assert.AreEqual(ring[i], p[i], "closed ring at the end");
        }

        [Test]
        public void ShieldMelt_SpinsTheRingOpenAtTheBack()
        {
            var from = new Vector2[N];
            from[0] = Vector2.zero; // the neck
            var c = VeilFormLibrary.ShieldCenter;
            for (int i = 1; i < N; i++)
            {
                float a = -20f * Mathf.Deg2Rad + (float)(i - 1) / (N - 2) * Mathf.PI * 2f * 0.97f; // starts at the hand, in front
                from[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * VeilFormLibrary.ShieldRadius;
            }
            float spin = VeilTransform.ShieldOpenSpin(from, 1, c);
            Assert.That(spin, Is.InRange(0f, 360f), "turned on in the ring's own sense");
            var into = new Vector2[N];
            VeilTransform.SpinRing(from, into, 1, c, spin);
            Vector2 d = into[1] - c;
            Assert.AreEqual(180f, Mathf.Abs(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg), 1f, "the ring opens at the hero's back");
            Assert.AreEqual(from[0], into[0], "the neck stays put");
            Assert.AreEqual(Vector2.Distance(from[5], c), Vector2.Distance(into[5], c), 1e-4f, "a spin, not a squash");
        }

        [Test]
        public void ShieldMelt_UncurlsAllAtOnce_OtherWeaponsNeckFirst()
        {
            float shieldGap = VeilTransform.MeltProgress(0.5f, 0f, VeilTransform.MeltSpread(VeilForm.Shield))
                              - VeilTransform.MeltProgress(0.5f, 1f, VeilTransform.MeltSpread(VeilForm.Shield));
            float bowGap = VeilTransform.MeltProgress(0.5f, 0f, VeilTransform.MeltSpread(VeilForm.Bow))
                           - VeilTransform.MeltProgress(0.5f, 1f, VeilTransform.MeltSpread(VeilForm.Bow));
            Assert.Less(shieldGap, 0.3f, "the ring opens as one spring: no tight coil left at its end");
            Assert.Greater(bowGap, 0.5f, "a bow melts from the neck");
        }

        [Test]
        public void NeckBand_StaysUntilTheWeaponHasTakenShape()
        {
            // On the first frame of a shield cast the band vanished and the scarf hung torn off the neck.
            Assert.AreEqual(0f, VeilRenderer.NeckBandShaped(0f), 1e-4f);
            Assert.AreEqual(0f, VeilRenderer.NeckBandShaped(0.4f), 1e-4f);
            Assert.AreEqual(1f, VeilRenderer.NeckBandShaped(1f), 1e-4f);
        }

        [Test]
        public void MeltingCloth_IsDrawnInFrontOfTheBody()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            for (int i = 0; i < sim.Count; i++) { sim.GuideWeight[i] = 1f; sim.GuideTargets[i] = new Vector3(0.1f, -0.1f * i, 0f); }
            sim.Step(1f / 60f, Vector3.zero);
            for (int i = 1; i < sim.Count; i++)
            {
                Assert.Less(sim.Positions[i].z, 0f, $"node {i} in front of the hero (not hidden behind the torso)");
                Assert.Less(Vector2.Distance(sim.Positions[i], sim.GuideTargets[i]), 1e-3f, $"node {i} on its path");
            }
        }

        [Test]
        public void BigShapes_TakeAFewFramesToForm()
        {
            Assert.GreaterOrEqual(VeilTransform.FormMorphTime(VeilForm.Shield, VeilCasting.CastMorphTime), 0.3f);
            Assert.GreaterOrEqual(VeilTransform.FormMorphTime(VeilForm.Bow, VeilCasting.CastMorphTime), 0.24f);
            Assert.AreEqual(VeilCasting.CastMorphTime, VeilTransform.FormMorphTime(VeilForm.Sword, VeilCasting.CastMorphTime));
        }
    }
}
