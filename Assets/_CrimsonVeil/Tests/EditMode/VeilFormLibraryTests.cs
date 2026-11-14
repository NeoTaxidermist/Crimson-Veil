using System;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class VeilFormLibraryTests
    {
        static readonly VeilForm[] Weapons = { VeilForm.Whip, VeilForm.Sword, VeilForm.Spear, VeilForm.Shield, VeilForm.Bow };

        [TestCaseSource(nameof(Weapons))]
        public void Shape_StartsAtNeckAndIsEvenlySpaced(VeilForm form)
        {
            var pts = new Vector2[20];
            float handFraction = VeilFormLibrary.BuildShape(form, pts);

            Assert.AreEqual(Vector2.zero, pts[0]);
            Assert.That(handFraction, Is.InRange(0f, 1f));

            float first = Vector2.Distance(pts[0], pts[1]);
            // The bow has two sharp corners by design (where the string meets the limb tips): a node that
            // straddles one is closer to its neighbour along the chord. Nothing else may be uneven.
            // (The spear has one: the scarf comes down to its butt and the shaft runs back forward through the hand.)
            // (The shield has one: the reach out from the hand turns onto its ring.)
            int allowedCorners = form == VeilForm.Bow ? 2 : form == VeilForm.Spear || form == VeilForm.Shield ? 1 : 0, corners = 0;
            for (int i = 1; i < pts.Length - 1; i++)
            {
                float d = Vector2.Distance(pts[i], pts[i + 1]);
                if (Mathf.Abs(d - first) <= first * 0.35f) continue;
                corners++;
                Assert.Greater(d, first * 0.2f, $"{form} segment {i} collapsed");
                Assert.LessOrEqual(corners, allowedCorners, $"{form} segment {i}");
            }
        }

        [TestCaseSource(nameof(Weapons))]
        public void WeaponForms_AreHarderThanFlow(VeilForm form)
        {
            Assert.Greater(VeilFormLibrary.Hardness(form), VeilFormLibrary.Hardness(VeilForm.Flow));
            Assert.Greater(VeilFormLibrary.AttackDuration(form), 0f);
        }

        [Test]
        public void Whip_IsSofterThanSword()
        {
            Assert.Less(VeilFormLibrary.Hardness(VeilForm.Whip), VeilFormLibrary.Hardness(VeilForm.Sword));
        }

        [Test]
        public void Shield_IsAnOpenRingRoundTheWholeHero()
        {
            var pts = new Vector2[48];
            float hf = VeilFormLibrary.BuildShape(VeilForm.Shield, pts);
            var c = VeilFormLibrary.ShieldCenter;
            Assert.Less(c.magnitude, 0.8f, "centred on the body (the anchor is at the neck)");
            float swept = 0f, minY = 9f, maxY = -9f;
            int start = Mathf.CeilToInt(hf * (pts.Length - 1));
            for (int i = start; i < pts.Length; i++)
            {
                Assert.AreEqual(VeilFormLibrary.ShieldRadius, Vector2.Distance(pts[i], c), 0.03f, "one clean ring");
                if (i > start) swept += Vector2.SignedAngle(pts[i - 1] - c, pts[i] - c);
                minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y);
            }
            Assert.That(Mathf.Abs(swept), Is.InRange(290f, 340f), "nearly all the way round, leaving a small gap");
            Assert.Greater(maxY, 0.15f, "over the head");
            Assert.Less(minY, -1.4f, "down to the feet");
        }

        [Test]
        public void ShieldRing_CirclesRoundTheHero()
        {
            var c = VeilFormLibrary.ShieldCenter;
            var p = c + Vector2.right * VeilFormLibrary.ShieldRadius;
            var a = VeilWeaponPose.Idle(VeilForm.Shield, p, true, 0f);
            var b = VeilWeaponPose.Idle(VeilForm.Shield, p, true, 0.25f);
            float turned = Vector2.SignedAngle(a - c, b - c);
            Assert.AreEqual(VeilFormLibrary.ShieldSpin * 0.25f, turned, 3f, "a quarter second later it has gone round");
            Assert.AreEqual(VeilFormLibrary.ShieldRadius, Vector2.Distance(b, c), 0.05f, "staying on its ring");
        }

        [Test]
        public void Sword_IsAStraightBlade()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Sword, out int start);
            Vector2 a = pts[start], b = pts[pts.Count - 1];
            for (int i = start; i < pts.Count; i++)
                Assert.Less(LineDistance(a, b, pts[i]), 1e-4f, "every point of the blade on one line");
            Assert.AreEqual(VeilFormLibrary.SwordLength, Vector2.Distance(a, b), 1e-3f);
        }

        static float LineDistance(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(a + ab * t, p);
        }

        [Test]
        public void HardeningWave_TravelsFromNeckToTip()
        {
            Assert.AreEqual(0f, VeilTransform.Wave(0f, 0.5f), 1e-5f);
            Assert.AreEqual(1f, VeilTransform.Wave(1f, 1f), 1e-5f);
            Assert.Greater(VeilTransform.Wave(0.4f, 0.1f), VeilTransform.Wave(0.4f, 0.9f), "neck hardens first");
            for (float p = 0f; p <= 1f; p += 0.1f)
                Assert.GreaterOrEqual(VeilTransform.Wave(p + 0.1f, 0.5f) + 1e-6f, VeilTransform.Wave(p, 0.5f), "monotonic");
            Assert.Less(VeilTransform.TipSoftness(1f), 1f, "the tip stays a bit soft");
            Assert.AreEqual(1f, VeilTransform.TipSoftness(0.5f), 1e-5f);
        }

        [Test]
        public void Morph_StartsAtOldShapeAndEndsAtNewShape()
        {
            var a = new Vector2(0.2f, -1f);
            var b = new Vector2(1.5f, 0.6f);
            Assert.AreEqual(a, VeilTransform.MorphNode(a, b, 0f, 0.5f, 3.3f));
            Assert.AreEqual(b, VeilTransform.MorphNode(a, b, 1f, 0.5f, 3.3f));
            Assert.Greater(VeilTransform.MorphProgress(0.5f, 0.1f), VeilTransform.MorphProgress(0.5f, 0.9f), "neck leads the morph");
            Assert.AreEqual(1f, VeilTransform.MorphProgress(1f, 1f), 1e-5f);
        }

        [Test]
        public void IdlePose_KeepsTheGripNearTheHand()
        {
            var hand = VeilFormLibrary.Hand;
            // (Not the shield: its ring is not held — it circles round the body, see ShieldRing_CirclesRoundTheHero.)
            foreach (var f in Weapons)
                if (f != VeilForm.Shield)
                    Assert.Less(Vector2.Distance(hand, VeilWeaponPose.Idle(f, hand, true, 1.7f)), 0.05f, f.ToString());
        }

        [Test]
        public void Spear_ReachesFurtherThanSword()
        {
            var sword = new Vector2[20];
            var spear = new Vector2[20];
            VeilFormLibrary.BuildShape(VeilForm.Sword, sword);
            VeilFormLibrary.BuildShape(VeilForm.Spear, spear);
            Assert.Greater(spear[19].x, sword[19].x);
        }

        [Test]
        public void Resample_PreservesEndpointsAndReturnsMarkerFraction()
        {
            var pts = new[] { Vector2.zero, new Vector2(1f, 0f), new Vector2(3f, 0f) };
            var output = new Vector2[5];
            float f = VeilFormLibrary.Resample(pts, 1, output);
            Assert.AreEqual(Vector2.zero, output[0]);
            Assert.AreEqual(new Vector2(3f, 0f), output[4]);
            Assert.AreEqual(new Vector2(1.5f, 0f), output[2]);
            Assert.AreEqual(1f / 3f, f, 1e-5f);
        }

        [TestCaseSource(nameof(Weapons))]
        public void AttackPose_StartsAndEndsAtRest(VeilForm form)
        {
            var p = new Vector2(1.3f, 0.4f);
            Assert.That(Vector2.Distance(p, VeilWeaponPose.Apply(form, p, true, 0f)), Is.LessThan(1e-4f));
            Assert.That(Vector2.Distance(p, VeilWeaponPose.Apply(form, p, true, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void AttackPose_SwordSwingsMidway()
        {
            var tip = VeilFormLibrary.Hand + new Vector2(1.4f, 0f);
            var mid = VeilWeaponPose.Apply(VeilForm.Sword, tip, true, 0.55f);
            Assert.Greater(Vector2.Distance(tip, mid), 1f);
            Assert.AreEqual(tip, VeilWeaponPose.Apply(VeilForm.Sword, tip, false, 0.55f), "non-weapon part stays put");
        }
    }
}
