using System;
using System.Collections;
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
    /// The shield's ring on the real hero, measured as the frame is drawn: it stays a ring round him when he turns
    /// round and runs the other way. (Mirrored with him, every node of it crossed to the far side of the circle: halfway
    /// through the turn it was squashed into two upright bars, then trailed back together with a gap and a loose end.)
    /// </summary>
    [Category("Veil")]
    public class ShieldRingPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";
        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("RingRoom");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 4f);
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
            veil = player.GetComponent<VeilController>();
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm))) if (f != VeilForm.Flow) veil.Unlock(f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(player);
            Object.Destroy(root);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return null;
        }

        float roundMin = 99f, roundMax = 0f, worstLink;
        int ringFrames;

        /// <summary>How round the ring is right now: the least and the most distance of its cloth from the middle of the
        /// body, in its radii, and the longest gap between two neighbouring nodes of it (m).</summary>
        void MeasureRing()
        {
            if (veil.ShapeForm != VeilForm.Shield || veil.TransformProgress < 0.95f) return;
            var sim = veil.Simulation;
            var pos = sim.Positions;
            int n = sim.Count;
            int from = Mathf.Clamp(Mathf.CeilToInt(veil.HandFraction * (n - 1)) + 3, 0, n - 1);
            Vector3 c = veil.Anchor.position + new Vector3(veil.ShieldCentre.x, veil.ShieldCentre.y, 0f);
            float r = VeilFormLibrary.ShieldRadius;
            for (int i = from; i < n; i++)
            {
                float d = new Vector2(pos[i].x - c.x, pos[i].y - c.y).magnitude / r;
                roundMin = Mathf.Min(roundMin, d);
                roundMax = Mathf.Max(roundMax, d);
                if (i > from) worstLink = Mathf.Max(worstLink, Vector2.Distance(pos[i], pos[i - 1]));
            }
            ringFrames++;
        }

        [UnityTest]
        public IEnumerator TurningRound_WithTheShieldUp_TheRingStaysARingRoundHim()
        {
            yield return Wait(0.4f);
            input.held = VeilForm.Shield;
            input.cast = VeilForm.Shield;
            input.moveX = 1f;
            yield return Wait(0.8f);
            Assert.AreEqual(VeilForm.Shield, veil.Form, "the shield is up");
            var probe = player.AddComponent<EndOfFrameProbe>();
            probe.each = MeasureRing;
            // running right, then turned round and running left, twice
            for (int k = 0; k < 2; k++)
            {
                input.moveX = -1f;
                yield return Wait(0.7f);
                input.moveX = 1f;
                yield return Wait(0.7f);
            }
            probe.each = null;
            input.moveX = 0f; input.held = null;
            Debug.Log($"[shield ring] {ringFrames} frames through four turns: the cloth {roundMin:F2} to {roundMax:F2} radii from the middle; the longest link {worstLink:F2} m");
            Assert.Greater(ringFrames, 60, "the ring was up all through");
            Assert.Greater(roundMin, 0.75f, $"never squashed in to the body ({roundMin:F2} of its radius; in the middle of a turn it came in to 0 at the sides)");
            Assert.Less(roundMax, 1.3f, $"never flung out of the ring ({roundMax:F2} of its radius)");
            Assert.Less(worstLink, 0.35f, $"and it holds together: no gap opens between its nodes ({worstLink:F2} m)");
        }
    }
}
