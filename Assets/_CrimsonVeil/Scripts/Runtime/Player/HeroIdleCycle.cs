using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's standing loop, from ArtSource/ref/anim_sheet.png (idle row, 8 frames; fitted by ArtSource/hero_fit_anim.py): the rig was fitted to each side-view silhouette and the angles tabulated
    /// (ArtSource/walk_to_idle_table.py - generated, do not edit by hand). He stands in the concept's wide stance - the
    /// near leg forward with the knee eased, the far leg back and straight - and his arms and head sway gently.
    /// </summary>
    public static class HeroIdleCycle
    {
        /// <summary>Seconds for one loop of the 12 frames.</summary>
        public const float Duration = 3.2f;

        public static readonly HeroPose[] Frames =
        {
            new HeroPose { spine = -4.7f, head = 11.5f, bob = 0.0222f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -52.0f, elbowL = 46.4f, armR = 21.8f, elbowR = 9.2f },
            new HeroPose { spine = -5.2f, head = 10.9f, bob = 0.0235f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -54.1f, elbowL = 47.6f, armR = 21.0f, elbowR = 9.5f },
            new HeroPose { spine = -5.9f, head = 7.2f, bob = 0.0254f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -58.5f, elbowL = 51.2f, armR = 20.0f, elbowR = 10.0f },
            new HeroPose { spine = -6.1f, head = 3.8f, bob = 0.0260f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -62.4f, elbowL = 54.9f, armR = 19.8f, elbowR = 10.6f },
            new HeroPose { spine = -6.3f, head = 3.4f, bob = 0.0260f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -63.7f, elbowL = 56.8f, armR = 20.2f, elbowR = 10.8f },
            new HeroPose { spine = -6.7f, head = 5.0f, bob = 0.0260f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -62.2f, elbowL = 56.0f, armR = 20.9f, elbowR = 10.0f },
            new HeroPose { spine = -6.6f, head = 6.9f, bob = 0.0254f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -58.3f, elbowL = 52.6f, armR = 21.4f, elbowR = 9.0f },
            new HeroPose { spine = -5.5f, head = 9.3f, bob = 0.0235f, thighL = 18.1f, kneeL = 7.5f, footL = -10.6f, thighR = -22.0f, kneeR = 0.0f, footR = 22.0f, armL = -53.9f, elbowL = 48.5f, armR = 21.9f, elbowR = 8.9f },
        };

        /// <summary>The pose at <paramref name="time"/> seconds into the loop (frames blended smoothly, the loop wraps).</summary>
        public static HeroPose Sample(float time)
        {
            int n = Frames.Length;
            float f = Mathf.Repeat(time / Duration, 1f) * n;
            int i = Mathf.FloorToInt(f) % n;
            float u = f - Mathf.Floor(f);
            u = u * u * (3f - 2f * u);
            return HeroPose.Lerp(Frames[i], Frames[(i + 1) % n], u);
        }
    }
}
