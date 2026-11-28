---
name: crimson-veil-mocap-branch
description: "Anything using third-party (Mixamo) motion goes on branch mocap-mixamo, not main, until the user has tested it; old git has no `git switch`"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-21T13:00:29.299Z
---

2026-11-19: the user asked that all work with foreign assets (Mixamo mocap) be committed to a separate branch so it can be tested first. Branch `mocap-mixamo` (first commit 4d7be0c) holds HeroMocapBaker, HeroMocap, the baked clips in Animations/Hero. `main` stays without it until the user approves a merge.

2026-11-20: the user approved the merge. The branch was first rewritten so each commit got its own version (v0.33.0–v0.42.1), then main was fast-forwarded to v0.42.1. Mocap now lives on main. Branch mocap-mixamo is deleted locally and on GitHub. The user wants a merged branch deleted right after the merge. From v0.42.1 the user created test branch `newFeatures`: new work goes there, every commit versioned, merged to main only when the user says so. 2026-11-21: newFeatures (v0.43.0–v0.45.2) fast-forwarded into main and deleted locally and on GitHub; then a new `newFeatures` was created from main at v0.45.2 (on GitHub too) and work continues on it. 2026-11-28: the user said "Otaguj to a zamerguj do mainu. Pak budeme ale pokračovat nad branchí": tagged `alpha_10` on v0.109.1, main fast-forwarded to it (was v0.45.x-era a608949), and work continues on the same `newFeatures` (not deleted and recreated this time - it equals main).

**Why:** test the third-party-based work before it enters the main line (see [[crimson-veil-no-stock-art]]).

**How to apply:** commit mocap-related changes on `mocap-mixamo` and push the branch. Every commit there still gets its own version number (CHANGELOG section + `vX.Y.Z` tag), the same as on main. The earlier note "no version tags" was wrong and led to 22 unversioned commits; see [[crimson-veil-project]]. Merge only when the user says so. The machine's git is old: use `git checkout -b`, not `git switch`. Raw sources stay gitignored (ArtSource/mocap, Assets/_CrimsonVeil/Art/Mocap).
