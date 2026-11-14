using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's standing loop, from the walk-cycle sheet (ArtSource/ref/walk_cycle.png): the rig was fitted to each
    /// of its 12 side-view silhouettes (ArtSource/hero_fit_walk.py) and the angles tabulated
    /// (ArtSource/walk_to_idle_table.py - generated, do not edit by hand). He stands in the concept's wide stance - the
    /// near leg forward with the knee eased, the far leg back and straight - and his arms and head sway gently.
    /// </summary>
    public static class HeroIdleCycle
    {
        /// <summary>Seconds for one loop of the 12 frames.</summary>
        public const float Duration = 3.6f;

        public static readonly HeroPose[] Frames =
        {
            new HeroPose { spine = 0.9f, head = -4.8f, bob = 0.0127f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -7.5f, elbowL = 8.1f, armR = 23.2f, elbowR = 8.0f },
            new HeroPose { spine = 0.5f, head = -4.9f, bob = 0.0140f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -9.8f, elbowL = 8.2f, armR = 21.3f, elbowR = 8.0f },
            new HeroPose { spine = 0.7f, head = -3.7f, bob = 0.0155f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -15.7f, elbowL = 8.4f, armR = 19.5f, elbowR = 8.0f },
            new HeroPose { spine = 0.9f, head = -2.1f, bob = 0.0143f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -22.1f, elbowL = 8.7f, armR = 18.6f, elbowR = 8.0f },
            new HeroPose { spine = 1.0f, head = 0.4f, bob = 0.0077f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -22.9f, elbowL = 9.4f, armR = 19.1f, elbowR = 8.0f },
            new HeroPose { spine = 1.1f, head = 3.5f, bob = 0.0012f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -17.6f, elbowL = 9.9f, armR = 21.0f, elbowR = 8.0f },
            new HeroPose { spine = 1.3f, head = 5.1f, bob = -0.0020f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -11.9f, elbowL = 9.7f, armR = 23.8f, elbowR = 8.0f },
            new HeroPose { spine = 1.7f, head = 4.4f, bob = -0.0027f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -9.2f, elbowL = 9.0f, armR = 25.9f, elbowR = 8.0f },
            new HeroPose { spine = 1.9f, head = 2.9f, bob = -0.0010f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -8.2f, elbowL = 8.4f, armR = 26.6f, elbowR = 8.0f },
            new HeroPose { spine = 2.1f, head = 2.1f, bob = 0.0018f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -7.6f, elbowL = 8.1f, armR = 26.2f, elbowR = 8.0f },
            new HeroPose { spine = 2.1f, head = 0.6f, bob = 0.0050f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -7.3f, elbowL = 8.0f, armR = 25.3f, elbowR = 8.0f },
            new HeroPose { spine = 1.8f, head = -2.4f, bob = 0.0095f, thighL = 20.8f, kneeL = 10.5f, footL = -10.3f, thighR = -23.2f, kneeR = 0.0f, footR = 23.2f, armL = -7.3f, elbowL = 8.0f, armR = 24.5f, elbowR = 8.0f },
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
