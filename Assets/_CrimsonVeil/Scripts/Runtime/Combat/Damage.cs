using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    public enum DamageKind
    {
        Slash,
        Pierce,
        Bash,
        Arrow,
        Contact,
    }

    public struct DamageInfo
    {
        public float amount;
        /// <summary>Velocity (m/s) to push the receiver with.</summary>
        public Vector2 knockback;
        public Vector3 point;
        public DamageKind kind;
        public GameObject source;
    }

    /// <summary>Anything the veil, arrows or enemies can hit.</summary>
    public interface IDamageable
    {
        /// <summary>Returns true when the hit registered (not blocked/invulnerable/dead).</summary>
        bool ReceiveHit(in DamageInfo info);
    }

    /// <summary>Things that can be knocked off-balance (e.g. an enemy whose strike hits a shield).</summary>
    public interface IStaggerable
    {
        void Stagger();
    }

    /// <summary>Hit points with invulnerability frames. Engine independent.</summary>
    public sealed class HealthModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float InvulnerableTime { get; set; }
        public bool IsDead => Current <= 0f;
        public bool IsInvulnerable => invulnerableTimer > 0f;
        public float Fraction => Max > 0f ? Current / Max : 0f;

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        float invulnerableTimer;

        public HealthModel(float max, float invulnerableTime = 0f)
        {
            Max = Mathf.Max(0.01f, max);
            Current = Max;
            InvulnerableTime = invulnerableTime;
        }

        public bool TryDamage(in DamageInfo info)
        {
            if (IsDead || IsInvulnerable || info.amount <= 0f) return false;
            Current = Mathf.Max(0f, Current - info.amount);
            invulnerableTimer = InvulnerableTime;
            Damaged?.Invoke(info);
            if (IsDead) Died?.Invoke();
            return true;
        }

        public void Tick(float dt) => invulnerableTimer = Mathf.Max(0f, invulnerableTimer - dt);

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(Max, Current + Mathf.Max(0f, amount));
        }

        public void Restore()
        {
            Current = Max;
            invulnerableTimer = 0f;
        }
    }
}
