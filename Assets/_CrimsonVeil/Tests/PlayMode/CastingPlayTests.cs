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
    /// <summary>Weapons cast like spells: one press forms, strikes and melts back; combos, chains, held weapons,
    /// parry, the scarf's strength and guard, the pogo, the glide, the ledge catch, relics and the scarf's senses.</summary>
    public class CastingPlayTests
    {
        GameObject root, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        PlayerHealth health;

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
            root = new GameObject("CastingLevel");
            Box(new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 4f));

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
            foreach (var a in Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None)) Object.Destroy(a.gameObject);
            Object.Destroy(player);
            Object.Destroy(root);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        Enemy SpawnEnemy(Vector3 at)
        {
            var go = new GameObject("TestWraith");
            go.transform.SetParent(root.transform);
            go.transform.position = at;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f); cap.height = 2f; cap.radius = 0.4f;
            var e = go.AddComponent<Enemy>();
            e.Brain.Settings.windupTime = 10f; // passive during the test
            return e;
        }

        [UnityTest]
        public IEnumerator Cast_CostsScarfStrength_AndTheScarfGrowsBack()
        {
            yield return Wait(0.3f);
            input.cast = VeilForm.Spear;
            yield return Wait(0.1f);
            Assert.AreEqual(1f - VeilCasting.Cost(VeilForm.Spear), veil.Charge.Value, 0.03f, "the cast shortened the scarf");
            yield return Wait(2.5f);
            Assert.AreEqual(1f, veil.Charge.Value, 0.01f, "grown back");
        }

        [UnityTest]
        public IEnumerator LockedWeapon_OrShortScarf_Fizzles()
        {
            veil.SetUnlocked(VeilForm.Sword);
            int fizzles = 0;
            veil.CastFizzled += _ => fizzles++;
            yield return Wait(0.3f);
            input.cast = VeilForm.Spear;
            yield return Wait(0.2f);
            Assert.AreEqual(VeilForm.Flow, veil.Form, "not learnt yet");
            veil.Unlock(VeilForm.Bow);
            veil.Charge.Drain(100f, 1f);
            input.cast = VeilForm.Bow;
            yield return Wait(0.2f);
            Assert.AreEqual(VeilForm.Flow, veil.Form, "the scarf is too short");
            Assert.AreEqual(2, fizzles);
            input.cast = VeilForm.Sword;
            yield return Wait(0.15f);
            Assert.IsTrue(veil.IsAttacking, "the sword costs nothing — always at hand");
        }

        [UnityTest]
        public IEnumerator PressingDuringABlow_ContinuesTheCombo_WithoutMeltingBack()
        {
            int blows = 0;
            veil.Attacked += _ => blows++;
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.25f);
            input.cast = VeilForm.Sword;
            bool melted = false;
            float end = Time.time + 0.9f;
            while (Time.time < end)
            {
                if (veil.Form == VeilForm.Flow) melted = true;
                yield return null;
            }
            Assert.AreEqual(2, blows);
            Assert.IsFalse(melted, "the blade stays a blade between blows");
        }

        [UnityTest]
        public IEnumerator CastingAnotherWeaponMidBlow_PoursStraightIntoIt()
        {
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.2f);
            input.cast = VeilForm.Spear;
            bool melted = false, spear = false;
            float end = Time.time + 1f;
            while (Time.time < end)
            {
                if (veil.Form == VeilForm.Flow) melted = true;
                if (veil.Form == VeilForm.Spear && veil.IsAttacking) spear = true;
                yield return null;
            }
            Assert.IsTrue(spear, "the spear thrust follows the sword");
            Assert.IsFalse(melted, "the veil poured from blade to spear without becoming cloth");
        }

        [UnityTest]
        public IEnumerator Bow_HeldStaysDrawn_AndLoosesOnRelease()
        {
            yield return Wait(0.3f);
            input.held = VeilForm.Bow;
            input.cast = VeilForm.Bow;
            yield return Wait(0.8f);
            Assert.IsTrue(veil.Drawing, "held: the string stays at full draw");
            Assert.AreEqual(0, Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None).Length, "nothing loosed while drawn");
            input.aimY = 1f;
            yield return Wait(0.2f);
            input.held = null;
            yield return Wait(0.3f);
            var arrows = Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None);
            Assert.AreEqual(1, arrows.Length, "released: one arrow");
        }

        [UnityTest]
        public IEnumerator ShieldRaisedJustBeforeTheBlow_Parries_LaterItBlocks()
        {
            var enemy = SpawnEnemy(new Vector3(1.3f, 0f, 0f));
            yield return Wait(0.3f);
            input.held = VeilForm.Shield;
            input.cast = VeilForm.Shield;
            yield return null;
            yield return null;
            var blow = new DamageInfo { amount = 1f, knockback = new Vector2(-4f, 2f), source = enemy.gameObject, point = enemy.transform.position };
            health.ReceiveHit(blow);
            Assert.AreEqual(1, health.ParryCount, "raised a moment before: parried");
            Assert.AreEqual(health.Model.Max, health.Model.Current);
            yield return Wait(0.8f);
            health.ReceiveHit(blow);
            Assert.AreEqual(1, health.ParryCount);
            Assert.AreEqual(1, health.BlockCount, "held up: a plain block");
            Assert.AreEqual(health.Model.Max, health.Model.Current);
        }

        [UnityTest]
        public IEnumerator FullScarf_TakesAHitForTheHero_ThenIsTornShort()
        {
            yield return Wait(0.3f);
            var blow = new DamageInfo { amount = 1f, knockback = new Vector2(-4f, 2f), source = root, point = Vector3.zero };
            health.ReceiveHit(blow);
            Assert.AreEqual(1, health.AbsorbCount);
            Assert.AreEqual(health.Model.Max, health.Model.Current, "the scarf took it");
            Assert.Less(veil.Charge.Value, 0.05f, "torn short");
            yield return Wait(0.8f);
            health.ReceiveHit(blow);
            Assert.Less(health.Model.Current, health.Model.Max, "the next one gets through");
        }

        [UnityTest]
        public IEnumerator SpearThrustDownFromTheAir_BouncesOffTheEnemy()
        {
            yield return Wait(0.2f);
            controller.Teleport(new Vector3(0f, 3.3f, 0f)); // above where the enemy will stand
            var enemy = SpawnEnemy(new Vector3(0f, 0f, 0f));
            input.aimY = -1f;
            yield return null;
            veil.Charge.Restore();
            input.cast = VeilForm.Spear;
            float before = enemy.Health.Current, maxUp = 0f;
            float end = Time.time + 1.2f;
            while (Time.time < end)
            {
                maxUp = Mathf.Max(maxUp, controller.Velocity.y);
                yield return null;
            }
            Assert.Less(enemy.Health.Current, before, "the downward thrust struck");
            Assert.Greater(maxUp, controller.Motor.Settings.JumpVelocity * 0.8f, "and bounced the hero up");
        }

        [UnityTest]
        public IEnumerator ScarfCatchesALedgeTheHandsCannotReach()
        {
            Box(new Vector3(4f, 3f, 0f), new Vector3(4f, 6f, 4f)); // wall face at x 2, top at y 6
            yield return Wait(0.2f);
            controller.Teleport(new Vector3(1.55f, 0.05f, 0f));
            yield return Wait(0.2f);
            input.moveX = 1f;
            input.jump = true;
            input.jumpHeld = true;
            bool caught = false;
            float end = Time.time + 2f;
            while (Time.time < end)
            {
                if (controller.ScarfCatching) caught = true;
                if (controller.Motor.State == MotorState.LedgeHang) input.jump = true;
                if (controller.Motor.State == MotorState.Climbing) input.moveX = 0f;
                yield return null;
            }
            input.moveX = 0f;
            yield return Wait(0.3f);
            Assert.IsTrue(caught, "the scarf lashed round the lip");
            Assert.Greater(player.transform.position.y, 5.9f, "hauled up and climbed on top");
        }

        [UnityTest]
        public IEnumerator RelicTouched_TeachesTheWeapon_RelicsBehindAreFound()
        {
            veil.SetUnlocked(VeilForm.Sword);
            var behind = new GameObject("RelicBehind");
            behind.transform.position = new Vector3(-5f, 0f, 0f);
            behind.AddComponent<SphereCollider>();
            behind.AddComponent<VeilRelic>().form = VeilForm.Shield;
            var ahead = new GameObject("RelicAhead");
            ahead.transform.position = new Vector3(2.5f, 1f, 0f);
            ahead.AddComponent<SphereCollider>().radius = 0.9f;
            ahead.AddComponent<VeilRelic>().form = VeilForm.Spear;
            try
            {
                yield return Wait(0.3f);
                Assert.IsTrue(veil.IsUnlocked(VeilForm.Shield), "a relic left behind counts as found");
                Assert.IsFalse(veil.IsUnlocked(VeilForm.Spear));
                input.moveX = 1f;
                yield return Wait(0.6f);
                input.moveX = 0f;
                Assert.IsTrue(veil.IsUnlocked(VeilForm.Spear), "walking through the relic teaches the spear");
            }
            finally
            {
                Object.Destroy(behind);
                Object.Destroy(ahead);
            }
        }

        [UnityTest]
        public IEnumerator Hud_ShowsLifeInTheCorner_TheScarfNoLonger()
        {
            yield return Wait(0.3f);
            var hud = Object.FindFirstObjectByType<CrimsonVeil.UI.ScarfHud>();
            Assert.IsNotNull(hud, "the hero brings the HUD with him into any level");
            veil.Charge.Drain(100f, 1f); // no scarf guard: the blow lands
            health.ReceiveHit(new DamageInfo { amount = 2f, knockback = Vector2.zero, source = root, point = Vector3.zero });
            yield return Wait(0.2f);
            Assert.AreEqual(health.Model.Current, hud.Bar.health, 1e-3f, "the gauge shows the life left at once");
            Assert.AreEqual(health.Model.Max, hud.Bar.maxHealth, 1e-3f);
            Assert.Greater(hud.Bar.trailingHealth, hud.Bar.health, "what was just lost glows on as embers");
            Assert.AreEqual(veil.Charge.Value, hud.Bar.charge, 0.02f, "the thread shows the scarf's strength");
            yield return Wait(1f);
            Assert.AreEqual(1f, veil.Vitality, 1e-3f, "the scarf itself no longer thins with the wounds");
        }

        [UnityTest]
        public IEnumerator Scarf_LeansToSecrets_AndBristlesNearEnemies()
        {
            var secret = new GameObject("Secret");
            secret.transform.SetParent(root.transform);
            secret.transform.position = new Vector3(-4f, 2f, 0f);
            secret.AddComponent<VeilHint>();
            yield return Wait(0.4f);
            Assert.Greater(veil.HintPull, 0.3f);
            Assert.Less(veil.HintDirection.x, -0.8f, "it leans towards the secret");
            Assert.Less(veil.Danger, 0.05f);
            SpawnEnemy(new Vector3(2.5f, 0f, 0f));
            yield return Wait(0.7f);
            Assert.Greater(veil.Danger, 0.3f, "an enemy close by: the scarf bristles");
        }

        /// <summary>Stands in for the hero's animator: swings the forearm fast, posed in LateUpdate.</summary>
        [DefaultExecutionOrder(0)]
        sealed class SwingingArm : MonoBehaviour
        {
            void LateUpdate() => transform.localRotation = Quaternion.Euler(0f, 0f, -15f + 70f * Mathf.Sin(Time.time * 24f));
        }

        [UnityTest]
        public IEnumerator AWeaponStaysInTheHand_ThroughAFastSwing()
        {
            var forearm = new GameObject("LowerArm.R").transform;
            forearm.SetParent(player.transform, false);
            forearm.localPosition = new Vector3(0.15f, 1.3f, 0f);
            forearm.gameObject.AddComponent<SwingingArm>();
            var hand = new GameObject("Hand.R").transform;
            hand.SetParent(forearm, false);
            hand.localPosition = new Vector3(0.28f, 0f, 0f);
            veil.Hand = hand;
            yield return Wait(0.3f);

            veil.SetForm(VeilForm.Sword);
            yield return Wait(0.8f); // fully formed (while forming, the blade is still pouring out of the cloth)
            Assert.IsTrue(veil.Attack());
            float worst = 0f;
            int frames = 0;
            for (int k = 0; k < 40; k++)
            {
                yield return new WaitForEndOfFrame();
                if (!veil.IsAttacking) continue;
                var sim = veil.Simulation;
                int hi = Mathf.Clamp(Mathf.CeilToInt(veil.HandFraction * (sim.Count - 1)), 0, sim.Count - 1);
                Vector2 grip = sim.Targets[hi], h = hand.position;
                worst = Mathf.Max(worst, Vector2.Distance(grip, h));
                frames++;
            }
            Assert.Greater(frames, 10, "the sword formed and swung");
            Assert.Less(worst, 0.12f, "the blade grows out of the hand this frame, not where it was a moment ago");
        }
    }
}
