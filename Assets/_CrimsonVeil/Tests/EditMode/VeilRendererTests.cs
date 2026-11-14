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
            Assert.Less(VeilRenderer.ScarfWidth(3.2f), VeilRenderer.ScarfWidth(2f), "narrows into the whorls");
        }

        [Test]
        public void LooseScarf_TwistsShowingItsDarkReverse()
        {
            float minW = 1f, minShade = 1f, maxShade = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                float tw = VeilRenderer.Twist(t, 0f);
                minW = Mathf.Min(minW, VeilRenderer.TwistWidth(tw));
                float sh = VeilRenderer.TwistShade(tw);
                minShade = Mathf.Min(minShade, sh); maxShade = Mathf.Max(maxShade, sh);
            }
            Assert.Less(minW, 0.9f, "narrows a little where it turns");
            Assert.AreEqual(1f, maxShade, 1e-3f, "the bright crimson face");
            Assert.Less(minShade, 0.75f, "and the deeper wine reverse");
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
        public void LooseEnds_RollDownAndUnder_IntoTightSpirals()
        {
            var main = VeilRenderer.Strands[1];
            // weapon tongues flick with curlSign; the loose ends roll the other way (key art: down and under)
            Assert.AreEqual(-Mathf.Sign(main.curlSign), Mathf.Sign(VeilRenderer.LooseCurl(main)));
            Assert.Greater(Mathf.Abs(VeilRenderer.LooseCurl(main)), 0.8f, "the main mass winds into a whole whorl (the sheet)");
        }

        [Test]
        public void RestShape_IsTheSheetsLevelS_AndLoops()
        {
            // ArtSource/ref/scarf_anim_sheet.png: down ~0.4 m at 1.2 m, back up near the neck's line at 2 m, down again
            for (float time = 0f; time < VeilRestShape.Duration; time += 0.2f)
            {
                float trough = VeilRestShape.Height(1.2f, time), crest = VeilRestShape.Height(2f, time);
                Assert.That(trough, Is.InRange(-0.5f, -0.25f), "the first dip");
                Assert.Greater(crest - trough, 0.15f, "an S, not a straight streamer");
                Assert.Less(VeilRenderer.ScarfWidth(0f), 0.2f);
            }
            for (float d = 0f; d <= VeilRestShape.Reach; d += 0.4f)
                Assert.AreEqual(VeilRestShape.Height(d, 0.05f), VeilRestShape.Height(d, 0.05f + VeilRestShape.Duration), 1e-4f, "a seamless loop");
            Assert.AreEqual(12, VeilRestShape.Frames, "the sheet's 12 frames");
        }

        [Test]
        public void ScarfColours_AreTheSheetsPureCrimson_NeverOrangeOrGrey()
        {
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/Veil.mat");
            Assert.AreEqual(1f, m.GetFloat("_Cloth"), "the scarf is painted cloth");
            foreach (var name in new[] { "_ClothDeep", "_ClothMid", "_ClothLight" })
            {
                var c = m.GetColor(name);
                Assert.Less(Mathf.Max(c.g, c.b), 0.06f, name + ": pure red ink (the sheet), no orange or pink cast");
                Assert.Greater(c.r, 0.3f, name);
            }
            Assert.Greater(m.GetColor("_ClothLight").r, m.GetColor("_ClothMid").r - 1e-3f);
            Assert.Less(m.GetColor("_ClothDeep").r, m.GetColor("_ClothMid").r);
        }

        [Test]
        public void Scarf_HangsFromTheNeck_NotCentredOnIt()
        {
            // Centred on the neck, half of the broad band (and its upper layers) rose over the head when running.
            Assert.Greater(VeilRenderer.HangBelowNeck, 0.15f);
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
        public void StoppingHero_ScarfIsFoldedBackNotSweptInFront()
        {
            Assert.AreEqual(0f, VeilRenderer.FoldBack(0f), 1e-5f, "continuous at the chest line");
            Assert.Less(VeilRenderer.FoldBack(2f), 0.11f, "however far it swings, it stays within a hand's breadth");
            Assert.Greater(VeilRenderer.FoldBack(0.2f), VeilRenderer.FoldBack(0.05f), "folds smoothly, no hard cut");
            Assert.Less(VeilRenderer.FrontLimit, 0.25f);
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
