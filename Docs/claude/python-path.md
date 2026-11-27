---
name: python-path
description: "Python is installed (uv, 3.12) but bare `python` in Claude's shell hits the WindowsApps Store stub; call the full path"
metadata:
  node_type: memory
  type: reference
  modified: 2026-11-13T08:39:13.510Z
---

Python 3.12 was installed via uv (for the Unity MCP setup) and works: `C:\Users\vasek\.local\bin\python.exe` (→ `AppData\Roaming\uv\python\cpython-3.12-windows-x86_64-none\python.exe`). Also `uv.exe` / `uvx.exe` in the same folder.

In Claude's shell, PATH lists `AppData\Local\Microsoft\WindowsApps` before `.local\bin`, so bare `python` resolves to the Microsoft Store stub and fails with "Permission denied".

**How to apply:** always invoke `C:\Users\vasek\.local\bin\python.exe` (Bash: `/c/Users/vasek/.local/bin/python.exe`) instead of `python`; don't conclude Python is missing. Related: [[claude-app-msix-virtualization]], [[unity-mcp-setup]].
