using System.Linq;
using CrimsonVeil.EditorTools;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The environment matches the key art: lighter grey layers and many modelled set pieces
    /// (aqueduct, castle, cliff village, brambles, weeds, chains, fallen leaves).
    /// </summary>
    public class EnvironmentDetailTests
    {
        [Test]
        public void AllRuinModels_ExistAsMeshes()
        {
            foreach (var model in LevelKitFactory.RuinModels)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitFactory.RuinDir + model + ".fbx");
                Assert.IsNotNull(asset, model + ".fbx");
                int tris = asset.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
                Assert.Greater(tris, 100, model + " is a real model, not a placeholder");
            }
        }

        [Test]
        public void BigLandmarks_AreRichlyDetailed()
        {
            foreach (var model in new[] { "Aqueduct", "Castle", "CliffRuin" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitFactory.RuinDir + model + ".fbx");
                int tris = asset.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
                Assert.Greater(tris, 1500, model);
            }
        }

        [Test]
        public void Ruin_TintsStoneByLayer_AndKeepsCrimsonSlots()
        {
            var m = LevelKitMaterials.Get();
            var leaves = LevelKitFactory.Ruin("LeafPile", Vector3.zero, LevelLayer.Near);
            var aqueduct = LevelKitFactory.Ruin("Aqueduct", Vector3.zero, LevelLayer.Mid);
            try
            {
                Assert.IsTrue(leaves.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).All(x => x == m.crimson), "fallen leaves are crimson");
                Assert.IsTrue(aqueduct.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).All(x => x == m.inkMid), "stone takes the layer ink");
                Assert.AreEqual(LevelKitFactory.LayerScale(LevelLayer.Mid), aqueduct.transform.localScale.x, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(leaves);
                Object.DestroyImmediate(aqueduct);
            }
        }

        [Test]
        public void Crimson_IsARedAccent()
        {
            var c = LevelKitMaterials.Get().crimson.GetColor("_BaseColor");
            Assert.Greater(c.r, 0.5f);
            Assert.Less(c.g, 0.1f);
        }

        [Test]
        public void Gallows_HangsAModelledChain_AndTheTreeHasFallenLeaves()
        {
            var gallows = LevelKitFactory.Gallows(Vector3.zero);
            var tree = LevelKitFactory.CrimsonTree(Vector3.zero);
            try
            {
                var chain = gallows.transform.Find("Chain");
                Assert.IsNotNull(chain);
                Assert.IsNotNull(chain.GetComponentInChildren<MeshFilter>(), "a real chain model, not a stick");
                Assert.IsNull(chain.GetComponent<DepthLayer>(), "the gallows root carries the layer");
                Assert.IsNotNull(tree.transform.Find("LeafPile"));
                Assert.GreaterOrEqual(tree.GetComponent<TreeProp>().leavesPerTip, 24, "a dense canopy");
            }
            finally
            {
                Object.DestroyImmediate(gallows);
                Object.DestroyImmediate(tree);
            }
        }

        [Test]
        public void Catalog_OffersTheNewPieces()
        {
            foreach (var id in new[] { "Aqueduct", "Castle", "CliffRuin", "Bramble", "Weeds", "Chain", "LeafPile" })
            {
                var e = LevelKitCatalog.Find(id);
                Assert.IsNotNull(e, id);
                Assert.IsFalse(string.IsNullOrEmpty(e.label), id + " has a Czech label");
            }
        }

        [Test]
        public void EnvironmentLayers_AreLighterGreyWithDepth()
        {
            var m = LevelKitMaterials.Get();
            float play = m.inkPlay.GetColor("_BaseColor").grayscale, near = m.inkNear.GetColor("_BaseColor").grayscale,
                  mid = m.inkMid.GetColor("_BaseColor").grayscale, far = m.inkFar.GetColor("_BaseColor").grayscale;
            Assert.Greater(play, 0.05f, "the walkable rock is dark grey, not black");
            Assert.Less(play, near);
            Assert.Less(near, mid);
            Assert.Less(mid, far);
            Assert.Greater(mid, 0.4f, "mid layer is light, as in the key art");
        }
    }
}
