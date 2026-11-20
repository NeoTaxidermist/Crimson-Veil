using System;
using System.Collections;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The body as it is drawn against the level: the controller's capsule is upright and narrow, but the body bends
    /// out of it - lying down in a slide, curled in a roll, turned over in the somersault, hung along the whip. Each
    /// move is done into a wall (and onto the floor) on the real hero, and the drawn body (every vertex of its skin)
    /// must stay out of the stone.
    /// </summary>
    /// <summary>Calls back once a frame after all the animation (springs, face, scarf), as the frame is drawn.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class EndOfFrameProbe : MonoBehaviour
    {
        public Action each;
        void LateUpdate() => each?.Invoke();
    }

    public class HeroBodyWallPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";
        /// <summary>How far (m) the drawn body may go into stone: a cloth fold, a hair's breadth.</summary>
        const float Tolerance = 0.05f;
        const float Wall = 10f;   // the wall's face (x); it stands from x 10 to 14, 16 m high

        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;
        Mesh baked;

        GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(root.transform);
            b.transform.position = center;
            b.transform.localScale = size;
            return b;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("WallRoom");
            Box("floor", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 4f));
            Box("wall", new Vector3(Wall + 2f, 8f, 0f), new Vector3(4f, 16f, 4f));
            holder = new GameObject("Holder");
            holder.SetActive(false);
            player = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab"), holder.transform);
            Object.DestroyImmediate(player.GetComponent<PlayerInputReader>());
            input = player.AddComponent<ScriptedInput>();
            controller = player.GetComponent<PlayerController>();
            controller.InputSource = input;
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.SetParent(null, true);
            Object.Destroy(holder);
            player.SetActive(true);
            var veil = player.GetComponent<VeilController>();
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm))) if (f != VeilForm.Flow) veil.Unlock(f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(player);
            Object.Destroy(root);
            if (baked != null) Object.Destroy(baked);
        }

        Vector3 Pos => player.transform.position;

        /// <summary>The drawn body's extent: the furthest right (into the wall) and the lowest (into the floor).</summary>
        void Extent(out float right, out float low)
        {
            if (baked == null) baked = new Mesh();
            right = -99f; low = 99f;
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled) continue;
                smr.BakeMesh(baked, true);
                var t = smr.transform;
                foreach (var v in baked.vertices)
                {
                    var w = t.TransformPoint(v);
                    if (w.x > right && w.y < wallTop) { right = w.x; rightY = w.y; }   // (over the top of the block there is no wall)
                    if (w.y < low) low = w.y;
                }
            }
        }

        float intoWall, intoFloor;
        float wallTop = 16f, rightY;
        string worst;

        void Measure(string phase)
        {
            Extent(out float right, out float low);
            float wall = right - Wall;
            // (into the floor only where the floor is: out in front of the wall)
            float floor = -low;
            if (wall > intoWall) { intoWall = wall; worst = $"{phase} at x {Pos.x:F2} y {Pos.y:F2} state {controller.Motor.State}, the deepest point {rightY - Pos.y:F2} m over the feet"; }
            if (floor > intoFloor) intoFloor = floor;
        }

        /// <summary>
        /// Measures the body as it is drawn: at the end of the frame, after all the animation (a coroutine runs between
        /// the capsule's move and the body's pose - a state never on screen).
        /// </summary>
        IEnumerator Watch(float seconds, string phase, Func<bool> until = null)
        {
            var probe = player.GetComponent<EndOfFrameProbe>();
            if (probe == null) probe = player.AddComponent<EndOfFrameProbe>();
            probe.each = () => Measure(phase);
            float end = Time.time + seconds;
            while (Time.time < end && (until == null || !until())) yield return null;
            yield return null;
            probe.each = null;
        }

        IEnumerator StartAt(Vector3 at)
        {
            controller.Teleport(at);
            input.moveX = 0f; input.aimY = 0f; input.stand = false;
            yield return Watch(0.5f, "settling");
            intoWall = 0f; intoFloor = 0f; worst = "-";
        }

        void AssertOut(string what)
        {
            Debug.Log($"[body vs wall] {what}: into wall {intoWall:F3} m ({worst}), into floor {intoFloor:F3} m");
            Assert.Less(intoWall, Tolerance, $"{what}: the body went {intoWall:F2} m into the wall ({worst})");
            Assert.Less(intoFloor, Tolerance, $"{what}: the body went {intoFloor:F2} m into the floor");
        }

        [UnityTest]
        public IEnumerator Walking_AndRunning_IntoAWall()
        {
            yield return StartAt(new Vector3(Wall - 3f, 0.05f, 0f));
            input.moveX = 0.3f;
            yield return Watch(3f, "walking into it");
            yield return StartAt(new Vector3(Wall - 8f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Watch(2.5f, "running into it");
            AssertOut("walking and running");
        }

        [UnityTest]
        public IEnumerator Sliding_IntoAWall()
        {
            yield return StartAt(new Vector3(Wall - 9f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Watch(0.6f, "run-up");
            input.aimY = -1f; input.dash = true;
            yield return Watch(2f, "slide");
            AssertOut("sliding");
        }

        [UnityTest]
        public IEnumerator Crawling_IntoAWall()
        {
            yield return StartAt(new Vector3(Wall - 2.5f, 0.05f, 0f));
            input.aimY = -1f;
            yield return Watch(0.4f, "crouch");
            input.moveX = 1f;
            yield return Watch(3f, "crawl");
            AssertOut("crawling");
        }

        [UnityTest]
        public IEnumerator Dashing_IntoAWall()
        {
            yield return StartAt(new Vector3(Wall - 6f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Watch(0.4f, "run-up");
            input.dash = true;
            yield return Watch(1.5f, "dash");
            AssertOut("dashing");
        }

        [UnityTest]
        public IEnumerator RollingOutOfADrop_IntoAWall()
        {
            yield return StartAt(new Vector3(Wall - 11f, 0.05f, 0f));
            controller.Teleport(new Vector3(Wall - 11f, 9f, 0f));
            input.moveX = 1f;
            yield return Watch(3f, "drop and roll");
            AssertOut("rolling");
        }

        [UnityTest]
        public IEnumerator HardLanding_AgainstAWall()
        {
            yield return StartAt(new Vector3(Wall - 0.5f, 0.05f, 0f));
            controller.Teleport(new Vector3(Wall - 0.4f, 9f, 0f));
            yield return Watch(3f, "drop");
            AssertOut("a hard landing");
        }

        [UnityTest]
        public IEnumerator Somersault_AtAWall()
        {
            yield return StartAt(new Vector3(Wall - 1.5f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Watch(0.5f, "up to it");
            input.jump = true; input.jumpHeld = true;
            yield return Watch(1.5f, "jump", () => controller.Velocity.y < 2f && !controller.Motor.IsGrounded);
            input.jump = true;
            yield return Watch(0.4f, "somersault");
            input.jumpHeld = false;
            yield return Watch(3f, "somersault", () => controller.Motor.IsGrounded);
            yield return Watch(0.5f, "landed");
            AssertOut("the somersault");
        }

        [UnityTest]
        public IEnumerator HangingOnALip_AndClimbing()
        {
            // a lower block to hang on: its lip 4.2 m up, its face at the wall's
            Object.Destroy(root.transform.Find("wall").gameObject);
            Box("block", new Vector3(Wall + 2f, 2.1f, 0f), new Vector3(4f, 4.2f, 4f));
            wallTop = 4.2f;
            yield return StartAt(new Vector3(Wall - 3.5f, 0.05f, 0f));
            input.moveX = 0.6f;
            yield return Watch(0.3f, "run-up");
            input.jump = true; input.jumpHeld = true;
            yield return Watch(2f, "jump", () => controller.Motor.State == MotorState.LedgeHang);
            input.jumpHeld = false; input.moveX = 0f;
            Assert.AreEqual(MotorState.LedgeHang, controller.Motor.State, "caught the lip");
            yield return Watch(1f, "hanging");
            // (climbing over the top the body goes over the block: only the hang and the way up its face count)
            AssertOut("hanging on the lip");
        }
    }
}
