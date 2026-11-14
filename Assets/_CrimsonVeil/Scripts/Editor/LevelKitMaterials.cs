using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CrimsonVeil.World;

namespace CrimsonVeil.EditorTools
{
    /// <summary>All shared materials, textures and render settings of the look (greys + crimson).</summary>
    public sealed class LevelKitMaterials
    {
        public const string Root = "Assets/_CrimsonVeil";
        public const string MatDir = Root + "/Materials";

        public Material inkFG, inkPlay, inkNear, inkMid, inkFar, inkSky, grass, banner, leaf, hero, wraith, wraithEyes;
        /// <summary>Solid crimson for the "Crimson" slot of modelled set pieces (fallen leaves, cloth on ruins).</summary>
        public Material crimson;
        /// <summary>Pure ink for the hero's shadow double (the creatures themselves are shaded grey: <see cref="wraith"/>).</summary>
        public Material shadowInk;
        public Material mistBand, mistLow, waterfall, shaft, moon;
        public Material veil, wisp, leafParticle, inkGrade;
        /// <summary>Simple particle materials (hit bursts, ropes) — the veil shader is for cloth only.</summary>
        public Material burst, rope;

        static LevelKitMaterials cached;

        public static LevelKitMaterials Get(bool refresh = false)
        {
            if (cached != null && !refresh && cached.inkPlay != null) return cached;
            cached = Create();
            return cached;
        }

        public Material ForLayer(LevelLayer layer)
        {
            switch (layer)
            {
                case LevelLayer.Foreground: return inkFG;
                case LevelLayer.Playfield: return inkPlay;
                case LevelLayer.Near: return inkNear;
                case LevelLayer.Mid: return inkMid;
                case LevelLayer.Far: return inkFar;
                default: return inkSky;
            }
        }

        static LevelKitMaterials Create()
        {
            var sil = Shader.Find("CrimsonVeil/Silhouette");
            var mist = Shader.Find("CrimsonVeil/Mist");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            Material Ink(string n, float v, float mistStrength, float grain, float cracks) =>
                Cracks(Sil(n, sil, new Color(v, v, v * 1.04f), mistStrength, grain), cracks);

            var maple = SpriteTexture("Maple", 128, MapleLeaf);
            var m = new LevelKitMaterials
            {
                inkFG = Ink("InkForeground", 0.025f, 0f, 0.3f, 0f),
                // The walkable rock: dark grey stone with grain, cracks and a moonlit top edge (key art), not a black slab.
                // Relief: carved stones, voussoirs and strata catch the moon on top and shade on their sides,
                // strongest up close and fading into the mist with distance.
                inkPlay = Relief(Edge(Ink("InkPlay", 0.075f, 0.55f, 0.5f, 0.55f), 0.16f), 1.1f),
                inkNear = Relief(Edge(Ink("InkNear", 0.26f, 1.2f, 0.5f, 0.55f), 0.06f), 0.75f),
                inkMid = Relief(Ink("InkMid", 0.46f, 1.2f, 0.45f, 0.45f), 0.45f),
                inkFar = Relief(Ink("InkFar", 0.6f, 1.1f, 0.4f, 0.3f), 0.25f),
                inkSky = Ink("InkSky", 0.7f, 0.6f, 0.3f, 0f),
                grass = Wind(Sil("GrassInk", sil, new Color(0.02f, 0.02f, 0.022f), 0.4f, 0.2f), 1f, false),
                // Cloth tones (folds, hem, woven band, scorched end) and the ink emblem come from the mesh.
                banner = Tinted(Wind(Sil("BannerCrimson", sil, new Color(0.66f, 0.015f, 0.03f), 0.3f, 0.6f), 1.6f, true)),
                crimson = Sil("RuinCrimson", sil, new Color(0.7f, 0.02f, 0.035f), 0.25f, 0.6f),
                leaf = Clip(Wind(Sil("LeafCrimson", sil, new Color(0.78f, 0.02f, 0.035f), 0.2f, 0.7f), 0.6f, false), maple),
                hero = Sil("HeroInk", sil, new Color(0.012f, 0.012f, 0.014f), 0.12f, 0.15f),
                // Creatures: dark grey, modelled by the moonlight, with a pale rim so they read on any background.
                wraith = Creature(Sil("WraithInk", sil, new Color(0.075f, 0.075f, 0.082f), 0.15f, 0.3f), 0.8f, 0.55f, 3f, 0.6f),
                shadowInk = Creature(Sil("ShadowInk", sil, new Color(0.006f, 0.006f, 0.008f), 0.15f, 0.25f), 0f, 0.22f, 4f, 0f),
                wraithEyes = Sil("WraithEyes", sil, new Color(1f, 0.02f, 0.04f), 0f, 0f),

                mistBand = MistMat("MistBand", mist, "_MODE_MIST", 0, new Color(0.8f, 0.8f, 0.8f), 0.55f, new Vector2(3f, 1.2f), new Vector2(0.025f, 0f), new Vector2(0.15f, 0.45f)),
                mistLow = MistMat("MistLow", mist, "_MODE_MIST", 0, new Color(0.78f, 0.78f, 0.78f), 0.4f, new Vector2(18f, 1.5f), new Vector2(0.01f, 0f), new Vector2(0.05f, 0.5f)),
                waterfall = MistMat("Waterfall", mist, "_MODE_WATERFALL", 2, new Color(0.88f, 0.88f, 0.88f), 0.85f, new Vector2(6f, 8f), new Vector2(0f, 1.1f), new Vector2(0.25f, 0.1f)),
                shaft = MistMat("LightShaft", mist, "_MODE_SHAFT", 1, Color.white, 0.16f, new Vector2(2f, 4f), new Vector2(0.01f, -0.02f), new Vector2(0.35f, 0.3f)),
                moon = Mat("Moon", unlit, new Color(3.4f, 3.4f, 3.3f)),

                veil = VeilMat("Veil", 0.45f, 0.75f, 0.6f, cloth: true),
                wisp = VeilMat("VeilSmoke", 0.9f, 0.95f, 0.5f, cloth: true), // red brush filaments, not grey smoke
                burst = Transparent(Mat("InkBurst", particles, new Color(1.1f, 0.05f, 0.06f, 1f))),
                rope = Transparent(Mat("Rope", particles, new Color(1.1f, 0.04f, 0.05f, 1f))),
                leafParticle = Transparent(Mat("Leaf", particles, new Color(0.9f, 0.03f, 0.05f, 1f))),
                inkGrade = Mat("InkGrade", Shader.Find("CrimsonVeil/InkGrade"), Color.white),
            };
            m.shaft.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.shaft.SetFloat("_DstBlend", (float)BlendMode.One);
            m.burst.SetTexture("_BaseMap", SpriteTexture("SoftDot", 64, SoftDot));
            m.leafParticle.SetTexture("_BaseMap", maple);
            AssetDatabase.SaveAssets();
            return m;
        }

