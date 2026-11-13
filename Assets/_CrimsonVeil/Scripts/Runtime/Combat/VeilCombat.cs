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

        VeilController veil;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        readonly Collider[] buffer = new Collider[16];
        float lastProgress = -1f;

        void Awake() => veil = GetComponent<VeilController>();

        void Update()
        {
            float t = veil.AttackProgress;
            if (t < 0f)
            {
                lastProgress = -1f;
                return;
            }
            if (lastProgress < 0f || t < lastProgress) hitThisSwing.Clear();
            lastProgress = t;

            var form = veil.ShapeForm;
            if (!VeilCombatProfile.IsActive(form, t)) return;

            var sim = veil.Simulation;
            var p = sim.Positions;
            int n = sim.Count;
            int start = Mathf.Clamp(Mathf.FloorToInt(veil.HandFraction * (n - 1)), 0, n - 2);
            float radius = VeilCombatProfile.HitRadius(form);
            float z = transform.position.z;

            for (int i = start; i < n - 1; i++)
            {
                Vector3 a = p[i], b = p[i + 1];
                a.z = z; b.z = z;
                int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, buffer, hitMask, QueryTriggerInteraction.Collide);
                for (int k = 0; k < count; k++)
                {
                    var col = buffer[k];
                    if (col == null || col.transform.IsChildOf(transform)) continue;
                    var target = col.GetComponentInParent<IDamageable>();
                    if (target == null || hitThisSwing.Contains(target)) continue;
                    hitThisSwing.Add(target);

                    Vector3 point = col.ClosestPoint((a + b) * 0.5f);
                    var info = new DamageInfo
                    {
                        amount = VeilCombatProfile.Damage(form),
                        knockback = new Vector2(veil.Facing * VeilCombatProfile.Knockback(form), upwardKnock),
                        point = point,
                        kind = VeilCombatProfile.Kind(form),
                        source = gameObject,
                    };
                    if (target.ReceiveHit(info))
                    {
                        Hit?.Invoke(target, info);
                        CombatFx.HitStop(form == VeilForm.Spear || form == VeilForm.Shield ? 0.08f : 0.055f);
                        CombatFx.Shake(form == VeilForm.Shield ? 0.45f : 0.3f);
                        CombatFx.Burst(point, new Vector2(veil.Facing, 0.4f), CombatFx.Crimson, 22);
                    }
                }
            }
        }
    }
}
