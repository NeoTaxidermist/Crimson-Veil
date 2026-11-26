using System.Collections.Generic;
using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The sounds made in code, the flash and the hit-stop's lengths.</summary>
    [Category("Combat")]
    public class HitFeelTests
    {
        /// <summary>How often the signal crosses zero per second: a measure of how bright it is.</summary>
        static float Crossings(float[] x)
        {
            int n = 0;
            for (int i = 1; i < x.Length; i++) if ((x[i - 1] < 0f) != (x[i] < 0f)) n++;
            return n / (x.Length / (float)SoundSynth.Rate);
        }

        [Test]
        public void EveryCue_IsMade_Loud_UnclippedAndWithoutAClickAtTheEnd()
        {
            foreach (Cue cue in System.Enum.GetValues(typeof(Cue)))
            {
                var x = GameSound.Make(cue);
                Assert.Greater(x.Length, SoundSynth.Rate / 20, $"{cue}: long enough to hear");
                Assert.Less(x.Length, SoundSynth.Rate * 2f, $"{cue}: short");
                float peak = 0f;
                foreach (var v in x) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.AreEqual(SoundSynth.Peak, peak, 0.01f, $"{cue}: as loud as the rest, never clipping");
                Assert.Less(Mathf.Abs(x[x.Length - 1]), 1e-3f, $"{cue}: it fades out (no click)");
            }
        }

        [Test]
        public void TheSounds_AreTheSameEveryTime()
        {
            CollectionAssert.AreEqual(GameSound.Make(Cue.Hit), GameSound.Make(Cue.Hit));
        }

        [Test]
        public void ABlow_IsDull_ASwing_IsAiry_AndTheyRiseFromTheShieldToTheWhip()
        {
            float hit = Crossings(GameSound.Make(Cue.Hit));
            float hurt = Crossings(GameSound.Make(Cue.Hurt));
            float sword = Crossings(GameSound.Make(Cue.SwingSword));
            Assert.Less(hit, sword, "a blow lands lower than the blade sings through the air (it came out a 5 kHz hiss once)");
            Assert.Less(hurt, sword);
            Assert.Less(Crossings(GameSound.Make(Cue.SwingShield)), Crossings(GameSound.Make(Cue.SwingSpear)));
            Assert.Less(Crossings(GameSound.Make(Cue.SwingSpear)), Crossings(GameSound.Make(Cue.SwingWhip)));
            Assert.Greater(hurt, 150f, "the wound is not all below what a small speaker plays");
        }

        [Test]
        public void TheFlash_IsFullAtOnce_ThenGone()
        {
            Assert.AreEqual(1f, HitFlash.Strength(0f), 1e-5f);
            Assert.Less(HitFlash.Strength(0.5f), 0.3f);
            Assert.AreEqual(0f, HitFlash.Strength(1f));
            Assert.AreEqual(0f, HitFlash.Strength(-0.1f));
        }

        [Test]
        public void TheWorldStandsStill_LongEnoughToSee_AndLongerForAFinisher()
        {
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield })
            {
                float s = CombatFx.HitStopFor(form, false);
                Assert.That(s, Is.InRange(0.065f, 0.15f), form + " (0.055 s it was: not seen)");
                Assert.Greater(CombatFx.HitStopFor(form, true), s * 1.3f, form + ": the finisher holds it longer");
            }
            Assert.Greater(CombatFx.HitStopFor(VeilForm.Spear, false), CombatFx.HitStopFor(VeilForm.Sword, false), "a heavy weapon holds it longer");
        }

        [Test]
        public void EachWeapon_SwingsWithItsOwnSound()
        {
            var seen = new HashSet<Cue>();
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield })
                Assert.IsTrue(seen.Add(GameSound.SwingOf(form)), form.ToString());
        }
    }
}
