#if UNITY_EDITOR
using System.Collections;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The hard parts of the level's extension, played in the real scene with steered input: into the tower under its
    /// wall, up the wall-jump shaft, over the widest rampart gap, and up the steps out of the well.
    /// </summary>
    public class LevelExtensionPlayTests
    {
        const string ScenePath = "Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity";
        PlayerController pc;
        ScriptedInput input;

        [TearDown]
        public void TearDown() => CrimsonVeil.UI.PlaytestLog.WriteFiles = true;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CrimsonVeil.UI.PlaytestLog.WriteFiles = false;   // test runs are no playtests
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            pc = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(pc, "the level has a hero");
            // just the way, no fights
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is CrimsonVeil.Combat.CreatureBase || mb.GetType().Name == "Enemy") mb.gameObject.SetActive(false);
            input = pc.gameObject.AddComponent<ScriptedInput>();
            pc.InputSource = input;
            var vc = pc.GetComponent<VeilController>();
            if (vc != null)
                typeof(VeilController).GetField("input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vc, input);
        }

        IEnumerator At(float x, float y)
        {
            pc.Teleport(new Vector3(x, y + 0.05f, 0f));
            input.moveX = 0f; input.jumpHeld = false;
            float end = Time.time + 0.3f;
            while (Time.time < end) yield return null;
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        Vector3 P => pc.transform.position;

        [UnityTest]
        public IEnumerator IntoTheTower_UnderItsWall()
        {
            yield return At(112f, 0f);
            input.moveX = 1f;
            yield return Wait(1.2f);
            Assert.Greater(P.x, 118f, "walked in under the tower's left wall");
            Assert.AreEqual(0f, P.y, 0.2f);
        }

        /// <summary>
        /// Up a wall-jump shaft: in under its pillar (going <paramref name="dir"/> until past <paramref name="inX"/>),
        /// kicking from wall to wall (a hand may catch a lip: then climb), until at <paramref name="topY"/>; then out
        /// <paramref name="outDir"/> onto the top.
        /// </summary>
        IEnumerator Shaft(int dir, float inX, float topY, int outDir, float exitX = float.NaN)
        {
            input.moveX = dir;
            float end = Time.time + 2f;
            while (Time.time < end && (P.x - inX) * dir < 0f) yield return null;
            int wall = dir;
            input.jump = true; input.jumpHeld = true;
            end = Time.time + 14f;
            while (Time.time < end && P.y < topY - 0.1f)
            {
                var m = pc.Motor;
                if (m.State == MotorState.LedgeHang || m.State == MotorState.Climbing) { input.aimY = 1f; yield return null; continue; }
                if (m.State == MotorState.WallSlide && m.AttachSide == wall)
                {
                    wall = -wall;                          // kick off and steer to the other wall
                    input.jump = true;
                    input.moveX = wall;
                }
                yield return null;
            }
            // a hand may have caught the pillar's lip: let the climb finish
            end = Time.time + 2f;
            while (Time.time < end && pc.Motor.State != MotorState.Normal) { input.aimY = 1f; yield return null; }
            input.aimY = 0f;
            if (!float.IsNaN(exitX) && pc.IsGrounded && (P.x - exitX) * outDir < 0f)
            {
                // up on the pillar: the way on is across the shaft's mouth onto the wall's top
                yield return Hop(outDir, topY, exitX, 0f);
                yield break;
            }
            input.moveX = outDir;
            yield return Wait(1.0f);
            input.moveX = 0f;
            yield return Wait(0.4f);
        }

        /// <summary>A jump <paramref name="dir"/> to the next platform (top <paramref name="top"/>, beyond <paramref name="pastX"/>):
        /// climbing its lip if a hand catches it; stopped once standing on it.</summary>
        IEnumerator Hop(int dir, float top, float pastX, float runUp = 0.12f)
        {
            input.moveX = dir;
            if (runUp > 0f) yield return Wait(runUp);
            input.jump = true; input.jumpHeld = true;
            float end = Time.time + 4f;
            yield return Wait(0.15f);
            while (Time.time < end)
            {
                var m = pc.Motor;
                input.aimY = m.State == MotorState.LedgeHang ? 1f : 0f;
                if (m.State == MotorState.Climbing) input.moveX = 0f;
                // over the platform: let go of the direction (a player does, or he sails past a narrow ledge)
                if (!pc.IsGrounded && (P.x - pastX) * dir > 0f) input.moveX = 0f;
                if (pc.IsGrounded && Mathf.Abs(P.y - top) < 0.25f && (P.x - pastX) * dir > 0f) break;
                yield return null;
            }
            input.moveX = 0f; input.jumpHeld = false; input.aimY = 0f;
            yield return Wait(0.3f);
            Assert.AreEqual(top, P.y, 0.25f, "on the platform at " + top);
        }

        [UnityTest]
        public IEnumerator IntoTheBelfry_UnderItsWall_AndUpItsShaft()
        {
            yield return At(218f, 7f);
            input.moveX = 1f;
            yield return Wait(1.0f);
            Assert.Greater(P.x, 224f, "walked in under the belfry's left wall");
            Assert.AreEqual(7f, P.y, 0.2f);
            yield return At(232.6f, 27f);                  // on ledge 5
            yield return Shaft(1, 235.6f, 38f, 1, 238.2f);
            Assert.AreEqual(38f, P.y, 0.25f, "out of the shaft onto the belfry's top");
            Assert.Greater(P.x, 238f);
        }

        [UnityTest]
        public IEnumerator UpTheBelfryLedges()
        {
            yield return At(233f, 11f);                    // ledge 1
            yield return Hop(-1, 15f, 228.5f);             // to ledge 2
            yield return At(228.6f, 15f);
            yield return Hop(1, 19f, 232.5f);              // to ledge 3
            yield return At(232.4f, 19f);
            yield return Hop(-1, 23f, 228.5f);             // to ledge 4
        }

        [UnityTest]
        public IEnumerator AlongTheRidge_StoneByStone()
        {
            yield return At(250.8f, 38f);                  // near the end of the belfry's roof
            yield return Hop(1, 42f, 254.4f);
            yield return At(255.8f, 42f);
            yield return Hop(1, 45f, 259.4f);
            yield return At(262.8f, 45f);
            yield return Hop(1, 44f, 268.4f);
            yield return At(270.8f, 44f);
            yield return Hop(1, 44f, 276.4f);
        }

        [UnityTest]
        public IEnumerator DownTheChasm_LedgeByLedge()
        {
            yield return At(281.5f, 44f);
            yield return Hop(1, 36f, 284.9f);              // ledge 1
            yield return At(287.1f, 36f);
            yield return Hop(1, 29f, 291.6f);              // ledge 2
            yield return At(291.6f, 29f);
            yield return Hop(-1, 22f, 288.6f);             // ledge 3, back to the left
        }

        [UnityTest]
        public IEnumerator OutOfTheChasm_UpItsShaft_IntoTheGarden()
        {
            yield return At(303f, -14f);                   // the chasm's floor
            yield return Shaft(1, 307.6f, 2f, 1, 310.2f);
            Assert.AreEqual(2f, P.y, 0.25f, "out of the chasm");
            Assert.Greater(P.x, 310f);
        }

        [UnityTest]
        public IEnumerator UpTheShaft_ToTheTopOfTheTower()
        {
            yield return At(122f, 16f);                    // on ledge D
            input.moveX = -1f;                             // in under the pillar, into the shaft
            float end = Time.time + 1.5f;
            while (Time.time < end && P.x > 118.6f) yield return null;
            int dir = -1;
            input.jump = true; input.jumpHeld = true;
            end = Time.time + 8f;
            while (Time.time < end && P.y < 24.9f)
            {
                var m = pc.Motor;
                // high enough, a hand catches the pillar's lip: climb up onto it
                if (m.State == MotorState.LedgeHang || m.State == MotorState.Climbing) { input.aimY = 1f; yield return null; continue; }
                if (m.State == MotorState.WallSlide && m.AttachSide == dir)
                {
                    dir = -dir;                            // kick off and steer to the other wall
                    input.jump = true;
                    input.moveX = dir;
                }
                yield return null;
            }
            Assert.GreaterOrEqual(P.y, 24.9f, "wall-jumped up the shaft (and climbed the lip) to the top");
            input.aimY = 0f;
            input.moveX = 1f;                              // over the pillar onto the top
            yield return Wait(1.0f);
            input.moveX = 0f;
            yield return Wait(0.4f);
            Assert.AreEqual(25f, P.y, 0.2f, "standing on the top of the tower");
            Assert.Greater(P.x, 120.3f);
        }

        [UnityTest]
        public IEnumerator FromTheTowerTop_AlongTheRamparts_UpOntoTheHighOne()
        {
            yield return At(128f, 25f);                    // on the top of the tower
            input.moveX = 1f;
            float end = Time.time + 3f;
            while (Time.time < end && P.x < 140.3f) yield return null;
            Assert.AreEqual(25f, P.y, 0.2f, "walked out of the tower onto rampart 1");
            input.jump = true; input.jumpHeld = true;      // at its end: over the gap, up onto rampart 2
            end = Time.time + 2f;
            while (Time.time < end && !(pc.IsGrounded && P.x > 145f)) yield return null;
            input.moveX = 0f; input.jumpHeld = false;
            yield return Wait(0.3f);
            Assert.AreEqual(26.5f, P.y, 0.2f, "on rampart 2");
        }

        [UnityTest]
        public IEnumerator OverTheWidestRampartGap_WithADoubleJump()
        {
            yield return At(161f, 24f);                    // rampart 3
            input.moveX = 1f;
            yield return Wait(0.2f);
            input.jump = true; input.jumpHeld = true;
            yield return Wait(0.32f);
            input.jump = true;                             // the second jump, high in the first
            float end = Time.time + 2f;
            while (Time.time < end && !(pc.IsGrounded && P.x > 169f)) yield return null;
            input.moveX = 0f; input.jumpHeld = false;     // landed: stop (rampart 4 is only 7 m long)
            yield return Wait(0.4f);
            Assert.AreEqual(25f, P.y, 0.2f, "landed on rampart 4");
            Assert.Greater(P.x, 169f);
        }

        [UnityTest]
        public IEnumerator OutOfTheWell_UpTheSteps()
        {
            yield return At(192f, -4f);
            input.moveX = 1f;
            float end = Time.time + 10f;
            while (Time.time < end && !(P.y > 6.8f && P.x > 204f))
            {
                var m = pc.Motor;
                if (m.State == MotorState.LedgeHang) input.aimY = 1f;          // climb every lip
                else input.aimY = 0f;
                if (m.IsGrounded && Mathf.Abs(pc.Velocity.x) < 0.5f) { input.jump = true; input.jumpHeld = true; }   // stopped by a step: jump at it
                else if (pc.Velocity.y < 0f) input.jumpHeld = false;
                yield return null;
            }
            Assert.Greater(P.y, 6.8f, "up on the plateau");
            Assert.Greater(P.x, 204f);
        }
    }
}
#endif
