---
name: crimson-veil-project
description: "CrimsonVeil game vision, repo, and required workflow (tests, changelog, commit+push every change)"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-20T07:14:56.318Z
---

Working title **Crimson Veil** (started 2026-11-13). 2.5D action platformer: movement/combat like Prince of Persia: The Lost Crown, mood like Limbo. Greyscale world + crimson details only. Hero's magic scarf swirls like a Kármán vortex street and hardens (never fully) into whip, sword, spear, shield, bow. Puzzles + wordless strong story.

Unity project: `C:\Dev\CrimsonVeil` (moved from `C:\Dev\NewGame\My project` on 2026-11-13; user must re-add it in Unity Hub). Git repo root = that folder, remote https://github.com/NeoTaxidermist/Crimson-Veil.git, branch `main`, v0.1.0 tagged. Code lives in `Assets/_CrimsonVeil` (asmdefs CrimsonVeil.Runtime/Editor/Tests.*). Scene is rebuilt by menu "Crimson Veil > Build Prototype Scene" — serialized defaults in the scene override code defaults, so rebuild after tuning defaults.

State at end of 2026-11-13 session: v0.8.0 pushed. Scarf look approved direction (flame-like, key-art proportions); weapons flame-style with tips, whip coils, bow recurve with string; 8 new creatures (ShieldMonk, RootCrawler, HangedMan, ShadowDouble, CrowSwarm, AshMoth, LanternWisp, PaperKite) built from primitives. Open ideas: Blender models for creatures, difficulty tuning in real play, darker wine shades in the veil.

**Why:** user's explicit requirements for the project.
**How to apply:** every change: write/extend tests (EditMode + PlayMode), update CHANGELOG.md (Czech, Keep a Changelog), commit and push to GitHub. See [[unity-mcp-setup]].

**STRICT RULE (user, 2026-11-20): every single commit has its own version number AND its CHANGELOG entry, on every branch, feature branches included.** That means its own `## [X.Y.Z] – date` section in CHANGELOG plus the tag `vX.Y.Z` on that commit. Housekeeping commits are no exception: bump a patch version. Never leave work under an unversioned heading. On 2026-11-20 the user found the 22 commits on mocap-mixamo had a CHANGELOG but no versions, and 9 old commits on main lacking one or the other. They called it a broken rule. Since v0.43.0, git hooks in `.githooks/` enforce it: pre-commit needs a new, higher version section; post-commit creates the annotated tag; pre-push blocks untagged commits. Local config is `core.hooksPath=.githooks` and `push.followTags=true`. Never bypass with --no-verify. README media are in Docs/ (moves GIFs from the gallery frames, enemies, world shots). Do NOT re-record them on my own: the user asks for that now and then (2026-11-20). Keep the README text up to date with every change.
