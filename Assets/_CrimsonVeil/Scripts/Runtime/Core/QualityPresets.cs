using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The picture's quality levels, as PC games commonly offer them: a few presets from low to high, the one that suits
    /// the graphics card chosen on the first start (and marked "doporučeno" in the menu), the player free to pick
    /// another. Each level is a Unity quality level with its own URP asset (ProjectSettings/QualitySettings.asset):
    /// <list type="bullet">
    /// <item><b>Vysoká</b> - everything: ambient occlusion (half resolution), SMAA, four shadow cascades.</item>
    /// <item><b>Střední</b> - integrated graphics: no ambient occlusion, FXAA, two cascades. Full resolution.</item>
    /// <item><b>Nízká</b> - weak graphics: as Střední, drawn at 80 % and stretched to the screen (bilinear: FSR's
    /// sharpening cost more than the smaller picture saved on an Intel UHD), one cascade.</item>
    /// </list>
    /// The ink look (grain, cracks, mist, paper, the crimson) is the same at every level.
    /// <para><b>Vlastní</b> - the player's own mix of the four things the presets differ in (<see cref="Detail"/>):
    /// a quality level of its own with its own URP asset and renderer (Custom_RPAsset, Custom_Renderer), changed at run
    /// time by <see cref="ApplyDetail"/>, so the presets' assets are never touched. Changing any one item in the menu
    /// makes the quality Vlastní; picking a preset shows that preset's items.</para>
    /// </summary>
    public static class QualityPresets
    {
        public const string Low = "Nízká", Medium = "Střední", High = "Vysoká", Custom = "Vlastní";

        /// <summary>What the presets differ in, and what Vlastní lets the player set one by one.</summary>
        public struct Detail
        {
            /// <summary>Screen-space ambient occlusion (the shading where things meet; ~2-3 ms on an Intel UHD).</summary>
            public bool ambientOcclusion;
            /// <summary>None, FXAA or SMAA.</summary>
            public AntialiasingMode antiAliasing;
            /// <summary>The share of the screen's resolution the picture is drawn at (one of <see cref="RenderScales"/>).</summary>
            public float renderScale;
            /// <summary>0 low (one cascade, 1024), 1 medium (two, 1024), 2 high (four, 2048).</summary>
            public int shadows;

            public override string ToString() => $"AO {ambientOcclusion}, {antiAliasing}, {renderScale:0.##}, shadows {shadows}";
        }

        /// <summary>The drawing scales offered (below 75 % the picture turns mushy).</summary>
        public static readonly float[] RenderScales = { 0.75f, 0.8f, 0.9f, 1f };
        /// <summary>The anti-aliasing modes offered, in the menu's order.</summary>
        public static readonly AntialiasingMode[] AntiAliasings =
            { AntialiasingMode.None, AntialiasingMode.FastApproximateAntialiasing, AntialiasingMode.SubpixelMorphologicalAntiAliasing };
        public static readonly string[] AntiAliasingLabels = { "Vypnuto", "FXAA (rychlé)", "SMAA (ostré)" };
        public static readonly string[] ShadowLabels = { "Nízké", "Střední", "Vysoké" };
        static readonly int[] ShadowCascades = { 1, 2, 4 }, ShadowResolutions = { 1024, 1024, 2048 };

        /// <summary>The items of a preset, as its URP asset has them (QualityPresetTests keeps the two the same).</summary>
        public static Detail Preset(string level)
        {
            level = Label(level);
            if (level == Low || level == Low + " (mobil)")
                return new Detail { ambientOcclusion = false, antiAliasing = AntialiasingMode.FastApproximateAntialiasing, renderScale = 0.8f, shadows = 0 };
            if (level == Medium)
                return new Detail { ambientOcclusion = false, antiAliasing = AntialiasingMode.FastApproximateAntialiasing, renderScale = 1f, shadows = 1 };
            return new Detail { ambientOcclusion = true, antiAliasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing, renderScale = 1f, shadows = 2 };
        }

        /// <summary><paramref name="d"/> with every item one that is offered (a damaged or old save).</summary>
        public static Detail Sanitize(Detail d)
        {
            float best = 1f;
            foreach (var s in RenderScales) if (Mathf.Abs(s - d.renderScale) < Mathf.Abs(best - d.renderScale)) best = s;
            d.renderScale = float.IsNaN(d.renderScale) ? 1f : best;
            d.shadows = Mathf.Clamp(d.shadows, 0, ShadowCascades.Length - 1);
            if (System.Array.IndexOf(AntiAliasings, d.antiAliasing) < 0) d.antiAliasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return d;
        }

        public static int ShadowCascadesOf(int shadows) => ShadowCascades[Mathf.Clamp(shadows, 0, 2)];
        public static int ShadowResolutionOf(int shadows) => ShadowResolutions[Mathf.Clamp(shadows, 0, 2)];

        /// <summary>The shadow item (0..2) an asset's shadows are, by its cascades.</summary>
        public static int ShadowsOf(UniversalRenderPipelineAsset a)
            => a.shadowCascadeCount >= 4 ? 2 : a.shadowCascadeCount >= 2 ? 1 : 0;

        /// <summary>The renderer feature that draws the ambient occlusion in <paramref name="a"/>'s renderer (null: none).</summary>
        public static ScriptableRendererFeature AmbientOcclusionOf(UniversalRenderPipelineAsset a)
        {
            if (a == null || a.rendererDataList.Length == 0 || a.rendererDataList[0] == null) return null;
            return a.rendererDataList[0].rendererFeatures.Find(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
        }

        /// <summary>Puts <paramref name="d"/> into Vlastní's URP asset (the anti-aliasing goes on the cameras).</summary>
        public static void ApplyDetail(UniversalRenderPipelineAsset a, Detail d)
        {
            if (a == null) return;
            d = Sanitize(d);
            a.renderScale = d.renderScale;
            // stretched bilinearly: FSR's sharpening cost more than the smaller picture saved on an Intel UHD
            a.upscalingFilter = d.renderScale < 1f ? UpscalingFilterSelection.Linear : UpscalingFilterSelection.Auto;
            a.shadowCascadeCount = ShadowCascadesOf(d.shadows);
            a.mainLightShadowmapResolution = ShadowResolutionOf(d.shadows);
            var ao = AmbientOcclusionOf(a);
            if (ao != null) ao.SetActive(d.ambientOcclusion);
        }

        /// <summary>The level the menu calls <paramref name="name"/> ("PC" was the high level's name until v0.106).</summary>
        public static string Label(string name) => name == "PC" ? High : name == "Mobile" ? Low + " (mobil)" : name;

        /// <summary>Anti-aliasing at a level: SMAA on Vysoká, the cheaper FXAA below (SMAA was ~1 ms on an Intel UHD).</summary>
        public static AntialiasingMode AntiAliasing(string level) =>
            Label(level) == Custom ? Sanitize(GameSettings.Custom).antiAliasing : Preset(level).antiAliasing;

        /// <summary>The level this computer's graphics card is offered first.</summary>
        public static string Recommended => Recommend(SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize, SystemInfo.graphicsShaderLevel);

        static readonly Regex AmdMobileIgpu = new Regex(@"\b\d{3}m\b");

        /// <summary>
        /// Integrated graphics (Intel UHD / Iris, AMD Radeon Graphics / Vega / 680M-890M) get Střední, the old Intel HD
        /// Graphics and anything below shader model 4.5 Nízká, the rest (a graphics card of its own) Vysoká.
        /// </summary>
        public static string Recommend(string gpu, int memoryMB, int shaderLevel)
        {
            if (shaderLevel > 0 && shaderLevel < 45) return Low;
            gpu = (gpu ?? "").ToLowerInvariant();
            bool intel = gpu.Contains("intel") && !gpu.Contains("arc");
            if (intel && gpu.Contains("hd graphics") && !gpu.Contains("uhd")) return Low;
            bool amd = gpu.Contains("radeon") && !gpu.Contains(" rx") && !gpu.Contains("pro") && !gpu.Contains("radeon vii")
                && (gpu.Contains("radeon(tm) graphics") || gpu.Contains("radeon graphics") || (gpu.Contains("vega") && gpu.Contains("graphics"))
                    || AmdMobileIgpu.IsMatch(gpu));
            if (intel || amd) return Medium;
            if (memoryMB > 0 && memoryMB < 1500) return Medium;
            return High;
        }

        /// <summary>The index of <paramref name="level"/> among the quality levels (-1: none of that name).</summary>
        public static int IndexOf(string level)
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++) if (names[i] == level || Label(names[i]) == Label(level)) return i;
            return -1;
        }

        /// <summary>The name of the level now in effect.</summary>
        public static string Current
        {
            get
            {
                var names = QualitySettings.names;
                int q = QualitySettings.GetQualityLevel();
                return q >= 0 && q < names.Length ? Label(names[q]) : High;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            if (!Application.isEditor) ApplyToCameras();
        }

        static readonly System.Collections.Generic.HashSet<int> Smoothed = new System.Collections.Generic.HashSet<int>();

        /// <summary>The items in effect: Vlastní's own, or the preset's.</summary>
        public static Detail CurrentDetail => Current == Custom ? Sanitize(GameSettings.Custom) : Preset(Current);

        /// <summary>Gives every camera the level's anti-aliasing (it lives on the camera, not in the URP asset).</summary>
        public static void ApplyToCameras()
        {
            var aa = AntiAliasing(Current);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var d = cam.GetComponent<UniversalAdditionalCameraData>();
                if (d == null) continue;
                // (a camera that never smoothed keeps that; one this turned off - Vlastní without anti-aliasing - is
                // remembered, so turning it back on reaches it again)
                int id = cam.GetInstanceID();
                if (d.antialiasing == AntialiasingMode.None && !Smoothed.Contains(id)) continue;
                Smoothed.Add(id);
                d.antialiasing = aa;
                if (aa == AntialiasingMode.SubpixelMorphologicalAntiAliasing) d.antialiasingQuality = AntialiasingQuality.High;
            }
        }
    }
}
