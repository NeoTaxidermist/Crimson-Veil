---
name: crimson-veil-enemy-rebuild
description: "User wants every enemy rebuilt from scratch to the hero's level, one at a time, each shown; wraith done in v0.96.0 - method and pitfalls"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-26T19:51:06.975Z
---

2026-11-26: user chose "rebuild enemies from scratch" (not a quick smoothing pass) so they match the hero (anatomy, detail). One enemy per version, render each and show the user. Done: Wraith (v0.96.0). Left: ShieldMonk (monk_build.py), HangedMan (hanged_build.py), RootCrawler/AshMoth/LanternWisp/PaperKite/Crow (creature_kit + ArtSource/creatures/*.py, rigid parts), ShadowDouble uses the hero mesh.

Method that worked (wraith_build.py): dense ring_loft bodies with displacement functions (ribs, collarbones, spine), a displaced-sphere skull, holes via `keep(z, th)`, tube_loop with a fixed plane normal for rims/cords, then SOLIDIFY (use_even_offset False - even offset shot a blade across the face) + SUBSURF 1 + DECIMATE 0.4 (~50k verts; hero 42k; 11 wraiths in level). Keep bone names (EnemyAnimator, HeroHands, SpringBones "Tatter."). Mesh in FBX is stored z-up.
Pitfalls: blob-assembled faces look cartoonish; separate capped segments for a rim look like beads; a closed "face hollow" hides what is behind - cut a real hole.
Performance (v0.97, user: a chapter may hold hundreds of enemies): Dormancy sleeps creatures >12 m past the picture, wakes <8 m; FixBounds replaces updateWhenOffscreen (it CPU-re-skinned every creature every frame - the real cost). Stress probe kept in scratchpad keep/TempStressProbe.cs: 100 wraiths/30 in view 13.9 ms, 300 creatures/10 in view 10.8 ms (editor). Next lever if many are on screen: LODs.
**ShieldMonk done v0.101.0 (2026-11-27, DELL):** monk_build.py is now self-contained like wraith_build.py (rig_kit no longer used by it; hanged_build still uses rig_kit). User chose a **flail** instead of the old "stick" (the hanging rosary read as a stick) plus a new flail attack. Pitfalls met:
- In-game the shield arm is whichever arm is on the camera's side (R facing +x, L facing -x), so a weapon skinned to one hand would end up on the shield arm half the time. The flail is therefore separate objects (Flail haft, FlailLink.0-5, FlailHead) placed by ShieldMonk.AnimateFlail into the FREE fist each frame; the chain is FlailChain (verlet, kept in z of the tip). Part axes are measured from mesh bounds (LongAxis) - don't assume the FBX axis conversion.
- A verlet chain pulled by the head alone does not follow an arc (the constraints drag it back): pull every node towards its spot on the tip->target line, outer ones more.
- Reset the chain at the real tip on the first animated frame (the model's rest pose is elsewhere), else it flails about for a second.
- New FBX parts keep the FBX materials (MonkInk) in the prefab: LevelKitFactory swaps materials only when building. Fix in place with PrefabUtility.LoadPrefabContents (do NOT rebuild the prefab: the scene holds 65 instances whose overrides hang on file IDs).
- The shield used to float at a fixed spot while the arm held an empty fist at the face (invisible with the old blob hands, obvious with real ones): it now follows the shield hand; MonkShieldArm/Elbow (30/40) put the fist before the chest.
- In-game shots: a temporary PlayMode test (Category TempShots) rendering a Camera to a RenderTexture on a plain background, deleted before the commit.
Next: HangedMan (hanged_build.py).
Renders: scratchpad model_turn_open.py (Blender, front/34/side/back + close-up) and keep/TempEnemyShots.cs (PlayMode in-game shot next to the hero; copy into Tests/PlayMode, delete before commit). See [[crimson-veil-no-stock-art]], [[crimson-veil-cutscenes]].
