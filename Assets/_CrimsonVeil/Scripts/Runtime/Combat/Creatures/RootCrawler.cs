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
        float stateTime, hopTimer, sinceLanding = 1f, tilt;
        bool onGround;
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
                    if (grounded && !onGround) sinceLanding = 0f;
                    onGround = grounded;
                    sinceLanding += dt;
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
            Animate(dt);
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

        void Animate(float dt)
        {
            if (visual == null) return;
            bool hanging = Current == State.Cling;
            // Leaping it leans into the leap; it squashes landing and crouching to leap, stretches in flight.
            bool skitter = Current == State.Skitter;
            float lean = skitter && !onGround ? Mathf.Clamp(-velocity.x * 4f, -24f, 24f) : 0f;
            tilt = Mathf.Lerp(tilt, lean, 1f - Mathf.Exp(-12f * dt));
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, (hanging ? 180f : 0f) + tilt), 0.2f);
            float sy = skitter ? CreatureAnimation.HopSquash(onGround, sinceLanding, Mathf.Max(0f, hopTimer), velocity.y)
                     : Current == State.Drop ? CreatureAnimation.HopSquash(false, 1f, 1f, velocity.y)
                     : hanging ? 1f + Mathf.Sin(Time.time * 2.4f) * 0.05f : 1f;
            float sx = 1f / Mathf.Sqrt(Mathf.Max(0.3f, sy));
            visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(sx, sy, sx), 1f - Mathf.Exp(-20f * dt));
            if (roots == null) return;
            float speed = Current == State.Pinned ? 18f : skitter ? 9f : 2f;
            // In the air the roots sweep back from the leap and gather; landing they splay out and grip.
            float gather = skitter && !onGround ? 0.45f : Current == State.Drop ? -0.25f : 0f;
            float splay = skitter && onGround && sinceLanding < 0.2f ? (1f - sinceLanding / 0.2f) * 0.1f : 0f;
            for (int i = 0; i < roots.Length; i++)
            {
                // fanned out away from the surface it clings to / stands on, never down into it
                float wiggle = Mathf.Sin(Time.time * speed + i * 1.7f) * (Current == State.Pinned ? 6f : RootCrawlerLogic.Wiggle);
                float a = RootCrawlerLogic.RootAngle(i, roots.Length) * (1f - gather + splay) - tilt * 0.6f;
                roots[i].localRotation = Quaternion.Euler(0f, 0f, a + wiggle);
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

        /// <summary>How far (degrees, either side of straight away from the surface) the roots fan out. The knot sits
        /// 0.25 m off the surface; measured on the model, past ~85° (fan + writhe) the bent roots dip under it.</summary>
        public const float Spread = 72f;

        /// <summary>How far each root writhes about its place in the fan.</summary>
        public const float Wiggle = 12f;

        /// <summary>The place of root <paramref name="i"/> of <paramref name="count"/> in the fan (degrees from straight
        /// away from the surface): evenly across it, the outer ones splayed out like legs bracing on the surface.</summary>
        public static float RootAngle(int i, int count)
        {
            if (count <= 1) return 0f;
            return Mathf.Lerp(-Spread, Spread, i / (float)(count - 1));
        }
    }
}
