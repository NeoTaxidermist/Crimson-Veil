using System;
using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Hero's life, shown by the scarf-shaped gauge in the top-left corner (<see cref="CrimsonVeil.UI.ScarfHud"/>).
    /// A hardened shield facing the attacker blocks the hit and staggers it.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 5f;
        [SerializeField] float invulnerableTime = 0.9f;
        [SerializeField] float stunTime = 0.3f;
        [SerializeField] float respawnDelay = 1.4f;
        [SerializeField] float regenDelay = 6f;
        [SerializeField] float regenPerSecond = 0.25f;
        [SerializeField] Transform visualRoot;

        public HealthModel Model { get; private set; }
        public bool IsAlive => Model != null && !Model.IsDead;
        public int BlockCount { get; private set; }
        public int ParryCount { get; private set; }
        /// <summary>Hits the full-length scarf took for the hero.</summary>
        public int AbsorbCount { get; private set; }

        public event Action Blocked;
        public event Action Parried;
        public event Action Absorbed;
        public event Action Died;
        public event Action Respawned;

        PlayerController player;
        VeilController veil;
        float sinceHit = 999f;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            veil = GetComponent<VeilController>();
            Model = new HealthModel(maxHealth, invulnerableTime);
            if (visualRoot == null) visualRoot = transform.Find("Visual");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Model.Tick(dt);
            sinceHit += dt;
            if (IsAlive && sinceHit > regenDelay) Model.Heal(regenPerSecond * dt);
            dimTimer = Mathf.Max(0f, dimTimer - dt);
            float dim = dimTimer > 0f ? dimAmount : 0f;
            // Life is shown by the HUD (top-left), not by the scarf: the scarf only dims when smothered by ash.
            if (veil != null) veil.Vitality = Mathf.MoveTowards(veil.Vitality, 1f - dim, dt * 2f);
        }

        void Start() => CrimsonVeil.UI.ScarfHud.Ensure();

        float dimTimer, dimAmount;

        /// <summary>Ash and the like smother the veil: its glow (and with it the hero's strength) is dimmed for a while.</summary>
        /// <summary>The shield is raised and held: it keeps the ash of a moth off the scarf (v0.109.1).</summary>
        public bool ShieldHeld => veil != null && veil.ShieldHeld;

        /// <summary>How many times the held shield kept ash off the scarf (for tests).</summary>
        public int AshWardedCount { get; private set; }

        /// <summary>
        /// Ash falls on the hero: it smothers the scarf - unless the shield is held over him, then it slides off it
        /// (a puff of grey off the ring). The held shield drains the scarf as ever, so it is a choice, not free.
        /// </summary>
        public void Ash(float amount, float seconds)
        {
            if (ShieldHeld)
            {
                AshWardedCount++;
                if (UnityEngine.Random.value < 0.15f)
                    CombatFx.Burst(transform.position + Vector3.up * 2f, Vector2.up, new Color(0.35f, 0.35f, 0.36f, 0.8f), 3, 1.5f);
                return;
            }
            Dim(amount, seconds);
        }

        public void Dim(float amount, float seconds)
        {
            dimAmount = Mathf.Clamp01(Mathf.Max(dimTimer > 0f ? dimAmount : 0f, amount));
            dimTimer = Mathf.Max(dimTimer, seconds);
        }

        /// <summary>True while the veil is smothered.</summary>
        public bool IsDimmed => dimTimer > 0f;

        public bool ReceiveHit(in DamageInfo info)
        {
            if (!IsAlive) return false;

            float attackerX = info.source != null ? info.source.transform.position.x : info.point.x;
            // a blow from a hostile reaching him (taken, blocked, parried or by the scarf) is an exchange: a fight
            if (!Model.IsInvulnerable) Hostiles.NoteHitFrom(info.source);
            int facing = player.Motor != null ? player.Motor.Facing : 1;
            if (Model.IsInvulnerable) return false;

            // Parry: the shield raised just as the blow lands turns it back — the attacker reels, the scarf feeds.
            if (veil != null && veil.InParryWindow && VeilCombatProfile.FromFront(facing, transform.position.x, attackerX))
            {
                ParryCount++;
                if (info.source != null && info.source.TryGetComponent<IStaggerable>(out var ps)) ps.Stagger();
                veil.Charge.Refund(0.3f);
                Model.GrantInvulnerability(0.3f);
                CombatFx.HitStop(0.14f);
                CombatFx.Shake(0.35f);
                CombatFx.Burst(transform.position + new Vector3(facing * 0.7f, 1.2f, 0f), new Vector2(facing, 0.6f), CombatFx.Crimson, 30, 6f);
                GameSound.Play(Cue.Parry);
                Parried?.Invoke();
                return false;
            }
            if (veil != null && VeilCombatProfile.ShieldBlocks(veil.Form, veil.Hardness, facing, transform.position.x, attackerX))
            {
                BlockCount++;
                player.Motor.ApplyKnockback(new Vector2(-facing * 3.5f, 0f), 0.12f);
                if (info.source != null && info.source.TryGetComponent<IStaggerable>(out var s)) s.Stagger();
                CombatFx.HitStop(0.07f);
                CombatFx.Shake(0.25f);
                CombatFx.Burst(transform.position + new Vector3(facing * 0.6f, 1.1f, 0f), new Vector2(facing, 0.3f), CombatFx.Crimson, 12, 4f);
                GameSound.Play(Cue.Block);
                Blocked?.Invoke();
                return false;
            }

            // The scarf at full length takes the blow for the hero — and is torn short for it.
            if (veil != null && veil.Form == VeilForm.Flow && info.amount > 0f && veil.Charge.TryAbsorbHit())
            {
                AbsorbCount++;
                veil.Interrupt(0.12f);   // the chain is broken all the same
                Model.GrantInvulnerability(0.6f);
                player.Motor.ApplyKnockback(info.knockback * 0.5f, 0.12f);
                CombatFx.HitStop(0.08f);
                CombatFx.Shake(0.35f);
                CombatFx.Burst(transform.position + Vector3.up * 1.4f, info.knockback, CombatFx.Crimson, 26, 5f);
                GameSound.Play(Cue.Absorb);
                CrimsonVeil.UI.HurtVignette.Pulse(0.45f);
                Absorbed?.Invoke();
                return false;
            }

            if (!Model.TryDamage(info)) return false;
            sinceHit = 0f;
            player.Motor.ApplyKnockback(info.knockback, stunTime);
            // struck, the hero's blow is broken off and his combo starts again - except a strike under the feet
            // driven down onto an enemy: bumping its body there is the point of it
            if (veil != null && !(info.kind == DamageKind.Contact && (veil.DownStrike || veil.ThrustingDown)))
                veil.Interrupt(stunTime);
            // struck: the world stops a moment longer than for a blow he lands, his body flashes crimson, the edges
            // of the picture run red, and he is heard to be hit
            CombatFx.HitStop(0.13f);
            CombatFx.Shake(0.6f);
            CombatFx.Burst(transform.position + Vector3.up * 1.1f, info.knockback, CombatFx.Crimson, 30, 6f);
            HitFlash.Flash(visualRoot != null ? visualRoot.gameObject : gameObject, HitFlash.Wound, HitFlash.HeroSeconds);
            CrimsonVeil.UI.HurtVignette.Pulse(1f);
            GameSound.Play(Cue.Hurt);

            if (Model.IsDead) StartCoroutine(DieAndRespawn());
            return true;
        }

        IEnumerator DieAndRespawn()
        {
            Died?.Invoke();
            if (veil != null)
            {
                // dead, the veil lets its weapon go and casts nothing until he is back
                veil.Interrupt(0f);
                veil.SetForm(VeilForm.Flow);
                veil.Disarmed = true;
            }
            CombatFx.Burst(transform.position + Vector3.up * 1f, Vector2.up, CombatFx.Crimson, 60, 7f);
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
            player.enabled = false;

            yield return new WaitForSeconds(respawnDelay);

            player.enabled = true;
            player.Respawn();
            Model.Restore();
            sinceHit = 999f;
            if (veil != null)
            {
                veil.SetForm(VeilForm.Flow);
                veil.Disarmed = false;
                veil.Vitality = 1f;
                veil.Charge.Restore();
                veil.Simulation.Reset(transform.position + Vector3.up * 1.5f);
            }
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
            Respawned?.Invoke();
        }
    }
}
