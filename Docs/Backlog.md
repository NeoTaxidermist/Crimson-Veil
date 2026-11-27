# Backlog – co se musí, co by se mohlo a nápady

Všechno, o čem jsme mluvili a co se nedělá hned. Sdílí ho oba počítače a hook na začátku každé session ho předá Claudovi spolu s `Docs/Rozpracovano.md`.

**Pravidla (pro Claude):**
- Jakmile s uživatelem probereme něco, co se nebude dělat hned (nápad, „to uděláme později“, otevřená otázka, chyba nalezená mimo úkol), **zapiš to sem ještě v téže odpovědi** a commitni s pushem. Nic takového nesmí zůstat jen v konverzaci.
- Každá položka má: **co**, **proč / kdo to chtěl** (citace uživatele, pokud je), **dopady** (na co to má vliv: kód, výkon, testy, grafika, ostatní položky), **stav** a **odkazy** (poznámky v `Docs/claude/`, soubory, verze).
- Pořadí v sekci = priorita. Když se položka začne dělat, napiš do jejího stavu „rozpracováno na <počítač>“ a podrobnosti dej do `Docs/Rozpracovano.md`. Hotové položky přesuň dolů do „Hotovo“ s verzí; po čase je můžeš smazat, protože historii drží CHANGELOG.
- Neplánuj sám bez uživatele: nové položky zařazuj, ale o pořadí v „Musí se“ rozhoduje uživatel.

Značky: **[rozhodnuto]** uživatel výslovně chce · **[návrh]** můj návrh, uživatel ještě nepotvrdil · odhad: S = do hodiny, M = půl dne, L = víc dní.

---

## Musí se (rozhodnuto, čeká na práci)

### 1. Přestavba nepřátel na úroveň hrdiny – další: ShieldMonk  [rozhodnuto] · L (každý nepřítel M)
- **Co:** postavit každého nepřítele znovu od nuly s anatomií a detailem jako hrdina. Jeden nepřítel na verzi, každého vyrenderovat a ukázat uživateli. Pořadí: ShieldMonk (`monk_build.py`), HangedMan (`hanged_build.py`), pak tvorové z `creature_kit` (RootCrawler, AshMoth, LanternWisp, PaperKite, Crow). ShadowDouble používá síť hrdiny.
- **Proč:** uživatel 2026-11-26 zvolil přestavbu od nuly, ne rychlé vyhlazení.
- **Dopady:** výkon (kapitola může mít stovky nepřátel; Dormancy a FixBounds z v0.97 to drží, při mnoha nepřátelích na obrazovce budou potřeba LOD), animace (zachovat jména kostí kvůli EnemyAnimator, HeroHands a SpringBones), testy nepřátel, cutscény (detail ze všech stran).
- **Stav:** hotový je přízrak (Wraith, v0.96). Ostatní nezačaté.
- **Odkazy:** `Docs/claude/crimson-veil-enemy-rebuild.md`, `crimson-veil-no-stock-art.md`, `crimson-veil-cutscenes.md`.

### 2. Benchmark na DELLu (Intel UHD)  [rozhodnuto] · S
- **Co:** sestavit hru (IL2CPP + D3D11) na DELLu a změřit `Tools/bench.ps1` v okně 1920×1080.
- **Proč:** ověřit, že sestavení přes IL2CPP na DELLu funguje, a zjistit výkon na slabém hardwaru. Cíl je ~250 fps; na Lenovu je ~350 fps.
- **Dopady:** může ukázat, co je drahé na integrované grafice (pixely vs. CPU) a změnit priority optimalizace.
- **Stav:** DELL je připravený (v0.100.4), zatím se nestavělo.
- **Odkazy:** `Docs/claude/crimson-veil-performance.md`.

---

## Čeká na rozhodnutí uživatele

### Úchop na hraně: otevřená dlaň místo pěsti  [návrh] · S
- **Co:** hrdina se na hraně drží otevřenou dlaní na horní ploše; je to moje rozhodnutí z v0.81.
- **Dopady:** animace šplhání, testy těla ve zdi (`HeroBodyWallPlayTests`).
- **Odkazy:** `Docs/claude/crimson-veil-clipping-measure.md`.

