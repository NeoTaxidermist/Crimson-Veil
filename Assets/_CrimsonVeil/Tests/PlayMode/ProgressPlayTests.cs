using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.UI;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// Life, healing and what the foes shed (v0.110, the user: "ten autoheal je divný... z nepřátel by mělo něco po
    /// zabití vypadnout"): life no longer grows back; a wound is bound with the knot his blows tie (R held); a slain
    /// foe sheds crimson he gathers; dying leaves it where he fell; a checkpoint makes him whole and its altar sells
    /// upgrades for crimson; four shards make one more life.
    /// </summary>
    [Category("Combat")]
    public class ProgressPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        PlayerHealth health;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            HeroProgress.Reset();
            InputBindings.BuildDefaultsForTests();
            root = new GameObject("ProgressLevel");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 4f);
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
            var neck = new GameObject("VeilAnchor").transform;
            neck.SetParent(player.transform, false);
            neck.localPosition = new Vector3(0f, 1.5f, 0f);
            veil.Anchor = neck;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (AltarPanel.IsOpen) AltarPanel.Instance.Close();
            CrimsonVeil.Core.GameClock.Reset();
            InputBindings.GameplayBlocked = false;
            InputBindings.EndTests();
            foreach (var d in Object.FindObjectsByType<CrimsonDrop>(FindObjectsSortMode.None)) Object.Destroy(d.gameObject);
            if (CrimsonCache.Current != null) Object.Destroy(CrimsonCache.Current.gameObject);
            Object.Destroy(player);
            Object.Destroy(root);
            HeroProgress.Reset();
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        DamageInfo Blow(float amount) => new DamageInfo { amount = amount, knockback = Vector2.zero, point = player.transform.position + Vector3.up };

        [UnityTest]
        public IEnumerator LifeDoesNotGrowBackByItself()
        {
            yield return Wait(0.3f);
            veil.Charge.TryCast(0.5f);   // (the scarf short: it does not take the blow for him)
            Assert.IsTrue(health.ReceiveHit(Blow(1f)));
            float wounded = health.Model.Current;
            yield return Wait(7.5f);
            Assert.AreEqual(wounded, health.Model.Current, 1e-4f, "after 7.5 s with nothing hitting him, life is as it was (it grew back after 6 s)");
        }

        [UnityTest]
        public IEnumerator AWound_IsBoundWithTheKnotHisBlowsTie()
        {
            yield return Wait(0.3f);
            veil.Charge.TryCast(0.5f);
            Assert.IsTrue(health.ReceiveHit(Blow(1f)));
            float wounded = health.Model.Current;
            yield return Wait(1f);   // (past the moment of grace after the blow)
            input.heal = true;
            yield return Wait(PlayerHealth.MendTime + 0.3f);
            Assert.AreEqual(wounded, health.Model.Current, 1e-4f, "the knot empty: nothing to bind it with");
            input.heal = false;
            for (int i = 0; i < 4; i++) HeroProgress.BlowLanded();
            input.heal = true;
            yield return Wait(PlayerHealth.MendTime * 0.5f);
            Assert.Greater(health.Mending, 0.2f, "binding it");
            // held until it is bound (in the whole suite a slow frame or a stray contact can restart it once)
            string seen = "";
            float until = Time.time + 2.5f;
            while (health.MendCount == 0 && Time.time < until)
            {
                seen = $"mending {health.Mending:F2}, knot {HeroProgress.Mend:F2}, grounded {controller.IsGrounded}, attacking {veil.IsAttacking}, life {health.Model.Current}/{health.Model.Max}, timeScale {Time.timeScale}";
                yield return null;
            }
            input.heal = false;
            Assert.AreEqual(wounded + HeroProgress.HealAmount, health.Model.Current, 1e-3f, "bound: a life back (last seen: " + seen + ")");
            Assert.AreEqual(0f, HeroProgress.Mend, "the knot untied");
            Assert.AreEqual(1, health.MendCount);
        }

        [UnityTest]
        public IEnumerator ASlainFoe_ShedsCrimson_HeGathers()
        {
            var go = new GameObject("TestWraith");
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(1.2f, 0f, 0f);
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f); cap.height = 2f; cap.radius = 0.4f;
            var e = go.AddComponent<Enemy>();
            e.Brain.Settings.windupTime = 10f;
            yield return Wait(0.3f);
            Assert.IsTrue(e.ReceiveHit(new DamageInfo { amount = 999f, knockback = Vector2.zero, point = go.transform.position + Vector3.up, source = player }));
            Assert.Greater(Object.FindObjectsByType<CrimsonDrop>(FindObjectsSortMode.None).Length, 0, "it sheds drops");
            yield return Wait(2f);
            Assert.AreEqual(Enemy.CrimsonWorth, HeroProgress.Crimson, "drawn to him and gathered");
        }

        [UnityTest]
        public IEnumerator Dying_LeavesTheCrimsonWhereHeFell_ToBeFetchedBack()
        {
            yield return Wait(0.3f);
            HeroProgress.AddCrimson(12);
            controller.Teleport(new Vector3(6f, 0.05f, 0f));
            yield return Wait(0.2f);
            veil.Charge.TryCast(0.5f);
            health.ReceiveHit(Blow(99f));
            Assert.IsFalse(health.IsAlive);
            Assert.AreEqual(0, HeroProgress.Crimson, "he no longer carries it");
            Assert.IsNotNull(CrimsonCache.Current, "it lies where he fell");
            Assert.AreEqual(12, CrimsonCache.Current.Worth);
            yield return Wait(2f);
            Assert.IsTrue(health.IsAlive, "back at the spawn");
            controller.Teleport(new Vector3(6f, 0.05f, 0f));
            yield return Wait(0.3f);
            Assert.AreEqual(12, HeroProgress.Crimson, "fetched back");
            Assert.IsNull(CrimsonCache.Current);
        }

        [UnityTest]
        public IEnumerator ACheckpoint_MakesHimWhole_AndItsAltarSellsUpgrades()
        {
            var cp = new GameObject("Checkpoint");
            cp.transform.SetParent(root.transform);
            cp.transform.position = new Vector3(8f, 0f, 0f);
            cp.AddComponent<Checkpoint>();
            yield return Wait(0.3f);
            veil.Charge.TryCast(0.5f);
            health.ReceiveHit(Blow(2f));
            Assert.Less(health.Model.Current, health.Model.Max);
            controller.Teleport(new Vector3(8f, 0.05f, 0f));
            yield return Wait(0.2f);
            Assert.AreEqual(health.Model.Max, health.Model.Current, 1e-4f, "whole again");
            Assert.AreEqual(1f, veil.Charge.Value, 0.01f, "the scarf full");
            Assert.AreSame(cp.GetComponent<Checkpoint>(), Checkpoint.Nearby);
            HeroProgress.AddCrimson(50);
            input.interact = true;
            yield return null; yield return null;
            Assert.IsTrue(AltarPanel.IsOpen, "F opens the altar");
            Assert.AreEqual(0f, Time.timeScale, "the game stands still");
            AltarPanel.Instance.Buy(HeroProgress.Upgrade.Blade);
            Assert.AreEqual(1, HeroProgress.Level(HeroProgress.Upgrade.Blade));
            Assert.AreEqual(10, HeroProgress.Crimson);
            AltarPanel.Instance.Close();
            Assert.IsFalse(AltarPanel.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator FourShards_GiveOneMoreLife()
        {
            yield return Wait(0.3f);
            float before = health.Model.Max;
            veil.Charge.TryCast(0.5f);
            health.ReceiveHit(Blow(1f));
            for (int i = 0; i < 4; i++)
            {
                var s = new GameObject("HeartShard");
                s.transform.SetParent(root.transform);
                s.transform.position = player.transform.position + new Vector3(0.2f * i, 0f, 0f);
                s.AddComponent<HeartShard>();
            }
            yield return Wait(0.5f);
            Assert.AreEqual(4, HeroProgress.Shards, "all four taken");
            Assert.AreEqual(before + 1f, health.Model.Max, 1e-4f, "one more life");
            Assert.AreEqual(health.Model.Max, health.Model.Current, 1e-4f, "and all of them full");
        }
    }
}
