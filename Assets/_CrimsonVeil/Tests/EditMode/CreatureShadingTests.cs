using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Creatures are shaded grey with a pale rim so they read on dark backgrounds; the hero's shadow stays pure ink.</summary>
    public class CreatureShadingTests
    {
        [Test]
        public void Creatures_AreShadedGrey_WithAPaleRim()
        {
            var m = LevelKitMaterials.Get(true);
            Assert.Greater(m.wraith.GetFloat("_RimLight"), 0.3f, "a pale rim along the outline");
            Assert.Greater(m.wraith.GetFloat("_FormShade"), 0.3f, "modelled by the moonlight");
            float v = m.wraith.GetColor("_BaseColor").grayscale;
            Assert.That(v, Is.InRange(0.04f, 0.15f), "dark grey, not black and not washed out");
            Assert.Greater(m.wraith.GetColor("_RimColor").grayscale, 0.4f);
        }

        [Test]
        public void ShadowDouble_StaysPureInk()
        {
            var m = LevelKitMaterials.Get();
            Assert.Less(m.shadowInk.GetColor("_BaseColor").grayscale, 0.02f);
            Assert.AreEqual(0f, m.shadowInk.GetFloat("_FormShade"), 1e-4f);
            Assert.AreEqual("ShadowInk", LevelKitFactory.ShadowDouble(Vector3.zero).GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.name);
        }

        [Test]
        public void Environment_IsUntouched()
        {
            var m = LevelKitMaterials.Get();
            foreach (var mat in new[] { m.inkPlay, m.inkFG, m.inkNear, m.hero })
            {
                Assert.AreEqual(0f, mat.GetFloat("_RimLight"), 1e-4f, mat.name);
                Assert.AreEqual(0f, mat.GetFloat("_FormShade"), 1e-4f, mat.name);
            }
        }
    }
}
