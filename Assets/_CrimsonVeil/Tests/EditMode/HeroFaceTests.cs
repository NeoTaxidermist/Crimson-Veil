using System.Linq;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero's face rig (cutscenes): the bones, their skin, the expressions, blinking, talking.</summary>
    public class HeroFaceTests
    {
        const string HeroPath = "Assets/_CrimsonVeil/Art/Hero/Hero.fbx";
        static readonly string[] FaceBones = { "Jaw", "Eye.L", "Eye.R", "Lid.U.L", "Lid.U.R", "Lid.D.L", "Lid.D.R", "Brow.L", "Brow.R" };

        [Test]
        public void TheFace_HasAJaw_EyesWithLids_AndBrows_AllOnTheHead_AndEachMovesSkin()
        {
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            var bones = hero.GetComponentsInChildren<Transform>(true);
            foreach (var n in FaceBones)
            {
                var b = bones.FirstOrDefault(t => t.name == n);
                Assert.IsNotNull(b, n);
                Assert.AreEqual("Head", b.parent.name, n + " hangs on the head");
            }
            var smr = hero.GetComponentInChildren<SkinnedMeshRenderer>();
            var weighted = new System.Collections.Generic.HashSet<string>();
            foreach (var w in smr.sharedMesh.boneWeights)
            {
                if (w.weight0 > 0.3f) weighted.Add(smr.bones[w.boneIndex0].name);
                if (w.weight1 > 0.3f) weighted.Add(smr.bones[w.boneIndex1].name);
            }
            foreach (var n in FaceBones) CollectionAssert.Contains(weighted, n, n + " carries part of the face");
        }

        [Test]
        public void Expressions_ReadAsTheyShould()
        {
            var angry = HeroFace.Pose(FaceExpression.Angry);
            Assert.Greater(angry.frown, 0.7f); Assert.Less(angry.browL, 0f); Assert.Greater(angry.lidUpper, 0.1f);
            var surprised = HeroFace.Pose(FaceExpression.Surprised);
            Assert.Greater(surprised.browL, 0.7f); Assert.Less(surprised.lidUpper, 0f, "eyes wide"); Assert.Greater(surprised.jaw, 0.2f);
            var sad = HeroFace.Pose(FaceExpression.Sad);
            Assert.Greater(sad.browL, 0f); Assert.Less(sad.lookPitch, 0f, "eyes down");
            var neutral = HeroFace.Pose(FaceExpression.Neutral);
            Assert.AreEqual(0f, neutral.jaw); Assert.AreEqual(0f, neutral.frown);
        }

        [Test]
        public void ABlink_ShutsFast_HoldsAMoment_OpensSlower()
        {
            Assert.AreEqual(0f, HeroFace.BlinkCurve(-0.1f));
            Assert.AreEqual(1f, HeroFace.BlinkCurve(HeroFace.BlinkShut + 0.01f), 1e-4f, "shut");
            Assert.AreEqual(0f, HeroFace.BlinkCurve(HeroFace.BlinkLength + 0.01f), "open again");
            Assert.Less(HeroFace.BlinkShut, HeroFace.BlinkOpen, "shutting is quicker than opening");
            Assert.Less(HeroFace.BlinkLength, 0.3f, "a blink, not a nap");
        }

        [Test]
        public void Talking_WorksTheJawInSyllables_WithPauses()
        {
            float hi = 0f; int shut = 0, n = 0;
            for (float t = 0f; t < 10f; t += 0.01f, n++)
            {
                float j = HeroFace.TalkJaw(t);
                Assert.That(j, Is.InRange(0f, 0.5f));
                hi = Mathf.Max(hi, j);
                if (j < 0.02f) shut++;
            }
            Assert.Greater(hi, 0.2f, "it opens to speak");
            Assert.Greater(shut, n / 10, "between syllables and in pauses it closes");
        }
    }
}
