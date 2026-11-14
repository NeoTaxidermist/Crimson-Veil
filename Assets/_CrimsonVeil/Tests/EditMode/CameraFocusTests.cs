using CrimsonVeil.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The camera frames the floor the hero stands on and leads where he is going — calm, not jittery.</summary>
    public class CameraFocusTests
    {
        const float Dt = 1f / 60f, Run = 8f;

        static CameraFocus Fresh(Vector2 at)
        {
            var f = new CameraFocus();
            f.Reset(at);
            return f;
        }

        [Test]
        public void JumpingOnTheSpot_DoesNotMoveTheCameraUpAndDown()
        {
            var f = Fresh(Vector2.zero);
            float g = 47f, vy = 17.9f, y = 0f, maxDy = 0f;
            for (int i = 0; i < 200; i++)
            {
                vy -= g * Dt; y = Mathf.Max(0f, y + vy * Dt);
                var p = f.Step(Dt, new Vector2(0f, y), new Vector2(0f, vy), y <= 0f && vy < 0f, Run);
                maxDy = Mathf.Max(maxDy, Mathf.Abs(p.y));
            }
            Assert.Less(maxDy, 0.4f, "a jump keeps the floor line (the hero rises within the frame)");
        }

        [Test]
        public void LandingHigher_ReframesOnTheNewFloor()
        {
            var f = Fresh(Vector2.zero);
            f.Step(Dt, new Vector2(0f, 2.5f), new Vector2(0f, 5f), false, Run);
            Assert.AreEqual(0f, f.FloorY, 1e-3f, "mid-air: still the old floor");
            f.Step(Dt, new Vector2(1f, 4f), Vector2.zero, true, Run);
            Assert.AreEqual(4f, f.FloorY, 1e-3f, "landed on the ledge: frame it");
        }

        [Test]
        public void TurningRoundOrATap_DoesNotSwingTheView()
        {
            var f = Fresh(Vector2.zero);
            float x = 0f;
            for (int i = 0; i < 120; i++) { x += Run * Dt; f.Step(Dt, new Vector2(x, 0f), new Vector2(Run, 0f), true, Run); }
            Assert.Greater(f.Lead, 1.6f, "running right: the view leads to the right");
            for (int i = 0; i < 6; i++) f.Step(Dt, new Vector2(x, 0f), new Vector2(-2f, 0f), true, Run); // a brief turn / tap back
            Assert.Greater(f.Lead, 1.2f, "a moment's turn does not swing the camera round");
            var g = Fresh(Vector2.zero);
            for (int i = 0; i < 120; i++) g.Step(Dt, Vector2.zero, Vector2.zero, true, Run); // standing, turning to strike
            Assert.AreEqual(0f, g.Lead, 1e-3f);
        }

        [Test]
        public void ARealFall_IsFollowedAndLed()
        {
            var f = Fresh(Vector2.zero);
            float y = 0f, vy = 0f;
            Vector2 p = Vector2.zero;
            for (int i = 0; i < 90; i++)
            {
                vy = Mathf.Max(-18f, vy - 75f * Dt); y += vy * Dt;
                p = f.Step(Dt, new Vector2(0f, y), new Vector2(0f, vy), false, Run);
            }
            Assert.Less(y, -15f);
            Assert.Less(p.y, y + f.windowDown, "the camera came down with him");
            Assert.Less(p.y, y + f.windowDown - 1f, "and looks a little ahead of the fall");
        }

        [Test]
        public void AJumpAcrossTheLevel_Snaps()
        {
            var f = Fresh(Vector2.zero);
            var p = f.Step(Dt, new Vector2(60f, 5f), Vector2.zero, true, Run);
            Assert.AreEqual(60f, p.x, 0.01f);
            Assert.AreEqual(5f, p.y, 0.01f);
        }
    }
}
