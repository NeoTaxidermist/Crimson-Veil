using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The fight made smoother and readable (v0.109, the user: "plynulejší, předvídatelnější, lepší reakce na zásah, na
    /// minutí, na vykrytí"): every press strikes after the same moment; a press held through a blow follows it; a blow
    /// pins the hero for its cut; a blow that lands throws the foe back and it reels; a miss is shown; a blow on a shield
    /// rings off it and throws the hero back; a strike turned by his shield staggers any foe; and every foe gives the
    /// same sign before it strikes (the root crawler too, which used to drop without one).
    /// </summary>
    [Category("Combat")]
    public class CombatFeelPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        VeilCombat combat;

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
            Time.timeScale = 1f;
            root = new GameObject("FeelLevel");
            Box(new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 4f));
            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            input = player.AddComponent<ScriptedInput>();
            controller = player.AddComponent<PlayerController>();
            veil = player.AddComponent<VeilController>();
            combat = player.AddComponent<VeilCombat>();
            player.AddComponent<PlayerHealth>();
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
            Object.Destroy(player);
            Object.Destroy(root);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator EveryPress_StrikesAfterTheSameMoment_WeaponInHandOrNot()
        {
            float pressed = -1f, struck = -1f;
            veil.Attacked += _ => struck = Time.time;
            yield return Wait(0.3f);
            // from the loose scarf: the sword has to form
            input.cast = VeilForm.Sword;
            yield return null;
            pressed = Time.time - Time.deltaTime;
            while (struck < 0f && Time.time < pressed + 1f) yield return null;
            float fromCloth = struck - pressed;
            // the sword still in hand (the blow and its recovery over, it lingers): pressed again
            while (veil.IsAttacking || veil.Recovering) yield return null;
            Assert.AreEqual(VeilForm.Sword, veil.Form, "the sword is still in hand");
            struck = -1f;
            input.cast = VeilForm.Sword;
            yield return null;
            pressed = Time.time - Time.deltaTime;
            while (struck < 0f && Time.time < pressed + 1f) yield return null;
            float inHand = struck - pressed;
            Debug.Log($"[startup] from the scarf {fromCloth * 1000f:F0} ms, in hand {inHand * 1000f:F0} ms");
            // (the press is read at the end of a frame: within a frame either way of the startup)
            Assert.That(fromCloth, Is.InRange(VeilController.CastStartup - 0.02f, VeilController.CastStartup + 0.06f));
            Assert.That(inHand, Is.InRange(VeilController.CastStartup - 0.02f, VeilController.CastStartup + 0.06f));
            Assert.Less(Mathf.Abs(fromCloth - inHand), 0.04f, "the same moment either way");
        }

        [UnityTest]
        public IEnumerator APressHeldThroughTheBlow_FollowsIt()
        {
            int blows = 0;
            veil.Attacked += _ => blows++;
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.1f);               // early in the blow
            input.cast = VeilForm.Sword;
            input.held = VeilForm.Sword;           // and held
            yield return Wait(1.0f);
            input.held = null;
            Assert.AreEqual(2, blows, "held into the blow's last moments, the press follows it");
        }

        [UnityTest]
        public IEnumerator ABlow_PinsTheHero_ForItsCut()
        {
            yield return Wait(0.3f);
            input.moveX = 1f;
            yield return Wait(0.4f);
            input.moveX = 0f;
            yield return Wait(0.3f);
            Assert.AreEqual(1, controller.Motor.Facing);
            input.cast = VeilForm.Sword;
            yield return null;
            input.moveX = -1f;                     // pressed the other way at once
            float walkedBack = 0f;
            bool wasPinned = false;
            float until = Time.time + 2f;
            while ((veil.PinnedNow || !wasPinned) && Time.time < until)
            {
                if (veil.PinnedNow) wasPinned = true;
                Assert.AreEqual(1, controller.Motor.Facing, "pinned: he keeps the way he struck");
                walkedBack = Mathf.Min(walkedBack, controller.Velocity.x);
                yield return null;
            }
            Assert.IsTrue(wasPinned);
            Assert.Greater(walkedBack, -0.5f, "and does not walk out of the blow");
            yield return Wait(0.3f);
            Assert.AreEqual(-1, controller.Motor.Facing, "the cut done, he turns to the keys");
        }

        AshMoth SpawnMoth(Vector3 at)
        {
            var go = new GameObject("Moth");
            go.transform.SetParent(root.transform);
            go.transform.position = at;
            go.AddComponent<SphereCollider>().radius = 0.5f;
            AshMoth.BuildVisual(go.transform, null, null);
            return go.AddComponent<AshMoth>();
        }

        [UnityTest]
        public IEnumerator ABlowThatLands_ThrowsTheFoeBack_AndItReels()
        {
            var moth = SpawnMoth(new Vector3(6f, 2f, 0f));
            yield return Wait(0.2f);
            Assert.IsTrue(moth.ReceiveHit(new DamageInfo { amount = 0.1f, knockback = new Vector2(5f, 2.5f), point = moth.transform.position, source = player }));
            Assert.IsTrue(moth.Reeling, "it reels");
            float most = 0f;
            float end = Time.time + 0.15f;
            while (Time.time < end) { most = Mathf.Max(most, moth.Recoil.x); yield return null; }
            Assert.Greater(most, 0.05f, "thrown back along the blow");
            yield return Wait(0.6f);
            Assert.IsFalse(moth.Reeling, "and soon over it");
            Assert.Less(moth.Recoil.magnitude, 0.02f, "sprung back to its place");
        }

        [UnityTest]
        public IEnumerator AMiss_IsShown_AHitIsNot()
        {
            yield return Wait(0.3f);
            int before = combat.Missed;
            input.cast = VeilForm.Sword;
            yield return Wait(0.8f);
            Assert.AreEqual(before + 1, combat.Missed, "a cut through air is shown as a miss");
            // a wraith in reach: the blow lands, no miss
            var go = new GameObject("TestWraith");
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(1.1f, 0f, 0f);
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f); cap.height = 2f; cap.radius = 0.4f;
            var e = go.AddComponent<Enemy>();
            e.Brain.Settings.windupTime = 10f;
            yield return Wait(0.6f);
            int landed = 0;
            combat.Hit += (t, i) => landed++;
            input.cast = VeilForm.Sword;
            yield return Wait(0.8f);
            Assert.AreEqual(1, landed, "the blow landed");
            Assert.AreEqual(before + 1, combat.Missed, "and is not shown as a miss");
        }

        ShieldMonk SpawnMonk(Vector3 at)
        {
            var go = new GameObject("Monk");
            go.transform.SetParent(root.transform);
            go.transform.position = at;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = Vector3.up; cap.height = 2f;
            ShieldMonk.BuildVisual(go.transform, null, null);
            return go.AddComponent<ShieldMonk>();
        }

        [UnityTest]
        public IEnumerator ABlowOnAShield_RingsOffIt_AndThrowsTheHeroBack()
        {
            var monk = SpawnMonk(new Vector3(-3f, 0f, 0f));   // facing right: the hero in front of his shield
            input.moveX = -1f;
            yield return Wait(0.1f);
            input.moveX = 0f;
            yield return Wait(0.2f);
            Assert.AreEqual(-1, veil.Facing, "the hero faces the monk");
            input.cast = VeilForm.Sword;
            while (!veil.IsAttacking) yield return null;
            int blocks = GameSound.Count.TryGetValue(Cue.Block, out var c) ? c : 0;
            Assert.IsFalse(monk.ReceiveHit(new DamageInfo { amount = 1f, knockback = new Vector2(-5f, 2f), point = monk.transform.position + Vector3.up, kind = DamageKind.Slash, source = player }),
                           "the blade is stopped by the shield");
            Assert.AreEqual(1, combat.Deflected, "the hero learns his blow was stopped");
            Assert.IsFalse(veil.IsAttacking, "the blow is broken off: it does not go on through the shield");
            Assert.Greater(controller.Motor.Velocity.x, 2f, "he is thrown back from it");
            Assert.AreEqual(blocks + 1, GameSound.Count[Cue.Block], "it rings off the shield");
        }

        [UnityTest]
        public IEnumerator EveryFoeGivesTheSameSign_AndAStrikeTurnedByTheShieldStaggersIt()
        {
            int signs = CombatFx.Telegraphs;
            var monk = SpawnMonk(new Vector3(-1.4f, 0f, 0f));   // the hero within his bash
            float end = Time.time + 3f;
            while (monk.Current != ShieldMonk.State.Windup && Time.time < end) yield return null;
            Assert.AreEqual(ShieldMonk.State.Windup, monk.Current, "he winds up a bash");
            Assert.AreEqual(signs + 1, CombatFx.Telegraphs, "with the sign every foe gives");
            monk.Stagger();                                   // the hero's shield turned it
            Assert.IsTrue(monk.Reeling, "staggered");
            Assert.AreEqual(ShieldMonk.State.Recover, monk.Current, "the bash he was winding up is lost");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheRootCrawler_WarnsBeforeItDrops()
        {
            Box(new Vector3(4f, 5.5f, 0f), new Vector3(6f, 1f, 4f));   // the ceiling it clings to
            yield return null;
            Physics.SyncTransforms();
            var go = new GameObject("Crawler");
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(4f, 4.5f, 0f);
            go.AddComponent<SphereCollider>().radius = 0.3f;
            RootCrawler.BuildVisual(go.transform, null, null);
            var crawler = go.AddComponent<RootCrawler>();
            yield return Wait(0.2f);
            Assert.AreEqual(RootCrawler.State.Cling, crawler.Current);
            int signs = CombatFx.Telegraphs;
            controller.Teleport(new Vector3(4f, 0.05f, 0f));   // right under it
            float end = Time.time + 1f;
            while (!crawler.AboutToDrop && Time.time < end) yield return null;
            Assert.IsTrue(crawler.AboutToDrop, "it quivers on its root");
            Assert.AreEqual(signs + 1, CombatFx.Telegraphs, "with the sign every foe gives");
            float warned = Time.time;
            while (crawler.Current == RootCrawler.State.Cling && Time.time < warned + 2f) yield return null;
            Assert.AreEqual(RootCrawler.State.Drop, crawler.Current, "then it drops");
            Assert.GreaterOrEqual(Time.time - warned, RootCrawler.DropWarning - 0.05f, "not before its warning has played out");
        }
    }
}
