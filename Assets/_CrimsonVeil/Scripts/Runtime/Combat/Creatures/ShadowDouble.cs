using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// The hero's own shadow, a moment late. It repeats his path and his very poses about a second behind him —
    /// but it never jumps: where he leapt a gap, it walks on and falls. Weapons only push it away;
    /// the only way to beat it is to lead it into a pit or a fall. Its touch hurts.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class ShadowDouble : CreatureBase
    {
        [SerializeField] float delay = 0.9f;
        [SerializeField] float wakeRange = 11f;
        [SerializeField] float fatalFall = 3.5f;

        /// <summary>True once the hero has come close and the shadow started following.</summary>
        public bool IsAwake { get; private set; }
        public float Delay => delay;

        struct Frame
        {
            public float time, x, facing, bob;
            public Quaternion[] pose;
        }

        readonly List<Frame> history = new List<Frame>();
        Transform visual, heroVisual;
        Transform[] mine, his;
        float fallVelocity, lastGroundY, push, clock;

        protected override void Awake()
        {
            maxHealth = 1f;
            contactDamage = 1f;
            base.Awake();
            GetComponent<CapsuleCollider>().isTrigger = true;
            visual = CreatureParts.Find(transform, "Visual");
            lastGroundY = transform.position.y;
        }

        protected override void Start()
        {
            base.Start();
            if (target.IsValid) heroVisual = CreatureParts.Find(target.Health.transform, "Visual");
            if (heroVisual == null || visual == null) return;
            // Pair up the bones by name (the double is built from the same model).
            var names = new Dictionary<string, Transform>();
            foreach (var t in heroVisual.GetComponentsInChildren<Transform>()) if (!names.ContainsKey(t.name)) names[t.name] = t;
            var a = new List<Transform>();
            var b = new List<Transform>();
            foreach (var t in visual.GetComponentsInChildren<Transform>())
            {
                if (t == visual) continue;
                if (names.TryGetValue(t.name, out var h)) { a.Add(t); b.Add(h); }
            }
            mine = a.ToArray();
            his = b.ToArray();
        }

        protected override void Tick(float dt)
        {
            if (IsDead) return;
            clock += dt;
            if (!HasTarget) return;
            var heroPos = target.Position;
            if (!IsAwake && Mathf.Abs(heroPos.x - transform.position.x) < wakeRange) IsAwake = true;
            if (!IsAwake) return;

            Record(heroPos);
            if (!ShadowDoubleLogic.TrySample(history, clock - delay, fr => fr.time, out var frame)) return;

            var pos = transform.position;
            // Follow his path in x — but on its own feet: no jumping, so a gap he leapt is a gap it falls into.
            float wantX = frame.x + push;
            push = Mathf.MoveTowards(push, 0f, 3f * dt);
            int dir = wantX > pos.x ? 1 : -1;
            float step = Mathf.Min(Mathf.Abs(wantX - pos.x), 9f * dt);
            // Once it walks off an edge it drops like a stone: no air control, so it can't carry over a gap.
            bool falling = fallVelocity < -0.5f;
            if (!falling && step > 0f && !Physics.Raycast(pos + Vector3.up * 0.9f, new Vector3(dir, 0f, 0f), 0.35f + step, ~0, QueryTriggerInteraction.Ignore))
                pos.x += dir * step;

            float g = GroundBelow(pos + Vector3.up * 0.6f, 0.75f - fallVelocity * dt);
            if (!float.IsNaN(g) && pos.y <= g + 0.05f && fallVelocity <= 0f)
            {
                pos.y = g;
                fallVelocity = 0f;
                lastGroundY = g;
            }
            else
            {
                fallVelocity -= 25f * dt;
                pos.y += fallVelocity * dt;
                if (ShadowDoubleLogic.FellToDeath(lastGroundY, pos.y, fatalFall)) { transform.position = pos; Kill(); return; }
            }
            transform.position = pos;

            // Wear his pose of a moment ago.
            if (visual != null)
            {
                var s = visual.localScale;
                s.x = frame.facing;
                visual.localScale = s;
                visual.localPosition = new Vector3(0f, frame.bob, 0f);
            }
            if (mine != null && frame.pose != null)
                for (int i = 0; i < mine.Length && i < frame.pose.Length; i++) mine[i].localRotation = frame.pose[i];

            TouchDamage(pos + Vector3.up * 0.9f, 0.55f, contactDamage, new Vector2(Mathf.Sign(heroPos.x - pos.x) * 6f, 3f));
        }

        void Record(Vector3 heroPos)
        {
            var f = new Frame { time = clock, x = heroPos.x, facing = 1f, bob = 0f };
            if (heroVisual != null)
            {
                f.facing = Mathf.Sign(heroVisual.localScale.x == 0f ? 1f : heroVisual.localScale.x);
                f.bob = heroVisual.localPosition.y;
            }
            if (his != null)
            {
                f.pose = new Quaternion[his.Length];
                for (int i = 0; i < his.Length; i++) f.pose[i] = his[i].localRotation;
            }
            history.Add(f);
            // Keep a little more than the delay.
            while (history.Count > 2 && history[1].time < clock - delay - 0.5f) history.RemoveAt(0);
        }

        /// <summary>Blades pass through a shadow: it is only pushed back, never hurt.</summary>
        protected override bool AcceptHit(in DamageInfo info)
        {
            push += Mathf.Clamp(info.knockback.x, -6f, 6f) * 0.25f;
            CombatFx.Burst(info.point, info.knockback.normalized, CombatFx.Ink, 10, 3f);
            return false;
        }
    }

    /// <summary>Shadow double rules, engine independent.</summary>
    public static class ShadowDoubleLogic
    {
        /// <summary>A fall deeper than <paramref name="fatal"/> metres below the last ground destroys the shadow.</summary>
        public static bool FellToDeath(float lastGroundY, float y, float fatal) => lastGroundY - y > fatal;

        /// <summary>The most recent recorded entry not newer than <paramref name="time"/> (generic over a time getter).</summary>
        public static bool TrySample<T>(IReadOnlyList<T> frames, float time, System.Func<T, float> timeOf, out T frame)
        {
            frame = default;
            for (int i = frames.Count - 1; i >= 0; i--)
            {
                if (timeOf(frames[i]) <= time) { frame = frames[i]; return true; }
            }
            return false;
        }
    }
}
