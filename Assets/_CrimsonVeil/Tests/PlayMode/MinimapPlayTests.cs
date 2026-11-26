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
    /// <summary>The map in the real level: it opens and closes, shows the whole level and the hero where he is.</summary>
    [Category("UI")]
    public class MinimapPlayTests
    {
        [TearDown]
        public void TearDown() => CrimsonVeil.UI.PlaytestLog.WriteFiles = true;

        [UnityTest]
        public IEnumerator TheMap_ShowsTheWholeLevel_AndTheHeroOnIt()
        {
            CrimsonVeil.UI.PlaytestLog.WriteFiles = false;   // test runs are no playtests
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var map = Object.FindFirstObjectByType<Minimap>(FindObjectsInactive.Include);
            Assert.IsNotNull(map, "every level has the map (with the HUD)");
            Assert.IsFalse(map.IsOpen, "closed until TAB");

            map.Toggle();
            yield return null;
            Assert.IsTrue(map.IsOpen);
            Assert.IsTrue(map.HeroMarker.gameObject.activeInHierarchy);
            Assert.Less(map.Model.Bounds.xMin, -13f, "from the start wall...");
            Assert.Greater(map.Model.Bounds.xMax, 222f, "...to the end wall");
            Assert.IsTrue(map.Model.RevealAll, "the whole level shows for now");

            var pc = Object.FindFirstObjectByType<PlayerController>();
            var before = map.HeroMarker.anchoredPosition;
            pc.Teleport(new Vector3(124f, 0.05f, 0f));
            for (int i = 0; i < 3; i++) yield return null;
            var after = map.HeroMarker.anchoredPosition;
            Assert.Greater(after.x, before.x + 50f, "the mark goes with him");
            var uv = map.Model.WorldToUv((Vector2)pc.transform.position + Vector2.up * 0.9f);
            var size = ((RectTransform)map.HeroMarker.parent).sizeDelta;
            Assert.AreEqual(uv.x * size.x, after.x, 1f);
            Assert.AreEqual(uv.y * size.y, after.y, 1f);

            map.Toggle();
            yield return null;
            Assert.IsFalse(map.IsOpen);
            Assert.IsFalse(map.HeroMarker.gameObject.activeInHierarchy);
        }
    }
}
#endif
