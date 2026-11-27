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

        /// <summary>
        /// Renderer options tried in a build without building it again:
        /// <c>-cvfeat Name=0,Name=1</c> switches renderer features off/on, <c>-cvssao Field=value,...</c> sets the SSAO
        /// feature's settings (Downsample, Source, Samples, BlurQuality, NormalSamples, Radius, Intensity...; enums by number),
        /// <c>-cvopaque 0/1</c> the opaque texture, <c>-cvaa off|fxaa|smaa0|smaa1|smaa2</c> the camera's anti-aliasing.
        /// </summary>
        static void ApplyRenderOptions()
        {
            const System.Reflection.BindingFlags Any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (asset == null) return;
            var opaque = Argument("-cvopaque");
            if (opaque != null) asset.supportsCameraOpaqueTexture = opaque == "1";
            var feat = Argument("-cvfeat");
            var ssao = Argument("-cvssao");
            if (feat != null || ssao != null)
            {
                var list = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_RendererDataList", Any)?.GetValue(asset) as UnityEngine.Rendering.Universal.ScriptableRendererData[];
                if (list != null)
                    foreach (var data in list)
                    {
                        if (data == null) continue;
                        foreach (var f in data.rendererFeatures)
                        {
                            if (f == null) continue;
                            if (feat != null)
                                foreach (var kv in feat.Split(','))
                                    if (kv.Split('=') is var p && p.Length == 2 && p[0] == f.name) f.SetActive(p[1] == "1");
                            if (ssao != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion")
                            {
                                var settings = f.GetType().GetField("m_Settings", Any)?.GetValue(f);
                                if (settings == null) continue;
                                foreach (var kv in ssao.Split(','))
                                {
                                    if (!(kv.Split('=') is var p) || p.Length != 2) continue;
                                    var field = settings.GetType().GetField(p[0], Any);
                                    if (field == null) continue;
                                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                                    object v = field.FieldType.IsEnum ? System.Enum.ToObject(field.FieldType, int.Parse(p[1], inv))
                                        : field.FieldType == typeof(bool) ? p[1] == "1"
                                        : System.Convert.ChangeType(p[1], field.FieldType, inv);
                                    field.SetValue(settings, v);
                                }
                            }
                        }
                        data.SetDirty();
                    }
            }
            var aa = Argument("-cvaa");
            if (aa != null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    var d = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    if (d == null) continue;
                    d.antialiasing = aa == "off" ? UnityEngine.Rendering.Universal.AntialiasingMode.None
                        : aa == "fxaa" ? UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing
                        : UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    if (aa.StartsWith("smaa") && aa.Length == 5) d.antialiasingQuality = (UnityEngine.Rendering.Universal.AntialiasingQuality)(aa[4] - '0');
                }
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
            // -cvoff Name,Name: every object of those names (and all under them) switched off, to see what each costs;
            // -cvoffmat Mat,Mat: every renderer drawing one of those materials hidden (a whole depth layer at once)
            var off = Argument("-cvoff");
            var offMat = Argument("-cvoffmat");
            if (off != null || offMat != null)
            {
                var names = new HashSet<string>((off ?? "").Split(','));
                var mats = new HashSet<string>((offMat ?? "").Split(','));
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (names.Contains(t.name)) t.gameObject.SetActive(false);
                    if (offMat != null)
                        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                            if (r.sharedMaterial != null && mats.Contains(r.sharedMaterial.name)) r.enabled = false;
                }
            }
            // -cvcombine Name,Name: those objects' meshes batched together (what static batching would save)
            var combine = Argument("-cvcombine");
            if (combine != null)
                foreach (var name in combine.Split(','))
                {
                    var go = GameObject.Find(name);
                    if (go != null) StaticBatchingUtility.Combine(go);
                }
            // -cvnoshadow: no light casts shadows (what the shadow maps cost)
            if (Argument("-cvnoshadow") != null || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cvnoshadow") >= 0)
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.shadows = LightShadows.None;
            ApplyRenderOptions();
            // -cvat X,Y: the hero stands there (next to a group of enemies) and cannot be hurt, so the enemies attack
            // for the whole run instead of killing him and sending him back to the start
            CrimsonVeil.Combat.PlayerHealth guard = null;
            var at = Argument("-cvat");
            if (at != null && at.Split(',') is var xy && xy.Length == 2
                && float.TryParse(xy[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ax)
                && float.TryParse(xy[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ay))
            {
                var hero = Object.FindAnyObjectByType<CrimsonVeil.Player.PlayerController>();
                if (hero != null)
                {
                    hero.Teleport(new Vector3(ax, ay, hero.transform.position.z));
                    guard = hero.GetComponent<CrimsonVeil.Combat.PlayerHealth>();
                }
            }
            // in a development build: what each part costs (the profiler's markers)
            var recs = new List<(string name, Unity.Profiling.ProfilerRecorder rec)>();
            if (Debug.isDebugBuild)
                foreach (var m in Markers) recs.Add((m, Unity.Profiling.ProfilerRecorder.StartNew(new Unity.Profiling.ProfilerCategory("Any"), m, 4)));
            var sums = new double[recs.Count];
            float settle = Time.realtimeSinceStartup + Settle;
            while (Time.realtimeSinceStartup < settle) { QualitySettings.vSyncCount = 0; guard?.Model?.GrantInvulnerability(1f); yield return null; }
            var dts = new List<float>();
            float end = Time.realtimeSinceStartup + Measure;
            while (Time.realtimeSinceStartup < end)
            {
                guard?.Model?.GrantInvulnerability(1f);
                yield return null;
                dts.Add(Time.unscaledDeltaTime);
                for (int i = 0; i < recs.Count; i++) if (recs[i].rec.Valid && recs[i].rec.Count > 0) sums[i] += recs[i].rec.LastValue * 1e-6;
            }
            // -cvshot file.png: the picture as it is now (to compare two builds' looks)
            var shot = Argument("-cvshot");
            if (shot != null)
            {
                ScreenCapture.CaptureScreenshot(shot);
                yield return null;
                yield return null;
            }
            dts.Sort();
            float At(float q) => dts[Mathf.Clamp(Mathf.RoundToInt(q * (dts.Count - 1)), 0, dts.Count - 1)] * 1000f;
            var sb = new StringBuilder();
            if (at != null) sb.AppendLine($"hero at {at}");
            sb.AppendLine($"scene {scene}, {dts.Count} frames in {Measure} s, {Screen.width}x{Screen.height}, quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            sb.AppendLine($"median {At(0.5f):F2} ms ({1000f / At(0.5f):F0} fps), p10 {At(0.1f):F2}, p90 {At(0.9f):F2}, p99 {At(0.99f):F2}, worst {At(1f):F2}, frames a second on average {dts.Count / Measure:F0}");
            sb.AppendLine($"display {Screen.currentResolution.width}x{Screen.currentResolution.height} at {Screen.currentResolution.refreshRateRatio.value:F0} Hz, {Screen.fullScreenMode}");
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
