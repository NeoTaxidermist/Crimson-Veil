using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Scripted input for captures, demos and smoke tests: runs in <see cref="direction"/> and jumps
    /// every <see cref="jumpInterval"/> seconds. Assign to <see cref="PlayerController.InputSource"/>.
    /// </summary>
    public sealed class AutopilotInput : MonoBehaviour, IPlayerInput
    {
        public float direction = 1f;
        public float jumpInterval = 1.6f;
        public float jumpHold = 0.3f;

        float timer;
        bool jumpPulse;

        void Update()
        {
            float prev = timer;
            timer += Time.deltaTime;
            if (jumpInterval > 0f && Mathf.Floor(timer / jumpInterval) > Mathf.Floor(prev / jumpInterval)) jumpPulse = true;
        }

        public float MoveX => direction;
        public float AimY => 0f;
        public bool JumpPressed { get { bool v = jumpPulse; jumpPulse = false; return v; } }
        public bool JumpHeld => jumpInterval > 0f && Mathf.Repeat(timer, jumpInterval) < jumpHold;
        public bool DashPressed => false;
        public VeilForm? CastPressed => null;
        public bool CastHeld(VeilForm form) => false;
    }
}
