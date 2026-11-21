using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The scarf in motion: smooth from frame to frame. Its whorls ease round instead of swinging with every flicker of
    /// a strand's tip, the cloth folds in curves instead of crumpling node by node when he turns round, its tongues stay
    /// with the band, the fold back behind the chest comes in gently, and nothing flies off it like broken pieces.
    /// </summary>
    public class VeilMotionTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void AWhorl_EasesRound_TheShortWay()
        {
            float a = VeilRenderer.EaseAngle(0f, Mathf.PI * 0.5f, VeilRenderer.CurlEase, Dt);
            Assert.Greater(a, 0f);
            Assert.Less(a, 0.15f, "a frame's flicker barely turns it");
            // across the seam of the circle: the short way round
            float b = VeilRenderer.EaseAngle(Mathf.PI * 0.95f, -Mathf.PI * 0.95f, VeilRenderer.CurlEase, Dt);
            Assert.Greater(b, Mathf.PI * 0.95f, "turning on through pi, not back round through zero");
            float c = 0f;
            for (int i = 0; i < 120; i++) c = VeilRenderer.EaseAngle(c, 1f, VeilRenderer.CurlEase, Dt);
            Assert.AreEqual(1f, c, 0.01f, "it gets there within two seconds");
            Assert.That(VeilRenderer.CurlEase, Is.InRange(2f, 10f));
        }

        [Test]
        public void TheFoldBehindTheChest_ComesInGently()
        {
            Assert.That(VeilRenderer.SettleRate, Is.InRange(0.5f, 4f), "over a fraction of a second, not within a frame");
        }

        [Test]
        public void TurningRound_TheClothFoldsInCurves_NotAZigzag()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero) { DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f };
            Vector3 neck = Vector3.zero;
            for (int f = 0; f < 180; f++) { neck.x += 6f * Dt; sim.Step(Dt, neck); }
            float worst = 0f;
            int sharp = 0, measured = 0;
            // he turns round (at once: harsher than the game, where his facing eases over) and runs back
            sim.DriftDirection = 1f;
            sim.AmbientFlow = new Vector2(0.75f, 0f);
            for (int f = 0; f < 90; f++)
            {
                neck.x -= 6f * Dt;
                sim.Step(Dt, neck);
                for (int i = sim.Settings.wrapNodes + 2; i < sim.Count - 1; i++)
                {
                    Vector3 a = sim.Positions[i] - sim.Positions[i - 1], b = sim.Positions[i + 1] - sim.Positions[i];
                    if (a.sqrMagnitude < 1e-8f || b.sqrMagnitude < 1e-8f) continue;
                    float angle = Vector3.Angle(a, b);
                    worst = Mathf.Max(worst, angle);
                    measured++;
                    if (angle > VeilSimulation.MaxKink) sharp++;
                }
            }
            Assert.Less(worst, 90f, "no node folds back on its neighbours (drawn, a zigzag broke the cloth into pieces)");
            Assert.Less(sharp, measured * 0.06f, "sharp bends only in the moment of the swing, here and there");
            Assert.AreEqual(0f, VeilSimulation.KinkExcess(Vector3.zero, Vector3.right, new Vector3(2f, 0.3f, 0f)), 1e-6f, "a gentle curve is left alone");
            Assert.Greater(VeilSimulation.KinkExcess(Vector3.zero, Vector3.right, Vector3.zero), 0.9f, "folded right back");
        }

        [Test]
        public void TheTongues_StayWithTheBand_WhenHeTurnsRound()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero) { DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f };
            var strands = new VeilStrands(VeilStrands.FromDrawing());
            Vector3 neck = Vector3.zero;
            float worst = 0f;
            for (int f = 0; f < 300; f++)
            {
                if (f == 150) { sim.DriftDirection = 1f; sim.AmbientFlow = new Vector2(0.75f, 0f); }
                neck.x += (f < 150 ? 6f : -6f) * Dt;
                sim.Step(Dt, neck);
                strands.Step(Dt, sim);
                for (int s = 0; s < strands.Count; s++)
                    for (int j = 1; j < VeilStrands.Nodes; j++)
                        worst = Mathf.Max(worst, Vector3.Distance(strands.Positions[s][j], strands.Homes[s][j]));
            }
            Assert.LessOrEqual(worst, VeilStrands.MaxStray + 0.12f, "never far from its place by the band (they hung under it like a comb)");
        }

        [Test]
        public void TheLooseScarf_ShedsNoPieces()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Player.prefab");
            var wisps = prefab.GetComponentInChildren<VeilWisps>(true);
            var so = new SerializedObject(wisps);
            Assert.AreEqual(0f, so.FindProperty("baseRate").floatValue, "no drops or threads off the loose scarf: in motion they read as broken pieces of it");
            Assert.AreEqual(0f, so.FindProperty("speedRate").floatValue);
            Assert.Greater(so.FindProperty("weaponRate").floatValue, 0f, "a weapon still smokes");
        }
    }
}
