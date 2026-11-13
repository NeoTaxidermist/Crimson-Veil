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
        public Vector3 LureTarget { get; private set; }

        Light glow;
        Vector3 home;
        float flicker;

        protected override void Awake()
        {
            maxHealth = 1f;
            contactDamage = 0f;
            base.Awake();
            GetComponent<SphereCollider>().isTrigger = true;
            glow = GetComponentInChildren<Light>();
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
            transform.position = Vector3.MoveTowards(pos, want, speed * dt);
        }

        /// <summary>Any hit catches it instead of hurting it.</summary>
        protected override bool AcceptHit(in DamageInfo info)
        {
            if (!IsCaught)
            {
                IsCaught = true;
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
