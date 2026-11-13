using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Test input. "Pressed" flags are one-shot pulses consumed by the first reader.</summary>
    public sealed class ScriptedInput : MonoBehaviour, IPlayerInput
    {
        public float moveX;
        public float aimY;
        public bool jump, jumpHeld, dash, attack;
        public VeilForm? form;

        public float MoveX => moveX;
        public float AimY => aimY;
        public bool JumpPressed { get { bool v = jump; jump = false; return v; } }
        public bool JumpHeld => jumpHeld;
        public bool DashPressed { get { bool v = dash; dash = false; return v; } }
        public bool AttackPressed { get { bool v = attack; attack = false; return v; } }
        public VeilForm? FormRequested { get { var v = form; form = null; return v; } }
    }
}
