# Rozpracováno – předávka mezi počítači

Vývoj běží na dvou počítačích (**Lenovo** = hlavní, RTX 2060; **DELL** = Latitude 5421, Intel UHD). Na každém je stálá session „Crimson Veil – Lenovo“ / „Crimson Veil – DELL“. Uživatel z telefonu vybírá, kde se pracuje, a **musí jít kdykoli pokračovat na druhém počítači**. Druhá session nevidí naši konverzaci, zná jen tento soubor a git.

**Pravidla předávky (pro Claude):**
- **Na začátku session** hook `Tools/session-start.ps1` sám stáhne změny a předá ti tento soubor (bez těchto pravidel) a přehled backlogu. **Při každé zprávě uživatele** udělá totéž, ale jen když druhý počítač mezitím něco pushnul (`-OnPrompt`). Přečti i poznámky v `Docs/claude/`, které se týkají úkolu. Zapiš do „Teď pracuje“, že pracuješ na tomto počítači.
- **Průběžně, po každém dokončeném kroku** (ne až na konci dne): aktualizuj svou sekci v „Poslední session“, commitni s verzí a pushni. Uživatel může přejít na druhý počítač kdykoli, i bez ohlášení.
- Do „Poslední session“ piš konkrétně, aby druhá session mohla rovnou navázat: o čem jsme mluvili, co uživatel chtěl, co je hotové, co ne a proč, co je rozdělané (soubory, stav), rozhodnutí uživatele a přesný další krok. Starší body, které už neplatí, mazej. Trvalé poznatky patří do `Docs/claude/`.
- Necommitnutou práci na konci odpovědi nenechávej. Pokud to opravdu nejde (testy neprošly, čeká se na uživatele), napiš do své sekce, co je rozdělané a v jakém stavu.
- Nepracuj, pokud tu stojí, že druhý počítač má rozdělanou necommitnutou práci. Zeptej se uživatele.

## Teď pracuje
**Lenovo** (session „Crimson Veil – Lenovo“) od 2026-11-28. Uživatel řekl „Teď budeš pracovat ty“. DELL je volný.

## Poslední session

