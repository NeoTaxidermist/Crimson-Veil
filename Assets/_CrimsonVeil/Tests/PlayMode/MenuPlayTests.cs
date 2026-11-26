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
    public class MenuPlayTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; InputBindings.BuildDefaultsForTests(); }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            InputBindings.EndTests();
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
            CollectionAssert.Contains(cells, "J", "the sword's key is listed");
            CollectionAssert.Contains(cells, "Mezerník");
            CollectionAssert.Contains(cells, "Výchozí ovládání");
            menu.SettingsPanel.Back();
            Assert.IsFalse(menu.SettingsPanel.IsOpen);

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
    }
}
