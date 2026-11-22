using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The body goes with the blow: he looks where he strikes, and striking upwards the trunk goes back into it. (The
    /// rules; on the hero himself: HeroMoveSetPlayTests.StrikingUp_HeLooksAlongTheBlow_AndLeansBackIntoIt.)
    /// </summary>
    public class HeroBlowBearingTests
    {
        [Test]
        public void HeLooksAlongTheBlow_FromTheWindUpToItsEnd_AndOnlyAlongASteepOne()
        {
            Assert.AreEqual(0f, HeroAnimator.LookThrough(-1f), "no blow, no turn");
            Assert.AreEqual(0f, HeroAnimator.LookThrough(0f), 1e-5f, "it comes in from the start of the blow");
            Assert.AreEqual(1f, HeroAnimator.LookThrough(0.12f), 1e-4f, "...with the wind-up");
            Assert.AreEqual(1f, HeroAnimator.LookThrough(0.5f), 1e-4f, "all through the strike");
            Assert.AreEqual(1f, HeroAnimator.LookThrough(0.78f), 1e-4f);
            Assert.AreEqual(0f, HeroAnimator.LookThrough(1f), 1e-4f, "ahead again as it ends");
            for (float t = 0f; t < 1f; t += 0.02f)
                Assert.Less(Mathf.Abs(HeroAnimator.LookThrough(t + 0.02f) - HeroAnimator.LookThrough(t)), 0.3f, $"no jump at {t:F2}");

            Assert.AreEqual(0f, HeroAnimator.LookSteep(0f), "a level blow: the head is the pose's (he looks that way already)");
            Assert.AreEqual(0f, HeroAnimator.LookSteep(8f), 1e-5f);
            Assert.AreEqual(1f, HeroAnimator.LookSteep(45f), 1e-5f, "a diagonal one turns it");
            Assert.AreEqual(1f, HeroAnimator.LookSteep(90f), 1e-5f);
            Assert.AreEqual(1f, HeroAnimator.LookSteep(-90f), 1e-5f, "down as well as up");
            Assert.AreEqual(HeroAnimator.LookSteep(20f), HeroAnimator.LookSteep(-20f), 1e-5f);
        }

        [Test]
        public void TheFaceTurnsOntoTheBlow_AsFarAsANeckGoesOffTheChest()
        {
            // within the neck's reach: right onto the blow
            Assert.AreEqual(55f, HeroAnimator.LookTurn(-10f, 45f, 10f), 1e-4f, "looking 10 under the level, the blow at 45: 55 back");
            Assert.AreEqual(0f, HeroAnimator.LookTurn(45f, 45f, 10f), 1e-4f, "looking along it already: none");
            // straight up over an upright chest: as far as the neck goes, not a head laid flat on the back
            Assert.AreEqual(HeroAnimator.LookUpMost + 15f, HeroAnimator.LookTurn(-15f, 90f, 0f), 1e-4f);
            Assert.Less(HeroAnimator.LookUpMost, 60f, "a neck's reach");
            // the chest gone back into the blow: that much further
            Assert.AreEqual(20f + HeroAnimator.LookUpMost, HeroAnimator.LookTurn(0f, 90f, 20f), 1e-4f);
            // downwards
            Assert.AreEqual(-HeroAnimator.LookDownMost, HeroAnimator.LookTurn(0f, -90f, 0f), 1e-4f, "down off an upright chest: as far as the chin goes");
            // bent over in the leap, striking under his feet: right down onto the blow - and never past it
            Assert.AreEqual(-60f, HeroAnimator.LookTurn(-30f, -90f, -60f), 1e-4f);
            Assert.AreEqual(-90f, -30f + HeroAnimator.LookTurn(-30f, -90f, -80f), 1e-4f, "not past the blow");
        }

        [Test]
        public void StrikingUp_TheTrunkGoesBack_WithTheBlow_NotInItsWindUp()
        {
            Assert.AreEqual(0f, HeroAnimator.LeanThrough(-1f), "no blow, no lean");
            Assert.AreEqual(0f, HeroAnimator.LeanThrough(0.1f), 1e-5f, "in the wind-up he coils down for it");
            Assert.AreEqual(0f, HeroAnimator.LeanThrough(0.2f), 1e-5f);
            Assert.AreEqual(1f, HeroAnimator.LeanThrough(0.45f), 1e-4f, "back as the blow rises");
            Assert.AreEqual(1f, HeroAnimator.LeanThrough(0.7f), 1e-4f);
            Assert.AreEqual(0f, HeroAnimator.LeanThrough(1f), 1e-4f, "upright again as it ends");
            Assert.Greater(HeroAnimator.LookThrough(0.15f), HeroAnimator.LeanThrough(0.15f) + 0.5f, "the eyes go first");

            Assert.AreEqual(0f, HeroAnimator.LeanFor(0f), "a level blow is its own pose");
            Assert.AreEqual(0f, HeroAnimator.LeanFor(-90f), "a blow downwards bends him over by itself");
            Assert.AreEqual(0f, HeroAnimator.LeanFor(20f), 1e-5f);
            Assert.AreEqual(-HeroAnimator.LeanBackMost, HeroAnimator.LeanFor(90f), 1e-4f, "straight up: all of it");
            Assert.That(HeroAnimator.LeanFor(45f), Is.InRange(-HeroAnimator.LeanBackMost * 0.6f, -1f), "a diagonal blow: some");
            for (float aim = 0f; aim < 90f; aim += 5f)
                Assert.LessOrEqual(HeroAnimator.LeanFor(aim + 5f), HeroAnimator.LeanFor(aim) + 1e-5f, $"the steeper, the further back ({aim})");
            Assert.That(HeroAnimator.LeanBackMost, Is.InRange(6f, 20f), "a lean one sees, not a fall");
        }

        [Test]
        public void TheLeanIsOnlyEverBack_AndNoMoreThanTheBlowAsks()
        {
            Assert.AreEqual(-HeroAnimator.LeanBackMost, HeroAnimator.LeanTurn(0f, 90f), 1e-4f, "upright: back to the lean");
            Assert.AreEqual(-HeroAnimator.LeanBackMost - 5f, HeroAnimator.LeanTurn(5f, 90f), 1e-4f, "leaning forward: that much more");
            Assert.AreEqual(-HeroAnimator.LeanTurnMost, HeroAnimator.LeanTurn(25f, 90f), 1e-4f, "at a run he straightens some: he is not thrown back");
            Assert.AreEqual(0f, HeroAnimator.LeanTurn(-HeroAnimator.LeanBackMost, 90f), 1e-4f, "back that far already: none");
            Assert.AreEqual(0f, HeroAnimator.LeanTurn(-30f, 90f), 1e-4f, "never forward");
            Assert.AreEqual(0f, HeroAnimator.LeanTurn(10f, 0f), 1e-5f, "a level blow: none, however he leans");
            Assert.AreEqual(0f, HeroAnimator.LeanTurn(10f, -90f), 1e-5f, "nor one downwards");
            float atARun = HeroAnimator.LeanTurn(12f, 45f);
            Assert.Less(atARun, -1f, "the diagonal cut at a run: a little");
            Assert.Greater(atARun, -HeroAnimator.LeanTurnMost * 0.5f, "...only a little");
        }
    }
}
