using CrimsonVeil.EditorTools;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Foreground grass must grow on the ground, never hang in the air over a pit.</summary>
    public class DecorationPlacementTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Floor from x -3 .. 3 with its top at y = 2; a pit everywhere else.
            LevelKitFactory.Terrain(new Vector3(0f, 2f, 0f), 6f, 2f);
            Physics.SyncTransforms();
        }

        [Test]
        public void Grass_SitsOnTheGround()
        {
            Assert.IsTrue(PrototypeSceneBuilder.GroundSpan(0f, 1.5f, out float y, out float w));
            Assert.AreEqual(2f, y, 0.15f, "foot on the floor top");
            Assert.Greater(w, 1f);
        }

        [Test]
        public void Grass_IsNotPlacedOverAPit()
        {
            Assert.IsFalse(PrototypeSceneBuilder.GroundSpan(10f, 1.5f, out _, out _));
        }

        [Test]
        public void Grass_ShrinksAtAnEdgeInsteadOfOverhangingIt()
        {
            if (PrototypeSceneBuilder.GroundSpan(2.4f, 1.5f, out _, out float w))
                Assert.LessOrEqual(2.4f + w * 0.5f, 3.2f, "clump stays on the block");
        }

        [Test]
        public void Bramble_FitsOnlyWhereItsCanesStayOnTheBlock()
        {
            Assert.IsTrue(PrototypeSceneBuilder.Fits("Bramble", 0f, 1f, out float y), "centre of a 6 m floor");
            Assert.AreEqual(2f, y, 0.15f);
            Assert.IsFalse(PrototypeSceneBuilder.Fits("Bramble", 2.2f, 1f, out _), "would hang over the edge");
            Assert.IsTrue(PrototypeSceneBuilder.Fits("Weeds", 1.8f, 1f, out _), "narrower weeds still fit");
        }

        [Test]
        public void TerrainGrass_GrowsFromTheFrontLip_AndCoversTheTop()
        {
            var block = LevelKitFactory.Terrain(new Vector3(20f, 0f, 0f), 6f, 2f).GetComponent<TerrainBlock>();
            var gen = block.transform.Find("__Generated");
            var front = gen.Find("GrassFront");
            Assert.LessOrEqual(front.localPosition.z, -block.depth * 0.5f + 0.2f, "no bare top face in front of the grass");
            Assert.IsNotNull(gen.Find("GrassMid"), "a middle row fills the top between front and back");
            Assert.Less(front.GetComponent<MeshFilter>().sharedMesh.bounds.min.y, -0.03f, "roots sink into the edge, no seam");
        }
    }
}
