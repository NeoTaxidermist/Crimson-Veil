using System;
using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    [Serializable]
    public class MotorSettings
    {
        [Header("Run")]
        public float maxRunSpeed = 8f;
        public float groundAcceleration = 85f;
        public float groundDeceleration = 100f;
        public float turnAcceleration = 160f;
        public float airAcceleration = 55f;
        public float airDeceleration = 22f;

        [Header("Jump")]
        public float jumpHeight = 3.4f;
        public float timeToApex = 0.38f;
        public float fallGravityMultiplier = 1.6f;
        public float jumpCutGravityMultiplier = 2.8f;
        [Tooltip("|vy| below this counts as the apex of a jump.")]
        public float apexThreshold = 1.6f;
        public float apexGravityMultiplier = 0.55f;
        public float apexSpeedBonus = 1.1f;
        public float maxFallSpeed = 18f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.13f;
        public float groundStickVelocity = 2f;
        [Tooltip("Jumps in the air before touching ground, a ledge or a wall again (the double jump with a somersault).")]
        public int airJumps = 1;
        public float airJumpHeight = 2.4f;

        [Header("Dash")]
        public float dashSpeed = 19f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.35f;
        [Tooltip("Fraction of dash speed kept when the dash ends.")]
        public float dashEndSpeedFactor = 0.55f;

        [Header("Wall")]
        public float wallSlideSpeed = 2.3f;
        public float wallJumpHeight = 3.3f;
        public float wallJumpPush = 7.5f;
        [Tooltip("After a wall jump, horizontal input is ignored this long so the push carries.")]
        public float wallJumpInputLock = 0.12f;
        public float wallCoyoteTime = 0.1f;

        [Header("Ledge")]
        public float climbDuration = 0.34f;
        public float ledgeRegrabDelay = 0.3f;

        [Header("Whip swing")]
        [Tooltip("Gravity while hanging on the whip, as a fraction of the jump gravity (lower = a slower, graceful arc).")]
        public float swingGravityFactor = 0.55f;
        [Tooltip("Tangential push (m/s²) from leaning into the swing with left/right.")]
        public float swingPump = 15f;
        [Tooltip("Up/down climbs the whip (m/s).")]
        public float swingReelSpeed = 3f;
        public float swingMinLength = 1.6f;
        public float swingMaxLength = 7.5f;
        [Tooltip("Furthest the swing may rise (degrees from hanging straight down).")]
        public float swingMaxAngle = 100f;
        [Tooltip("Letting go multiplies the swing speed by this and adds an upward kick (m/s).")]
        public float swingReleaseBoost = 1.15f;
        public float swingReleaseLift = 5f;
        [Tooltip("After letting go, speed above the run speed bleeds off this slowly (m/s²) — the leap carries.")]
        public float swingCarryDrag = 3f;
        public float swingCarryTime = 0.7f;
        public float SwingGravity => Gravity * swingGravityFactor;

        public float Gravity => MovementMath.GravityForJump(jumpHeight, timeToApex);
        public float JumpVelocity => MovementMath.JumpVelocity(jumpHeight, timeToApex);
        public float WallJumpVelocity => Mathf.Sqrt(2f * Gravity * wallJumpHeight);
        public float AirJumpVelocity => Mathf.Sqrt(2f * Gravity * airJumpHeight);
    }

    public enum MotorState
    {
        Normal,
        WallSlide,
        LedgeHang,
        Climbing,
        /// <summary>Hanging from the whip latched to a grapple point, swinging as a pendulum.</summary>
        Swinging,
    }

    public struct MotorInput
    {
        public float moveX;
        /// <summary>Vertical stick/keys: up climbs a ledge, down drops from it.</summary>
        public float aimY;
        public bool jumpPressed;
        public bool jumpHeld;
        public bool dashPressed;
    }

    public struct MotorContacts
    {
        public bool grounded;
        public bool ceiling;
        /// <summary>-1 wall on the left, +1 wall on the right, 0 none.</summary>
        public int wallSide;
        /// <summary>Direction of a grabbable ledge in reach (-1/+1), 0 if none.</summary>
        public int ledgeSide;
        /// <summary>Where the hero holds the whip (hands), world XY — needed while swinging.</summary>
        public Vector2 gripPosition;
    }

    /// <summary>
    /// Engine-independent platformer motor: velocity in, velocity out.
    /// Coyote time, jump buffering, variable jump height, apex hang, fast fall, dash,
    /// wall slide / wall jump / wall dash and ledge grab + climb.
    /// </summary>
    public sealed class MotorLogic
    {
        public MotorSettings Settings { get; }
        public Vector2 Velocity;

        public MotorState State { get; private set; } = MotorState.Normal;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => dashTimer > 0f;
        public bool IsJumping { get; private set; }
        public bool IsStunned => stunTimer > 0f;
        public int Facing { get; private set; } = 1;

        /// <summary>Seconds of gliding left (granted by a captured kite's cloth); spent only while gliding.</summary>
        public float GlideTime { get; set; }
        /// <summary>True while the hero floats down holding jump.</summary>
        public bool IsGliding { get; private set; }
        /// <summary>The scarf can carry the hero: press jump again while falling and hold it to glide.</summary>
        public bool ScarfGlide { get; set; }
        /// <summary>The shield is held overhead: the fall is slowed without holding jump.</summary>
        public bool Umbrella { get; set; }
        bool glideLatched;

        /// <summary>Bounce up off something struck below (the spear's downward thrust).</summary>
        public void Bounce(float factor = 0.95f)
        {
            Velocity.y = Settings.JumpVelocity * factor;
            IsJumping = true;
            IsGrounded = false;
            airDashAvailable = true;
            airJumpsUsed = 0;
            glideLatched = false;
            Jumped?.Invoke();
        }
        /// <summary>Fall speed (m/s) while gliding.</summary>
        public const float GlideFallSpeed = 2.2f;
        /// <summary>Side of the wall/ledge the hero is attached to.</summary>
        public int AttachSide { get; private set; }
        /// <summary>0..1 progress of a ledge climb.</summary>
        public float ClimbProgress { get; private set; }

        public event Action Jumped;
        /// <summary>The second jump, in the air (the hero somersaults).</summary>
        public event Action AirJumped;
        /// <summary>Air jumps still available before landing.</summary>
        public int AirJumpsLeft => Mathf.Max(0, Settings.airJumps - airJumpsUsed);
        public event Action Landed;
        public event Action Dashed;
        public event Action WallJumped;
        public event Action LedgeGrabbed;
        public event Action ClimbStarted;
        public event Action ClimbFinished;
        public event Action SwingStarted;
        /// <summary>The hero let go with a jump (the leap) — not fired when the swing just ends.</summary>
        public event Action SwingReleased;
        /// <summary>Any end of a swing: leap, touching ground, a hit.</summary>
        public event Action SwingEnded;

        // --- Whip swing (a pendulum around the grapple point) ---------------------------------------
        public Vector2 SwingAnchor { get; private set; }
        public float SwingLength { get; private set; }
        /// <summary>Radians from hanging straight down; + = the hero is to the right of the anchor.</summary>
        public float SwingAngle { get; private set; }
        /// <summary>Angular velocity (rad/s), + = swinging towards the right.</summary>
        public float SwingAngularVelocity { get; private set; }
        public bool IsSwinging => State == MotorState.Swinging;
        float carryTimer;

        /// <summary>Hands position for a given angle/length around the anchor.</summary>
        public static Vector2 SwingPoint(Vector2 anchor, float length, float angle)
            => anchor + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * length;

        /// <summary>Unit tangent of the swing at <paramref name="angle"/> (direction of + angular velocity).</summary>
        public static Vector2 SwingTangent(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        /// <summary>
        /// Latches onto a grapple point: the hands at <paramref name="grip"/> hang from <paramref name="anchor"/>
        /// and keep their current momentum along the swing. False if the anchor is not above the hands or
        /// out of the whip's length.
        /// </summary>
        public bool BeginSwing(Vector2 anchor, Vector2 grip)
        {
            var s = Settings;
            Vector2 d = grip - anchor;
            float len = d.magnitude;
            if (d.y > -0.3f || len > s.swingMaxLength + 0.5f || State == MotorState.Climbing || State == MotorState.Swinging) return false;
            SwingAnchor = anchor;
            SwingLength = Mathf.Clamp(len, s.swingMinLength, s.swingMaxLength);
            SwingAngle = Mathf.Atan2(d.x, -d.y);
            // Keep the run/jump momentum along the swing. Leaping up at the ring, the rope is slack and the
            // straight projection would swing the hero backwards: the swing goes the way he was travelling.
            float along = Vector2.Dot(Velocity, SwingTangent(SwingAngle));
            if (Mathf.Abs(Velocity.x) > 1f && Mathf.Sign(along) != Mathf.Sign(Velocity.x))
                along = Mathf.Sign(Velocity.x) * Mathf.Abs(Velocity.x) * 0.8f;
            SwingAngularVelocity = along / SwingLength;
            State = MotorState.Swinging;
            IsGrounded = false;
            IsJumping = false;
            dashTimer = 0f;
            airDashAvailable = true;
            airJumpsUsed = 0;
            lastWallJumpSide = 0;
            SwingStarted?.Invoke();
            return true;
        }

        /// <summary>Lets go of the whip without a leap (the whip melted, a cutscene…).</summary>
        public void ReleaseSwing()
        {
            if (State != MotorState.Swinging) return;
            Velocity = SwingVelocity;
            EndSwing(false);
        }

        /// <summary>Velocity of the hands along the swing (m/s).</summary>
        public Vector2 SwingVelocity => SwingTangent(SwingAngle) * (SwingAngularVelocity * SwingLength);

        void EndSwing(bool leap)
        {
            var s = Settings;
            State = MotorState.Normal;
            if (leap)
            {
                Velocity = SwingVelocity * s.swingReleaseBoost + new Vector2(0f, s.swingReleaseLift);
                if (Mathf.Abs(Velocity.x) > 0.5f) Facing = Velocity.x > 0f ? 1 : -1;
                IsJumping = true;
                carryTimer = s.swingCarryTime;
                SwingReleased?.Invoke();
            }
            regrabTimer = s.ledgeRegrabDelay;
            SwingEnded?.Invoke();
        }

        /// <summary>Pendulum step. Returns the velocity that moves the hands to their new place on the arc.</summary>
        Vector2 TickSwing(float dt, MotorInput input, MotorContacts contacts, float moveX)
        {
            var s = Settings;
            if (bufferTimer > 0f)
            {
                bufferTimer = 0f;
                EndSwing(true);
                return Velocity;
            }
            // Re-sync with where the hands really are (a wall may have stopped them): a blocked swing loses momentum.
            Vector2 d = contacts.gripPosition - SwingAnchor;
            if (d.sqrMagnitude > 1e-4f)
            {
                float actual = Mathf.Atan2(d.x, -d.y);
                if (Mathf.Abs(Mathf.DeltaAngle(actual * Mathf.Rad2Deg, SwingAngle * Mathf.Rad2Deg)) > 6f) SwingAngularVelocity *= 0.3f;
                SwingAngle = actual;
            }
            // Feet on the ground: down lets go of the whip; otherwise the whip hauls the hero off the ground.
            if (contacts.grounded && input.aimY < -0.5f)
            {
                EndSwing(false);
                Velocity = new Vector2(SwingVelocity.x, 0f);
                return Velocity;
            }
            float reel = contacts.grounded ? Mathf.Max(input.aimY, 0f) + 1.5f : input.aimY;

            SwingLength = Mathf.Clamp(SwingLength - reel * s.swingReelSpeed * dt, s.swingMinLength, s.swingMaxLength);
            float L = SwingLength;
            float acc = -(s.SwingGravity / L) * Mathf.Sin(SwingAngle)
                        + moveX * s.swingPump * Mathf.Cos(SwingAngle) / L;
            SwingAngularVelocity += acc * dt;
            SwingAngularVelocity *= Mathf.Exp(-0.12f * dt);
            float next = SwingAngle + SwingAngularVelocity * dt;
            float max = s.swingMaxAngle * Mathf.Deg2Rad;
            if (Mathf.Abs(next) > max)
            {
                next = Mathf.Sign(next) * max;
                SwingAngularVelocity = 0f;
            }
            SwingAngle = next;
            Vector2 target = SwingPoint(SwingAnchor, L, SwingAngle);
            Velocity = Vector2.ClampMagnitude((target - contacts.gripPosition) / dt, 40f);
            return Velocity;
        }

        float coyoteTimer;
        float bufferTimer;
        float dashTimer;
        float dashCooldownTimer;
        float stunTimer;
        float wallCoyoteTimer;
        int lastWallSide;
        int lastWallJumpSide;
        float inputLockTimer;
        float regrabTimer;
        bool airDashAvailable = true;
        int airJumpsUsed;

        public MotorLogic(MotorSettings settings)
        {
            Settings = settings ?? new MotorSettings();
        }

        /// <summary>Pushes the hero (hit reaction). Input is ignored while stunned.</summary>
        public void ApplyKnockback(Vector2 velocity, float stunTime)
        {
            bool wasSwinging = State == MotorState.Swinging;
            Velocity = velocity;
            stunTimer = Mathf.Max(stunTimer, stunTime);
            dashTimer = 0f;
            IsJumping = false;
            State = MotorState.Normal;
            if (wasSwinging) SwingEnded?.Invoke(); // a hit knocks the hero off the whip
            if (velocity.y > 0f) IsGrounded = false;
        }

        /// <summary>A short step forward into an attack (only on the ground).</summary>
        public void Lunge(float speed)
        {
            if (!IsGrounded || State != MotorState.Normal) return;
            Velocity.x = Facing * Mathf.Max(Facing * Velocity.x, speed);
        }

        public Vector2 Tick(float dt, MotorInput input, MotorContacts contacts)
        {
            var s = Settings;
            if (dt <= 0f) return Velocity;
            IsGliding = false;

            if (stunTimer > 0f)
            {
                stunTimer -= dt;
                input = new MotorInput();
            }
            regrabTimer -= dt;
            bufferTimer = input.jumpPressed ? s.jumpBufferTime : bufferTimer - dt;
            dashCooldownTimer -= dt;
            inputLockTimer -= dt;

            float moveX = Mathf.Clamp(input.moveX, -1f, 1f);
            bool hasInput = Mathf.Abs(moveX) > 0.01f;
            int inputSide = hasInput ? (moveX > 0f ? 1 : -1) : 0;
            carryTimer -= dt;

            if (State == MotorState.Swinging) return TickSwing(dt, input, contacts, moveX);

            // --- Ledge hang & climb (position is driven by the controller) -------------
            if (State == MotorState.Climbing)
            {
                ClimbProgress = Mathf.Min(1f, ClimbProgress + dt / Mathf.Max(0.01f, s.climbDuration));
                Velocity = Vector2.zero;
                if (ClimbProgress >= 1f)
                {
                    State = MotorState.Normal;
                    IsGrounded = true;
                    coyoteTimer = s.coyoteTime;
                    ClimbFinished?.Invoke();
                }
                return Velocity;
            }

            if (State == MotorState.LedgeHang)
            {
                Velocity = Vector2.zero;
                airDashAvailable = true;
                airJumpsUsed = 0;
                lastWallJumpSide = 0;
                bool jump = bufferTimer > 0f;
                if (jump && inputSide == -AttachSide)
                {
                    bufferTimer = 0f;
                    WallJump(AttachSide);
                    return Velocity;
                }
                if (jump || input.aimY > 0.5f)
                {
                    bufferTimer = 0f;
                    State = MotorState.Climbing;
                    ClimbProgress = 0f;
                    ClimbStarted?.Invoke();
                    return Velocity;
                }
                if (input.aimY < -0.5f)
                {
                    State = MotorState.Normal; // let go
                    regrabTimer = s.ledgeRegrabDelay;
                }
                else return Velocity;
            }

            // --- Ground / wall bookkeeping ---------------------------------------------
            bool wasGrounded = IsGrounded;
            IsGrounded = contacts.grounded;
            if (IsGrounded)
            {
                coyoteTimer = s.coyoteTime;
                airDashAvailable = true;
                airJumpsUsed = 0;
                wallCoyoteTimer = 0f;
                lastWallJumpSide = 0;
                if (!wasGrounded && Velocity.y <= 0f)
                {
                    IsJumping = false;
                    Landed?.Invoke();
                }
            }
            else
            {
                coyoteTimer -= dt;
                if (contacts.wallSide != 0)
                {
                    lastWallSide = contacts.wallSide;
                    wallCoyoteTimer = s.wallCoyoteTime;
                    // A fresh wall refreshes the air dash — the wall you just kicked off does not.
                    if (contacts.wallSide != lastWallJumpSide) airDashAvailable = true;
                }
                else wallCoyoteTimer -= dt;
            }

            bool locked = inputLockTimer > 0f;
            if (hasInput && !IsDashing && !locked) Facing = inputSide;

            // Ledge grab: falling or near the apex, pushing into a ledge.
            if (!IsGrounded && contacts.ledgeSide != 0 && regrabTimer <= 0f && Velocity.y <= 2f && !IsDashing
                && (inputSide == contacts.ledgeSide || contacts.wallSide == contacts.ledgeSide))
            {
                State = MotorState.LedgeHang;
                AttachSide = contacts.ledgeSide;
                Facing = AttachSide;
                Velocity = Vector2.zero;
                IsJumping = false;
                LedgeGrabbed?.Invoke();
                return Velocity;
            }

            // Wall slide: airborne, falling, pushing into a wall.
            bool sliding = !IsGrounded && contacts.wallSide != 0 && inputSide == contacts.wallSide && Velocity.y <= 0f && !IsDashing;
            State = sliding ? MotorState.WallSlide : MotorState.Normal;
            if (sliding)
            {
                AttachSide = contacts.wallSide;
                Facing = AttachSide;
            }

            bool canJump = bufferTimer > 0f && coyoteTimer > 0f;
            int jumpWall = sliding ? contacts.wallSide : lastWallSide;
            // No climbing a single wall by kicking off it again and again: the next wall jump must be
            // from the other side (or after touching ground / grabbing a ledge).
            bool canWallJump = !canJump && bufferTimer > 0f && !IsGrounded && (sliding || wallCoyoteTimer > 0f)
                               && jumpWall != 0 && jumpWall != lastWallJumpSide;
            // Double jump: a fresh press in the air (a buffered one does not fire it late). Once it is spent, a
            // press just before landing is buffered into a ground jump. A wall slide refreshes it like the ground.
            if (sliding) airJumpsUsed = 0;
            bool canAirJump = !canJump && !canWallJump && input.jumpPressed && !IsGrounded && !sliding
                              && airJumpsUsed < s.airJumps && !IsStunned;

            // --- Dash (from a wall it always launches away from it) ------------------------
            if (input.dashPressed && !IsDashing && dashCooldownTimer <= 0f && (IsGrounded || airDashAvailable))
            {
                if (sliding) Facing = -contacts.wallSide;
                else if (hasInput) Facing = inputSide;
                dashTimer = s.dashDuration;
                dashCooldownTimer = s.dashCooldown;
                if (!IsGrounded && !sliding) airDashAvailable = false;
                IsJumping = false;
                State = MotorState.Normal;
                inputLockTimer = 0f;
                Dashed?.Invoke();
            }

            if (IsDashing)
            {
                if (canJump || canWallJump || canAirJump)
                {
                    dashTimer = 0f; // dash-jump keeps the momentum
                }
                else
                {
                    dashTimer -= dt;
                    Velocity = new Vector2(Facing * s.dashSpeed, 0f);
                    if (dashTimer <= 0f) Velocity.x *= s.dashEndSpeedFactor;
                    if (contacts.wallSide != 0 && contacts.wallSide == Facing)
                    {
                        Velocity.x = 0f;
                        dashTimer = 0f;
                    }
                    return Velocity;
                }
            }

            // --- Horizontal ----------------------------------------------------------------
            if (!locked)
            {
                float maxSpeed = s.maxRunSpeed;
                if (!IsGrounded && IsJumping && Mathf.Abs(Velocity.y) < s.apexThreshold) maxSpeed *= s.apexSpeedBonus;
                float target = moveX * maxSpeed;

                float rate;
                if (IsGrounded)
                {
                    if (!hasInput) rate = s.groundDeceleration;
                    else if (Mathf.Abs(Velocity.x) > 0.01f && Mathf.Sign(target) != Mathf.Sign(Velocity.x)) rate = s.turnAcceleration;
                    else rate = s.groundAcceleration;
                }
                else
                {
                    rate = hasInput ? s.airAcceleration : s.airDeceleration;
                }

                if (hasInput && Mathf.Sign(target) == Mathf.Sign(Velocity.x) && Mathf.Abs(Velocity.x) > Mathf.Abs(target))
                    rate = IsGrounded ? s.groundDeceleration : s.airDeceleration;
                // The leap off a swing carries while the hero keeps leaning into it; letting go of the
                // direction (or steering against it) brakes as usual, so he can drop onto a perch.
                if (carryTimer > 0f && !IsGrounded && Mathf.Abs(Velocity.x) > s.maxRunSpeed
                    && hasInput && Mathf.Sign(target) == Mathf.Sign(Velocity.x))
                {
                    rate = s.swingCarryDrag;
                    target = Mathf.Sign(Velocity.x) * s.maxRunSpeed;
                }
                if (IsStunned) rate = s.airDeceleration;

                Velocity.x = Mathf.MoveTowards(Velocity.x, target, rate * dt);
            }

            if (contacts.wallSide != 0 && Mathf.Sign(Velocity.x) == contacts.wallSide && Mathf.Abs(Velocity.x) > 0f)
                Velocity.x = 0f;

            // --- Vertical ------------------------------------------------------------------
            if (canJump)
            {
                Velocity.y = s.JumpVelocity;
                IsJumping = true;
                IsGrounded = false;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                Jumped?.Invoke();
            }
            else if (canWallJump)
            {
                bufferTimer = 0f;
                WallJump(jumpWall);
            }
            else if (canAirJump)
            {
                Velocity.y = s.AirJumpVelocity;
                airJumpsUsed++;
                IsJumping = true;
                bufferTimer = 0f;
                glideLatched = false;
                AirJumped?.Invoke();
            }
            else if (IsGrounded && Velocity.y <= 0f)
            {
                Velocity.y = -s.groundStickVelocity;
            }
            else
            {
                float mult = 1f;
                if (Velocity.y < 0f) mult = s.fallGravityMultiplier;
                else if (IsJumping && !input.jumpHeld) mult = s.jumpCutGravityMultiplier;
                if (IsJumping && input.jumpHeld && Mathf.Abs(Velocity.y) < s.apexThreshold)
                    mult = s.apexGravityMultiplier;

                Velocity.y -= s.Gravity * mult * dt;
                if (Velocity.y < -s.maxFallSpeed) Velocity.y = -s.maxFallSpeed;
                // Gliding: on a captured kite's cloth holding jump while falling floats the hero down; the scarf
                // does it too, on a fresh press of jump in the air (so a held jump never turns floaty by itself);
                // a shield held overhead slows the fall like an umbrella.
                if (input.jumpPressed && Velocity.y < 0f) glideLatched = true;
                if (!input.jumpHeld) glideLatched = false;
                bool held = input.jumpHeld && Velocity.y < 0f && State == MotorState.Normal;
                IsGliding = (held && (GlideTime > 0f || (ScarfGlide && glideLatched))) || (Umbrella && Velocity.y < 0f && State == MotorState.Normal);
                if (IsGliding)
                {
                    if (GlideTime > 0f && held) GlideTime = Mathf.Max(0f, GlideTime - dt);
                    if (Velocity.y < -GlideFallSpeed) Velocity.y = Mathf.MoveTowards(Velocity.y, -GlideFallSpeed, s.Gravity * 3f * dt);
                }
                if (State == MotorState.WallSlide && Velocity.y < -s.wallSlideSpeed) Velocity.y = -s.wallSlideSpeed;
            }

            if (contacts.ceiling && Velocity.y > 0f) Velocity.y = 0f;
            return Velocity;
        }

        void WallJump(int wallSide)
        {
            var s = Settings;
            Velocity = new Vector2(-wallSide * s.wallJumpPush, s.WallJumpVelocity);
            Facing = -wallSide;
            lastWallJumpSide = wallSide;
            State = MotorState.Normal;
            IsJumping = true;
            IsGrounded = false;
            wallCoyoteTimer = 0f;
            coyoteTimer = 0f;
            inputLockTimer = s.wallJumpInputLock;
            regrabTimer = s.ledgeRegrabDelay;
            WallJumped?.Invoke();
        }

        public void ResetState()
        {
            Velocity = Vector2.zero;
            State = MotorState.Normal;
            IsGrounded = false;
            IsJumping = false;
            coyoteTimer = bufferTimer = dashTimer = dashCooldownTimer = stunTimer = 0f;
            wallCoyoteTimer = inputLockTimer = regrabTimer = 0f;
            lastWallJumpSide = 0;
            ClimbProgress = 0f;
            airDashAvailable = true;
            airJumpsUsed = 0;
            carryTimer = 0f;
            SwingAngularVelocity = 0f;
        }
    }
}
