using CrimsonVeil.CameraSystem;
using CrimsonVeil.EditorTools;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class CameraTests
    {
        static float Bias(bool grounded, float vy, float drop, float look) =>
            CameraFollow25D.ComputeVerticalBias(grounded, vy, drop, look, 2.5f, 3.5f, 0.28f, 5f, 4.5f);

        [Test]
        public void FlatGround_NoBias() => Assert.AreEqual(0f, Bias(true, 0f, 0f, 0f));

        [Test]
        public void DropAhead_LooksDown()
        {
            Assert.Less(Bias(true, 0f, 8f, 0f), -2f);
            Assert.GreaterOrEqual(Bias(true, 0f, 30f, 0f), -3.5f, "but not further than the cap");
        }

        [Test]
        public void Falling_LeadsDownwards() => Assert.Less(Bias(false, -12f, 0f, 0f), -3f);

        [Test]
        public void HoldingUpOrDown_Looks()
        {
            Assert.AreEqual(4.5f, Bias(true, 0f, 0f, 1f), 1e-4f);
            Assert.AreEqual(-4.5f, Bias(true, 0f, 0f, -1f), 1e-4f);
        }

        [Test]
        public void CameraZone_IsRelativeToItsObject()
        {
            var go = new GameObject("Zone");
            var z = go.AddComponent<CameraZone>();
            z.minX = -16f; z.maxX = 4f;
            go.transform.position = new Vector3(80f, 0f, 0f);
            Assert.IsTrue(z.Contains(72f));
            Assert.IsFalse(z.Contains(0f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RopeBridgeZone_FollowsTheBridgeWhenPlaced()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bridge = LevelKitCatalog.Place(LevelKitCatalog.Find("RopeBridge"), new Vector3(80f, 0f, 0f), LevelLayer.Playfield, null);
            var zone = bridge.GetComponentInChildren<CameraZone>();
            Assert.IsNotNull(zone);
            Assert.IsTrue(zone.Contains(75f), "standing near the rope post frames the rope");
            Assert.IsFalse(zone.Contains(10f));
        }
    }
}
