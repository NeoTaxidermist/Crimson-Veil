using System;
using System.Collections.Generic;
using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The settings: the controls, every one with its key and its gamepad button. Choose one and press the new key or
    /// button (Esc leaves it as it was); one that another action had swaps over to that one. Kept between sessions.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        /// <summary>Some binding is waiting for its new key: Esc then cancels that, it doesn't close a menu.</summary>
        public static bool Rebinding { get; private set; }
        /// <summary>The frame the last rebinding ended (its Esc is not a menu's Esc).</summary>
        public static int RebindEndedFrame { get; private set; } = -10;

        public bool IsOpen => gameObject.activeSelf;

        Action onBack;
        readonly List<(InputBindings.Entry entry, bool pad, Button button)> cells = new List<(InputBindings.Entry, bool, Button)>();
        Button back;
        InputActionRebindingExtensions.RebindingOperation op;

        public static SettingsPanel Create(Transform canvas, Action onBack)
        {
            var root = MenuKit.Panel("Settings", canvas, MenuKit.Backdrop);
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            panel.onBack = onBack;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        void Build(RectTransform root)
        {
            _ = InputBindings.Asset;
            // one column, top to bottom: the title, the hint, the list, the buttons (in a low window it is all scaled
            // down together: placed apart, the buttons lay over the end of the list)
            var list = MenuKit.Column("Content", root, 4f, TextAnchor.UpperCenter);
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
            list.sizeDelta = new Vector2(1180f, 1040f);

            var title = MenuKit.Label(list, "NASTAVENÍ  ·  OVLÁDÁNÍ", 44, MenuKit.Paper);
            title.rectTransform.sizeDelta = new Vector2(1180f, 60f);
            var hint = MenuKit.Label(list, "Vyber akci a stiskni novou klávesu nebo tlačítko.  Esc zruší změnu.  Obsazená klávesa se vymění.", 22, MenuKit.Dim);
            hint.rectTransform.sizeDelta = new Vector2(1180f, 44f);

            Row(list, "AKCE", "KLÁVESNICE", "OVLADAČ", header: true);
            foreach (var e in InputBindings.Entries)
            {
                var row = Row(list, e.label, null, null, header: false);
                cells.Add((e, false, Cell(row, e, false)));
                if (e.padFixed == null) cells.Add((e, true, Cell(row, e, true)));
                else
                {
                    var t = MenuKit.Label(row, e.padFixed, 24, MenuKit.Dim);
                    t.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
                    t.rectTransform.sizeDelta = new Vector2(300f, 40f);
                }
            }

            MenuKit.Rect("Gap", list).sizeDelta = new Vector2(10f, 18f);
            var buttons = MenuKit.Rect("Buttons", list);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            buttons.sizeDelta = new Vector2(900f, 64f);
            MenuKit.Button(buttons, "Výchozí ovládání", () => { InputBindings.ResetAll(); Refresh(); }, 380f);
            back = MenuKit.Button(buttons, "Zpět", Back, 380f);
            Refresh();
        }

        RectTransform Row(Transform list, string label, string key, string pad, bool header)
        {
            var row = MenuKit.Rect("Row " + label, list);
            row.sizeDelta = new Vector2(1180f, header ? 40f : 42f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false; h.childControlHeight = false;
            var name = MenuKit.Label(row, label, header ? 20 : 26, header ? MenuKit.Dim : MenuKit.Paper, TextAnchor.MiddleLeft);
            name.rectTransform.sizeDelta = new Vector2(520f, 40f);
            if (header)
            {
                foreach (var s in new[] { key, pad })
                {
                    var t = MenuKit.Label(row, s, 20, MenuKit.Dim);
                    t.rectTransform.sizeDelta = new Vector2(300f, 40f);
                }
            }
            return row;
        }

        Button Cell(Transform row, InputBindings.Entry e, bool pad)
        {
            Button b = null;
            b = MenuKit.Button(row, "", () => StartRebind(e, pad, b), 300f, 40f, 24);
            return b;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
            if (cells.Count > 0) MenuKit.Select(cells[0].button);
        }

        public void Back()
        {
            if (Rebinding) return;
            gameObject.SetActive(false);
            onBack?.Invoke();
        }

        void Refresh()
        {
            foreach (var (entry, pad, button) in cells) MenuKit.SetLabel(button, InputBindings.Shown(entry, pad));
        }

        void StartRebind(InputBindings.Entry e, bool pad, Button b)
        {
            if (Rebinding) return;
            int index = InputBindings.BindingIndex(pad);
            string old = e.action.bindings[index].effectivePath;
            Rebinding = true;
            MenuKit.SetLabel(b, pad ? "Stiskni tlačítko…" : "Stiskni klávesu…");
            e.action.Disable();
            op = e.action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(pad ? "<Gamepad>" : "<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.08f)
                .OnComplete(o =>
                {
                    string chosen = e.action.bindings[index].effectivePath;
                    e.action.ApplyBindingOverride(index, old);   // (put back: Rebind swaps a taken key over properly)
                    InputBindings.Rebind(e, index, chosen);
                    EndRebind(e, b);
                })
                .OnCancel(o => EndRebind(e, b))
                .Start();
        }

        void EndRebind(InputBindings.Entry e, Button b)
        {
            op?.Dispose();
            op = null;
            e.action.Enable();
            Rebinding = false;
            RebindEndedFrame = Time.frameCount;
            Refresh();
            MenuKit.Select(b);
        }

        /// <summary>A menu's Esc now (not the one that just cancelled a rebinding).</summary>
        public static bool MenuEscPressed()
            => !Rebinding && Time.frameCount > RebindEndedFrame + 1 && InputBindings.Action("Pause").WasPressedThisFrame();

        void Update()
        {
            if (MenuEscPressed()) Back();
        }

        void OnDisable()
        {
            if (op != null) { op.Cancel(); }
        }
    }
}
