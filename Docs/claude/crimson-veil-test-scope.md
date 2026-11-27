---
name: crimson-veil-test-scope
description: Run only the test categories of the touched areas (EditMode + PlayMode by NUnit category, map in Tests/KATEGORIE.md); full suites only after big parts
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-26T11:54:22.174Z
---

2026-11-22 the user said test runs take far too long; 2026-11-26 they objected again ("to jako poustis vsechny testy kvuli takovehle malickosti?") and asked to tag the tests and run only the relevant ones per change.

**Why:** full PlayMode takes ~8 min (139 tests), full EditMode ~35 s (623); the user watches the session and the waiting annoys them.

**How to apply:**
- Since v0.87.0 every test class has NUnit categories: Motor, Hero, Veil, Combat, World, UI, Camera, Tools. `Assets/_CrimsonVeil/Tests/KATEGORIE.md` maps changed files to categories. Run `run_tests` with `category_names` for both EditMode and PlayMode - no longer the whole EditMode by default.
- New test classes must get a `[Category]` (an uncategorised test is never selected).
- Full suites only after a bigger finished part, before merging to main and before milestone tags.
- Say in the CHANGELOG exactly which categories/tests ran.
- Every commit still needs its tests, CHANGELOG and tag per [[crimson-veil-project]].
