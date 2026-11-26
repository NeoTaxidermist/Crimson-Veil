using CrimsonVeil.Combat;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's moving, heard: a step each time a foot is set down (louder at a run), the push of a jump, the
    /// somersault's rush, the landing (heavier from higher up), the dash, the roll, the grab of a ledge and the climb
    /// over it, the scrape of a slide and of a slide down a wall, the whip latching on, creaking as he swings, the air
    /// at the bottom of each swing and the let-go. Added by <see cref="PlayerController"/>; needs nothing else.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class HeroSounds : MonoBehaviour
    {
        /// <summary>From this fall (m) a landing is a hard one.</summary>
        public const float HardFall = 3f;
        /// <summary>From this speed (m/s) a step is a running one.</summary>
        public const float RunFrom = 4f;

        PlayerController player;
        HeroAnimator animator;
        MotorLogic motor;
        int rolledFrame = -1;
        float lastSwingAngle;
        float swooshCooldown;

        void Start()
        {
            GameSound.WarmUp();   // (the sounds are made ahead, not when first heard)
            player = GetComponent<PlayerController>();
            motor = player.Motor;
            animator = GetComponentInChildren<HeroAnimator>();
            if (animator != null) animator.FootSetDown += OnFootDown;
            if (motor == null) return;
            motor.Jumped += OnJumped;
            motor.AirJumped += OnAirJumped;
            motor.WallJumped += OnWallJumped;
            motor.Landed += OnLanded;
            motor.Rolled += OnRolled;
            motor.Dashed += OnDashed;
            motor.LedgeGrabbed += OnLedgeGrabbed;
            motor.ClimbStarted += OnClimbStarted;
            motor.SwingStarted += OnSwingStarted;
            motor.SwingReleased += OnSwingReleased;
        }

        void OnDestroy()
        {
            if (animator != null) animator.FootSetDown -= OnFootDown;
            if (motor == null) return;
            motor.Jumped -= OnJumped;
            motor.AirJumped -= OnAirJumped;
            motor.WallJumped -= OnWallJumped;
            motor.Landed -= OnLanded;
            motor.Rolled -= OnRolled;
            motor.Dashed -= OnDashed;
            motor.LedgeGrabbed -= OnLedgeGrabbed;
            motor.ClimbStarted -= OnClimbStarted;
            motor.SwingStarted -= OnSwingStarted;
            motor.SwingReleased -= OnSwingReleased;
        }

        /// <summary>The step a foot set down at <paramref name="speed"/> makes, and how loud (0..1).</summary>
        public static (Cue cue, float loudness) StepAt(float speed) =>
            (speed >= RunFrom ? Cue.StepRun : Cue.StepWalk, Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(HeroAnimator.FootfallFrom, 8f, speed)));

        /// <summary>The landing from a fall of <paramref name="fall"/> m, and how loud (0..1).</summary>
        public static (Cue cue, float loudness) LandingFrom(float fall) =>
            (fall >= HardFall ? Cue.LandHard : Cue.Land, Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(0.2f, HardFall * 2f, fall)));

        void OnFootDown(int foot, float speed)
        {
            var (cue, loud) = StepAt(speed);
            GameSound.Play(cue, loud);
        }

        void OnJumped() => GameSound.Play(Cue.Jump);
        void OnAirJumped() => GameSound.Play(Cue.AirJump);
        void OnWallJumped() => GameSound.Play(Cue.WallJump);
        void OnDashed() => GameSound.Play(Cue.Dash);
        void OnLedgeGrabbed() => GameSound.Play(Cue.LedgeGrab);
        void OnClimbStarted() => GameSound.Play(Cue.Mantle);
        void OnSwingStarted() => GameSound.Play(Cue.RopeLatch);
        void OnSwingReleased() => GameSound.Play(Cue.RopeLetGo);

        void OnRolled()
        {
            rolledFrame = Time.frameCount;
            GameSound.Play(Cue.Roll);
        }

        void OnLanded()
        {
            var (cue, loud) = LandingFrom(motor.LastFallHeight);
            // (rolled out of it: the roll carries the fall, only a soft touch-down under it)
            if (rolledFrame == Time.frameCount) loud *= 0.5f;
            GameSound.Play(cue, loud);
        }

        void Update()
        {
            if (motor == null) return;
            // The ruin, under everything while he is in it: no sound breaks out of a digital silence.
            GameSound.SetLoop(Loop.Ambience, 1f);
            var v = motor.Velocity;
            // The slide along the floor and down a wall scrape as fast as he goes; the scarf sail hums in the air.
            GameSound.SetLoop(Loop.Slide, motor.IsSliding && motor.IsGrounded ? Mathf.InverseLerp(0.5f, 8f, Mathf.Abs(v.x)) : 0f,
                0.85f + 0.03f * Mathf.Abs(v.x));
            GameSound.SetLoop(Loop.WallSlide, motor.State == MotorState.WallSlide ? Mathf.InverseLerp(0.3f, 5f, Mathf.Abs(v.y)) : 0f,
                0.9f + 0.04f * Mathf.Abs(v.y));
            GameSound.SetLoop(Loop.Glide, motor.IsGliding ? 0.4f + 0.6f * Mathf.InverseLerp(1f, 8f, v.magnitude) : 0f,
                0.9f + 0.02f * v.magnitude);
            // On the whip: it creaks as hard as he swings, and the air rushes past at the bottom of a strong swing.
            float w = Mathf.Abs(motor.SwingAngularVelocity);
            GameSound.SetLoop(Loop.Rope, motor.IsSwinging ? 0.25f + 0.75f * Mathf.InverseLerp(0.3f, 3f, w) : 0f, 0.9f + 0.08f * w);
            swooshCooldown -= Time.deltaTime;
            if (motor.IsSwinging)
            {
                float a = motor.SwingAngle;
                if (Mathf.Sign(a) != Mathf.Sign(lastSwingAngle) && w > 1.5f && swooshCooldown <= 0f)
                {
                    GameSound.Play(Cue.RopeSwoosh, Mathf.InverseLerp(1.5f, 4f, w) * 0.7f + 0.3f);
                    swooshCooldown = 0.4f;
                }
                lastSwingAngle = a;
            }
            else lastSwingAngle = 0f;
        }
    }
}
