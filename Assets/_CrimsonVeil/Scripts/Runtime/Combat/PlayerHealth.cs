using System;
using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Hero's life. There is no HUD: the veil itself thins and dims as health drops.
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

        public event Action Blocked;
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
            if (veil != null) veil.Vitality = Mathf.MoveTowards(veil.Vitality, Model.Fraction * (1f - dim), dt * 2f);
        }

        float dimTimer, dimAmount;

        /// <summary>Ash and the like smother the veil: its glow (and with it the hero's strength) is dimmed for a while.</summary>
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
            int facing = player.Motor != null ? player.Motor.Facing : 1;
            if (veil != null && VeilCombatProfile.ShieldBlocks(veil.Form, veil.Hardness, facing, transform.position.x, attackerX))
            {
                BlockCount++;
                player.Motor.ApplyKnockback(new Vector2(-facing * 3.5f, 0f), 0.12f);
                if (info.source != null && info.source.TryGetComponent<IStaggerable>(out var s)) s.Stagger();
                CombatFx.HitStop(0.05f);
                CombatFx.Shake(0.25f);
                CombatFx.Burst(transform.position + new Vector3(facing * 0.6f, 1.1f, 0f), new Vector2(facing, 0.3f), CombatFx.Crimson, 12, 4f);
                Blocked?.Invoke();
                return false;
            }

            if (!Model.TryDamage(info)) return false;
            sinceHit = 0f;
            player.Motor.ApplyKnockback(info.knockback, stunTime);
            CombatFx.HitStop(0.09f);
            CombatFx.Shake(0.6f);
            CombatFx.Burst(transform.position + Vector3.up * 1.1f, info.knockback, CombatFx.Ink, 16);

            if (Model.IsDead) StartCoroutine(DieAndRespawn());
            return true;
        }

        IEnumerator DieAndRespawn()
        {
            Died?.Invoke();
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
                veil.Vitality = 1f;
                veil.Simulation.Reset(transform.position + Vector3.up * 1.5f);
            }
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
            Respawned?.Invoke();
        }
    }
}
