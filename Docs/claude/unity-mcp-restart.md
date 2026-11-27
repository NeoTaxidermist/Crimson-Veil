---
name: unity-mcp-restart
description: After a Claude app restart unityMCP may refuse connections - check the editor really runs (Hub's helper is also named unity.exe), start server + editor by hand
metadata:
  type: project
  modified: 2026-11-27
---

Seen on the Lenovo on 2026-11-27 after restarting the Claude app: `unityMCP` failed with ECONNREFUSED on 127.0.0.1:8080.

- **A process called `Unity` is not necessarily the editor.** Unity Hub keeps `C:\Program Files\Unity Hub\resources\unity.exe serve` running (18 MB, no window). The editor is `...\Hub\Editor\6000.3.28f1\Editor\Unity.exe`. Check `Path`, not the name.
- The MCP for Unity server (a `uvx` process the editor plugin spawns) was gone too. It can be started from Claude's shell exactly as the plugin does (`Editor/Services/Server/ServerCommandBuilder.cs`), version = the package's `package.json` version:
  `uvx --from "mcpforunityserver==10.3.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools` (Start-Process, hidden). It listens after ~8 s.
- Then start the editor: `Unity.exe -projectPath C:\Dev\CrimsonVeil`. With the server already up, the plugin connected by itself in ~20 s, and the tools that need Unity (`execute_code`, `run_tests`) come back after that. `manage_tools sync` says "Is Unity running?" until then.
- If the package version in `Packages/manifest.json` changes, use the new version in `--from`.
