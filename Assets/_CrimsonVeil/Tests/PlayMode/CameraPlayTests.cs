using System.Collections;
using CrimsonVeil.CameraSystem;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The follow camera in a running scene: camera zones and arriving somewhere new.</summary>
    public class CameraPlayTests
    {
        GameObject root, player, cam;
        PlayerController controller;
        CameraFollow25D follow;
        static readonly Vector3 ZoneOffset = new Vector3(0f, 5f, -22f);

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CameraLevel");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(20f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 4f);

            var zone = new GameObject("Zone").AddComponent<CameraZone>();
            zone.transform.SetParent(root.transform);
            zone.transform.position = new Vector3(40f, 0f, 0f);
            zone.minX = -5f; zone.maxX = 5f;
            zone.offset = ZoneOffset;

            player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<ScriptedInput>();
            controller = player.AddComponent<PlayerController>();
            player.SetActive(true);

            cam = new GameObject("TestCamera");
            cam.AddComponent<Camera>();
            follow = cam.AddComponent<CameraFollow25D>();
            follow.Target = controller;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(cam);
            Object.Destroy(player);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ArrivingInACameraZone_TakesItsFramingAtOnce()
        {
            yield return new WaitForSeconds(0.3f);
            controller.Teleport(new Vector3(40f, 0.05f, 0f));
            yield return null;
            yield return null;
            Assert.AreEqual(ZoneOffset.z, cam.transform.position.z, 0.05f, "a respawn in a zone does not drift into its framing");
            Assert.AreEqual(40f, cam.transform.position.x, 1f);
        }
    }
}
