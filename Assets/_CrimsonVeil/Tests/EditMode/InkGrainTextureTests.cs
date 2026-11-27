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
