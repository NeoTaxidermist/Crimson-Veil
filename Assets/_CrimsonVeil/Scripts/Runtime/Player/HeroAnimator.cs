using System;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Applies <see cref="HeroPoseSolver"/> to the hero rig by bone name. Rotation axes and signs are
    /// calibrated at startup from the rig itself, so the import axis convention does not matter.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public sealed class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] VeilController veil;
        [Tooltip("Root of the imported hero model (child of the player's visual root).")]
        [SerializeField] Transform model;
        [Tooltip("Optional: moved to the neck every frame so the veil follows the body.")]
        [SerializeField] Transform veilAnchor;
        [SerializeField] Vector3 veilAnchorOffset = new Vector3(0f, -0.04f, 0f);

        public HeroPoseSolver Solver { get; } = new HeroPoseSolver();
        public bool IsReady { get; private set; }

        sealed class Joint
        {
            public Transform t;
            public Quaternion rest;
            public Vector3 axis; // local-space axis where +angle moves the probe forward/up
        }

        Joint spine, chest, head, thighL, kneeL, footL, thighR, kneeR, footR, armL, elbowL, armR, elbowR;
        Transform hips;
        Vector3 hipsRest;
        Transform neck;

        void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (veil == null && player != null) veil = player.GetComponent<VeilController>();
            if (model == null) model = transform;
            IsReady = Setup();
        }

        bool Setup()
        {
            Transform Find(string n) => FindDeep(model, n);
            hips = Find("Hips");
            neck = Find("Neck");
            var footLT = Find("Foot.L");
            if (hips == null || neck == null || footLT == null)
            {
                Debug.LogWarning("[HeroAnimator] Rig bones not found under " + model.name);
                return false;
            }

            // Align the model so its forward (heel → toe) matches our forward (+Z of the parent).
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            Vector3 toe = footLT.position + footLT.up * 0.1f;
            Vector3 modelForward = Vector3.ProjectOnPlane(toe - footLT.position, up).normalized;
            Vector3 wanted = model.parent != null ? model.parent.forward : Vector3.forward;
            if (modelForward.sqrMagnitude > 0.5f)
                model.rotation = Quaternion.FromToRotation(modelForward, wanted) * model.rotation;

            Vector3 forward = wanted;
            Vector3 lateral = Vector3.Cross(up, forward).normalized;
            hipsRest = hips.localPosition;

            spine = Make(Find("Spine"), lateral, () => Find("Chest").position, forward);
            chest = Make(Find("Chest"), lateral, () => neck.position, forward);
            head = Make(Find("Head"), lateral, () => Find("Head").position + Find("Head").up * 0.15f, forward);
            thighL = Make(Find("UpperLeg.L"), lateral, () => Find("LowerLeg.L").position, forward);
            thighR = Make(Find("UpperLeg.R"), lateral, () => Find("LowerLeg.R").position, forward);
            kneeL = Make(Find("LowerLeg.L"), lateral, () => Find("Foot.L").position, -forward);
            kneeR = Make(Find("LowerLeg.R"), lateral, () => Find("Foot.R").position, -forward);
            footL = Make(footLT, lateral, () => footLT.position + footLT.up * 0.15f, up);
            var footRT = Find("Foot.R");
            footR = Make(footRT, lateral, () => footRT.position + footRT.up * 0.15f, up);
            armL = Make(Find("UpperArm.L"), lateral, () => Find("LowerArm.L").position, forward);
            armR = Make(Find("UpperArm.R"), lateral, () => Find("LowerArm.R").position, forward);
            elbowL = Make(Find("LowerArm.L"), lateral, () => Find("Hand.L").position, forward);
            elbowR = Make(Find("LowerArm.R"), lateral, () => Find("Hand.R").position, forward);
            return spine != null && thighL != null && thighR != null && armL != null && armR != null;
        }

        static Joint Make(Transform t, Vector3 worldAxis, Func<Vector3> probe, Vector3 measure)
        {
            if (t == null) return null;
            var j = new Joint { t = t, rest = t.localRotation };
            Vector3 local = Quaternion.Inverse(t.rotation) * worldAxis;

            Vector3 before = probe();
            t.localRotation = j.rest * Quaternion.AngleAxis(10f, local);
            Vector3 after = probe();
            t.localRotation = j.rest;

            j.axis = Vector3.Dot(after - before, measure) >= 0f ? local : -local;
            return j;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        void LateUpdate()
        {
            if (!IsReady || player == null || player.Motor == null) return;

            var s = new HeroState
            {
                speed = Mathf.Abs(player.Velocity.x),
                maxSpeed = player.Motor.Settings.maxRunSpeed,
                verticalVelocity = player.Velocity.y,
                grounded = player.IsGrounded,
                dashing = player.Motor.IsDashing,
                attacking = veil != null && veil.IsAttacking,
            };
            var p = Solver.Step(Time.deltaTime, s);
            Apply(p);

            if (veilAnchor != null)
                veilAnchor.position = neck.position + veilAnchorOffset;
        }

        void Apply(in HeroPose p)
        {
            Set(spine, p.spine); Set(chest, p.chest); Set(head, p.head);
            Set(thighL, p.thighL); Set(kneeL, p.kneeL); Set(footL, p.footL);
            Set(thighR, p.thighR); Set(kneeR, p.kneeR); Set(footR, p.footR);
            Set(armL, p.armL); Set(elbowL, p.elbowL); Set(armR, p.armR); Set(elbowR, p.elbowR);

            var parent = hips.parent;
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            hips.localPosition = hipsRest + (parent != null ? parent.InverseTransformVector(up * p.bob) : up * p.bob);
        }

        static void Set(Joint j, float degrees)
        {
            if (j == null) return;
            j.t.localRotation = j.rest * Quaternion.AngleAxis(degrees, j.axis);
        }
    }
}
