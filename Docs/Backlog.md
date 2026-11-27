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

### 1. Přestavba nepřátel na úroveň hrdiny – hotovo, čeká na posouzení  [rozhodnuto] · L (každý nepřítel M)
- **Co:** postavit každého nepřítele znovu od nuly s anatomií a detailem jako hrdina. Jeden nepřítel na verzi, každého vyrenderovat a ukázat uživateli. Pořadí: ShieldMonk (`monk_build.py`), HangedMan (`hanged_build.py`), pak tvorové z `creature_kit` (RootCrawler, AshMoth, LanternWisp, PaperKite, Crow). ShadowDouble používá síť hrdiny.
- **Proč:** uživatel 2026-11-26 zvolil přestavbu od nuly, ne rychlé vyhlazení.
- **Dopady:** výkon (kapitola může mít stovky nepřátel; Dormancy a FixBounds z v0.97 to drží, při mnoha nepřátelích na obrazovce budou potřeba LOD), animace (zachovat jména kostí kvůli EnemyAnimator, HeroHands a SpringBones), testy nepřátel, cutscény (detail ze všech stran).
- **ShieldMonk – zadání uživatele (2026-11-27):** „potřebuje adekvátnější zbraň, ne tu hůlku“. Uživatel vybral **řemdih** (hlavice s hroty na řetězu) a **nový útok zbraní** vedle nárazu štítem. Řemdih nahradí růženec v pravé ruce.
- **Stav:**
  - **Hotovo:** přízrak (Wraith, v0.96) a ShieldMonk s řemdihem (v0.101.0, uživatel schválil: „Je to dobré, další“).
  - **HangedMan (v0.102.0):** uživatel ho přijal pokynem „Předělej všechny ostatní modely“.
  - **RootCrawler, AshMoth, LanternWisp, PaperKite, Crow (v0.103.0):** čekají na posouzení uživatelem.
  - Zbývá ShadowDouble, který používá síť hrdiny, takže je hotový s ním.
- **Výkon nových modelů (změřeno ve v0.104.0, DELL, 1080p):** nepřátelé v záběru stojí 0,45–1,0 ms. U skupin nepřátel je to 50–54 fps, na startu 58 fps; rozdíl dělá hlavně pozadí. LOD zatím není potřeba.
- **Odkazy:** `Docs/claude/crimson-veil-enemy-rebuild.md`, `crimson-veil-no-stock-art.md`, `crimson-veil-cutscenes.md`.

---

## Čeká na rozhodnutí uživatele

### Výkon na slabé grafice (Intel UHD: 58 fps v 1080p) – vyřešeno ve v0.106.0, čeká na vyzkoušení uživatelem  [rozhodnuto] · M
- **Co:** změřeno na DELLu ve v0.100.20. Hra je tam omezená pixely: 1080p 17,1 ms (58 fps), 720p 9,0 ms (111), 540p 6,4 ms (156). Na Lenovu (RTX 2060) je to ~350 fps.
- **v0.106.0 – vyřešeno:** uživatel řekl „vyhledej si na netu, jak se tyhle nastavení kvality obecně řeší, a vyřeš to“. Hra má tři úrovně (Nízká, Střední, Vysoká) a při prvním spuštění se vybere podle grafické karty: DELL dostane Střední, 96 fps na startu, 81 mezi nepřáteli. Praskliny v kamenech jsou předpečené pro všechny úrovně. Otevřené: vlastní nastavení jednotlivých položek („Vlastní“), které mají velké hry. Odhad 1,5–2 h: v menu Obraz SSAO, vyhlazování, rozlišení vykreslování a stíny; změna přepne předvolbu na „Vlastní“; ukládání; testy. Uživatel 2026-11-27: „Teď ne.“
- **Volba kvality v menu už existuje** (uživatel 2026-11-27: „vždyť to už je v menu, jen tam máš zatím jen jednu“): `SettingsPanel` → „Kvalita obrazu“, nabízí `QualitySettings.names` a ukládá je přes `GameSettings`. Pro Windows je teď jen „PC“ (v menu „Vysoká“). „Mobile“ (v menu „Nízká“) má ve `ProjectSettings/QualitySettings.asset` vyřazenou platformu Standalone. Nová úroveň pro slabou grafiku = nová úroveň kvality s vlastním URP assetem (např. bez SSAO). Menu ji nabídne samo.
- **v0.105.0:** uživatel řekl „optimalizuj, ale elegantně, ať to nevypadá hnusně“. Zrychleno beze změny vzhledu na 62–72 fps ve Full HD (mlha a papír z předpečených textur, SSAO v polovičním rozlišení, bez zbytečné kopie obrazu). Další páky by už sahaly do vzhledu nebo do shaderu vrstev pozadí (vzdálené vrstvy 2,9 ms, střední 1,4 ms).
- **v0.104.0:** ve Full HD mezi nepřáteli 50–54 fps. Grafika potřebuje 17,5–20 ms na snímek, procesor ~5 ms, takže brzdí jen GPU. Uživatel: „hlavně na DELLu změř i Full HD, 540p je brutální nouzovka“, tedy cílem je Full HD, ne nižší rozlišení.
- **Kde je čas (1080p):**
  - ~7 ms pevné celoobrazovkové průchody rendereru, z toho **SSAO 2,8 ms** (bez něj 14,3 ms = 70 fps). Zbytek, tedy InkGrade, kopie obrazu a hloubky a HDR 32bit, se zatím nerozdělil.
  - ~6 ms vrstvy pozadí v `Dekorace` (prostřední a vzdálené vrstvy, mlha, paprsky, vodopády), které se kreslí přes sebe (overdraw).
  - Hrdina ~1,6 ms.
