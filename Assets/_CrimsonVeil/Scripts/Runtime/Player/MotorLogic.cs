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
        public float jumpHeight = 3.1f;
        public float timeToApex = 0.36f;
        public float fallGravityMultiplier = 1.8f;
        public float jumpCutGravityMultiplier = 2.8f;
        [Tooltip("|vy| below this counts as the apex of a jump.")]
        public float apexThreshold = 1.6f;
        public float apexGravityMultiplier = 0.55f;
        public float apexSpeedBonus = 1.1f;
        public float maxFallSpeed = 20f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.13f;
        public float groundStickVelocity = 2f;

        [Header("Dash")]
        public float dashSpeed = 19f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.35f;
        [Tooltip("Fraction of dash speed kept when the dash ends.")]
        public float dashEndSpeedFactor = 0.55f;

        public float Gravity => MovementMath.GravityForJump(jumpHeight, timeToApex);
        public float JumpVelocity => MovementMath.JumpVelocity(jumpHeight, timeToApex);
    }

    public struct MotorInput
    {
        public float moveX;
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
    }

    /// <summary>
    /// Engine-independent platformer motor: velocity in, velocity out.
    /// Coyote time, jump buffering, variable jump height, apex hang, fast fall and dash.
    /// </summary>
    public sealed class MotorLogic
    {
        public MotorSettings Settings { get; }
        public Vector2 Velocity;

        public bool IsGrounded { get; private set; }
        public bool IsDashing => dashTimer > 0f;
        public bool IsJumping { get; private set; }
        public int Facing { get; private set; } = 1;

        public event Action Jumped;
        public event Action Landed;
        public event Action Dashed;

        float coyoteTimer;
        float bufferTimer;
        float dashTimer;
        float dashCooldownTimer;
        bool airDashAvailable = true;

        public MotorLogic(MotorSettings settings)
        {
            Settings = settings ?? new MotorSettings();
        }

        public Vector2 Tick(float dt, MotorInput input, MotorContacts contacts)
        {
            var s = Settings;
            if (dt <= 0f) return Velocity;

            bool wasGrounded = IsGrounded;
            IsGrounded = contacts.grounded;

            if (IsGrounded)
            {
                coyoteTimer = s.coyoteTime;
                airDashAvailable = true;
                if (!wasGrounded && Velocity.y <= 0f)
                {
                    IsJumping = false;
                    Landed?.Invoke();
                }
            }
            else
            {
                coyoteTimer -= dt;
            }

            bufferTimer = input.jumpPressed ? s.jumpBufferTime : bufferTimer - dt;
            dashCooldownTimer -= dt;

            float moveX = Mathf.Clamp(input.moveX, -1f, 1f);
            bool hasInput = Mathf.Abs(moveX) > 0.01f;
            if (hasInput && !IsDashing) Facing = moveX > 0f ? 1 : -1;

            bool canJump = bufferTimer > 0f && coyoteTimer > 0f;

            // --- Dash -------------------------------------------------------
            if (input.dashPressed && !IsDashing && dashCooldownTimer <= 0f && (IsGrounded || airDashAvailable))
            {
                if (hasInput) Facing = moveX > 0f ? 1 : -1;
                dashTimer = s.dashDuration;
                dashCooldownTimer = s.dashCooldown;
                if (!IsGrounded) airDashAvailable = false;
                IsJumping = false;
                Dashed?.Invoke();
            }

            if (IsDashing)
            {
                if (canJump)
                {
                    // Dash-jump: cancel the dash but keep its momentum.
                    dashTimer = 0f;
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

            // --- Horizontal -------------------------------------------------
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

            // Keep momentum (dash-jumps) when moving faster than max speed in the input direction.
            if (hasInput && Mathf.Sign(target) == Mathf.Sign(Velocity.x) && Mathf.Abs(Velocity.x) > Mathf.Abs(target))
                rate = IsGrounded ? s.groundDeceleration : s.airDeceleration;

            Velocity.x = Mathf.MoveTowards(Velocity.x, target, rate * dt);

            if (contacts.wallSide != 0 && Mathf.Sign(Velocity.x) == contacts.wallSide && Mathf.Abs(Velocity.x) > 0f)
                Velocity.x = 0f;

            // --- Vertical ---------------------------------------------------
            if (canJump)
            {
                Velocity.y = s.JumpVelocity;
                IsJumping = true;
                IsGrounded = false;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                Jumped?.Invoke();
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
            }

            if (contacts.ceiling && Velocity.y > 0f) Velocity.y = 0f;

            return Velocity;
        }

        public void ResetState()
        {
            Velocity = Vector2.zero;
            IsGrounded = false;
            IsJumping = false;
            coyoteTimer = bufferTimer = dashTimer = dashCooldownTimer = 0f;
            airDashAvailable = true;
        }
    }
}
