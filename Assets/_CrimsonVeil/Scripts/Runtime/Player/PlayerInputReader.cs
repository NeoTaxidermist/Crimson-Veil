using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Keyboard + gamepad bindings built in code (no asset wiring needed). Every weapon button casts its
    /// weapon: the veil forms it, strikes and melts back into cloth.
    /// Keyboard: A/D or arrows move, W/S aim, Space jump, Shift dash;
    ///   J / LMB sword · K spear · L whip · U shield (hold) · I bow (hold to draw) — or 1-5.
    /// Gamepad: stick move/aim, South jump, East/RB dash; West sword · North spear · RT whip · LT shield · LB bow (D-pad too).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        InputAction move, aim, jump, dash;
        InputAction whip, sword, spear, shield, bow;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone(min=0.2,max=0.95)");

            aim = new InputAction("Aim", InputActionType.Value, expectedControlType: "Axis");
            aim.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            aim.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow");
            aim.AddBinding("<Gamepad>/leftStick/y").WithProcessor("axisDeadzone(min=0.3,max=0.95)");

            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            dash = Button("Dash", "<Keyboard>/leftShift", "<Gamepad>/buttonEast", "<Gamepad>/rightShoulder");

            sword = Button("Sword", "<Keyboard>/j", "<Mouse>/leftButton", "<Keyboard>/2", "<Gamepad>/buttonWest", "<Gamepad>/dpad/up");
            spear = Button("Spear", "<Keyboard>/k", "<Keyboard>/3", "<Gamepad>/buttonNorth", "<Gamepad>/dpad/right");
            whip = Button("Whip", "<Keyboard>/l", "<Keyboard>/1", "<Gamepad>/rightTrigger", "<Gamepad>/dpad/left");
            shield = Button("Shield", "<Keyboard>/u", "<Mouse>/rightButton", "<Keyboard>/4", "<Gamepad>/leftTrigger", "<Gamepad>/dpad/down");
            bow = Button("Bow", "<Keyboard>/i", "<Keyboard>/5", "<Gamepad>/leftShoulder");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) action.AddBinding(b);
            return action;
        }

        InputAction[] All => new[] { move, aim, jump, dash, whip, sword, spear, shield, bow };

        void OnEnable() { foreach (var a in All) a.Enable(); }
        void OnDisable() { foreach (var a in All) a.Disable(); }
        void OnDestroy() { foreach (var a in All) a.Dispose(); }

        public float MoveX => move.ReadValue<float>();
        public float AimY => aim.ReadValue<float>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool JumpHeld => jump.IsPressed();
        public bool DashPressed => dash.WasPressedThisFrame();

        InputAction For(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Whip: return whip;
                case VeilForm.Sword: return sword;
                case VeilForm.Spear: return spear;
                case VeilForm.Shield: return shield;
                case VeilForm.Bow: return bow;
                default: return null;
            }
        }

        public VeilForm? CastPressed
        {
            get
            {
                foreach (var f in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow })
                    if (For(f).WasPressedThisFrame()) return f;
                return null;
            }
        }

        public bool CastHeld(VeilForm form) => For(form) is InputAction a && a.IsPressed();
    }
}
