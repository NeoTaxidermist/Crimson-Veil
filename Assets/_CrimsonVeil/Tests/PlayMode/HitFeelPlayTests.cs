using System;
using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.UI;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// A hit is felt, on the real hero: a blow he lands stops the world long enough to see, the foe flashes, the cut
    /// is drawn and heard; a blow he takes stops it longer, flashes him crimson, runs the edges red and is heard.
    /// (Before: a 0.055 s stop - three frames - no flash, no sound; the blow passed through as through smoke.)
    /// </summary>
    [Category("Combat")]
    [Category("Veil")]
    public class HitFeelPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";
        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        PlayerHealth health;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("FeelRoom");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 4f);
            root.AddComponent<CombatFx>();   // (the scene's effects: the hit-stop, the cut)
            holder = new GameObject("Holder");
            holder.SetActive(false);
            player = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab"), holder.transform);
            Object.DestroyImmediate(player.GetComponent<PlayerInputReader>());
            input = player.AddComponent<ScriptedInput>();
            controller = player.GetComponent<PlayerController>();
            controller.InputSource = input;
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.SetParent(null, true);
            Object.Destroy(holder);
            player.SetActive(true);
            veil = player.GetComponent<VeilController>();
            health = player.GetComponent<PlayerHealth>();
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm))) if (f != VeilForm.Flow) veil.Unlock(f);
            GameSound.ResetCount();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(player);
            Object.Destroy(root);
            // (at once: a HUD only marked for destroying is still found by the next test's hero, and its red edges
            // are gone the moment it goes)
            foreach (var s in Object.FindObjectsByType<ScarfHud>(FindObjectsSortMode.None)) Object.DestroyImmediate(s.gameObject);
        }

        Enemy SpawnEnemy(Vector3 at)
        {
            var go = new GameObject("TestWraith");
            go.transform.SetParent(root.transform);
            go.transform.position = at;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f); cap.height = 2f; cap.radius = 0.4f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Quad);   // (something drawn, to flash)
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.up;
            var e = go.AddComponent<Enemy>();
            e.Brain.Settings.windupTime = 10f;   // passive during the test
            return e;
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        /// <summary>How long (real seconds) the world stood still from now on, within <paramref name="seconds"/>.</summary>
        IEnumerator MeasureStop(float seconds, Action<float> got)
        {
            float stopped = 0f;
            float end = Time.realtimeSinceStartup + seconds;
            float last = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                float now = Time.realtimeSinceStartup;
                if (Time.timeScale < 0.5f) stopped += now - last;
                last = now;
            }
            got(stopped);
        }

        [UnityTest]
        public IEnumerator ABlowHeLands_StopsTheWorldToBeSeen_TheFoeFlashes_TheCutIsDrawnAndHeard()
        {
            var foe = SpawnEnemy(new Vector3(1.3f, 0f, 0f));
            yield return Wait(0.4f);
            int before = GameSound.Played;
            input.cast = VeilForm.Sword;
            float flashed = 0f, stopped = 0f, longest = 0f;
            bool slash = false;
            float hitAt = -1f, swingAfterHit = -1f;
            float end = Time.realtimeSinceStartup + 1f;
            float last = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                float now = Time.realtimeSinceStartup;
                if (Time.timeScale < 0.5f) stopped += now - last;
                longest = Mathf.Max(longest, CombatFx.HitStopLeft);
                last = now;
                var f = foe != null ? foe.GetComponent<HitFlash>() : null;
                if (f != null) flashed = Mathf.Max(flashed, f.Now);
                slash |= GameObject.Find("Slash") != null;
                if (hitAt < 0f && GameSound.Count.ContainsKey(Cue.Hit)) hitAt = now;
                if (hitAt > 0f && swingAfterHit < 0f && now - hitAt > 0.1f) swingAfterHit = GameSound.Sounding(Cue.SwingSword);
            }
            Debug.Log($"[hit feel] the world stood still {stopped:F3} s (set for {longest:F3} s), the foe flashed up to {flashed:F2}, the cut drawn {slash}; sounds {GameSound.Played - before}, last {GameSound.Last}");
            Assert.Less(foe.Health.Current, foe.Health.Max, "the sword struck it");
            // (measured as it is set: a test's frames can be longer than the whole stop)
            Assert.Greater(stopped + longest, 0.07f, "the world stands still long enough to see the blow land (0.055 s it was)");
            Assert.Less(stopped, 0.3f, "and no longer than a moment");
            Assert.Greater(flashed, 0.5f, "the foe flashes");
            Assert.IsTrue(slash, "the cut is drawn through it");
            Assert.GreaterOrEqual(GameSound.Played - before, 2, "the swing and the hit are heard");
            Assert.That(GameSound.Count.TryGetValue(Cue.Hit, out int hits) ? hits : 0, Is.GreaterThanOrEqualTo(1), "the hit is heard (the last sound may be a step)");
            Assert.That(swingAfterHit, Is.InRange(0f, 0.001f), "the blade stops in it: its whoosh is gone within 0.1 s of the blow, not running on under it");
        }

        [UnityTest]
        public IEnumerator ABlowHeTakes_StopsItLonger_FlashesHimCrimson_RunsTheEdgesRed_AndIsHeard()
        {
            yield return Wait(0.4f);
            veil.Charge.Drain(1f, 1f);   // (a full scarf would take the blow for him)
            yield return null;
            float stopped = -1f;
            var info = new DamageInfo { amount = 0.5f, knockback = new Vector2(3f, 2f), point = player.transform.position + Vector3.up, kind = DamageKind.Slash };
            Assert.IsTrue(health.ReceiveHit(info), "struck");
            float set = CombatFx.HitStopLeft;
            Assert.AreEqual(Cue.Hurt, GameSound.Last, "he is heard to be hit");
            Assert.Greater(HurtVignette.Level, 0.9f, "the edges of the picture run red");
            var block = new MaterialPropertyBlock();
            float flash = 0f;
            foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.GetPropertyBlock(block);
                flash = Mathf.Max(flash, block.GetFloat("_HitFlash"));
            }
            Assert.Greater(flash, 0.9f, "his body flashes");
            yield return MeasureStop(0.5f, s => stopped = s);
            Debug.Log($"[hit feel] struck: the world stands still for {set:F3} s (measured {stopped:F3} s); the edges now {HurtVignette.Level:F2}");
            Assert.Greater(set, CombatFx.HitStopFor(VeilForm.Sword, false), "longer than for a blow he lands");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f, "and then the world runs on");
            yield return Wait(0.6f);
            Assert.Less(HurtVignette.Level, 0.05f, "and the red ebbs away");
        }
    }
}
