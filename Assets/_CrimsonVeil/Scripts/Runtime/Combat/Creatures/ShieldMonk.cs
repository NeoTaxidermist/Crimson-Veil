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

        float home, stateTime, disarmTimer, turnTimer, fall;
        Transform shield, visual;

        protected override void Awake()
        {
            maxHealth = 4f;
            base.Awake();
            var cap = GetComponent<CapsuleCollider>();
            cap.isTrigger = true;
            home = transform.position.x;
            visual = CreatureParts.Find(transform, "Visual");
            shield = CreatureParts.Find(transform, "Shield");
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
            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.MoveTowards(s.x, Facing, dt * 4f);
                visual.localScale = s;
                // A heavy, swaying gait.
                visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.04f, 0f);
            }
            if (shield == null) return;
            // Up in front; raised for the bash; torn away and hanging low when disarmed.
            Vector3 up = new Vector3(0.55f, 1.05f, -0.15f), raised = new Vector3(0.75f, 1.3f, -0.15f), down = new Vector3(-0.1f, 0.35f, 0.25f);
            Vector3 targetPos = !ShieldUp ? down : Current == State.Windup ? Vector3.Lerp(up, raised, stateTime / windupTime) : Current == State.Bash ? up + Vector3.right * 0.3f : up;
            shield.localPosition = Vector3.Lerp(shield.localPosition, targetPos, 1f - Mathf.Exp(-12f * dt));
            float tilt = ShieldUp ? 0f : 70f;
            shield.localRotation = Quaternion.Slerp(shield.localRotation, Quaternion.Euler(0f, 0f, 90f + tilt), 1f - Mathf.Exp(-10f * dt));
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
