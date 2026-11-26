using System;
using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The game's sounds, made in code (no recordings). Each is built of layers the way a real sound is - a blow is
    /// the edge biting, the body's thump, the ink splashing and the room answering - and each comes in several
    /// variants (<paramref name="variant"/>: the same kind of sound, never the same one twice: the seed, the pitch, the
    /// timing and the balance of the layers all differ). Every sound ends in a short tail of the stone room it is
    /// heard in. Mono buffers, deterministic, peaking at <see cref="Peak"/>.
    /// <para>(The first set was one or two layers, dry, and the same every time: it sounded made.)</para>
    /// </summary>
    public static class SoundSynth
    {
        public const int Rate = 44100;
        /// <summary>Every sound is normalised to this peak (headroom for several at once).</summary>
        public const float Peak = 0.8f;

        // ===================================================================================== the fight

        /// <summary>
        /// A weapon of ink and cloth cutting the air: a band of noise sweeping with the blade's speed, and the cloth it
        /// is made of fluttering in it (an irregular flapping, 20-45 times a second).
        /// </summary>
        public static float[] Swing(float centreHz, float seconds, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float c0 = centreHz * Vary(r, 0.12f);
            float len = seconds * Vary(r, 0.15f);
            float peakAt = 0.35f + 0.25f * (float)r.NextDouble();
            int n = Len(len);
            var o = new float[n];
            var noise = Noise(n, r);
            var b1 = new Biquad(); var b2 = new Biquad(); var cl = new Biquad();
            float flap = 0f, flapRate = 22f + 20f * (float)r.NextDouble();
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float speed = Bell(t, peakAt);
                float c = c0 * (0.55f + 0.9f * speed);
                if ((i & 31) == 0) { b1.BandPass(c, 1.3f); b2.BandPass(c, 1.3f); cl.BandPass(c * 0.45f, 2.5f); }
                float air = b2.Step(b1.Step(noise[i])) * speed * speed;
                // the cloth: a flapping that speeds up with the swing, its beats a little irregular
                flap += flapRate * (0.6f + speed) / Rate * (0.85f + 0.3f * Mathf.PerlinNoise(t * 7f, variant));
                float beat = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * flap), 3f);
                float cloth = cl.Step(noise[(i * 7 + 13) % n]) * beat * speed * 0.9f;
                o[i] = air + cloth;
            }
            return Finish(o, 0.06f, 0.12f, r);
        }

        /// <summary>
        /// A blow landing: the edge biting (a few clicks within a few milliseconds), the body's falling thump, the ink
        /// splashing (a dull wash and droplets scattering after it) and the room. <paramref name="weight"/> 1 is a plain
        /// hit, more a heavier one: lower, longer, more ink.
        /// </summary>
        public static float[] Hit(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float w = Mathf.Max(0.5f, weight) * Vary(r, 0.08f);
            int n = Len(0.28f * w);
            var o = new float[n];
            var noise = Noise(n, r);
            var l1 = new Biquad(); var l2 = new Biquad(); float cut = 850f / w * Vary(r, 0.15f); l1.LowPass(cut, 0.7f); l2.LowPass(cut, 0.7f);
            var h1 = new Biquad(); var h2 = new Biquad(); h1.HighPass(2600f, 0.7f); h2.HighPass(2600f, 0.7f);
            var body = new Biquad(); body.BandPass(320f * Vary(r, 0.2f) / w, 1.8f);
            float f0 = 190f / w * Vary(r, 0.12f), f1 = 70f * Vary(r, 0.1f);   // (a small speaker plays little under ~150 Hz)
            int clicks = 2 + r.Next(3);
            var clickAt = new int[clicks];
            for (int k = 0; k < clicks; k++) clickAt[k] = (int)(Rate * 0.004f * k * (0.6f + 0.8f * (float)r.NextDouble()));
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(f1, f0, Mathf.Exp(-s / 0.028f)) / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-s / (0.06f * w));
                float edge = 0f;
                for (int k = 0; k < clicks; k++)
                {
                    int d = i - clickAt[k];
                    if (d >= 0 && d < Rate / 200) edge += h2.Step(h1.Step(noise[i])) * Mathf.Exp(-d / (Rate * 0.0012f)) * (1f - 0.25f * k);
                }
                float wash = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / (0.045f * w)) * Mathf.Clamp01(s / 0.005f);
                float flesh = body.Step(noise[i]) * Mathf.Exp(-s / 0.03f);
                o[i] = Soft(thump * 1.1f + edge * 0.7f + wash * 2.6f + flesh * 3f);
            }
            Droplets(o, r, 0.015f, 0.2f * w, 5 + (int)(6 * w), 1400f, 3200f, 0.18f);
            return Finish(o, 0.1f, 0.18f, r);
        }

        /// <summary>
        /// The hero struck: a heavy blow to the body, the cloth of him crunching, and his breath driven out of him
        /// (a short voiced exhale - noise through the vowel's two formants).
        /// </summary>
        public static float[] Hurt(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.45f);
            var o = new float[n];
            var noise = Noise(n, r);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(900f, 0.7f); l2.LowPass(900f, 0.7f);
            float f0 = 170f * Vary(r, 0.1f), f1 = 85f * Vary(r, 0.08f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(f1, f0, Mathf.Exp(-s / 0.045f)) / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-s / 0.12f);
                float crunch = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.07f) * 5f;
                o[i] = Soft(1.1f * thump + crunch);
            }
            // the breath, a moment after the blow
            Mix(o, Breath(r, 0.22f, 0.32f), (int)(Rate * (0.03f + 0.02f * (float)r.NextDouble())), 0.55f);
            return Finish(o, 0.1f, 0.18f, r);
        }

        /// <summary>Metal struck (the shield): inharmonic partials, each its own decay and a little out of tune each time, and the edge's click.</summary>
        public static float[] Clank(float baseHz, float ring, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.5f * ring);
            var o = new float[n];
            var noise = Noise(n, r);
            float b = baseHz * Vary(r, 0.06f);
            float[] ratio = { 1f, 1.93f, 3.02f, 4.71f, 6.1f, 8.2f };
            float[] amp = { 1f, 0.6f, 0.45f, 0.3f, 0.18f, 0.1f };
            float[] decay = { 0.2f, 0.13f, 0.09f, 0.06f, 0.04f, 0.025f };
            for (int k = 0; k < ratio.Length; k++) { ratio[k] *= Vary(r, 0.015f); amp[k] *= Vary(r, 0.25f); }
            var h1 = new Biquad(); h1.HighPass(2500f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float v = 0f;
                for (int k = 0; k < ratio.Length; k++)
                    v += amp[k] * Mathf.Sin(2f * Mathf.PI * b * ratio[k] * s + k) * Mathf.Exp(-s / (decay[k] * ring));
                v += h1.Step(noise[i]) * Mathf.Exp(-s / 0.004f) * 1.2f;
                o[i] = v;
            }
            return Finish(o, 0.14f, 0.3f, r);
        }

        /// <summary>A foe undone: its ink bursts (a splash and droplets), drains away bright to dark, a low swell under it.</summary>
        public static float[] Dissolve(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.65f);
            var o = new float[n];
            var noise = Noise(n, r);
            var l1 = new Biquad(); var l2 = new Biquad();
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float t = (float)i / n;
                if ((i & 31) == 0) { float cut = Mathf.Lerp(2400f, 150f, Mathf.Pow(t, 0.6f)); l1.LowPass(cut, 0.8f); l2.LowPass(cut, 0.8f); }
                float hiss = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.22f) * Mathf.Clamp01(s / 0.008f);
                phase += 2f * Mathf.PI * Mathf.Lerp(70f, 38f, t) / Rate;
                o[i] = Soft(hiss * 2.5f + Mathf.Sin(phase) * Mathf.Exp(-s / 0.16f) * 0.6f);
            }
            Droplets(o, r, 0.01f, 0.35f, 14, 900f, 2600f, 0.3f);
            return Finish(o, 0.12f, 0.25f, r);
        }

        /// <summary>The whip's crack: a sharp snap with a short ring of the air after it.</summary>
        public static float[] Crack(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.14f);
            var o = new float[n];
            var noise = Noise(n, r);
            var h1 = new Biquad(); h1.HighPass(1500f * Vary(r, 0.15f), 0.7f);
            var bp = new Biquad(); bp.BandPass(1800f * Vary(r, 0.2f), 1.2f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                o[i] = h1.Step(noise[i]) * Mathf.Exp(-s / 0.004f) * 2f + bp.Step(noise[i]) * Mathf.Exp(-s / 0.03f);
            }
            return Finish(o, 0.12f, 0.25f, r);
        }

        /// <summary>A string let go (the bow): a plucked string with the limbs' soft thump and a breath of air.</summary>
        public static float[] Pluck(float hz, float seconds, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float f = hz * Vary(r, 0.04f);
            int n = Len(seconds);
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
            var buf = Noise(period, r);
            var o = new float[n];
            var air = Noise(n, r);
            var bp = new Biquad(); bp.BandPass(1200f, 1f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = i % period, k1 = (k + 1) % period;
                float v = buf[k];
                buf[k] = 0.996f * 0.5f * (buf[k] + buf[k1]);
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * 90f / Rate;
                o[i] = v * Mathf.Exp(-s / (seconds * 0.4f)) + Mathf.Sin(phase) * Mathf.Exp(-s / 0.02f) * 0.6f
                       + bp.Step(air[i]) * Bell(s / 0.12f, 0.3f) * 0.5f;
            }
            return Finish(o, 0.08f, 0.15f, r);
        }

        /// <summary>An arrow striking home: a short, dry thunk and a splinter of grit.</summary>
        public static float[] Thunk(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.14f);
            var o = new float[n];
            var noise = Noise(n, r);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(1200f, 0.7f); l2.LowPass(1200f, 0.7f);
            float f0 = 230f * Vary(r, 0.1f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(85f, f0, Mathf.Exp(-s / 0.012f)) / Rate;
                o[i] = Mathf.Sin(phase) * Mathf.Exp(-s / 0.03f) + l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.01f) * 2f;
            }
            Grit(o, r, 0f, 0.05f, 6, 0.25f);
            return Finish(o, 0.1f, 0.18f, r);
        }

        // ===================================================================================== moving

        /// <summary>
        /// A footstep on stone: the heel's dull thud, the sole scuffing (a short band of noise), and grit crunching
        /// under it. <paramref name="weight"/>: a walk ~0.6, a run 1, a landing more.
        /// </summary>
        public static float[] Step(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            float w = Mathf.Max(0.3f, weight);
            int n = Len(0.12f + 0.08f * w);
            var o = new float[n];
            var noise = Noise(n, r);
            var l1 = new Biquad(); var l2 = new Biquad(); float cut = 520f * Vary(r, 0.2f); l1.LowPass(cut, 0.7f); l2.LowPass(cut, 0.7f);
            var b1 = new Biquad(); var b2 = new Biquad(); float sc = 2100f * Vary(r, 0.25f); b1.BandPass(sc, 1.1f); b2.BandPass(sc, 1.1f);
            float f0 = 120f * Vary(r, 0.15f);
            float heelToSole = 0.012f + 0.02f * (float)r.NextDouble();   // (the heel, then the sole rolling down)
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(55f, f0, Mathf.Exp(-s / 0.015f)) / Rate;
                float thud = (Mathf.Sin(phase) * 0.8f + l2.Step(l1.Step(noise[i])) * 2.5f) * Mathf.Exp(-s / (0.018f + 0.012f * w));
                float s2 = s - heelToSole;
                float scuff = s2 > 0f ? b2.Step(b1.Step(noise[i])) * Mathf.Exp(-s2 / 0.02f) * Mathf.Clamp01(s2 / 0.004f) * 1.4f : 0f;
                o[i] = Soft(thud * w + scuff * (0.5f + 0.4f * w));
            }
            Grit(o, r, 0.005f, 0.06f, 3 + r.Next(5), 0.2f);
            return Finish(o, 0.05f, 0.1f, r);
        }

        /// <summary>Coming down from a jump or a fall: a heavy step on both feet, the knees taking it, grit scattering.</summary>
        public static float[] Land(float weight, int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var a = Step(weight * 1.4f, seed + 1, variant);
            var b = Step(weight * 1.2f, seed + 2, variant);
            int lag = (int)(Rate * (0.008f + 0.02f * (float)r.NextDouble()));   // (one foot a moment after the other)
            var o = new float[a.Length + lag + Len(0.05f)];
            Mix(o, a, 0, 1f);
            Mix(o, b, lag, 0.8f);
            Grit(o, r, 0.01f, 0.12f, 6 + (int)(6 * weight), 0.3f);
            return Finish(o, 0.08f, 0.15f, r);
        }

        /// <summary>Pushing off the ground (or a wall) into a jump: a scuff and the cloth flicking up.</summary>
        public static float[] PushOff(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.25f)];
            Mix(o, Step(0.5f, seed + 3, variant), 0, 0.8f);
            Mix(o, Swing(900f, 0.2f, seed + 4, variant), Len(0.015f), 0.45f);
            return Finish(o, 0.05f, 0.1f, r);
        }

        /// <summary>A rush of air (the dash, the somersault, letting go of the rope): a broad whoosh and the scarf's flutter.</summary>
        public static float[] Rush(float centreHz, float seconds, int seed, int variant = 0) => Swing(centreHz, seconds, seed, variant);

        /// <summary>Rolling out of a fall: two soft thumps (shoulder, back) and the cloth rustling over the stone.</summary>
        public static float[] Roll(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.5f)];
            Mix(o, Step(0.9f, seed + 5, variant), 0, 0.9f);
            Mix(o, Step(0.7f, seed + 6, variant), Len(0.13f + 0.04f * (float)r.NextDouble()), 0.7f);
            var rustle = Scrape(1600f, 0.42f, seed + 7, variant, 0.5f);
            Mix(o, rustle, Len(0.02f), 0.5f);
            return Finish(o, 0.06f, 0.12f, r);
        }

        /// <summary>A hand slapping onto a stone lip and taking the weight: a flat smack and a little grit.</summary>
        public static float[] Grab(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            int n = Len(0.12f);
            var o = new float[n];
            var noise = Noise(n, r);
            var b1 = new Biquad(); var b2 = new Biquad(); float c = 1300f * Vary(r, 0.2f); b1.BandPass(c, 0.9f); b2.BandPass(c, 0.9f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * 140f / Rate;
                o[i] = Soft(b2.Step(b1.Step(noise[i])) * Mathf.Exp(-s / 0.012f) * 3f + Mathf.Sin(phase) * Mathf.Exp(-s / 0.025f) * 0.6f);
            }
            Grit(o, r, 0.01f, 0.06f, 4, 0.25f);
            return Finish(o, 0.08f, 0.14f, r);
        }

        /// <summary>Hauling up over a lip: cloth and boots scraping the stone, a step on top, and his breath.</summary>
        public static float[] Mantle(int seed, int variant = 0)
        {
            var r = Rng(seed, variant);
            var o = new float[Len(0.75f)];
            Mix(o, Scrape(1100f, 0.45f, seed + 8, variant, 0.8f), 0, 0.6f);
            Mix(o, Breath(r, 0.3f, 0.25f), Len(0.05f), 0.35f);
            Mix(o, Step(0.8f, seed + 9, variant), Len(0.5f), 0.9f);
            return Finish(o, 0.06f, 0.12f, r);
        }

        /// <summary>
        /// Something rubbing over stone for a while (a slide, sliding down a wall): rough noise in a band, its grain
        /// coming and going irregularly. <paramref name="roughness"/> 0..1: how gritty.
        /// </summary>
        public static float[] Scrape(float centreHz, float seconds, int seed, int variant = 0, float roughness = 0.7f)
        {
            var r = Rng(seed, variant);
            int n = Len(seconds);
            var o = new float[n];
            var noise = Noise(n, r);
            var b1 = new Biquad(); var b2 = new Biquad(); float c = centreHz * Vary(r, 0.1f); b1.BandPass(c, 0.8f); b2.BandPass(c, 0.8f);
            var lp = new Biquad(); lp.LowPass(c * 0.4f, 0.7f);
            float off = (float)r.NextDouble() * 100f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float grain = 0.55f + 0.45f * (Mathf.PerlinNoise(t * 35f + off, off) * 2f - 0.5f);
                float rough = 1f + roughness * (Mathf.PerlinNoise(t * 180f + off, 3f) - 0.5f) * 1.6f;
                o[i] = (b2.Step(b1.Step(noise[i])) * 1.6f + lp.Step(noise[i]) * 0.6f) * grain * rough * Bell(t / seconds, 0.2f, 0.05f);
            }
            Grit(o, r, 0f, seconds, (int)(seconds * 40f * roughness), 0.15f);
            return Normalise(o);
        }

        /// <summary>
        /// A slide's scrape as a loop (it plays as long as he slides): the same sound without a beginning or an end,
        /// its last 80 ms crossfaded into its first, so it runs on without a seam.
        /// </summary>
        public static float[] ScrapeLoop(float centreHz, float seconds, int seed, float roughness)
        {
            int fade = Len(0.08f);
            // (without the one-shot scrape's swell in and out)
            var steady = ScrapeSteady(centreHz, seconds + 0.08f, seed, roughness);
            int n = steady.Length - fade;
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = steady[i];
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                o[i] = steady[i] * k + steady[n + i] * (1f - k);
            }
            float m = 0f;
            foreach (var v in o) m = Mathf.Max(m, Mathf.Abs(v));
            if (m > 1e-6f) for (int i = 0; i < n; i++) o[i] *= Peak / m;
            return o;
        }

        static float[] ScrapeSteady(float centreHz, float seconds, int seed, float roughness)
        {
            var r = Rng(seed, 0);
            int n = Len(seconds);
            var o = new float[n];
            var noise = Noise(n, r);
            var b1 = new Biquad(); var b2 = new Biquad(); b1.BandPass(centreHz, 0.8f); b2.BandPass(centreHz, 0.8f);
            var lp = new Biquad(); lp.LowPass(centreHz * 0.4f, 0.7f);
            float off = (float)r.NextDouble() * 100f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float grain = 0.55f + 0.45f * (Mathf.PerlinNoise(t * 35f + off, off) * 2f - 0.5f);
                float rough = 1f + roughness * (Mathf.PerlinNoise(t * 180f + off, 3f) - 0.5f) * 1.6f;
                o[i] = (b2.Step(b1.Step(noise[i])) * 1.6f + lp.Step(noise[i]) * 0.6f) * grain * rough;
            }
            Grit(o, r, 0f, seconds, (int)(seconds * 40f * roughness), 0.15f);
            return o;
        }

        /// <summary>
        /// The rope creaking under him as a loop: stick-slip friction - clicks in an uneven rhythm through the rope's
        /// two resonances - played faster and louder as he swings faster.
        /// </summary>
        public static float[] CreakLoop(float seconds, int seed)
        {
            var r = Rng(seed, 0);
            int fade = Len(0.08f);
            int n = Len(seconds);
            int all = n + fade;
            var o = new float[all];
            var noise = Noise(all, r);
            var r1 = new Biquad(); r1.BandPass(420f, 6f);
            var r2 = new Biquad(); r2.BandPass(780f, 5f);
            int next = 0;
            float pulse = 0f;
            for (int i = 0; i < all; i++)
            {
                if (i >= next)
                {
                    pulse = 1f;
                    next = i + (int)(Rate / (90f + 70f * (float)r.NextDouble()));
                }
                float x = noise[i] * pulse;
                pulse *= 0.9f;
                float t = (float)i / n;
                float swell = 0.4f + 0.6f * Mathf.Sin(Mathf.PI * 2f * t) * Mathf.Sin(Mathf.PI * 2f * t);   // (twice a loop: the two ends of a swing)
                o[i] = (r1.Step(x) * 1.3f + r2.Step(x)) * swell;
            }
            return Seamless(o, n, fade);
        }

        /// <summary>
        /// Air streaming past (the scarf sail): broad soft noise, low, swelling and ebbing slowly and unevenly, as a
        /// seamless loop.
        /// </summary>
        public static float[] WindLoop(float centreHz, float seconds, int seed)
        {
            var r = Rng(seed, 0);
            int fade = Len(0.25f);
            int n = Len(seconds);
            int all = n + fade;
            var o = new float[all];
            var noise = Noise(all, r);
            var b1 = new Biquad(); b1.BandPass(centreHz, 0.5f);
            var b2 = new Biquad(); b2.BandPass(centreHz * 2.6f, 1.2f);
            var lp = new Biquad(); lp.LowPass(centreHz * 0.5f, 0.7f);
            float off = (float)r.NextDouble() * 100f;
            for (int i = 0; i < all; i++)
            {
                float t = (float)i / Rate;
                float gust = 0.55f + 0.45f * Mathf.PerlinNoise(t * 1.3f + off, off);
                float flutter = 1f + 0.25f * (Mathf.PerlinNoise(t * 9f + off, 7f) - 0.5f);
                float whistle = Mathf.PerlinNoise(t * 0.7f + off, 11f);   // (now and then a higher edge to it)
                o[i] = (b1.Step(noise[i]) + lp.Step(noise[i]) * 0.8f + b2.Step(noise[i]) * 0.5f * whistle) * gust * flutter;
            }
            return Seamless(o, n, fade);
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

        /// <summary>White noise -1..1, the same for the same seed.</summary>
        public static float[] Noise(int n, int seed) => Noise(n, new System.Random(seed));

        static float[] Noise(int n, System.Random r)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = (float)(r.NextDouble() * 2.0 - 1.0);
            return o;
        }

        /// <summary>Adds <paramref name="src"/> into <paramref name="dst"/> from sample <paramref name="at"/>, scaled.</summary>
        static void Mix(float[] dst, float[] src, int at, float gain)
        {
            for (int i = 0; i < src.Length && at + i < dst.Length; i++) if (at + i >= 0) dst[at + i] += src[i] * gain;
        }

        /// <summary>Droplets of ink scattering: short, bright ticks with a little pitch, spread over a while after a splash.</summary>
        static void Droplets(float[] o, System.Random r, float from, float to, int count, float lowHz, float highHz, float level)
        {
            for (int k = 0; k < count; k++)
            {
                float at = from + (to - from) * Mathf.Pow((float)r.NextDouble(), 1.6f);
                int start = (int)(at * Rate);
                float f = Mathf.Lerp(lowHz, highHz, (float)r.NextDouble());
                float a = level * (0.4f + 0.6f * (float)r.NextDouble());
                int len = Len(0.012f + 0.02f * (float)r.NextDouble());
                for (int i = 0; i < len && start + i < o.Length; i++)
                {
                    float s = (float)i / Rate;
                    // a drop: its pitch rises as it lands (the air cavity closing)
                    o[start + i] += Mathf.Sin(2f * Mathf.PI * f * (1f + 4f * s) * s) * Mathf.Exp(-s / 0.005f) * a;
                }
            }
        }

        /// <summary>Grit: tiny dry clicks (stone crumbs under a foot, under cloth) spread over a while.</summary>
        static void Grit(float[] o, System.Random r, float from, float to, int count, float level)
        {
            for (int k = 0; k < count; k++)
            {
                int start = (int)((from + (to - from) * (float)r.NextDouble()) * Rate);
                float a = level * (0.3f + 0.7f * (float)r.NextDouble());
                int len = Len(0.0015f + 0.002f * (float)r.NextDouble());
                for (int i = 0; i < len && start + i < o.Length; i++)
                    o[start + i] += (float)(r.NextDouble() * 2.0 - 1.0) * a * (1f - (float)i / len);
            }
        }

        /// <summary>A breath (exhaled, voiced a little): noise through the formants of an open vowel, a falling swell.</summary>
        static float[] Breath(System.Random r, float seconds, float voiced)
        {
            int n = Len(seconds);
            var o = new float[n];
            var noise = Noise(n, r);
            var f1 = new Biquad(); f1.BandPass(700f * Vary(r, 0.1f), 4f);
            var f2 = new Biquad(); f2.BandPass(1150f * Vary(r, 0.1f), 5f);
            var air = new Biquad(); air.HighPass(1800f, 0.7f);
            float pitch = 115f * Vary(r, 0.12f), phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += 2f * Mathf.PI * pitch * (1f - 0.15f * t) / Rate;
                float buzz = (Mathf.Repeat(phase / (2f * Mathf.PI), 1f) - 0.5f) * voiced;   // (a sawtooth: the voice)
                float src = noise[i] * 0.6f + buzz;
                o[i] = (f1.Step(src) + 0.6f * f2.Step(src) + 0.15f * air.Step(noise[i])) * Bell(t, 0.15f);
            }
            return Normalise(o, false);
        }

        /// <summary>
        /// The room: a short tail of the stone ruin the sound is heard in (a few echoes off near walls, then a soft
        /// diffuse decay), mixed under the sound, then normalised and faded out.
        /// </summary>
        static float[] Finish(float[] dry, float wet, float seconds, System.Random r)
        {
            int tail = Len(seconds);
            var o = new float[dry.Length + tail];
            Array.Copy(dry, o, dry.Length);
            // a sparse impulse: early reflections (5-35 ms) then a decaying scatter, darker as it goes
            int taps = 28;
            var lp = new Biquad(); lp.LowPass(2800f, 0.7f);
            var wetBuf = new float[o.Length];
            for (int k = 0; k < taps; k++)
            {
                float d = k < 6 ? 0.005f + 0.03f * (float)r.NextDouble() : 0.02f + seconds * (float)r.NextDouble();
                float g = (k < 6 ? 0.55f : 0.35f) * Mathf.Exp(-d / (seconds * 0.35f)) * ((r.Next(2) == 0) ? 1f : -1f);
                int at = Len(d);
                for (int i = 0; i < dry.Length && at + i < wetBuf.Length; i++) wetBuf[at + i] += dry[i] * g;
            }
            for (int i = 0; i < o.Length; i++) o[i] += lp.Step(wetBuf[i]) * wet;
            return Normalise(o);
        }

        /// <summary>Scales <paramref name="x"/> so its loudest sample is <see cref="Peak"/> (and fades its end out, unless it loops).</summary>
        public static float[] Normalise(float[] x, bool fadeEnd = true)
        {
            float m = 0f;
            foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v));
            if (m < 1e-6f) return x;
            float k = Peak / m;
            for (int i = 0; i < x.Length; i++) x[i] *= k;
            if (!fadeEnd) return x;
            // (no click at the end: the last 4 ms fade out)
            int fade = Mathf.Min(x.Length, Rate / 250);
            for (int i = 0; i < fade; i++) x[x.Length - 1 - i] *= (float)i / fade;
            return x;
        }

        /// <summary>A gentle saturation (a hit has a little grit, and nothing clips).</summary>
        static float Soft(float v) => (float)Math.Tanh(v);

        /// <summary>A 12 dB/oct filter (RBJ biquad): band pass (constant peak gain), low pass, high pass.</summary>
        sealed class Biquad
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;

            static void Omega(float hz, float q, out float cos, out float alpha)
            {
                float w = 2f * Mathf.PI * Mathf.Clamp(hz, 20f, Rate * 0.45f) / Rate;
                cos = Mathf.Cos(w);
                alpha = Mathf.Sin(w) / (2f * q);
            }

            void Set(float nb0, float nb1, float nb2, float a0, float na1, float na2)
            {
                b0 = nb0 / a0; b1 = nb1 / a0; b2 = nb2 / a0; a1 = na1 / a0; a2 = na2 / a0;
            }

            public void BandPass(float hz, float q)
            {
                Omega(hz, q, out float c, out float al);
                Set(al, 0f, -al, 1f + al, -2f * c, 1f - al);
            }

            public void LowPass(float hz, float q)
            {
                Omega(hz, q, out float c, out float al);
                Set((1f - c) * 0.5f, 1f - c, (1f - c) * 0.5f, 1f + al, -2f * c, 1f - al);
            }

            public void HighPass(float hz, float q)
            {
                Omega(hz, q, out float c, out float al);
                Set((1f + c) * 0.5f, -(1f + c), (1f + c) * 0.5f, 1f + al, -2f * c, 1f - al);
            }

            public float Step(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
    }
}
