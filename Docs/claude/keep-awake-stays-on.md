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

**How to apply:** since v0.106.5 `Tools/session-start.ps1` (SessionStart and every user message, on both computers) runs `Tools/keep-awake.ps1 -Start` itself and reports in its context when it had to start it. Both copies share the PID file, so either one counts as running. Leave it running at the end. Stop it only on the user's explicit word. (My interpretation: they want the machine available for long Unity jobs and remote sessions at any time, not only while I am mid-task.)

**2026-11-28:** after an app restart on the Lenovo it was not running and I did not check at take-over; the user had to ask ("Beží ten proces, aby neusnul PC?"). That is why the hook does it now.
