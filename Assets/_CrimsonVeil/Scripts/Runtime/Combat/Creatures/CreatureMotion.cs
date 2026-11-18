using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Moving a creature through the level without passing through it: a sphere swept along the step stops at the
    /// first wall, ledge or ceiling and slides along it (the hero, other creatures and triggers are not obstacles).
    /// Flyers steer freely towards where they want to be; this keeps them out of the stone on the way.
    /// </summary>
    public static class CreatureMotion
    {
        /// <summary>Kept between the sphere and the surface it stops at.</summary>
        public const float Skin = 0.02f;

        static readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>
        /// Where a sphere of <paramref name="radius"/> at <paramref name="from"/> ends up moving by
        /// <paramref name="step"/>: up to the first obstacle, then sliding along it (up to three surfaces).
        /// A sphere that starts inside stone is let out (it is not held where it is).
        /// </summary>
        public static Vector3 Slide(Vector3 from, Vector3 step, float radius, Transform self = null)
        {
            Vector3 pos = from;
            Vector3 rest = step;
            for (int i = 0; i < 3 && rest.sqrMagnitude > 1e-10f; i++)
            {
                float len = rest.magnitude;
                Vector3 dir = rest / len;
                if (!FirstObstacle(pos, dir, len + Skin, radius, self, out var hit))
                {
                    pos += rest;
                    break;
                }
                float advance = Mathf.Max(0f, hit.distance - Skin);
                pos += dir * advance;
                rest = dir * (len - advance);
                rest -= hit.normal * Vector3.Dot(rest, hit.normal);   // slide along the surface
                rest.z = 0f;
            }
            return pos;
        }

        /// <summary>Is a sphere of <paramref name="radius"/> at <paramref name="p"/> stuck in the level?</summary>
        public static bool Blocked(Vector3 p, float radius, Transform self = null)
        {
            foreach (var c in Physics.OverlapSphere(p, radius, ~0, QueryTriggerInteraction.Ignore))
                if (IsObstacle(c, self)) return true;
            return false;
        }

        static bool FirstObstacle(Vector3 pos, Vector3 dir, float dist, float radius, Transform self, out RaycastHit first)
        {
            first = default;
            float best = float.MaxValue;
            int n = Physics.SphereCastNonAlloc(pos, radius, dir, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                // started inside it (distance 0): let the creature out instead of pinning it there
                if (h.distance <= 0f) continue;
                if (!IsObstacle(h.collider, self)) continue;
                // only surfaces it moves into
                if (Vector3.Dot(h.normal, dir) >= 0f) continue;
                if (h.distance < best) { best = h.distance; first = h; }
            }
            return best < float.MaxValue;
        }

        /// <summary>The level's stone stops creatures; the hero, creatures, anything that can be hit and triggers don't.</summary>
        public static bool IsObstacle(Collider c, Transform self)
        {
            if (c == null || c.isTrigger) return false;
            if (self != null && c.transform.IsChildOf(self)) return false;
            if (c is CharacterController) return false;
            // anything that can be hit (creatures, crows, threads, the hero) is no wall
            if (c.GetComponentInParent<IDamageable>() != null) return false;
            if (c.GetComponentInParent<CrimsonVeil.Player.PlayerController>() != null) return false;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return false;   // loose props
            return true;
        }
    }
}
