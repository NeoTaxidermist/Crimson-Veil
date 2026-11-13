using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Ink wraith: hovers along the ground, patrols, chases, telegraphs and lunges.
    /// Uses a trigger capsule so the hero can pass through it (contact still hurts).
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class Enemy : MonoBehaviour, IDamageable, IStaggerable
    {
        [SerializeField] EnemyBrainSettings brain = new EnemyBrainSettings();
        [SerializeField] float maxHealth = 3f;
        [SerializeField] float strikeDamage = 1f;
        [SerializeField] float contactDamage = 0.5f;
        [SerializeField] float strikeReach = 1.5f;
        [SerializeField] float strikeKnockback = 7f;
        [SerializeField] float knockbackDrag = 18f;
        [SerializeField] float killY = -25f;

        public EnemyBrain Brain { get; private set; }
        public HealthModel Health { get; private set; }
        public int Facing => Brain.Facing;
        public bool IsDead => Health.IsDead;
        /// <summary>0..1 during the wind-up, for the telegraph (eyes flare, arms rise).</summary>
        public float WindupProgress => Brain.State == EnemyState.Windup ? Mathf.Clamp01(Brain.StateTime / Brain.Settings.windupTime) : 0f;

        public event Action<DamageInfo> Hurt;
        public event Action Died;

        CombatTarget target;
        float knockX;
        float fallVelocity;
        bool strikeLanded;
        CapsuleCollider body;

        void Awake()
        {
            body = GetComponent<CapsuleCollider>();
            body.isTrigger = true;
            Brain = new EnemyBrain(brain, transform.position.x);
            Health = new HealthModel(maxHealth, 0.1f);
            Brain.StateChanged += s => { if (s == EnemyState.Strike) strikeLanded = false; };
        }

        void Start() => target = CombatTarget.Find();

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Health.Tick(dt);

            var pos = transform.position;
            bool hasTarget = target.IsValid;
            var senses = new EnemySenses
            {
                selfX = pos.x,
                playerX = hasTarget ? target.Position.x : pos.x + 1000f,
                playerDeltaY = hasTarget ? target.Position.y - pos.y : 0f,
                playerAlive = hasTarget && target.IsAlive,
                pathClear = PathClear(Brain.Facing),
            };
            var intent = Brain.Tick(dt, senses);

            // Horizontal motion: own intent + decaying knockback, never through walls or off ledges on purpose.
            knockX = Mathf.MoveTowards(knockX, 0f, knockbackDrag * dt);
            float vx = intent.velocityX + knockX;
            if (Mathf.Abs(vx) > 0.01f && !WallClear(Math.Sign(vx))) { vx = 0f; knockX = 0f; }
            pos.x += vx * dt;

            // Hover on the ground; fall if knocked off a ledge.
            if (Physics.Raycast(pos + Vector3.up * 1.2f, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos.y = Mathf.MoveTowards(pos.y, hit.point.y, 12f * dt);
                fallVelocity = 0f;
            }
            else
            {
                fallVelocity -= 25f * dt;
                pos.y += fallVelocity * dt;
            }
            transform.position = pos;
            if (pos.y < killY && !IsDead) Kill();

            if (!hasTarget || IsDead || !target.IsAlive) return;

            Vector3 toTarget = target.Position - pos;
            if (intent.strikeActive && !strikeLanded && Mathf.Sign(toTarget.x) == Facing && Mathf.Abs(toTarget.x) < strikeReach && Mathf.Abs(toTarget.y) < 1.4f)
            {
                strikeLanded = true;
                target.Damageable.ReceiveHit(new DamageInfo
                {
                    amount = strikeDamage,
                    knockback = new Vector2(Facing * strikeKnockback, 4f),
                    point = pos + Vector3.up * 1.1f,
                    kind = DamageKind.Slash,
                    source = gameObject,
                });
            }
            else if (Brain.State != EnemyState.Hurt && Mathf.Abs(toTarget.x) < 0.55f && Mathf.Abs(toTarget.y) < 1.4f)
            {
                target.Damageable.ReceiveHit(new DamageInfo
                {
                    amount = contactDamage,
                    knockback = new Vector2(Mathf.Sign(toTarget.x) * 5f, 3f),
                    point = pos + Vector3.up,
                    kind = DamageKind.Contact,
                    source = gameObject,
                });
            }
        }

        bool WallClear(int dir)
        {
            // The hero's CharacterController is not a wall — the wraith drifts through.
            if (Physics.Raycast(transform.position + Vector3.up * 0.8f, new Vector3(dir, 0f, 0f), out var h, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                return h.collider is CharacterController;
            return true;
        }

        bool PathClear(int dir)
        {
            if (!WallClear(dir)) return false;
            Vector3 ahead = transform.position + new Vector3(dir * 0.7f, 0.6f, 0f);
            return Physics.Raycast(ahead, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore);
        }

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsDead || !Health.TryDamage(info)) return false;
            knockX = info.knockback.x;
            Brain.OnHurt(Health.IsDead);
            Hurt?.Invoke(info);
            if (Health.IsDead) Kill();
            return true;
        }

        public void Stagger()
        {
            if (IsDead) return;
            Brain.OnStaggered();
            knockX = -Facing * 6f;
        }

        void Kill()
        {
            if (!Health.IsDead) Health.TryDamage(new DamageInfo { amount = 9999f });
            Brain.OnHurt(true);
            body.enabled = false;
            Died?.Invoke();
            CombatFx.Burst(transform.position + Vector3.up * 1.1f, Vector2.up, CombatFx.Ink, 40, 5f);
            Destroy(gameObject, 1.6f);
        }
    }

    /// <summary>Cached reference to the hero as a combat target.</summary>
    public struct CombatTarget
    {
        public PlayerHealth Health;
        public IDamageable Damageable => Health;
        public bool IsValid => Health != null;
        public bool IsAlive => Health != null && Health.IsAlive && Health.isActiveAndEnabled;
        public Vector3 Position => Health.transform.position;

        public static CombatTarget Find() => new CombatTarget { Health = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>() };
    }
}
