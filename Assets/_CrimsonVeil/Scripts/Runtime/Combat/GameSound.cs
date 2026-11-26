using System.Collections.Generic;
using CrimsonVeil.Core;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>The fight's sounds (made in code by <see cref="SoundSynth"/>).</summary>
    public enum Cue
    {
        SwingSword, SwingSpear, SwingWhip, SwingShield, Hit, HitHeavy, Hurt, Block, Parry, Dissolve, WhipCrack, BowLoose,
        ArrowHit, Absorb,
    }

    /// <summary>
    /// Plays the fight's sounds: a few voices made on first use (kept across scenes), each cue a little different
    /// every time it is played (pitch and level). Loud as the settings' effects volume (the overall volume is the
    /// listener's). Works without any setup; silent where there is no audio.
    /// </summary>
    public sealed class GameSound : MonoBehaviour
    {
        const int Voices = 10;
        static GameSound instance;
        static readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        AudioSource[] voices;
        int nextVoice;

        /// <summary>The last cue played and how many have been (for tests).</summary>
        public static Cue? Last { get; private set; }
        public static int Played { get; private set; }

        /// <summary>How loud each cue is relative to the others.</summary>
        public static float Level(Cue cue)
        {
            switch (cue)
            {
                case Cue.SwingSword: case Cue.SwingSpear: case Cue.SwingWhip: case Cue.SwingShield: return 0.35f;
                case Cue.Hit: return 0.8f;
                case Cue.HitHeavy: return 1f;
                case Cue.Hurt: return 1f;
                case Cue.Block: return 0.7f;
                case Cue.Parry: return 0.85f;
                case Cue.Dissolve: return 0.7f;
                case Cue.WhipCrack: return 0.6f;
                case Cue.BowLoose: return 0.5f;
                case Cue.ArrowHit: return 0.7f;
                case Cue.Absorb: return 0.6f;
                default: return 0.7f;
            }
        }

        /// <summary>The swing of each weapon.</summary>
        public static Cue SwingOf(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Spear: return Cue.SwingSpear;
                case VeilForm.Whip: return Cue.SwingWhip;
                case VeilForm.Shield: return Cue.SwingShield;
                default: return Cue.SwingSword;
            }
        }

        /// <summary>The samples of a cue.</summary>
        public static float[] Make(Cue cue)
        {
            switch (cue)
            {
                case Cue.SwingSword: return SoundSynth.Swing(1500f, 0.2f, 11);
                case Cue.SwingSpear: return SoundSynth.Swing(1100f, 0.24f, 12);
                case Cue.SwingWhip: return SoundSynth.Swing(2400f, 0.16f, 13);
                case Cue.SwingShield: return SoundSynth.Swing(650f, 0.3f, 14);
                case Cue.Hit: return SoundSynth.Hit(1f, 21);
                case Cue.HitHeavy: return SoundSynth.Hit(1.7f, 22);
                case Cue.Hurt: return SoundSynth.Hurt(31);
                case Cue.Block: return SoundSynth.Clank(520f, 1f, 41);
                case Cue.Parry: return SoundSynth.Clank(780f, 1.8f, 42);
                case Cue.Dissolve: return SoundSynth.Dissolve(51);
                case Cue.WhipCrack: return SoundSynth.Crack(61);
                case Cue.BowLoose: return SoundSynth.Pluck(196f, 0.4f, 71);
                case Cue.ArrowHit: return SoundSynth.Thunk(81);
                case Cue.Absorb: return SoundSynth.Swing(420f, 0.35f, 91);
                default: return SoundSynth.Hit(1f, 1);
            }
        }

        static AudioClip Clip(Cue cue)
        {
            if (clips.TryGetValue(cue, out var c) && c != null) return c;
            var data = Make(cue);
            c = AudioClip.Create("cv_" + cue, data.Length, 1, SoundSynth.Rate, false);
            c.SetData(data, 0);
            clips[cue] = c;
            return c;
        }

        static GameSound Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("GameSound");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<GameSound>();
                instance.voices = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    var src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.spatialBlend = 0f;   // (a side-on game: the fight is heard in front, not placed in space)
                    instance.voices[i] = src;
                }
                return instance;
            }
        }

        /// <summary>Plays <paramref name="cue"/> (<paramref name="loudness"/> 0..1 scales it further).</summary>
        public static void Play(Cue cue, float loudness = 1f)
        {
            Last = cue;
            Played++;
            float volume = GameSettings.EffectsLevel * Level(cue) * Mathf.Clamp01(loudness);
            if (volume <= 0.001f) return;
            var self = Instance;
            var src = self.voices[self.nextVoice];
            self.nextVoice = (self.nextVoice + 1) % Voices;
            src.clip = Clip(cue);
            src.volume = volume;
            src.pitch = Random.Range(0.94f, 1.06f);   // (never twice the same)
            src.Play();
        }

        /// <summary>Tests: forget what was played.</summary>
        public static void ResetCount() { Last = null; Played = 0; }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
