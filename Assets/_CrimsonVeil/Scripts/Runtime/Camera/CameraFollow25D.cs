using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// Side-on follow camera that shows where you are going:
    /// <list type="bullet">
    /// <item>leads where the hero is heading and stays on the floor he stands on (<see cref="CameraFocus"/>):
    /// jumps and quick turns do not shake the view; a real fall is led,</item>
    /// <item>peeks down over an edge the hero stands at,</item>
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
        [SerializeField] float leadDistance = 2.2f;
        [SerializeField] float followSmoothX = 0.18f;
        [SerializeField] float followSmoothFloor = 0.3f;
        [SerializeField] float followSmoothAir = 0.22f;

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
            currentOffset = target != null ? DesiredOffset(target.transform.position.x) : offset;
            if (target != null) Focus.Reset(target.transform.position);
            basePosition = target != null ? target.transform.position + currentOffset : transform.position;
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

        /// <summary>Platformer framing: lead from movement, floor-locked height (see <see cref="CameraFocus"/>).</summary>
        public CameraFocus Focus { get; } = new CameraFocus();

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 tp = target.transform.position;
            var motor = target.Motor;

            // Where to look: the hero's floor line and where he is heading (not where he happens to face).
            var state = motor.State;
            bool supported = motor.IsGrounded || state == MotorState.LedgeHang || state == MotorState.Climbing || state == MotorState.Swinging;
            Focus.lookAhead = leadDistance;
            var before = Focus.Focus;
            Vector2 focus = Focus.Step(dt, tp, target.Velocity, supported, motor.Settings.maxRunSpeed);
            bool snapped = (focus - before).sqrMagnitude > Focus.snapDistance * Focus.snapDistance;

            // Walking into a camera zone eases into its framing; arriving in one (respawn, teleport) takes it at once.
            currentOffset = snapped ? DesiredOffset(tp.x)
                : Vector3.Lerp(currentOffset, DesiredOffset(tp.x), 1f - Mathf.Exp(-2.5f * dt));

            // Deliberate look up/down: hold W/S while standing still.
            float lookInput = 0f;
            var input = target.InputSource;
            if (input != null && motor.IsGrounded && Mathf.Abs(input.MoveX) < 0.1f && Mathf.Abs(input.AimY) > 0.5f)
            {
                lookHeld += dt;
                if (lookHeld > lookHoldDelay) lookInput = input.AimY;
            }
            else lookHeld = 0f;

            // Peek down over an edge only while standing at it (running past ledges must not bob the view).
            bool standing = motor.IsGrounded && Mathf.Abs(target.Velocity.x) < 0.5f;
            float drop = standing ? ProbeDrop(tp, motor.Facing) : 0f;
            float wantedBias = ComputeVerticalBias(true, 0f, drop, lookInput, dropThreshold, maxDropLook, fallLead, maxFallLead, lookDistance);
            verticalBias = Mathf.SmoothDamp(verticalBias, wantedBias, ref verticalBiasVel, 0.45f, Mathf.Infinity, dt);

            Vector3 goal = new Vector3(focus.x + currentOffset.x, focus.y + currentOffset.y + verticalBias, tp.z + currentOffset.z);
            Vector3 p = basePosition;
            if (snapped)
            {
                p = goal;
                velX = velY = 0f;
            }
            else
            {
                p.x = Mathf.SmoothDamp(p.x, goal.x, ref velX, followSmoothX, Mathf.Infinity, dt);
                p.y = Mathf.SmoothDamp(p.y, goal.y, ref velY, supported ? followSmoothFloor : followSmoothAir, Mathf.Infinity, dt);
                p.z = goal.z;
            }
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
