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
            // (+0.15: in the quick 0.6 s turn the band swings faster and the tongues trail it a few cm more)
            Assert.LessOrEqual(worst, VeilStrands.MaxStray + 0.15f, "never far from its place by the band (they hung under it like a comb)");
        }

        [Test]
        public void TheTurn_RunsDownTheScarf_FromTheNeck_OverTheTop()
        {
            // as the swing reaches the tip, the neck is halfway round - straight up - and the tip not yet moving
            float t = VeilSimulation.TurnTravel;
            float neck = VeilSimulation.SwingHeading(Mathf.PI, 0f, t, 0f), tip = VeilSimulation.SwingHeading(Mathf.PI, 0f, t, 1f);
            Assert.AreEqual(Mathf.PI * 0.5f, neck, 1e-3f, "the cloth by the neck swings first, up over the top");
            Assert.AreEqual(Mathf.PI, tip, 1e-4f, "the tail still streams the old way");
            Assert.AreEqual(-1f, VeilSimulation.DriftAt(-1f, 1f, t, 1f), 1e-4f);
            float last = -1f;
            for (int i = 10; i >= 0; i--)
            {
                float d = VeilSimulation.DriftAt(-1f, 1f, 0.4f, i / 10f);
                Assert.GreaterOrEqual(d, last - 1e-5f, "one wave from the neck to the tip");
                last = d;
            }
            Assert.AreEqual(1f, VeilSimulation.DriftAt(-1f, 1f, VeilSimulation.TurnTravel + VeilSimulation.TurnBlend, 1f), 1e-4f, "and it is over in well under a second");
            // (at 0.9 s the scarf answered his turning round unnaturally late)
            Assert.LessOrEqual(VeilSimulation.TurnTravel + VeilSimulation.TurnBlend, 0.65f);

            // the sheet keeps its length all the way round: up mid-turn, its waves laid down
            Vector2 up = VeilSimulation.SheetOffset(2f, 0.3f, Mathf.PI * 0.5f, 0f);
            Assert.AreEqual(0f, up.x, 1e-4f);
            Assert.AreEqual(2f, up.y, 1e-4f, "straight up over his head at full length");
            Vector2 right = VeilSimulation.SheetOffset(2f, 0.3f, 0f, 0f), left = VeilSimulation.SheetOffset(2f, 0.3f, Mathf.PI, 0f);
            Assert.AreEqual(right.y, left.y, 1e-4f, "its S rises the same on either side (mirrored, not upside down)");
            Assert.AreEqual(-right.x, left.x, 1e-4f);
            Assert.Greater(VeilSimulation.SheetOffset(2f, 0f, 0f, 0.8f).y, 1f, "tilted up into a fall");
            Assert.Greater(VeilSimulation.SheetOffset(2f, 0f, Mathf.PI, 0.8f).y, 1f, "on either side");
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
        public void LeapingUp_TheTurnSwingsUnderneath_FallingOrStanding_OverTheTop()
        {
            float pi = Mathf.PI;
            Assert.AreEqual(0f, VeilSimulation.TurnGoal(pi, 0f, false), 1e-4f, "from the left round to the right over the top");
            Assert.Greater(Mathf.Sin(Mathf.Lerp(pi, VeilSimulation.TurnGoal(pi, 0f, false), 0.5f)), 0.99f, "halfway: straight up");
            Assert.Less(Mathf.Sin(Mathf.Lerp(pi, VeilSimulation.TurnGoal(pi, 0f, true), 0.5f)), -0.99f, "leaping up: halfway straight down, under him");
            Assert.Greater(Mathf.Sin(Mathf.Lerp(0f, VeilSimulation.TurnGoal(0f, pi, false), 0.5f)), 0.99f, "the other way round too");
            Assert.Less(Mathf.Sin(Mathf.Lerp(0f, VeilSimulation.TurnGoal(0f, pi, true), 0.5f)), -0.99f);
            // turning back mid-swing: the short way from where it is
            Assert.AreEqual(2f * pi, VeilSimulation.TurnGoal(1.5f * pi, 0f, false), 1e-4f);
            Assert.AreEqual(pi, VeilSimulation.TurnGoal(0.5f * pi, pi, true), 1e-4f);
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
        public void PastABlock_TheShapeMovesOutSideways_AndTheTailIsBraked()
        {
            var sim = new VeilSimulation(new VeilSettings(), Vector3.zero);
            sim.Obstacles.Add(Rect.MinMaxRect(-3f, -5f, -1f, 5f));
            Vector3 inside = new Vector3(-2.5f, 1f, 0f);
            Vector3 mine = sim.OutSideways(inside, 0f);
            Assert.Greater(mine.x, -1f, "out onto the face on his side");
            Assert.AreEqual(1f, mine.y, 1e-5f, "at the same height: the band runs up along the face");
            Assert.Less(sim.OutSideways(inside, -10f).x, -3f, "from the other side, out the other way");
            Assert.AreEqual(5f, sim.OutSideways(new Vector3(5f, 1f, 0f), 0f).x, 1e-5f, "outside, left alone");
            Vector3 underTop = sim.OutSideways(new Vector3(-2.5f, 4.9f, 0f), 0f);
            Assert.AreEqual(-2.5f, underTop.x, 1e-5f, "just under the top of a block he ran off: out over the top");
            Assert.Greater(underTop.y, 5f, "(drawn into a line down its edge, the cloth on the top folded over the corner)");
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
    }
}
