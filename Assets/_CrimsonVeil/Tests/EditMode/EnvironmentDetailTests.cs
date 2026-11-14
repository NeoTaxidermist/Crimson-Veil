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
        public void NearRuinPieces_AreRebuiltWithMasonry()
        {
            foreach (var model in new[] { "RuinWall", "Tower" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitFactory.RuinDir + model + ".fbx");
                int tris = asset.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
                Assert.Greater(tris, 1500, model + " carries dressed stones, not a flat slab");
            }
        }

        [Test]
        public void EnvironmentRelief_FadesWithDepth()
        {
            var m = LevelKitMaterials.Get();
            float fg = m.inkFG.GetFloat("_Relief"), play = m.inkPlay.GetFloat("_Relief"), near = m.inkNear.GetFloat("_Relief"),
                  mid = m.inkMid.GetFloat("_Relief"), far = m.inkFar.GetFloat("_Relief");
            Assert.AreEqual(0f, fg, 1e-4f, "the foreground stays pure ink");
            Assert.Greater(play, 0.5f, "carved stone reads up close");
            Assert.Greater(play, near);
            Assert.Greater(near, mid);
            Assert.Greater(mid, far);
            Assert.Greater(far, 0f);
        }

        [Test]
        public void Arch_HasCarvedMasonryAndIvy()
        {
            const float depth = 1.2f;
            var plain = ProceduralMeshes.Arch(4f, 0.8f, 4.5f, 0.9f, depth, 0f, 3);
            Assert.Less(plain.bounds.min.z, -depth * 0.5f - 0.04f, "stones stand proud of the front face");
            int chamfers = plain.normals.Count(n => n.y > 0.6f && n.z < -0.6f);
            Assert.Greater(chamfers, 40, "dressed stones with moonlit chamfers");
            Assert.Greater(plain.vertexCount, 1500);
        }

        [Test]
        public void Column_IsFluted_WithAbacus_AndBrokenOnesShatter()
        {
            var whole = ProceduralMeshes.Column(6f, 0.38f, false, 1);
            // Flutes: the shaft's radius rises and falls round its circumference.
            float minR = float.MaxValue, maxR = 0f;
            foreach (var v in whole.vertices)
                if (v.y > 2.5f && v.y < 3.5f) { float r = new Vector2(v.x, v.z).magnitude; minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r); }
            Assert.Greater(maxR - minR, 0.015f, "fluted, not a smooth cylinder");
            Assert.AreEqual(6f, whole.bounds.max.y, 0.02f, "the abacus caps it at its full height");
            var broken = ProceduralMeshes.Column(6f, 0.38f, true, 1);
            Assert.Greater(broken.bounds.size.x, 0.38f * 2f * 1.45f + 0.3f, "fallen pieces lie beside the stump");
        }

        [Test]
        public void Masonry_HasDarkMortar_AndStonesOfVaryingShade()
        {
            var arch = ProceduralMeshes.Arch(4f, 0.8f, 4.5f, 0.9f, 1.2f, 0f, 2);
            var tones = new System.Collections.Generic.HashSet<int>(arch.colors.Select(c => Mathf.RoundToInt(c.r * 100f)));
            Assert.Greater(arch.colors.Length, 0, "the arch carries per-vertex tones");
            Assert.IsTrue(arch.colors.Any(c => c.r < 0.6f), "a dark mortar bed behind the stones");
            Assert.Greater(tones.Count(t => t >= 85), 8, "each stone its own shade");

            var wall = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitFactory.RuinDir + "Castle.fbx");
            var cols = wall.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.colors).ToArray();
            Assert.Greater(cols.Length, 0, "Blender masonry exports its tones too");
            Assert.IsTrue(cols.Any(c => c.r < 0.6f) && cols.Any(c => c.r > 0.75f && c.r < 0.99f));
            Assert.AreEqual(1f, LevelKitMaterials.Get().inkNear.GetFloat("_VertexTint"), 1e-4f, "the stone materials use them");
        }

        [Test]
        public void Urn_IsGadrooned()
        {
            var urn = ProceduralMeshes.Urn(3, 0.8f);
            float minR = float.MaxValue, maxR = 0f;
            foreach (var v in urn.vertices)
                if (v.y > 0.15f && v.y < 0.42f) { float r = new Vector2(v.x, v.z).magnitude; minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r); }
            Assert.Greater(maxR - minR, 0.006f, "lobed belly");
        }

        [Test]
        public void Enemies_AreRichlyModelled()
        {
            foreach (var (model, min) in new[] { ("ShieldMonk", 3500), ("HangedMan", 2000), ("RootCrawler", 2000), ("Crow", 900),
                                                  ("AshMoth", 2500), ("LanternWisp", 1200), ("PaperKite", 450) })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitMaterials.Root + "/Art/Enemy/" + model + ".fbx");
                Assert.IsNotNull(asset, model);
                int tris = asset.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
                Assert.Greater(tris, min, model);
            }
        }

        [Test]
        public void TerrainFace_HasStrata_InsideTheBlockAndBelowTheGrass()
        {
            var mesh = ProceduralMeshes.Terrain(8f, 4f, 4f, TerrainStyle.Rock, 0.7f, 5);
            var proud = mesh.vertices.Where(v => v.z < -2f - 0.02f).ToArray();
            Assert.Greater(proud.Length, 50, "slabs stand out of the front face");
            Assert.IsTrue(proud.All(v => v.y < -0.3f), "strata never reach the grass line");
            Assert.IsTrue(proud.All(v => Mathf.Abs(v.x) < 4.3f), "strata stay on the block");
            var ruin = ProceduralMeshes.Terrain(8f, 4f, 4f, TerrainStyle.Ruin, 0.7f, 5);
            Assert.Greater(ruin.normals.Count(n => n.y > 0.6f && n.z < -0.6f), 20, "ruin blocks are dressed ashlar");
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
