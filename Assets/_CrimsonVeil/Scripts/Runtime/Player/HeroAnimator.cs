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
            if (IsReady)
            {
                var hands = new HeroHands(model, hips); // (in the bind pose: the fingers' rest is measured here)
                Hands = hands.IsReady ? hands : null;
            }
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

            // The rest of the body (HeroBodyMotion): each joint calibrated the same way (turn it a little, see which
            // way its probe goes). Second axes: the spine and chest turn round the vertical, the arms swing out.
            var shoulderLT = Find("Shoulder.L");
            var shoulderRT = Find("Shoulder.R");
            var upperL = Find("UpperArm.L");
            var upperR = Find("UpperArm.R");
            var chestT = Find("Chest");
            var legR = Find("UpperLeg.R");
            if (shoulderRT != null)
            {
                spine?.WithSecond(up, () => shoulderRT.position, forward);
                chest?.WithSecond(up, () => shoulderRT.position, forward);
            }
            pelvis = chestT != null ? RigJoint.Create(hips, lateral, () => chestT.position, forward) : null;
            if (pelvis != null && legR != null) pelvis.WithSecond(up, () => legR.position, forward);
            neckJ = headT != null ? RigJoint.Create(neck, lateral, () => headT.position, forward) : null;
            shoulderL = upperL != null ? RigJoint.Create(shoulderLT, lateral, () => upperL.position, forward) : null;
            shoulderR = upperR != null ? RigJoint.Create(shoulderRT, lateral, () => upperR.position, forward) : null;
            Vector3 Outward(Transform t) => t != null && chestT != null ? Vector3.Project(t.position - chestT.position, lateral).normalized : lateral;
            if (armL != null && upperL != null) { var c = Find("LowerArm.L"); var o = Outward(upperL); armL.WithSecond(forward, () => c.position, o); }
            if (armR != null && upperR != null) { var c = Find("LowerArm.R"); var o = Outward(upperR); armR.WithSecond(forward, () => c.position, o); }
            RigJoint Tip(string bone, float reach, Vector3 measure)
            {
                var t = Find(bone);
                return t == null ? null : RigJoint.Create(t, lateral, () => t.position + t.up * reach, measure);
            }
            wristL = Tip("Hand.L", 0.08f, forward);
            wristR = Tip("Hand.R", 0.08f, forward);
            toeL = Tip("Toe.L", 0.05f, up);
            toeR = Tip("Toe.R", 0.05f, up);
            return spine != null && thighL != null && thighR != null && armL != null && armR != null;
        }

        RigJoint pelvis, neckJ, shoulderL, shoulderR, wristL, wristR, toeL, toeR;

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
                attacking = veil != null && veil.IsAttacking && veil.AttackProgress >= 0f,
                attackForm = veil != null ? veil.ShapeForm : VeilForm.Flow,
                attackT = veil != null ? veil.AttackProgress : -1f,
                attackAim = veil != null ? veil.AttackAim : 0f,
                wallSliding = player.Motor.State == MotorState.WallSlide,
                hanging = player.Motor.State == MotorState.LedgeHang,
                climbing = player.Motor.State == MotorState.Climbing,
                climbProgress = player.Motor.ClimbProgress,
                swinging = player.Motor.IsSwinging,
                swingSpeed = player.Motor.SwingAngularVelocity * player.Motor.Facing,
                weaponHeld = veil != null && veil.Form != VeilForm.Flow ? veil.TransformProgress : 0f,
                somersault = player.FlipProgress,
            };
            var p = Solver.Step(Time.deltaTime, s);
            Apply(p);
            if (Hands != null)
            {
                // the drawing hand pinches the string from the moment it reaches it until it lets go
                bool drawing = veil != null && veil.ShapeForm == VeilForm.Bow
                               && (veil.Drawing || (veil.IsAttacking && veil.AttackProgress > 0.1f && veil.AttackProgress < 0.4f));
                HeroHands.Targets(s.attackForm, s.weaponHeld, drawing, out float r, out float l);
                Hands.Apply(r, l, Time.deltaTime);
            }

            if (veilAnchor != null)
                veilAnchor.position = neck.position + veilAnchorOffset;
        }

        /// <summary>The fingers (null when the model has no finger bones).</summary>
        public HeroHands Hands { get; private set; }

        /// <summary>The sword hand's grip, 0 relaxed .. 1 a fist.</summary>
        public float Fist => Hands != null ? Hands.RightGrip : 0f;

        void Apply(in HeroPose p)
        {
            // The pelvis tilts under the body: the legs and the spine are measured from it, so they take the tilt back
            // out and keep the key pose's lines (the tilt shows in the hips themselves).
            float tilt = pelvis != null ? p.pelvis : 0f;
            pelvis?.Set(p.pelvis, p.pelvisTurn);
            spine?.Set(p.spine - tilt, p.twist * 0.45f); chest?.Set(p.chest, p.twist * 0.55f);
            neckJ?.Set(p.neck); head?.Set(p.head);
            thighL?.Set(p.thighL + tilt); kneeL?.Set(p.kneeL); footL?.Set(p.footL); toeL?.Set(p.toeL);
            thighR?.Set(p.thighR + tilt); kneeR?.Set(p.kneeR); footR?.Set(p.footR); toeR?.Set(p.toeR);
            shoulderL?.Set(p.shoulderL); shoulderR?.Set(p.shoulderR);
            armL?.Set(p.armL, p.armOutL); elbowL?.Set(p.elbowL); wristL?.Set(p.wristL);
            armR?.Set(p.armR, p.armOutR); elbowR?.Set(p.elbowR); wristR?.Set(p.wristR);

            var parent = hips.parent;
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            hips.localPosition = hipsRest + (parent != null ? parent.InverseTransformVector(up * p.bob) : up * p.bob);
        }
    }
}
