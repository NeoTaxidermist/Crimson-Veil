using System.Collections.Generic;
using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Finds bone chains named "{prefix}{n}.0 → .1 → .2" (e.g. Hair.3.0) and makes them swing with
    /// <see cref="SpringChainSolver"/>. Runs after the body animation so hair reacts to the pose.
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

        sealed class Chain
        {
            public Transform[] bones;
            public Quaternion[] restLocal;
            public float tipLength;
            public SpringChainSolver solver;
            public Vector3[] rest;
        }

        readonly List<Chain> chains = new List<Chain>();
        Vector3 lastRootPos;

        public int ChainCount => chains.Count;

        void Awake()
        {
            if (root == null) root = transform;
            Collect(root);
            lastRootPos = root.position;
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
            var c = new Chain { bones = bones, restLocal = new Quaternion[bones.Length] };
            for (int i = 0; i < bones.Length; i++) c.restLocal[i] = bones[i].localRotation;
            c.tipLength = bones.Length > 1 ? Vector3.Distance(bones[bones.Length - 2].position, bones[bones.Length - 1].position) : 0.1f;
            c.rest = new Vector3[bones.Length + 1];
            FillRest(c);
            c.solver = new SpringChainSolver(c.rest) { Stiffness = stiffness, TipStiffness = tipStiffness, Damping = damping, Gravity = gravity };
            chains.Add(c);
        }

        static void FillRest(Chain c)
        {
            var b = c.bones;
            for (int i = 0; i < b.Length; i++) c.rest[i] = b[i].position;
            // Bones point along their local +Y (Blender export convention).
            c.rest[b.Length] = b[b.Length - 1].position + b[b.Length - 1].up * c.tipLength;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || chains.Count == 0) return;
            Vector3 bodyVel = (root.position - lastRootPos) / dt;
            lastRootPos = root.position;
            Vector3 wind = -bodyVel * drag;

            foreach (var c in chains)
            {
                // Rest pose = animated pose: restore local rotations, then read positions.
                for (int i = 0; i < c.bones.Length; i++) c.bones[i].localRotation = c.restLocal[i];
                FillRest(c);
                c.solver.Gravity = gravity + wind;
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
    }
}
