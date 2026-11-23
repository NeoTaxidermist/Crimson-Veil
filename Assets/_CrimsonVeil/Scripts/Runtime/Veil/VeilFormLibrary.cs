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
                case VeilForm.Sword: return 0.96f; // a straight, stiff blade
                case VeilForm.Spear: return 0.95f;
                case VeilForm.Shield: return 0.95f; // a firm ring
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
                case VeilForm.Shield: return 0.22f; // one firm ring
                case VeilForm.Bow: return 0.1f;
                default: return 0.65f;
            }
        }

        public static float AttackDuration(VeilForm form)
        {
            switch (form)
            {
                // (a lash is quick: back over the shoulder, thrown, cracked a third of a second after the press - at
                // 0.8 s, cracking after half a second, it was a slow swing of a rope)
                case VeilForm.Whip: return 0.62f;
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
            // (a weapon's shape never changes: worked out once per form and length, then copied - built anew at every
            // cast, its lists were garbage)
            var key = ((int)form, output.Length);
            if (!Shapes.TryGetValue(key, out var built))
            {
                var pts = new Vector2[output.Length];
                var points = ControlPoints(form, out int weaponStartIndex);
                float hand = Resample(points, weaponStartIndex, pts);
                built = (pts, hand);
                Shapes[key] = built;
            }
            System.Array.Copy(built.points, output, output.Length);
            return built.hand;
        }

        static readonly Dictionary<(int, int), (Vector2[] points, float hand)> Shapes = new Dictionary<(int, int), (Vector2[] points, float hand)>();

        public static List<Vector2> ControlPoints(VeilForm form, out int weaponStartIndex)
        {
            var pts = new List<Vector2> { Vector2.zero, Shoulder, Hand };
            weaponStartIndex = 2;
            switch (form)
            {
                case VeilForm.Whip:
                    // A whip has a handle: its butt just behind the fist, the stock through it and a little ahead,
                    // and the lash running on from the end of the stock.
                    pts[2] = Hand - new Vector2(WhipPommel, 0f);
                    pts.Add(Hand);
                    pts.Add(Hand + new Vector2(WhipHandle, 0f));
                    // (all but straight: thrown out, the lash is a taut line to its tip - drooping 0.4 m it reached the
                    // floor before the target)
                    pts.Add(Hand + new Vector2(1.1f, 0.03f));
                    pts.Add(Hand + new Vector2(2.3f, -0.03f));
                    pts.Add(Hand + new Vector2(3.4f, -0.14f));
                    break;
                case VeilForm.Sword:
                {
                    // A sword with its hilt: the pommel just behind the fist, the grip through it, the crossguard
                    // just in front of it, then the straight blade (SwordLength from the fist to the point).
                    var dir = new Vector2(Mathf.Cos(SwordAngle * Mathf.Deg2Rad), Mathf.Sin(SwordAngle * Mathf.Deg2Rad));
                    pts[2] = Hand - dir * SwordPommel;
                    pts.Add(Hand);
                    pts.Add(Hand + dir * SwordGuard);
                    const int steps = 8;
                    for (int i = 1; i <= steps; i++) pts.Add(Hand + dir * (SwordGuard + (SwordLength - SwordGuard) * i / steps));
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
                    weaponStartIndex = pts.Count; // the weapon is the ring itself, not the reach out to it
                    AddRing(pts, ShieldCenter, ShieldRadius, ShieldTurns, 40);
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
            var extended = ControlPoints(VeilForm.Whip, out _);
            Vector2 stockEnd = Hand + new Vector2(WhipHandle, 0f);
            int lashFrom = extended.IndexOf(stockEnd);
            float lash = 0f;
            for (int i = lashFrom + 1; i < extended.Count; i++) lash += Vector2.Distance(extended[i - 1], extended[i]);

            // Held like a coiled rope: the handle in the fist, a couple of long loops hanging from its end, fanned out a
            // little so each reads on its own, and the last stretch of the lash dangling below.
            var pts = new List<Vector2> { Vector2.zero, Shoulder, Hand - new Vector2(WhipPommel, 0f), Hand, stockEnd };
            float left = lash;
            Vector2 prev = stockEnd;
            for (int loop = 0; loop < WhipCoilLoops && left > 1e-4f; loop++)
            {
                float rx = 0.12f + 0.025f * loop, ry = 0.28f + 0.03f * loop;
                float tilt = (-14f + 16f * loop) * Mathf.Deg2Rad;
                Vector2 down = new Vector2(Mathf.Sin(tilt), -Mathf.Cos(tilt)), side = new Vector2(-down.y, down.x);
                Vector2 centre = stockEnd + down * ry;
                const int steps = 40;
                for (int k = 1; k <= steps && left > 1e-4f; k++)
                {
                    float th = Mathf.PI * 2f * k / steps;
                    Vector2 next = centre - down * (ry * Mathf.Cos(th)) + side * (rx * Mathf.Sin(th));
                    left -= Append(pts, ref prev, next, left);
                }
            }
            // The tail hangs out of the coil.
            if (left > 1e-4f) Append(pts, ref prev, prev + new Vector2(0.05f, -1f).normalized * left, left);
            return Resample(pts, 2, output);
        }

        static float Append(List<Vector2> pts, ref Vector2 prev, Vector2 next, float left)
        {
            float d = Vector2.Distance(prev, next);
            if (d > left) { next = prev + (next - prev) * (left / d); d = left; }
            pts.Add(next);
            prev = next;
            return d;
        }

        /// <summary>The whip's handle: its butt this far (m) behind the fist, its stock ending this far in front of it.</summary>
        public const float WhipPommel = 0.07f, WhipHandle = 0.17f; // a knob just behind the little finger, the stock running on past the thumb

        static float[] whipHandle;

        /// <summary>Where along the veil (0 neck .. 1 end) the whip's handle lies: its butt (where the weapon starts),
        /// the fist and the end of the stock, where the lash begins.</summary>
        public static void WhipHandleSpan(out float butt, out float fist, out float stockEnd)
        {
            if (whipHandle == null)
            {
                var pts = ControlPoints(VeilForm.Whip, out int start);
                int fistIndex = pts.IndexOf(Hand);
                float total = 0f;
                var at = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) { total += Vector2.Distance(pts[i - 1], pts[i]); at[i] = total; }
                whipHandle = new[] { at[start] / total, at[fistIndex] / total, at[fistIndex + 1] / total };
            }
            butt = whipHandle[0]; fist = whipHandle[1]; stockEnd = whipHandle[2];
        }

        /// <summary>
        /// The part of the veil held firmly in the fist from the moment a weapon is cast (so it is in the hand at once
        /// and the rest grows out of it): a sword's hilt, a whip's handle, a bow's grip. None for a spear (its shaft
        /// slides through the fist in a thrust) or the shield (it floats round the body).
        /// </summary>
        public static bool HeldSpan(VeilForm form, out float from, out float to)
        {
            from = to = 0f;
            switch (form)
            {
                case VeilForm.Sword: { SwordHilt(out from, out _, out float guard); to = guard + 0.01f; return true; }
                case VeilForm.Whip: { WhipHandleSpan(out from, out _, out to); return true; }
                case VeilForm.Bow: { float g = GripFraction(VeilForm.Bow); from = g - 0.025f; to = g + 0.025f; return true; }
                default: return false;
            }
        }

        /// <summary>Number of loops the resting whip is coiled into.</summary>
        public const int WhipCoilLoops = 2;

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

        /// <summary>The blade: its angle above the forearm's line (degrees) and length (m).</summary>
        public const float SwordAngle = 36f, SwordLength = 1.5f;
        /// <summary>The hilt: the pommel this far (m) behind the fist, the crossguard this far in front of it.</summary>
        public const float SwordPommel = 0.13f, SwordGuard = 0.06f;

        static float[] swordHilt;

        /// <summary>
        /// Where along the veil (0 neck .. 1 end) the sword's hilt lies: the pommel (where the weapon starts), the fist
        /// (the grip) and the crossguard, where the blade begins.
        /// </summary>
        public static void SwordHilt(out float pommel, out float fist, out float guard)
        {
            if (swordHilt == null)
            {
                var pts = ControlPoints(VeilForm.Sword, out int start);
                int fistIndex = pts.IndexOf(Hand);
                float total = 0f;
                var at = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) { total += Vector2.Distance(pts[i - 1], pts[i]); at[i] = total; }
                swordHilt = new[] { at[start] / total, at[fistIndex] / total, at[fistIndex + 1] / total };
            }
            pommel = swordHilt[0]; fist = swordHilt[1]; guard = swordHilt[2];
        }

        /// <summary>Centre of the shield bubble: the middle of the body (the veil anchor is at the neck).</summary>
        public static readonly Vector2 ShieldCenter = new Vector2(0f, -0.58f);
        /// <summary>Radius of the shield bubble round the hero.</summary>
        public const float ShieldRadius = 1.22f; // clear of the head and the outstretched arm

        /// <summary>How much of the circle the shield's ring covers: almost all of it, leaving a small gap.</summary>
        public const float ShieldTurns = 0.97f;
        /// <summary>How fast the shield's ring circles round the hero (degrees per second, + = anticlockwise).</summary>
        public const float ShieldSpin = 330f;

        /// <summary>
        /// The shield is the veil whirled into one clean ring round the whole hero, open by a small gap: it starts
        /// level with the hand and runs round almost a full circle. (It circles round the body — see
        /// <see cref="VeilWeaponPose.Idle"/>; the band from the neck to the hand melts away.)
        /// </summary>
        static void AddRing(List<Vector2> pts, Vector2 center, float radius, float turns, int samples)
        {
            Vector2 fromHand = pts[pts.Count - 1] - center;
            float a0 = Mathf.Atan2(fromHand.y, fromHand.x);
            for (int i = 0; i <= samples; i++)
            {
                float a = a0 + (float)i / samples * turns * Mathf.PI * 2f;
                pts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }

        /// <summary>0 for the part of the shield at the hand, 1 out on the bubble (which spins round the body).</summary>
        public static float BubbleWeight(Vector2 local)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, Vector2.Distance(local, ShieldCenter) / ShieldRadius));


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
                case VeilForm.Whip: return Curve(t, WhipWoundAt, 120f, WhipThrownAt, -6f);
                case VeilForm.Spear: return Curve(t, 0.2f, -0.35f, 0.45f, 1.1f) * 22f;
                case VeilForm.Shield: return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 18f;
                case VeilForm.Bow: return Curve(t, 0.25f, -10f, 0.4f, 4f);
                default: return 0f;
            }
        }

        /// <summary>
        /// How far the weapon is turned through depth (radians) at attack progress <paramref name="t"/>, about its
        /// hand: + its far end behind the hero, - in front of him. A slash is wound up behind the body, crosses the
        /// picture as it strikes and follows through in front of it; a thrust barely turns; a shield and a bow none.
        /// (The picture - the shape, the reach, what it hits - is the same: only depth changes.)
        /// </summary>
        public static float SweepTilt(VeilForm form, float t)
        {
            if (t < 0f) return 0f;
            switch (form)
            {
                case VeilForm.Sword: return Curve(t, 0.18f, 0.75f, 0.55f, -0.65f);
                case VeilForm.Whip: return Curve(t, WhipWoundAt, 0.55f, WhipThrownAt, -0.3f);
                case VeilForm.Spear: return Curve(t, 0.2f, 0.18f, 0.45f, -0.12f);
                default: return 0f;
            }
        }

        /// <summary>Which way through depth (-1 towards the camera .. 1 away) the weapon is sweeping at
        /// <paramref name="t"/>: the spray of a hit flies on with the blow.</summary>
        public static float SweepDirection(VeilForm form, float t)
        {
            if (t < 0f) return 0f;
            const float e = 0.03f;
            float rate = (SweepTilt(form, Mathf.Min(1f, t + e)) - SweepTilt(form, Mathf.Max(0f, t - e))) / (2f * e);
            return Mathf.Clamp(rate * 0.25f, -1f, 1f);
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
                    return armDriven ? local : Rotate(local, pivot, Curve(t, WhipWoundAt, 120f, WhipThrownAt, -6f));
                case VeilForm.Spear:
                    return local + new Vector2(Curve(t, 0.2f, -0.35f, 0.45f, 1.1f), 0f);
                case VeilForm.Shield:
                {
                    // Bash: the ring bursts outwards all round the hero and whirls on faster.
                    Vector2 c = VeilFormLibrary.ShieldCenter;
                    float swell = 1f + 0.4f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    return Rotate(c + (local - c) * swell, c, Curve(t, 0.15f, 20f, 0.6f, 140f));
                }
                case VeilForm.Bow:
                    return local + new Vector2(Curve(t, 0.25f, -0.15f, 0.4f, 0.05f), 0f);
                default:
                    return local;
            }
        }

        /// <summary>
        /// Living rest pose: weapons breathe instead of freezing — the sword sways, the spear bobs,
        /// the whip ripples and the shield's open ring circles round the hero.
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
                    // The open ring circles round the hero, gap and all, and breathes a little.
                    Vector2 c = VeilFormLibrary.ShieldCenter;
                    Vector2 p = c + (local - c) * (1f + 0.025f * Mathf.Sin(time * 2.2f));
                    return Rotate(p, c, Mathf.Repeat(time * VeilFormLibrary.ShieldSpin, 360f));
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
                case VeilForm.Shield: return 0f; // the bubble surrounds the body, it does not swing with the arm
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
        /// (Early in it: the lash runs out behind him as the hand goes back. Paid out through the whole wind-up, the
        /// coil hung at the fist as a knot of loops for the first quarter of a second.)
        public static float WhipUncoil(float t) => t < 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.01f, 0.16f, t));

        /// <summary>Per-node unfurling: the coil pays out from the hand towards the tip.</summary>
        public static float WhipNodeOpen(float open, float alongLash) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(open * 1.6f - alongLash * 0.6f));

        /// <summary>Attack progress at which the hand is back over the shoulder (the lash laid out behind him), at which
        /// it has driven forward and stopped at arm's length, and until which the thrown lash is held taut - then it
        /// goes slack and is let fall.</summary>
        public const float WhipWoundAt = 0.24f, WhipThrownAt = 0.36f, WhipHeldTo = 0.66f;

        /// <summary>Seconds the whip's tip lags behind its handle - the loop that rolls down it.</summary>
        public const float WhipLag = 0.11f;

        /// <summary>Attack progress at which the whip's loop reaches the tip (the crack): the stop of the hand and the
        /// loop's run down the lash after it.</summary>
        public const float WhipCrackAt = 0.54f;

        /// <summary>The way the lash is thrown from behind him, in degrees off the blow's aim: up and back for a level
        /// lash, down behind the hip for one aimed up.</summary>
        public const float WhipBack = 150f;

        /// <summary>
        /// The turn (degrees) the lash takes up at attack progress <paramref name="t"/>: the forearm's
        /// (<paramref name="forearmTurn"/>) - but as the blow begins it lies out behind him (<see cref="WhipBack"/> off
        /// the <paramref name="aim"/>) and only joins the arm as the hand arrives back there. The scarf is behind him:
        /// the lash that forms from it is thrown from there. (Following the arm from the first moment it flopped
        /// out in front of him, then was hauled back over his head for the wind-up.)
        /// </summary>
        public static float WhipLashTurn(float t, float forearmTurn, float aim)
        {
            if (t < 0f || t >= WhipWoundAt) return forearmTurn;
            return Mathf.LerpAngle(aim + WhipBack, forearmTurn, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f * WhipWoundAt, WhipWoundAt, t)));
        }

        /// <summary>Whether the whole whip follows its handle exactly at attack progress <paramref name="t"/>: from the
        /// throw until the lash has cracked and been held (a loop of 40 m/s at the tip is not something the cloth's
        /// springs can follow: left to them it was smeared into a slow arc). Before and after, it is supple.</summary>
        public static bool WhipDriven(float t) => t >= WhipWoundAt * 0.5f && t <= WhipHeldTo;

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

        /// <summary>Degrees the slack lash bends from one piece to the next as it falls from the stock: a rope's curve,
        /// not a corner.</summary>
        public const float WhipSlackBend = 16f;
        /// <summary>Seconds the lash takes to fall slack once it has cracked, and to be taken up again by the next blow.</summary>
        public const float WhipSlackTime = 0.4f, WhipTakeUpTime = 0.07f;
        /// <summary>How high (m) over the floor the lash lies on it, and from how high over it the falling lash starts
        /// to turn along it (it is laid down along the floor: no corner where it lands).</summary>
        public const float WhipOnFloor = 0.03f, WhipLandFrom = 0.7f;
        /// <summary>Degrees under the level the lash still runs as it reaches the floor: it comes down onto it. (Turned
        /// right along the floor it only ever neared it, and hung a few centimetres over it all its length.)</summary>
        public const float WhipLandAngle = 8f;

        /// <summary>
        /// The way (degrees) the slack lash wants to run at <paramref name="height"/> m over the floor, heading
        /// <paramref name="heading"/> (+1 ahead, -1 back): straight down while it is high, turning along the floor
        /// as it comes down to it - still a little down (<see cref="WhipLandAngle"/>), so it lands.
        /// </summary>
        public static float WhipHang(float height, float heading)
        {
            float land = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(height / WhipLandFrom));
            return heading >= 0f ? Mathf.Lerp(-90f, -WhipLandAngle, land) : Mathf.Lerp(-90f, -180f + WhipLandAngle, land);
        }

        /// <summary>Whether the lash hangs slack at attack progress <paramref name="t"/>: in the hand between blows, and
        /// once the thrown lash has cracked and been held (<see cref="WhipHeldTo"/>).</summary>
        public static bool WhipFalls(float t) => t < 0f || t > WhipHeldTo;

        /// <summary>How slack (0 taut .. 1 hanging) the piece of the lash at <paramref name="along"/> (0 the stock .. 1
        /// the tip) is when the whip as a whole is <paramref name="slack"/>: the tip, which nothing holds, goes first.</summary>
        public static float WhipSlackAt(float slack, float along)
            => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(slack * 1.3f - (1f - along) * 0.3f));

        /// <summary>
        /// The lash let go: it falls from the stock in a rope's curve - each piece bent a little further down than the
        /// one before, until it hangs straight - reaches the floor and lies along it, the way it was going. Blended in
        /// by <paramref name="slack"/> from the thrown lash <paramref name="lash"/>, by the pieces' ANGLES (not their
        /// places): the lash keeps its length and has no corner at any stage of the fall. (Left to the cloth's springs
        /// it stood out level in the air after the crack, and the scarf's own shape, pulling its soft end back behind
        /// his neck, crumpled it into ripples: up to 19 bends in 30 nodes.)
        /// </summary>
        /// <param name="floorAt">The floor's height under a point of the shape (null: none); a negative infinity where
        /// there is none.</param>
        /// <param name="blocked">Whether a point of the shape is inside a wall (null: never): there the lash doubles
        /// back along the floor instead of running into the stone.</param>
        public static void WhipSlack(Vector2[] lash, int handIndex, float slack, System.Func<Vector2, float> floorAt,
            System.Func<Vector2, bool> blocked, Vector2[] output)
        {
            int last = lash.Length - 1;
            for (int i = 0; i <= handIndex && i <= last; i++) output[i] = lash[i];
            if (handIndex >= last) return;
            Vector2 stock = handIndex > 0 ? lash[handIndex] - lash[handIndex - 1] : lash[handIndex + 1] - lash[handIndex];
            float hang = Mathf.Atan2(stock.y, stock.x) * Mathf.Rad2Deg;
            float heading = stock.x >= 0f ? 1f : -1f;
            bool turned = false, atWall = false;   // (sent back by a wall: from there on it runs the other way)
            float onFloor = Mathf.SmoothStep(0f, 1f, slack / 0.25f);
            for (int i = handIndex + 1; i <= last; i++)
            {
                Vector2 seg = lash[i] - lash[i - 1];
                float len = seg.magnitude;
                float own = Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg;
                float along = (float)(i - handIndex) / (last - handIndex);
                Vector2 from = output[i - 1];
                float over = floorAt != null ? from.y - (floorAt(from) + WhipOnFloor) : float.PositiveInfinity;
                hang = Mathf.MoveTowardsAngle(hang, WhipHang(over, heading), WhipSlackBend);
                float angle = Mathf.LerpAngle(own, hang, WhipSlackAt(slack, along)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (turned) dir.x = heading * Mathf.Abs(dir.x);
                else if (Mathf.Abs(dir.x) > 0.2f) heading = Mathf.Sign(dir.x);
                Vector2 p = from + dir * len;
                if (onFloor > 0f)
                {
                    if (blocked != null && blocked(p))
                    {
                        // a wall: it cannot go on - it drops down the wall's face and runs back from its foot (eased in
                        // with the fall: thrown, it went through)
                        if (!atWall) { heading = -heading; turned = true; }
                        atWall = true;
                        p = Vector2.Lerp(p, from + Vector2.down * len, onFloor);
                        hang = -90f;
                    }
                    else atWall = false;
                    float floor = floorAt != null ? floorAt(p) + WhipOnFloor : float.NegativeInfinity;
                    if (p.y < floor)
                    {
                        // laid on the floor (or up a low step onto it), as far along it as the piece is long
                        float rise = Mathf.Clamp(floor - from.y, -len, len);
                        float run = Mathf.Sqrt(Mathf.Max(0f, len * len - rise * rise));
                        var laid = new Vector2(from.x + heading * run, from.y + rise);
                        p = Vector2.Lerp(p, laid, onFloor);
                        hang = heading > 0f ? 0f : 180f;   // (and on along the floor)
                    }
                }
                output[i] = p;
            }
        }

        /// <summary>The grapple ring's iron: the middle of its rim, from the ring's centre (m).</summary>
        public const float RingRim = 0.34f;
        /// <summary>The whip wound round the ring's iron: the loop's radius (m), and how far round it goes (turns).</summary>
        public const float RingWind = 0.09f, RingTurns = 0.85f;
        /// <summary>The lash on a ring: up to this share of it runs from the hand to the ring, up to the second it is
        /// wound round the iron; the rest is its end, hanging from the knot.</summary>
        public const float RopeTaut = 0.78f, RopeWound = 0.94f;
        /// <summary>How far (m) the whip's end hangs down from the knot.</summary>
        public const float RopeEnd = 0.24f;

        /// <summary>
        /// The whip lashed round a ring, at <paramref name="along"/> of its lash (0 the stock .. 1 the tip): taut from
        /// the hand to the ring's iron on his side (he hangs on it: barely a sag), wound round the iron, its end hanging
        /// from the knot (swayed <paramref name="sway"/> m by his swing).
        /// </summary>
        public static Vector2 WhipOnRing(Vector2 grip, Vector2 ring, float along, float sway)
        {
            Vector2 to = ring - grip;
            float dist = to.magnitude;
            Vector2 dir = dist > 1e-4f ? to / dist : Vector2.up;
            var side = new Vector2(-dir.y, dir.x);
            Vector2 iron = ring - dir * Mathf.Min(RingRim, 0.5f * dist);
            float turns = RingTurns * 2f * Mathf.PI;
            if (along <= RopeTaut)
            {
                float u = along / RopeTaut;
                // (it meets the loop along the loop's own line: no corner where it goes round the iron)
                Vector2 sag = side.y > 0f ? -side : side;   // (the way it sags: down)
                return Vector2.Lerp(grip, iron + side * RingWind, u) + sag * (Mathf.Sin(u * Mathf.PI) * 0.03f);
            }
            if (along <= RopeWound)
            {
                float w = (along - RopeTaut) / (RopeWound - RopeTaut) * turns;
                return iron + (side * Mathf.Cos(w) + dir * Mathf.Sin(w)) * RingWind;
            }
            Vector2 knot = iron + (side * Mathf.Cos(turns) + dir * Mathf.Sin(turns)) * RingWind;
            float v = (along - RopeWound) / (1f - RopeWound);
            return knot + new Vector2(sway * v * v, -RopeEnd * v);
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
