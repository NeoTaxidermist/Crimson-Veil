using CrimsonVeil.EditorTools;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The scarf after the new key art: deep crimson ink (measured from the picture), solid cloth whose crossing strands
    /// hide each other cleanly, the under-layer well behind the band, fine threads and drops of ink round the tail.
    /// </summary>
    [Category("Veil")]
    public class VeilKeyArtTests
    {
        [Test]
        public void TheInk_IsTheKeyArtsDeepCrimson_NotAPureRed()
        {
            // the key art, sRGB red of the scarf: ~75 deep, ~120 its body, ~190 its light; a little green and blue in it
            Assert.That(LevelKitMaterials.ClothDeep.r, Is.InRange(0.2f, 0.32f));
            Assert.That(LevelKitMaterials.ClothMid.r, Is.InRange(0.45f, 0.6f));
            Assert.That(LevelKitMaterials.ClothLight.r, Is.InRange(0.72f, 0.9f));
            foreach (var c in new[] { LevelKitMaterials.ClothDeep, LevelKitMaterials.ClothMid, LevelKitMaterials.ClothLight })
            {
                Assert.Greater(c.g, 0.02f, "not a pure red");
                Assert.Less(c.g, c.r * 0.2f, "still plainly red");
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/Veil.mat");
            Assert.AreEqual(LevelKitMaterials.ClothMid.r, mat.GetColor("_ClothMid").r, 1e-3f, "the scarf's material carries it");
            Assert.AreEqual(1f, mat.GetFloat("_SolidDepth"), "its solid cloth writes depth");
            Assert.AreEqual(1f, mat.GetFloat("_Volume"));
        }

        [Test]
        public void TheCloth_IsSolid_ItsStrandsCrossingCleanly()
        {
            Assert.GreaterOrEqual(VeilRenderer.SolidCloth, 0.95f, "solid ink, not a wash seen through");
            Assert.AreEqual(1f, VeilRenderer.OpaqueLoose, "every loose layer");
            Assert.LessOrEqual(VeilRenderer.ChunkSegments, 4, "painted back to front in short pieces");
            Assert.GreaterOrEqual(VeilRenderer.UnderLayerDepth, 0.08f, "the dark under-layer well behind the band: it never pokes through it");
            var smoke = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/VeilSmoke.mat");
            Assert.AreEqual(0f, smoke.GetFloat("_SolidDepth"), "the smoke threads stay a wash");
        }

        [Test]
        public void TheHerosCollar_IsTheScarfsCrimson_TheRuinsKeepTheirs()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Player.prefab");
            bool collar = false;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsFalse(m != null && m.name == "RuinCrimson", "not the ruins' brighter crimson (it didn't match the scarf at the neck)");
                    if (m != null && m.name == "HeroCrimson") collar = true;
                }
            Assert.IsTrue(collar, "the collar and trim in the hero's own crimson");
            var hero = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/HeroCrimson.mat");
            Assert.AreEqual(LevelKitMaterials.HeroCrimson.r, hero.GetColor("_BaseColor").r, 1e-3f);
            Assert.Less(LevelKitMaterials.HeroCrimson.r, 0.5f, "a deep crimson like the scarf where it leaves the collar");
            // not one flat red: the scarf's painted strokes, deep and bright, run round it
            Assert.AreEqual(1f, hero.GetFloat("_InkStreaks"), "painted like the scarf");
            Assert.Less(hero.GetColor("_StreakDeep").r, LevelKitMaterials.HeroCrimson.r);
            Assert.Greater(hero.GetColor("_StreakLight").r, LevelKitMaterials.HeroCrimson.r);
            Assert.Less(hero.GetFloat("_MistStrength"), 0.1f, "no mist greying it beside the scarf");
        }

        [Test]
        public void TheScarf_IsPaintedInk_InABoldS_NotRubber()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CrimsonVeil/Materials/Veil.mat");
            Assert.LessOrEqual(mat.GetFloat("_LightAmount"), 0.2f, "the light only hints at its folds (more read as rubber)");
            Assert.LessOrEqual(VeilRenderer.CupDepth, 0.15f, "barely bowing");
            Assert.GreaterOrEqual(VeilRestShape.SDepth, 1.4f, "the key art's S, deeper than the sheet's");
            Assert.GreaterOrEqual(VeilRenderer.RippleHeight, 0.15f, "a flame's swell running along it");
            Assert.Less(VeilRenderer.RippleWaves, 1.2f, "a long wave: one bold S, not a wobble");
            // the tongues hold together in the S (spread wide, each flew its own way and the flame fell apart)
            Assert.LessOrEqual(VeilStrands.Reach, 0.35f);
            Assert.GreaterOrEqual(VeilStrands.HoldTail, 25f, "held to their places down to the tail");
            Assert.LessOrEqual(VeilRenderer.TongueRipple, 1f, "rippling with the band, not against it");
        }

        [Test]
        public void AtARun_ItKeepsItsS_AsOneCloth_WithOpenWhorls()
        {
            Assert.GreaterOrEqual(VeilController.RunShapeWeight, 0.8f, "at a run it keeps its S (eased off, the wind drew it out straight)");
            Assert.LessOrEqual(VeilRenderer.LooseTintContrast, 0.5f, "its layers only hint at their tints: one cloth, not bands");
            Assert.That(VeilRenderer.LooseCurlTurns, Is.InRange(0.7f, 1f), "open whorls, under a turn");
            Assert.Less(LevelKitMaterials.HeroCrimson.g, 0.02f, "the collar a clean crimson, no grey in it");
        }

        [Test]
        public void TheTail_ShedsThreadsOfSmoke_AndDropsOfInk()
        {
            Assert.That(VeilWisps.DropShare, Is.InRange(0.15f, 0.5f), "some drops among the threads");
            Assert.LessOrEqual(VeilWisps.DropPoints, 4, "a drop is a short dab");
            Assert.AreEqual(0f, VeilWisps.DropT(0, 3), 1e-6f, "pointed where it starts");
            Assert.AreEqual(0.5f, VeilWisps.DropT(1, 3), 1e-6f, "full in the middle");
            Assert.AreEqual(1f, VeilWisps.DropT(2, 3), 1e-6f, "pointed where it ends");
            Assert.AreEqual(0.5f, VeilWisps.DropT(0, 1), 1e-6f, "a single sample: its full width");
        }
    }
}
