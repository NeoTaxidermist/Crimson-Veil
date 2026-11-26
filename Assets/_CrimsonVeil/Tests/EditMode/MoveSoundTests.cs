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
                    Assert.Less(x.Length, SoundSynth.Rate * 1.2f, $"{cue} {v}: short");
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
                double diff = 0;
                for (int i = 0; i < n; i++) diff += Math.Abs(a[i] - b[i]);
                Assert.Greater(diff / n, 0.005, $"{cue}: its second variant is another sound, not the first again");
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

        [Test]
        public void AStep_IsDull_ADash_IsAiry()
        {
            Assert.Less(Crossings(GameSound.Make(Cue.StepRun)), Crossings(GameSound.Make(Cue.Dash)), "a foot thuds, the dash rushes");
            Assert.Less(Crossings(GameSound.Make(Cue.LandHard)), Crossings(GameSound.Make(Cue.Dash)));
            Assert.Less(Crossings(GameSound.Make(Cue.Hit)), Crossings(GameSound.Make(Cue.SwingSword)), "a blow is still duller than the blade");
        }
    }
}
