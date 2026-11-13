using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A crimson rope / thread that holds a creature (the hanged man's noose, the kite's string).
    /// Crimson means "the veil can touch this": any veil weapon or arrow cuts it.
    /// The owner positions it every frame with <see cref="Set"/>.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class CreatureThread : MonoBehaviour, IDamageable
    {
        [SerializeField] LineRenderer line;
        [SerializeField, Range(0f, 1f)] float sag = 0.08f;

        public bool IsCut { get; private set; }
        public event Action Cut;

        CapsuleCollider hitbox;

        void Awake()
        {
            hitbox = GetComponent<CapsuleCollider>();
            hitbox.isTrigger = true;
            hitbox.direction = 1;
            hitbox.radius = 0.12f;
            if (line == null) line = GetComponent<LineRenderer>();
        }

        public void Bind(LineRenderer renderer) => line = renderer;

        /// <summary>Stretches the thread from <paramref name="a"/> to <paramref name="b"/> (world space).</summary>
        public void Set(Vector3 a, Vector3 b)
        {
            if (IsCut) return;
            if (line != null)
            {
                int n = line.positionCount;
                float len = Vector3.Distance(a, b);
                for (int i = 0; i < n; i++)
                {
                    float t = n > 1 ? (float)i / (n - 1) : 0f;
                    line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * sag * len));
                }
            }
            if (hitbox == null) return;
            Vector3 mid = (a + b) * 0.5f, d = b - a;
            transform.position = mid;
            transform.rotation = d.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, d) : Quaternion.identity;
            hitbox.height = d.magnitude;
        }

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsCut) return false;
            IsCut = true;
            if (hitbox != null) hitbox.enabled = false;
            if (line != null) line.enabled = false;
            CombatFx.Burst(info.point, Vector2.down, CombatFx.Crimson, 18, 3f);
            Cut?.Invoke();
            return true;
        }
    }
}
