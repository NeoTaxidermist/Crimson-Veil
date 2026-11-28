# Znalosti projektu pro Claude

Poznámky, které si Claude vedl během vývoje: pravidla od uživatele, rozhodnutí, postupy a úskalí. Dřív byly jen v místní paměti Claude na Lenovu, teď jsou tady, aby je měla každá session na každém počítači.

**Pravidla:**
- Před prací si přečti poznámky, které se týkají úkolu. Poznámky jsou psané anglicky, s uživatelem se mluví česky.
- Nové poznatky o projektu (rozhodnutí uživatele, postupy, úskalí) zapisuj sem jako samostatný soubor a přidej řádek do rejstříku níže. Commituj je s prací, které se týkají.
- Do místní paměti Claude patří jen věci, které platí pro jeden konkrétní počítač.
- Rozpracovanou práci předávej přes `Docs/Rozpracovano.md`.

## Projekt
- [Crimson Veil project](crimson-veil-project.md) – game vision, repo path, rule: tests + CHANGELOG + VERSION (tag + section) for EVERY commit on every branch + push
- [Mluvit česky](speak-czech.md) – all user-facing text in Czech, incl. mid-task status updates
- [Clean character shots](character-shots-clean.md) – hero photos without grass/busy background: hide other renderers, plain backdrop
- [Milestone tags](milestone-tags-underscore.md) – alpha_N with underscore, never hyphen
- [Scarf work state](crimson-veil-scarf-state.md) – refs, open issues (shield, salto, weapons), clip recording, no recompile in play
- [Cutscenes from the models](crimson-veil-cutscenes.md) – models will drive story cutscenes: check close-ups from all sides
- [No stock art](crimson-veil-no-stock-art.md) – no ready-made characters/environment kits; tag withoutAssets marks the all-self-made state
- [Branches](crimson-veil-mocap-branch.md) – mocap merged at v0.42.1; new work on test branch newFeatures; delete merged branches; old git: checkout -b, no switch
- [Animation gallery artifact](crimson-veil-anim-artifact.md) – re-record + republish "Crimson Veil – animace" ONLY when the user asks
- [Move-set tests](crimson-veil-moveset-tests.md) – every move/combo gets a PlayMode test on the real hero prefab (HeroMoveSetPlayTests)
- [Check Play before refresh](unity-check-play-before-refresh.md) – isPlaying check before refresh_unity/tests; user playtests while I work
- [Hero animation layering](crimson-veil-additive-style-plan.md) – style layer (v0.71), firm footfalls (v0.77), head+trunk follow the blow (v0.78) done; never 100% clips; gait tempo ruling; bone-layer pitfalls
- [3D scarf state](crimson-veil-3d-veil-plan.md) – v0.74.0 on newFeatures, NOT merged to main until the user says; how to diagnose scarf kinks (harness + target-vs-cloth plots) and pitfalls
- [Unity memory and restarts](unity-memory-watchdog.md) – garbage test ballooned Unity to 49 GB (fixed v0.57); watchdog for long jobs; restart Unity myself
- [Test scope](crimson-veil-test-scope.md) – small changes: full EditMode + PlayMode of the touched area only; full PlayMode after bigger parts
- [Clipping measure + ledge hold](crimson-veil-clipping-measure.md) – vertex-in-stone method, BakeMesh(mesh, true) pitfall, v0.81.0 ledge state and my open decisions
- [Enemy rebuild](crimson-veil-enemy-rebuild.md) – rebuild each enemy to hero level, one per version, show each; wraith done v0.96; method + pitfalls
- [Performance](crimson-veil-performance.md) – target ~250 fps; measure builds with Benchmark -cvbench; v0.98 breakdown; levers IL2CPP + Burst
- [Health and crimson](health-and-crimson.md) – v0.110: no autoheal, mend knot from hits (R), drops -> altar upgrades (F at checkpoints), heart shards, crimson left at death
- [Combat feel](combat-feel.md) – v0.109 rules: startup 0.05, follow window 0.3 + held press, pin during the cut, sweep, recoil/reel, whiff, deflect, telegraph
- [Kite cloth free glide](kite-cloth-free-glide.md) – gliding without scarf drain = the PaperKite's cloth (8 s), by design; user: leave as is
- [Sound judging](crimson-veil-sound-judging.md) – CLAP model + LUFS instead of ears; sine thumps and digital silence sounded fake; mix targets

## Prostředí (zjištěno na Lenovu, na jiném počítači ověř)
- [Unity MCP setup](unity-mcp-setup.md) – C:\Dev\CrimsonVeil 6.3 project path, MCP for Unity on 127.0.0.1:8080, Start Server+Connect routine
- [Claude app MSIX virtualization](claude-app-msix-virtualization.md) – AppData\Roaming installs from Claude are invisible to Unity; user runs them
- [Unity batchmode pitfall](unity-batchmode-opens-last-project.md) – -createProjectPath opened Swarm game instead; create projects in Hub
- [Python path](python-path.md) – use C:\Users\vasek\.local\bin\python.exe; bare `python` hits the Store stub
- [Unity MCP po restartu](unity-mcp-restart.md) – ECONNREFUSED na 8080: ověř, že běží editor (ne pomocník Hubu `resources\unity.exe`), a spusť server přes uvx a editor ručně
- [DELL setup pitfalls](dell-setup-pitfalls.md) – Hub 4 has no headless CLI (deep link + signed IL2CPP installer), Smart App Control blocks uv Python
- [Keep-awake stays on](keep-awake-stays-on.md) – C:\_ClaudeAI\unity\keep-awake.ps1 must keep running; never stop it when a task ends
