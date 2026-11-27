# Crimson Veil – pokyny pro Claude

2.5D akční plošinovka v Unity 6000.3.28f1 (URP). Šedý tušový svět, jen karmínové detaily. Hrdina má magickou šálu, která se mění ve zbraně. Kód je v `Assets/_CrimsonVeil`.

## Nejdřív
- Hook SessionStart (`.claude/settings.json` → `Tools/session-start.ps1`) na začátku každé session sám udělá `git fetch` a `git pull --ff-only` a předá ti celý `Docs/Rozpracovano.md`. Když hlásí chybu (pull selhal, offline), vyřeš to dřív, než začneš pracovat.
- **Předávka průběžně:** uživatel může kdykoli přejít na druhý počítač a ta session nevidí naši konverzaci. Po každém dokončeném kroku proto aktualizuj svou sekci „Poslední session“ v `Docs/Rozpracovano.md` (o čem jsme mluvili, hotovo, nehotovo, rozdělané, rozhodnutí, další krok), commitni a pushni. Hook Stop (`Tools/session-stop.ps1`) tě vrátí, když zůstanou nepushnuté commity nebo necommitnutá předávka či backlog.
- **Backlog (`Docs/Backlog.md`):** všechno, co s uživatelem probereme a nedělá se hned (nápad, „uděláme později“, otevřená otázka, chyba mimo úkol), zapiš tam ještě v téže odpovědi: co, proč, dopady, stav, odkazy. Nic takového nesmí zůstat jen v konverzaci. O pořadí v sekci „Musí se“ rozhoduje uživatel.
- **`git pull`, pak přečti `Docs/Rozpracovano.md`** (kdo kde pracuje, co je rozdělané a co je na řadě) a v `Docs/claude/README.md` poznámky k úkolu. Vývoj běží na dvou počítačích (Lenovo, DELL) a jediné, co sdílejí, je tento repozitář.
- **Na konci úkolu** aktualizuj `Docs/Rozpracovano.md`, commitni a pushni. Nové poznatky o projektu zapisuj do `Docs/claude/`, ne do místní paměti Claude.

## Komunikace
- S uživatelem mluv **česky**, včetně průběžných zpráv. Commit messages a komentáře v kódu piš anglicky.
- Uživatel často pracuje vzdáleně (Remote Control). Piš stručně a srozumitelně.

## Každý commit (vynucují to hooky v `.githooks/`)
- Vlastní verze: nová sekce `## [X.Y.Z] – RRRR-MM-DD` v `CHANGELOG.md` (česky, Keep a Changelog). Hook post-commit vytvoří tag `vX.Y.Z`, pre-push nepustí commit bez tagu. **Nikdy `--no-verify`.**
- Po klonování je nutné: `git config core.hooksPath .githooks` a `git config push.followTags true`.
- K tomu testy, aktualizovaný README (počty testů) a push větve i tagu.
- Commit message končí řádkem `Co-Authored-By: Claude <noreply@anthropic.com>`.
- `git add` jen vlastní soubory, nikdy `Assets/_Recovery`, `Assets/Resources.meta` ani dočasné `InitTestScene*.unity`.
- Milníkové tagy se jmenují `alpha_N` (podtržítko, ne pomlčka).

## Větve
- Práce probíhá na `newFeatures`. **Do `main` slučuj jen na výslovný pokyn uživatele.** Sloučené větve maž.

## Unity přes MCP
- MCP for Unity (`com.coplaydev.unity-mcp`) na `http://127.0.0.1:8080/mcp`. Server se spouští sám při startu editoru (Window → MCP for Unity, jinak Start Server → Connect).
- **Před každým `refresh_unity` a spuštěním testů ověř `EditorApplication.isPlaying`.** Uživatel často hraje, zatímco pracuješ. Za běhu hry nikdy nepřekládej.
- PlayMode testy spouštěj s `init_timeout` 120000.

## Testy
- Každý pohyb a kombo má PlayMode test na skutečném prefabu hrdiny (`HeroMoveSetPlayTests`).
- Malé změny: celý EditMode a PlayMode dotčené kategorie (`Assets/_CrimsonVeil/Tests/KATEGORIE.md`). Po větším celku celý PlayMode. Nové testovací třídy musí mít `[Category]`.
- V CHANGELOGu uveď, které testy proběhly.

## Grafika a obsah
- **Žádné hotové postavy ani balíčky prostředí** (asset store apod.). Modely se staví skripty v `ArtSource/*_build.py` v Blenderu. Před jakýmkoli stahováním se zeptej.
- Záběry postav bez trávy a rušivého pozadí. Modely budou i v cutscénách, proto kontroluj detaily ze všech stran.
- Galerii animací ani média v README nepřetáčej sám, jen když o to uživatel požádá.

## Výkon
- Cíl je ~250 fps. Měř **sestavenou hru**, ne editor: `-cvbench soubor`, `-cvres 1920x1080` (měř v okně, celá obrazovka se zarovnává na obnovovací frekvenci), `-cvoff Jméno,Jméno`, `-cvoffmat Materiál`, `-cvshot snímek.png`. Viz `Tools/bench.ps1`.
- Buildy jsou IL2CPP + Direct3D 11. IL2CPP potřebuje modul Unity „Windows Build Support (IL2CPP)“ a Windows SDK.

## Prostředí
- Na Windows volej Python přes `uv` (`%USERPROFILE%\.local\bin\python.exe`). Holé `python` vede na stub z Microsoft Store.
- `Tools/keep-awake.ps1 -Start` drží počítač vzhůru při dlouhých úlohách. Nevypínej ho sám.
- Nový počítač: postup je v `Docs/NovyPocitac.md`.
