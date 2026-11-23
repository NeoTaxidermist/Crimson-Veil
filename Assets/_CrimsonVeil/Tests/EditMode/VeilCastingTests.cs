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
        public void ACast_NeedsAStubOfScarf_AndTakesWhatIsLeft()
        {
            // Each weapon used to need its whole cost: a scarf a quarter long threw the whip (0.15) but not the bow
            // (0.3), with nothing to tell which. Now: while there is a stub of scarf to see, every weapon can be cast.
            var c = new VeilCharge();
            foreach (var f in new[] { VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow })
            {
                Assert.Greater(VeilCasting.Cost(f), VeilCasting.MinToCast - 1e-4f, f + ": it costs at least a stub");
                Assert.IsTrue(c.CanCast(VeilCasting.Cost(f)), f + " from a full scarf");
            }
            c.Drain(100f, 1f);
            c.Refund(0.23f);   // (what was left after five arrows 0.9 s apart)
            Assert.IsFalse(c.CanAfford(VeilCasting.Cost(VeilForm.Bow)), "(not the bow's whole cost)");
            Assert.IsTrue(c.CanCast(VeilCasting.Cost(VeilForm.Bow)), "a quarter of the scarf: the bow can be cast");
            Assert.IsTrue(c.TryCast(VeilCasting.Cost(VeilForm.Bow)));
            Assert.AreEqual(0f, c.Value, 1e-4f, "it took all that was left");
            Assert.IsFalse(c.CanCast(VeilCasting.Cost(VeilForm.Whip)), "nothing left: nothing but the sword");
            Assert.IsFalse(c.TryCast(VeilCasting.Cost(VeilForm.Whip)));
            Assert.IsTrue(c.TryCast(VeilCasting.Cost(VeilForm.Sword)), "the sword is always there");
            c.Refund(VeilCasting.MinToCast - 0.02f);
            Assert.IsFalse(c.CanCast(VeilCasting.Cost(VeilForm.Bow)), "less than a stub: not yet");
            c.Refund(0.03f);
            Assert.IsTrue(c.CanCast(VeilCasting.Cost(VeilForm.Bow)), "a stub one can see: again");
            Assert.That(VeilCasting.MinToCast, Is.InRange(0.05f, 0.15f), "a stub one can see, not a sliver nor half the scarf");
        }

        [Test]
        public void AMeltingWeapon_IsLedLightly_AndSaysSo()
        {
            // (the guide read 1 - Mathf.SmoothStep(0.88, 1, m), meant as "fully, then handed over": Unity's SmoothStep
            // runs from its first argument to its second, so it was 0.12 at most. Led fully the cloth is swept under
            // the floor - so the light lead stays, by its name.)
            Assert.AreEqual(VeilTransform.MeltLead, VeilTransform.MeltGuide(0f), 1e-5f);
            Assert.AreEqual(VeilTransform.MeltLead, VeilTransform.MeltGuide(0.8f), 1e-5f, "all the way");
            Assert.AreEqual(0f, VeilTransform.MeltGuide(1f), 1e-5f, "then the cloth has it");
            Assert.That(VeilTransform.MeltLead, Is.InRange(0.05f, 0.3f), "lightly: the cloth's springs, the floor and the body do most of it");
        }

        [Test]
        public void EveryWeapon_HasARecovery_HeavierOnesLonger()
        {
            foreach (var f in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow })
                Assert.Greater(VeilCasting.Recovery(f), 0.05f, f.ToString());
            Assert.Less(VeilCasting.Recovery(VeilForm.Sword), VeilCasting.Recovery(VeilForm.Spear), "the sword is the quickest");
        }

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
        public void PressingJumpAgainWhileFalling_GlidesOnTheScarf_OnceTheDoubleJumpIsSpent()
        {
            var m = Airborne(out var air);
            for (int i = 0; i < 40; i++) m.Tick(1f / 60f, new MotorInput(), air);
            Assert.Less(m.Velocity.y, 0f);
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true, jumpHeld = true }, air);
            Assert.Greater(m.Velocity.y, 0f, "the first press in the air is the double jump");
            for (int i = 0; i < 60; i++) m.Tick(1f / 60f, new MotorInput(), air);
            Assert.Less(m.Velocity.y, 0f);
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true, jumpHeld = true }, air);
            for (int i = 0; i < 40; i++) m.Tick(1f / 60f, new MotorInput { jumpHeld = true }, air);
            Assert.IsTrue(m.IsGliding);
            Assert.AreEqual(-MotorLogic.GlideFallSpeed, m.Velocity.y, 0.3f);
        }

        [Test]
        public void DoubleJump_OnceInTheAir_RefreshedOnLanding()
        {
            var m = Airborne(out var air);
            int flips = 0;
            m.AirJumped += () => flips++;
            for (int i = 0; i < 30; i++) m.Tick(1f / 60f, new MotorInput(), air);
            float before = m.Velocity.y;
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true, jumpHeld = true }, air);
            Assert.AreEqual(1, flips);
            Assert.AreEqual(m.Settings.AirJumpVelocity, m.Velocity.y, 0.5f, "a fresh upward kick");
            Assert.Greater(m.Velocity.y, before);
            Assert.AreEqual(0, m.AirJumpsLeft);

            for (int i = 0; i < 10; i++) m.Tick(1f / 60f, new MotorInput(), air);
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true }, air);
            Assert.AreEqual(1, flips, "only one jump in the air");

            m.Tick(1f / 60f, new MotorInput(), new MotorContacts { grounded = true });
            Assert.AreEqual(1, m.AirJumpsLeft, "landing gives it back");
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
