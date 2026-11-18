#if UNITY_EDITOR
using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The scarf catching a lip out of the hands' reach is the scarf itself drawn out to it (no flat line across): its
    /// end reaches the lip while it hauls the hero up, and lets go after.
    /// </summary>
    public class ScarfStrandPlayTests
    {
        [TearDown]
        public void TearDown() => CrimsonVeil.UI.PlaytestLog.WriteFiles = true;

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator TheScarfItself_ReachesTheLip_AndLetsGoAfter()
        {
            CrimsonVeil.UI.PlaytestLog.WriteFiles = false;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is CrimsonVeil.Combat.CreatureBase || mb.GetType().Name == "Enemy") mb.gameObject.SetActive(false);
            var pc = Object.FindFirstObjectByType<PlayerController>();
            var veil = pc.GetComponent<VeilController>();
            var input = pc.gameObject.AddComponent<ScriptedInput>();
            pc.InputSource = input;
            typeof(VeilController).GetField("input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(veil, input);

            // on the start's floor (top 0), a block whose lip (6.3 m) is out of the hands' reach at the top of a jump
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = new Vector3(-1.5f, 3.15f, 0f);
            box.transform.localScale = new Vector3(3f, 6.3f, 4f);
            pc.Teleport(new Vector3(-5.5f, 0.05f, 0f));
            yield return Wait(0.3f);

            input.moveX = 1f;
            input.jump = true; input.jumpHeld = true;
            float end = Time.time + 1.5f;
            while (Time.time < end && !pc.ScarfCatching) yield return null;
            Assert.IsTrue(pc.ScarfCatching, "the scarf caught the lip");
            yield return Wait(0.12f);
            if (pc.ScarfCatching)
            {
                var tip = veil.Simulation.Positions[veil.Simulation.Count - 1];
                Assert.Less(Vector2.Distance(tip, pc.ScarfCatchPoint), 0.45f, "the scarf's own end is at the lip, wound round it");
                Assert.Greater(veil.StrandAt(0.9f), 0.5f, "drawn as the taut cord there");
                Assert.AreEqual(0f, veil.StrandAt(0.05f), 1e-3f, "the band at the neck stays cloth");
                // it runs from the neck to the lip: halfway along the scarf it is halfway there (no V back to the trailing cloth)
                var mid = veil.Simulation.Positions[(veil.Simulation.Count - 1) / 2];
                var neck = veil.Simulation.Positions[0];
                Vector2 toLip = (Vector2)pc.ScarfCatchPoint - (Vector2)neck;
                float proj = Vector2.Dot((Vector2)mid - (Vector2)neck, toLip.normalized) / toLip.magnitude;
                Assert.That(proj, Is.InRange(0.3f, 0.75f), "the middle of the scarf lies on the way from the neck to the lip");
            }
            input.moveX = 0f; input.jumpHeld = false;
            yield return Wait(0.6f);
            Assert.IsFalse(pc.ScarfCatching);
            Assert.AreEqual(0f, veil.StrandWeight, 1e-3f, "let go: the scarf is cloth again");
            Object.Destroy(box);
        }
    }
}
#endif
