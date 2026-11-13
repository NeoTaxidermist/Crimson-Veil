using CrimsonVeil.Combat;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>A shard of hardened veil fired from the bow form. Damages or cuts what it hits.</summary>
    public sealed class VeilArrow : MonoBehaviour
    {
        const float Gravity = 4f;
        static readonly RaycastHit[] Hits = new RaycastHit[8];

        Vector3 velocity;
        float life = 2.5f;
        GameObject owner;

        public Vector3 Velocity => velocity;
        public bool HasHit { get; private set; }

        public static VeilArrow Spawn(Vector3 position, Vector3 velocity, Material material, GameObject owner = null)
        {
            var go = new GameObject("VeilArrow");
            go.transform.position = position;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) trail.sharedMaterial = material;
            trail.startColor = new Color(1f, 0.05f, 0.05f, 1f);
            trail.endColor = new Color(0.5f, 0f, 0f, 0f);

            var arrow = go.AddComponent<VeilArrow>();
            arrow.velocity = velocity;
            arrow.owner = owner;
            return arrow;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity.y -= Gravity * dt;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;

            if (dist > 0f && TryHit(step / dist, dist, out var hit))
            {
                transform.position = hit.point;
                HasHit = true;
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    var info = new DamageInfo
                    {
                        amount = VeilCombatProfile.Damage(VeilForm.Bow),
                        knockback = new Vector2(Mathf.Sign(velocity.x) * 3f, 1f),
                        point = hit.point,
                        kind = DamageKind.Arrow,
                        source = owner != null ? owner : gameObject,
                    };
                    if (target.ReceiveHit(info))
                    {
                        CombatFx.HitStop(0.04f);
                        CombatFx.Burst(hit.point, -velocity, CombatFx.Crimson, 14, 4f);
                    }
                }
                enabled = false;
                Destroy(gameObject, 1.2f);
                return;
            }

            transform.position += step;
            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }

        bool TryHit(Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(transform.position, dir, Hits, dist, ~0, QueryTriggerInteraction.Collide);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var h = Hits[i];
                if (owner != null && h.collider.transform.IsChildOf(owner.transform)) continue;
                // Pass through triggers that aren't damageable (e.g. checkpoints).
                if (h.collider.isTrigger && h.collider.GetComponentInParent<IDamageable>() == null) continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                    found = true;
                }
            }
            return found;
        }
    }
}
