using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>A choice button's left and right: while it is chosen they step through its values (see MenuKit.Option).</summary>
    public sealed class OptionCycler : MonoBehaviour
    {
        public Func<string> shown;
        public Action<int> step;

        public void Refresh()
        {
            var b = GetComponent<Button>();
            if (b != null && shown != null) MenuKit.SetLabel(b, "‹   " + shown() + "   ›");
        }

        void Update()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject != gameObject || step == null) return;
            int dir = 0;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) dir = -1;
            if (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) dir = 1;
            if (pad != null && pad.dpad.left.wasPressedThisFrame) dir = -1;
            if (pad != null && pad.dpad.right.wasPressedThisFrame) dir = 1;
            if (dir == 0) return;
            step(dir);
            Refresh();
        }
    }
}
