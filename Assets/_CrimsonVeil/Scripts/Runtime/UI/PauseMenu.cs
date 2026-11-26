using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// In the game, Esc (or Start on a gamepad) stops it and opens this: Pokračovat, Nastavení, Hlavní menu, Ukončit
    /// hru. The game stands still while it is open and the hero hears nothing.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public Button Resume { get; private set; }
        public SettingsPanel SettingsPanel { get; private set; }
        RectTransform panel;
        Button settingsButton;

        /// <summary>Gives the HUD's canvas a pause menu.</summary>
        public static PauseMenu Create(GameObject hudCanvas)
        {
            var canvas = MenuKit.Canvas("Pause", 200, hudCanvas.transform);
            var menu = canvas.gameObject.AddComponent<PauseMenu>();
            menu.Build(canvas.transform);
            return menu;
        }

        void Build(Transform canvas)
        {
            panel = MenuKit.Panel("Panel", canvas, MenuKit.Backdrop);
            var title = MenuKit.Label(panel, "PAUZA", 64, MenuKit.Paper);
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 220f);
            var column = MenuKit.Column("Buttons", panel, 18f);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.anchoredPosition = new Vector2(0f, -20f);
            column.sizeDelta = new Vector2(420f, 330f);
            Resume = MenuKit.Button(column, "Pokračovat", Close);
            settingsButton = MenuKit.Button(column, "Nastavení", OpenSettings);
            MenuKit.Button(column, "Hlavní menu", ToMainMenu);
            MenuKit.Button(column, "Ukončit hru", MenuKit.Quit);
            SettingsPanel = SettingsPanel.Create(canvas, () => { panel.gameObject.SetActive(true); MenuKit.Select(settingsButton); });
            panel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (SettingsPanel.IsOpen || SettingsPanel.Rebinding) return;   // (its own Esc goes back from it)
            if (!SettingsPanel.MenuEscPressed()) return;
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            CrimsonVeil.Core.GameClock.Pause();
            InputBindings.GameplayBlocked = true;
            panel.gameObject.SetActive(true);
            MenuKit.Select(Resume);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            panel.gameObject.SetActive(false);
            if (SettingsPanel.IsOpen) SettingsPanel.gameObject.SetActive(false);
            CrimsonVeil.Core.GameClock.Resume();   // (to a blow's hit-stop if one is still landing, else full speed)
            InputBindings.GameplayBlocked = false;
        }

        void OpenSettings()
        {
            panel.gameObject.SetActive(false);
            SettingsPanel.Open();
        }

        void ToMainMenu()
        {
            CrimsonVeil.Core.GameClock.Reset();
            InputBindings.GameplayBlocked = false;
            SceneManager.LoadScene(MainMenu.MenuScene);
        }

        void OnDestroy()
        {
            if (IsOpen) { CrimsonVeil.Core.GameClock.Resume(); InputBindings.GameplayBlocked = false; }
        }
    }
}
