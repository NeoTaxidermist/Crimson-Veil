using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Hooded monk behind a stone shield. Walks slowly at the hero and bashes him with the shield when he is close;
    /// a little further off he whirls his flail up over his head and brings it down in front of him with a step
    /// (over his head too: jumping over him is no longer safe while he swings).
    /// From the front he is untouchable — get behind him (jump over, wall-jump past) or tear the shield
    /// away with the whip's lash; for a few seconds he is then open from every side (and too shaken to swing).
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class ShieldMonk : CreatureBase
    {
        [SerializeField] float walkSpeed = 1.3f;
        [SerializeField] float sightRange = 8f;
        [SerializeField] float bashRange = 1.7f;
        [SerializeField] float windupTime = 0.65f;
        [SerializeField] float bashDamage = 1f;
        [SerializeField] float disarmTime = 3.2f;
        [SerializeField] float turnTime = 0.9f;
        [SerializeField] float flailRange = 2.8f;
        [SerializeField] float flailDamage = 1f;
        [SerializeField] float flailCooldown = 2.4f;

        /// <summary>Seconds he whirls the flail up before the blow (the hero's warning), and the blow itself.</summary>
        public const float FlailWindupTime = 0.8f, FlailSwingTime = 0.4f;

        public int Facing { get; private set; } = 1;
        public bool ShieldUp => disarmTimer <= 0f;
        public enum State { Walk, Windup, Bash, Recover, FlailWindup, FlailSwing }
        public State Current { get; private set; }

        /// <summary>The flail's head, in the world (null on the primitive stand-in without a flail).</summary>
        public Transform FlailHead => flailHead;

        /// <summary>The haft's tip, where the chain hangs from (zero without a flail).</summary>
        public Vector3 FlailTipPosition => chain != null ? FlailTip() : Vector3.zero;

        /// <summary>He sees the hero (in front of him, on his level) or is in the middle of a bash.</summary>
        public override bool EngagedWithHero => !IsDead && HasTarget
            && (Current != State.Walk || (Mathf.Abs(target.Position.x - transform.position.x) < sightRange
                                          && Mathf.Abs(target.Position.y - transform.position.y) < 3f));

        float home, stateTime, disarmTimer, turnTimer, fall, flailTimer;
        Transform shield, visual;
        CreatureTurn turn = new CreatureTurn(1);

        protected override void Awake()
        {
            maxHealth = 4f;
            base.Awake();
            var cap = GetComponent<CapsuleCollider>();
            cap.isTrigger = true;
            home = transform.position.x;
            visual = CreatureParts.Find(transform, "Visual");
            shield = CreatureParts.Find(transform, "Shield");
            // (a rigged model brings its shield inside the model: it is moved in the Visual's frame, so it goes there)
            if (shield != null && visual != null && shield.parent != visual) shield.SetParent(visual, true);
            // (its rest is taken in the world: facing +x, the shield's face towards the camera)
            if (shield != null) shieldRest = shield.rotation;
            SetupRig();
            SetupFlail();
            lastX = transform.position.x;
        }

        CrimsonVeil.Core.RigJoint spineJ, chestJ, headJ, armLJ, elbowLJ, armRJ, elbowRJ;
        Transform handL, handR, lowerArmL, lowerArmR;
        CrimsonVeil.Player.HeroHands hands;
        CreaturePose posed;
        float phase, lastX, moving;

        // the flail: its haft in the free fist, the links and the head hung on a simulated chain
        Transform flailHaft, flailHead;
        Transform[] flailLinks;
        Vector3 haftAxis, linkAxis;
        float haftLength;
        FlailChain chain;
        bool chainPlaced;

        /// <summary>The model's flail parts (none on the primitive stand-in): moved under the Visual, measured, and a
        /// chain made of their sizes.</summary>
        void SetupFlail()
        {
            flailHaft = visual != null ? CreatureParts.Find(visual, "Flail") : null;
            flailHead = visual != null ? CreatureParts.Find(visual, "FlailHead") : null;
            if (flailHaft == null || flailHead == null) { flailHaft = flailHead = null; return; }
            var links = new System.Collections.Generic.List<Transform>();
            for (int i = 0; ; i++)
            {
                var l = CreatureParts.Find(visual, "FlailLink." + i);
                if (l == null) break;
                links.Add(l);
            }
            flailLinks = links.ToArray();
            foreach (var t in links) t.SetParent(visual, true);
            flailHaft.SetParent(visual, true);
            flailHead.SetParent(visual, true);
            haftAxis = LongAxis(flailHaft, out float haftHalf, out float haftCentre);
            haftLength = (haftCentre + haftHalf) * flailHaft.lossyScale.y;
            float linkLen = 0.075f;
            if (flailLinks.Length > 0) { linkAxis = LongAxis(flailLinks[0], out float lh, out _); linkLen = 2f * lh * flailLinks[0].lossyScale.y; }
            float headR = 0.1f;
            var hm = flailHead.GetComponent<MeshFilter>();
            if (hm != null && hm.sharedMesh != null) headR = hm.sharedMesh.bounds.extents.x * flailHead.lossyScale.x * 0.75f;
            var seg = new float[flailLinks.Length + 1];
            for (int i = 0; i < flailLinks.Length; i++) seg[i] = linkLen * 0.8f;   // (the links lie in each other)
            seg[seg.Length - 1] = headR + linkLen * 0.4f;
            chain = new FlailChain(seg, FlailTip());
        }

        /// <summary>The local axis along a part's longest size (its sign towards the far end) and the half length.</summary>
        static Vector3 LongAxis(Transform t, out float half, out float centre)
        {
            half = 0.1f; centre = 0f;
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return Vector3.up;
            var b = mf.sharedMesh.bounds;
            var e = b.extents;
            int k = e.x >= e.y && e.x >= e.z ? 0 : (e.y >= e.z ? 1 : 2);
            var axis = Vector3.zero; axis[k] = 1f;
            half = e[k]; centre = b.center[k];
            if (centre < 0f) { axis = -axis; centre = -centre; }
            return axis;
        }

        /// <summary>The world rotation that lays a part's local <paramref name="axis"/> along <paramref name="dir"/>,
        /// turned <paramref name="roll"/> degrees round it.</summary>
        static Quaternion Lay(Vector3 axis, Vector3 dir, float roll = 0f) =>
            Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.AngleAxis(roll, Vector3.up) * Quaternion.FromToRotation(axis, Vector3.up);

        /// <summary>Which hand is free (the shield is held by the arm on the camera's side).</summary>
        Transform FreeHand(out Transform forearm)
        {
            float side = visual != null ? visual.TransformDirection(Vector3.right).x : Facing;
            forearm = side >= 0f ? lowerArmL : lowerArmR;
            return side >= 0f ? handL : handR;
        }

        Vector3 FlailTip() => flailHaft.position + flailHaft.rotation * haftAxis * haftLength;

        /// <summary>The rigged model's body joints (none on the primitive stand-in).</summary>
        void SetupRig()
        {
            var model = visual != null ? CreatureParts.Find(visual, "Model") : null;
            if (model == null) return;
            Transform F(string n) => CrimsonVeil.Core.RigJoint.FindDeep(model, n);
            Vector3 up = model.up, forward = model.forward;
            Vector3 lateral = Vector3.Cross(up, forward).normalized;
            CrimsonVeil.Core.RigJoint J(string bone, string child)
            {
                var b = F(bone); var c = F(child);
                return b == null || c == null ? null : CrimsonVeil.Core.RigJoint.Create(b, lateral, () => c.position, forward);
            }
            spineJ = J("Spine", "Chest"); chestJ = J("Chest", "Head");
            var h = F("Head");
            headJ = h == null ? null : CrimsonVeil.Core.RigJoint.Create(h, lateral, () => h.position + h.up * 0.2f, forward);
            armLJ = J("UpperArm.L", "LowerArm.L"); armRJ = J("UpperArm.R", "LowerArm.R");
            elbowLJ = J("LowerArm.L", "Hand.L"); elbowRJ = J("LowerArm.R", "Hand.R");
            handL = F("Hand.L"); handR = F("Hand.R"); lowerArmL = F("LowerArm.L"); lowerArmR = F("LowerArm.R");
            var hips = F("Hips");
            if (hips != null)
            {
                var hh = new CrimsonVeil.Player.HeroHands(model, hips);
                if (hh.IsReady) hands = hh;   // (fists: one round the shield's grip, one round the flail's haft)
            }
        }

        /// <summary>0..1 through the current wind-up / bash / recovery.</summary>
        float StateProgress()
        {
            switch (Current)
            {
                case State.Windup: return Mathf.Clamp01(stateTime / windupTime);
                case State.Bash: return Mathf.Clamp01(stateTime / 0.25f);
                case State.Recover: return Mathf.Clamp01(stateTime / 0.7f);
                case State.FlailWindup: return Mathf.Clamp01(stateTime / FlailWindupTime);
                case State.FlailSwing: return Mathf.Clamp01(stateTime / FlailSwingTime);
                default: return 0f;
            }
        }

        Quaternion shieldRest = Quaternion.identity;

        /// <summary>How far (degrees) the shield's face is turned from the camera towards the way he faces.</summary>
        public const float ShieldTurn = 40f;

        /// <summary>
        /// Where the shield is held, in the world, for a body turned <paramref name="fx"/> (+1 facing +x .. -1 facing -x,
        /// in between while he turns round): in front of him on the side he faces, kept on the camera's side of the body
        /// (swung wide in front of it as he turns), its face turned towards his front - so whichever way he faces, the
        /// hero sees the shield's face, never its back.
        /// </summary>
        public static void ShieldHold(Vector3 local, float fx, Quaternion rest, out Vector3 offset, out Quaternion rotation)
        {
            float side = Mathf.Abs(fx);
            offset = new Vector3(fx * local.x, local.y, local.z - 0.3f * (1f - side));
            rotation = Quaternion.Euler(0f, -ShieldTurn * fx, 0f) * rest;
        }

        protected override void Tick(float dt)
        {
            if (IsDead) return;
            stateTime += dt;
            disarmTimer = Mathf.Max(0f, disarmTimer - dt);
            flailTimer = Mathf.Max(0f, flailTimer - dt);
            var pos = transform.position;

            float dx = HasTarget ? target.Position.x - pos.x : 0f;
            bool sees = HasTarget && Mathf.Abs(dx) < sightRange && Mathf.Abs(target.Position.y - pos.y) < 3f;

            // He turns slowly: that is the opening to get behind him.
            int want = sees ? (dx >= 0f ? 1 : -1) : Facing;
            if (!sees && Mathf.Abs(pos.x - home) > 3f) want = pos.x > home ? -1 : 1;
            if (want != Facing && Current == State.Walk)
            {
                turnTimer += dt;
                if (turnTimer >= turnTime) { Facing = want; turnTimer = 0f; }
            }
            else turnTimer = 0f;

            float vx = 0f;
            switch (Current)
            {
                case State.Walk:
                {
                    var attack = ShieldMonkLogic.Choose(sees && want == Facing, Mathf.Abs(dx), bashRange, flailRange,
                                                        ShieldUp, flailTimer <= 0f && chain != null);
                    if (attack == State.Windup || attack == State.FlailWindup) Enter(attack);
                    else if (turnTimer <= 0f) vx = Facing * walkSpeed * (sees ? 1f : 0.5f);
                    break;
                }
                case State.FlailWindup:
                    if (!ShieldUp) Enter(State.Recover);           // torn out of it: no blow
                    else if (stateTime >= FlailWindupTime) Enter(State.FlailSwing);
                    break;
                case State.FlailSwing:
                {
                    // a heavy step into the blow; the head hurts on its way down in front of him
                    float t = stateTime / FlailSwingTime;
                    vx = Facing * 4f * (1f - Mathf.Clamp01(t));
                    if (chain != null && FlailChain.Striking(FlailChain.SwingAngle(t)))
                        TouchDamage(chain.Head, 0.45f, flailDamage, new Vector2(Facing * 7f, 5f), DamageKind.Bash);
                    if (stateTime >= FlailSwingTime) { flailTimer = flailCooldown; Enter(State.Recover); }
                    break;
                }
                case State.Windup:
                    if (stateTime >= windupTime) Enter(State.Bash);
                    break;
                case State.Bash:
                    vx = Facing * 5f * (1f - stateTime / 0.25f);
                    TouchDamage(pos + new Vector3(Facing * 0.8f, 1.1f, 0f), 1f, bashDamage, new Vector2(Facing * 9f, 4f), DamageKind.Bash);
                    if (stateTime >= 0.25f) Enter(State.Recover);
                    break;
                case State.Recover:
                    if (stateTime >= 0.7f) Enter(State.Walk);
                    break;
            }
            if (!ShieldUp) vx *= 0.4f; // without the shield he staggers about

            // Walk on the ground, never off a ledge or into a wall.
            if (vx != 0f)
            {
                int dir = vx > 0f ? 1 : -1;
                bool wall = Physics.Raycast(pos + Vector3.up, new Vector3(dir, 0f, 0f), 0.6f, ~0, QueryTriggerInteraction.Ignore);
                bool floor = !float.IsNaN(GroundBelow(pos + new Vector3(dir * 0.6f, 0.5f, 0f), 1.2f));
                if (!wall && floor) pos.x += vx * dt;
            }
            float g = GroundBelow(pos + Vector3.up * 0.8f, 3f);
            if (!float.IsNaN(g) && pos.y - g < 0.15f) { pos.y = Mathf.MoveTowards(pos.y, g, 8f * dt); fall = 0f; }
            else { fall -= 25f * dt; pos.y += fall * dt; }
            transform.position = pos;

            Animate(dt);
            TouchDamage(pos + Vector3.up, 0.55f, contactDamage, new Vector2(Mathf.Sign(dx) * 5f, 3f));
        }

        void Enter(State s) { Current = s; stateTime = 0f; }

        protected override bool AcceptHit(in DamageInfo info)
        {
            float attackerX = info.source != null ? info.source.transform.position.x : info.point.x;
            bool front = ShieldMonkLogic.FromFront(Facing, transform.position.x, attackerX);
            if (ShieldMonkLogic.Disarms(info.kind, front, ShieldUp))
            {
                disarmTimer = disarmTime;
                CombatFx.Burst(transform.position + new Vector3(Facing * 0.6f, 1.1f, 0f), new Vector2(-Facing, 1f), CombatFx.Ink, 20, 5f);
                return false; // the lash takes the shield, not his life
            }
            if (ShieldMonkLogic.Blocks(Facing, transform.position.x, attackerX, ShieldUp))
            {
                CombatFx.Burst(info.point, new Vector2(-Facing, 0.5f), CombatFx.Crimson, 8, 3f);
                CombatFx.Shake(0.15f);
                return false;
            }
            return true;
        }

        void Animate(float dt)
        {
            // The heavy walk is driven by the distance walked; blows are staged (see CreatureAnimation.Monk).
            float x = transform.position.x, moved = Mathf.Abs(x - lastX);
            lastX = x;
            moving = Mathf.Lerp(moving, dt > 0f ? Mathf.Clamp01(moved / dt / walkSpeed) : 0f, 1f - Mathf.Exp(-6f * dt));
            phase = Mathf.Repeat(phase + moved / CreatureAnimation.MonkStride, 1f);
            var pose = CreatureAnimation.Monk(Current, StateProgress(), moving, phase, ShieldUp, Time.time);
            float k = 1f - Mathf.Exp(-(Current == State.Bash ? 30f : 12f) * dt);
            posed.spine = Mathf.Lerp(posed.spine, pose.spine, k); posed.chest = Mathf.Lerp(posed.chest, pose.chest, k);
            posed.head = Mathf.Lerp(posed.head, pose.head, k);
            posed.armL = Mathf.Lerp(posed.armL, pose.armL, k); posed.elbowL = Mathf.Lerp(posed.elbowL, pose.elbowL, k);
            posed.armR = Mathf.Lerp(posed.armR, pose.armR, k); posed.elbowR = Mathf.Lerp(posed.elbowR, pose.elbowR, k);
            posed.lift = Mathf.Lerp(posed.lift, pose.lift, k); posed.lunge = Mathf.Lerp(posed.lunge, pose.lunge, k);

            if (visual != null)
            {
                // He turns his whole body round (heavy, slow), not a mirrored flip.
                visual.localRotation = turn.Step(Facing, dt, 0.5f);
                visual.localPosition = new Vector3(posed.lunge * Facing, posed.lift, 0f);
            }
            // The shield arm is the one on the camera's side (the right facing +x, the left facing -x).
            float side = visual != null ? visual.TransformDirection(Vector3.right).x : Facing;
            var shieldArm = side >= 0f ? armRJ : armLJ;
            var shieldElbow = side >= 0f ? elbowRJ : elbowLJ;
            var freeArm = side >= 0f ? armLJ : armRJ;
            var freeElbow = side >= 0f ? elbowLJ : elbowRJ;
            spineJ?.Set(posed.spine); chestJ?.Set(posed.chest); headJ?.Set(posed.head);
            shieldArm?.Set(posed.armL); shieldElbow?.Set(posed.elbowL);
            freeArm?.Set(posed.armR); freeElbow?.Set(posed.elbowR);
            hands?.Apply(1f, 1f, dt);
            AnimateFlail(dt);
            if (shield == null) return;
            // Up in front; raised for the bash; torn away and hanging low when disarmed.
            Vector3 up = new Vector3(0.55f, 1.05f, -0.15f), raised = new Vector3(0.75f, 1.3f, -0.15f), down = new Vector3(-0.1f, 0.35f, 0.25f);
            Vector3 targetPos = !ShieldUp ? down : Current == State.Windup ? Vector3.Lerp(up, raised, stateTime / windupTime) : Current == State.Bash ? up + Vector3.right * 0.3f : up;
            // Held in the world frame: it goes round in front of him as he turns, its face always towards his front.
            float fx = visual != null ? visual.TransformDirection(Vector3.right).x : Facing;
            ShieldHold(targetPos, fx, shieldRest, out var offset, out var hold);
            // Torn away, it tips over (away from where he faces).
            float tilt = ShieldUp ? 0f : 70f;
            var wantRot = Quaternion.Euler(0f, 0f, tilt * (fx >= 0f ? 1f : -1f)) * hold;
            Vector3 at = transform.position + (visual != null ? visual.localPosition : Vector3.zero) + offset;
            // Up, it is in the shield arm's fist (its grip behind its face: the face before the fist, towards the camera)
            var shieldHand = side >= 0f ? handR : handL;
            if (ShieldUp && shieldHand != null) at = shieldHand.position + new Vector3(Facing * 0.04f, 0.02f, -0.16f);
            shield.position = Vector3.Lerp(shield.position, at, 1f - Mathf.Exp(-16f * dt));
            shield.rotation = Quaternion.Slerp(shield.rotation, wantRot, 1f - Mathf.Exp(-10f * dt));
        }

        /// <summary>
        /// The flail follows the free fist: the haft held out along the forearm (raised towards the chain in a blow),
        /// the chain swinging from its tip; whirled up and brought down, the head is drawn along the blow's arc.
        /// </summary>
        void AnimateFlail(float dt)
        {
            if (chain == null) return;
            var hand = FreeHand(out var forearm);
            if (hand == null) return;
            Vector3 fore = forearm != null ? (hand.position - forearm.position).normalized : Vector3.down;
            Vector3 front = new Vector3(Facing, 0f, 0f);
            Vector3 grip = hand.position + fore * 0.06f;
            bool swinging = Current == State.FlailWindup || Current == State.FlailSwing;
            Vector3 dir = swinging ? (chain.Head - grip).normalized : (fore + Vector3.up * 1.2f + front * 0.4f).normalized;
            if (dir.sqrMagnitude < 0.5f) dir = Vector3.up;
            flailHaft.SetPositionAndRotation(grip, Quaternion.Slerp(flailHaft.rotation, Lay(haftAxis, dir), swinging ? 1f : 1f - Mathf.Exp(-14f * dt)));
            Vector3 tip = FlailTip();
            if (!chainPlaced) { chain.Reset(tip); chainPlaced = true; }   // (hung from the fist he holds it in, not from the model's rest)
            float pull = 0f;
            Vector3 target = default;
            if (swinging)
            {
                float t = StateProgress();
                float deg = Current == State.FlailWindup ? FlailChain.WindupAngle(t) : FlailChain.SwingAngle(t);
                target = FlailChain.ArcPoint(tip, Facing, deg, chain.Length * 0.95f);
                pull = Current == State.FlailWindup ? 0.35f : 0.7f;
            }
            chain.Step(tip, Mathf.Min(dt, 1f / 30f), 9.81f, 0.985f, target, pull);
            for (int i = 0; i < flailLinks.Length && i + 1 < chain.Count; i++)
            {
                var a = chain[i]; var b = chain[i + 1];
                var d = b - a;
                flailLinks[i].SetPositionAndRotation((a + b) * 0.5f, Lay(linkAxis, d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.down, (i % 2) * 90f));
            }
            var head = chain.Head;
            var back = chain[chain.Count - 2] - head;
            flailHead.SetPositionAndRotation(head, Lay(haftAxis, back.sqrMagnitude > 1e-8f ? back.normalized : Vector3.up));
        }

        /// <summary>Builds the monk's silhouette under <paramref name="root"/>.</summary>
        public static void BuildVisual(Transform root, Material ink, Material accent)
        {
            var v = CreatureParts.Group(root, "Visual", Vector3.zero);
            CreatureParts.Part(v, PrimitiveType.Cylinder, "Robe", new Vector3(0f, 0.75f, 0f), new Vector3(0.75f, 0.75f, 0.6f), ink);
            CreatureParts.Part(v, PrimitiveType.Sphere, "Shoulders", new Vector3(0f, 1.4f, 0f), new Vector3(0.75f, 0.45f, 0.55f), ink);
            CreatureParts.Part(v, PrimitiveType.Sphere, "Hood", new Vector3(0.05f, 1.75f, 0f), new Vector3(0.48f, 0.55f, 0.48f), ink);
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeL", new Vector3(0.2f, 1.72f, -0.2f), Vector3.one * 0.06f, accent);
            CreatureParts.Part(v, PrimitiveType.Sphere, "EyeR", new Vector3(0.12f, 1.72f, -0.22f), Vector3.one * 0.05f, accent);
            var shield = CreatureParts.Group(v, "Shield", new Vector3(0.55f, 1.05f, -0.15f), new Vector3(0f, 0f, 90f));
            CreatureParts.Part(shield, PrimitiveType.Cylinder, "Slab", Vector3.zero, new Vector3(1.0f, 0.06f, 1.0f), ink);
            CreatureParts.Part(shield, PrimitiveType.Cube, "RuneV", new Vector3(0f, -0.04f, -0.1f), new Vector3(0.06f, 0.02f, 0.55f), accent);
            CreatureParts.Part(shield, PrimitiveType.Cube, "RuneH", new Vector3(0f, -0.04f, -0.1f), new Vector3(0.4f, 0.02f, 0.06f), accent);
        }
    }

    /// <summary>The shield monk's rules, engine independent.</summary>
    public static class ShieldMonkLogic
    {
        /// <summary>Is the attacker on the side the monk is facing?</summary>
        public static bool FromFront(int facing, float monkX, float attackerX)
        {
            float dx = attackerX - monkX;
            return Mathf.Abs(dx) < 0.05f || Mathf.Sign(dx) == facing;
        }

        /// <summary>Hits from the front bounce off the raised shield.</summary>
        public static bool Blocks(int facing, float monkX, float attackerX, bool shieldUp) => shieldUp && FromFront(facing, monkX, attackerX);

        /// <summary>The whip's lash, from the front, tears the raised shield away.</summary>
        public static bool Disarms(DamageKind kind, bool fromFront, bool shieldUp) => shieldUp && fromFront && kind == DamageKind.Lash;

        /// <summary>
        /// What he starts while walking: the shield bash when the hero is close in front of him, the flail a little
        /// further off (only with his shield up - torn from it he is too shaken - and the flail's rest over),
        /// otherwise he keeps walking (<see cref="ShieldMonk.State.Walk"/>).
        /// </summary>
        public static ShieldMonk.State Choose(bool facesHero, float distance, float bashRange, float flailRange, bool shieldUp, bool flailReady)
        {
            if (!facesHero) return ShieldMonk.State.Walk;
            if (distance < bashRange) return ShieldMonk.State.Windup;
            if (distance < flailRange && shieldUp && flailReady) return ShieldMonk.State.FlailWindup;
            return ShieldMonk.State.Walk;
        }
    }
}
