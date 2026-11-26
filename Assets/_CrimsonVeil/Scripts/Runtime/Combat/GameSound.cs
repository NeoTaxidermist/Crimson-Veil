using System.Collections;
using System.Collections.Generic;
using CrimsonVeil.Core;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>The game's sounds (made in code by <see cref="SoundSynth"/>).</summary>
    public enum Cue
    {
        // the fight
        SwingSword, SwingSpear, SwingWhip, SwingShield, Hit, HitHeavy, Hurt, Block, Parry, Dissolve, WhipCrack, BowLoose,
        ArrowHit, Absorb,
        // moving
        StepWalk, StepRun, Jump, AirJump, Land, LandHard, Dash, Roll, LedgeGrab, Mantle, WallJump, RopeLatch, RopeLetGo, RopeSwoosh,
    }

    /// <summary>Sounds that run on while something lasts.</summary>
    public enum Loop { Slide, WallSlide, Rope, Glide }

    /// <summary>
    /// Plays the game's sounds: a few voices made on first use (kept across scenes). Each cue has several variants and
    /// one of them is picked at random (never the same twice running), a little higher or lower each time; loops run
    /// while something lasts and fade in and out. As loud as the settings' effects volume (the overall volume is the
    /// listener's). Works without any setup.
    /// </summary>
    public sealed class GameSound : MonoBehaviour
    {
        const int Voices = 14;
        static GameSound instance;
        static readonly Dictionary<(Cue, int), AudioClip> clips = new Dictionary<(Cue, int), AudioClip>();
        static readonly Dictionary<Cue, int> lastVariant = new Dictionary<Cue, int>();
        AudioSource[] voices;
        int nextVoice;
        readonly Dictionary<Loop, AudioSource> loops = new Dictionary<Loop, AudioSource>();
        readonly Dictionary<Loop, float> loopWanted = new Dictionary<Loop, float>();

        /// <summary>The last cue played and how many have been (for tests).</summary>
        public static Cue? Last { get; private set; }
        public static int Played { get; private set; }
        /// <summary>How many times each cue was played (for tests).</summary>
        public static readonly Dictionary<Cue, int> Count = new Dictionary<Cue, int>();

        /// <summary>How loud each cue is relative to the others.</summary>
        public static float Level(Cue cue)
        {
            switch (cue)
            {
                case Cue.SwingSword: case Cue.SwingSpear: case Cue.SwingWhip: case Cue.SwingShield: return 0.4f;
                case Cue.Hit: return 0.85f;
                case Cue.HitHeavy: return 1f;
                case Cue.Hurt: return 1f;
                case Cue.Block: return 0.7f;
                case Cue.Parry: return 0.85f;
                case Cue.Dissolve: return 0.7f;
                case Cue.WhipCrack: return 0.6f;
                case Cue.BowLoose: return 0.5f;
                case Cue.ArrowHit: return 0.7f;
                case Cue.Absorb: return 0.6f;
                case Cue.StepWalk: return 0.22f;
                case Cue.StepRun: return 0.32f;
                case Cue.Jump: return 0.35f;
                case Cue.AirJump: return 0.35f;
                case Cue.Land: return 0.45f;
                case Cue.LandHard: return 0.7f;
                case Cue.Dash: return 0.45f;
                case Cue.Roll: return 0.5f;
                case Cue.LedgeGrab: return 0.5f;
                case Cue.Mantle: return 0.45f;
                case Cue.WallJump: return 0.4f;
                case Cue.RopeLatch: return 0.55f;
                case Cue.RopeLetGo: return 0.35f;
                case Cue.RopeSwoosh: return 0.3f;
                default: return 0.6f;
            }
        }

        public static float Level(Loop loop) => loop == Loop.Slide ? 0.35f : loop == Loop.WallSlide ? 0.3f : loop == Loop.Glide ? 0.18f : 0.35f;

        /// <summary>How many variants a cue has (steps the most: they are heard the most).</summary>
        public static int Variants(Cue cue) => cue == Cue.StepWalk || cue == Cue.StepRun ? 8 : 4;

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

        /// <summary>The samples of a cue's variant.</summary>
        public static float[] Make(Cue cue, int variant = 0)
        {
            int v = variant;
            switch (cue)
            {
                case Cue.SwingSword: return SoundSynth.Swing(1500f, 0.22f, 11, v);
                case Cue.SwingSpear: return SoundSynth.Swing(1100f, 0.26f, 12, v);
                case Cue.SwingWhip: return SoundSynth.Swing(2400f, 0.18f, 13, v);
                case Cue.SwingShield: return SoundSynth.Swing(650f, 0.32f, 14, v);
                case Cue.Hit: return SoundSynth.Hit(1f, 21, v);
                case Cue.HitHeavy: return SoundSynth.Hit(1.7f, 22, v);
                case Cue.Hurt: return SoundSynth.Hurt(31, v);
                case Cue.Block: return SoundSynth.Clank(520f, 1f, 41, v);
                case Cue.Parry: return SoundSynth.Clank(780f, 1.7f, 42, v);
                case Cue.Dissolve: return SoundSynth.Dissolve(51, v);
                case Cue.WhipCrack: return SoundSynth.Crack(61, v);
                case Cue.BowLoose: return SoundSynth.Pluck(196f, 0.4f, 71, v);
                case Cue.ArrowHit: return SoundSynth.Thunk(81, v);
                case Cue.Absorb: return SoundSynth.Swing(420f, 0.35f, 91, v);
                case Cue.StepWalk: return SoundSynth.Step(0.6f, 101, v);
                case Cue.StepRun: return SoundSynth.Step(1f, 102, v);
                case Cue.Jump: return SoundSynth.PushOff(103, v);
                case Cue.AirJump: return SoundSynth.Rush(800f, 0.35f, 104, v);
                case Cue.Land: return SoundSynth.Land(0.8f, 105, v);
                case Cue.LandHard: return SoundSynth.Land(1.5f, 106, v);
                case Cue.Dash: return SoundSynth.Rush(1300f, 0.28f, 107, v);
                case Cue.Roll: return SoundSynth.Roll(108, v);
                case Cue.LedgeGrab: return SoundSynth.Grab(109, v);
                case Cue.Mantle: return SoundSynth.Mantle(110, v);
                case Cue.WallJump: return SoundSynth.PushOff(111, v);
                case Cue.RopeLatch: return SoundSynth.Clank(1400f, 0.5f, 112, v);
                case Cue.RopeLetGo: return SoundSynth.Rush(700f, 0.3f, 113, v);
                case Cue.RopeSwoosh: return SoundSynth.Rush(550f, 0.45f, 114, v);
                default: return SoundSynth.Hit(1f, 1, v);
            }
        }

        /// <summary>The samples of a loop (seamless).</summary>
        public static float[] Make(Loop loop)
        {
            switch (loop)
            {
                case Loop.Slide: return SoundSynth.ScrapeLoop(1400f, 1.2f, 121, 0.8f);
                case Loop.WallSlide: return SoundSynth.ScrapeLoop(800f, 1.2f, 122, 0.6f);
                case Loop.Glide: return SoundSynth.WindLoop(420f, 2.5f, 124);
                default: return SoundSynth.CreakLoop(1.4f, 123);
            }
        }

        static readonly Dictionary<Loop, AudioClip> loopClips = new Dictionary<Loop, AudioClip>();

        /// <summary>Every cue's variants and every loop have been made (for tests).</summary>
        public static bool Warm { get; private set; }

        /// <summary>
        /// Makes every sound ahead, a few each frame (about 3 ms of a frame) from the moment the sounds are first
        /// needed: made on first use, a sound cost a frame's hitch and the garbage of its samples in the middle of play.
        /// </summary>
        public static void WarmUp() { var _ = Instance; }

        IEnumerator MakeAhead()
        {
            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            foreach (Loop l in System.Enum.GetValues(typeof(Loop)))
            {
                LoopClip(l);
                if (clock.ElapsedMilliseconds > 3) { yield return null; clock.Restart(); }
            }
            foreach (Cue c in System.Enum.GetValues(typeof(Cue)))
                for (int v = 0; v < Variants(c); v++)
                {
                    Clip(c, v);
                    if (clock.ElapsedMilliseconds > 3) { yield return null; clock.Restart(); }
                }
            Warm = true;
        }

        static AudioClip LoopClip(Loop loop)
        {
            if (loopClips.TryGetValue(loop, out var c) && c != null) return c;
            c = ToClip("cv_loop_" + loop, Make(loop));
            loopClips[loop] = c;
            return c;
        }

        static AudioClip Clip(Cue cue, int variant)
        {
            if (clips.TryGetValue((cue, variant), out var c) && c != null) return c;
            c = ToClip("cv_" + cue + "_" + variant, Make(cue, variant));
            clips[(cue, variant)] = c;
            return c;
        }

        static AudioClip ToClip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, SoundSynth.Rate, false);
            c.SetData(data, 0);
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
                for (int i = 0; i < Voices; i++) instance.voices[i] = instance.NewSource(false);
                instance.StartCoroutine(instance.MakeAhead());
                return instance;
            }
        }

        AudioSource NewSource(bool loop)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;   // (a side-on game: the fight is heard in front, not placed in space)
            return src;
        }

        /// <summary>A variant other than the one last played, at random.</summary>
        static int PickVariant(Cue cue)
        {
            int n = Variants(cue);
            lastVariant.TryGetValue(cue, out int last);
            int v = Random.Range(0, n);
            if (n > 1 && v == last) v = (v + 1 + Random.Range(0, n - 1)) % n;
            lastVariant[cue] = v;
            return v;
        }

        /// <summary>Plays <paramref name="cue"/> (<paramref name="loudness"/> 0..1 scales it further).</summary>
        public static void Play(Cue cue, float loudness = 1f)
        {
            Last = cue;
            Played++;
            Count.TryGetValue(cue, out int k);
            Count[cue] = k + 1;
            float volume = GameSettings.EffectsLevel * Level(cue) * Mathf.Clamp01(loudness);
            if (volume <= 0.001f) return;
            var self = Instance;
            var src = self.voices[self.nextVoice];
            self.nextVoice = (self.nextVoice + 1) % Voices;
            src.clip = Clip(cue, PickVariant(cue));
            src.volume = volume;
            src.pitch = Random.Range(0.95f, 1.05f);
            src.Play();
        }

        /// <summary>
        /// Keeps a loop going at <paramref name="loudness"/> (0..1; 0 lets it fade out) and <paramref name="pitch"/>.
        /// Call it every frame while the thing lasts.
        /// </summary>
        public static void SetLoop(Loop loop, float loudness, float pitch = 1f)
        {
            if (loudness <= 0f && instance == null) return;
            var self = Instance;
            if (!self.loops.TryGetValue(loop, out var src))
            {
                src = self.NewSource(true);
                src.clip = LoopClip(loop);
                src.volume = 0f;
                self.loops[loop] = src;
            }
            self.loopWanted[loop] = Mathf.Clamp01(loudness) * Level(loop) * GameSettings.EffectsLevel;
            src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            if (loudness > 0f && !src.isPlaying)
            {
                src.time = Random.Range(0f, src.clip.length * 0.9f);   // (never from the same place)
                src.Play();
            }
        }

        /// <summary>How loud a loop is now (for tests).</summary>
        public static float LoopLevel(Loop loop) => instance != null && instance.loops.TryGetValue(loop, out var s) && s.isPlaying ? s.volume : 0f;

        void Update()
        {
            foreach (var kv in loops)
            {
                loopWanted.TryGetValue(kv.Key, out float want);
                var src = kv.Value;
                // (fade in fast, out a little slower: a scrape stops with the slide, not with a click)
                float rate = want > src.volume ? 8f : 5f;
                src.volume = Mathf.MoveTowards(src.volume, want, rate * Time.unscaledDeltaTime);
                if (src.volume <= 0.001f && want <= 0f && src.isPlaying) src.Stop();
                loopWanted[kv.Key] = 0f;   // (kept only while someone keeps asking for it)
            }
        }

        /// <summary>Tests: forget what was played.</summary>
        public static void ResetCount() { Last = null; Played = 0; Count.Clear(); }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
