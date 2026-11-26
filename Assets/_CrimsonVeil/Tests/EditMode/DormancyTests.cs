using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>When a creature far out of the picture sleeps and when it wakes.</summary>
    [Category("Combat")]
    public class DormancyTests
    {
        [Test]
        public void HowFarOutside_ThePicture()
        {
            var view = new Rect(-10f, -5f, 20f, 10f);
            Assert.AreEqual(0f, DormancyWatch.Outside(view, new Vector2(3f, 2f)), 1e-5f, "inside");
            Assert.AreEqual(6f, DormancyWatch.Outside(view, new Vector2(16f, 0f)), 1e-5f, "beyond the right edge");
            Assert.AreEqual(5f, DormancyWatch.Outside(view, new Vector2(13f, 9f)), 1e-4f, "beyond a corner: the straight distance");
        }

        [Test]
        public void ItWakes_BeforeItIsSeen_AndSleepsOnlyWellAway_NoFlickerAtTheEdge()
        {
            Assert.Less(DormancyWatch.WakeMargin, DormancyWatch.SleepMargin, "a gap between waking and sleeping");
            Assert.Greater(DormancyWatch.WakeMargin, 5f, "awake well before it comes into the picture");
            float between = (DormancyWatch.WakeMargin + DormancyWatch.SleepMargin) / 2f;
            Assert.IsTrue(DormancyWatch.ShouldBeAwake(between, asleep: false), "awake: it stays awake in the gap");
            Assert.IsFalse(DormancyWatch.ShouldBeAwake(between, asleep: true), "asleep: it stays asleep in the gap");
            Assert.IsTrue(DormancyWatch.ShouldBeAwake(0f, asleep: true), "in the picture: awake");
            Assert.IsFalse(DormancyWatch.ShouldBeAwake(DormancyWatch.SleepMargin + 1f, asleep: false), "far beyond: asleep");
        }

        [Test]
        public void ThePicture_IsWhatThePerspectiveCameraSees_OfThePlaneOfPlay()
        {
            var go = new GameObject("Cam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 30f;
                cam.aspect = 16f / 9f;
                go.transform.position = new Vector3(5f, 2f, -18f);
                var v = DormancyWatch.View(cam).Value;
                float h = 18f * Mathf.Tan(15f * Mathf.Deg2Rad);
                Assert.AreEqual(5f, v.center.x, 1e-3f);
                Assert.AreEqual(2f, v.center.y, 1e-3f);
                Assert.AreEqual(2f * h, v.height, 1e-3f);
                Assert.AreEqual(2f * h * 16f / 9f, v.width, 1e-3f);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
