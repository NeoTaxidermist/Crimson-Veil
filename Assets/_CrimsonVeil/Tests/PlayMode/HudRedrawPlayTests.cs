using System.Collections;
using CrimsonVeil.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The HUD costs nothing it need not: the life gauge's flutter is redrawn some thirty times a second (every frame,
    /// it re-batched the whole HUD each frame - 0.5 ms of a 4 ms frame), a change of life at once; the red edges are not
    /// drawn while they are clear.
    /// </summary>
    [Category("UI")]
    public class HudRedrawPlayTests
    {
        ScarfHud hud;

        [SetUp]
        public void SetUp() => hud = ScarfHud.Ensure();

        [TearDown]
        public void TearDown() { if (hud != null) Object.DestroyImmediate(hud.gameObject); }

        static IEnumerator Wait(float s)
        {
            float end = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator TheGauge_FluttersThirtyTimesASecond_AndShowsALossAtOnce()
        {
            var bar = hud.Bar;
            yield return Wait(0.3f);
            int before = bar.Redraws, frames = 0;
            float end = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < end) { yield return null; frames++; }
            int redraws = bar.Redraws - before;
            Debug.Log($"[hud] 1 s: {frames} frames, the gauge redrawn {redraws} times");
            Assert.That(redraws, Is.InRange(15, 40), "its flutter some thirty times a second");
            if (frames > 60) Assert.Less(redraws, frames * 0.75f, "not every frame");

            yield return null;
            int was = bar.Redraws;
            bar.health -= 1f;
            yield return null;
            yield return null;
            Assert.Greater(bar.Redraws, was, "a lost point of life is drawn at once, not at the next flutter");
            Assert.IsNotNull(bar.GetComponent<Canvas>(), "on a canvas of its own: its redraw re-batches only itself");
        }

        [UnityTest]
        public IEnumerator TheRedEdges_AreNotDrawn_WhileClear()
        {
            var image = hud.GetComponentInChildren<HurtVignette>(true).GetComponent<RawImage>();
            yield return null;
            Assert.IsFalse(image.enabled, "clear: not drawn at all");
            HurtVignette.Pulse(1f);
            yield return null;
            Assert.IsTrue(image.enabled, "struck: the edges run red");
            yield return Wait(HurtVignette.Seconds + 0.2f);
            Assert.IsFalse(image.enabled, "and when the red has ebbed, not drawn again");
        }
    }
}
