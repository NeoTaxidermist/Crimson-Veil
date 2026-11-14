using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// A secret the scarf senses: within <see cref="radius"/> the loose scarf drifts towards it instead of
    /// streaming behind the hero — it says nothing, it only points. Place it at hidden urns, breakable walls,
    /// high perches.
    /// </summary>
    [ExecuteAlways]
    public sealed class VeilHint : MonoBehaviour
    {
        [Tooltip("How close (m) the hero must be before the scarf starts to lean towards the secret.")]
        [Min(1f)] public float radius = 9f;

        static readonly List<VeilHint> all = new List<VeilHint>();

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        /// <summary>
        /// Direction to the strongest secret near <paramref name="from"/> and how strongly it pulls (0..1, 1 = on top
        /// of it). Zero when none is in range.
        /// </summary>
        public static Vector2 Pull(Vector2 from, out float strength)
        {
            strength = 0f;
            Vector2 dir = Vector2.zero;
            foreach (var h in all)
            {
                if (h == null || !h.isActiveAndEnabled) continue;
                Vector2 d = (Vector2)h.transform.position - from;
                float k = 1f - d.magnitude / h.radius;
                if (k <= strength) continue;
                strength = k;
                dir = d.sqrMagnitude > 1e-4f ? d.normalized : Vector2.up;
            }
            return dir;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.1f, 0.15f, 0.8f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.35f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.1f, 0.15f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
