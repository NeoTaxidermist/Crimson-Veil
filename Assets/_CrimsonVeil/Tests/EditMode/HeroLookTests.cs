using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero stays ink-black, but the moon lifts his form just enough to read as 3D.</summary>
    public class HeroLookTests
    {
        [Test]
        public void TheHero_IsBlack_WithAFaintlyMoonlitForm()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/HeroInk.mat");
            Assert.IsNotNull(m);
            Assert.IsTrue(m.HasProperty("_FormLift"), "the silhouette shader can add moonlight on near-black ink");
            Assert.Less(m.GetColor("_BaseColor").grayscale, 0.03f, "still ink-black");
            float lift = m.GetFloat("_FormLift");
            Assert.That(lift, Is.InRange(0.01f, 0.06f), "a hint of form, not a grey figure");
            Assert.That(m.GetFloat("_RimLight"), Is.InRange(0.01f, 0.12f), "a faint rim, not an outline");
            Assert.AreEqual(CrimsonVeil.EditorTools.LevelKitMaterials.HeroFormLift, lift, 1e-4f, "the material builder makes the same look");
        }
    }
}
