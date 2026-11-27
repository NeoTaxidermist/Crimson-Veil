---
name: unity-batchmode-opens-last-project
description: Never create Unity projects via batchmode -createProjectPath here; it opened and upgraded the last-opened project instead
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-13T07:44:36.708Z
---

Running `Unity.exe -batchmode -quit -createProjectPath C:\Dev\NewGame -cloneFromTemplate <tgz>` (6000.3.28f1) did not create the project: Unity appended the last-opened project path (`C:/Dev/Unity/swarm-game/Swarm game`) and opened that instead, upgrading Swarm game from 6000.0.63f1 to 6.3. The user decided to keep Swarm game on 6.3 (2026-11-13).

**Why:** it silently modified an unrelated project the user had explicitly chosen not to upgrade.
**How to apply:** have the user create new projects in Unity Hub. Before any batchmode Unity run, pass an explicit `-projectPath` and verify in the log's COMMAND LINE ARGUMENTS / WorkingDir which project actually opened. See [[unity-mcp-setup]].
