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
