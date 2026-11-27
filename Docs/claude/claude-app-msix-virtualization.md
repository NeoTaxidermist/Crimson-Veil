---
name: claude-app-msix-virtualization
description: Installs run from the Claude desktop app into AppData\Roaming are virtualized and invisible to Unity/other apps
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-13T07:44:33.106Z
---

The Claude desktop app on this machine is an MSIX package. Files that Claude's shell writes to `AppData\Roaming` (e.g. `uv python install`) are silently redirected to `C:\Users\vasek\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\...`, so Unity and other apps can't see them. `dangerouslyDisableSandbox` does not help. Writes to `C:\Users\vasek\.local\bin` and `C:\Dev\...` are real.

**Why:** a Python install via uv looked successful to Claude but Unity's MCP setup still showed "Python not found"; the user had to run it in their own PowerShell.
**How to apply:** for tool/runtime installs other apps must see (Python, uv tools, global configs in AppData), give the user the command to run in their own terminal instead of running it. See [[unity-mcp-setup]].
