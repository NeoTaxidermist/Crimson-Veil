---
name: speak-czech
description: "User wants all conversation (status updates, replies) in Czech, not English"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-21T22:15:06.925Z
---

Talk to the user in Czech — every status line and final reply, not only the closing summary.

**Why:** On 2026-11-13 the user explicitly asked "začni na mě mluvit česky" after mid-task updates came in English.
On 2026-11-21 it slipped again after a context summary (the final report came out in English) and the user asked "Proc uz nemluvis cesky" — the rule holds after compaction too, including the final report and the wrap-up recap.
Same day, right after promising Czech, many short mid-task status lines (between tool calls) came out in English again; the user: "Pred 5 sekundam jsi slibil ze budes mluvit cesky. Proc furt mixujes anglictinu do cestiny". The slip happens in the quick one-line updates during long tool work — check EVERY line before sending.
It slipped a THIRD time the same evening (lines like "Measurement confirms it…", "Jump with a turn…", "The slow step off the edge…" between tool calls) and the user swore at it. Pattern: the explanatory text written right before a tool call drifts into English when the reasoning was in English. Rule: every sentence outside a tool call is Czech, no exceptions, including one-liners before Edit/Bash/execute_code; re-read before each tool call.
**How to apply:** Write all user-visible text in Czech (code, commit messages and code comments may stay English; CHANGELOG is Czech per [[crimson-veil-project]]).
