using System.Collections;
using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The bestiary in a live scene: blocks, cut ropes and threads, a swarm shot down.</summary>
    public class CreaturePlayTests
    {
        GameObject ground, attacker;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 4f);
            attacker = new GameObject("Attacker");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var c in Object.FindObjectsByType<CreatureBase>(FindObjectsSortMode.None)) Object.Destroy(c.gameObject);
            foreach (var s in Object.FindObjectsByType<CrowSwarm>(FindObjectsSortMode.None)) Object.Destroy(s.gameObject);
            Object.Destroy(ground);
            Object.Destroy(attacker);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        DamageInfo HitFrom(float x, DamageKind kind)
        {
            attacker.transform.position = new Vector3(x, 1f, 0f);
            return new DamageInfo { amount = 1f, kind = kind, point = new Vector3(x * 0.5f, 1f, 0f), knockback = new Vector2(-Mathf.Sign(x) * 3f, 1f), source = attacker };
        }

        [UnityTest]
        public IEnumerator Slide_StopsAtStone_SlidesAlongIt_AndIgnoresCreatures()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(3f, 3f, 0f);
            wall.transform.localScale = new Vector3(1f, 6f, 4f);       // face at x 2.5
            var other = new GameObject("OtherMoth");
            other.transform.position = new Vector3(1f, 1f, 0f);
            other.AddComponent<SphereCollider>().radius = 0.5f;
            other.AddComponent<AshMoth>().enabled = false;            // a creature: no wall
            other.GetComponent<SphereCollider>().isTrigger = false;
            yield return new WaitForFixedUpdate();

            var p = CreatureMotion.Slide(new Vector3(0f, 2f, 0f), new Vector3(5f, 0f, 0f), 0.3f);
            Assert.AreEqual(2.5f - 0.3f, p.x, 0.05f, "stopped at the wall's face");
            var d = CreatureMotion.Slide(new Vector3(0f, 2f, 0f), new Vector3(5f, 2f, 0f), 0.3f);
            Assert.LessOrEqual(d.x, 2.25f);
            Assert.Greater(d.y, 3.5f, "slid up along the wall instead of stopping dead");
            var through = CreatureMotion.Slide(new Vector3(0f, 1f, 0f), new Vector3(1.8f, 0f, 0f), 0.3f);
            Assert.AreEqual(1.8f, through.x, 1e-3f, "another creature is no wall");
            Object.Destroy(wall); Object.Destroy(other);
        }

        [UnityTest]
        public IEnumerator AshMoth_FluttersAgainstAWall_NeverThroughIt()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(1.5f, 3f, 0f);
            wall.transform.localScale = new Vector3(1f, 6f, 4f);       // face at x 1.0, inside its flutter
            var go = new GameObject("Moth");
            go.transform.position = new Vector3(0f, 3f, 0f);
            AshMoth.BuildVisual(go.transform, null, null);
            go.AddComponent<AshMoth>();
            float maxX = float.MinValue;
            float end = Time.time + 4f;
            while (Time.time < end) { maxX = Mathf.Max(maxX, go.transform.position.x); yield return null; }
            Assert.LessOrEqual(maxX + AshMoth.BodyRadius, 1.0f + 0.03f, "its wings stay this side of the stone");
            Assert.Greater(maxX, 0.2f, "(it does flutter up to it)");
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator AshMoth_BeatsItsWings_QuickDown_SlowerUp()
        {
            var go = new GameObject("Moth");
            go.transform.position = new Vector3(-10f, 3f, 0f);
            AshMoth.BuildVisual(go.transform, null, null);
            go.AddComponent<AshMoth>();
            var wing = go.transform.Find("Visual/WingR");
            yield return Wait(0.3f);
            int down = 0, up = 0;
            float last = Quaternion.Angle(Quaternion.identity, wing.localRotation), lo = float.MaxValue, hi = float.MinValue;
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                yield return null;
                float a = Quaternion.Angle(Quaternion.identity, wing.localRotation);   // how far down the wing is
                lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a);
                float d = a - last;
                if (d > 0.5f) down++; else if (d < -0.5f) up++;
                last = a;
            }
            Assert.Greater(hi - lo, 50f, "the wings really beat");
            Assert.Less(down, up, "the power stroke down is quicker than the recovery up");
        }

        [UnityTest]
        public IEnumerator ShieldMonk_BlocksTheFront_UntilTheWhipTearsTheShieldAway()
        {
            var go = new GameObject("Monk");
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = Vector3.up; cap.height = 2f;
            ShieldMonk.BuildVisual(go.transform, null, null);
            var monk = go.AddComponent<ShieldMonk>();
            yield return null;

            int f = monk.Facing;
            Assert.IsFalse(monk.ReceiveHit(HitFrom(f * 2f, DamageKind.Slash)), "a blade bounces off the shield");
            Assert.IsTrue(monk.ShieldUp);
            Assert.IsFalse(monk.ReceiveHit(HitFrom(f * 2f, DamageKind.Lash)), "the lash takes the shield, not his life");
            Assert.IsFalse(monk.ShieldUp, "shield torn away");
            yield return Wait(0.2f);
            Assert.IsTrue(monk.ReceiveHit(HitFrom(f * 2f, DamageKind.Slash)), "now the blade lands");
        }

        [UnityTest]
        public IEnumerator HangedMan_CutRopeDrops_AndLeavesASolidBody()
        {
            var go = new GameObject("Hanged");
            go.transform.position = new Vector3(0f, 1.5f, 0f);
            go.AddComponent<CapsuleCollider>();
            HangedMan.BuildVisual(go.transform, null, null, null);
            var man = go.AddComponent<HangedMan>();
            yield return Wait(0.3f);
            Assert.IsFalse(man.Fallen);

            var rope = go.GetComponentInChildren<CreatureThread>();
            Assert.IsTrue(rope.ReceiveHit(HitFrom(2f, DamageKind.Arrow)));
            yield return Wait(1.5f);
            Assert.IsTrue(man.Fallen);
            var box = go.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "the body is now something to stand on");
            Assert.IsFalse(box.isTrigger);
            Assert.AreEqual(0f, go.transform.position.y, 0.2f, "lying on the ground");
        }

        [UnityTest]
        public IEnumerator HangedMan_CutOverTheHero_FallsPastHim_AndNeverPinsHim()
        {
            var hero = new GameObject("Hero");
            hero.SetActive(false);
            hero.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = hero.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            var input = hero.AddComponent<ScriptedInput>();
            var pc = hero.AddComponent<CrimsonVeil.Player.PlayerController>();
            hero.SetActive(true);
            var go = new GameObject("Hanged");
            go.transform.position = new Vector3(0f, 2.2f, 0f);
            go.AddComponent<CapsuleCollider>();
            HangedMan.BuildVisual(go.transform, null, null, null);
            var man = go.AddComponent<HangedMan>();
            try
            {
                yield return Wait(0.2f);
                go.GetComponentInChildren<CreatureThread>().ReceiveHit(HitFrom(2f, DamageKind.Arrow));
                yield return Wait(1.2f);
                Assert.IsTrue(man.Fallen);
                Assert.AreEqual(0f, go.transform.position.y, 0.2f, "on the ground, not on the hero's head");
                float start = hero.transform.position.x;
                input.moveX = 1f;
                yield return Wait(0.8f);
                Assert.Greater(hero.transform.position.x, start + 2f, "the hero walks out from under the body");
                yield return Wait(0.3f);
                Assert.IsFalse(go.GetComponent<BoxCollider>().isTrigger, "once he is clear, the body is solid again");
            }
            finally { Object.Destroy(hero); }
        }

        [UnityTest]
        public IEnumerator CrowSwarm_EveryCrowIsATarget_AndTheSwarmEndsWithTheLast()
        {
            var go = new GameObject("Swarm");
            go.transform.position = new Vector3(0f, 3f, 0f);
            CrowSwarm.BuildVisual(go.transform, null, null, 5);
            var swarm = go.AddComponent<CrowSwarm>();
            bool ended = false;
            swarm.Destroyed += () => ended = true;
            yield return null;
            Assert.AreEqual(5, swarm.Alive);
            foreach (var crow in go.GetComponentsInChildren<Crow>()) Assert.IsTrue(crow.ReceiveHit(HitFrom(2f, DamageKind.Lash)));
            Assert.AreEqual(0, swarm.Alive);
            Assert.IsTrue(ended);
        }

        [UnityTest]
        public IEnumerator PaperKite_CutThread_CrashesAndLeavesItsCloth()
        {
            var go = new GameObject("Kite");
            go.transform.position = Vector3.zero;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.9f, 0.9f, 0.4f);
            PaperKite.BuildVisual(go.transform, null, null, null);
            var kite = go.AddComponent<PaperKite>();
            yield return Wait(0.3f);
            Assert.Greater(go.transform.position.y, 3f, "flying on its thread");

            go.GetComponentInChildren<CreatureThread>().ReceiveHit(HitFrom(2f, DamageKind.Arrow));
            Assert.IsTrue(kite.Free);
            yield return Wait(4f);
            Assert.IsNotNull(go.GetComponent<KiteCloth>(), "its cloth lies on the ground to be taken");
            Assert.Less(go.transform.position.y, 0.5f);
        }
    }
}
