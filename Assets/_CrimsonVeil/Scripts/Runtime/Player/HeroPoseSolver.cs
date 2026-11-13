using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Sagittal-plane pose in degrees. Sign convention: positive swings the bone's child towards the
    /// character's forward direction (thigh/upper arm/spine/elbow); knee positive = bend (shin goes back);
    /// foot positive = toes up. <see cref="bob"/> is a vertical hips offset in metres.
    /// </summary>
    public struct HeroPose
    {
        public float spine, chest, head, bob;
        public float thighL, kneeL, footL, thighR, kneeR, footR;
        public float armL, elbowL, armR, elbowR;

        public static HeroPose Lerp(in HeroPose a, in HeroPose b, float t)
        {
            return new HeroPose
            {
                spine = Mathf.Lerp(a.spine, b.spine, t), chest = Mathf.Lerp(a.chest, b.chest, t),
                head = Mathf.Lerp(a.head, b.head, t), bob = Mathf.Lerp(a.bob, b.bob, t),
                thighL = Mathf.Lerp(a.thighL, b.thighL, t), kneeL = Mathf.Lerp(a.kneeL, b.kneeL, t), footL = Mathf.Lerp(a.footL, b.footL, t),
                thighR = Mathf.Lerp(a.thighR, b.thighR, t), kneeR = Mathf.Lerp(a.kneeR, b.kneeR, t), footR = Mathf.Lerp(a.footR, b.footR, t),
                armL = Mathf.Lerp(a.armL, b.armL, t), elbowL = Mathf.Lerp(a.elbowL, b.elbowL, t),
                armR = Mathf.Lerp(a.armR, b.armR, t), elbowR = Mathf.Lerp(a.elbowR, b.elbowR, t),
            };
        }

        public bool IsFinite()
        {
            float s = spine + chest + head + bob + thighL + kneeL + footL + thighR + kneeR + footR + armL + elbowL + armR + elbowR;
            return !float.IsNaN(s) && !float.IsInfinity(s);
        }
    }

    public struct HeroState
    {
        public float speed;      // |vx|
        public float maxSpeed;
        public float verticalVelocity;
        public bool grounded;
        public bool dashing;
        public bool attacking;
        public bool wallSliding;
        public bool hanging;
        public bool climbing;
        public float climbProgress;
        /// <summary>0..1 how much a veil weapon is formed — the arm rises to hold it.</summary>
        public float weaponHeld;
        /// <summary>Weapon swing in degrees (+ = up) — the sword arm follows the blade.</summary>
        public float swingAngle;
    }

    /// <summary>
    /// Procedural locomotion: the gait phase advances with distance travelled (no foot sliding),
    /// stride and amplitude scale with speed, plus jump/fall/dash/land/attack overlays.
    /// </summary>
    public sealed class HeroPoseSolver
    {
        public float walkStride = 1.0f;
        public float runStride = 2.2f;
        public float sharpness = 18f;
        public float landDuration = 0.22f;

        /// <summary>Sword-arm angle when standing ready (blade forward and low).</summary>
        public const float HoldArm = 28f;
        /// <summary>Sword-arm angle when running (blade trailing low behind).</summary>
        public const float RunHoldArm = -32f;

        public float Phase { get; private set; }
        public HeroPose Current { get; private set; }
        public float LandWeight => landTimer > 0f ? landTimer / landDuration : 0f;

        float time;
        float landTimer;
        bool wasGrounded = true;
        bool initialised;

        public HeroPose Step(float dt, HeroState s)
        {
            if (dt <= 0f) return Current;
            time += dt;

            float run = Mathf.Clamp01(s.speed / Mathf.Max(0.01f, s.maxSpeed));
            float stride = Mathf.Lerp(walkStride, runStride, run);
            if (s.grounded) Phase = Mathf.Repeat(Phase + s.speed * dt / stride, 1f);

            if (s.grounded && !wasGrounded) landTimer = landDuration;
            wasGrounded = s.grounded;
            landTimer = Mathf.Max(0f, landTimer - dt);

            HeroPose target;
            if (s.climbing) target = ClimbPose(s.climbProgress);
            else if (s.hanging) target = HangPose(time);
            else if (s.wallSliding) target = WallSlidePose(time);
            else if (s.dashing) target = DashPose();
            else if (!s.grounded) target = AirPose(s.verticalVelocity);
            else target = GroundPose(run);

            // Landing absorbs into the knees.
            float land = LandWeight;
            if (land > 0f)
            {
                float k = Mathf.Sin(land * Mathf.PI * 0.5f);
                target.thighL += 30f * k; target.thighR += 25f * k;
                target.kneeL += 50f * k; target.kneeR += 45f * k;
                target.spine += 14f * k;
                target.bob -= 0.14f * k;
                target.armL += 15f * k; target.armR += 15f * k;
            }

            if (s.weaponHeld > 0f && !s.hanging && !s.climbing)
            {
                // Hold the weapon where the veil's grip is (forward and a little below the chest).
                // Ready stance: blade held forward-low when standing, trailing low behind when running
                // (like the key art). The weapon follows the forearm, so this *is* the weapon's pose.
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(s.weaponHeld));
                float hold = Mathf.Lerp(HoldArm, RunHoldArm, Mathf.SmoothStep(0f, 1f, run));
                float sway = Mathf.Sin(time * 1.4f) * 3f * (1f - run) + Mathf.Sin(Phase * Mathf.PI * 4f) * 6f * run;
                target.armR = Mathf.Lerp(target.armR, hold + sway, w);
                target.elbowR = Mathf.Lerp(target.elbowR, Mathf.Lerp(18f, 30f, run), w);
            }

            if (s.attacking)
            {
                // The arm drives the blade: raised on the wind-up, cutting down through the swing,
                // the body turning into it and the free arm counter-balancing.
                target.armR = HoldArm + s.swingAngle * 0.95f;
                target.elbowR = Mathf.Lerp(28f, 4f, Mathf.Abs(s.swingAngle) / 100f);
                target.chest += s.swingAngle * -0.1f + 4f;
                target.spine += Mathf.Max(0f, -s.swingAngle) * 0.12f;
                target.head += s.swingAngle * -0.05f;
                target.armL = Mathf.Lerp(target.armL, -25f - s.swingAngle * 0.3f, 0.7f);
            }

            if (!initialised)
            {
                Current = target;
                initialised = true;
            }
            else
            {
                float t = MovementMath.Damp(s.grounded ? sharpness : sharpness * 0.6f, dt);
                Current = HeroPose.Lerp(Current, target, t);
            }
            return Current;
        }

        HeroPose GroundPose(float run)
        {
            float a = Phase * Mathf.PI * 2f;
            float sinA = Mathf.Sin(a), cosA = Mathf.Cos(a);
            float sinB = -sinA, cosB = -cosA; // right leg is half a cycle apart

            float thighAmp = Mathf.Lerp(0f, 48f, Mathf.SmoothStep(0f, 1f, run * 1.6f));
            float swingBend = Mathf.Lerp(0f, 95f, run);
            float baseBend = Mathf.Lerp(4f, 14f, run);
            float armAmp = Mathf.Lerp(0f, 55f, run);
            float breathe = Mathf.Sin(time * 1.7f) * (1f - run);

            var p = new HeroPose
            {
                spine = Mathf.Lerp(2f, 15f, run) + breathe * 1.2f,
                chest = Mathf.Lerp(0f, 5f, run) + breathe * 1.5f,
                head = -Mathf.Lerp(0f, 10f, run) + Mathf.Sin(time * 0.6f) * 2f * (1f - run),
                bob = (Mathf.Abs(sinA) - 0.5f) * Mathf.Lerp(0f, 0.07f, run) - 0.035f * run + breathe * 0.006f,

                thighL = sinA * thighAmp + 3f * (1f - run),
                kneeL = baseBend + swingBend * Mathf.Max(0f, cosA) + 4f * (1f - run),
                thighR = sinB * thighAmp - 2f * (1f - run),
                kneeR = baseBend + swingBend * Mathf.Max(0f, cosB) + 7f * (1f - run),

                armL = -sinA * armAmp + 4f * (1f - run) + breathe * 2f,
                elbowL = Mathf.Lerp(12f, 85f, run),
                armR = -sinB * armAmp + 2f * (1f - run) - breathe * 2f,
                elbowR = Mathf.Lerp(16f, 85f, run),
            };
            p.footL = -0.35f * p.thighL + 0.15f * p.kneeL * Mathf.Max(0f, cosA);
            p.footR = -0.35f * p.thighR + 0.15f * p.kneeR * Mathf.Max(0f, cosB);
            return p;
        }

        static HeroPose AirPose(float vy)
        {
            var rise = new HeroPose
            {
                spine = 10f, chest = 4f, head = -6f, bob = 0f,
                thighL = 62f, kneeL = 95f, footL = -10f,
                thighR = -22f, kneeR = 45f, footR = -25f,
                armL = -35f, elbowL = 40f, armR = 60f, elbowR = 55f,
            };
            var fall = new HeroPose
            {
                spine = 2f, chest = -4f, head = 6f, bob = 0f,
                thighL = 28f, kneeL = 40f, footL = 10f,
                thighR = -6f, kneeR = 22f, footR = 0f,
                armL = 70f, elbowL = 25f, armR = -25f, elbowR = 30f,
            };
            float f = Mathf.Clamp01((2f - vy) / 10f);
            return HeroPose.Lerp(rise, fall, f);
        }

        /// <summary>Both hands on the lip above, body hanging, legs gently swinging.</summary>
        public static HeroPose HangPose(float time)
        {
            float swing = Mathf.Sin(time * 2.2f) * 6f;
            return new HeroPose
            {
                spine = -6f, chest = -8f, head = 10f, bob = 0f,
                thighL = 8f + swing, kneeL = 18f, footL = -15f,
                thighR = -4f + swing, kneeR = 28f, footR = -20f,
                armL = 172f, elbowL = 8f, armR = 168f, elbowR = 12f,
            };
        }

        /// <summary>Mantle: pull up (arms push down), knee comes over the lip, then stand.</summary>
        public static HeroPose ClimbPose(float t)
        {
            float pull = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
            float stand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.55f) / 0.45f));
            var hang = HangPose(0f);
            var mid = new HeroPose
            {
                spine = 35f, chest = 15f, head = -10f, bob = -0.1f,
                thighL = 95f, kneeL = 120f, footL = -10f,
                thighR = 10f, kneeR = 40f, footR = -20f,
                armL = 40f, elbowL = 70f, armR = 30f, elbowR = 80f,
            };
            var up = new HeroPose { spine = 6f, chest = 2f, thighL = 10f, kneeL = 15f, thighR = -5f, kneeR = 10f, armL = 10f, elbowL = 20f, armR = 5f, elbowR = 25f };
            return HeroPose.Lerp(HeroPose.Lerp(hang, mid, pull), up, stand);
        }

        /// <summary>Facing the wall: one hand braced high, a foot against the stone.</summary>
        public static HeroPose WallSlidePose(float time)
        {
            float jitter = Mathf.Sin(time * 30f) * 1.5f; // scraping down
            return new HeroPose
            {
                spine = -4f, chest = -6f, head = 12f, bob = 0f,
                thighL = 55f + jitter, kneeL = 85f, footL = 20f,
                thighR = 12f, kneeR = 30f, footR = 0f,
                armL = 130f, elbowL = 25f, armR = 45f + jitter, elbowR = 40f,
            };
        }

        static HeroPose DashPose()
        {
            return new HeroPose
            {
                spine = 28f, chest = 8f, head = -14f, bob = -0.08f,
                thighL = 35f, kneeL = 80f, footL = -10f,
                thighR = -45f, kneeR = 35f, footR = -30f,
                armL = -70f, elbowL = 15f, armR = -60f, elbowR = 20f,
            };
        }
    }
}
