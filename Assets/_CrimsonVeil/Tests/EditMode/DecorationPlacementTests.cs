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
    }
}
