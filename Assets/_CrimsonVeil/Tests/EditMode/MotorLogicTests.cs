using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class MotorLogicTests
    {
        const float Dt = 1f / 60f;
        static readonly MotorContacts Ground = new MotorContacts { grounded = true };
        static readonly MotorContacts Air = new MotorContacts { grounded = false };

        MotorSettings settings;
        MotorLogic motor;

        [SetUp]
        public void SetUp()
        {
            settings = new MotorSettings();
            motor = new MotorLogic(settings);
        }

        void Run(int frames, MotorInput input, MotorContacts contacts)
        {
            for (int i = 0; i < frames; i++) motor.Tick(Dt, input, contacts);
        }

        [Test]
        public void Run_ReachesMaxSpeedQuickly()
        {
            Run(20, new MotorInput { moveX = 1f }, Ground);
            Assert.AreEqual(settings.maxRunSpeed, motor.Velocity.x, 1e-3f);
            Assert.AreEqual(1, motor.Facing);
        }

        [Test]
        public void Release_StopsOnGround()
        {
            Run(20, new MotorInput { moveX = 1f }, Ground);
            Run(10, new MotorInput(), Ground);
            Assert.AreEqual(0f, motor.Velocity.x, 1e-3f);
        }

        [Test]
        public void Turnaround_UsesFasterTurnAcceleration()
        {
            Run(20, new MotorInput { moveX = 1f }, Ground);
            motor.Tick(Dt, new MotorInput { moveX = -1f }, Ground);
            float expected = settings.maxRunSpeed - settings.turnAcceleration * Dt;
            Assert.AreEqual(expected, motor.Velocity.x, 1e-3f);
            Assert.AreEqual(-1, motor.Facing);
        }

        [Test]
        public void Jump_SetsJumpVelocityAndFiresEvent()
        {
            int jumps = 0;
            motor.Jumped += () => jumps++;
            motor.Tick(Dt, new MotorInput(), Ground);
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Ground);
            Assert.AreEqual(settings.JumpVelocity, motor.Velocity.y, 1e-4f);
            Assert.AreEqual(1, jumps);
        }

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingLedge()
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            Run(3, new MotorInput(), Air); // 50 ms < 100 ms coyote
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Air);
            Assert.AreEqual(settings.JumpVelocity, motor.Velocity.y, 1e-4f);
        }

        [Test]
        public void CoyoteTime_ExpiresAfterWindow()
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            Run(12, new MotorInput(), Air); // 200 ms
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Air);
            Assert.Less(motor.Velocity.y, 0f);
        }

        [Test]
        public void JumpBuffer_JumpsOnLandingWhenPressedJustBefore()
        {
            Run(30, new MotorInput(), Air);
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Air);
            Run(3, new MotorInput { jumpHeld = true }, Air);
            motor.Tick(Dt, new MotorInput { jumpHeld = true }, Ground);
            Assert.AreEqual(settings.JumpVelocity, motor.Velocity.y, 1e-4f);
        }

        float SimulateJumpApex(bool hold)
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Ground);
            float y = motor.Velocity.y * Dt, apex = y;
            for (int i = 0; i < 300 && motor.Velocity.y > -1f; i++)
            {
                motor.Tick(Dt, new MotorInput { jumpHeld = hold }, Air);
                y += motor.Velocity.y * Dt;
                apex = Mathf.Max(apex, y);
            }
            return apex;
        }

        [Test]
        public void HeldJump_ReachesAboutDesignedHeight()
        {
            float apex = SimulateJumpApex(hold: true);
            Assert.That(apex, Is.InRange(settings.jumpHeight * 0.95f, settings.jumpHeight * 1.25f));
        }

        [Test]
        public void ReleasedJump_IsLowerThanHeldJump()
        {
            float held = SimulateJumpApex(hold: true);
            motor = new MotorLogic(settings);
            float tapped = SimulateJumpApex(hold: false);
            Assert.Less(tapped, held * 0.75f);
        }

        [Test]
        public void Falling_IsClampedToMaxFallSpeed()
        {
            Run(300, new MotorInput(), Air);
            Assert.AreEqual(-settings.maxFallSpeed, motor.Velocity.y, 1e-4f);
        }

        [Test]
        public void Dash_MovesAtDashSpeedThenEnds()
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            motor.Tick(Dt, new MotorInput { dashPressed = true }, Ground);
            Assert.IsTrue(motor.IsDashing);
            Assert.AreEqual(settings.dashSpeed, motor.Velocity.x, 1e-4f);
            Run(Mathf.CeilToInt(settings.dashDuration / Dt) + 1, new MotorInput(), Ground);
            Assert.IsFalse(motor.IsDashing);
            Assert.Less(motor.Velocity.x, settings.dashSpeed);
        }

        [Test]
        public void AirDash_OnlyOncePerAirtime()
        {
            Run(5, new MotorInput(), Air);
            int dashes = 0;
            motor.Dashed += () => dashes++;
            motor.Tick(Dt, new MotorInput { dashPressed = true }, Air);
            Run(40, new MotorInput(), Air);
            motor.Tick(Dt, new MotorInput { dashPressed = true }, Air);
            Assert.AreEqual(1, dashes);
        }

        [Test]
        public void DashJump_KeepsMomentum()
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            motor.Tick(Dt, new MotorInput { dashPressed = true, moveX = 1f }, Ground);
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true, moveX = 1f }, Ground);
            Assert.IsFalse(motor.IsDashing);
            Assert.AreEqual(settings.JumpVelocity, motor.Velocity.y, 1e-4f);
            Assert.Greater(motor.Velocity.x, settings.maxRunSpeed);
        }

        [Test]
        public void Wall_StopsHorizontalVelocityTowardsIt()
        {
            Run(20, new MotorInput { moveX = 1f }, Ground);
            motor.Tick(Dt, new MotorInput { moveX = 1f }, new MotorContacts { grounded = true, wallSide = 1 });
            Assert.AreEqual(0f, motor.Velocity.x, 1e-4f);

            // ...but you can still move away from it.
            motor.Tick(Dt, new MotorInput { moveX = -1f }, new MotorContacts { grounded = true, wallSide = 1 });
            Assert.Less(motor.Velocity.x, 0f);
        }

        [Test]
        public void Ceiling_KillsUpwardVelocity()
        {
            motor.Tick(Dt, new MotorInput(), Ground);
            motor.Tick(Dt, new MotorInput { jumpPressed = true, jumpHeld = true }, Ground);
            motor.Tick(Dt, new MotorInput { jumpHeld = true }, new MotorContacts { ceiling = true });
            Assert.LessOrEqual(motor.Velocity.y, 0f);
        }

        [Test]
        public void Landing_FiresEvent()
        {
            int landings = 0;
            motor.Landed += () => landings++;
            Run(10, new MotorInput(), Air);
            motor.Tick(Dt, new MotorInput(), Ground);
            Assert.AreEqual(1, landings);
        }
    }
}
