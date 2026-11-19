using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// The chain of blows. A combo is a position in the chain - first, second, the finisher - not a weapon: the
    /// veil may pour from one weapon into the next between blows and the chain goes on. A pause resets it, so a lone
    /// blow is always the first. The finisher hits harder, throws further and leaves the body longer to recover;
    /// the more different weapons the chain was woven from, the harder it hits. The first blow of a weapon poured
    /// into mid-chain (the morph strike) is quicker, carried by the transformation itself.
    /// </summary>
    public sealed class VeilCombo
    {
        /// <summary>Blows in a full chain (the last is the finisher).</summary>
        public const int Length = 3;
        /// <summary>Seconds without a blow after which the chain starts again from the first.</summary>
        public const float ResetAfter = 0.6f;

        /// <summary>The finisher's damage and knockback, before the bonus for variety.</summary>
        public const float FinisherDamage = 1.5f, FinisherKnockback = 1.6f;
        /// <summary>Added to the finisher's damage for each further different weapon in the chain.</summary>
        public const float VarietyBonus = 0.25f;
        /// <summary>Extra seconds of recovery after the finisher: the reward is a commitment.</summary>
        public const float FinisherRecovery = 0.12f;
        /// <summary>The morph strike: quicker and a touch harder.</summary>
        public const float MorphSpeed = 1.25f, MorphDamage = 1.15f;

        /// <summary>One blow of the chain.</summary>
        public struct Blow
        {
            /// <summary>0 first, 1 second, 2 the finisher.</summary>
            public int step;
            public VeilForm form;
            public bool finisher;
            /// <summary>The first blow of a weapon poured into mid-chain.</summary>
            public bool morph;
            /// <summary>How many different weapons the chain has used, this blow included.</summary>
            public int variety;

            public float DamageScale
            {
                get
                {
                    float d = 1f;
                    if (morph) d *= MorphDamage;
                    if (finisher) d *= FinisherDamage + VarietyBonus * (variety - 1);
                    return d;
                }
            }

            public float KnockbackScale => finisher ? FinisherKnockback : 1f;
            /// <summary>The blow's duration is divided by this (the bow keeps its draw).</summary>
            public float Speed => morph && form != VeilForm.Bow ? MorphSpeed : 1f;
            public float ExtraRecovery => finisher ? FinisherRecovery : 0f;

            public static Blow None => new Blow { step = -1 };
        }

        public Blow Current { get; private set; } = Blow.None;
        /// <summary>Position of the latest blow in the chain (0 when none).</summary>
        public int Step => Mathf.Max(0, Current.step);

        float sinceBlow = 99f;
        bool striking;
        VeilForm[] used = new VeilForm[Length];
        int usedCount;

        /// <summary>Seconds since the last blow ended (while none is under way).</summary>
        public float SinceBlow => sinceBlow;

        /// <summary>A blow begins: where it stands in the chain.</summary>
        public Blow Begin(VeilForm form)
        {
            var last = Current;
            bool goesOn = last.step >= 0 && !last.finisher && sinceBlow <= ResetAfter;
            int step = goesOn ? last.step + 1 : 0;
            if (step == 0) usedCount = 0;
            bool seen = false;
            for (int i = 0; i < usedCount; i++) if (used[i] == form) seen = true;
            if (!seen && usedCount < used.Length) used[usedCount++] = form;
            Current = new Blow
            {
                step = step,
                form = form,
                finisher = step == Length - 1,
                morph = goesOn && last.form != form,
                variety = usedCount,
            };
            striking = true;
            sinceBlow = 0f;
            return Current;
        }

        /// <summary>The blow ended (spent or cut short).</summary>
        public void End() => striking = false;

        public void Tick(float dt)
        {
            if (!striking) sinceBlow += dt;
        }

        /// <summary>Forgets the chain (death, respawn).</summary>
        public void Reset()
        {
            Current = Blow.None;
            usedCount = 0;
            striking = false;
            sinceBlow = 99f;
        }
    }
}
