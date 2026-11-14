using CrimsonVeil.UI;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The scarf-shaped life gauge: one torn piece per point of life, lost ones left as ash.</summary>
    public class ScarfHudTests
    {
        [Test]
        public void OnePiecePerPointOfLife_InOrder_WithGaps()
        {
            var p = ScarfHudLayout.Pieces(5f, 5f);
            Assert.AreEqual(5, p.Count);
            for (int i = 0; i < p.Count; i++)
            {
                Assert.AreEqual(1f, p[i].fill);
                if (i > 0) Assert.Greater(p[i].start, p[i - 1].end, "torn apart by a small gap");
            }
            Assert.AreEqual(0f, p[0].start, 1e-4f);
            Assert.AreEqual(1f, p[4].end, 1e-4f, "the ribbon spans the whole gauge");
        }

        [Test]
        public void LostLife_BurnsPiecesFromTheLooseEnd()
        {
            var p = ScarfHudLayout.Pieces(5f, 2.5f);
            Assert.AreEqual(1f, p[0].fill);
            Assert.AreEqual(1f, p[1].fill);
            Assert.AreEqual(0.5f, p[2].fill, 1e-4f, "half a point: half a piece");
            Assert.AreEqual(0f, p[3].fill);
            Assert.AreEqual(0f, p[4].fill);
            Assert.AreEqual(p[2].FillEnd, ScarfHudLayout.LivingEnd(5f, 2.5f), 1e-4f, "the flame burns where life ends");
        }

        [Test]
        public void Dead_LeavesOnlyAsh_AndOverhealIsClamped()
        {
            Assert.AreEqual(0f, ScarfHudLayout.LivingEnd(5f, 0f));
            foreach (var piece in ScarfHudLayout.Pieces(5f, 99f)) Assert.AreEqual(1f, piece.fill);
        }

        [Test]
        public void Ribbon_FluttersMoreAtItsLooseEnd()
        {
            float nearKnot = 0f, looseEnd = 0f;
            for (float t = 0f; t < 3f; t += 0.05f)
            {
                nearKnot = UnityEngine.Mathf.Max(nearKnot, UnityEngine.Mathf.Abs(ScarfHudLayout.Wave(0.03f, t, 6f)));
                looseEnd = UnityEngine.Mathf.Max(looseEnd, UnityEngine.Mathf.Abs(ScarfHudLayout.Wave(0.95f, t, 6f)));
            }
            Assert.Less(nearKnot, 1f, "tied fast at the knot");
            Assert.Greater(looseEnd, 4f);
        }
    }
}
