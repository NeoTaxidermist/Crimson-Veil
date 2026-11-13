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
        public void Attack_RaisesFrontArm()
        {
            var s = Ground(0f);
            s.attacking = true;
            var p = Run(new HeroPoseSolver(), 0.3f, s);
            Assert.Greater(p.armR, 70f);
        }

        [Test]
        public void HoldingWeapon_RaisesArmToTheGrip()
        {
            var s = Ground(0f);
            var bare = Run(new HeroPoseSolver(), 0.5f, s);
            s.weaponHeld = 1f;
            var armed = Run(new HeroPoseSolver(), 0.5f, s);
            Assert.Greater(armed.armR, bare.armR + 40f);
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
