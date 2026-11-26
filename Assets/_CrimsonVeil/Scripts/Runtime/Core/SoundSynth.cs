using System;
using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The game's sounds, made in code (no recordings): noise shaped by filters and envelopes, falling sine thumps,
    /// metal partials, a plucked string. Each cue is a short mono buffer, deterministic (the same every run), peaking
    /// at <see cref="Peak"/>.
    /// </summary>
    public static class SoundSynth
    {
        public const int Rate = 44100;
        /// <summary>Every sound is normalised to this peak (headroom for several at once).</summary>
        public const float Peak = 0.8f;

        // --- the cues -------------------------------------------------------------------------------------------
        // (noise is white: every filter on it is two 12 dB/oct stages - with gentler ones the hiss above the band
        // carried the sound, and a blow came out a 5 kHz hiss)

        /// <summary>A blade cutting the air: noise through a band that sweeps up and back, swelling and fading.</summary>
        public static float[] Swing(float centreHz, float seconds, int seed)
        {
            int n = Len(seconds);
            var noise = Noise(n, seed);
            var o = new float[n];
            var b1 = new Biquad(); var b2 = new Biquad();
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float c = centreHz * (0.6f + 0.8f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f)));
                if ((i & 31) == 0) { b1.BandPass(c, 1.4f); b2.BandPass(c, 1.4f); }
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f)), 2f);
                o[i] = b2.Step(b1.Step(noise[i])) * env;
            }
            return Normalise(o);
        }

        /// <summary>
        /// A blow landing: a falling thump (the body of it), a short crack at the very start (the edge biting) and a
        /// dull wet splash of ink after it. <paramref name="weight"/> 1 is a plain hit, more a heavier one.
        /// </summary>
        public static float[] Hit(float weight, int seed)
        {
            float w = Mathf.Max(0.5f, weight);
            int n = Len(0.24f * w);
            var o = new float[n];
            var noise = Noise(n, seed);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(900f / w, 0.7f); l2.LowPass(900f / w, 0.7f);
            var h1 = new Biquad(); var h2 = new Biquad(); h1.HighPass(2500f, 0.7f); h2.HighPass(2500f, 0.7f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float f = Mathf.Lerp(46f, 150f / w, Mathf.Exp(-s / 0.03f));
                phase += 2f * Mathf.PI * f / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-s / (0.06f * w));
                float crack = h2.Step(h1.Step(noise[i])) * Mathf.Exp(-s / 0.005f) * 0.6f;
                float splash = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / (0.05f * w)) * 3f * Mathf.Clamp01(s / 0.006f);
                o[i] = Soft(thump * 1.4f + crack + splash);
            }
            return Normalise(o);
        }

        /// <summary>The hero struck: a deep, dull blow and a muffled crunch - heavier and lower than a hit he lands.</summary>
        public static float[] Hurt(int seed)
        {
            int n = Len(0.36f);
            var o = new float[n];
            var noise = Noise(n, seed);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(900f, 0.7f); l2.LowPass(900f, 0.7f);   // (at 420 Hz the whole of it sat under what a laptop speaker plays)
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float f = Mathf.Lerp(62f, 160f, Mathf.Exp(-s / 0.045f));
                phase += 2f * Mathf.PI * f / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-s / 0.12f);
                float crunch = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.07f) * 6f;
                o[i] = Soft(1.1f * thump + crunch);
            }
            return Normalise(o);
        }

        /// <summary>Metal struck (the shield blocking): inharmonic partials, each dying away at its own rate, and a click.</summary>
        public static float[] Clank(float baseHz, float ring, int seed)
        {
            int n = Len(0.5f * ring);
            var o = new float[n];
            var noise = Noise(n, seed);
            float[] ratio = { 1f, 1.93f, 3.02f, 4.71f, 6.1f };
            float[] amp = { 1f, 0.6f, 0.45f, 0.3f, 0.18f };
            float[] decay = { 0.2f, 0.13f, 0.09f, 0.06f, 0.04f };
            var h1 = new Biquad(); h1.HighPass(2500f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float v = 0f;
                for (int k = 0; k < ratio.Length; k++)
                    v += amp[k] * Mathf.Sin(2f * Mathf.PI * baseHz * ratio[k] * s) * Mathf.Exp(-s / (decay[k] * ring));
                v += h1.Step(noise[i]) * Mathf.Exp(-s / 0.004f) * 1.2f;
                o[i] = v;
            }
            return Normalise(o);
        }

        /// <summary>A foe undone: its ink bursts and drains away - noise falling from bright to dark, a low swell under it.</summary>
        public static float[] Dissolve(int seed)
        {
            int n = Len(0.6f);
            var o = new float[n];
            var noise = Noise(n, seed);
            var l1 = new Biquad(); var l2 = new Biquad();
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float t = (float)i / n;
                if ((i & 31) == 0) { float cut = Mathf.Lerp(2400f, 150f, Mathf.Pow(t, 0.6f)); l1.LowPass(cut, 0.8f); l2.LowPass(cut, 0.8f); }
                float hiss = l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.22f) * Mathf.Clamp01(s / 0.008f);
                phase += 2f * Mathf.PI * Mathf.Lerp(70f, 38f, t) / Rate;
                float low = Mathf.Sin(phase) * Mathf.Exp(-s / 0.16f) * 0.6f;
                o[i] = Soft(hiss * 2.5f + low);
            }
            return Normalise(o);
        }

        /// <summary>The whip's crack: a sharp snap and a very short tail.</summary>
        public static float[] Crack(int seed)
        {
            int n = Len(0.12f);
            var o = new float[n];
            var noise = Noise(n, seed);
            var h1 = new Biquad(); h1.HighPass(1500f, 0.7f);
            var bp = new Biquad(); bp.BandPass(1800f, 1.2f);
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                float snap = h1.Step(noise[i]) * Mathf.Exp(-s / 0.004f) * 2f;
                float tail = bp.Step(noise[i]) * Mathf.Exp(-s / 0.03f);
                o[i] = snap + tail;
            }
            return Normalise(o);
        }

        /// <summary>A string let go (the bow): a plucked string (Karplus-Strong) with a soft thump of the limbs.</summary>
        public static float[] Pluck(float hz, float seconds, int seed)
        {
            int n = Len(seconds);
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / hz));
            var buf = Noise(period, seed);
            var o = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = i % period;
                int k1 = (k + 1) % period;
                float v = buf[k];
                buf[k] = 0.996f * 0.5f * (buf[k] + buf[k1]);
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * 90f / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-s / 0.02f) * 0.6f;
                o[i] = v * Mathf.Exp(-s / (seconds * 0.4f)) + thump;
            }
            return Normalise(o);
        }

        /// <summary>An arrow striking home: a short, dry thunk.</summary>
        public static float[] Thunk(int seed)
        {
            int n = Len(0.12f);
            var o = new float[n];
            var noise = Noise(n, seed);
            var l1 = new Biquad(); var l2 = new Biquad(); l1.LowPass(1200f, 0.7f); l2.LowPass(1200f, 0.7f);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(85f, 230f, Mathf.Exp(-s / 0.012f)) / Rate;
                o[i] = Mathf.Sin(phase) * Mathf.Exp(-s / 0.03f) + l2.Step(l1.Step(noise[i])) * Mathf.Exp(-s / 0.01f) * 2f;
            }
            return Normalise(o);
        }

        // --- the means ------------------------------------------------------------------------------------------

        public static int Len(float seconds) => Mathf.Max(16, Mathf.RoundToInt(seconds * Rate));

        /// <summary>White noise -1..1, the same for the same seed.</summary>
        public static float[] Noise(int n, int seed)
        {
            var r = new System.Random(seed);
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = (float)(r.NextDouble() * 2.0 - 1.0);
            return o;
        }

        /// <summary>Scales <paramref name="x"/> so its loudest sample is <see cref="Peak"/>.</summary>
        public static float[] Normalise(float[] x)
        {
            float m = 0f;
            foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v));
            if (m < 1e-6f) return x;
            float k = Peak / m;
            for (int i = 0; i < x.Length; i++) x[i] *= k;
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
