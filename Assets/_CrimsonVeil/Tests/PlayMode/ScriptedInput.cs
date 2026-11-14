using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Test input. "Pressed" values are one-shot pulses consumed by the first reader.</summary>
    public sealed class ScriptedInput : MonoBehaviour, IPlayerInput
    {
        public float moveX;
        public float aimY;
        public bool jump, jumpHeld, dash;
        /// <summary>Cast this weapon (one shot).</summary>
        public VeilForm? cast;
        /// <summary>The weapon whose button is held down.</summary>
        public VeilForm? held;

        public float MoveX => moveX;
        public float AimY => aimY;
        public bool JumpPressed { get { bool v = jump; jump = false; return v; } }
        public bool JumpHeld => jumpHeld;
        public bool DashPressed { get { bool v = dash; dash = false; return v; } }
        public VeilForm? CastPressed { get { var v = cast; cast = null; return v; } }
        public bool CastHeld(VeilForm form) => held == form;
    }
}
