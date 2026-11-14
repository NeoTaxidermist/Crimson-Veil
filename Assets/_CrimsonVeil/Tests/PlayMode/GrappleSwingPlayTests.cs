using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>Whip swing on a grapple ring against real colliders: a gap too wide to jump is crossed by swinging.</summary>
    public class GrappleSwingPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;
        static readonly Vector3 Ring = new Vector3(6.5f, 7.5f, 0f);

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
            root = new GameObject("GrappleLevel");
            Box(new Vector3(-5f, -0.5f, 0f), new Vector3(10f, 1f, 4f));   // near floor x -10..0
            Box(new Vector3(23f, -0.5f, 0f), new Vector3(20f, 1f, 4f));   // far floor x 13..33: a 13 m gap
            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform);
            ring.transform.position = Ring;
            ring.AddComponent<GrapplePoint>();

            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(-6f, 0.05f, 0f);
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

        /// <summary>Runs right and jumps off the edge; with <paramref name="grapple"/> latches, swings and lets go.</summary>
        IEnumerator RunAndJump(bool grapple, System.Action<bool> swung)
        {
            yield return Wait(0.3f);
            input.moveX = 1f;
            while (player.transform.position.x < -0.6f) yield return null;
            input.jump = true;
            input.jumpHeld = true;
            bool didSwing = false, released = false;
            float end = Time.time + 6f;
            while (Time.time < end)
            {
                var m = controller.Motor;
                if (grapple && !didSwing && GrapplePoint.CanReach(controller.GripPosition, Ring, 7.5f) && controller.TryGrapple(Ring))
                    didSwing = true;
                if (m.IsSwinging && m.SwingAngle > 0.55f && m.SwingAngularVelocity > 0f && !released)
                {
                    input.jump = true; // let go at the forward rise: the leap
                    released = true;
                }
                if (released && controller.IsGrounded) { input.moveX = 0f; break; }
                if (player.transform.position.y < -6f) break;
                yield return null;
            }
            swung(didSwing);
        }

        [UnityTest]
        public IEnumerator Gap_IsTooWideForAPlainJump()
        {
            yield return RunAndJump(false, _ => { });
            Assert.Less(player.transform.position.y, -1f, "without the whip the hero falls into the gap");
        }

        [UnityTest]
        public IEnumerator SwingOnTheRing_CarriesTheHeroAcrossTheGap()
        {
            bool swung = false;
            yield return RunAndJump(true, s => swung = s);
            yield return Wait(0.3f);
            Assert.IsTrue(swung, "the ring was in reach during the jump");
            Assert.Greater(player.transform.position.x, 13.2f, "landed on the far side");
            Assert.Greater(player.transform.position.y, -0.3f, "standing on the far floor, not in the gap");
        }

        [UnityTest]
        public IEnumerator Swinging_IsAPendulumUnderTheRing()
        {
            yield return Wait(0.2f);
            controller.Teleport(new Vector3(Ring.x - 3f, 2.5f, 0f)); // in the air over the gap, left of the ring
            yield return null;
            Assert.IsTrue(controller.TryGrapple(Ring));
            float minX = float.MaxValue, maxX = float.MinValue, maxDist = 0f;
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                var g = controller.GripPosition;
                minX = Mathf.Min(minX, g.x); maxX = Mathf.Max(maxX, g.x);
                maxDist = Mathf.Max(maxDist, Vector2.Distance(g, Ring));
                yield return null;
            }
            Assert.IsTrue(controller.Motor.IsSwinging, "hangs on until it jumps");
            Assert.Less(minX, Ring.x - 1f);
            Assert.Greater(maxX, Ring.x + 1f, "swings through under the ring to the other side");
            Assert.LessOrEqual(maxDist, controller.Settings.swingMaxLength + 0.3f, "the whip never stretches");
        }

        /// <summary>
        /// The prototype scene's high perch, relative to its ring: ring at (2, 7.8), a perch 6 m up from x -10 to -6
        /// — far above a jump. Standing near the ring, latch, pump, let go at the high back-swing: up onto the perch.
        /// </summary>
        [UnityTest]
        public IEnumerator PumpingAndLettingGo_ReachesAPerchFarAboveAJump()
        {
            var perchRing = new Vector3(2f, 7.8f, 0f);
            Box(new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 4f));   // floor x -10..10
            Box(new Vector3(-8f, 5.5f, 0f), new Vector3(4f, 1f, 4f));    // perch x -10..-6, top 6
            yield return Wait(0.2f);
            controller.Teleport(new Vector3(3f, 0.05f, 0f));
            yield return Wait(0.3f);
            Assert.IsTrue(controller.TryGrapple(perchRing));
            bool released = false;
            float peak = 0f, apex = float.MinValue;
            float end = Time.time + 10f;
            while (Time.time < end)
            {
                var m = controller.Motor;
                if (m.IsSwinging)
                {
                    // Lean the way the swing goes (pumping) until the swing is big; let go rising high on the left.
                    peak = Mathf.Max(peak, Mathf.Abs(m.SwingAngle));
                    input.moveX = Mathf.Abs(m.SwingAngularVelocity) > 0.05f ? Mathf.Sign(m.SwingAngularVelocity) : -1f;
                    if (peak > 1.45f && m.SwingAngle < -1.15f && m.SwingAngularVelocity < 0f)
                    {
                        input.jump = true;
                        input.jumpHeld = true;
                        input.moveX = -1f;
                        released = true;
                    }
                }
                else if (released)
                {
                    apex = Mathf.Max(apex, player.transform.position.y);
                    if (player.transform.position.x < -7f) input.moveX = 0f; // over the perch: stop leaning, drop onto it
                    if (controller.IsGrounded) { input.moveX = 0f; break; }
                }
                yield return null;
            }
            Assert.Greater(apex, 6.5f, "the leap rises far above a plain jump (3.4 m)");
            yield return Wait(0.3f);
            Assert.IsTrue(released, "pumped high enough to let go");
            Assert.Greater(player.transform.position.y, 5.9f, "landed up on the perch");
            Assert.Less(player.transform.position.x, -6f);
        }

        [UnityTest]
        public IEnumerator WhipAttack_InReach_LatchesInsteadOfCracking()
        {
            var veil = player.AddComponent<VeilController>();
            var low = new GameObject("LowRing");
            low.transform.SetParent(root.transform);
            low.transform.position = new Vector3(-3f, 6f, 0f); // above the near floor, ahead of the hero
            low.AddComponent<GrapplePoint>();
            yield return Wait(0.3f);
            Assert.IsNotNull(veil.GrappleCandidate, "the ring above is lit: the hero knows the whip and it is in reach");
            Assert.AreEqual(VeilForm.Flow, veil.Form, "lit while the scarf is still cloth — no switching needed");
            input.cast = VeilForm.Whip;
            yield return Wait(0.2f);
            Assert.IsTrue(controller.Motor.IsSwinging, "the attack lashed the whip round the ring");
            Assert.IsNotNull(veil.Latched);
            Assert.IsFalse(veil.IsAttacking, "no crack while latched");
            input.jump = true;
            yield return Wait(0.2f);
            Assert.IsFalse(controller.Motor.IsSwinging);
            Assert.IsNull(veil.Latched, "letting go frees the whip");
        }
    }
}
