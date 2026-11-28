using System;
using System.Collections.Generic;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Turns the hardened veil into a weapon: during the active part of a swing, every segment of the
    /// weapon part of the ribbon sweeps a capsule and damages what it touches (once per swing).
    /// Hits use the simulated ribbon, so what you see is what hits.
    /// </summary>
    [DefaultExecutionOrder(55)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilCombat : MonoBehaviour
    {
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float upwardKnock = 2.5f;

        public event Action<IDamageable, DamageInfo> Hit;
        public int HitsThisSwing => hitThisSwing.Count;
        /// <summary>How many of the hero's blows cut only air (for tests).</summary>
        public int Missed { get; private set; }

        VeilController veil;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        readonly Collider[] buffer = new Collider[16];
        float lastProgress = -1f;

        void Awake() => veil = GetComponent<VeilController>();

        void OnEnable() => Deflection.Blocked += OnBlocked;
        void OnDisable() => Deflection.Blocked -= OnBlocked;

        /// <summary>Recoil (m/s) the hero is thrown back with when his blow is stopped by a shield, and how long he reels.</summary>
        public const float DeflectRecoil = 4.5f, DeflectReel = 0.18f;

        /// <summary>How many of the hero's blows were stopped by a shield (for tests).</summary>
        public int Deflected { get; private set; }

        /// <summary>
        /// The blow struck a shield: it rings off it, sparks fly back, and the hero is thrown back a step with the blow
        /// broken off (it does not go on through the shield). (Until v0.109: a few sparks and the blade went on.)
        /// </summary>
        void OnBlocked(GameObject blocker, DamageInfo info)
        {
            if (info.source != gameObject) return;
            Deflected++;
            blockedThisSwing = true;
            CombatFx.Deflect(info.point, veil.Facing);
            var player = GetComponent<CrimsonVeil.Player.PlayerController>();
            if (player != null && player.Motor != null)
                player.Motor.ApplyKnockback(new Vector2(-veil.Facing * DeflectRecoil, player.IsGrounded ? 0f : 1.5f), DeflectReel);
            veil.Interrupt(DeflectReel);
        }

        bool blockedThisSwing, whiffChecked;

        void Update()
        {
            float t = veil.AttackProgress;
            if (t < 0f)
            {
                lastProgress = -1f;
                return;
            }
            bool newSwing = lastProgress < 0f || t < lastProgress;
            if (newSwing) { hitThisSwing.Clear(); blockedThisSwing = false; whiffChecked = false; }
            lastProgress = t;

            var form = veil.ShapeForm;
            var sim = veil.Simulation;
            var p = sim.Positions;
            int n = sim.Count;
            if (!VeilCombatProfile.IsActive(form, t))
            {
                // the cut is over and touched nothing: a miss, shown (not punished) - a streak along the way the tip
                // was going
                if (!whiffChecked && form != VeilForm.Bow && t > VeilCombatProfile.ActiveWindow(form).y)
                {
                    whiffChecked = true;
                    if (hitThisSwing.Count == 0 && !blockedThisSwing && n > 1)
                    {
                        Vector3 tip = p[n - 1];
                        tip.z = transform.position.z;
                        Vector2 along = sweptValid && swept != null && swept.Length == n
                            ? new Vector2(tip.x - swept[n - 1].x, tip.y - swept[n - 1].y) : Vector2.zero;
                        if (along.sqrMagnitude < 1e-6f) along = new Vector2(veil.Facing, 0f);
                        Missed++;
                        CombatFx.Whiff(tip, along);
                    }
                }
                sweptValid = false;
                return;
            }

            int start = Mathf.Clamp(Mathf.FloorToInt(veil.HandFraction * (n - 1)), 0, n - 2);
            float radius = VeilCombatProfile.HitRadius(form);
            float z = transform.position.z;
            if (swept == null || swept.Length != n) { swept = new Vector3[n]; sweptValid = false; }

            // The blade along its length this frame, and - so a fast tip at a low frame rate doesn't pass over a thin
            // foe between two frames - each of its joints along the way it came since the last one.
            for (int i = start; i < n - 1; i++)
            {
                Vector3 a = p[i], b = p[i + 1];
                a.z = z; b.z = z;
                Strike(a, b, radius, form, z);
            }
            if (sweptValid && !newSwing)
                for (int i = start + 1; i < n; i++)
                {
                    Vector3 was = swept[i], now = p[i];
                    was.z = z; now.z = z;
                    if ((now - was).sqrMagnitude > radius * radius) Strike(was, now, radius, form, z);
                }
            for (int i = 0; i < n; i++) swept[i] = p[i];
            sweptValid = true;
        }

        Vector3[] swept;
        bool sweptValid;

        /// <summary>Damages whatever the capsule from <paramref name="a"/> to <paramref name="b"/> touches (once a swing).</summary>
        void Strike(Vector3 a, Vector3 b, float radius, VeilForm form, float z)
        {
            {
                int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, buffer, hitMask, QueryTriggerInteraction.Collide);
                for (int k = 0; k < count; k++)
                {
                    var col = buffer[k];
                    if (col == null || col.transform.IsChildOf(transform)) continue;
                    var target = col.GetComponentInParent<IDamageable>();
                    if (target == null || hitThisSwing.Contains(target)) continue;
                    hitThisSwing.Add(target);

                    Vector3 point = col.ClosestPoint((a + b) * 0.5f);
                    var blow = veil.Combo.Current;
                    var info = new DamageInfo
                    {
                        amount = VeilCombatProfile.Damage(form) * blow.DamageScale,
                        // (a rising strike out of the crouch throws it up rather than away)
                        knockback = veil.Rising
                            ? new Vector2(veil.Facing * VeilCombatProfile.Knockback(form) * 0.3f, VeilController.RiseKnock * (blow.finisher ? 1.2f : 1f))
                            : new Vector2(veil.Facing * VeilCombatProfile.Knockback(form) * blow.KnockbackScale,
                                          upwardKnock * (blow.finisher ? 1.4f : 1f)),
                        point = point,
                        kind = VeilCombatProfile.Kind(form),
                        source = gameObject,
                    };
                    if (target.ReceiveHit(info))
                    {
                        veil.NotifyHitLanded(); // feeds the scarf; a downward spear bounces the hero up
                        if (col.GetComponentInParent<IHostile>() != null) Hostiles.NoteExchange();
                        Hit?.Invoke(target, info);
                        // the finisher lands heavier: a longer stop, a harder shake, a bigger spray, a deeper sound
                        float heavy = blow.finisher ? 1.6f : 1f;
                        CombatFx.HitStop(CombatFx.HitStopFor(form, blow.finisher));
                        CombatFx.Shake((form == VeilForm.Shield ? 0.45f : 0.32f) * heavy);
                        // (the spray flies on with the blade, through depth too)
                        CombatFx.Burst(point, new Vector2(veil.Facing, 0.4f), CombatFx.Crimson, blow.finisher ? 56 : 34, 8f,
                                       VeilWeaponPose.SweepDirection(form, veil.AttackProgress));
                        // the cut: a stroke through the point struck, along the blade's way
                        Vector2 along = new Vector2(b.x - a.x, b.y - a.y);
                        Vector2 across = new Vector2(-along.y, along.x);
                        CombatFx.Slash(point, form == VeilForm.Spear ? new Vector2(veil.Facing, 0f) : across,
                                       blow.finisher ? 1.6f : 1.1f, blow.finisher ? 0.16f : 0.11f);
                        GameSound.Cut(GameSound.SwingOf(form));   // (the blade stops in it: no more whoosh)
                        GameSound.Play(blow.finisher ? Cue.HitHeavy : Cue.Hit);
                    }
                }
            }
        }
    }
}
