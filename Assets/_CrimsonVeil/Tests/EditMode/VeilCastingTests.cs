using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The scarf's strength, cast costs, and the motor's scarf glide / umbrella / pogo bounce.</summary>
    public class VeilCastingTests
    {
        [Test]
        public void Charge_IsSpent_AndGrowsBackAfterABreath()
        {
            var c = new VeilCharge();
            Assert.IsTrue(c.TrySpend(0.3f));
            Assert.AreEqual(0.7f, c.Value, 1e-4f);
            c.Tick(0.3f);
            Assert.AreEqual(0.7f, c.Value, 1e-4f, "no regrowth within the breath after a cast");
            c.Tick(0.5f);
            Assert.Greater(c.Value, 0.7f);
            for (int i = 0; i < 100; i++) c.Tick(0.1f);
            Assert.AreEqual(1f, c.Value, 1e-4f, "never beyond full");
        }

        [Test]
        public void Charge_CannotBeOverspent()
        {
            var c = new VeilCharge();
            c.Drain(100f, 1f);
            Assert.IsFalse(c.TrySpend(VeilCasting.Cost(VeilForm.Bow)));
            Assert.AreEqual(0f, c.Value, 1e-4f);
            Assert.IsTrue(c.TrySpend(VeilCasting.Cost(VeilForm.Sword)), "the sword is always there");
        }

        [Test]
        public void FullScarf_TakesOneHit_ThenIsTornShort()
        {
            var c = new VeilCharge();
            Assert.IsTrue(c.TryAbsorbHit());
            Assert.AreEqual(0f, c.Value, 1e-4f);
            Assert.IsFalse(c.TryAbsorbHit(), "torn short, it cannot take another");
            c.Refund(0.5f);
            Assert.IsFalse(c.TryAbsorbHit(), "only a full-length scarf can");
        }

        [Test]
        public void LandedBlows_FeedTheScarf()
        {
            var c = new VeilCharge();
            c.TrySpend(0.5f);
            c.OnHitLanded();
            Assert.AreEqual(0.5f + c.HitRefund, c.Value, 1e-4f);
        }

        [Test]
        public void Costs_SwordFree_BowDearest()
        {
            Assert.AreEqual(0f, VeilCasting.Cost(VeilForm.Sword));
            foreach (var f in new[] { VeilForm.Whip, VeilForm.Spear, VeilForm.Shield })
            {
                Assert.Greater(VeilCasting.Cost(f), 0f, f.ToString());
                Assert.Less(VeilCasting.Cost(f), VeilCasting.Cost(VeilForm.Bow), f.ToString());
            }
            Assert.Less(VeilCasting.CastTransformTime, 0.2f, "the transformation is the wind-up: it must be quick");
        }

        static MotorLogic Airborne(out MotorContacts air)
        {
            var m = new MotorLogic(new MotorSettings()) { ScarfGlide = true };
            air = new MotorContacts();
            m.Tick(1f / 60f, new MotorInput(), new MotorContacts { grounded = true });
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true, jumpHeld = true }, new MotorContacts { grounded = true });
            return m;
        }

        [Test]
        public void HoldingJump_NeverGlidesByItself()
        {
            var m = Airborne(out var air);
            for (int i = 0; i < 90; i++) m.Tick(1f / 60f, new MotorInput { jumpHeld = true }, air);
            Assert.IsFalse(m.IsGliding, "a held jump is a jump, not a glide");
            Assert.Less(m.Velocity.y, -MotorLogic.GlideFallSpeed - 1f);
        }

        [Test]
        public void PressingJumpAgainWhileFalling_GlidesOnTheScarf()
        {
            var m = Airborne(out var air);
            for (int i = 0; i < 40; i++) m.Tick(1f / 60f, new MotorInput(), air);
            Assert.Less(m.Velocity.y, 0f);
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true, jumpHeld = true }, air);
            for (int i = 0; i < 40; i++) m.Tick(1f / 60f, new MotorInput { jumpHeld = true }, air);
            Assert.IsTrue(m.IsGliding);
            Assert.AreEqual(-MotorLogic.GlideFallSpeed, m.Velocity.y, 0.3f);
        }

        [Test]
        public void ShieldOverhead_SlowsTheFall_WithoutJump()
        {
            var m = Airborne(out var air);
            m.ScarfGlide = false;
            m.Umbrella = true;
            for (int i = 0; i < 80; i++) m.Tick(1f / 60f, new MotorInput(), air);
            Assert.IsTrue(m.IsGliding);
            Assert.AreEqual(-MotorLogic.GlideFallSpeed, m.Velocity.y, 0.3f);
        }

        [Test]
        public void Bounce_LaunchesUpAndRefreshesTheAirDash()
        {
            var m = Airborne(out var air);
            for (int i = 0; i < 40; i++) m.Tick(1f / 60f, new MotorInput(), air);
            m.Bounce();
            Assert.Greater(m.Velocity.y, m.Settings.JumpVelocity * 0.9f);
            Assert.IsTrue(m.IsJumping);
        }

        [Test]
        public void Hint_PullsTowardsTheNearestSecret()
        {
            var h = new GameObject("Secret").AddComponent<VeilHint>();
            try
            {
                h.transform.position = new Vector3(-4f, 2f, 0f);
                var dir = VeilHint.Pull(new Vector2(0f, 2f), out float k);
                Assert.Greater(k, 0.4f);
                Assert.Less(dir.x, -0.9f);
                VeilHint.Pull(new Vector2(40f, 2f), out float far);
                Assert.AreEqual(0f, far, "out of range: the scarf streams behind as usual");
            }
            finally { Object.DestroyImmediate(h.gameObject); }
        }
    }
}
