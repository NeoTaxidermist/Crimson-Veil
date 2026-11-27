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
        /// <summary>The mist bands, light shafts and waterfalls read the same grain (they worked out two fbm a pixel
        /// over eight screen-wide bands: ~1.9 ms of a 20 ms frame on an Intel UHD at 1080p).</summary>
        public const string MistShaderPath = "Assets/_CrimsonVeil/Shaders/Mist.shader";
        public const string Property = "_CVNoise";

        /// <summary>The final grade's paper fibres (two octaves of value noise per screen pixel), baked the same way.</summary>
        public const string PaperPath = "Assets/_CrimsonVeil/Shaders/CVPaper.png";
        public const string GradeShaderPath = "Assets/_CrimsonVeil/Shaders/InkGrade.shader";
        public const string PaperProperty = "_CVPaper";
        /// <summary>Paper texture: texels a side, cells of the fine octave it spans, the coarse octave's period (the
        /// shader's 0.13 rounded to 1/8 so it wraps too).</summary>
        public const int PaperSize = 1024, PaperPeriod = 256, PaperCoarsePeriod = 32;

        /// <summary>
        /// The stone cracks: the distance to the nearest Voronoi cell border (CV_VoronoiEdge on a warped lattice), worked
        /// out per pixel it cost 3.3 ms of a 16.6 ms frame on an Intel UHD at 1080p. A distance field stays sharp when
        /// magnified (bilinear between texels follows the line). <see cref="CrackPeriod"/> cells across, 32 texels each;
        /// the warp reads the grain at half the crack scale (was 0.7) so it wraps with the tile.
        /// </summary>
        public const string CracksPath = "Assets/_CrimsonVeil/Shaders/CVCracks.png";
        public const string CracksProperty = "_CVCracks";
        public const int CracksSize = 2048, CrackPeriod = 64;
        /// <summary>Distances are stored up to this (cracks only use the first 0.06), for 8-bit precision near the line.</summary>
        public const float CrackRange = 0.5f;

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
            BakePaper();
            BakeCracks();
            Configure();
        }

        /// <summary>The crack distance field (see <see cref="CracksPath"/>).</summary>
        public static void BakeCracks()
        {
            var px = new byte[CracksSize * CracksSize];
            float k = (float)CrackPeriod / CracksSize;
            System.Threading.Tasks.Parallel.For(0, CracksSize, y =>
            {
                for (int x = 0; x < CracksSize; x++)
                {
                    float e = CrackEdge((x + 0.5f) * k, (y + 0.5f) * k);
                    px[y * CracksSize + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(e, CrackRange) / CrackRange * 255f), 0, 255);
                }
            });
            var tex = new Texture2D(CracksSize, CracksSize, TextureFormat.R8, false, true);
            tex.SetPixelData(px, 0);
            tex.Apply();
            File.WriteAllBytes(CracksPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CracksPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>The crack distance at <paramref name="cx"/>,<paramref name="cy"/> (crack cells), wrapping every <see cref="CrackPeriod"/>.</summary>
        public static float CrackEdge(float cx, float cy)
        {
            // warp: the grain at half scale (period 32 there = 64 cells here), shifted by 0.6 as in the shader
            float w = Fbm(cx * 0.5f, cy * 0.5f) * 0.6f;
            return VoronoiEdge(cx + w, cy + w, CrackPeriod);
        }

        /// <summary>CV_VoronoiEdge on a lattice wrapped at <paramref name="period"/>.</summary>
        static float VoronoiEdge(float px, float py, int period)
        {
            int gx = Mathf.FloorToInt(px), gy = Mathf.FloorToInt(py);
            float fx = px - gx, fy = py - gy;
            float d1 = 8f, d2 = 8f;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = (((gx + ox) % period) + period) % period, cy = (((gy + oy) % period) + period) % period;
                    float rx = ox + Hash21(cx, cy) - fx;
                    float ry = oy + Hash21(cx + 17.3f, cy + 17.3f) - fy;
                    float d = rx * rx + ry * ry;
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) d2 = d;
                }
            return Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
        }

        /// <summary>CV_Hash21.</summary>
        static float Hash21(float x, float y)
        {
            x = Frac(x * 123.34f); y = Frac(y * 456.21f);
            float d = x * (x + 45.32f) + y * (y + 45.32f);
            x += d; y += d;
            return Frac(x * y);
        }

        /// <summary>The paper fibres: 0.6 of value noise at the fibre scale + 0.4 at an eighth of it, both wrapping.</summary>
        public static void BakePaper()
        {
            var px = new byte[PaperSize * PaperSize];
            float k = (float)PaperPeriod / PaperSize;
            for (int y = 0; y < PaperSize; y++)
                for (int x = 0; x < PaperSize; x++)
                {
                    float u = (x + 0.5f) * k, v = (y + 0.5f) * k;
                    float p = ValueNoise(u, v, PaperPeriod, 0) * 0.6f + ValueNoise(u * 0.125f, v * 0.125f, PaperCoarsePeriod, 1) * 0.4f;
                    px[y * PaperSize + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(p * 255f), 0, 255);
                }
            var tex = new Texture2D(PaperSize, PaperSize, TextureFormat.R8, false, true);
            tex.SetPixelData(px, 0);
            tex.Apply();
            File.WriteAllBytes(PaperPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PaperPath, ImportAssetOptions.ForceUpdate);
        }

        static void ConfigureTexture(string path, int size)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
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
            ti.maxTextureSize = size;
            ti.SaveAndReimport();
        }

        static void Bind(string shaderPath, string property, string texturePath) => Bind(shaderPath, new[] { property }, new[] { texturePath });

        static void Bind(string shaderPath, string[] properties, string[] texturePaths)
        {
            var si = (ShaderImporter)AssetImporter.GetAtPath(shaderPath);
            var tex = new Texture[texturePaths.Length];
            for (int i = 0; i < tex.Length; i++) tex[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[i]);
            si.SetNonModifiableTextures(properties, tex);
            si.SaveAndReimport();
        }

        /// <summary>Import settings (linear, one channel, no mips, tiling, bilinear) and the shader's binding.</summary>
        public static void Configure()
        {
            ConfigureTexture(TexturePath, Size);
            ConfigureTexture(PaperPath, PaperSize);
            ConfigureTexture(CracksPath, CracksSize);
            Bind(ShaderPath, new[] { Property, CracksProperty }, new[] { TexturePath, CracksPath });
            Bind(MistShaderPath, Property, TexturePath);
            Bind(GradeShaderPath, PaperProperty, PaperPath);
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
