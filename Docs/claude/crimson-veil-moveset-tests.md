---
name: crimson-veil-moveset-tests
description: Every hero move and combo must have a PlayMode test on the real hero prefab (HeroMoveSetPlayTests)
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-19T19:47:07.165Z
---

On 2026-11-19 the user said: "evidentne mas malo testu. Vsechny tyhle pohyby a komba atd by meli byt testovane." Until then the moves were tested only on a bare PlayerController, with no model and no mocap. Bugs that only showed on video slipped through (a stuck tunnel, a slide that got up mid-slide, a roll on the spot, a level up-cut).

**Why:** the user wants the motion itself tested, not just the motor logic.

**How to apply:**
- Add each new or changed move or combo to `Assets/_CrimsonVeil/Tests/PlayMode/HeroMoveSetPlayTests.cs`. It runs the game's Player.prefab: the input is swapped for ScriptedInput and all weapons are unlocked. The test should check both what the game does and what the body plays (mocap clip keys, bone heights, hand positions).
- After fixing a bug, briefly revert the fix and confirm the test fails.

Related: [[crimson-veil-project]], [[crimson-veil-anim-artifact]].
