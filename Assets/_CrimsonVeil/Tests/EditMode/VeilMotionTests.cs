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
        static readonly float TurnAcceleration = new CrimsonVeil.Player.MotorSettings().turnAcceleration;

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
            // he turns round and runs back (his run swings over at the motor's turn acceleration)
            sim.DriftDirection = 1f;
            sim.AmbientFlow = new Vector2(0.75f, 0f);
            float v = 6f;
            for (int f = 0; f < 90; f++)
            {
                v = Mathf.MoveTowards(v, -6f, TurnAcceleration * Dt);
                neck.x += v * Dt;
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
            // (the turn runs down the scarf as a travelling fold - VeilSimulation.TurnTravel - whose crest bends sharply for a moment)
            Assert.Less(sharp, measured * 0.09f, "sharp bends only in the crest of the swing, here and there");
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
        public void TheTurn_RunsDownTheScarf_FromTheNeck()
        {
            // a moment into the turn: the neck has swung over, the tail not yet
            float t = 0.25f;
            Assert.Greater(VeilSimulation.DriftAt(-1f, 1f, t, 0f), 0.5f, "the cloth by the neck turns first");
            Assert.AreEqual(-1f, VeilSimulation.DriftAt(-1f, 1f, t, 1f), 1e-4f, "the tail still streams the old way");
            float last = 2f;
            for (int i = 0; i <= 10; i++)
            {
                float d = VeilSimulation.DriftAt(-1f, 1f, t, i / 10f);
                Assert.LessOrEqual(d, last + 1e-5f, "one wave from the neck to the tip");
                last = d;
            }
            Assert.AreEqual(1f, VeilSimulation.DriftAt(-1f, 1f, VeilSimulation.TurnTravel + VeilSimulation.TurnBlend, 1f), 1e-4f, "and it is over within about three quarters of a second");
        }

        [Test]
        public void TurningRound_TheScarfSwingsOver_WithoutBunchingAtTheHead()
        {
            foreach (float speed in new[] { 3f, 6f })
            {
                var sim = new VeilSimulation(new VeilSettings(), Vector3.zero) { DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f };
                Vector3 neck = Vector3.zero;
                float v = speed;
                for (int f = 0; f < 180; f++) { neck.x += v * Dt; sim.Step(Dt, neck); }
                sim.DriftDirection = 1f;
                sim.AmbientFlow = new Vector2(0.75f, 0f);
                int mostAtTheHead = 0;
                float biggestJump = 0f;
                var last = new Vector3[sim.Count];
                for (int i = 0; i < sim.Count; i++) last[i] = sim.Positions[i] - neck;
                for (int f = 0; f < 90; f++)
                {
                    v = Mathf.MoveTowards(v, -speed, TurnAcceleration * Dt);
                    neck.x += v * Dt;
                    sim.Step(Dt, neck);
                    int atTheHead = 0;
                    for (int i = sim.Settings.wrapNodes + 2; i < sim.Count; i++)
                    {
                        Vector3 rel = sim.Positions[i] - neck;
                        if (rel.magnitude < 0.5f) atTheHead++;
                        biggestJump = Mathf.Max(biggestJump, Vector3.Distance(rel, last[i]));
                        last[i] = rel;
                    }
                    mostAtTheHead = Mathf.Max(mostAtTheHead, atTheHead);
                }
                // (flipped all at once, ~30 points of cloth piled up within half a metre of his head, and points leapt 0.4-0.5 m a frame)
                Assert.LessOrEqual(mostAtTheHead, 20, $"at {speed} m/s it swings over without crumpling into a ball round his head");
                Assert.Less(biggestJump, 0.35f, $"at {speed} m/s no part of it leaps across in a frame");
            }
        }

        [Test]
        public void Falling_TheScarfStreamsUpBehindHim()
        {
            Assert.Greater(VeilController.TrailTiltFor(new Vector3(-5f, -15f, 0f)), 0.8f, "a fall tilts it well up");
            Assert.LessOrEqual(VeilController.TrailTiltFor(new Vector3(0f, -40f, 0f)), VeilController.MaxTiltUp, "but never straight up");
            Assert.AreEqual(0f, VeilController.TrailTiltFor(new Vector3(6f, -1f, 0f)), 1e-4f, "a step down leaves it level");
            Assert.Less(VeilController.TrailTiltFor(new Vector3(0f, 10f, 0f)), 0f, "leaping up it trails a little lower");
            Assert.GreaterOrEqual(VeilController.TrailTiltFor(new Vector3(0f, 40f, 0f)), -VeilController.MaxTiltDown);
            Assert.AreEqual(1f, VeilRenderer.SettleInTilt(0f), 1e-4f);
            Assert.AreEqual(0f, VeilRenderer.SettleInTilt(0.6f), 1e-4f, "streaming up over him it is not cut at the chest line");

            // the cloth itself: falling for a second, its middle and tail are above the neck
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero) { DriftDirection = 1f, AmbientFlow = new Vector2(0.75f, 0f), RestShapeWeight = 1f };
            Vector3 neck = Vector3.zero;
            for (int f = 0; f < 120; f++) sim.Step(Dt, neck);
            float vy = 0f;
            for (int f = 0; f < 60; f++)
            {
                vy = Mathf.Max(vy - 30f * Dt, -18f);
                neck += new Vector3(-4f, vy, 0f) * Dt;
                sim.Step(Dt, neck);
                sim.TrailTilt = Mathf.MoveTowards(sim.TrailTilt, VeilController.TrailTiltFor(sim.AnchorVelocity), Dt * VeilController.TrailTiltRate);
            }
            float above = 0f;
            int n = 0;
            for (int i = sim.Count / 2; i < sim.Count; i++) { above += sim.Positions[i].y - neck.y; n++; }
            above /= n;
            Debug.Log($"fall: tail {above:F2} m above the neck");
            Assert.Greater(above, 0.8f, "the scarf flies up behind him (level, it caught on the ledge he dropped off)");
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
