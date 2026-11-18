using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Enemies move with their gait and stage their blows: anticipation, action, follow-through.</summary>
    public class CreatureAnimationTests
    {
        [Test]
        public void Wraith_ChasingRakesItsClawsInTurn_WithTheDistance_NotInPlace()
        {
            var a = CreatureAnimation.Wraith(EnemyState.Chase, 0f, 1f, 0.25f, 0f);
            var b = CreatureAnimation.Wraith(EnemyState.Chase, 0f, 1f, 0.75f, 0f);
            Assert.Greater(a.armL - a.armR, 40f, "one claw reaches while the other rakes back");
            Assert.Greater(b.armR - b.armL, 40f, "half a stroke on, they have swapped");
            // standing in the same spot (the phase doesn't move), the pose doesn't change with time
            var c = CreatureAnimation.Wraith(EnemyState.Chase, 0f, 1f, 0.25f, 3.7f);
            Assert.AreEqual(a.armL, c.armL, 1e-4f);
            Assert.Greater(a.spine, CreatureAnimation.Wraith(EnemyState.Patrol, 0f, 0f, 0f, 0f).spine, "it stoops into the chase");
        }

        [Test]
        public void Wraith_Windup_RearsBackAndUp_ThenStrikes_LeadingArmFirst_ThenFollowsThrough()
        {
            var rest = CreatureAnimation.Wraith(EnemyState.Patrol, 0f, 0f, 0f, 0f);
            var coiled = CreatureAnimation.Wraith(EnemyState.Windup, 1f, 0f, 0f, 0f);
            Assert.Less(coiled.spine, rest.spine - 15f, "anticipation: it rears back");
            Assert.Greater(coiled.lift, 0.2f, "and rises");
            Assert.Less(coiled.lunge, -0.1f, "and draws back");
            Assert.Greater(coiled.armR, 120f, "claws thrown high");

            var early = CreatureAnimation.Wraith(EnemyState.Strike, 0.3f, 0f, 0f, 0f);
            Assert.Less(early.armR, coiled.armR - 60f, "the strike arm is already coming down");
            Assert.Greater(early.armL - early.armR, 30f, "the other claw follows behind it");
            var hit = CreatureAnimation.Wraith(EnemyState.Strike, 1f, 0f, 0f, 0f);
            Assert.Greater(hit.spine, 15f, "the body snaps forward");
            Assert.Greater(hit.lunge, 0.25f, "and throws itself after the blow");

            // follow-through: picks up where the blow ended (no jump), the claw swings on past its rest, then settles
            var after = CreatureAnimation.Wraith(EnemyState.Recover, 0f, 0f, 0f, 0f);
            Assert.AreEqual(hit.armR, after.armR, 1f);
            Assert.AreEqual(hit.spine, after.spine, 1f);
            Assert.AreEqual(hit.lunge, after.lunge, 0.02f);
            float lowest = float.MaxValue;
            for (float t = 0f; t <= 1f; t += 0.02f) lowest = Mathf.Min(lowest, CreatureAnimation.Wraith(EnemyState.Recover, t, 0f, 0f, 0f).armR);
            var settled = CreatureAnimation.Wraith(EnemyState.Recover, 1f, 0f, 0f, 0f);
            Assert.Less(lowest, settled.armR - 10f, "the claw swings through past its rest");
            Assert.AreEqual(0f, settled.lunge, 0.02f, "and the body is gathered back");
        }

        [Test]
        public void Monk_WalksWithAHeavyStep_DrawsBackBehindTheShield_AndThrowsHimselfIntoTheBash()
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (float ph = 0f; ph < 1f; ph += 0.05f)
            {
                var w = CreatureAnimation.Monk(ShieldMonk.State.Walk, 0f, 1f, ph, true, 0f);
                lo = Mathf.Min(lo, w.lift); hi = Mathf.Max(hi, w.lift);
            }
            Assert.Greater(hi - lo, 0.05f, "the weight drops onto each step");
            var still = CreatureAnimation.Monk(ShieldMonk.State.Walk, 0f, 0f, 0.3f, true, 0f);
            Assert.AreEqual(0f, still.lift, 1e-4f, "standing, no stepping");

            var coiled = CreatureAnimation.Monk(ShieldMonk.State.Windup, 1f, 0f, 0f, true, 0f);
            var bash = CreatureAnimation.Monk(ShieldMonk.State.Bash, 1f, 0f, 0f, true, 0f);
            Assert.Less(coiled.spine, 0f, "he draws back");
            Assert.Less(coiled.lunge, 0f);
            Assert.Greater(bash.spine, 20f, "the whole body behind the shield");
            Assert.Greater(bash.lunge, 0.3f);
            Assert.Less(bash.elbowL, 10f, "the shield arm driven straight out");

            var open = CreatureAnimation.Monk(ShieldMonk.State.Walk, 0f, 0f, 0f, false, 0f);
            Assert.Greater(open.spine, still.spine + 8f, "disarmed, he hunches");
        }

        [Test]
        public void HopSquash_FlatLanding_CrouchingToLeap_StretchedInFlight()
        {
            Assert.AreEqual(1f, CreatureAnimation.HopSquash(true, 1f, 1f, 0f), 1e-4f, "at rest");
            Assert.Less(CreatureAnimation.HopSquash(true, 0.09f, 1f, 0f), 0.75f, "squashed landing");
            Assert.Less(CreatureAnimation.HopSquash(true, 1f, 0.01f, 0f), 0.8f, "crouched to leap");
            Assert.Greater(CreatureAnimation.HopSquash(false, 1f, 1f, 5f), 1.1f, "stretched in flight");
            Assert.LessOrEqual(CreatureAnimation.HopSquash(false, 1f, 1f, 100f), 1.3f + 1e-4f, "never pulled out of shape");
        }
    }
}
