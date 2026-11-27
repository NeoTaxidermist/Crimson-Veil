---
name: crimson-veil-3d-veil-plan
description: 3D scarf state (v0.74.0 on newFeatures, not merged) + how to diagnose scarf kinks (in-game harness, target-vs-cloth plots) and the pitfalls
metadata:
  node_type: memory
  type: project
  modified: 2026-11-23T07:53:44.887Z
---

2026-11-21: the user chose a full 3D scarf (real 3D cloth simulation, interweaving strands, 3D weapons). Five phases done v0.46-v0.50 on branch newFeatures ([[crimson-veil-mocap-branch]]); milestone tag 3D_veil on v0.50.0; the user approved ("Je to dobre").

**Standing rule (user, 2026-11-21): do NOT merge newFeatures to main until they say so.** Ask at the end of a piece of work; never merge on my own.

**Why:** visuals, believable blows; the models will drive cutscenes seen from all sides ([[crimson-veil-cutscenes]]).

State 2026-11-22: v0.74.0 (scarf) tagged alpha_9 and fable_5_2 at the user's request; asked about merging, the user said no ("ne nemerguj"). After it on newFeatures: v0.75.0 hair (new locks after the concept + styled motion), v0.76.0 whip (a real lash), v0.77.0 firm footfalls, v0.78.0 head and trunk follow the blow ([[crimson-veil-additive-style-plan]]), v0.79.0 (2026-11-23, from the user's playtest video) whip held on its line (taut on a ring, falls slack after the crack, reeled in when the ring is let go), weapons toned to the scarf's crimson, layers kept in a turn. Gallery republished for v0.74.0 and again for v0.77.0 (Version 38) on their word; it does NOT yet show v0.78.0/v0.79.0 - re-record/publish only when they ask ([[crimson-veil-anim-artifact]]).

v0.80.0 (2026-11-23, "OPRAV TO VSECHNO" + a second playtest): every weapon press counts (a press during a blow is remembered, the last one - this REVERSES the v0.16.20 rule that dropped early presses; the user found dropped presses "badly predictable": "luk nekdy nevystreli sip"); a cast needs only a stub of scarf (`VeilCasting.MinToCast` 0.1) and a failed cast puffs ink at the hand; the crouched sword is a level thrust (user: "tukat do zeme misto bodani"), the crouched whip is thrown over the top and cracks over the floor (user: "bic je az nejak moc po zemi"); rope sag no longer flips sides when hanging still; bow string in deep crimson; `VeilTransform.MeltLead` 0.12 is now named (firm lead was tried and rejected: it sweeps the cloth under the floor). Gallery still shows v0.77.

Rulings from this: presses must never be silently lost; attacks from the crouch must read as attacks on an enemy, not on the floor.

Looks the user has ruled on (keep): free flecks/smoke after attacks stay ("ty volne ulomky po utocich byly fajn"); the LOOSE scarf's drops/threads stay off (they read as a broken scarf); losing volume during a turn was the user's own idea (TurnSlim) and is accepted; kinks/tears are not accepted at all ("Zadne zalamovani. Musi to byt plynule a nadherne." 2026-11-22, after two rounds they called disappointing).

**How to apply - diagnosing scarf kinks (what finally worked, 2026-11-22):**
- Don't guess and don't tune constants: measure on the real thing. In play mode chain ClipRecorder clips (scenarios: turn from standing, running turn, quick taps, jump, turn at take-off, mid-air turn + somersault, wall behind, under an overhanging block, crouch + turn in the tunnel at x=-55.6, fall off the ledge at (-51.5, 10.1) landing by a block) and in Probe count, on VeilRenderer's private `vertices`, where the main band's centre turns by more than 30 degrees between samples (main strand index 1, L/R pairs, `perStrand`, `samples` via reflection), split neck / body / tail.
- Then dump per frame VeilSimulation's private `shapeAt` (the line the cloth is led to) next to `Positions` and the obstacle rects, and plot them with PIL. Whether the TARGET is wrong or the cloth can't follow it is visible at once; almost every kink was a bad target.
- A temporary static helper class under Assets/<Temp>/Editor saves re-sending the harness through execute_code; delete it before committing.
- My interpretation of why earlier rounds failed: I patched symptoms in the renderer and springs. The fix was making the target shape smooth and of the scarf's own length by construction (one line laid from the neck, direction running down it with a delay, blending ANGLES not positions, nothing offset sideways from a curved line).

Pitfalls met here:
- ClipRecorder capture runs faster than real time: never `rm` a result file after starting a run (use fresh tags).
- `ToString("F2")` in execute_code uses the Czech locale (decimal commas): pass CultureInfo.InvariantCulture.
- Non-ASCII characters (Czech words) in a Bash command or heredoc break the shell: write such text with the Write tool into a .py file. Python has no PIL: `~/.local/bin/uv.exe run -q --no-project --with pillow python script.py` ([[python-path]]).
- Don't open contact sheets of more than ~40 frames at once.
- `Mathf.SmoothStep(from, to, t)` interpolates between from and to - it is NOT the shader's smoothstep(edge0, edge1, x). For an edge test write `Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x))`. (I wrote `1 - Mathf.SmoothStep(0.88f, 1f, m)` for a guide weight: it was 0.12 at most.)
- PlayMode tests that read bone or cloth positions must do it at the end of the frame (`EndOfFrameProbe`, LateUpdate order 10000), not in the coroutine's Update callback: mid-frame the body has moved and the pose is not solved yet, and the reading shakes by speed x the difference of two frame times. Two tests were "flaky" for exactly this (planted feet, braking from a run). When a test fails, run it on the previous commit too (git stash, twice) before blaming the change.
- The whip's lash goes where the FOREARM points (WhipChain replays the forearm's turn): a wind-up with the forearm hanging puts the lash on the floor in front of him. Forearm world angle in the crouch ~ 0.84*(armR + elbowR) - 110 degrees.
- ClipRecorder at 960x540 steps unevenly (one long frame, one short): measure motion per frame at 320-640 px, use the big size only for looking at colours.
- A soft weapon's loose nodes are pulled to the SCARF's laid line (`shapeAt`) by the sim - anything meant to keep a weapon's shape must be held on its targets (`HeldTo` / `HeldBeyond`), and collisions skip held nodes.
