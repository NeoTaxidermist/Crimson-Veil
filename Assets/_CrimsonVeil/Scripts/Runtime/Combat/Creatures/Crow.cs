using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>One ink crow of a <see cref="CrowSwarm"/>. A single blow downs it.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Crow : MonoBehaviour, IDamageable, IStaggerable
    {
        /// <summary>How far (m) the crow is flung off its place in the swarm right now (it flies back to it).</summary>
        public Vector3 Fling;
        /// <summary>How far (m) a crow whose peck hits the hero's shield is flung away.</summary>
        public const float ShieldFling = 2.4f;

        /// <summary>
        /// Its peck hit the hero's shield: the crow is flung away off it, tumbling, and the swarm scatters a moment
        /// (v0.109.1; before, the shield stopped the peck but the crow flew on as if nothing had happened).
        /// </summary>
        public void Stagger()
        {
            if (IsDead) return;
            Vector3 away = Swarm != null ? transform.position - Swarm.HeroChest : Vector3.up;
            away.z = 0f;
            if (away.sqrMagnitude < 1e-4f) away = Vector3.up;
            Fling = (away.normalized + Vector3.up * 0.4f).normalized * ShieldFling;
            CombatFx.Burst(transform.position, new Vector2(away.x, away.y), CombatFx.Ink, 8, 3f);
            if (Swarm != null) Swarm.OnCrowStaggered(this);
        }
        public bool IsDead { get; private set; }
        public CrowSwarm Swarm { get; set; }
        /// <summary>Own phase / radius around the swarm centre.</summary>
        public float Phase, Radius, Speed;
        public Vector3 Velocity;

        Transform wingL, wingR;
        float fallVelocity;
        int heading = 1;
        CreatureTurn turn = new CreatureTurn(1);

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
            // Flap: a quick power stroke down, slower back up; labouring up, gliding down; stooping in a dive the wings
            // are tucked half-closed. Up (0) .. down past the body (~150).
            float windup = Swarm != null ? Swarm.DiveWindup : 0f;
            bool diving = Velocity.y < -2f;
            wingPhase += dt * FlyerAnimation.BeatRate(3.4f, Velocity.y) * (1f + 0.6f * windup);
            float stroke = FlyerAnimation.Stroke(wingPhase + Phase);
            float flap = diving ? Mathf.Lerp(flap0, 95f, 1f - Mathf.Exp(-10f * dt)) + Mathf.Sin(Time.time * 30f + Phase) * 4f
                                : Mathf.Lerp(5f, 5f + 145f * FlyerAnimation.StrokeWidth(Velocity.y), stroke);
            flap0 = flap;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(-flap, 0f, 0f);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(flap, 0f, 0f);
            // Banks round in a quick 3D turn, not a mirrored flip; keep the last heading while hovering. The body pitches
            // into its flight: nose down in the dive, up as it climbs out.
            if (Mathf.Abs(Velocity.x) > 0.2f) heading = Velocity.x > 0f ? 1 : -1;
            pitch = Mathf.Lerp(pitch, FlyerAnimation.Pitch(Velocity, 55f), 1f - Mathf.Exp(-10f * dt));
            transform.localRotation = turn.Step(heading, dt, 0.18f) * Quaternion.Euler(0f, 0f, pitch);
        }

        float wingPhase, flap0, pitch;

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
