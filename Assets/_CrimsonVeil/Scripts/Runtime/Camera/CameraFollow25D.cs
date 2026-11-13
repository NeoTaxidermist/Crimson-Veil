using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// Side-on follow camera with look-ahead, softer vertical tracking while airborne and
    /// trauma-based shake (shake = trauma², decays over time, smooth Perlin noise).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class CameraFollow25D : MonoBehaviour
    {
        [SerializeField] PlayerController target;
        [SerializeField] Vector3 offset = new Vector3(0f, 2.4f, -13f);
        [SerializeField] float lookAhead = 2.2f;
        [SerializeField] float lookAheadSmooth = 0.5f;
        [SerializeField] float smoothX = 0.12f;
        [SerializeField] float smoothYGrounded = 0.18f;
        [SerializeField] float smoothYAir = 0.45f;

        [Header("Shake")]
        [SerializeField] float maxShakeOffset = 0.35f;
        [SerializeField] float maxShakeRoll = 1.5f;
        [SerializeField] float traumaDecay = 1.6f;
        [SerializeField] float shakeFrequency = 22f;

        static CameraFollow25D instance;

        float lookAheadCurrent, lookAheadVel;
        float velX, velY;
        Vector3 basePosition;
        float trauma;

        public PlayerController Target { get => target; set => target = value; }
        public float Trauma => trauma;

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

        [SerializeField] float zoneBlendSharpness = 2.5f;
        CameraZone[] zones = new CameraZone[0];
        Vector3 currentOffset;

        Vector3 DesiredOffset(float x)
        {
            foreach (var z in zones)
                if (z != null && z.Contains(x)) return z.offset;
            return offset;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Vector3 tp = target.transform.position;

            float moving = Mathf.Abs(target.Velocity.x) > 1f ? 1f : 0.4f;
            float desiredLook = target.Motor.Facing * lookAhead * moving;
            lookAheadCurrent = Mathf.SmoothDamp(lookAheadCurrent, desiredLook, ref lookAheadVel, lookAheadSmooth, Mathf.Infinity, dt);

            currentOffset = Vector3.Lerp(currentOffset, DesiredOffset(tp.x), 1f - Mathf.Exp(-zoneBlendSharpness * dt));

            Vector3 p = basePosition;
            float smoothY = target.IsGrounded || target.Velocity.y < -8f ? smoothYGrounded : smoothYAir;
            p.x = Mathf.SmoothDamp(p.x, tp.x + currentOffset.x + lookAheadCurrent, ref velX, smoothX, Mathf.Infinity, dt);
            p.y = Mathf.SmoothDamp(p.y, tp.y + currentOffset.y, ref velY, smoothY, Mathf.Infinity, dt);
            p.z = tp.z + currentOffset.z;
            basePosition = p;

            // Shake uses unscaled time so it keeps rumbling through hit-stop.
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
