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
        public void Bow_IsHeldOutInFront_NotHiddenByTheBody()
        {
            Assert.Greater(HeroPoseSolver.HoldArmFor(VeilForm.Bow), 65f, "bow arm extended forward");
            HeroPoseSolver.WeaponHold(VeilForm.Bow, 0f, out _, out float elbow);
            Assert.Less(elbow, 15f, "arm nearly straight");
            Assert.Less(VeilFormLibrary.BowLimbReachBack, 0.35f, "the limbs don't curl back into the torso");
            Assert.AreEqual(0f, VeilWeaponPose.ForearmFollow(VeilForm.Bow), "stays upright");
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
