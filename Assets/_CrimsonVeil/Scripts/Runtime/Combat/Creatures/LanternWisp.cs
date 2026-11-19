using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A will-o'-the-wisp in a little iron lantern. It is harmless itself — it lures: it drifts ahead of the hero
    /// and hangs, glowing invitingly, over the nearest drop in his path. Strike it and it is caught: it stops
    /// and burns bright and steady where it is, a light for the dark (and for puzzles).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class LanternWisp : CreatureBase
    {
        [SerializeField] float sightRange = 10f;
        [SerializeField] float hover = 1.6f;
        [SerializeField] float speed = 2.2f;
        [SerializeField] float lookAhead = 10f;

        public bool IsCaught { get; private set; }

        /// <summary>Harmless: it lures and lights, it never makes a fight.</summary>
        public override bool Fights => false;
        public Vector3 LureTarget { get; private set; }

        Light glow;
        Vector3 home;
        float flicker;
        Transform visual, flame;
        Vector3 flameRest, lastVelocity;
        float swing, swingVelocity, caughtJolt;

        /// <summary>Where the lantern hangs from (its ring, above the flame).</summary>
        static readonly Vector3 Ring = new Vector3(0f, 0.29f, 0f);

        /// <summary>The lantern swings from its ring behind the way it is carried; the flame breathes and gutters.</summary>
        void Animate(Vector3 velocity, float dt)
        {
            Vector3 accel = (velocity - lastVelocity) / Mathf.Max(dt, 1e-4f);
            lastVelocity = velocity;
            FlyerAnimation.Pendulum(ref swing, ref swingVelocity, Mathf.Clamp(accel.x, -30f, 30f), dt);
            caughtJolt = Mathf.MoveTowards(caughtJolt, 0f, dt * 1.5f);
            if (visual != null)
            {
                var rot = Quaternion.Euler(0f, 0f, -swing);
                visual.localRotation = rot;
                visual.localPosition = Ring - rot * Ring;   // turning about the ring, not the flame
            }
            if (flame != null)
            {
                float breathe = IsCaught ? 1.15f + caughtJolt * 0.6f : 0.85f + 0.3f * flicker + Mathf.Sin(Time.time * 9f) * 0.05f;
                // a quick swing drags the flame out long
                float drag = 1f + Mathf.Min(0.5f, Mathf.Abs(swingVelocity) * 0.004f);
                flame.localScale = new Vector3(flameRest.x * breathe / Mathf.Sqrt(drag), flameRest.y * breathe * drag, flameRest.z * breathe / Mathf.Sqrt(drag));
            }
        }

        protected override void Awake()
        {
            maxHealth = 1f;
            contactDamage = 0f;
            base.Awake();
            GetComponent<SphereCollider>().isTrigger = true;
            glow = GetComponentInChildren<Light>();
            visual = CreatureParts.Find(transform, "Visual");
            flame = CreatureParts.Find(transform, "Flame");
            if (flame != null) flameRest = flame.localScale;
            home = LureTarget = transform.position;
        }

        protected override void Tick(float dt)
        {
            var pos = transform.position;
            float t = Time.time;
            flicker = Mathf.Lerp(flicker, Random.Range(0.75f, 1.15f), 1f - Mathf.Exp(-12f * dt));
            if (glow != null) glow.intensity = (IsCaught ? 4.5f : 2.2f) * (IsCaught ? 1f : flicker);
            if (IsCaught)
            {
                transform.position = pos + Vector3.up * Mathf.Sin(t * 1.5f) * 0.002f;
                Animate(Vector3.zero, dt);
                return;
            }

            if (HasTarget && Mathf.Abs(target.Position.x - pos.x) < sightRange)
            {
                // It always stays ahead of the hero, on the side it is on.
                int dir = pos.x >= target.Position.x ? 1 : -1;
                // Find the first drop ahead and wait above it.
                float from = target.Position.x;
                LureTarget = LanternWispLogic.FindDrop(from, dir, lookAhead, 0.5f, x => GroundBelow(new Vector3(x, target.Position.y + 3f, 0f), 12f), target.Position.y, out var dropX)
                    ? new Vector3(dropX + dir * 0.8f, target.Position.y + hover, 0f)
                    : new Vector3(from + dir * 4f, target.Position.y + hover, 0f);
            }
            else LureTarget = home;

            Vector3 want = LureTarget + new Vector3(Mathf.Sin(t * 0.9f) * 0.3f, Mathf.Sin(t * 1.7f) * 0.25f, 0f);
            var next = CreatureMotion.Slide(pos, Vector3.MoveTowards(pos, want, speed * dt) - pos, 0.3f, transform);
            transform.position = next;
            Animate((next - pos) / dt, dt);
        }

        /// <summary>Any hit catches it instead of hurting it.</summary>
        protected override bool AcceptHit(in DamageInfo info)
        {
            if (!IsCaught)
            {
                IsCaught = true;
                caughtJolt = 1f;
                swingVelocity += Mathf.Sign(info.knockback.x + 1e-3f) * -260f;   // struck, it swings hard, then stills
                CombatFx.Burst(transform.position, Vector2.up, CombatFx.Crimson, 16, 2f);
            }
            return false;
        }

        public static void BuildVisual(Transform root, Material ink, Material accent)
        {
            var v = CreatureParts.Group(root, "Visual", Vector3.zero);
            CreatureParts.Part(v, PrimitiveType.Sphere, "Flame", Vector3.zero, Vector3.one * 0.18f, accent);
            CreatureParts.Part(v, PrimitiveType.Cube, "Top", new Vector3(0f, 0.2f, 0f), new Vector3(0.26f, 0.04f, 0.26f), ink);
            CreatureParts.Part(v, PrimitiveType.Cube, "Bottom", new Vector3(0f, -0.2f, 0f), new Vector3(0.26f, 0.04f, 0.26f), ink);
            CreatureParts.Part(v, PrimitiveType.Cube, "BarL", new Vector3(-0.12f, 0f, 0f), new Vector3(0.025f, 0.4f, 0.025f), ink);
            CreatureParts.Part(v, PrimitiveType.Cube, "BarR", new Vector3(0.12f, 0f, 0f), new Vector3(0.025f, 0.4f, 0.025f), ink);
            CreatureParts.Part(v, PrimitiveType.Cube, "Ring", new Vector3(0f, 0.29f, 0f), new Vector3(0.1f, 0.12f, 0.02f), ink);
            BuildLight(v);
        }

        /// <summary>The lantern's glow (works with any visual, e.g. the Blender model).</summary>
        public static void BuildLight(Transform v)
        {
            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(v, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.82f, 0.75f);
            light.shadows = LightShadows.None;
        }
    }

    /// <summary>Lantern wisp rules, engine independent.</summary>
    public static class LanternWispLogic
    {
        /// <summary>
        /// Walks from <paramref name="fromX"/> in <paramref name="dir"/> and returns the first x where the ground falls
        /// away (no ground, or more than 2.5 m below <paramref name="floorY"/>).
        /// </summary>
        public static bool FindDrop(float fromX, int dir, float range, float step, System.Func<float, float> groundAt, float floorY, out float dropX)
        {
            for (float d = step; d <= range; d += step)
            {
                float x = fromX + dir * d;
                float g = groundAt(x);
                if (float.IsNaN(g) || floorY - g > 2.5f) { dropX = x; return true; }
            }
            dropX = fromX;
            return false;
        }
    }
}
