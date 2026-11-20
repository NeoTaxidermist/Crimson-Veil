using System.Collections.Generic;
using CrimsonVeil.Combat;
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
            mocap = GetComponent<HeroMocap>();
            health = player != null ? player.GetComponent<PlayerHealth>() : null;
            IsReady = Setup();
            if (IsReady && GetComponent<HeroWallTouchUp>() == null) gameObject.AddComponent<HeroWallTouchUp>().Animator = this;
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
            upperArmL = upperL; upperArmR = upperR;
            lowerArmL = Find("LowerArm.L"); lowerArmR = Find("LowerArm.R");
            handL = Find("Hand.L"); handR = Find("Hand.R");
            wristL = Tip("Hand.L", 0.08f, forward);
            wristR = Tip("Hand.R", 0.08f, forward);
            toeL = Tip("Toe.L", 0.05f, up);
            toeR = Tip("Toe.R", 0.05f, up);
            SetupContacts();
            SetupBodyPoints();
            return spine != null && thighL != null && thighR != null && armL != null && armR != null;
        }

        HeroMocap mocap;
        PlayerHealth health;
        float stillTime, lastAttackT = -1f;
        int swing;
        bool wasStriking;
        // what happened lately (for the captured motion: see HeroMocap.Context)
        HeroMocap.Context ctx = HeroMocap.Context.Default;
        bool wasGroundedM = true, wasHangingM, wasWallSliding, wasDead;
        float lastVy, lastHealth = -1f, sinceFast = 99f, airTop, sinceBlow = 99f;
        int lastBlocks, lastAbsorbs, lastFacing;

        /// <summary>Keeps <see cref="ctx"/>: the times since a landing (and how hard), a jump off a wall, a catch of the
        /// lip, a hurt, a block, death and the return to life; the swing's phase, the dash's progress.</summary>
        void Track(in HeroState s, float dt)
        {
            ctx.sinceLanded += dt; ctx.sinceWallJump += dt; ctx.sinceHangStart += dt; ctx.sinceHurt += dt;
            ctx.sinceBlock += dt; ctx.sinceDeath += dt; ctx.sinceRespawn += dt;
            ctx.sinceFlinch += dt; ctx.sinceTurn += dt; ctx.sinceStop += dt;
            // turning round, and pulling up, out of a run
            int facing = player.Motor.Facing;
            if (lastFacing != 0 && facing != lastFacing && s.grounded && sinceFast < 0.25f) ctx.sinceTurn = 0f;
            lastFacing = facing;
            // (the moment the keys let go of a run: the brake is 2-3 frames, and played out at a run its last stride left
            // him a moment in the air - the run's flight - while he stopped; setting off again ends the pull-up)
            bool keys = player.InputSource != null && Mathf.Abs(player.InputSource.MoveX) > 0.1f;
            if (s.grounded && !keys && s.speed < HeroMocap.StopFromSpeed + HeroMocap.StopBrakeMargin && sinceFast < 0.3f && ctx.sinceStop > HeroMocap.StopTime && ctx.sinceTurn > 0.3f) ctx.sinceStop = 0f;
            if (keys && ctx.sinceStop < HeroMocap.StopTime) ctx.sinceStop = 99f;
            sinceBlow = s.attacking ? 0f : sinceBlow + dt;
            sinceFast = s.grounded && s.speed >= HeroMocap.StopFromSpeed && sinceBlow > 0.5f ? 0f : sinceFast + dt;   // (a blow's lunge is no run)
            bool still = s.grounded && s.speed < 0.2f;
            ctx.stillTime = still ? ctx.stillTime + dt : 0f;
            float y = player.transform.position.y;
            if (s.grounded && !wasGroundedM) { ctx.sinceLanded = 0f; ctx.landFallHeight = player != null ? player.Motor.LastFallHeight : airTop - y; ctx.landRunSpeed = s.speed; }
            airTop = s.grounded || s.hanging || s.climbing || s.swinging || s.wallSliding ? y : Mathf.Max(airTop, y);
            if (!s.grounded && wasGroundedM && s.verticalVelocity > 0.5f) ctx.jumpSpeed = s.verticalVelocity;
            if (!s.grounded && wasWallSliding && !s.wallSliding && s.verticalVelocity > 0.5f)
            {
                ctx.sinceWallJump = 0f;
                ctx.jumpSpeed = s.verticalVelocity;
            }
            if (s.hanging && !wasHangingM) ctx.sinceHangStart = 0f;
            wasGroundedM = s.grounded; wasHangingM = s.hanging; wasWallSliding = s.wallSliding;
            if (!s.grounded) lastVy = s.verticalVelocity;
            if (health != null && health.Model != null)
            {
                float hp = health.Model.Current;
                if (lastHealth >= 0f && hp < lastHealth - 1e-4f && health.IsAlive) ctx.sinceHurt = 0f;
                lastHealth = hp;
                ctx.health01 = health.Model.Fraction;
                bool dead = !health.IsAlive;
                if (dead && !wasDead) ctx.sinceDeath = 0f;
                if (!dead && wasDead) ctx.sinceRespawn = 0f;
                wasDead = dead;
                ctx.dead = dead;
                if (health.BlockCount != lastBlocks) { ctx.sinceBlock = 0f; lastBlocks = health.BlockCount; }
                if (health.AbsorbCount != lastAbsorbs) { ctx.sinceFlinch = 0f; lastAbsorbs = health.AbsorbCount; }
            }
            ctx.drawing = veil != null && veil.Drawing;
            ctx.shieldHeld = veil != null && veil.ShieldHeld;
            ctx.dashProgress = player.Motor.DashProgress;
            if (s.swinging)
            {
                var m = player.Motor;
                float omega = Mathf.Sqrt(m.Settings.SwingGravity / Mathf.Max(0.5f, m.SwingLength));
                ctx.swingPhase = HeroMocap.SwingPhase(m.SwingAngle * player.BodyFacing, m.SwingAngularVelocity * player.BodyFacing, omega);
                ctx.swingAmplitude = HeroMocap.SwingAmplitude(m.SwingAngle, m.SwingAngularVelocity, omega);
            }
            bool striking = s.attacking && s.attackT >= 0f;
            if (striking && (!wasStriking || s.attackT < lastAttackT - 0.3f)) swing++;
            wasStriking = striking;
            lastAttackT = s.attackT;
            // which blow of the chain: the veil's combo (a pause starts it again from the first)
            ctx.swing = veil != null ? veil.Combo.Step : swing;
        }

        RigJoint pelvis, neckJ, shoulderL, shoulderR, wristL, wristR, toeL, toeR;
        Transform upperArmL, upperArmR, lowerArmL, lowerArmR, handL, handR;

        /// <summary>
        /// Hanging and mantling, the hands stay on the lip while the body moves: the pose only roughly places the arms,
        /// a two-bone reach puts the wrists on the stone - hooked over the lip while hanging and pulling up, flat on
        /// the top while pressing over it - until the knee is up and they let go.
        /// </summary>
        void ReachLip(in HeroState s)
        {
            float weight = HeroPoseSolver.LipHold(s.hanging, s.climbing, s.climbProgress);
            if (weight <= 0f) return;
            int dir = player.Motor.AttachSide != 0 ? player.Motor.AttachSide : player.Motor.Facing;
            Vector2 off = HeroPoseSolver.LipHandOffset(s.hanging ? 0f : s.climbProgress);
            var lip = player.LipPoint;
            Reach(upperArmL, lowerArmL, handL, lip, off, dir, weight);
            // hanging with a weapon cast, the right hand has let go of the lip to wield it
            float free = HeroPoseSolver.HangFreeArm(s.hanging, s.weaponHeld);
            Reach(upperArmR, lowerArmR, handR, lip, off, dir, weight * (1f - free));
        }

        /// <summary>
        /// On the whip both fists hold the rope - the whip hand at the grip, the other just above it - whichever way
        /// the body turns: the arms are only roughly posed, a two-bone reach closes the hands on the line.
        /// </summary>
        void GripRope()
        {
            var m = player.Motor;
            Vector3 grip = player.GripPosition;
            Vector3 up = HeroPoseSolver.RopeUp(grip, m.SwingAnchor);
            Vector3 forward = model != null ? model.forward : Vector3.right * player.BodyFacing;
            GripRopeHand(upperArmR, lowerArmR, handR, grip + up * HeroPoseSolver.RopeLowHand, up, forward);
            GripRopeHand(upperArmL, lowerArmL, handL, grip + up * HeroPoseSolver.RopeHighHand, up, forward);
        }

        static void GripRopeHand(Transform upper, Transform lower, Transform hand, Vector3 onRope, Vector3 up, Vector3 forward)
        {
            if (upper == null) return;
            // the wrist sits just under the fist closed round the rope
            var target = onRope - up * HeroPoseSolver.RopeWristBelow;
            var pole = upper.position + forward * 0.8f - up * 0.3f;   // the elbows bend forward, a little down
            LimbReach.Solve(upper, lower, hand, target, pole, 1f);
        }

        static void Reach(Transform upper, Transform lower, Transform hand, Vector3 lip, Vector2 off, int dir, float weight)
        {
            if (upper == null) return;
            var target = new Vector3(lip.x + dir * off.x, lip.y + off.y, upper.position.z);
            var pole = upper.position + new Vector3(-dir * 1f, -0.6f, 0f);   // the elbows bend back and down, away from the wall
            LimbReach.Solve(upper, lower, hand, target, pole, weight);
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
                attacking = veil != null && veil.IsAttacking && veil.AttackProgress >= 0f,
                attackForm = veil != null ? veil.ShapeForm : VeilForm.Flow,
                attackT = veil != null ? veil.AttackProgress : -1f,
                attackAim = veil != null ? veil.AttackAim : 0f,
                wallSliding = player.Motor.State == MotorState.WallSlide,
                hanging = player.Motor.State == MotorState.LedgeHang,
                climbing = player.Motor.State == MotorState.Climbing,
                climbProgress = player.Motor.ClimbProgress,
                swinging = player.Motor.IsSwinging,
                swingSpeed = player.Motor.SwingAngularVelocity * player.BodyFacing,   // (relative to the way the body faces on the whip)
                weaponHeld = veil != null && veil.Form != VeilForm.Flow ? veil.TransformProgress : 0f,
                somersault = player.FlipProgress,
                crouching = player.Motor.IsCrouching,
                sliding = player.Motor.IsSliding,
                slideProgress = player.Motor.SlideProgress,
                rolling = player.Motor.IsRolling,
                rollProgress = player.Motor.RollProgress,
            };
            if (s.hanging)
            {
                // the arms reach for where the lip really is
                var feet = player.transform.position;
                s.hangArm = HeroPoseSolver.HangArm((player.LipPoint.x - feet.x) * player.Motor.AttachSide, player.LipPoint.y - feet.y);
            }
            var p = Solver.Step(Time.deltaTime, s);
            Apply(p);
            // motion capture over the procedural pose (before the hands are set on the lip, so they stay on it)
            if (mocap != null)
            {
                Track(s, Time.deltaTime);
                stillTime = ctx.stillTime;
                mocap.Apply(HeroMocap.Choose(s, ctx), Time.deltaTime);
            }
            // on the ground the body lies on it: the sole, a heel, a knee, a hand on the floor - not through it, not over it
            KeepOnGround(s, Time.deltaTime);
            // and out of the walls: the capsule stops at a wall, the body bent out of it (a lean, a slide, a somersault) not
            // sliding down a wall the braced hand and foot are set on the stone (then the rest is kept out of it)
            if (s.wallSliding) BraceOnWall();
            KeepOffWalls(s, Time.deltaTime);
            if (s.hanging || s.climbing) ReachLip(s);
            if (s.swinging) GripRope();
            if (Hands != null)
            {
                // the drawing hand pinches the string from the moment it reaches it until it lets go
                bool drawing = veil != null && veil.ShapeForm == VeilForm.Bow
                               && (veil.Drawing || (veil.IsAttacking && veil.AttackProgress > 0.1f && veil.AttackProgress < 0.4f));
                HeroHands.Targets(s.attackForm, s.weaponHeld, drawing, out float r, out float l);
                // on the whip both fists are closed round the rope
                if (s.swinging) { r = 1f; l = 1f; }
                // hanging and mantling, both hands grip the lip (then lie flat on the top as he presses over it)
                else if (s.hanging || s.climbing)
                {
                    float grip = HeroPoseSolver.LedgeGrip(s.hanging, s.climbing, s.climbProgress);
                    r = Mathf.Lerp(grip, r, HeroPoseSolver.HangFreeArm(s.hanging, s.weaponHeld));   // the weapon hand
                    l = grip;
                }
                Hands.Apply(r, l, Time.deltaTime);
            }

            if (veilAnchor != null)
                veilAnchor.position = neck.position + veilAnchorOffset;
        }

        // --- ground contact -------------------------------------------------------------------------------------

        /// <summary>The bones whose skin can touch the floor: the soles, heels and toes, the shins and knees, the hands.</summary>
        static readonly string[] ContactBones = { "Foot.L", "Foot.R", "Toe.L", "Toe.R", "LowerLeg.L", "LowerLeg.R", "Hand.L", "Hand.R" };
        /// <summary>Points of the skin per contact bone, in the bone's own space (the vertices it carries most).</summary>
        const int PointsPerBone = 96;
        Transform[] contactBones;
        Vector3[][] contactPoints;
        float groundOffset;
        public const float GroundRunSpeed = 2f;
        /// <summary>How far (m) the body may be put up or down to lie on the floor.</summary>
        public const float GroundMaxLift = 0.35f, GroundMaxDrop = 0.15f;

        /// <summary>How far (m) over the floor the lowest point of the skin was this frame (after the ground contact).</summary>
        public float SkinOverFloor { get; private set; }

        void SetupContacts()
        {
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.sharedMesh == null) return;
            // the skin as it stands at rest (a baked copy: the model's own mesh is not readable in the game)
            var bakedMesh = new Mesh();
            skin.BakeMesh(bakedMesh, true);
            var local = new List<Vector3>();
            bakedMesh.GetVertices(local);
            Destroy(bakedMesh);
            var toWorld = skin.transform.localToWorldMatrix;
            // each contact bone's stretch of the body: from it to the next joint (the toes and the hands reach on a little)
            Transform F(string n) => RigJoint.FindDeep(model, n);
            contactBones = new Transform[ContactBones.Length];
            var from = new Vector3[ContactBones.Length];
            var to = new Vector3[ContactBones.Length];
            for (int c = 0; c < ContactBones.Length; c++)
            {
                var b = F(ContactBones[c]);
                contactBones[c] = b;
                if (b == null) continue;
                string side = ContactBones[c].EndsWith(".L") ? "L" : "R";
                Transform next = ContactBones[c].StartsWith("Foot") ? F("Toe." + side)
                               : ContactBones[c].StartsWith("LowerLeg") ? F("Foot." + side)
                               : ContactBones[c].StartsWith("Hand") ? F("Finger." + side + ".1.0") : null;
                from[c] = b.position;
                to[c] = next != null ? next.position : b.position + (b.position - b.parent.position).normalized * 0.1f;
                if (ContactBones[c].StartsWith("Hand") && next != null) to[c] = next.position + (next.position - b.position) * 0.8f;
            }
            var mine = new List<Vector3>[ContactBones.Length];
            for (int c = 0; c < mine.Length; c++) mine[c] = new List<Vector3>();
            foreach (var lv in local)
            {
                var w = toWorld.MultiplyPoint3x4(lv);
                int best = -1; float bestD = 0.16f;   // (only the skin near a contact bone)
                for (int c = 0; c < ContactBones.Length; c++)
                {
                    if (contactBones[c] == null) continue;
                    var seg = to[c] - from[c];
                    float t = Mathf.Clamp01(Vector3.Dot(w - from[c], seg) / Mathf.Max(1e-6f, seg.sqrMagnitude));
                    float d = Vector3.Distance(w, from[c] + seg * t);
                    if (d < bestD) { bestD = d; best = c; }
                }
                if (best >= 0) mine[best].Add(contactBones[best].InverseTransformPoint(w));
            }
            contactPoints = new Vector3[ContactBones.Length][];
            for (int c = 0; c < ContactBones.Length; c++)
            {
                // an even spread of them (enough to find the lowest within a centimetre or so)
                int step = Mathf.Max(1, mine[c].Count / PointsPerBone);
                var pick = new List<Vector3>();
                for (int i = 0; i < mine[c].Count; i += step) pick.Add(mine[c][i]);
                contactPoints[c] = pick.ToArray();
            }
        }

        /// <summary>The lowest point of the skin that can touch the floor (world y), or NaN.</summary>
        public float SkinLowest()
        {
            if (contactBones == null) return float.NaN;
            float y = float.PositiveInfinity;
            for (int c = 0; c < contactBones.Length; c++)
            {
                var b = contactBones[c];
                if (b == null) continue;
                var m = b.localToWorldMatrix;
                foreach (var p in contactPoints[c]) y = Mathf.Min(y, m.MultiplyPoint3x4(p).y);
            }
            return float.IsInfinity(y) ? float.NaN : y;
        }

        /// <summary>
        /// On the ground the body lies on it: the drawn body is put up or down so its lowest point is on the floor. The
        /// pose is a blend - the procedural one and the captured one each on the floor, the two together can put a heel
        /// through it (a slide sank 4-17 cm) or lift it (crawling, standing 4 cm over it). Running only lifts: the run
        /// has its flight.
        /// </summary>
        void KeepOnGround(in HeroState s, float dt)
        {
            float low = SkinLowest();
            bool contact = s.grounded && !s.hanging && !s.climbing && !s.swinging && !float.IsNaN(low);
            float want = 0f;
            if (contact)
            {
                var origin = player.transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    float gap = hit.point.y - low;   // (the pose is set afresh each frame: no offset in it yet)
                    bool running = s.speed > GroundRunSpeed && !s.crouching && !s.sliding && !s.rolling;
                    want = Mathf.Clamp(gap, running ? 0f : -GroundMaxDrop, GroundMaxLift);
                }
            }
            // up at once (nothing sinks into the floor, not for a frame), down smoothly (nothing drops)
            groundOffset = want > groundOffset ? want : Mathf.Lerp(groundOffset, want, 1f - Mathf.Exp(-30f * dt));
            Vector3 up = model.parent != null ? model.parent.up : Vector3.up;
            hips.position += up * groundOffset;
            if (contact && Physics.Raycast(player.transform.position + Vector3.up * 0.5f, Vector3.down, out var h2, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                SkinOverFloor = SkinLowest() - h2.point.y;
            else SkinOverFloor = float.NaN;
        }

        // --- walls -----------------------------------------------------------------------------------------------

        /// <summary>Points of the whole skin (hair and all), each in the space of the bone nearest it at rest.</summary>
        Transform[] bodyBones;
        Vector3[][] bodyPoints;
        const int BodyPointCount = 4000;
        /// <summary>The body is looked at in bands of this height (m): per band only its furthest point each way is cast.</summary>
        const float WallBand = 0.05f;
        const int WallBands = 64;
        readonly float[] bandRight = new float[WallBands], bandLeft = new float[WallBands];
        float wallOffset;
        readonly RaycastHit[] wallHits = new RaycastHit[6];
        /// <summary>How far (m) the drawn body may be pushed back out of a wall.</summary>
        public const float WallMaxPush = 0.6f;
        /// <summary>The body's half width the controller's capsule already keeps out of a wall (m).</summary>
        public const float WallCapsule = 0.2f;
        /// <summary>How far (m) clear of a wall the body is put once it touches one.</summary>
        public const float WallClearance = 0.015f;

        /// <summary>How far (m) the drawn body went into a wall this frame, after it was pushed out (0 = clear of it).</summary>
        public float SkinInWall { get; private set; }

        void SetupBodyPoints()
        {
            var bones = new List<Transform>();
            var pts = new List<List<Vector3>>();
            var all = new List<Vector3>();
            var mesh = new Mesh();
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skin.sharedMesh == null || skin.bones == null || skin.bones.Length == 0) continue;
                skin.BakeMesh(mesh, true);
                var local = new List<Vector3>();
                mesh.GetVertices(local);
                var toWorld = skin.transform.localToWorldMatrix;
                foreach (var v in local) all.Add(toWorld.MultiplyPoint3x4(v));
                foreach (var b in skin.bones) if (b != null && !bones.Contains(b)) { bones.Add(b); pts.Add(new List<Vector3>()); }
            }
            Destroy(mesh);
            if (bones.Count == 0 || all.Count == 0) return;
            int step = Mathf.Max(1, all.Count / BodyPointCount);
            for (int i = 0; i < all.Count; i += step)
            {
                var w = all[i];
                int best = 0; float bestD = float.MaxValue;
                for (int b = 0; b < bones.Count; b++)
                {
                    float d = (bones[b].position - w).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = b; }
                }
                pts[best].Add(bones[best].InverseTransformPoint(w));
            }
            var keepB = new List<Transform>(); var keepP = new List<Vector3[]>();
            for (int b = 0; b < bones.Count; b++) if (pts[b].Count > 0) { keepB.Add(bones[b]); keepP.Add(pts[b].ToArray()); }
            bodyBones = keepB.ToArray();
            bodyPoints = keepP.ToArray();
        }

        /// <summary>
        /// How far (m) the drawn body goes into a wall on its right and on its left: from the body's middle (the capsule's
        /// axis) along each point of the skin sticking out of the capsule, the first wall in the way. Floors, roofs and
        /// creatures are no walls.
        /// </summary>
        public void SkinIntoWalls(out float right, out float left)
        {
            right = 0f; left = 0f;
            if (bodyBones == null) return;
            var centre = player.transform.position;
            float bottom = centre.y - 0.6f;   // (bands from below the feet - a somersault, a roll - up over the head)
            for (int i = 0; i < WallBands; i++) { bandRight[i] = WallCapsule; bandLeft[i] = WallCapsule; }
            for (int b = 0; b < bodyBones.Length; b++)
            {
                var m = bodyBones[b].localToWorldMatrix;
                foreach (var lp in bodyPoints[b])
                {
                    var w = m.MultiplyPoint3x4(lp);
                    int band = Mathf.Clamp((int)((w.y - bottom) / WallBand), 0, WallBands - 1);
                    float dx = w.x - centre.x;
                    if (dx > bandRight[band]) bandRight[band] = dx;
                    else if (-dx > bandLeft[band]) bandLeft[band] = -dx;
                }
            }
            for (int i = 0; i < WallBands; i++)
            {
                float y = bottom + (i + 0.5f) * WallBand;
                if (bandRight[i] > WallCapsule) right = Mathf.Max(right, Into(centre, y, 1, bandRight[i]));
                if (bandLeft[i] > WallCapsule) left = Mathf.Max(left, Into(centre, y, -1, bandLeft[i]));
            }
        }

        /// <summary>How far a point <paramref name="reach"/> out from the body's middle at height y goes into a wall that way.</summary>
        float Into(Vector3 centre, float y, int side, float reach)
        {
            var from = new Vector3(centre.x, y, centre.z);
            int n = Physics.RaycastNonAlloc(from, new Vector3(side, 0f, 0f), wallHits, reach, ~0, QueryTriggerInteraction.Ignore);
            float nearest = reach;
            for (int i = 0; i < n; i++)
            {
                var h = wallHits[i];
                if (Mathf.Abs(h.normal.x) < 0.5f) continue;   // a floor's or a roof's face, grazed
                if (h.collider.transform.IsChildOf(player.transform)) continue;
                if (h.collider.GetComponentInParent<IHostile>() != null) continue;   // creatures are no walls
                if (h.distance < nearest) nearest = h.distance;
            }
            return reach - nearest;
        }

        /// <summary>
        /// The capsule stops at a wall; the body bent out of it does not: leaning into a run or a dash, crawling, sliding,
        /// in the somersault, landing hard, the head, the hands, the knees went up to 0.44 m into the stone. The drawn
        /// body is put back out of it - at once (not into a wall, not for a frame), and back again smoothly. Hanging on a
        /// lip (the knees went 0.24 m into its face) the body is put back too and the hands then reach onto the lip; on
        /// the rope the swing keeps the body clear itself (<see cref="PlayerController.SwingBodyBlocked"/>).
        /// </summary>
        void KeepOffWalls(in HeroState s, float dt)
        {
            float want = 0f;
            // (on the lip too: the hands are reached onto it afterwards; on the rope the swing keeps the body clear)
            bool free = !s.swinging && bodyBones != null;
            wallFree = free;
            if (free)
            {
                SkinIntoWalls(out float right, out float left);
                // (a little clear of the stone: the hair and the cloth swing on after this)
                if (right > 0f) right += WallClearance;
                if (left > 0f) left += WallClearance;
                want = Mathf.Clamp(left - right, -WallMaxPush, WallMaxPush);
            }
            bool further = Mathf.Abs(want) > Mathf.Abs(wallOffset) || want * wallOffset < 0f;
            wallOffset = further ? want : Mathf.Lerp(wallOffset, want, 1f - Mathf.Exp(-20f * dt));
            hips.position += Vector3.right * wallOffset;
            if (free)
            {
                SkinIntoWalls(out float r2, out float l2);
                SkinInWall = Mathf.Max(r2, l2);
            }
            else SkinInWall = 0f;
        }

        bool wallFree;
        Transform legUpperL, legLowerL, legFootL;
        /// <summary>How far (m, + right) the body is put out of a wall this frame.</summary>
        public float WallOffset => wallOffset;

        /// <summary>How far (m) the wrist and the ankle of the braced limbs are from the wall's face in a wall slide.</summary>
        public const float BraceHand = 0.05f, BraceFoot = 0.1f;

        /// <summary>
        /// Sliding down a wall he faces it, one hand braced high on it and a foot against it
        /// (<see cref="HeroPoseSolver.WallSlidePose"/>). The pose only reaches out roughly: the hand and the knee went
        /// into the stone, and kept out of it (<see cref="KeepOffWalls"/>) the whole body hung half a metre in front
        /// of the wall it slid down. The hand and the foot are reached onto its face.
        /// </summary>
        void BraceOnWall()
        {
            int side = player.Motor.AttachSide;
            if (side == 0) return;
            var centre = player.transform.position;
            float reach = 1.2f;
            float face = float.NaN;
            int n = Physics.RaycastNonAlloc(centre + Vector3.up * 1.2f, new Vector3(side, 0f, 0f), wallHits, reach, ~0, QueryTriggerInteraction.Ignore);
            float nearest = reach;
            for (int i = 0; i < n; i++)
            {
                var h = wallHits[i];
                if (Mathf.Abs(h.normal.x) < 0.5f || h.collider.transform.IsChildOf(player.transform)) continue;
                if (h.collider.GetComponentInParent<IHostile>() != null) continue;
                if (h.distance < nearest) { nearest = h.distance; face = h.point.x; }
            }
            if (float.IsNaN(face)) return;
            var away = new Vector3(-side, 0f, 0f);
            // both hands on the stone (the high one braced, the low one pressing: it stuck 0.3 m into it), the elbows down
            // and away from it
            foreach (var (upper, lower, hand) in new[] { (upperArmL, lowerArmL, handL), (upperArmR, lowerArmR, handR) })
            {
                if (upper == null) continue;
                var target = new Vector3(face - side * BraceHand, hand.position.y, hand.position.z);
                LimbReach.Solve(upper, lower, hand, target, upper.position + away * 0.5f + Vector3.down * 0.4f, 1f);
                // the palm flat on the stone, the fingers up it (reaching straight at it they went 0.17 m into it)
                var finger = RigJoint.FindDeep(hand, hand.name.EndsWith(".L") ? "Finger.L.2.0" : "Finger.R.2.0");
                if (finger != null)
                {
                    Vector3 along = finger.position - hand.position;
                    if (along.sqrMagnitude > 1e-6f) hand.rotation = Quaternion.FromToRotation(along, Vector3.up) * hand.rotation;
                }
            }
            if (legUpperL == null) { legUpperL = RigJoint.FindDeep(model, "UpperLeg.L"); legLowerL = RigJoint.FindDeep(model, "LowerLeg.L"); legFootL = RigJoint.FindDeep(model, "Foot.L"); }
            if (legFootL != null)
            {
                // the sole against the stone, the knee folded up and away from it (not through it)
                var target = new Vector3(face - side * BraceFoot, legFootL.position.y, legFootL.position.z);
                LimbReach.Solve(legUpperL, legLowerL, legFootL, target, legUpperL.position + Vector3.up * 0.5f + away * 0.5f, 1f);
            }
        }

        /// <summary>
        /// The last word on the walls, once the hair and the cloth have swung (<see cref="SpringBones"/>), the face and
        /// the fingers are set: whatever of the body they carried into a wall is put back out with the rest. (After the
        /// main fit the hair, a hand, swung on up to 5 cm into the stone.) Not on the lip or the rope: there the hands
        /// hold on.
        /// </summary>
        public void WallTouchUp()
        {
            if (!wallFree || bodyBones == null || player.Motor.State == MotorState.LedgeHang || player.Motor.State == MotorState.Climbing) return;
            SkinIntoWalls(out float right, out float left);
            if (right <= 0f && left <= 0f) { SkinInWall = 0f; return; }
            if (right > 0f) right += WallClearance;
            if (left > 0f) left += WallClearance;
            float more = Mathf.Clamp(wallOffset + left - right, -WallMaxPush, WallMaxPush) - wallOffset;
            if (Mathf.Abs(more) < 1e-5f) return;
            wallOffset += more;
            var shift = Vector3.right * more;
            hips.position += shift;
            if (veilAnchor != null) veilAnchor.position += shift;
            SkinIntoWalls(out float r2, out float l2);
            SkinInWall = Mathf.Max(r2, l2);
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
