# Rozpracováno – předávka mezi počítači

Vývoj běží na dvou počítačích (**Lenovo** = hlavní, RTX 2060; **DELL** = Latitude 5421, Intel UHD). Na každém je stálá session „Crimson Veil – Lenovo“ / „Crimson Veil – DELL“. Uživatel z telefonu vybírá, kde se pracuje.

**Pravidla předávky (pro Claude):**
- **Na začátku práce:** `git pull`, přečti tento soubor a poznámky v `Docs/claude/`, které se týkají úkolu. Pak sem zapiš, že pracuješ na tomto počítači.
- **Na konci každého úkolu, nebo když uživatel řekne, že přechází na druhý počítač:** aktualizuj tento soubor (co je hotové, co zbývá, co uživatel rozhodl a ještě není v CHANGELOGu), commitni s verzí a pushni.
- Nepracuj, pokud tu stojí, že druhý počítač má rozdělanou necommitnutou práci. Zeptej se uživatele.

## Teď pracuje
Lenovo – volno (vše commitnuté a pushnuté, v0.100.1+).

## Stav
- Větev `newFeatures`, do `main` se slučuje jen na pokyn uživatele.
- Výkon vyřešen ve v0.100.0: ~350 fps v okně 1080p na Lenovu. Na DELLu (Intel UHD) se zatím neměřilo.
- DELL: instalace podle `Docs/NovyPocitac.md` zatím neproběhla.

## Další na řadě
- **Přestavba nepřátel** na úroveň hrdiny, jeden na verzi a každého ukázat uživateli. Hotový je přízrak (v0.96). Další je **ShieldMonk**, pak ostatní. Postup je v `Docs/claude/crimson-veil-enemy-rebuild.md`.
- Otevřené body u šály a zbraní jsou v `Docs/claude/crimson-veil-scarf-state.md` a `crimson-veil-3d-veil-plan.md`.

## Rozhodnutí uživatele, která ještě nejsou v CHANGELOGu
- (zatím nic)
