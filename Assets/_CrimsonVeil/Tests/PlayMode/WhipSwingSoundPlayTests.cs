using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// On the whip the sound goes with the swing itself: he is heard each time he passes under the ring - the whip
    /// groaning, the air rushing - and nowhere else (a creaking loop of its own beside it ran out of step with him).
    /// </summary>
    [Category("Motor")]
    [Category("Hero")]
    public class WhipSwingSoundPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;
        static readonly Vector3 Ring = new Vector3(6.5f, 7.5f, 0f);

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("SwingSoundLevel");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(-5f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(10f, 1f, 4f);
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
            GameSound.ResetCount();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(root);
        }

        static int Heard(Cue cue) => GameSound.Count.TryGetValue(cue, out int k) ? k : 0;

        [UnityTest]
        public IEnumerator Swinging_IsHeardAsHePassesUnderTheRing_AndNowhereElse()
        {
            var sounds = player.GetComponent<HeroSounds>();
            Assert.IsNotNull(sounds, "the hero has his sounds without any setup");
            float end = Time.time + 0.3f;
            while (Time.time < end) yield return null;
            input.moveX = 1f;
            while (player.transform.position.x < -0.6f) yield return null;
            input.jump = true;
            input.jumpHeld = true;
            bool latched = false;
            int creaks = 0, crossings = 0;
            float worst = 0f, lastAngle = 0f;
            end = Time.time + 4f;
            while (Time.time < end)
            {
                var m = controller.Motor;
                if (!latched && GrapplePoint.CanReach(controller.GripPosition, Ring, 7.5f) && controller.TryGrapple(Ring))
                {
                    latched = true;
                    input.moveX = 0f;
                }
                int before = Heard(Cue.RopeCreak) + Heard(Cue.RopeSwoosh);
                yield return null;
                if (!m.IsSwinging) { lastAngle = 0f; continue; }
                if (lastAngle != 0f && Mathf.Sign(m.SwingAngle) != Mathf.Sign(lastAngle)) crossings++;
                lastAngle = m.SwingAngle;
                if (Heard(Cue.RopeCreak) + Heard(Cue.RopeSwoosh) > before)
                {
                    creaks++;
                    worst = Mathf.Max(worst, Mathf.Abs(m.SwingAngle));
                }
            }
            Debug.Log($"[hero sounds] on the whip: passed under the ring {crossings} times, heard {sounds.BottomsHeard} times " +
                      $"({Heard(Cue.RopeCreak)} groans, {Heard(Cue.RopeSwoosh)} rushes), at most {worst * Mathf.Rad2Deg:F0} deg off the bottom");
            Assert.IsTrue(latched, "he caught the ring");
            Assert.AreEqual(1, Heard(Cue.RopeLatch), "the whip is heard catching, once");
            Assert.GreaterOrEqual(crossings, 2, "he swung through");
            Assert.That(sounds.BottomsHeard, Is.InRange(crossings - 1, crossings), "heard each time he passed under the ring");
            Assert.Less(worst, 0.35f, "and only there - under the ring, not at the ends of the swing");
        }
    }
}
