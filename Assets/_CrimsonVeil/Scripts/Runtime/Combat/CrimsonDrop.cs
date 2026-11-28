using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A drop of crimson a slain foe sheds (<see cref="Spill"/>): it bursts up, falls to the ground, and once the hero
    /// comes near it is drawn to him and taken (<see cref="HeroProgress.AddCrimson"/>). The one colour of the world, so
    /// it is seen in the grey at a glance.
    /// </summary>
    public sealed class CrimsonDrop : MonoBehaviour
    {
        /// <summary>From this far (m) a drop on the ground is drawn to the hero; this close it is taken.</summary>
        public const float DrawnFrom = 2.6f, TakenAt = 0.55f;
        /// <summary>Seconds a fresh drop flies before it can be drawn (so the burst is seen).</summary>
        public const float Settle = 0.35f;

        /// <summary>How much crimson the drop is worth.</summary>
        public int Worth = 1;
        Vector3 velocity;
        float age;
        bool grounded;
        CombatTarget target;
        static Material material;

        /// <summary>A slain foe's crimson: <paramref name="count"/> drops burst out of <paramref name="at"/>.</summary>
        public static void Spill(Vector3 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var d = Make(at);
                float a = Random.Range(-1f, 1f);
                d.velocity = new Vector3(a * 3f, Random.Range(4f, 7f), 0f);
            }
        }

        /// <summary>A drop at <paramref name="at"/>, worth <paramref name="worth"/>.</summary>
        public static CrimsonDrop Make(Vector3 at, int worth = 1)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "CrimsonDrop";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.localScale = Vector3.one * 0.14f;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var d = go.AddComponent<CrimsonDrop>();
            d.Worth = worth;
            return d;
        }

        public static Material Material
        {
            get
            {
                if (material != null) return material;
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                material = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "CrimsonDrop" };
                var c = new Color(0.95f, 0.05f, 0.08f, 1f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
                material.color = c;
                return material;
            }
        }

        void Start() => target = CombatTarget.Find();

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (!target.IsValid) target = CombatTarget.Find();
            Vector3 pos = transform.position;
            Vector3 chest = target.IsValid ? target.Position + Vector3.up * 0.9f : pos;
            Vector3 to = chest - pos; to.z = 0f;
            bool drawn = target.IsValid && target.IsAlive && age > Settle && to.magnitude < DrawnFrom;
            if (drawn)
            {
                // drawn to him, faster the nearer it comes
                float speed = Mathf.Lerp(14f, 5f, to.magnitude / DrawnFrom);
                pos = Vector3.MoveTowards(pos, chest, speed * dt);
                if ((chest - pos).magnitude < TakenAt)
                {
                    HeroProgress.AddCrimson(Worth);
                    CombatFx.Burst(chest, Vector2.up, CombatFx.Crimson, 4, 1.5f);
                    Destroy(gameObject);
                    return;
                }
                grounded = false;
            }
            else if (!grounded)
            {
                velocity.y -= 22f * dt;
                Vector3 step = velocity * dt;
                if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out var hit, 0.1f - step.y + 0.08f, ~0, QueryTriggerInteraction.Ignore)
                    && velocity.y <= 0f && !(hit.collider is CharacterController))
                {
                    pos.y = hit.point.y + 0.08f;
                    velocity = Vector3.zero;
                    grounded = true;
                }
                else pos += step;
                if (pos.y < -60f) { Destroy(gameObject); return; }
            }
            // a drop on the ground pulses faintly, to be seen in the grass
            if (grounded) transform.localScale = Vector3.one * (0.14f + 0.02f * Mathf.Sin(Time.time * 5f + pos.x));
            transform.position = pos;
        }
    }

    /// <summary>
    /// The crimson the hero carried when he died, left where he fell (<see cref="HeroProgress.DeathLeavesCrimson"/>):
    /// a pool of drops he can fetch back. Dying again before he does loses it.
    /// </summary>
    public sealed class CrimsonCache : MonoBehaviour
    {
        public static CrimsonCache Current { get; private set; }
        public int Worth;
        CombatTarget target;

        /// <summary>Leaves <paramref name="worth"/> crimson at <paramref name="at"/> (the one left before is lost).</summary>
        public static CrimsonCache Leave(Vector3 at, int worth)
        {
            if (Current != null) Destroy(Current.gameObject);
            Current = null;
            if (worth <= 0) return null;
            var go = new GameObject("CrimsonCache");
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var c = go.AddComponent<CrimsonCache>();
            c.Worth = worth;
            // drawn as a little heap of drops
            for (int i = 0; i < 5; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(d.GetComponent<Collider>());
                d.transform.SetParent(go.transform, false);
                d.transform.localPosition = new Vector3((i - 2) * 0.12f, 0.08f + (i % 2) * 0.1f, 0f);
                d.transform.localScale = Vector3.one * 0.16f;
                d.GetComponent<MeshRenderer>().sharedMaterial = CrimsonDrop.Material;
            }
            Current = c;
            return c;
        }

        void Start() => target = CombatTarget.Find();

        void Update()
        {
            if (!target.IsValid) { target = CombatTarget.Find(); return; }
            if (!target.IsAlive) return;
            Vector3 d = target.Position - transform.position;
            if (Mathf.Abs(d.x) < 0.8f && Mathf.Abs(d.y) < 1.6f)
            {
                HeroProgress.AddCrimson(Worth);
                CombatFx.Burst(transform.position + Vector3.up * 0.4f, Vector2.up, CombatFx.Crimson, 24, 4f);
                if (Current == this) Current = null;
                Destroy(gameObject);
                return;
            }
            transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 3f));
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }

    /// <summary>
    /// A shard of a life, hidden in a secret place: a turning crimson splinter. Every <see cref="HeroProgress.ShardsPerHeart"/>
    /// make one more life (and fill them all).
    /// </summary>
    public sealed class HeartShard : MonoBehaviour
    {
        Transform gem;
        CombatTarget target;

        void Awake()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Gem";
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Vector3.up * 0.6f;
            g.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
            g.GetComponent<MeshRenderer>().sharedMaterial = CrimsonDrop.Material;
            gem = g.transform;
        }

        void Start() => target = CombatTarget.Find();

        void Update()
        {
            if (gem != null)
            {
                gem.localRotation = Quaternion.Euler(0f, Time.time * 90f, 45f);
                gem.localPosition = Vector3.up * (0.6f + 0.08f * Mathf.Sin(Time.time * 2.2f));
            }
            if (!target.IsValid) { target = CombatTarget.Find(); return; }
            if (!target.IsAlive) return;
            Vector3 d = target.Position + Vector3.up * 0.9f - (transform.position + Vector3.up * 0.6f);
            d.z = 0f;
            if (d.magnitude > 0.9f) return;
            bool life = HeroProgress.AddShard();
            if (life) target.Health.GrowByOneLife();
            CombatFx.Burst(transform.position + Vector3.up * 0.6f, Vector2.up, CombatFx.Crimson, life ? 60 : 24, life ? 7f : 4f);
            GameSound.Play(Cue.Absorb);
            Destroy(gameObject);
        }
    }
}
