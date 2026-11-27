# Rozpracováno – předávka mezi počítači

Vývoj běží na dvou počítačích (**Lenovo** = hlavní, RTX 2060; **DELL** = Latitude 5421, Intel UHD). Na každém je stálá session „Crimson Veil – Lenovo“ / „Crimson Veil – DELL“. Uživatel z telefonu vybírá, kde se pracuje, a **musí jít kdykoli pokračovat na druhém počítači**. Druhá session nevidí naši konverzaci, zná jen tento soubor a git.

**Pravidla předávky (pro Claude):**
- **Na začátku session** hook `Tools/session-start.ps1` sám stáhne změny a předá ti celý tento soubor. Přečti i poznámky v `Docs/claude/`, které se týkají úkolu. Zapiš do „Teď pracuje“, že pracuješ na tomto počítači.
- **Průběžně, po každém dokončeném kroku** (ne až na konci dne): aktualizuj svou sekci v „Poslední session“, commitni s verzí a pushni. Uživatel může přejít na druhý počítač kdykoli, i bez ohlášení.
- Do „Poslední session“ piš konkrétně, aby druhá session mohla rovnou navázat: o čem jsme mluvili, co uživatel chtěl, co je hotové, co ne a proč, co je rozdělané (soubory, stav), rozhodnutí uživatele a přesný další krok. Starší body, které už neplatí, mazej. Trvalé poznatky patří do `Docs/claude/`.
- Necommitnutou práci na konci odpovědi nenechávej. Pokud to opravdu nejde (testy neprošly, čeká se na uživatele), napiš do své sekce, co je rozdělané a v jakém stavu.
- Nepracuj, pokud tu stojí, že druhý počítač má rozdělanou necommitnutou práci. Zeptej se uživatele.

## Teď pracuje
DELL – volno (vše commitnuté a pushnuté, v0.100.9). Lenovo volno.

## Poslední session

### DELL – 2026-11-27 (session „Crimson Veil – DELL“)
- **O čem jsme mluvili:** příprava DELLu na vývoj podle `Docs/NovyPocitac.md`. Pak proč je hra v editoru rozmazaná a jak zajistit, aby se práce automaticky předávala mezi počítači.
- **Hotovo:**
  - Celá instalace: Git, Unity 6000.3.28f1 s IL2CPP, Visual Studio 2026 s C++ a SDK, Blender 5.3, uv a Python. MCP for Unity se automatickým startem, Blender MCP. EditMode 655/655 prošlo. Detaily jsou v sekci Stav.
  - Rozmazaná hra na DELLu: v okně Game bylo zapnuté „Low Resolution Aspect Ratios“ a Windows má zvětšení 150 %. Vypnuto (v0.100.7).
  - Automatická předávka: hook SessionStart (v0.100.8) a tato struktura souboru s pojistkou Stop hook (v0.100.9).
- **Nehotovo:** benchmark sestavené hry na Intel UHD (`Tools/bench.ps1`), zatím se nestavělo.
- **Rozdělané:** nic. Necommitnuté jsou jen soubory, které vygenerovalo Unity (`Assets/Settings/Mobile_RPAsset.asset`, `.vsconfig`, `CrimsonVeil.slnx`), ty se necommitují.
- **Rozhodnutí uživatele:** Smart App Control na DELLu je vypnuté.
- **Další krok:** podle uživatele. Na řadě je přestavba nepřátel (ShieldMonk), viz „Další na řadě“.

### Lenovo
- v0.100.5: doplnil do `Docs/NovyPocitac.md` přesný postup napojení Blenderu. Jinak volno.

## Stav
- Větev `newFeatures`, do `main` se slučuje jen na pokyn uživatele.
- Výkon vyřešen ve v0.100.0: ~350 fps v okně 1080p na Lenovu. Na DELLu (Intel UHD) se zatím neměřilo.
- **DELL je připravený k vývoji (2026-11-27):** Git 2.56.1, Unity 6000.3.28f1 + IL2CPP (navíc je tam 6000.6.11f1, projekt ho nepoužívá), Visual Studio 2026 s C++ a Windows SDK 10.0.26100, Blender 5.3 s doplňkem MCP od Blender Lab, uv 0.13.10 a Python 3.12.16, Smart App Control vypnuté. MCP for Unity a Blender MCP fungují, keep-awake běží, Remote Control pro nové sessions zapnutý. Úskalí jsou v `Docs/claude/dell-setup-pitfalls.md`.

## Další na řadě
- **Přestavba nepřátel** na úroveň hrdiny, jeden na verzi a každého ukázat uživateli. Hotový je přízrak (v0.96). Další je **ShieldMonk**, pak ostatní. Postup je v `Docs/claude/crimson-veil-enemy-rebuild.md`.
- Otevřené body u šály a zbraní jsou v `Docs/claude/crimson-veil-scarf-state.md` a `crimson-veil-3d-veil-plan.md`.
- Benchmark na DELLu (Intel UHD).

## Rozhodnutí uživatele, která ještě nejsou v CHANGELOGu
- (zatím nic)
