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
                case VeilForm.Whip: return 0.5f;
                case VeilForm.Sword: return 0.92f;
                case VeilForm.Spear: return 0.95f;
                case VeilForm.Shield: return 0.88f;
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
                case VeilForm.Shield: return 0.2f;
                case VeilForm.Bow: return 0.07f;
                default: return 0.3f;
            }
        }

        public static float AttackDuration(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.58f;
                case VeilForm.Sword: return 0.46f;
                case VeilForm.Spear: return 0.5f;
                case VeilForm.Shield: return 0.45f;
                case VeilForm.Bow: return 0.72f;
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
                    // A gently curved blade: steep at the hilt, flattening towards the tip.
                    Vector2 p = Hand;
                    const int steps = 8;
                    for (int i = 1; i <= steps; i++)
                    {
                        float t = (float)i / steps;
                        float a = Mathf.Lerp(48f, 26f, t) * Mathf.Deg2Rad;
                        p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (1.5f / steps);
                        pts.Add(p);
                    }
                    break;
                }
                case VeilForm.Spear:
                    pts.Add(Hand + new Vector2(2.5f, 0.12f));
                    break;
                case VeilForm.Shield:
                    AddSpiral(pts, ShieldCenter, 0.62f, 0.2f, 1.5f, 40);
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

        /// <summary>Centre of the vortex shield, in front of the hand.</summary>
        public static readonly Vector2 ShieldCenter = Hand + new Vector2(0.38f, 0.32f);

        /// <summary>
        /// The shield is the veil wound into a vortex: from the hand it sweeps out and spirals inward,
        /// a living Kármán eddy held in front of the hero.
        /// </summary>
        static void AddSpiral(List<Vector2> pts, Vector2 center, float outer, float inner, float turns, int samples)
        {
            Vector2 fromHand = pts[pts.Count - 1] - center;
            float a0 = Mathf.Atan2(fromHand.y, fromHand.x);
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                float a = a0 + t * turns * Mathf.PI * 2f;
                float r = Mathf.Lerp(outer, inner, Mathf.SmoothStep(0f, 1f, t));
                if (i < 4) r = Mathf.Lerp(fromHand.magnitude, r, i / 4f); // ease out of the grip
                pts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
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

        /// <summary>
        /// How much the weapon is swung (degrees, + = upwards) at attack progress t — the hero's arm follows it.
        /// Thrusts are expressed as an equivalent arm extension.
        /// </summary>
        public static float SwingAngle(VeilForm form, float t)
        {
            if (t < 0f) return 0f;
            switch (form)
            {
                case VeilForm.Sword: return Curve(t, 0.18f, 55f, 0.55f, -105f);
                case VeilForm.Whip: return Curve(t, 0.25f, 75f, 0.6f, -35f);
                case VeilForm.Spear: return Curve(t, 0.2f, -0.35f, 0.45f, 1.1f) * 22f;
                case VeilForm.Shield: return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 18f;
                case VeilForm.Bow: return Curve(t, 0.25f, -10f, 0.4f, 4f);
                default: return 0f;
            }
        }

        /// <summary>Transforms a local shape point for attack progress <paramref name="t"/> (0..1).</summary>
        /// <param name="armDriven">When the weapon already follows the hero's forearm, the arm animation
        /// performs sword and whip swings, so they are not rotated here a second time.</param>
        public static Vector2 Apply(VeilForm form, Vector2 local, bool isWeaponPart, float t, bool armDriven = false)
        {
            if (!isWeaponPart || t < 0f) return local;
            Vector2 pivot = VeilFormLibrary.Hand;
            switch (form)
            {
                case VeilForm.Sword:
                    return armDriven ? local : Rotate(local, pivot, Curve(t, 0.18f, 55f, 0.55f, -105f));
                case VeilForm.Whip:
                    return armDriven ? local : Rotate(local, pivot, Curve(t, 0.25f, 75f, 0.6f, -35f));
                case VeilForm.Spear:
                    return local + new Vector2(Curve(t, 0.2f, -0.35f, 0.45f, 1.1f), 0f);
                case VeilForm.Shield:
                    // Bash: the vortex surges forward and spins up.
                    return Rotate(local, VeilFormLibrary.ShieldCenter, Curve(t, 0.15f, 25f, 0.6f, -160f))
                        + new Vector2(Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.5f, 0f);
                case VeilForm.Bow:
                    return local + new Vector2(Curve(t, 0.25f, -0.15f, 0.4f, 0.05f), 0f);
                default:
                    return local;
            }
        }

        /// <summary>
        /// Living rest pose: weapons breathe instead of freezing — the sword sways, the spear bobs,
        /// the whip ripples and the shield vortex slowly turns and pulses.
        /// </summary>
        public static Vector2 Idle(VeilForm form, Vector2 local, bool isWeaponPart, float time)
        {
            if (!isWeaponPart) return local;
            Vector2 hand = VeilFormLibrary.Hand;
            float dist = Vector2.Distance(local, hand);
            switch (form)
            {
                case VeilForm.Sword:
                    return Rotate(local, hand, Mathf.Sin(time * 1.3f) * 2.5f);
                case VeilForm.Spear:
                    return local + new Vector2(0f, Mathf.Sin(time * 1.1f - dist * 0.8f) * 0.03f * dist);
                case VeilForm.Whip:
                    return local + new Vector2(0f, Mathf.Sin(time * 3f - dist * 2.2f) * 0.07f * dist);
                case VeilForm.Shield:
                {
                    Vector2 c = VeilFormLibrary.ShieldCenter;
                    Vector2 p = c + (local - c) * (1f + Mathf.Sin(time * 2f) * 0.04f);
                    return Rotate(p, c, Mathf.Sin(time * 0.9f) * 10f);
                }
                default:
                    return local;
            }
        }

        /// <summary>How strongly each weapon turns with the forearm (a bow stays upright, a sword is the arm).</summary>
        public static float ForearmFollow(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return 1f;
                case VeilForm.Spear: return 1f;
                case VeilForm.Whip: return 0.85f;
                case VeilForm.Shield: return 0.35f;
                case VeilForm.Bow: return 0.25f;
                default: return 0f;
            }
        }

        /// <summary>How much a weapon is swung behind the hero while running.</summary>
        public static float CarryFactor(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return 1f;
                case VeilForm.Spear: return 0.85f;
                case VeilForm.Whip: return 0.5f;
                default: return 0f;
            }
        }

        public static Vector2 Rotate(Vector2 p, Vector2 pivot, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            Vector2 d = p - pivot;
            return pivot + new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }
    }
}
