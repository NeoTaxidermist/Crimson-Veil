using System.Collections.Generic;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The rules of the bestiary, engine independent.</summary>
    public class CreatureLogicTests
    {
        // ---- shield monk
        [Test]
        public void ShieldMonk_BlocksFromTheFront_NotFromBehind()
        {
            Assert.IsTrue(ShieldMonkLogic.Blocks(1, 0f, 1f, true), "facing right, hit from the right: blocked");
            Assert.IsFalse(ShieldMonkLogic.Blocks(1, 0f, -1f, true), "from behind: it lands");
            Assert.IsFalse(ShieldMonkLogic.Blocks(1, 0f, 1f, false), "shield torn away: it lands");
        }

        [Test]
        public void ShieldMonk_OnlyTheWhipTearsTheShieldAway()
        {
            Assert.IsTrue(ShieldMonkLogic.Disarms(DamageKind.Lash, true, true));
            Assert.IsFalse(ShieldMonkLogic.Disarms(DamageKind.Slash, true, true), "a sword just bounces off");
            Assert.IsFalse(ShieldMonkLogic.Disarms(DamageKind.Pierce, true, true));
            Assert.IsFalse(ShieldMonkLogic.Disarms(DamageKind.Lash, false, true), "from behind there's nothing to pull");
            Assert.AreEqual(DamageKind.Lash, VeilCombatProfile.Kind(CrimsonVeil.Veil.VeilForm.Whip), "the whip deals a lash");
        }

        // ---- root crawler
        [Test]
        public void RootCrawler_DropsOnAHeroPassingUnderneath()
        {
            Assert.IsTrue(RootCrawlerLogic.ShouldDrop(0.5f, -3f, 1.4f));
            Assert.IsFalse(RootCrawlerLogic.ShouldDrop(4f, -3f, 1.4f), "too far to the side");
            Assert.IsFalse(RootCrawlerLogic.ShouldDrop(0.5f, 1f, 1.4f), "the hero is above it");
        }

        // ---- hanged man
        [Test]
        public void HangedMan_SwingsBackToRest_AndPumpsTowardsTheHero()
        {
            Assert.Less(HangedManLogic.Acceleration(0.5f, 0f, 3f, 9.81f, 0.2f), 0f, "pulled back towards hanging straight");
            Assert.Greater(HangedManLogic.Acceleration(-0.5f, 0f, 3f, 9.81f, 0.2f), 0f);
            Assert.AreEqual(1f, HangedManLogic.Pump(0f, 1f, 2f), "moving towards the hero: push");
            Assert.AreEqual(0f, HangedManLogic.Pump(0f, -1f, 2f), "moving away: let it swing back");

            // Pumping makes the swing grow towards the hero.
            float angle = 0.05f, omega = 0f, maxTowards = 0f;
            for (int i = 0; i < 600; i++)
            {
                float a = HangedManLogic.Acceleration(angle, omega, 3.2f, 9.81f, 0.25f) + HangedManLogic.Pump(angle, omega, 2f) * 2.2f;
                omega += a / 60f; angle += omega / 60f;
                maxTowards = Mathf.Max(maxTowards, angle);
            }
            Assert.Greater(maxTowards, 0.3f, "swings out at the hero");
        }

        // ---- shadow double
        struct F { public float t; }

        [Test]
        public void ShadowDouble_ReplaysTheHeroLate_AndDiesInAFall()
        {
            var frames = new List<F> { new F { t = 0f }, new F { t = 0.5f }, new F { t = 1f }, new F { t = 1.5f } };
            Assert.IsTrue(ShadowDoubleLogic.TrySample(frames, 1.2f, f => f.t, out var s));
            Assert.AreEqual(1f, s.t, "the latest frame not newer than now − delay");
            Assert.IsFalse(ShadowDoubleLogic.TrySample(frames, -1f, f => f.t, out _), "nothing that old yet");
            Assert.IsTrue(ShadowDoubleLogic.FellToDeath(0f, -4f, 3.5f));
            Assert.IsFalse(ShadowDoubleLogic.FellToDeath(0f, -1f, 3.5f), "a step down is not a fall");
        }

        [Test]
        public void HangedMan_SwingStaysWithinItsReach()
        {
            // With the shipped pump strength the swing grows but never wraps up over the beam.
            float angle = 0.05f, omega = 0f, max = 0f;
            for (int i = 0; i < 1200; i++)
            {
                float a = HangedManLogic.Acceleration(angle, omega, 3.2f, 9.81f, 0.25f);
                if (Mathf.Abs(angle) < 0.75f) a += HangedManLogic.Pump(angle, omega, 2f) * 0.9f;
                omega += a / 60f; angle += omega / 60f;
                max = Mathf.Max(max, Mathf.Abs(angle));
            }
            Assert.Less(max, 1.15f, "a menacing swing, not a windmill");
        }

        // ---- crows
        [Test]
        public void CrowSwarm_DivesThroughTheHeroAndClimbsOut()
        {
            Vector3 from = new Vector3(0f, 3f, 0f), to = new Vector3(6f, 3f, 0f);
            Assert.AreEqual(from, CrowSwarmLogic.DivePath(from, to, 0f));
            Assert.AreEqual(to, CrowSwarmLogic.DivePath(from, to, 1f));
            Assert.Less(CrowSwarmLogic.DivePath(from, to, 0.5f).y, 1f, "swoops down to the hero's height");
        }

        // ---- ash moth
        [Test]
        public void AshMoth_CloudCatchesAHeroStandingInIt()
        {
            var cloud = new Vector3(10f, 1f, 0f);
            Assert.IsTrue(AshMothLogic.InCloud(new Vector3(10.5f, 1.2f, 0f), cloud, 1.7f));
            Assert.IsFalse(AshMothLogic.InCloud(new Vector3(13f, 1.2f, 0f), cloud, 1.7f), "stepped out of it");
            Assert.AreEqual(4.2f, AshMothLogic.HoverHeight(4.2f, 20f), 1e-4f, "open sky: high above");
            Assert.AreEqual(2.8f, AshMothLogic.HoverHeight(4.2f, 3.6f), 1e-4f, "under an overhang: below the rock");
        }

        // ---- lantern wisp
        [Test]
        public void LanternWisp_FindsTheDropAheadOfTheHero()
        {
            // Floor until x = 5, then a pit.
            float Ground(float x) => x < 5f ? 0f : float.NaN;
            Assert.IsTrue(LanternWispLogic.FindDrop(0f, 1, 10f, 0.5f, Ground, 0f, out float drop));
            Assert.AreEqual(5f, drop, 0.01f);
            Assert.IsFalse(LanternWispLogic.FindDrop(0f, -1, 10f, 0.5f, Ground, 0f, out _), "solid ground behind");
        }

        // ---- paper kite
        [Test]
        public void PaperKite_FliesOnATautThread_AndSwoopsAtTheHero()
        {
            var anchor = Vector3.zero;
            Assert.AreEqual(5f, Vector3.Distance(anchor, PaperKiteLogic.OnThread(anchor, 0.4f, 5f)), 1e-4f, "the thread is taut");
            Assert.Greater(PaperKiteLogic.OnThread(anchor, 0f, 5f).y, 4.9f, "no wind: straight up");
            Assert.Greater(PaperKiteLogic.AngleTowards(new Vector3(3f, 1f, 0f)), 0.3f, "leans towards a hero to its right");
            Assert.Less(PaperKiteLogic.AngleTowards(new Vector3(-3f, 1f, 0f)), -0.3f);
        }

        [Test]
        public void KiteCloth_LetsTheHeroGlide()
        {
            var motor = new MotorLogic(new MotorSettings());
            var air = new MotorContacts { grounded = false };
            var hold = new MotorInput { jumpHeld = true };
            for (int i = 0; i < 60; i++) motor.Tick(1f / 60f, hold, air);
            float freeFall = motor.Velocity.y;

            var glider = new MotorLogic(new MotorSettings()) { GlideTime = 8f };
            for (int i = 0; i < 60; i++) glider.Tick(1f / 60f, hold, air);
            Assert.IsTrue(glider.IsGliding);
            Assert.AreEqual(-MotorLogic.GlideFallSpeed, glider.Velocity.y, 0.3f, "floats down");
            Assert.Less(freeFall, -8f, "without the cloth he drops");
            Assert.Less(glider.GlideTime, 8f, "gliding spends the cloth");
        }
    }
}
