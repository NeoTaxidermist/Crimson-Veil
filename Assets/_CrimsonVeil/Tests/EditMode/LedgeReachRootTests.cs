using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hands hang on the real lip (not in the air beside a rounded rock), and the root crawler's roots stay out of the stone.</summary>
    [Category("Hero")]
    [Category("Motor")]
    public class LedgeReachRootTests
    {
        [Test]
        public void TheHero_HangsTheHandsReachBehindTheLip()
        {
            // a sheer wall: the lip is where the wall is - his body against it, the hands just over the lip
            Assert.AreEqual(5f - 0.31f, PlayerController.HangX(1, 5f, 5f, 0.28f), 1e-4f);
            // a rounded / undercut rock: the side lower down is well behind the lip - he hangs from the lip, not the side
            Assert.AreEqual(5f - PlayerController.HangReach, PlayerController.HangX(1, 5f, 5.6f, 0.28f), 1e-4f);
            // a bulge below the lip: never inside it
            Assert.AreEqual(4.8f - 0.31f, PlayerController.HangX(1, 5f, 4.8f, 0.28f), 1e-4f);
            // facing left, mirrored
            Assert.AreEqual(-5f + PlayerController.HangReach, PlayerController.HangX(-1, -5f, -5.6f, 0.28f), 1e-4f);
        }

        [Test]
        public void TheArms_PointAtTheLip()
        {
            float near = HeroPoseSolver.HangArm(PlayerController.HangReach, 1.95f);
            Assert.AreEqual(HeroPoseSolver.DefaultHangArm, near, 6f, "the usual hang");
            Assert.Less(HeroPoseSolver.HangArm(0.6f, 1.95f), near, "a lip further ahead: the arms reach further forward");
            Assert.Greater(HeroPoseSolver.HangArm(0.05f, 2.0f), near, "straight above: the arms go up");
            var far = HeroPoseSolver.HangPose(0f, HeroPoseSolver.HangArm(0.6f, 1.95f));
            Assert.Less(far.armL, near);
        }

        [Test]
        public void TheRoots_FanAwayFromTheSurface()
        {
            for (int i = 0; i < 9; i++)
                Assert.LessOrEqual(Mathf.Abs(RootCrawlerLogic.RootAngle(i, 9)), RootCrawlerLogic.Spread + 1e-3f);
            Assert.AreEqual(-RootCrawlerLogic.Spread, RootCrawlerLogic.RootAngle(0, 9), 1e-4f);
            Assert.AreEqual(RootCrawlerLogic.Spread, RootCrawlerLogic.RootAngle(8, 9), 1e-4f);
        }

        [Test]
        public void TheRoots_NeverDipIntoTheStone_EvenWrithing()
        {
            // the knot sits 0.25 m off the ground (0.3 m under a ceiling): no root may reach further than that towards it
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Enemy/RootCrawler.fbx");
            float worst = 0f;
            float limit = RootCrawlerLogic.Spread + RootCrawlerLogic.Wiggle;
            foreach (var mf in asset.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.name.StartsWith("Root")) continue;
                foreach (var a in new[] { -limit, limit })
                {
                    var rot = Quaternion.Euler(0f, 0f, a);
                    foreach (var v in mf.sharedMesh.vertices) worst = Mathf.Min(worst, (rot * v).y);
                }
            }
            Assert.Greater(worst, -0.25f, "a root tip went under the surface");
        }
    }
}
