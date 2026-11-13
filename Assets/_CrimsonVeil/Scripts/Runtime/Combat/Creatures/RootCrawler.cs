using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A knot of black roots. It clings to a ceiling (or overhang) and drops on the hero passing beneath,
    /// then skitters along the ground in short leaps. A spear thrust pins it: stunned, it takes double damage.
    /// Without a ceiling above its spawn it simply hunts on the ground.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class RootCrawler : CreatureBase
    {
        [SerializeField] float ceilingSearch = 7f;
        [SerializeField] float dropTrigger = 1.4f;
        [SerializeField] float skitterSpeed = 4.2f;
        [SerializeField] float pinTime = 2.4f;

        public enum State { Cling, Drop, Skitter, Pinned }
        public State Current { get; private set; }
        public int Facing { get; private set; } = 1;

        Vector3 velocity;
        float stateTime, hopTimer;
        Transform visual;
        Transform[] roots;

        protected override void Awake()
        {
            maxHealth = 2f;
            contactDamage = 0.5f;
            base.Awake();
            GetComponent<SphereCollider>().isTrigger = true;
            visual = CreatureParts.Find(transform, "Visual");
            if (visual != null)
            {
                var list = new System.Collections.Generic.List<Transform>();
                foreach (Transform t in visual) if (t.name.StartsWith("Root")) list.Add(t);
                roots = list.ToArray();
            }
        }

        protected override void Start()
        {
            base.Start();
            // Look for a ceiling to hang from.
            var p = transform.position + Vector3.up * 0.3f;
            if (Physics.Raycast(p, Vector3.up, out var hit, ceilingSearch, ~0, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point + Vector3.down * 0.3f;
                Current = State.Cling;
            }
            else Current = State.Skitter;
        }

        protected override void Tick(float dt)
        {
            if (IsDead) { Fall(dt); return; }
            stateTime += dt;
            var pos = transform.position;
            float dx = HasTarget ? target.Position.x - pos.x : 0f;
            if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0f ? 1 : -1;

            switch (Current)
            {
                case State.Cling:
                    if (HasTarget && RootCrawlerLogic.ShouldDrop(dx, target.Position.y - pos.y, dropTrigger))
                    {
                        Current = State.Drop; stateTime = 0f; velocity = Vector3.zero;
                    }
                    break;
                case State.Drop:
                    if (Fall(dt)) { Current = State.Skitter; stateTime = 0f; CombatFx.Burst(transform.position, Vector2.up, CombatFx.Ink, 12, 3f); }
                    break;
                case State.Skitter:
                {
                    // Short leaps towards the hero.
                    bool grounded = Fall(dt);
                    hopTimer -= dt;
                    if (grounded && hopTimer <= 0f && HasTarget && Mathf.Abs(dx) < 12f)
                    {
                        velocity = new Vector3(Facing * skitterSpeed, 4.5f, 0f);
                        hopTimer = 0.55f;
                    }
                    if (grounded && hopTimer > 0.3f) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 20f * dt);
                    break;
                }
                case State.Pinned:
                    Fall(dt);
                    velocity.x = 0f;
                    if (stateTime >= pinTime) { Current = State.Skitter; stateTime = 0f; }
                    break;
            }

            if (Current != State.Pinned && Current != State.Cling)
                TouchDamage(transform.position, 0.6f, contactDamage, new Vector2(Facing * 5f, 3f));
            Animate();
        }

        /// <summary>Gravity + ground; returns true when standing.</summary>
        bool Fall(float dt)
        {
            var pos = transform.position;
            velocity.y -= 25f * dt;
            Vector3 step = velocity * dt;
            // Don't run into walls.
            if (step.x != 0f && Physics.Raycast(pos, new Vector3(Mathf.Sign(step.x), 0f, 0f), 0.35f, ~0, QueryTriggerInteraction.Ignore))
            { step.x = 0f; velocity.x = 0f; }
            float g = GroundBelow(pos + Vector3.up * 0.2f, 0.6f - step.y);
            bool grounded = false;
            if (!float.IsNaN(g) && pos.y + step.y <= g + 0.25f && velocity.y <= 0f)
            {
                pos.y = g + 0.25f;
                velocity.y = 0f;
                grounded = true;
                step.y = 0f;
            }
            pos += step;
            transform.position = pos;
            return grounded;
        }

        protected override void OnHurt(in DamageInfo info)
        {
            if (Current == State.Cling) { Current = State.Drop; velocity = Vector3.zero; }
            if (info.kind == DamageKind.Pierce) { Current = State.Pinned; stateTime = 0f; velocity = Vector3.zero; }
            else velocity += new Vector3(info.knockback.x * 0.6f, 3f, 0f);
        }

        // Pinned, every blow lands twice as hard.
        protected override float DamageMultiplier(in DamageInfo info) => Current == State.Pinned ? 2f : 1f;

        void Animate()
        {
            if (visual == null) return;
            bool hanging = Current == State.Cling;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, hanging ? 180f : 0f), 0.2f);
            if (roots == null) return;
            float speed = Current == State.Pinned ? 18f : Current == State.Skitter ? 9f : 2f;
            for (int i = 0; i < roots.Length; i++)
            {
                float baseAngle = i * (360f / roots.Length);
                roots[i].localRotation = Quaternion.Euler(0f, 0f, baseAngle + Mathf.Sin(Time.time * speed + i * 1.7f) * (Current == State.Pinned ? 6f : 18f));
            }
        }

        public static void BuildVisual(Transform root, Material ink, Material accent)
        {
            var v = CreatureParts.Group(root, "Visual", Vector3.zero);
            CreatureParts.Part(v, PrimitiveType.Sphere, "Knot", Vector3.zero, new Vector3(0.42f, 0.36f, 0.36f), ink);
            for (int i = 0; i < 9; i++)
            {
                var arm = CreatureParts.Group(v, "Root" + i, Vector3.zero, new Vector3(0f, 0f, i * 40f));
                float len = 0.45f + 0.18f * ((i * 7) % 3);
                CreatureParts.Part(arm, PrimitiveType.Cube, "Seg", new Vector3(0f, len * 0.5f, 0f), new Vector3(0.05f, len, 0.05f), ink, new Vector3(0f, 0f, (i % 2 == 0 ? 12f : -12f)));
                CreatureParts.Part(arm, PrimitiveType.Cube, "Tip", new Vector3(0.06f, len * 0.95f, 0f), new Vector3(0.03f, len * 0.4f, 0.03f), ink, new Vector3(0f, 0f, -35f));
            }
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeL", new Vector3(0.08f, 0.04f, -0.18f), Vector3.one * 0.07f, accent);
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeR", new Vector3(-0.06f, 0.06f, -0.18f), Vector3.one * 0.05f, accent);
        }
    }

    /// <summary>Root crawler rules, engine independent.</summary>
    public static class RootCrawlerLogic
    {
        /// <summary>Drop when the hero passes underneath (close in x, below it).</summary>
        public static bool ShouldDrop(float dx, float dy, float trigger) => Mathf.Abs(dx) < trigger && dy < -0.5f && dy > -9f;
    }
}
