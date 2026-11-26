using System;
using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The game's sounds, made in code (no recordings), the way a sound is made in the world: something strikes
    /// something (a short burst of noise, softer or sharper) and the struck thing rings in its own modes - stone
    /// briefly and high, a body low and dull, wood hollow, metal long - and the stone ruin answers with its echo. Air
    /// is pink noise through a moving band, never a periodic flapping; nothing is driven into distortion. Each sound
    /// has variants (<c>variant</c>: the same kind of sound, never the same one twice). Mono buffers, deterministic,
    /// peaking at <see cref="Peak"/>; how loud each is played is set by its measured loudness (<see cref="Loudness"/>).
    /// <para>(The first sets thumped with a falling sine - a drum machine's kick, which a listening model heard in the
    /// steps and landings with 0.9-0.99 certainty - were saturated and all equally loud.)</para>
    /// </summary>
    public static class SoundSynth
    {
        public const int Rate = 44100;
        /// <summary>Every sound is normalised to this peak (its loudness in play is set separately).</summary>
        public const float Peak = 0.8f;

        // ===================================================================================== materials

        /// <summary>A struck thing's resonant modes: frequency (Hz), decay time (s), amplitude.</summary>
        struct Modes
        {
            public float[] f, tau, amp;
        }

        /// <summary><paramref name="count"/> modes spread (log) over <paramref name="lowHz"/>..<paramref name="highHz"/>,
        /// decaying in <paramref name="longTau"/> at the bottom down to <paramref name="shortTau"/> at the top.</summary>
        static Modes Body(System.Random r, int count, float lowHz, float highHz, float longTau, float shortTau, float tilt)
        {
            var m = new Modes { f = new float[count], tau = new float[count], amp = new float[count] };
            for (int k = 0; k < count; k++)
            {
                float u = (k + (float)r.NextDouble()) / count;
                m.f[k] = lowHz * Mathf.Pow(highHz / lowHz, u);
                m.tau[k] = Mathf.Lerp(longTau, shortTau, u) * Vary(r, 0.3f);
                m.amp[k] = Mathf.Pow(1f - 0.85f * u, tilt) * Vary(r, 0.35f);
            }
            return m;
        }

        static Modes Stone(System.Random r) => Body(r, 8, 400f, 4200f, 0.012f, 0.003f, 0.9f);
        static Modes Flesh(System.Random r, float weight) => Body(r, 6, 150f / Mathf.Sqrt(weight), 1100f / Mathf.Sqrt(weight), 0.03f * weight, 0.006f, 0.8f);
        static Modes Wood(System.Random r) => Body(r, 7, 320f, 2600f, 0.035f, 0.008f, 1f);
        static Modes Skin(System.Random r) => Body(r, 6, 450f, 2400f, 0.008f, 0.003f, 0.7f);

        /// <summary>
        /// Something striking something at <paramref name="at"/> s: a burst of noise (rising in
        /// <paramref name="attack"/> s, gone in about <paramref name="decay"/> s, no brighter than
        /// <paramref name="brightHz"/> - soft things strike dull) rings the <paramref name="modes"/>; a little of the
        /// burst itself is heard too (<paramref name="direct"/>).
        /// </summary>
        static void Strike(float[] o, float at, System.Random r, Modes modes, float gain, float brightHz, float attack, float decay, float direct = 0.25f)
        {
            int start = Mathf.Max(0, (int)(at * Rate));
            int n = o.Length - start;
            if (n <= 0) return;
            int burstLen = Mathf.Min(n, Len(attack + decay * 6f));
            var exc = new float[n];
            var l1 = new Biquad(); var l2 = new Biquad();
            l1.LowPass(brightHz, 0.7f); l2.LowPass(brightHz, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float env = i < burstLen ? Mathf.Clamp01(s / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-Mathf.Max(0f, s - attack) / decay) : 0f;
                float w = i < burstLen ? (float)(r.NextDouble() * 2.0 - 1.0) * env : 0f;
                exc[i] = l2.Step(l1.Step(w));
            }
            var ring = new float[n];
            for (int k = 0; k < modes.f.Length; k++)
            {
                var bp = new Biquad();
                float q = Mathf.Max(0.7f, Mathf.PI * modes.f[k] * modes.tau[k]);
                bp.BandPass(modes.f[k], q);
                // (a narrow mode takes less of a broad burst: given back, so that each mode rings as loud as asked)
                float g = modes.amp[k] * Mathf.Sqrt(q);
                for (int i = 0; i < n; i++) ring[i] += bp.Step(exc[i]) * g;
            }
            float rm = PeakOf(ring), em = PeakOf(exc);
            float rk = rm > 1e-9f ? 1f / rm : 0f, ek = em > 1e-9f ? 1f / em : 0f;
            for (int i = 0; i < n; i++) o[start + i] += gain * (ring[i] * rk + exc[i] * ek * direct);
        }

        // ===================================================================================== the fight

        /// <summary>
        /// A blade of ink and cloth cutting the air: pink noise through a band that rises with the blade's speed and
        /// falls as it slows, roughened by turbulence, and the cloth it is made of rustling under it.
        /// </summary>
        public static float[] Swing(float centreHz, float seconds, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float c0 = centreHz * Vary(r, 0.12f);
            float len = seconds * Vary(r, 0.15f);
            float peakAt = 0.4f + 0.2f * (float)r.NextDouble();
            int n = Len(len);
            var o = new float[n];
            var air = Pink(n, r);
            var cloth = Pink(n, r);
            var turb = Wobble(n, r, 30f);
            var rustle = Wobble(n, r, 70f);
            var b1 = new Biquad(); var b2 = new Biquad(); var cl = new Biquad();
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float speed = Bell(t, peakAt);
                float c = c0 * (0.5f + 0.8f * speed);
                if ((i & 31) == 0) { b1.BandPass(c, 0.9f); b2.BandPass(c * 1.15f, 0.9f); cl.LowPass(c * 0.45f, 0.7f); }
                float a = b2.Step(b1.Step(air[i])) * speed * Mathf.Sqrt(speed) * (0.75f + 0.35f * turb[i]);
                float f = cl.Step(cloth[i]) * speed * speed * Mathf.Abs(rustle[i]) * 0.45f;
                o[i] = a + f;
            }
            return Finish(o, 0.12f, 0.6f, r);
        }

        /// <summary>
        /// A blow landing in an ink body: the edge's short slice, the body struck (low, dull, damped), the wet give of
        /// the ink and a few drops falling after it. <paramref name="weight"/> 1 a plain hit, more a heavier one.
        /// </summary>
        public static float[] Hit(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float w = Mathf.Max(0.5f, weight) * Vary(r, 0.08f);
            int n = Len(0.3f * w);
            var o = new float[n];
            // the slice: a very short, high breath of the edge going in
            var slice = Pink(Len(0.012f), r);
            var h1 = new Biquad(); h1.HighPass(2800f * Vary(r, 0.15f), 0.7f);
            for (int i = 0; i < slice.Length; i++) o[i] += h1.Step(slice[i]) * Mathf.Exp(-i / (Rate * 0.003f)) * 0.5f;
            // the body struck
            Strike(o, 0.002f, r, Flesh(r, w), 0.55f, 2600f / Mathf.Sqrt(w), 0.001f, 0.003f * w, 0.35f);
            Strike(o, 0.003f, r, Skin(r), 0.8f, 4500f, 0.0005f, 0.0012f, 0.35f);   // (the slap of it)
            // the ink giving: a wet band, its grain rough
            var wet = Pink(n, r);
            var grain = Wobble(n, r, 90f);
            var b1 = new Biquad(); var b2 = new Biquad(); float wc = 1300f * Vary(r, 0.2f) / Mathf.Sqrt(w);
            b1.BandPass(wc, 0.9f); b2.BandPass(wc, 0.9f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                o[i] += b2.Step(b1.Step(wet[i])) * Mathf.Exp(-s / (0.05f * w)) * Mathf.Clamp01(s / 0.006f) * Mathf.Abs(grain[i]) * 2f;
            }
            Droplets(o, r, 0.04f, 0.22f * w, 2 + (int)(2 * w), 700f, 1600f, 0.05f);
            return Finish(o, 0.16f, 0.8f, r, 150f / Mathf.Sqrt(w), 9000f);
        }

        /// <summary>The hero struck: the blow to his body, his clothes rustling with it and his breath driven out.</summary>
        public static float[] Hurt(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.45f);
            var o = new float[n];
            Strike(o, 0.001f, r, Flesh(r, 1.25f), 0.5f, 2200f, 0.0012f, 0.003f, 0.3f);
            Strike(o, 0.002f, r, Skin(r), 0.6f, 4000f, 0.0006f, 0.0015f, 0.35f);
            Rustle(o, r, 0.004f, 0.16f, 1900f, 0.5f);
            Mix(o, Breath(r, 0.28f, 0.3f), (int)(Rate * (0.03f + 0.02f * (float)r.NextDouble())), 0.5f);
            return Finish(o, 0.14f, 0.75f, r, 130f, 9000f);
        }

        /// <summary>
        /// Struck metal (the shield): a burst ringing a set of inharmonic modes, the pairs slightly apart so they beat,
        /// dulled a little (the shield is wrapped in the veil's cloth).
        /// </summary>
        public static float[] Clank(float baseHz, float ring, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.45f * ring);
            var o = new float[n];
            float b = baseHz * Vary(r, 0.06f);
            float[] ratio = { 1f, 1.006f, 1.93f, 3.02f, 3.04f, 4.71f, 6.1f };
            float[] amp = { 1f, 0.7f, 0.55f, 0.35f, 0.25f, 0.18f, 0.08f };
            float[] decay = { 0.15f, 0.14f, 0.09f, 0.06f, 0.05f, 0.035f, 0.02f };
            var m = new Modes { f = new float[ratio.Length], tau = new float[ratio.Length], amp = new float[ratio.Length] };
            for (int k = 0; k < ratio.Length; k++)
            {
                m.f[k] = b * ratio[k] * Vary(r, 0.012f);
                m.tau[k] = decay[k] * ring * Vary(r, 0.15f);
                m.amp[k] = amp[k] * Vary(r, 0.25f);
            }
            Strike(o, 0.001f, r, m, 1f, 5000f, 0.0005f, 0.001f, 0.45f);
            Strike(o, 0.001f, r, Body(r, 10, baseHz * 1.5f, baseHz * 12f, 0.05f * ring, 0.01f, 0.8f), 0.6f, 6000f, 0.0005f, 0.001f, 0.1f);
            Strike(o, 0.001f, r, Flesh(r, 0.8f), 0.4f, 900f, 0.002f, 0.005f, 0.1f);   // (the arm behind it taking the blow)
            return Finish(o, 0.18f, 1f, r);
        }

        /// <summary>A foe undone: its ink bursts, drains away bright to dark over a low, soft rumble, a few drops after.</summary>
        public static float[] Dissolve(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.7f);
            var o = new float[n];
            var noise = Pink(n, r);
            var rumble = Brown(n, r);
            var grain = Wobble(n, r, 40f);
            var l1 = new Biquad(); var l2 = new Biquad(); var lr = new Biquad(); lr.LowPass(140f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float t = (float)i / n;
                if ((i & 31) == 0) { float cut = Mathf.Lerp(2000f, 160f, Mathf.Pow(t, 0.6f)); l1.LowPass(cut, 0.7f); l2.LowPass(cut, 0.7f); }
                float hiss = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.22f) * Mathf.Clamp01(s / 0.01f) * (0.7f + 0.4f * grain[i]);
                o[i] = hiss * 1.6f + lr.Step(rumble[i]) * Bell(t, 0.3f) * 0.25f;
            }
            for (int k = 0; k < 16; k++)
            {
                float at = 0.02f + 0.5f * Mathf.Pow((float)r.NextDouble(), 1.3f);
                Strike(o, at, r, Skin(r), 0.12f * (0.4f + 0.6f * (float)r.NextDouble()), 3000f, 0.0008f, 0.0015f, 0.4f);   // (a bubble bursting)
            }
            return Finish(o, 0.2f, 1f, r, 80f, 9000f);
        }

        /// <summary>The whip's crack: the tip outrunning sound - one sharp pressure wave (an N) - and the ruin echoing it.</summary>
        public static float[] Crack(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.12f);
            var o = new float[n];
            int start = Len(0.002f);
            float period = 0.0005f * Vary(r, 0.3f);
            int pl = Len(period);
            for (int i = 0; i < pl && start + i < n; i++) o[start + i] = Mathf.Sin(2f * Mathf.PI * i / pl) * 1.2f;
            var tail = Pink(n, r);
            var h1 = new Biquad(); h1.HighPass(1400f * Vary(r, 0.15f), 0.7f);
            var lp = new Biquad(); lp.LowPass(6000f, 0.7f);
            for (int i = start; i < n; i++)
            {
                float s = (float)(i - start) / Rate;
                o[i] += lp.Step(h1.Step(tail[i])) * Mathf.Exp(-s / 0.012f) * 0.8f;
            }
            return Finish(o, 0.28f, 1.1f, r, 200f);
        }

        /// <summary>A bowstring let go: a plucked string (Karplus-Strong), the limbs' wooden knock, a breath of the arrow leaving.</summary>
        public static float[] Pluck(float hz, float seconds, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float f = hz * Vary(r, 0.04f);
            int n = Len(seconds);
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
            var buf = Noise(period, r);
            var o = new float[n];
            var lp = new Biquad(); lp.LowPass(2500f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                int k = i % period, k1 = (k + 1) % period;
                float v = buf[k];
                buf[k] = 0.985f * 0.5f * (buf[k] + buf[k1]);
                float s = (float)i / Rate;
                o[i] = lp.Step(v) * Mathf.Exp(-s / 0.05f) * 0.45f;
            }
            Strike(o, 0f, r, Wood(r), 0.6f, 2200f, 0.001f, 0.003f, 0.2f);
            Rustle(o, r, 0.01f, 0.12f, 1300f, 0.3f);
            Mix(o, Swing(2200f, 0.16f, seed + 11, variant), Len(0.008f), 0.8f);   // (the arrow leaving)
            return Finish(o, 0.12f, 0.7f, r, 120f, 10000f);
        }

        /// <summary>An arrow striking home: a hollow knock in a body and a splinter of grit.</summary>
        public static float[] Thunk(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.16f);
            var o = new float[n];
            Strike(o, 0.001f, r, Wood(r), 0.7f, 3000f, 0.0006f, 0.002f, 0.3f);
            Strike(o, 0.002f, r, Flesh(r, 0.9f), 0.6f, 2000f, 0.001f, 0.003f, 0.2f);
            Grit(o, r, 0.003f, 0.05f, 4, 0.06f);
            return Finish(o, 0.14f, 0.7f, r, 120f);
        }

        // ===================================================================================== moving

        /// <summary>
        /// A footstep on stone in soft boots: the heel's dull knock - damped at once, nothing in it rings (ringing modes
        /// made it a tap of drumsticks) - the sole rolling down after it, the leather's scuff and a crumb of grit.
        /// <paramref name="weight"/>: a walk ~0.6, a run 1.
        /// </summary>
        public static float[] Step(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float w = Mathf.Max(0.3f, weight);
            int n = Len(0.14f + 0.06f * w);
            var o = new float[n];
            float bright = (1100f + 700f * w) * Vary(r, 0.25f);
            // (damped: each mode dies within a couple of its own periods - a knock, not a note)
            var heel = Body(r, 4, 110f * Vary(r, 0.15f), 650f, 0.006f, 0.0018f, 1f);
            Strike(o, 0.001f, r, heel, w, bright, 0.002f + 0.002f * (float)r.NextDouble(), 0.004f, 0.6f);
            float roll = (0.025f + 0.035f * (float)r.NextDouble()) / Mathf.Sqrt(w);   // (heel, then the sole)
            var sole = Body(r, 3, 160f, 800f, 0.004f, 0.0015f, 1f);
            Strike(o, roll, r, sole, 0.45f * w * Vary(r, 0.35f), bright * 1.2f, 0.003f, 0.005f, 0.6f);
            Rustle(o, r, roll * 0.6f, 0.04f + 0.04f * w, 1700f * Vary(r, 0.3f), 0.12f + 0.08f * w);
            Grit(o, r, 0.004f, 0.08f, 1 + r.Next(3), 0.035f);
            return Finish(o, 0.09f, 0.55f, r, 70f, 8000f);
        }

        /// <summary>Coming down from a jump or a fall: both feet a moment apart, the body's weight in the knees, clothes settling.</summary>
        public static float[] Land(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.25f + 0.08f * weight);
            var o = new float[n];
            Mix(o, Step(Mathf.Min(1.4f, weight * 1.2f), seed + 1, variant), 0, 0.9f);
            Mix(o, Step(weight, seed + 2, variant), (int)(Rate * (0.006f + 0.02f * (float)r.NextDouble())), 0.7f);
            Strike(o, 0.012f, r, Flesh(r, 1.1f), 0.2f * weight, 1200f, 0.003f, 0.006f, 0.1f);   // (the knees take it)
            Rustle(o, r, 0.01f, 0.16f, 1400f, 0.35f);
            Grit(o, r, 0.01f, 0.14f, 3 + (int)(4 * weight), 0.06f);
            return Finish(o, 0.12f, 0.7f, r, 130f, 11000f);
        }

        /// <summary>Pushing off the ground (or a wall) into a jump: a scuff of the foot and the clothes rising.</summary>
        public static float[] PushOff(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.28f)];
            Mix(o, Step(0.5f, seed + 3, variant), 0, 0.7f);
            Mix(o, Swing(900f, 0.22f, seed + 4, variant), Len(0.015f), 0.5f);
            return Finish(o, 0.06f, 0.5f, r);
        }

        /// <summary>A rush of air (the dash, the somersault, letting go of the whip).</summary>
        public static float[] Rush(float centreHz, float seconds, int seed, int variant = 0) => Swing(centreHz, seconds, seed, variant);

        /// <summary>Rolling out of a fall: the shoulder and then the back on stone (dull, soft) and the clothes dragging.</summary>
        public static float[] Roll(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.55f)];
            Strike(o, 0.002f, r, Flesh(r, 1f), 0.25f, 1800f, 0.003f, 0.006f, 0.15f);
            Strike(o, 0.002f, r, Stone(r), 0.15f, 1400f, 0.002f, 0.003f, 0.1f);
            float second = 0.14f + 0.05f * (float)r.NextDouble();
            Strike(o, second, r, Flesh(r, 0.9f), 0.2f, 1700f, 0.004f, 0.008f, 0.15f);
            Mix(o, Scrape(1100f, 0.42f, seed + 7, variant, 0.3f), Len(0.02f), 0.8f);
            Rustle(o, r, 0.0f, 0.4f, 2200f, 0.45f);
            return Finish(o, 0.1f, 0.6f, r, 180f, 10000f);
        }

        /// <summary>A hand slapping onto a stone lip and taking the weight: a short skin smack, a crumb of grit.</summary>
        public static float[] Grab(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.14f)];
            Strike(o, 0.001f, r, Skin(r), 1f, 5500f, 0.0004f, 0.001f, 0.35f);
            Strike(o, 0.002f, r, Stone(r), 0.3f, 2000f, 0.0008f, 0.0015f, 0.1f);
            Rustle(o, r, 0.004f, 0.1f, 1700f, 0.25f);   // (the sleeve, as the arm takes his weight)
            Grit(o, r, 0.005f, 0.06f, 3, 0.05f);
            return Finish(o, 0.12f, 0.6f, r, 250f, 11000f);
        }

        /// <summary>Hauling up over a lip: clothes and boots scraping the stone, his breath, a step on top.</summary>
        public static float[] Mantle(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.75f)];
            Mix(o, Scrape(1000f, 0.45f, seed + 8, variant, 0.5f), 0, 0.5f);
            Mix(o, Breath(r, 0.3f, 0.12f), Len(0.06f), 0.3f);
            Mix(o, Step(0.8f, seed + 9, variant), Len(0.5f), 0.9f);
            return Finish(o, 0.1f, 0.6f, r, 100f);
        }

        /// <summary>The whip catching on a beam: its end wrapping round it (a few quick soft knocks, closer and closer) and the wood taking the pull.</summary>
        public static float[] Latch(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.3f)];
            var wood = Skin(r);
            int wraps = 3 + r.Next(2);
            float at = 0.002f, gap = 0.035f * Vary(r, 0.2f);
            for (int k = 0; k < wraps; k++)
            {
                Strike(o, at, r, wood, (1f - 0.18f * k) * Vary(r, 0.15f), 2600f, 0.0008f, 0.002f, 0.25f);
                at += gap;
                gap *= 0.72f;
            }
            Mix(o, Swing(1400f, 0.12f, seed + 10, variant), 0, 0.5f);
            return Finish(o, 0.14f, 0.7f, r, 150f);
        }

        /// <summary>
        /// Something rubbing over stone for a while (a slide, a slide down a wall): pink noise in a band, its grain
        /// coming and going irregularly. <paramref name="roughness"/> 0..1: how gritty.
        /// </summary>
        public static float[] Scrape(float centreHz, float seconds, int seed, int variant = 0, float roughness = 0.7f)
        {
            var r = Rng(seed, variant);
            int n = Len(seconds);
            var o = ScrapeSteady(centreHz * Vary(r, 0.1f), seconds, r, roughness);
            for (int i = 0; i < n; i++) o[i] *= Bell((float)i / n, 0.2f, 0.05f);
            return Normalise(o);
        }

        /// <summary>A slide's scrape as a seamless loop (it plays as long as he slides).</summary>
        public static float[] ScrapeLoop(float centreHz, float seconds, int seed, float roughness)
        {
            int fade = Len(0.1f);
            var steady = ScrapeSteady(centreHz, seconds + 0.1f, Rng(seed, 0), roughness);
            return Seamless(steady, steady.Length - fade, fade);
        }

        static float[] ScrapeSteady(float centreHz, float seconds, System.Random r, float roughness)
        {
            int n = Len(seconds);
            var o = new float[n];
            var noise = Pink(n, r);
            var grain = Wobble(n, r, 25f);
            var rough = Wobble(n, r, 45f);
            var b1 = new Biquad(); var b2 = new Biquad(); b1.BandPass(centreHz, 0.8f); b2.BandPass(centreHz * 0.8f, 0.8f);
            var lp = new Biquad(); lp.LowPass(centreHz * 0.35f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float g = 0.7f + 0.3f * grain[i];
                float k = 1f + roughness * 0.35f * rough[i];
                o[i] = (b2.Step(b1.Step(noise[i])) * 1.4f + lp.Step(noise[i]) * 0.7f) * g * Mathf.Max(0f, k);
            }
            Grit(o, r, 0f, seconds, (int)(seconds * 50f * roughness), 0.06f);
            return o;
        }

        /// <summary>
        /// The whip groaning once under his weight (the bottom of a swing, where it pulls hardest): stick-slip catches
        /// whose rate glides up and back, through the leather's and the beam's resonances.
        /// </summary>
        public static float[] Creak(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float len = 0.22f + 0.14f * (float)r.NextDouble();
            var o = new float[Len(len + 0.08f)];
            float f0 = 30f + 20f * (float)r.NextDouble(), f1 = f0 * (1.5f + 0.8f * (float)r.NextDouble());
            var r1 = new Biquad(); r1.BandPass(330f * Vary(r, 0.12f), 7f);
            var r2 = new Biquad(); r2.BandPass(760f * Vary(r, 0.12f), 6f);
            var r3 = new Biquad(); r3.BandPass(1450f * Vary(r, 0.12f), 5f);
            float phase = 0f;
            int body = Len(len);
            for (int i = 0; i < o.Length; i++)
            {
                float t = (float)i / body;
                float env = t < 1f ? Bell(t, 0.4f) : 0f;
                float rate = Mathf.Lerp(f0, f1, Mathf.Sin(Mathf.PI * Mathf.Min(t, 1f))) * (0.9f + 0.2f * (float)r.NextDouble());
                phase += rate / Rate;
                float x = 0f;
                if (phase >= 1f) { phase -= 1f; x = env * (0.6f + 0.4f * (float)r.NextDouble()); }
                x += (float)(r.NextDouble() * 2.0 - 1.0) * env * 0.02f;
                o[i] = r1.Step(x) * 1.2f + r2.Step(x) + r3.Step(x) * 0.4f;
            }
            return Finish(o, 0.12f, 0.6f, r, 150f, 7000f);
        }

        /// <summary>Air streaming past (the scarf sail): soft, low pink noise, gusting slowly and unevenly, seamless.</summary>
        public static float[] WindLoop(float centreHz, float seconds, int seed)
        {
            var r = Rng(seed, 0);
            int fade = Len(0.3f);
            int n = Len(seconds);
            int all = n + fade;
            var o = new float[all];
            var noise = Pink(all, r);
            var gust = Wobble(all, r, 0.8f);
            var flutter = Wobble(all, r, 8f);
            var b1 = new Biquad(); b1.BandPass(centreHz, 0.6f);
            var b2 = new Biquad(); b2.BandPass(centreHz * 2.4f, 1.4f);
            var lp = new Biquad(); lp.LowPass(centreHz * 0.6f, 0.7f);
            for (int i = 0; i < all; i++)
            {
                float g = 0.6f + 0.4f * gust[i];
                o[i] = (b1.Step(noise[i]) + lp.Step(noise[i]) * 0.8f + b2.Step(noise[i]) * 0.3f * Mathf.Max(0f, gust[i])) * g * (1f + 0.15f * flutter[i]);
            }
            return Seamless(o, n, fade);
        }

        /// <summary>
        /// The ruin itself, under everything (seamless): air moving low through it in slow gusts, a faint high breath
        /// of it in the openings, now and then a drop falling far off or a stone settling, all of it in the ruin's long
        /// echo. (Without it every sound broke out of digital silence - as nothing in the world does - and a listening
        /// model heard a drum machine in nearly every blow; over a faint floor of the room it heard the blow.)
        /// </summary>
        public static float[] AmbienceLoop(float seconds, int seed)
        {
            var r = Rng(seed, 0);
            int fade = Len(1.5f);
            int n = Len(seconds);
            int all = n + fade;
            var o = new float[all];
            var low = Brown(all, r);
            var air = Pink(all, r);
            var gust = Wobble(all, r, 0.15f);
            var drift = Wobble(all, r, 0.05f);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(380f, 0.7f); l2.LowPass(380f, 0.7f);
            var hp = new Biquad(); hp.HighPass(40f, 0.7f);
            var b1 = new Biquad(); b1.BandPass(2600f, 0.7f);
            var w1 = new Biquad(); var w2 = new Biquad(); w1.BandPass(700f, 0.6f); w2.BandPass(700f, 0.6f);
            var howl = Wobble(all, r, 0.25f);
            for (int i = 0; i < all; i++)
            {
                float g = 0.55f + 0.45f * gust[i];
                // (the low air, the wind in the openings when it gusts - soft, never a whistle - the faint high breath of it)
                float through = Mathf.Max(0f, howl[i] * 0.5f + gust[i] * 0.5f);
                o[i] = hp.Step(l2.Step(l1.Step(low[i]))) * g * 1.2f + w2.Step(w1.Step(air[i])) * through * 0.35f
                       + b1.Step(air[i]) * 0.05f * (0.6f + 0.4f * drift[i]);
            }
            float fm = PeakOf(o);
            for (int i = 0; i < all; i++) o[i] /= Mathf.Max(fm, 1e-6f);
            // far off, now and then: a drop, a stone settling (few, soft, deep in the echo)
            var far = new float[all];
            int events = Mathf.RoundToInt(seconds * 0.6f);
            for (int k = 0; k < events; k++)
            {
                float at = (float)r.NextDouble() * (seconds - 0.8f);
                // a stone settling, or a few pebbles trickling down after it
                int pebbles = 1 + r.Next(4);
                for (int p = 0; p < pebbles; p++)
                {
                    Strike(far, at, r, Stone(r), 0.25f * (1f - 0.15f * p), 2200f, 0.001f, 0.002f, 0.15f);
                    at += 0.04f + 0.08f * (float)r.NextDouble();
                }
            }
            var tail = Finish(far, 0.7f, 2.2f, r, 200f, 5000f);   // (heard through the stone: dark, mostly echo)
            for (int i = 0; i < all && i < tail.Length; i++) o[i] += tail[i] * 0.08f;
            return Seamless(o, n, fade);
        }

        // ===================================================================================== loudness

        /// <summary>
        /// How loud a sound is heard (LUFS, after ITU-R BS.1770: K-weighted, in 50 ms blocks overlapping by half,
        /// gated at -70 LUFS and 10 LU under the ungated level). What it is played at is set from this, not from its peak:
        /// a step peaks as high as a blow, but is heard far quieter.
        /// </summary>
        public static float Loudness(float[] x)
        {
            var shelf = new Biquad(); shelf.HighShelf(1681f, 4f, 0.707f);
            var hp = new Biquad(); hp.HighPass(38f, 0.5f);
            var k = new float[x.Length];
            for (int i = 0; i < x.Length; i++) k[i] = hp.Step(shelf.Step(x[i]));
            int block = Rate / 20, hop = block / 2;
            int blocks = Mathf.Max(1, (x.Length - block) / hop + 1);
            var ms = new double[blocks];
            for (int b = 0; b < blocks; b++)
            {
                double sum = 0;
                int from = b * hop, to = Mathf.Min(x.Length, from + block);
                for (int i = from; i < to; i++) sum += (double)k[i] * k[i];
                ms[b] = sum / Mathf.Max(1, to - from);
            }
            double Lufs(double m) => -0.691 + 10.0 * Math.Log10(Math.Max(m, 1e-12));
            double abs = 0; int na = 0;
            foreach (var m in ms) if (Lufs(m) > -70.0) { abs += m; na++; }
            if (na == 0) return -70f;
            double rel = Lufs(abs / na) - 10.0;
            double gated = 0; int ng = 0;
            foreach (var m in ms) if (Lufs(m) > rel) { gated += m; ng++; }
            return (float)Lufs(gated / Math.Max(1, ng));
        }

        // ===================================================================================== the means

        public static int Len(float seconds) => Mathf.Max(16, Mathf.RoundToInt(seconds * Rate));

        /// <summary>A random stream for a sound and one of its variants.</summary>
        static System.Random Rng(int seed, int variant) => new System.Random(seed * 7919 + variant * 104729 + 17);

        /// <summary>1 ± <paramref name="spread"/>, at random.</summary>
        static float Vary(System.Random r, float spread) => 1f + spread * ((float)r.NextDouble() * 2f - 1f);

        /// <summary>A swell: 0 at the ends, 1 at <paramref name="peak"/> (0..1), rising faster than it falls.</summary>
        static float Bell(float t, float peak, float edge = 0f)
        {
            if (t <= 0f || t >= 1f) return 0f;
            float u = t < peak ? t / peak : 1f - (t - peak) / (1f - peak);
            u = Mathf.Clamp01(u);
            return Mathf.Max(edge, u * u * (3f - 2f * u));
        }

        static float PeakOf(float[] x)
        {
            float m = 0f;
            foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v));
            return m;
        }

        /// <summary>White noise -1..1, the same for the same seed.</summary>
        public static float[] Noise(int n, int seed) => Noise(n, new System.Random(seed));

        static float[] Noise(int n, System.Random r)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = (float)(r.NextDouble() * 2.0 - 1.0);
            return o;
        }

        /// <summary>Pink noise (-3 dB an octave, as most noise in the world; white is a hiss): Paul Kellet's filter.</summary>
        static float[] Pink(int n, System.Random r)
        {
            var o = new float[n];
            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)(r.NextDouble() * 2.0 - 1.0);
                b0 = 0.99765f * b0 + w * 0.0990460f;
                b1 = 0.96300f * b1 + w * 0.2965164f;
                b2 = 0.57000f * b2 + w * 1.0526913f;
                o[i] = (b0 + b1 + b2 + w * 0.1848f) * 0.25f;
            }
            return o;
        }

        /// <summary>Brown noise (-6 dB an octave): a rumble.</summary>
        static float[] Brown(int n, System.Random r)
        {
            var o = new float[n];
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                last = (last + 0.02f * (float)(r.NextDouble() * 2.0 - 1.0)) / 1.02f;
                o[i] = last * 3.5f;
            }
            return o;
        }

        /// <summary>A slow random wander about 0 (about -1..1) changing about <paramref name="hz"/> times a second.</summary>
        static float[] Wobble(int n, System.Random r, float hz)
        {
            var o = new float[n];
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * hz / Rate);
            float y1 = 0f, y2 = 0f;
            double sq = 0;
            for (int i = 0; i < n; i++)
            {
                float w = (float)(r.NextDouble() * 2.0 - 1.0);
                y1 += (w - y1) * a;
                y2 += (y1 - y2) * a;
                o[i] = y2;
                sq += y2 * y2;
            }
            float k = sq > 0 ? (float)(1.0 / Math.Sqrt(sq / n)) * 0.5f : 0f;
            for (int i = 0; i < n; i++) o[i] = Mathf.Clamp(o[i] * k, -1f, 1f);
            return o;
        }

        /// <summary>Adds <paramref name="src"/> into <paramref name="dst"/> from sample <paramref name="at"/>, scaled.</summary>
        static void Mix(float[] dst, float[] src, int at, float gain)
        {
            for (int i = 0; i < src.Length && at + i < dst.Length; i++) if (at + i >= 0) dst[at + i] += src[i] * gain;
        }

        /// <summary>Clothes rustling: pink noise in a band, its grain uneven, rising and gone within <paramref name="seconds"/>.</summary>
        static void Rustle(float[] o, System.Random r, float at, float seconds, float hz, float level)
        {
            int start = (int)(at * Rate), n = Len(seconds);
            var noise = Pink(n, r);
            var grain = Wobble(n, r, 60f);
            var b1 = new Biquad(); var b2 = new Biquad(); b1.BandPass(hz, 0.8f); b2.BandPass(hz, 0.8f);
            for (int i = 0; i < n && start + i < o.Length; i++)
                o[start + i] += b2.Step(b1.Step(noise[i])) * Bell((float)i / n, 0.25f) * Mathf.Abs(grain[i]) * level * 4f;
        }

        /// <summary>Drops falling after a splash: soft, short, a little pitch, spread over a while.</summary>
        static void Droplets(float[] o, System.Random r, float from, float to, int count, float lowHz, float highHz, float level)
        {
            for (int k = 0; k < count; k++)
            {
                float at = from + (to - from) * Mathf.Pow((float)r.NextDouble(), 1.4f);
                int start = (int)(at * Rate);
                float f = Mathf.Lerp(lowHz, highHz, (float)r.NextDouble());
                float a = level * (0.4f + 0.6f * (float)r.NextDouble());
                int len = Len(0.02f);
                for (int i = 0; i < len && start + i < o.Length; i++)
                {
                    float s = (float)i / Rate;
                    // a drop: its pitch rises as it lands (the air cavity closing), soft at the start
                    o[start + i] += Mathf.Sin(2f * Mathf.PI * f * (1f + 2.5f * s) * s) * Mathf.Clamp01(s / 0.001f) * Mathf.Exp(-s / 0.006f) * a;
                }
            }
        }

        /// <summary>Grit: tiny crumbs crunching (stone under a foot), band limited, spread over a while.</summary>
        static void Grit(float[] o, System.Random r, float from, float to, int count, float level)
        {
            var bp = new Biquad(); bp.BandPass(3200f * Vary(r, 0.25f), 1.2f);
            var g = new float[o.Length];
            for (int k = 0; k < count; k++)
            {
                int start = (int)((from + (to - from) * (float)r.NextDouble()) * Rate);
                float a = level * (0.3f + 0.7f * (float)r.NextDouble());
                int len = Len(0.002f + 0.002f * (float)r.NextDouble());
                for (int i = 0; i < len && start + i < g.Length; i++)
                    g[start + i] += (float)(r.NextDouble() * 2.0 - 1.0) * a * (1f - (float)i / len);
            }
            for (int i = 0; i < o.Length; i++) o[i] += bp.Step(g[i]) * 3f;
        }

        /// <summary>A breath (exhaled, voiced a little): noise through the two formants of an open vowel, dark, a falling swell.</summary>
        static float[] Breath(System.Random r, float seconds, float voiced)
        {
            int n = Len(seconds);
            var o = new float[n];
            var noise = Pink(n, r);
            var f1 = new Biquad(); f1.BandPass(650f * Vary(r, 0.1f), 3f);
            var f2 = new Biquad(); f2.BandPass(1100f * Vary(r, 0.1f), 4f);
            var lp = new Biquad(); lp.LowPass(2200f, 0.7f);
            var vlp = new Biquad(); vlp.LowPass(500f, 0.7f);
            float pitch = 110f * Vary(r, 0.12f), phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += 2f * Mathf.PI * pitch * (1f - 0.15f * t) / Rate;
                // (the voice: a soft pulse, not a buzzing saw)
                float voice = vlp.Step(Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase)), 3f)) * voiced * 3f;
                float src = noise[i] * 2f + voice;
                o[i] = lp.Step(f1.Step(src) + 0.6f * f2.Step(src)) * Bell(t, 0.12f);
            }
            return Normalise(o, false);
        }

        /// <summary>
        /// The ruin around the sound: a short pre-delay, then a feedback delay network (four lines, Householder
        /// mixed, darkening as it decays) dying away in <paramref name="t60"/> s, mixed <paramref name="wet"/> under the
        /// sound; then the whole is taken a little darker (nothing in a stone ruin at night is bright) and normalised.
        /// </summary>
        static float[] Finish(float[] dry, float wet, float t60, System.Random r, float lowCut = 35f, float highCut = 7000f)
        {
            int tail = Len(t60 * 0.75f);
            var o = new float[dry.Length + tail];
            Array.Copy(dry, o, dry.Length);
            int[] d = { Len(0.0297f * Vary(r, 0.05f)), Len(0.0371f * Vary(r, 0.05f)), Len(0.0411f * Vary(r, 0.05f)), Len(0.0437f * Vary(r, 0.05f)) };
            var lines = new float[4][];
            var pos = new int[4];
            var g = new float[4];
            var damp = new float[4];
            for (int k = 0; k < 4; k++)
            {
                lines[k] = new float[d[k]];
                g[k] = Mathf.Pow(10f, -3f * d[k] / (t60 * Rate));
            }
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 3000f / Rate);
            int pre = Len(0.012f);
            var outs = new float[4];
            for (int i = 0; i < o.Length; i++)
            {
                float input = i - pre >= 0 && i - pre < dry.Length ? dry[i - pre] : 0f;
                float sum = 0f;
                for (int k = 0; k < 4; k++) { outs[k] = lines[k][pos[k]]; sum += outs[k]; }
                float mixv = sum * 0.5f;   // (Householder: x - 2/N * sum)
                float rev = 0f;
                for (int k = 0; k < 4; k++)
                {
                    float fb = (outs[k] - mixv) * g[k];
                    damp[k] += (fb - damp[k]) * a;
                    lines[k][pos[k]] = damp[k] + input * 0.5f;
                    pos[k] = (pos[k] + 1) % d[k];
                    rev += outs[k] * (k % 2 == 0 ? 1f : -1f);
                }
                o[i] += rev * wet;
            }
            // (dark: a gentle roll-off over ~7 kHz; nothing under the low cut - a small thing struck has no sub-bass)
            var lp = new Biquad(); lp.LowPass(highCut, 0.6f);
            var hp = new Biquad(); hp.HighPass(lowCut, 0.7f);
            var hp2 = new Biquad(); hp2.HighPass(lowCut, 0.7f);
            for (int i = 0; i < o.Length; i++) o[i] = hp2.Step(hp.Step(lp.Step(o[i])));
            int fade = Len(Mathf.Min(0.15f, t60 * 0.3f));
            for (int i = 0; i < fade; i++) o[o.Length - 1 - i] *= (float)i / fade;
            return Normalise(o);
        }

        /// <summary>The first <paramref name="n"/> samples of <paramref name="x"/>, its tail beyond them crossfaded
        /// into its start so that it loops without a seam; at full level.</summary>
        static float[] Seamless(float[] x, int n, int fade)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = x[i];
            for (int i = 0; i < fade && n + i < x.Length; i++)
            {
                float k = (float)i / fade;
                o[i] = x[i] * k + x[n + i] * (1f - k);
            }
            return Normalise(o, false);
        }

        /// <summary>Scales <paramref name="x"/> so its loudest sample is <see cref="Peak"/> (and fades its end out, unless it loops).</summary>
        public static float[] Normalise(float[] x, bool fadeEnd = true)
        {
            float m = PeakOf(x);
            if (m < 1e-6f) return x;
            float k = Peak / m;
            for (int i = 0; i < x.Length; i++) x[i] *= k;
            if (!fadeEnd) return x;
            int fade = Mathf.Min(x.Length, Rate / 250);
            for (int i = 0; i < fade; i++) x[x.Length - 1 - i] *= (float)i / fade;
            return x;
        }

        /// <summary>A 12 dB/oct filter (RBJ biquad, in double: the narrow modes of metal need it).</summary>
        sealed class Biquad
        {
            double b0, b1, b2, a1, a2, x1, x2, y1, y2;

            static void Omega(float hz, float q, out double cos, out double alpha, out double sin)
            {
                double w = 2.0 * Math.PI * Mathf.Clamp(hz, 20f, Rate * 0.45f) / Rate;
                cos = Math.Cos(w);
                sin = Math.Sin(w);
                alpha = sin / (2.0 * q);
            }

            void Set(double nb0, double nb1, double nb2, double a0, double na1, double na2)
            {
                b0 = nb0 / a0; b1 = nb1 / a0; b2 = nb2 / a0; a1 = na1 / a0; a2 = na2 / a0;
            }

            public void BandPass(float hz, float q)
            {
                Omega(hz, q, out double c, out double al, out _);
                Set(al, 0.0, -al, 1.0 + al, -2.0 * c, 1.0 - al);
            }

            public void LowPass(float hz, float q)
            {
                Omega(hz, q, out double c, out double al, out _);
                Set((1.0 - c) * 0.5, 1.0 - c, (1.0 - c) * 0.5, 1.0 + al, -2.0 * c, 1.0 - al);
            }

            public void HighPass(float hz, float q)
            {
                Omega(hz, q, out double c, out double al, out _);
                Set((1.0 + c) * 0.5, -(1.0 + c), (1.0 + c) * 0.5, 1.0 + al, -2.0 * c, 1.0 - al);
            }

            public void HighShelf(float hz, float dB, float q)
            {
                Omega(hz, q, out double c, out double al, out _);
                double A = Math.Pow(10.0, dB / 40.0), sq = 2.0 * Math.Sqrt(A) * al;
                Set(A * ((A + 1) + (A - 1) * c + sq), -2 * A * ((A - 1) + (A + 1) * c), A * ((A + 1) + (A - 1) * c - sq),
                    (A + 1) - (A - 1) * c + sq, 2 * ((A - 1) - (A + 1) * c), (A + 1) - (A - 1) * c - sq);
            }

            public float Step(float x)
            {
                double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return (float)y;
            }
        }
    }
}