        // ------------------------------------------------------------------ material helpers

        static Material Sil(string name, Shader shader, Color color, float mist, float grain)
        {
            var m = Mat(name, shader, color);
            m.SetFloat("_MistStrength", mist);
            m.SetFloat("_GrainStrength", grain);
            m.SetFloat("_EdgeLight", color.grayscale > 0.15f && color.r - color.g < 0.1f ? 0.03f : 0f);
            return m;
        }

        /// <summary>Creature look on the silhouette shader: moonlight form shading, a pale rim, darker feet.</summary>
        static Material Creature(Material m, float formShade, float rim, float rimWidth, float footShade)
        {
            m.SetFloat("_FormShade", formShade);
            m.SetFloat("_RimLight", rim);
            m.SetFloat("_RimWidth", rimWidth);
            m.SetColor("_RimColor", new Color(0.6f, 0.6f, 0.64f));
            m.SetFloat("_FootShade", footShade);
            m.SetFloat("_FootHeight", 2f);
            m.SetFloat("_EdgeLight", 0.05f);
            return m;
        }

        static Material VeilMat(string name, float edgeSoftness, float erosion, float flowSpeed, bool cloth = false)
        {
            var m = Mat(name, Shader.Find("CrimsonVeil/Veil"), Color.white);
            m.SetColor("_Deep", cloth ? new Color(0.34f, 0.0f, 0.025f) : new Color(0.6f, 0.02f, 0.04f)); // smoke: wine red, never grey
            m.SetColor("_Mid", new Color(0.78f, 0.02f, 0.04f));
            m.SetColor("_Glow", new Color(1.45f, 0.1f, 0.07f));
            m.SetFloat("_EdgeSoftness", edgeSoftness);
            m.SetFloat("_Erosion", erosion);
            m.SetFloat("_FlowSpeed", flowSpeed);
            // The loose scarf as in its animation sheet: pure crimson ink (sRGB ~0.80/0.02/0.03, light 1.0/0.04/0.04,
            // dark 0.36/0/0.01) - lighter only where it is translucent, never orange;
            // the smoke threads keep the fire look.
            m.SetFloat("_Cloth", cloth ? 1f : 0f);
            m.SetColor("_ClothDeep", new Color(0.36f, 0.0f, 0.01f));
            m.SetColor("_ClothMid", new Color(0.8f, 0.02f, 0.03f));
            m.SetColor("_ClothLight", new Color(1.0f, 0.04f, 0.04f));
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        /// <summary>Moonlight catching the upper surfaces (cliff tops, ledges, cornices).</summary>
        static Material Edge(Material m, float amount)
        {
            m.SetFloat("_EdgeLight", amount);
            return m;
        }

        /// <summary>Stone relief: moonlit tops and shaded sides of carved detail; flat camera-facing faces unchanged.</summary>
        static Material Relief(Material m, float amount)
        {
            m.SetFloat("_Relief", amount);
            m.SetFloat("_VertexTint", 1f); // masonry: stone shades and dark mortar come from the mesh
            return m;
        }

        static Material Tinted(Material m)
        {
            m.SetFloat("_VertexTint", 1f);
            return m;
        }

        static Material Cracks(Material m, float amount)
        {
            m.SetFloat("_Cracks", amount);
            m.SetFloat("_CrackScale", 1.1f);
            return m;
        }

        static Material Clip(Material m, Texture2D shape)
        {
            m.SetTexture("_MainTex", shape);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            return m;
        }

        static Material Wind(Material m, float weight, bool pinTop)
        {
            m.SetFloat("_Wind", 1f);
            m.EnableKeyword("_WIND_ON");
            m.SetFloat("_WindWeight", weight);
            m.SetFloat("_PinTop", pinTop ? 1f : 0f);
            m.SetFloat("_Cull", 0f);
            return m;
        }

        static Material MistMat(string name, Shader shader, string keyword, int mode, Color color, float opacity, Vector2 scale, Vector2 scroll, Vector2 edge)
        {
            var m = Mat(name, shader, color);
            m.SetColor("_Color", color);
            m.SetFloat("_Mode", mode);
            foreach (var k in new[] { "_MODE_MIST", "_MODE_SHAFT", "_MODE_WATERFALL" }) m.DisableKeyword(k);
            m.EnableKeyword(keyword);
            m.SetFloat("_Opacity", opacity);
            m.SetVector("_NoiseScale", scale);
            m.SetVector("_Scroll", scroll);
            m.SetVector("_EdgeSoftness", edge);
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        static Material Mat(string name, Shader shader, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Transparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ textures

        static float SoftDot(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
        }

        /// <summary>Five-lobed maple leaf with serrated edges and a short stem (like the key art).</summary>
        public static float MapleLeaf(float x, float y)
        {
            // leaf points up; stem at the bottom
            y += 0.12f;
            float a = Mathf.Atan2(x, y);              // 0 = up
            float r = Mathf.Sqrt(x * x + y * y);
            float lobes = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f)), 0.6f);    // 5 lobes over the circle
            float side = Mathf.Clamp01(1f - Mathf.Abs(a) / 2.6f);           // no lobes at the stem
            float serr = 0.035f * Mathf.Abs(Mathf.Sin(a * 22f));
            float edge = (0.32f + 0.55f * lobes * side) - serr;
            float leafA = Mathf.Clamp01((edge - r) * 30f);
            float stem = (Mathf.Abs(x) < 0.025f && y < 0.05f && y > -0.25f) ? 1f : 0f;
            return Mathf.Max(leafA, stem);
        }

        static Texture2D SpriteTexture(string name, int size, System.Func<float, float, float> alpha)
        {
            string path = $"{MatDir}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(u, v)));
            }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ rendering setup

        public static void InstallInkGrade(Material material)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f != null && f.name == "InkGrade");
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    feature.name = "InkGrade";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
                    var so = new SerializedObject(data);
                    var list = so.FindProperty("m_RendererFeatures");
                    var map = so.FindProperty("m_RendererFeatureMap");
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                feature.passMaterial = material;
                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                feature.fetchColorBuffer = true;
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
        }

        public static VolumeProfile VolumeProfile()
        {
            string path = Root + "/Settings/CrimsonVeil_Volume.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) return existing;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            Add<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.75f);
            Add<ColorAdjustments>(profile).contrast.Override(10f);
            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.42f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(Color.black);
            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium4);
            grain.intensity.Override(0.22f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }
    }
}
