using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Wings beat with the flight, bodies pitch into it, a carried lantern swings behind.</summary>
    public class FlyerAnimationTests
    {
        [Test]
        public void Stroke_IsAQuickPowerStrokeDown_AndASlowerRecovery()
        {
            Assert.AreEqual(0f, FlyerAnimation.Stroke(0f), 1e-4f, "starts up");
            Assert.AreEqual(1f, FlyerAnimation.Stroke(FlyerAnimation.DownstrokeShare), 1e-4f, "fully down early in the cycle");
            Assert.AreEqual(0f, FlyerAnimation.Stroke(1f), 1e-4f, "back up at the end");
            // each half-way point is reached halfway through its own (unequal) share of the cycle
            float downHalf = FlyerAnimation.Stroke(FlyerAnimation.DownstrokeShare * 0.5f);
            float upHalf = FlyerAnimation.Stroke(FlyerAnimation.DownstrokeShare + (1f - FlyerAnimation.DownstrokeShare) * 0.5f);
            Assert.AreEqual(0.5f, downHalf, 0.01f);
            Assert.AreEqual(0.5f, upHalf, 0.01f);
            Assert.Less(FlyerAnimation.DownstrokeShare, 0.5f, "the downstroke takes less time than the recovery");
        }

        [Test]
        public void ClimbingItLabours_SinkingItGlides()
        {
            Assert.Greater(FlyerAnimation.BeatRate(2f, 3f), FlyerAnimation.BeatRate(2f, 0f));
            Assert.Less(FlyerAnimation.BeatRate(2f, -3f), FlyerAnimation.BeatRate(2f, 0f));
            Assert.Greater(FlyerAnimation.BeatRate(2f, -100f), 0f, "never stops dead in the air");
            Assert.AreEqual(1f, FlyerAnimation.StrokeWidth(2f), 1e-4f);
            Assert.Less(FlyerAnimation.StrokeWidth(-4f), 0.5f, "gliding down, the wings are held open, beating shallow");
        }

        [Test]
        public void Pitch_NoseDownDiving_UpClimbing_LevelHovering()
        {
            Assert.Less(FlyerAnimation.Pitch(new Vector2(3f, -6f), 55f), -30f);
            Assert.Less(FlyerAnimation.Pitch(new Vector2(-3f, -6f), 55f), -30f, "whichever way it flies");
            Assert.Greater(FlyerAnimation.Pitch(new Vector2(3f, 4f), 55f), 20f);
            Assert.AreEqual(0f, FlyerAnimation.Pitch(new Vector2(0.05f, 0.05f), 55f), 1e-4f);
            Assert.GreaterOrEqual(FlyerAnimation.Pitch(new Vector2(0f, -50f), 55f), -55f, "never past its limit");
        }

        [Test]
        public void ALanternCarriedOff_SwingsBehind_ThenSettlesStraight()
        {
            float a = 0f, v = 0f;
            for (int i = 0; i < 10; i++) FlyerAnimation.Pendulum(ref a, ref v, 12f, 1f / 60f);
            Assert.Greater(a, 2f, "pushed towards +x, its bottom lags back (to -x)");
            for (int i = 0; i < 600; i++) FlyerAnimation.Pendulum(ref a, ref v, 0f, 1f / 60f);
            Assert.AreEqual(0f, a, 0.5f, "left alone it hangs straight");
        }

        [Test]
        public void TheKiteRearsBack_AwayFromTheHero_BeforeItSwoops()
        {
            Assert.Less(PaperKiteLogic.RearBack(0f, 0.8f), 0f, "hero to the right: it pulls to the left first");
            Assert.Greater(PaperKiteLogic.RearBack(0f, -0.8f), 0f);
            Assert.GreaterOrEqual(PaperKiteLogic.RearBack(-1.2f, 0.5f), -1.3f, "never past the thread's limit");
        }
    }
}
