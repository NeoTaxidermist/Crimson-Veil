using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    [Category("Hero")]
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
        public void Idle_StandsInTheConceptStance_SwaysAndBreathes()
        {
            var solver = new HeroPoseSolver();
            float minChest = float.MaxValue, maxChest = float.MinValue, minArm = float.MaxValue, maxArm = float.MinValue;
            for (int i = 0; i < 300; i++)
            {
                var p = solver.Step(Dt, Ground(0f));
                if (i < 60) continue;
                Assert.Greater(p.thighL, 15f, "near leg forward");
                Assert.Less(p.thighR, -15f, "far leg back");
                minChest = Mathf.Min(minChest, p.chest);
                maxChest = Mathf.Max(maxChest, p.chest);
                minArm = Mathf.Min(minArm, p.armL);
                maxArm = Mathf.Max(maxArm, p.armL);
            }
            Assert.Greater(maxChest - minChest, 1f, "chest should rise and fall");
            Assert.Greater(maxArm - minArm, 8f, "the arms sway (as on the walk sheet)");
        }

        [Test]
        public void IdleCycle_LoopsSmoothly_WithTheFeetFlat()
        {
            var a = HeroIdleCycle.Sample(0f);
            var b = HeroIdleCycle.Sample(HeroIdleCycle.Duration);
            Assert.AreEqual(a.armL, b.armL, 1e-3f, "the loop wraps");
            float maxJump = 0f;
            for (int i = 1; i <= 360; i++)
            {
                var p = HeroIdleCycle.Sample((i - 1) * HeroIdleCycle.Duration / 360f);
                var q = HeroIdleCycle.Sample(i * HeroIdleCycle.Duration / 360f);
                maxJump = Mathf.Max(maxJump, Mathf.Max(Mathf.Abs(q.armL - p.armL), Mathf.Abs(q.armR - p.armR), Mathf.Abs(q.head - p.head)));
            }
            Assert.Less(maxJump, 1f, "no jerks between the sheet's frames");
            foreach (var f in HeroIdleCycle.Frames)
            {
                Assert.AreEqual(0f, f.footL + (f.thighL - f.kneeL), 0.2f, "near foot flat");
                Assert.AreEqual(0f, f.footR + (f.thighR - f.kneeR), 0.2f, "far foot flat");
                Assert.GreaterOrEqual(f.kneeL, 0f, "knees never bend backwards");
                Assert.GreaterOrEqual(f.kneeR, 0f);
            }
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
            s.attackT = 0.66f;
            var down = Run(new HeroPoseSolver(), 0.3f, s);
            Assert.Less(down.armR, 30f, "follow-through low");
            Assert.Greater(down.armR, 0f, "follow-through still in front, not behind the body");
            Assert.Greater(down.thighL, 30f, "front leg lunges");
            Assert.Greater(down.spine, 15f, "body commits to the cut");
        }

        [Test]
        public void SwordCut_HandStaysInFrontAndSweepsWithoutASnap()
        {
            const float upper = 0.3f, fore = 0.28f;
            float dur = CrimsonVeil.Veil.VeilFormLibrary.AttackDuration(CrimsonVeil.Veil.VeilForm.Sword);
            float prev = float.NaN, maxSpeed = 0f;
            for (int i = 0; i <= 400; i++)
            {
                float t = i / 400f;
                var k = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Sword, t);
                float a = k.armR * Mathf.Deg2Rad, f = (k.armR + k.elbowR) * Mathf.Deg2Rad;
                float handForward = Mathf.Sin(a) * upper + Mathf.Sin(f) * fore;
                Assert.Greater(handForward, 0.05f, $"sword fist in front of the shoulder at t={t:0.00}, not hidden behind the head or body");
                if (!float.IsNaN(prev)) maxSpeed = Mathf.Max(maxSpeed, Mathf.Abs(k.armR - prev) / (dur / 400f));
                prev = k.armR;
            }
            Assert.Less(maxSpeed, 1300f, "the arm sweeps over several frames, no one-frame jump");
        }

        [Test]
        public void SpearAttack_DrawsBackThenThrustsStraight()
        {
            var back = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Spear, 0.3f);
            Assert.Greater(back.elbowR, 90f, "drawn back to the chest");
            // (the forearm whips straight a beat after the shoulder: see HeroAttackChoreography.ElbowLag)
            var thrust = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Spear, 0.45f + HeroAttackChoreography.ElbowLag);
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
                Assert.AreEqual(HeroPoseSolver.HoldArmFor(f), HeroAttackChoreography.Sample(f, 0f).armR, 1e-3f, f.ToString());
                Assert.AreEqual(HeroPoseSolver.HoldArmFor(f), HeroAttackChoreography.Sample(f, 1f).armR, 1e-3f, f.ToString());
            }
        }

        [Test]
        public void HoldingWeapon_RaisesArmToTheGrip()
        {
            var s = Ground(0f);
            var bare = Run(new HeroPoseSolver(), 0.5f, s);
            s.weaponHeld = 1f;
            var armed = Run(new HeroPoseSolver(), 0.5f, s);
            // (standing, the near arm already hangs a little forward - the walk sheet's stance)
            Assert.AreEqual(HeroPoseSolver.HoldArm, armed.armR, 5f, "standing: blade held forward at the grip");
            Assert.Greater(armed.armR, bare.armR, "the weapon raises the arm");
            var running = Ground(8f);
            running.weaponHeld = 1f;
            var p = Run(new HeroPoseSolver(), 0.6f, running);
            Assert.Greater(p.armR, 25f, "running: forward guard, the weapon is never dragged behind");
        }

        [Test]
        public void Hanging_RaisesBothArms_ClimbEndsStanding()
        {
            var hang = Run(new HeroPoseSolver(), 0.5f, new HeroState { hanging = true, maxSpeed = 8f });
            Assert.That(hang.armL, Is.InRange(135f, 165f), "up and forward to the lip, which is in front of him");
            Assert.That(hang.armR, Is.InRange(135f, 165f));
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

        [Test]
        public void HangingWithAWeapon_TheRightHandLetsGoOfTheLip_AndSwingsTheBlow_TheLeftHolds()
        {
            Assert.AreEqual(0f, HeroPoseSolver.HangFreeArm(false, 1f), "only hanging");
            Assert.AreEqual(0f, HeroPoseSolver.HangFreeArm(true, 0f), "no weapon: both hands on the lip");
            Assert.AreEqual(1f, HeroPoseSolver.HangFreeArm(true, 1f), 1e-5f);

            var hang = new HeroState { hanging = true, hangArm = HeroPoseSolver.DefaultHangArm };
            var bare = Run(new HeroPoseSolver(), 0.6f, hang);

            var armed = hang;
            armed.weaponHeld = 1f;
            armed.attackForm = CrimsonVeil.Veil.VeilForm.Sword;
            var held = Run(new HeroPoseSolver(), 0.6f, armed);
            Assert.Greater(Mathf.Abs(held.armR - bare.armR), 40f, "the sword arm comes down off the lip to hold the blade");
            Assert.AreEqual(bare.armL, held.armL, 2f, "the left hand still holds the lip");
            Assert.AreEqual(bare.thighL, held.thighL, 2f, "the legs stay braced");

            // a blow: the free arm sweeps through the swing
            var solver = new HeroPoseSolver();
            Run(solver, 0.4f, armed);
            float lo = float.MaxValue, hi = float.MinValue;
            for (float t = 0f; t <= 1f; t += Dt * 2f)
            {
                var s = armed;
                s.attacking = true;
                s.attackT = t;
                var p = solver.Step(Dt, s);
                lo = Mathf.Min(lo, p.armR); hi = Mathf.Max(hi, p.armR);
                Assert.AreEqual(bare.armL, p.armL, 2f, "the left hand holds on through the blow");
            }
            Assert.Greater(hi - lo, 60f, "hanging, the blow is really swung");
        }

        [Test]
        public void Attacks_FlowThroughTheirKeys_WithoutStoppingAtEach()
        {
            // the sword arm's speed through the cut (0.5) and the follow-through (0.66) keys: it keeps moving
            // (at the top of the wind-up it turns round, so there it may pass through zero)
            foreach (var key in new[] { 0.5f, 0.66f })
            {
                float a = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Sword, key - 0.01f).armR;
                float b = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Sword, key + 0.01f).armR;
                Assert.Greater(Mathf.Abs(b - a), 0.5f, "the arm doesn't halt at the key pose at t " + key);
            }
            // it still starts from and returns to the ready stance, eased
            var r0 = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Sword, 0f);
            var r1 = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Sword, 1f);
            Assert.AreEqual(r0.armR, r1.armR, 1e-3f);
        }

        [Test]
        public void Attacks_BreakThroughTheJoints_TheBodyLeads_TheForearmFollows()
        {
            Assert.Greater(HeroAttackChoreography.TorsoLead, 0f);
            Assert.Greater(HeroAttackChoreography.ElbowLag, 0f);
            // into the spear thrust the shoulder is up before the forearm has whipped straight
            float shoulderUp = -1f, elbowStraight = -1f;
            for (float t = 0.3f; t <= 0.7f; t += 0.002f)
            {
                var k = HeroAttackChoreography.Sample(CrimsonVeil.Veil.VeilForm.Spear, t);
                if (shoulderUp < 0f && k.armR >= 75f) shoulderUp = t;
                if (elbowStraight < 0f && k.elbowR <= 10f) elbowStraight = t;
            }
            Assert.Greater(shoulderUp, 0f); Assert.Greater(elbowStraight, 0f);
            Assert.Greater(elbowStraight, shoulderUp, "the forearm straightens after the shoulder has come up");
        }

        [Test]
        public void StartAndStop_TheBodyLeansIn_ThenCarriesOnOverTheFeet_AndSettles()
        {
            Assert.Greater(HeroPoseSolver.TorsoTarget(1f), 5f, "setting off: lean into it");
            Assert.Greater(HeroPoseSolver.TorsoTarget(-1f), 10f, "stopping: the upper body carries on forward");
            Assert.Greater(HeroPoseSolver.ArmTarget(-1f), 10f, "stopping: the arms are flung forward");

            var solver = new HeroPoseSolver();
            Run(solver, 1f, Ground(8f));
            float peak = 0f, low = 0f;
            for (int i = 0; i < 90; i++)
            {
                solver.Step(Dt, Ground(i < 5 ? 8f - 1.6f * (i + 1) : 0f));   // a stop in a tenth of a second
                peak = Mathf.Max(peak, solver.Momentum);
                low = Mathf.Min(low, solver.Momentum);
            }
            Assert.Greater(peak, 4f, "the stop throws the torso forward");
            Assert.Less(low, -0.3f, "then it swings back past upright");
            Assert.AreEqual(0f, solver.Momentum, 0.3f, "and settles");
        }

        [Test]
        public void StartingToRun_ThePoseRunsIn_OverAFewFrames()
        {
            var solver = new HeroPoseSolver();
            Run(solver, 1f, Ground(0f));
            var idle = solver.Current;
            var first = solver.Step(Dt, Ground(8f));
            var later = Run(solver, 0.5f, Ground(8f));
            Assert.Less(Mathf.Abs(first.spine - idle.spine), Mathf.Abs(later.spine - idle.spine) * 0.6f,
                "one frame in, the body is not yet in its full run lean");
        }

        [Test]
        public void IdleArms_Live_EachOnItsOwnRhythm()
        {
            float lo = float.MaxValue, hi = float.MinValue, corr = 0f;
            for (float t = 0f; t < 12f; t += 0.05f)
            {
                var l = HeroPoseSolver.IdleLife(t);
                lo = Mathf.Min(lo, l.elbowR); hi = Mathf.Max(hi, l.elbowR);
                corr += l.armL * l.armR;
            }
            Assert.Greater(hi - lo, 6f, "the elbow eases in and out");
            var a = HeroPoseSolver.IdleLife(1f);
            Assert.AreNotEqual(a.armL, a.armR, "the arms are not mirrored");
        }

        [Test]
        public void TheMantle_FlowsThroughItsKeys_TheFeetScrabble_TheTrailingLegStepsUp()
        {
            // it still passes through each key pose ...
            var knee = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbKneeEnd);
            Assert.Greater(knee.thighL, 100f);
            // ... without halting at it: the body keeps moving through the press
            float a = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbPressEnd - 0.01f).spine;
            float b = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbPressEnd + 0.01f).spine;
            Assert.Greater(Mathf.Abs(b - a), 0.2f, "the torso flows on through the press");
            // pulling up, the legs work in turns against the wall
            float lo = float.MaxValue, hi = float.MinValue;
            for (float t = 0f; t < HeroPoseSolver.ClimbPullEnd; t += 0.005f)
            {
                var p = HeroPoseSolver.ClimbPose(t);
                lo = Mathf.Min(lo, p.thighL - p.thighR); hi = Mathf.Max(hi, p.thighL - p.thighR);
            }
            Assert.Greater(hi - lo, 20f, "the feet scrabble, one then the other");
            // the trailing leg comes through and steps up beside the knee
            var step = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbStep);
            Assert.Greater(step.thighR, 45f, "the trailing leg swings up");
        }

        [Test]
        public void OnTheWhip_BothHandsCloseOnTheRope()
        {
            var up = HeroPoseSolver.RopeUp(new Vector3(0f, 2f, 0.4f), new Vector2(3f, 6f));
            Assert.AreEqual(new Vector3(0.6f, 0.8f, 0f).x, up.x, 1e-4f, "up the rope, towards the ring");
            Assert.AreEqual(0.8f, up.y, 1e-4f);
            Assert.AreEqual(0f, up.z, "the rope lies in the plane of play");
            Assert.AreEqual(Vector3.up, HeroPoseSolver.RopeUp(new Vector3(1f, 1f, 0f), new Vector2(1f, 1f)));
            Assert.Greater(HeroPoseSolver.RopeHighHand - HeroPoseSolver.RopeLowHand, 0.08f, "one fist above the other, not one on the other");
            Assert.Less(HeroPoseSolver.RopeHighHand - HeroPoseSolver.RopeLowHand, 0.2f, "close together, like holding a rope");
        }

        [Test]
        public void OnTheWhip_TheLegsTrailTheSwing_AndPump()
        {
            var forward = HeroPoseSolver.SwingPose(1f, 0f);
            var back = HeroPoseSolver.SwingPose(-1f, 0f);
            Assert.Greater(forward.thighL, back.thighL + 30f, "reaching out ahead on the way forward");
            Assert.Less(back.kneeL, 30f, "trailing behind, the legs stay nearly straight (folded, he knelt in the air)");
            Assert.Greater(back.thighL - back.kneeL, -35f, "the shins never stick up behind him");
            Assert.Greater(forward.kneeL, 5f, "the knees never lock straight");

            // the legs lag the swing: a sudden swing forward reaches them only over a few frames, then overshoots
            var solver = new HeroPoseSolver();
            var s = new HeroState { swinging = true, swingSpeed = 0f };
            Run(solver, 0.5f, s);
            s.swingSpeed = 2.2f;
            solver.Step(Dt, s);
            Assert.Less(solver.SwingLegs, 0.2f, "one frame in, the legs have not caught up yet");
            float peak = 0f;
            for (int k = 0; k < 90; k++) { solver.Step(Dt, s); peak = Mathf.Max(peak, solver.SwingLegs); }
            Assert.Greater(peak, 1.02f, "they swing past, then settle");
            Assert.AreEqual(1f, solver.SwingLegs, 0.05f);
        }
    }
}
