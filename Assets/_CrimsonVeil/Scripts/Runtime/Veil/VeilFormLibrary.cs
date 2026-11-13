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
                case VeilForm.Whip: return 0.75f; // firm enough to show the rolling loop, still supple
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
                case VeilForm.Whip: return 0.1f;
                case VeilForm.Sword: return 0.16f;
                case VeilForm.Spear: return 0.07f; // a slim shaft
                case VeilForm.Shield: return 0.26f;
                case VeilForm.Bow: return 0.1f;
                default: return 0.65f;
            }
        }

        public static float AttackDuration(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.8f;
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
                    // Held a third of the way along, not by its end: the scarf runs down to the butt behind the
                    // hand, and the shaft passes through the fist straight ahead (level: a spear is thrust).
                    pts.RemoveRange(1, 2); // straight from the neck down to the butt
                    pts.Add(Hand + new Vector2(-SpearButt, 0f));
                    weaponStartIndex = pts.Count - 1;
                    pts.Add(Hand);
                    pts.Add(Hand + new Vector2(SpearReach, 0f));
                    break;
                case VeilForm.Shield:
                    AddSpiral(pts, ShieldCenter, 0.62f, 0.2f, 1.5f, 40);
                    break;
                case VeilForm.Bow:
                    AddBow(pts, out _, out _);
                    break;
                default:
                    pts.Clear();
                    pts.Add(Vector2.zero);
                    pts.Add(new Vector2(-0.8f, -2.4f));
                    weaponStartIndex = 0;
                    break;
            }
            return pts;
        }

        static readonly float[] gripFractions = new float[8];

        /// <summary>
        /// Where along the veil (0 neck .. 1 end) the hand grips the weapon. Usually where the weapon starts, but a
        /// spear's shaft starts at the butt behind the hand.
        /// </summary>
        public static float GripFraction(VeilForm form)
        {
            int k = (int)form;
            if (k < 0 || k >= gripFractions.Length) return 0f;
            if (gripFractions[k] > 0f) return gripFractions[k];
            var pts = ControlPoints(form, out int start);
            int grip = pts.IndexOf(Hand);
            if (grip < 0) grip = start;
            float total = 0f, upTo = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                float d = Vector2.Distance(pts[i - 1], pts[i]);
                total += d;
                if (i <= grip) upTo += d;
            }
            gripFractions[k] = total > 1e-6f ? Mathf.Max(1e-4f, upTo / total) : 1e-4f;
            return gripFractions[k];
        }

        /// <summary>Spear shaft behind the hand (the butt) and ahead of it (to the point), in metres.</summary>
        public const float SpearButt = 1.05f, SpearReach = 2.1f;

        /// <summary>
        /// The whip at rest: coiled in the hand. Same ribbon and same length as the extended whip (so the hand
        /// sits at the same fraction and every node has a partner in both shapes), wound into a hanging coil.
        /// </summary>
        public static float BuildWhipCoil(Vector2[] output)
        {
            var extended = ControlPoints(VeilForm.Whip, out int handIndex);
            float lash = 0f;
            for (int i = handIndex + 1; i < extended.Count; i++) lash += Vector2.Distance(extended[i - 1], extended[i]);

            var pts = new List<Vector2> { Vector2.zero, Shoulder, Hand };
            Vector2 center = Hand + WhipCoilOffset;
            float drop = Vector2.Distance(Hand, center) - WhipCoilRadius;
            Vector2 entry = Hand + (center - Hand).normalized * drop;
            pts.Add(entry);
            float left = lash - drop;
            float a0 = Mathf.Atan2(entry.y - center.y, entry.x - center.x);
            const float step = 0.04f;
            float angle = a0;
            Vector2 prev = entry;
            while (left > 1e-4f)
            {
                // Loops of rope: the radius breathes a little from loop to loop.
                float r = WhipCoilRadius * (1f + 0.12f * Mathf.Sin((angle - a0) * 0.5f));
                angle -= step / r;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                float d = Vector2.Distance(prev, next);
                if (d >= left) { next = prev + (next - prev) * (left / d); d = left; }
                pts.Add(next);
                left -= d;
                prev = next;
            }
            return Resample(pts, 2, output);
        }

        /// <summary>Coil of the resting whip: centre relative to the hand, and loop radius (m).</summary>
        public static readonly Vector2 WhipCoilOffset = new Vector2(0.06f, -0.3f);
        public const float WhipCoilRadius = 0.16f;

        /// <summary>
        /// Upper limb of a recurve bow, relative to the grip (x = towards the target, the archer is at -x):
        /// it sweeps back towards the archer and its tip flicks forward again.
        /// </summary>
        static readonly Vector2[] BowLimb =
        {
            new Vector2(-0.02f, 0.16f), new Vector2(-0.07f, 0.32f), new Vector2(-0.14f, 0.47f),
            new Vector2(-0.2f, 0.6f), new Vector2(-0.22f, 0.7f), new Vector2(-0.19f, 0.78f), new Vector2(-0.14f, 0.83f),
        };

        /// <summary>
        /// The bow as one ribbon: neck → hand (grip) → upper limb → the string straight down → lower limb back
        /// into the grip. Returns the point indices where the string starts (upper tip) and ends (lower tip).
        /// </summary>
        static void AddBow(List<Vector2> pts, out int stringStart, out int stringEnd)
        {
            // The grip sits in the fist, a little ahead of the wrist.
            Vector2 grip = Hand + BowGripOffset;
            pts.Add(grip);
            foreach (var p in BowLimb) pts.Add(grip + Bend(p));
            stringStart = pts.Count - 1;
            var tip = Bend(BowLimb[BowLimb.Length - 1]);
            Vector2 top = pts[stringStart], bottom = grip + new Vector2(tip.x, -tip.y);
            const int stringSteps = 10;
            for (int i = 1; i <= stringSteps; i++) pts.Add(Vector2.Lerp(top, bottom, (float)i / stringSteps));
            stringEnd = pts.Count - 1;
            for (int i = BowLimb.Length - 2; i >= 0; i--) { var b = Bend(BowLimb[i]); pts.Add(grip + new Vector2(b.x, -b.y)); }
            pts.Add(grip);
        }

        /// <summary>Size of the bow relative to the limb template (1 = 1.66 m tall).</summary>
        public const float BowScale = 0.78f;
        /// <summary>Extra sweep of the limbs towards the archer, so there is a clear gap between grip and string.</summary>
        public const float BowDepth = 1.6f;
        public static readonly Vector2 BowGripOffset = new Vector2(0.07f, 0f);

        static Vector2 Bend(Vector2 limbPoint) => new Vector2(limbPoint.x * BowDepth, limbPoint.y) * BowScale;

        /// <summary>Gap (m) between the grip and the string at rest (brace height).</summary>
        public static float BowBraceHeight => -Bend(BowLimb[BowLimb.Length - 1]).x;

        /// <summary>How far (m) the bow's limb tips reach back from the grip towards the hero.</summary>
        public static float BowLimbReachBack
        {
            get
            {
                float min = 0f;
                foreach (var p in BowLimb) min = Mathf.Min(min, Bend(p).x + BowGripOffset.x);
                return -min;
            }
        }

        /// <summary>1 on the bow string, 0 on the limbs, with a short smooth blend at the tips.</summary>
        public static float BowStringWeight(float f)
        {
            var r = BowStringRange;
            const float e = 0.01f;
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r.x - e, r.x + e, f))
                 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r.y - e, r.y + e, f)));
        }

        static Vector2 bowString = new Vector2(-1f, -1f);

        /// <summary>Where along the veil (0 neck .. 1 end) the bow string runs, from the upper to the lower tip.</summary>
        public static Vector2 BowStringRange
        {
            get
            {
                if (bowString.x >= 0f) return bowString;
                var pts = new List<Vector2> { Vector2.zero, Shoulder, Hand };
                AddBow(pts, out int s0, out int s1);
                var cum = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                float total = cum[pts.Count - 1];
                bowString = new Vector2(cum[s0] / total, cum[s1] / total);
                return bowString;
            }
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
                case VeilForm.Spear: return 0f;  // a spear stays level: it is aimed, the arm only pushes it
                case VeilForm.Whip: return 1f;   // the handle is the forearm; the rest lags behind (WhipChain)
                case VeilForm.Shield: return 0.35f;
                case VeilForm.Bow: return 0f;    // a bow stays upright
                default: return 0f;
            }
        }

        /// <summary>How much a weapon follows the run-guard tilt while running.</summary>
        public static float CarryFactor(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return 1f;
                case VeilForm.Spear: return 0f;
                case VeilForm.Whip: return 0.5f;
                default: return 0f;
            }
        }

        /// <summary>
        /// How far the bow string is drawn (0..1) at attack progress t: pulled back to the face, held, the arrow
        /// is loosed at 0.3 and the string snaps straight again.
        /// </summary>
        public static float BowDraw(float t)
        {
            if (t < 0f) return 0f;
            if (t < 0.3f) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.22f, t));
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.34f, t));
        }

        /// <summary>How far the coiled whip has unfurled (0 coiled .. 1 out) at attack progress t: during the wind-up.</summary>
        public static float WhipUncoil(float t) => t < 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.4f, t));

        /// <summary>Per-node unfurling: the coil pays out from the hand towards the tip.</summary>
        public static float WhipNodeOpen(float open, float alongLash) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(open * 1.6f - alongLash * 0.6f));

        /// <summary>Attack progress at which the whip's loop reaches the tip (the crack).</summary>
        public const float WhipCrackAt = 0.66f;

        /// <summary>
        /// Whip as a chain that propagates the handle's motion with a delay: segment k of the lash (s = 0 at the
        /// handle, 1 at the tip) points the way the handle pointed <c>s · lag</c> seconds ago. A fast swing that
        /// stops therefore rolls a loop out along the whip, and the tip moves last and fastest — the crack.
        /// Segment lengths and the rest curve come from <paramref name="rest"/>; nodes before the hand are copied.
        /// </summary>
        /// <param name="turnAgo">Handle turn (degrees) the given number of seconds ago.</param>
        public static void WhipChain(Vector2[] rest, int handIndex, System.Func<float, float> turnAgo, float lag, Vector2[] output)
        {
            int last = rest.Length - 1;
            for (int i = 0; i <= handIndex && i <= last; i++) output[i] = rest[i];
            if (handIndex >= last) return;
            for (int i = handIndex + 1; i <= last; i++)
            {
                Vector2 seg = rest[i] - rest[i - 1];
                float len = seg.magnitude;
                float s = (float)(i - handIndex) / (last - handIndex);
                float a = Mathf.Atan2(seg.y, seg.x) + turnAgo(s * lag) * Mathf.Deg2Rad;
                output[i] = output[i - 1] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len;
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
