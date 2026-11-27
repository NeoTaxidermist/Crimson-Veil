---
name: keep-awake-stays-on
description: "C:\\_ClaudeAI\\unity\\keep-awake.ps1 must stay running; never stop it when a task ends, only when the user says so"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-22T13:59:03.507Z
---

The keep-awake script `C:\_ClaudeAI\unity\keep-awake.ps1` (`-Start` / `-Status` / `-Stop`, hidden background PowerShell, PID in %TEMP%\keep-awake.pid) is to stay running. Do not stop it when a piece of work is finished.

**Why:** 2026-11-22 I stopped it on my own after finishing v0.74.0 ("the work is done"); the computer went to sleep and the user came back annoyed: "jak je mozne ze pocitac usnul, ma bezet to co ho drzi vzhuru".

**How to apply:** at the start of a session run `-Status` and `-Start` it if it is not running; leave it running at the end. Stop it only on the user's explicit word. (My interpretation: they want the machine available for long Unity jobs and remote sessions at any time, not only while I am mid-task.)
