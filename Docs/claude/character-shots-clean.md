---
name: character-shots-clean
description: Photos of the hero/characters must be taken without grass and without a distracting background
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-14T19:14:32.156Z
---

When taking screenshots of the hero (or other characters) to show poses, model or animation, hide the grass/vegetation and the busy level background: plain light backdrop with just the character (and its scarf/veil).

**Why:** the user said so after in-game shots where grass and ruins hid the figure (2026-11-14).

**How to apply:** in play mode move the roots "Player", "VeilCloth", "VeilSmoke", "InkBurst" (all children) to layer 31 and set Main Camera cullingMask = 1<<31 (disabling Renderers is not enough: grass and terrain are drawn without them), clearFlags SolidColor light grey (0.86), hide "HUD"; zoom the camera (FOV ~10-11) on the hero, screenshot with camera "Main Camera". Level overview shots ("fotky ze hry") can still show the level. Related: [[crimson-veil-project]].
