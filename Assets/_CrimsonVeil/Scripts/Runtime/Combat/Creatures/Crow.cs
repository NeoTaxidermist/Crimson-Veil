using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>One ink crow of a <see cref="CrowSwarm"/>. A single blow downs it.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Crow : MonoBehaviour, IDamageable
    {
        public bool IsDead { get; private set; }
        public CrowSwarm Swarm { get; set; }
        /// <summary>Own phase / radius around the swarm centre.</summary>
        public float Phase, Radius, Speed;
        public Vector3 Velocity;

        Transform wingL, wingR;
        float fallVelocity;

        void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            wingL = CreatureParts.Find(transform, "WingL");
            wingR = CreatureParts.Find(transform, "WingR");
        }

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsDead) return false;
            IsDead = true;
            GetComponent<SphereCollider>().enabled = false;
            fallVelocity = 2f;
            CombatFx.Burst(transform.position, info.knockback.normalized, CombatFx.Ink, 10, 3.5f);
            if (Swarm != null) Swarm.OnCrowDown(this);
            return true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (IsDead)
            {
                // Tumbles down and fades.
                fallVelocity -= 18f * dt;
                transform.position += Vector3.up * fallVelocity * dt;
                transform.Rotate(0f, 0f, 540f * dt);
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, dt * 0.8f);
                if (transform.localScale.x <= 0.01f) Destroy(gameObject);
                return;
            }
            // Flap: fast when climbing, gliding when diving.
            // Up (0°) … down past the body (~150°); a diving crow holds its wings half-closed.
            bool diving = Velocity.y < -2f;
            float flap = diving ? 60f + Mathf.Sin(Time.time * 6f + Phase) * 10f : 75f + Mathf.Sin(Time.time * 22f + Phase * 7f) * 75f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(-flap, 0f, 0f);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(flap, 0f, 0f);
            if (Mathf.Abs(Velocity.x) > 0.2f)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * Mathf.Sign(Velocity.x);
                transform.localScale = s;
            }
        }

        public static void BuildVisual(Transform root, Material ink, Material accent)
        {
            CreatureParts.Part(root, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.14f, 0.2f, 0.12f), ink, new Vector3(0f, 0f, 90f));
            CreatureParts.Part(root, PrimitiveType.Sphere, "Head", new Vector3(0.17f, 0.04f, 0f), Vector3.one * 0.11f, ink);
            CreatureParts.Part(root, PrimitiveType.Cube, "Beak", new Vector3(0.26f, 0.03f, 0f), new Vector3(0.09f, 0.025f, 0.025f), ink);
            CreatureParts.Part(root, PrimitiveType.Sphere, "Eye", new Vector3(0.2f, 0.07f, -0.05f), Vector3.one * 0.03f, accent);
            CreatureParts.Part(root, PrimitiveType.Cube, "Tail", new Vector3(-0.2f, 0f, 0f), new Vector3(0.14f, 0.02f, 0.08f), ink, new Vector3(0f, 0f, 8f));
            // Wings in the picture plane, rooted at the back; flapping swings them up and down past the body.
            var l = CreatureParts.Group(root, "WingL", new Vector3(0f, 0.04f, -0.03f));
            CreatureParts.Part(l, PrimitiveType.Cube, "Feathers", new Vector3(-0.03f, 0.17f, 0f), new Vector3(0.16f, 0.34f, 0.015f), ink, new Vector3(0f, 0f, 12f));
            var r = CreatureParts.Group(root, "WingR", new Vector3(0f, 0.04f, 0.03f));
            CreatureParts.Part(r, PrimitiveType.Cube, "Feathers", new Vector3(-0.03f, 0.17f, 0f), new Vector3(0.16f, 0.34f, 0.015f), ink, new Vector3(0f, 0f, 12f));
        }
    }
}
