using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A great ash moth drawn to the veil's glow. It flutters high above the hero — out of reach of blades,
    /// a target for the bow — and every few seconds shakes ash from its wings. Ash that settles on the hero
    /// smothers the veil: for a while it dims (and the hero with it).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class AshMoth : CreatureBase
    {
        [SerializeField] float sightRange = 14f;
        [SerializeField] float hoverHeight = 4.2f;
        [SerializeField] float dustInterval = 4.5f;
        [SerializeField] float cloudTime = 2.4f;
        [SerializeField] float cloudRadius = 1.7f;
        [SerializeField] float dimAmount = 0.55f;
        [SerializeField] float dimSeconds = 4f;

        public bool Dusting => cloudTimer > 0f;
        public Vector3 CloudCentre { get; private set; }

        Vector3 home, velocity;
        float dustTimer, cloudTimer, fall;
        Transform wingL, wingR, visual;

        protected override void Awake()
        {
            maxHealth = 2f;
            contactDamage = 0.5f;
            base.Awake();
            GetComponent<SphereCollider>().isTrigger = true;
            home = transform.position;
            dustTimer = dustInterval;
            visual = CreatureParts.Find(transform, "Visual");
            wingL = CreatureParts.Find(transform, "WingL");
            wingR = CreatureParts.Find(transform, "WingR");
        }

        protected override void Tick(float dt)
        {
            var pos = transform.position;
            if (IsDead)
            {
                fall -= 12f * dt;
                pos.y += fall * dt;
                transform.position = pos;
                return;
            }

            bool hunting = HasTarget && Vector3.Distance(target.Position, pos) < sightRange;
            // Erratic moth flight around a point above the hero.
            float height = hoverHeight;
            if (hunting && Physics.Raycast(target.Position + Vector3.up * 1.8f, Vector3.up, out var roof, hoverHeight, ~0, QueryTriggerInteraction.Ignore))
                height = AshMothLogic.HoverHeight(hoverHeight, roof.distance + 1.8f);
            Vector3 anchor = hunting ? target.Position + Vector3.up * height : home;
            float t = Time.time;
            Vector3 flutter = new Vector3(Mathf.Sin(t * 1.3f) * 1.6f + Mathf.Sin(t * 3.7f) * 0.4f, Mathf.Sin(t * 2.1f) * 0.6f + Mathf.Sin(t * 5.3f) * 0.2f, 0f);
            Vector3 want = anchor + flutter;
            velocity = Vector3.Lerp(velocity, (want - pos) * 1.8f, 1f - Mathf.Exp(-3f * dt));
            pos += velocity * dt;
            transform.position = pos;

            // Shake the ash out: a cloud settles on the ground under the moth.
            cloudTimer = Mathf.Max(0f, cloudTimer - dt);
            if (hunting)
            {
                dustTimer -= dt;
                if (dustTimer <= 0f)
                {
                    dustTimer = dustInterval;
                    cloudTimer = cloudTime;
                    float g = GroundBelow(pos, 12f);
                    CloudCentre = new Vector3(pos.x, (float.IsNaN(g) ? pos.y - hoverHeight : g) + 0.9f, 0f);
                    CombatFx.Burst(pos, Vector2.down, new Color(0.35f, 0.35f, 0.36f, 0.8f), 40, 2.5f);
                }
            }
            if (cloudTimer > 0f)
            {
                if (Random.value < dt * 30f)
                    CombatFx.Burst(CloudCentre + new Vector3(Random.Range(-cloudRadius, cloudRadius), Random.Range(0f, 2f), 0f), Vector2.down, new Color(0.3f, 0.3f, 0.32f, 0.7f), 2, 0.6f);
                if (HasTarget && AshMothLogic.InCloud(TargetPos, CloudCentre, cloudRadius))
                    target.Health.Dim(dimAmount, dimSeconds);
            }

            TouchDamage(pos, 0.6f, contactDamage, new Vector2(Mathf.Sign(velocity.x + 1e-3f) * 4f, 2f));
            Animate(t);
        }

        void Animate(float t)
        {
            // 0 = wings up and open to the camera, larger = folded down / foreshortened.
            float beat = 55f + Mathf.Sin(t * 14f) * 55f + (Dusting ? Mathf.Sin(t * 40f) * 15f : 0f);
            if (wingL != null) wingL.localRotation = Quaternion.Euler(-beat, 0f, 0f);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(beat, 0f, 0f);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-velocity.x * 6f, -25f, 25f));
        }

        protected override void OnHurt(in DamageInfo info) => velocity += new Vector3(info.knockback.x, info.knockback.y + 2f, 0f) * 0.5f;

        public static void BuildVisual(Transform root, Material ink, Material accent)
        {
            var v = CreatureParts.Group(root, "Visual", Vector3.zero);
            CreatureParts.Part(v, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.16f, 0.32f, 0.16f), ink, new Vector3(0f, 0f, 90f));
            CreatureParts.Part(v, PrimitiveType.Sphere, "Head", new Vector3(0.3f, 0.02f, 0f), Vector3.one * 0.14f, ink);
            CreatureParts.Part(v, PrimitiveType.Cube, "AntennaA", new Vector3(0.4f, 0.12f, -0.03f), new Vector3(0.02f, 0.22f, 0.02f), ink, new Vector3(0f, 0f, -35f));
            CreatureParts.Part(v, PrimitiveType.Cube, "AntennaB", new Vector3(0.4f, 0.12f, 0.03f), new Vector3(0.02f, 0.22f, 0.02f), ink, new Vector3(0f, 0f, -20f));
            // Wings spread in the picture plane (seen from the side); a beat tilts them towards / away from us.
            foreach (var side in new[] { -1, 1 })
            {
                var wing = CreatureParts.Group(v, side < 0 ? "WingL" : "WingR", new Vector3(0f, 0.06f, side * 0.03f));
                CreatureParts.Part(wing, PrimitiveType.Sphere, "Fore", new Vector3(0.14f, 0.42f, 0f), new Vector3(0.62f, 0.8f, 0.02f), ink, new Vector3(0f, 0f, -12f));
                CreatureParts.Part(wing, PrimitiveType.Sphere, "Hind", new Vector3(-0.2f, 0.3f, 0f), new Vector3(0.45f, 0.55f, 0.02f), ink, new Vector3(0f, 0f, 20f));
                // Eye-spots: the crimson "eyes" on the wings.
                CreatureParts.Part(wing, PrimitiveType.Sphere, "Spot", new Vector3(0.14f, 0.5f, -0.02f), new Vector3(0.22f, 0.22f, 0.012f), accent);
            }
        }
    }

    /// <summary>Ash moth rules, engine independent.</summary>
    public static class AshMothLogic
    {
        /// <summary>How high above the hero to flutter: as high as it likes, but under a ceiling (never inside rock).</summary>
        public static float HoverHeight(float desired, float ceilingAboveHero) => Mathf.Clamp(ceilingAboveHero - 0.8f, 1.6f, desired);

        /// <summary>Is the point inside the settling ash cloud (a band hugging the ground)?</summary>
        public static bool InCloud(Vector3 point, Vector3 centre, float radius)
            => Mathf.Abs(point.x - centre.x) <= radius && Mathf.Abs(point.y - centre.y) <= 1.4f;
    }
}
