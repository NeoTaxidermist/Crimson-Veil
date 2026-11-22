using System.Collections.Generic;
using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Finds bone chains named "{prefix}{n}.0 → .1 → .2" (e.g. Hair.3.0) and makes them swing with
    /// <see cref="SpringChainSolver"/>. Runs after the body animation so hair reacts to the pose.
    ///
    /// Hair is styled as well as simulated (<see cref="lean"/>, <see cref="flutter"/>, <see cref="coreRadius"/>): each
    /// lock keeps its place in the hairstyle, leans with the wind of his motion - back at a run, up in a fall - and
    /// its tip flutters; the springs add the lag and the follow-through. (On springs alone, with a force for the wind,
    /// the locks were thrown about by every step: some folded into the skull, some stood forward over the face, and
    /// the head changed its shape from frame to frame.)
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class SpringBones : MonoBehaviour
    {
        [SerializeField] Transform root;
        [SerializeField] string prefix = "Hair.";
        [SerializeField] float stiffness = 9f;
        [SerializeField] float tipStiffness = 2.2f;
        [SerializeField, Range(0.5f, 1f)] float damping = 0.86f;
        [SerializeField] Vector3 gravity = new Vector3(0f, -5f, 0f);
        [Tooltip("Extra push opposite to the body's motion (wind of running).")]
        [SerializeField] float drag = 0.6f;
        [Tooltip("Bones the chains are kept out of (capsules from each bone to its first child), e.g. the legs for cloth.")]
        [SerializeField] string[] colliderBones = new string[0];
        [SerializeField] float colliderRadius = 0.09f;
        [Tooltip("Furthest (degrees) a bone may swing from its animated rest; 0 = no limit.")]
        [SerializeField] float maxBend = 0f;
        [Tooltip("The same at the chain's tip (it eases from maxBend at the root to this); 0 = maxBend all along.")]
        [SerializeField] float tipBend = 0f;
        [Tooltip("Hair: degrees a lock leans with the wind of the motion, per m/s (0 = it is not styled, only simulated).")]
        [SerializeField] float lean = 0f;
        [Tooltip("Hair: the furthest (degrees) a lock leans with the wind.")]
        [SerializeField] float maxLean = 34f;
        [Tooltip("Hair: degrees the tip of a lock flutters standing still, and more per m/s of wind.")]
        [SerializeField] float flutter = 0f;
        [SerializeField] float flutterPerSpeed = 0f;
        [Tooltip("Hair: the furthest (degrees) a lock's tip flutters.")]
        [SerializeField] float maxFlutter = 9f;
        [Tooltip("Hair: radius (m) of the skull the locks are kept out of, centred coreOffset m up the bone they grow from; 0 = none.")]
        [SerializeField] float coreRadius = 0f;
        [SerializeField] float coreOffset = 0.11f;

        Transform[] colliders, colliderEnds;

        sealed class Chain
        {
            public Transform[] bones;
            public Quaternion[] restLocal;
            public float tipLength;
            public SpringChainSolver solver;
            public Vector3[] rest;
            public Transform grownFrom;   // the bone the chain grows from (the head, for hair)
            public Vector3 lastRoot, wind;
            public float phase;
        }

        readonly List<Chain> chains = new List<Chain>();
        Vector3 lastRootPos;

        public int ChainCount => chains.Count;
        public string Prefix => prefix;
        /// <summary>The chain's bones, root first (for tests).</summary>
        public Transform[] ChainBones(int chain) => chains[chain].bones;
        /// <summary>The wind (m/s) a chain feels: against the motion of the place it grows from, smoothed.</summary>
        public Vector3 ChainWind(int chain) => chains[chain].wind;
        /// <summary>The chain's rest directions as its bones were posed before the springs moved them this frame:
        /// from its root to each joint and its tip (for tests).</summary>
        public Vector3[] ChainRest(int chain) => chains[chain].rest;

        /// <summary>Seconds over which the wind of the motion is smoothed (the bob of a step is not a gust).</summary>
        public const float WindSmoothing = 0.07f;

        void Awake()
        {
            if (root == null) root = transform;
            Collect(root);
            lastRootPos = root.position;
            var cs = new List<Transform>();
            var ends = new List<Transform>();
            foreach (var n in colliderBones)
            {
                var b = RigJoint.FindDeep(root, n);
                if (b == null || b.childCount == 0) continue;
                cs.Add(b);
                ends.Add(b.GetChild(0));
            }
            colliders = cs.ToArray();
            colliderEnds = ends.ToArray();
        }

        void Collect(Transform t)
        {
            if (t.name.StartsWith(prefix) && t.name.EndsWith(".0"))
            {
                var list = new List<Transform> { t };
                string stem = t.name.Substring(0, t.name.Length - 1);
                for (int i = 1; i < 8; i++)
                {
                    Transform next = null;
                    foreach (Transform c in list[list.Count - 1])
                        if (c.name == stem + i) next = c;
                    if (next == null) break;
                    list.Add(next);
                }
                AddChain(list.ToArray());
            }
            for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i));
        }

        void AddChain(Transform[] bones)
        {
            var c = new Chain { bones = bones, restLocal = new Quaternion[bones.Length], grownFrom = bones[0].parent };
            for (int i = 0; i < bones.Length; i++) c.restLocal[i] = bones[i].localRotation;
            c.tipLength = bones.Length > 1 ? Vector3.Distance(bones[bones.Length - 2].position, bones[bones.Length - 1].position) : 0.1f;
            c.rest = new Vector3[bones.Length + 1];
            FillRest(c);
            c.lastRoot = c.rest[0];
            c.phase = chains.Count * 1.7f;
            c.solver = new SpringChainSolver(c.rest) { Stiffness = stiffness, TipStiffness = tipStiffness, Damping = damping, Gravity = gravity, MaxBend = maxBend, TipBend = tipBend };
            chains.Add(c);
        }

        static void FillRest(Chain c)
        {
            var b = c.bones;
            for (int i = 0; i < b.Length; i++) c.rest[i] = b[i].position;
            // Bones point along their local +Y (Blender export convention).
            c.rest[b.Length] = b[b.Length - 1].position + b[b.Length - 1].up * c.tipLength;
        }

        /// <summary>Degrees a lock leans in a wind of <paramref name="speed"/> m/s: <paramref name="perSpeed"/> for each
        /// m/s in a light wind, levelling off towards <paramref name="most"/>.</summary>
        public static float LeanAngle(float speed, float perSpeed, float most)
            => most <= 0f || perSpeed <= 0f ? 0f : most * (1f - Mathf.Exp(-perSpeed * Mathf.Max(0f, speed) / most));

        /// <summary>
        /// The direction of a piece of a lock with the wind in it: <paramref name="dir"/> turned by
        /// <paramref name="degrees"/> towards where the <paramref name="wind"/> blows there. The wind does not blow
        /// into the skull (<paramref name="outward"/>: the way out of the head at the lock's root): it flows round
        /// it, so a fringe in a head wind is lifted up off the brow - not blown down through the face - and a lock on
        /// the crown is laid back. Straight against the skull there is nowhere for it to go: no lean.
        /// </summary>
        public static Vector3 Leaned(Vector3 dir, Vector3 wind, float degrees, Vector3 outward)
        {
            if (degrees <= 0f || wind.sqrMagnitude < 1e-8f || dir.sqrMagnitude < 1e-12f) return dir;
            Vector3 flow = wind;
            float share = 1f;
            if (outward.sqrMagnitude > 1e-8f)
            {
                Vector3 n = outward.normalized;
                float into = Vector3.Dot(wind, n);
                if (into < 0f) flow = wind - n * into;
                share = Mathf.Clamp01(flow.magnitude / (FlowRound * wind.magnitude));
                if (share <= 0f) return dir;
            }
            return Vector3.RotateTowards(dir, flow.normalized * dir.magnitude, degrees * share * Mathf.Deg2Rad, 0f);
        }

        /// <summary>The share of the wind's speed that still flows along the skull where it leans a lock in full (less
        /// of it - the wind all but straight against the head - and the lean fades out).</summary>
        public const float FlowRound = 0.2f;

        /// <summary>Degrees the tip of a lock flutters in a wind of <paramref name="speed"/> m/s.</summary>
        public static float FlutterAngle(float speed, float still, float perSpeed, float most)
            => Mathf.Min(most, still + perSpeed * Mathf.Max(0f, speed));

        /// <summary>How fast (Hz) the locks flutter in a wind of <paramref name="speed"/> m/s: a slow sway standing,
        /// a quick flicker at a run.</summary>
        public static float FlutterRate(float speed) => Mathf.Lerp(0.7f, 6.5f, Mathf.Clamp01(speed / 10f));

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || chains.Count == 0) return;
            using var marker = CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Springs);
            Vector3 bodyVel = (root.position - lastRootPos) / dt;
            lastRootPos = root.position;
            Vector3 wind = -bodyVel * drag;
            bool styled = lean > 0f || flutter > 0f || flutterPerSpeed > 0f;

            for (int ci = 0; ci < chains.Count; ci++)
            {
                var c = chains[ci];
                // Rest pose = animated pose: restore local rotations, then read positions.
                for (int i = 0; i < c.bones.Length; i++) c.bones[i].localRotation = c.restLocal[i];
                FillRest(c);
                c.solver.Gravity = gravity + wind;
                c.solver.Colliders.Clear();
                for (int k = 0; k < colliders.Length; k++)
                    c.solver.Colliders.Add(new SpringChainSolver.Capsule(colliders[k].position, colliderEnds[k].position, colliderRadius));
                Vector3 core = Vector3.zero;
                bool hasCore = coreRadius > 0f && c.grownFrom != null;
                if (hasCore)
                {
                    core = c.grownFrom.position + c.grownFrom.up * coreOffset;
                    c.solver.Colliders.Add(new SpringChainSolver.Capsule(core, core, coreRadius));
                }
                if (styled) Style(c, dt, core, hasCore);
                c.solver.Step(dt, c.rest);

                // Aim each bone at its simulated child.
                var p = c.solver.Positions;
                for (int i = 0; i < c.bones.Length; i++)
                {
                    var bone = c.bones[i];
                    Vector3 current = bone.up;
                    Vector3 wanted = p[i + 1] - p[i];
                    if (wanted.sqrMagnitude < 1e-8f) continue;
                    bone.rotation = Quaternion.FromToRotation(current, wanted.normalized) * bone.rotation;
                }
            }
        }

        /// <summary>The chain's rest for this frame as the wind combs it: leaned with the wind of its own motion and
        /// fluttering at the tip (more towards the tip, the root stays where it grows).</summary>
        void Style(Chain c, float dt, Vector3 core, bool hasCore)
        {
            Vector3 moved = c.rest[0] - c.lastRoot;
            c.lastRoot = c.rest[0];
            if (moved.sqrMagnitude > 4f)   // (carried elsewhere at once)
            {
                c.wind = Vector3.zero;
                c.solver.Reset(c.rest);
                moved = Vector3.zero;
            }
            c.wind = Vector3.Lerp(c.wind, -moved / dt, 1f - Mathf.Exp(-dt / WindSmoothing));
            float speed = c.wind.magnitude;
            float leanBy = LeanAngle(speed, lean, maxLean);
            float flutterBy = FlutterAngle(speed, flutter, flutterPerSpeed, maxFlutter);
            c.phase += 2f * Mathf.PI * FlutterRate(speed) * dt;
            int n = c.rest.Length - 1;
            Vector3 from = c.rest[0], laid = c.rest[0];
            for (int i = 0; i < n; i++)
            {
                Vector3 to = c.rest[i + 1];
                Vector3 dir = to - from;
                float share = (i + 1f) / n;
                Vector3 outward = hasCore ? from - core : Vector3.zero;
                dir = Leaned(dir, c.wind, leanBy * share, outward);
                if (flutterBy > 0f)
                {
                    // two wobbles across the lock, out of step with each other and running down it
                    Vector3 across = Vector3.Cross(dir, Vector3.forward);
                    if (across.sqrMagnitude < 1e-10f) across = Vector3.Cross(dir, Vector3.up);
                    across.Normalize();
                    Vector3 other = Vector3.Cross(dir, across).normalized;
                    float a = flutterBy * share * Mathf.Sin(c.phase - i * 0.9f);
                    float b = 0.6f * flutterBy * share * Mathf.Sin(c.phase * 1.31f + 1.3f - i * 1.3f);
                    dir = Quaternion.AngleAxis(a, other) * (Quaternion.AngleAxis(b, across) * dir);
                }
                from = to;
                laid += dir;
                c.rest[i + 1] = laid;
            }
        }
    }
}
