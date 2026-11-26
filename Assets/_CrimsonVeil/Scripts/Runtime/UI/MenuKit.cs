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
        /// A choice among a few (a resolution, a window mode): shown as "‹ value ›"; a click, or right when it is
        /// chosen, takes the next one, left the one before.
        /// </summary>
        public static Button Option(Transform parent, Func<string> shown, Action<int> step, float width = 420f)
        {
            var b = Button(parent, "", () => step(1), width, 44f, 24);
            b.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            var cycler = b.gameObject.AddComponent<OptionCycler>();
            cycler.shown = shown;
            cycler.step = step;
            cycler.Refresh();
            b.onClick.AddListener(cycler.Refresh);
            return b;
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
