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
        public void TheCloth_BendsInCurves_HeldByItsBendingConstraint()
        {
            // two segments bent by MaxBend at their middle node span SkipLength
            float seg = 0.1f;
            float a = VeilSimulation.MaxBend * Mathf.Deg2Rad;
            Vector3 p0 = Vector3.zero, p1 = new Vector3(seg, 0f, 0f), p2 = p1 + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * seg;
            Assert.AreEqual(VeilSimulation.SkipLength(seg), Vector3.Distance(p0, p2), 1e-4f);
            Assert.That(VeilSimulation.MaxBend, Is.InRange(20f, VeilSimulation.MaxKink), "an S still has room; no fold");
            // turning round, the cloth is led along the fold: at once, all through the turn, and let go gently after
            float end = VeilSimulation.TurnTravel + VeilSimulation.TurnBlend;
            Assert.AreEqual(0f, VeilSimulation.TurnGuide(99f), 1e-5f, "no turn: left to itself");
            Assert.AreEqual(0f, VeilSimulation.TurnGuide(0f), 1e-5f, "it comes in from nothing");
            Assert.AreEqual(1f, VeilSimulation.TurnGuide(0.1f), 1e-5f, "led within a twentieth of a second");
            Assert.AreEqual(1f, VeilSimulation.TurnGuide(end), 1e-5f, "until the fold has run off the tip");
            Assert.That(VeilSimulation.TurnGuide(end + 0.5f * VeilSimulation.GuideSettle), Is.InRange(0.3f, 0.7f), "let go over a moment, not at once");
            Assert.AreEqual(0f, VeilSimulation.TurnGuide(end + VeilSimulation.GuideSettle), 1e-5f);
        }

        [Test]
        public void TurningRound_TheScarfGoesOverSlim_AndOpensOutAfter()
        {
            // how far the tip has swung round: not yet, halfway over the top, trailing behind him again
            Assert.AreEqual(0f, VeilRenderer.SwungOver(1f, -1f), 1e-4f, "still streaming the old way");
            Assert.AreEqual(0f, VeilRenderer.SwungOver(0f, -1f), 1e-4f, "over the top");
            Assert.AreEqual(1f, VeilRenderer.SwungOver(-1f, -1f), 1e-4f, "behind him again");
            Assert.AreEqual(1f, VeilRenderer.SwungOver(0f, 0f), 1e-4f, "no breeze: nothing to swing");
            // it slims at once as the swing starts and opens out over a moment after
            float slim = 0f;
            for (int f = 0; f < 10; f++) slim = VeilRenderer.TurnSlim(slim, 0f, Dt);
            Assert.AreEqual(1f, slim, 1e-4f, "slim within a sixth of a second");
            float half = slim;
            for (int f = 0; f < 6; f++) half = VeilRenderer.TurnSlim(half, 1f, Dt);
            Assert.That(half, Is.InRange(0.5f, 0.9f), "opening out gently, not at once");
            for (int f = 0; f < 60; f++) slim = VeilRenderer.TurnSlim(slim, 1f, Dt);
            Assert.AreEqual(0f, slim, 1e-4f, "full again within a second");
            // slim, but still a band: it does not vanish into a thread
            Assert.That(VeilRenderer.TurnWidth, Is.InRange(0.4f, 0.7f));
            Assert.That(VeilRenderer.TurnLayers, Is.InRange(0f, 0.5f), "the layers close up on it");
            Assert.That(VeilRenderer.TurnLayerAlpha, Is.InRange(0f, 0.4f), "and fade into it");
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
            // (+0.15: in the quick 0.6 s turn the band swings faster and the tongues trail it a few cm more)
            Assert.LessOrEqual(worst, VeilStrands.MaxStray + 0.15f, "never far from its place by the band (they hung under it like a comb)");
        }

        [Test]
        public void TheTurn_RunsDownTheScarf_FromTheNeck_OverTheTop()
        {
            const float step = 1f / 240f;
            var neck = new Vector3(0f, 5f, 0f);
            var sim = new VeilSimulation(new VeilSettings(), neck) { DriftDirection = -1f, RestShapeWeight = 1f };
            for (int i = 0; i < 120; i++) sim.Step(step, neck);
            Assert.AreEqual(Mathf.PI, sim.HeadingAt(0f), 1e-3f, "streaming to the left");
            Assert.AreEqual(-1f, sim.DriftAt(1f), 1e-3f);

            sim.DriftDirection = 1f;
            float t = 0f, highest = 0f;
            for (; t < VeilSimulation.TurnBlend - 1e-4f; t += step)
            {
                sim.Step(step, neck);
                highest = Mathf.Max(highest, Mathf.Sin(sim.HeadingAt(0f)));
            }
            Assert.Greater(highest, 0.99f, "the cloth by the neck goes up over the top");
            for (; t < 0.3f - 1e-4f; t += step) sim.Step(step, neck);
            float last = -1f;
            for (int i = 10; i >= 0; i--)
            {
                float d = sim.DriftAt(i / 10f);
                Assert.GreaterOrEqual(d, last - 1e-4f, "one wave from the neck to the tip");
                last = d;
            }
            // as the fold reaches the tip, the cloth by the neck has turned and the tip is not yet moving
            for (; t < VeilSimulation.TurnTravel - 1e-4f; t += step) sim.Step(step, neck);
            Assert.AreEqual(0f, sim.HeadingAt(0f), 1e-3f, "the cloth by the neck swings first");
            Assert.AreEqual(Mathf.PI, sim.HeadingAt(1f), 0.03f, "the tail still streams the old way");
            Assert.AreEqual(-1f, sim.DriftAt(1f), 0.03f);
            for (; t < VeilSimulation.TurnTravel + VeilSimulation.TurnBlend + 0.02f; t += step) sim.Step(step, neck);
            Assert.AreEqual(1f, sim.DriftAt(1f), 1e-3f, "and it is over in well under a second");
            Assert.AreEqual(0f, sim.HeadingAt(1f), 1e-3f);
            // (a fold running down it, a part of the scarf in it at a time: all of it swung over at once like the hand
            // of a clock, its tip flew at over 30 m/s and the cloth broke up)
            Assert.Less(VeilSimulation.TurnBlend, 0.6f * VeilSimulation.TurnTravel);
            // (at 0.9 s the scarf answered his turning round unnaturally late)
            Assert.LessOrEqual(VeilSimulation.TurnTravel + VeilSimulation.TurnBlend, 0.65f);

            // the sheet is laid out piece by piece; a piece keeps its length all the way round, its waves laid down mid-turn
            float len = Mathf.Sqrt(0.2f * 0.2f + 0.1f * 0.1f);
            Vector2 up = VeilSimulation.SheetPiece(0.2f, 0.1f, Mathf.PI * 0.5f, 0f);
            Assert.AreEqual(0f, up.x, 1e-4f);
            Assert.AreEqual(len, up.y, 1e-4f, "straight up over his head at full length");
            Vector2 right = VeilSimulation.SheetPiece(0.2f, 0.1f, 0f, 1f), left = VeilSimulation.SheetPiece(0.2f, 0.1f, Mathf.PI, -1f);
            Assert.AreEqual(0.2f, right.x, 1e-4f, "at rest: its run back");
            Assert.AreEqual(0.1f, right.y, 1e-4f, "and its rise");
            Assert.AreEqual(right.y, left.y, 1e-4f, "its S rises the same on either side (mirrored, not upside down)");
            Assert.AreEqual(-right.x, left.x, 1e-4f);
            for (int k = 0; k <= 8; k++)
                Assert.AreEqual(len, VeilSimulation.SheetPiece(0.2f, 0.1f, Mathf.PI * k / 8f, 1f - k / 4f).magnitude, 1e-4f, "never stretched or squeezed");
            Assert.Greater(VeilSimulation.SheetPiece(2f, 0f, VeilSimulation.FlowGoal(1f, 0.8f), 1f).y, 1f, "tilted up into a fall");
            Assert.Greater(VeilSimulation.SheetPiece(2f, 0f, VeilSimulation.FlowGoal(-1f, 0.8f), -1f).y, 1f, "on either side");
            // the part still to come over lies above the fold: its waves are calmed (they hung across the part
            // already turned and the scarf read as a figure of eight)
            Assert.AreEqual(1f, VeilSimulation.FoldCalm(1f), 1e-5f, "on the neck's side: all of its S");
            Assert.AreEqual(VeilSimulation.CalmS, VeilSimulation.FoldCalm(-1f), 1e-5f, "on the other: calmed");
            Assert.That(VeilSimulation.FoldCalm(0f), Is.InRange(VeilSimulation.CalmS + 0.1f, 0.9f), "eased from one to the other");
        }

        [Test]
        public void TurningRound_TheScarfSwingsOver_WithoutBunchingAtTheHead()
        {
            // standing still, sneaking and running; crouched, the neck low over the floor
            foreach (float speed in new[] { 0f, 1.5f, 3f, 6f })
            {
                var sim = new VeilSimulation(new VeilSettings(), new Vector3(0f, 0.8f, 0f)) { DriftDirection = -1f, AmbientFlow = new Vector2(-0.75f, 0f), RestShapeWeight = 1f };
                sim.Obstacles.Add(Rect.MinMaxRect(-100f, -5f, 100f, 0f));
                Vector3 neck = new Vector3(0f, 0.8f, 0f);
                float v = speed;
                for (int f = 0; f < 180; f++) { neck.x += v * Dt; sim.Step(Dt, neck); }
                sim.DriftDirection = 1f;
                sim.AmbientFlow = new Vector2(0.75f, 0f);
                int mostAtTheHead = 0;
                float biggestJump = 0f;
                var last = new Vector3[sim.Count];
                for (int i = 0; i < sim.Count; i++) last[i] = sim.Positions[i] - neck;
                for (int f = 0; f < 110; f++)
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
                Assert.LessOrEqual(mostAtTheHead, 10, $"at {speed} m/s it swings over without crumpling into a ball round his head");
                // (the 0.6 s turn sweeps the tail round at ~0.39 m a frame: fast but continuous; flipped, it leapt 0.4-0.5 m)
                Assert.Less(biggestJump, 0.42f, $"at {speed} m/s no part of it leaps across in a frame");
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
        public void OverALedge_TheShapeLetsGo_AndTheClothDrapes()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            sim.Obstacles.Add(Rect.MinMaxRect(0f, -2f, 4f, 0f));
            Assert.AreEqual(0f, sim.DepthInObstacles(new Vector3(-0.5f, -1f, 0f)), 1e-5f, "outside");
            Assert.AreEqual(0.1f, sim.DepthInObstacles(new Vector3(1f, -0.1f, 0f)), 1e-5f, "just under the top");
            Assert.AreEqual(1f, VeilSimulation.ShapeOutsideWalls(0f), 1e-5f, "in the open the S holds");
            Assert.AreEqual(0f, VeilSimulation.ShapeOutsideWalls(VeilSimulation.ShapeWallFade), 1e-5f, "its place deep in the ledge: let go");
            Assert.Greater(VeilSimulation.ShapeOutsideWalls(0.1f), 0f, "eased, not cut");

            // its corners are rounded for the cloth: dragged round the edge it slides along a curve
            var box = Rect.MinMaxRect(0f, -2f, 4f, 0f);
            Assert.IsFalse(VeilSimulation.RoundedCorner(box, new Vector2(0.02f, -0.02f), VeilSimulation.CornerRadius, out _, out bool cut), "the very corner is cut off: free");
            Assert.IsTrue(cut);
            Assert.IsTrue(VeilSimulation.RoundedCorner(box, new Vector2(0.15f, -0.15f), VeilSimulation.CornerRadius, out Vector2 onCurve, out _), "inside the rounding");
            Assert.AreEqual(VeilSimulation.CornerRadius, Vector2.Distance(onCurve, new Vector2(VeilSimulation.CornerRadius, -VeilSimulation.CornerRadius)), 0.01f, "put out onto the curve");
            Assert.IsFalse(VeilSimulation.RoundedCorner(box, new Vector2(2f, -0.1f), VeilSimulation.CornerRadius, out _, out bool side));
            Assert.IsFalse(side, "along the top it is a plain floor");
            var inCut = sim.OutOfObstacles(new Vector3(0.03f, -0.03f, 0f), 0f);
            Assert.AreEqual(0.03f, inCut.x, 1e-5f, "nothing drawn in the cut-off corner is pushed square out of it");

            // he walks off the end of a ledge with the scarf streaming back over it and drops: none of it stays
            // pressed onto the top while he falls away below
            var fall = new VeilSimulation(new VeilSettings(), new Vector3(0.3f, 1.4f, 0f)) { DriftDirection = 1f, AmbientFlow = new Vector2(0.75f, 0f), RestShapeWeight = 1f };
            fall.Obstacles.Add(Rect.MinMaxRect(0f, -10f, 8f, 0f));
            Vector3 neck = new Vector3(0.3f, 1.4f, 0f);
            for (int f = 0; f < 120; f++) { neck.x -= 1.5f * Dt; fall.Step(Dt, neck); }
            float vy = 0f;
            int stuck = 0;
            for (int f = 0; f < 60; f++)
            {
                vy = Mathf.Max(vy - 60f * Dt, -18f);
                neck += new Vector3(-1.5f, vy, 0f) * Dt;
                fall.Step(Dt, neck);
                fall.TrailTilt = Mathf.MoveTowards(fall.TrailTilt, VeilController.TrailTiltFor(fall.AnchorVelocity), Dt * VeilController.TrailTiltRate);
                if (neck.y < -1.5f)
                    for (int i = 0; i < fall.Count; i++)
                        if (fall.Positions[i].y < 0.1f && fall.Positions[i].x > 0.05f) { stuck++; break; }
            }
            Assert.LessOrEqual(stuck, 3, "the scarf comes off the ledge after him (it lay crumpled on the top)");
        }

        [Test]
        public void TurningRound_TheScarfTakesTheNearerWay_OverTheTopWhenItLiesLevel()
        {
            float pi = Mathf.PI;
            // level: straight across - over the top (underneath only where it is sent that way)
            Assert.AreEqual(0f, VeilSimulation.TurnRoute(pi, 0f, 1), 1e-4f, "from the left round to the right over the top");
            Assert.Greater(Mathf.Sin(Mathf.Lerp(pi, VeilSimulation.TurnRoute(pi, 0f, 1), 0.5f)), 0.99f, "halfway: straight up");
            Assert.Less(Mathf.Sin(Mathf.Lerp(pi, VeilSimulation.TurnRoute(pi, 0f, -1), 0.5f)), -0.99f, "sent underneath: halfway straight down");
            Assert.Greater(Mathf.Sin(Mathf.Lerp(0f, VeilSimulation.TurnRoute(0f, pi, 1), 0.5f)), 0.99f, "the other way round too");
            Assert.Less(Mathf.Sin(Mathf.Lerp(0f, VeilSimulation.TurnRoute(0f, pi, -1), 0.5f)), -0.99f);
            // falling, the sheet is tilted up on both sides: over the top is a short turn, underneath nearly a full
            // circle (taken that way, the scarf closed into a ring over his head)
            float tilt = 1f;
            float from = VeilSimulation.FlowGoal(-1f, tilt), to = VeilSimulation.TurnRoute(from, VeilSimulation.FlowGoal(1f, tilt), -1);
            Assert.AreEqual(pi - 2f * tilt, Mathf.Abs(to - from), 1e-4f, "the nearer way, whatever way was asked for");
            Assert.Greater(Mathf.Sin(0.5f * (from + to)), 0.99f, "over the top");
            // ...unless it has to go underneath (a ceiling right over him)
            float forced = VeilSimulation.TurnRoute(from, VeilSimulation.FlowGoal(1f, tilt), -1, true);
            Assert.AreEqual(pi + 2f * tilt, Mathf.Abs(forced - from), 1e-4f);
            // turning back mid-swing: the short way from where it is
            Assert.AreEqual(2f * pi, VeilSimulation.TurnRoute(1.5f * pi, 0f, 1), 1e-4f);
            Assert.AreEqual(pi, VeilSimulation.TurnRoute(0.5f * pi, pi, -1), 1e-4f);
            Assert.AreEqual(0.3f, VeilSimulation.TurnRoute(0.3f, 0.3f, 1), 1e-6f, "nowhere to turn to");
        }

        [Test]
        public void ATurnInMidAir_ThenAFall_LeavesNoRingInTheScarf()
        {
            // He turns round as he leaps and is falling a moment later: the scarf's direction runs down it from the
            // neck as one motion, so from the neck to the tip it never turns by more than the neck itself did.
            var sim = new VeilSimulation(new VeilSettings(), new Vector3(0f, 5f, 0f)) { DriftDirection = -1f, RestShapeWeight = 1f };
            var neck = new Vector3(0f, 5f, 0f);
            for (int i = 0; i < 120; i++) sim.Step(Dt, neck);
            sim.DriftDirection = 1f;
            float worst = 0f;
            for (int i = 0; i < 90; i++)
            {
                sim.TrailTilt = Mathf.MoveTowards(sim.TrailTilt, i > 6 ? 1.1f : 0f, Dt * 10f);
                sim.Step(Dt, neck);
                float lowest = float.MaxValue, highest = float.MinValue;
                for (int k = 0; k <= 20; k++)
                {
                    float h = sim.HeadingAt(k / 20f);
                    lowest = Mathf.Min(lowest, h);
                    highest = Mathf.Max(highest, h);
                }
                worst = Mathf.Max(worst, highest - lowest);
            }
            Assert.Less(worst, Mathf.PI + 0.05f, "the scarf turns by half a circle at most from neck to tip");
            Assert.AreEqual(1.1f, sim.HeadingAt(1f), 0.02f, "and ends streaming up behind him on the new side");
            Assert.AreEqual(1f, sim.DriftAt(1f), 1e-3f);
        }

        [Test]
        public void InAFall_TheTailStaysWithTheShape_AndTheWhorlsOpen()
        {
            Assert.AreEqual(1f, VeilSimulation.FallDrag(0f), 1e-5f, "standing by a wall the cloth beyond the touch still drapes limply down it");
            Assert.AreEqual(0f, VeilSimulation.FallDrag(0.6f), 1e-5f, "falling past a ledge it keeps its shape (limp, the tail whipped round in front of him)");
            Assert.AreEqual(1f, VeilRenderer.UnrollInTilt(0f), 1e-5f);
            Assert.Less(VeilRenderer.UnrollInTilt(0.9f), 0.25f, "streaming up, the whorls open into tongues (wound up, half curled forward over his head)");
            Assert.AreEqual(1f, VeilRenderer.KeepOutInTilt(0f), 1e-5f);
            Assert.AreEqual(0f, VeilRenderer.KeepOutInTilt(0.6f), 1e-5f, "falling past a ledge the drawn band is not laid flat on its top in steps");
        }

        [Test]
        public void PastABlock_TheLineIsLaidAlongItsFace_AndTheTailIsBraked()
        {
            // a wall to his right: a piece of the scarf's line heading into it is turned along its face - at its own
            // length, and upwards (the cloth streams up a wall like a flame)
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            sim.Obstacles.Add(Rect.MinMaxRect(1f, -5f, 3f, 5f));
            Vector2 from = new Vector2(0.9f, 0f), into = new Vector2(0.1f, 0.02f);
            Vector2 open = sim.LayPiece(Vector3.zero, new Vector2(0.4f, 0f), into);
            Assert.AreEqual(0.5f, open.x, 1e-5f, "nothing in its way: laid straight on");
            Assert.AreEqual(0.02f, open.y, 1e-5f);
            Vector2 laid = sim.LayPiece(Vector3.zero, from, into);
            Assert.AreEqual(into.magnitude, (laid - from).magnitude, 1e-4f, "the piece keeps its length");
            Assert.LessOrEqual(laid.x, 1f - VeilSimulation.WallSkin + 1e-3f, "it stays out of the wall");
            Assert.Greater(laid.y - from.y, 0.05f, "and runs up its face");
            Vector2 next = sim.LayPiece(Vector3.zero, laid, new Vector2(0.1f, -0.05f));
            Assert.Greater(next.y, laid.y, "the next piece keeps to that side (turned each to its own nearest side, the line zigzagged)");
            var down = new VeilSimulation(new VeilSettings(), Vector3.zero);
            down.Obstacles.Add(Rect.MinMaxRect(1f, -5f, 3f, 5f));
            Assert.Greater(down.LayPiece(Vector3.zero, from, new Vector2(0.1f, -0.02f)).y, from.y, "rather up a wall than down it");
            Assert.Greater(VeilSimulation.ForwardBrake, 5f);

            // he steps slowly off a ledge, the scarf streaming back over it: falling, none of it flies out in front of him
            var fall = new VeilSimulation(new VeilSettings(), new Vector3(-0.2f, 1.4f, 0f)) { DriftDirection = 1f, AmbientFlow = new Vector2(0.75f, 0f), RestShapeWeight = 1f };
            fall.Obstacles.Add(Rect.MinMaxRect(0f, -10f, 8f, 0f));
            Vector3 neck = new Vector3(-0.2f, 1.4f, 0f);
            for (int f = 0; f < 120; f++) fall.Step(Dt, neck);
            float vy = 0f, ahead = 0f;
            for (int f = 0; f < 60; f++)
            {
                vy = Mathf.Max(vy - 60f * Dt, -18f);
                neck += new Vector3(-1f, vy, 0f) * Dt;
                fall.Step(Dt, neck);
                fall.TrailTilt = Mathf.MoveTowards(fall.TrailTilt, VeilController.TrailTiltFor(fall.AnchorVelocity), Dt * VeilController.TrailTiltRate);
                for (int i = 0; i < fall.Count; i++) ahead = Mathf.Max(ahead, neck.x - fall.Positions[i].x);
            }
            Assert.Less(ahead, 0.5f, "the tail is not slung out in front of him off the corner (it flew a metre and more)");
        }

        [Test]
        public void NoStrandIsDrawnThinnerThanAPixel()
        {
            // edge-on: it turns a little back towards the camera, at full colour
            float cs = 0.01f, hw = 0.1f, alpha = 1f;
            VeilRenderer.VisibleHalf(ref cs, ref hw, ref alpha, 0.01f);
            Assert.GreaterOrEqual(Mathf.Abs(cs) * hw, 0.01f - 1e-6f, "a strand turned edge-on still shows a pixel (it broke up into dots)");
            Assert.AreEqual(1f, alpha, 1e-6f);
            Assert.AreEqual(0.1f, hw, 1e-6f, "its width is not changed");
            // a hair-thin tip: drawn a pixel wide but as much fainter
            cs = 1f; hw = 0.0025f; alpha = 0.8f;
            VeilRenderer.VisibleHalf(ref cs, ref hw, ref alpha, 0.01f);
            Assert.AreEqual(0.01f, hw, 1e-6f);
            Assert.AreEqual(0.2f, alpha, 1e-5f, "as much colour, spread over the pixel");
            // broad cloth facing the camera is left alone
            cs = -0.9f; hw = 0.2f; alpha = 1f;
            VeilRenderer.VisibleHalf(ref cs, ref hw, ref alpha, 0.01f);
            Assert.AreEqual(-0.9f, cs, 1e-6f);
            Assert.AreEqual(0.2f, hw, 1e-6f);

            var go = new GameObject("cam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                float near = VeilRenderer.PixelSize(cam, new Vector3(0f, 0f, 5f)), far = VeilRenderer.PixelSize(cam, new Vector3(0f, 0f, 20f));
                Assert.Greater(near, 0f);
                Assert.AreEqual(4f, far / near, 1e-3f, "a pixel covers more of the world farther away");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SeenFromAboveOrBehind_TheScarfHasVolume_AndWeaponsAreItsCloth()
        {
            int n = VeilRenderer.Strands.Length;
            Assert.AreEqual(0f, VeilStrands.DepthOf(VeilRenderer.MainStrand, n), 1e-6f, "the band itself stays where it was");
            Assert.AreEqual(VeilStrands.DepthFan, VeilStrands.DepthOf(n - 1, n), 1e-5f, "the last tongue fans out farthest");
            Assert.GreaterOrEqual(VeilStrands.DepthFan, 0.3f, "a flame's volume, not a sheet 0.2 m thick (from above it was a thin line)");
            float last = -1f;
            int towards = 0, away = 0;
            for (int s = VeilRenderer.MainStrand + 1; s < n; s++)
            {
                float d = VeilStrands.DepthOf(s, n);
                Assert.Greater(Mathf.Abs(d), last, "fanned out in order");
                last = Mathf.Abs(d);
                if (d < 0f) towards++; else away++;
            }
            Assert.Greater(towards, 1, "to both sides of the band: from the front it opens out (all behind it, it was a thin fin)");
            Assert.Greater(away, 1);
            Assert.That(VeilRenderer.BandRoll, Is.InRange(0.15f, 0.45f), "the band turned a little out of the picture: from the front more than an edge, from the side nearly all its width");
            Assert.Greater(VeilRenderer.DepthRoll, 0.2f, "the fanned tongues turn their faces up and out");
            Assert.Less(VeilRenderer.DepthRoll, 0.8f, "but from the side they keep most of their width");
            Assert.Less(VeilRenderer.WeaponTintContrast, 0.75f, "a weapon is the scarf's cloth set solid, not a scribble of bright and dark lines");
            Assert.LessOrEqual(VeilRenderer.ShieldLickReach, 0.2f, "the shield burns as a band, not a ring of spikes");
            // the weapon's flame tongues curl a little at their ends, as the scarf's whorls in the key art - a detail:
            // the blade's core keeps its short straight tongue and the weapon its shape
            Assert.That(VeilRenderer.WeaponTongueTurns, Is.InRange(0.2f, 0.45f), "a hook at the end, not a hoop back onto the blade");
            Assert.AreEqual(0.22f, VeilRenderer.FlameTongueLength(true), 1e-6f, "the core's tongue unchanged");
            Assert.LessOrEqual(VeilRenderer.FlameTongueLength(false), 0.45f, "tongues stay short next to the blade");
        }

        [Test]
        public void WithAWallBehindHim_TheScarfRisesUpIt()
        {
            Assert.AreEqual(0f, VeilSimulation.WallTiltFor(3f, 2.5f), 1e-6f, "room to spare: it streams level");
            Assert.Greater(VeilSimulation.WallTiltFor(1.25f, 2.5f), 0.9f, "half the room: it rises steeply");
            Assert.AreEqual(VeilSimulation.MaxWallTilt, VeilSimulation.WallTiltFor(0f, 2.5f), 1e-6f, "his back to the wall: up it like a flame, never over his head");
            Assert.Less(VeilSimulation.MaxWallTilt, Mathf.PI * 0.5f);

            var sim = new VeilSimulation(new VeilSettings(), new Vector3(0f, 1.4f, 0f)) { DriftDirection = 1f };
            sim.Obstacles.Add(Rect.MinMaxRect(0.6f, -1f, 2f, 5f));   // a wall behind him
            sim.Obstacles.Add(Rect.MinMaxRect(-10f, -1f, 10f, 0f));  // the floor
            sim.Obstacles.Add(Rect.MinMaxRect(-3f, 3f, -1f, 4f));    // a ledge over the way ahead: no wall
            Assert.AreEqual(0.6f, sim.RoomBehind(new Vector3(0f, 1.4f, 0f), 1f, 3f), 1e-5f, "the wall behind him");
            Assert.AreEqual(3f, sim.RoomBehind(new Vector3(0f, 1.4f, 0f), -1f, 3f), 1e-5f, "the floor and a ledge overhead are no wall");

            // standing with his back to it, the scarf rises instead of piling up against it
            Vector3 neck = new Vector3(0f, 1.4f, 0f);
            sim.AmbientFlow = new Vector2(0.75f, 0f);
            sim.RestShapeWeight = 1f;
            for (int f = 0; f < 180; f++) sim.Step(Dt, neck);
            Assert.Greater(sim.WallTilt, 1f);
            float top = 0f;
            for (int i = sim.Count / 2; i < sim.Count; i++) top = Mathf.Max(top, sim.Positions[i].y - neck.y);
            Assert.Greater(top, 1f, "its tail is high up the wall, not in a heap behind his head");
        }

        [Test]
        public void InASomersault_TheTonguesFollowTheBandsRing()
        {
            Assert.GreaterOrEqual(VeilRenderer.SwirlFollowBand, 0.5f, "wound round him their own cloth broke into zigzags");
            Assert.LessOrEqual(VeilRenderer.SwirlFollowBand, 1f);
        }

        [Test]
        public void TheLooseScarf_ShedsNoPieces()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Player.prefab");
            var wisps = prefab.GetComponentInChildren<VeilWisps>(true);
            var so = new SerializedObject(wisps);
            Assert.AreEqual(0f, so.FindProperty("baseRate").floatValue, "no drops or threads off the loose scarf: in motion they read as broken pieces of it");
            Assert.AreEqual(0f, so.FindProperty("speedRate").floatValue);
            Assert.Greater(so.FindProperty("weaponRate").floatValue, 0f, "a weapon still smokes (the user likes its flecks)");
            Assert.Greater(so.FindProperty("swingRate").floatValue, 0f, "and a swing leaves its flecks of smoke behind");
        }
            /// <summary>The sharpest bend (degrees) at a node of the loose cloth, in the picture plane.</summary>
        static float SharpestBend(VeilSimulation sim)
        {
            float worst = 0f;
            for (int i = 6; i < sim.Count - 1; i++)
                worst = Mathf.Max(worst, Vector2.Angle(sim.Positions[i] - sim.Positions[i - 1], sim.Positions[i + 1] - sim.Positions[i]));
            return worst;
        }

        static VeilSimulation StreamingRight(Vector3 neck)
        {
            var sim = new VeilSimulation(new VeilSettings(), neck) { DriftDirection = 1f, RestShapeWeight = 1f, AmbientFlow = new Vector2(0.75f, 0f) };
            sim.Obstacles.Add(Rect.MinMaxRect(-10f, -1f, 10f, 0f));   // the floor
            return sim;
        }

        [Test]
        public void TheScarf_StreamsWhereThereIsRoom_UnderABlock_UpAWall()
        {
            var neck = new Vector3(0f, 1.5f, 0f);
            var open = StreamingRight(neck);
            for (int f = 0; f < 120; f++) open.Step(Dt, neck);
            Assert.AreEqual(0f, open.WallTilt, 1e-4f, "nothing in its way: level");

            // a block hangs over its path, its underside just over the height of his neck: the sheet dips under it
            // (laid straight on it was only turned aside at the block itself, and its S was pressed into a Z there)
            var block = Rect.MinMaxRect(1.8f, 1.75f, 6f, 4f);
            var under = StreamingRight(neck);
            under.Obstacles.Add(block);
            float worst = 0f, lowest = 9f, highest = -9f;
            for (int f = 0; f < 240; f++)
            {
                under.Step(Dt, neck);
                if (f < 120) continue;
                worst = Mathf.Max(worst, SharpestBend(under));
                lowest = Mathf.Min(lowest, under.WallTilt);
                highest = Mathf.Max(highest, under.WallTilt);
                for (int i = 0; i < under.Count; i++)
                    Assert.IsFalse(block.Contains((Vector2)under.Positions[i]), "none of it in the block");
            }
            Assert.Less(highest, -0.05f, "it dips");
            Assert.GreaterOrEqual(lowest, -VeilSimulation.MaxDip - 1e-4f, "never far below level");
            Assert.Less(highest - lowest, 0.3f, "and holds its tilt (picked afresh each moment, it leapt up the wall and back with every wave of its S)");
            Assert.LessOrEqual(worst, VeilSimulation.MaxBend + 5f, "one smooth line");
            Assert.AreEqual(under.Settings.length * VeilSimulation.RestReach, under.Positions[under.Count - 1].x, 0.6f, "streaming on under it at its full reach");

            // a wall behind him closer than the scarf is long: it rises up it like a flame
            var wall = Rect.MinMaxRect(1f, 0f, 4f, 9f);
            var risen = StreamingRight(neck);
            risen.Obstacles.Add(wall);
            worst = 0f;
            for (int f = 0; f < 240; f++)
            {
                risen.Step(Dt, neck);
                if (f < 120) continue;
                worst = Mathf.Max(worst, SharpestBend(risen));
                for (int i = 0; i < risen.Count; i++)
                    Assert.IsFalse(wall.Contains((Vector2)risen.Positions[i]), "none of it in the wall");
            }
            Assert.Greater(risen.WallTilt, 0.9f);
            Assert.LessOrEqual(risen.WallTilt, VeilSimulation.MaxFreeTilt + 1e-4f, "never over his head");
            Assert.LessOrEqual(worst, VeilSimulation.MaxBend + 5f);
            Assert.Greater(risen.Positions[risen.Count - 1].y, neck.y + 2f, "its tail high up the wall");

            // the tilt it has is kept while it is clear, and it comes back level a step at a time when the way is free
            Assert.AreEqual(0f, open.FreeTilt(neck, 1f, 0f, 0f, 0f), 1e-6f);
            Assert.AreEqual(0.5f - VeilSimulation.TiltSearch, open.FreeTilt(neck, 1f, 0f, 0f, 0.5f), 1e-5f, "a step back towards level");
            float held = under.WallTilt;
            Assert.AreEqual(held, under.FreeTilt(neck, 1f, 0f, 0f, held), VeilSimulation.TiltSearch + 1e-4f, "no leap from where it is");
            Assert.AreEqual(1.25f, VeilSimulation.SheetTotal(1.25f, 0f), 1e-6f, "falling: its own tilt");
            Assert.AreEqual(VeilSimulation.MaxFreeTilt, VeilSimulation.SheetTotal(1.25f, 1f), 1e-6f, "never past upright");
            Assert.AreEqual(-VeilSimulation.MaxDip, VeilSimulation.SheetTotal(0f, -2f), 1e-6f);
        }

        [Test]
        public void TheScarf_WindsIntoTheSomersaultsRing_AndOutOfIt_AsOneLine()
        {
            // one piece: turned towards the ring's by the share it is wound, its length likewise
            float turn = 0f;
            Vector2 sheet = new Vector2(-0.1f, 0f), ring = new Vector2(0f, 0.08f);
            Assert.AreEqual(0f, Vector2.Distance(sheet, VeilSimulation.WoundStep(sheet, ring, 0f, ref turn)), 1e-5f, "not wound: the sheet's piece");
            turn = 0f;
            Assert.AreEqual(0f, Vector2.Distance(ring, VeilSimulation.WoundStep(sheet, ring, 1f, ref turn)), 1e-5f, "wound: the ring's");
            turn = 0f;
            Vector2 half = VeilSimulation.WoundStep(sheet, ring, 0.5f, ref turn);
            Assert.AreEqual(0.09f, half.magnitude, 1e-5f, "halfway: a piece of a length between the two");
            Assert.AreEqual(135f, Mathf.Atan2(half.y, half.x) * Mathf.Rad2Deg, 0.01f, "turned halfway, the nearer way");
            // the next piece goes on from it: the line winds one way round (no piece takes the other way)
            Vector2 on = VeilSimulation.WoundStep(sheet, new Vector2(0.08f, 0.01f), 0.5f, ref turn);
            Assert.Less(Mathf.DeltaAngle(135f, Mathf.Atan2(on.y, on.x) * Mathf.Rad2Deg), 0f, "on round the same way");
            Assert.Greater(Mathf.DeltaAngle(135f, Mathf.Atan2(on.y, on.x) * Mathf.Rad2Deg), -60f);

            // the whole scarf: wound round him and let go again, it stays one line of its own length
            // (drawn point by point part of the way to the ring, it shrank, looped and knotted in between)
            var neck = new Vector3(0f, 5f, 0f);
            var sim = new VeilSimulation(new VeilSettings(), neck) { DriftDirection = -1f, RestShapeWeight = 1f, AmbientFlow = new Vector2(-0.75f, 0f) };
            for (int f = 0; f < 120; f++) sim.Step(Dt, neck);
            float worst = 0f, shortest = 99f;
            for (int f = 0; f < 60; f++)
            {
                float wound = f < 8 ? Mathf.SmoothStep(0f, 1f, f / 8f) : f < 30 ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (f - 30) / 7f);
                sim.SwirlWeight = wound;
                sim.SwirlCenter = new Vector2(0f, 4.45f);
                sim.WrapTrail = 1f;
                sim.SwirlSpeed = -12f;
                sim.RestShapeWeight = 1f - wound;
                sim.Step(Dt, neck);
                worst = Mathf.Max(worst, SharpestBend(sim));
                float length = 0f;
                for (int i = 1; i < sim.Count; i++) length += Vector3.Distance(sim.Positions[i], sim.Positions[i - 1]);
                shortest = Mathf.Min(shortest, length);
            }
            Assert.LessOrEqual(worst, 45f, "no sharp bend on the way in or out");
            Assert.Greater(shortest, 0.97f * sim.Settings.length, "at its full length throughout");
        }

        [Test]
        public void TheFoldOfATurn_NeedsRoomOverTheWholeScarf()
        {
            var neck = new Vector3(0f, 1.5f, 0f);
            var sim = StreamingRight(neck);
            sim.Obstacles.Add(Rect.MinMaxRect(2f, 2.1f, 3f, 4f));   // a block hanging over the tail, not over his head
            Assert.AreEqual(10f, sim.RoomOver(neck, 1f), 1e-4f, "over his head: free");
            Assert.AreEqual(0.6f, sim.RoomOver(neck, 1f, 3f), 1e-4f, "over the scarf's reach: the block's underside");
            Assert.AreEqual(1.5f, sim.RoomOver(neck, -1f, 3f), 1e-4f, "under him: the floor");
            Assert.Less(VeilSimulation.FoldRoomLeast, VeilSimulation.FoldRoom);
            Assert.That(VeilSimulation.FoldTight, Is.InRange(0.4f, 0.9f), "squeezed, the fold is still a curve the cloth can make");

            // turning round in a low passage: the fold goes over tighter, and nothing is pressed into the ceiling
            var ceiling = Rect.MinMaxRect(-6f, 2.05f, 6f, 5f);
            var low = StreamingRight(neck);
            low.Obstacles.Add(ceiling);
            for (int f = 0; f < 120; f++) low.Step(Dt, neck);
            low.DriftDirection = -1f;
            low.AmbientFlow = new Vector2(-0.75f, 0f);
            float worst = 0f;
            for (int f = 0; f < 90; f++)
            {
                low.Step(Dt, neck);
                worst = Mathf.Max(worst, SharpestBend(low));
                for (int i = 0; i < low.Count; i++)
                    Assert.IsFalse(ceiling.Contains((Vector2)low.Positions[i]), "none of it in the ceiling");
            }
            // (a fold of the usual size does not fit: pressed against the ceiling it doubled over, bent by 120 degrees)
            Assert.LessOrEqual(worst, 70f, "it comes round under the ceiling without doubling over");
            Assert.Less(low.Positions[low.Count - 1].x, -1.5f, "and streams the new way");
        }
    }
}
