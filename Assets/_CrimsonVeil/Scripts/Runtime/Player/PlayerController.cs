using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// 2.5D player: 3D CharacterController locked to the XY plane, driven by <see cref="MotorLogic"/>.
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

        public MotorLogic Motor { get; private set; }
        public IPlayerInput InputSource { get; set; }
        public Vector3 Velocity => new Vector3(Motor.Velocity.x, Motor.Velocity.y, 0f);
        public bool IsGrounded => Motor.IsGrounded;

        CharacterController controller;
        CollisionFlags lastFlags;
        int wallSide;
        float planeZ;
        Vector3 spawnPoint;
        Vector3 squash = Vector3.one;
        float yaw = 90f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Motor = new MotorLogic(settings);
            planeZ = transform.position.z;
            spawnPoint = transform.position;
            if (InputSource == null) InputSource = GetComponent<IPlayerInput>();

            Motor.Jumped += () => squash = new Vector3(0.82f, 1.2f, 0.82f);
            Motor.Landed += () => squash = new Vector3(1.18f, 0.84f, 1.18f);
            Motor.Dashed += () => squash = new Vector3(1.25f, 0.85f, 1f);
        }

        void Update()
        {
            Simulate(Time.deltaTime);
        }

        public void Simulate(float dt)
        {
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 1f / 30f);

            var input = new MotorInput();
            if (InputSource != null)
            {
                input.moveX = InputSource.MoveX;
                input.jumpPressed = InputSource.JumpPressed;
                input.jumpHeld = InputSource.JumpHeld;
                input.dashPressed = InputSource.DashPressed;
            }

            var contacts = new MotorContacts
            {
                grounded = controller.isGrounded,
                ceiling = (lastFlags & CollisionFlags.Above) != 0,
                wallSide = (lastFlags & CollisionFlags.Sides) != 0 ? wallSide : 0,
            };

            wallSide = 0;
            Vector2 v = Motor.Tick(dt, input, contacts);
            lastFlags = controller.Move(new Vector3(v.x, v.y, 0f) * dt);

            var p = transform.position;
            if (Mathf.Abs(p.z - planeZ) > 0.0001f)
            {
                p.z = planeZ;
                Teleport(p, keepVelocity: true);
            }

            if (p.y < killY) Respawn();

            UpdateVisual(dt);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (Mathf.Abs(hit.normal.y) < 0.4f && Mathf.Abs(hit.normal.x) > 0.3f)
                wallSide = hit.normal.x > 0f ? -1 : 1;
        }

        public void Teleport(Vector3 position, bool keepVelocity = false)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            if (!keepVelocity) Motor.ResetState();
        }

        public void Respawn() => Teleport(spawnPoint);

        public void SetSpawnPoint(Vector3 point) => spawnPoint = point;

        void UpdateVisual(float dt)
        {
            if (visualRoot == null) return;
            float targetYaw = Motor.Facing > 0 ? 90f : -90f;
            yaw = Mathf.LerpAngle(yaw, targetYaw, MovementMath.Damp(turnSharpness, dt));
            visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);

            squash = Vector3.Lerp(squash, Vector3.one, MovementMath.Damp(squashRecover, dt));
            visualRoot.localScale = squash;
        }
    }
}
