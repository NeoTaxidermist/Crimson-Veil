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

        /// <summary>wa*a + wb*b + wc*c + wd*d, field by field (the weights need not add up to 1: splines, offsets).</summary>
        public static HeroPose Combine(in HeroPose a, float wa, in HeroPose b, float wb, in HeroPose c, float wc, in HeroPose d, float wd)
        {
            float F(float x, float y, float z, float w) => x * wa + y * wb + z * wc + w * wd;
            return new HeroPose
            {
                spine = F(a.spine, b.spine, c.spine, d.spine), chest = F(a.chest, b.chest, c.chest, d.chest),
                head = F(a.head, b.head, c.head, d.head), bob = F(a.bob, b.bob, c.bob, d.bob),
                thighL = F(a.thighL, b.thighL, c.thighL, d.thighL), kneeL = F(a.kneeL, b.kneeL, c.kneeL, d.kneeL), footL = F(a.footL, b.footL, c.footL, d.footL),
                thighR = F(a.thighR, b.thighR, c.thighR, d.thighR), kneeR = F(a.kneeR, b.kneeR, c.kneeR, d.kneeR), footR = F(a.footR, b.footR, c.footR, d.footR),
                armL = F(a.armL, b.armL, c.armL, d.armL), elbowL = F(a.elbowL, b.elbowL, c.elbowL, d.elbowL),
                armR = F(a.armR, b.armR, c.armR, d.armR), elbowR = F(a.elbowR, b.elbowR, c.elbowR, d.elbowR),
                neck = F(a.neck, b.neck, c.neck, d.neck), pelvis = F(a.pelvis, b.pelvis, c.pelvis, d.pelvis),
                pelvisTurn = F(a.pelvisTurn, b.pelvisTurn, c.pelvisTurn, d.pelvisTurn), twist = F(a.twist, b.twist, c.twist, d.twist),
                shoulderL = F(a.shoulderL, b.shoulderL, c.shoulderL, d.shoulderL), shoulderR = F(a.shoulderR, b.shoulderR, c.shoulderR, d.shoulderR),
                armOutL = F(a.armOutL, b.armOutL, c.armOutL, d.armOutL), armOutR = F(a.armOutR, b.armOutR, c.armOutR, d.armOutR),
                wristL = F(a.wristL, b.wristL, c.wristL, d.wristL), wristR = F(a.wristR, b.wristR, c.wristR, d.wristR),
                toeL = F(a.toeL, b.toeL, c.toeL, d.toeL), toeR = F(a.toeR, b.toeR, c.toeR, d.toeR),
            };
        }

        /// <summary>
        /// A smooth curve through key poses at the given times (cubic Hermite, each key's tangent from its neighbours,
        /// still at the first and the last): the body flows through the keys instead of halting at each one.
        /// </summary>
        public static HeroPose Spline(HeroPose[] keys, float[] times, float t)
        {
            int last = keys.Length - 1;
            if (t <= times[0]) return keys[0];
            if (t >= times[last]) return keys[last];
            int i = 0;
            while (i < last - 1 && t > times[i + 1]) i++;
            float h = Mathf.Max(1e-4f, times[i + 1] - times[i]);
            float u = (t - times[i]) / h;
            float u2 = u * u, u3 = u2 * u;
            float h00 = 2f * u3 - 3f * u2 + 1f, h10 = u3 - 2f * u2 + u, h01 = -2f * u3 + 3f * u2, h11 = u3 - u2;
            const float tension = 0.8f;
            // tangent at key i: (P[i+1] - P[i-1]) / span, zero at the ends; written as weights on the four poses around
            float ci = i > 0 ? tension * h / (times[i + 1] - times[i - 1]) : 0f;
            float cj = i + 1 < last ? tension * h / (times[i + 2] - times[i]) : 0f;
            var prev = keys[Mathf.Max(i - 1, 0)];
            var next = keys[Mathf.Min(i + 2, last)];
            return Combine(prev, -h10 * ci, keys[i], h00 - h11 * cj, keys[i + 1], h01 + h10 * ci, next, h11 * cj);
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
        /// <summary>The blow is struck from the crouch: the legs stay down, the blow sweeps low (VeilAim.LowBlow).</summary>
        public bool lowBlow;
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
        /// <summary>Crouched low on the ground (down held, or under a low ceiling): crawling when moving.</summary>
        public bool crouching;
        /// <summary>Sliding along the ground; <see cref="slideProgress"/> 0..1 through it.</summary>
        public bool sliding;
        public float slideProgress;
        /// <summary>Rolling out of a hard landing; <see cref="rollProgress"/> 0..1 through it.</summary>
        public bool rolling;
        public float rollProgress;
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

        /// <summary>
        /// The ready stance crouched: the hand is down by the knees, so a blade held as standing points into the floor -
        /// the forearm comes up level and the blade stands up ahead in a low guard; the spear is couched higher and
        /// the bow held up level in front (low, its lower limb went into the floor).
        /// </summary>
        public static void WeaponHoldLow(VeilForm form, out float arm, out float elbow)
        {
            switch (form)
            {
                case VeilForm.Bow:
                    arm = 118f; elbow = 4f; return;
                case VeilForm.Spear:
                    arm = 50f; elbow = 62f; return;
                default:
                    arm = LowHoldArm; elbow = LowHoldElbow; return;
            }
        }

        /// <summary>The crouched guard of a blade (shoulder, elbow): the forearm level, the blade up ahead.</summary>
        public const float LowHoldArm = 30f, LowHoldElbow = 50f;

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
        float runVis, runVisVel, lastSpeed, accelSmooth;
        float torso, torsoVel, armSwing, armSwingVel;

        /// <summary>Degrees the torso is pitched by the start / stop (+ forward) and the arms swung by it (+ forward).</summary>
        public float Momentum => torso;
        public float ArmMomentum => armSwing;

        /// <summary>
        /// A damped spring towards <paramref name="target"/> (angular frequency omega, damping ratio zeta < 1 swings
        /// past it and settles): the body's weight carried through a start or a stop.
        /// </summary>
        public static void Spring(ref float x, ref float v, float target, float omega, float zeta, float dt)
        {
            float a = omega * omega * (target - x) - 2f * zeta * omega * v;
            v += a * dt;
            x += v * dt;
        }

        /// <summary>
        /// Where the body's weight goes as the run starts or stops (<paramref name="accel01"/>: the ground speed's
        /// change, +1 a full sprint start, -1 a full stop): setting off he leans into the first steps; stopping, the
        /// upper body carries on forward over the planted feet (then swings back past upright and settles).
        /// </summary>
        public static float TorsoTarget(float accel01) => accel01 > 0f ? 9f * accel01 : -15f * accel01;

        /// <summary>The arms: pulled back driving off into the run, flung forward by a stop.</summary>
        public static float ArmTarget(float accel01) => -20f * accel01;

        /// <summary>
        /// Standing, the arms are never still: they follow the breath a beat behind the chest and each sways on its
        /// own slow rhythm (never mirrored), the elbows easing in and out, with a slow shift of the weight - added to
        /// the idle loop (whose arms barely move).
        /// </summary>
        public static HeroPose IdleLife(float time)
        {
            float breath = Mathf.Sin(time * 1.85f);
            return new HeroPose
            {
                armR = 4f * Mathf.Sin(time * 1.85f - 0.7f) + 2.5f * Mathf.Sin(time * 0.53f + 0.4f),
                elbowR = 5f + 5f * Mathf.Sin(time * 1.85f - 1.2f) + 2f * Mathf.Sin(time * 0.71f),
                // (the far arm goes with the idle loop's own sway, a beat behind it, and drifts slowly)
                armL = 3.5f * Mathf.Cos(time * 2f * Mathf.PI / HeroIdleCycle.Duration - 0.5f) + 2f * Mathf.Sin(time * 0.47f + 2.1f),
                elbowL = 4f * Mathf.Sin(time * 1.61f - 0.4f),
                chest = 1.2f * breath,
                spine = 1.2f * Mathf.Sin(time * 0.37f),
                head = -1.5f * Mathf.Sin(time * 1.85f - 0.4f) + 2f * Mathf.Sin(time * 0.29f + 1f),
            };
        }
        float landTimer;
        bool wasGrounded = true;
        bool initialised;

        public HeroPose Step(float dt, HeroState s)
        {
            if (dt <= 0f) return Current;
            time += dt;

            float run = Mathf.Clamp01(s.speed / Mathf.Max(0.01f, s.maxSpeed));
            // The ground speed jumps to a run in a tenth of a second (the controls stay sharp): the body pours into
            // the run and out of it over a few frames more, so the pose doesn't snap from standing to running.
            runVis = Mathf.SmoothDamp(runVis, run, ref runVisVel, run > runVis ? 0.06f : 0.1f, Mathf.Infinity, dt);
            float accel = (s.speed - lastSpeed) / dt;
            lastSpeed = s.speed;
            accelSmooth = Mathf.Lerp(accelSmooth, accel, 1f - Mathf.Exp(-25f * dt));
            bool plainFeet = s.grounded && !s.hanging && !s.climbing && !s.swinging && !s.wallSliding && !s.sliding && !s.dashing
                             && !(s.attacking && s.attackT >= 0f);   // (a blow's lunge has its own lean)
            float accel01 = plainFeet ? Mathf.Clamp(accelSmooth / 85f, -1.2f, 1.2f) : 0f;
            Spring(ref torso, ref torsoVel, TorsoTarget(accel01), 11f, 0.42f, dt);
            Spring(ref armSwing, ref armSwingVel, ArmTarget(accel01), 12f, 0.38f, dt);
            // (the stride follows the real speed, so the feet never slide; the pose follows the body's own)
            float stride = Mathf.Lerp(walkStride, runStride, run);
            run = Mathf.Clamp01(runVis);
            if (s.crouching && !s.sliding) stride = CrouchStride;   // a crouch-walk: long, unhurried steps
            if (s.grounded) Phase = Mathf.Repeat(Phase + s.speed * dt / stride, 1f);

            if (s.grounded && !wasGrounded) landTimer = landDuration;
            wasGrounded = s.grounded;
            landTimer = Mathf.Max(0f, landTimer - dt);

            // the legs on the whip trail the swing's speed on a spring
            if (s.swinging) Spring(ref swingLegs, ref swingLegsVel, Mathf.Clamp(s.swingSpeed / 2.2f, -1f, 1f), 6.5f, 0.4f, dt);
            else { swingLegs = 0f; swingLegsVel = 0f; }

            HeroPose target;
            if (s.climbing) target = ClimbPose(s.climbProgress);
            else if (s.swinging) target = SwingPose(swingLegs, time);
            else if (s.hanging) target = HangPose(time, s.hangArm);
            else if (s.wallSliding) target = WallSlidePose(time);
            else if (s.dashing) target = DashPose();
            else if (!s.grounded) target = AirPose(s.verticalVelocity);
            else if (s.sliding) target = SlidePose(s.slideProgress);
            else if (s.crouching) target = CrouchPose(s.speed, Phase, time);
            else target = Standing(GroundPose(run), run);

            // The weight of a start or a stop: the torso leans into the first steps or carries on over the planted
            // feet, the knees take it, the arms swing through and the head lags; then it all settles.
            if (plainFeet || Mathf.Abs(torso) > 0.3f || Mathf.Abs(armSwing) > 0.3f)
            {
                target.spine += torso;
                target.chest += torso * 0.4f;
                target.head -= torso * 0.45f;
                float give = Mathf.Max(0f, torso);
                target.kneeL += give * 1.1f; target.kneeR += give * 1.1f;
                target.thighL += give * 0.4f; target.thighR += give * 0.4f;
                target.bob -= give * 0.0035f;
                target.armL += armSwing;
                target.armR += armSwing * (s.weaponHeld > 0.3f ? 0.3f : 1f);
                target.elbowL += Mathf.Abs(armSwing) * 0.5f;
            }

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
                if (s.crouching && s.grounded && !s.sliding) WeaponHoldLow(s.attackForm, out hold, out holdElbow);
                float sway = Mathf.Sin(time * 1.4f) * 3f * (1f - run) + Mathf.Sin(Phase * Mathf.PI * 4f) * 2.5f * run;
                target.armR = Mathf.Lerp(target.armR, hold + sway, w);
                target.elbowR = Mathf.Lerp(target.elbowR, holdElbow, w);
            }

            bool attacking = s.attacking && s.attackT >= 0f && !s.hanging && !s.climbing && !s.swinging;
            if (attacking)
                target = s.lowBlow && s.grounded
                    ? HeroAttackChoreography.ApplyLow(target, s.attackForm, s.attackT, s.attackAim)
                    : HeroAttackChoreography.Apply(target, s.attackForm, s.attackT, run, s.attackAim);

            // Hanging from a ledge with a weapon cast: the left hand keeps the lip, the right lets go and wields it.
            float free = HangFreeArm(s.hanging, s.weaponHeld);
            bool hangBlow = free > 0f && s.attacking && s.attackT >= 0f;
            if (free > 0f) target = OneHandedHang(target, s, free, time);

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
                float sharp = attacking || hangBlow || flipping ? sharpness * 2.6f : (s.grounded ? sharpness : sharpness * 0.6f);
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
            var life = IdleLife(time);
            idle.armR += life.armR; idle.elbowR += life.elbowR; idle.armL += life.armL; idle.elbowL += life.elbowL;
            idle.chest += life.chest; idle.spine += life.spine; idle.head += life.head;
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

        /// <summary>How far the hips sink crouched (m): the body fits under a ceiling a metre up.</summary>
        public const float CrouchDrop = 0.5f;

        /// <summary>Distance (m) per gait cycle crouch-walking: two steps of 0.8 m (at 2 m/s about 2.5 steps a second).</summary>
        public const float CrouchStride = 1.6f;

        /// <summary>
        /// Crouched low: deep knees, the body bent over them, the head up to see ahead, the hands ready near the
        /// knees. Moving, a low crouch-walk: long, low steps (one stride per gait cycle, as in the walk, the phase
        /// following the distance), the soles flat on the ground, the swinging foot lifted clear, the heel rising
        /// as the leg pushes off behind - not a patter on tiptoe.
        /// </summary>
        public static HeroPose CrouchPose(float speed, float phase, float time)
        {
            float move = Mathf.Clamp01(speed / 2.0f);
            float a = phase * Mathf.PI * 2f;
            float sinA = Mathf.Sin(a), cosA = Mathf.Cos(a);
            float breathe = Mathf.Sin(time * 1.8f) * (1f - move);
            float thighL = Mathf.Lerp(92f, 86f, move) + 28f * sinA * move;
            float thighR = Mathf.Lerp(80f, 86f, move) - 28f * sinA * move;
            // the leg swinging forward lifts its foot clear (more knee); the one behind pushes off
            float kneeL = Mathf.Lerp(122f, 116f, move) + 24f * Mathf.Max(0f, cosA) * move;
            float kneeR = Mathf.Lerp(128f, 116f, move) + 24f * Mathf.Max(0f, -cosA) * move;
            float pushL = Mathf.Max(0f, -sinA) * move, pushR = Mathf.Max(0f, sinA) * move;
            return new HeroPose
            {
                spine = 30f + 6f * move + breathe, chest = 10f, head = 22f + 3f * move, neck = 8f,
                bob = -CrouchDrop - 0.03f * Mathf.Abs(sinA) * move,          // lowest with the legs apart
                thighL = thighL, kneeL = kneeL, thighR = thighR, kneeR = kneeR,
                // the soles flat on the ground; the heel lifts as the leg pushes off behind, the toes bend with it
                footL = FlatFoot(thighL, kneeL) - 16f * pushL, footR = FlatFoot(thighR, kneeR) - 16f * pushR,
                toeL = 8f + 26f * pushL, toeR = 8f + 26f * pushR,
                armL = 32f - 18f * sinA * move, elbowL = 55f, armR = 24f + 18f * sinA * move, elbowR = 50f,
            };
        }

        /// <summary>The foot angle (+ toes up) that keeps the sole level with the ground for a thigh and knee: the
        /// shin slopes by thigh - knee, the foot turns that back.</summary>
        public static float FlatFoot(float thigh, float knee) => knee - thigh;

        /// <summary>
        /// A slide: leaning back, the hips low over the ground, the front leg stretched out ahead, the back one tucked
        /// under, a hand trailing on the ground; at its end, gathering into the crouch.
        /// </summary>
        public static HeroPose SlidePose(float t)
        {
            var slide = new HeroPose
            {
                spine = -18f, chest = -10f, head = 26f, neck = 10f, bob = -0.72f,
                thighL = 78f, kneeL = 8f, footL = 18f,               // stretched out ahead
                thighR = 18f, kneeR = 128f, footR = -10f, toeR = 30f, // tucked under
                armL = -38f, elbowL = 8f, wristL = -50f,             // trailing on the ground
                armR = 22f, elbowR = 78f,                            // the other hand held in to the chest
            };
            var crouch = CrouchPose(0f, 0f, 0f);
            return HeroPose.Lerp(slide, crouch, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, t)));
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
        /// <summary>
        /// Hanging on a ledge, how far the right hand has let go of the lip to hold the weapon (0 both hands on the
        /// stone .. 1 the weapon in hand). Only hanging; climbing and swinging keep both hands busy.
        /// </summary>
        public static float HangFreeArm(bool hanging, float weaponHeld)
            => hanging ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(weaponHeld / 0.5f)) : 0f;

        /// <summary>
        /// The hang with the right arm free: the weapon held out from the wall, and a blow swung with that arm alone
        /// (the left keeps the body on the lip, the legs stay braced).
        /// </summary>
        public static HeroPose OneHandedHang(HeroPose hang, in HeroState s, float free, float time)
        {
            WeaponHold(s.attackForm, 0f, out float hold, out float holdElbow);
            var arm = hang;
            arm.armR = hold + Mathf.Sin(time * 1.4f) * 3f;
            arm.elbowR = holdElbow;
            arm.shoulderR = 0f;
            arm.wristR = 0f;
            if (s.attacking && s.attackT >= 0f)
            {
                var blow = HeroAttackChoreography.Apply(arm, s.attackForm, s.attackT, 0f, s.attackAim);
                arm.armR = blow.armR;
                arm.elbowR = blow.elbowR;
                // the body twists a little into the one-armed blow
                arm.chest = hang.chest + (blow.chest - hang.chest) * 0.35f;
            }
            hang.armR = Mathf.Lerp(hang.armR, arm.armR, free);
            hang.elbowR = Mathf.Lerp(hang.elbowR, arm.elbowR, free);
            hang.shoulderR = Mathf.Lerp(hang.shoulderR, arm.shoulderR, free);
            hang.wristR = Mathf.Lerp(hang.wristR, arm.wristR, free);
            hang.chest = Mathf.Lerp(hang.chest, arm.chest, free);
            return hang;
        }

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
        /// <summary>
        /// Hanging from the whip. <paramref name="legs"/> (-1..1) is where the legs have been carried by the swing (see
        /// <see cref="SwingLegs"/>: they trail it on a spring, not stiffly with it): swinging forward they reach out
        /// ahead, the body piking a little, the toes pointed; swinging back they trail behind, the knees eased, the back
        /// arched - a gymnast's pump. Together, the near leg a touch ahead; they never lock straight.
        /// </summary>
        public static HeroPose SwingPose(float legs, float time)
        {
            float k = Mathf.Clamp(legs, -1.2f, 1.2f);
            float fwd = Mathf.Max(0f, k), back = Mathf.Max(0f, -k);
            float breathe = Mathf.Sin(time * 1.9f) * 2f;
            return new HeroPose
            {
                spine = 2f + 9f * fwd - 10f * back + breathe * 0.5f, chest = -4f + 3f * fwd - 6f * back, head = 6f - 4f * fwd + 6f * back,
                // (trailing behind, the legs stay nearly straight in line with the arched body: thighs swung back with the
                // knees bent too, the shins stuck up behind him at the top of a high swing, as if kneeling in the air)
                thighL = 12f + 24f * fwd - 12f * back, kneeL = 16f + 4f * fwd + 8f * back, footL = -28f - 6f * back,
                thighR = 4f + 20f * fwd - 10f * back, kneeR = 22f + 4f * fwd + 6f * back, footR = -30f - 6f * back,   // (a light pike forward: a deep one sat him in the air)
                armL = 165f, elbowL = 14f, armR = 176f, elbowR = 6f,
            };
        }

        /// <summary>Where the swing has carried the legs: a spring after the swing's speed (they lag, overshoot, settle).</summary>
        public float SwingLegs => swingLegs;

        /// <summary>On the whip: how far up the rope (m) from the grip the whip hand and the other hand close on it.</summary>
        public const float RopeLowHand = 0f, RopeHighHand = 0.12f;
        /// <summary>How far (m) the wrist sits under the point of the rope its fist closes round.</summary>
        public const float RopeWristBelow = 0.07f;

        /// <summary>Up the rope: from the grip towards the ring (straight up if they coincide).</summary>
        public static Vector3 RopeUp(Vector3 grip, Vector2 anchor)
        {
            var d = new Vector3(anchor.x - grip.x, anchor.y - grip.y, 0f);
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.up;
        }
        float swingLegs, swingLegsVel;

        /// <summary>
        /// Mantle, keyed to <see cref="ClimbRiseOver"/> so the hands stay on the lip: the pull-up (elbows bend, the chin
        /// comes to the lip, the legs kick), the press (arms lock straight down onto the lip, the chest leans over it),
        /// the knee swung up onto the top, then standing up.
        /// </summary>
        static readonly HeroPose[] ClimbKeys = new HeroPose[6];
        static readonly float[] ClimbTimes = { 0f, ClimbPullEnd, ClimbPressEnd, ClimbKneeEnd, ClimbStep, 1f };

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
            // The knee comes up over the lip and the foot is set down on the top, the body still low over the hands:
            // the climb has his place on the top by now (ClimbRiseOver), so he is crouched deep on it (the bob) and
            // rises from there - the hips go up evenly from the press to standing, not 0.85 m in a hop.
            var knee = new HeroPose
            {
                spine = 46f, chest = 20f, head = -16f, neck = -8f, bob = ClimbCrouch,
                // (the trailing leg still hangs off the edge behind him, down the wall: forward of that its thigh went through the corner)
                thighL = 104f, kneeL = 124f, footL = 14f, toeL = 4f, thighR = -10f, kneeR = 36f, footR = -10f,
                armL = 72f, elbowL = 24f, armR = 64f, elbowR = 30f,   // (the arms straight down under the leaned chest)
                shoulderL = -6f, shoulderR = -6f, wristL = -30f, wristR = -30f,
            };
            // rising on that foot, the trailing leg swings through and steps up beside it
            var step = new HeroPose
            {
                spine = 26f, chest = 12f, head = -8f, neck = -4f, bob = ClimbCrouch * 0.45f,
                thighL = 58f, kneeL = 80f, footL = 6f, thighR = 62f, kneeR = 96f, footR = 8f, toeR = 6f,
                armL = 38f, elbowL = 28f, armR = 28f, elbowR = 32f,
            };
            var up = new HeroPose
            {
                spine = 6f, chest = 2f, thighL = 12f, kneeL = 16f, thighR = -5f, kneeR = 10f,
                armL = 10f, elbowL = 20f, armR = 5f, elbowR = 25f,
            };
            // (into arrays of its own: two new ones each frame of the climb were garbage)
            var keys = ClimbKeys;
            keys[0] = hang; keys[1] = chin; keys[2] = press; keys[3] = knee; keys[4] = step; keys[5] = up;
            var p = HeroPose.Spline(keys, ClimbTimes, t);

            // pulling up, the feet scrabble up the wall in turns (they push, they find no hold), then hang loose
            if (t < ClimbPullEnd)
            {
                float u = t / ClimbPullEnd;
                float env = Mathf.Sin(u * Mathf.PI);
                float kick = Mathf.Sin(u * Mathf.PI * 3f) * env;
                p.thighL += 16f * kick; p.kneeL += 14f * Mathf.Abs(kick);
                p.thighR -= 16f * kick; p.kneeR += 14f * Mathf.Abs(kick);
                p.footL += 10f * kick; p.footR -= 10f * kick;
            }
            // pressing over the top, the trailing leg hangs and swings a little with the heave
            else if (t < ClimbKneeEnd)
            {
                float u = Mathf.InverseLerp(ClimbPullEnd, ClimbKneeEnd, t);
                float swing = Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Sin(u * Mathf.PI);
                p.thighR += 8f * swing; p.kneeR += 6f * Mathf.Abs(swing);
            }
            return p;
        }

        /// <summary>Climb progress at which the trailing leg has stepped up beside the knee.</summary>
        public const float ClimbStep = 0.9f;

        /// <summary>How low (m, the hips) he is crouched on the top as the knee comes up onto it.</summary>
        public const float ClimbCrouch = -0.66f;

        /// <summary>How firmly the hands are kept on the lip (0..1): all the way while hanging, pulling up and pressing over;
        /// they let go as the knee comes up and the hero stands.</summary>
        public static float LipHold(bool hanging, bool climbing, float climbProgress)
        {
            if (hanging) return 1f;
            if (!climbing) return 0f;
            // (they stay on the top while the knee comes up over it and the foot is set down, and are off it by the time
            // he is on his feet: let go straight after the press - at 0.6 to 0.72 it was - he rose the last 0.85 m with
            // nothing under him)
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LipLetGo, ClimbKneeEnd, climbProgress));
        }

        /// <summary>Climb progress from which the hands come off the lip (they are off it at <see cref="ClimbKneeEnd"/>).</summary>
        public const float LipLetGo = 0.66f;

        /// <summary>
        /// Where the wrists are on the lip (x towards the ledge, y up, from the lip's corner) at climb progress
        /// <paramref name="t"/> (0 = hanging): just short of the corner, the fingers hooked over it; then, pressing over
        /// it, palms flat on the top.
        /// </summary>
        public static Vector2 LipHandOffset(float t)
        {
            var grip = new Vector2(-0.04f, 0.05f);   // (the wrist level with the corner put the palm 8 cm into the stone)
            var flat = new Vector2(0.08f, 0.05f);
            return Vector2.Lerp(grip, flat, LipPress(t));
        }

        /// <summary>How far (0..1) the hands have gone from hooked over the lip's corner to pressing flat on its top, at
        /// climb progress <paramref name="t"/> (0 = hanging).</summary>
        public static float LipPress(float t)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ClimbPullEnd - 0.05f, ClimbPressEnd - 0.05f, t));

        /// <summary>
        /// How far a shoulder at <paramref name="shoulder"/> has to move for an arm that reaches <paramref name="reach"/>
        /// m to get its hand onto <paramref name="target"/>: nothing while it reaches, else straight towards the target
        /// by what is missing. (The hands hold the lip and the body hangs from them.)
        /// </summary>
        public static Vector3 PullToReach(Vector3 shoulder, Vector3 target, float reach)
        {
            Vector3 to = target - shoulder;
            float d = to.magnitude;
            return d <= reach || d < 1e-5f ? Vector3.zero : to * ((d - reach) / d);
        }

        /// <summary>How far the fingers close on a lip: the hand lies on its top and the fingers press onto the stone.
        /// (A fist there - 0.8 it was - closed on nothing, over the lip.)</summary>
        public const float LipFingers = 0.2f;

        /// <summary>How far the fingers close while hanging or climbing (they hold the lip until the knee is on top).</summary>
        public static float LedgeGrip(bool hanging, bool climbing, float climbProgress)
        {
            if (hanging) return LipFingers;
            if (!climbing) return 0f;
            if (climbProgress < ClimbPullEnd) return LipFingers;
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
