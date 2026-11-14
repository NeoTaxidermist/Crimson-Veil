using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    public class PlayerControllerPlayTests
    {
        GameObject ground, player;
        ScriptedInput input;
        PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 4f);

            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 1f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            input = player.AddComponent<ScriptedInput>();
            controller = player.AddComponent<PlayerController>();
            player.AddComponent<VeilController>();
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        IEnumerator WaitSeconds(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Player_LandsOnGround()
        {
            yield return WaitSeconds(0.8f);
            Assert.IsTrue(controller.IsGrounded);
            Assert.AreEqual(0f, player.transform.position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Player_RunsRightAndStaysOnPlane()
        {
            yield return WaitSeconds(0.5f);
            input.moveX = 1f;
            yield return WaitSeconds(0.6f);
            Assert.Greater(player.transform.position.x, 2f);
            Assert.AreEqual(0f, player.transform.position.z, 1e-3f);
        }

        [UnityTest]
        public IEnumerator DoubleJump_RisesAgainWithASomersault()
        {
            yield return WaitSeconds(0.5f);
            input.jump = true;
            input.jumpHeld = true;
            yield return WaitSeconds(0.5f); // past the apex of the first jump
            float y1 = player.transform.position.y;
            Assert.Less(controller.FlipProgress, 0f, "no flip on the ground jump");
            input.jump = true;
            yield return WaitSeconds(0.15f);
            Assert.That(controller.FlipProgress, Is.InRange(0.05f, 0.95f), "somersaulting");
            Assert.Greater(controller.FlipWrap, 0.9f, "the scarf wound round");
            yield return WaitSeconds(0.2f);
            Assert.Greater(player.transform.position.y, y1 + 0.8f, "the second jump carries him higher");
            input.jumpHeld = false;
            yield return WaitSeconds(1.5f);
            Assert.IsTrue(controller.IsGrounded);
            Assert.Less(controller.FlipProgress, 0f, "landed: the turn is over");
        }

        [UnityTest]
        public IEnumerator Player_JumpsAndComesBack()
        {
            yield return WaitSeconds(0.5f);
            input.jump = true;
            input.jumpHeld = true;
            yield return WaitSeconds(0.3f);
            Assert.Greater(player.transform.position.y, 1.5f);
            input.jumpHeld = false;
            yield return WaitSeconds(1.2f);
            Assert.IsTrue(controller.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Autopilot_RunsAndJumps()
        {
            var auto = player.AddComponent<AutopilotInput>();
            auto.jumpInterval = 0.5f;
            controller.InputSource = auto;
            float maxY = 0f;
            float end = Time.time + 1.6f;
            while (Time.time < end)
            {
                maxY = Mathf.Max(maxY, player.transform.position.y);
                yield return null;
            }
            Assert.Greater(player.transform.position.x, 6f);
            Assert.Greater(maxY, 1.5f, "autopilot should have jumped");
        }

        [UnityTest]
        public IEnumerator Veil_HardensIntoSwordAndAttacks()
        {
            var veil = player.GetComponent<VeilController>();
            yield return WaitSeconds(0.3f);
            // One press: the scarf whirls into the blade (the transformation is the wind-up) and strikes at once.
            input.cast = VeilForm.Sword;
            yield return WaitSeconds(0.2f);
            Assert.AreEqual(VeilForm.Sword, veil.Form);
            Assert.IsTrue(veil.IsAttacking, "the blow follows the transformation without a second press");

            // After the blow it lingers a moment, then melts back into cloth by itself.
            yield return WaitSeconds(0.46f + VeilCasting.Linger + 0.9f);
            Assert.AreEqual(VeilForm.Flow, veil.Form);
            Assert.AreEqual(0f, veil.Hardness, 1e-3f);
        }
    }
}
