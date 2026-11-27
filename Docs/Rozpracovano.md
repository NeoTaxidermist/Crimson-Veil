# Rozpracováno – předávka mezi počítači

Vývoj běží na dvou počítačích (**Lenovo** = hlavní, RTX 2060; **DELL** = Latitude 5421, Intel UHD). Na každém je stálá session „Crimson Veil – Lenovo“ / „Crimson Veil – DELL“. Uživatel z telefonu vybírá, kde se pracuje, a **musí jít kdykoli pokračovat na druhém počítači**. Druhá session nevidí naši konverzaci, zná jen tento soubor a git.

**Pravidla předávky (pro Claude):**
- **Na začátku session** hook `Tools/session-start.ps1` sám stáhne změny a předá ti tento soubor (bez těchto pravidel) a přehled backlogu. **Při každé zprávě uživatele** udělá totéž, ale jen když druhý počítač mezitím něco pushnul (`-OnPrompt`). Přečti i poznámky v `Docs/claude/`, které se týkají úkolu. Zapiš do „Teď pracuje“, že pracuješ na tomto počítači.
- **Průběžně, po každém dokončeném kroku** (ne až na konci dne): aktualizuj svou sekci v „Poslední session“, commitni s verzí a pushni. Uživatel může přejít na druhý počítač kdykoli, i bez ohlášení.
- Do „Poslední session“ piš konkrétně, aby druhá session mohla rovnou navázat: o čem jsme mluvili, co uživatel chtěl, co je hotové, co ne a proč, co je rozdělané (soubory, stav), rozhodnutí uživatele a přesný další krok. Starší body, které už neplatí, mazej. Trvalé poznatky patří do `Docs/claude/`.
- Necommitnutou práci na konci odpovědi nenechávej. Pokud to opravdu nejde (testy neprošly, čeká se na uživatele), napiš do své sekce, co je rozdělané a v jakém stavu.
- Nepracuj, pokud tu stojí, že druhý počítač má rozdělanou necommitnutou práci. Zeptej se uživatele.

## Teď pracuje
**DELL** – benchmark sestavené hry na Intel UHD (převzato od Lenova 2026-11-27, v0.100.19). Lenovo volno.

## Poslední session

### DELL – 2026-11-27 (session „Crimson Veil – DELL“)
- **O čem jsme mluvili:** příprava DELLu na vývoj podle `Docs/NovyPocitac.md`. Pak proč je hra v editoru rozmazaná a jak zajistit, aby se práce automaticky předávala mezi počítači.
- **Hotovo:**
  - Celá instalace: Git, Unity 6000.3.28f1 s IL2CPP, Visual Studio 2026 s C++ a SDK, Blender 5.3, uv a Python. MCP for Unity se automatickým startem, Blender MCP. EditMode 655/655 prošlo. Detaily jsou v sekci Stav.
  - Rozmazaná hra na DELLu: v okně Game bylo zapnuté „Low Resolution Aspect Ratios“ a Windows má zvětšení 150 %. Vypnuto (v0.100.7).
  - Automatická předávka: hook SessionStart (v0.100.8) a tato struktura souboru s pojistkou Stop hook (v0.100.9).
  - `Docs/Backlog.md` (v0.100.10): uživatel chce v gitu seznam nápadů, toho, co se musí a co by se mohlo udělat, včetně dopadů, protože o věcech mluvíme a dělat je jde až za pár dní. Naplněno z otevřených bodů v `Docs/claude/`.
  - Předávka ověřená ve dvou nových sessions. První ukázala, že výstup hooku přes 10 KB se zobrazí jen jako náhled; opraveno ve v0.100.11 (~4,7 KB). Druhá dostala všechno a odpověděla správně.
  - Hook při každé zprávě (v0.100.13): otevřená session si stáhne změny z druhého počítače, jakmile jí uživatel napíše. Pokud se nic nezměnilo, je tichý (~1 s). Ověřeno v dočasném klonu.
