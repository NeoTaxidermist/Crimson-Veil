using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// Measures the built game, not the editor: started with <c>-cvbench &lt;file&gt;</c> it loads the game scene,
    /// lets it settle, measures every frame's time for a few seconds (vsync and any frame cap off), writes the
    /// median and the spread to the file and quits. Without the argument it does nothing at all.
    /// <para>(The editor's own work and its checks sit on every frame measured inside it: a build's numbers are the
    /// ones a player sees.)</para>
    /// </summary>
    public sealed class Benchmark : MonoBehaviour
    {
        /// <summary>What a development build reports part by part (the engine's markers and ours).</summary>
        static readonly string[] Markers =
        {
            "PlayerLoop", "Main Thread", "CPU Main Thread Frame Time", "CPU Render Thread Frame Time", "GPU Frame Time",
            "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate", "FixedUpdate.PhysicsFixedUpdate",
            "PostLateUpdate.FinishFrameRendering", "Inl_UniversalRenderPipeline.RenderSingleCameraInternal", "RenderPipelineManager.DoRenderLoop_Internal",
            "Inl_MainLightShadow", "MainLightShadow", "Inl_ScriptableRenderer.Execute", "CullScriptable", "Culling", "UGUI.Rendering.UpdateBatches",
            "PostLateUpdate.PlayerUpdateCanvases", "PostLateUpdate.UpdateAllRenderers", "MeshSkinning.Update", "Gfx.WaitForPresentOnGfxThread",
            "Gfx.WaitForGfxCommandsFromMainThread", "Physics.Simulate", "ParticleSystem.Update", "Audio.Update",
            "CV.Player", "CV.HeroAnimator", "CV.HeroAnimator.Walls", "CV.HeroMocap", "CV.HeroWallTouchUp", "CV.SpringBones", "CV.Veil",
            "CV.VeilRenderer", "CV.HeroFace", "CV.EnemyAnimator",
        };

        /// <summary>Seconds left to settle, then measured.</summary>
        public const float Settle = 3f, Measure = 8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            string file = Argument("-cvbench");
            if (file == null) return;
            var go = new GameObject("Benchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<Benchmark>().StartCoroutine(go.GetComponent<Benchmark>().Run(file));
        }

        /// <summary>The value after <paramref name="name"/> on the command line (null: not given).</summary>
        public static string Argument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        IEnumerator Run(string file)
        {
            string scene = Argument("-cvscene") ?? "Prototype_Ruins";
            if (SceneManager.GetActiveScene().name != scene) SceneManager.LoadScene(scene);
            yield return null;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            // -cvres 960x540: another resolution (twice the frame at a quarter of the pixels says the GPU bounds it)
            var res = Argument("-cvres");
            if (res != null && res.Split('x') is var wh && wh.Length == 2 && int.TryParse(wh[0], out int w) && int.TryParse(wh[1], out int h))
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            // -cvoff Name,Name: those objects (and all under them) switched off, to see what each costs
            var off = Argument("-cvoff");
            if (off != null)
                foreach (var name in off.Split(','))
                {
                    var go = GameObject.Find(name);
                    if (go != null) go.SetActive(false);
                }
            // -cvnoshadow: no light casts shadows (what the shadow maps cost)
            if (Argument("-cvnoshadow") != null || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cvnoshadow") >= 0)
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.shadows = LightShadows.None;
            // in a development build: what each part costs (the profiler's markers)
            var recs = new List<(string name, Unity.Profiling.ProfilerRecorder rec)>();
            if (Debug.isDebugBuild)
                foreach (var m in Markers) recs.Add((m, Unity.Profiling.ProfilerRecorder.StartNew(new Unity.Profiling.ProfilerCategory("Any"), m, 4)));
            var sums = new double[recs.Count];
            float settle = Time.realtimeSinceStartup + Settle;
            while (Time.realtimeSinceStartup < settle) { QualitySettings.vSyncCount = 0; yield return null; }
            var dts = new List<float>();
            float end = Time.realtimeSinceStartup + Measure;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                dts.Add(Time.unscaledDeltaTime);
                for (int i = 0; i < recs.Count; i++) if (recs[i].rec.Valid && recs[i].rec.Count > 0) sums[i] += recs[i].rec.LastValue * 1e-6;
            }
            dts.Sort();
            float At(float q) => dts[Mathf.Clamp(Mathf.RoundToInt(q * (dts.Count - 1)), 0, dts.Count - 1)] * 1000f;
            var sb = new StringBuilder();
            sb.AppendLine($"scene {scene}, {dts.Count} frames in {Measure} s, {Screen.width}x{Screen.height}, quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            sb.AppendLine($"median {At(0.5f):F2} ms ({1000f / At(0.5f):F0} fps), p10 {At(0.1f):F2}, p90 {At(0.9f):F2}, p99 {At(0.99f):F2}, worst {At(1f):F2}");
            sb.AppendLine($"{SystemInfo.processorType}, {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}, Unity {Application.unityVersion}");
            for (int i = 0; i < recs.Count; i++)
            {
                if (sums[i] > 0.002 * dts.Count) sb.AppendLine($"  {recs[i].name,-62} {sums[i] / dts.Count:F3} ms");
                recs[i].rec.Dispose();
            }
            System.IO.File.WriteAllText(file, sb.ToString());
            Application.Quit();
        }
    }
}
