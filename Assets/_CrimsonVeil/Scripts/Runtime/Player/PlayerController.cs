using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// 2.5D player: 3D CharacterController locked to the XY plane, driven by <see cref="MotorLogic"/>.
    /// Probes walls and ledges with raycasts so wall slides and ledge grabs are stable.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] MotorSettings settings = new MotorSettings();
        [SerializeField] Transform visualRoot;
        [SerializeField] float turnSharpness = 22f;
        [SerializeField] float squashRecover = 12f;
        [SerializeField] float killY = -25f;
        [SerializeField] LayerMask environment = ~0;

        [Header("Ledge")]
        [Tooltip("Feet are this far below the ledge top while hanging.")]
        [SerializeField] float hangDrop = 1.95f;
        [SerializeField] float minLedgeHeight = 1.2f;
        [SerializeField] float maxLedgeHeight = 2.1f;

        [Header("Whip swing")]
        [Tooltip("Height of the hands holding the whip above the feet while hanging (arms up).")]
        [SerializeField] float gripHeight = 2.05f;

        /// <summary>Where the hands hold the whip (world).</summary>
        public Vector3 GripPosition => transform.position + Vector3.up * gripHeight;

        /// <summary>Latches the whip onto <paramref name="anchor"/> and starts swinging. False if out of reach.</summary>
        public bool TryGrapple(Vector3 anchor) => Motor.BeginSwing(anchor, GripPosition);

        [Header("Scarf ledge catch")]
        [Tooltip("The scarf catches ledges up to this height above the feet (the hands alone reach maxLedgeHeight).")]
        [SerializeField] float scarfLedgeHeight = 3.1f;
        [SerializeField] float scarfPullTime = 0.22f;
        /// <summary>Set by the veil: can the scarf reach out for a ledge right now (it is cloth and strong enough)?</summary>
        public System.Func<bool> ScarfLedgeReach;
        /// <summary>The scarf caught a ledge out of the hands' reach and is pulling the hero up to it.</summary>
        public event System.Action ScarfCaught;
        public bool ScarfCatching => scarfPull > 0f;
        /// <summary>The lip of the ledge the scarf caught.</summary>
        public Vector3 ScarfCatchPoint { get; private set; }
        bool scarfLedge;
        float scarfPull;
        Vector3 pullFrom, pullTo;

        public MotorLogic Motor { get; private set; }
        public IPlayerInput InputSource { get; set; }
        public Vector3 Velocity => new Vector3(Motor.Velocity.x, Motor.Velocity.y, 0f);
        public bool IsGrounded => Motor.IsGrounded;
        public MotorSettings Settings => settings;

        CharacterController controller;
        CollisionFlags lastFlags;
        float planeZ;
        Vector3 spawnPoint;
        Vector3 squash = Vector3.one;
        float yaw = 90f;
        float roll;
        Vector3 visualRest;

        // Ledge bookkeeping
        float probedLedgeTop, probedWallX;
        int probedLedgeSide;
        Vector3 climbStart, climbTarget;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Motor = new MotorLogic(settings);
            planeZ = transform.position.z;
            spawnPoint = transform.position;
            if (InputSource == null) InputSource = GetComponent<IPlayerInput>();
            if (visualRoot != null) visualRest = visualRoot.localPosition;
            Motor.SwingReleased += () => squash = new Vector3(0.84f, 1.18f, 0.84f);

            Motor.Jumped += () => squash = new Vector3(0.82f, 1.2f, 0.82f);
            Motor.WallJumped += () => squash = new Vector3(0.8f, 1.22f, 0.8f);
            Motor.Landed += () => squash = new Vector3(1.18f, 0.84f, 1.18f);
            Motor.Dashed += () => squash = new Vector3(1.25f, 0.85f, 1f);
            Motor.LedgeGrabbed += OnLedgeGrabbed;
            Motor.ClimbStarted += OnClimbStarted;
            Motor.ClimbFinished += OnClimbFinished;
        }

        void Update() => Simulate(Time.deltaTime);

        public void Simulate(float dt)
        {
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 1f / 30f);

            var input = new MotorInput();
            if (InputSource != null)
            {
                input.moveX = InputSource.MoveX;
                input.aimY = InputSource.AimY;
                input.jumpPressed = InputSource.JumpPressed;
                input.jumpHeld = InputSource.JumpHeld;
                input.dashPressed = InputSource.DashPressed;
            }

            var state = Motor.State;
            bool attached = state == MotorState.LedgeHang || state == MotorState.Climbing || state == MotorState.Swinging;
            int wall = attached ? 0 : ProbeWall(input.moveX);
            int ledge = 0;
            if (!attached && !controller.isGrounded && Motor.Velocity.y <= 2f)
            {
                int dir = Mathf.Abs(input.moveX) > 0.1f ? (input.moveX > 0f ? 1 : -1) : Motor.Facing;
                scarfLedge = false;
                if (ProbeLedge(dir, out probedLedgeTop, out probedWallX)) ledge = dir;
                // Out of the hands' reach, the scarf can still lash round the lip and haul the hero up.
                else if (ScarfLedgeReach != null && ScarfLedgeReach() && ProbeLedge(dir, scarfLedgeHeight, out probedLedgeTop, out probedWallX))
                {
                    ledge = dir;
                    scarfLedge = true;
                }
            }
            probedLedgeSide = ledge;

            var contacts = new MotorContacts
            {
                grounded = (!attached || state == MotorState.Swinging) && controller.isGrounded,
                ceiling = (lastFlags & CollisionFlags.Above) != 0,
                wallSide = wall,
                ledgeSide = ledge,
                gripPosition = GripPosition,
            };

            Vector2 v = Motor.Tick(dt, input, contacts);

            if (Motor.State == MotorState.LedgeHang && scarfPull > 0f)
            {
                scarfPull = Mathf.Max(0f, scarfPull - dt);
                SetPosition(Vector3.Lerp(pullFrom, pullTo, Mathf.SmoothStep(0f, 1f, 1f - scarfPull / scarfPullTime)));
            }
            else if (Motor.State == MotorState.Climbing)
            {
                SetPosition(ClimbPath(climbStart, climbTarget, Motor.ClimbProgress));
            }
            else if (Motor.State != MotorState.LedgeHang)
            {
                lastFlags = controller.Move(new Vector3(v.x, v.y, 0f) * dt);
            }

            var p = transform.position;
            if (Mathf.Abs(p.z - planeZ) > 0.0001f)
            {
                p.z = planeZ;
                SetPosition(p);
            }
            if (p.y < killY) Respawn();

            UpdateVisual(dt);
        }

        /// <summary>Wall in contact on the side we push towards (or either side), measured at two heights.</summary>
        int ProbeWall(float moveX)
        {
            int preferred = Mathf.Abs(moveX) > 0.1f ? (moveX > 0f ? 1 : -1) : 0;
            if (preferred != 0 && WallAt(preferred)) return preferred;
            if (WallAt(1)) return 1;
            if (WallAt(-1)) return -1;
            return 0;
        }

        bool WallAt(int dir)
        {
            float reach = controller.radius + 0.08f;
            Vector3 d = new Vector3(dir, 0f, 0f);
            Vector3 p = transform.position;
            return Hit(p + Vector3.up * 0.45f, d, reach, out var low) && Mathf.Abs(low.normal.y) < 0.4f
                && Hit(p + Vector3.up * 1.35f, d, reach, out var high) && Mathf.Abs(high.normal.y) < 0.4f;
        }

        /// <summary>
        /// A grabbable ledge: wall at chest height, free space above it, a flat top within hand reach
        /// and room to stand on it.
        /// </summary>
        public bool ProbeLedge(int dir, out float top, out float wallX) => ProbeLedge(dir, maxLedgeHeight, out top, out wallX);

        /// <summary>As above, for ledges up to <paramref name="maxHeight"/> above the feet.</summary>
        public bool ProbeLedge(int dir, float maxHeight, out float top, out float wallX)
        {
            float maxLedgeHeight = maxHeight;
            top = wallX = 0f;
            Vector3 p = transform.position;
            Vector3 d = new Vector3(dir, 0f, 0f);
            float reach = controller.radius + 0.35f;

            if (!Hit(p + Vector3.up * 1.3f, d, reach, out var wallHit) || Mathf.Abs(wallHit.normal.y) > 0.4f) return false;
            if (Hit(p + Vector3.up * (maxLedgeHeight + 0.15f), d, reach + 0.2f, out _)) return false;

            Vector3 down = new Vector3(wallHit.point.x + dir * 0.25f, p.y + maxLedgeHeight + 0.15f, p.z);
            if (!Hit(down, Vector3.down, maxLedgeHeight + 0.15f - minLedgeHeight + 0.05f, out var topHit) || topHit.normal.y < 0.7f) return false;

            float h = topHit.point.y - p.y;
            if (h < minLedgeHeight || h > maxLedgeHeight) return false;

            Vector3 standBottom = new Vector3(wallHit.point.x + dir * 0.5f, topHit.point.y + 0.35f, p.z);
            if (Physics.CheckCapsule(standBottom, standBottom + Vector3.up * 1.2f, 0.25f, environment, QueryTriggerInteraction.Ignore)) return false;

            top = topHit.point.y;
            wallX = wallHit.point.x;
            return true;
        }

        bool Hit(Vector3 origin, Vector3 dir, float dist, out RaycastHit hit)
        {
            return Physics.Raycast(origin, dir, out hit, dist, environment, QueryTriggerInteraction.Ignore);
        }

        void OnLedgeGrabbed()
        {
            int dir = probedLedgeSide != 0 ? probedLedgeSide : Motor.AttachSide;
            var hang = new Vector3(probedWallX - dir * (controller.radius + 0.03f), probedLedgeTop - hangDrop, planeZ);
            climbTarget = new Vector3(probedWallX + dir * 0.45f, probedLedgeTop + 0.02f, planeZ);
            squash = new Vector3(0.9f, 1.12f, 0.9f);
            if (scarfLedge)
            {
                // The scarf lashes round the lip and hauls the hero up to hang from it.
                pullFrom = transform.position;
                pullTo = hang;
                scarfPull = scarfPullTime;
                ScarfCatchPoint = new Vector3(probedWallX, probedLedgeTop, planeZ);
                scarfLedge = false;
                ScarfCaught?.Invoke();
            }
            else SetPosition(hang);
        }

        void OnClimbStarted()
        {
            if (scarfPull > 0f) { SetPosition(pullTo); scarfPull = 0f; }
            climbStart = transform.position;
        }

        void OnClimbFinished()
        {
            SetPosition(climbTarget);
            squash = new Vector3(1.12f, 0.9f, 1.12f);
        }

        /// <summary>Up first, then over the lip — a mantle, not a diagonal slide.</summary>
        public static Vector3 ClimbPath(Vector3 start, Vector3 target, float t)
        {
            float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.6f));
            float over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.65f));
            return new Vector3(Mathf.Lerp(start.x, target.x, over), Mathf.Lerp(start.y, target.y, up), Mathf.Lerp(start.z, target.z, over));
        }

        void SetPosition(Vector3 p)
        {
            controller.enabled = false;
            transform.position = p;
            controller.enabled = true;
        }

        public void Teleport(Vector3 position, bool keepVelocity = false)
        {
            SetPosition(position);
            if (!keepVelocity) Motor.ResetState();
        }

        public void Respawn() => Teleport(spawnPoint);

        public void SetSpawnPoint(Vector3 point) => spawnPoint = point;

        void UpdateVisual(float dt)
        {
            if (visualRoot == null) return;
            float targetYaw = Motor.Facing > 0 ? 90f : -90f;
            yaw = Mathf.LerpAngle(yaw, targetYaw, MovementMath.Damp(turnSharpness, dt));
            // On the whip the whole body hangs along it, pivoting round the hands.
            float targetRoll = Motor.IsSwinging ? Motor.SwingAngle * Mathf.Rad2Deg : 0f;
            roll = Mathf.LerpAngle(roll, targetRoll, MovementMath.Damp(Motor.IsSwinging ? 20f : 10f, dt));
            var tilt = Quaternion.AngleAxis(roll, Vector3.forward);
            visualRoot.localRotation = tilt * Quaternion.Euler(0f, yaw, 0f);
            var grip = Vector3.up * gripHeight;
            visualRoot.localPosition = visualRest + grip - tilt * grip;

            squash = Vector3.Lerp(squash, Vector3.one, MovementMath.Damp(squashRecover, dt));
            visualRoot.localScale = squash;
        }
    }
}
