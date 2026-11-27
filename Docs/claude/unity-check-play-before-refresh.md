---
name: unity-check-play-before-refresh
description: "Before refresh_unity / running tests, check EditorApplication.isPlaying - the user often playtests in Play mode"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-20T09:39:27.828Z
---

Before every `refresh_unity` (it recompiles) and before running tests, check with execute_code whether `EditorApplication.isPlaying` is true. The user often playtests while I work (recording videos). Twice on 2026-11-20 I recompiled in the middle of their Play mode, and the tests then failed to start.

**Why:** a recompile in Play mode breaks the running game; the rule "no recompile in play" is the user's (see [[crimson-veil-scarf-state]]).

**How to apply:** if Unity is playing, don't refresh. Ask the user to stop it, or stop it myself only when they say so; once they have said "ty ji ukonči" for that occasion, that's fine.
