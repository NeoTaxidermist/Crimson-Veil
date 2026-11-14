using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A paper-kite wraith flown on a crimson thread from a stake in the ground. It sways on the wind and swoops
    /// at a hero within the thread's reach. Cut the thread (an arrow, any veil weapon) — or bring it down — and it
    /// crashes; its cloth stays behind, and a hero who picks it up can glide (hold jump while falling).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PaperKite : CreatureBase
    {
        [SerializeField] float threadLength = 5.5f;
        [SerializeField] float windSway = 0.6f;
        [SerializeField] float swoopSpeed = 2.8f;
        [SerializeField] float glideSeconds = 8f;

        public bool Free { get; private set; }
        public Vector3 Anchor => anchor;

        Vector3 anchor, velocity;
        float angle, angularVelocity;
        CreatureThread thread;
        Transform visual;
        LineRenderer tail;
        readonly Vector3[] tailPoints = new Vector3[6];
        bool landed;

        protected override void Awake()
        {
            maxHealth = 2f;
            contactDamage = 0.75f;
            base.Awake();
            GetComponent<BoxCollider>().isTrigger = true;
            anchor = transform.position; // placed at the stake
            visual = CreatureParts.Find(transform, "Visual");
            thread = GetComponentInChildren<CreatureThread>();
            if (thread != null) thread.Cut += Release;
            var tailT = CreatureParts.Find(transform, "TailLine");
            if (tailT != null) tail = tailT.GetComponent<LineRenderer>();
            angle = 0.3f;
        }

        protected override void Tick(float dt)
        {
            var pos = transform.position;
            if (Free) { Crash(dt, pos); UpdateTail(); return; }

            // On the thread: the wind sways it; within reach of the hero it swoops at him.
            float wind = Mathf.Sin(Time.time * 0.7f) * windSway + Mathf.Sin(Time.time * 1.9f) * windSway * 0.3f;
            float want = wind;
            if (HasTarget)
            {
                Vector3 toHero = TargetPos - anchor;
                if (toHero.magnitude < threadLength + 1.5f && toHero.y > -1f)
                    want = PaperKiteLogic.AngleTowards(toHero);
            }
            angularVelocity = Mathf.Lerp(angularVelocity, (want - angle) * swoopSpeed, 1f - Mathf.Exp(-4f * dt));
            angle += angularVelocity * dt;
            angle = Mathf.Clamp(angle, -1.3f, 1.3f);
            Vector3 next = PaperKiteLogic.OnThread(anchor, angle, threadLength);
            velocity = (next - pos) / dt;
            transform.position = next;
            if (thread != null) thread.Set(anchor, next);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg * 0.6f + Mathf.Sin(Time.time * 6f) * 4f);

            if (!IsDead) TouchDamage(next, 0.7f, contactDamage, new Vector2(Mathf.Sign(velocity.x + 1e-3f) * 5f, 2f));
            UpdateTail();
        }

        void Release()
        {
            if (Free) return;
            Free = true;
            velocity.y = Mathf.Max(velocity.y, 1f);
        }

        /// <summary>Shot down, it crashes and leaves its cloth.</summary>
        protected override void OnDeath()
        {
            if (thread != null && !thread.IsCut) thread.ReceiveHit(new DamageInfo { point = transform.position });
            Release();
        }

        void Crash(float dt, Vector3 pos)
        {
            if (landed) return;
            // Tumbling paper: falls slowly, side-slipping.
            velocity.y = Mathf.Max(velocity.y - 9f * dt, -4f);
            velocity.x = Mathf.Lerp(velocity.x, Mathf.Sin(Time.time * 2f) * 2f, 1f - Mathf.Exp(-2f * dt));
            pos += velocity * dt;
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f) * 50f);
            float g = GroundBelow(pos + Vector3.up * 0.4f, 0.6f);
            if (!float.IsNaN(g) && pos.y <= g + 0.15f)
            {
                pos.y = g + 0.1f;
                landed = true;
                if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, 80f);
                // The cloth stays: touch it to learn to glide.
                var box = GetComponent<BoxCollider>();
                box.enabled = true;
                box.size = new Vector3(1.4f, 0.8f, 1f);
                var pickup = gameObject.AddComponent<KiteCloth>();
                pickup.Seconds = glideSeconds;
            }
            transform.position = pos;
        }

        void UpdateTail()
        {
            if (tail == null) return;
            // The tail streams behind against the motion, rippling.
            Vector3 root = transform.position + Vector3.down * 0.5f;
            Vector3 back = velocity.sqrMagnitude > 0.01f ? -velocity.normalized : Vector3.down;
            back = (back + Vector3.down * 0.6f).normalized;
            for (int i = 0; i < tailPoints.Length; i++)
            {
                float t = (float)i / (tailPoints.Length - 1);
                Vector3 side = new Vector3(-back.y, back.x, 0f);
                tailPoints[i] = root + back * (t * 1.3f) + side * (Mathf.Sin(Time.time * 7f - t * 6f) * 0.15f * t);
            }
            tail.SetPositions(tailPoints);
        }

        public static void BuildVisual(Transform root, Material ink, Material accent, Material threadMaterial)
        {
            var v = CreatureParts.Group(root, "Visual", Vector3.zero);
            CreatureParts.Part(v, PrimitiveType.Cube, "Sail", Vector3.zero, new Vector3(0.75f, 0.75f, 0.02f), ink, new Vector3(0f, 0f, 45f));
            CreatureParts.Part(v, PrimitiveType.Cube, "StrutV", new Vector3(0f, 0f, -0.02f), new Vector3(0.03f, 1.06f, 0.02f), accent);
            CreatureParts.Part(v, PrimitiveType.Cube, "StrutH", new Vector3(0f, 0.12f, -0.02f), new Vector3(0.9f, 0.03f, 0.02f), accent);
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeL", new Vector3(-0.12f, 0.2f, -0.03f), Vector3.one * 0.08f, accent);
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeR", new Vector3(0.12f, 0.2f, -0.03f), Vector3.one * 0.08f, accent);
            BuildStrings(root, threadMaterial);
        }

        /// <summary>The fluttering tail and the cuttable thread (works with any visual, e.g. the Blender model).</summary>
        public static void BuildStrings(Transform root, Material threadMaterial)
        {
            var tail = CreatureParts.Line(root, "TailLine", threadMaterial, 0.05f, 6);
            tail.startColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            tail.endColor = new Color(0.05f, 0.05f, 0.05f, 0f);

            var threadGo = new GameObject("Thread");
            threadGo.transform.SetParent(root, false);
            threadGo.AddComponent<CapsuleCollider>();
            threadGo.AddComponent<CreatureThread>().Bind(CreatureParts.Line(root, "ThreadLine", threadMaterial, 0.03f, 12));
        }
    }

    /// <summary>Paper kite rules, engine independent.</summary>
    public static class PaperKiteLogic
    {
        /// <summary>Kite position on a taut thread of <paramref name="length"/> at <paramref name="angle"/> from vertical.</summary>
        public static Vector3 OnThread(Vector3 anchor, float angle, float length)
            => anchor + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * length;

        /// <summary>Thread angle (from vertical) pointing at a target offset from the anchor.</summary>
        public static float AngleTowards(Vector3 offset) => Mathf.Atan2(offset.x, Mathf.Max(0.1f, offset.y + 1.5f));
    }
}
