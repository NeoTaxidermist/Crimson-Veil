using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Crouching (down on the ground), crawling, sliding (down + dash), and the low ceiling.</summary>
    public class CrouchSlideMotorTests
    {
        const float Dt = 1f / 60f;
        static readonly MotorContacts Ground = new MotorContacts { grounded = true };

        static MotorLogic Standing()
        {
            var m = new MotorLogic(new MotorSettings());
            for (int i = 0; i < 10; i++) m.Tick(Dt, new MotorInput(), Ground);
            return m;
        }

        [Test]
        public void DownOnTheGround_Crouches_AndCrawlsSlowly()
        {
            var m = Standing();
            for (int i = 0; i < 60; i++) m.Tick(Dt, new MotorInput { moveX = 1f, aimY = -1f }, Ground);
            Assert.IsTrue(m.IsCrouching);
            Assert.AreEqual(m.Settings.crouchSpeed, m.Velocity.x, 0.05f, "a crawl, not a run");
            for (int i = 0; i < 60; i++) m.Tick(Dt, new MotorInput { moveX = 1f }, Ground);
            Assert.IsFalse(m.IsCrouching, "let go of down: up again");
            Assert.AreEqual(m.Settings.maxRunSpeed, m.Velocity.x, 0.05f);
        }

        [Test]
        public void UnderALowCeiling_ItStaysCrouched_AndCantJump()
        {
            var m = Standing();
            var low = new MotorContacts { grounded = true, lowCeiling = true };
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, low);
            Assert.IsTrue(m.IsCrouching, "no room to stand: crouched without holding down");
            Assert.IsTrue(m.IsGrounded, "no jumping into the ceiling");
            Assert.LessOrEqual(m.Velocity.y, 0f);
        }

        [Test]
        public void DownAndDash_Slides_FastThenSlowing()
        {
            var m = Standing();
            m.Tick(Dt, new MotorInput { aimY = -1f }, Ground);
            m.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f, dashPressed = true }, Ground);
            Assert.IsTrue(m.IsSliding);
            Assert.IsFalse(m.IsDashing, "on the ground, crouched, the dash is a slide");
            float early = m.Velocity.x;
            Assert.Greater(early, m.Settings.maxRunSpeed, "faster than running");
            float travelled = 0f;
            for (int i = 0; i < 60 && m.IsSliding; i++) { m.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f }, Ground); travelled += m.Velocity.x * Dt; }
            Assert.IsFalse(m.IsSliding, "it ends");
            Assert.Greater(travelled, 3f, "a slide carries a few metres");
            Assert.IsTrue(m.IsCrouching, "and ends low (down still held)");
        }

        [Test]
        public void ASlide_GoesTheWayTheArrowPoints()
        {
            var m = Standing();
            m.Tick(Dt, new MotorInput { aimY = -1f, moveX = -1f, dashPressed = true }, Ground);
            Assert.IsTrue(m.IsSliding);
            Assert.Less(m.Velocity.x, -5f);
            Assert.AreEqual(-1, m.Facing);
        }

        [Test]
        public void Planted_DownAims_ItDoesNotCrouch()
        {
            var m = Standing();
            for (int i = 0; i < 10; i++) m.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f, stand = true }, Ground);
            Assert.IsFalse(m.IsCrouching, "Ctrl held: down aims a blow low (the bow down and forward)");
            m.Tick(Dt, new MotorInput { aimY = -1f, stand = true }, new MotorContacts { grounded = true, lowCeiling = true });
            Assert.IsTrue(m.IsCrouching, "under a low ceiling he is crouched anyway");
        }

        [Test]
        public void DashWithoutDown_IsStillADash()
        {
            var m = Standing();
            m.Tick(Dt, new MotorInput { moveX = 1f, dashPressed = true }, Ground);
            Assert.IsTrue(m.IsDashing);
            Assert.IsFalse(m.IsSliding);
        }

        [Test]
        public void SlidingIntoAWall_Stops_AndOffAnEdge_Falls()
        {
            var m = Standing();
            m.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f, dashPressed = true }, Ground);
            m.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f }, new MotorContacts { grounded = true, wallSide = 1 });
            Assert.IsFalse(m.IsSliding);
            Assert.AreEqual(0f, m.Velocity.x, 1e-4f);

            var n = Standing();
            n.Tick(Dt, new MotorInput { aimY = -1f, moveX = 1f, dashPressed = true }, Ground);
            n.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts());
            Assert.IsFalse(n.IsSliding, "off the edge");
            Assert.Greater(n.Velocity.x, 3f, "the momentum carries on into the fall");
        }

        [Test]
        public void TheCrouchAndSlidePoses_AreLow()
        {
            var crouch = HeroPoseSolver.CrouchPose(0f, 0f, 0f);
            Assert.Less(crouch.bob, -0.4f, "the hips sink");
            Assert.Greater(crouch.kneeL, 100f, "deep knees");
            var crawl = HeroPoseSolver.CrouchPose(2.0f, 0.1f, 0f);
            Assert.AreNotEqual(crouch.thighL, crawl.thighL, "crawling, the legs move");
            // flat on the soles, not on tiptoe: the foot undoes the shin's slope
            Assert.AreEqual(HeroPoseSolver.FlatFoot(crouch.thighL, crouch.kneeL), crouch.footL, 1e-3f);
            Assert.AreEqual(HeroPoseSolver.FlatFoot(crouch.thighR, crouch.kneeR), crouch.footR, 1e-3f);
            Assert.Greater(crouch.footL, 0f, "a deep knee tips the shin back: the toes come up to stay level");
            // long steps: one stride per gait cycle (the legs swap after half a cycle, not after a fraction of it)
            var q1 = HeroPoseSolver.CrouchPose(2.0f, 0.25f, 0f);
            var q3 = HeroPoseSolver.CrouchPose(2.0f, 0.75f, 0f);
            Assert.Greater(q1.thighL, q1.thighR + 30f, "a quarter in: the left leg forward");
            Assert.Greater(q3.thighR, q3.thighL + 30f, "three quarters in: the right one");
            // unhurried: at the crawling speed, no more than ~3 steps a second (it was a patter of almost 4)
            float stepsPerSecond = 2f * new MotorSettings().crouchSpeed / HeroPoseSolver.CrouchStride;
            Assert.LessOrEqual(stepsPerSecond, 3f);
            var slide = HeroPoseSolver.SlidePose(0.2f);
            Assert.Less(slide.bob, crouch.bob, "a slide is lower still");
            Assert.Less(slide.spine, 0f, "leaning back");
            Assert.Less(slide.kneeL, 20f, "the front leg stretched out");
            var end = HeroPoseSolver.SlidePose(1f);
            Assert.AreEqual(crouch.bob, end.bob, 1e-3f, "it ends in the crouch");
        }
    }
}
