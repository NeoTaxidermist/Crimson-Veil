using System.Collections;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>Wall slide / wall jump / ledge climb against real colliders.</summary>
    public class TraversalPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;

        GameObject Box(Vector3 center, Vector3 size)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.transform.SetParent(root.transform);
            b.transform.position = center;
            b.transform.localScale = size;
            return b;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TraversalLevel");
            Box(new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 4f));       // floor
            Box(new Vector3(4.5f, 2.3f, 0f), new Vector3(2f, 4.6f, 4f));     // ledge, top at y=4.6 (above a plain jump)
            Box(new Vector3(-6f, 6f, 0f), new Vector3(1f, 12f, 4f));         // tall wall on the left

            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            input = player.AddComponent<ScriptedInput>();
            controller = player.AddComponent<PlayerController>();
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(root);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator JumpAtLedge_GrabsAndClimbsOnTop()
        {
            yield return Wait(0.3f);
            input.moveX = 1f;
            yield return Wait(0.25f);
            input.jump = true;
            input.jumpHeld = true;
            bool grabbed = false;
            float end = Time.time + 1.2f;
            while (Time.time < end)
            {
                if (controller.Motor.State == MotorState.LedgeHang || controller.Motor.State == MotorState.Climbing) grabbed = true;
                if (controller.Motor.State == MotorState.LedgeHang) input.jump = true; // climb up
                if (controller.Motor.State == MotorState.Climbing) input.moveX = 0f;   // don't run off the far side
                yield return null;
            }
            input.moveX = 0f;
            input.jumpHeld = false;
            yield return Wait(0.4f);
            Assert.IsTrue(grabbed, "the ledge is higher than a jump, so it must be grabbed");
            Assert.Greater(player.transform.position.y, 4.5f, "standing on top of the ledge");
            Assert.Greater(player.transform.position.x, 3.5f);
        }

        [UnityTest]
        public IEnumerator Shaft_CanBeClimbedByAlternatingWallJumps()
        {
            // Second wall 3.2 m to the right of the tall left wall: a shaft like the one in the level.
            Box(new Vector3(-2.3f, 6f, 0f), new Vector3(1f, 12f, 4f));
            player.transform.position = new Vector3(-4f, 0.05f, 0f);
            controller.Teleport(new Vector3(-4f, 0.05f, 0f));
            yield return Wait(0.2f);

            int dir = -1;
            input.moveX = dir;
            input.jump = true; input.jumpHeld = true;
            float maxY = 0f;
            float end = Time.time + 5f;
            while (Time.time < end)
            {
                var m = controller.Motor;
                if (m.State == MotorState.WallSlide && m.AttachSide == dir)
                {
                    dir = -dir;           // kick off and steer to the other wall
                    input.jump = true;
                    input.moveX = dir;
                }
                maxY = Mathf.Max(maxY, player.transform.position.y);
                yield return null;
            }
            Assert.Greater(maxY, 7f, "a 3.2 m shaft must be climbable");
        }

        [UnityTest]
        public IEnumerator WallJump_GainsHeightOnTallWall()
        {
            yield return Wait(0.3f);
            input.moveX = -1f;
            yield return Wait(0.6f);
            input.jump = true; input.jumpHeld = true;
            yield return Wait(0.45f);
            // Now airborne against the wall: kick off it.
            float startY = player.transform.position.y;
            input.jump = true;
            yield return Wait(0.05f);
            Assert.Greater(controller.Motor.Velocity.x, 0f, "pushed away from the left wall");
            Assert.Greater(controller.Motor.Velocity.y, 0f);
            float maxY = startY;
            float end = Time.time + 0.5f;
            while (Time.time < end) { maxY = Mathf.Max(maxY, player.transform.position.y); yield return null; }
            Assert.Greater(maxY, startY + 1f);
        }
    }
}
