using CrimsonVeil.EditorTools;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrimsonVeil.Tests
{
    /// <summary>Every piece in the Level Editor palette must place cleanly.</summary>
    public class LevelKitTests
    {
        Scene scene;

        // The test runner restores the user's scenes after the run.
        [SetUp]
        public void SetUp() => scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static System.Collections.Generic.IEnumerable<string> Ids()
        {
            foreach (var e in LevelKitCatalog.Entries) yield return e.id;
        }

        [TestCaseSource(nameof(Ids))]
        public void Piece_PlacesAtTheClickedPoint(string id)
        {
            var entry = LevelKitCatalog.Find(id);
            var go = LevelKitCatalog.Place(entry, new Vector3(12.5f, 3f, 0f), LevelLayer.Mid, null);
            Assert.IsNotNull(go, id);
            Assert.AreEqual(scene, go.scene, "placed into the active scene");
            if (entry.category != LevelKitCatalog.Category.Scena)
            {
                Assert.AreEqual(12.5f, go.transform.position.x, 1e-3f);
                Assert.AreEqual(3f, go.transform.position.y, 1e-3f);
                float expectedZ = entry.layered ? DepthLayer.Z(LevelLayer.Mid) : 0f;
                Assert.AreEqual(expectedZ, go.transform.position.z, 1e-3f, "depth layer");
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    Assert.IsNotNull(m, $"{id}: renderer {r.name} has a missing material");
        }

        [Test]
        public void Terrain_GeneratesBodyGrassAndExactCollider()
        {
            var go = LevelKitCatalog.Place(LevelKitCatalog.Find("Platform"), new Vector3(0f, 2f, 0f), LevelLayer.Playfield, null);
            var t = go.GetComponent<TerrainBlock>();
            t.width = 7f; t.height = 3f; t.Rebuild();
            var box = go.GetComponent<BoxCollider>();
            Assert.AreEqual(new Vector3(7f, 3f, t.depth), box.size);
            Assert.AreEqual(-1.5f, box.center.y, 1e-4f, "top of the collider is the walkable top");
            var gen = go.transform.Find("__Generated");
            Assert.IsNotNull(gen.Find("Body"));
            var grass = gen.Find("GrassFront").GetComponent<MeshFilter>().sharedMesh;
            Assert.LessOrEqual(grass.bounds.max.x, 3.5f + 1e-3f, "grass never overhangs the right edge");
            Assert.GreaterOrEqual(grass.bounds.min.x, -3.5f - 1e-3f, "nor the left edge");
        }

        [Test]
        public void ApplyLayer_MovesAndRetintsDecoration()
        {
            var go = LevelKitCatalog.Place(LevelKitCatalog.Find("Arch"), Vector3.zero, LevelLayer.Near, null);
            var mats = LevelKitMaterials.Get();
            LevelKitFactory.ApplyLayer(go, LevelLayer.Far);
            Assert.AreEqual(DepthLayer.Z(LevelLayer.Far), go.transform.position.z, 1e-3f);
            Assert.AreEqual(mats.inkFar, go.GetComponent<Renderer>().sharedMaterial);
            Assert.Greater(go.transform.localScale.x, 1.5f, "far ruins are drawn bigger");
        }
    }
}
