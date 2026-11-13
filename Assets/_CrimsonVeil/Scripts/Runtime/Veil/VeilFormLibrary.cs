using System.Collections.Generic;
using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    public enum VeilForm
    {
        Flow = 0,
        Whip = 1,
        Sword = 2,
        Spear = 3,
        Shield = 4,
        Bow = 5,
    }

    /// <summary>
    /// Target shapes of the veil's weapon forms, in local 2D space relative to the neck anchor
    /// (x = facing direction, y = up). The ribbon always starts at the neck, wraps to the hand
    /// and then becomes the weapon — it is never a separate object.
    /// </summary>
    public static class VeilFormLibrary
    {
        public static readonly Vector2 Shoulder = new Vector2(0.2f, -0.22f);
        public static readonly Vector2 Hand = new Vector2(0.42f, -0.38f);

        public static float Hardness(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.55f;
                case VeilForm.Sword: return 0.95f;
                case VeilForm.Spear: return 0.97f;
                case VeilForm.Shield: return 0.95f;
                case VeilForm.Bow: return 0.9f;
                default: return 0f;
            }
        }

        public static float Width(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.07f;
                case VeilForm.Sword: return 0.16f;
                case VeilForm.Spear: return 0.08f;
                case VeilForm.Shield: return 0.5f;
                case VeilForm.Bow: return 0.07f;
                default: return 0.3f;
            }
        }

        public static float AttackDuration(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.42f;
                case VeilForm.Sword: return 0.3f;
                case VeilForm.Spear: return 0.34f;
                case VeilForm.Shield: return 0.3f;
                case VeilForm.Bow: return 0.4f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Fills <paramref name="output"/> with the shape resampled to evenly spaced points.
        /// Returns the fraction (0..1) of the ribbon length where the weapon part starts.
        /// </summary>
        public static float BuildShape(VeilForm form, Vector2[] output)
        {
            var points = ControlPoints(form, out int weaponStartIndex);
            return Resample(points, weaponStartIndex, output);
        }

        public static List<Vector2> ControlPoints(VeilForm form, out int weaponStartIndex)
        {
            var pts = new List<Vector2> { Vector2.zero, Shoulder, Hand };
            weaponStartIndex = 2;
            switch (form)
            {
                case VeilForm.Whip:
                    pts.Add(Hand + new Vector2(1.1f, 0.05f));
                    pts.Add(Hand + new Vector2(2.3f, -0.1f));
                    pts.Add(Hand + new Vector2(3.4f, -0.4f));
                    break;
                case VeilForm.Sword:
                {
                    float a = 35f * Mathf.Deg2Rad;
                    pts.Add(Hand + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.45f);
                    break;
                }
                case VeilForm.Spear:
                    pts.Add(Hand + new Vector2(2.5f, 0.12f));
                    break;
                case VeilForm.Shield:
                    AddArc(pts, Hand + new Vector2(0.25f, 0.25f), 0.75f, -75f, 75f, 10, 0.35f);
                    break;
                case VeilForm.Bow:
                {
                    // Neck → top limb tip, then arc down through the grip (hand) to the bottom tip.
                    var center = Hand + new Vector2(-0.85f, 0f);
                    pts.Clear();
                    pts.Add(Vector2.zero);
                    AddArc(pts, center, 0.85f, 60f, -60f, 12, 1f);
                    weaponStartIndex = 1;
                    break;
                }
                default:
                    pts.Clear();
                    pts.Add(Vector2.zero);
                    pts.Add(new Vector2(-0.8f, -2.4f));
                    weaponStartIndex = 0;
                    break;
            }
            return pts;
        }

        static void AddArc(List<Vector2> pts, Vector2 center, float radius, float fromDeg, float toDeg, int segments, float squashX)
        {
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, (float)i / segments) * Mathf.Deg2Rad;
                pts.Add(center + new Vector2(Mathf.Cos(a) * radius * squashX, Mathf.Sin(a) * radius));
            }
        }

        public static float Resample(IReadOnlyList<Vector2> points, int markerIndex, Vector2[] output)
        {
            int n = output.Length;
            var cumulative = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
                cumulative[i] = cumulative[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            float total = cumulative[points.Count - 1];

            int seg = 0;
            for (int i = 0; i < n; i++)
            {
                float s = total * i / (n - 1);
                while (seg < points.Count - 2 && cumulative[seg + 1] < s) seg++;
                float segLen = cumulative[seg + 1] - cumulative[seg];
                float t = segLen > 1e-6f ? (s - cumulative[seg]) / segLen : 0f;
                output[i] = Vector2.Lerp(points[seg], points[seg + 1], Mathf.Clamp01(t));
            }

            return total > 1e-6f ? cumulative[Mathf.Clamp(markerIndex, 0, points.Count - 1)] / total : 0f;
        }
    }

    /// <summary>Procedural attack motion applied on top of a form's rest shape.</summary>
    public static class VeilWeaponPose
    {
        /// <summary>
        /// Three-key curve: 0 → v1 (by t1, smooth) → v2 (by t2, ease-out snap) → 0 (by 1, smooth).
        /// </summary>
        public static float Curve(float t, float t1, float v1, float t2, float v2)
        {
            t = Mathf.Clamp01(t);
            if (t < t1) return Mathf.Lerp(0f, v1, MovementMath.EaseInOutSine(t / t1));
            if (t < t2) return Mathf.Lerp(v1, v2, MovementMath.EaseOutCubic((t - t1) / (t2 - t1)));
            return Mathf.Lerp(v2, 0f, MovementMath.EaseInOutSine((t - t2) / (1f - t2)));
        }

        /// <summary>Transforms a local shape point for attack progress <paramref name="t"/> (0..1).</summary>
        public static Vector2 Apply(VeilForm form, Vector2 local, bool isWeaponPart, float t)
        {
            if (!isWeaponPart || t < 0f) return local;
            Vector2 pivot = VeilFormLibrary.Hand;
            switch (form)
            {
                case VeilForm.Sword:
                    return Rotate(local, pivot, Curve(t, 0.18f, 55f, 0.55f, -105f));
                case VeilForm.Whip:
                    return Rotate(local, pivot, Curve(t, 0.25f, 75f, 0.6f, -35f));
                case VeilForm.Spear:
                    return local + new Vector2(Curve(t, 0.2f, -0.35f, 0.45f, 1.1f), 0f);
                case VeilForm.Shield:
                    return local + new Vector2(Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.5f, 0f);
                case VeilForm.Bow:
                    return local + new Vector2(Curve(t, 0.25f, -0.15f, 0.4f, 0.05f), 0f);
                default:
                    return local;
            }
        }

        static Vector2 Rotate(Vector2 p, Vector2 pivot, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            Vector2 d = p - pivot;
            return pivot + new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }
    }
}
