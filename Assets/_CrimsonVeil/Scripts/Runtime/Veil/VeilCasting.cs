using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Rules of casting weapons like spells: what each costs of the scarf's strength, how long a weapon
    /// lingers for a follow-up blow, the parry window, the cast transformation speed.
    /// </summary>
    public static class VeilCasting
    {
        /// <summary>Seconds a cast weapon keeps its shape after the blow — another press continues the combo.</summary>
        public const float Linger = 0.25f;
        /// <summary>Melting back into cloth once a weapon is let go: quick, so the scarf is back at once.</summary>
        public const float MeltTime = 0.16f;
        /// <summary>Hardening / morph time of a cast: the transformation *is* the wind-up, so it must be quick.</summary>
        public const float CastTransformTime = 0.12f;
        public const float CastMorphTime = 0.14f;
        /// <summary>Raising the shield this long before a blow lands parries it.</summary>
        public const float ParryWindow = 0.2f;
        /// <summary>How long the bow can be held drawn before it looses on its own.</summary>
        public const float MaxDraw = 2.5f;
        /// <summary>Attack progress at which a drawn bow holds (string at full draw).</summary>
        public const float BowHoldAt = 0.27f;
        /// <summary>The spear thrust turned straight down (pogo), degrees.</summary>
        public const float DownThrustTilt = -82f;
        /// <summary>Furthest the bow aims up or down, degrees.</summary>
        public const float BowAimRange = 55f;

        /// <summary>Fraction of the scarf's strength a cast costs. The sword — the hero's own blade — is free.</summary>
        public static float Cost(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return 0.15f;
                case VeilForm.Spear: return 0.25f;
                case VeilForm.Shield: return 0.2f;
                case VeilForm.Bow: return 0.3f;
                default: return 0f;
            }
        }

        /// <summary>Strength drained per second while the shield is held up or the scarf glides.</summary>
        public const float ShieldHoldDrain = 0.18f;
        public const float GlideDrain = 0.3f;
        /// <summary>Strength the scarf spends to catch a ledge out of the hands' reach.</summary>
        public const float LedgeCatchCost = 0.1f;
    }

    /// <summary>
    /// The scarf's strength (0..1): its length. Casting spends it — the scarf visibly shortens — and it
    /// grows back after a breath; landing blows feeds it. At full length it can take one hit for the hero,
    /// after which it is torn short and must grow back.
    /// </summary>
    public sealed class VeilCharge
    {
        public float Value { get; private set; } = 1f;
        public float RegrowDelay = 0.6f;
        public float RegrowPerSecond = 0.38f;
        public float HitRefund = 0.08f;
        float sinceSpend = 99f;

        public bool IsFull => Value >= 0.999f;

        public bool CanAfford(float cost) => Value + 1e-4f >= cost;

        public bool TrySpend(float cost)
        {
            if (cost <= 0f) return true;
            if (!CanAfford(cost)) return false;
            Value = Mathf.Max(0f, Value - cost);
            sinceSpend = 0f;
            return true;
        }

        /// <summary>Continuous drain (shield held, gliding). Returns false once empty.</summary>
        public bool Drain(float perSecond, float dt)
        {
            if (Value <= 0f) return false;
            Value = Mathf.Max(0f, Value - perSecond * dt);
            sinceSpend = 0f;
            return Value > 0f;
        }

        public void Refund(float amount) => Value = Mathf.Min(1f, Value + amount);

        /// <summary>A blow landed: the veil feeds on it.</summary>
        public void OnHitLanded() => Refund(HitRefund);

        /// <summary>The full-length scarf takes a hit instead of the hero, and is torn short.</summary>
        public bool TryAbsorbHit()
        {
            if (!IsFull) return false;
            Value = 0f;
            sinceSpend = -0.6f; // torn: it takes a little longer before it starts growing back
            return true;
        }

        public void Tick(float dt)
        {
            sinceSpend += dt;
            if (sinceSpend >= RegrowDelay) Value = Mathf.Min(1f, Value + RegrowPerSecond * dt);
        }

        public void Restore() { Value = 1f; sinceSpend = 99f; }
    }
}
