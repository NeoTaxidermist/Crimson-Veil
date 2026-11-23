using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class VeilRendererTests
    {
        [Test]
        public void Tendrils_StartOnTheMainRibbon_SoTheSwirlStaysConnected()
        {
            foreach (var st in VeilRenderer.Strands)
            {
                if (st.start <= 0f) continue;
                Assert.AreEqual(0f, VeilRenderer.BranchOffset(0f, st.side, 0.3f), 1e-5f, "tendril must branch from the ribbon");
                Assert.Greater(Mathf.Abs(VeilRenderer.BranchOffset(1f, st.side, 0.3f)), 0.01f, "tendril drifts apart towards its end");
            }
        }

        [Test]
        public void Scarf_HasSeveralEnds_EachEndingInACurl()
        {
            int ends = 0, sameWay = 0;
            foreach (var st in VeilRenderer.Strands)
            {
                ends++;
                Assert.Greater(st.curlTurns, 0.5f, "every end winds into a curl");
                Assert.LessOrEqual(st.curlTurns, 1.1f, "a loose swirl, not a tight ring (rings read as octopus tentacles)");
                Assert.Greater(st.curlLength, 0.2f);
                if (st.curlSign == VeilRenderer.Strands[1].curlSign) sameWay++;
            }
            Assert.GreaterOrEqual(ends, 5, "the veil splits into many ends like the key art");
            Assert.GreaterOrEqual(sameWay, ends / 2, "most ends roll down and under into the lower whorl, the upper tongues into the upper one (the sheet)");
        }

        [Test]
        public void Scarf_HasTheSheetsProportions()
        {
            const float heroHeight = 2f; // Hero.fbx with hair
            float length = new VeilSettings().length;
            float width = VeilFormLibrary.Width(VeilForm.Flow);
            Assert.That(length, Is.InRange(heroHeight * 1.4f, heroHeight * 1.8f), "as long as in its animation sheet (~3.2 m behind a 1.9 m hero)");
            Assert.GreaterOrEqual(width, heroHeight * 0.3f, "a robust band, about a third of the hero's height");
        }

        [Test]
        public void LooseScarf_HasTheSheetsWidths()
        {
            // measured on ArtSource/ref/scarf_anim_sheet.png (hero 170 px = 1.9 m)
            Assert.That(VeilRenderer.ScarfWidth(0f), Is.InRange(0.12f, 0.2f), "slim where it leaves the neck");
            Assert.That(VeilRenderer.ScarfWidth(1f), Is.InRange(0.28f, 0.36f));
            Assert.That(VeilRenderer.ScarfWidth(2f), Is.InRange(0.34f, 0.42f), "widest along the middle");
            Assert.Less(VeilRenderer.ScarfWidth(3.2f), VeilRenderer.ScarfWidth(2f), "narrowing as its tongues take over (the key art)");
            foreach (var st in VeilRenderer.Strands)
                Assert.GreaterOrEqual(VeilRenderer.LooseEnd(st), 0.9f, "every loose layer runs on into the whorls at the end");
        }

        [Test]
        public void LooseScarf_SwaysRoundItsLength_ItsBrightFaceAlwaysToTheCamera()
        {
            // The ribbon turns in depth for real (its width across the picture follows |cos| of the twist), but as in the
            // key art only sways: it narrows where it leans away and never turns over into a dark knot.
            Vector3 nrm = Vector3.down;
            float minW = 1f, maxDepth = 0f;
            bool leansOneWay = false, leansTheOther = false;
            for (float time = 0f; time < 5f; time += 0.25f)
                for (float t = 0f; t <= 1f; t += 0.01f)
                {
                    float tw = VeilRenderer.Twist(t, time);
                    var side = VeilRenderer.RibbonSide(nrm, tw);
                    var face = VeilRenderer.RibbonFace(nrm, tw);
                    Assert.AreEqual(1f, side.magnitude, 1e-4f, "the cloth keeps its width, only turned");
                    Assert.AreEqual(0f, Vector3.Dot(side, face), 1e-4f, "the face is square to the width");
                    Assert.Less(face.z, -0.5f, "its bright face towards the camera (no dark reverse turned out)");
                    minW = Mathf.Min(minW, new Vector2(side.x, side.y).magnitude);
                    maxDepth = Mathf.Max(maxDepth, Mathf.Abs(side.z));
                    if (side.z > 0.3f) leansOneWay = true;
                    if (side.z < -0.3f) leansTheOther = true;
                }
            Assert.That(minW, Is.InRange(0.5f, 0.7f), "narrowing where it leans away, never to a pinch");
            Assert.Greater(maxDepth, 0.7f, "leaning into depth");
            Assert.IsTrue(leansOneWay && leansTheOther, "swaying both ways");
            Assert.AreEqual(0f, VeilRenderer.Twist(0f, 2f), 1e-6f, "flat where it leaves the neck");
            Assert.AreEqual(-1f, VeilRenderer.RibbonFace(nrm, 0f).z, 1e-5f, "untwisted it faces the camera");
        }

        [Test]
        public void TheCloth_BowsOutAcrossItsMiddle_ItsNormalsFollowingTheBow()
        {
            // (a cord's bow, as a set weapon's: the loose cloth bows only a little - more read as rubber)
            Vector3 left = new Vector3(0f, 0.1f, 0f), right = new Vector3(0f, -0.1f, 0f), face = Vector3.back;
            const float cup = 0.24f;
            Assert.That(VeilRenderer.CupDepth, Is.InRange(0.05f, 0.15f), "the loose cloth bows a little");
            VeilRenderer.CupSection(left, right, face, cup, out var c, out var nl, out var nr);
            Assert.AreEqual(0f, c.y, 1e-5f, "in the middle");
            Assert.AreEqual(-0.1f * cup, c.z, 1e-5f, "bowing out towards its face");
            Assert.Greater(nl.y, 0.2f, "the edge's normal leans out over its edge");
            Assert.Less(nr.y, -0.2f);
            Assert.AreEqual(1f, nl.magnitude, 1e-4f);
            // edge-on (face across the picture) it still shows the depth of its bow
            VeilRenderer.CupSection(new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, -0.1f), Vector3.up, cup, out c, out _, out _);
            Assert.Greater(c.y, 0.02f, "edge-on, not a hairline");
            VeilRenderer.CupSection(left, right, face, 0f, out c, out nl, out _);
            Assert.AreEqual(Vector3.zero, c, "a weapon stays flat (for now)");
            Assert.AreEqual(face, nl);
        }

        [Test]
        public void TheCloth_IsPaintedBackToFront_StrandsInFrontOfTheBandCoverIt()
        {
            // three pieces of two vertices each: the band (z 0), a strand wound round behind it (z 0.3), one in front (z -0.3)
            var verts = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 0.3f), new Vector3(1f, 0f, 0.3f),
                new Vector3(0f, 0f, -0.3f), new Vector3(1f, 0f, -0.3f),
            };
            int[] first = { 0, 2, 4 }, count = { 2, 2, 2 }, order = { 0, 1, 2 };
            var key = new float[3];
            Vector3 eye = new Vector3(0f, 0f, -10f), forward = Vector3.forward;
            VeilRenderer.PaintOrder(verts, first, count, key, order, eye, forward, 1);
            CollectionAssert.AreEqual(new[] { 1, 0, 2 }, order, "behind first, the band, then the strand in front over it");

            // the strand swings round: next frame it is in front of the one that was
            for (int v = 2; v < 4; v++) verts[v].z = -0.5f;
            VeilRenderer.PaintOrder(verts, first, count, key, order, eye, forward, 1);
            CollectionAssert.AreEqual(new[] { 0, 2, 1 }, order);

            // layers lying together keep the drawing's order (a later strand counts a little nearer)
            for (int v = 0; v < verts.Length; v++) verts[v].z = 0f;
            VeilRenderer.PaintOrder(verts, first, count, key, order, eye, forward, 1);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, order);
        }

        [Test]
        public void ATongue_IsDrawnOnEveryOtherSample_NeverLeavingOutItsEnd()
        {
            Assert.IsFalse(VeilRenderer.Skipped(0, 2, 103));
            Assert.IsTrue(VeilRenderer.Skipped(1, 2, 103));
            Assert.IsFalse(VeilRenderer.Skipped(102, 2, 103));
            Assert.IsFalse(VeilRenderer.Skipped(101, 2, 102), "the last sample is always drawn");
            for (int i = 0; i < 103; i++) Assert.IsFalse(VeilRenderer.Skipped(i, 1, 103), "the band: every sample");
        }

        [Test]
        public void LooseScarf_IsClothAtTheNeck_SmokeTonguesOnlyFurtherBack()
        {
            for (int s = 0; s < VeilRenderer.Strands.Length; s++)
            {
                float near = VeilRenderer.LooseLayerAlpha(s, 0.15f), far = VeilRenderer.LooseLayerAlpha(s, 0.8f);
                Assert.AreEqual(1f, far, 1e-4f, "the key art's tongues of smoke at the back");
                if (s > 1) Assert.AreEqual(0f, near, 1e-4f, "near the neck one band of cloth (the concept)");
            }
        }

        [Test]
        public void LooseEnds_HookOverIntoAFlamesTip_NotABallOfWool()
        {
            // an S of whorls: the band and the tongues above it curl up and over, those below roll down and under
            Assert.Less(VeilRenderer.LooseCurl(VeilRenderer.Strands[1]), 0f, "the band curls up and back");
            bool up = false, down = false;
            foreach (var st in VeilRenderer.Strands)
            {
                float c = VeilRenderer.LooseCurl(st);
                if (st.side > 0.05f) Assert.Greater(c, 0f, "below the band: down and under"); else Assert.Less(c, 0f, "above it: up and over");
                up |= c < 0f; down |= c > 0f;
                Assert.That(Mathf.Abs(c), Is.InRange(0.4f, 1.2f), "an open whorl, never wound round and round itself");
            }
            Assert.IsTrue(up && down);
            // of a thin line of smoke, not a broad stroke wound up (that made a ball of wool)
            Assert.Less(VeilRenderer.TipWidth, 0.05f, "narrowing to a thin line");
            Assert.Less(VeilRenderer.SmokeTaper, 0.6f, "within its first stretch");
            Assert.Less(VeilRenderer.TipAlpha, 0.5f, "fading into smoke");
            // a tongue swells out of the band and narrows towards its tip
            Assert.Greater(VeilRenderer.TongueWidth(0.4f), VeilRenderer.TongueWidth(0f) * 1.8f);
            Assert.Less(VeilRenderer.TongueWidth(1f), VeilRenderer.TongueWidth(0.4f) * 0.75f);
        }

        [Test]
        public void AFlamesRipple_RunsBackAlongTheLooseScarf()
        {
            Assert.AreEqual(0f, VeilRenderer.FlameRipple(0f, 1.3f), 1e-6f, "still at the neck");
            float max = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f) max = Mathf.Max(max, Mathf.Abs(VeilRenderer.FlameRipple(t, 0.4f)));
            Assert.That(max, Is.InRange(0.04f, VeilRenderer.RippleHeight + 1e-4f), "a ripple, not a flail");
            // a crest moves back along it (towards the tail) as time goes on
            float crestAt(float time) { float best = -1f, at = 0f; for (float t = 0.1f; t <= 0.75f; t += 0.002f) { float h = VeilRenderer.FlameRipple(t, time); if (h > best) { best = h; at = t; } } return at; }
            Assert.Greater(crestAt(0.15f), crestAt(0f), "travelling from the neck to the tail");
        }

        [Test]
        public void RestShape_IsTheSheetsS_LiftedAsInTheKeyArt_AndLoops()
        {
            // ArtSource/ref/scarf_anim_sheet.png's S (a dip at 1.2 m, a crest at 2 m), lifted off the neck as in the key art
            Assert.AreEqual(0f, VeilRestShape.Lift(0f), 1e-6f, "at the neck");
            for (float time = 0f; time < VeilRestShape.Duration; time += 0.2f)
            {
                float trough = VeilRestShape.Height(1.2f, time), crest = VeilRestShape.Height(2f, time);
                Assert.That(trough, Is.InRange(-0.1f, 0.25f), "its dip no lower than the neck: it flies at the shoulders and above");
                Assert.Greater(crest, 0.15f, "rising above the neck");
                Assert.Greater(crest - trough, 0.15f, "an S, not a straight streamer");
                Assert.Less(VeilRenderer.ScarfWidth(0f), 0.2f);
            }
            for (float d = 0f; d <= VeilRestShape.Reach; d += 0.4f)
                Assert.AreEqual(VeilRestShape.Height(d, 0.05f), VeilRestShape.Height(d, 0.05f + VeilRestShape.Duration), 1e-4f, "a seamless loop");
            Assert.AreEqual(12, VeilRestShape.Frames, "the sheet's 12 frames");
        }

        [Test]
        public void AWeapon_IsTheScarfsOwnCrimson_NotABrighterPinkerRedBesideIt()
        {
            // Measured in the game, the red of the picture's crimson pixels: the scarf (112, 4, 4), the weapons
            // (150, 26, 26) - a third brighter, and pink. A weapon had lower thresholds for the scarf's bright strokes, a
            // heat in its core, a glint a quarter white, and was thin between its streaks (the grey behind showed).
            foreach (var path in new[] { "Assets/_CrimsonVeil/Materials/Veil.mat", "Assets/_CrimsonVeil/Materials/VeilSmoke.mat" })
            {
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsTrue(m.HasProperty("_WeaponTone") && m.HasProperty("_WeaponHeat"), path);
                Assert.AreEqual(1f, m.GetFloat("_WeaponInk"), 1e-4f, path + ": a weapon is all ink - none of the orange flame under it");
                Assert.That(m.GetFloat("_WeaponTone"), Is.InRange(0.6f, 0.85f), path + ": toned to the scarf's layers");
                Assert.LessOrEqual(m.GetFloat("_WeaponHeat"), 0.35f, path + ": only a little heat and white in it");
            }
        }

        [Test]
        public void ScarfColours_AreTheKeyArtsCrimson_NeverOrangeOrGrey()
        {
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/Veil.mat");
            Assert.AreEqual(1f, m.GetFloat("_Cloth"), "the scarf is painted cloth");
            foreach (var name in new[] { "_ClothDeep", "_ClothMid", "_ClothLight" })
            {
                var c = m.GetColor(name);
                Assert.Less(Mathf.Max(c.g, c.b), c.r * 0.16f, name + ": crimson ink, no orange or pink cast");
                Assert.Greater(c.r, 0.2f, name + ": a deep red, never black");
            }
            Assert.Greater(m.GetColor("_ClothLight").r, m.GetColor("_ClothMid").r - 1e-3f);
            Assert.Less(m.GetColor("_ClothDeep").r, m.GetColor("_ClothMid").r);
        }

        [Test]
        public void TheWrapRoundTheNeck_IsNotDrawn_TheScarfGrowsFromTheBackOfTheNeck()
        {
            const float wrapEnd = 3f / 34f;
            Assert.AreEqual(0f, VeilRenderer.FromTheNeck(0f, wrapEnd), 1e-6f, "at the throat: nothing (it doubled back into a blob)");
            Assert.AreEqual(0f, VeilRenderer.FromTheNeck(wrapEnd - VeilRenderer.NeckTuck, wrapEnd), 1e-6f, "starts tucked under the collar");
            Assert.AreEqual(1f, VeilRenderer.FromTheNeck(wrapEnd + VeilRenderer.NeckEmerge, wrapEnd), 1e-6f, "full just past the wrap");
            Assert.Less(VeilRenderer.NeckEmerge, 0.08f, "grows out quickly");
            // gathered narrow where it slips out, opening out as it flies (no thick slab stuck to the neck)
            Assert.AreEqual(VeilRenderer.NeckGatherWidth, VeilRenderer.NeckGather(wrapEnd - VeilRenderer.NeckTuck, wrapEnd), 1e-6f);
            Assert.Less(VeilRenderer.NeckGatherWidth, 0.5f);
            Assert.AreEqual(1f, VeilRenderer.NeckGather(0.4f, wrapEnd), 1e-6f, "its full width further back");
        }

        [Test]
        public void BroadStroke_DoesNotFoldOverItselfInATightTurn()
        {
            // a 0.4 m wide stroke turning 90 degrees within 0.1 m: its inner edge would cross over (a stray triangle)
            var c = new[] { new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0f, 0f), new Vector3(0.1f, 0.1f, 0f) };
            var n = new[] { Vector3.up, new Vector3(-0.7071f, 0.7071f, 0f), Vector3.left };
            var verts = new Vector3[6];
            for (int i = 0; i < 3; i++) { verts[i * 2] = c[i] + n[i] * 0.2f; verts[i * 2 + 1] = c[i] - n[i] * 0.2f; }
            VeilRenderer.ClampFolds(verts, new Vector2[6], 0, 3);
            float half = (verts[2] - verts[3]).magnitude * 0.5f;
            Assert.Less(half, 0.1f, "narrowed to within the radius of the turn");
            var hardData = new Vector2[6];
            for (int i = 0; i < 6; i++) hardData[i] = new Vector2(1f, 0f);
            for (int i = 0; i < 3; i++) { verts[i * 2] = c[i] + n[i] * 0.2f; verts[i * 2 + 1] = c[i] - n[i] * 0.2f; }
            VeilRenderer.ClampFolds(verts, hardData, 0, 3);
            Assert.AreEqual(0.2f, (verts[2] - verts[3]).magnitude * 0.5f, 1e-4f, "a weapon keeps its shape");
        }

        [Test]
        public void Scarf_HangsFromTheNeck_NotCentredOnIt()
        {
            // Centred on the neck, half of the broad band (and its upper layers) rose over the head when running; the key
            // art's scarf flies high, so it hangs only a little.
            Assert.Greater(VeilRenderer.HangBelowNeck, 0.05f);
            Assert.Less(VeilRenderer.HangBelowNeck, 0.5f, "still wraps the neck");
        }

        [Test]
        public void Weapon_IsNotTiedToTheNeck()
        {
            const float hand = 0.3f;
            Assert.AreEqual(0f, VeilRenderer.NeckBandFade(0.1f, hand, 0f), 1e-4f, "loose scarf: all visible");
            Assert.AreEqual(1f, VeilRenderer.NeckBandFade(0.1f, hand, 1f), 1e-4f, "set: the neck → hand band is gone");
            Assert.AreEqual(0f, VeilRenderer.NeckBandFade(0.6f, hand, 1f), 1e-4f, "the weapon itself stays");
            Assert.That(VeilRenderer.NeckBandFade(0.1f, hand, 0.7f), Is.InRange(0.05f, 0.95f), "melts gradually as it sets");
        }

        [Test]
        public void StoppingHero_ScarfStopsAtHisChest_NotSweptInFront()
        {
            // he faces +x; the breeze still streams the loose scarf forward past him (as just after he turned)
            float Run(float hold)
            {
                var sim = new VeilSimulation(new VeilSettings(), Vector3.zero) { DriftDirection = 1f, RestShapeWeight = 1f, FrontFacing = 1f, FrontHold = hold };
                sim.AmbientFlow = new Vector2(0.75f, 0f);
                float most = float.MinValue;
                for (int f = 0; f < 180; f++)
                {
                    sim.Step(1f / 60f, Vector3.zero);
                    for (int i = sim.Settings.wrapNodes + 1; i < sim.Count; i++) most = Mathf.Max(most, sim.Positions[i].x);
                }
                return most;
            }
            Assert.Greater(Run(0f), 1f, "free, it streams out in front");
            Assert.Less(Run(1f), VeilSimulation.FrontReach + 0.05f, "held, the cloth itself stops at his chest line");
            Assert.Less(VeilSimulation.FrontReach, 0.25f);
        }

        [Test]
        public void TheDrawnLine_NeverLoopsBetweenTheClothsNodes()
        {
            // segments of very unequal length: the uniform spline overshoots and turns back; the centripetal one does not
            Vector3 p0 = new Vector3(-1f, 0f, 0f), p1 = Vector3.zero, p2 = new Vector3(0.05f, 0f, 0f), p3 = new Vector3(0.1f, 1f, 0f);
            Vector3 along = p2 - p1;
            float last = -1f;
            bool forward = true;
            for (int k = 0; k <= 20; k++)
            {
                float d = Vector3.Dot(VeilRenderer.CentripetalCatmullRom(p0, p1, p2, p3, k / 20f) - p1, along);
                if (d < last - 1e-6f) forward = false;
                last = d;
            }
            Assert.IsTrue(forward, "it runs on from one node to the next, never back");
            Assert.AreEqual(p1, VeilRenderer.CentripetalCatmullRom(p0, p1, p2, p3, 0f));
            Assert.That(Vector3.Distance(p2, VeilRenderer.CentripetalCatmullRom(p0, p1, p2, p3, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void ATightBend_IsNarrowedSmoothly_NotPinchedAtOnePoint()
        {
            // a broad band along x with one sharp little kink in the middle
            const int n = 21;
            var verts = new Vector3[n * 2];
            for (int k = 0; k < n; k++)
            {
                var c = new Vector3(k * 0.03f, k == 10 ? 0.03f : 0f, 0f);
                verts[k * 2] = c + Vector3.up * 0.15f;
                verts[k * 2 + 1] = c - Vector3.up * 0.15f;
            }
            VeilRenderer.ClampFolds(verts, null, 0, n);
            for (int k = 1; k < n - 1; k++)
            {
                float w = (verts[k * 2] - verts[k * 2 + 1]).magnitude;
                float around = Mathf.Min((verts[k * 2 - 2] - verts[k * 2 - 1]).magnitude, (verts[k * 2 + 2] - verts[k * 2 + 3]).magnitude);
                Assert.Greater(w, 0.7f * around, $"sample {k}: no notch cut into the band");
            }
            // a curl at the very end: the band stays narrow to its tip (it opened out to full width in its last sample)
            for (int k = 0; k < n; k++)
            {
                float ang = k < 14 ? 0f : (k - 14) * 1.2f;
                var c = new Vector3(0.42f + 0.02f * Mathf.Sin(ang), 0.02f * (1f - Mathf.Cos(ang)), 0f) + (k < 14 ? new Vector3((k - 14) * 0.03f, 0f, 0f) : Vector3.zero);
                verts[k * 2] = c + Vector3.up * 0.15f;
                verts[k * 2 + 1] = c - Vector3.up * 0.15f;
            }
            VeilRenderer.ClampFolds(verts, null, 0, n);
            Assert.Less((verts[(n - 1) * 2] - verts[(n - 1) * 2 + 1]).magnitude, 0.1f, "narrow to the tip");
        }

        [Test]
        public void Curl_IsContinuousAndWindsIntoASpiral()
        {
            const int count = 24;
            var pts = new Vector3[count + 1];
            var start = new Vector3(1f, 2f, 0f);
            VeilRenderer.BuildCurl(start, 0f, 1f, 0.6f, 1.5f, count, pts);
            Assert.AreEqual(start, pts[0], "curl continues exactly from the strand end");

            float maxStep = 0f, turned = 0f;
            for (int i = 1; i <= count; i++)
            {
                maxStep = Mathf.Max(maxStep, Vector3.Distance(pts[i], pts[i - 1]));
                if (i >= 2)
                {
                    Vector3 a = pts[i - 1] - pts[i - 2], b = pts[i] - pts[i - 1];
                    turned += Vector2.SignedAngle(a, b);
                }
            }
            Assert.Less(maxStep, 0.6f / count + 1e-4f, "no gaps");
            Assert.Greater(turned, 360f, "winds more than a full turn");
            Assert.Less(Vector3.Distance(pts[0], pts[count]), 0.6f * 0.6f, "it curls up instead of running straight");

            // Mirrored winding turns the other way.
            VeilRenderer.BuildCurl(start, 0f, -1f, 0.6f, 1.5f, count, pts);
            Assert.Less(pts[count / 2].y, start.y);
        }
    }
}
