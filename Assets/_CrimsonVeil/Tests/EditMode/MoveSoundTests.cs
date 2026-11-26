using System;
using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The sounds are less made: each cue comes in variants that differ, each ends in the room's tail without a click,
    /// the loops run on without a seam, and the hero's moving picks the right sound for what he does.
    /// </summary>
    [Category("Combat")]
    [Category("Hero")]
    public class MoveSoundTests
    {
        static float Crossings(float[] x)
        {
            int n = 0;
            for (int i = 1; i < x.Length; i++) if ((x[i - 1] < 0f) != (x[i] < 0f)) n++;
            return n / (x.Length / (float)SoundSynth.Rate);
        }

        static float Peak(float[] x)
        {
            float m = 0f;
            foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v));
            return m;
        }

        [Test]
        public void EveryVariantOfEveryCue_IsMade_Loud_UnclippedAndFadedOut()
        {
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
                for (int v = 0; v < GameSound.Variants(cue); v++)
                {
                    var x = GameSound.Make(cue, v);
                    Assert.Greater(x.Length, SoundSynth.Rate / 20, $"{cue} {v}: long enough to hear");
                    Assert.Less(x.Length, SoundSynth.Rate * 2f, $"{cue} {v}: short (with the room)");
                    Assert.AreEqual(SoundSynth.Peak, Peak(x), 0.01f, $"{cue} {v}: as loud as the rest, never clipping");
                    Assert.Less(Mathf.Abs(x[x.Length - 1]), 1e-3f, $"{cue} {v}: it fades out (no click)");
                }
        }

        [Test]
        public void TheVariants_AreNotTheSameSound()
        {
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
            {
                var a = GameSound.Make(cue, 0);
                var b = GameSound.Make(cue, 1);
                int n = Mathf.Min(a.Length, b.Length);
                double diff = 0, level = 0;
                for (int i = 0; i < n; i++) { diff += Math.Abs(a[i] - b[i]); level += Math.Abs(a[i]); }
                // (relative to how loud it is: a crack is nearly all silence)
                Assert.Greater(diff / level, 0.3, $"{cue}: its second variant is another sound, not the first again");
                CollectionAssert.AreEqual(a, GameSound.Make(cue, 0), $"{cue}: and each variant is the same every time");
            }
        }

        [Test]
        public void EveryLoop_RunsOnWithoutASeam()
        {
            foreach (Loop loop in Enum.GetValues(typeof(Loop)))
            {
                var x = GameSound.Make(loop);
                Assert.Greater(x.Length, SoundSynth.Rate, $"{loop}: at least a second before it repeats");
                Assert.AreEqual(SoundSynth.Peak, Peak(x), 0.01f, loop.ToString());
                float typical = 0f, largest = 0f;
                for (int i = 1; i < x.Length; i++)
                {
                    float d = Mathf.Abs(x[i] - x[i - 1]);
                    typical += d;
                    largest = Mathf.Max(largest, d);
                }
                typical /= x.Length;
                float seam = Mathf.Abs(x[0] - x[x.Length - 1]);
                Assert.Less(seam, Mathf.Max(typical * 4f, largest * 0.5f), $"{loop}: the end runs into the start as any sample into the next");
            }
        }

        [Test]
        public void ARunningStep_IsARunningOne_AndLouderThanAWalk()
        {
            var walk = HeroSounds.StepAt(1.5f);
            var run = HeroSounds.StepAt(7f);
            Assert.AreEqual(Cue.StepWalk, walk.cue);
            Assert.AreEqual(Cue.StepRun, run.cue);
            Assert.Greater(run.loudness, walk.loudness);
            Assert.Greater(GameSound.Level(Cue.StepRun), GameSound.Level(Cue.StepWalk));
            Assert.Less(GameSound.Level(Cue.StepRun), GameSound.Level(Cue.Hit), "steps stay under the fight");
            Assert.GreaterOrEqual(GameSound.Variants(Cue.StepRun), 6, "the sound heard the most has the most variants");
        }

        [Test]
        public void AFallFromHigher_LandsHarder()
        {
            var hop = HeroSounds.LandingFrom(0.8f);
            var drop = HeroSounds.LandingFrom(5f);
            Assert.AreEqual(Cue.Land, hop.cue);
            Assert.AreEqual(Cue.LandHard, drop.cue);
            Assert.Greater(drop.loudness, hop.loudness);
        }

        /// <summary>
        /// The mix: each sound is played at its measured loudness, the fight around -23 LUFS, the moving well under it,
        /// the ruin's air under all (the first sets were played at about -14, every one at its full peak: loud, harsh).
        /// </summary>
        [Test]
        public void AsPlayed_TheFightIsModerate_TheMovingQuieter_TheRuinQuietest()
        {
            float Played(Cue c, int v)
            {
                var x = GameSound.Make(c, v);
                float g = GameSound.GainFor(x, GameSound.Level(c));
                return SoundSynth.Loudness(x) + 20f * Mathf.Log10(Mathf.Min(1f, g));
            }
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                float l = Played(c, 0);
                Assert.That(l, Is.InRange(GameSound.Level(c) - 3f, GameSound.Level(c) + 0.5f), $"{c}: played as loud as the mix sets it");
                Assert.Less(l, -20f, $"{c}: nothing blares");
            }
            Assert.Less(Played(Cue.StepRun, 0), Played(Cue.Hit, 0) - 8f, "a step is felt more than heard, well under a blow");
            Assert.Less(GameSound.Level(Loop.Ambience), GameSound.Level(Cue.StepWalk) - 4f, "the ruin's air is under even a step");
        }

        [Test]
        public void TheLoudnessMeasure_ReadsALouderSoundLouder_By20dBForTenTimes()
        {
            var x = GameSound.Make(Loop.Slide);   // (steady: a fading sound drops under the gate when ten times quieter)
            var quiet = new float[x.Length];
            for (int i = 0; i < x.Length; i++) quiet[i] = x[i] * 0.1f;
            Assert.AreEqual(SoundSynth.Loudness(x) - 20f, SoundSynth.Loudness(quiet), 0.1f);
        }

        [Test]
        public void AStep_IsDull_ADash_IsAiry()
        {
            Assert.Less(Crossings(GameSound.Make(Cue.StepRun)), Crossings(GameSound.Make(Cue.Dash)), "a foot thuds, the dash rushes");
            Assert.Less(Crossings(GameSound.Make(Cue.LandHard)), Crossings(GameSound.Make(Cue.Dash)));
            Assert.Less(Crossings(GameSound.Make(Cue.Hit)), Crossings(GameSound.Make(Cue.SwingSword)), "a blow is still duller than the blade");
        }
    }
}
