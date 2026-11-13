using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>How each weapon is held and moves: level spear, visible bow, a whip that really lashes.</summary>
    public class VeilWeaponMotionTests
    {
        static Vector2[] StraightWhip(int n, int handIndex)
        {
            var rest = new Vector2[n];
            for (int i = 0; i < n; i++) rest[i] = new Vector2(Mathf.Max(0, i - handIndex) * 0.15f, 0f);
            return rest;
        }

        [Test]
        public void Whip_AtRest_FollowsTheHandleAsOnePiece()
        {
            var rest = StraightWhip(24, 4);
            var outp = new Vector2[24];
            VeilWeaponPose.WhipChain(rest, 4, _ => 30f, 0.16f, outp);
            Vector2 dir = (outp[23] - outp[4]).normalized;
            Assert.AreEqual(30f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0.5f, "no swing: the whole lash is turned with the handle");
        }

        [Test]
        public void Whip_SwingThatStops_RollsALoopDownTheLash()
        {
            // The handle swung from up-and-back (160°) to forward (0°) during the last 0.12 s, then stopped.
            float TurnAgo(float s) => s < 0.04f ? 0f : Mathf.Lerp(0f, 160f, Mathf.InverseLerp(0.04f, 0.16f, s));
            var rest = StraightWhip(24, 4);
            var outp = new Vector2[24];
            VeilWeaponPose.WhipChain(rest, 4, TurnAgo, 0.16f, outp);

            Vector2 first = outp[6] - outp[5], lastSeg = outp[23] - outp[22];
            Assert.Greater(Vector2.Angle(first, lastSeg), 120f, "near the hand it points ahead, at the tip it still points back: a loop");
            float straight = 19 * 0.15f;
            Assert.Less(Vector2.Distance(outp[4], outp[23]), straight * 0.8f, "the loop shortens the reach until it rolls out");
        }

        [Test]
        public void Whip_CracksAfterTheHandStops()
        {
            var keys = HeroAttackChoreography.For(VeilForm.Whip);
            float stop = 0f;
            foreach (var k in keys) if (k.armR > 80f && k.armR < 110f && k.elbowR < 15f) { stop = k.t; break; }
            Assert.Greater(stop, 0f, "the hand drives forward and stops at arm's length");
            Assert.Greater(VeilWeaponPose.WhipCrackAt, stop, "the crack comes after the stop, when the loop reaches the tip");
            var window = CrimsonVeil.Combat.VeilCombatProfile.ActiveWindow(VeilForm.Whip);
            Assert.IsTrue(VeilWeaponPose.WhipCrackAt >= window.x && VeilWeaponPose.WhipCrackAt <= window.y, "the crack hits");
        }

        [Test]
        public void Spear_IsLevel_AndDoesNotTiltWithTheArm()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Spear, out int start);
            Assert.AreEqual(pts[start].y, pts[pts.Count - 1].y, 1e-4f, "the shaft points straight ahead");
            Assert.AreEqual(0f, VeilWeaponPose.ForearmFollow(VeilForm.Spear), "the arm pushes it, it doesn't rotate it");
            Assert.AreEqual(0f, VeilWeaponPose.CarryFactor(VeilForm.Spear));
        }

        [Test]
        public void Spear_IsHeldAlongTheShaft_ButtReachesBehindTheHand()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Spear, out int start);
            Vector2 butt = pts[start], tip = pts[pts.Count - 1], hand = VeilFormLibrary.Hand;
            Assert.Greater(hand.x - butt.x, 0.5f, "the butt sticks out behind the hand");
            Assert.Greater(tip.x - hand.x, 1.8f, "most of the shaft is in front");
            Assert.IsTrue(pts.Contains(hand), "the shaft passes through the fist");
            Assert.Less(butt.x, 0f, "the butt reaches back past the hero's neck line");
        }

        [Test]
        public void Bow_LimbsAreEqual_StringIsVisible()
        {
            var range = VeilFormLibrary.BowStringRange;
            var pts = new Vector2[64];
            float hf = VeilFormLibrary.BuildShape(VeilForm.Bow, pts);
            // Upper limb (grip → upper tip) and lower limb (lower tip → grip) take the same share of the ribbon.
            Assert.AreEqual(range.x - hf, 1f - range.y, 0.03f);
            Assert.Greater(VeilFormLibrary.BowBraceHeight, 0.15f, "a clear gap between grip and string");
            Assert.Less(VeilFormLibrary.Width(VeilForm.Bow), VeilFormLibrary.BowBraceHeight, "limbs thinner than the gap: it reads as a bow");
        }

        [Test]
        public void Bow_IsHeldOutInFront_NotHiddenByTheBody()
        {
            Assert.Greater(HeroPoseSolver.HoldArmFor(VeilForm.Bow), 65f, "bow arm extended forward");
            HeroPoseSolver.WeaponHold(VeilForm.Bow, 0f, out _, out float elbow);
            Assert.Less(elbow, 15f, "arm nearly straight");
            Assert.Less(VeilFormLibrary.BowLimbReachBack, 0.35f, "the limbs don't curl back into the torso");
            Assert.AreEqual(0f, VeilWeaponPose.ForearmFollow(VeilForm.Bow), "stays upright");
        }

        [Test]
        public void Bow_IsARecurveBowWithAString()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Bow, out int grip);
            Assert.AreEqual(VeilFormLibrary.Hand, pts[grip], "the scarf runs from the neck to the hand first");
            Assert.AreEqual(VeilFormLibrary.Hand + VeilFormLibrary.BowGripOffset, pts[pts.Count - 1], "the lower limb ends back in the grip");
            float top = float.MinValue, bottom = float.MaxValue;
            foreach (var p in pts) { top = Mathf.Max(top, p.y); bottom = Mathf.Min(bottom, p.y); }
            Assert.That(top - VeilFormLibrary.Hand.y, Is.InRange(0.5f, 0.8f), "upper limb: a bow, not a pole");
            Assert.That(VeilFormLibrary.Hand.y - bottom, Is.InRange(0.5f, 0.8f), "lower limb");

            var range = VeilFormLibrary.BowStringRange;
            Assert.That(range.x, Is.InRange(0.2f, 0.6f));
            Assert.Greater(range.y - range.x, 0.25f, "the string spans tip to tip");
            Assert.AreEqual(1f, VeilFormLibrary.BowStringWeight((range.x + range.y) * 0.5f), 1e-4f);
            Assert.AreEqual(0f, VeilFormLibrary.BowStringWeight(range.x * 0.5f), 1e-4f, "limbs are not string");
        }

        [Test]
        public void Bow_StringIsDrawnThenSnapsBack()
        {
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(-1f));
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(0.02f), 1e-4f);
            Assert.AreEqual(1f, VeilWeaponPose.BowDraw(0.26f), 1e-4f, "full draw before the release at 0.3");
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(0.4f), 1e-4f, "snapped straight after the release");
        }

        [Test]
        public void Whip_RestsCoiledInTheHand()
        {
            var extended = new Vector2[32];
            var coil = new Vector2[32];
            float hfExt = VeilFormLibrary.BuildShape(VeilForm.Whip, extended);
            float hfCoil = VeilFormLibrary.BuildWhipCoil(coil);
            Assert.AreEqual(hfExt, hfCoil, 0.01f, "same ribbon, same length: the hand sits at the same place");
            int hand = Mathf.CeilToInt(hfExt * 31);
            for (int i = hand; i < 32; i++)
                Assert.Less(Vector2.Distance(coil[i], VeilFormLibrary.Hand), 0.65f, $"node {i} stays in the coil by the hand");
            Assert.Greater(Vector2.Distance(extended[31], VeilFormLibrary.Hand), 2.5f, "while the paid-out lash is long");
        }

        [Test]
        public void Whip_UncoilsDuringTheWindUp_FromTheHandOut()
        {
            Assert.AreEqual(0f, VeilWeaponPose.WhipUncoil(-1f), "coiled when not attacking");
            Assert.AreEqual(1f, VeilWeaponPose.WhipUncoil(0.42f), 1e-4f, "fully out before the throw");
            Assert.Greater(VeilWeaponPose.WhipNodeOpen(0.4f, 0.1f), VeilWeaponPose.WhipNodeOpen(0.4f, 0.9f), "pays out from the hand");
            Assert.AreEqual(1f, VeilWeaponPose.WhipNodeOpen(1f, 1f), 1e-4f);
            Assert.Greater(VeilController.WhipCoilTime, 0.2f, "coils back smoothly, not instantly");
        }

        [Test]
        public void Weapon_IsInFrontOfTheHero_ClothBehind()
        {
            Assert.AreEqual(0f, VeilSimulation.WeaponDepthBlend(0f), 1e-4f);
            Assert.AreEqual(1f, VeilSimulation.WeaponDepthBlend(VeilFormLibrary.Hardness(VeilForm.Bow) * 0.9f), 1e-4f);
            Assert.Greater(new VeilSettings().weaponDepth, 0.15f);
        }
    }
}
