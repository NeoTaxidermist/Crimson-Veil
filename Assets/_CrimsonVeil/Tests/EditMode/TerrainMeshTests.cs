using System.Linq;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class TerrainMeshTests
    {
        [TestCase(6f, 2f, TerrainStyle.Rock, 1)]
        [TestCase(3f, 0.8f, TerrainStyle.Rock, 9)]
        [TestCase(1.4f, 8f, TerrainStyle.Ruin, 4)]
        [TestCase(20f, 12f, TerrainStyle.Rock, 77)]
        public void Outline_TopIsWalkableAndInsideTheCollider(float w, float h, TerrainStyle style, int seed)
        {
            var o = ProceduralMeshes.TerrainOutline(w, h, style, 1f, seed);
            var top = o.Where(p => p.y > -0.05f).ToList();
            Assert.Greater(top.Count, 3);
            Assert.That(top.Max(p => p.y), Is.LessThanOrEqualTo(0.03f), "top bumps stay tiny so feet match the box collider");
            Assert.That(top.Min(p => p.x), Is.LessThanOrEqualTo(-w * 0.5f + 0.2f));
            Assert.That(top.Max(p => p.x), Is.GreaterThanOrEqualTo(w * 0.5f - 0.2f));
        }

        [Test]
        public void Triangulate_SimplePolygonGivesNMinus2Triangles()
        {
            var square = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            Assert.AreEqual(6, ProceduralMeshes.Triangulate(square).Count);

            // Concave "L" shape.
            var l = new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 1), new Vector2(1, 1), new Vector2(1, 2), new Vector2(0, 2) };
            var tris = ProceduralMeshes.Triangulate(l);
            Assert.AreEqual((l.Length - 2) * 3, tris.Count);
            float area = 0f;
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector2 a = l[tris[i]], b = l[tris[i + 1]], c = l[tris[i + 2]];
                area += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
            }
            Assert.AreEqual(3f, area, 1e-4f, "triangles cover exactly the L");
        }

        [Test]
        public void TerrainOutline_TriangulatesCompletely()
        {
            var o = ProceduralMeshes.TerrainOutline(8f, 3f, TerrainStyle.Rock, 1f, 5);
            Assert.AreEqual((o.Count - 2) * 3, ProceduralMeshes.Triangulate(o).Count);
        }

        [Test]
        public void TerrainMesh_HasExpectedExtent()
        {
            var m = ProceduralMeshes.Terrain(6f, 2f, 4f, TerrainStyle.Rock, 0.7f, 3);
            Assert.AreEqual(4f, m.bounds.size.z, 1e-3f);
            Assert.AreEqual(6f, m.bounds.size.x, 0.6f);
            Assert.Less(m.bounds.max.y, 0.05f);
            Assert.Less(m.bounds.min.y, -1.9f);
        }

        [Test]
        public void HangingRoots_HangDown()
        {
            var m = ProceduralMeshes.HangingRoots(2, 10, 3f, new Vector2(0.2f, 0.8f));
            Assert.LessOrEqual(m.bounds.max.y, 1e-4f);
            Assert.Less(m.bounds.min.y, -0.15f);
        }

        [Test]
        public void Column_StandsAndBrokenIsShorter()
        {
            var whole = ProceduralMeshes.Column(6f, 0.4f, false, 1);
            var broken = ProceduralMeshes.Column(6f, 0.4f, true, 1);
            Assert.AreEqual(6f, whole.bounds.max.y, 1e-3f);
            Assert.Less(broken.bounds.max.y, 5.5f);
            Assert.AreEqual(0f, whole.bounds.min.y, 1e-3f);
        }
    }
}
