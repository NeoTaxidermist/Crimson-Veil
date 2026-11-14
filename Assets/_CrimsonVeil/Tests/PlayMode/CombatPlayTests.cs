using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    public class CombatPlayTests
    {
        GameObject ground, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        PlayerHealth health;
        readonly System.Collections.Generic.List<GameObject> spawned = new System.Collections.Generic.List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 4f);

            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            input = player.AddComponent<ScriptedInput>();
            controller = player.AddComponent<PlayerController>();
            veil = player.AddComponent<VeilController>();
            player.AddComponent<VeilCombat>();
            health = player.AddComponent<PlayerHealth>();
            player.SetActive(true);

            // Tie the veil at the neck, like the real hero.
            var neck = new GameObject("VeilAnchor").transform;
            neck.SetParent(player.transform, false);
            neck.localPosition = new Vector3(0f, 1.5f, 0f);
            veil.Anchor = neck;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var go in spawned) if (go != null) Object.Destroy(go);
            spawned.Clear();
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        GameObject Track(GameObject go) { spawned.Add(go); return go; }

        Enemy SpawnEnemy(float x)
        {
            var go = Track(new GameObject("TestWraith"));
            go.transform.position = new Vector3(x, 0f, 0f);
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f); cap.height = 2f; cap.radius = 0.4f;
            return go.AddComponent<Enemy>();
        }

        /// <summary>Casts a weapon (one press: the veil forms it and strikes at once).</summary>
        IEnumerator CastAfterSettling(VeilForm form)
        {
            yield return Wait(0.3f);
            input.cast = form;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sword_BreaksUrn()
        {
            var urn = Track(new GameObject("Urn"));
            urn.transform.position = new Vector3(1.2f, 0f, 0f);
            var box = urn.AddComponent<BoxCollider>();
            box.isTrigger = true; box.center = new Vector3(0f, 0.4f, 0f); box.size = new Vector3(0.6f, 0.8f, 0.6f);
            var breakable = urn.AddComponent<Breakable>();

            yield return CastAfterSettling(VeilForm.Sword);
            yield return Wait(0.5f);
            Assert.IsTrue(breakable == null || breakable.IsBroken, "the sword swing should shatter the urn");
        }

        [UnityTest]
        public IEnumerator Spear_WoundsEnemyOncePerThrust()
        {
            var enemy = SpawnEnemy(2.2f);
            enemy.Brain.Settings.windupTime = 10f; // keep it from fighting back during the test
            yield return Wait(0.3f);
            float before = enemy.Health.Current;
            input.cast = VeilForm.Spear;
            yield return Wait(0.6f);
            Assert.AreEqual(before - VeilCombatProfile.Damage(VeilForm.Spear), enemy.Health.Current, 1e-3f, "exactly one hit per thrust");
            Assert.Greater(enemy.transform.position.x, 2.2f, "knocked back away from the hero");
        }

        [UnityTest]
        public IEnumerator Enemy_TelegraphsThenWoundsHero()
        {
            veil.Charge.Drain(100f, 1f); // a spent scarf cannot take the blow for the hero
            var enemy = SpawnEnemy(1.4f);
            yield return Wait(0.2f);
            Assert.AreEqual(EnemyState.Windup, enemy.Brain.State, "in range: it raises its arms first");
            Assert.AreEqual(health.Model.Max, health.Model.Current, "no damage during the telegraph");
            yield return Wait(enemy.Brain.Settings.windupTime + 0.3f);
            Assert.Less(health.Model.Current, health.Model.Max);
        }

        [UnityTest]
        public IEnumerator Shield_BlocksAndStaggers()
        {
            input.held = VeilForm.Shield; // hold the shield up
            yield return CastAfterSettling(VeilForm.Shield);
            yield return Wait(0.35f);
            Assert.AreEqual(VeilForm.Shield, veil.Form, "held, the shield stays");
            var enemy = SpawnEnemy(1.4f);
            yield return Wait(enemy.Brain.Settings.windupTime + 0.4f);
            Assert.AreEqual(health.Model.Max, health.Model.Current, "shield facing the wraith takes no damage");
            Assert.GreaterOrEqual(health.BlockCount, 1);
        }

        [UnityTest]
        public IEnumerator ArrowLoosedUpwards_CutsRopeAndDropsBridge()
        {
            var hingeGo = Track(new GameObject("Hinge"));
            hingeGo.transform.position = new Vector3(12f, 0f, 0f);
            var hinge = hingeGo.AddComponent<HingedBridge>();
            // High above sword reach; an arrow loosed at ~50° from the neck crosses it mid-span.
            var a = Track(new GameObject("RopeA")).transform; a.position = new Vector3(3f, 5f, 0f);
            var b = Track(new GameObject("RopeB")).transform; b.position = new Vector3(8f, 8f, 0f);
            var rope = Track(new GameObject("Rope")).AddComponent<CuttableRope>();
            rope.Configure(a, b, hinge, null);

            input.aimY = 1f;
            yield return CastAfterSettling(VeilForm.Bow);
            yield return Wait(1.2f);
            Assert.IsTrue(rope.IsCut, "an upward arrow should reach the high rope");
            Assert.IsTrue(hinge.Released);
            yield return Wait(1.2f);
            Assert.IsTrue(hinge.Landed);
        }

        [UnityTest]
        public IEnumerator HeroDies_AndRespawnsWithFullHealth()
        {
            controller.SetSpawnPoint(new Vector3(-5f, 0.05f, 0f));
            for (int i = 0; i < 10 && health.IsAlive; i++)
            {
                health.ReceiveHit(new DamageInfo { amount = 1f, knockback = Vector2.zero, source = ground });
                yield return Wait(1f);
            }
            Assert.IsFalse(health.IsAlive);
            yield return Wait(1.8f);
            Assert.IsTrue(health.IsAlive);
            Assert.AreEqual(health.Model.Max, health.Model.Current);
            Assert.AreEqual(-5f, player.transform.position.x, 0.5f);
        }
    }
}
