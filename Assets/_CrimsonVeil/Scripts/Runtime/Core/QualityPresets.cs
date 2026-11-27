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
    /// </summary>
    public static class QualityPresets
    {
        public const string Low = "Nízká", Medium = "Střední", High = "Vysoká";

        /// <summary>The level the menu calls <paramref name="name"/> ("PC" was the high level's name until v0.106).</summary>
        public static string Label(string name) => name == "PC" ? High : name == "Mobile" ? Low + " (mobil)" : name;

        /// <summary>Anti-aliasing at a level: SMAA on Vysoká, the cheaper FXAA below (SMAA was ~1 ms on an Intel UHD).</summary>
        public static AntialiasingMode AntiAliasing(string level) =>
            level == Low || level == Medium ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;

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

        /// <summary>Gives every camera the level's anti-aliasing (it lives on the camera, not in the URP asset).</summary>
        public static void ApplyToCameras()
        {
            var aa = AntiAliasing(Current);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var d = cam.GetComponent<UniversalAdditionalCameraData>();
                if (d == null || d.antialiasing == AntialiasingMode.None) continue; // (a camera that never smoothed keeps that)
                d.antialiasing = aa;
                if (aa == AntialiasingMode.SubpixelMorphologicalAntiAliasing) d.antialiasingQuality = AntialiasingQuality.High;
            }
        }
    }
}
