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
            Assert.GreaterOrEqual(sameWay, ends - 1, "the ends roll the same way: one swirling flame, not separate tentacles");
        }

        [Test]
        public void Scarf_HasKeyArtProportions_RobustNotThinAndLong()
        {
            const float heroHeight = 2f; // Hero.fbx with hair
            float length = new VeilSettings().length;
            float width = VeilFormLibrary.Width(VeilForm.Flow);
            Assert.LessOrEqual(length, heroHeight * 1.4f, "not needlessly long next to the hero");
            Assert.GreaterOrEqual(width, heroHeight * 0.3f, "a robust band, about a third of the hero's height");
        }

        [Test]
        public void Scarf_HangsFromTheNeck_NotCentredOnIt()
        {
            // Centred on the neck, half of the broad band (and its upper layers) rose over the head when running.
            Assert.Greater(VeilRenderer.HangBelowNeck, 0.15f);
            Assert.Less(VeilRenderer.HangBelowNeck, 0.5f, "still wraps the neck");
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
