using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The menus' pieces, made in code in the game's own look: black ink, light grey type, the scarf's crimson for what
    /// is chosen. Every menu works with the mouse, the keyboard (arrows / WASD, Enter) and a gamepad.
    /// </summary>
    public static class MenuKit
    {
        public static readonly Color Backdrop = new Color(0.035f, 0.035f, 0.04f, 0.93f);
        public static readonly Color Ink = new Color(0.07f, 0.07f, 0.08f, 1f);
        public static readonly Color Paper = new Color(0.86f, 0.85f, 0.83f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.54f, 0.53f, 1f);
        public static readonly Color Crimson = new Color(0.66f, 0.06f, 0.08f, 1f);

        static Font font;
        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>A canvas over everything (scaled for 1920 x 1080).</summary>
        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            if (parent != null)
            {
                // inside another canvas (the HUD's): over the whole of it, drawn in its own order (left at its default
                // 100 x 100 the pause menu's backdrop was a little square, the game bright all round the buttons)
                canvas.overrideSorting = true;
                canvas.sortingOrder = order;
                Fill((RectTransform)go.transform);
            }
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // (scaled by the height: a menu is a column, and in a wide, low window it must still fit top to bottom)
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>The scene's event system, on the new input system (made if there is none).</summary>
        public static EventSystem EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es != null) return es;
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return es;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>A full-screen panel of <paramref name="color"/>.</summary>
        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var rt = Fill(Rect(name, parent));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A column that lays its children out from the top, <paramref name="spacing"/> apart.</summary>
        public static RectTransform Column(string name, Transform parent, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = false; v.childControlHeight = false;
            v.childForceExpandWidth = false; v.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>A button: dark, its label light; chosen (pointed at, or selected with keys / a gamepad) it turns crimson.</summary>
        public static Button Button(Transform parent, string label, Action onClick, float width = 420f, float height = 64f, int size = 30)
        {
            var rt = Rect(label, parent);
            rt.sizeDelta = new Vector2(width, height);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = height;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = Ink;
            colors.highlightedColor = Crimson;
            colors.selectedColor = Crimson;
            colors.pressedColor = new Color(0.45f, 0.03f, 0.05f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            colors.fadeDuration = 0.06f;
            b.colors = colors;
            b.targetGraphic = img;
            var text = Label(rt, label, size, Paper);
            Fill(text.rectTransform);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        /// <summary>A volume slider 0..1: a dark track, the scarf's crimson filling it, a light handle. Left and right
        /// (keys, the stick) move it when it is chosen; up and down move on to the next item.</summary>
        public static Slider Slider(Transform parent, float value, Action<float> onChange, float width = 420f)
        {
            var rt = Rect("Slider", parent);
            rt.sizeDelta = new Vector2(width, 36f);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = 36f;
            var bg = Rect("Track", rt);
            bg.anchorMin = new Vector2(0f, 0.35f); bg.anchorMax = new Vector2(1f, 0.65f);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            bg.gameObject.AddComponent<Image>().color = Ink;
            var fillArea = Rect("Fill Area", rt);
            fillArea.anchorMin = new Vector2(0f, 0.35f); fillArea.anchorMax = new Vector2(1f, 0.65f);
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = Rect("Fill", fillArea);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = Crimson;
            var handleArea = Rect("Handle Area", rt);
            Fill(handleArea);
            var handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(18f, 0f);
            handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.color = Color.white;
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hImg;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            var colors = s.colors;
            colors.normalColor = Dim; colors.highlightedColor = Paper; colors.selectedColor = Paper;
            colors.pressedColor = Paper;
            s.colors = colors;
            s.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            s.SetValueWithoutNotify(value);
            if (onChange != null) s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        /// <summary>
        /// A drop-down list (for a choice among many: the resolutions): the chosen value with a mark ▾; opened (a
        /// click, Enter, the gamepad's A) it lists all of them, scrolling when they are many, and one is picked with the
        /// mouse or up / down and Enter.
        /// </summary>
        public static Dropdown Dropdown(Transform parent, System.Collections.Generic.IList<string> items, int index, Action<int> onChange, float width = 560f)
        {
            var go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            go.name = "Dropdown";
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, 44f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = 44f;
            var dd = go.GetComponent<Dropdown>();

            // the look: dark, light type, the scarf's crimson for the chosen and the pointed-at
            foreach (var t in go.GetComponentsInChildren<Text>(true)) { t.font = Font; t.color = Paper; t.fontSize = 24; }
            dd.captionText.alignment = TextAnchor.MiddleCenter;
            go.GetComponent<Image>().color = Color.white;
            var colors = dd.colors;
            colors.normalColor = Ink; colors.highlightedColor = Crimson; colors.selectedColor = Crimson;
            colors.pressedColor = new Color(0.45f, 0.03f, 0.05f, 1f);
            dd.colors = colors;
            var arrow = go.transform.Find("Arrow");
            if (arrow != null)
            {
                arrow.GetComponent<Image>().color = Color.clear;
                var mark = Label(arrow, "▾", 26, Paper);
                Fill(mark.rectTransform);
            }
            var template = (RectTransform)go.transform.Find("Template");
            template.sizeDelta = new Vector2(template.sizeDelta.x, 420f);
            template.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 1f);
            foreach (var img in template.GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Item Background") img.color = Ink;
                else if (img.name == "Item Checkmark") img.color = Crimson;
                else if (img.name == "Handle") img.color = Dim;
                else if (img.name == "Scrollbar") img.color = new Color(0.12f, 0.12f, 0.13f, 1f);
            }
            var item = template.Find("Viewport/Content/Item");
            if (item != null)
            {
                ((RectTransform)item).sizeDelta = new Vector2(0f, 40f);
                var toggle = item.GetComponent<Toggle>();
                var tc = toggle.colors;
                tc.normalColor = Ink; tc.highlightedColor = Crimson; tc.selectedColor = Crimson;
                toggle.colors = tc;
                var content = (RectTransform)template.Find("Viewport/Content");
                content.sizeDelta = new Vector2(0f, 44f);
            }
            dd.navigation = new Navigation { mode = Navigation.Mode.Vertical };

            dd.options.Clear();
            foreach (var s in items) dd.options.Add(new Dropdown.OptionData(s));
            dd.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, items.Count - 1)));
            dd.RefreshShownValue();
            if (onChange != null) dd.onValueChanged.AddListener(i => onChange(i));
            return dd;
        }

        /// <summary>A tick box (on / off), the tick in the scarf's crimson.</summary>
        public static Toggle Toggle(Transform parent, bool on, Action<bool> onChange)
        {
            var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
            go.name = "Toggle";
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(44f, 44f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 44f; le.preferredHeight = 44f;
            var tg = go.GetComponent<Toggle>();
            var bg = (RectTransform)go.transform.Find("Background");
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.anchoredPosition = Vector2.zero; bg.sizeDelta = Vector2.zero;
            var check = (RectTransform)bg.Find("Checkmark");
            check.anchorMin = new Vector2(0.2f, 0.2f); check.anchorMax = new Vector2(0.8f, 0.8f);
            check.offsetMin = check.offsetMax = Vector2.zero;
            check.GetComponent<Image>().color = Crimson;
            var label = go.transform.Find("Label");
            if (label != null) UnityEngine.Object.Destroy(label.gameObject);
            var colors = tg.colors;
            colors.normalColor = Ink; colors.highlightedColor = Dim; colors.selectedColor = Dim;
            tg.colors = colors;
            tg.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            tg.SetIsOnWithoutNotify(on);
            if (onChange != null) tg.onValueChanged.AddListener(v => onChange(v));
            return tg;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        /// <summary>Selects <paramref name="b"/> so the keys and a gamepad move from it.</summary>
        public static void Select(Selectable b)
        {
            var es = EventSystem.current != null ? EventSystem.current : EnsureEventSystem();
            if (b == null || es == null) return;
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(b.gameObject);
        }

        /// <summary>Leaves the game (in the editor: stops playing).</summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
