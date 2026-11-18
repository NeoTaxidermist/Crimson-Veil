using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>A creature's body pose: joint angles (degrees, + = forward/up) and how the body floats and lunges.</summary>
    public struct CreaturePose
    {
        public float spine, chest, head;
        public float armL, elbowL, armR, elbowR;
        /// <summary>Height of the body above its rest (m).</summary>
        public float lift;
        /// <summary>How far the body is thrown forward, in the way it faces (m; - = drawn back).</summary>
        public float lunge;
    }

    /// <summary>
    /// Key poses of the enemies, engine independent. Movement is driven by the distance travelled (a gait phase), so
    /// the limbs never paddle in place; attacks are staged as anticipation - action - follow-through:
    /// the body coils back before a blow, the leading arm strikes first and the other follows, and the body overshoots
    /// and settles after it.
    /// </summary>
    public static class CreatureAnimation
    {
        static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        /// <summary>Fast in, slow out (a whip of a blow).</summary>
        static float Snap(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - t); }
        /// <summary>A settling overshoot: 1 at t=0, swings past 0 and dies down.</summary>
        public static float Settle(float t) => Mathf.Exp(-4.5f * t) * Mathf.Cos(9f * t);

        /// <summary>Distance (m) of one full stroke of the wraith's claws as it drifts.</summary>
        public const float WraithStride = 1.8f;

        /// <summary>
        /// The ink wraith. <paramref name="t"/> is 0..1 through the current state (wind-up, strike, recover, hurt),
        /// <paramref name="speed01"/> its drift speed against its chase speed, <paramref name="phase"/> 0..1 the
        /// stroke of its claws (advanced by distance), <paramref name="time"/> for breathing and trembling.
        /// </summary>
        public static CreaturePose Wraith(EnemyState state, float t, float speed01, float phase, float time)
        {
            float stroke = Mathf.Sin(phase * Mathf.PI * 2f);
            float breathe = Mathf.Sin(time * 2.1f);
            speed01 = Mathf.Clamp01(speed01);
            var p = new CreaturePose { spine = 4f, chest = 2f, head = -3f, elbowL = 25f, elbowR = 30f };
            switch (state)
            {
                case EnemyState.Patrol:
                    // drifting: the claws paddle slowly with the distance, the body leans a little into the drift
                    p.spine = 4f + 5f * speed01;
                    p.armL = 12f + breathe * 6f + stroke * 16f * speed01;
                    p.armR = 8f - breathe * 6f - stroke * 16f * speed01;
                    p.elbowL = 28f - stroke * 10f * speed01;
                    p.elbowR = 32f + stroke * 10f * speed01;
                    break;
                case EnemyState.Chase:
                    // hunting: stooped into the chase, the claws reach out in turn and rake back (it pulls itself on)
                    p.spine = 7f + 7f * speed01; p.chest = 4f; p.head = -9f;
                    p.armL = 38f + stroke * 42f;
                    p.armR = 38f - stroke * 42f;
                    p.elbowL = 22f + (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 14f;
                    p.elbowR = 22f + (1f + Mathf.Cos(phase * Mathf.PI * 2f)) * 14f;
                    p.lift = -0.04f * speed01;
                    break;
                case EnemyState.Windup:
                {
                    // anticipation: it rears up and back, the claws thrown up and forward over the head (the strike arm highest; straight up they vanish behind the hood), held
                    // trembling at the top - then it comes
                    float w = Ease(t / 0.55f);
                    float tremble = Mathf.Sin(time * 47f) * 3.5f * Ease((t - 0.55f) / 0.3f);
                    p.spine = Mathf.Lerp(4f, -18f, w) + tremble * 0.4f;
                    p.chest = Mathf.Lerp(2f, -9f, w);
                    p.head = Mathf.Lerp(-3f, 10f, w);
                    p.armR = Mathf.Lerp(20f, 138f, w) + tremble;
                    p.armL = Mathf.Lerp(20f, 122f, w) - tremble;
                    p.elbowR = Mathf.Lerp(30f, 55f, w);
                    p.elbowL = Mathf.Lerp(30f, 45f, w);
                    p.lift = 0.3f * w;
                    p.lunge = -0.22f * w;
                    break;
                }
                case EnemyState.Strike:
                {
                    // action: the strike arm whips down first, the other rakes after it; the body snaps forward and low
                    float lead = Snap(t / 0.55f), follow = Snap((t - 0.25f) / 0.6f);
                    p.armR = Mathf.Lerp(138f, 30f, lead);
                    p.armL = Mathf.Lerp(122f, 45f, follow);
                    p.elbowR = Mathf.Lerp(55f, 4f, lead);
                    p.elbowL = Mathf.Lerp(45f, 8f, follow);
                    p.spine = Mathf.Lerp(-18f, 24f, lead);
                    p.chest = Mathf.Lerp(-9f, 8f, lead);
                    p.head = Mathf.Lerp(10f, -10f, lead);
                    p.lift = Mathf.Lerp(0.3f, -0.08f, lead);
                    p.lunge = Mathf.Lerp(-0.22f, 0.35f, lead);
                    break;
                }
                case EnemyState.Recover:
                {
                    // follow-through: the claws swing on past the body, it overshoots, sags, and gathers itself
                    float s = Settle(t * 1.4f);
                    float back = Ease(t / 0.8f);
                    // (the swing-through starts from where the blow ended: no jump)
                    p.armR = Mathf.Lerp(30f, 8f, back) - 40f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.45f));
                    p.armL = Mathf.Lerp(45f, 12f, back) - 30f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.08f) / 0.45f));
                    p.elbowR = Mathf.Lerp(4f, 30f, back);
                    p.elbowL = Mathf.Lerp(8f, 26f, back);
                    p.spine = Mathf.Lerp(6f, 24f, s * 0.5f + 0.5f * (1f - back));
                    p.chest = 3f + 5f * s;
                    p.head = Mathf.Lerp(-10f, 2f, back);
                    p.lift = Mathf.Lerp(-0.08f, 0f, back);
                    p.lunge = Mathf.Lerp(0.35f, 0f, back);
                    break;
                }
                case EnemyState.Hurt:
                {
                    // recoil: snapped back from the blow, arms flung, then springing back
                    float r = 1f - Ease(t);
                    float jolt = Settle(t * 0.9f);
                    p.spine = -20f * jolt; p.chest = -12f * jolt; p.head = 18f * jolt;
                    p.armL = -45f * r + 10f; p.armR = 40f * r + 10f;
                    p.elbowL = p.elbowR = 20f + 45f * r;
                    p.lunge = -0.3f * Mathf.Max(0f, jolt);
                    p.lift = 0.1f * r;
                    break;
                }
                case EnemyState.Dead:
                    p.spine = -8f; p.chest = -14f; p.head = 24f; p.armL = p.armR = 120f; p.elbowL = p.elbowR = 10f;
                    break;
            }
            return p;
        }

        /// <summary>Distance (m) of one full step cycle (two steps) of the shield monk's heavy walk.</summary>
        public const float MonkStride = 1.3f;

        /// <summary>
        /// The shield monk. The left arm (towards the camera) holds the shield forward; the right swings with the
        /// heavy step. <paramref name="t"/> is 0..1 through the wind-up / bash / recovery, <paramref name="moving"/>
        /// 0..1 how much he walks, <paramref name="phase"/> 0..1 the step cycle, <paramref name="armed"/> whether he
        /// still has the shield.
        /// </summary>
        public static CreaturePose Monk(ShieldMonk.State state, float t, float moving, float phase, bool armed, float time)
        {
            float step = Mathf.Sin(phase * Mathf.PI * 2f);
            float plant = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f));   // twice per cycle the weight drops onto a foot
            float breathe = Mathf.Sin(time * 1.3f);
            moving = Mathf.Clamp01(moving);
            var p = new CreaturePose
            {
                spine = 6f + 3f * moving + breathe * 1.2f,
                chest = 3f,
                head = -4f + plant * 3f * moving,
                armL = 62f, elbowL = 48f,                         // the shield held up in front
                armR = 8f + step * 22f * moving, elbowR = 18f + Mathf.Max(0f, step) * 18f * moving,
                lift = (plant - 0.6f) * 0.07f * moving,
            };
            if (!armed)
            {
                // disarmed: hunched, both hands groping empty in front of him, staggering
                p.spine = 20f + Mathf.Sin(time * 3.1f) * 4f;
                p.head = 8f;
                p.armL = 30f + Mathf.Sin(time * 2.3f) * 12f; p.elbowL = 40f;
                p.armR = 22f - Mathf.Sin(time * 2.3f) * 12f; p.elbowR = 45f;
                p.lift = -0.06f;
                return p;
            }
            switch (state)
            {
                case ShieldMonk.State.Windup:
                {
                    // he plants himself and draws back behind the shield, the shield arm cocked
                    float w = Ease(t);
                    p.spine = Mathf.Lerp(p.spine, -12f, w); p.chest = Mathf.Lerp(3f, -6f, w); p.head = Mathf.Lerp(-4f, 6f, w);
                    p.armL = Mathf.Lerp(62f, 95f, w); p.elbowL = Mathf.Lerp(48f, 85f, w);
                    p.armR = Mathf.Lerp(p.armR, -25f, w); p.elbowR = Mathf.Lerp(p.elbowR, 30f, w);
                    p.lift = -0.08f * w;
                    p.lunge = -0.2f * w;
                    break;
                }
                case ShieldMonk.State.Bash:
                {
                    // the whole body behind the shield: arm driven straight out, he throws himself after it
                    float b = Snap(t / 0.7f);
                    p.spine = Mathf.Lerp(-12f, 26f, b); p.chest = Mathf.Lerp(-6f, 10f, b); p.head = Mathf.Lerp(6f, -8f, b);
                    p.armL = Mathf.Lerp(95f, 88f, b); p.elbowL = Mathf.Lerp(85f, 4f, b);
                    p.armR = Mathf.Lerp(-25f, -40f, b); p.elbowR = 20f;
                    p.lift = Mathf.Lerp(-0.08f, -0.12f, b);
                    p.lunge = Mathf.Lerp(-0.2f, 0.4f, b);
                    break;
                }
                case ShieldMonk.State.Recover:
                {
                    // the weight of it carries him on, he sags and heaves himself back behind the shield
                    float back = Ease(t);
                    float s = Settle(t * 1.2f);
                    p.spine = Mathf.Lerp(26f, p.spine, back) + 6f * s;
                    p.chest = Mathf.Lerp(10f, 3f, back);
                    p.head = Mathf.Lerp(-8f, -4f, back);
                    p.armL = Mathf.Lerp(88f, 62f, back); p.elbowL = Mathf.Lerp(4f, 48f, back);
                    p.armR = Mathf.Lerp(-40f, p.armR, back); p.elbowR = Mathf.Lerp(20f, p.elbowR, back);
                    p.lift = Mathf.Lerp(-0.12f, 0f, back);
                    p.lunge = Mathf.Lerp(0.4f, 0f, back);
                    break;
                }
            }
            return p;
        }

        /// <summary>
        /// Squash and stretch of a hopping body: squashed flat landing (<paramref name="sinceLanding"/> s ago) and
        /// crouching to leap (<paramref name="toLeap"/> s before it), stretched along its flight at
        /// <paramref name="vy"/> m/s. Returns the vertical scale (the width keeps the volume: 1/sqrt).
        /// </summary>
        public static float HopSquash(bool grounded, float sinceLanding, float toLeap, float vy)
        {
            if (!grounded) return 1f + Mathf.Clamp(Mathf.Abs(vy) * 0.035f, 0f, 0.3f);
            float land = sinceLanding < 0.18f ? Mathf.Sin(sinceLanding / 0.18f * Mathf.PI) * 0.35f : 0f;
            float crouch = toLeap < 0.12f ? (1f - toLeap / 0.12f) * 0.25f : 0f;
            return 1f - Mathf.Max(land, crouch);
        }
    }
}
