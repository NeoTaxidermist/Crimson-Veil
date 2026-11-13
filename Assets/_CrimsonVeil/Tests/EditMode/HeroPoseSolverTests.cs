using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class HeroPoseSolverTests
    {
        const float Dt = 1f / 60f;

        static HeroState Ground(float speed) => new HeroState { speed = speed, maxSpeed = 8f, grounded = true };

        static HeroPose Run(HeroPoseSolver solver, float seconds, HeroState s)
        {
            HeroPose p = default;
            for (int i = 0; i < Mathf.RoundToInt(seconds / Dt); i++) p = solver.Step(Dt, s);
            return p;
        }

        [Test]
        public void Phase_AdvancesWithDistanceNotTime()
        {
            var solver = new HeroPoseSolver();
            Run(solver, 1f, Ground(0f));
            Assert.AreEqual(0f, solver.Phase, 1e-5f, "standing still must not walk in place");

            solver = new HeroPoseSolver();
            // 8 m/s for 0.1375 s = 1.1 m = half a run stride (2.2 m)
            for (int i = 0; i < 11; i++) solver.Step(0.0125f, Ground(8f));
            Assert.AreEqual(0.5f, solver.Phase, 0.01f);
        }

        [Test]
        public void Running_SwingsLegsInOppositePhase()
        {
            var solver = new HeroPoseSolver();
            float maxDiff = 0f, maxAbs = 0f;
            for (int i = 0; i < 120; i++)
            {
                var p = solver.Step(Dt, Ground(8f));
                maxDiff = Mathf.Max(maxDiff, Mathf.Abs(p.thighL - p.thighR));
                maxAbs = Mathf.Max(maxAbs, Mathf.Abs(p.thighL));
            }
            Assert.Greater(maxAbs, 30f, "running stride should be big");
            Assert.Greater(maxDiff, 60f, "legs should scissor");
        }

        [Test]
        public void Running_LeansForwardMoreThanIdle()
        {
            var idle = Run(new HeroPoseSolver(), 1f, Ground(0f));
            var run = Run(new HeroPoseSolver(), 1f, Ground(8f));
            Assert.Greater(run.spine, idle.spine + 8f);
        }

        [Test]
        public void Idle_IsCalmAndBreathes()
        {
            var solver = new HeroPoseSolver();
            float minChest = float.MaxValue, maxChest = float.MinValue;
            for (int i = 0; i < 240; i++)
            {
                var p = solver.Step(Dt, Ground(0f));
                Assert.Less(Mathf.Abs(p.thighL), 10f);
                minChest = Mathf.Min(minChest, p.chest);
                maxChest = Mathf.Max(maxChest, p.chest);
            }
            Assert.Greater(maxChest - minChest, 1f, "chest should rise and fall");
        }

        [Test]
        public void Rising_TucksLeadKnee()
        {
            var p = Run(new HeroPoseSolver(), 0.5f, new HeroState { verticalVelocity = 10f, maxSpeed = 8f });
            Assert.Greater(p.thighL, 45f);
            Assert.Greater(p.kneeL, 70f);
        }

        [Test]
        public void Landing_BendsKneesThenRecovers()
        {
            var solver = new HeroPoseSolver();
            Run(solver, 0.5f, new HeroState { verticalVelocity = -12f, maxSpeed = 8f });
            var impact = Run(solver, 0.06f, Ground(0f));
            Assert.Greater(solver.LandWeight, 0f);
            Assert.Greater(impact.kneeL, 30f);
            var settled = Run(solver, 1f, Ground(0f));
            Assert.Less(settled.kneeL, 15f);
            Assert.AreEqual(0f, solver.LandWeight);
        }

        [Test]
        public void Dash_LeansHard()
        {
            var p = Run(new HeroPoseSolver(), 0.3f, new HeroState { dashing = true, grounded = true, speed = 19f, maxSpeed = 8f });
            Assert.Greater(p.spine, 20f);
            Assert.Less(p.armL, -40f);
        }

        [Test]
        public void SwordAttack_WindsUpCutsAndFollowsThrough()
        {
            var s = Ground(0f);
            s.attacking = true;
            s.attackForm = CrimsonVeil.Veil.VeilForm.Sword;
            s.attackT = 0.28f;
            var up = Run(new HeroPoseSolver(), 0.3f, s);
            Assert.Greater(up.armR, 120f, "blade raised back over the shoulder");
            s.attackT = 0.52f;
            var down = Run(new HeroPoseSolver(), 0.3f, s);
            Assert.Less(down.armR, -30f, "follow-through below");
            Assert.Greater(down.thighL, 30f, "front leg lunges");
            Assert.Greater(down.spine, 15f, "body commits to the cut");
        }

        [Test]
        public void SpearAttack_DrawsBackThenThrustsStraight()
        {
            var back = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Spear, 0.3f);
            Assert.Greater(back.elbowR, 90f, "drawn back to the chest");
            var thrust = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Spear, 0.45f);
            Assert.Greater(thrust.armR, 75f);
            Assert.Less(thrust.elbowR, 10f, "arm fully extended");
        }

        [Test]
        public void BowAttack_DrawsTheStringWithTheOtherHand()
        {
            var draw = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Bow, 0.3f);
            Assert.Greater(draw.armR, 80f, "bow arm extended");
            Assert.Greater(draw.elbowL, 120f, "string hand pulled to the chest");
        }

        [Test]
        public void EveryAttack_StartsAndEndsInTheReadyStance()
        {
            foreach (var f in new[] { CrimsonVeil.Veil.VeilForm.Sword, CrimsonVeil.Veil.VeilForm.Spear, CrimsonVeil.Veil.VeilForm.Whip, CrimsonVeil.Veil.VeilForm.Shield, CrimsonVeil.Veil.VeilForm.Bow })
            {
                Assert.AreEqual(HeroPoseSolver.HoldArm, HeroAttackChoreography.Sample(f, 0f).armR, 1e-3f, f.ToString());
                Assert.AreEqual(HeroPoseSolver.HoldArm, HeroAttackChoreography.Sample(f, 1f).armR, 1e-3f, f.ToString());
            }
        }

        [Test]
        public void HoldingWeapon_RaisesArmToTheGrip()
        {
            var s = Ground(0f);
            var bare = Run(new HeroPoseSolver(), 0.5f, s);
            s.weaponHeld = 1f;
            var armed = Run(new HeroPoseSolver(), 0.5f, s);
            Assert.Greater(armed.armR, bare.armR + 15f, "standing: blade held forward");
            var running = Ground(8f);
            running.weaponHeld = 1f;
            var p = Run(new HeroPoseSolver(), 0.6f, running);
            Assert.Greater(p.armR, 25f, "running: forward guard, the weapon is never dragged behind");
        }

        [Test]
        public void Hanging_RaisesBothArms_ClimbEndsStanding()
        {
            var hang = Run(new HeroPoseSolver(), 0.5f, new HeroState { hanging = true, maxSpeed = 8f });
            Assert.Greater(hang.armL, 150f);
            Assert.Greater(hang.armR, 150f);
            var end = HeroPoseSolver.ClimbPose(1f);
            Assert.Less(Mathf.Abs(end.armL), 30f);
            Assert.Less(end.kneeL, 30f);
        }

        [Test]
        public void Output_StaysFinite()
        {
            var solver = new HeroPoseSolver();
            var p = solver.Step(5f, new HeroState { speed = 1000f, maxSpeed = 0f, verticalVelocity = float.MaxValue / 2f });
            Assert.IsTrue(p.IsFinite());
        }
    }
}
