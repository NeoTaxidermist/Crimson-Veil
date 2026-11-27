---
name: crimson-veil-cutscenes
description: "Hero and enemy models will also drive animated story cutscenes — judge them in close-up from every side, not just the game's side view"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-19T08:03:40.507Z
---

The user plans animated cutscenes that tell the story, built from the same hero model (and later the enemy models). Stated 2026-11-19.

**Why:** the game's side-view silhouette hides flaws (hairline, temples, shards on the cheeks, short feet) that a front or 3/4 close-up shows at once.

**How to apply:** when changing a model (ArtSource/*_build.py), render the head and body from front, 3/4 both sides, side, back (scratchpad hero_render.py pattern) and fix what a close-up would show. Facial rig (jaw/eyes/brows) may be needed later for cutscene acting. See [[crimson-veil-project]].
