#if UNITY_EDITOR
using System.Collections;
using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The rigged bestiary models alive in a scene: the monk's shield and the hanged man's loose limbs.</summary>
    [Category("Combat")]
    public class BestiaryModelPlayTests
    {
        GameObject floor, creature;

        [SetUp]
        public void SetUp()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(30f, 1f, 4f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(floor);
            if (creature != null) Object.Destroy(creature);
        }

        static GameObject Spawn(string id, Vector3 at)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/" + id + ".prefab");
            Assert.IsNotNull(prefab, id);
            return Object.Instantiate(prefab, at, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator TheMonksShield_ComesOutOfTheModel_AndIsHeldBeforeHim()
        {
            creature = Spawn("ShieldMonk", Vector3.zero);
            yield return null;
            var visual = CreatureParts.Find(creature.transform, "Visual");
            var shield = CreatureParts.Find(creature.transform, "Shield");
            Assert.IsNotNull(shield);
            Assert.AreSame(visual, shield.parent, "moved under the Visual, where the script poses it");
            for (int i = 0; i < 40; i++) yield return null;
            var m = creature.GetComponent<ShieldMonk>();
            float side = (shield.position.x - creature.transform.position.x) * m.Facing;
            Assert.Greater(side, 0.2f, "the shield is up on the side he faces");
            Assert.That(shield.position.y - creature.transform.position.y, Is.InRange(0.6f, 1.6f), "at chest height");
        }

        [UnityTest]
        public IEnumerator TheMonksFlail_IsInHisFreeFist_ItsHeadHangingOnTheChain()
        {
            creature = Spawn("ShieldMonk", Vector3.zero);
            var m = creature.GetComponent<ShieldMonk>();
            for (float t = 0f; t < 1f; t += Time.deltaTime) yield return null;   // (time for the chain to settle)
            var haft = CreatureParts.Find(creature.transform, "Flail");
            Assert.IsNotNull(haft);
            Assert.IsNotNull(m.FlailHead);
            Assert.AreSame(CreatureParts.Find(creature.transform, "Visual"), haft.parent, "moved under the Visual");
            // the free hand: the shield is held by the arm on the camera's side (-z), so the free one is the far one
            var handL = RigJoint.FindDeep(creature.transform, "Hand.L");
            var handR = RigJoint.FindDeep(creature.transform, "Hand.R");
            var shield = CreatureParts.Find(creature.transform, "Shield");
            var free = handL.position.z > handR.position.z ? handL : handR;
            Assert.Less(Vector3.Distance(haft.position, free.position), 0.15f,
                $"the haft in the free fist (haft {haft.position}, L {handL.position}, R {handR.position}, shield {shield.position}, state {m.Current}, facing {m.Facing})");
            var tip = m.FlailTipPosition;
            Assert.Greater(tip.y, haft.position.y + 0.2f, "the haft held up out of the fist");
            Assert.Less(m.FlailHead.position.y, tip.y - 0.3f,
                $"the head hangs on its chain, below the haft's tip (tip {tip}, head {m.FlailHead.position}, grip {haft.position})");
            Assert.Greater(m.FlailHead.position.y - creature.transform.position.y, 0.05f, "not through the floor");
            Assert.IsFalse(float.IsNaN(m.FlailHead.position.x));
        }

        [UnityTest]
        public IEnumerator TheMonk_WhirlsTheFlail_AndBringsItDownOnAHeroAStepAway()
        {
            var hero = new GameObject("Hero");
            hero.SetActive(false);
            hero.transform.position = new Vector3(2.3f, 0.05f, 0f);
            var cc = hero.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            hero.AddComponent<ScriptedInput>();
            hero.AddComponent<CrimsonVeil.Player.PlayerController>();
            var health = hero.AddComponent<PlayerHealth>();
            hero.SetActive(true);
            try
            {
                yield return null;
                creature = Spawn("ShieldMonk", Vector3.zero);
                var m = creature.GetComponent<ShieldMonk>();
                bool whirled = false, swung = false;
                float headTop = float.MinValue;
                float before = health.Model.Current;
                for (float t = 0f; t < 3f && health.Model.Current >= before; t += Time.deltaTime)
                {
                    if (m.Current == ShieldMonk.State.FlailWindup) whirled = true;
                    if (m.Current == ShieldMonk.State.FlailSwing) swung = true;
                    if (whirled) headTop = Mathf.Max(headTop, m.FlailHead.position.y);
                    yield return null;
                }
                Assert.IsTrue(whirled, "a step off, he whirls the flail up (not the shield bash)");
                Assert.IsTrue(swung, "and brings it down");
                Assert.Greater(headTop, 2.2f, "whirled up over his head (a warning the hero can see)");
                Assert.Less(health.Model.Current, before, "the head lands on the hero a step away");
            }
            finally { Object.Destroy(hero); }
        }

        [UnityTest]
        public IEnumerator TheHangedMansLimbs_HangLoose()
        {
            creature = Spawn("HangedMan", new Vector3(0f, 3f, 0f));
            var foot = RigJoint.FindDeep(creature.transform, "Tatter.2.2");
            var hips = RigJoint.FindDeep(creature.transform, "Hips");
            Assert.IsNotNull(foot, "a leg chain");
            float leg = Vector3.Distance(foot.position, hips.position);
            for (int i = 0; i < 60; i++) yield return null;
            Assert.IsFalse(float.IsNaN(foot.position.x), "the spring chain stays sane");
            // (the body itself swings and drops on its rope: the foot is measured from the hips)
            Assert.Less(Vector3.Distance(foot.position, hips.position), leg * 1.15f, "dangling from the hip, not torn off");
        }
    }
}
#endif
