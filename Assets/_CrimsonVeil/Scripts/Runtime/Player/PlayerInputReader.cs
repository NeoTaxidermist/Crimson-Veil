using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Keyboard + gamepad bindings built in code (no asset wiring needed).
    /// Keyboard: A/D or arrows move, Space jump, Shift dash, J / LMB attack, 1-5 veil forms.
    /// Gamepad: stick move, South jump, East/RB dash, West attack, D-pad + LB veil forms.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        InputAction move, aim, jump, dash, attack;
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
            attack = Button("Attack", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");

            whip = Button("Whip", "<Keyboard>/1", "<Gamepad>/dpad/left");
            sword = Button("Sword", "<Keyboard>/2", "<Gamepad>/dpad/up");
            spear = Button("Spear", "<Keyboard>/3", "<Gamepad>/dpad/right");
            shield = Button("Shield", "<Keyboard>/4", "<Gamepad>/dpad/down");
            bow = Button("Bow", "<Keyboard>/5", "<Gamepad>/leftShoulder");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) action.AddBinding(b);
            return action;
        }

        InputAction[] All => new[] { move, aim, jump, dash, attack, whip, sword, spear, shield, bow };

        void OnEnable() { foreach (var a in All) a.Enable(); }
        void OnDisable() { foreach (var a in All) a.Disable(); }
        void OnDestroy() { foreach (var a in All) a.Dispose(); }

        public float MoveX => move.ReadValue<float>();
        public float AimY => aim.ReadValue<float>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool JumpHeld => jump.IsPressed();
        public bool DashPressed => dash.WasPressedThisFrame();
        public bool AttackPressed => attack.WasPressedThisFrame();

        public VeilForm? FormRequested
        {
            get
            {
                if (whip.WasPressedThisFrame()) return VeilForm.Whip;
                if (sword.WasPressedThisFrame()) return VeilForm.Sword;
                if (spear.WasPressedThisFrame()) return VeilForm.Spear;
                if (shield.WasPressedThisFrame()) return VeilForm.Shield;
                if (bow.WasPressedThisFrame()) return VeilForm.Bow;
                return null;
            }
        }
    }
}
