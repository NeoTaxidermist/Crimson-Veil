using System;
using System.Linq;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The hero's frame-by-frame code reuses what it used to make anew (the garbage is measured on the hero in
    /// HeroMoveSetPlayTests.TheHero_AllocatesNoGarbage_FrameByFrame): what is reused must give what was made anew.
    /// </summary>
    [Category("Hero")]
    [Category("Veil")]
    public class NoGarbageTests
    {
        [Test]
        public void EveryPartOfTheHero_HasItsOwnProfilerMarker()
        {
            Assert.AreEqual(Enum.GetValues(typeof(Perf.Id)).Length, Perf.Names.Length);
            Assert.AreEqual(Perf.Names.Length, Perf.Names.Distinct().Count());
            foreach (var n in Perf.Names) StringAssert.StartsWith("CV.", n);
            using (Perf.Begin(Perf.Id.Animator)) using (Perf.Begin(Perf.Id.Pose)) { }   // nested scopes open and close
        }

        [Test]
        public void AWeaponsShape_RememberedOnce_IsTheShapeWorkedOut()
        {
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm)))
            {
                var fresh = new Vector2[48];
                var points = VeilFormLibrary.ControlPoints(f, out int start);
                float freshHand = VeilFormLibrary.Resample(points, start, fresh);
                var a = new Vector2[48];
                float handA = VeilFormLibrary.BuildShape(f, a);
                a[5] = new Vector2(99f, 99f);   // the copy handed out is the caller's: changing it changes no other
                var b = new Vector2[48];
                float handB = VeilFormLibrary.BuildShape(f, b);
                Assert.AreEqual(freshHand, handA, 1e-6f, f.ToString());
                Assert.AreEqual(freshHand, handB, 1e-6f, f.ToString());
                for (int i = 0; i < fresh.Length; i++) Assert.AreEqual(fresh[i], b[i], $"{f} point {i}");
            }
        }

        [Test]
        public void ABlowsKeys_FromSharedBuffers_DontMixOneBlowIntoAnother()
        {
            // the choreography works in buffers of its own: sampled in turn, blow after blow, each comes out as alone
            var forms = new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow };
            foreach (var f in forms)
                for (float t = 0f; t <= 1f; t += 0.1f)
                {
                    var alone = HeroAttackChoreography.Sample(f, t, 30f);
                    foreach (var g in forms) { HeroAttackChoreography.Sample(g, 1f - t, -40f); HeroAttackChoreography.SampleLow(g, t); }
                    var again = HeroAttackChoreography.Sample(f, t, 30f);
                    Assert.AreEqual(alone.armR, again.armR, 1e-5f, $"{f} t{t}");
                    Assert.AreEqual(alone.spine, again.spine, 1e-5f, $"{f} t{t}");
                    Assert.AreEqual(alone.kneeR, again.kneeR, 1e-5f, $"{f} t{t}");
                }
        }

        [Test]
        public void TheClimb_FromItsOwnKeyArrays_IsTheSameEveryTime()
        {
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                var a = HeroPoseSolver.ClimbPose(t);
                HeroPoseSolver.ClimbPose(1f - t);
                var b = HeroPoseSolver.ClimbPose(t);
                Assert.AreEqual(a.armL, b.armL, 1e-6f);
                Assert.AreEqual(a.thighR, b.thighR, 1e-6f);
                Assert.IsTrue(a.IsFinite());
            }
            Assert.AreEqual(HeroPoseSolver.HangPose(0f).armL, HeroPoseSolver.ClimbPose(0f).armL, 1e-4f, "it starts hanging");
        }
    }
}
