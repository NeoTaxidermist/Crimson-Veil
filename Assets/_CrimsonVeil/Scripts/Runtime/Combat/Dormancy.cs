using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Puts a creature to sleep while it is far out of the picture and wakes it before it comes into it: asleep, nothing
    /// of it runs - its mind, its animation, its swinging cloth - and it is no hostile (it is out of the fight); its body
    /// stays where it is, its colliders too. A chapter holds hundreds of creatures; only those near the picture cost
    /// anything. Added by the creatures themselves (<see cref="Enemy"/>, <see cref="Creatures.CreatureBase"/>,
    /// <see cref="Creatures.CrowSwarm"/>); <see cref="DormancyWatch"/> decides, a few times a second.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Dormancy : MonoBehaviour
    {
        static readonly List<Dormancy> all = new List<Dormancy>();
        public static IReadOnlyList<Dormancy> All => all;

        /// <summary>Asleep now.</summary>
        public bool Asleep { get; private set; }

        readonly List<Behaviour> slept = new List<Behaviour>();

        /// <summary>Gives <paramref name="go"/> its dormancy (once) and has it watched.</summary>
        public static Dormancy Track(GameObject go)
        {
            var d = go.GetComponent<Dormancy>();
            if (d == null) d = go.AddComponent<Dormancy>();
            DormancyWatch.Ensure();
            FixBounds(go);
            return d;
        }

        /// <summary>How far (m) a creature's pose may reach past its body at rest: its bounds are that much larger.</summary>
        public const float PoseReach = 0.8f;

        /// <summary>
        /// Its skinned bodies keep fixed bounds (the body at rest and <see cref="PoseReach"/> round it) instead of
        /// re-measuring the posed body every frame: that re-skinned the whole mesh on the CPU each frame for every
        /// creature - 1.2 ms a wraith in view, 30 of them half the frame. (Set for them all when the prefabs were made,
        /// so that a pose reaching past the rest never cut a creature off at the picture's edge.)
        /// </summary>
        public static void FixBounds(GameObject go)
        {
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.updateWhenOffscreen) continue;
                var b = smr.localBounds;
                smr.updateWhenOffscreen = false;
                b.Expand(2f * PoseReach);
                smr.localBounds = b;
            }
        }

        void Awake() { if (!all.Contains(this)) all.Add(this); }

        void OnDestroy() => all.Remove(this);

        /// <summary>Puts it to sleep: every behaviour on it that runs is stopped (and remembered).</summary>
        public void Sleep()
        {
            if (Asleep) return;
            Asleep = true;
            slept.Clear();
            foreach (var b in GetComponentsInChildren<Behaviour>(false))
            {
                if (b == this || !b.enabled || b is Collider2D || b is Camera || b is AudioSource) continue;
                if (!(b is MonoBehaviour) && !(b is Animator)) continue;
                b.enabled = false;
                slept.Add(b);
            }
        }

        /// <summary>Wakes it: what was running runs again, where it left off.</summary>
        public void Wake()
        {
            if (!Asleep) return;
            Asleep = false;
            foreach (var b in slept) if (b != null) b.enabled = true;
            slept.Clear();
        }

        /// <summary>Where it is (its body, for the watch).</summary>
        public Vector3 Position => transform.position;
    }

    /// <summary>
    /// Watches every <see cref="Dormancy"/> a few times a second: awake within <see cref="WakeMargin"/> of the picture (or
    /// of the hero, when there is no camera), asleep beyond <see cref="SleepMargin"/> (the gap between the two keeps one
    /// at the edge from flickering on and off). One per scene, made on first use.
    /// </summary>
    public sealed class DormancyWatch : MonoBehaviour
    {
        /// <summary>Metres beyond the picture's edge within which a creature is awake: it wakes before it is seen.</summary>
        public const float WakeMargin = 8f;
        /// <summary>Metres beyond the picture's edge past which it falls asleep.</summary>
        public const float SleepMargin = 12f;
        /// <summary>Seconds between looks.</summary>
        public const float Every = 0.2f;
        /// <summary>Off: nothing sleeps (for measuring).</summary>
        public static bool Enabled = true;

        static DormancyWatch instance;
        float next;
        PlayerHealth hero;

        public static void Ensure()
        {
            if (instance != null) return;
            instance = new GameObject("DormancyWatch").AddComponent<DormancyWatch>();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>How far (m) <paramref name="p"/> is outside the rectangle <paramref name="view"/> (0 inside it).</summary>
        public static float Outside(Rect view, Vector2 p)
        {
            float dx = Mathf.Max(view.xMin - p.x, 0f, p.x - view.xMax);
            float dy = Mathf.Max(view.yMin - p.y, 0f, p.y - view.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>What the camera sees of the plane the game is played in (z = 0), or null without a camera.</summary>
        public static Rect? View(Camera cam)
        {
            if (cam == null) return null;
            float depth = Mathf.Abs(cam.transform.position.z);
            if (depth < 0.01f) return null;
            float h = cam.orthographic ? cam.orthographicSize : depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float w = h * cam.aspect;
            var c = cam.transform.position;
            return new Rect(c.x - w, c.y - h, 2f * w, 2f * h);
        }

        /// <summary>Should a creature <paramref name="outside"/> m beyond the picture be awake, when it is <paramref name="asleep"/> now?</summary>
        public static bool ShouldBeAwake(float outside, bool asleep) => asleep ? outside <= WakeMargin : outside <= SleepMargin;

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Every;
            var all = Dormancy.All;
            if (!Enabled)
            {
                for (int i = 0; i < all.Count; i++) all[i].Wake();
                return;
            }
            var view = View(Camera.main);
            if (view == null)
            {
                // no camera (a test room): round the hero instead, as wide as the picture usually is
                if (hero == null) hero = CombatTarget.Find().Health;
                if (hero == null) { for (int i = 0; i < all.Count; i++) all[i].Wake(); return; }
                var h = hero.transform.position;
                view = new Rect(h.x - 9f, h.y - 5f, 18f, 10f);
            }
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null) continue;
                var p = d.Position;
                bool awake = ShouldBeAwake(Outside(view.Value, new Vector2(p.x, p.y)), d.Asleep);
                if (awake) d.Wake(); else d.Sleep();
            }
        }
    }
}
