using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The rest of the body on top of the key poses (which only bend the limbs and the spine forward and back): the
    /// pelvis and the chest turn against each other in the stride, the collarbones go with the arms, the arms swing
    /// clear of the body, the neck keeps the gaze level, the toes bend as the heel lifts, the body turns into a blow,
    /// and the wrists trail behind a swinging arm (<see cref="WristLag"/>). Pure functions of the pose and the state.
    /// </summary>
    public static class HeroBodyMotion
    {
        /// <summary>Adds the body's secondary motion to <paramref name="p"/> (phase 0..1 of the stride, run 0..1).</summary>
        public static HeroPose Layer(HeroPose p, in HeroState s, float phase, float run, float time, bool attacking)
        {
            bool onFeet = s.grounded && !s.hanging && !s.climbing && !s.swinging && !s.wallSliding;
            float gait = onFeet ? Mathf.SmoothStep(0f, 1f, run / 0.15f) : 0f;
            float idle = onFeet ? 1f - gait : 0f;
            float a = phase * Mathf.PI * 2f;
            float sinA = Mathf.Sin(a);

            // The stride: the hip of the forward leg goes forward (the left leg leads at sinA > 0), the chest turns
            // the other way, so the opposite shoulder and arm come forward with the leg - and a slow sway standing.
            p.pelvisTurn += -sinA * Mathf.Lerp(4f, 10f, run) * gait + Mathf.Sin(time * 0.55f) * 2.5f * idle;
            p.twist += sinA * Mathf.Lerp(7f, 16f, run) * gait + Mathf.Sin(time * 0.55f + 1.2f) * 2f * idle;
            p.pelvis += Mathf.Lerp(2f, 7f, run) * gait + 2f * idle;

            // The collarbones go with the arms (a shoulder reaches forward into a thrust, draws back on the wind-up).
            p.shoulderL += ShoulderReach(p.armL);
            p.shoulderR += ShoulderReach(p.armR);

            // The arms hang a little out from the body, further when they swing far forward / back or go overhead,
            // so they pass by the hips and the head instead of through them.
            p.armOutL += ArmOut(p.armL, run);
            p.armOutR += ArmOut(p.armR, run);

            // The neck holds the head up as the body leans.
            p.neck += -0.22f * (p.spine + p.chest);

            // The toes bend up as the heel lifts off behind (push-off); in the air they hang a little.
            if (onFeet)
            {
                p.toeL += Mathf.Clamp(-p.thighL * 0.9f, 0f, 38f) * gait;
                p.toeR += Mathf.Clamp(-p.thighR * 0.9f, 0f, 38f) * gait;
            }
            else if (!s.hanging && !s.climbing)
            {
                p.toeL -= 8f; p.toeR -= 8f;
            }

            // Into a blow the body turns: leaning into it turns the sword shoulder in, the hips follow a little.
            if (attacking)
            {
                float into = Mathf.Clamp((p.spine + p.chest) * 0.55f, -18f, 24f);
                p.twist += into;
                p.pelvisTurn += into * 0.35f;
            }

            // An empty hand hangs relaxed, bent a touch towards the palm.
            p.wristL += 8f;
            p.wristR += s.weaponHeld > 0.3f ? 0f : 8f;
            return p;
        }

        /// <summary>
        /// The collarbone going with the arm (degrees, + the shoulder forward): it reaches forward into a thrust and draws
        /// back on a wind-up - but an arm raised overhead must not take it along: turning the collarbone forward there
        /// tips the raised arm back over the head (the hands would hang from a ledge behind the head, the bow would be
        /// drawn behind it), so the reach fades out as the arm goes up past the horizontal.
        /// </summary>
        public static float ShoulderReach(float arm)
        {
            float reach = Mathf.Clamp(arm * 0.12f, -10f, 18f);
            return reach * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(95f, 140f, arm)));
        }

        static float ArmOut(float arm, float run) => 6f + 5f * run + Mathf.Clamp((Mathf.Abs(arm - 20f) - 45f) * 0.14f, 0f, 14f);

        /// <summary>
        /// How far a wrist trails a swinging arm (degrees): against the arm's angular speed (deg/s), so the hand
        /// follows a beat behind like a loose hand does. A hand holding a weapon is held firm (the blade goes with the
        /// forearm), so it trails only a little.
        /// </summary>
        public static float WristLag(float armSpeed, bool holding)
        {
            float lag = Mathf.Clamp(-armSpeed * 0.03f, -28f, 28f);
            return holding ? lag * 0.25f : lag;
        }
    }
}
