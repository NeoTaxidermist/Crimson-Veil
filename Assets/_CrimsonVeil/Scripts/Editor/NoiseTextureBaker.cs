using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Bakes the ink grain the Silhouette shader used to work out per pixel (five octaves of value noise, twice for
    /// the grain, once each for the mist's wobble and the cracks' warp: ~100 hashes a pixel, 1.7 ms of a 4.5 ms frame
    /// at 1080p) into one tiling texture it now only reads. Same recipe as <c>CV_Fbm</c> in CVCommon.hlsl, but every
    /// octave's lattice wraps around, so the texture tiles without a seam.
    /// <para>The texture is set on the shader itself (its importer's non-modifiable textures), so every material
    /// drawing with it has it, in the editor and in a build alike.</para>
    /// </summary>
    public static class NoiseTextureBaker
    {
        public const string TexturePath = "Assets/_CrimsonVeil/Shaders/CVNoise.png";
        public const string ShaderPath = "Assets/_CrimsonVeil/Shaders/Silhouette.shader";
        public const string Property = "_CVNoise";

        /// <summary>Texels a side, and lattice cells of the first octave the texture spans (the shader's period).</summary>
        public const int Size = 2048, Period = 32;
        public const int Octaves = 5;

        [MenuItem("Crimson Veil/Bake Ink Grain Texture")]
        public static void Bake()
        {
            var px = new byte[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(Fbm((x + 0.5f) * Period / Size, (y + 0.5f) * Period / Size) * 255f), 0, 255);
            var tex = new Texture2D(Size, Size, TextureFormat.R8, false, true);
            tex.SetPixelData(px, 0);
            tex.Apply();
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            Configure();
        }

        /// <summary>Import settings (linear, one channel, no mips, tiling, bilinear) and the shader's binding.</summary>
        public static void Configure()
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            ti.textureType = TextureImporterType.SingleChannel;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            ti.SetTextureSettings(s);
            ti.sRGBTexture = false;
            ti.mipmapEnabled = false; // (the noise worked out per pixel had no mips either: same look far off)
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = Size;
            ti.SaveAndReimport();

            var si = (ShaderImporter)AssetImporter.GetAtPath(ShaderPath);
            si.SetNonModifiableTextures(new[] { Property }, new Texture[] { AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) });
            si.SaveAndReimport();
        }

        /// <summary><c>CV_Fbm</c> with each octave's lattice wrapped at <see cref="Period"/> times its scale.</summary>
        public static float Fbm(float px, float py)
        {
            float v = 0f, a = 0.5f;
            int period = Period;
            for (int o = 0; o < Octaves; o++)
            {
                v += a * ValueNoise(px, py, period, o);
                px *= 2f; py *= 2f; period *= 2;
                a *= 0.5f;
            }
            return v;
        }

        static float ValueNoise(float px, float py, int period, int octave)
        {
            int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
            float fx = px - ix, fy = py - iy;
            float a = Hash(ix, iy, period, octave), b = Hash(ix + 1, iy, period, octave);
            float c = Hash(ix, iy + 1, period, octave), d = Hash(ix + 1, iy + 1, period, octave);
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        /// <summary><c>CV_Hash21</c> on the wrapped lattice point (each octave shifted so they do not line up).</summary>
        static float Hash(int ix, int iy, int period, int octave)
        {
            float x = ((ix % period) + period) % period + octave * 17.1f;
            float y = ((iy % period) + period) % period + octave * 9.7f;
            x = Frac(x * 123.34f); y = Frac(y * 456.21f);
            float d = x * (x + 45.32f) + y * (y + 45.32f);
            x += d; y += d;
            return Frac(x * y);
        }

        static float Frac(float v) => v - Mathf.Floor(v);
    }
}
