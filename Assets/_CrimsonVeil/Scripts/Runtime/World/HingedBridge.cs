using CrimsonVeil.Combat;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// A plank or pillar standing on a hinge. When released it falls (accelerating like gravity),
    /// thuds and settles with a small bounce — becoming a bridge.
    /// </summary>
    public sealed class HingedBridge : MonoBehaviour
    {
        [SerializeField] float fromAngle;
        [SerializeField] float toAngle = -90f;
        [SerializeField] float duration = 0.9f;

        public bool Released { get; private set; }
        public bool Landed { get; private set; }

        float t;

        public void Configure(float from, float to, float seconds)
        {
            fromAngle = from;
            toAngle = to;
            duration = seconds;
            transform.localRotation = Quaternion.Euler(0f, 0f, from);
        }

        public void Release()
        {
            if (Released) return;
            Released = true;
            t = 0f;
        }

        void Update()
        {
            if (!Released || Landed) return;
            t += Time.deltaTime / Mathf.Max(0.05f, duration);
            transform.localRotation = Quaternion.Euler(0f, 0f, FallAngle(t, fromAngle, toAngle));
            if (t >= 1f)
            {
                Landed = true;
                CombatFx.Shake(0.5f);
                CombatFx.Burst(transform.position + transform.up * 2f, Vector2.up, CombatFx.Ink, 30, 4f);
            }
        }

        /// <summary>
        /// Angle at normalised time t: quadratic (gravity-like) fall until 85 %, then a small bounce.
        /// </summary>
        public static float FallAngle(float t, float from, float to)
        {
            t = Mathf.Clamp01(t);
            const float impact = 0.85f;
            float p;
            if (t < impact)
            {
                float u = t / impact;
                p = u * u;
            }
            else
            {
                float u = (t - impact) / (1f - impact);
                p = 1f - Mathf.Sin(u * Mathf.PI) * 0.05f;
            }
            return Mathf.LerpUnclamped(from, to, p);
        }
    }
}
