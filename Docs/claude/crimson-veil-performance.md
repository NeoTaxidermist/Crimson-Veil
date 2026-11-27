---
name: crimson-veil-performance
description: "User wants ~250 fps; how to measure the built game (Benchmark -cvbench), the v0.98 breakdown, and the levers (IL2CPP module missing, Burst for the scarf)"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-27T08:13:09.174Z
---

2026-11-26: user expects ~250 fps for this game ("brutal room for optimisation"). Measure BUILDS, not the editor (editor adds ~4 ms).
- Build via execute_code BuildPipeline.BuildPlayer to C:\_ClaudeAI\unity\build (release) / build_dev (Development); run with PowerShell script C:\_ClaudeAI\unity\build\bench2.ps1 (`-cvbench file`, `-cvres WxH`, `-cvoff Name,Name`, `-cvnoshadow`; dev build prints marker breakdown). Bash breaks on non-ASCII args (Terén): use PowerShell.
- v0.98 numbers (1080p, i7-9750H/RTX 2060): 6.0 ms (166 fps) D3D11, CPU-bound (quarter resolution same). Hero+scarf 2 ms (4.0 ms without). Dev build: scarf drawing VeilRenderer 2.4 ms (9 strands x ~137 points, math not obstacle checks), scarf sim 1.0, HeroAnimator 0.73, SpringBones 0.49, mocap 0.31, UI canvases 0.48 (rebuilt every frame), render submission 1.43.
- v0.99: IL2CPP installed (Hub CLI `install-modules -m windows-il2cpp` works from here; needed Windows SDK 10.0.26100 via VS installer `modify --add Microsoft.VisualStudio.Component.Windows11SDK.26100 --passive` with -Verb RunAs; user approves UAC). Build is IL2CPP + D3D11 first: 240 fps (4.15 ms); hero 0.14 ms; remaining base ~4 ms = render submission + UI canvases. run_tests has init_timeout (use 120000 for PlayMode - earlier "failed to initialize" was this).
- v0.100 (2026-11-27): 2.8 ms (~350 fps) windowed 1080p. Win was baking CV_Fbm into Shaders/CVNoise.png (shader non-modifiable texture, NoiseTextureBaker) - per-pixel noise was 1.7 ms. Method that found it: -cvoff/-cvoffmat at 1080p vs 540p (cost gone at low res = pixel cost), then a temp build with noise stubbed out. Measure WINDOWED (-cvres 1920x1080): fullscreen paces to 144 Hz. Compare looks via editor Camera.Render of the same frame (live game shots differ). Rejected: GRD (slower), static batching (0.15 ms), depth priming Forced (Silhouette objects vanished). CPU floor ~2.3 ms now (everything off at 540p).
- (old) Levers: IL2CPP needs Unity Hub module "Windows Build Support (IL2CPP)" (not installed; VS C++ present) - user must install; Burst is present (URP dependency) - scarf to Burst jobs is the big code task.
- **DELL (2026-11-27, v0.100.19 build, i5-11500H + Intel UHD, on AC):** IL2CPP + D3D11 release build, scene Prototype_Ruins, windowed. **1080p 17.1 ms (58 fps), 720p 9.0 ms (111), 540p 6.4 ms (156).** Pixel-bound on the iGPU: ~6.9 ms per megapixel on top of ~2.8 ms. Not a vsync cap (17.1, not 16.7, and it scales with resolution). Breakdown at 1080p (switch off -> new median):
  - whole `Level` off -> 8.4 ms; also Atmosphere, Global Volume and Player off -> still ~7.2-7.7 ms. **~7 ms are fixed full-screen passes** of PC_Renderer, not scene content.
  - **SSAO renderer feature: 2.8 ms** (temp build without it: 14.3 ms 1080p, 5.3 ms 540p). Not yet split: InkGrade full-screen pass, opaque texture copy (2x bilinear), depth copy after opaques, HDR 32-bit buffer.
  - `Dekorace` off -> 11.0 ms (-6 ms): layered background (InkMid -1.4, InkFar -0.5, Mist+LightShaft+Waterfall -1.0, Grass+Foreground -0.4, Wraith materials -0.4). Player -1.6 ms.
  - Switching OPAQUE fore layers off makes it SLOWER (Terén +0.6, InkPlay +2.1, InkNear +0.4): they hide expensive layers behind them -> **overdraw of the background layers** is a real cost on the iGPU.
  - Shadows off: no change at 1080p (GPU bound elsewhere).
  - 540p frames alternate ~3.2 / ~9.5 ms (p10/p90) - not investigated.
- **Measuring pitfalls (found on the DELL):** `Tools/bench.ps1` always passes `-cvres` and `Benchmark.Argument` takes the FIRST occurrence, so `-Extra "-cvres",...` was silently ignored - use `-Res` (added v0.100.20). A PowerShell 5.1 script file without BOM mangles non-ASCII object names (`Terén`): write them as `"Ter$([char]0xE9)n"`. Names with a space (`Global Volume`) need inner quotes through Start-Process. A temp build that disables a renderer feature followed by `AssetDatabase.SaveAssets` rewrites the shader-keyword prefilter in `PC_RPAsset.asset` / `UniversalRenderPipelineGlobalSettings.asset` (SSAO variants would be stripped from later builds): `git checkout` both files and re-import afterwards.
See [[crimson-veil-enemy-rebuild]] (dormancy, FixBounds in v0.97).
