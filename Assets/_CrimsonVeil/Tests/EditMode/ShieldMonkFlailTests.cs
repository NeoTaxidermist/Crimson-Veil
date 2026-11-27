using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The shield monk rebuilt to the hero's level (v0.101): his flail (the user's choice instead of the old "stick" -
    /// a spiked head on a chain), the blow he brings it down with, and the model made as finely as the hero.
    /// </summary>
    [Category("Combat")]
    public class ShieldMonkFlailTests
    {
        static float[] Segments() => new[] { 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.14f };

        [Test]
        public void TheChain_HangsStraightDown_AtItsLength()
        {
            var c = new FlailChain(Segments(), new Vector3(1f, 2f, 0f));
            Assert.AreEqual(0.5f, c.Length, 1e-4f);
            Assert.AreEqual(8, c.Count, "the tip, six links' joints and the head");
            for (int i = 0; i < 60; i++) c.Step(new Vector3(1f, 2f, 0f), 1f / 60f);
            Assert.AreEqual(1f, c.Head.x, 0.01f, "at rest it hangs plumb");
            Assert.AreEqual(1.5f, c.Head.y, 0.01f, "a chain's length below the tip");
        }

        [Test]
        public void TheChain_KeepsEveryLink_WhenTheFistJerksIt_AndSwingsBehind()
        {
            var seg = Segments();
            var c = new FlailChain(seg, Vector3.zero);
            for (int i = 0; i < 30; i++) c.Step(new Vector3(i * 0.05f, 0f, 0f), 1f / 60f);   // dragged sideways fast
            for (int i = 1; i < c.Count; i++)
                Assert.AreEqual(seg[i - 1], Vector3.Distance(c[i - 1], c[i]), 0.003f, "link " + i + " neither stretches nor shrinks");
            Assert.Less(c.Head.x, c[0].x - 0.05f, "the head trails behind the fist that drags it");
            Assert.AreEqual(0f, c.Head.z, 1e-5f, "kept in the picture's plane");
        }

        [Test]
        public void TheBlow_DrawsTheHeadAlongItsArc()
        {
            var c = new FlailChain(Segments(), Vector3.zero);
            var target = FlailChain.ArcPoint(Vector3.zero, 1, 30f, c.Length * 0.95f);
            for (int i = 0; i < 12; i++) c.Step(Vector3.zero, 1f / 60f, 9.81f, 0.985f, target, 0.7f);
            Assert.Less(Vector3.Distance(c.Head, target), 0.08f, "the head follows the arm's arc");
            Assert.Greater(c.Head.x, 0.3f, "ahead of him, the way he faces");
            var left = FlailChain.ArcPoint(Vector3.zero, -1, 30f, 1f);
            Assert.Less(left.x, 0f, "facing -x the blow lands on the other side");
        }

        [Test]
        public void TheFlail_IsWhirledUpBehind_ThenBroughtOverHisHead_AndDownInFront()
        {
            float cocked = Mathf.Repeat(FlailChain.WindupAngle(1f), 360f);
            Assert.AreEqual(FlailChain.SwingAngle(0f), cocked, 0.5f, "the blow starts where the wind-up leaves the head");
            Assert.AreEqual(150f, FlailChain.SwingAngle(0f), 0.01f, "high behind him");
            Assert.AreEqual(-40f, FlailChain.SwingAngle(1f), 0.01f, "down in front of him");
            Assert.Greater(FlailChain.SwingAngle(0.3f), FlailChain.SwingAngle(0.6f), "always on, never back");
            Assert.IsFalse(FlailChain.Striking(150f), "behind him it hurts no one (the hero gets a warning)");
            Assert.IsTrue(FlailChain.Striking(60f), "high in front: it hits a hero jumping over him");
            Assert.IsTrue(FlailChain.Striking(0f), "level ahead");
            Assert.IsFalse(FlailChain.Striking(-70f), "spent, against the floor");
        }

        [Test]
        public void HeBashesClose_SwingsTheFlailFurtherOff_AndOnlyWithHisShield()
        {
            const float bash = 1.7f, flail = 2.8f;
            Assert.AreEqual(ShieldMonk.State.Windup, ShieldMonkLogic.Choose(true, 1.2f, bash, flail, true, true), "close: the shield");
            Assert.AreEqual(ShieldMonk.State.FlailWindup, ShieldMonkLogic.Choose(true, 2.3f, bash, flail, true, true), "a step off: the flail");
            Assert.AreEqual(ShieldMonk.State.Walk, ShieldMonkLogic.Choose(true, 2.3f, bash, flail, false, true), "torn from his shield he is too shaken to swing");
            Assert.AreEqual(ShieldMonk.State.Walk, ShieldMonkLogic.Choose(true, 2.3f, bash, flail, true, false), "the flail has its rest");
            Assert.AreEqual(ShieldMonk.State.Walk, ShieldMonkLogic.Choose(true, 4f, bash, flail, true, true), "too far: he walks on");
            Assert.AreEqual(ShieldMonk.State.Walk, ShieldMonkLogic.Choose(false, 1.2f, bash, flail, true, true), "not while turned away");
            Assert.AreEqual(ShieldMonk.State.Windup, ShieldMonkLogic.Choose(true, 1.2f, bash, flail, false, true), "the bash as before, shield or not");
        }

        [Test]
        public void HeRaisesTheFlailOverHisHead_AndThrowsHisWeightIntoTheBlow()
        {
            var still = CreatureAnimation.Monk(ShieldMonk.State.Walk, 0f, 0f, 0f, true, 0f);
            var up = CreatureAnimation.Monk(ShieldMonk.State.FlailWindup, 1f, 0f, 0f, true, 0f);
            var blow = CreatureAnimation.Monk(ShieldMonk.State.FlailSwing, 1f, 0f, 0f, true, 0f);
            Assert.Greater(up.armR, 120f, "the flail arm high over his head");
            Assert.Less(up.spine, still.spine, "leaning back as he whirls it");
            Assert.Less(blow.armR, 10f, "brought down through in front");
            Assert.Greater(blow.spine, 15f, "his body follows the blow");
            Assert.Greater(blow.lunge, 0.3f, "stepping after it");
            Assert.AreEqual(still.armL, up.armL, 15f, "the shield stays up before him all the while");
        }

        static GameObject Model() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Enemy/ShieldMonk.fbx"));

        [Test]
        public void TheMonk_HasHands_AFlailOfHaftLinksAndHead_AndAShield()
        {
            var m = Model();
            try
            {
                foreach (var b in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "UpperArm.L", "LowerArm.R", "Hand.L", "Hand.R" })
                    Assert.IsNotNull(RigJoint.FindDeep(m.transform, b), b + " (the script poses it)");
                foreach (var side in new[] { "L", "R" })
                {
                    for (int i = 0; i < 4; i++) Assert.IsNotNull(RigJoint.FindDeep(m.transform, $"Finger.{side}.{i}.2"), $"finger {side}{i}");
                    Assert.IsNotNull(RigJoint.FindDeep(m.transform, $"Thumb.{side}.1"), "thumb " + side);
                }
                Assert.IsTrue(new HeroHands(m.transform, RigJoint.FindDeep(m.transform, "Hips")).IsReady, "fists that close round haft and grip");
                foreach (var part in new[] { "Shield", "Flail", "FlailHead" })
                    Assert.IsNotNull(CreatureParts.Find(m.transform, part), part + " (moved by the script)");
                int links = 0;
                while (CreatureParts.Find(m.transform, "FlailLink." + links) != null) links++;
                Assert.GreaterOrEqual(links, 6, "a chain of links between haft and head");
                Assert.IsNull(CreatureParts.Find(m.transform, "Tatter.16.4"), "no rosary hanging from the fist any more");
            }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void TheMonk_IsMadeAsFinelyAsTheHero_YetLightEnoughForAFew()
        {
            var m = Model();
            var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx"));
            try
            {
                var smr = m.GetComponentInChildren<SkinnedMeshRenderer>();
                int heroVerts = hero.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount;
                Assert.Greater(smr.sharedMesh.vertexCount, heroVerts * 0.7f, "as finely made as the hero");
                Assert.Less(smr.sharedMesh.vertexCount, 60000, "light enough for several at once");
                Assert.AreEqual(2, smr.sharedMaterials.Length, "ink and crimson");
                var head = CreatureParts.Find(m.transform, "FlailHead").GetComponent<MeshFilter>().sharedMesh;
                Assert.Greater(head.vertexCount, 2000, "a spiked head, not a ball");
                Assert.AreEqual(2, head.subMeshCount, "ink with a crimson band");
            }
            finally { Object.DestroyImmediate(m); Object.DestroyImmediate(hero); }
        }
    }
}
