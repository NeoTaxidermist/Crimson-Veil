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

        static Transform Bone(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = Bone(c, n); if (r != null) return r; }
            return null;
        }

        /// <summary>
        /// A step is heard as the foot is seen to land: the ankle down where it stands, not on its way down (the step
        /// was heard 0.08-0.11 s early, from 7 cm up), and once a footfall (the sole's tremble doubled them).
        /// </summary>
        [UnityTest]
        public IEnumerator Walking_EachStepIsHeard_AsItsFootIsSeenToLand_OnceAFootfall()
        {
            var anim = player.GetComponentInChildren<HeroAnimator>();
            var foot = new[] { Bone(player.transform, "Foot.L"), Bone(player.transform, "Foot.R") };
            var toe = new[] { Bone(player.transform, "Toe.L"), Bone(player.transform, "Toe.R") };
            float Ankle(int k) => Mathf.Min(foot[k].position.y, toe[k].position.y);
            var heardAt = new System.Collections.Generic.List<(int foot, float ankle, float time)>();
            anim.FootSetDown += (k, s) => heardAt.Add((k, Ankle(k), Time.time));
            yield return Wait(0.5f);
            input.moveX = 0.45f;
            float[] lowest = { 9f, 9f };
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                yield return null;
                for (int k = 0; k < 2; k++) lowest[k] = Mathf.Min(lowest[k], Ankle(k));
            }
            Debug.Log($"[hero sounds] walking 3 s: {heardAt.Count} steps; ankle above where it stands when heard: " +
                      string.Join(", ", heardAt.ConvertAll(h => $"{h.foot}:{(h.ankle - lowest[h.foot]) * 100f:F1} cm")));
            Assert.That(heardAt.Count, Is.InRange(5, 11), "a step for each footfall of 3 s walking");
            foreach (var h in heardAt)
                Assert.Less(h.ankle - lowest[h.foot], 0.035f, "heard as the foot lands, not on its way down");
            for (int i = 1; i < heardAt.Count; i++)
                for (int j = 0; j < i; j++)
                    if (heardAt[j].foot == heardAt[i].foot)
                        Assert.Greater(heardAt[i].time - heardAt[j].time, 0.24f, "one step a footfall, never two");
        }

        /// <summary>The swing is heard as the blade is seen to cut (it lags the arm: cloth), not at the press.</summary>
        [UnityTest]
        public IEnumerator TheSwing_IsHeardAsTheBladeCuts_NotAtThePress()
        {
            var veil = player.GetComponent<CrimsonVeil.Veil.VeilController>();
            veil.Unlock(CrimsonVeil.Veil.VeilForm.Sword);
            yield return Wait(0.5f);
            input.cast = CrimsonVeil.Veil.VeilForm.Sword;
            float pressed = -1f, heard = -1f;
            float end = Time.time + 1f;
            while (Time.time < end && heard < 0f)
            {
                yield return null;
                if (pressed < 0f && veil.IsAttacking) pressed = Time.time - Time.deltaTime;
                if (Heard(Cue.SwingSword) > 0) heard = Time.time;
            }
            Debug.Log($"[hero sounds] the sword's swing heard {heard - pressed:F3} s after the blow began");
            Assert.Greater(pressed, 0f, "he struck");
            Assert.That(heard - pressed, Is.InRange(0.05f, 0.17f), "with the slash (the blade fastest ~0.2 s in), not at the press");
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
