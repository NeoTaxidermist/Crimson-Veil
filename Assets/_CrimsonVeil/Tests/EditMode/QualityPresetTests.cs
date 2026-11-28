using CrimsonVeil.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The picture's quality levels (Nízká, Střední, Vysoká): they exist on Windows in that order, each with its own URP
    /// asset (ambient occlusion only on Vysoká, a lower drawing scale only on Nízká), the high one is the default, the
    /// graphics card picks the first one offered, and a saved choice survives the levels being reordered.
    /// </summary>
    [Category("UI")]
    public class QualityPresetTests
    {
        [TestCase("NVIDIA GeForce RTX 2060", 6144, 50, QualityPresets.High)]
        [TestCase("Intel(R) UHD Graphics", 1024, 50, QualityPresets.Medium)]          // the DELL
        [TestCase("Intel(R) Iris(R) Xe Graphics", 2048, 50, QualityPresets.Medium)]
        [TestCase("Intel(R) HD Graphics 620", 1024, 50, QualityPresets.Low)]
        [TestCase("Intel(R) Arc(TM) A770 Graphics", 16384, 50, QualityPresets.High)]
        [TestCase("AMD Radeon(TM) Graphics", 512, 50, QualityPresets.Medium)]
        [TestCase("AMD Radeon 780M", 4096, 50, QualityPresets.Medium)]
        [TestCase("AMD Radeon(TM) Vega 8 Graphics", 2048, 50, QualityPresets.Medium)]
        [TestCase("AMD Radeon RX 6700 XT", 12288, 50, QualityPresets.High)]
        [TestCase("AMD Radeon RX 6800M", 12288, 50, QualityPresets.High)]
        [TestCase("Some Old Card", 4096, 40, QualityPresets.Low)]
        public void TheGraphicsCardPicksTheLevel(string gpu, int memory, int shaderLevel, string level)
            => Assert.AreEqual(level, QualityPresets.Recommend(gpu, memory, shaderLevel));

        static UniversalRenderPipelineAsset Asset(string level)
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var arr = so.FindProperty("m_QualitySettings");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("name").stringValue != level) continue;
                var ex = e.FindPropertyRelative("excludedTargetPlatforms");
                for (int j = 0; j < ex.arraySize; j++)
                    Assert.AreNotEqual("Standalone", ex.GetArrayElementAtIndex(j).stringValue, level + " is left out on Windows");
                return e.FindPropertyRelative("customRenderPipeline").objectReferenceValue as UniversalRenderPipelineAsset;
            }
            Assert.Fail("no quality level " + level);
            return null;
        }

        static bool HasAmbientOcclusion(UniversalRenderPipelineAsset a)
        {
            var list = new SerializedObject(a).FindProperty("m_RendererDataList");
            var data = list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            var ao = data.rendererFeatures.Find(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            return ao != null && ao.isActive;
        }

        [Test]
        public void ThreeLevelsLowToHigh_EachWithItsOwnPipeline()
        {
            var names = QualitySettings.names;
            int low = System.Array.IndexOf(names, QualityPresets.Low), mid = System.Array.IndexOf(names, QualityPresets.Medium), high = System.Array.IndexOf(names, QualityPresets.High);
            Assert.That(low >= 0 && low < mid && mid < high, "levels out of order: " + string.Join(", ", names));
            var l = Asset(QualityPresets.Low); var m = Asset(QualityPresets.Medium); var h = Asset(QualityPresets.High);
            Assert.IsTrue(HasAmbientOcclusion(h), "Vysoká keeps the ambient occlusion");
            Assert.IsFalse(HasAmbientOcclusion(m));
            Assert.IsFalse(HasAmbientOcclusion(l));
            Assert.AreEqual(1f, h.renderScale); Assert.AreEqual(1f, m.renderScale, "Střední is drawn at full resolution");
            Assert.Less(l.renderScale, 1f);
            Assert.GreaterOrEqual(l.renderScale, 0.75f, "below 75 % the picture turns mushy (540p was an emergency)");
            Assert.That(l.renderScale < 1f);
            // stretched bilinearly: FSR's sharpening cost more than the smaller picture saved on an Intel UHD (11.4 ms vs 7.7)
            Assert.AreEqual(UpscalingFilterSelection.Linear, l.upscalingFilter);
            foreach (var a in new[] { l, m, h }) Assert.IsFalse(a.supportsCameraOpaqueTexture, a.name + " copies the frame for nothing");
        }

        [Test]
        public void WindowsStartsOnTheHighLevel()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var map = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < map.arraySize; i++)
            {
                var e = map.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue == "Standalone")
                    Assert.AreEqual(QualityPresets.High, QualitySettings.names[e.FindPropertyRelative("second").intValue]);
            }
        }

        [Test]
        public void AntiAliasing_SmaaOnHigh_FxaaBelow()
        {
            Assert.AreEqual(AntialiasingMode.SubpixelMorphologicalAntiAliasing, QualityPresets.AntiAliasing(QualityPresets.High));
            Assert.AreEqual(AntialiasingMode.FastApproximateAntialiasing, QualityPresets.AntiAliasing(QualityPresets.Medium));
            Assert.AreEqual(AntialiasingMode.FastApproximateAntialiasing, QualityPresets.AntiAliasing(QualityPresets.Low));
        }

        [TestCase(QualityPresets.Low)]
        [TestCase(QualityPresets.Medium)]
        [TestCase(QualityPresets.High)]
        public void APresetsItems_AreWhatItsPipelineDoes(string level)
        {
            var a = Asset(level);
            var d = QualityPresets.Preset(level);
            Assert.AreEqual(HasAmbientOcclusion(a), d.ambientOcclusion, "ambient occlusion");
            Assert.AreEqual(a.renderScale, d.renderScale, 1e-4f, "drawing scale");
            Assert.AreEqual(QualityPresets.ShadowsOf(a), d.shadows, "shadows");
            Assert.AreEqual(QualityPresets.ShadowResolutionOf(d.shadows), a.mainLightShadowmapResolution, "shadow map");
            Assert.AreEqual(QualityPresets.AntiAliasing(level), d.antiAliasing);
            Assert.AreEqual(d, QualityPresets.Sanitize(d), "every preset item is one the menu offers");
        }

        [Test]
        public void Vlastni_IsALevelAfterVysoka_WithItsOwnPipelineAndRenderer()
        {
            var names = QualitySettings.names;
            Assert.AreEqual(System.Array.IndexOf(names, QualityPresets.High) + 1, System.Array.IndexOf(names, QualityPresets.Custom), string.Join(", ", names));
            var c = Asset(QualityPresets.Custom);
            Assert.AreNotSame(Asset(QualityPresets.High), c, "changing Vlastní never touches a preset");
            var renderer = new SerializedObject(c).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
            foreach (var level in new[] { QualityPresets.Low, QualityPresets.Medium, QualityPresets.High })
                Assert.AreNotSame(new SerializedObject(Asset(level)).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue, renderer, level + " shares Vlastní's renderer");
            Assert.IsNotNull(QualityPresets.AmbientOcclusionOf(c), "Vlastní can turn the ambient occlusion on");
            Assert.IsFalse(c.supportsCameraOpaqueTexture);
            Assert.AreNotEqual(QualityPresets.Custom, QualityPresets.Recommend("Intel(R) UHD Graphics", 1024, 50));
        }

        [Test]
        public void ApplyDetail_SetsVlastnisPipeline()
        {
            var c = Asset(QualityPresets.Custom);
            var ao = QualityPresets.AmbientOcclusionOf(c);
            float scale = c.renderScale; var filter = c.upscalingFilter; int cascades = c.shadowCascadeCount, res = c.mainLightShadowmapResolution; bool aoOn = ao.isActive;
            try
            {
                QualityPresets.ApplyDetail(c, new QualityPresets.Detail { ambientOcclusion = false, antiAliasing = AntialiasingMode.None, renderScale = 0.75f, shadows = 0 });
                Assert.IsFalse(ao.isActive);
                Assert.AreEqual(0.75f, c.renderScale, 1e-4f);
                Assert.AreEqual(UpscalingFilterSelection.Linear, c.upscalingFilter);
                Assert.AreEqual(1, c.shadowCascadeCount);
                Assert.AreEqual(1024, c.mainLightShadowmapResolution);
                QualityPresets.ApplyDetail(c, QualityPresets.Preset(QualityPresets.High));
                Assert.IsTrue(ao.isActive);
                Assert.AreEqual(1f, c.renderScale);
                Assert.AreEqual(4, c.shadowCascadeCount);
                Assert.AreEqual(2048, c.mainLightShadowmapResolution);
            }
            finally
            {
                c.renderScale = scale; c.upscalingFilter = filter; c.shadowCascadeCount = cascades; c.mainLightShadowmapResolution = res; ao.SetActive(aoOn);
            }
        }

        [Test]
        public void Sanitize_KeepsOnlyWhatTheMenuOffers()
        {
            var d = QualityPresets.Sanitize(new QualityPresets.Detail { antiAliasing = (AntialiasingMode)77, renderScale = 0.3f, shadows = 9 });
            Assert.AreEqual(0.75f, d.renderScale);
            Assert.AreEqual(2, d.shadows);
            Assert.AreEqual(AntialiasingMode.SubpixelMorphologicalAntiAliasing, d.antiAliasing);
            Assert.AreEqual(0.9f, QualityPresets.Sanitize(new QualityPresets.Detail { renderScale = 0.88f }).renderScale);
            Assert.AreEqual(1f, QualityPresets.Sanitize(new QualityPresets.Detail { renderScale = float.NaN }).renderScale);
        }

        [Test]
        public void ChangingAnItem_MakesTheQualityVlastni_PickingAPresetShowsItsItems()
        {
            GameSettings.BeginTests();
            try
            {
                GameSettings.SetQuality(QualityPresets.IndexOf(QualityPresets.Medium));
                Assert.AreEqual(QualityPresets.Preset(QualityPresets.Medium), GameSettings.ChosenDetail);
                var d = GameSettings.ChosenDetail;
                d.ambientOcclusion = true;
                GameSettings.SetDetail(d);
                Assert.AreEqual(QualityPresets.Custom, GameSettings.LevelName(GameSettings.Quality));
                Assert.IsTrue(GameSettings.ChosenDetail.ambientOcclusion);
                Assert.AreEqual(1f, GameSettings.ChosenDetail.renderScale, "the rest stays Střední's");
                var before = GameSettings.Capture();
                GameSettings.SetQuality(QualityPresets.IndexOf(QualityPresets.Low));
                Assert.AreEqual(QualityPresets.Preset(QualityPresets.Low), GameSettings.ChosenDetail);
                GameSettings.Restore(before);
                Assert.AreEqual(QualityPresets.Custom, GameSettings.LevelName(GameSettings.Quality), "Zrušit takes it back");
                Assert.IsTrue(GameSettings.ChosenDetail.ambientOcclusion);
                Assert.AreEqual(AntialiasingMode.FastApproximateAntialiasing, QualityPresets.AntiAliasing(QualityPresets.Custom), "Vlastní's own anti-aliasing");
            }
            finally { GameSettings.EndTests(); }
        }

        [Test]
        public void ASavedChoiceIsKeptByName()
        {
            Assert.AreEqual(QualityPresets.IndexOf(QualityPresets.Medium), GameSettings.SavedQuality(QualityPresets.Medium, true));
            Assert.AreEqual(QualityPresets.IndexOf(QualityPresets.Low), GameSettings.SavedQuality(QualityPresets.Low, false));
            // saved before v0.106: an index into the one level there was, the high one
            Assert.AreEqual(QualityPresets.IndexOf(QualityPresets.High), GameSettings.SavedQuality("", true));
            Assert.AreEqual(QualityPresets.IndexOf(QualityPresets.High), GameSettings.SavedQuality("PC", false));
            // nothing saved: what the graphics card is recommended
            Assert.AreEqual(GameSettings.RecommendedIndex(), GameSettings.SavedQuality("", false));
            Assert.AreEqual(GameSettings.RecommendedIndex(), GameSettings.SavedQuality("Ultra", false));
        }
    }
}
