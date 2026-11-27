---
name: unity-memory-watchdog
description: Unity ballooned to 16-49 GB (disk C filled via pagefile); garbage test fixed; run long Unity jobs under the watchdog; restart Unity myself when hung
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-21T20:51:28.975Z
---

2026-11-21: Unity grew to 35-49 GB and stopped responding twice; Windows paged it onto C: (free space fell to 4.5 GB). Cause: the PlayMode test TheHero_AllocatesNoGarbage_FrameByFrame enabled the profiler and the editor kept every recorded frame (no Profiler window open). Fixed in v0.57.0 (ClearAllFrames after each read); a full PlayMode run now peaks ~3 GB.

The user was angry ("Jako uz zase kurva?") that I re-ran the test after the first hang, and told me to kill and restart Unity myself instead of asking them ("Ja tam nebudu chodit. Ukonci si ho sam a zresrartuj ho").

**Why:** a hung, ballooning Unity eats the disk and costs the user time; they don't want to do the clicking.

**How to apply:**
- Run long Unity jobs (full PlayMode runs, gallery recording) with the watchdog: `powershell -File <scratchpad>\watchdog.ps1 -LimitGB 12` in the background (kills Unity above the limit; log in watchdog.txt).
- If Unity hangs or balloons: Stop-Process it, start `C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe -projectPath "C:\Dev\CrimsonVeil"`, answer the "Recovering Scene Backups" dialog via UI Automation (SetProcessDPIAware before clicking; "No" for backups of test scenes), wait for the title "Unity 6".
- Delete leftover `Assets/InitTestScene*.unity` from killed test runs; never commit `Assets/_Recovery`.
- Never re-run a test that just hung Unity without first finding why.
- Do not delete the scratchpad folder (user said no). Related: [[unity-mcp-setup]], [[unity-check-play-before-refresh]].