### DELL – 2026-11-27 (session „Crimson Veil – DELL“)
- **O čem jsme mluvili:** příprava DELLu na vývoj podle `Docs/NovyPocitac.md`. Pak proč je hra v editoru rozmazaná a jak zajistit, aby se práce automaticky předávala mezi počítači. Pak převzetí práce od Lenova (hook SessionStart stáhl v0.100.14–18) a měření výkonu na DELLu.
- **Hotovo:**
  - **Benchmark na DELLu (v0.100.20):** build IL2CPP + D3D11 funguje. 1080p **17,1 ms (58 fps)**, 720p 9,0 ms, 540p 6,4 ms. Omezují to pixely na Intel UHD. Asi 7 ms jsou pevné průchody rendereru (z toho SSAO 2,8 ms), asi 6 ms vrstvy pozadí v `Dekorace` (overdraw). Podrobnosti jsou v `Docs/claude/crimson-veil-performance.md`. Volba cíle pro slabé počítače čeká na uživatele (backlog). `bench.ps1` má nově `-Res`. Build zůstal v `C:\Dev\CrimsonVeil-build`, dočasný build bez SSAO v `C:\Dev\CrimsonVeil-build-nossao` (mimo repozitář).
  - Celá instalace: Git, Unity 6000.3.28f1 s IL2CPP, Visual Studio 2026 s C++ a SDK, Blender 5.3, uv a Python. MCP for Unity se automatickým startem, Blender MCP. EditMode 655/655 prošlo. Detaily jsou v sekci Stav.
  - Rozmazaná hra na DELLu: v okně Game bylo zapnuté „Low Resolution Aspect Ratios“ a Windows má zvětšení 150 %. Vypnuto (v0.100.7).
  - Automatická předávka: hook SessionStart (v0.100.8) a tato struktura souboru s pojistkou Stop hook (v0.100.9).
  - `Docs/Backlog.md` (v0.100.10): uživatel chce v gitu seznam nápadů, toho, co se musí a co by se mohlo udělat, včetně dopadů, protože o věcech mluvíme a dělat je jde až za pár dní. Naplněno z otevřených bodů v `Docs/claude/`.
  - Předávka ověřená ve dvou nových sessions. První ukázala, že výstup hooku přes 10 KB se zobrazí jen jako náhled; opraveno ve v0.100.11 (~4,7 KB). Druhá dostala všechno a odpověděla správně.
  - Hook při každé zprávě (v0.100.13): otevřená session si stáhne změny z druhého počítače, jakmile jí uživatel napíše. Pokud se nic nezměnilo, je tichý (~1 s). Ověřeno v dočasném klonu.
  - **ShieldMonk přestavěný (v0.101.0):** uživatel chtěl „adekvátnější zbraň, ne tu hůlku“ a vybral řemdih s novým útokem. Hotové:
    - nový model na úrovni hrdiny (tvář, ruce s prsty, roucha, štít),
    - řemdih (samostatné díly, simulovaný řetěz, vždy ve volné ruce),
    - útok řemdihem na 1,7–2,8 m,
    - štít v pěsti.
    
    Záběry ze hry dostal uživatel. Postup a úskalí jsou v `Docs/claude/crimson-veil-enemy-rebuild.md`.
  - ShieldMonk uživatel schválil („Je to dobré, další“).
  - **HangedMan přestavěný (v0.102.0):**
    - Kostra a herní logika zůstaly.
    - Nový model: vyhublé tělo s žebry, zlomeně padlá hlava s tváří, jazykem a vlasy, roztržená košile, ruce s prsty, bosá chodidla, oprátka s uzlem a okov s řetězem.
    - Společné nástroje pro stavbu jsou v `ArtSource/sculpt_kit.py`.
    - Záběry ze hry dostal uživatel.
  - **Zbylí tvorové (v0.103.0):** uživatel řekl „Předělej všechny ostatní modely“ (tím vzal HangedMana).
    - RootCrawler, AshMoth, LanternWisp, PaperKite a Crow jsou přestavění, se zachovanými jmény dílů.
    - Do kitu přibylo zjemnění a zředění.
    - Řetěz řemdihu je místo volné simulace jedno tlumené kyvadlo: PlayMode test na DELLu ukázal, že se Verletův řetěz roztáčel a skládal.
    - Rendery z Blenderu dostal uživatel.
  - **Výkon nového bestiáře na DELLu (v0.104.0):** uživatel chtěl měřit hlavně Full HD („540p je brutální nouzovka“).
    - Start beze změny: 58 fps.
    - U skupin nepřátel 50–54 fps. Nový `-cvat X,Y` v `Benchmark.cs` postaví hrdinu ke skupině a drží ho nezranitelného.
    - Samotní nepřátelé stojí 0,45–1,0 ms, zbytek dělá pozadí.
    - Vývojový build: GPU 17,5–20 ms na snímek, CPU ~5 ms, takže brzdí jen grafika.
    - Buildy jsou v `C:\Dev\CrimsonVeil-build` (release) a `C:\Dev\CrimsonVeil-build-dev` (vývojový), mimo repozitář.
  - **Optimalizace beze změny vzhledu (v0.105.0):** uživatel řekl „optimalizuj, ale elegantně, ať to nevypadá hnusně“.
    - Full HD na DELLu: start 58 → 72 fps, mezi nepřáteli 50–52 → 62–64 fps.
    - Mlha a papír v InkGrade se čtou z předpečených textur, SSAO běží v polovičním rozlišení a nepoužívaná kopie obrazu je vypnutá.
    - Vzhled jsem kontroloval na snímcích starého a nového buildu.
    - Postup, měření a zamítnuté varianty jsou v `Docs/claude/crimson-veil-performance.md`.
  - **Úrovně kvality (v0.106.0):** uživatel řekl „vyhledej si na netu, jak se tyhle nastavení kvality obecně řeší, a vyřeš to“ (volba v menu existovala, ale jen s jednou úrovní).
    - Úrovně: Nízká, Střední a Vysoká. Každá má vlastní URP asset, vyhlazování nastavuje `QualityPresets.cs`.
    - První spuštění vybere úroveň podle grafiky a v menu je u ní „(doporučeno)“. Volba se ukládá podle jména.
    - Praskliny v kamenech jsou předpečené (−2 ms na všech úrovních).
    - DELL Full HD, start / u nepřátel: Vysoká 82/70 fps, Střední 96/81 fps, Nízká 130/106 fps.
    - **v0.106.2:** u ShieldMonka už prsty nevyčnívají ze štítu, záběr z pěti stran je ověřený. Úkol s HUD byl hotový už ve v0.100.0.
    - **Na Lenovu:** hra si uložila starou kvalitu jako index, takže se načte jako Vysoká. Automatický výběr se projeví jen bez uloženého nastavení.
