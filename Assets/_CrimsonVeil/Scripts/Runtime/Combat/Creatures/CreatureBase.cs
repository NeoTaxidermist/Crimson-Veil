using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Shared core of the bestiary: health, hits from the veil and arrows, contact damage, death.
    /// A creature only implements <see cref="Tick"/> (its behaviour) and optionally filters hits.
    /// Visuals are built in the editor by each creature's static <c>BuildVisual</c> and found by name at runtime.
    /// </summary>
    public abstract class CreatureBase : MonoBehaviour, IDamageable, IHostile, IStaggerable
    {
        /// <summary>Seconds a creature reels from a blow that lands (its touch harms nothing meanwhile).</summary>
        public const float HurtReel = 0.3f;
        /// <summary>Seconds it reels when its own strike is blocked or parried by the hero's shield.</summary>
        public const float StaggerReel = 0.7f;
        /// <summary>How far (m) the body is thrown back by a blow, at most, before it springs back.</summary>
        public const float RecoilReach = 0.28f;

        /// <summary>It is reeling (from a blow, or a strike turned by the shield): it harms nothing, its wind-ups stop.</summary>
        public bool Reeling => reel > 0f;
        /// <summary>How far (m) the body is thrown back from its place right now (springing back to zero).</summary>
        public Vector3 Recoil => recoil;
        float reel;
        Vector3 recoil, recoilVel, recoilApplied, visualWritten;
        Quaternion tiltApplied = Quaternion.identity, rotWritten;
        Transform recoilVisual;
        bool visualSearched;

        /// <summary>The hero's shield turned its strike (block or parry): it reels back.</summary>
        public void Stagger()
        {
            if (IsDead) return;
            reel = Mathf.Max(reel, StaggerReel);
            Vector3 away = transform.position - (target.IsValid ? target.Position : transform.position - Vector3.right);
            Kick(new Vector2(Mathf.Sign(away.x == 0f ? 1f : away.x) * 6f, 2f), 1.2f);
            HitFlash.Flash(gameObject, HitFlash.Pale, HitFlash.FoeSeconds);
            OnReel(true);
        }

        /// <summary>A blow landed or a strike was turned: the body is thrown along <paramref name="push"/> and springs back.</summary>
        void Kick(Vector2 push, float strength)
        {
            Vector2 d = push.sqrMagnitude > 1e-4f ? push.normalized : Vector2.right;
            float reach = Mathf.Min(RecoilReach, 0.04f * push.magnitude) * strength;
            recoilVel += new Vector3(d.x, d.y * 0.5f, 0f) * (reach * 14f);
        }

        /// <summary>It begins to reel (<paramref name="staggered"/>: its strike was turned): a creature stops its wind-up.</summary>
        protected virtual void OnReel(bool staggered) { }

        /// <summary>The body springs back from a recoil (a stiff, quickly damped spring), drawn over the creature's own
        /// pose: whatever its Tick put on the visual this frame stays, the recoil is added on top.</summary>
        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!visualSearched) { recoilVisual = CreatureParts.Find(transform, "Visual"); visualSearched = true; }
            const float k = 260f, damping = 26f;
            recoilVel += (-recoil * k - recoilVel * damping) * dt;
            recoil += recoilVel * dt;
            var visual = recoilVisual;
            if (visual == null) return;
            // (taken off again only if the creature left the visual as it was: a creature that sets it every frame has
            // already put its own pose there)
            if (visual.localPosition == visualWritten) visual.localPosition -= recoilApplied;
            if (visual.localRotation == rotWritten) visual.localRotation *= Quaternion.Inverse(tiltApplied);
            Vector3 local = visual.parent != null ? visual.parent.InverseTransformVector(recoil) : recoil;
            float lean = Mathf.Clamp(recoil.x * 70f, -14f, 14f);
            var tilt = Quaternion.Euler(0f, 0f, -lean);
            visual.localPosition += local;
            visual.localRotation *= tilt;
            recoilApplied = local;
            tiltApplied = tilt;
            visualWritten = visual.localPosition;
            rotWritten = visual.localRotation;
        }
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

        protected virtual void Awake()
        {
            Health = new HealthModel(maxHealth, 0.12f);
            Dormancy.Track(gameObject);   // (asleep far out of the picture: a chapter holds hundreds)
        }

        /// <summary>How many drops of crimson it sheds when slain (a harmless one sheds none).</summary>
        public virtual int CrimsonWorth => Fights ? 3 : 0;

        /// <summary>Does it fight the hero at all? (A harmless lure does not: it never makes a fight.)</summary>
        public virtual bool Fights => true;
        /// <summary>It is after the hero now (see <see cref="IHostile.EngagedWithHero"/>).</summary>
        public virtual bool EngagedWithHero => false;
        public bool IsAlive => !IsDead;
        public virtual Vector3 HostilePosition => transform.position + Vector3.up * 0.6f;

        protected virtual void OnEnable() { if (Fights) Hostiles.Register(this); }
        protected virtual void OnDisable() => Hostiles.Unregister(this);

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
            reel -= dt;
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
            // every blow that lands throws the body back along it, and it reels a moment (the finisher's push is
            // stronger, and throws it further)
            reel = Mathf.Max(reel, HurtReel);
            Kick(info.knockback, 1f);
            OnReel(false);
            OnHurt(info);
            Hurt?.Invoke(info);
            CombatFx.Burst(info.point, info.knockback.normalized, CombatFx.Ink, 20, 5f);
            HitFlash.Flash(gameObject, HitFlash.Pale, HitFlash.FoeSeconds);
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
            if (!HasTarget || contactCooldown > 0f || amount <= 0f || Reeling) return false;   // (reeling, it harms nothing)
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
            // slain, it sheds its crimson (v0.110); one that fell out of the world has none to give
            if (transform.position.y > killY) CrimsonDrop.Spill(transform.position + Vector3.up * 0.6f, CrimsonWorth);
            CombatFx.Burst(transform.position + Vector3.up * 0.6f, Vector2.up, CombatFx.Ink, 32, 4.5f);
            GameSound.Play(Cue.Dissolve);
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
