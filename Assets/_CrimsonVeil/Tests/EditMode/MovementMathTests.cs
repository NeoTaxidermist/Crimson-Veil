using CrimsonVeil.Core;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    public class MovementMathTests
    {
        [Test]
        public void JumpVelocityAndGravity_ReachRequestedHeight()
        {
            float g = MovementMath.GravityForJump(3f, 0.4f);
            float v = MovementMath.JumpVelocity(3f, 0.4f);
            Assert.AreEqual(3f, MovementMath.ApexHeight(v, g), 1e-4f);
            Assert.AreEqual(0.4f, v / g, 1e-4f, "time to apex = v / g");
        }

        [Test]
        public void Damp_IsFrameRateIndependent()
        {
            // Two half steps must equal one full step.
            float full = MovementMath.Damp(10f, 0.1f);
            float half = MovementMath.Damp(10f, 0.05f);
            float twoHalves = 1f - (1f - half) * (1f - half);
            Assert.AreEqual(full, twoHalves, 1e-5f);
        }

        [Test]
        public void Easings_HitEndpoints()
        {
            Assert.AreEqual(0f, MovementMath.EaseOutCubic(0f), 1e-6f);
            Assert.AreEqual(1f, MovementMath.EaseOutCubic(1f), 1e-6f);
            Assert.AreEqual(0f, MovementMath.EaseInOutSine(0f), 1e-6f);
            Assert.AreEqual(1f, MovementMath.EaseInOutSine(1f), 1e-6f);
        }
    }
}
