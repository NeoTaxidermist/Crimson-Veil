using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The ink wraith's model: a rigged spectre with claws and a torn shroud that swings.</summary>
    public class WraithModelTests
    {
        static GameObject Model() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Enemy/Wraith.fbx"));

        [Test]
        public void TheWraith_HasItsBody_ClawsAndShroudStrips()
        {
            var w = Model();
            try
            {
                foreach (var b in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "UpperArm.L", "LowerArm.R", "Hand.L" })
                    Assert.IsNotNull(RigJoint.FindDeep(w.transform, b), b + " (EnemyAnimator poses it)");
                foreach (var side in new[] { "L", "R" })
                    for (int i = 0; i < 3; i++)
                        Assert.IsNotNull(RigJoint.FindDeep(w.transform, $"Finger.{side}.{i}.2"), $"claw {side}{i}");
                int strips = 0;
                foreach (var t in w.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Tatter.") && t.name.EndsWith(".0")) strips++;
                Assert.GreaterOrEqual(strips, 10, "the shroud tears into strips");
                var smr = w.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.AreEqual(2, smr.sharedMaterials.Length, "ink and the crimson eyes");
                Assert.That(smr.bounds.size.y, Is.InRange(1.8f, 2.4f), "a tall spectre, a head over the hero");
            }
            finally { Object.DestroyImmediate(w); }
        }

        [Test]
        public void TheClaws_CurlAndSplay()
        {
            var w = Model();
            try
            {
                var hands = new HeroHands(w.transform, RigJoint.FindDeep(w.transform, "Hips"));
                Assert.IsTrue(hands.IsReady, "three claws and a thumb are a hand");
                var hand = RigJoint.FindDeep(w.transform, "Hand.L");
                var tip = RigJoint.FindDeep(w.transform, "Finger.L.1.2");
                hands.Apply(0f, 0f, 10f);
                float rest = Vector3.Distance(tip.position, hand.position);
                hands.Apply(1f, 1f, 10f);
                Assert.Less(Vector3.Distance(tip.position, hand.position), rest * 0.9f, "raked shut");
            }
            finally { Object.DestroyImmediate(w); }
        }

        [Test]
        public void TheClaws_ThrowOpenOnTheWindUp_AndRakeShutInTheStrike()
        {
            Assert.Less(EnemyAnimator.ClawGrip(EnemyState.Windup, 1f), 0f, "the telegraph: claws thrown wide");
            Assert.AreEqual(1f, EnemyAnimator.ClawGrip(EnemyState.Strike, 0f), 1e-4f);
            Assert.Greater(EnemyAnimator.ClawGrip(EnemyState.Patrol, 0f), 0f, "hooked while drifting");
        }

        [Test]
        public void ThePrefab_SwingsItsStrips()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Wraith.prefab");
            var springs = prefab.GetComponent<SpringBones>();
            Assert.IsNotNull(springs);
            Assert.AreEqual("Tatter.", new SerializedObject(springs).FindProperty("prefix").stringValue);
        }
    }
}
