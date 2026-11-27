# Rozpracováno – předávka mezi počítači

Vývoj běží na dvou počítačích (**Lenovo** = hlavní, RTX 2060; **DELL** = Latitude 5421, Intel UHD). Na každém je stálá session „Crimson Veil – Lenovo“ / „Crimson Veil – DELL“. Uživatel z telefonu vybírá, kde se pracuje.

**Pravidla předávky (pro Claude):**
- **Na začátku práce:** `git pull`, přečti tento soubor a poznámky v `Docs/claude/`, které se týkají úkolu. Pak sem zapiš, že pracuješ na tomto počítači.
- **Na konci každého úkolu, nebo když uživatel řekne, že přechází na druhý počítač:** aktualizuj tento soubor (co je hotové, co zbývá, co uživatel rozhodl a ještě není v CHANGELOGu), commitni s verzí a pushni.
- Nepracuj, pokud tu stojí, že druhý počítač má rozdělanou necommitnutou práci. Zeptej se uživatele.

## Teď pracuje
DELL – volno, připravený k vývoji (vše commitnuté a pushnuté, v0.100.4). Lenovo volno.

## Stav
- Větev `newFeatures`, do `main` se slučuje jen na pokyn uživatele.
- Výkon vyřešen ve v0.100.0: ~350 fps v okně 1080p na Lenovu. Na DELLu (Intel UHD) se zatím neměřilo.
- **DELL: instalace podle `Docs/NovyPocitac.md` (2026-11-27):**
  - Hotovo: Git 2.56.1, klon `newFeatures` v `C:\Dev\CrimsonVeil` (hooky, followTags, identita jako na Lenovu), Unity Hub 4.0.2 (balíček z Microsoft Store), **Unity 6000.3.28f1 + Windows Build Support (IL2CPP)**, Visual Studio Community 2026 (18.12.1) s C++ a Windows SDK 10.0.26100, Blender 5.3, uv 0.13.10 a Python 3.12.16 (spustil uživatel ve vlastním PowerShellu). Session „Crimson Veil – DELL“, Remote Control pro nové sessions zapnutý, keep-awake běží.
  - Na DELLu je navíc Unity 6000.6.11f1, projekt ho nepoužívá.
  - Po restartu: **Smart App Control vypnuté** (blokovalo Python z uv), Python 3.12.16 jede, projekt otevřený přes Hub (první import proběhl), **MCP for Unity připojené, automatický start zapnutý**, konzole bez chyb (jen varování o samoprotínajících se polygonech v modelech ruin), **EditMode 655/655 prošlo**.
  - Zbývá: benchmark sestavené hry na Intel UHD (`Tools/bench.ps1`) a ověření Blender MCP (doplněk v Blenderu nainstaluje uživatel).
  - Úskalí jsou v `Docs/claude/dell-setup-pitfalls.md`.

## Další na řadě
- **Přestavba nepřátel** na úroveň hrdiny, jeden na verzi a každého ukázat uživateli. Hotový je přízrak (v0.96). Další je **ShieldMonk**, pak ostatní. Postup je v `Docs/claude/crimson-veil-enemy-rebuild.md`.
- Otevřené body u šály a zbraní jsou v `Docs/claude/crimson-veil-scarf-state.md` a `crimson-veil-3d-veil-plan.md`.

## Rozhodnutí uživatele, která ještě nejsou v CHANGELOGu
- (zatím nic)
