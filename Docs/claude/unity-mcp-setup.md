---
name: unity-mcp-setup
description: "How Claude connects to the user's Unity 6.3 project (MCP for Unity), paths, startup routine and known gotchas"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-13T08:13:17.680Z
---

Main game project (set up 2026-11-12/13, moved 2026-11-13): `C:\Dev\CrimsonVeil` (was `C:\Dev\NewGame\My project`), Unity 6000.3.28f1, URP 3D template. The game is planned as **2.5D** (3D scene, constrained camera). User works in Czech.

Unity is connected via community plugin **MCP for Unity** (CoplayDev, `com.coplaydev.unity-mcp` pinned to `#v10.3.0` in Packages/manifest.json), HTTP Local transport. Claude config is `C:\_ClaudeAI\unity\.mcp.json` → server `unityMCP` at `http://127.0.0.1:8080/mcp` (127.0.0.1, not localhost, to avoid IPv6 ::1). Server exposes ~49 tools (manage_scene, manage_gameobject, create_script, read_console, ...).

Startup routine each time: in Unity **Window → MCP for Unity → Start Server → Connect** (status "Session Active"), then run Claude in `C:\_ClaudeAI\unity`.

Blender is connected too (official Blender MCP). Plan for assets: export GLB/FBX from Blender via execute_blender_code straight into the project's Assets folder, then place via unityMCP; watch scale and axes (Blender Z-up vs Unity Y-up). The plugin's "Blender Bridge" menu is NOT used — it needs the separate ahujasid BlenderMCP addon on port 9876.

**Why:** setup took several non-obvious steps; see [[claude-app-msix-virtualization]] and [[unity-batchmode-opens-last-project]].
**How to apply:** if Unity tools are missing, first ask the user to Start Server + Connect, then check port 8080. The MCP server (uvx mcp-for-unity) keeps running after Unity/Hub close and locks the project's Library (pidfile); before moving/renaming the project folder the user must stop it themselves (Claude is not allowed to kill it).

2026-11-21: MCP now connects by itself on editor load: EditorPrefs MCPForUnity.AutoStartOnLoad = 1 (registry HKCU\Software\Unity Technologies\Unity Editor 5.x, value MCPForUnity.AutoStartOnLoad_h2539145689, DWORD 1; Unity names prefs key_h<djb2-xor hash>). No manual Start Server/Connect needed after a restart.
