using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class HealthModelTests
    {
        static DamageInfo Hit(float amount) => new DamageInfo { amount = amount };

        [Test]
        public void Damage_ReducesHealthAndFiresEvents()
        {
            var h = new HealthModel(3f);
            int damaged = 0, died = 0;
            h.Damaged += _ => damaged++;
            h.Died += () => died++;
            Assert.IsTrue(h.TryDamage(Hit(1f)));
            Assert.AreEqual(2f, h.Current);
            Assert.IsTrue(h.TryDamage(Hit(5f)));
            Assert.AreEqual(0f, h.Current);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(2, damaged);
            Assert.AreEqual(1, died);
            Assert.IsFalse(h.TryDamage(Hit(1f)), "dead things take no more hits");
        }

        [Test]
        public void Invulnerability_BlocksHitsUntilItExpires()
        {
            var h = new HealthModel(5f, invulnerableTime: 0.5f);
            Assert.IsTrue(h.TryDamage(Hit(1f)));
            Assert.IsFalse(h.TryDamage(Hit(1f)));
            h.Tick(0.6f);
            Assert.IsTrue(h.TryDamage(Hit(1f)));
            Assert.AreEqual(3f, h.Current);
        }

        [Test]
        public void ZeroDamage_IsIgnored()
        {
            var h = new HealthModel(2f, 1f);
            Assert.IsFalse(h.TryDamage(Hit(0f)));
            Assert.IsFalse(h.IsInvulnerable);
        }

        [Test]
        public void Heal_ClampsAndRestoreRefills()
        {
            var h = new HealthModel(4f);
            h.TryDamage(Hit(3f));
            h.Heal(10f);
            Assert.AreEqual(4f, h.Current);
            h.TryDamage(Hit(4f));
            h.Heal(1f);
            Assert.AreEqual(0f, h.Current, "no healing the dead");
            h.Restore();
            Assert.AreEqual(4f, h.Current);
            Assert.AreEqual(1f, h.Fraction);
        }
    }

    public class EnemyBrainTests
    {
        const float Dt = 1f / 60f;

        static EnemySenses See(float self, float player, bool clear = true) =>
            new EnemySenses { selfX = self, playerX = player, playerAlive = true, pathClear = clear };

        static EnemyIntent Run(EnemyBrain b, float seconds, EnemySenses s)
        {
            EnemyIntent i = default;
            for (int k = 0; k < Mathf.RoundToInt(seconds / Dt); k++) i = b.Tick(Dt, s);
            return i;
        }

        [Test]
        public void Patrol_TurnsAroundAtEdgesOfRange()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            var i = b.Tick(Dt, new EnemySenses { selfX = -3.5f, playerX = 100f, playerAlive = true, pathClear = true });
            Assert.Greater(i.velocityX, 0f, "past the left edge it walks back right");
            i = b.Tick(Dt, new EnemySenses { selfX = 0f, playerX = 100f, playerAlive = true, pathClear = false });
            Assert.Less(i.velocityX, 0f, "blocked path flips direction");
        }

        [Test]
        public void SeesPlayer_ChasesTowardsThem()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.Tick(Dt, See(0f, 5f));
            Assert.AreEqual(EnemyState.Chase, b.State);
            var i = b.Tick(Dt, See(0f, 5f));
            Assert.AreEqual(1, i.facing);
            Assert.AreEqual(b.Settings.chaseSpeed, i.velocityX, 1e-4f);
        }

        [Test]
        public void InRange_TelegraphsThenStrikesThenRecovers()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.Tick(Dt, See(0f, 1f));
            b.Tick(Dt, See(0f, 1f));
            Assert.AreEqual(EnemyState.Windup, b.State);

            var i = Run(b, b.Settings.windupTime * 0.9f, See(0f, 1f));
            Assert.AreEqual(EnemyState.Windup, b.State);
            Assert.IsFalse(i.strikeActive, "no damage during the telegraph");
            Assert.AreEqual(0f, i.velocityX);

            i = Run(b, b.Settings.windupTime * 0.2f, See(0f, 1f));
            Assert.AreEqual(EnemyState.Strike, b.State);
            Assert.IsTrue(i.strikeActive);
            Assert.Greater(i.velocityX, 0f, "lunges at the player");

            Run(b, b.Settings.strikeTime + 0.05f, See(0f, 1f));
            Assert.AreEqual(EnemyState.Recover, b.State);
            Assert.AreEqual(1, b.StrikeCount);
        }

        [Test]
        public void Hurt_InterruptsWindup()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.Tick(Dt, See(0f, 1f));
            b.Tick(Dt, See(0f, 1f));
            b.OnHurt(dead: false);
            Assert.AreEqual(EnemyState.Hurt, b.State);
            Run(b, b.Settings.windupTime + 0.1f, See(0f, 1f));
            Assert.AreEqual(0, b.StrikeCount, "interrupted wind-up never lands");
        }

        [Test]
        public void Dead_StaysDead()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.OnHurt(dead: true);
            var i = Run(b, 2f, See(0f, 1f));
            Assert.AreEqual(EnemyState.Dead, b.State);
            Assert.AreEqual(0f, i.velocityX);
            b.OnStaggered();
            Assert.AreEqual(EnemyState.Dead, b.State);
        }

        [Test]
        public void LosesPlayer_ReturnsToPatrol()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.Tick(Dt, See(0f, 5f));
            b.Tick(Dt, See(0f, 30f));
            Assert.AreEqual(EnemyState.Patrol, b.State);
        }

        [Test]
        public void Stagger_ForcesLongRecovery()
        {
            var b = new EnemyBrain(new EnemyBrainSettings(), 0f);
            b.OnStaggered();
            Run(b, b.Settings.recoverTime, See(0f, 1f));
            Assert.AreEqual(EnemyState.Recover, b.State, "stagger recovery is longer than normal");
        }
    }

    public class VeilCombatProfileTests
    {
        [Test]
        public void Flow_DealsNoDamage()
        {
            Assert.AreEqual(0f, VeilCombatProfile.Damage(VeilForm.Flow));
        }

        [Test]
        public void Spear_OutdamagesWhip_ShieldKnocksBackHardest()
        {
            Assert.Greater(VeilCombatProfile.Damage(VeilForm.Spear), VeilCombatProfile.Damage(VeilForm.Whip));
            Assert.Greater(VeilCombatProfile.Knockback(VeilForm.Shield), VeilCombatProfile.Knockback(VeilForm.Sword));
        }

        [Test]
        public void ActiveWindow_IsInsideTheSwing()
        {
            foreach (var f in new[] { VeilForm.Whip, VeilForm.Sword, VeilForm.Spear, VeilForm.Shield })
            {
                Assert.IsFalse(VeilCombatProfile.IsActive(f, 0f), f + " at start");
                Assert.IsFalse(VeilCombatProfile.IsActive(f, 1f), f + " at end");
                var w = VeilCombatProfile.ActiveWindow(f);
                Assert.IsTrue(VeilCombatProfile.IsActive(f, (w.x + w.y) * 0.5f), f + " mid");
            }
        }

        [Test]
        public void SegmentDistance_Works()
        {
            Assert.AreEqual(1f, VeilCombatProfile.SegmentDistance(Vector3.zero, Vector3.right * 2f, new Vector3(1f, 1f, 0f)), 1e-5f);
            Assert.AreEqual(1f, VeilCombatProfile.SegmentDistance(Vector3.zero, Vector3.right * 2f, new Vector3(3f, 0f, 0f)), 1e-5f);
            Assert.AreEqual(2f, VeilCombatProfile.SegmentDistance(Vector3.zero, Vector3.zero, new Vector3(0f, 2f, 0f)), 1e-5f);
        }

        [Test]
        public void ShieldBubble_BlocksFromBothSides()
        {
            Assert.IsTrue(VeilCombatProfile.ShieldBlocks(VeilForm.Shield, 1f, facing: 1, heroX: 0f, attackerX: 2f));
            Assert.IsTrue(VeilCombatProfile.ShieldBlocks(VeilForm.Shield, 1f, facing: 1, heroX: 0f, attackerX: -2f), "the bubble surrounds him");
            Assert.IsFalse(VeilCombatProfile.ShieldBlocks(VeilForm.Shield, 0.3f, facing: 1, heroX: 0f, attackerX: 2f), "not hardened yet");
            Assert.IsFalse(VeilCombatProfile.ShieldBlocks(VeilForm.Sword, 1f, facing: 1, heroX: 0f, attackerX: 2f));
        }
    }

    public class MotorKnockbackTests
    {
        [Test]
        public void Knockback_IgnoresInputWhileStunned()
        {
            var m = new MotorLogic(new MotorSettings());
            var ground = new MotorContacts { grounded = true };
            m.Tick(1f / 60f, new MotorInput(), ground);
            m.ApplyKnockback(new Vector2(-6f, 5f), 0.3f);
            Assert.IsTrue(m.IsStunned);
            var v = m.Tick(1f / 60f, new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true }, ground);
            Assert.Less(v.x, -5f, "knockback carries, input ignored");
            Assert.Less(v.y, 5f, "no jump while stunned");
            for (int i = 0; i < 30; i++) m.Tick(1f / 60f, new MotorInput(), new MotorContacts());
            Assert.IsFalse(m.IsStunned);
        }
    }
}