- **Nehotovo:** rozdělení zbylých ~4 ms pevných průchodů (InkGrade, kopie obrazu a hloubky, HDR). Je potřeba přepínač renderer features v `Benchmark.cs`, nebo dočasné buildy.
- **Rozdělané:** nic, práce je předaná. Odložené („Vlastní“ nastavení kvality, odhad 1,5–2 h) je v backlogu. Necommitnuté jsou jen soubory, které vygenerovalo Unity (`Assets/Settings/Mobile_RPAsset.asset`, `.vsconfig`, `CrimsonVeil.slnx`), ty se necommitují.
- **Rozhodnutí uživatele:** Smart App Control na DELLu je vypnuté. Stačí dvě stálé sessions (DELL, Lenovo), nové se zakládat nemusí.
- **Další krok:**
  - Uživatel posoudí nové tvory.
  - Uživatel vyzkouší úrovně kvality ve hře (menu → Nastavení → Obraz → Kvalita obrazu).
  - Cíl výkonu pro slabou grafiku podle uživatele „zatím nech být“.

### Lenovo – 2026-11-28 (session „Crimson Veil – Lenovo“)
- **O čem jsme mluvili:** 2026-11-27 výkon (v0.100.0: zrno tuše zapečené do textury, ~350 fps), příprava DELLu a předávka mezi počítači (v0.100.1–v0.100.18). 2026-11-28 uživatel řekl „Teď budeš pracovat ty“, Lenovo převzalo práci od DELLu.
- **Hotovo:**
  - Převzetí: hook při každé zprávě (`-OnPrompt`) na Lenovu **funguje**. Při zprávě „Teď budeš pracovat ty“ sám stáhl 18 commitů z DELLu (v0.100.19–v0.106.3) a předal předávku.
  - Unity na Lenovu načetlo změny bez chyb kompilace. Úrovně kvality jsou Mobile, Nízká, Střední, Vysoká.
  - **Keep-awake z hooku (v0.106.5):** na Lenovu po restartu neběžel a uživatel se musel zeptat. `Tools/session-start.ps1` ho teď na obou počítačích spouští sám, na začátku session i při každé zprávě. Když ho musí spustit, napíše to do kontextu. Otestováno na Lenovu: hook trvá ~2 s, nezasekne se a keep-awake běží dál i po skončení hooku.
