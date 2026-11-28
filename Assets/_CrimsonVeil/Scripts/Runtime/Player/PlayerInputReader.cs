using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's controls, read from the game's shared, rebindable actions (<see cref="InputBindings"/>): the left hand
    /// moves, the right fights. While a menu is open the hero hears nothing.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        InputAction left, right, up, down, lookUp, lookDown, jump, dash, stand, heal, interact;

        void Awake()
        {
            left = InputBindings.Action("MoveLeft");
            right = InputBindings.Action("MoveRight");
            up = InputBindings.Action("AimUp");
            down = InputBindings.Action("AimDown");
            lookUp = InputBindings.Action("LookUp");
            lookDown = InputBindings.Action("LookDown");
            jump = InputBindings.Action("Jump");
            dash = InputBindings.Action("Dash");
            stand = InputBindings.Action("Stand");
            heal = InputBindings.Action("Heal");
            interact = InputBindings.Action("Interact");
        }

        static bool Blocked => InputBindings.GameplayBlocked || InputBindings.JustUnblocked;

        public float MoveX => Blocked ? 0f : InputBindings.Axis(left, right, InputBindings.MoveStick);
        public float AimY => Blocked ? 0f : InputBindings.Axis(down, up, InputBindings.AimStick);
        public float LookY => Blocked ? 0f : InputBindings.Axis(lookDown, lookUp, InputBindings.LookStick);
        public bool JumpPressed => !Blocked && jump.WasPressedThisFrame();
        public bool JumpHeld => !Blocked && jump.IsPressed();
        public bool DashPressed => !Blocked && dash.WasPressedThisFrame();
        public bool StandHeld => !Blocked && stand.IsPressed();
        public bool HealHeld => !Blocked && heal.IsPressed();
        public bool InteractPressed => !Blocked && interact.WasPressedThisFrame();

        public VeilForm? CastPressed
        {
            get
            {
                if (Blocked) return null;
                foreach (var f in InputBindings.Weapons)   // (read every frame: a new array of them each time was garbage)
                    if (InputBindings.For(f).WasPressedThisFrame()) return f;
                return null;
            }
        }

        public bool CastHeld(VeilForm form) => !Blocked && InputBindings.For(form) is InputAction a && a.IsPressed();
    }
}
