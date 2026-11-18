using System.Collections.Generic;
using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Chains, cages and banners in the backdrop hang from stone in their own layer, not from thin air.</summary>
    public class HangingPropsTests
    {
        const string ScenePath = "Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity";

        [Test]
        public void ChainRun_CoversTheDistance_WithLinksNeverStretched()
        {
            Assert.IsEmpty(HangingProps.ChainRun(0f, 3.8f));
            var one = HangingProps.ChainRun(2f, 3.8f);
            Assert.AreEqual(1, one.Length);
            Assert.AreEqual(2f, one[0], 1e-5f);
            var three = HangingProps.ChainRun(9f, 3.8f);
            Assert.AreEqual(3, three.Length);
            float sum = 0f;
            foreach (var l in three) { sum += l; Assert.LessOrEqual(l, 3.8f + 1e-4f, "squeezed, never stretched"); }
            Assert.AreEqual(9f, sum, 1e-4f);
        }

        [Test]
        public void EveryChainCageAndBanner_HangsFromStoneInItsOwnLayer()
        {
            // (the scene may be open in the editor already: then it is read as it is and left open)
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform deco = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var d = root.transform.Find("Dekorace");
                    if (d != null) deco = d;
                }
                Assert.IsNotNull(deco, "the level has its decoration");

                var stone = new List<Bounds>();
                int groups = 0;
                foreach (Transform child in deco)
                {
                    Assert.IsFalse(HangingProps.Hanging.Contains(child.name), child.name + " at " + child.position + " hangs on nothing");
                    if (!HangingProps.Solid.Contains(child.name)) continue;
                    foreach (var r in child.GetComponentsInChildren<Renderer>()) stone.Add(r.bounds);
                }
                foreach (Transform child in deco)
                {
                    if (child.name != HangingProps.GroupName) continue;
                    groups++;
                    var b = BoundsOf(child);
                    float z = child.GetChild(0).position.z;
                    var top = new Vector3(b.center.x, b.max.y - 0.05f, z);
                    bool hung = Inside(stone, top);
                    // a banner may hang on a wall right behind it
                    for (float dz = 0.3f; !hung && dz <= HangingProps.WallBehind + 0.6f; dz += 0.1f)
                        hung = Inside(stone, top + new Vector3(0f, -0.3f, dz));
                    Assert.IsTrue(hung, child.GetChild(0).name + " at " + child.position + ": its top is in no stone of its layer");
                }
                Assert.Greater(groups, 5, "there are still chains, cages and banners in the backdrop");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static bool Inside(List<Bounds> stone, Vector3 p)
        {
            foreach (var s in stone)
            {
                var e = s; e.Expand(0.1f);
                if (e.Contains(p)) return true;
            }
            return false;
        }

        static Bounds BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
