using System;
using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The game's controls: one set of actions shared by the hero, the menus and the settings, built in code, each with
    /// one key and one gamepad button the player can rebind; the changes are kept between sessions.
    /// <para>The keyboard is for two hands: the left moves (A/D, aims with W/S, Space jump, Shift dash, Ctrl planted,
    /// Q/E look, Tab map), the right fights - on the home row where its fingers rest, in the order the weapons are
    /// learned: J sword, K spear, L whip, ; bow (ů on a Czech keyboard); the shield, the one weapon that is held,
    /// on U, over the index finger. (The weapons used to lie on J K L U I and on 1-5 and the mouse: the left hand had
    /// to cross the keyboard, and the order followed nothing.)</para>
    /// </summary>
    public static class InputBindings
    {
        /// <summary>One thing the player can do, with its name in the settings and its default key and button.</summary>
        public sealed class Entry
        {
            public string id, label, key, pad;
            /// <summary>The gamepad does it with a stick (not rebindable): what to show for it.</summary>
            public string padFixed;
            public InputAction action;
        }

        public const string KeyboardGroup = "Keyboard", GamepadGroup = "Gamepad";
        /// <summary>Where the player's own bindings are kept.</summary>
        public const string PrefsKey = "CrimsonVeil.Bindings";

        /// <summary>Everything that can be rebound, in the order the settings list it.</summary>
        public static readonly Entry[] Entries =
        {
            new Entry { id = "MoveLeft", label = "Pohyb doleva", key = "<Keyboard>/a", padFixed = "Levá páčka" },
            new Entry { id = "MoveRight", label = "Pohyb doprava", key = "<Keyboard>/d", padFixed = "Levá páčka" },
            new Entry { id = "AimUp", label = "Mířit nahoru / šplhat", key = "<Keyboard>/w", padFixed = "Levá páčka" },
            new Entry { id = "AimDown", label = "Mířit dolů / podřep", key = "<Keyboard>/s", padFixed = "Levá páčka" },
            new Entry { id = "Jump", label = "Skok", key = "<Keyboard>/space", pad = "<Gamepad>/buttonSouth" },
            new Entry { id = "Dash", label = "Úskok", key = "<Keyboard>/leftShift", pad = "<Gamepad>/buttonEast" },
            new Entry { id = "Stand", label = "Postoj na místě (držet)", key = "<Keyboard>/leftCtrl", pad = "<Gamepad>/leftStickPress" },
            new Entry { id = "LookUp", label = "Pohled nahoru", key = "<Keyboard>/e", padFixed = "Pravá páčka" },
            new Entry { id = "LookDown", label = "Pohled dolů", key = "<Keyboard>/q", padFixed = "Pravá páčka" },
            new Entry { id = "Sword", label = "Meč", key = "<Keyboard>/j", pad = "<Gamepad>/buttonWest" },
            new Entry { id = "Spear", label = "Kopí", key = "<Keyboard>/k", pad = "<Gamepad>/buttonNorth" },
            new Entry { id = "Whip", label = "Bič", key = "<Keyboard>/l", pad = "<Gamepad>/rightTrigger" },
            new Entry { id = "Bow", label = "Luk (držet a pustit)", key = "<Keyboard>/semicolon", pad = "<Gamepad>/leftShoulder" },
            new Entry { id = "Shield", label = "Štít (držet)", key = "<Keyboard>/u", pad = "<Gamepad>/leftTrigger" },
            new Entry { id = "Map", label = "Mapa", key = "<Keyboard>/tab", pad = "<Gamepad>/select" },
            new Entry { id = "Pause", label = "Pauza / menu", key = "<Keyboard>/escape", pad = "<Gamepad>/start" },
        };

        static InputActionAsset asset;
        static InputAction moveStick, aimStick, lookStick;

        /// <summary>The actions (made on first use, with the player's saved bindings applied).</summary>
        public static InputActionAsset Asset
        {
            get
            {
                if (asset == null) Build();
                return asset;
            }
        }

        public static InputAction MoveStick { get { _ = Asset; return moveStick; } }
        public static InputAction AimStick { get { _ = Asset; return aimStick; } }
        public static InputAction LookStick { get { _ = Asset; return lookStick; } }

        /// <summary>A menu is open: the hero hears nothing (the menu's own keys still work).</summary>
        public static bool GameplayBlocked { get; set; }

        public static Entry Find(string id)
        {
            _ = Asset;
            foreach (var e in Entries) if (e.id == id) return e;
            return null;
        }

        public static InputAction Action(string id) => Find(id)?.action;

        /// <summary>The weapon's action.</summary>
        public static InputAction For(VeilForm form)
        {
            switch (form)
            {
                case VeilForm.Sword: return Action("Sword");
                case VeilForm.Spear: return Action("Spear");
                case VeilForm.Whip: return Action("Whip");
                case VeilForm.Bow: return Action("Bow");
                case VeilForm.Shield: return Action("Shield");
                default: return null;
            }
        }

        /// <summary>The order the weapons are listed and read in.</summary>
        public static readonly VeilForm[] Weapons = { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Bow, VeilForm.Shield };

        static void Build()
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "CrimsonVeilControls";
            asset.AddControlScheme(KeyboardGroup).WithRequiredDevice("<Keyboard>");
            asset.AddControlScheme(GamepadGroup).WithRequiredDevice("<Gamepad>");
            var map = asset.AddActionMap("Hra");
            foreach (var e in Entries)
            {
                e.action = map.AddAction(e.id, InputActionType.Button);
                // binding 0: the key; binding 1: the gamepad's button (or nothing, where a stick does it)
                e.action.AddBinding(e.key, groups: KeyboardGroup);
                e.action.AddBinding(e.pad ?? "", groups: GamepadGroup);
            }
            moveStick = map.AddAction("MoveStick", InputActionType.Value, "<Gamepad>/leftStick/x", processors: "axisDeadzone(min=0.2,max=0.95)", expectedControlLayout: "Axis");
            aimStick = map.AddAction("AimStick", InputActionType.Value, "<Gamepad>/leftStick/y", processors: "axisDeadzone(min=0.3,max=0.95)", expectedControlLayout: "Axis");
            lookStick = map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick/y", processors: "axisDeadzone(min=0.3,max=0.95)", expectedControlLayout: "Axis");
            Load();
            asset.Enable();
        }

        /// <summary>The binding of <paramref name="e"/> for the keyboard (0) or the gamepad (1).</summary>
        public static int BindingIndex(bool gamepad) => gamepad ? 1 : 0;

        /// <summary>What is bound now, as the player sees it ("J", "Mezerník", "X"...; "—" for nothing).</summary>
        public static string Shown(Entry e, bool gamepad)
        {
            _ = Asset;   // (the actions are made on first use: the pause menu's settings were built before any)
            if (gamepad && e.padFixed != null) return e.padFixed;
            var b = e.action.bindings[BindingIndex(gamepad)];
            if (string.IsNullOrEmpty(b.effectivePath)) return "—";
            string s = e.action.GetBindingDisplayString(BindingIndex(gamepad));
            return string.IsNullOrEmpty(s) ? "—" : KeyName(b.effectivePath, s);
        }

        /// <summary>Czech names for the keys whose own name is English.</summary>
        public static string KeyName(string path, string shown)
        {
            switch (path)
            {
                case "<Keyboard>/space": return "Mezerník";
                case "<Keyboard>/leftShift": return "Levý Shift";
                case "<Keyboard>/rightShift": return "Pravý Shift";
                case "<Keyboard>/leftCtrl": return "Levý Ctrl";
                case "<Keyboard>/rightCtrl": return "Pravý Ctrl";
                case "<Keyboard>/escape": return "Esc";
                case "<Keyboard>/enter": return "Enter";
                case "<Keyboard>/backspace": return "Backspace";
                case "<Keyboard>/tab": return "Tab";
                case "<Gamepad>/leftStickPress": return "Stisk levé páčky";
                case "<Gamepad>/rightStickPress": return "Stisk pravé páčky";
                case "<Gamepad>/start": return "Start";
                case "<Gamepad>/select": return "Select";
                default: return shown.ToUpperInvariant().Length == 1 ? shown.ToUpperInvariant() : shown;
            }
        }

        /// <summary>
        /// Puts <paramref name="path"/> on binding <paramref name="index"/> of <paramref name="e"/>. A key or button that
        /// another action already has goes over to this one, and that action takes this one's old key in exchange: no
        /// key does two things, and nothing is left without one.
        /// </summary>
        public static void Rebind(Entry e, int index, string path)
        {
            _ = Asset;
            string old = e.action.bindings[index].effectivePath;
            if (old == path) return;
            foreach (var other in Entries)
            {
                if (other == e) continue;
                if (other.action.bindings[index].effectivePath == path)
                    other.action.ApplyBindingOverride(index, old);
            }
            e.action.ApplyBindingOverride(index, path);
            Save();
        }

        /// <summary>Every key and button back to the defaults.</summary>
        public static void ResetAll()
        {
            _ = Asset;
            asset.RemoveAllBindingOverrides();
            Save();
        }

        /// <summary>Whether changes are written to the player's settings (off in tests: they must not touch his bindings).</summary>
        public static bool Persist { get; set; } = true;

        public static void Save()
        {
            if (asset == null || !Persist) return;
            PlayerPrefs.SetString(PrefsKey, ToJson());
            PlayerPrefs.Save();
        }

        static void Load()
        {
            if (!Persist) return;
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try { Apply(json); }
            catch (Exception ex) { Debug.LogWarning("[controls] the saved bindings could not be read, the defaults are used: " + ex.Message); }
        }

        /// <summary>
        /// The player's changes as text (what is saved): one line per changed binding, "action index path". (By the
        /// actions' names: the input system's own save names each binding by an id made afresh every time the game
        /// starts, so what was saved found nothing to apply to the next time.)
        /// </summary>
        public static string ToJson()
        {
            _ = Asset;
            var sb = new System.Text.StringBuilder();
            foreach (var e in Entries)
                for (int i = 0; i < e.action.bindings.Count; i++)
                {
                    var b = e.action.bindings[i];
                    if (b.overridePath != null) sb.Append(e.id).Append(' ').Append(i).Append(' ').Append(b.overridePath).Append('\n');
                }
            return sb.ToString();
        }

        /// <summary>Applies saved changes (as <see cref="ToJson"/> gave them) over the defaults.</summary>
        public static void FromJson(string json)
        {
            _ = Asset;
            asset.RemoveAllBindingOverrides();
            Apply(json);
        }

        static void Apply(string saved)
        {
            if (string.IsNullOrEmpty(saved)) return;
            foreach (var line in saved.Split('\n'))
            {
                var parts = line.Trim().Split(new[] { ' ' }, 3);
                if (parts.Length < 2) continue;
                var e = Find(parts[0]);
                if (e == null || !int.TryParse(parts[1], out int i) || i < 0 || i >= e.action.bindings.Count) continue;
                e.action.ApplyBindingOverride(i, parts.Length > 2 ? parts[2] : "");
            }
        }

        /// <summary>Forgets everything (tests): the next use builds the defaults again, without reading what is saved.</summary>
        public static void ResetForTests()
        {
            if (asset != null) { asset.Disable(); UnityEngine.Object.DestroyImmediate(asset); }
            asset = null;
            foreach (var e in Entries) e.action = null;
            GameplayBlocked = false;
        }

        /// <summary>Fresh defaults that read and write nothing of the player's (tests); <see cref="EndTests"/> undoes it.</summary>
        public static void BuildDefaultsForTests()
        {
            ResetForTests();
            Persist = false;
            Build();
        }

        /// <summary>Back to the player's own bindings after a test.</summary>
        public static void EndTests()
        {
            ResetForTests();
            Persist = true;
        }

        /// <summary>-1..1 from two buttons and a stick.</summary>
        public static float Axis(InputAction negative, InputAction positive, InputAction stick)
        {
            float v = (positive.IsPressed() ? 1f : 0f) - (negative.IsPressed() ? 1f : 0f);
            float s = stick.ReadValue<float>();
            return Mathf.Clamp(Mathf.Abs(s) > Mathf.Abs(v) ? s : v, -1f, 1f);
        }
    }
}
