using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Shared core of the bestiary: health, hits from the veil and arrows, contact damage, death.
    /// A creature only implements <see cref="Tick"/> (its behaviour) and optionally filters hits.
    /// Visuals are built in the editor by each creature's static <c>BuildVisual</c> and found by name at runtime.
    /// </summary>
    public abstract class CreatureBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 2f;
        [SerializeField] protected float contactDamage = 0.5f;
        [SerializeField] protected float killY = -25f;

        public HealthModel Health { get; private set; }
        public bool IsDead => Health != null && Health.IsDead;
        /// <summary>Time since the last registered hit (for hurt flashes and staggers).</summary>
        public float SinceHurt { get; private set; } = 99f;

        public event Action<DamageInfo> Hurt;
        public event Action Died;

        protected CombatTarget target;
        float contactCooldown;

        protected virtual void Awake() => Health = new HealthModel(maxHealth, 0.12f);

        protected virtual void Start() => target = CombatTarget.Find();

        protected bool HasTarget => target.IsValid && target.IsAlive;
        /// <summary>The hero's chest.</summary>
        protected Vector3 TargetPos => target.Position + Vector3.up * 0.9f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Health.Tick(dt);
            contactCooldown -= dt;
            SinceHurt += dt;
            if (!target.IsValid) target = CombatTarget.Find();
            Tick(dt);
            if (!IsDead && transform.position.y < killY) Kill();
        }

        /// <summary>Behaviour, every frame (also after death, for falling corpses and fading).</summary>
        protected abstract void Tick(float dt);

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsDead) return false;
            if (!AcceptHit(info)) return false;
            var dealt = info;
            dealt.amount *= DamageMultiplier(info);
            if (!Health.TryDamage(dealt)) return false;
            SinceHurt = 0f;
            OnHurt(info);
            Hurt?.Invoke(info);
            CombatFx.Burst(info.point, info.knockback.normalized, CombatFx.Ink, 14, 4f);
            if (Health.IsDead) Kill();
            return true;
        }

        /// <summary>Return false to shrug a hit off (blocked, invulnerable, wrong weapon).</summary>
        protected virtual bool AcceptHit(in DamageInfo info) => true;

        protected virtual void OnHurt(in DamageInfo info) { }

        /// <summary>Scales incoming damage (e.g. a pinned crawler takes double).</summary>
        protected virtual float DamageMultiplier(in DamageInfo info) => 1f;

        /// <summary>Damages the hero if his chest is within <paramref name="radius"/> of <paramref name="center"/>.</summary>
        protected bool TouchDamage(Vector3 center, float radius, float amount, Vector2 knockback, DamageKind kind = DamageKind.Contact)
        {
            if (!HasTarget || contactCooldown > 0f || amount <= 0f) return false;
            Vector3 d = TargetPos - center;
            d.z = 0f;
            if (d.sqrMagnitude > radius * radius) return false;
            contactCooldown = 0.6f;
            target.Damageable.ReceiveHit(new DamageInfo
            {
                amount = amount,
                knockback = knockback,
                point = center,
                kind = kind,
                source = gameObject,
            });
            return true;
        }

        public void Kill()
        {
            if (Health == null || (IsDead && diedFired)) return;
            if (!Health.IsDead) Health.TryDamage(new DamageInfo { amount = 9999f });
            diedFired = true;
            Died?.Invoke();
            CombatFx.Burst(transform.position + Vector3.up * 0.6f, Vector2.up, CombatFx.Ink, 32, 4.5f);
            OnDeath();
        }

        bool diedFired;

        /// <summary>Default death: dissolve and remove. Creatures that leave a body (rope, kite) override it.</summary>
        protected virtual void OnDeath()
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            gameObject.AddComponent<CrimsonVeil.World.ShrinkAndDestroy>().Begin(1.2f);
        }

        /// <summary>Ground height under x (or NaN): first solid surface below <paramref name="from"/>.</summary>
        protected static float GroundBelow(Vector3 from, float maxDistance = 30f)
        {
            return Physics.Raycast(from, Vector3.down, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y : float.NaN;
        }
    }
}
