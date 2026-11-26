using System.Collections;
using System.Linq;
using CrimsonVeil.Player;
using CrimsonVeil.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CrimsonVeil.Tests
{
    /// <summary>The title screen, the settings and the pause menu, in the game itself.</summary>
    [Category("UI")]
    public class MenuPlayTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; InputBindings.BuildDefaultsForTests(); CrimsonVeil.Core.GameSettings.BeginTests(); }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            InputBindings.EndTests();
            CrimsonVeil.Core.GameSettings.EndTests();
        }

        [UnityTest]
        public IEnumerator TheTitleScreen_HasPlaySettingsAndExit_AndPlayStartsTheGame()
        {
            yield return SceneManager.LoadSceneAsync(MainMenu.MenuScene);
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.IsNotNull(menu, "the title screen");
            Assert.AreEqual("Hrát", menu.Play.GetComponentInChildren<Text>().text);
            Assert.AreEqual("Nastavení", menu.Settings.GetComponentInChildren<Text>().text);
            Assert.AreEqual("Konec", menu.Exit.GetComponentInChildren<Text>().text);
            Assert.AreEqual(menu.Play.gameObject, UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, "Play is chosen: Enter starts");

            menu.OpenSettings();
            yield return null;
            Assert.IsTrue(menu.SettingsPanel.IsOpen);
            var cells = menu.SettingsPanel.GetComponentsInChildren<Button>().Select(b => b.GetComponentInChildren<Text>().text).ToList();
            CollectionAssert.Contains(cells, "U", "the sword's key is listed");
            CollectionAssert.Contains(cells, "Mezerník");
            CollectionAssert.Contains(cells, "Výchozí ovládání");
            // the sound and the picture: their own pages
            menu.SettingsPanel.Show(SettingsPanel.Page.Sound);
            yield return null;
            var sliders = menu.SettingsPanel.GetComponentsInChildren<Slider>();
            Assert.AreEqual(3, sliders.Length, "overall, music, effects");
            menu.SettingsPanel.Show(SettingsPanel.Page.Picture);
            yield return null;
            var panel = menu.SettingsPanel;
            Assert.Greater(panel.ResolutionList.options.Count, 0, "the resolutions in a drop-down list");
            Assert.AreEqual(3, panel.ModeList.options.Count, "window, borderless, fullscreen");
            Assert.AreEqual(QualitySettings.names.Length, panel.QualityList.options.Count);
            // a change is tried, Zrušit takes it back...
            bool vsync = CrimsonVeil.Core.GameSettings.VSync;
            panel.VSyncBox.isOn = !vsync;
            Assert.AreNotEqual(vsync, CrimsonVeil.Core.GameSettings.VSync, "ticked: changed");
            panel.Cancel();
            Assert.AreEqual(vsync, CrimsonVeil.Core.GameSettings.VSync, "Zrušit takes it back");
            Assert.IsFalse(panel.IsOpen);
            // ...and Uložit keeps it
            menu.OpenSettings();
            yield return null;
            panel.Show(SettingsPanel.Page.Picture);
            panel.VSyncBox.isOn = !vsync;
            panel.Save();
            Assert.AreEqual(!vsync, CrimsonVeil.Core.GameSettings.VSync, "Uložit keeps it");
            Assert.IsFalse(panel.IsOpen);
            Assert.IsFalse(menu.SettingsPanel.IsOpen, "closed");

            menu.Play.onClick.Invoke();
            float end = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != MainMenu.GameScene && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(MainMenu.GameScene, SceneManager.GetActiveScene().name, "Play loads the game");
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerController>(), "with the hero in it");
            Assert.IsNotNull(Object.FindFirstObjectByType<PauseMenu>(), "and a pause menu");
        }

        [UnityTest]
        public IEnumerator ThePauseMenu_StopsTheGame_AndTheHeroHearsNothing_TillItIsClosed()
        {
            var hud = ScarfHud.Ensure();
            yield return null;
            var pause = hud.GetComponentInChildren<PauseMenu>(true);
            Assert.IsNotNull(pause);
            Assert.IsFalse(pause.IsOpen);
            pause.Open();
            yield return null;
            var backdrop = (RectTransform)pause.transform.Find("Panel");
            var hudRect = ((RectTransform)hud.transform).rect;
            Assert.AreEqual(hudRect.width, backdrop.rect.width, 1f, "the dark backdrop covers the whole screen (it was a 100 x 100 square)");
            Assert.AreEqual(hudRect.height, backdrop.rect.height, 1f);
            Assert.AreEqual(0f, Time.timeScale, "the game stands still");
            Assert.IsTrue(InputBindings.GameplayBlocked, "the hero hears nothing");
            var reader = hud.gameObject.AddComponent<PlayerInputReader>();
            Assert.AreEqual(0f, reader.MoveX);
            Assert.IsNull(reader.CastPressed);
            pause.Close();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(InputBindings.GameplayBlocked);
            Object.Destroy(hud.gameObject);
        }

        /// <summary>
        /// Paused while a blow is landing, the game still stands - the hit-stop's end used to set it running again under
        /// the open menu (a foe struck the hero dead behind it) - and closed, it runs at full speed, not at the crawl the
        /// menu found it at (the game went on at 3 %).
        /// </summary>
        [UnityTest]
        public IEnumerator PausedWhileABlowLands_TheGameStillStands_AndRunsAtFullSpeedAfter()
        {
            var fxHost = new GameObject("Fx");
            fxHost.AddComponent<CrimsonVeil.Combat.CombatFx>();
            var hud = ScarfHud.Ensure();
            yield return null;
            var pause = hud.GetComponentInChildren<PauseMenu>(true);
            CrimsonVeil.Combat.CombatFx.HitStop(0.15f);
            Assert.Less(Time.timeScale, 0.5f, "a blow lands: the world crawls");
            pause.Open();
            Assert.AreEqual(0f, Time.timeScale, "paused: it stands");
            float end = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                Assert.AreEqual(0f, Time.timeScale, "and stands when the blow's moment is over");
            }
            pause.Close();
            Assert.AreEqual(1f, Time.timeScale, "closed: full speed, not the crawl");
            CrimsonVeil.Combat.CombatFx.HitStop(0.1f);
            pause.Open();
            pause.Close();
            Assert.Less(Time.timeScale, 0.5f, "closed while a blow is still landing: it finishes landing");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(1f, Time.timeScale, "and then full speed");
            Object.Destroy(hud.gameObject);
            Object.Destroy(fxHost);
        }
    }
}
