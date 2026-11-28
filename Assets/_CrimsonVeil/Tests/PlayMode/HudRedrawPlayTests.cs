using System.Collections;
using CrimsonVeil.UI;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The HUD costs nothing it need not: the scarf at the bottom flutters, redrawn some thirty times a second (every
    /// frame, the old gauge re-batched the whole HUD each frame - 0.5 ms of a 4 ms frame), a change at once; the life
    /// rings are redrawn only when life changes; the red edges are not drawn while they are clear. And the scarf marks
    /// where each weapon's cost lies - nearer its start on a longer scarf.
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
        public IEnumerator TheScarf_FluttersThirtyTimesASecond_TheRingsOnlyOnAChange()
        {
            var scarf = hud.Energy;
            var rings = hud.Life;
            yield return Wait(0.3f);
            int before = scarf.Redraws, ringsBefore = rings.Redraws, frames = 0;
            float end = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < end) { yield return null; frames++; }
            int redraws = scarf.Redraws - before;
            Debug.Log($"[hud] 1 s: {frames} frames, the scarf redrawn {redraws} times, the rings {rings.Redraws - ringsBefore}");
            Assert.That(redraws, Is.InRange(15, 40), "its flutter some thirty times a second");
            if (frames > 60) Assert.Less(redraws, frames * 0.75f, "not every frame");
            Assert.LessOrEqual(rings.Redraws - ringsBefore, 2, "the rings stand still while life does");

            int was = rings.Redraws;
            rings.health -= 1f;
            yield return null;
            yield return null;
            Assert.Greater(rings.Redraws, was, "a lost point of life is drawn at once");
            Assert.IsNotNull(scarf.GetComponent<Canvas>(), "the scarf on a canvas of its own: its redraw re-batches only itself");
            Assert.IsNotNull(rings.GetComponent<Canvas>());
        }

        [Test]
        public void TheScarf_MarksTheWeaponsCosts_NearerOnALongerScarf()
        {
            float bow = EnergyScarf.NotchAt(VeilForm.Bow, 1f);
            Assert.AreEqual(VeilCasting.Cost(VeilForm.Bow), bow, 1e-4f, "the bow's notch at its cost");
            Assert.Less(EnergyScarf.NotchAt(VeilForm.Bow, 1.5f), bow, "a longer scarf: the same cost is a smaller share of it");
            float last = 0f;
            foreach (var f in EnergyScarf.Marked) { float u = EnergyScarf.NotchAt(f, 1f); Assert.GreaterOrEqual(u, last); last = u; }
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
