using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The menus confirm with Space too (v0.112, the user: "potvrzení i přes mezerník"), as well as Enter and the
    /// gamepad's A: the chosen button is pressed, a tick box ticked, a drop-down opened. Not while a key is being
    /// rebound (then Space is the key wanted) nor in the frame a rebinding ended.
    /// </summary>
    public sealed class MenuSubmit : MonoBehaviour
    {
        /// <summary>How many times Space confirmed something (for tests).</summary>
        public static int Submitted { get; private set; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.spaceKey.wasPressedThisFrame) return;
            Submit();
        }

        /// <summary>Confirms what is chosen, as Space does.</summary>
        public static bool Submit()
        {
            if (SettingsPanel.Rebinding || Time.frameCount <= SettingsPanel.RebindEndedFrame + 1) return false;
            var es = EventSystem.current;
            var go = es != null ? es.currentSelectedGameObject : null;
            if (go == null || !go.activeInHierarchy) return false;
            if (go.TryGetComponent<Selectable>(out var s) && !s.IsInteractable()) return false;
            // (a drop-down list open: Space picks the item under the selection, as Enter does)
            bool done = ExecuteEvents.Execute(go, new BaseEventData(es), ExecuteEvents.submitHandler);
            if (done) Submitted++;
            return done;
        }
    }
}
