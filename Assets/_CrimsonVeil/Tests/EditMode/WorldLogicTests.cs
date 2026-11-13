using System.Linq;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class WorldLogicTests
    {
        [Test]
        public void BrokenArch_LosesTopCornerButKeepsBase()
        {
            var whole = ProceduralMeshes.Arch(4f, 1f, 3f, 0.6f, 1f, 0f, 0);
            var broken = ProceduralMeshes.Arch(4f, 1f, 3f, 0.6f, 1f, 0.6f, 2);
            int wholeTris = whole.triangles.Length, brokenTris = broken.triangles.Length;
            Assert.Less(brokenTris, wholeTris * 0.9f, "a chunk should be missing");
            Assert.Greater(brokenTris, wholeTris * 0.3f, "but it is still an arch");

            // The bottom of both pillars survives.
            var v = broken.vertices;
            var t = broken.triangles;
            bool leftBase = false, rightBase = false;
            for (int i = 0; i < t.Length; i += 3)
            {
                var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                if (c.y < 0.5f && c.x < -2f) leftBase = true;
                if (c.y < 0.5f && c.x > 2f) rightBase = true;
            }
            Assert.IsTrue(leftBase && rightBase);
        }

        [Test]
        public void BrokenArch_SideDependsOnSeed()
        {
            float TopCentroidX(Mesh m)
            {
                var v = m.vertices; var t = m.triangles;
                float sum = 0f; int n = 0;
                for (int i = 0; i < t.Length; i += 3)
                {
                    var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                    if (c.y > 4f) { sum += c.x; n++; }
                }
                return n > 0 ? sum / n : 0f;
            }
            float even = TopCentroidX(ProceduralMeshes.Arch(4f, 1f, 3f, 0.6f, 1f, 0.6f, 2));
            float odd = TopCentroidX(ProceduralMeshes.Arch(4f, 1f, 3f, 0.6f, 1f, 0.6f, 3));
            Assert.Less(even, 0f, "even seed breaks the right corner, so what remains leans left");
            Assert.Greater(odd, 0f);
        }

        [Test]
        public void Rubble_RestsOnGroundWithinWidth()
        {
            var m = ProceduralMeshes.Rubble(4, 10, 3f, 0.8f);
            Assert.Greater(m.vertexCount, 0);
            Assert.Less(m.bounds.min.y, 0.2f);
            Assert.Less(m.bounds.size.x, 3f + 1.6f);
            Assert.IsTrue(m.vertices.All(p => !float.IsNaN(p.x)));
        }

        [Test]
        public void Urn_HasRequestedHeightAndClosedTop()
        {
            var m = ProceduralMeshes.Urn(1, 0.8f);
            Assert.AreEqual(0.8f, m.bounds.max.y, 1e-3f);
            Assert.AreEqual(0f, m.bounds.min.y, 1e-3f);
            Assert.AreEqual(0f, m.bounds.center.x, 1e-3f);
        }

        [Test]
        public void HingedBridge_FallsAcceleratingAndSettles()
        {
            Assert.AreEqual(0f, HingedBridge.FallAngle(0f, 0f, -90f), 1e-4f);
            Assert.AreEqual(-90f, HingedBridge.FallAngle(1f, 0f, -90f), 1e-3f);
            float a1 = HingedBridge.FallAngle(0.2f, 0f, -90f), a2 = HingedBridge.FallAngle(0.4f, 0f, -90f), a3 = HingedBridge.FallAngle(0.6f, 0f, -90f);
            Assert.Greater(Mathf.Abs(a3 - a2), Mathf.Abs(a2 - a1), "accelerates like gravity");
            Assert.Greater(HingedBridge.FallAngle(0.92f, 0f, -90f), -90f, "small bounce after impact");
        }
    }
}