### Stisk zbraně na hraně se tiše zahodí  [návrh] · S
- **Co:** ve visu nejsou zbraně (rozhodnuto ve v0.82), ale stisk se zahodí bez odezvy. Pravidlo uživatele přitom říká, že stisk se nesmí tiše ztratit.
- **Návrh:** viditelná odezva (např. obláček inkoustu jako u nepovedeného kouzla), nebo si stisk zapamatovat na dobu po vylezení.
- **Dopady:** ovládání, testy pohybové sady.

### Klávesy zbraní  [návrh] · S
- **Co:** teď J K L ; a štít U (můj návrh, v0.85). Uživatel navrhoval U I O P a štít J.
- **Dopady:** výchozí `InputBindings`, nápověda v menu, testy rebindingu. Uložená nastavení hráčů zůstanou.

### Sloučení `newFeatures` do `main`  [čeká na pokyn] · S
- **Co:** do `main` se slučuje jen na výslovný pokyn uživatele. Naposledy (v0.74) řekl „ne nemerguj“.

---

## Mělo by se (otevřené chyby a dluhy)

### Šála a zbraně – otevřené body z playtestů · M
- **Co:** ve zbývajících 0,06–0,12 s vylézání není ruka na hraně ani noha nahoře; meč ve visu míří do zdi (ve visu teď zbraň mizí, ověřit, jestli to platí i tady).
- **Starší (v0.16, ověřit, jestli ještě platí):** zubaté plameny prstence štítu, šála se po saltu na okamžik zmuchlá před tělem, zbraně mají starý plamenný vzhled.
- **Dopady:** vzhled šály, testy šály. Zlomy šály uživatel nepřijímá vůbec („Zadne zalamovani“).
- **Odkazy:** `Docs/claude/crimson-veil-scarf-state.md`, `crimson-veil-3d-veil-plan.md` (jak měřit zlomy), `crimson-veil-clipping-measure.md`.

### Galerie animací ukazuje v0.77  · M
- **Co:** galerie (artifact) ukazuje v0.77; od té doby se změnila hlava a trup, bič, zbraně a šplhání.
- **Pravidlo:** přetočit a publikovat **jen když o to uživatel požádá**.
- **Odkazy:** `Docs/claude/crimson-veil-anim-artifact.md`.

---

## Mohlo by se (nápady a možnosti)

### Šála do Burst jobů · L  [návrh]
- **Co:** přepsat simulaci a vykreslování šály (VeilRenderer 2,4 ms, simulace 1,0 ms ve v0.98) na Burst jobs.
- **Proč:** největší zbývající páka na výkon CPU; dnes je strop CPU ~2,3 ms.
- **Dopady:** velký zásah do kódu šály, riziko zlomů a jiného chování. Je potřeba měřit podle postupu v `crimson-veil-3d-veil-plan.md` a mít kompletní testy šály.
- **Odkazy:** `Docs/claude/crimson-veil-performance.md`.

### LOD pro nepřátele · M  [návrh]
- **Co:** úrovně detailu pro přestavěné nepřátele, až jich bude hodně na obrazovce najednou.
- **Dopady:** souvisí s položkou 1 (přestavba nepřátel), Blender skripty v `ArtSource/`.

### UI plátna se přestavují každý snímek (0,48 ms) · S  [návrh]
- **Co:** rozdělit HUD na statické a měnící se plátno.
- **Dopady:** výkon, testy HUD a menu.

### Starší nápady (2026-11-13, ověřit, jestli ještě platí) · ?
- Ladění obtížnosti při skutečném hraní.
- Tmavší vínové odstíny v šále.

---

## Hotovo (posledních pár, pak smazat)
- v0.100.9 – předávka mezi sessions a počítači (`Docs/Rozpracovano.md`, hooky).
- v0.100.4 – DELL připravený k vývoji.
