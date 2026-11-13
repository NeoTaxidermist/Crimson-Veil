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
