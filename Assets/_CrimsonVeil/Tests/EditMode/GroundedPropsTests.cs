using System.Collections.Generic;
using CrimsonVeil.EditorTools;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Arches, columns and walls in the backdrop stand on the ground or on stone of their own layer.</summary>
    public class GroundedPropsTests
    {
        const string ScenePath = "Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity";

        [Test]
        public void TheFloorOfEachLayer_IsBelowItsGroundLevelRuins()
        {
            Assert.Less(GroundedProps.Floor(LevelLayer.Far), GroundedProps.Floor(LevelLayer.Mid));
            Assert.Less(GroundedProps.Floor(LevelLayer.Mid), GroundedProps.Floor(LevelLayer.Near));
            GroundedProps.Feet(new Bounds(new Vector3(10f, 0f, 0f), new Vector3(4f, 2f, 1f)), out float l, out float r);
            Assert.Greater(l, 8f); Assert.Less(r, 12f); Assert.Less(l, r);
        }

        [Test]
        public void EveryArchColumnAndWall_StandsOnTheGround_OrOnStoneOfItsLayer()
        {
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

                var stone = new List<(Transform owner, Bounds b)>();
                foreach (Transform child in deco)
                {
                    if (!HangingProps.Solid.Contains(child.name) && !GroundedProps.Standing.Contains(child.name)) continue;
                    foreach (var r in child.GetComponentsInChildren<Renderer>()) stone.Add((child, r.bounds));
                }
                int high = 0;
                foreach (Transform child in deco)
                {
                    if (!GroundedProps.Standing.Contains(child.name)) continue;
                    var tag = child.GetComponent<DepthLayer>();
                    if (tag == null || tag.layer == LevelLayer.Playfield) continue;
                    Assert.IsTrue(GroundedProps.Bounds(child, out var b));
                    if (b.min.y <= GroundedProps.Floor(tag.layer) + GroundedProps.FloorMist) continue;
                    high++;
                    GroundedProps.Feet(b, out float l, out float r);
                    foreach (var x in new[] { l, r })
                    {
                        var foot = new Vector3(x, b.min.y - 0.1f, child.position.z);
                        bool on = false;
                        foreach (var (owner, sb) in stone)
                        {
                            if (owner == child) continue;
                            var e = sb; e.Expand(new Vector3(0.1f, 2f * GroundedProps.FootGap, 0.1f));
                            if (e.Contains(foot)) { on = true; break; }
                        }
                        Assert.IsTrue(on, child.name + " at " + child.position + " (" + tag.layer + "): a foot at x " + x.ToString("F1") + " stands on nothing");
                    }
                }
                Assert.Greater(high, 0, "(some ruins still stand up high, on the towers behind the climb)");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
