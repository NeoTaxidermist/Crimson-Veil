using System.Collections;
using System.Collections.Generic;
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
    /// parry, the scarf's strength and guard, the pogo, the glide, relics and the scarf's senses.</summary>
    [Category("Veil")]
    [Category("Combat")]
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
            yield return Wait(0.4f); // late in the blow: the follow-up
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
        public IEnumerator SwordSwordWhip_IsOneCombo_EndingInTheWhipsFinisher()
        {
            var blows = new System.Collections.Generic.List<VeilCombo.Blow>();
            veil.ComboBlow += b => blows.Add(b);
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;          // late in the blow: the follow-up
            float end = Time.time + 1.2f;
            while (blows.Count < 2 && Time.time < end) yield return null;
            yield return Wait(0.3f);
            input.cast = VeilForm.Whip;           // pours into the whip mid-chain
            end = Time.time + 1.2f;
            while (blows.Count < 3 && Time.time < end) yield return null;
            Assert.AreEqual(3, blows.Count);
            Assert.AreEqual(new[] { 0, 1, 2 }, blows.ConvertAll(b => b.step).ToArray());
            Assert.IsTrue(blows[2].finisher);
            Assert.AreEqual(VeilForm.Whip, blows[2].form);
            Assert.IsTrue(blows[2].morph, "the whip's first blow is the morph strike");
            Assert.AreEqual(2, blows[2].variety);
            while (veil.IsAttacking) yield return null;
            yield return Wait(VeilCombo.ResetAfter + 0.5f);
            input.cast = VeilForm.Sword;
            end = Time.time + 1f;
            while (blows.Count < 4 && Time.time < end) yield return null;
            Assert.AreEqual(0, blows[3].step, "after a pause: the first blow again");
        }

        [UnityTest]
        public IEnumerator AnEnemysBlow_BreaksOffTheComboMidChain()
        {
            var blows = new System.Collections.Generic.List<VeilCombo.Blow>();
            veil.ComboBlow += b => blows.Add(b);
            int broken = 0;
            veil.Interrupted += () => broken++;
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;          // the second blow, queued
            float end = Time.time + 1.2f;
            while (blows.Count < 2 && Time.time < end) yield return null;
            while (veil.IsAttacking && veil.AttackProgress < 0.5f) yield return null;
            Assert.IsTrue(veil.IsAttacking);
            input.cast = VeilForm.Sword;          // a third, late in the blow: waiting to follow
            yield return null;
            var hit = new DamageInfo { amount = 1f, knockback = new Vector2(-3f, 2f), point = player.transform.position + Vector3.up };
            Assert.IsTrue(health.ReceiveHit(hit), "struck mid-combo");
            Assert.AreEqual(1, broken);
            Assert.IsFalse(veil.IsAttacking, "the blow is broken off");
            Assert.AreEqual(0, veil.Combo.Step);
            yield return Wait(0.4f);   // (shorter than the pause that would start the chain again by itself)
            Assert.AreEqual(2, blows.Count, "the press waiting to follow is lost");
            input.cast = VeilForm.Sword;
            end = Time.time + 1f;
            while (blows.Count < 3 && Time.time < end) yield return null;
            Assert.AreEqual(0, blows[2].step, "the combo starts again from the first blow");
        }

        [UnityTest]
        public IEnumerator Dead_TheVeilCastsNothing_UntilHeIsBack()
        {
            yield return Wait(0.3f);
            for (int i = 0; i < 10 && health.IsAlive; i++)
            {
                health.ReceiveHit(new DamageInfo { amount = 1f, knockback = Vector2.zero, point = player.transform.position });
                if (health.IsAlive) yield return Wait(1f);
            }
            Assert.IsFalse(health.IsAlive);
            yield return null;
            Assert.IsTrue(veil.Disarmed);
            int blows = 0;
            veil.Attacked += _ => blows++;
            input.cast = VeilForm.Sword;
            yield return Wait(0.4f);
            Assert.AreEqual(0, blows, "no blade floats where he fell");
            Assert.AreEqual(VeilForm.Flow, veil.Form);
            yield return Wait(1.6f);
            Assert.IsTrue(health.IsAlive);
            Assert.IsFalse(veil.Disarmed);
            input.cast = VeilForm.Sword;
            yield return Wait(0.3f);
            Assert.AreEqual(1, blows, "back on his feet he fights again");
            Assert.AreEqual(0, veil.Combo.Step, "and his combo starts from the first blow");
        }

        [UnityTest]
        public IEnumerator SwordAimedUp_CutsAboveTheHead()
        {
            yield return Wait(0.3f);
            input.aimY = 1f;
            input.cast = VeilForm.Sword;
            yield return Wait(0.12f);
            Assert.IsTrue(veil.IsAttacking);
            Assert.Greater(veil.AttackAim, 80f, "up: straight up");
            var sim = veil.Simulation;
            float highest = float.MinValue;
            while (veil.IsAttacking)
            {
                highest = Mathf.Max(highest, sim.Positions[sim.Count - 1].y);
                yield return null;
            }
            Assert.Greater(highest, veil.Anchor.position.y + 0.6f, "the blade cuts above the head");
        }

        [UnityTest]
        public IEnumerator AimAssist_TurnsTheBlowOntoAnEnemyNearTheLine()
        {
            SpawnEnemy(new Vector3(2f, 0f, 0f)); // its body a little below the line from the neck
            yield return Wait(0.3f);
            input.cast = VeilForm.Spear;
            yield return Wait(0.2f);
            Assert.IsTrue(veil.IsAttacking);
            Assert.Less(veil.AttackAim, -5f, "the thrust is turned down onto the enemy");
            Assert.Greater(veil.AttackAim, -VeilAim.AssistCone - 1f);
        }

        [UnityTest]
        public IEnumerator SwordAimedDownInTheAir_IsAStrikeUnderTheFeet()
        {
            yield return Wait(0.3f);
            input.jump = true; input.jumpHeld = true;
            yield return Wait(0.15f);
            input.aimY = -1f;
            input.cast = VeilForm.Sword;
            yield return Wait(0.15f);
            Assert.IsTrue(veil.IsAttacking);
            Assert.Less(veil.AttackAim, -80f);
            Assert.IsTrue(veil.DownStrike, "it bounces the hero off what it hits");
        }

        [UnityTest]
        public IEnumerator MashingInABlow_GivesOneFollowUp_NoSoonerAndNoMore()
        {
            // (presses early in a blow used to be dropped altogether: a press not heard, with nothing to tell why)
            int blows = 0;
            float second = -1f, started = -1f;
            veil.Attacked += _ => { blows++; if (blows == 1) started = Time.time; if (blows == 2) second = Time.time; };
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.12f); // the blow has only just begun
            for (int k = 0; k < 4; k++) { input.cast = VeilForm.Sword; yield return null; yield return null; }
            yield return Wait(1.6f);
            Assert.AreEqual(2, blows, "mashing in a blow: one follows it - not none, not four");
            float whole = VeilFormLibrary.AttackDuration(VeilForm.Sword) + VeilCasting.Recovery(VeilForm.Sword);
            Assert.Greater(second - started, whole * 0.6f, "and no sooner than the blow and its recovery allow: mashing cannot hurry it");
        }

        [UnityTest]
        public IEnumerator EveryPressCounts_ABowPressedDuringAnotherBlow_LoosesItsArrowAfterIt()
        {
            // "The bow sometimes does not shoot": pressed in the first 40 % of another blow, the press was dropped - a bow
            // 0.12 s after a sword cut or 0.2 s after a spear thrust never came, and of six bow presses 0.9 s apart one
            // was lost. And a scarf too short for the bow's whole cost refused it without a sign.
            var seen = new HashSet<int>();
            System.Func<int> arrows = () =>
            {
                foreach (var a in Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None)) seen.Add(a.GetInstanceID());
                return seen.Count;
            };
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.12f);
            Assert.IsTrue(veil.IsAttacking && veil.AttackProgress < 0.4f, "early in the sword's cut");
            input.cast = VeilForm.Bow;
            { float until1 = Time.time + 1.6f; while (Time.time < until1) { arrows(); yield return null; } }
            Assert.AreEqual(1, arrows(), "the bow pressed early in a sword cut follows it and looses");
            yield return Wait(2.5f);   // (the scarf grows back)

            input.cast = VeilForm.Spear;
            yield return Wait(0.2f);
            input.cast = VeilForm.Bow;
            { float until2 = Time.time + 1.8f; while (Time.time < until2) { arrows(); yield return null; } }
            Assert.AreEqual(2, arrows(), "and after a spear thrust");
            yield return Wait(3f);
            Assert.AreEqual(1f, veil.Charge.Value, 0.02f, "(the scarf is whole again)");

            // six presses 0.9 s apart - quicker than the bow can follow: every one of them looses an arrow
            int before = arrows();
            int fizzles = 0;
            veil.CastFizzled += _ => fizzles++;
            for (int k = 0; k < 6; k++)
            {
                input.cast = VeilForm.Bow;
                { float until3 = Time.time + 0.9f; while (Time.time < until3) { arrows(); yield return null; } }
            }
            { float until4 = Time.time + 1.6f; while (Time.time < until4) { arrows(); yield return null; } }
            Assert.AreEqual(6, arrows() - before, "six presses, six arrows (the scarf had a stub left for the last: it took all of it)");
            Assert.AreEqual(0, fizzles);
            Assert.Less(veil.Charge.Value, 0.6f, "the scarf is spent on them");
            // ...and with nothing left it says so
            veil.Charge.Drain(100f, 1f);
            input.cast = VeilForm.Bow;
            { float until5 = Time.time + 0.4f; while (Time.time < until5) { arrows(); yield return null; } }
            Assert.AreEqual(1, fizzles, "no scarf: the cast fizzles - told, and shown at the hand");
            Assert.AreEqual(6, arrows() - before, "and nothing is loosed");
        }

        [UnityTest]
        public IEnumerator ABlow_PlaysToItsEnd_AndTheBodyRecovers()
        {
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.15f);
            Assert.IsTrue(veil.IsAttacking);
            float started = veil.AttackProgress;
            input.cast = VeilForm.Spear; // another weapon mid-blow: it doesn't cut the blow short
            Assert.IsFalse(veil.Attack(), "no second blow over the first");
            yield return Wait(0.1f);
            Assert.AreEqual(VeilForm.Sword, veil.Form);
            Assert.Greater(veil.AttackProgress, started, "the same blow carries on");
            while (veil.IsAttacking) yield return null;
            Assert.IsTrue(veil.Recovering, "a moment of recovery after the blow");
            Assert.IsFalse(veil.Attack(), "nothing begins while recovering");
            yield return Wait(VeilCasting.Recovery(VeilForm.Sword) + 0.05f);
            Assert.IsFalse(veil.Recovering);
        }

        [UnityTest]
        public IEnumerator CastingAnotherWeaponMidBlow_PoursStraightIntoIt()
        {
            yield return Wait(0.3f);
            input.cast = VeilForm.Sword;
            yield return Wait(0.4f);
            input.cast = VeilForm.Spear;
            bool melted = false, spear = false;
            float end = Time.time + 1.5f;
            while (Time.time < end)
            {
                if (veil.Form == VeilForm.Flow) melted = true;
                if (veil.Form == VeilForm.Spear && veil.IsAttacking) spear = true;
                if (spear && !veil.IsAttacking) break; // (after the thrust it melts back, as it should)
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
        public IEnumerator TheShieldHeldInTheAir_Guards_ButDoesNotSlowTheFall_OnlyTheScarfGlides()
        {
            // a fall from 8 m: with the shield held up, and with nothing in hand
            veil.SetUnlocked(VeilForm.Shield);
            yield return Wait(0.3f);
            float[] took = new float[2];
            for (int k = 0; k < 2; k++)
            {
                bool shield = k == 0;
                controller.Teleport(new Vector3(-10f + 8f * k, 8f, 0f));
                input.held = shield ? VeilForm.Shield : (VeilForm?)null;
                if (shield) input.cast = VeilForm.Shield;
                float start = Time.time;
                bool guarded = false;
                yield return null;
                while (player.transform.position.y > 0.1f && Time.time - start < 6f)
                {
                    guarded |= veil.ShieldHeld;
                    Assert.IsFalse(controller.Motor.IsGliding, "only the scarf glides");
                    yield return null;
                }
                took[k] = Time.time - start;
                if (shield) Assert.IsTrue(guarded, "the shield was held up all the way down");
                input.held = null;
                yield return Wait(0.6f);
            }
            Debug.Log($"[shield] an 8 m fall: {took[0]:F2} s with the shield held up, {took[1]:F2} s without it");
            Assert.AreEqual(took[1], took[0], 0.1f, "with the shield up he falls as fast as without it (gliding, 8 m took over 3 s)");
            Assert.Less(took[0], 2f);
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
        public IEnumerator ALedgeOutOfTheHandsReach_IsNotCaught_TheScarfGrapplesOnlyRings()
        {
            Box(new Vector3(4f, 3f, 0f), new Vector3(4f, 6f, 4f)); // wall face at x 2, top at y 6
            yield return Wait(0.2f);
            controller.Teleport(new Vector3(1.55f, 0.05f, 0f));
            veil.Charge.Restore();
            yield return Wait(0.2f);
            input.moveX = 1f;
            input.jump = true;
            input.jumpHeld = true;
            bool hung = false;
            float end = Time.time + 1.5f;
            while (Time.time < end)
            {
                if (controller.Motor.State == MotorState.LedgeHang || controller.Motor.State == MotorState.Climbing) hung = true;
                yield return null;
            }
            input.moveX = 0f;
            Assert.IsFalse(hung, "too high for the hands, and the scarf doesn't lash round lips");
            Assert.Less(player.transform.position.y, 1f, "fell back down at the foot of the wall");
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
                // where the sword really is (not just where it is meant to be): its grip, against the palm
                var sim = veil.Simulation;
                float gf = VeilFormLibrary.GripFraction(VeilForm.Sword) * (sim.Count - 1);
                int i0 = Mathf.Min(Mathf.FloorToInt(gf), sim.Count - 2);
                Vector2 grip = Vector3.Lerp(sim.Positions[i0], sim.Positions[i0 + 1], gf - i0);
                Vector2 palm = VeilController.PalmPoint(hand);
                worst = Mathf.Max(worst, Vector2.Distance(grip, palm));
                frames++;
            }
            Assert.Greater(frames, 10, "the sword formed and swung");
            Assert.Less(worst, 0.04f, "the hilt stays in the fist through the swing, not trailing behind it");
        }
    }
}
