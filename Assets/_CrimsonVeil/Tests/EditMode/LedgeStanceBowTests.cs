using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The ledge hang and the mantle, the planted stance (Ctrl: attack from the spot), the bow aimed overhead, and the
    /// shield monk turning his shield with him.
    /// </summary>
    public class LedgeStanceBowTests
    {
        const float Dt = 1f / 60f;

        // ---- planted stance

        [Test]
        public void Planted_TheKeysTurnTheHero_ButHeDoesNotStep()
        {
            var m = new MotorLogic(new MotorSettings());
            var ground = new MotorContacts { grounded = true };
            for (int i = 0; i < 10; i++) m.Tick(Dt, new MotorInput(), ground);
            for (int i = 0; i < 30; i++) m.Tick(Dt, new MotorInput { moveX = -1f, stand = true }, ground);
            Assert.AreEqual(0f, m.Velocity.x, 1e-4f, "planted: no running");
            Assert.AreEqual(-1, m.Facing, "but turned the way the key points (the blow goes that way)");
            for (int i = 0; i < 30; i++) m.Tick(Dt, new MotorInput { moveX = -1f }, ground);
            Assert.Less(m.Velocity.x, -1f, "let go of Ctrl and he runs again");
        }

        [Test]
        public void Planted_DoesNotStopAJumpOrTheAir()
        {
            var m = new MotorLogic(new MotorSettings());
            for (int i = 0; i < 20; i++) m.Tick(Dt, new MotorInput { moveX = 1f, stand = true }, new MotorContacts());
            Assert.Greater(m.Velocity.x, 0.5f, "in the air the stance doesn't hold him");
        }

        // ---- ledge

        [Test]
        public void TheMantle_GoesUpFirst_ThenOver()
        {
            var press = HeroPoseSolver.ClimbRiseOver(HeroPoseSolver.ClimbPressEnd);
            Assert.AreEqual(0f, press.y, 1e-4f, "nothing over the lip while still pulling and pressing up");
            Assert.Greater(press.x, 0.4f);
            var knee = HeroPoseSolver.ClimbRiseOver(HeroPoseSolver.ClimbKneeEnd);
            Assert.AreEqual(1f, knee.x, 1e-4f, "the feet are up at the lip before he walks over it");
            Assert.AreEqual(Vector2.one, HeroPoseSolver.ClimbRiseOver(1f));
            float prev = -1f;
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                var k = HeroPoseSolver.ClimbRiseOver(t);
                Assert.GreaterOrEqual(k.x, prev - 1e-5f, "it only rises");
                prev = k.x;
            }
        }

        [Test]
        public void TheMantle_PressesDownOnTheLip_ThenTheKneeComesUp()
        {
            var pull = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbPullEnd);
            Assert.Greater(pull.elbowL, 90f, "the pull-up: elbows bent, chin to the lip");
            var press = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbPressEnd);
            Assert.Less(press.armL, 0f, "the press: arms straight down onto the top");
            Assert.Less(press.elbowL, 20f);
            var knee = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbKneeEnd);
            Assert.Greater(knee.thighL, 100f, "a knee swung up onto the top");
        }

        [Test]
        public void TheHands_GripTheLip_AndOpenOnTheTop()
        {
            // (the hand lies on the lip's top, the fingers pressing onto it: the fist it made there - 0.8 - closed on nothing)
            Assert.That(HeroPoseSolver.LedgeGrip(true, false, 0f), Is.InRange(0.05f, 0.4f));
            Assert.That(HeroPoseSolver.LedgeGrip(false, true, 0.1f), Is.InRange(0.05f, 0.4f));
            Assert.Less(HeroPoseSolver.LedgeGrip(false, true, 0.7f), 0.3f, "palms flat as he presses over");
            Assert.AreEqual(0f, HeroPoseSolver.LedgeGrip(false, false, 0f));
        }

        [Test]
        public void ARaisedArm_DoesNotTakeTheCollarboneForward()
        {
            Assert.Greater(HeroBodyMotion.ShoulderReach(60f), 5f, "a thrust: the shoulder reaches in");
            Assert.AreEqual(0f, HeroBodyMotion.ShoulderReach(170f), 1e-4f, "hanging overhead: it would tip the arms back over the head");
            Assert.AreEqual(0f, HeroBodyMotion.ShoulderReach(150f), 1e-4f);
        }

        // ---- bow

        [Test]
        public void TheBowAimedUp_StandsOverTheBrow_NotBehindTheHead()
        {
            var up = HeroAttackChoreography.Sample(VeilForm.Bow, 0.3f, 90f);
            var level = HeroAttackChoreography.Sample(VeilForm.Bow, 0.3f, 0f);
            float lean = (level.spine + level.chest) - (up.spine + up.chest);   // + leaning back
            float bowArm = up.armR + lean;                                     // the bow arm's angle in the world (0 down, 90 forward, 180 up)
            Assert.That(bowArm, Is.InRange(140f, 172f), "up, a little in front of the face");
            var half = HeroAttackChoreography.Sample(VeilForm.Bow, 0.3f, 45f);
            Assert.Greater(half.armR, level.armR + 25f, "aimed half way up the arms still rise");
        }

        // ---- shield monk

        [Test]
        public void TheMonksShield_FacesTheWayHeFaces_OnTheCameraSide()
        {
            var rest = Quaternion.identity;   // the shield's face is its -z (towards the camera) at rest
            foreach (int f in new[] { 1, -1 })
            {
                ShieldMonk.ShieldHold(new Vector3(0.55f, 1.05f, -0.15f), f, rest, out var offset, out var rot);
                var face = rot * Vector3.back;
                Assert.Greater(face.x * f, 0.3f, "turned towards his front");
                Assert.Less(face.z, -0.3f, "and still towards the camera: its face, never its back");
                Assert.Greater(offset.x * f, 0.3f, "held out on the side he faces");
                Assert.Less(offset.z, 0f, "on the camera's side of the body");
            }
            ShieldMonk.ShieldHold(new Vector3(0.55f, 1.05f, -0.15f), 0f, rest, out var mid, out _);
            Assert.Less(mid.z, -0.3f, "turning round, it swings wide in front of him, not through him");
        }
    }
}
