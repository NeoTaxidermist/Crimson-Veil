using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// Side-on follow camera that shows where you are going:
    /// <list type="bullet">
    /// <item>look-ahead in the facing / moving direction,</item>
    /// <item>looks down when there is a drop ahead or below, and leads the fall,</item>
    /// <item>hold up/down (W/S) while standing to look up or down,</item>
    /// <item>camera zones can override the framing (puzzles, shafts),</item>
    /// <item>trauma-based shake (unscaled time, keeps rumbling through hit-stop).</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class CameraFollow25D : MonoBehaviour
    {
        [SerializeField] PlayerController target;
        [SerializeField] Vector3 offset = new Vector3(0f, 2f, -15f);
        [SerializeField] float lookAhead = 3.4f;
        [SerializeField] float lookAheadSmooth = 0.45f;
        [SerializeField] float smoothX = 0.12f;
        [SerializeField] float smoothYGrounded = 0.2f;
        [SerializeField] float smoothYAir = 0.4f;

        [Header("Seeing what's below")]
        [Tooltip("How far ahead (m) to probe for a drop.")]
        [SerializeField] float dropProbeAhead = 4.5f;
        [Tooltip("A drop deeper than this lowers the view.")]
        [SerializeField] float dropThreshold = 2.5f;
        [SerializeField] float maxDropLook = 3.5f;
        [Tooltip("Seconds of fall velocity the camera leads by.")]
        [SerializeField] float fallLead = 0.28f;
        [SerializeField] float maxFallLead = 5f;

        [Header("Look up / down (hold W/S while standing)")]
        [SerializeField] float lookHoldDelay = 0.35f;
        [SerializeField] float lookDistance = 4.5f;

        [Header("Shake")]
        [SerializeField] float maxShakeOffset = 0.35f;
        [SerializeField] float maxShakeRoll = 1.5f;
        [SerializeField] float traumaDecay = 1.6f;
        [SerializeField] float shakeFrequency = 22f;
        [SerializeField] LayerMask environment = ~0;

        static CameraFollow25D instance;

        float lookAheadCurrent, lookAheadVel;
        float velX, velY;
        Vector3 basePosition;
        float trauma;
        CameraZone[] zones = new CameraZone[0];
        Vector3 currentOffset;
        float verticalBias, verticalBiasVel;
        float lookHeld;

        public PlayerController Target { get => target; set => target = value; }
        public float Trauma => trauma;
        /// <summary>Current extra vertical framing (negative = looking down).</summary>
        public float VerticalBias => verticalBias;

        public static void AddTrauma(float amount)
        {
            if (instance != null) instance.trauma = Mathf.Clamp01(instance.trauma + amount);
        }

        void OnEnable() => instance = this;

        void OnDisable()
        {
            if (instance == this) instance = null;
        }

        void Start()
        {
            if (target == null) target = FindFirstObjectByType<PlayerController>();
            zones = FindObjectsByType<CameraZone>(FindObjectsSortMode.None);
            currentOffset = offset;
            basePosition = target != null ? target.transform.position + offset : transform.position;
            transform.position = basePosition;
        }

        Vector3 DesiredOffset(float x)
        {
            foreach (var z in zones)
                if (z != null && z.Contains(x)) return z.offset;
            return offset;
        }

        /// <summary>
        /// Vertical framing bias: look down at drops ahead / under the hero, lead a fall, or follow the
        /// player's deliberate look up/down. Pure function of the inputs so it can be tested.
        /// </summary>
        public static float ComputeVerticalBias(bool grounded, float vy, float dropAhead, float lookInput,
                                                float dropThreshold, float maxDropLook, float fallLead, float maxFallLead, float lookDistance)
        {
            float bias = 0f;
            if (dropAhead > dropThreshold) bias -= Mathf.Min(maxDropLook, (dropAhead - dropThreshold) * 0.6f + 1f);
            if (!grounded && vy < -2f) bias -= Mathf.Min(maxFallLead, -vy * fallLead);
            if (Mathf.Abs(lookInput) > 0.5f) bias = Mathf.Sign(lookInput) * lookDistance;
            return bias;
        }

        float ProbeDrop(Vector3 feet, int facing)
        {
            // Depth of the ground a few metres ahead (and straight below when airborne).
            float worst = 0f;
            foreach (float ahead in new[] { dropProbeAhead * 0.5f, dropProbeAhead })
            {
                var from = feet + new Vector3(facing * ahead, 0.5f, 0f);
                float depth = Physics.Raycast(from, Vector3.down, out var hit, 30f, environment, QueryTriggerInteraction.Ignore)
                    ? feet.y - hit.point.y : 30f;
                worst = Mathf.Max(worst, depth);
            }
            return worst;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Vector3 tp = target.transform.position;
            var motor = target.Motor;

            currentOffset = Vector3.Lerp(currentOffset, DesiredOffset(tp.x), 1f - Mathf.Exp(-2.5f * dt));

            float moving = Mathf.Abs(target.Velocity.x) > 1f ? 1f : 0.5f;
            float desiredLook = motor.Facing * lookAhead * moving;
            lookAheadCurrent = Mathf.SmoothDamp(lookAheadCurrent, desiredLook, ref lookAheadVel, lookAheadSmooth, Mathf.Infinity, dt);

            // Deliberate look up/down: hold W/S while standing still.
            float lookInput = 0f;
            var input = target.InputSource;
            if (input != null && motor.IsGrounded && Mathf.Abs(input.MoveX) < 0.1f && Mathf.Abs(input.AimY) > 0.5f)
            {
                lookHeld += dt;
                if (lookHeld > lookHoldDelay) lookInput = input.AimY;
            }
            else lookHeld = 0f;

            float drop = ProbeDrop(tp, motor.Facing);
            float wantedBias = ComputeVerticalBias(motor.IsGrounded, target.Velocity.y, drop, lookInput,
                dropThreshold, maxDropLook, fallLead, maxFallLead, lookDistance);
            verticalBias = Mathf.SmoothDamp(verticalBias, wantedBias, ref verticalBiasVel, 0.35f, Mathf.Infinity, dt);

            Vector3 p = basePosition;
            float smoothY = target.IsGrounded || target.Velocity.y < -8f ? smoothYGrounded : smoothYAir;
            p.x = Mathf.SmoothDamp(p.x, tp.x + currentOffset.x + lookAheadCurrent, ref velX, smoothX, Mathf.Infinity, dt);
            p.y = Mathf.SmoothDamp(p.y, tp.y + currentOffset.y + verticalBias, ref velY, smoothY, Mathf.Infinity, dt);
            p.z = tp.z + currentOffset.z;
            basePosition = p;

            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            float shake = trauma * trauma;
            float t = Time.unscaledTime * shakeFrequency;
            Vector3 jitter = new Vector3(Mathf.PerlinNoise(t, 0.1f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * maxShakeOffset * shake);
            float roll = (Mathf.PerlinNoise(t, 3.3f) - 0.5f) * 2f * maxShakeRoll * shake;

            transform.position = p + jitter;
            transform.rotation = Quaternion.Euler(0f, 0f, roll);
        }
    }
}
