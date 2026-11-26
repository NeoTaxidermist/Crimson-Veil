using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The real hero is heard moving: his feet as they are set down while he runs, the jump and the landing, the dash,
    /// the slide's scrape for as long as he slides (then it fades away).
    /// </summary>
    [Category("Hero")]
    [Category("Motor")]
    public class HeroSoundsPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";
        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("SoundRoom");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 4f);
            holder = new GameObject("Holder");
            holder.SetActive(false);
            player = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab"), holder.transform);
            Object.DestroyImmediate(player.GetComponent<PlayerInputReader>());
            input = player.AddComponent<ScriptedInput>();
            controller = player.GetComponent<PlayerController>();
            controller.InputSource = input;
            player.transform.position = new Vector3(-30f, 0.05f, 0f);
            player.transform.SetParent(null, true);
            Object.Destroy(holder);
            player.SetActive(true);
            GameSound.ResetCount();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(root);
            foreach (var s in Object.FindObjectsByType<UI.ScarfHud>(FindObjectsSortMode.None)) Object.DestroyImmediate(s.gameObject);
        }

        static int Heard(Cue cue) => GameSound.Count.TryGetValue(cue, out int k) ? k : 0;

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Running_HisFeetAreHeard_AsTheyAreSetDown_AndAJumpAndItsLanding()
        {
            Assert.IsNotNull(player.GetComponent<HeroSounds>(), "the hero has his sounds without any setup");
            yield return Wait(0.5f);
            int still = Heard(Cue.StepWalk) + Heard(Cue.StepRun);
            input.moveX = 1f;
            yield return Wait(2f);
            int steps = Heard(Cue.StepWalk) + Heard(Cue.StepRun) - still;
            Debug.Log($"[hero sounds] 2 s of running: {steps} steps heard ({Heard(Cue.StepRun)} running), speed {controller.Motor.Velocity.x:F1} m/s");
            Assert.AreEqual(0, still, "standing still, no steps");
            if (GameSettings.EffectsLevel > 0f) Assert.Greater(GameSound.LoopLevel(Loop.Ambience), 0f, "the ruin's air is heard under him");
            Assert.That(steps, Is.InRange(3, 12), "a step for each foot set down - not one a frame, not none");
            Assert.Greater(Heard(Cue.StepRun), 0, "at a run they are running steps");
            input.jump = true;
            input.jumpHeld = true;
            yield return null;
            yield return null;
            Assert.AreEqual(1, Heard(Cue.Jump), "the push of the jump");
            input.jumpHeld = false;
            float end = Time.time + 2f;
            while (Time.time < end && Heard(Cue.Land) + Heard(Cue.LandHard) == 0) yield return null;
            Assert.AreEqual(1, Heard(Cue.Land) + Heard(Cue.LandHard), "and its landing, once");
            Assert.AreEqual(0, Heard(Cue.LandHard), "a hop is no hard landing");
        }

        [UnityTest]
        public IEnumerator TheSlide_ScrapesForAsLongAsItLasts_TheDashRushes()
        {
            yield return Wait(0.5f);
            input.moveX = 1f;
            yield return Wait(0.6f);
            input.aimY = -1f;
            input.dash = true;
            float loudest = 0f;
            bool slid = false;
            float end = Time.time + 0.5f;
            while (Time.time < end)
            {
                yield return null;
                slid |= controller.Motor.IsSliding;
                if (controller.Motor.IsSliding) loudest = Mathf.Max(loudest, GameSound.LoopLevel(Loop.Slide));
            }
            Assert.IsTrue(slid, "he slid");
            if (GameSettings.EffectsLevel > 0f) Assert.Greater(loudest, 0.05f, "the slide scrapes");
            input.aimY = 0f;
            input.moveX = 0f;
            yield return Wait(1.2f);
            Assert.IsFalse(controller.Motor.IsSliding);
            Assert.Less(GameSound.LoopLevel(Loop.Slide), 0.01f, "and the scrape is gone when the slide is");
            int dashes = Heard(Cue.Dash);
            input.moveX = 1f;
            input.dash = true;
            yield return Wait(0.2f);
            Assert.AreEqual(dashes + 1, Heard(Cue.Dash), "the dash rushes");
            float until = Time.realtimeSinceStartup + 5f;
            while (!GameSound.Warm && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(GameSound.Warm, "every sound is made ahead within moments (not when first heard, with a hitch)");
        }
    }
}
