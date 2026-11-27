---
name: milestone-tags-underscore
description: "Crimson Veil milestone git tags are named with an underscore: alpha_1, alpha_2, ..."
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-14T20:03:08.837Z
---

Milestone tags the user asks for ("otaguj jako alpha-2") are written with an underscore: alpha_1, alpha_2, alpha_3 ... even if the user types a hyphen. Version tags stay vX.Y.Z.

**Why:** the user confirmed on 2026-11-14 that they want all such tags with an underscore (alpha-2 was renamed to alpha_2).

**How to apply:** tag the current commit as alpha_N and push the tag; never create alpha-N. Related: [[crimson-veil-project]].
