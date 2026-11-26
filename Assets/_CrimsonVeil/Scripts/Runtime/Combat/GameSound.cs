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
        StepWalk, StepRun, Jump, AirJump, Land, LandHard, Dash, Roll, LedgeGrab, Mantle, WallJump, RopeLatch, RopeLetGo, RopeSwoosh, RopeCreak,
    }

    /// <summary>Sounds that run on while something lasts.</summary>
    public enum Loop { Slide, WallSlide, Glide, Ambience }

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
        static readonly Dictionary<(Cue, int), float> gains = new Dictionary<(Cue, int), float>();
        static readonly Dictionary<Loop, float> loopGains = new Dictionary<Loop, float>();
        static readonly Dictionary<Cue, int> lastVariant = new Dictionary<Cue, int>();
        AudioSource[] voices;
        Cue[] voiceCue;
        float[] voiceFade;
        int nextVoice;
        readonly Dictionary<Loop, AudioSource> loops = new Dictionary<Loop, AudioSource>();
        readonly Dictionary<Loop, float> loopWanted = new Dictionary<Loop, float>();

        /// <summary>The last cue played and how many have been (for tests).</summary>
        public static Cue? Last { get; private set; }
        public static int Played { get; private set; }
        /// <summary>How many times each cue was played (for tests).</summary>
        public static readonly Dictionary<Cue, int> Count = new Dictionary<Cue, int>();

        /// <summary>
        /// How loud each cue is heard (LUFS at full volume): the mix. Measured loudness, not peak - a step peaks as high
        /// as a blow. The fight sits around -23, the hero's moving well under it (steps -39 to -36, as in a film, where
        /// they are felt more than heard), nothing near the -14 the first sets were played at.
        /// </summary>
        public static float Level(Cue cue) => BaseLevel(cue) + MixTrim;

        /// <summary>
        /// The whole mix taken down by this (dB): at full volume it was too loud (the user, after v0.93). The balance
        /// between the sounds is <see cref="BaseLevel"/>'s.
        /// </summary>
        public const float MixTrim = -4f;

        static float BaseLevel(Cue cue)
        {
            switch (cue)
            {
                case Cue.SwingSword: case Cue.SwingSpear: case Cue.SwingShield: return -30f;
                case Cue.SwingWhip: return -31f;
                case Cue.Hit: return -24f;
                case Cue.HitHeavy: return -22f;
                case Cue.Hurt: return -22f;
                case Cue.Block: return -25f;
                case Cue.Parry: return -24f;
                case Cue.Dissolve: return -27f;
                case Cue.WhipCrack: return -30f;   // (a crack is all peak: heard well at this)
                case Cue.BowLoose: return -29f;
                case Cue.ArrowHit: return -27f;
                case Cue.Absorb: return -29f;
                case Cue.StepWalk: return -39f;
                case Cue.StepRun: return -36f;
                case Cue.Jump: return -37f;
                case Cue.AirJump: return -34f;
                case Cue.Land: return -34f;
                case Cue.LandHard: return -30f;
                case Cue.Dash: return -32f;
                case Cue.Roll: return -33f;
                case Cue.LedgeGrab: return -34f;
                case Cue.Mantle: return -35f;
                case Cue.WallJump: return -35f;
                case Cue.RopeLatch: return -32f;
                case Cue.RopeLetGo: return -36f;
                case Cue.RopeSwoosh: return -35f;
                case Cue.RopeCreak: return -42f;   // (under the rush: louder, the swing read less as swinging)
                default: return -30f;
            }
        }

        /// <summary>How loud each loop is heard at its fullest (LUFS).</summary>
        public static float Level(Loop loop) => MixTrim + (loop == Loop.Slide ? -33f : loop == Loop.WallSlide ? -35f : loop == Loop.Glide ? -40f : -46f);

        /// <summary>The volume that plays <paramref name="samples"/> at <paramref name="lufs"/> (never past full scale).</summary>
        public static float GainFor(float[] samples, float lufs)
        {
            float g = Mathf.Pow(10f, (lufs - SoundSynth.Loudness(samples)) / 20f);
            float peak = 0f;
            foreach (var v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));
            return peak > 0f ? Mathf.Min(g, 0.9f / peak) : g;
        }

        /// <summary>How many variants a cue has (steps the most: they are heard the most).</summary>
        public static int Variants(Cue cue) => cue == Cue.StepWalk || cue == Cue.StepRun ? 8 : 4;

        /// <summary>
        /// When (attack progress 0..1) a weapon's swing is heard: so that the whoosh swells as the blade is SEEN to cut
        /// the air. The blade is cloth and lags the arm - measured on the hero, the sword's tip is fastest 0.2 s after
        /// the press, 0.1 s after the whoosh played from the press had peaked.
        /// </summary>
        public static float SwingSoundAt(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return 0.2f;
                case VeilForm.Spear: return 0.12f;
                case VeilForm.Whip: return 0.28f;
                case VeilForm.Shield: return 0.03f;
                default: return 0f;
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
                case Cue.RopeLatch: return SoundSynth.Latch(112, v);
                case Cue.RopeLetGo: return SoundSynth.Rush(700f, 0.3f, 113, v);
                case Cue.RopeSwoosh: return SoundSynth.Rush(550f, 0.45f, 114, v);
                case Cue.RopeCreak: return SoundSynth.Creak(115, v);
                default: return SoundSynth.Hit(1f, 1, v);
            }
        }

        /// <summary>The samples of a loop (seamless).</summary>
        public static float[] Make(Loop loop)
        {
            switch (loop)
            {
                case Loop.Slide: return SoundSynth.ScrapeLoop(750f, 1.6f, 121, 0.9f);
                case Loop.WallSlide: return SoundSynth.ScrapeLoop(600f, 1.6f, 122, 0.7f);
                case Loop.Glide: return SoundSynth.WindLoop(420f, 2.5f, 124);
                default: return SoundSynth.AmbienceLoop(12f, 125);
            }
        }

        static readonly Dictionary<Loop, AudioClip> loopClips = new Dictionary<Loop, AudioClip>();

        /// <summary>Every cue's variants and every loop have been made (for tests).</summary>
        public static bool Warm { get; private set; }

        /// <summary>
        /// Makes every sound ahead, from the moment the sounds are first needed: the samples on a worker thread (the
        /// synthesis is plain arithmetic; the ruin's 12 s alone take over half a second), the clips from them on the
        /// main thread, a few a frame. Made on first use, a sound cost a hitch and its garbage in the middle of play.
        /// </summary>
        public static void WarmUp() { var _ = Instance; }

        readonly struct Made
        {
            public readonly bool isLoop; public readonly Cue cue; public readonly int variant; public readonly Loop loop;
            public readonly float[] samples; public readonly float gain;
            public Made(bool isLoop, Cue cue, int variant, Loop loop, float[] samples, float gain)
            { this.isLoop = isLoop; this.cue = cue; this.variant = variant; this.loop = loop; this.samples = samples; this.gain = gain; }
        }

        IEnumerator MakeAhead()
        {
            var made = new System.Collections.Concurrent.ConcurrentQueue<Made>();
            var work = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // (the ruin first: it is heard the moment he is in it; then what is heard most)
                    foreach (Loop l in new[] { Loop.Ambience, Loop.Slide, Loop.WallSlide, Loop.Glide })
                    {
                        var x = Make(l);
                        made.Enqueue(new Made(true, default, 0, l, x, GainFor(x, Level(l))));
                    }
                    foreach (Cue c in System.Enum.GetValues(typeof(Cue)))
                        for (int v = 0; v < Variants(c); v++)
                        {
                            var x = Make(c, v);
                            made.Enqueue(new Made(false, c, v, default, x, GainFor(x, Level(c))));
                        }
                }
                catch (System.Exception e) { Debug.LogException(e); }
            });
            var clock = new System.Diagnostics.Stopwatch();
            while (true)
            {
                clock.Restart();
                while (clock.ElapsedMilliseconds < 2 && made.TryDequeue(out var m))
                {
                    if (m.isLoop)
                    {
                        if (!loopClips.ContainsKey(m.loop)) { loopClips[m.loop] = ToClip("cv_loop_" + m.loop, m.samples); loopGains[m.loop] = m.gain; }
                    }
                    else if (!clips.ContainsKey((m.cue, m.variant)))
                    {
                        clips[(m.cue, m.variant)] = ToClip("cv_" + m.cue + "_" + m.variant, m.samples);
                        gains[(m.cue, m.variant)] = m.gain;
                    }
                }
                if (work.IsCompleted && made.IsEmpty) break;
                yield return null;
            }
            if (work.IsFaulted) Debug.LogException(work.Exception);
            Warm = true;
        }

        static AudioClip LoopClip(Loop loop)
        {
            if (loopClips.TryGetValue(loop, out var c) && c != null) return c;
            var x = Make(loop);   // (not made ahead yet: made now)
            c = ToClip("cv_loop_" + loop, x);
            loopClips[loop] = c;
            loopGains[loop] = GainFor(x, Level(loop));
            return c;
        }

        static AudioClip Clip(Cue cue, int variant)
        {
            if (clips.TryGetValue((cue, variant), out var c) && c != null) return c;
            var x = Make(cue, variant);   // (not made ahead yet: made now)
            c = ToClip("cv_" + cue + "_" + variant, x);
            clips[(cue, variant)] = c;
            gains[(cue, variant)] = GainFor(x, Level(cue));
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
                instance.voiceCue = new Cue[Voices];
                instance.voiceFade = new float[Voices];
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
            float volume = GameSettings.EffectsLevel * Mathf.Clamp01(loudness);
            if (volume <= 0.001f) return;
            var self = Instance;
            var src = self.voices[self.nextVoice];
            self.nextVoice = (self.nextVoice + 1) % Voices;
            int v = PickVariant(cue);
            self.voiceCue[self.nextVoice == 0 ? Voices - 1 : self.nextVoice - 1] = cue;
            self.voiceFade[self.nextVoice == 0 ? Voices - 1 : self.nextVoice - 1] = 0f;
            src.clip = Clip(cue, v);
            // (a little louder or softer each time, within 1.5 dB: no two steps alike)
            src.volume = Mathf.Min(1f, volume * gains[(cue, v)] * Random.Range(0.84f, 1f));
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
            self.loopWanted[loop] = Mathf.Min(1f, Mathf.Clamp01(loudness) * loopGains[loop] * GameSettings.EffectsLevel);
            src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            if (loudness > 0f && !src.isPlaying)
            {
                src.time = Random.Range(0f, src.clip.length * 0.9f);   // (never from the same place)
                src.Play();
            }
        }

        /// <summary>
        /// Lets <paramref name="cue"/> die away within <paramref name="seconds"/> wherever it is playing: a blade that
        /// strikes something stops cutting the air (its whoosh ran on under the blow, as if pasted on).
        /// </summary>
        public static void Cut(Cue cue, float seconds = 0.05f)
        {
            if (instance == null) return;
            for (int i = 0; i < Voices; i++)
                if (instance.voiceCue[i] == cue && instance.voices[i].isPlaying)
                    instance.voiceFade[i] = instance.voices[i].volume / Mathf.Max(0.005f, seconds);
        }

        /// <summary>Is <paramref name="cue"/> sounding now, and how loud (for tests).</summary>
        public static float Sounding(Cue cue)
        {
            if (instance == null) return 0f;
            float v = 0f;
            for (int i = 0; i < Voices; i++)
                if (instance.voiceCue[i] == cue && instance.voices[i].isPlaying) v = Mathf.Max(v, instance.voices[i].volume);
            return v;
        }

        /// <summary>How loud a loop is now (for tests).</summary>
        public static float LoopLevel(Loop loop) => instance != null && instance.loops.TryGetValue(loop, out var s) && s.isPlaying ? s.volume : 0f;

        void Update()
        {
            for (int i = 0; i < Voices; i++)
            {
                if (voiceFade[i] <= 0f) continue;
                var s = voices[i];
                s.volume = Mathf.MoveTowards(s.volume, 0f, voiceFade[i] * Time.unscaledDeltaTime);
                if (s.volume <= 0.0005f) { s.Stop(); voiceFade[i] = 0f; }
            }
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