- **Nehotovo:** benchmark sestavené hry na Intel UHD (`Tools/bench.ps1`), zatím se nestavělo.
- **Rozdělané:** nic. Necommitnuté jsou jen soubory, které vygenerovalo Unity (`Assets/Settings/Mobile_RPAsset.asset`, `.vsconfig`, `CrimsonVeil.slnx`), ty se necommitují.
- **Rozhodnutí uživatele:** Smart App Control na DELLu je vypnuté. Stačí dvě stálé sessions (DELL, Lenovo), nové se zakládat nemusí.
- **Další krok:** uživatel jednou restartuje aplikaci Claude na DELLu i na Lenovu (na Lenovu po `git pull`), aby stálé sessions načetly hooky. Pak podle uživatele, nahoře v `Docs/Backlog.md` je přestavba nepřátel (ShieldMonk) a benchmark na DELLu.

### Lenovo – 2026-11-27 (session „Crimson Veil – Lenovo“)
- **O čem jsme mluvili:** výkon (v0.100.0: zrno tuše zapečené do textury, ~350 fps), příprava DELLu a předávka mezi počítači (v0.100.1–v0.100.5: `CLAUDE.md`, `Docs/NovyPocitac.md`, poznámky v `Docs/claude/`, napojení Blenderu).
- **Hotovo:** `git pull` na v0.100.13, hooky předávky na Lenovu (v0.100.15–16).
- **Hooky na Lenovu:** stálá session běží ve složce `C:\_ClaudeAI\unity` (je tam `.mcp.json` pro Unity a buildy). Na pokyn uživatele jsou proto hooky zkopírované do `C:\_ClaudeAI\unity\.claude\settings.json` s pevnými cestami `C:\Dev\CrimsonVeil\Tools\session-start.ps1` / `session-stop.ps1`. `C:\_ClaudeAI\unity\CLAUDE.md` načítá repozitářový `CLAUDE.md`. **Ověřeno po restartu aplikace Claude:** hook SessionStart proběhl a předal předávku i backlog. **Když se změní `.claude/settings.json` v repozitáři, je potřeba změnu přenést i sem.**
- **Unity po restartu:** editor neběžel (běžel jen pomocný proces Unity Hubu `resources\unity.exe serve`, nezaměňovat) a server MCP for Unity na 8080 taky ne. Spuštěno ručně: server `uvx --from "mcpforunityserver==10.3.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools` a editor `Unity.exe -projectPath C:\Dev\CrimsonVeil`. Unity se připojilo samo za 20 s.
- **Neověřeno:** hook při každé zprávě (`-OnPrompt`) na Lenovu se ještě neozval, protože od restartu DELL nic nepushnul. Stop hook se ještě sám nespustil, ručně funguje. Jestli uživatel vidí v telefonu obě sessions, ví jen on. Navržený test: na DELLu něco commitnout a pushnout, pak napsat Lenovu. Má se objevit blok `[git-sync: ...]`.
- **Rozdělané:** nic. Uživatel řekl „předej práci“ (2026-11-27).
- **Další krok:** podle uživatele. Nahoře v `Docs/Backlog.md` je ShieldMonk a benchmark na DELLu.

## Stav
- Větev `newFeatures`, do `main` se slučuje jen na pokyn uživatele.
- Výkon vyřešen ve v0.100.0: ~350 fps v okně 1080p na Lenovu. Na DELLu (Intel UHD) se zatím neměřilo.
- **DELL je připravený k vývoji (2026-11-27):** Git 2.56.1, Unity 6000.3.28f1 + IL2CPP (navíc je tam 6000.6.11f1, projekt ho nepoužívá), Visual Studio 2026 s C++ a Windows SDK 10.0.26100, Blender 5.3 s doplňkem MCP od Blender Lab, uv 0.13.10 a Python 3.12.16, Smart App Control vypnuté. MCP for Unity a Blender MCP fungují, keep-awake běží, Remote Control pro nové sessions zapnutý. Úskalí jsou v `Docs/claude/dell-setup-pitfalls.md`.

## Další na řadě
Všechno, co se má, může nebo čeká na rozhodnutí, je v **`Docs/Backlog.md`** (s dopady a odkazy). Nahoře v sekci „Musí se“: přestavba nepřátel (další je ShieldMonk) a benchmark na DELLu.

## Rozhodnutí uživatele, která ještě nejsou v CHANGELOGu
- (zatím nic)
