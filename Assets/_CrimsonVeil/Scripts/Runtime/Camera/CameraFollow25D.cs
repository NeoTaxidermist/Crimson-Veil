using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>Side-on follow camera with look-ahead and softer vertical tracking while airborne.</summary>
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

        float lookAheadCurrent, lookAheadVel;
        float velX, velY;

        public PlayerController Target { get => target; set => target = value; }

        void Start()
        {
            if (target == null) target = FindFirstObjectByType<PlayerController>();
            if (target != null) transform.position = target.transform.position + offset;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Vector3 tp = target.transform.position;

            float moving = Mathf.Abs(target.Velocity.x) > 1f ? 1f : 0.4f;
            float desiredLook = target.Motor.Facing * lookAhead * moving;
            lookAheadCurrent = Mathf.SmoothDamp(lookAheadCurrent, desiredLook, ref lookAheadVel, lookAheadSmooth, Mathf.Infinity, dt);

            Vector3 p = transform.position;
            float smoothY = target.IsGrounded || target.Velocity.y < -8f ? smoothYGrounded : smoothYAir;
            p.x = Mathf.SmoothDamp(p.x, tp.x + offset.x + lookAheadCurrent, ref velX, smoothX, Mathf.Infinity, dt);
            p.y = Mathf.SmoothDamp(p.y, tp.y + offset.y, ref velY, smoothY, Mathf.Infinity, dt);
            p.z = tp.z + offset.z;
            transform.position = p;
        }
    }
}
