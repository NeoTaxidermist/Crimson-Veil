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
        /// <summary>The hero's model (its forward is the way the body faces).</summary>
        public Transform Model => model;
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
            // (for the brace on a wall: found here, as a search reads every bone's name - a new string each)
            braceFingerL = handL != null ? RigJoint.FindDeep(handL, "Finger.L.2.0") : null;
            braceFingerR = handR != null ? RigJoint.FindDeep(handR, "Finger.R.2.0") : null;
            palmL = PalmOf(handL, "L"); palmR = PalmOf(handR, "R");
            legUpperL = Find("UpperLeg.L"); legLowerL = Find("LowerLeg.L"); legFootL = Find("Foot.L");
            wristL = Tip("Hand.L", 0.08f, forward);
            wristR = Tip("Hand.R", 0.08f, forward);
            toeL = Tip("Toe.L", 0.05f, up);
            toeR = Tip("Toe.R", 0.05f, up);
            SetupContacts();
            handSkinR = System.Array.IndexOf(ContactBones, "Hand.R");
            SetupFootfalls();
            lookHead = Find("Head"); lookNeck = Find("Neck"); lookChest = Find("UpperChest");
            // (the way the face looks, in the head's own space: in the pose he is built in the eyes' bones point
            // straight ahead - later the eyes move in the head, so the head is what is measured)
            var eyeL = Find("Eye.L"); var eyeR = Find("Eye.R");
            hasFace = lookHead != null && eyeL != null && eyeR != null;
            if (hasFace)
            {
                Vector3 ahead = ((eyeL.up + eyeR.up) * 0.5f).normalized;
                faceAhead = Quaternion.Inverse(lookHead.rotation) * ahead;
                if (lookChest != null) chestAhead = Quaternion.Inverse(lookChest.rotation) * ahead;
            }
            leanSpine = Find("Spine"); leanChest = Find("Chest");
            if (leanSpine != null && leanChest != null && lookNeck != null)
            {
                // (half the turn is taken at the waist and half at the chest: how far that carries the neck, against
                // the whole of the turn taken at the hips)
                float whole = Vector3.Distance(hips.position, lookNeck.position);
                float above = Vector3.Distance(leanSpine.position, lookNeck.position) + Vector3.Distance(leanChest.position, lookNeck.position);
                leanGain = above > 1e-4f ? Mathf.Clamp(2f * whole / above, 1f, 2.5f) : 1f;
            }
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
            HandsOffLip = 0f;
            LipPull = 0f;
            int dir = player.Motor.AttachSide != 0 ? player.Motor.AttachSide : player.Motor.Facing;
            float climb = s.hanging ? 0f : s.climbProgress;
            Vector2 off = HeroPoseSolver.LipHandOffset(climb);
            var lip = player.LipPoint;
            if (weight <= 0f)
            {
                LegsAtTheLip(lip, dir, s.climbing);
                return;
            }
            // hanging with a weapon cast, the right hand has let go of the lip to wield it
            float free = HeroPoseSolver.HangFreeArm(s.hanging, s.weaponHeld);
            float right = weight * (1f - free);

            // The hands hold the lip and the body hangs from them - never the other way round. The catch throws the
            // knees at the wall, and the body, put back out of it (KeepOffWalls), went further from the lip than the
            // arms are long: for the first half second of every hang the hands were 15 to 25 cm off the stone, in the
            // air. So the body comes back as far as the arms need, and under the lip it is the legs that give way.
            Vector3 pull = WithinReach(upperArmL, lowerArmL, handL, lip, off, dir);
            if (right > 0.5f)
            {
                Vector3 other = WithinReach(upperArmR, lowerArmR, handR, lip, off, dir);
                if (other.sqrMagnitude > pull.sqrMagnitude) pull = other;
            }
            pull *= weight;
            if (pull.sqrMagnitude > 0f) hips.position += pull;
            LipPull = pull.magnitude;
            LegsAtTheLip(lip, dir, s.climbing);
            float press = HeroPoseSolver.LipPress(climb);
            HandsOffLip = Reach(upperArmL, lowerArmL, handL, lip, off, dir, weight, press);
            float offRight = Reach(upperArmR, lowerArmR, handR, lip, off, dir, right, press);
            if (right > 0.5f) HandsOffLip = Mathf.Max(HandsOffLip, offRight);
            LayOnLip(handL, palmL, dir, weight);
            LayOnLip(handR, palmR, dir, right);
            if (free > 0f) KeepHandOffTheFace(upperArmR, lowerArmR, handR, handSkinR, lip, dir, free);
        }

        /// <summary>How far (m) the fingers reach past the hand's own skin (a fist round a grip, an open hand).</summary>
        public const float FingersPastHand = 0.08f;
        int handSkinR = -1;

        /// <summary>
        /// Hanging with a weapon cast, the hand that wields it has let go of the lip - and, wielding it at the wall he
        /// hangs on, went into the stone (up to 19 cm, the forearm after it). Below the lip's top it stays this side of
        /// the face: the arm folds, the elbow back and down. Over the top there is no wall, and the hand is free.
        /// </summary>
        void KeepHandOffTheFace(Transform upper, Transform lower, Transform hand, int skin, Vector3 lip, int dir, float weight)
        {
            if (upper == null || lower == null || hand == null || skin < 0 || contactBones == null) return;
            float ahead = FurthestOf(skin, dir);
            if (float.IsInfinity(ahead)) return;
            float past = ahead + FingersPastHand - lip.x * dir + ToesOffFace;
            float below = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lip.y + 0.1f, lip.y, hand.position.y));
            float back = past * below * weight;
            if (back <= 0f) return;
            Vector3 target = hand.position - new Vector3(dir * back, 0f, 0f);
            Vector3 pole = upper.position + new Vector3(-dir * 0.6f, -0.6f, 0f);
            Quaternion kept = hand.rotation;
            LimbReach.Solve(upper, lower, hand, target, pole, 1f);
            hand.rotation = kept;
        }

        /// <summary>A hand's own frame, found once from the fingers as they were modelled: forward along the hand (wrist
        /// to knuckles), up out of its back - in the hand bone's space.</summary>
        struct Palm { public bool known; public Quaternion frame; }
        Palm palmL, palmR;

        static Palm PalmOf(Transform hand, string side)
        {
            if (hand == null) return default;
            var first = RigJoint.FindDeep(hand, "Finger." + side + ".0.0");
            var last = RigJoint.FindDeep(hand, "Finger." + side + ".3.0");
            var knuckle = RigJoint.FindDeep(hand, "Finger." + side + ".1.0");
            var tip = RigJoint.FindDeep(hand, "Finger." + side + ".1.2");
            if (first == null || last == null || knuckle == null || tip == null) return default;
            Vector3 a = hand.InverseTransformPoint(first.position), b = hand.InverseTransformPoint(last.position);
            Vector3 along = (0.5f * (a + b)).normalized;
            Vector3 palm = Vector3.Cross(along, b - a);
            if (palm.sqrMagnitude < 1e-10f) return default;
            // (the fingers curl towards the palm: that is its side)
            Vector3 curl = hand.InverseTransformPoint(tip.position) - hand.InverseTransformPoint(knuckle.position);
            if (Vector3.Dot(palm, curl) < 0f) palm = -palm;
            return new Palm { known = true, frame = Quaternion.LookRotation(along, -palm.normalized) };
        }

        /// <summary>How far down (degrees) from level the fingers point, lying on a lip's top: onto the stone.</summary>
        public const float LipFingersDown = 0f;

        /// <summary>
        /// The hand lies on the lip's top: the palm down on the stone, the fingers into the ledge. (Left as the arm
        /// brought it, it stood up over the corner like a hook closed on nothing; and pressing over the lip the fingers
        /// went down into its top, 11 cm.)
        /// </summary>
        static void LayOnLip(Transform hand, in Palm palm, int dir, float weight)
        {
            if (hand == null || !palm.known || weight <= 0f) return;
            float tilt = LipFingersDown * Mathf.Deg2Rad;
            var fingers = new Vector3(dir * Mathf.Cos(tilt), -Mathf.Sin(tilt), 0f);
            Quaternion want = Quaternion.LookRotation(fingers, Vector3.up) * Quaternion.Inverse(palm.frame);
            hand.rotation = Quaternion.Slerp(hand.rotation, want, weight);
        }

        /// <summary>How far (m) the wrists of the hands that hold a lip are from their place on it this frame, the reach
        /// done (0 when he holds none).</summary>
        public float HandsOffLip { get; private set; }

        /// <summary>How far (m) the body was brought back towards the lip this frame so the arms reach it.</summary>
        public float LipPull { get; private set; }

        /// <summary>The share of the arm's length it is stretched to at the most, holding a lip.</summary>
        public const float ArmStretch = 0.98f;

        /// <summary>How near (m) the wall under a lip a knee's joint may come before the leg gives way (the knee's own thickness).</summary>
        public const float LegOffFace = 0.06f;

        /// <summary>How far the shoulder has to come towards the hand's place on the lip for the arm to reach it.</summary>
        static Vector3 WithinReach(Transform upper, Transform lower, Transform hand, Vector3 lip, Vector2 off, int dir)
        {
            if (upper == null || lower == null || hand == null) return Vector3.zero;
            var target = new Vector3(lip.x + dir * off.x, lip.y + off.y, upper.position.z);
            float arm = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
            return HeroPoseSolver.PullToReach(upper.position, target, arm * ArmStretch);
        }

        /// <summary>How far (m) clear of the stone the toes are kept beside the wall under a lip; a leg gives way to the
        /// stone fully once it is this far (m) into where it may not be.</summary>
        public const float ToesOffFace = 0.015f, LegGive = 0.03f;
        /// <summary>How far (m) over a lip's top the middle of a knee or of a shin is kept (their own thickness).</summary>
        public const float ShinOverCorner = 0.07f;
        /// <summary>Coming over a lip, a foot over its top and within this (m) above it is set down on it (fully from
        /// half of it).</summary>
        public const float StepDown = 0.25f;

        /// <summary>
        /// The legs at a lip, hanging from it and coming up over it. Under the lip a foot stays off its wall: it is set
        /// back off the face, the knee turned out to the side (as a climber's against a wall), where from the side it
        /// reads as a leg hanging a little bent. Coming up over the lip, a foot the pose has in the stone is set back and
        /// up by as much: so it slides up the face to the corner and from there on along the top, where it stands, the
        /// knee folded up and forward - one unbroken way, whichever part of the stone the pose swings it through. (The
        /// climb sets his place on the top while the body is still low over the hands: a foot that came over the lip hung
        /// under its top, in the stone - or, the body lifted clear of it, he rose over the ledge with nothing under him.)
        /// </summary>
        void LegsAtTheLip(Vector3 lip, int dir, bool climbing)
        {
            if (footfalls[0] == null || footfalls[1] == null) return;
            for (int k = 0; k < 2; k++)
            {
                var f = footfalls[k];
                if (f.footPoints < 0 || f.toePoints < 0) continue;
                // (the foot by its skin, not its bones: the toes reach 8 cm past theirs)
                float low = Mathf.Min(LowestOf(f.footPoints), LowestOf(f.toePoints));
                float ahead = Mathf.Max(FurthestOf(f.footPoints, dir), FurthestOf(f.toePoints, dir));
                if (float.IsInfinity(low) || float.IsInfinity(ahead)) continue;
                float under = lip.y - low;                         // how far the foot's lowest point is under the lip's top
                float past = ahead - lip.x * dir + ToesOffFace;    // how far its foremost point is past where it may be at the face
                if (under > 0f && past > 0f)
                {
                    float back = climbing ? Mathf.Min(past, under) : past;
                    float up = climbing ? back : 0f;
                    Vector3 hip = f.upper.position, foot = f.foot.position;
                    var target = new Vector3(foot.x - dir * back, foot.y + up, foot.z);
                    Vector3 mid = 0.5f * (hip + target);
                    // (the knee out to its own side: the left leg's to the left, the right leg's to the right)
                    Vector3 aside = mid + new Vector3(-dir * 0.15f, 0f, hip.z >= hips.position.z ? 1f : -1f);
                    Vector3 ahead3 = mid + new Vector3(dir, 0.3f, 0f);
                    float onTop = climbing ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.06f, 0.06f, past - under)) : 0f;
                    Vector3 pole = Vector3.Lerp(f.lower.position, Vector3.Lerp(aside, ahead3, onTop), Mathf.SmoothStep(0f, 1f, back / LegGive));
                    Quaternion kept = f.foot.rotation;
                    LimbReach.Solve(f.upper, f.lower, f.foot, target, pole, 1f);
                    f.foot.rotation = kept;
                }
                else if (climbing && under <= 0f)
                {
                    // Over the top and close above it, the foot is set down on it: it is that foot that carries him
                    // once the hands let go. (Left where the pose had it - a hand's breadth over the stone while the
                    // body caught up - there was a moment with neither a hand on the lip nor a foot on the top.)
                    float over = (f.foot.position.x - lip.x) * dir;
                    float down = -under;
                    float set = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.1f, over))
                                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(StepDown, StepDown * 0.5f, down));
                    if (set > 0f && down > 0f)
                    {
                        Vector3 target = f.foot.position + Vector3.down * (down * set);
                        Quaternion kept = f.foot.rotation;
                        LimbReach.Solve(f.upper, f.lower, f.foot, target, f.lower.position, 1f);
                        f.foot.rotation = kept;
                    }
                }
                KneeOutOfTheStone(f, lip, dir);
            }
        }

        /// <summary>The foremost point (world x, times <paramref name="dir"/>) of the skin one contact bone carries.</summary>
        float FurthestOf(int c, int dir)
        {
            var b = contactBones[c];
            if (b == null) return float.NegativeInfinity;
            var m = b.localToWorldMatrix;
            float x = float.NegativeInfinity;
            foreach (var p in contactPoints[c]) x = Mathf.Max(x, m.MultiplyPoint3x4(p).x * dir);
            return x;
        }

        /// <summary>
        /// A knee at the wall under the lip, a knee at the lip's corner not yet clear over it, a shin lying across the
        /// corner (the knee over the top, the foot still out past the edge - the trailing leg, coming up after him): the
        /// knee is turned out to the side, as a hurdler's trailing leg goes over the bar. From the side it reads as the
        /// leg drawn up short, and it is clear of the stone. The foot stays where it is.
        /// </summary>
        void KneeOutOfTheStone(Footfall f, Vector3 lip, int dir)
        {
            Vector3 hip = f.upper.position, knee = f.lower.position, foot = f.foot.position;
            float kx = (knee.x - lip.x) * dir, ax = (foot.x - lip.x) * dir;
            // (how far the knee, with its thickness, is into the stone: past the face and under the top)
            float into = Mathf.Min(kx + LegOffFace, lip.y + ShinOverCorner - knee.y);
            if (kx > 0f && ax < 0f)
            {
                float along = kx / (kx - ax);   // where along the shin (knee to ankle) it is over the face
                into = Mathf.Max(into, lip.y + ShinOverCorner - Mathf.Lerp(knee.y, foot.y, along));
            }
            if (into <= 0f) return;
            Vector3 pole = 0.5f * (hip + foot) + new Vector3(-dir * 0.15f, 0f, hip.z >= hips.position.z ? 1f : -1f);
            Quaternion kept = f.foot.rotation;
            LimbReach.Solve(f.upper, f.lower, f.foot, foot, pole, Mathf.SmoothStep(0f, 1f, into / LegGive));
            f.foot.rotation = kept;
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

        /// <summary>Reaches the hand onto the lip; returns how far (m) the wrist is from its place once that is done.</summary>
        static float Reach(Transform upper, Transform lower, Transform hand, Vector3 lip, Vector2 off, int dir, float weight, float press)
        {
            if (upper == null) return 0f;
            var target = new Vector3(lip.x + dir * off.x, lip.y + off.y, upper.position.z);
            // The elbows bend back, away from the wall: down while he hangs and pulls up; pressing over the lip they are
            // up, over the hands, as in any press (kept down there, the forearm came up to the palm from under the lip).
            var pole = upper.position + new Vector3(-dir * 1f, Mathf.Lerp(-0.6f, 0.8f, press), 0f);
            LimbReach.Solve(upper, lower, hand, target, pole, weight);
            return Vector3.Distance(hand.position, target);
        }

        void LateUpdate()
        {
            if (!IsReady || player == null || player.Motor == null) return;
            using var marker = CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Animator);
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
                lowBlow = veil != null && veil.LowBlow,
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
            HeroPose p;
            using (CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Pose)) { p = Solver.Step(Time.deltaTime, s); Apply(p); }
            // motion capture over the procedural pose (before the hands are set on the lip, so they stay on it)
            if (mocap != null)
            {
                Track(s, Time.deltaTime);
                stillTime = ctx.stillTime;
                mocap.Apply(HeroMocap.Choose(s, ctx), Time.deltaTime);
            }
            // the body goes with the blow: striking upwards the trunk goes back into it...
            LeanIntoTheBlow(s, Time.deltaTime);
            // ...and his eyes are on it: the neck and the head turn to where it goes
            LookAlongTheBlow(s, Time.deltaTime);
            // on the ground the body lies on it: the sole, a heel, a knee, a hand on the floor - not through it, not over it
            using (CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Ground))
            {
                KeepOnGround(s, Time.deltaTime);
                // ...and the foot that carries him stays where it was set down
                PlantFeet(s, Time.deltaTime);
            }
            // and out of the walls: the capsule stops at a wall, the body bent out of it (a lean, a slide, a somersault) not
            // sliding down a wall the braced hand and foot are set on the stone (then the rest is kept out of it)
            if (s.wallSliding) BraceOnWall();
            using (CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Walls)) KeepOffWalls(s, Time.deltaTime);
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
            onFloor = false;
            if (contact)
            {
                var origin = player.transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    onFloor = true;
                    floorY = hit.point.y;
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

        // --- eyes on the blow -------------------------------------------------------------------------------------

        Transform lookHead, lookNeck, lookChest;
        Vector3 faceAhead, chestAhead;
        bool hasFace;
        float lookWeight;
        float lookAim;

        /// <summary>He looks where he strikes (off: the head as the pose has it - to compare).</summary>
        public bool EyesOnTheBlow { get; set; } = true;

        /// <summary>Degrees his face looks above the level (below: negative), as the head was posed this frame.</summary>
        public float LookPitch { get; private set; }

        /// <summary>The furthest (degrees) the face is turned up off the way the chest faces to look along a blow, and
        /// down off it: as far as a neck goes.</summary>
        public const float LookUpMost = 44f, LookDownMost = 35f;
        /// <summary>The share of the turn the neck takes (the head takes the rest).</summary>
        public const float LookNeckShare = 0.4f;
        /// <summary>How fast (1/s) he turns to look along a blow, and looks ahead again after it.</summary>
        public const float LookIn = 14f, LookOut = 7f;

        /// <summary>How much (0..1) he looks along the blow at attack progress <paramref name="t"/>: from the wind-up, all
        /// through the strike, ahead again as it ends.</summary>
        public static float LookThrough(float t)
            => t < 0f ? 0f : Mathf.SmoothStep(0f, 1f, t / 0.12f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.78f) / 0.22f));

        /// <summary>How much (0..1) a blow aimed <paramref name="aim"/> degrees off the level turns his head: a level
        /// blow leaves it to the pose (he is looking that way already), a steep one turns it all the way.</summary>
        public static float LookSteep(float aim) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 30f, Mathf.Abs(aim)));

        /// <summary>
        /// The turn (degrees, + = back) that brings a face looking <paramref name="pitch"/> degrees above the level to
        /// look along a blow aimed <paramref name="aim"/> degrees above it, the chest facing <paramref name="chest"/>
        /// degrees above the level - as far as a neck goes off the chest (<see cref="LookUpMost"/>,
        /// <see cref="LookDownMost"/>): striking straight up he looks most of the way up.
        /// </summary>
        public static float LookTurn(float pitch, float aim, float chest)
            => chest + Mathf.Clamp(Mathf.DeltaAngle(chest, aim), -LookDownMost, LookUpMost) - pitch;

        /// <summary>
        /// He looks where he strikes: striking up the head goes back, striking under his feet it goes down, the bow
        /// drawn at the sky he looks along the arrow. (In the poses the head only nodded towards a blow: thrusting a
        /// spear straight up he looked 17 to 31 degrees DOWN.) The neck and the head turn, about the axis across his
        /// body, so it reads in the side view; the trunk has its own part (<see cref="LeanIntoTheBlow"/>). The turn
        /// comes in with the wind-up and goes as the blow ends.
        /// </summary>
        void LookAlongTheBlow(in HeroState s, float dt)
        {
            if (!hasFace) return;
            bool drawing = veil != null && veil.ShapeForm == VeilForm.Bow && veil.Drawing;
            float through = !EyesOnTheBlow ? 0f : drawing ? 1f : s.attacking ? LookThrough(s.attackT) : 0f;
            float aim = drawing || s.attacking ? s.attackAim : lookAim;
            float want = through * LookSteep(aim);
            if (want > 0f) lookAim = aim;
            lookWeight = Mathf.Lerp(lookWeight, want, 1f - Mathf.Exp(-(want > lookWeight ? LookIn : LookOut) * dt));
            if (lookWeight < 1e-3f) lookWeight = 0f;
            // (his facing is the way the body is turned in the picture)
            float facing = player.BodyFacing >= 0 ? 1f : -1f;
            Vector3 look = lookHead.rotation * faceAhead;
            float pitch = Mathf.Atan2(look.y, look.x * facing) * Mathf.Rad2Deg;
            if (lookWeight > 0f && look.x * facing > 0.05f)
            {
                Vector3 front = lookChest != null ? lookChest.rotation * chestAhead : new Vector3(facing, 0f, 0f);
                float chest = Mathf.Atan2(front.y, Mathf.Max(0.05f, front.x * facing)) * Mathf.Rad2Deg;
                float turn = LookTurn(pitch, lookAim, chest) * lookWeight;
                // (about the axis across the body: seen from the side, anticlockwise is back for one facing right)
                Vector3 axis = Vector3.forward * facing;
                float neckShare = lookNeck != null ? LookNeckShare : 0f;
                if (lookNeck != null) lookNeck.rotation = Quaternion.AngleAxis(turn * neckShare, axis) * lookNeck.rotation;
                lookHead.rotation = Quaternion.AngleAxis(turn * (1f - neckShare), axis) * lookHead.rotation;
                look = lookHead.rotation * faceAhead;
                pitch = Mathf.Atan2(look.y, look.x * facing) * Mathf.Rad2Deg;
            }
            LookPitch = pitch;
        }

        // --- the body in the blow ---------------------------------------------------------------------------------

        Transform leanSpine, leanChest;
        float leanGain = 1f;
        float leanWeight;
        float leanAim;

        /// <summary>Degrees his trunk (the line from the hips to the neck) leans forward off the upright (back: negative),
        /// as it was posed this frame.</summary>
        public float TrunkLean { get; private set; }

        /// <summary>Degrees the trunk leans back off the upright striking straight up, and the most its lean is changed
        /// to get there (running he leans forward: he straightens some for the blow, he is not thrown back).</summary>
        public const float LeanBackMost = 12f, LeanTurnMost = 18f;

        /// <summary>How much (0..1) the trunk goes back into a blow upwards at attack progress <paramref name="t"/>: not
        /// in the wind-up (there he coils down for it), back as the blow rises, upright again as it ends.</summary>
        public static float LeanThrough(float t)
            => t < 0f ? 0f : Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.25f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.78f) / 0.22f));

        /// <summary>Where (degrees forward off the upright; back: negative) the trunk leans for a blow aimed
        /// <paramref name="aim"/> degrees above the level: back for one going up, the more the steeper; the rest is the
        /// blow's own pose (0).</summary>
        public static float LeanFor(float aim) => -LeanBackMost * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 80f, aim));

        /// <summary>The change of lean (degrees, back: negative) that takes a trunk leaning <paramref name="lean"/>
        /// degrees forward into a blow aimed <paramref name="aim"/> degrees above the level: only ever back, none for
        /// one leaning back that far already, and no more than half as much again as the blow asks of one standing
        /// upright (<see cref="LeanTurnMost"/> for a blow straight up).</summary>
        public static float LeanTurn(float lean, float aim)
        {
            float to = LeanFor(aim);
            return Mathf.Clamp(to - lean, to * (LeanTurnMost / LeanBackMost), 0f);
        }

        /// <summary>
        /// The body goes with the blow: cutting, thrusting, lashing upwards the trunk rises and goes back into it. (The
        /// keys had it lean back; over the legs' own motion it stood upright or a little forward: 1 to 8 degrees forward
        /// thrusting the spear straight up.) Half the turn is taken at the waist and half at the chest, about the axis
        /// across his body; the arms keep the lines the blow gave them, so the weapon goes where it went. Not the bow (an
        /// archer stays upright) nor the shield (he braces under it), and a blow downwards bends him over by itself.
        /// </summary>
        void LeanIntoTheBlow(in HeroState s, float dt)
        {
            if (leanSpine == null || leanChest == null || lookNeck == null) return;
            bool blow = EyesOnTheBlow && s.attacking && !s.lowBlow && s.attackForm != VeilForm.Bow && s.attackForm != VeilForm.Shield
                        && !s.hanging && !s.climbing && !s.swinging && !s.wallSliding && !s.rolling && !s.sliding;
            float want = blow && LeanFor(s.attackAim) < 0f ? LeanThrough(s.attackT) : 0f;
            if (want > 0f) leanAim = s.attackAim;
            leanWeight = Mathf.Lerp(leanWeight, want, 1f - Mathf.Exp(-(want > leanWeight ? LookIn : LookOut) * dt));
            if (leanWeight < 1e-3f) leanWeight = 0f;
            float facing = player.BodyFacing >= 0 ? 1f : -1f;
            Vector3 line = lookNeck.position - hips.position;
            float lean = Mathf.Atan2(line.x * facing, line.y) * Mathf.Rad2Deg;
            // (only on his feet or in a jump: not head down in a somersault)
            if (leanWeight > 0f && line.y > 0.5f * line.magnitude)
            {
                float turn = LeanTurn(lean, leanAim) * leanWeight * leanGain;
                // (about the axis across the body: seen from the side, anticlockwise is back for one facing right)
                var half = Quaternion.AngleAxis(-turn * 0.5f, Vector3.forward * facing);
                Quaternion armL = upperArmL != null ? upperArmL.rotation : Quaternion.identity;
                Quaternion armR = upperArmR != null ? upperArmR.rotation : Quaternion.identity;
                leanSpine.rotation = half * leanSpine.rotation;
                leanChest.rotation = half * leanChest.rotation;
                if (upperArmL != null) upperArmL.rotation = armL;
                if (upperArmR != null) upperArmR.rotation = armR;
                line = lookNeck.position - hips.position;
                lean = Mathf.Atan2(line.x * facing, line.y) * Mathf.Rad2Deg;
            }
            TrunkLean = lean;
        }

        // --- firm footfalls ---------------------------------------------------------------------------------------

        sealed class Footfall
        {
            public Transform upper, lower, foot, ball;
            public int footPoints, toePoints;   // (their skin: indices into contactBones)
            public bool down;
            public float heldX;     // where the ball of the foot was set down (world x)
            public float lag;       // lifted: how far (world x) it still is behind where the pose has it - it catches up
            public float lastRel;   // where the pose had it last frame, from the body (x)
            public float slipped;   // metres it has slipped since it was set down (the leg stretched out)
            public bool early;      // lifted because the leg could hold it no longer (until it has caught up with the pose)
        }

        readonly Footfall[] footfalls = new Footfall[2];
        float floorY;
        bool onFloor;

        /// <summary>A foot counts as set down with its sole within this (m) of the floor, as lifted again over this.
        /// (Generous: the captured left foot steps a few centimetres higher than the right, and at 3 cm it was hardly
        /// ever planted. A foot low over the floor in its swing is not taken for a footfall: it is going forward.)</summary>
        public const float FootDownAt = 0.07f, FootUpAt = 0.11f;
        /// <summary>Seconds in which a lifted foot catches up with where the pose has it.</summary>
        public const float FootCatchUp = 0.06f;
        /// <summary>The share of the leg's length a planted foot may stretch it to: past it the foot slips after him.</summary>
        public const float LegStretch = 0.99f;
        /// <summary>The feet are planted from this speed (m/s); a foot is carrying him while it goes back under him at
        /// this share of his speed at least.</summary>
        public const float FootfallFrom = 0.4f, FootfallBack = 0.25f;
        /// <summary>A planted foot that has slipped this far (m) - the leg at full stretch - is lifted; lifted so, it is
        /// flicked up to this height (m) over where the pose has it while it catches up.</summary>
        public const float FootSlip = 0.03f, FootFlick = 0.05f;

        /// <summary>Firm footfalls on (off: the feet as the pose has them - to compare).</summary>
        public bool FirmFootfalls { get; set; } = true;

        /// <summary>Whether the foot (0 left, 1 right) is planted on the floor right now.</summary>
        public bool FootDown(int side) => footfalls[side] != null && footfalls[side].down;
        /// <summary>How far (m) the planted foot has slipped since it was set down (the leg at full stretch).</summary>
        public float FootSlipped(int side) => footfalls[side] != null ? footfalls[side].slipped : 0f;

        void SetupFootfalls()
        {
            for (int k = 0; k < 2; k++)
            {
                string side = k == 0 ? "L" : "R";
                var f = new Footfall
                {
                    upper = RigJoint.FindDeep(model, "UpperLeg." + side), lower = RigJoint.FindDeep(model, "LowerLeg." + side),
                    foot = RigJoint.FindDeep(model, "Foot." + side), ball = RigJoint.FindDeep(model, "Toe." + side),
                    footPoints = System.Array.IndexOf(ContactBones, "Foot." + side), toePoints = System.Array.IndexOf(ContactBones, "Toe." + side),
                };
                if (f.ball == null) f.ball = f.foot;
                footfalls[k] = f.upper != null && f.lower != null && f.foot != null && contactBones != null ? f : null;
            }
        }

        /// <summary>The lowest point (world y) of the skin one contact bone carries.</summary>
        float LowestOf(int c)
        {
            var b = contactBones[c];
            if (b == null) return float.PositiveInfinity;
            var m = b.localToWorldMatrix;
            float y = float.PositiveInfinity;
            foreach (var p in contactPoints[c]) y = Mathf.Min(y, m.MultiplyPoint3x4(p).y);
            return y;
        }

        /// <summary>
        /// Whether a foot carries him: its sole <paramref name="sole"/> m over the floor, going back under him at
        /// <paramref name="backward"/> m/s in the pose, he moving at <paramref name="speed"/> m/s. Set down
        /// (<paramref name="down"/>) it stays so until it is lifted clear or swings forward again.
        /// </summary>
        public static bool Carries(bool down, float sole, float backward, float speed)
        {
            if (down) return sole <= FootUpAt && backward >= 0f;
            return sole < FootDownAt && backward > FootfallBack * speed;
        }

        /// <summary>
        /// Where (x) a foot planted at <paramref name="held"/> can stay, the hip at <paramref name="hipX"/> and
        /// <paramref name="drop"/> m off it in the other directions, the leg <paramref name="leg"/> m long: where it
        /// was set down while the leg reaches it, else as near as the leg stretches (it slips after him).
        /// </summary>
        public static float PlantedAt(float held, float hipX, float drop, float leg)
        {
            float reach = leg * LegStretch;
            float span = Mathf.Sqrt(Mathf.Max(0f, reach * reach - drop * drop));
            return Mathf.Clamp(held, hipX - span, hipX + span);
        }

        /// <summary>
        /// Firm footfalls: the foot that carries him stays where it was set down, and the leg is turned to reach it.
        /// The captured gait is played at its own tempo whatever his speed (legs pattering at a run looked wrong), so
        /// its steps are shorter than the ground he covers: the standing foot slid back under him by a quarter of the
        /// way - 0.25 m a step at a walk, 0.5 m at a jog. Planted, the step is as long as it needs to be, as far as
        /// the leg reaches; lifted, the foot catches up with the pose in a moment.
        /// </summary>
        /// <summary>A foot (0 or 1) is set down on the floor, at this speed (m/s): a footstep.</summary>
        public event System.Action<int, float> FootSetDown;

        void PlantFeet(in HeroState s, float dt)
        {
            if (footfalls[0] == null || footfalls[1] == null) return;
            bool walking = FirmFootfalls && onFloor && s.grounded && s.speed > FootfallFrom && !s.dashing && !s.crouching && !s.sliding && !s.rolling
                           && !s.hanging && !s.climbing && !s.swinging && !s.wallSliding;
            float bodyX = player.transform.position.x;
            float going = Mathf.Sign(player.Velocity.x);
            for (int k = 0; k < 2; k++)
            {
                var f = footfalls[k];
                // (the ball of the foot is what stays put: the heel comes down onto it and rolls up off it, as the pose has it)
                Vector3 at = f.foot.position;
                float ballX = f.ball.position.x;
                float rel = ballX - bodyX;
                float backward = dt > 0f ? -(rel - f.lastRel) / dt * going : 0f;   // (> 0: the foot goes back under him)
                f.lastRel = rel;
                bool carries = walking && Carries(f.down, Mathf.Min(LowestOf(f.footPoints), LowestOf(f.toePoints)) - floorY, backward, s.speed);
                if (carries && !f.down && !f.early)
                {
                    f.down = true;
                    FootSetDown?.Invoke(k, s.speed);
                    f.heldX = ballX + f.lag;
                    f.slipped = 0f;
                }
                else if (!carries && f.down)
                {
                    f.down = false;
                    f.lag = f.heldX - ballX;
                }
                Vector3 target = at;
                if (f.down)
                {
                    Vector3 hip = f.upper.position;
                    float leg = Vector3.Distance(hip, f.lower.position) + Vector3.Distance(f.lower.position, at);
                    float drop = Mathf.Sqrt((at.y - hip.y) * (at.y - hip.y) + (at.z - hip.z) * (at.z - hip.z));
                    float ankle = at.x + (f.heldX - ballX);   // (the ankle, with the ball where it was set down)
                    float x = PlantedAt(ankle, hip.x, drop, leg);
                    f.slipped += Mathf.Abs(x - ankle);
                    f.heldX += x - ankle;
                    target.x = x;
                    // The leg at full stretch and the foot still down in the pose: it has carried him as far as it can.
                    // It leaves the ground now, a little early, rather than be dragged after him.
                    if (f.slipped > FootSlip)
                    {
                        f.down = false;
                        f.lag = f.heldX - ballX;
                        f.early = true;
                    }
                }
                if (!f.down)
                {
                    f.lag = Mathf.Abs(f.lag) < 1e-4f ? 0f : Mathf.Lerp(f.lag, 0f, 1f - Mathf.Exp(-dt / FootCatchUp));
                    target.x = at.x + f.lag;
                    // (lifted early it is flicked up off the floor as it catches up, not slid along it)
                    if (f.early) target.y += FootFlick * Mathf.Clamp01(Mathf.Abs(f.lag) / 0.25f);
                    if (Mathf.Abs(f.lag) < 0.01f) f.early = false;
                }
                if (Mathf.Abs(target.x - at.x) < 1e-4f && Mathf.Abs(target.y - at.y) < 1e-4f) continue;
                // (the knee keeps bending the way it bends in the pose; the foot keeps its own turn - heel, sole, toe)
                Quaternion kept = f.foot.rotation;
                Vector3 knee = f.lower.position;
                Vector3 bend = knee - 0.5f * (f.upper.position + at);
                Vector3 pole = knee + (bend.sqrMagnitude > 1e-6f ? bend.normalized : new Vector3(going, 0f, 0f)) * 0.6f;
                LimbReach.Solve(f.upper, f.lower, f.foot, target, pole, 1f);
                f.foot.rotation = kept;
            }
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
                if (IsCreature(h.collider)) continue;   // creatures are no walls
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
            // (coming over a lip he is on his way onto that wall's top: there the lip's own rules keep the legs out of the
            // stone - off its face below it, on its top above it)
            bool free = !s.swinging && bodyBones != null && !(s.climbing && s.climbProgress > HeroPoseSolver.ClimbPressEnd);
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
        Transform braceFingerL, braceFingerR;

        readonly System.Collections.Generic.Dictionary<Collider, bool> creatureColliders = new System.Collections.Generic.Dictionary<Collider, bool>();

        /// <summary>A creature's collider (creatures are no walls): looked up once per collider, then remembered.</summary>
        bool IsCreature(Collider c)
        {
            if (!creatureColliders.TryGetValue(c, out bool creature))
            {
                creature = c.GetComponentInParent<IHostile>() != null;
                if (creatureColliders.Count > 256) creatureColliders.Clear();   // (a level's worth: forget the old ones)
                creatureColliders[c] = creature;
            }
            return creature;
        }

        static void BraceHandOnWall(Transform upper, Transform lower, Transform hand, Transform finger, float face, int side, Vector3 away)
        {
            if (upper == null) return;
            var target = new Vector3(face - side * BraceHand, hand.position.y, hand.position.z);
            LimbReach.Solve(upper, lower, hand, target, upper.position + away * 0.5f + Vector3.down * 0.4f, 1f);
            // the palm flat on the stone, the fingers up it (reaching straight at it they went 0.17 m into it)
            if (finger != null)
            {
                Vector3 along = finger.position - hand.position;
                if (along.sqrMagnitude > 1e-6f) hand.rotation = Quaternion.FromToRotation(along, Vector3.up) * hand.rotation;
            }
        }

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
                if (IsCreature(h.collider)) continue;
                if (h.distance < nearest) { nearest = h.distance; face = h.point.x; }
            }
            if (float.IsNaN(face)) return;
            var away = new Vector3(-side, 0f, 0f);
            // both hands on the stone (the high one braced, the low one pressing: it stuck 0.3 m into it), the elbows down
            // and away from it
            // (the bones are found at setup: a little array of the two arms, the hand's name and a search for its finger
            // each frame were garbage)
            BraceHandOnWall(upperArmL, lowerArmL, handL, braceFingerL, face, side, away);
            BraceHandOnWall(upperArmR, lowerArmR, handR, braceFingerR, face, side, away);
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
