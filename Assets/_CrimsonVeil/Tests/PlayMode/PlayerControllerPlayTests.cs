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
        sealed class ScriptedInput : MonoBehaviour, IPlayerInput
        {
            public float moveX;
            public bool jump, jumpHeld, dash, attack;
            public VeilForm? form;

            // "Pressed" flags are one-shot pulses consumed by the first reader.
            public float MoveX => moveX;
            public bool JumpPressed { get { bool v = jump; jump = false; return v; } }
            public bool JumpHeld => jumpHeld;
            public bool DashPressed { get { bool v = dash; dash = false; return v; } }
            public bool AttackPressed { get { bool v = attack; attack = false; return v; } }
            public VeilForm? FormRequested { get { var v = form; form = null; return v; } }
        }

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
        public IEnumerator Veil_HardensIntoSwordAndAttacks()
        {
            var veil = player.GetComponent<VeilController>();
            yield return WaitSeconds(0.3f);
            input.form = VeilForm.Sword;
            yield return WaitSeconds(0.4f);
            Assert.AreEqual(VeilForm.Sword, veil.Form);
            Assert.AreEqual(VeilFormLibrary.Hardness(VeilForm.Sword), veil.Hardness, 1e-3f);
            input.attack = true;
            yield return null;
            yield return null;
            Assert.IsTrue(veil.IsAttacking);

            input.form = VeilForm.Sword; // toggle back to cloth
            yield return WaitSeconds(0.4f);
            Assert.AreEqual(VeilForm.Flow, veil.Form);
            Assert.AreEqual(0f, veil.Hardness, 1e-3f);
        }
    }
}
