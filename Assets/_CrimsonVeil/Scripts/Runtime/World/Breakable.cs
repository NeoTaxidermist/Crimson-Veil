using CrimsonVeil.Combat;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Urns, crates, brittle stone: shatters into tumbling shards when hit.</summary>
    public sealed class Breakable : MonoBehaviour, IDamageable
    {
        [SerializeField] float hitPoints = 1f;
        [SerializeField] int shards = 9;
        [SerializeField] float shardSize = 0.18f;
        [SerializeField] float shardLife = 2.5f;

        public bool IsBroken { get; private set; }

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsBroken) return false;
            hitPoints -= info.amount;
            if (hitPoints > 0f)
            {
                CombatFx.Burst(info.point, info.knockback, CombatFx.Ink, 6, 3f);
                return true;
            }
            Break(info);
            return true;
        }

        void Break(in DamageInfo info)
        {
            IsBroken = true;
            var r = GetComponentInChildren<Renderer>();
            var mat = r != null ? r.sharedMaterial : null;
            var bounds = r != null ? r.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
            var rnd = new System.Random(GetInstanceID());

            for (int i = 0; i < shards; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.name = "Shard";
                s.layer = 2; // Ignore Raycast: shards never block enemies or arrows
                float size = shardSize * (0.5f + (float)rnd.NextDouble());
                s.transform.localScale = new Vector3(size, size * (0.6f + (float)rnd.NextDouble()), size * 0.7f);
                s.transform.position = bounds.center + Vector3.Scale(bounds.extents, new Vector3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f, 0.2f));
                s.transform.rotation = Random.rotation;
                if (mat != null) s.GetComponent<Renderer>().sharedMaterial = mat;
                var rb = s.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.linearVelocity = new Vector3(info.knockback.x * 0.6f + ((float)rnd.NextDouble() - 0.5f) * 4f, 3f + (float)rnd.NextDouble() * 4f, ((float)rnd.NextDouble() - 0.5f) * 1.5f);
                rb.angularVelocity = Random.insideUnitSphere * 12f;
                s.AddComponent<ShrinkAndDestroy>().Begin(shardLife);
            }

            CombatFx.Burst(bounds.center, Vector2.up, CombatFx.Ink, 20, 4f);
            CombatFx.Shake(0.15f);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var rr in GetComponentsInChildren<Renderer>()) rr.enabled = false;
            Destroy(gameObject, 0.1f);
        }
    }
}
