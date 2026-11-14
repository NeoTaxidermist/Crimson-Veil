using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>Per-form combat numbers: damage, knockback, hit radius and the active part of the swing.</summary>
    public static class VeilCombatProfile
    {
        public static float Damage(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.75f;
                case VeilForm.Sword: return 1f;
                case VeilForm.Spear: return 1.25f;
                case VeilForm.Shield: return 0.5f;
                case VeilForm.Bow: return 1f; // per arrow
                default: return 0f;
            }
        }

        public static float Knockback(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 3f;
                case VeilForm.Sword: return 5f;
                case VeilForm.Spear: return 7f;
                case VeilForm.Shield: return 11f;
                default: return 4f;
            }
        }

        public static float HitRadius(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Shield: return 0.45f;
                case VeilForm.Sword: return 0.25f;
                default: return 0.18f;
            }
        }

        public static DamageKind Kind(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return DamageKind.Lash;
                case VeilForm.Spear: return DamageKind.Pierce;
                case VeilForm.Shield: return DamageKind.Bash;
                case VeilForm.Bow: return DamageKind.Arrow;
                default: return DamageKind.Slash;
            }
        }

        /// <summary>Normalised attack time window in which the weapon deals damage.</summary>
        public static Vector2 ActiveWindow(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return new Vector2(0.52f, 0.78f); // the lash rolling out to the crack
                case VeilForm.Spear: return new Vector2(0.34f, 0.62f);
                case VeilForm.Shield: return new Vector2(0.24f, 0.55f);
                case VeilForm.Bow: return new Vector2(2f, 2f); // arrows deal the damage
                default: return new Vector2(0.3f, 0.56f);
            }
        }

        public static bool IsActive(VeilForm form, float t)
        {
            var w = ActiveWindow(form);
            return t >= w.x && t <= w.y;
        }

        /// <summary>Closest distance from point p to segment ab.</summary>
        public static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
            return Vector3.Distance(a + ab * t, p);
        }

        /// <summary>Is the attacker in front of the hero (or right on him)?</summary>
        public static bool FromFront(int facing, float heroX, float attackerX)
        {
            float dx = attackerX - heroX;
            return Mathf.Abs(dx) < 0.05f || Mathf.Sign(dx) == facing;
        }

        /// <summary>The shield is a bubble round the whole hero: once formed it blocks hits from either side.</summary>
        public static bool ShieldBlocks(VeilForm form, float hardness, int facing, float heroX, float attackerX)
            => form == VeilForm.Shield && hardness >= 0.7f;
    }
}
