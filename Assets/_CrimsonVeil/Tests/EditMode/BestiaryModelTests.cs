using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The bestiary's 3D models: the shield monk and the hanged man are rigged like the wraith (loose chains swing on
    /// SpringBones); the crawler, moth, kite, crow and lantern are detailed kit models whose named parts the scripts drive.
    /// </summary>
    [Category("Combat")]
    public class BestiaryModelTests
    {
        const string Art = "Assets/_CrimsonVeil/Art/Enemy/";
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";

        static GameObject Load(string fbx) => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art + fbx + ".fbx"));

        static int Chains(Transform root)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Tatter.") && t.name.EndsWith(".0")) n++;
            return n;
        }

        static int Verts(Transform root)
        {
            int n = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) n += mf.sharedMesh.vertexCount;
            foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.sharedMesh != null) n += s.sharedMesh.vertexCount;
            return n;
        }

        [Test]
        public void TheShieldMonk_IsRigged_WithRobeChains_AndASeparateShield()
        {
            var m = Load("ShieldMonk");
            try
            {
                Assert.IsNotNull(CreatureParts.Find(m.transform, "Shield"), "the script raises and drops the shield");
                Assert.GreaterOrEqual(Chains(m.transform), 12, "hem strips, mantle tails, belt cords");
                Assert.That(m.GetComponentInChildren<SkinnedMeshRenderer>().bounds.size.y, Is.InRange(1.7f, 2.5f));
            }
            finally { Object.DestroyImmediate(m); }
        }

        /// <summary>
        /// Rebuilt to stand beside the hero (v0.102): a starved body with every rib, a fallen head with a face, real hands
        /// and feet - yet light enough for several hanging in one place; the crimson noose round his neck.
        /// </summary>
        [Test]
        public void TheHangedMan_IsMadeAsFinelyAsTheHero_WithTheNooseRoundHisNeck()
        {
            var h = Load("HangedMan");
            var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx"));
            try
            {
                var smr = h.GetComponentInChildren<SkinnedMeshRenderer>();
                var mesh = smr.sharedMesh;
                int heroVerts = hero.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount;
                Assert.Greater(mesh.vertexCount, heroVerts * 0.7f, "as finely made as the hero (it had a fraction of his detail)");
                Assert.Less(mesh.vertexCount, 60000, "light enough for several at once");
                Assert.AreEqual(2, smr.sharedMaterials.Length, "ink and crimson");
                // crimson at the neck: the noose (stored z up; the neck is at 1.6-1.75 m, the tongue in the fallen head above)
                var v = mesh.vertices;
                int neck = 0;
                foreach (int i in mesh.GetTriangles(1)) if (v[i].z > 1.55f && v[i].z < 1.78f) neck++;
                Assert.Greater(neck, 200, "the noose round his neck");
                Assert.That(smr.bounds.size.y, Is.InRange(1.9f, 2.3f), "a man's height, the feet at the bottom");
            }
            finally { Object.DestroyImmediate(h); Object.DestroyImmediate(hero); }
        }

        [Test]
        public void TheHangedMan_HasLooseLimbs_AndAFallenHead()
        {
            var h = Load("HangedMan");
            try
            {
                for (int i = 0; i < 5; i++) Assert.IsNotNull(RigJoint.FindDeep(h.transform, $"Tatter.{i}.0"), "limb / head chain " + i);
                Assert.GreaterOrEqual(Chains(h.transform), 10, "arms, legs, head and shirt strips");
            }
            finally { Object.DestroyImmediate(h); }
        }

        [Test]
        public void TheRiggedPrefabs_SwingTheirChains()
        {
            foreach (var id in new[] { "ShieldMonk", "HangedMan" })
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + id + ".prefab");
                var springs = p.GetComponentInChildren<SpringBones>(true);
                Assert.IsNotNull(springs, id);
                Assert.AreEqual("Tatter.", new SerializedObject(springs).FindProperty("prefix").stringValue, id);
            }
        }

        [TestCase("RootCrawler", new[] { "Knot", "Root0", "Root8" }, 8000)]
        [TestCase("AshMoth", new[] { "Body", "WingL", "WingR" }, 5000)]
        [TestCase("Crow", new[] { "Body", "WingL", "WingR" }, 5000)]
        [TestCase("PaperKite", new string[0], 4000)]
        [TestCase("LanternWisp", new[] { "Lantern", "Flame", "Wisps" }, 4500)]
        public void TheKitCreatures_KeepTheirParts_AndAreDetailed(string fbx, string[] parts, int minVerts)
        {
            var c = Load(fbx);
            try
            {
                foreach (var p in parts) Assert.IsNotNull(CreatureParts.Find(c.transform, p), fbx + " needs " + p);
                Assert.GreaterOrEqual(Verts(c.transform), minVerts, fbx + ": modelled in detail, not a few primitives");
            }
            finally { Object.DestroyImmediate(c); }
        }

        [Test]
        public void TheCrowWings_HingeAtTheShoulder()
        {
            var c = Load("Crow");
            try
            {
                var l = CreatureParts.Find(c.transform, "WingL");
                var r = CreatureParts.Find(c.transform, "WingR");
                Assert.AreEqual(0.04f, l.localPosition.y, 0.01f, "the pivot the flap turns about");
                Assert.Less(l.localPosition.z * r.localPosition.z, 0f, "one wing either side of the body");
            }
            finally { Object.DestroyImmediate(c); }
        }
    }
}
