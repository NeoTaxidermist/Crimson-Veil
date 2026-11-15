using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Aiming without a mouse, the same on a keyboard and a gamepad: the direction keys / stick give one of eight
    /// directions, and a gentle aim assist turns the blow onto the target in a narrow cone around it. Angles are
    /// in degrees relative to the hero's facing: 0 ahead, +90 straight up, -90 straight down.
    /// </summary>
    public static class VeilAim
    {
        /// <summary>Half-angle (degrees) of the cone in which the aim assist finds a target.</summary>
        public const float AssistCone = 22f;

        /// <summary>
        /// The aimed direction from the inputs: up, up-ahead, ahead, down-ahead, down. Straight down only in the air
        /// (a strike under the feet); on the ground "down" means low and ahead for the weapons that reach there, and
        /// a level blow for the sword and spear (a cut into the floor is no use).
        /// </summary>
        public static float Direction(float moveX, float aimY, bool airborne, VeilForm form)
        {
            bool moving = Mathf.Abs(moveX) > 0.3f;
            if (aimY > 0.5f) return moving ? 45f : 90f;
            if (aimY < -0.5f)
            {
                if (airborne) return moving ? -45f : -90f;
                return form == VeilForm.Bow || form == VeilForm.Whip ? -45f : 0f;
            }
            return 0f;
        }

        /// <summary>Furthest a weapon can be aimed up / down (degrees). The shield is a ring round the body: not aimed.</summary>
        public static float Limit(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword:
                case VeilForm.Spear:
                case VeilForm.Whip:
                case VeilForm.Bow: return 90f;
                default: return 0f;
            }
        }

        /// <summary>How far a weapon's aim assist looks for a target (m).</summary>
        public static float Reach(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return 2.6f;
                case VeilForm.Spear: return 3.2f;
                case VeilForm.Whip: return 4.6f;
                case VeilForm.Bow: return 14f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Aim assist: the target nearest to the aimed direction (within <see cref="AssistCone"/>, within reach,
        /// in front of the hero), the closer the better; the blow turns onto it. No target: the aim as given.
        /// <paramref name="targets"/> are world positions, <paramref name="origin"/> where the blow starts.
        /// </summary>
        public static float Assist(float angle, Vector2 origin, int facing, IReadOnlyList<Vector2> targets, float reach, float limit)
        {
            if (targets == null || targets.Count == 0 || reach <= 0f) return angle;
            float best = float.MaxValue, bestAngle = angle;
            for (int i = 0; i < targets.Count; i++)
            {
                Vector2 d = targets[i] - origin;
                float dist = d.magnitude;
                if (dist < 1e-3f || dist > reach) continue;
                if (d.x * facing < -0.2f) continue; // behind the hero
                float a = Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
                float off = Mathf.Abs(Mathf.DeltaAngle(angle, a));
                if (off > AssistCone) continue;
                float score = off / AssistCone + dist / reach; // on the line first, then near
                if (score < best) { best = score; bestAngle = a; }
            }
            return Mathf.Clamp(bestAngle, -limit, limit);
        }
    }
}
