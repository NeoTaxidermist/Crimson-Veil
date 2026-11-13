using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class WallLedgeMotorTests
    {
        const float Dt = 1f / 60f;
        MotorLogic m;
        MotorSettings s;

        [SetUp]
        public void SetUp()
        {
            s = new MotorSettings();
            m = new MotorLogic(s);
        }

        static MotorContacts Wall(int side) => new MotorContacts { wallSide = side };

        void FallInto(int side, int frames = 40)
        {
            for (int i = 0; i < frames; i++) m.Tick(Dt, new MotorInput { moveX = side }, Wall(side));
        }

        [Test]
        public void PushingIntoWall_WhileFalling_Slides()
        {
            FallInto(1);
            Assert.AreEqual(MotorState.WallSlide, m.State);
            Assert.AreEqual(-s.wallSlideSpeed, m.Velocity.y, 1e-3f, "fall speed is capped by friction");
            Assert.AreEqual(1, m.Facing, "faces the wall");
        }

        [Test]
        public void NotPushing_DoesNotSlide()
        {
            for (int i = 0; i < 40; i++) m.Tick(Dt, new MotorInput(), Wall(1));
            Assert.AreEqual(MotorState.Normal, m.State);
            Assert.Less(m.Velocity.y, -s.wallSlideSpeed);
        }

        [Test]
        public void WallJump_PushesAwayAndUp_ThenLocksInputBriefly()
        {
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            Assert.AreEqual(-s.wallJumpPush, m.Velocity.x, 1e-3f);
            Assert.AreEqual(s.WallJumpVelocity, m.Velocity.y, 1e-3f);
            Assert.AreEqual(-1, m.Facing);

            // Still holding towards the wall: the push must carry for a moment.
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpHeld = true }, new MotorContacts());
            Assert.Less(m.Velocity.x, -s.wallJumpPush * 0.9f);
        }

        [Test]
        public void SameWall_CannotBeClimbedByRepeatedWallJumps()
        {
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            Assert.Greater(m.Velocity.y, 0f, "first kick works");
            // Steer straight back into the same wall and try again once falling.
            for (int i = 0; i < 60; i++) m.Tick(Dt, new MotorInput { moveX = 1f }, Wall(1));
            float vyBefore = m.Velocity.y;
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            Assert.LessOrEqual(m.Velocity.y, vyBefore + 0.01f, "no second kick off the same wall");
        }

        [Test]
        public void OppositeWall_AllowsTheNextWallJump()
        {
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            for (int i = 0; i < 40; i++) m.Tick(Dt, new MotorInput { moveX = -1f }, Wall(-1));
            m.Tick(Dt, new MotorInput { moveX = -1f, jumpPressed = true, jumpHeld = true }, Wall(-1));
            Assert.Greater(m.Velocity.y, 0f);
            Assert.Greater(m.Velocity.x, 0f, "kicked off the left wall");
        }

        [Test]
        public void Landing_ResetsTheWallJumpRule()
        {
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            m.Tick(Dt, new MotorInput(), new MotorContacts { grounded = true });
            m.Tick(Dt, new MotorInput(), new MotorContacts { grounded = true });
            for (int i = 0; i < 5; i++) m.Tick(Dt, new MotorInput(), new MotorContacts());
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, Wall(1));
            Assert.Greater(m.Velocity.y, 0f, "same wall again after touching the ground");
        }

        [Test]
        public void WallCoyote_AllowsLateWallJump()
        {
            FallInto(-1);
            m.Tick(Dt, new MotorInput(), new MotorContacts());
            m.Tick(Dt, new MotorInput(), new MotorContacts()); // left the wall 2 frames ago
            m.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, new MotorContacts());
            Assert.Greater(m.Velocity.y, 0f);
            Assert.Greater(m.Velocity.x, 0f, "pushed away from the left wall");
        }

        [Test]
        public void WallDash_LaunchesAwayFromWall()
        {
            FallInto(1);
            m.Tick(Dt, new MotorInput { moveX = 1f, dashPressed = true }, Wall(1));
            Assert.IsTrue(m.IsDashing);
            Assert.AreEqual(-s.dashSpeed, m.Velocity.x, 1e-3f);
        }

        [Test]
        public void TouchingWall_RefreshesAirDash()
        {
            for (int i = 0; i < 5; i++) m.Tick(Dt, new MotorInput(), new MotorContacts());
            int dashes = 0;
            m.Dashed += () => dashes++;
            m.Tick(Dt, new MotorInput { moveX = 1f, dashPressed = true }, new MotorContacts());
            for (int i = 0; i < 30; i++) m.Tick(Dt, new MotorInput(), new MotorContacts());
            m.Tick(Dt, new MotorInput { moveX = 1f, dashPressed = true }, new MotorContacts());
            Assert.AreEqual(1, dashes, "only one air dash without touching anything");
            FallInto(1, 3);
            m.Tick(Dt, new MotorInput { moveX = 1f, dashPressed = true }, Wall(1));
            Assert.AreEqual(2, dashes, "a wall gives the dash back");
        }

        [Test]
        public void Ledge_GrabsWhenPushingIntoIt_AndClimbs()
        {
            for (int i = 0; i < 10; i++) m.Tick(Dt, new MotorInput(), new MotorContacts());
            int grabs = 0, finished = 0;
            m.LedgeGrabbed += () => grabs++;
            m.ClimbFinished += () => finished++;
            m.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            Assert.AreEqual(MotorState.LedgeHang, m.State);
            Assert.AreEqual(Vector2.zero, m.Velocity);
            Assert.AreEqual(1, grabs);

            // Hanging is stable without input.
            for (int i = 0; i < 30; i++) m.Tick(Dt, new MotorInput(), new MotorContacts());
            Assert.AreEqual(MotorState.LedgeHang, m.State);

            m.Tick(Dt, new MotorInput { jumpPressed = true }, new MotorContacts());
            Assert.AreEqual(MotorState.Climbing, m.State);
            for (int i = 0; i < Mathf.CeilToInt(s.climbDuration / Dt) + 2 && m.State == MotorState.Climbing; i++)
                m.Tick(Dt, new MotorInput(), new MotorContacts());
            Assert.AreEqual(MotorState.Normal, m.State);
            Assert.AreEqual(1, finished);
            Assert.IsTrue(m.IsGrounded);
        }

        [Test]
        public void Ledge_DownLetsGo_AndDoesNotRegrabImmediately()
        {
            m.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            m.Tick(Dt, new MotorInput { aimY = -1f }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            Assert.AreNotEqual(MotorState.LedgeHang, m.State);
            m.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            Assert.AreNotEqual(MotorState.LedgeHang, m.State, "regrab delay");
        }

        [Test]
        public void Ledge_JumpAwayIsAWallJump()
        {
            m.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            m.Tick(Dt, new MotorInput { moveX = -1f, jumpPressed = true, jumpHeld = true }, new MotorContacts());
            Assert.AreEqual(MotorState.Normal, m.State);
            Assert.Less(m.Velocity.x, 0f);
            Assert.Greater(m.Velocity.y, 0f);
        }

        [Test]
        public void Rising_FastDoesNotGrabLedge()
        {
            m.Tick(Dt, new MotorInput(), new MotorContacts { grounded = true });
            m.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, new MotorContacts { grounded = true });
            m.Tick(Dt, new MotorInput { moveX = 1f, jumpHeld = true }, new MotorContacts { ledgeSide = 1, wallSide = 1 });
            Assert.AreNotEqual(MotorState.LedgeHang, m.State, "only grab when near the apex or falling");
        }

        [Test]
        public void ClimbPath_GoesUpBeforeOver()
        {
            var a = new Vector3(0f, 0f, 0f);
            var b = new Vector3(1f, 2f, 0f);
            Assert.AreEqual(a, PlayerController.ClimbPath(a, b, 0f));
            Assert.AreEqual(b, PlayerController.ClimbPath(a, b, 1f));
            var early = PlayerController.ClimbPath(a, b, 0.3f);
            Assert.Greater(early.y, 0.6f);
            Assert.Less(early.x, 0.05f, "doesn't clip into the ledge corner");
        }
    }
}
