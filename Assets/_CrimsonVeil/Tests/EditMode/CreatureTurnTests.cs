using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Creatures turn round in 3D (through facing the camera), never as a mirrored sprite flip.</summary>
    public class CreatureTurnTests
    {
        [Test]
        public void Turn_IsARotation_ThroughTheCamera()
        {
            var t = new CreatureTurn(1);
            Assert.AreEqual(0f, t.Yaw, 1e-4f);
            bool sawCamera = false;
            for (int i = 0; i < 60; i++)
            {
                var q = t.Step(-1, 1f / 60f, 0.35f);
                var forward = q * Vector3.right;
                Assert.AreEqual(1f, forward.magnitude, 1e-4f, "a rotation, no squashing scale");
                if (forward.z < -0.95f) sawCamera = true;
            }
            Assert.IsTrue(sawCamera, "mid-turn the body faces the camera (-z)");
            Assert.AreEqual(180f, t.Yaw, 1e-3f);
            Assert.IsFalse(t.Turning);
        }

        [Test]
        public void Turn_TakesItsDuration_AndEasesInAndOut()
        {
            var t = new CreatureTurn(1);
            t.Step(-1, 0.05f, 0.5f);
            float early = t.Yaw;
            Assert.IsTrue(t.Turning);
            Assert.Less(early, 180f * 0.1f, "slow start (eased), not a snap");
            t.Step(-1, 0.2f, 0.5f);
            Assert.AreEqual(90f, t.Yaw, 1e-3f, "half way at half the time");
            t.Step(-1, 0.3f, 0.5f);
            Assert.AreEqual(180f, t.Yaw, 1e-3f);
        }

        [Test]
        public void Turn_BackAgain_RetracesThePath()
        {
            var t = new CreatureTurn(-1);
            Assert.AreEqual(180f, t.Yaw, 1e-4f);
            t.Step(1, 0.175f, 0.35f);
            Assert.AreEqual(90f, t.Yaw, 1e-3f, "turns back through the camera, not the long way round");
        }
    }
}
