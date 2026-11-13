using CrimsonVeil.Core;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Applies <see cref="HeroPoseSolver"/> to the hero rig by bone name. Rotation axes and signs are
    /// calibrated at startup from the rig itself (<see cref="RigJoint"/>).
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

        RigJoint spine, chest, head, thighL, kneeL, footL, thighR, kneeR, footR, armL, elbowL, armR, elbowR;
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
            Transform Find(string n) => RigJoint.FindDeep(model, n);
            hips = Find("Hips");
            neck = Find("Neck");
            var footLT = Find("Foot.L");
            var footRT = Find("Foot.R");
            if (hips == null || neck == null || footLT == null || footRT == null)
            {
                Debug.LogWarning("[HeroAnimator] Rig bones not found under " + model.name);
                return false;
            }

            // Align the model so its forward (heel → toe) matches our forward (+Z of the parent).
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            Vector3 modelForward = Vector3.ProjectOnPlane(footLT.up, up).normalized;
            Vector3 forward = model.parent != null ? model.parent.forward : Vector3.forward;
            if (modelForward.sqrMagnitude > 0.5f)
                model.rotation = Quaternion.FromToRotation(modelForward, forward) * model.rotation;

            Vector3 lateral = Vector3.Cross(up, forward).normalized;
            hipsRest = hips.localPosition;

            RigJoint J(string bone, string child, Vector3 measure)
            {
                var c = Find(child);
                return c == null ? null : RigJoint.Create(Find(bone), lateral, () => c.position, measure);
            }

            spine = J("Spine", "Chest", forward);
            chest = J("Chest", "Neck", forward);
            var headT = Find("Head");
            head = RigJoint.Create(headT, lateral, () => headT.position + headT.up * 0.15f, forward);
            thighL = J("UpperLeg.L", "LowerLeg.L", forward);
            thighR = J("UpperLeg.R", "LowerLeg.R", forward);
            kneeL = J("LowerLeg.L", "Foot.L", -forward);
            kneeR = J("LowerLeg.R", "Foot.R", -forward);
            footL = RigJoint.Create(footLT, lateral, () => footLT.position + footLT.up * 0.15f, up);
            footR = RigJoint.Create(footRT, lateral, () => footRT.position + footRT.up * 0.15f, up);
            armL = J("UpperArm.L", "LowerArm.L", forward);
            armR = J("UpperArm.R", "LowerArm.R", forward);
            elbowL = J("LowerArm.L", "Hand.L", forward);
            elbowR = J("LowerArm.R", "Hand.R", forward);
            return spine != null && thighL != null && thighR != null && armL != null && armR != null;
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
                wallSliding = player.Motor.State == MotorState.WallSlide,
                hanging = player.Motor.State == MotorState.LedgeHang,
                climbing = player.Motor.State == MotorState.Climbing,
                climbProgress = player.Motor.ClimbProgress,
                weaponHeld = veil != null && veil.Form != VeilForm.Flow ? veil.TransformProgress : 0f,
                swingAngle = veil != null ? veil.CurrentSwing : 0f,
            };
            var p = Solver.Step(Time.deltaTime, s);
            Apply(p);

            if (veilAnchor != null)
                veilAnchor.position = neck.position + veilAnchorOffset;
        }

        void Apply(in HeroPose p)
        {
            spine?.Set(p.spine); chest?.Set(p.chest); head?.Set(p.head);
            thighL?.Set(p.thighL); kneeL?.Set(p.kneeL); footL?.Set(p.footL);
            thighR?.Set(p.thighR); kneeR?.Set(p.kneeR); footR?.Set(p.footR);
            armL?.Set(p.armL); elbowL?.Set(p.elbowL); armR?.Set(p.armR); elbowR?.Set(p.elbowR);

            var parent = hips.parent;
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            hips.localPosition = hipsRest + (parent != null ? parent.InverseTransformVector(up * p.bob) : up * p.bob);
        }
    }
}
