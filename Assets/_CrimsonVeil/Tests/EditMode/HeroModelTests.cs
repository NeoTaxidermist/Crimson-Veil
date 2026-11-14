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

        [Test]
        public void IsHeroSized()
        {
            var b = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.bounds;
            float h = Mathf.Max(b.size.y, b.size.z);
            Assert.That(h, Is.InRange(1.8f, 2.1f), "about 1.8 m tall with the hair");
        }
    }
}
