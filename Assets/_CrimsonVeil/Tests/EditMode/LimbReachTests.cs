using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The two-bone reach that keeps the hands on the lip while hanging and mantling.</summary>
    [Category("Hero")]
    public class LimbReachTests
    {
        [Test]
        public void TheMiddleJoint_KeepsBothBoneLengths_AndBendsToThePole()
        {
            var root = Vector3.zero;
            var target = new Vector3(0.4f, 0.3f, 0f);
            var mid = LimbReach.Middle(root, 0.3f, 0.3f, target, new Vector3(-1f, -1f, 0f));
            Assert.AreEqual(0.3f, Vector3.Distance(root, mid), 1e-3f);
            Assert.AreEqual(0.3f, Vector3.Distance(mid, target), 1e-3f);
            Assert.Less(Vector3.Cross(target, mid).z, 0f, "bent to the pole's side of the line");
        }

        [Test]
        public void OutOfReach_TheLimbStretchesStraightAtIt()
        {
            var mid = LimbReach.Middle(Vector3.zero, 0.3f, 0.3f, new Vector3(2f, 0f, 0f), Vector3.down);
            Assert.AreEqual(0.3f, mid.x, 1e-3f);
            Assert.AreEqual(0f, mid.y, 1e-2f);
        }

        [Test]
        public void Solve_PutsTheHandOnTheTarget()
        {
            var a = new GameObject("upper").transform;
            var b = new GameObject("lower").transform;
            var c = new GameObject("hand").transform;
            try
            {
                b.SetParent(a, false); b.localPosition = new Vector3(0f, -0.3f, 0f);
                c.SetParent(b, false); c.localPosition = new Vector3(0f, -0.3f, 0f);
                var target = new Vector3(0.25f, 0.35f, 0f);
                LimbReach.Solve(a, b, c, target, new Vector3(-1f, -0.5f, 0f), 1f);
                Assert.Less(Vector3.Distance(c.position, target), 0.01f);
                Assert.Less(b.position.x, 0.2f, "the elbow bends back, away from the wall");
            }
            finally { Object.DestroyImmediate(a.gameObject); }
        }

        [Test]
        public void TheHands_HoldTheLip_UntilThePressIsDone()
        {
            Assert.AreEqual(1f, HeroPoseSolver.LipHold(true, false, 0f));
            Assert.AreEqual(1f, HeroPoseSolver.LipHold(false, true, HeroPoseSolver.ClimbPullEnd));
            Assert.AreEqual(1f, HeroPoseSolver.LipHold(false, true, HeroPoseSolver.ClimbPressEnd), 1e-4f);
            Assert.AreEqual(0f, HeroPoseSolver.LipHold(false, true, HeroPoseSolver.ClimbKneeEnd), 1e-4f, "the knee is up: let go");
            Assert.Less(HeroPoseSolver.LipHandOffset(0f).x, 0f, "hanging: fingers hooked over the corner");
            Assert.Greater(HeroPoseSolver.LipHandOffset(HeroPoseSolver.ClimbPressEnd).x, 0.05f, "pressing: palms flat on the top");
        }
    }
}