- **Hooky na Lenovu:** stálá session běží ve složce `C:\_ClaudeAI\unity` (je tam `.mcp.json` pro Unity a buildy). Hooky jsou proto zkopírované do `C:\_ClaudeAI\unity\.claude\settings.json` s pevnými cestami `C:\Dev\CrimsonVeil\Tools\session-start.ps1` / `session-stop.ps1`. `C:\_ClaudeAI\unity\CLAUDE.md` načítá repozitářový `CLAUDE.md`. **Když se změní `.claude/settings.json` v repozitáři, je potřeba změnu přenést i sem.**
- **Unity po restartu aplikace Claude:** může neběžet editor (proces `resources\unity.exe serve` je jen pomocník Unity Hubu) ani server MCP for Unity na 8080. Postup je v `Docs/claude/unity-mcp-restart.md`.
- **Neověřeno:** Stop hook se ještě sám nespustil, ručně funguje.
  - **Kvalita „Vlastní“ (v0.107.0):** uživatel řekl „Vlastní nastavení kvality“. V menu Obraz jsou pod volbou Kvalita obrazu čtyři položky: Okolní stínění (SSAO), Vyhlazování hran (Vypnuto / FXAA / SMAA), Rozlišení vykreslování (75–100 %) a Stíny (Nízké / Střední / Vysoké). Předvolba ukáže svoje hodnoty, změna kterékoli položky přepne kvalitu na Vlastní (pátá úroveň s vlastním URP assetem a rendererem). Testy: EditMode 692/692, MenuPlayTests 3/3. V sestaveném buildu ověřeno přes `-cvdetail` (výpis ukazuje skutečný stav assetu). Snímek menu dostal uživatel.
  - **Úchop a stisk na hraně (v0.108.0):** uživatel vybral body 3 a 4 z backlogu. Ve visu se hrdina drží hranu prsty přehnutými přes roh, zápěstí má dole na stěně. Když se přetahuje nahoru, ruka se srovná naplocho. Stisk zbraně při šplhání se zapamatuje a zbraň se vyvolá nahoře, ve visu stisk odmítne obláček inkoustu. Testy: EditMode 692/692, PlayMode 155/155. Uživatel dostal snímky úchopu z boku a ze tří čtvrtin. Nejtěsnější místo je palec: 4,5 cm v kameni, tolerance je 5 cm.
  - **Plachtění bez čerpání šály (2026-11-28):** uživateli se zdálo, že plachtění někdy nečerpá šálu. Příčina je záměr: látka ze sestřeleného PaperKite dává 8 s plachtění zdarma (`KiteCloth`, `MotorLogic.GlideTime`) a ve hře to není vidět. Uživatel rozhodl nechat to, jak to je (v0.108.1).
  - **Boj, balíky A–E (v0.109.0):** uživatel řekl „Na chvilku přibít a je. Odezva bez trestu. A udělej všechno a b c d e“. Hotovo a popsané v `Docs/claude/combat-feel.md`. Testy: EditMode 692/692, PlayMode 163/163 (8 nových v `CombatFeelPlayTests`). Uživatel to ve hře ještě nezkoušel a nové efekty (ohlášení útoku, minutí, odraz od štítu) jsem neviděl na obrazovce, jen v testech.
  - **Štít proti všem (v0.109.1):** uživatel se ptal, jestli má štít fungovat proti všem nepřátelům. Blokoval všechny zásahy, jen vrány se nezapotácely a proti popelu z AshMoth štít nepomáhal. Teď vrána odletí od štítu a hejno 1 s neútočí. Držený štít nepustí popel na šálu, šálu přitom dál spotřebovává. Testy: EditMode 692/692; CombatFeel, Creature a Casting 42/42.
  - **Milník a sloučení (2026-11-28):** uživatel řekl „Otaguj to a zamerguj do mainu. Pak budeme ale pokračovat nad branchí“. Značka `alpha_10` je na v0.109.1 a `main` je na ni posunutá (fast-forward). Práce pokračuje na `newFeatures`.
  - **README, obrázky a galerie (v0.109.3):** uživatel řekl „Teď je ještě potřeba aktualizovat artefakty a popisy a obrázky v mainu atd“. Obnoveno: 9 portrétů nepřátel, hlavní obrázek, 4 snímky světa, 18 GIFů pohybu, texty README (boj, kvalita obrazu, výkon, nepřátelé, struktura) a galerie animací (https://claude.ai/artifact/HL292geRFfkEMM69R2QhfY, verze 39). Popisek luku v nastavení je jen „Luk“, střílí na stisk. Položka o klávesách zbraní je z backlogu pryč, klávesy už jsou U I O P a štít J. `main` posunutý na tuto verzi.
  - **Návrh: zdraví, léčení a drop z nepřátel (2026-11-28):** uživatel přemýšlí, jak má hra fungovat, aby byla zábavná. Samoléčení mu přijde divné a chce, aby z nepřátel něco padalo. Návrh je v `Docs/Backlog.md` (Musí se, bod 2). Čeká se na jeho rozhodnutí, v kódu nic.
  - **Zdraví a karmín (v0.110.0):** uživatel řekl „Udělej to všechno do branche. Btw těch checkpointů tolik v budoucnu nebude“. Hotovo podle návrhu, popis je v `Docs/claude/health-and-crimson.md`. Dvě odchylky od návrhu jsem uživateli řekl: léčení neplní šála (sama dorůstá, takže by to bylo zase samoléčení), ale uzel z úspěšných zásahů. Ze dvou variant trestu za smrt jsem dal přísnou, kapky zůstanou na místě smrti, a dá se přepnout. Uživatel to ve hře ještě nezkoušel. Ceny a množství kapek jsou první odhad.
  - **Návrh: restart kapitoly bez grindu (2026-11-28):** uživatel se ptal, jak dát hráči restart kapitoly i s nepřáteli, aby se nedalo grindovat. Návrh je v `Docs/Backlog.md` (Musí se, bod 2). Čeká se na rozhodnutí, v kódu nic.
  - **Restart kapitoly (v0.111.0):** uživatel řekl „Ano udělej to“ (návrh i se stropem). V pauze je „Začít kapitolu znovu“, které vrátí nepřátele i postup. Vylepšení mají strop podle kapitoly a nová hra z menu začíná bez postupu. Trest za smrt (přísná varianta) uživatel pořád nepotvrdil, je v backlogu.
  - **Menu a kapitoly (v0.112.0):** uživatel chtěl „brutálně vylepšit design menu“, karty kapitol s náhledem, rozdělit zkušební místnost a kapitolu a potvrzování mezerníkem. Hotovo: `TestRoom.unity` (editorový skript `ChapterScenes.Split`, volá ho i stavitel scény), v ruinách je průchod zazděný. Menu má pozadí z ruin s padajícími lístky a štětcové tahy, které platí pro všechna menu. Mezerník potvrzuje (`MenuSubmit`) a stisk, který zavře menu, hrdinu neovlivní. Testy: EditMode 701/701, PlayMode 173/173. Snímky menu dostal uživatel.
  - **HUD (v0.113.0):** uživatel odsouhlasil návrh („Udělej to“). Životy jsou kruhy tuše (`LifeRings`) vlevo nahoře, síla šály je malovaná šála se zářezy cen zbraní a uzlem (`EnergyScarf`) dole uprostřed a karmín je vpravo dole (`ProgressHud`). Stará šála jako ukazatel zdraví (`ScarfHealthBar`) je pryč. Snímek ze hry dostal uživatel.
  - **Šála v HUD jako šála (v0.113.1):** uživateli se nelíbil ohnivý pruh („myslel jsem, že to bude víc vypadat jako šála“). Teď ji tvoří 7 průsvitných pramenů od tmavého karmínu po jasný, které se kroutí přes sebe, mají lesk, ke konci se rozvolní a stočí a za nimi se táhnou kouřové nitky. Zářezy cen zbraní jsou malé značky nad šálou. Kruhy životů zůstaly, uživatel je zatím nekomentoval.
- **Rozdělané:** nic.
- **Další krok:** podle uživatele. Na DELLu by stálo za to změřit Vlastní (`bench.ps1 -Extra "-cvdetail","1,1,1,1"` apod.). Čeká se, až uživatel posoudí nové tvory (v0.103.0) a vyzkouší úrovně kvality.

## Stav
- Větev `newFeatures`, do `main` se slučuje jen na pokyn uživatele. Naposledy sloučeno 2026-11-28: `alpha_10` je na v0.109.1, `main` posunutý na v0.109.3 (obnovené README a obrázky).
- Výkon: ~350 fps v okně 1080p na Lenovu (v0.100.0). Na DELLu (Intel UHD) 58 fps v 1080p, 111 v 720p (v0.100.20); ve Full HD na Střední (automaticky) 96 fps na startu a 81 mezi nepřáteli, na Vysoké 82/70 (v0.106.0).
- **DELL je připravený k vývoji (2026-11-27):** Git 2.56.1, Unity 6000.3.28f1 + IL2CPP (navíc je tam 6000.6.11f1, projekt ho nepoužívá), Visual Studio 2026 s C++ a Windows SDK 10.0.26100, Blender 5.3 s doplňkem MCP od Blender Lab, uv 0.13.10 a Python 3.12.16, Smart App Control vypnuté. MCP for Unity a Blender MCP fungují, keep-awake běží, Remote Control pro nové sessions zapnutý. Úskalí jsou v `Docs/claude/dell-setup-pitfalls.md`.

## Další na řadě
Všechno, co se má, může nebo čeká na rozhodnutí, je v **`Docs/Backlog.md`** (s dopady a odkazy). V sekci „Musí se“ zbývá jen posouzení přestavěných tvorů uživatelem.

## Rozhodnutí uživatele, která ještě nejsou v CHANGELOGu
- (zatím nic)
