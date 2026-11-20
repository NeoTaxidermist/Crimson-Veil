using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Full-body attack choreography per veil weapon, as key poses over normalised attack time.
    /// The weapon follows the sword-arm's forearm, so these poses *are* the weapon's motion:
    /// <list type="bullet">
    /// <item>Sword – blade raised back over the shoulder, diagonal cut with a lunge, follow-through.</item>
    /// <item>Spear – drawn back to the chest, full-extension thrust with a deep lunge, retract.</item>
    /// <item>Whip – overhead wind-up, cracking downward lash, follow-through.</item>
    /// <item>Shield – crouch and brace, forward bash with the shoulder behind it.</item>
    /// <item>Bow – bow arm extended, the other hand draws the string to the chest, release and recoil.</item>
    /// </list>
    /// </summary>
    public static class HeroAttackChoreography
    {
        public struct Key
        {
            public float t, armR, elbowR, armL, elbowL, spine, chest, head, thighL, kneeL, thighR, kneeR;

            public Key(float t, float armR, float elbowR, float armL, float elbowL, float spine, float chest, float head,
                       float thighL, float kneeL, float thighR, float kneeR)
            {
                this.t = t; this.armR = armR; this.elbowR = elbowR; this.armL = armL; this.elbowL = elbowL;
                this.spine = spine; this.chest = chest; this.head = head;
                this.thighL = thighL; this.kneeL = kneeL; this.thighR = thighR; this.kneeR = kneeR;
            }
        }

        const float H = HeroPoseSolver.HoldArm;
        static readonly Key Hold = new Key(0f, H, 18f, 0f, 20f, 4f, 0f, 0f, 5f, 10f, -2f, 10f);

        static Key K(float t, float armR, float elbowR, float armL, float elbowL, float spine, float chest, float head,
                     float thighL, float kneeL, float thighR, float kneeR) =>
            new Key(t, armR, elbowR, armL, elbowL, spine, chest, head, thighL, kneeL, thighR, kneeR);

        static Key At(Key k, float t) { k.t = t; return k; }

        // The sword hand never leaves the front of the body: raised high beside the head (not behind it, where the
        // silhouette would swallow fist and hilt), one continuous diagonal cut down to the front, a low
        // follow-through still in front. The free arm stays tucked at the chest.
        static readonly Key[] Sword =
        {
            At(Hold, 0f),
            K(0.26f, 128f, 38f, 20f, 95f, -8f, -12f, 6f, 10f, 20f, -8f, 25f),   // wind-up: blade raised high, fist beside the head
            K(0.50f, 40f, 12f, -15f, 70f, 18f, 14f, -8f, 42f, 52f, -28f, 18f),  // one diagonal cut, lunge
            K(0.66f, 14f, 22f, -18f, 65f, 22f, 16f, -8f, 45f, 55f, -30f, 20f),  // follow-through low in front
            K(0.82f, 22f, 20f, -8f, 40f, 10f, 6f, -4f, 25f, 35f, -15f, 15f),
            At(Hold, 1f),
        };

        static readonly Key[] Spear =
        {
            At(Hold, 0f),
            K(0.30f, -30f, 110f, 40f, 60f, -6f, -8f, 4f, 10f, 25f, -10f, 30f),  // drawn back to the chest
            K(0.45f, 82f, 0f, -40f, 10f, 26f, 8f, -8f, 50f, 45f, -35f, 10f),    // full thrust, lunge
            K(0.65f, 78f, 5f, -30f, 15f, 22f, 6f, -6f, 45f, 45f, -30f, 12f),    // hold the reach
            At(Hold, 1f),
        };

        // A real crack: the hand goes back over the shoulder (the whip lies out behind), then drives forward
        // and *stops* at arm's length. The stop sends a loop rolling down the whip; it cracks at the tip a
        // moment later (the delay along the whip is done by the veil, see VeilWeaponPose.WhipChain).
        static readonly Key[] Whip =
        {
            At(Hold, 0f),
            K(0.38f, 165f, 70f, 25f, 30f, -14f, -12f, 8f, 6f, 15f, -6f, 20f),   // slow wind-up: hand back behind the head
            K(0.50f, 100f, 8f, -25f, 20f, 18f, 14f, -6f, 30f, 38f, -20f, 15f),  // fast drive forward, stop at arm's length
            K(0.70f, 90f, 10f, -25f, 20f, 16f, 12f, -6f, 30f, 38f, -20f, 15f),  // hold while the loop rolls out and cracks
            K(0.86f, 55f, 18f, -10f, 20f, 8f, 6f, -2f, 18f, 25f, -10f, 15f),    // let the whip fall
            At(Hold, 1f),
        };

        static readonly Key[] Shield =
        {
            At(Hold, 0f),
            K(0.20f, 70f, 70f, 10f, 40f, 2f, -4f, 4f, 20f, 45f, -5f, 45f),      // crouch and brace
            K(0.40f, 85f, 5f, -20f, 20f, 24f, 10f, -6f, 45f, 40f, -30f, 15f),   // bash with the shoulder
            K(0.60f, 80f, 15f, -10f, 20f, 18f, 8f, -4f, 35f, 35f, -20f, 15f),
            At(Hold, 1f),
        };

        static readonly Key[] Bow =
        {
            At(Hold, 0f),
            K(0.12f, 88f, 2f, 80f, 20f, 0f, -4f, 0f, 12f, 15f, -8f, 15f),       // bow up, hand to the string
            K(0.30f, 88f, 0f, 75f, 135f, -3f, -8f, 4f, 15f, 18f, -10f, 18f),    // full draw (release here)
            K(0.40f, 92f, 0f, -15f, 40f, -6f, -10f, 6f, 15f, 18f, -10f, 18f),   // release, hand flies back
            K(0.70f, 70f, 10f, 0f, 20f, 0f, -4f, 2f, 8f, 12f, -4f, 12f),
            At(Hold, 1f),
        };

        // ---- Aimed blows: their own poses, not the level blow turned round. A diagonal blends the level blow with
        // these. (Keys: t, armR, elbowR, armL, elbowL, spine, chest, head, thighL, kneeL, thighR, kneeR; the spine and
        // chest lean forward with +, the head looks up with +.)

        // A rising cut against things above: the blade swept low past the hip, then up in front of the body to
        // over the head; the body rises and leans back into it, eyes up.
        static readonly Key[] SwordUp =
        {
            At(Hold, 0f),
            K(0.24f, -25f, 35f, 25f, 95f, 8f, 10f, -2f, 18f, 35f, -10f, 35f),   // blade down past the hip, a little crouch
            // (the blade sits ~64° up off the forearm, as a sword is gripped: a forearm raised forward-up to ~120°
            // holds it straight up; an arm straight up would lay it back over the head)
            K(0.48f, 112f, 6f, -10f, 75f, -8f, -12f, 16f, 22f, 15f, -12f, 8f),   // rising cut, up on the toes, lean back
            K(0.66f, 118f, 2f, -12f, 70f, -10f, -14f, 18f, 20f, 15f, -10f, 10f),  // follow-through: the blade straight up
            K(0.82f, 70f, 25f, -5f, 45f, 0f, -2f, 4f, 10f, 15f, -5f, 12f),
            At(Hold, 1f),
        };

        // In the air, under the feet: the sword raised, the knees drawn up, then driven straight down past the
        // tucked legs with the point down; held there for the bounce.
        static readonly Key[] SwordDown =
        {
            At(Hold, 0f),
            K(0.24f, 125f, 45f, 35f, 95f, -6f, -6f, 4f, 60f, 85f, 45f, 85f),    // raised, knees drawn up
            K(0.46f, 12f, 2f, -20f, 70f, 22f, 18f, -16f, 65f, 95f, 50f, 95f),   // driven down, point under the feet
            K(0.70f, 6f, 2f, -20f, 70f, 20f, 16f, -14f, 60f, 90f, 45f, 90f),    // held for the bounce
            K(0.86f, 30f, 15f, -5f, 40f, 8f, 6f, -4f, 25f, 40f, 10f, 30f),
            At(Hold, 1f),
        };

        // Thrust overhead: drawn down to the chest, then driven straight up with the arm, the body stretched up.
        static readonly Key[] SpearUp =
        {
            At(Hold, 0f),
            K(0.30f, -30f, 110f, 40f, 60f, 4f, 6f, -2f, 12f, 30f, -10f, 30f),   // drawn down to the chest, crouched
            K(0.45f, 172f, 0f, -30f, 20f, -10f, -10f, 16f, 15f, 10f, -12f, 6f),  // driven straight up, stretched
            K(0.65f, 168f, 5f, -25f, 20f, -8f, -8f, 14f, 15f, 10f, -10f, 8f),   // hold the reach
            At(Hold, 1f),
        };

        // Thrust under the feet from the air: lifted to the chest, then rammed straight down, knees drawn up.
        static readonly Key[] SpearDown =
        {
            At(Hold, 0f),
            K(0.30f, 140f, 80f, 40f, 90f, -4f, -4f, 4f, 55f, 80f, 40f, 80f),    // lifted high, knees drawn up
            K(0.45f, 8f, 0f, -15f, 60f, 20f, 16f, -16f, 60f, 90f, 45f, 90f),    // rammed down
            K(0.65f, 6f, 0f, -15f, 60f, 18f, 14f, -14f, 55f, 85f, 40f, 85f),
            At(Hold, 1f),
        };

        // Lashing upwards: the hand drawn back low behind the hip, then driven up and stopped over the head.
        static readonly Key[] WhipUp =
        {
            At(Hold, 0f),
            K(0.38f, -25f, 60f, 25f, 30f, 8f, 10f, -4f, 12f, 30f, -8f, 30f),    // hand back low behind the hip
            K(0.50f, 170f, 6f, -20f, 20f, -10f, -12f, 16f, 20f, 15f, -12f, 10f), // driven up, stopped over the head
            K(0.70f, 165f, 8f, -20f, 20f, -8f, -10f, 14f, 20f, 15f, -12f, 10f),  // hold while it cracks
            K(0.86f, 90f, 18f, -10f, 20f, 0f, -2f, 4f, 12f, 20f, -6f, 15f),
            At(Hold, 1f),
        };

        // Lashing downwards: the usual wind-up over the shoulder, then driven down and stopped low in front.
        static readonly Key[] WhipDown =
        {
            At(Hold, 0f),
            K(0.38f, 165f, 70f, 25f, 30f, -14f, -12f, 8f, 6f, 15f, -6f, 20f),
            K(0.50f, 25f, 6f, -25f, 20f, 24f, 18f, -16f, 35f, 45f, -20f, 20f),  // driven down, stopped low in front
            K(0.70f, 20f, 8f, -25f, 20f, 22f, 16f, -14f, 35f, 45f, -20f, 20f),
            K(0.86f, 30f, 18f, -10f, 20f, 8f, 6f, -4f, 18f, 25f, -10f, 15f),
            At(Hold, 1f),
        };

        // ---- Low blows, struck from the crouch: the legs stay the crouch's (the keys' leg values are only the crouch's
        // for reference), the trunk bent over the knees, and the blow sweeps low over the floor. The arm rides on the
        // bent trunk, and the trunk bent forward carries the arm back and down with it: a level arm takes the lean
        // (spine + chest) on top of its 90 degrees.
        static readonly Key LowHold = new Key(0f, H, 18f, 32f, 55f, 30f, 10f, 22f, 92f, 122f, 80f, 128f);

        // A low cut at the ankles: the blade raised only so far that it stays under a tunnel's roof (the forearm
        // level, the blade ~20 degrees up), then chopped down level just over the floor, the body leaning into it.
        static readonly Key[] SwordLow =
        {
            At(LowHold, 0f),
            K(0.26f, 30f, 46f, 40f, 70f, 26f, 6f, 22f, 92f, 122f, 80f, 128f),    // raised low in front, under the roof
            K(0.50f, 50f, 16f, 10f, 60f, 40f, 12f, 14f, 92f, 122f, 80f, 128f),      // chopped down level over the floor
            K(0.66f, 62f, 18f, 5f, 60f, 42f, 12f, 14f, 92f, 122f, 80f, 128f),     // follow-through low in front
            K(0.82f, 40f, 25f, 25f, 55f, 34f, 10f, 18f, 92f, 122f, 80f, 128f),
            At(LowHold, 1f),
        };

        // A lunge from the crouch: drawn back to the hip, then driven out level just over the floor.
        static readonly Key[] SpearLow =
        {
            At(LowHold, 0f),
            K(0.30f, -10f, 100f, 30f, 60f, 26f, 6f, 20f, 92f, 122f, 80f, 128f),  // drawn back to the hip
            K(0.45f, 48f, 0f, 20f, 10f, 40f, 10f, 16f, 92f, 122f, 80f, 128f),   // driven out low
            K(0.65f, 46f, 5f, 20f, 15f, 38f, 10f, 16f, 92f, 122f, 80f, 128f),   // hold the reach
            At(LowHold, 1f),
        };

        // A lash along the floor: the hand drawn back low behind the hip, driven forward at shoulder height and
        // stopped there, so the loop rolls out ahead and the lash comes down along the ground.
        static readonly Key[] WhipLow =
        {
            At(LowHold, 0f),
            K(0.38f, -45f, 40f, 35f, 40f, 26f, 4f, 20f, 92f, 122f, 80f, 128f),   // hand back low behind the hip
            K(0.50f, 95f, 2f, -10f, 30f, 40f, 12f, 14f, 92f, 122f, 80f, 128f),   // driven forward low, stopped
            K(0.70f, 92f, 4f, -10f, 30f, 40f, 12f, 14f, 92f, 122f, 80f, 128f),   // hold while it cracks
            K(0.86f, 60f, 20f, 15f, 45f, 34f, 10f, 18f, 92f, 122f, 80f, 128f),
            At(LowHold, 1f),
        };

        // Crouched behind the shield: braced low, a short shove forward with the shoulder.
        static readonly Key[] ShieldLow =
        {
            At(LowHold, 0f),
            K(0.20f, 50f, 70f, 20f, 50f, 34f, 8f, 20f, 92f, 122f, 80f, 128f),    // braced low
            K(0.40f, 70f, 5f, -10f, 20f, 44f, 12f, 14f, 92f, 122f, 80f, 128f),   // shove with the shoulder
            K(0.60f, 65f, 15f, 0f, 20f, 40f, 10f, 16f, 92f, 122f, 80f, 128f),
            At(LowHold, 1f),
        };

        // A shot from the crouch: the trunk stays bent low over the knees (under a tunnel's roof), the bow level.
        static readonly Key[] BowLow =
        {
            At(LowHold, 0f),
            K(0.12f, 122f, 2f, 110f, 20f, 30f, 6f, 12f, 92f, 122f, 80f, 128f),     // bow up, hand to the string
            K(0.30f, 122f, 0f, 106f, 135f, 30f, 6f, 12f, 92f, 122f, 80f, 128f),    // full draw (release here)
            K(0.40f, 124f, 0f, 16f, 40f, 30f, 6f, 12f, 92f, 122f, 80f, 128f),     // release, hand flies back
            K(0.70f, 112f, 10f, 40f, 30f, 32f, 8f, 16f, 92f, 122f, 80f, 128f),
            At(LowHold, 1f),
        };

        public static Key[] LowFor(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return SwordLow;
                case VeilForm.Spear: return SpearLow;
                case VeilForm.Whip: return WhipLow;
                case VeilForm.Shield: return ShieldLow;
                case VeilForm.Bow: return BowLow;
                default: return null;
            }
        }

        /// <summary>The low blow's key pose at time t (the bow raised into its aim, as standing).</summary>
        public static Key SampleLow(VeilForm form, float t, float aim = 0f)
        {
            var keys = LowFor(form);
            if (keys == null) return LowHold;
            var k = SampleKeys(form, keys, t);
            if (form == VeilForm.Bow && Mathf.Abs(aim) > 1e-3f)
            {
                float raise = Mathf.SmoothStep(0f, 1f, t / 0.12f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f));
                k.armR += aim * raise;
                k.armL += aim * raise;
            }
            return k;
        }

        /// <summary>
        /// Overlays a low blow on the crouch: the arms, trunk and head take the blow; the legs are left the crouch's,
        /// so he stays down through it.
        /// </summary>
        public static HeroPose ApplyLow(HeroPose pose, VeilForm form, float t, float aim = 0f)
        {
            if (t < 0f || LowFor(form) == null) return pose;
            var k = SampleLow(form, t, aim);
            float w = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.85f) / 0.15f);
            pose.armR = Mathf.Lerp(pose.armR, k.armR, w);
            pose.elbowR = Mathf.Lerp(pose.elbowR, k.elbowR, w);
            pose.armL = Mathf.Lerp(pose.armL, k.armL, w);
            pose.elbowL = Mathf.Lerp(pose.elbowL, k.elbowL, w);
            pose.spine = Mathf.Lerp(pose.spine, k.spine, w);
            pose.chest = Mathf.Lerp(pose.chest, k.chest, w);
            pose.head = Mathf.Lerp(pose.head, k.head, w);
            return pose;
        }

        static Key[] Aimed(VeilForm form, bool up)
        {
            switch (form)
            {
                case VeilForm.Sword: return up ? SwordUp : SwordDown;
                case VeilForm.Spear: return up ? SpearUp : SpearDown;
                case VeilForm.Whip: return up ? WhipUp : WhipDown;
                default: return null;
            }
        }

        /// <summary>
        /// The blow aimed <paramref name="aim"/> degrees off level (+up): straight up / down plays that direction's
        /// own blow, a diagonal is half way between it and the level one. An archer doesn't hold the bow at the
        /// waist and point it up: both arms rise along the arrow and the body leans back (forward, aiming down).
        /// </summary>
        public static Key Sample(VeilForm form, float t, float aim)
        {
            var k = Sample(form, t);
            float a = Mathf.Clamp(aim / 90f, -1f, 1f);
            if (Mathf.Abs(a) < 1e-3f) return k;
            if (form == VeilForm.Bow)
            {
                // the bow comes up into the aim as it is raised to the string
                float raise = Mathf.SmoothStep(0f, 1f, t / 0.12f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f));
                // Aiming down the body bends over into it; aiming up it stays upright (an archer doesn't lean back -
                // the arms alone raise the bow, the chest just lifts a touch).
                float lean = (aim > 0f ? BowUpLean : 0.25f) * aim * raise;
                float arms = aim * raise - lean;    // the shoulders take the rest (they ride on the chest)
                // The level draw already holds the bow arm ~20° up: aiming high, that would carry the bow past the
                // vertical and back over the head. Straight up, the bow stands over the brow, a little in front.
                arms -= BowOverheadTrim * Mathf.Max(0f, a) * Mathf.Max(0f, a) * raise;
                k.armR += arms;
                k.armL += arms;
                k.spine -= lean * 0.45f;
                k.chest -= lean * 0.55f;
                k.head += aim * raise * 0.3f;
                return k;
            }
            var keys = Aimed(form, a > 0f);
            if (keys == null) return k;
            return Blend(k, SampleKeys(form, keys, t), Mathf.Abs(a));
        }

        /// <summary>Degrees the bow arms are held back from the aim when shooting straight up (see Sample).</summary>
        public const float BowOverheadTrim = 30f;

        /// <summary>The share of an upward aim the body takes by leaning back (almost none: it stays upright).</summary>
        public const float BowUpLean = 0.04f;

        static Key Blend(Key a, Key b, float u) => new Key(a.t,
            Mathf.Lerp(a.armR, b.armR, u), Mathf.Lerp(a.elbowR, b.elbowR, u),
            Mathf.Lerp(a.armL, b.armL, u), Mathf.Lerp(a.elbowL, b.elbowL, u),
            Mathf.Lerp(a.spine, b.spine, u), Mathf.Lerp(a.chest, b.chest, u), Mathf.Lerp(a.head, b.head, u),
            Mathf.Lerp(a.thighL, b.thighL, u), Mathf.Lerp(a.kneeL, b.kneeL, u),
            Mathf.Lerp(a.thighR, b.thighR, u), Mathf.Lerp(a.kneeR, b.kneeR, u));

        public static Key[] For(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return Sword;
                case VeilForm.Spear: return Spear;
                case VeilForm.Whip: return Whip;
                case VeilForm.Shield: return Shield;
                case VeilForm.Bow: return Bow;
                default: return null;
            }
        }

        /// <summary>Interpolated key pose at time t (smooth in-out between keys).</summary>
        public static Key Sample(VeilForm form, float t)
        {
            var keys = For(form);
            if (keys == null) return Hold;
            return SampleKeys(form, keys, t);
        }

        /// <summary>
        /// How much later than the body each part reaches its key (as a share of the attack): the torso leads the
        /// blow, the upper arm follows it, the forearm whips through last and the head settles after - the joints
        /// break in turn (successive breaking), so the blow is a wave through the body, not a rigid swing.
        /// </summary>
        public const float TorsoLead = 0.03f, ElbowLag = 0.035f, HeadLag = 0.03f;

        static Key SampleKeys(VeilForm form, Key[] keys, float t)
        {
            t = Mathf.Clamp01(t);
            var body = Spline(form, keys, t);
            var lead = Spline(form, keys, Mathf.Clamp01(t + TorsoLead));
            var lag = Spline(form, keys, Mathf.Clamp01(t - ElbowLag));
            var late = Spline(form, keys, Mathf.Clamp01(t - HeadLag));
            body.spine = lead.spine; body.chest = lead.chest;
            body.thighL = lead.thighL; body.kneeL = lead.kneeL; body.thighR = lead.thighR; body.kneeR = lead.kneeR;
            body.elbowR = lag.elbowR; body.elbowL = lag.elbowL;
            body.head = late.head;
            body.t = t;
            return body;
        }

        /// <summary>
        /// The keys joined by a smooth curve (cubic Hermite, the tangent at each key from its neighbours): the motion
        /// flows through the key poses without stopping at each one (an ease in and out at every key made each blow
        /// halt at every pose, stiffly); it eases only out of the ready stance and back into it.
        /// </summary>
        static Key Spline(VeilForm form, Key[] keys, float t)
        {
            int last = keys.Length - 1;
            for (int i = 0; i < last; i++)
            {
                var a = KeyAt(form, keys, i);
                var b = KeyAt(form, keys, i + 1);
                if (t > b.t && i + 1 < last) continue;
                float h = Mathf.Max(1e-4f, b.t - a.t);
                float u = Mathf.Clamp01((t - a.t) / h);
                var ta = Tangent(form, keys, i);
                var tb = Tangent(form, keys, i + 1);
                var o = new float[Fields];
                var fa = ToArray(a); var fb = ToArray(b);
                for (int f = 0; f < Fields; f++) o[f] = Hermite(fa[f], fb[f], ta[f] * h, tb[f] * h, u);
                return FromArray(t, o);
            }
            return KeyAt(form, keys, last);
        }

        /// <summary>Cubic Hermite between p0 and p1 with end tangents m0, m1 (per unit of u).</summary>
        public static float Hermite(float p0, float p1, float m0, float m1, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p0 + (u3 - 2f * u2 + u) * m0 + (-2f * u3 + 3f * u2) * p1 + (u3 - u2) * m1;
        }

        static Key KeyAt(VeilForm form, Key[] keys, int i)
        {
            int last = keys.Length - 1;
            // (a low blow starts and ends in the crouched guard)
            return i == 0 || i == last ? Ready(form, keys[i], keys == LowFor(form)) : keys[i];
        }

        /// <summary>Rate of change at key i (per unit of attack time): zero in the ready stance at either end.</summary>
        static float[] Tangent(VeilForm form, Key[] keys, int i)
        {
            var m = new float[Fields];
            int last = keys.Length - 1;
            if (i == 0 || i == last) return m;
            var p = ToArray(KeyAt(form, keys, i - 1));
            var n = ToArray(KeyAt(form, keys, i + 1));
            float span = Mathf.Max(1e-4f, keys[i + 1].t - keys[i - 1].t);
            // (0.8: a little tighter than Catmull-Rom, so a fast swing doesn't overshoot wildly between keys)
            for (int f = 0; f < Fields; f++) m[f] = 0.8f * (n[f] - p[f]) / span;
            return m;
        }

        const int Fields = 11;

        static float[] ToArray(Key k) => new[] { k.armR, k.elbowR, k.armL, k.elbowL, k.spine, k.chest, k.head, k.thighL, k.kneeL, k.thighR, k.kneeR };

        static Key FromArray(float t, float[] f) => new Key(t, f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10]);

        static Key Ready(VeilForm form, Key k, bool low = false)
        {
            float arm, elbow;
            if (low) HeroPoseSolver.WeaponHoldLow(form, out arm, out elbow);
            else HeroPoseSolver.WeaponHold(form, 0f, out arm, out elbow);
            k.armR = arm;
            k.elbowR = elbow;
            return k;
        }

        /// <summary>Overlays the attack on a locomotion pose. Legs only take over when standing, so you can attack on the run.</summary>
        public static HeroPose Apply(HeroPose pose, VeilForm form, float t, float run, float aim = 0f)
        {
            if (t < 0f || For(form) == null) return pose;
            var k = Sample(form, t, aim); // aimed blows have their own poses (see Sample(form, t, aim))
            float w = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.85f) / 0.15f); // hand back to locomotion at the end
            float legs = w * (1f - Mathf.Clamp01(run) * 0.75f);
            pose.armR = Mathf.Lerp(pose.armR, k.armR, w);
            pose.elbowR = Mathf.Lerp(pose.elbowR, k.elbowR, w);
            pose.armL = Mathf.Lerp(pose.armL, k.armL, w);
            pose.elbowL = Mathf.Lerp(pose.elbowL, k.elbowL, w);
            pose.spine = Mathf.Lerp(pose.spine, k.spine, w);
            pose.chest = Mathf.Lerp(pose.chest, k.chest, w);
            pose.head = Mathf.Lerp(pose.head, k.head, w);
            pose.thighL = Mathf.Lerp(pose.thighL, k.thighL, legs);
            pose.kneeL = Mathf.Lerp(pose.kneeL, k.kneeL, legs);
            pose.thighR = Mathf.Lerp(pose.thighR, k.thighR, legs);
            pose.kneeR = Mathf.Lerp(pose.kneeR, k.kneeR, legs);
            return pose;
        }

        /// <summary>Forward step speed of the lunge (m/s) and the moment it happens.</summary>
        public static bool Lunge(VeilForm form, out float speed, out float at)
        {
            switch (form)
            {
                case VeilForm.Sword: speed = 5f; at = 0.32f; return true;
                case VeilForm.Spear: speed = 8f; at = 0.34f; return true;
                case VeilForm.Shield: speed = 6f; at = 0.22f; return true;
                default: speed = 0f; at = 0f; return false;
            }
        }
    }
}
