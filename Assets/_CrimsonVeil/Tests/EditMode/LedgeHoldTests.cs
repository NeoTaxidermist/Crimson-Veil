using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// Holding a lip: the hands hold it and the body hangs from them; coming over it he is carried all the way (the hands
    /// on it until a foot is down on the top); and the stretch of a jump stops at a roof.
    /// </summary>
    public class LedgeHoldTests
    {
        [Test]
        public void TheBody_ComesToTheHands_OnlyAsFarAsTheArmIsShort()
        {
            var shoulder = new Vector3(0f, 1f, 0f);
            Assert.AreEqual(Vector3.zero, HeroPoseSolver.PullToReach(shoulder, new Vector3(0.3f, 1.4f, 0f), 0.6f), "within reach: the body stays");
            // 1 m away with an arm of 0.6: 0.4 m, straight towards the hand's place
            var pull = HeroPoseSolver.PullToReach(shoulder, new Vector3(0.6f, 1.8f, 0f), 0.6f);
            Assert.AreEqual(0.4f, pull.magnitude, 1e-4f);
            Assert.AreEqual(0.24f, pull.x, 1e-4f);
            Assert.AreEqual(0.32f, pull.y, 1e-4f);
            Assert.AreEqual(Vector3.zero, HeroPoseSolver.PullToReach(shoulder, shoulder, 0.6f), "no direction to go: nothing");
        }

        [Test]
        public void TheHands_LieOnTheLip_AndStayThere_UntilAFootIsComingDown()
        {
            Assert.AreEqual(0f, HeroPoseSolver.LipPress(0f), 1e-5f, "hanging: hooked over the corner");
            Assert.AreEqual(1f, HeroPoseSolver.LipPress(HeroPoseSolver.ClimbPressEnd), 1e-5f, "pressed over: flat on the top");
            Assert.AreEqual(HeroPoseSolver.LipHandOffset(0.2f).x, HeroPoseSolver.LipHandOffset(0f).x, 1e-5f, "pulling up, the hands do not move on the lip");
            // (they let go straight after the press once: at 0.6 to 0.72 of the climb, and he rose the rest with nothing under him)
            Assert.Greater(HeroPoseSolver.LipLetGo, HeroPoseSolver.ClimbPressEnd + 0.04f, "they stay while the knee comes up");
            Assert.AreEqual(1f, HeroPoseSolver.LipHold(false, true, HeroPoseSolver.LipLetGo), 1e-5f);
            Assert.Greater(HeroPoseSolver.LipHold(false, true, 0.5f * (HeroPoseSolver.LipLetGo + HeroPoseSolver.ClimbKneeEnd)), 0.3f, "and come off it gradually");
            Assert.AreEqual(0f, HeroPoseSolver.LipHold(false, true, HeroPoseSolver.ClimbKneeEnd), 1e-5f);
            // the hand lies on the top and the fingers press onto it: a fist there closes on nothing
            Assert.AreEqual(HeroPoseSolver.LipFingers, HeroPoseSolver.LedgeGrip(true, false, 0f), 1e-5f);
            Assert.That(HeroPoseSolver.LipFingers, Is.InRange(0.05f, 0.4f));
        }

        [Test]
        public void ComingOverTheLip_HeIsCrouchedOnIt_AndTheHipsRiseWithoutAHop()
        {
            // The climb sets his place at the top's height at the end of the knee phase (ClimbRiseOver): the hips' own
            // height over where he hung is that rise plus the pose's bob.
            const float rise = 1.95f;   // (the lip 1.95 m over his feet where he hangs)
            float Hips(float t) => HeroPoseSolver.ClimbRiseOver(t).x * rise + HeroPoseSolver.ClimbPose(t).bob;
            Assert.Less(HeroPoseSolver.ClimbCrouch, -0.5f, "crouched deep on the top");
            Assert.AreEqual(HeroPoseSolver.ClimbCrouch, HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbKneeEnd).bob, 1e-4f);
            float knee = Hips(HeroPoseSolver.ClimbKneeEnd) - Hips(HeroPoseSolver.ClimbPressEnd);
            Assert.That(knee, Is.InRange(0.05f, 0.45f), $"the knee comes up: the hips rise {knee:F2} m in that fifth of the climb (0.86 m it was - a hop)");
            float up = Hips(1f) - Hips(HeroPoseSolver.ClimbKneeEnd);
            Assert.Greater(up, 0.4f, "and he stands up from the crouch on his own legs");
            for (float t = HeroPoseSolver.ClimbPressEnd; t < 1f; t += 0.02f)
                Assert.GreaterOrEqual(Hips(t + 0.02f), Hips(t) - 0.03f, $"he does not sink back at {t:F2}");
            // the trailing leg still hangs down the wall behind him (forward of that its thigh lay through the corner)
            var k = HeroPoseSolver.ClimbPose(HeroPoseSolver.ClimbKneeEnd);
            Assert.Less(k.thighR, 0f);
            Assert.Less(k.kneeR, 60f, "hanging, not kicked up behind");
            Assert.Greater(k.armL, 40f, "the arms down under the leaned chest (not swept back along it)");
        }

        [Test]
        public void TheStretchOfAJump_StopsAtARoof()
        {
            // 1.8 m tall, taking off (a fifth taller for a moment)
            Assert.AreEqual(1.2f, PlayerController.StretchUnder(1.2f, 1.8f, 5f), 1e-5f, "open sky: all of it");
            float low = PlayerController.StretchUnder(1.2f, 1.8f, 2f);
            Assert.AreEqual((2f - PlayerController.RoofClearance) / 1.8f, low, 1e-5f, "a roof 2 m up: as far as it is");
            Assert.LessOrEqual(1.8f * low, 2f - PlayerController.RoofClearance + 1e-5f);
            Assert.AreEqual(1f, PlayerController.StretchUnder(1.2f, 1.8f, 1.8f), 1e-5f, "no room at all: his own height, never less (the capsule has that)");
            Assert.AreEqual(0.84f, PlayerController.StretchUnder(0.84f, 1.8f, 1.9f), 1e-5f, "a squash (landing) is left alone");
        }
    }
}
