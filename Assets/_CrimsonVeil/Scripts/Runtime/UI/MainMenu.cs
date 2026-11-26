using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The title screen (the MainMenu scene): Hrát starts the game, Nastavení opens the controls, Konec leaves.
    /// Built in code on start; the scene only holds a camera and this component.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        /// <summary>The scene the game starts in, and the menu's own (both in the build's scene list).</summary>
        public const string GameScene = "Prototype_Ruins", MenuScene = "MainMenu";

        public Button Play { get; private set; }
        public Button Settings { get; private set; }
        public Button Exit { get; private set; }
        public SettingsPanel SettingsPanel { get; private set; }
        RectTransform front;

        void Start()
        {
            Time.timeScale = 1f;
            InputBindings.GameplayBlocked = false;
            _ = InputBindings.Asset;
            Build();
        }

        void Build()
        {
            var canvas = MenuKit.Canvas("Menu", 100, transform);
            var bg = MenuKit.Panel("Backdrop", canvas.transform, new Color(0.08f, 0.08f, 0.09f, 1f));
            front = MenuKit.Fill(MenuKit.Rect("Front", bg));

            var title = MenuKit.Label(front, "CRIMSON VEIL", 118, MenuKit.Paper);
            title.fontStyle = FontStyle.Bold;
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 250f);
            // the scarf's stroke under the title
            var stroke = MenuKit.Rect("Stroke", front);
            stroke.anchorMin = stroke.anchorMax = new Vector2(0.5f, 0.5f);
            stroke.anchoredPosition = new Vector2(40f, 170f);
            stroke.sizeDelta = new Vector2(560f, 10f);
            stroke.localRotation = Quaternion.Euler(0f, 0f, -2f);
            stroke.gameObject.AddComponent<Image>().color = MenuKit.Crimson;

            var column = MenuKit.Column("Buttons", front, 18f);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.anchoredPosition = new Vector2(0f, -60f);
            column.sizeDelta = new Vector2(420f, 260f);
            Play = MenuKit.Button(column, "Hrát", StartGame);
            Settings = MenuKit.Button(column, "Nastavení", OpenSettings);
            Exit = MenuKit.Button(column, "Konec", MenuKit.Quit);

            var version = MenuKit.Label(front, "Esc · zpět    Enter · potvrdit    šipky / WASD · výběr", 20, MenuKit.Dim);
            var vrt = version.rectTransform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0f);
            vrt.anchoredPosition = new Vector2(0f, 40f);

            SettingsPanel = SettingsPanel.Create(canvas.transform, CloseSettings);
            MenuKit.Select(Play);
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            InputBindings.GameplayBlocked = false;
            SceneManager.LoadScene(GameScene);
        }

        public void OpenSettings()
        {
            front.gameObject.SetActive(false);
            SettingsPanel.Open();
        }

        void CloseSettings()
        {
            front.gameObject.SetActive(true);
            MenuKit.Select(Settings);
        }
    }
}
