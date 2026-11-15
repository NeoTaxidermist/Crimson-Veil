using CrimsonVeil.Core;
using CrimsonVeil.Veil;
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

        // ---- The rest of the body (all 0 = as the key poses were drawn; see HeroBodyMotion, which fills them in).
        /// <summary>Neck (sagittal, + forward): it keeps the gaze level as the body leans.</summary>
        public float neck;
        /// <summary>Pelvis tilt (+ top forward) and its turn round the vertical (+ the right hip forward).</summary>
        public float pelvis, pelvisTurn;
        /// <summary>The chest turned round the vertical against the hips (+ the right shoulder forward).</summary>
        public float twist;
        /// <summary>Collarbones (+ the shoulder forward), arms swung out to the side (+ away from the body).</summary>
        public float shoulderL, shoulderR, armOutL, armOutR;
        /// <summary>Wrists (+ the hand bent forward, the palm's way), toes (+ bent up, as the heel lifts).</summary>
        public float wristL, wristR, toeL, toeR;

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
                neck = Mathf.Lerp(a.neck, b.neck, t), pelvis = Mathf.Lerp(a.pelvis, b.pelvis, t),
                pelvisTurn = Mathf.Lerp(a.pelvisTurn, b.pelvisTurn, t), twist = Mathf.Lerp(a.twist, b.twist, t),
                shoulderL = Mathf.Lerp(a.shoulderL, b.shoulderL, t), shoulderR = Mathf.Lerp(a.shoulderR, b.shoulderR, t),
                armOutL = Mathf.Lerp(a.armOutL, b.armOutL, t), armOutR = Mathf.Lerp(a.armOutR, b.armOutR, t),
                wristL = Mathf.Lerp(a.wristL, b.wristL, t), wristR = Mathf.Lerp(a.wristR, b.wristR, t),
                toeL = Mathf.Lerp(a.toeL, b.toeL, t), toeR = Mathf.Lerp(a.toeR, b.toeR, t),
            };
        }

        public bool IsFinite()
        {
            float s = spine + chest + head + bob + thighL + kneeL + footL + thighR + kneeR + footR + armL + elbowL + armR + elbowR
                      + neck + pelvis + pelvisTurn + twist + shoulderL + shoulderR + armOutL + armOutR + wristL + wristR + toeL + toeR;
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
        /// <summary>Which weapon is attacking and how far into the attack (0..1), when <see cref="attacking"/>.</summary>
        public bool attacking;
        public CrimsonVeil.Veil.VeilForm attackForm;
        public float attackT;
        /// <summary>Where the blow is aimed, degrees from the facing (+up), see VeilAim.</summary>
        public float attackAim;
        public bool wallSliding;
        public bool hanging;
        public bool climbing;
        public float climbProgress;
        /// <summary>Hanging: the arm angle that reaches the lip (see HangArm); 0 = the usual one.</summary>
        public float hangArm;
        /// <summary>Hanging from the whip; <see cref="swingSpeed"/> is the angular velocity (rad/s, + = towards facing).</summary>
        public bool swinging;
        public float swingSpeed;
        /// <summary>0..1 how much a veil weapon is formed — the arm rises to hold it.</summary>
        public float weaponHeld;
        /// <summary>0..1 through the double jump's somersault, -1 when not flipping (the body tucks).</summary>
        public float somersault;
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
        /// <summary>Sword-arm angle when running: a forward guard, the blade leads the hero (never dragged behind).</summary>
        public const float RunHoldArm = 42f;

        /// <summary>
        /// Ready stance of the weapon arm per form (shoulder and elbow, standing / running).
        /// The bow is held at arm's length in front of the body (never hidden by the torso);
        /// the spear is couched at the hip with the forearm level, so the shaft points straight ahead.
        /// </summary>
        public static void WeaponHold(VeilForm form, float run, out float arm, out float elbow)
        {
            switch (form)
            {
                case VeilForm.Bow:
                    arm = Mathf.Lerp(78f, 62f, run); elbow = Mathf.Lerp(4f, 14f, run); return;
                case VeilForm.Spear:
                    arm = Mathf.Lerp(30f, 38f, run); elbow = Mathf.Lerp(62f, 58f, run); return;
                default:
                    arm = Mathf.Lerp(HoldArm, RunHoldArm, run); elbow = Mathf.Lerp(18f, 40f, run); return;
            }
        }

        /// <summary>How clenched the sword hand is (0 open .. 1 a fist) for how far a weapon has formed in it: it closes
        /// as the hilt / stock / grip forms in the palm, and opens again as the weapon melts.</summary>
        public static float FistWeight(float weaponHeld) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.4f, weaponHeld));

        /// <summary>Shoulder angle of the standing ready stance for a weapon (attacks start and end here).</summary>
        public static float HoldArmFor(VeilForm form)
        {
            WeaponHold(form, 0f, out float arm, out _);
            return arm;
        }

        public float Phase { get; private set; }
        public HeroPose Current { get; private set; }
        public float LandWeight => landTimer > 0f ? landTimer / landDuration : 0f;

        float time;
        float lagL, lagR;
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
            else if (s.swinging) target = SwingPose(s.swingSpeed, time);
            else if (s.hanging) target = HangPose(time, s.hangArm);
            else if (s.wallSliding) target = WallSlidePose(time);
            else if (s.dashing) target = DashPose();
            else if (!s.grounded) target = AirPose(s.verticalVelocity);
            else target = Standing(GroundPose(run), run);

            // Somersault: knees pulled to the chest, arms hugging the shins, then opening out to land.
            bool flipping = !s.grounded && !s.hanging && !s.climbing && !s.swinging && Somersault.Active(s.somersault);
            if (flipping) target = HeroPose.Lerp(target, TuckPose, Somersault.Tuck(s.somersault));

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

            if (s.weaponHeld > 0f && !s.hanging && !s.climbing && !s.swinging)
            {
                // Hold the weapon where the veil's grip is (forward and a little below the chest).
                // Ready stance: blade held forward-low when standing; when running the arm stays up in a
                // forward guard so the weapon leads the hero instead of being dragged behind.
                // The weapon follows the forearm, so this *is* the weapon's pose.
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(s.weaponHeld));
                WeaponHold(s.attackForm, Mathf.SmoothStep(0f, 1f, run), out float hold, out float holdElbow);
                float sway = Mathf.Sin(time * 1.4f) * 3f * (1f - run) + Mathf.Sin(Phase * Mathf.PI * 4f) * 2.5f * run;
                target.armR = Mathf.Lerp(target.armR, hold + sway, w);
                target.elbowR = Mathf.Lerp(target.elbowR, holdElbow, w);
            }

            bool attacking = s.attacking && s.attackT >= 0f && !s.hanging && !s.climbing && !s.swinging;
            if (attacking)
                target = HeroAttackChoreography.Apply(target, s.attackForm, s.attackT, run, s.attackAim);

            // The rest of the body: pelvis and chest turning in the stride, collarbones, arms clear of the body, the
            // neck, the toes, turning into a blow.
            target = HeroBodyMotion.Layer(target, s, Phase, run, time, attacking);

            if (!initialised)
            {
                Current = target;
                initialised = true;
            }
            else
            {
                // Attacks need snap: follow the choreography much more tightly than locomotion.
                float sharp = attacking || flipping ? sharpness * 2.6f : (s.grounded ? sharpness : sharpness * 0.6f);
                var prev = Current;
                var next = HeroPose.Lerp(Current, target, MovementMath.Damp(sharp, dt));
                // The wrists trail the arms' swing (a beat behind, like loose hands).
                bool holding = s.weaponHeld > 0.3f;
                lagL = Mathf.Lerp(lagL, HeroBodyMotion.WristLag((next.armL - prev.armL) / dt, false), MovementMath.Damp(14f, dt));
                lagR = Mathf.Lerp(lagR, HeroBodyMotion.WristLag((next.armR - prev.armR) / dt, holding), MovementMath.Damp(14f, dt));
                next.wristL = target.wristL + lagL;
                next.wristR = target.wristR + lagR;
                Current = next;
            }
            return Current;
        }

        /// <summary>
        /// Standing still the hero takes the concept's wide stance and sways in it (<see cref="HeroIdleCycle"/>, from the
        /// walk sheet); setting off, it pours into the gait. The chest keeps breathing.
        /// </summary>
        HeroPose Standing(HeroPose gait, float run)
        {
            float w = 1f - Mathf.SmoothStep(0f, 1f, run / 0.2f);
            if (w <= 0f) return gait;
            var idle = HeroIdleCycle.Sample(time);
            idle.chest = gait.chest;
            return HeroPose.Lerp(gait, idle, w);
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

        /// <summary>The tuck of a somersault: curled into a ball round the hips.</summary>
        public static readonly HeroPose TuckPose = new HeroPose
        {
            spine = 34f, chest = 18f, head = 22f, bob = 0f,
            thighL = 118f, kneeL = 135f, footL = 15f,
            thighR = 112f, kneeR = 140f, footR = 15f,
            armL = 55f, elbowL = 95f, armR = 50f, elbowR = 100f,
        };

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

        /// <summary>
        /// Both hands gripping the lip above, the arms almost straight, the body close to the wall: one foot braced
        /// against the stone, the other leg hanging and swinging a little, the head tipped back to the lip.
        /// </summary>
        public static HeroPose HangPose(float time, float arm = DefaultHangArm)
        {
            if (arm <= 0f) arm = DefaultHangArm;
            float swing = Mathf.Sin(time * 2.2f) * 5f;
            float breathe = Mathf.Sin(time * 1.6f) * 1.5f;
            return new HeroPose
            {
                spine = 8f + breathe, chest = -2f, head = 16f, neck = 6f, bob = 0f,
                thighL = 42f, kneeL = 78f, footL = 18f, toeL = 20f,          // braced against the wall
                thighR = 2f + swing, kneeR = 22f + swing * 0.5f, footR = -22f,
                armL = arm, elbowL = 10f, armR = arm - 4f, elbowR = 14f,   // reaching forward-up to the lip
                shoulderL = -4f, shoulderR = -4f, wristL = 30f, wristR = 30f, // hooked over the lip
            };
        }

        /// <summary>The hang's arm angle for the usual lip (PlayerController.HangReach ahead, at the top of the reach).</summary>
        public const float DefaultHangArm = 146f;

        /// <summary>Height of the shoulders above the feet (hanging, the body straight).</summary>
        public const float ShoulderHeight = 1.45f;

        /// <summary>
        /// The hang's arm angle (0 down, 90 forward, 180 up, against the chest) for a lip <paramref name="ahead"/> m in
        /// front of the feet and <paramref name="above"/> m above them: the arms point at it, so the hands are on the
        /// stone and not in the air beside it.
        /// </summary>
        public static float HangArm(float ahead, float above)
        {
            float world = 90f + Mathf.Atan2(above - ShoulderHeight, Mathf.Max(0.02f, ahead)) * Mathf.Rad2Deg;
            const float lean = 6f;   // the hang leans the chest forward this much (spine + chest)
            return Mathf.Clamp(world - lean, 110f, 175f);
        }

        /// <summary>Keys of the mantle (progress of the climb): the pull-up, the press over the lip, the knee on top, standing.</summary>
        public const float ClimbPullEnd = 0.35f, ClimbPressEnd = 0.6f, ClimbKneeEnd = 0.8f;

        /// <summary>
        /// How far up (0..1 of the rise) and over (0..1 onto the top) the hero is at climb progress <paramref name="t"/>:
        /// the pull-up lifts the shoulders to the lip, the press lifts the hips over it, the knee comes up onto the top
        /// and only then (feet clear of the stone) the body moves over the lip and stands.
        /// </summary>
        public static Vector2 ClimbRiseOver(float t)
        {
            t = Mathf.Clamp01(t);
            float rise = t < ClimbPullEnd ? Mathf.Lerp(0f, 0.26f, Mathf.SmoothStep(0f, 1f, t / ClimbPullEnd))
                       : t < ClimbPressEnd ? Mathf.Lerp(0.26f, 0.55f, Mathf.SmoothStep(0f, 1f, (t - ClimbPullEnd) / (ClimbPressEnd - ClimbPullEnd)))
                       : t < ClimbKneeEnd ? Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, (t - ClimbPressEnd) / (ClimbKneeEnd - ClimbPressEnd)))
                       : 1f;
            float over = t < ClimbPressEnd + 0.1f ? 0f
                       : t < ClimbKneeEnd ? Mathf.Lerp(0f, 0.4f, Mathf.SmoothStep(0f, 1f, (t - ClimbPressEnd - 0.1f) / (ClimbKneeEnd - ClimbPressEnd - 0.1f)))
                       : Mathf.Lerp(0.4f, 1f, Mathf.SmoothStep(0f, 1f, (t - ClimbKneeEnd) / (1f - ClimbKneeEnd)));
            return new Vector2(rise, over);
        }

        /// <summary>
        /// On the whip: both hands high on it (whip hand above), the body a pendulum. Swinging forward the legs
        /// trail and then kick ahead at the far end; swinging back they tuck — the pumping motion.
        /// </summary>
        public static HeroPose SwingPose(float swingSpeed, float time)
        {
            float k = Mathf.Clamp(swingSpeed / 2.2f, -1f, 1f);
            float idle = Mathf.Sin(time * 2.4f) * 4f * (1f - Mathf.Abs(k));
            return new HeroPose
            {
                spine = -4f + 10f * k, chest = -6f + 4f * k, head = 8f - 6f * k, bob = 0f,
                thighL = 14f + 40f * k + idle, kneeL = 20f + 35f * Mathf.Max(0f, -k), footL = -12f,
                thighR = -2f + 26f * k - idle, kneeR = 34f + 30f * Mathf.Max(0f, -k), footR = -18f,
                armL = 165f, elbowL = 14f, armR = 176f, elbowR = 6f,
            };
        }

        /// <summary>
        /// Mantle, keyed to <see cref="ClimbRiseOver"/> so the hands stay on the lip: the pull-up (elbows bend, the chin
        /// comes to the lip, the legs kick), the press (arms lock straight down onto the lip, the chest leans over it),
        /// the knee swung up onto the top, then standing up.
        /// </summary>
        public static HeroPose ClimbPose(float t)
        {
            t = Mathf.Clamp01(t);
            var hang = HangPose(0f);
            var chin = new HeroPose
            {
                spine = 10f, chest = 6f, head = -4f, neck = -4f,
                thighL = 30f, kneeL = 70f, footL = 10f, thighR = 12f, kneeR = 45f, footR = -15f,
                armL = 112f, elbowL = 118f, armR = 106f, elbowR = 124f,
                shoulderL = 4f, shoulderR = 4f, wristL = 45f, wristR = 45f,
            };
            var press = new HeroPose
            {
                spine = 34f, chest = 16f, head = -12f, neck = -8f, bob = -0.04f,
                thighL = 48f, kneeL = 95f, footL = 5f, thighR = 8f, kneeR = 40f, footR = -20f,
                armL = -14f, elbowL = 10f, armR = -18f, elbowR = 12f,
                shoulderL = -12f, shoulderR = -12f, wristL = -70f, wristR = -70f,   // the palms flat on the top
            };
            var knee = new HeroPose
            {
                spine = 40f, chest = 18f, head = -14f, neck = -8f, bob = -0.06f,
                thighL = 112f, kneeL = 138f, footL = 12f, toeL = 10f, thighR = 18f, kneeR = 64f, footR = -10f,
                armL = -6f, elbowL = 28f, armR = -10f, elbowR = 32f,
                shoulderL = -6f, shoulderR = -6f, wristL = -60f, wristR = -60f,
            };
            var up = new HeroPose
            {
                spine = 6f, chest = 2f, thighL = 12f, kneeL = 16f, thighR = -5f, kneeR = 10f,
                armL = 10f, elbowL = 20f, armR = 5f, elbowR = 25f,
            };
            if (t < ClimbPullEnd) return HeroPose.Lerp(hang, chin, Mathf.SmoothStep(0f, 1f, t / ClimbPullEnd));
            if (t < ClimbPressEnd) return HeroPose.Lerp(chin, press, Mathf.SmoothStep(0f, 1f, (t - ClimbPullEnd) / (ClimbPressEnd - ClimbPullEnd)));
            if (t < ClimbKneeEnd) return HeroPose.Lerp(press, knee, Mathf.SmoothStep(0f, 1f, (t - ClimbPressEnd) / (ClimbKneeEnd - ClimbPressEnd)));
            return HeroPose.Lerp(knee, up, Mathf.SmoothStep(0f, 1f, (t - ClimbKneeEnd) / (1f - ClimbKneeEnd)));
        }

        /// <summary>How firmly the hands are kept on the lip (0..1): all the way while hanging, pulling up and pressing over;
        /// they let go as the knee comes up and the hero stands.</summary>
        public static float LipHold(bool hanging, bool climbing, float climbProgress)
        {
            if (hanging) return 1f;
            if (!climbing) return 0f;
            // (past the press the body rises out of the arms' reach: they push off and let go as the knee comes up)
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ClimbPressEnd, ClimbPressEnd + 0.12f, climbProgress));
        }

        /// <summary>
        /// Where the wrists are on the lip (x towards the ledge, y up, from the lip's corner) at climb progress
        /// <paramref name="t"/> (0 = hanging): just short of the corner, the fingers hooked over it; then, pressing over
        /// it, palms flat on the top.
        /// </summary>
        public static Vector2 LipHandOffset(float t)
        {
            var grip = new Vector2(-0.03f, 0.0f);
            var flat = new Vector2(0.08f, 0.04f);
            return Vector2.Lerp(grip, flat, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ClimbPullEnd - 0.05f, ClimbPressEnd - 0.05f, t)));
        }

        /// <summary>How tightly the hands grip while hanging or climbing (they hold the lip until the knee is on top).</summary>
        public static float LedgeGrip(bool hanging, bool climbing, float climbProgress)
        {
            if (hanging) return 0.8f;
            if (!climbing) return 0f;
            if (climbProgress < ClimbPullEnd) return 0.85f;
            // pressing down on the top the fingers open flat, then let go
            return Mathf.Lerp(0.15f, 0f, Mathf.InverseLerp(ClimbKneeEnd, 1f, climbProgress));
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
