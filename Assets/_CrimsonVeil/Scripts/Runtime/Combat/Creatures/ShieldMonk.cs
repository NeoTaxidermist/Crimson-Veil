using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Hooded monk behind a stone shield. Walks slowly at the hero and bashes him with the shield.
    /// From the front he is untouchable — get behind him (jump over, wall-jump past) or tear the shield
    /// away with the whip's lash; for a few seconds he is then open from every side.
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

        public int Facing { get; private set; } = 1;
        public bool ShieldUp => disarmTimer <= 0f;
        public enum State { Walk, Windup, Bash, Recover }
        public State Current { get; private set; }

        /// <summary>He sees the hero (in front of him, on his level) or is in the middle of a bash.</summary>
        public override bool EngagedWithHero => !IsDead && HasTarget
            && (Current != State.Walk || (Mathf.Abs(target.Position.x - transform.position.x) < sightRange
                                          && Mathf.Abs(target.Position.y - transform.position.y) < 3f));

        float home, stateTime, disarmTimer, turnTimer, fall;
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
            lastX = transform.position.x;
        }

        CrimsonVeil.Core.RigJoint spineJ, chestJ, headJ, armLJ, elbowLJ, armRJ, elbowRJ;
        CreaturePose posed;
        float phase, lastX, moving;

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
        }

        /// <summary>0..1 through the current wind-up / bash / recovery.</summary>
        float StateProgress()
        {
            switch (Current)
            {
                case State.Windup: return Mathf.Clamp01(stateTime / windupTime);
                case State.Bash: return Mathf.Clamp01(stateTime / 0.25f);
                case State.Recover: return Mathf.Clamp01(stateTime / 0.7f);
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
                    if (sees && Mathf.Abs(dx) < bashRange && want == Facing) Enter(State.Windup);
                    else if (turnTimer <= 0f) vx = Facing * walkSpeed * (sees ? 1f : 0.5f);
                    break;
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
            shield.position = Vector3.Lerp(shield.position, transform.position + (visual != null ? visual.localPosition : Vector3.zero) + offset, 1f - Mathf.Exp(-12f * dt));
            shield.rotation = Quaternion.Slerp(shield.rotation, wantRot, 1f - Mathf.Exp(-10f * dt));
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
    }
}
