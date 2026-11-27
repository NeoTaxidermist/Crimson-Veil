---
name: crimson-veil-clipping-measure
description: "How to measure the hero's body passing through stone (and the BakeMesh pitfall that made my numbers wrong); state of the ledge hold after v0.81.0"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-26T11:39:07.239Z
---

2026-11-23, v0.81.0 on newFeatures (not merged, [[crimson-veil-3d-veil-plan]]): the user asked to look at the body passing through geometry and at the climbing animations ("na zacatku se na maly okamzik nedrzime za hranu ale ve vzduchu"). Done: hands on the lip from the catch frame (the body comes to the hands, `PullToReach`), palm flat on the top, supported mantle (crouch on the top, foot slides over the corner and is set down), free weapon hand kept out of the wall, a jump's stretch stops at a roof.

Open with the user (my decisions, not theirs - ask before building on them): the grip is an open palm on the top instead of a fist; the hair got its own 8 cm tolerance in the lip test (a lock swings 5-6 cm into the wall in the catch frame); 0.06-0.12 s of the mantle still has no hand on the lip and no foot on the top; the sword still points into the wall while hanging.

v0.82.0 (same day, user's rulings): **no weapons while hanging** ("ve visu nemuzu prece pouzivat zbrane, to nedava smysl") - casts are refused on a lip and during the climb, a weapon in hand melts away; the capsule is 1.9 m, the drawn height (user: "oprav kolizni kapsli"); the user suspected that hands on a wall ride off the stone because of mismatched visuals and was right - terrain blocks' drawn sides stood up to 18 cm off the collider's faces, now 2 cm (`ProceduralMeshes.SideRelief`). When the user feels something is "a bit off the wall", compare the drawn mesh's outline with the collider before tuning the hero. Still mine and unconfirmed: a weapon press on a lip is dropped silently; the open-palm grip.

v0.83.0 (user ruling 2026-11-26): only the scarf glides; the shield is just a shield (its in-air "umbrella" slow-fall removed). v0.84.0: shield ring kept on its thrown side, held on its circle, tilt by ring side - it no longer breaks in turns; the ring's deliberate gap (~30-45 deg drawn) stays - the user ruled "Nech mezeru" (2026-11-26); don't close it.

v0.85.0: title screen (scene MainMenu, build index 0), settings with rebinding (`InputBindings`, saved in PlayerPrefs by action name), pause menu on the HUD. Weapon keys are my proposal (J K L ; + shield U; the user had suggested U I O P + shield J) - unconfirmed by the user.

**Twice this day a recompile landed in play mode and broke the game with NullReferenceExceptions** (once in the user's own play session). `refresh_unity` can time out while Unity still compiles: after it, wait ~8 s and check both `isCompiling` and `isUpdating` before entering play; and never refresh while `isPlaying` - ask the user first ([[unity-check-play-before-refresh]]).

**How to apply - measuring clipping:**
- Depth in stone = for every baked skin vertex inside a collider's bounds, the least distance to its faces; name the bone that carries the vertex (`sharedMesh.boneWeights[i].boneIndex0`), or the number tells nothing (the worst was hair, then fingertips).
- **Bake with `smr.BakeMesh(mesh, true)` and then `transform.TransformPoint`.** Plain `BakeMesh(mesh)` + TransformPoint counts the model's scale twice whenever the hero is squashed or stretched (take-off 1.2, lip catch, landing, end of a climb). I verified this in the editor on the Player prefab (scale 1.5: 2.857 m right, 4.285 m wrong). My harness had it wrong and I reported wrong numbers to the user for the catch frame and the roof; `HeroBodyWallPlayTests` had it right. When a new test disagrees with an old one, check which measure is right before touching either.
- ClipRecorder events at time 0 fire during the warm-up frames (the hero has already run metres by frame 1): start scripted moves at 0.02.
- After `refresh_unity`, check `EditorApplication.isCompiling` is false before entering play: a compile that finishes in play mode reloads the domain and the game throws NullReferenceExceptions ([[unity-check-play-before-refresh]]).
- A backslash-n inside a Bash heredoc reaches Python halved: write patch scripts with the Write tool ([[python-path]]).
