using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// Blows from the crouch: a low blow played out in the crouch (the aim, the motor keeping him down, the pose, the
    /// clip), and the rising strike up out of it.
    /// </summary>
    public class LowBlowTests
    {
        static readonly MotorContacts Floor = new MotorContacts { grounded = true };
        static readonly MotorContacts Air = new MotorContacts();
        static readonly VeilForm[] Weapons = { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow };

        // --- the aim ------------------------------------------------------------------------------------------

        [Test]
        public void Crouched_TheBlowGoesLevel_DownIsTheCrouchNotAnAim()
        {
            foreach (var f in Weapons)
            {
                Assert.AreEqual(0f, VeilAim.Direction(0f, -1f, false, f, crouched: true), $"{f}: down held crouched");
                Assert.AreEqual(0f, VeilAim.Direction(1f, -1f, false, f, crouched: true), $"{f}: down and ahead (crawling)");
                Assert.AreEqual(0f, VeilAim.Direction(0f, 1f, false, f, crouched: true), $"{f}: up under a low roof");
            }
            // (planted, Ctrl held, he doesn't crouch: the bow and whip still aim down a slope)
            Assert.AreEqual(-45f, VeilAim.Direction(0f, -1f, false, VeilForm.Bow));
            // in the air "crouched" means nothing: down is under the feet
            Assert.AreEqual(-90f, VeilAim.Direction(0f, -1f, true, VeilForm.Sword, crouched: true));
        }

        [Test]
        public void ALowBlow_IsOnTheGround_Crouched_NotSliding()
        {
            Assert.IsTrue(VeilAim.LowBlow(true, true, false));
            Assert.IsFalse(VeilAim.LowBlow(true, false, false), "standing");
            Assert.IsFalse(VeilAim.LowBlow(false, true, false), "in the air");
            Assert.IsFalse(VeilAim.LowBlow(true, true, true), "sliding");
        }

        [Test]
        public void ARisingStrike_JustUpOutOfTheCrouch_UpHeld_ABladeOrTheWhip()
        {
            foreach (var f in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip })
                Assert.IsTrue(VeilAim.RisingStrike(true, 0.1f, 1f, f), f.ToString());
            Assert.IsFalse(VeilAim.RisingStrike(true, 0.1f, 1f, VeilForm.Bow), "the bow just shoots up");
            Assert.IsFalse(VeilAim.RisingStrike(true, 0.1f, 1f, VeilForm.Shield));
            Assert.IsFalse(VeilAim.RisingStrike(true, VeilAim.RiseWindow + 0.05f, 1f, VeilForm.Sword), "long since up: the usual cut overhead");
            Assert.IsFalse(VeilAim.RisingStrike(true, 0.1f, 0f, VeilForm.Sword), "not aimed up");
            Assert.IsFalse(VeilAim.RisingStrike(false, 0.1f, 1f, VeilForm.Sword), "in the air");
        }

        // --- the motor ----------------------------------------------------------------------------------------

        static MotorLogic Crouched()
        {
            var m = new MotorLogic(new MotorSettings());
            for (int i = 0; i < 20; i++) m.Tick(1f / 60f, new MotorInput { aimY = -1f }, Floor);
            Assert.IsTrue(m.IsCrouching);
            return m;
        }

        [Test]
        public void ALowBlow_KeepsHimDown_DownLetGo_UntilItEnds()
        {
            var m = Crouched();
            m.HoldLow = true;
            for (int i = 0; i < 20; i++) m.Tick(1f / 60f, new MotorInput(), Floor);
            Assert.IsTrue(m.IsCrouching, "down let go mid-blow: still down");
            m.HoldLow = false;
            m.Tick(1f / 60f, new MotorInput(), Floor);
            Assert.IsFalse(m.IsCrouching, "the blow over: he stands up");
        }

        [Test]
        public void ARisingStrike_LeapsHimUp_AndLeavesTheDoubleJump()
        {
            var m = Crouched();
            m.HoldLow = true;
            Assert.IsTrue(m.Rise());
            Assert.AreEqual(m.Settings.JumpVelocity * MotorLogic.RiseFactor, m.Velocity.y, 1e-4f);
            Assert.IsFalse(m.IsGrounded);
            Assert.IsFalse(m.IsCrouching);
            Assert.IsFalse(m.HoldLow, "out of the crouch");
            float y = 0f, top = 0f;
            // (the first frame the floor is still under him: he goes up off it all the same)
            y += m.Tick(1f / 60f, new MotorInput(), Floor).y / 60f;
            for (int i = 0; i < 120 && y >= 0f; i++) { y += m.Tick(1f / 60f, new MotorInput(), Air).y / 60f; top = Mathf.Max(top, y); }
            Assert.Greater(top, 1.2f, "up into the air");
            Assert.AreEqual(m.Settings.airJumps, m.AirJumpsLeft, "the double jump is still there");
            Assert.IsFalse(m.Rise(), "not again from the air");
        }

        // --- the pose -----------------------------------------------------------------------------------------

        [Test]
        public void ALowBlow_LeavesTheLegsTheCrouchs_AndEndsBackInIt()
        {
            var crouch = HeroPoseSolver.CrouchPose(0f, 0f, 0f);
            foreach (var f in Weapons)
            {
                for (float t = 0f; t <= 1f; t += 0.05f)
                {
                    var p = HeroAttackChoreography.ApplyLow(crouch, f, t);
                    Assert.AreEqual(crouch.thighL, p.thighL, 1e-4f, $"{f} t{t}");
                    Assert.AreEqual(crouch.kneeL, p.kneeL, 1e-4f, $"{f} t{t}");
                    Assert.AreEqual(crouch.thighR, p.thighR, 1e-4f, $"{f} t{t}");
                    Assert.AreEqual(crouch.kneeR, p.kneeR, 1e-4f, $"{f} t{t}");
                    Assert.AreEqual(crouch.bob, p.bob, 1e-4f, $"{f} t{t}: the hips stay down");
                }
                var end = HeroAttackChoreography.ApplyLow(crouch, f, 1f);
                Assert.AreEqual(crouch.armR, end.armR, 1e-3f, $"{f}: back in the crouch's arms at the end");
                Assert.AreEqual(crouch.spine, end.spine, 1e-3f, $"{f}: and its trunk");
            }
        }

        [Test]
        public void ALowBlow_IsItsOwn_NotTheStandingBlowOverTheCrouch()
        {
            foreach (var f in Weapons)
            {
                var low = HeroAttackChoreography.SampleLow(f, 0.45f);
                var standing = HeroAttackChoreography.Sample(f, 0.45f);
                Assert.Greater(Mathf.Abs(low.armR - standing.armR) + Mathf.Abs(low.spine - standing.spine), 10f, f.ToString());
                Assert.Greater(low.spine, 20f, $"{f}: the trunk stays bent over the knees");
            }
        }

        [Test]
        public void TheLowCut_IsNotRaisedOverhead_ItStaysUnderATunnelsRoof()
        {
            float raised = float.MinValue, standing = float.MinValue;
            for (float t = 0f; t < 0.4f; t += 0.02f)
            {
                raised = Mathf.Max(raised, HeroAttackChoreography.SampleLow(VeilForm.Sword, t).armR);
                standing = Mathf.Max(standing, HeroAttackChoreography.Sample(VeilForm.Sword, t).armR);
            }
            Assert.Greater(standing, 110f, "standing, the blade is raised high beside the head");
            Assert.Less(raised, 95f, "crouched, the arm only reaches out in front - over the bent trunk that is forward and down (the 1.35 m roof: measured on the hero in HeroMoveSetPlayTests)");
        }

        [Test]
        public void TheLowSword_IsAThrust_DrawnBackToTheHipAndDrivenOut_NotAChopAtTheFloor()
        {
            // (it was a cut at the ankles: the blade dipped to the floor in front of him and came up again - he tapped
            // the ground with it)
            var drawn = HeroAttackChoreography.SampleLow(VeilForm.Sword, 0.26f);
            var out1 = HeroAttackChoreography.SampleLow(VeilForm.Sword, 0.54f);   // (the forearm comes through last)
            var held = HeroAttackChoreography.SampleLow(VeilForm.Sword, 0.62f);
            Assert.Greater(drawn.elbowR, 80f, "drawn back: the elbow bent, the fist by the hip");
            Assert.Less(drawn.armR, 20f);
            Assert.Less(out1.elbowR, 15f, "driven out: the arm straight");
            Assert.Greater(out1.armR, drawn.armR + 60f, "and forward");
            Assert.AreEqual(out1.armR, held.armR, 10f, "held at full reach");
            Assert.Greater(out1.spine, drawn.spine + 8f, "the body goes into it");
            // the wrist lays the blade level along the thrust: all through it, then the blade comes back up into the guard
            Assert.AreEqual(0f, VeilWeaponPose.LowThrust(-1f));
            Assert.AreEqual(0f, VeilWeaponPose.LowThrust(0f), 1e-5f);
            Assert.AreEqual(1f, VeilWeaponPose.LowThrust(0.26f), 1e-4f, "level from the draw-back");
            Assert.AreEqual(1f, VeilWeaponPose.LowThrust(0.62f), 1e-4f, "to the end of the reach");
            Assert.AreEqual(0f, VeilWeaponPose.LowThrust(0.95f), 1e-4f, "then back up into the guard");
        }

        [Test]
        public void TheLowLash_IsTheStandingThrowMadeSmall_TheHandBackBesideTheHead_ThenOutNearlyLevel()
        {
            // (the hand used to be drawn back low behind the hip, the forearm hanging: the lash - which goes where the
            // forearm points - flopped onto the floor in front of him, and was thrown along the ground)
            var wound = HeroAttackChoreography.SampleLow(VeilForm.Whip, VeilWeaponPose.WhipWoundAt);
            var thrown = HeroAttackChoreography.SampleLow(VeilForm.Whip, VeilWeaponPose.WhipCrackAt);   // (the forearm whips through last)
            var held = HeroAttackChoreography.SampleLow(VeilForm.Whip, VeilWeaponPose.WhipHeldTo);
            Assert.Greater(wound.armR, 120f, "wound up: the arm raised");
            Assert.Greater(wound.elbowR, 60f, "the forearm laid back");
            Assert.Less(thrown.elbowR, 10f, "thrown: the arm straight");
            Assert.That(thrown.armR, Is.InRange(95f, 120f), "stopped nearly level over the bent trunk: the lash flies over the floor");
            Assert.AreEqual(thrown.armR, held.armR, 6f, "and held there while it cracks");
        }

        [Test]
        public void Crouched_ABladeIsHeldUpInALowGuard_NotIntoTheFloor()
        {
            HeroPoseSolver.WeaponHold(VeilForm.Sword, 0f, out float arm, out float elbow);
            HeroPoseSolver.WeaponHoldLow(VeilForm.Sword, out float lowArm, out float lowElbow);
            Assert.Greater(lowArm + lowElbow, arm + elbow + 25f, "the forearm comes up");
            HeroPoseSolver.WeaponHold(VeilForm.Bow, 0f, out float bow, out _);
            HeroPoseSolver.WeaponHoldLow(VeilForm.Bow, out float lowBow, out _);
            Assert.Greater(lowBow, bow + 25f, "the bow is held up level in front, over the bent trunk");
        }

        [Test]
        public void TheClip_KeepsTheCrouchedLegs_NoStandingBlowLaidOverIt()
        {
            foreach (var f in Weapons)
            {
                var s = new HeroState { grounded = true, crouching = true, attacking = true, attackT = 0.4f, attackForm = f, lowBlow = true, weaponHeld = 1f };
                var c = HeroMocap.Choose(s, 0f, 0);
                if (f == VeilForm.Bow && c.IsNone) continue;
                Assert.AreEqual(HeroMocap.CrouchIdle, c.key, f.ToString());
                Assert.AreEqual(HeroMocap.Mask.Legs, c.mask, $"{f}: only the legs: the blow is the procedural's");
                s.speed = 1f;
                Assert.IsTrue(HeroMocap.Choose(s, 0f, 0).IsNone, $"{f}: crawling, the crawl is procedural");
            }
        }
    }
}
