using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The Silhouette shader reads its ink grain from a baked texture instead of working five octaves of noise out per
    /// pixel (1.7 ms of a 4.5 ms frame at 1080p): the texture has to be there, bound to the shader, tile without a
    /// seam and carry the same noise (its average and spread), and only cut-out materials may pay for a clip().
    /// </summary>
    public class InkGrainTextureTests
    {
        [Test]
        public void TheShaderHasTheBakedGrainBound()
        {
            var si = (ShaderImporter)AssetImporter.GetAtPath(NoiseTextureBaker.ShaderPath);
            var tex = si.GetNonModifiableTexture(NoiseTextureBaker.Property) as Texture2D;
            Assert.IsNotNull(tex, "Silhouette.shader has no baked grain texture: every material would read flat grey");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.TexturePath), tex);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.CracksPath), si.GetNonModifiableTexture(NoiseTextureBaker.CracksProperty),
                "Silhouette.shader has no baked cracks: the stone would show none (white = far from any crack)");
            StringAssert.DoesNotContain("CV_VoronoiEdge(", System.IO.File.ReadAllText(NoiseTextureBaker.ShaderPath), "the cracks are worked out per pixel again (3.3 ms on an Intel UHD)");
        }

        [Test]
        public void TheBakedCracksTileAndAreThinLines()
        {
            // the distance field wraps with the tile, and most of the stone is far from a crack (lines, not a web)
            const int p = NoiseTextureBaker.CrackPeriod;
            Assert.AreEqual(NoiseTextureBaker.CrackEdge(3.3f, 7.1f), NoiseTextureBaker.CrackEdge(3.3f + p, 7.1f), 1e-3f);
            Assert.AreEqual(NoiseTextureBaker.CrackEdge(3.3f, 7.1f), NoiseTextureBaker.CrackEdge(3.3f, 7.1f + p), 1e-3f);
            var rng = new System.Random(3);
            int onCrack = 0; const int k = 4000;
            for (int i = 0; i < k; i++)
                if (NoiseTextureBaker.CrackEdge((float)rng.NextDouble() * p, (float)rng.NextDouble() * p) < 0.06f) onCrack++;
            Assert.That(onCrack > k / 50 && onCrack < k / 5, "share of the stone on a crack: " + onCrack * 100 / k + " %");
        }

        [Test]
        public void TheGrainTextureIsLinearTilingAndWithoutMips()
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(NoiseTextureBaker.TexturePath);
            Assert.IsFalse(ti.sRGBTexture, "noise values, not colours");
            Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode);
            Assert.IsFalse(ti.mipmapEnabled, "the grain worked out per pixel had no mips: far stone would turn flat");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.TexturePath);
            Assert.AreEqual(NoiseTextureBaker.Size, tex.width);
        }

        [Test]
        public void TheBakedNoiseTilesWithoutASeam()
        {
            // the noise just inside one edge flows on into the opposite edge, as between any two neighbours
            const int n = NoiseTextureBaker.Size;
            float step = (float)NoiseTextureBaker.Period / n, seam = 0f, inside = 0f;
            for (int i = 0; i < 64; i++)
            {
                float y = (i * 31 + 0.5f) * step;
                seam += Mathf.Abs(NoiseTextureBaker.Fbm((n - 0.5f) * step, y) - NoiseTextureBaker.Fbm(NoiseTextureBaker.Period + 0.5f * step, y));
                inside += Mathf.Abs(NoiseTextureBaker.Fbm(100.5f * step, y) - NoiseTextureBaker.Fbm(101.5f * step, y));
            }
            Assert.Less(seam, inside * 1.5f + 1e-4f);
            Assert.AreEqual(NoiseTextureBaker.Fbm(0.3f, 0.7f), NoiseTextureBaker.Fbm(0.3f + NoiseTextureBaker.Period, 0.7f), 1e-3f);
        }

        [Test]
        public void TheBakedNoiseHasTheGrainsAverageAndSpread()
        {
            // CV_Fbm: five octaves 0.5..1/32 of value noise - averages ~0.48, the shader's grain is built around it
            float sum = 0f, sq = 0f;
            const int k = 4000;
            var rng = new System.Random(5);
            for (int i = 0; i < k; i++)
            {
                float v = NoiseTextureBaker.Fbm((float)rng.NextDouble() * NoiseTextureBaker.Period, (float)rng.NextDouble() * NoiseTextureBaker.Period);
                sum += v; sq += v * v;
            }
            float mean = sum / k, sd = Mathf.Sqrt(sq / k - mean * mean);
            Assert.AreEqual(0.485f, mean, 0.03f);
            Assert.AreEqual(0.126f, sd, 0.03f);
        }

        [Test]
        public void TheMistAndTheGradeReadTheirBakedNoise()
        {
            // the mist bands read the grain, the final grade its paper fibres: unbound they would read flat grey
            // (no drifting mist, no paper)
            var mist = (ShaderImporter)AssetImporter.GetAtPath(NoiseTextureBaker.MistShaderPath);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.TexturePath), mist.GetNonModifiableTexture(NoiseTextureBaker.Property));
            var grade = (ShaderImporter)AssetImporter.GetAtPath(NoiseTextureBaker.GradeShaderPath);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.PaperPath), grade.GetNonModifiableTexture(NoiseTextureBaker.PaperProperty));
            foreach (var path in new[] { NoiseTextureBaker.MistShaderPath, NoiseTextureBaker.GradeShaderPath })
                StringAssert.DoesNotContain("CV_Fbm(", System.IO.File.ReadAllText(path), path + " works its noise out per pixel again");
        }

        [Test]
        public void ThePaperTextureIsLinearTilingAndHasThePapersSpread()
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(NoiseTextureBaker.PaperPath);
            Assert.IsFalse(ti.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode);
            Assert.IsFalse(ti.mipmapEnabled);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTextureBaker.PaperPath);
            Assert.AreEqual(NoiseTextureBaker.PaperSize, tex.width);
        }

        [Test]
        public void TheRendererSkipsWorkNothingShows()
        {
            // no shader reads the opaque copy (copying the frame cost ~0.3 ms on an integrated GPU), and the
            // ambient occlusion is worked out at half resolution (1.4 ms saved, the blur hides the difference)
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Assert.IsFalse(asset.supportsCameraOpaqueTexture, "the opaque texture is copied every frame for nothing");
            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/_CrimsonVeil" }))
                StringAssert.DoesNotContain("_CameraOpaqueTexture", System.IO.File.ReadAllText(AssetDatabase.GUIDToAssetPath(guid)),
                    "a shader reads the opaque texture now: switch it back on in PC_RPAsset");
            var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
            var ssao = data.rendererFeatures.Find(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            Assert.IsNotNull(ssao, "the ambient occlusion is part of the look");
            Assert.IsTrue(new SerializedObject(ssao).FindProperty("m_Settings.Downsample").boolValue, "SSAO at full resolution costs 1.4 ms more");
        }

        [Test]
        public void OnlyCutOutMaterialsUseTheClipVariant()
        {
            // a shader that may discard keeps the GPU from rejecting hidden pixels early: the solid ink must not
            var shader = Shader.Find("CrimsonVeil/Silhouette");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_CrimsonVeil" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader != shader) continue;
                bool cut = m.GetFloat("_AlphaClip") > 0.5f;
                Assert.AreEqual(cut, m.IsKeywordEnabled("_ALPHACLIP_ON"), m.name + ": the alpha-clip toggle and its variant disagree");
            }
        }
    }
}
