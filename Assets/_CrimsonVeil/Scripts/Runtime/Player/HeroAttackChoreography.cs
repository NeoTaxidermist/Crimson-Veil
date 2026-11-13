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

        static readonly Key[] Sword =
        {
            At(Hold, 0f),
            K(0.28f, 150f, 80f, 35f, 40f, -8f, -14f, 6f, 10f, 20f, -8f, 25f),   // wind-up over the shoulder
            K(0.42f, 60f, 20f, -10f, 30f, 14f, 12f, -6f, 40f, 50f, -25f, 15f),  // the cut
            K(0.52f, -55f, 5f, -35f, 25f, 24f, 20f, -10f, 45f, 55f, -30f, 20f), // follow-through, deep lunge
            K(0.75f, -20f, 20f, -10f, 20f, 12f, 8f, -4f, 25f, 35f, -15f, 15f),
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
            t = Mathf.Clamp01(t);
            int last = keys.Length - 1;
            for (int i = 0; i < last; i++)
            {
                // Every attack starts and ends in that weapon's own ready stance.
                var a = i == 0 ? Ready(form, keys[i]) : keys[i];
                var b = i + 1 == last ? Ready(form, keys[i + 1]) : keys[i + 1];
                if (t > b.t) continue;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.t, b.t, t));
                return new Key(t,
                    Mathf.Lerp(a.armR, b.armR, u), Mathf.Lerp(a.elbowR, b.elbowR, u),
                    Mathf.Lerp(a.armL, b.armL, u), Mathf.Lerp(a.elbowL, b.elbowL, u),
                    Mathf.Lerp(a.spine, b.spine, u), Mathf.Lerp(a.chest, b.chest, u), Mathf.Lerp(a.head, b.head, u),
                    Mathf.Lerp(a.thighL, b.thighL, u), Mathf.Lerp(a.kneeL, b.kneeL, u),
                    Mathf.Lerp(a.thighR, b.thighR, u), Mathf.Lerp(a.kneeR, b.kneeR, u));
            }
            return Ready(form, keys[last]);
        }

        static Key Ready(VeilForm form, Key k)
        {
            HeroPoseSolver.WeaponHold(form, 0f, out float arm, out float elbow);
            k.armR = arm;
            k.elbowR = elbow;
            return k;
        }

        /// <summary>Overlays the attack on a locomotion pose. Legs only take over when standing, so you can attack on the run.</summary>
        public static HeroPose Apply(HeroPose pose, VeilForm form, float t, float run)
        {
            if (t < 0f || For(form) == null) return pose;
            var k = Sample(form, t);
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
