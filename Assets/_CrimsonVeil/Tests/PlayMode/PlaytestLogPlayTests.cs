#if UNITY_EDITOR
using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The playtest measurement in the real level: sections as the hero gets on, falls counted, shown on the map.</summary>
    public class PlaytestLogPlayTests
    {
        [TearDown]
        public void TearDown() => PlaytestLog.WriteFiles = true;

        [UnityTest]
        public IEnumerator TheRun_IsMeasured_SectionsAndFalls()
        {
            PlaytestLog.WriteFiles = false;   // test runs are no playtests
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var log = Object.FindFirstObjectByType<PlaytestLog>();
            Assert.IsNotNull(log, "every level measures its playtest (with the HUD)");
            yield return null;
            Assert.IsNotNull(log.Model);
            Assert.AreEqual(0, log.Model.Current, "at the start");

            var pc = Object.FindFirstObjectByType<PlayerController>();
            pc.Teleport(new Vector3(124f, 0.05f, 0f));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual("Věž", log.Model.Records[log.Model.Current].name);

            int deaths = log.Model.Deaths;
            pc.Teleport(new Vector3(124f, -40f, 0f));       // below the kill plane
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(deaths + 1, log.Model.Deaths, "a fall counts");
            Assert.IsTrue(log.Invalid, "the hero was teleported across the level: this run is no playtest, it isn't saved");

            var map = Object.FindFirstObjectByType<Minimap>(FindObjectsInactive.Include);
            map.SetOpen(true);
            yield return null;
            StringAssert.Contains("smrti " + log.Model.Deaths, map.Title());
            map.SetOpen(false);
        }
    }
}
#endif
