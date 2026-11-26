using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero's fingers: both hands have finger bones and close round what they hold.</summary>
    [Category("Hero")]
    public class HeroHandsTests
    {
        [Test]
        public void EachHand_ClosesRoundWhatItHolds()
        {
            HeroHands.Targets(VeilForm.Sword, 1f, false, out float r, out float l);
            Assert.AreEqual(1f, r, 1e-4f, "the sword hand clenches round the hilt");
            Assert.AreEqual(0f, l, 1e-4f, "the free hand stays relaxed");
            HeroHands.Targets(VeilForm.Shield, 1f, false, out r, out l);
            Assert.That(r, Is.InRange(0.3f, 0.6f), "under the floating shield the palm is half open");
            HeroHands.Targets(VeilForm.Bow, 1f, true, out r, out l);
            Assert.AreEqual(1f, r, 1e-4f, "the bow hand grips the bow");
            Assert.Greater(l, 0.7f, "the drawing hand pinches the string");
            HeroHands.Targets(VeilForm.Flow, 0f, false, out r, out l);
            Assert.AreEqual(0f, r, 1e-4f); Assert.AreEqual(0f, l, 1e-4f);
        }

        [Test]
        public void TheHeroModel_HasFingerBonesInBothHands_AndTheyCurlIntoAFist()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx");
            Assert.IsNotNull(prefab);
            var hero = Object.Instantiate(prefab);
            try
            {
                Transform Find(string n) { foreach (var t in hero.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
                foreach (var side in new[] { "L", "R" })
                {
                    for (int i = 0; i < 4; i++)
                        for (int k = 0; k < 3; k++)
                            Assert.IsNotNull(Find($"Finger.{side}.{i}.{k}"), $"Finger.{side}.{i}.{k}");
                    Assert.IsNotNull(Find($"Thumb.{side}.0")); Assert.IsNotNull(Find($"Thumb.{side}.1"));
                }
                var hands = new HeroHands(hero.transform, Find("Hips"));
                Assert.IsTrue(hands.IsReady);
                foreach (var side in new[] { "L", "R" })
                {
                    var hand = Find("Hand." + side);
                    var tip = Find($"Finger.{side}.1.2");
                    hands.Apply(0f, 0f, 10f);
                    float open = Vector3.Distance(tip.position, hand.position);
                    hands.Apply(1f, 1f, 10f);
                    float fist = Vector3.Distance(tip.position, hand.position);
                    Assert.Less(fist, open * 0.8f, $"the {side} hand's fingers fold in");
                    // towards the palm, which faces the body: the fingertips come in towards the hips
                    var hips = Find("Hips");
                    hands.Apply(0f, 0f, 10f);
                    float openToBody = Mathf.Abs(tip.position.x - hips.position.x);
                    hands.Apply(1f, 1f, 10f);
                    Assert.Less(Mathf.Abs(tip.position.x - hips.position.x), openToBody, $"the {side} fingers curl into the palm, not out");
                    // the thumb's tip joint bends too in a fist
                    var thumbTip = Find($"Thumb.{side}.2");
                    hands.Apply(0f, 0f, 10f);
                    var tipRest = thumbTip.localRotation;
                    hands.Apply(1f, 1f, 10f);
                    Assert.Greater(Quaternion.Angle(tipRest, thumbTip.localRotation), HeroHands.ThumbTipCurl * 0.9f, $"the {side} thumb's tip curls");
                }
            }
            finally
            {
                Object.DestroyImmediate(hero);
            }
        }
    }
}
