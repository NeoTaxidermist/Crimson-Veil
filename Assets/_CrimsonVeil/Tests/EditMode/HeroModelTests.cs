using System.Linq;
using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero model (ArtSource/hero_build.py): the rig the game poses, the crimson trim, the hair.</summary>
    public class HeroModelTests
    {
        const string HeroPath = "Assets/_CrimsonVeil/Art/Hero/Hero.fbx";

        static GameObject Hero() => AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);

        [Test]
        public void HasEveryBoneTheAnimatorAndTheVeilUse()
        {
            var names = Hero().GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
            foreach (var b in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R",
                                      "LowerArm.R", "Hand.R", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R" })
                CollectionAssert.Contains(names, b);
        }

        [Test]
        public void WindsweptHair_IsMadeOfSpringChains()
        {
            var chains = Hero().GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Hair.") && t.name.EndsWith(".0"));
            Assert.GreaterOrEqual(chains, 8, "the mop's spikes (traced from the concept) each swing on their own");
        }

        [Test]
        public void InkBody_WithACrimsonScarfAndTrim()
        {
            var smr = Hero().GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = smr.sharedMesh;
            Assert.AreEqual(2, mesh.subMeshCount, "ink body + crimson scarf, chest trim and sash");
            var crimson = mesh.GetSubMesh(1).bounds;
            Assert.Less(crimson.size.magnitude, mesh.bounds.size.magnitude * 0.6f, "crimson only as accents");
            var ink = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var red = new Material(ink);
            var mats = LevelKitFactory.HeroMaterials(smr, ink, red);
            Assert.AreSame(ink, mats[0]);
            Assert.AreSame(red, mats[1]);
        }

        /// <summary>Heights (m above the soles) and sideways positions of the mesh's vertices (submesh or all).</summary>
        static (float h, float x)[] Points(int submesh = -1)
        {
            var mesh = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            var b = mesh.bounds;
            int up = b.size.y >= b.size.z ? 1 : 2;
            var v = mesh.vertices;
            var idx = submesh < 0 ? Enumerable.Range(0, v.Length) : mesh.GetIndices(submesh).Distinct();
            return idx.Select(i => (v[i][up] - b.min[up], v[i].x)).ToArray();
        }

        [Test]
        public void Tunic_HangsFromTheBelt_OverBothThighs()
        {
            var band = Points().Where(p => p.h > 0.64f && p.h < 0.74f).ToArray();
            float width = band.Max(p => p.x) - band.Min(p => p.x);
            Assert.Greater(width, 0.4f, "above the knees the tunic covers both legs (the legs alone are ~0.37 m across)");
        }

        [Test]
        public void Belt_SitsAtTheWaist_WithTheCrimsonSashKnot()
        {
            var red = Points(1);
            Assert.IsTrue(red.Any(p => p.h > 1.0f && p.h < 1.12f), "the buckle's inset and the sash knot at the waist");
            Assert.IsFalse(red.Any(p => p.h > 1.18f && p.h < 1.32f), "no crimson ring round the chest any more");
        }

        [Test]
        public void Head_IsASculptedFace_WithANoseAndRoundCheeks()
        {
            var mesh = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            var b = mesh.bounds;
            int up = b.size.y >= b.size.z ? 1 : 2, depth = 3 - up;
            var nose = mesh.vertices.Where(v => v[up] - b.min[up] > 1.66f && v[up] - b.min[up] < 1.68f).ToArray();
            // the face is on one side of the depth axis: the nose stands out there, not behind
            float best = float.MinValue;
            foreach (float s in new[] { 1f, -1f })
            {
                float mid = nose.Where(v => Mathf.Abs(v.x) < 0.006f).Max(v => s * v[depth]);
                float side = nose.Where(v => Mathf.Abs(v.x) > 0.03f && Mathf.Abs(v.x) < 0.036f).Max(v => s * v[depth]);
                best = Mathf.Max(best, mid - side);
            }
            Assert.Greater(best, 0.012f, "the nose stands out of the face (the old traced head was a flat slab)");
            float width = nose.Max(v => v.x) - nose.Min(v => v.x);
            Assert.That(width, Is.InRange(0.13f, 0.2f), "a head's width at the cheekbones (with the ears)");
        }

        [Test]
        public void IsHeroSized()
        {
            var b = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.bounds;
            float h = Mathf.Max(b.size.y, b.size.z);
            Assert.That(h, Is.InRange(1.8f, 2.1f), "about 1.8 m tall with the hair");
        }
    }
}
