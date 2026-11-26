using System;
using System.Collections.Generic;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The settings, on three pages. Ovládání: every action with its key and its gamepad button - choose one and press
    /// the new key or button (Esc leaves it as it was); one that another action had swaps over to that one. Zvuk: the
    /// overall volume, music, effects. Obraz: the resolution, the window mode, the quality (drop-down lists), vertical
    /// sync (a tick box). Changes take effect for trying (keys at once, the volume heard) but are kept only with Uložit;
    /// Zrušit (or Esc) takes them all back; Výchozí puts the open page back to the defaults.
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

        /// <summary>The settings' pages.</summary>
        public enum Page { Controls, Sound, Picture }
        public Page Current { get; private set; }
        readonly RectTransform[] pages = new RectTransform[3];
        readonly Button[] tabs = new Button[3];
        Button resetButton, cancelButton, saveButton;
        readonly List<Slider> sliders = new List<Slider>();
        Dropdown resolution, mode, quality;
        Toggle vsync;
        List<Vector2Int> resolutions = new List<Vector2Int>();
        string keysWhenOpened;
        GameSettings.Snapshot settingsWhenOpened;

        public Dropdown ResolutionList => resolution;
        public Dropdown ModeList => mode;
        public Dropdown QualityList => quality;
        public Toggle VSyncBox => vsync;

        void Build(RectTransform root)
        {
            _ = InputBindings.Asset;
            // one column, top to bottom: the title, the tabs, the page, the buttons (in a low window it is all scaled
            // down together: placed apart, the buttons lay over the end of the list)
            var column = MenuKit.Column("Content", root, 4f, TextAnchor.UpperCenter);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(1180f, 1040f);

            var title = MenuKit.Label(column, "NASTAVENÍ", 44, MenuKit.Paper);
            title.rectTransform.sizeDelta = new Vector2(1180f, 56f);

            var tabRow = MenuKit.Rect("Tabs", column);
            tabRow.sizeDelta = new Vector2(1180f, 56f);
            var th = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 16f; th.childAlignment = TextAnchor.MiddleCenter;
            th.childControlWidth = false; th.childControlHeight = false;
            string[] names = { "Ovládání", "Zvuk", "Obraz" };
            for (int i = 0; i < 3; i++)
            {
                var page = (Page)i;
                tabs[i] = MenuKit.Button(tabRow, names[i], () => Show(page), 260f, 48f, 26);
            }

            var holder = MenuKit.Rect("Page", column);
            holder.sizeDelta = new Vector2(1180f, 820f);
            pages[0] = PageColumn(holder, "Controls");
            pages[1] = PageColumn(holder, "Sound");
            pages[2] = PageColumn(holder, "Picture");
            BuildControls(pages[0]);
            BuildSound(pages[1]);
            BuildPicture(pages[2]);

            MenuKit.Rect("Gap", column).sizeDelta = new Vector2(10f, 18f);
            var buttons = MenuKit.Rect("Buttons", column);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            buttons.sizeDelta = new Vector2(1100f, 64f);
            resetButton = MenuKit.Button(buttons, "Výchozí ovládání", ResetPage, 340f);
            cancelButton = MenuKit.Button(buttons, "Zrušit", Cancel, 300f);
            saveButton = MenuKit.Button(buttons, "Uložit", Save, 300f);
            back = cancelButton;
            Show(Page.Controls);
        }

        static RectTransform PageColumn(RectTransform holder, string name)
        {
            var page = MenuKit.Column(name, holder, 4f, TextAnchor.UpperCenter);
            MenuKit.Fill(page);
            return page;
        }

        void BuildControls(RectTransform list)
        {
            var hint = MenuKit.Label(list, "Vyber akci a stiskni novou klávesu nebo tlačítko.  Esc zruší změnu.  Obsazená klávesa se vymění.", 22, MenuKit.Dim);
            hint.rectTransform.sizeDelta = new Vector2(1180f, 40f);
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
        }

        void BuildSound(RectTransform list)
        {
            var hint = MenuKit.Label(list, "Hra zatím nemá hudbu ani zvuky: hlasitosti se uloží a budou platit, až přibudou.", 22, MenuKit.Dim);
            hint.rectTransform.sizeDelta = new Vector2(1180f, 60f);
            VolumeRow(list, "Celková hlasitost", () => GameSettings.Master, GameSettings.SetMaster);
            VolumeRow(list, "Hudba", () => GameSettings.Music, GameSettings.SetMusic);
            VolumeRow(list, "Zvukové efekty", () => GameSettings.Effects, GameSettings.SetEffects);
        }

        void VolumeRow(RectTransform list, string label, Func<float> get, Action<float> set)
        {
            var row = Row(list, label, null, null, header: false);
            row.sizeDelta = new Vector2(1180f, 60f);
            Text value = null;
            var slider = MenuKit.Slider(row, get(), v => { set(v); if (value != null) value.text = GameSettings.Percent(v); }, 420f);
            value = MenuKit.Label(row, GameSettings.Percent(get()), 24, MenuKit.Paper);
            value.rectTransform.sizeDelta = new Vector2(120f, 40f);
            var src = slider.gameObject.AddComponent<ValueSource>();
            src.get = get;
            src.label = value;
            sliders.Add(slider);
        }

        void BuildPicture(RectTransform list)
        {
            var hint = MenuKit.Label(list, "Obraz se změní po uložení, a jen v sestavené hře (v editoru Unity ne).", 22, MenuKit.Dim);
            hint.rectTransform.sizeDelta = new Vector2(1180f, 60f);
            resolutions = GameSettings.Resolutions();
            var names = new List<string>();
            foreach (var r in resolutions) names.Add(GameSettings.Label(r));
            resolution = MenuKit.Dropdown(PictureRow(list, "Rozlišení"), names, resolutions.IndexOf(GameSettings.Resolution),
                i => { if (i >= 0 && i < resolutions.Count) GameSettings.SetResolution(resolutions[i]); });
            mode = MenuKit.Dropdown(PictureRow(list, "Režim zobrazení"),
                new[] { GameSettings.Label(GameSettings.WindowMode.Window), GameSettings.Label(GameSettings.WindowMode.Borderless), GameSettings.Label(GameSettings.WindowMode.Fullscreen) },
                (int)GameSettings.Mode, i => GameSettings.SetMode((GameSettings.WindowMode)i));
            var qualities = new List<string>();
            for (int q = 0; q < QualitySettings.names.Length; q++) qualities.Add(QualityName(q));
            quality = MenuKit.Dropdown(PictureRow(list, "Kvalita obrazu"), qualities, GameSettings.Quality, GameSettings.SetQuality);
            vsync = MenuKit.Toggle(PictureRow(list, "Svislá synchronizace (VSync)"), GameSettings.VSync, GameSettings.SetVSync);
            var why = MenuKit.Label(list, "VSync: hra kreslí snímky v rytmu monitoru. Obraz se netrhá a karta se zbytečně nezahřívá; odezva je o chlup pomalejší.", 20, MenuKit.Dim);
            why.rectTransform.sizeDelta = new Vector2(1180f, 50f);
        }

        static string QualityName(int q)
        {
            var names = QualitySettings.names;
            if (q < 0 || q >= names.Length) return "—";
            return names[q] == "PC" ? "Vysoká" : names[q] == "Mobile" ? "Nízká" : names[q];
        }

        RectTransform PictureRow(RectTransform list, string label)
        {
            var row = Row(list, label, null, null, header: false);
            row.sizeDelta = new Vector2(1180f, 60f);
            return row;
        }

        /// <summary>Opens one of the pages.</summary>
        public void Show(Page page)
        {
            Current = page;
            for (int i = 0; i < 3; i++)
            {
                pages[i].gameObject.SetActive(i == (int)page);
                var c = tabs[i].colors;
                c.normalColor = i == (int)page ? new Color(0.3f, 0.03f, 0.04f, 1f) : MenuKit.Ink;
                tabs[i].colors = c;
            }
            MenuKit.SetLabel(resetButton, page == Page.Controls ? "Výchozí ovládání" : page == Page.Sound ? "Výchozí zvuk" : "Výchozí obraz");
            // (the page's own tab is the one chosen: two lit tabs read as two pages open)
            if (gameObject.activeInHierarchy) MenuKit.Select(tabs[(int)page]);
            Refresh();
        }

        void ResetPage()
        {
            if (Current == Page.Controls) InputBindings.ResetAll();
            else if (Current == Page.Sound) GameSettings.ResetSound();
            else GameSettings.ResetPicture();
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
            keysWhenOpened = InputBindings.ToJson();
            settingsWhenOpened = GameSettings.Capture();
            Show(Page.Controls);
        }

        /// <summary>Keeps every change (the keys, the sound, the picture) and closes.</summary>
        public void Save()
        {
            if (Rebinding) return;
            InputBindings.Save();
            GameSettings.Keep();
            Close();
        }

        /// <summary>Takes every change back to how it was when the settings were opened, and closes.</summary>
        public void Cancel()
        {
            if (Rebinding) return;
            InputBindings.FromJson(keysWhenOpened);
            GameSettings.Restore(settingsWhenOpened);
            Close();
        }

        /// <summary>Esc: the same as Zrušit.</summary>
        public void Back() => Cancel();

        void Close()
        {
            gameObject.SetActive(false);
            onBack?.Invoke();
        }

        void Refresh()
        {
            foreach (var (entry, pad, button) in cells) MenuKit.SetLabel(button, InputBindings.Shown(entry, pad));
            foreach (var sl in sliders)
            {
                var src = sl.GetComponent<ValueSource>();
                if (src == null) continue;
                // (without notifying: shown again, not set - setting it would save the settings at every look)
                sl.SetValueWithoutNotify(src.get());
                if (src.label != null) src.label.text = GameSettings.Percent(src.get());
            }
            if (resolution != null)
            {
                resolution.SetValueWithoutNotify(Mathf.Max(0, resolutions.IndexOf(GameSettings.Resolution)));
                mode.SetValueWithoutNotify((int)GameSettings.Mode);
                quality.SetValueWithoutNotify(Mathf.Max(0, GameSettings.Quality));
                vsync.SetIsOnWithoutNotify(GameSettings.VSync);
            }
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
            // (Esc with a drop-down list open closes the list, not the settings)
            if (MenuEscPressed() && GameObject.Find("Dropdown List") == null) Back();
        }

        void OnDisable()
        {
            if (op != null) { op.Cancel(); }
        }
    }
}

namespace CrimsonVeil.UI
{
    /// <summary>Where a settings slider reads its value from (to show it again when the page is opened or reset).</summary>
    public sealed class ValueSource : MonoBehaviour
    {
        public System.Func<float> get;
        public UnityEngine.UI.Text label;
    }
}
