using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A hanged man swinging on a crimson rope. He swings himself at a hero who comes near (contact hurts);
    /// blows to the body only push him. Cut the rope (arrow or any veil weapon) and he drops — his body stays,
    /// a solid weight to stand on or to load a puzzle with.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class HangedMan : CreatureBase
    {
        [SerializeField] float ropeLength = 3.2f;
        [SerializeField] float reach = 4.5f;
        [SerializeField] float pump = 0.9f;
        [SerializeField] float damping = 0.25f;

        public bool Fallen { get; private set; }
        public float Angle => angle;

        Vector3 pivot;
        float angle, omega, fallVelocity;
        CreatureThread rope;
        Transform visual;
        bool landed;

        protected override void Awake()
        {
            maxHealth = 1f;
            contactDamage = 0.5f;
            base.Awake();
            var cap = GetComponent<CapsuleCollider>();
            cap.isTrigger = true;
            visual = CreatureParts.Find(transform, "Visual");
            // Built standing (feet at the root) for the editor; at runtime the root is his neck.
            cap.center = new Vector3(0f, -0.85f, 0f);
            if (visual != null) visual.localPosition = new Vector3(0f, -1.7f, 0f);
            rope = GetComponentInChildren<CreatureThread>();
            if (rope != null) rope.Cut += Drop;
            // The spawn point is where his feet hang; the rope is tied above.
            pivot = transform.position + Vector3.up * (ropeLength + 1.7f);
            angle = 0.25f;
        }

        protected override void Tick(float dt)
        {
            if (Fallen) { FallDown(dt); return; }

            // Pendulum (small-angle-free), pumped towards the hero when he is near.
            float g = 9.81f;
            float alpha = HangedManLogic.Acceleration(angle, omega, ropeLength, g, damping);
            if (HasTarget)
            {
                float dx = target.Position.x - pivot.x;
                if (Mathf.Abs(dx) < reach && Mathf.Abs(angle) < 0.75f) alpha += HangedManLogic.Pump(angle, omega, dx) * pump;
            }
            omega += alpha * dt;
            angle += omega * dt;
            angle = Mathf.Clamp(angle, -1.2f, 1.2f);

            Vector3 neck = pivot + new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f) * ropeLength;
            transform.position = neck; // the root is at his neck, so he swings from the noose
            transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            if (rope != null) rope.Set(pivot, neck);

            TouchDamage(neck + Vector3.down * 0.7f, 0.7f, contactDamage, new Vector2(Mathf.Sign(omega + 1e-4f) * 6f, 3f));
            // (the body turns a little on the rope)
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.5f) * 25f, 0f);
        }

        /// <summary>Blows to the body don't hurt a dead man; they shove the swing.</summary>
        protected override bool AcceptHit(in DamageInfo info)
        {
            if (!Fallen) omega += Mathf.Clamp(info.knockback.x, -8f, 8f) * 0.18f;
            return false;
        }

        void Drop()
        {
            if (Fallen) return;
            Fallen = true;
            fallVelocity = 0f;
            // From now on the root is at his feet.
            transform.rotation = Quaternion.identity;
            transform.position += Vector3.down * 1.7f;
            if (visual != null) { visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; }
            CombatFx.Shake(0.15f);
        }

        void FallDown(float dt)
        {
            if (landed) return;
            var pos = transform.position;
            fallVelocity -= 25f * dt;
            float g = GroundBelow(pos + Vector3.up * 0.5f, 0.5f - fallVelocity * dt + 0.1f);
            if (!float.IsNaN(g) && pos.y + fallVelocity * dt <= g)
            {
                pos.y = g;
                landed = true;
                // The body becomes a solid lump: something to stand on or to weigh down a plate.
                var cap = GetComponent<CapsuleCollider>();
                cap.enabled = false;
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.3f, 0f);
                box.size = new Vector3(1.6f, 0.6f, 0.8f);
                if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
                if (visual != null) visual.localPosition = new Vector3(0.85f, 0.2f, 0f); // lying across the box
                CombatFx.Burst(pos + Vector3.up * 0.3f, Vector2.up, CombatFx.Ink, 20, 3f);
                CombatFx.Shake(0.25f);
            }
            else pos.y += fallVelocity * dt;
            transform.position = pos;
        }

        public static void BuildVisual(Transform root, Material ink, Material accent, Material ropeMaterial)
        {
            // Hangs from the noose: the root is the neck, the body below it.
            var v = CreatureParts.Group(root, "Visual", Vector3.zero); // standing in the editor; hung from the neck at runtime
            CreatureParts.Part(v, PrimitiveType.Capsule, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.5f, 0.32f), ink);
            CreatureParts.Part(v, PrimitiveType.Sphere, "Head", new Vector3(0.08f, 1.68f, 0f), new Vector3(0.3f, 0.34f, 0.3f), ink, new Vector3(0f, 0f, -28f));
            CreatureParts.Part(v, PrimitiveType.Capsule, "ArmL", new Vector3(0.2f, 1.0f, -0.05f), new Vector3(0.1f, 0.38f, 0.1f), ink, new Vector3(0f, 0f, 6f));
            CreatureParts.Part(v, PrimitiveType.Capsule, "ArmR", new Vector3(-0.2f, 1.0f, 0.05f), new Vector3(0.1f, 0.38f, 0.1f), ink, new Vector3(0f, 0f, -4f));
            CreatureParts.Part(v, PrimitiveType.Capsule, "LegL", new Vector3(0.08f, 0.36f, 0f), new Vector3(0.13f, 0.38f, 0.13f), ink, new Vector3(0f, 0f, 3f));
            CreatureParts.Part(v, PrimitiveType.Capsule, "LegR", new Vector3(-0.08f, 0.38f, 0f), new Vector3(0.13f, 0.38f, 0.13f), ink, new Vector3(0f, 0f, -5f));
            CreatureParts.Part(v, PrimitiveType.Cylinder, "Noose", new Vector3(0f, 1.52f, 0f), new Vector3(0.26f, 0.03f, 0.26f), accent);
            BuildRope(root, ropeMaterial);
        }

        /// <summary>The cuttable crimson rope (works with any visual, e.g. the Blender model).</summary>
        public static void BuildRope(Transform root, Material ropeMaterial)
        {
            var rope = new GameObject("Rope");
            rope.transform.SetParent(root, false);
            rope.AddComponent<CapsuleCollider>();
            var thread = rope.AddComponent<CreatureThread>();
            thread.Bind(CreatureParts.Line(root, "RopeLine", ropeMaterial, 0.06f, 10));
        }
    }

    /// <summary>Hanged man pendulum, engine independent.</summary>
    public static class HangedManLogic
    {
        /// <summary>Angular acceleration of a damped pendulum.</summary>
        public static float Acceleration(float angle, float omega, float length, float g, float damping)
            => -(g / Mathf.Max(0.1f, length)) * Mathf.Sin(angle) - damping * omega;

        /// <summary>
        /// Pumping like a child on a swing, but towards the hero: push in the direction of motion while the swing
        /// heads his way, so each pass reaches further towards <paramref name="dxToHero"/>.
        /// </summary>
        public static float Pump(float angle, float omega, float dxToHero)
        {
            float towards = Mathf.Sign(dxToHero);
            return Mathf.Sign(omega) == towards ? towards : 0f;
        }
    }
}
