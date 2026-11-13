using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Weapons are made of the same flame as the scarf; the transformation is a vortex into the palm.</summary>
    public class VeilWeaponLookTests
    {
        [Test]
        public void SpiralIn_StartsAndEndsStill_SpinsInBetween()
        {
            Assert.AreEqual(0f, VeilTransform.SpiralIn(0f), 1e-4f, "leaves the scarf where it is");
            Assert.AreEqual(0f, VeilTransform.SpiralIn(1f), 1e-4f, "the finished weapon is not twisted");
            Assert.Greater(VeilTransform.SpiralIn(0.35f), 90f, "mid-way the cloth winds into the palm");
        }

        [Test]
        public void Vortex_SpinsFastestNearThePalm()
        {
            Assert.AreEqual(1f, VeilTransform.VortexFalloff(0f), 1e-4f);
            Assert.Greater(VeilTransform.VortexFalloff(0.3f), VeilTransform.VortexFalloff(1.5f));
            Assert.Less(VeilTransform.VortexFalloff(2.5f), 0.1f, "far cloth only drifts in, it isn't swept around");
        }

        [Test]
        public void Braid_OnlyOnTheWeapon_AndStaysCloseToTheBlade()
        {
            for (float f = 0f; f <= 1f; f += 0.05f)
            {
                Assert.AreEqual(0f, VeilRenderer.WeaponBraid(f, 1.3f, 0.7f, 0.2f, 0f), 1e-5f, "loose cloth isn't braided");
                Assert.LessOrEqual(Mathf.Abs(VeilRenderer.WeaponBraid(f, 1.3f, 0.7f, 0.2f, 1f)), 0.2f * 0.5f, "strands wrap the blade, no loose loops");
            }
        }

        [Test]
        public void FlameTongues_PeelOffAlongTheBlade_NotOnlyAtTheTip()
        {
            const float hand = 0.3f;
            float min = 1f, max = 0f;
            foreach (var st in VeilRenderer.Strands)
            {
                if (st.start <= 0f) continue;
                float e = VeilRenderer.WeaponStrandEnd(st, hand);
                Assert.Greater(e, hand, "on the blade, past the hand");
                Assert.LessOrEqual(e, 1f);
                min = Mathf.Min(min, e);
                max = Mathf.Max(max, e);
            }
            Assert.Greater(max - min, 0.25f, "spread along the blade");
        }

        [Test]
        public void Release_ExhalesUpAndBack_OnlyWhileMelting()
        {
            Assert.AreEqual(Vector2.zero, VeilController.ReleaseBreath(0f, 1));
            Assert.AreEqual(Vector2.zero, VeilController.ReleaseBreath(1f, 1));
            var mid = VeilController.ReleaseBreath(0.5f, 1);
            Assert.Greater(mid.y, 1f, "lifts the melting flame");
            Assert.Less(mid.x, 0f, "and blows it back behind a hero facing right");
            Assert.Greater(VeilController.ReleaseBreath(0.5f, -1).x, 0f);
        }

        [Test]
        public void WeaponCore_RunsAllTheWayToTheTip()
        {
            var core = VeilRenderer.Strands[1];
            Assert.AreEqual(1f, VeilRenderer.StrandEnd(core, true, false, 1f, 0.3f), 1e-5f, "a solid core along the whole blade / shaft");
            foreach (var st in VeilRenderer.Strands)
                Assert.AreEqual(1f, VeilRenderer.StrandEnd(st, false, true, 1f, 0.3f), 1e-5f, "every bow strand runs round the whole bow");
        }

        [Test]
        public void Spear_FlameTonguesStartInFrontOfTheHand_NotBehindIt()
        {
            var shape = new Vector2[32];
            float butt = VeilFormLibrary.BuildShape(VeilForm.Spear, shape);
            float grip = VeilFormLibrary.GripFraction(VeilForm.Spear);
            Assert.Greater(grip, butt + 0.15f, "the spear is gripped well in front of its butt");
            foreach (var st in VeilRenderer.Strands)
            {
                if (st.start <= 0f) continue;
                Assert.Greater(VeilRenderer.StrandEnd(st, false, false, 1f, grip), grip, "tongues flare in front of the hand");
            }
            Assert.AreEqual(VeilFormLibrary.BuildShape(VeilForm.Sword, shape), VeilFormLibrary.GripFraction(VeilForm.Sword), 0.02f,
                "a sword is gripped where the blade starts");
        }

        [Test]
        public void Weapons_AreVisibleBands_ButSlim()
        {
            foreach (var form in new[] { VeilForm.Whip, VeilForm.Sword, VeilForm.Spear, VeilForm.Shield, VeilForm.Bow })
                Assert.GreaterOrEqual(VeilFormLibrary.Width(form), 0.06f, form + " must not be a hairline");
            Assert.LessOrEqual(VeilFormLibrary.Width(VeilForm.Spear), 0.08f, "a spear is a slim shaft");
            Assert.LessOrEqual(VeilFormLibrary.Width(VeilForm.Sword), 0.17f, "a blade, not a plank");
        }
    }
}
