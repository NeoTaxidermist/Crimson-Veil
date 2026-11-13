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
            for (int i = 1; i < pts.Length - 1; i++)
                Assert.AreEqual(first, Vector2.Distance(pts[i], pts[i + 1]), first * 0.35f, $"{form} segment {i}");
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
