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
Renders: scratchpad model_turn_open.py (Blender, front/34/side/back + close-up) and keep/TempEnemyShots.cs (PlayMode in-game shot next to the hero; copy into Tests/PlayMode, delete before commit). See [[crimson-veil-no-stock-art]], [[crimson-veil-cutscenes]].
