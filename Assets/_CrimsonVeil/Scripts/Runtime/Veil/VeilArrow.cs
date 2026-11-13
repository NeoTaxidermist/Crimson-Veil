using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>A shard of hardened veil fired from the bow form.</summary>
    public sealed class VeilArrow : MonoBehaviour
    {
        Vector3 velocity;
        float life = 2.5f;
        const float Gravity = 4f;

        public static VeilArrow Spawn(Vector3 position, Vector3 velocity, Material material)
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
            return arrow;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity.y -= Gravity * dt;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            if (dist > 0f && Physics.Raycast(transform.position, step / dist, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                enabled = false;
                Destroy(gameObject, 1.2f);
                return;
            }
            transform.position += step;

            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
