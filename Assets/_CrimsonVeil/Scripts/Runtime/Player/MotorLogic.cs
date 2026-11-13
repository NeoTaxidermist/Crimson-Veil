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

        public float Gravity => MovementMath.GravityForJump(jumpHeight, timeToApex);
        public float JumpVelocity => MovementMath.JumpVelocity(jumpHeight, timeToApex);
        public float WallJumpVelocity => Mathf.Sqrt(2f * Gravity * wallJumpHeight);
    }

    public enum MotorState
    {
        Normal,
        WallSlide,
        LedgeHang,
        Climbing,
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
        /// <summary>Side of the wall/ledge the hero is attached to.</summary>
        public int AttachSide { get; private set; }
        /// <summary>0..1 progress of a ledge climb.</summary>
        public float ClimbProgress { get; private set; }

        public event Action Jumped;
        public event Action Landed;
        public event Action Dashed;
        public event Action WallJumped;
        public event Action LedgeGrabbed;
        public event Action ClimbStarted;
        public event Action ClimbFinished;

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

        public MotorLogic(MotorSettings settings)
        {
            Settings = settings ?? new MotorSettings();
        }

        /// <summary>Pushes the hero (hit reaction). Input is ignored while stunned.</summary>
        public void ApplyKnockback(Vector2 velocity, float stunTime)
        {
            Velocity = velocity;
            stunTimer = Mathf.Max(stunTimer, stunTime);
            dashTimer = 0f;
            IsJumping = false;
            State = MotorState.Normal;
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
                if (canJump || canWallJump)
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
        }
    }
}