- **Možnosti:**
  - úroveň kvality „Nízká“ pro integrovanou grafiku (bez SSAO, nižší render scale nebo dynamické rozlišení, méně vrstev mlhy),
  - volba v nastavení hry,
  - nebo nechat, pokud cíl 250 fps platí jen pro výkonnou grafiku.
- **Rozhodnout:** jaký cíl platí pro slabé počítače (např. stabilních 60 fps v 1080p?) a jestli smí být na nich méně efektů.
- **Stav:** uživatel 2026-11-27: „Zatím nech být.“ Nic nedělat, dokud to sám neotevře.
- **Dopady:** vzhled (SSAO a mlha jsou součást nálady), nastavení hry a menu, testy kvality, CHANGELOG. Na Lenovu se nic nezhorší.
- **Odkazy:** `Docs/claude/crimson-veil-performance.md` (sekce DELL).

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
- **Co:** úrovně detailu pro přestavěné nepřátele, až jich bude hodně na obrazovce najednou. Ve v0.104.0 stojí skupina 4–6 nepřátel v záběru na DELLu jen 0,45–1,0 ms, takže to zatím nespěchá.
- **Dopady:** souvisí s položkou 1 (přestavba nepřátel), Blender skripty v `ArtSource/`.


### Starší nápady (2026-11-13, ověřit, jestli ještě platí) · ?
- Ladění obtížnosti při skutečném hraní.
- Tmavší vínové odstíny v šále.

---

## Hotovo (posledních pár, pak smazat)
- v0.106.2 – ShieldMonk: prsty už nevyčnívají ze štítu při pohledu ze tří čtvrtin (štít odsazený od pěsti po své kolmici). Položka „UI plátna se přestavují každý snímek“ byla už hotová ve v0.100.0 (vlastní plátno šály, třepotání 30 Hz), dnes UI ~0,24 ms. Smazána.
- v0.106.0 – tři úrovně kvality s automatickým výběrem podle grafiky, praskliny předpečené.
- v0.105.0 – optimalizace pro slabou grafiku beze změny vzhledu (DELL Full HD 58 → 72 fps).
- v0.104.0 – výkon nového bestiáře na DELLu ve Full HD (`-cvat`), rozpad CPU a GPU.
- v0.103.0 – zbylí tvorové bestiáře přestavění (RootCrawler, AshMoth, LanternWisp, PaperKite, Crow), řetěz řemdihu jako tlumené kyvadlo.
- v0.102.0 – HangedMan přestavěný.
- v0.101.0 – ShieldMonk přestavěný, s řemdihem.
- v0.100.20 – benchmark na DELLu (Intel UHD): 58 fps v 1080p, rozpad nákladů, `bench.ps1 -Res`.
- v0.100.9 – předávka mezi sessions a počítači (`Docs/Rozpracovano.md`, hooky).
- v0.100.4 – DELL připravený k vývoji.
