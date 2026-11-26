using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The player's sound and picture settings: the volumes (overall, music, effects), the resolution, the window mode,
    /// vertical sync and the quality. Applied at start and whenever one changes, kept between sessions.
    /// <para>The game has no music or sounds yet: the overall volume drives the listener, and music and effects are
    /// read by whatever plays them (<see cref="MusicLevel"/>, <see cref="EffectsLevel"/>, <see cref="Changed"/>).</para>
    /// </summary>
    public static class GameSettings
    {
        public enum WindowMode { Window, Borderless, Fullscreen }

        public const float DefaultMaster = 1f, DefaultMusic = 0.8f, DefaultEffects = 0.8f;

        /// <summary>
        /// How loud a slider at <paramref name="v"/> (0..1) is played: its square, as the ear hears - a slider
        /// that scaled the sound straight took only 6 dB off at half way, and all the quiet end was crowded into its last
        /// tenth.
        /// </summary>
        public static float Heard(float v) => v * v;

        /// <summary>What the saved volumes mean: 2 since they are heard as <see cref="Heard"/>.</summary>
        public const int VolumesVersion = 2;
        const string Prefix = "CrimsonVeil.Settings.";

        public static float Master { get; private set; } = DefaultMaster;
        public static float Music { get; private set; } = DefaultMusic;
        public static float Effects { get; private set; } = DefaultEffects;
        public static WindowMode Mode { get; private set; } = WindowMode.Borderless;
        public static Vector2Int Resolution { get; private set; }
        public static bool VSync { get; private set; } = true;
        public static int Quality { get; private set; } = -1;

        /// <summary>Music's level with the overall volume in it (0..1), for whatever plays music.</summary>
        public static float MusicLevel => Heard(Music);
        /// <summary>The effects' level (0..1); the overall volume is applied by the listener.</summary>
        public static float EffectsLevel => Heard(Effects);

        /// <summary>Something was changed (the volumes, the picture).</summary>
        public static event Action Changed;

        /// <summary>Whether changes are written to the player's settings and applied to the screen (off in tests).</summary>
        public static bool Persist { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Load();
            Apply();
        }

        public static void SetMaster(float v) { Master = Clamp01(v); Commit(); }
        public static void SetMusic(float v) { Music = Clamp01(v); Commit(); }
        public static void SetEffects(float v) { Effects = Clamp01(v); Commit(); }
        public static void SetMode(WindowMode m) { Mode = m; Commit(); }
        public static void SetResolution(Vector2Int r) { Resolution = r; Commit(); }
        public static void SetVSync(bool on) { VSync = on; Commit(); }
        public static void SetQuality(int q) { Quality = Mathf.Clamp(q, 0, Mathf.Max(0, QualitySettings.names.Length - 1)); Commit(); }

        public static void ResetSound()
        {
            Master = DefaultMaster; Music = DefaultMusic; Effects = DefaultEffects;
            Commit();
        }

        public static void ResetPicture()
        {
            Mode = WindowMode.Borderless;
            Resolution = Native();
            VSync = true;
            Quality = Mathf.Max(0, QualitySettings.names.Length - 1);
            Commit();
        }

        static float Clamp01(float v) => float.IsNaN(v) ? 0f : Mathf.Clamp01(v);

        /// <summary>
        /// A setting was changed in the settings: the sound is heard at once (the volume can be tried); the picture and
        /// the saving wait for <see cref="Save"/> (the settings' Uložit), and <see cref="Restore"/> takes it all back.
        /// </summary>
        static void Commit()
        {
            AudioListener.volume = Heard(Master);
            Changed?.Invoke();
        }

        /// <summary>Every setting at one moment (what the settings are opened with, to go back to on Zrušit).</summary>
        public struct Snapshot
        {
            public float master, music, effects;
            public WindowMode mode;
            public Vector2Int resolution;
            public bool vsync;
            public int quality;
        }

        public static Snapshot Capture() => new Snapshot
        {
            master = Master, music = Music, effects = Effects, mode = Mode, resolution = Resolution, vsync = VSync, quality = Quality,
        };

        /// <summary>Back to <paramref name="s"/> (nothing is saved).</summary>
        public static void Restore(Snapshot s)
        {
            Master = s.master; Music = s.music; Effects = s.effects;
            Mode = s.mode; Resolution = s.resolution; VSync = s.vsync; Quality = s.quality;
            Commit();
        }

        /// <summary>Keeps the settings (Uložit): saved for the next time and the picture put into effect.</summary>
        public static void Keep()
        {
            Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Puts the settings into effect.</summary>
        public static void Apply()
        {
            AudioListener.volume = Heard(Master);
            // The picture only in the built game: in the editor the quality and vertical sync are the project's own
            // settings, and set from here they were written into it (ProjectSettings/QualitySettings.asset).
            if (!Persist || Application.isEditor) return;
            if (Quality >= 0 && Quality < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != Quality)
                QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            if (Resolution.x > 0 && Resolution.y > 0)
                Screen.SetResolution(Resolution.x, Resolution.y, ToUnity(Mode));
        }

        public static FullScreenMode ToUnity(WindowMode m)
            => m == WindowMode.Window ? FullScreenMode.Windowed
             : m == WindowMode.Fullscreen ? FullScreenMode.ExclusiveFullScreen
             : FullScreenMode.FullScreenWindow;

        public static string Label(WindowMode m)
            => m == WindowMode.Window ? "V okně" : m == WindowMode.Fullscreen ? "Celá obrazovka" : "Celá obrazovka bez okraje";

        public static string Label(Vector2Int r) => r.x + " × " + r.y;

        /// <summary>"80 %".</summary>
        public static string Percent(float v) => Mathf.RoundToInt(Clamp01(v) * 100f) + " %";

        /// <summary>The screen's own resolution (the largest it offers).</summary>
        public static Vector2Int Native()
        {
            var list = Resolutions();
            return list.Count > 0 ? list[list.Count - 1] : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        /// <summary>The resolutions the screen offers, each once (not once per refresh rate), smallest first.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var seen = new HashSet<Vector2Int>();
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (seen.Add(v)) list.Add(v);
            }
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        /// <summary>The next item round from <paramref name="current"/> (<paramref name="step"/> +1 / -1), wrapping.</summary>
        public static int Cycle(int current, int count, int step)
        {
            if (count <= 0) return 0;
            int i = (current + step) % count;
            return i < 0 ? i + count : i;
        }

        public static void Save()
        {
            if (!Persist) return;
            PlayerPrefs.SetFloat(Prefix + "Master", Master);
            PlayerPrefs.SetFloat(Prefix + "Music", Music);
            PlayerPrefs.SetFloat(Prefix + "Effects", Effects);
            PlayerPrefs.SetInt(Prefix + "Volumes", VolumesVersion);
            PlayerPrefs.SetInt(Prefix + "Mode", (int)Mode);
            PlayerPrefs.SetInt(Prefix + "Width", Resolution.x);
            PlayerPrefs.SetInt(Prefix + "Height", Resolution.y);
            PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Quality", Quality);
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            if (!Persist) { Defaults(); return; }
            Master = Clamp01(PlayerPrefs.GetFloat(Prefix + "Master", DefaultMaster));
            Music = Clamp01(PlayerPrefs.GetFloat(Prefix + "Music", DefaultMusic));
            Effects = Clamp01(PlayerPrefs.GetFloat(Prefix + "Effects", DefaultEffects));
            // (saved before the sliders followed the ear and the effects were turned down by default: those levels
            // meant something else - full effects were too loud - so the effects and music start again from the defaults)
            if (PlayerPrefs.GetInt(Prefix + "Volumes", 1) < VolumesVersion) { Effects = DefaultEffects; Music = DefaultMusic; }
            Mode = (WindowMode)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Mode", (int)WindowMode.Borderless), 0, 2);
            var native = Native();
            Resolution = new Vector2Int(PlayerPrefs.GetInt(Prefix + "Width", native.x), PlayerPrefs.GetInt(Prefix + "Height", native.y));
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
            Quality = PlayerPrefs.GetInt(Prefix + "Quality", QualitySettings.GetQualityLevel());
        }

        static void Defaults()
        {
            Master = DefaultMaster; Music = DefaultMusic; Effects = DefaultEffects;
            Mode = WindowMode.Borderless; Resolution = new Vector2Int(1920, 1080); VSync = true; Quality = 0;
        }

        /// <summary>Tests: defaults, nothing read or written, the screen left alone; <see cref="EndTests"/> undoes it.</summary>
        public static void BeginTests() { Persist = false; Defaults(); }
        public static void EndTests() { Persist = true; Load(); Apply(); }
    }
}
