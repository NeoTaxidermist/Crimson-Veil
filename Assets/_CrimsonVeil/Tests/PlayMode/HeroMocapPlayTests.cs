using System.Collections;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The captured motion on the real model: it turns his bones towards the clip by its weight, and lets go.</summary>
    public class HeroMocapPlayTests
    {
        GameObject hero;
        HeroMocap mocap;

        [SetUp]
        public void SetUp()
        {
            hero = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx"));
            hero.transform.position = new Vector3(1000f, 200f, 0f);
            mocap = hero.AddComponent<HeroMocap>();
            mocap.Bind(AssetDatabase.LoadAssetAtPath<HeroMocapLibrary>("Assets/_CrimsonVeil/Animations/Hero/HeroMocapLibrary.asset"), hero.transform);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(hero);

        [UnityTest]
        public IEnumerator TheMantle_PullsTheArmsUp_ThenLetsGo()
        {
            Assert.IsTrue(mocap.IsReady);
            var arm = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "UpperArm.R");
            var rest = arm.localRotation;
            var want = new HeroMocap.Choice { key = HeroMocap.Mantle, driven = true, time01 = 0.1f, weight = 1f };
            for (int i = 0; i < 20; i++)
            {
                arm.localRotation = rest;          // (the procedural pose each frame)
                mocap.Apply(want, 1f / 30f);
                yield return null;
            }
            Assert.Greater(Quaternion.Angle(rest, arm.localRotation), 40f, "the arm reaches up to the lip");
            for (int i = 0; i < 20; i++)
            {
                arm.localRotation = rest;
                mocap.Apply(HeroMocap.Choice.None, 1f / 30f);
                yield return null;
            }
            Assert.Less(Quaternion.Angle(rest, arm.localRotation), 1f, "faded out, the procedural pose is his again");
            Assert.AreEqual(0f, mocap.Weight, 1e-4f);
        }

        [UnityTest]
        public IEnumerator ACutscene_PlaysItsClip_OverTheGamesChoice()
        {
            mocap.Play(HeroMocap.Talk);
            for (int i = 0; i < 15; i++) { mocap.Apply(HeroMocap.Choice.None, 1f / 30f); yield return null; }
            Assert.AreEqual(HeroMocap.Talk, mocap.Current);
            Assert.Greater(mocap.Weight, 0.9f);
            mocap.StopScript();
            for (int i = 0; i < 30; i++) { mocap.Apply(HeroMocap.Choice.None, 1f / 30f); yield return null; }
            Assert.AreEqual(0f, mocap.Weight, 1e-4f);
        }
    }
}
