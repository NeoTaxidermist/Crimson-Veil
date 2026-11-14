using System.Linq;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    public class ProceduralMeshesTests
    {
        static void AssertFinite(Mesh m)
        {
            Assert.IsTrue(m.vertices.All(v => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)), m.name + " has NaN vertices");
            for (int s = 0; s < m.subMeshCount; s++)
                Assert.AreEqual(0, m.GetTriangles(s).Length % 3);
        }

        static void AssertWindingMatchesNormals(Mesh m)
        {
            var v = m.vertices;
            var n = m.normals;
            var t = m.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                var face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (face.sqrMagnitude < 1e-10f) continue;
                Assert.GreaterOrEqual(Vector3.Dot(face, n[t[i]]), -1e-5f, $"triangle {i / 3} faces away from its normal");
            }
        }

        [Test]
        public void Arch_HasExpectedBoundsAndOpening()
        {
            var m = ProceduralMeshes.Arch(span: 4f, pillarWidth: 1f, springHeight: 3f, crown: 0.5f, depth: 1f);
            AssertFinite(m);
            AssertWindingMatchesNormals(m);
            var b = m.bounds;
            // The wall body spans 6 m; carved imposts and the cornice may stand out a little on the camera side.
            Assert.That(b.size.x, Is.InRange(6f - 1e-3f, 6.5f));
            Assert.AreEqual(3f + 2f + 0.5f, b.size.y, 1e-3f);
            Assert.AreEqual(0.5f, b.max.z, 0.03f, "the back face is plain");
            Assert.LessOrEqual(b.min.z, -0.5f + 1e-3f);
            Assert.AreEqual(0f, b.min.y, 1e-3f);

            // Nothing of the wall body may lie inside the opening (only proud carving and hanging ivy).
            foreach (var p in m.vertices)
            {
                if (p.z < -0.5f + 0.03f) continue; // plinths, imposts and ivy on the front face may overhang the jambs
                var rel = new Vector2(p.x, Mathf.Max(0f, p.y - 3f));
                bool insideOpening = Mathf.Abs(p.x) < 1.99f && (p.y < 3f || rel.magnitude < 1.99f);
                Assert.IsFalse(insideOpening, $"vertex {p} inside arch opening");
            }
        }

        [Test]
        public void Grass_BladesStartAtGroundAndRespectHeight()
        {
            var m = ProceduralMeshes.Grass(7, 30, 2f, new Vector2(0.4f, 1f));
            AssertFinite(m);
            Assert.AreEqual(0f, m.bounds.min.y, 1e-4f);
            Assert.LessOrEqual(m.bounds.max.y, 1f + 1e-4f);
            Assert.IsTrue(m.uv.All(u => u.y >= 0f && u.y <= 1f));
        }

        [Test]
        public void Grass_ThinsOutTowardsTheEnds_AndSinksItsRoots()
        {
            var m = ProceduralMeshes.Grass(5, 400, 4f, new Vector2(1f, 1f), 0.05f, 0.8f, 0.1f);
            AssertFinite(m);
            float endTop = 0f, midTop = 0f;
            foreach (var v in m.vertices)
            {
                if (Mathf.Abs(v.x) > 1.9f) endTop = Mathf.Max(endTop, v.y);
                if (Mathf.Abs(v.x) < 0.5f) midTop = Mathf.Max(midTop, v.y);
            }
            Assert.Less(endTop, midTop * 0.6f, "short blades at the ends, no hard cut");
            Assert.That(m.bounds.min.y, Is.InRange(-0.1f - 1e-4f, -0.05f), "ragged base below the edge");
            Assert.LessOrEqual(m.bounds.max.y, 1f + 1e-4f, "a sunk root still reaches the same tip height");
        }

        [Test]
        public void Grass_IsDeterministicPerSeed()
        {
            var a = ProceduralMeshes.Grass(3, 10, 1f, new Vector2(0.2f, 0.5f));
            var b = ProceduralMeshes.Grass(3, 10, 1f, new Vector2(0.2f, 0.5f));
            var c = ProceduralMeshes.Grass(4, 10, 1f, new Vector2(0.2f, 0.5f));
            CollectionAssert.AreEqual(a.vertices, b.vertices);
            CollectionAssert.AreNotEqual(a.vertices, c.vertices);
        }

        [Test]
        public void Banner_HangsFromTopWithPinnedUv()
        {
            var m = ProceduralMeshes.Banner(2, 1f, 3f);
            AssertFinite(m);
            Assert.AreEqual(0f, m.bounds.max.y, 1e-4f);
            Assert.AreEqual(-3f, m.bounds.min.y, 1e-4f, "middle point reaches full length");
            var verts = m.vertices;
            var uvs = m.uv;
            for (int i = 0; i < verts.Length; i++)
                if (Mathf.Abs(verts[i].y) < 1e-5f) Assert.AreEqual(1f, uvs[i].y, 1e-5f);
        }

        [Test]
        public void Tree_HasTrunkAndLeafSubmeshes()
        {
            var m = ProceduralMeshes.Tree(5, 8f, 0.4f);
            AssertFinite(m);
            Assert.AreEqual(2, m.subMeshCount);
            Assert.Greater(m.GetTriangles(0).Length, 100);
            Assert.Greater(m.GetTriangles(1).Length, 100);
            Assert.Greater(m.bounds.size.y, 4f);
        }
    }
}
