# Changelog

Všechny podstatné změny projektu Crimson Veil.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/), verze podle [SemVer](https://semver.org/lang/cs/).

## [Unreleased]

## [0.30.0] – 2026-11-19
### Opraveno
- **Vlasy hrdiny.** Vlasová hmota tvořila „čepici" s ostrou hranou přes čelo jako hledí a z čela rostl hřeben bodlin. Teď:
  - hmota se nad čelem zanoří pod kůži a vynoří se až na vlasové linii vysoko na čele (`HAIRLINE`);
  - špičatá ofina roste z vlasové linie, ne z čela;
  - boční póly vlasové hmoty jsou uvnitř lebky, takže na tváři už není plochá „deska" až k čelisti;
  - pod obličejem je hmota stažená hluboko do hlavy, takže na lících nic netrčí.
- **Krátká chodidla.** Bota přečtená z kresby měřila asi 17 cm. Je natažená na 26 cm, což odpovídá postavě vysoké 1,83 m (`BOOT_LENGTH`). Výška a tvar boty zůstávají podle kresby.

### Změněno
- **Plynulejší útoky** (`HeroAttackChoreography`). Pohyb mezi klíčovými pózami se dřív na každé zastavil (zpomalení na nulu u každé klíčové pózy), proto působil topornými zastávkami. Teď:
  - póza vede plynulou křivkou (Hermite) skrz klíčové pózy a zpomaluje jen při odchodu z bojového postoje a při návratu do něj;
  - klouby se lámou postupně: trup vede (`TorsoLead`), paže jde za ním, předloktí švihne až po rameni (`ElbowLag`) a hlava se dorovná nakonec (`HeadLag`). Úder tak projde tělem jako vlna.
- **Rozběh a zastavení** (`HeroPoseSolver`). Ovládání zůstává ostré (rychlost se mění za desetinu sekundy), mění se jen pohyb těla:
  - póza do běhu a z něj přechází přes několik snímků, ne skokem. Délka kroku dál sedí na skutečnou rychlost, takže nohy neklouzají;
  - pružina „váhy těla": při rozběhu se postava předkloní do prvních kroků a ruce se stáhnou dozadu;
  - při zastavení trup setrvačností přepadne přes zapřené nohy, kolena to zachytí, ruce švihnou dopředu, pak se tělo zhoupne zpět a usadí.
  - Při úderu se pružina nepoužije, protože výpad má vlastní náklon.
- **Ruce při stání** (`HeroPoseSolver.IdleLife`). Ruce se ve smyčce stání skoro nehýbaly. Teď:
  - jdou s dechem o chvilku později než hrudník;
  - každá se houpe vlastním pomalým rytmem a lokty se povolují a přitahují;
  - váha se pomalu přenáší a hlava se lehce pohupuje.
- Inspirace:
  - Valve a Gears of War (SIGGRAPH) k rozběhům a zastavením;
  - rozbory úderu nápřah–úder–dotažení (Sakurai, Dark Souls);
  - principy idle animace (dech a navazující pohyb).

### Testy
- `HeroModelTests`: bota je dlouhá 24–30 cm.
- `HeroPoseSolverTests`:
  - úder se na klíčových pózách nezastaví;
  - předloktí se narovná až po rameni;
  - rozběh i zastavení předkloní tělo, zastavení ho přehoupne zpět a usadí;
  - póza do běhu najede postupně;
  - ruce při stání žijí a nejsou zrcadlové.
  - Test bodnutí kopím čte natažení o `ElbowLag` později.

## [0.29.0] – 2026-11-19
### Přidáno
- **Hrdina má nové kosti v oblečení a to vlaje.** Model staví `ArtSource/hero_build.py`. Do tuniky a šerpy, které byly dosud přišité k bokům a stehnům, teď přidává kosti:
  - **tunika** má volné díly pod rozparky, přední a zadní, každý rozdělený na levou a pravou půlku. Celkem 4 řetízky po 2 kostech (`Cloth.N.0..1`), visí z boků. Dole u lemu kmitá 60 % látky na pružině a zbytek dál sleduje boky a stehno, takže ji krok pořád nese;
  - **oba konce rudé šerpy** mají po 3 kostech (`Sash.N.0..2`) a od uzlu dolů se volně houpou.
- Ve hře je pohání dvě nové `SpringBones`:
  - tunika je tužší a drží se mimo nohy;
  - šerpa je lehká a rozhoupe se do široka.
- `SpringChainSolver` umí nově:
  - **srážky s válci** (`Capsule`, `PushOut`): látka se drží mimo stehna a lýtka a neprojde jimi;
  - **omezení ohybu** (`MaxBend`): díl tuniky se vychýlí nejvýš o 32° od své polohy, takže se při běhu neodklopí do ostrého „ocásku".
- `SpringBones` má nová nastavení `colliderBones`, `colliderRadius` a `maxBend`.

### Testy
- `HeroModelTests`: tunika má 4 a šerpa 2 řetízky, visí z boků a opravdu hýbou sítí.
- `SpringChainSolverTests`:
  - bod se vytlačí z válce;
  - řetízek rozhoupaný do nohy zůstane mimo ni;
  - žádný článek se nevychýlí za `MaxBend`.

## [0.28.1] – 2026-11-18
### Opraveno
- **Kamenné oblouky v pozadí už nevisí ve vzduchu.** U věže a hradeb stálo 9 oblouků v blízké vrstvě ve výšce 23–44 m. Byly postavené od oka proti věžím v hlubší vrstvě a v jejich vlastní vrstvě pod nimi nic nebylo. Jakmile se kamera pohnula, vrstvy se rozjely a oblouky visely.
- Nový průchod builderu scény `GroundedProps.Ground` platí pro oblouky, sloupy, zdi, schody, sochy a mrtvé stromy v pozadí:
  - kulisa nad podlahou své vrstvy (tam, kde ji neschová mlha) musí mít obě nohy na kameni své vrstvy;
  - když je nemá, posadí se na nejbližší rovný kamenný vrchol, který ji unese a nad kterým má místo. Přednost má vlastní vrstva, jinak vrchol věže v hlubší vrstvě;
  - když takový vrchol není, kulisa zmizí.
  - V prototypu se 5 kulis přesadilo a 2 zmizely.
- Scéna je přestavěná.

### Testy
- Nové `GroundedPropsTests`: každý oblouk, sloup a zeď v pozadí stojí na podlaze své vrstvy, nebo oběma nohama na kameni.

## [0.28.0] – 2026-11-18
### Přidáno
- **Nové animace létajících nepřátel** (`FlyerAnimation`). Křídla mávají podle letu:
  - rychlý silový záběr dolů a pomalejší návrat nahoru;
  - při stoupání mávají rychleji a naplno;
  - při klesání plachtí s křídly otevřenými a mávají jen mělce.
- **Můra:**
  - každý záběr dolů tělo nadzvedne;
  - zásah ji roztočí;
  - mrtvá složí křídla a padá ve spirále jako list.
  - **Nápřah před popelem (0,7 s):** zastaví se ve vzduchu, trochu vystoupá, roztáhne křídla s rudými oky naplno k hrdinovi a chvěje se. Pak popel vytřese prudkým tleskáním křídel.
- **Vrány:**
  - tělo se naklání do letu, při střemhlavém náletu zobákem dolů a při vybírání nahoru;
  - v náletu mají křídla přitažená.
  - **Nápřah před náletem (0,7 s):** hejno se stáhne těsně k sobě, vystoupá a mává usilovněji.
- **Bludička:**
  - lucerna se houpe na svém kroužku a zaostává za směrem, kterým ji bludička nese (kyvadlo);
  - plamen dýchá a při prudkém zhoupnutí se protáhne;
  - po zásahu se lucerna prudce rozhoupe, uklidní se a plamen vzplane.
- **Papírový drak:**
  - nakloní se do náletu;
  - papír se třepetá tím víc, čím rychleji letí, a plachta se vydouvá.
  - **Nápřah (0,5 s):** když se hrdina dostane na dosah, drak se nejdřív zakloní na druhou stranu a třese se, a teprve pak nalétne.

### Testy
- Nové `FlyerAnimationTests`:
  - záběr dolů je rychlejší než návrat nahoru;
  - při stoupání mávají častěji, při klesání plachtí;
  - náklon jde zobákem dolů v náletu a nahoru při stoupání;
  - nesená lucerna se zhoupne dozadu a pak se srovná;
  - drak se zakloní od hrdiny.
- `CreaturePlayTests`: můra ve hře opravdu mává a záběr dolů je rychlejší než nahoru.

## [0.27.0] – 2026-11-18
### Přidáno
- **Zbraň i při visení na římse.** Seslat zbraň šlo i dřív, ale póza držela obě ruce na hraně, takže čepel jen trčela u římsy a úder se neodehrál. Teď:
  - levá ruka drží římsu;
  - pravá se pustí a vezme zbraň (`HeroPoseSolver.HangFreeArm`, `OneHandedHang`);
  - úder se švihne jen touto rukou, trup se do něj trochu natočí a nohy zůstanou zapřené o zeď;
  - prsty pravé ruky přejdou z úchopu hrany na úchop zbraně.
- **Nové animace nepřátel** (`CreatureAnimation`). Pohyb je řízený uraženou vzdáleností, takže končetiny nehrabou na místě. Útoky mají nápřah, úder a dotažení.
  - **Přízrak:**
    - při honičce se shrbí a drápy se střídavě natahují a hrabou dozadu;
    - při nápřahu se vzepne dozadu a nahoru, drápy zvedne nad hlavu dopředu a nahoře se třese;
    - úder vede nejdřív jedna ruka a druhá ji následuje, tělo se vrhne dopředu a dolů;
    - po úderu drápy přešvihnou, tělo se zhoupne a sebere se;
    - zásah ho odhodí a zase pruží zpět.
  - **Mnich se štítem:**
    - těžký krok, při kterém váha dosedá na každou nohu;
    - ruka na straně kamery drží štít a druhá se houpe;
    - před výpadem se zapře a stáhne za štít;
    - při výpadu se za štítem vrhne celým tělem a ruku propne;
    - bez štítu se shrbí a tápe rukama.
  - **Kořenový běžec:**
    - před skokem se přikrčí, v letu se natáhne a nakloní do skoku, při dopadu se zmáčkne;
    - kořeny se v letu stáhnou a při dopadu se rozprostřou;
    - když visí, dýchá.

### Testy
- `HeroPoseSolverTests`: při visení se zbraní pravá ruka pustí hranu a úder opravdu švihne, levá ruka a nohy drží.
- Nové `CreatureAnimationTests`:
  - drápy přízraka se střídají podle uražené dráhy, ne podle času;
  - nápřah, úder a dotažení jdou ve správném pořadí a navazují bez skoku;
  - mnich má těžký krok, stáhne se a vrhne se do výpadu;
  - kořenový běžec se zmáčkne, natáhne a nikdy se nezdeformuje.

## [0.26.6] – 2026-11-18
### Opraveno
- **Létající nepřátelé už neprolétají překážkami.** Můra, bludička, papírový drak a vrány se pohybovaly bez kontroly prostředí. Teď se pohybují přes společný `CreatureMotion.Slide`:
  - koule kolem těla se zastaví u zdi, římsy nebo stropu a klouže podél nich;
  - hrdina, jiní tvorové a cokoli, co jde zasáhnout, překážkou nejsou;
  - kdo začne uvnitř kamene, smí z něj ven.
  - Můra (poloměr 0,45 m) se třepetá podél zdi, místo aby jí prolétla.
  - Drak na niti se při náletu zastaví o překážku.
  - Sestřelený drak padá a zastaví se o zeď.
  - Chodci (přízrak, mnich, stín, kořenový běžec) už předtím měli paprsek proti zdi a okraji, u nich se nic nemění.

### Testy
- `CreaturePlayTests`:
  - pohyb se zastaví u zdi, klouže podél ní a jiný tvor překážkou není;
  - můra se třepetá u zdi, ale nikdy jí neprostrčí křídla.

## [0.26.5] – 2026-11-18
### Opraveno
- **Klouzání po zdi už není nekonečný pomalý výtah.** Když se hrdina tlačí do zdi, nejdřív se drží a klouže pomalu (2,3 m/s), ale jen 0,5 s. Pak mu stisk povolí a klouže rychleji a rychleji, za 0,9 s až 8 m/s, pořád pomaleji než volný pád. Když se od zdi pustí a chytí ji znovu, drží se zase. Odraz od zdi funguje kdykoli.
- **Řetězy, klece a prapory v pozadí už nevisí ze vzduchu.** Byly rozmístěné od oka podle věží ve vzdálenější vrstvě. Vrstvy se ale při pohybu kamery posouvají různě, takže věci visely v prázdnu. Generátor levelu je teď ukotvuje (`HangingProps`):
  - každá věc hledá nad sebou kámen ve své vrstvě a řetěz k němu dosáhne;
  - když tam nic není, posune se kousek do strany nebo se přesune do vrstvy věže za ní, před její zeď;
  - prapor může viset přímo na zdi, ale jen když má zeď za sebou po celé ploše;
  - věc, která nemá kde viset, zmizí (12 z 37).

### Testy
- `WallLedgeMotorTests`: hrdina se nejdřív drží, pak klouže rychleji, nikdy nezpomalí, a po znovuchycení se zase drží.
- `HangingPropsTests`:
  - délky článků řetězu (jen stlačené, nikdy natažené);
  - každý řetěz, klec a prapor ve scéně má vršek v kameni své vrstvy nebo zeď za sebou.

## [0.26.4] – 2026-11-18
### Odstraněno
- **Šála už nechytá hrany.** Hrany chytají jen ruce (do 2,1 m nad chodidly). Šála se jako bič zachytává jen o kruhy. Zmizelo vytahování šálou k vyšší hraně, jeho cena v síle šály (`LedgeCatchCost`) i šňůra kreslená k hraně.

### Testy
- `CastingPlayTests`: hrana mimo dosah rukou se nechytí a hrdina spadne zpátky ke zdi.
- Testy chycení hrany šálou (`ScarfStrandPlayTests` a test pouštění hrany v `TraversalPlayTests`) jsou odstraněné.

## [0.26.3] – 2026-11-18
### Opraveno
- **Hrdina už nezůstane „přivázaný“ k hraně.** Když šála chytila hranu mimo dosah rukou a hrdina během vytahování (0,22 s) odskočil od zdi, pustil se nebo dostal ránu, vytahování se nikdy neukončilo. Mezi rukou a starou hranou pak zůstala natrvalo nakreslená červená čára. Vytahování teď skončí, jakmile hrdina přestane viset.
- **Lano už není dvojité a nevypadá jako červená čára.**
  - Zmizela samostatná plochá čára (`LineRenderer`), která se kreslila přes látku biče. Lano je teď jen samotná šála.
  - Při švihu na kruhu je bič napnutý: prohýbá se o 3 cm místo 12, takže vedle sebe nevznikají dvě čáry.
  - Natažené lano drží vzhled zbraně. Prameny se kolem sebe proplétají po celé délce až ke kruhu s pevnou roztečí v metrech, takže se na 7 m dlouhém laně nerozmáznou do rovné čáry. Lano je stejně silné po celé délce a jen o málo silnější než bič.
  - Chycení hrany šálou: šála sama vystřelí od krku napnutě k hraně jako spletená šňůra a konec se kolem hrany omotá. Po vytažení se pustí a vrátí se do látky.

### Testy
- `TraversalPlayTests`: když hrdina během vytahování šálou odskočí, šála hranu pustí.
- `ScarfStrandPlayTests`:
  - konec šály je u hrany;
  - šňůra vede od krku k hraně;
  - u krku zůstává látka;
  - po vytažení je šála zase látkou.
- `VeilWeaponLookTests`:
  - proplétání lana drží rozteč v metrech;
  - všechny prameny vedou až ke kruhu;
  - prameny zůstávají na laně;
  - šířka lana.

## [0.26.2] – 2026-11-15
### Opraveno
- **Plížení už není cupitání po špičkách.**
  - Chodidla stojí naplocho, protože úhel chodidla vyrovnává sklon holeně (`HeroPoseSolver.FlatFoot`). Dřív byla špičkou dolů o 50–60°.
  - Krok je delší a pomalejší: jeden cyklus na 1,6 m (`CrouchStride`) místo 2,2× zrychleného cyklu. Plížení je pomalejší (2 m/s místo 2,6), takže vychází asi 2,5 kroku za sekundu místo skoro 4.
  - Noha vpředu se zvedá nad zem, pata vzadu se při odrazu zvedne a prsty se ohnou. Tělo je nejníž, když jsou nohy od sebe.
- Na stránce s animacemi přibyl klip plížení a skluz je přetočený s novým plížením na konci.

### Testy
- `CrouchSlideMotorTests`:
  - chodidla naplocho;
  - nohy se střídají po půl cyklu;
  - nejvýš 3 kroky za sekundu.

## [0.26.1] – 2026-11-15
### Změněno
- **Ctrl + šipka dolů míří, nepřikrčuje.** S držením Ctrl (útok z místa) šipka dolů slouží k míření rány dolů, hrdina se nepřikrčí. Pod nízkým stropem zůstává přikrčený i tak.
- Stránka s animacemi je aktualizovaná na stav 0.26:
  - nové klipy: přikrčení a narovnání, dash, skluz, vylézání na římsu a luk nad hlavu;
  - přetočené klipy: míření a nový přízrak;
  - popisy odpovídají současnému chování.

### Testy
- `CrouchSlideMotorTests.Planted_DownAims_ItDoesNotCrouch`.

## [0.26.0] – 2026-11-15
### Přidáno
- **Přikrčení:** šipka dolů na zemi. Hrdina se skrčí (hluboká kolena, tělo nad nimi, hlava nahoru) a s šipkou do strany se plíží rychlostí 2,6 m/s. Tělo je pak vysoké jen 1 m, takže proleze pod nízkým stropem.
- **Skluz:** šipka dolů + Shift (s šipkou doleva nebo doprava). Hrdina vyjede po zemi rychlostí 13 m/s, zpomalí na plížení a ujede přes 4 m.
  - Póza: zaklonění, přední noha natažená, zadní pod tělem, ruka se táhne po zemi. Skluz končí v přikrčení.
  - Do zdi se zastaví. Z hrany přejde v pád a hybnost nese dál. Skokem se dá přerušit.
  - Shift bez šipky dolů je dál obyčejný dash.
- **Nízký strop:** pod ním se hrdina nedokáže narovnat ani vyskočit. Zůstane přikrčený, i když šipku dolů pustí, a vstane až venku.
- **Tunely v levelu:**
  - Na plošině před zvonicí 5 m dlouhý tunel s mezerou 1,2 m.
  - V zahradě 6 m dlouhý tunel s mezerou 1,1 m: dá se proplížit, skluzem je to rychlejší.
  - Oba jsou příliš vysoké na přeskočení, i dvojskokem.
  - Mnichové a přízrak jsou přesunutí za tunely, kontrolní bod před tunel na plošině.

### Testy
- `CrouchSlideMotorTests`:
  - přikrčení a plížení;
  - pod nízkým stropem nejde vstát ani skočit;
  - skluz je rychlý, zpomaluje a ujede přes 3 m;
  - skluz jde směrem šipky;
  - dash bez šipky dolů zůstává dashem;
  - skluz do zdi a z hrany;
  - pózy jsou nízko.
- `LevelExtensionTests.TheTunnels_MustBeCrawledOrSlid`: tunel je dost vysoký pro skrčené tělo a nízký pro stojící; přes něj se nedá přeskočit.
- `LevelExtensionPlayTests` (ve skutečné scéně):
  - stojící hrdina do tunelu na plošině nevejde, přikrčený ho proleze a venku vstane;
  - zahradním tunelem projede skluzem rychleji než plížením.

## [0.25.0] – 2026-11-15
### Přidáno
- **Měření playtestu** (`PlaytestLog`, vzniká s HUD):
  - Každá hra se měří: kdy hrdina poprvé vstoupil do kterého úseku levelu, jak dlouho v něm byl, kolikrát v něm zemřel nebo spadl, a celkový čas. Hra končí u koncové zdi.
  - Tabulka se průběžně ukládá do `Playtests/playtest_<datum>_<čas>.txt` ve složce projektu (ve hře mimo editor do datové složky hráče). Zapisuje se při každém novém úseku, smrti, konci a každých 30 s, takže přežije i zastavený Play.
  - Neukládá se hra kratší než 10 s ani hra, ve které hrdinu někdo přesunul (testy, ladicí teleporty). Automatické testy zápis vypínají.
  - Složka `Playtests/` je v `.gitignore`.
- **Čas, smrti a aktuální úsek** jsou vidět v titulku mapy (TAB).
- 13 pojmenovaných úseků levelu: od startu přes věž, hradby, studnu, zvonici, hřeben a propast po zahradu (`PlaytestModel.PrototypeSections`).
- `PlayerController.Fell`: událost pádu pod level, počítá se jako smrt.

### Opraveno
- Mapa a měření se po načtení nového levelu nebo nového hrdiny napojí na něj, místo aby sahaly na zničený objekt.

### Testy
- `PlaytestModelTests`:
  - časy úseků od prvního vstupu;
  - konec hry zastaví hodiny;
  - smrti se počítají po úsecích;
  - čitelná tabulka;
  - úseky levelu jsou správně seřazené.
- `PlaytestLogPlayTests` (ve skutečné scéně):
  - úseky se zaznamenávají;
  - pád se počítá jako smrt;
  - přesunutý hrdina zneplatní běh;
  - smrti jsou vidět na mapě.

## [0.24.0] – 2026-11-15
Level je zase delší (konec z x 224 na 340) a mnohem vertikálnější: hlavní cesta vede od −14 m až do 45 m.

### Přidáno
- **10. Zvonice (x 222–240):**
  - Vchází se pod její stěnou. Schod a pět říms střídavě vlevo a vpravo vedou 31 m nahoru (od 7 do 38 m).
  - Šachta na skoky ode zdí u pravé stěny, kruh pro bič.
  - Vyhlídka nad pilířem s urnou a tajemstvím.
  - Kořenový běžec nad vchodem, přízrak, můra, kontrolní body.
- **11. Hřeben (x 240–282, 38–45 m):** ze střechy zvonice po plovoucích kamenech vysoko nad údolím. Papírový drak, havrani, kruh pro bič, mnich na konci.
- **12. Propast (x 282–310):** sestup po sedmi římsách střídavě vlevo a vpravo z 44 m až na dno v −14 m. Lucerna, kořenový běžec pod římsou, kruh, přízrak.
- **13. Výstup z propasti:** 16 m vysoká šachta na skoky ode zdí.
- **14. Zahrada a konec (x 312–340):** mnich, přízrak, urna, koncová zeď.
- Kamerové zóny pro zvonici a propast.
- Pozadí do výšky: velké věže, oblouky, prapory, řetězy, klece, vodopád, akvadukt a hrad.

### Testy
- `LevelExtensionTests`:
  - všechny šachty jsou správně (vejde se pod pilíř, šířka 2,8–3,6 m, zeď sahá k římse a nahoru k výstupu);
  - level sahá za x 330;
  - výškový rozdíl hlavní cesty je přes 50 m.
- `LevelExtensionPlayTests` (ve skutečné scéně):
  - vstup do zvonice a výstup její šachtou;
  - stoupání po římsách zvonice;
  - přeskoky po kamenech hřebene;
  - sestup do propasti;
  - výstup šachtou z propasti do zahrady.

## [0.23.0] – 2026-11-15
### Přidáno
- **Mapa levelu na TAB** (na gamepadu tlačítko Select / View): otevře se a zavře stejnou klávesou, hra běží dál.
  - Terén se kreslí světlým inkoustem na tmavém listu. Horní hrany jsou zvýrazněné, aby byly vidět římsy.
  - Hrdina je karmínový kosočtverec, který se posouvá s ním. Kontrolní body jsou světlé kosočtverce, kruhy pro bič malé čtverečky.
  - Mapa se kreslí sama z bloků terénu (`TerrainBlock`), takže sedí na každý level a po každé úpravě. Vzniká spolu s HUD, nic se nemusí zapojovat.
- **Připraveno na mlhu:**
  - Teď je vidět celý level (`Minimap.revealAll`).
  - Mapa už si pamatuje, kudy hrdina prošel (do 9 m kolem něj, `revealRadius`).
  - Po vypnutí `revealAll` zůstane zbytek v mlze a odkryje se až tam, kam hrdina dojde. Značky v mlze se skryjí.
- Logika mapy je oddělená od UI (`MinimapModel`): mřížka po 25 cm, pevná zem, prozkoumané buňky a převod ze světa do obrázku.

### Testy
- `MinimapModelTests`:
  - mřížka pokrývá level, ne hluboké nitro země;
  - zem je označená;
  - celá mapa je vidět, a přesto se pamatuje, kam hrdina došel;
  - v mlze je vidět jen prozkoumané;
  - převod ze světa do obrázku.
- `MinimapPlayTests` (ve skutečné scéně):
  - mapa existuje a je zavřená;
  - TAB ji otevře a ukáže celý level od začátku do konce;
  - značka hrdiny jde s ním a sedí na jeho místě;
  - druhé stisknutí ji zavře.

## [0.22.0] – 2026-11-15
Level je skoro dvakrát delší (konec z x 117 na 224) a hodně vertikální: stoupá do 25 m a klesá do studny v hloubce −4 m.

### Přidáno
- **6. Věž (x 115–133):**
  - Vchází se pod její stěnou.
  - Uvnitř se po čtyřech římsách stoupá střídavě vlevo a vpravo, vždy o 4 m.
  - Pak šachta na skoky ode zdí (3,2 m široká, vchází se pod visícím pilířem) až na vrchol ve 25 m.
  - Navíc kruh pro švih bičem.
  - Kořenový běžec pod římsou nad vchodem, přízrak, popelavá můra, urny a dva kontrolní body.
- **7. Hradby (x 133–176, 24–26,5 m):**
  - Vysoké plošiny s mezerami nad propastí. Nejširší mezera (6 m) se přeskočí dvojskokem, nebo se dá přelétnout švihem z kruhu.
  - Mnich se štítem, papírový drak, havraní roj, přízrak.
- **8. Studna (x 176–200):** sestup z římsy na římsu až na dno v −4 m. Kořenový běžec pod poslední římsou, lucerna, přízrak.
- **9. Výstup:** po schodech ze studny nahoru na poslední plošinu s mnichem a přízrakem, na konci zeď.
- **Kamerové zóny** pro věž a studnu se širším záběrem.
- **Pozadí do výšky:**
  - zvětšené věže, oblouky hradeb, prapory, řetězy a klece;
  - vodopád a rozšířené pozadí, mlha a světelné paprsky po celé délce.
- Tvar nové části je popsaný daty (`PrototypeSceneBuilder.Extension`, `ExtensionRoute`), aby šla hlavní cesta zkontrolovat testy.

### Testy
- `LevelExtensionTests` (bez hry, z dat levelu):
  - každý krok hlavní cesty je na dosah hrdiny (výšlap do 4,6 m, mezera při výšlapu do 3 m, skok, dvojskok, seskok);
  - na žádnou visící římsu se neskáče zespodu;
  - šachta je správně široká a dá se do ní vejít;
  - nad každou plošinou je místo, kde stát;
  - level je delší a vertikální.
- `LevelExtensionPlayTests` (ve skutečné scéně s řízeným ovládáním):
  - vstup do věže pod zdí;
  - výstup šachtou skoky ode zdí až na vrchol;
  - z věže po hradbách na vyšší hradbu;
  - dvojskok přes nejširší mezeru;
  - výstup ze studny po schodech na plošinu.

## [0.21.2] – 2026-11-15
### Opraveno
- **Vylézání na římsu:** ruce dřív sledovaly ručně klíčovanou pózu a během přítahu se od hrany vzdalovaly. Teď na hraně zůstávají.
  - Nová dvoukloubová IK paže (`LimbReach`) drží zápěstí na skutečné hraně:
    - při visu a přítahu prsty zahnuté přes roh;
    - při vzepření dlaně naplocho na horní ploše.
  - Lokty se ohýbají dozadu a dolů, od zdi.
  - Hranu ruce pustí po vzepření, kdy jde koleno nahoru a tělo už je mimo dosah (`HeroPoseSolver.LipHold`, `LipHandOffset`).
  - Ve hře změřeno: ruce jsou od hrany nejvýš 8 cm.
- **Luk mířený nahoru:** postava se už nezaklání, stojí rovně a luk zvedají jen paže (`BowUpLean`). Mířením dolů se tělo dál sklání do rány.

### Testy
- `LimbReachTests`:
  - kloub drží délky kostí a ohýbá se k pólu;
  - mimo dosah se paže natáhne;
  - řešení postaví ruku na cíl s loktem od zdi;
  - ruce drží hranu do konce vzepření.
- `VeilAimTests`: lučištník při míření nahoru zůstává vzpřímený.

## [0.21.1] – 2026-11-15
### Opraveno
- **Hrdina visel na římse rukama ve vzduchu, asi půl metru od skály.**
  - Hrana se měřila paprskem, který narazil na vlastní kolizi hrdiny, takže vyšla o kus vedle.
  - Teď se hrana měří přímo pod horní plochou a hrdinovy kolize se nepočítají (`PlayerController.LipPoint`).
  - Hrdina visí za hranou na dosah rukou (`HangReach`), u vyboulené stěny nikdy ne v ní (`HangX`). Vylézá se na místo nad skutečnou hranou.
  - Paže při visu míří přímo na hranu podle její skutečné polohy (`HeroPoseSolver.HangArm`).
  - Ověřeno ve hře na plovoucí skále ze scény.
- **Kořenovému běžci procházely nožičky stropem i zemí.**
  - Kořeny se rozevíraly do celého kruhu, teď jen do vějíře ±72° směrem od povrchu, na kterém visí nebo stojí, a kmitají o ±12° (`RootCrawlerLogic.RootAngle`).
  - Mez je změřená na modelu: ohnuté kořeny při víc než ~85° klesnou pod povrch.

### Testy
- `LedgeReachRootTests`:
  - poloha visu u kolmé, podříznuté a vyboulené stěny;
  - paže míří na hranu;
  - vějíř kořenů;
  - žádný kořen modelu nesahá pod povrch ani při kmitání.
- `TraversalPlayTests.HangingFromALedge_TheHandsAreOnTheLip_NotInTheAir`: skutečnou fyzikou se změří hrana a ověří, že hrdina visí na dosah za ní.

## [0.21.0] – 2026-11-15
Útok z místa (Ctrl), opravené šplhání a luk nad hlavou, mnich otáčí štít a propracovanější modely přízraku, mnicha a oběšence.

### Přidáno
- **Útok z místa:** dokud držíš Ctrl (na gamepadu stisk levé páčky), hrdina na zemi stojí. Šipky ho jen otáčejí a míří, takže jde seknout nahoru a dopředu bez rozběhu.
  - Při držení Ctrl se ruší i výpad vpřed u seku.
  - `IPlayerInput.StandHeld`, `MotorInput.stand`; `ClipRecorder` má povel `stand:1` / `stand:0`.

### Opraveno
- **Vis na římse:**
  - Obě ruce drží hranu před hrdinou (dřív mířily kolmo nahoru za hlavu).
  - Tělo je u zdi, jedna noha se opírá o kámen, druhá volně visí a hlava je zakloněná k hraně.
  - Prsty se kolem hrany sevřou.
- **Vylézání na římsu:** dřív hrdina jen vyjel rovně nahoru a hned stál. Teď je to 0,8 s dlouhý přítah ve čtyřech fázích:
  1. přítah na rukou s pokrčenými lokty, brada k hraně;
  2. vzepření na napjatých pažích s dlaněmi naplocho na horní ploše;
  3. koleno přes hranu;
  4. teprve pak přenesení těla nahoru a postavení.
  - Dráha těla (`HeroPoseSolver.ClimbRiseOver`) je sladěná s pózou, takže nohy neprojdou kamenem a ruce zůstávají na hraně.
- **Luk mířený nahoru** byl za hlavou, teď je nad čelem, kousek před obličejem.
  - Příčina: klíční kost se při zvednuté paži natáčela dopředu a tím paži stáčela dozadu přes hlavu. Teď se to při paži nad vodorovnou postupně vypíná (`HeroBodyMotion.ShoulderReach`).
  - Paže luku se při míření kolmo nahoru drží o kousek zpět (`BowOverheadTrim`).
- **Mnich otáčí štít:** štít drží v prostoru světa čelem ke směru, kam se dívá, a natočený ke kameře. Při otočce ho přenese obloukem před tělem. Ze žádné strany už není vidět jen zadní strana štítu (`ShieldMonk.ShieldHold`).

### Změněno
- **Přízrak:**
  - Kulatá kápě se špičkou svěšenou dopředu a hlubokou tmavou dutinou místo obličeje, v níž hoří oči.
  - Přes ramena roztrhaná pelerína, delší vzadu.
  - Rubáš padá v hlubokých záhybech.
  - Široké řasené rukávy s dlouhým cípem visícím za předloktím.
  - Stuhy rubáše leží podél lemu.
- **Mnich:**
  - Otevřená kápě s dutinou a očima.
  - Hábit v záhybech místo tyček sklad, stažený provazem s uzlem.
  - Pelerína v záhybech, vzadu delší.
  - Široké řasené rukávy, těžké pěsti s klouby a palcem.
  - Stuhy lemu leží podél lemu.
- **Oběšenec:**
  - Vychrtlý trup: žebra, propadlé břicho, kosti hrudní a lopatky.
  - Košile roztržená vpředu a v záhybech.
  - Obličej s nadočnicovým obloukem, lícními kostmi, nosem, ušima a pokleslou čelistí.
  - Smyčka ze dvou pramenů s katovským uzlem.
  - Ruce se čtyřmi prsty a palcem, kostnatá chodidla s prsty.
  - Řetěz na pouti z dalších článků.
- **Sdílená sada `rig_kit.py`:** `folds` pro záhyby a `chain_ribbon(along=...)` pro stuhy ležící podél lemu.

### Testy
- `LedgeStanceBowTests`:
  - z místa se hrdina otočí, ale nerozběhne, a ve vzduchu Ctrl nebrzdí;
  - přítah jde nejdřív nahoru a pak přes hranu;
  - pózy přítahu, vzepření a kolena;
  - úchop hrany;
  - klíční kost u zvednuté paže;
  - luk nahoru je nad čelem;
  - štít mnicha je čelem ven z obou stran.
- `WallLedgeMotorTests`, `HeroPoseSolverTests` a `VeilAimTests` jsou upravené na nové chování.

## [0.20.1] – 2026-11-15
### Opraveno
- Do changelogu chyběl zápis k verzi 0.20.0, je doplněný níže.

## [0.20.0] – 2026-11-15
Nové 3D modely všech nepřátel ve stejném stylu jako přízrak.

### Přidáno
- **Mnich se štítem** (`ArtSource/monk_build.py`) má kostru a skinovaný model.
  - Hábit se záhyby a roztrhaným lemem, plášť přes ramena, kápě s očima.
  - Karmínový provazový pás se šňůrami, růženec.
  - Velký štít ve tvaru draka s okrajem, puklicí a karmínovým okem se stékajícími kapkami.
  - Lem, cípy pláště, šňůry a růženec visí na řetězech kostí, které rozhýbávají `SpringBones`.
- **Oběšenec** (`ArtSource/hanged_build.py`): vychrtlá mrtvola s žebry v roztrhané košili a otrhaných kalhotách, bosá.
  - Má karmínovou smyčku na krku, zlomený krk a hlavu svěšenou na stranu.
  - Na kotníku má železné pouto s utrženým řetězem.
  - Ruce, nohy, hlava i cípy košile jsou volné řetězy kostí. Při houpání se opožďují a při pádu se bezvládně zhroutí.
- **Sdílená sada `ArtSource/rig_kit.py`** pro riggované nestvůry: lofty s váhami kostí, kostra, látkové a končetinové řetězy, export a náhledy.
- **`creature_kit.blade`:** tenká prohnutá plocha se skutečným objemem (peří, blány, papír, kouřové stuhy).
- **`LevelKitFactory.RiggedCreature`** vloží riggovaný model pod `Visual`, natočí ho po směru hry a přidá `SpringBones`.

### Změněno
- **Havran** je modelovaný pero po peru: obrysové peří po celém těle, krční a šíjové peří, dvoudílný zobák se štětinami a oči v důlcích. Ocas tvoří vějíř 10 per a nohy mají šupiny, 3+1 prsty a drápy. Křídlo má ruční a loketní letky a tři řady krovek.
- **Popelavá můra:** chlupatá hruď s límcem, kroužkovaný zadeček s chomáčem, fasetové oči, stočený sosák a šest kloubových chlupatých nohou. Křídla jsou prohnuté blány, žilky, pásy, skvrny a oko se drží jejich zakřivení a po roztrhaném okraji visí třásně šupin.
- **Kořenolez:** propletený uzel silných kořenů s trnitou tlamou a šesti očima v důlcích. Kořeny jsou tři spletené prameny s větvičkami, vlásečnicemi, trny a hákovitým koncem.
- **Papírový drak:** papír se mezi výztuhami vzdouvá a je zmačkaný. Obličej, záplaty i znaky leží na zvlněném povrchu a z úsměvu stéká barva. Roztržené rohy se kroutí, bambus má kolénka a luk je prohnutý. Na koncích visí modlitební proužky.
- **Lucerna:** kroucené pruty, gotické oblouky s kružbami, žebrovaná střecha s ostny, trnitá koruna pod dnem a delší řetěz. Plamen má jazyky a prázdné oči a z lucerny se točí stuhy karmínového kouře.
- **Přízrak** má širší, pomačkané rukávy místo tenkých „hůlek“.
- Mnich přesune štít z modelu pod `Visual` až za běhu. Uvnitř vnořeného prefabu to v editoru nejde.

### Testy
- `BestiaryModelTests`:
  - mnich má štít a řetězy;
  - oběšenec má volné končetiny a hlavu;
  - prefaby mají `SpringBones`;
  - kitové nestvůry mají své díly a dost detailů;
  - křídla havrana mají kloub v rameni.
- `BestiaryModelPlayTests`:
  - mnichův štít je ve hře pod `Visual`, před ním a ve výšce hrudi;
  - končetiny oběšence visí z boků a neodletí.
- `CreatureModelTests` a `EnvironmentDetailTests` počítají i skinované modely.

## [0.19.0] – 2026-11-15
Nový 3D model přízraku (Wraith).

### Přidáno
- **Nový Wraith** postavený skriptem `ArtSource/wraith_build.py`, stejně jako hrdina, takže jde znovu vygenerovat a ladit.
  - Shrbený vychrtlý přízrak asi 2,1 m, vznáší se nad zemí.
  - Hluboká kápě se štítem vystrčeným nad obličej, ve stínu pod ním dvě karmínové oči. Jejich poloha se měří z povrchu kápě, aby nikdy nezapadly dovnitř.
  - Široký rubáš od ramen se při vznášení vleče dozadu a jeho lem je roztrhaný.
  - Dlouhé rukávy se zvonovitě rozšiřují a končí roztřepeným okrajem. Z nich vychází kostnaté předloktí a ruce se třemi dlouhými drápy a palcem.
  - Dole se rubáš trhá na 10 stuh inkoustu, další 2 vlají od ramen. Stuhy jsou široké v boční rovině, kterou vidí hra.
- **Kostra:** původní kosti (`EnemyAnimator` ho dál ovládá), krk, kosti drápů (`Finger.<strana>.<dráp>.<článek>`, `Thumb`) a řetězy stuh (`Tatter.<n>.<článek>`).
- **Stuhy se pohybují pomocí `SpringBones`** (předpona `Tatter.`, volnější a těžší než vlasy hrdiny). Přidává je `LevelKitFactory.WraithStrips` i prefab.
- **Drápy:** při klouzání jsou zahnuté, při nápřahu se rozevřou dokořán (telegraf útoku), úderem se sevřou (`EnemyAnimator.ClawGrip`).
  - `HeroHands` teď zvládne libovolný počet prstů a záporné sevření, kdy se prsty natáhnou a rozevřou.

### Změněno
- Náklony přízraku v `EnemyAnimator` jsou menší. Nový model je už sám shrbený a se starými hodnotami se při pronásledování a úderu sklápěl skoro vodorovně.

### Testy
- `WraithModelTests`:
  - model má tělo, drápy a aspoň 10 stuh;
  - je vysoký kolem 2 m;
  - drápy se sevřou;
  - na nápřah se rozevřou, při úderu sevřou;
  - prefab má na stuhy `SpringBones`.

## [0.18.1] – 2026-11-15
Hrdina je nepatrně nasvícený měsícem.

### Změněno
- **Hrdina zůstává inkoustově černý, ale je u něj vidět 3D tvar.**
  - Shader siluety má nový parametr `_FormLift`. Ke skoro černé barvě přidá trochu chladné šedé tam, kam svítí měsíc: na ramena, temeno, horní strany paží a záhyby tuniky.
  - Násobení barvy, které dosud používali tvorové (`_FormShade`), by na skoro černé nic neudělalo.
  - Hrdina má k tomu slabý okraj (`_RimLight`).
  - Hodnoty jsou nastavené tak, aby byl ve hře pořád tmavší než tráva v popředí: lift 0,03, okraj 0,06 (`LevelKitMaterials.HeroLook`).

### Testy
- `HeroLookTests`: hrdina je černý a nasvícený jen nepatrně. Materiál ve hře odpovídá generátoru materiálů.

## [0.18.0] – 2026-11-15
Celé tělo v pohybu: pánev, krk, klíční kosti, zápěstí, špičky a natáčení trupu.

### Přidáno
- **Kosti prstů u nohou** (`Toe.L/R`, od bříška chodidla ke špičce). Bota se přes ně při odrazu ohne (`hero_build.py`).
- **Nové klouby v animaci** (`HeroAnimator`):
  - krk, klíční kosti, zápěstí, špičky;
  - pánev: náklon i natočení kolem svislé osy;
  - druhé osy kloubů: trup se natáčí kolem svislé osy, paže jdou do strany od těla (`RigJoint.WithSecond`).
- **`HeroBodyMotion`: vrstva pohybu celého těla nad klíčovými pózami.**
  - **Krok:** pánev a hrudník se protáčejí proti sobě. S vykročenou nohou jde dopředu její kyčel a opačné rameno.
  - **Stoj:** pomalé houpání.
  - **Klíční kosti** jdou s pažemi: rameno se natáhne do bodnutí a stáhne při nápřahu.
  - **Paže** visí kousek od těla a při velkém švihu dopředu, dozadu nebo nad hlavu jdou víc do strany, aby míjely boky a hlavu.
  - **Krk** drží hlavu, když se tělo předklání.
  - **Špičky** se ohnou, když se pata zvedá k odrazu. Ve vzduchu volně visí.
  - **Trup** se natočí do úderu, rameno se zbraní jde dovnitř.
  - **Zápěstí** se opožďují za švihem paže jako volná ruka. Ruka se zbraní jen málo, aby jílec zůstal v pěsti a čepel šla s předloktím.
- **Náklon pánve** se odečte z nohou a páteře, takže klíčové pózy zachovají své linie a náklon je vidět v bocích.
- `ClipRecorder.OrbitYaw`: záběr kolem hrdiny, např. ze tří čtvrtin, na kontrolu pohybu do hloubky.

### Testy
- `HeroBodyMotionTests`:
  - pánev a ramena se v kroku protáčejí proti sobě;
  - klíční kosti jdou s pažemi a paže míjejí tělo;
  - špičky se ohnou jen u odrážející nohy na zemi;
  - krk drží pohled;
  - trup se natočí do úderu;
  - zápěstí se opožďují, u zbraně méně;
  - druhá osa kloubu je správně kalibrovaná;
  - model má prsty u nohou.

## [0.17.5] – 2026-11-15
Štít s napůl otevřenou dlaní.

### Změněno
- **Ruka pod štítem je napůl otevřená** (`HeroHands.ShieldGrip` = 0,45). Kruh štítu se vznáší kousek od dlaně, takže ruka ho nesvírá v pěsti, ale ani nezůstává úplně otevřená.

### Testy
- Pod štítem je dlaň napůl otevřená.

## [0.17.4] – 2026-11-15
Kosti prstů v obou rukou.

### Přidáno
- **Kostra má prsty.** Každá ruka má 14 kostí: `Finger.<strana>.<prst>.<článek>` (čtyři prsty po třech článcích, 0 = ukazováček vpředu) a `Thumb.<strana>.<článek>`. Prsty jsou na ně navázané, na kloubech napůl s předchozím článkem (`hero_build.py`).
- **`HeroHands`: ruce svírají, co drží.**
  - Pravá ruka se sevře kolem jílce, násady biče, ratiště kopí a rukojeti luku.
  - U štítu, který se vznáší kolem těla, zůstává dlaň otevřená.
  - Levá ruka sevře tětivu od chvíle, kdy na ni sáhne, do výstřelu.
  - Volná ruka je uvolněná.
  - Osa ohybu každého kloubu se počítá z klidové pózy (směr prstu a dlaně), takže nezávisí na tom, jak exportem prošly lokální osy kostí.

### Změněno
- Tvar `Fist.R` (shape key) nahradily kosti prstů.

### Testy
- Model má v obou rukou všech 28 kostí prstů a při sevření se prsty sbalí dovnitř dlaně (konec prstu blíž k zápěstí i k tělu).
- Která ruka co svírá: meč, štít, luk s natahováním, volná šála.

## [0.17.3] – 2026-11-15
Ruka svírá zbraň v pěsti a bič má skutečnou rukojeť.

### Opraveno
- **Pravá ruka se se zbraní sevře v pěst.** Model měl jen otevřenou dlaň s roztaženými prsty a nic nesvíral.
  - Pravá ruka má nový tvar `Fist.R` (shape key): prsty se ohnou kolem rukojeti a palec se přes ně zavře.
  - `HeroAnimator` pěst zavře, jakmile se v ruce zformuje zbraň, a otevře ji, když se zbraň rozplyne (`HeroPoseSolver.FistWeight`).
- **Rukojeť biče je pevný předmět, ne inkoust.**
  - Rukojeť dřív tvořil tlustý červený pruh inkoustu, který vypadal jako šleh a pěst ho držela za konec.
  - Teď je to tmavá rovná násada: hlavice za malíčkem, pěst kolem ní, a pak ostrá červená objímka (`VeilController.UpdateWhipHandle`). Inkoust šály se přes rukojeť nekreslí, šleh začíná až za objímkou.
  - Násada vede 0,17 m před pěst.
- Tětiva luku, lano biče a rukojeť přebírají vrstvu hrdiny, takže je vidí každá kamera, která vidí hrdinu (i nahrávač klipů). Na dřívějších klipech luku tětiva chyběla.

### Testy
- Rukojeť biče má šířku úchopu, šleh začíná u objímky v plné šířce a hlavice je za pěstí.
- Ruka se sevře, jak se zbraň formuje.

## [0.17.2] – 2026-11-15
Úklid vizuálních dluhů: kopí nahoru, šála u zdi, návrat ze štítu a rukojeť biče.

### Opraveno
- **Kopí mířené nahoru už nemá přes hlavu vodorovný pruh.** Úsek šály od krku k patě kopí se s mířením neotáčel a jeho poslední kus se natáhl do dlouhé, napůl průhledné čáry. Teď se otáčí spolu se zbraní, plynule od krku, a končí vždy u paty.
- **Šála u zdi nedělá kudrnatý chuchvalec.** Konec šály opřený o zeď nevytváří víry a kudrlinky, látka se přitiskne a splývá po zdi.
- **Návrat ze štítu:**
  - Kruh se nejdřív dotočí, až se otevře za zády, a šála z něj vyteče dozadu. Dřív visela svisle přes přední stranu těla.
  - Kruh se rozvine naráz jako pružina, ne od krku. Jeho konec tak nezůstane stočený do cívky a nevznikne chuchvalec.
  - Návrat ze štítu trvá 0,7 s.
  - Vedená látka se fyzice předává až na čáře vlající šály (u 88 % cesty místo 70 %).
- **Rukojeť biče je vidět.** Násada je delší (0,26 m) a silnější, s výraznou hlavicí a objímkou.

### Změněno
- Stránka s animacemi: přenahrané klipy šály, luku, štítu, salta a biče. Staré byly natočené nahrávačem, který kreslil tělo o snímek pozadu.

### Testy
- Kruh štítu se otevře za zády, krk zůstane na místě a kruh se jen otočí.
- Štít se rozvine naráz, luk od krku.

## [0.17.1] – 2026-11-15
Přirozené pózy pro míření nahoru a dolů.

### Opraveno
- **Mířené údery mají vlastní pózy.** Dřív se jen pootočilo rameno nebo samotná zbraň a zbytek těla zůstal stejný. Například luk mířený nahoru držel hrdina vodorovně u pasu. Teď:
  - **Meč nahoru:** sek zespodu. Čepel jde od boku nahoru před tělem a skončí svisle nad hlavou. Hrdina se vytáhne na špičky a zakloní.
  - **Meč dolů ve vzduchu:** meč zvednutý, kolena skrčená, pak úder s hrotem kolmo pod nohy. Zápěstí se při úderu stočí (`SwordDownWrist`).
  - **Kopí nahoru / dolů:** přitažení k hrudi a bodnutí nad hlavu. Ve vzduchu zvednutí a vražení pod nohy.
  - **Bič nahoru / dolů:** ruka dole za bokem a šleh vzhůru. Dolů nápřah přes rameno a šleh nízko před sebe.
  - **Luk:** obě paže se zvednou podél šípu a tělo se zakloní (dolů předkloní), pohled jde se směrem. Šíp vylétá ze skutečné ruky s lukem.
  - Šikmé směry plynule míchají pózu dopředu a pózu nahoru nebo dolů.
- Meč se drží s čepelí asi 64° nad osou předloktí. Proto sek nahoru končí s paží šikmo vpřed nahoře, ne svisle, kdy by čepel ležela přes hlavu dozadu.

### Testy
- Sek nahoru končí nad hlavou se záklonem. Sek dolů míří hrotem pod nohy se skrčenými koleny.
- Lukostřelec mířící nahoru zvedá obě paže, zaklání se a dívá se nahoru. Mířící dolů se předkloní.

## [0.17.0] – 2026-11-15
Míření v osmi směrech bez myši, s přichycením na cíl.

### Přidáno
- **Míření v 8 směrech** (`VeilAim`), stejně na klávesnici i na gamepadu.
  - Šipky / W-S spolu s A/D, na gamepadu levá páčka.
  - Směry: nahoru, šikmo nahoru, dopředu, šikmo dolů a dolů. Přímo dolů jde jen ve vzduchu.
  - Na zemi míří dolů jen luk a bič (šikmo dolů). Meč a kopí do podlahy nesekají.
- **Směrové údery všech zbraní.**
  - Meč a bič: celý švih se otočí s ramenem a zbraň jde s předloktím. Hrudník a pohled jdou se směrem.
  - Kopí a luk se natočí samy a paže jen dosáhne daným směrem.
  - Směr se přidává během nápřahu, takže póza neskočí.
- **Sek dolů ve vzduchu s odrazem.** Meč i kopí mířené pod nohy (`DownStrike`) odrazí hrdinu od zásahu nahoru.
- **Přichycení na cíl.** V kuželu ±22° kolem zvoleného směru a v dosahu zbraně se úder stočí na nejbližší cíl: nepřítele, tvora, rozbitné, lano. Přednost má cíl na linii, pak bližší.
  - Dosah: meč 2,6 m, kopí 3,2 m, bič 4,6 m, luk 14 m.
  - Luk míří z místa, odkud šíp vylétá, a v okamžiku výstřelu přichycení dorovná, protože hrdina se mezitím mohl posunout.
- **Rozhlížení kamerou je oddělené od míření.**
  - Pravá páčka nebo PgUp/PgDn kdykoliv.
  - W/S ve stoji jen s volnou šálou. Se zbraní W/S míří a kamera se nehne.
- `ClipRecorder`: příkaz `aim:1` / `aim:-1` / `aim:0`.

### Změněno
- Luk: samotné „nahoru“ teď střílí kolmo vzhůru. Šikmo nahoru je nahoru + dopředu. Dřív měl luk jen náklon ±55°.

### Testy
- `VeilAimTests`:
  - 8 směrů;
  - štít se nemíří;
  - přichycení v kuželu, ignoruje cíle za zády a mimo dosah, upřednostní cíl na linii;
  - mířený švih otočí paži, luk ne;
  - směr naroste během nápřahu.
- PlayMode:
  - meč mířený nahoru seká nad hlavou;
  - přichycení stočí bodnutí na nepřítele;
  - meč dolů ve vzduchu je sek pod nohy.
- Test šípu na vysoké lano míří šikmo nahoru a přichycením trefí lano.

## [0.16.21] – 2026-11-15
Šála neprochází zdmi.

### Opraveno
- **Šála koliduje se zdmi, sloupy a podlahou.**
  - Simulace šály dosud o scéně nevěděla, takže u zdi vlála skrz a její konec vykoukl z druhé strany sloupu.
  - `VeilController` teď každý snímek sesbírá pevnou geometrii kolem hrdiny v jeho rovině. Spouště (triggery), postavy, nepřátelé a volné předměty se ignorují.
  - Volná látka se ze zdi vrací tam, kudy do ní vnikla, tedy na stranu, odkud šála přišla. Nikdy ne na protější stěnu silného sloupu.
  - Šála u zdi ztratí rychlost do zdi a s mírným třením klouže podél ní.
- **Šála u zdi splývá.** Látka za místem dotyku přestane vlát do zdi a spadne podél ní, místo aby se o zeď tlačila.
- Zbraně v ruce se kolizí neřídí.

### Testy
- `VeilCollisionTests`:
  - šála vlající do sloupu se zastaví o jeho stěnu a nevyjde na druhé straně;
  - bez zdí vlaje volně;
  - vstupní bod leží na stěně, kterou úsek protnul;
  - hrdina ani spoušť nejsou zeď.

## [0.16.20] – 2026-11-15
Útoky nejdou spamovat ani přerušit.

### Změněno
- **Úder se vždy dokončí.** Nic ho nepřeruší, ani jiná zbraň, ani hák biče na kroužek, který dřív rozjetý úder rozbil.
- **Navázání jen ke konci úderu.** Další stisk se zapamatuje jen od 40 % úderu (`VeilCasting.ComboWindow`) nebo během zotavení. Dřívější mačkání se zahodí, takže už neřetězí údery.
- **Zotavení po úderu** (`VeilCasting.Recovery`). Meč 0,12 s, štít 0,16 s, bič 0,2 s, kopí a luk 0,22 s. Teprve potom může začít další úder. Navázaný úder během zotavení drží zbraň v ruce, takže se šála mezi údery neroztaví.

### Testy
- `MashingEarlyInABlow_DoesNotChainBlows`: mačkání na začátku úderu dá jen jeden úder.
- `ABlow_PlaysToItsEnd_AndTheBodyRecovers`: jiná zbraň uprostřed úderu ho nepřeruší a po úderu přijde zotavení.
- Testy okna pro navázání a doby zotavení. Kombo testy stisknou až ke konci úderu.

## [0.16.19] – 2026-11-15
Přechody šála ↔ štít ↔ luk bez utržení, schování a průchodu u nohou.

### Opraveno
- **Šála se při vzniku zbraně zvedá přes rameno, ne dolů kolem nohou.** Vír, kterým se látka stahuje do dlaně, se točil opačným směrem a táhl šálu nízko za nohama (`spiralDirection` = -1).
- **Šála se na začátku vzniku štítu neutrhne od krku.**
  - Úsek od krku k ruce mizel podle vlastní tvrdosti. Tvrdnutí začíná u krku, takže první snímek ukázal utržený kus šály.
  - Teď úsek zmizí, až je zbraň ztvrdlá u ruky a zformovaná (`NeckBandShaped`).
  - Kruh štítu se od ruky oddělí, až když je hozený kolem těla.
- **Odvíjející se zbraň je vidět.** Při návratu do šály látka přechází přes tělo. Dosud byla kreslená za trupem a na pár snímků se ztratila, teď je po dobu vedení vpředu.

### Testy
- Krční úsek zůstane, dokud zbraň nemá tvar.
- Vedená látka je před tělem a na své dráze.

## [0.16.18] – 2026-11-15
Plynulé přechody mezi šálou, štítem a lukem.

### Opraveno
- **Návrat zbraně do šály už se nezmuchlá do chuchvalce.**
  - Dřív šála po puštění štítu či luku na několik snímků zkolabovala do pomačkaného klubka za tělem a teprve pak naskočila do vlající podoby.
  - Nově se zbraň odvine jako řetěz (`VeilTransform.MeltChain`): každý úsek se otočí ze směru ve zbrani do směru ve vlající šále, od krku ke špičce, a délku mění plynule. Nic se nestlačí.
  - Látka je po dobu 0,5 s vedena po této dráze (`VeilSimulation.GuideTargets/GuideWeight`) a na konci plynule předána fyzice šály.
- **Štít se rozvine z ruky.** Dřív stál celý kruh hotový za tři snímky. Teď se z dlaně rozvine kolem těla jako hozená smyčka (`VeilTransform.SweepRing`).
- **Velké tvary mají na vznik o pár snímků víc.** Štít 0,3 s, luk 0,24 s (`FormMorphTime`). Tvrdnutí a s ním herní načasování (parry, výstřel) zůstává beze změny.

### Testy
- `VeilMeltTests`:
  - odvíjení začíná ve tvaru zbraně a končí na šále;
  - nikdy nestlačí látku;
  - nikde neskočí;
  - krk se pouští první;
  - štít se rozvíjí z dlaně;
  - časy vzniku.

## [0.16.17] – 2026-11-15
Meč sedí v pěsti po celé seknutí a bič má rukojeť.

### Opraveno
- **Nahrávač klipů ukazoval tělo o snímek pozadu.**
  - `ClipRecorder` snímal kameru v `LateUpdate`, tedy dřív, než Unity přepočítá kostru těla.
  - Tělo se proto kreslilo se stavem z minulého snímku, zatímco zbraň už byla aktuální, a při švihu meč na záznamu „ujížděl“ z ruky až o ~25 cm.
  - Teď se snímá až na konci snímku (`WaitForEndOfFrame`), takže záznam odpovídá hře.
- **Nová choreografie seknutí mečem.**
  - Dříve paže skočila zepředu dozadu o 115° během jediného snímku a při nápřahu byla pěst schovaná za hlavou.
  - Teď je meč zvednutý vysoko vedle hlavy, následuje jeden plynulý šikmý sek a dokončení zůstává dole před tělem. Volná ruka je přitažená k hrudi.
- **Jílec je v pěsti už od vzniku zbraně.** Úsek rukojeti (hlavice–záštita) drží pevně v dlani hned od začátku přeměny a kreslí se za pěstí (`HeldSpan`, `HeldTuck`).

### Přidáno
- **Bič má rukojeť.** Hlavice, tužší násada a objímka, za kterou se šleh zužuje. Smyčky stočeného biče visí z konce násady.

### Testy
- `SwordCut_HandStaysInFrontAndSweepsWithoutASnap`: pěst je po celé seknutí před ramenem a paže se nikde neotočí rychleji než 1300 °/s.
- Testy rukojeti biče a drženého úseku zbraně.

## [0.16.16] – 2026-11-15
Zbraň drží v ruce i během animace útoku.

### Opraveno
- **Ztvrdlá zbraň se pohybuje přesně s rukou.**
  - Simulace dosud táhla zbraň za jejím tvarem s malým zpožděním, takže při rychlém švihu meč zaostával za pěstí o 7–19 cm.
  - Nově sedí ztvrdlé uzly po výpočtu fyziky přesně na tvaru zbraně (`RigidHold`): meč, kopí, luk a štít jdou s pěstí jako pevný předmět.
  - Měkká zůstává jen látka v přechodu a bič, který má vlnu po celé délce.
  - Změřeno po snímcích: meč 0 mm od dlaně během celého seknutí (dříve až 19 cm), až na jeden snímek s rázem zásahu.
- **PlayMode test „zbraň zůstává v ruce“ hlídá skutečnou polohu rukojeti proti středu dlaně s tolerancí 4 cm.** Dřív hlídal jen cílovou polohu s tolerancí 12 cm, a proto chybu nezachytil.

### Přidáno
- `ClipRecorder.Probe`: měření zapsané u každého nahraného snímku do `probe.txt`.

## [0.16.15] – 2026-11-15
Meč s jílcem a zbraně v dlani.

### Přidáno
- **Meč má jílec:**
  - hlavice 13 cm za pěstí;
  - úzká rukojeť procházející dlaní (0,32 šířky čepele);
  - příčná záštita 6 cm před pěstí (2,6× šíře čepele);
  - teprve pak čepel.
  - Plamenné jazyky meče začínají až od záštity, jílec zůstává čistý.
  - Dosah meče od pěsti ke špičce je pořád 1,5 m.

### Opraveno
- **Zbraně se drží ve středu dlaně, ne v zápěstí.**
  - Kost ruky začíná v zápěstí, takže úchop byl 9–10 cm za pěstí.
  - Nově je 7,5 cm po kosti ruky (`PalmPoint`), i pro tětivu luku a lano biče.
  - Změřeno ve hře, vzdálenost úchopu od středu dlaně: meč 1,6 cm, luk 1,6 cm (tažná ruka 2,4 cm), kopí 3,9 cm (násada prochází pěstí), bič asi 4,6 cm.
  - Štít obepíná tělo volně, tak má být.

## [0.16.14] – 2026-11-15
Šála při saltu: obtočí se kolem těla a po dopadu se hned rozbalí.

### Změněno
- **Během salta se šála ovine kolem těla jako spirála.**
  - Začíná u krku, vede dokola kolem boků a rozšiřuje se z poloměru krku o 0,25 m.
  - Opožďuje se za otáčkou, takže 3,2 m šály obepne tělo asi ze tří čtvrtin.
  - Látka se na spirálu klade rychle (`WrapRate` 38/s) a otáčí se s tělem. Už se nezamotává do klubka ve víru vedle hrdiny.
- **Po dopadu se šála hned rozbalí.**
  - Víření ji pustí za 0,12 s (dřív za 0,45 s) a po dopadu už nepokračuje.
  - Dalších 0,5 s ji tvar ze sešitu stahuje zpět až čtyřnásobnou silou a látka se smí volně hýbat nahoru i dolů.
  - Za zhruba čtvrt sekundy zase vlaje za hrdinou.

## [0.16.13] – 2026-11-15
Štít, salto a zbraně podle záběrů ze hry.

### Změněno
- **Zbraně jsou ze stejné rudé tuše jako šála** (meč, kopí, bič, luk i štít). Jsou z plné tuše a v jádru mají trochu žáru (`_WeaponInk` 0,85), už nemají starý plamenný vzhled.
- **Štít je kulatý kruh z tuše s oblými jazyky plamene.**
  - Jazyky jsou široké tahy štětce, ne jehlové špičky: je jich 6 místo 9 a jsou oblé, ne ostré.
  - Odlétají jen 0,3 m místo 0,6 m a v plameni se nezužují na linku.
  - Kruh se vlní pomaleji, takže už není hranatý.
- **Po saltu se šála nezmuchlá před hrdinou.**
  - Víření po dopadu dál běží a zpomaluje, takže šálu odnese dokola dolů a dozadu.
  - Šálu pustí za 0,3 s místo 0,45 s.
  - Po saltu se 0,2–0,9 s nesklápí látka před hrudí, šála se vrátí sama.
- Rudých čárek kolem zbraní je méně a mizí dřív (2 místo 5 za sekundu, při švihu 2,5 místo 16, životnost 1,1 s místo 1,9 s).

## [0.16.12] – 2026-11-14
Nahrávač krátkých klipů ze hry.

### Přidáno
- **`ClipRecorder`** natáčí krátké klipy ze hry na ukázku (šála, zbraně, štít, salto).
  - Ovládá hrdinu podle časového scénáře vstupů, například `move:1`, `jump`, `cast:Sword`, `hold:Shield`, `release`.
  - Běží s pevnou snímkovou frekvencí (`Time.captureFramerate`), takže klip je plynulý i v pomalém editoru.
  - Kamera sleduje hrdinu a každý snímek se uloží jako PNG.
  - Stejně jako `AutopilotInput` je to zdroj vstupu pro `PlayerController.InputSource`.

## [0.16.11] – 2026-11-14
Objemné víry na konci šály a čisté tahy bez úlomků.

### Změněno
- **Víry na konci jsou ze širokých tahů s objemem, jako v sešitu.**
  - Pás se ke konci rozšiřuje na 0,48 m.
  - Jazyky jsou širší a plnější a víc kryjí.
  - Tah se ve víru zužuje až u středu.
- **Horní jazyky jsou nižší a hustší.** Nad pás stoupají jen málo, celek zůstává ve vodorovném pásu.
- **Všechny vrstvy volné šály doběhnou až do vírů na konci.** Vrstva, která končila uprostřed pásu, se tam stáčela do zbloudilého úlomku.
- **Jazyk vyrůstá z pásu od nulové šířky**, už nezačíná rovně useknutým obdélníčkem.
- Volná šála už nepouští dýmové nitky (v sešitu nejsou, jejich lomené čáry rušily). Zbraně je mají dál.

### Opraveno
- **Široký tah se v ostrém ohybu nepřeloží přes sebe.** V každém bodě se jeho šířka omezí na 0,9 poloměru ohybu, takže zmizely trojúhelníkové střípky u vírů. Na zbraně se to neuplatňuje.

## [0.16.10] – 2026-11-14
Barvy a víry šály podle animačního sešitu.

### Změněno
- **Barvy změřené v sešitu, sRGB:**
  - čistá karmínová (0,80/0,02/0,03), světla čistě rudá (1,0/0,04/0,04), tmavé tóny (0,36/0/0,01);
  - žádný oranžový ani šedý nádech;
  - světlejší je šála jen tam, kde je průsvitná.
- **Průsvitnost jako v sešitu:**
  - u krku neprůhledná, uprostřed lehce průsvitná, ve vírech růžovočervená lazura;
  - tenká lazura má jasně červenou barvu, nikdy ne tmavou prosvítající do šeda;
  - tmavé linky ve vírech už nejsou neprůhledné.
- **Víry na konci jsou ze širších tahů:**
  - jazyky jsou u kořene širší a do víru se zužují jen málo;
  - tahy se ztenčují až ve středu víru a stáčejí se víc dokola;
  - spirály mají 34 bodů místo 22, takže jsou hladké a bez hranatých úlomků.
- **Dýmové nitky jsou jemné vlasové linky** (1,5–4,5 cm) a je jich méně, jako tenké tahy kolem šály v sešitu.

## [0.16.9] – 2026-11-14
Šála podle animačního sešitu (12 snímků smyčky).

### Přidáno
- **Tvar šály ze sešitu `ArtSource/ref/scarf_anim_sheet.png`.**
  - `ArtSource/scarf_sheet_fit.py` v každém z 12 snímků najde středovou linii šály a vygeneruje tabulku `VeilRestShape`: výšku linie každých 0,2 m od krku. Hrdina má na snímcích 170 px, tedy 1,9 m.
  - Šála drží vodorovnou vlnu S: v 1,2 m klesne asi 0,4 m pod krk, ve 2 m se vrátí skoro do výšky krku a za 2,6 m znovu klesne. Smyčka trvá 2,4 s.
  - Simulace ji k tomuto tvaru přitahuje. V klidu pevně, v běhu jen napůl. Při saltu, otáčení, vycítění tajemství a nebezpečí se tvar uvolní a projeví se fyzika.
- Do `ArtSource/ref` přibyl key art (`key_art.png`) a animační sešit šály.

### Změněno
- **Délka šály je 3,2 m od krku**, plus víry na konci. Dřív byla 2,7 m.
- **Šířky podle sešitu:**
  - u krku úzká, 0,16 m;
  - o metr dál 0,32 m;
  - uprostřed 0,38 m;
  - k vírům 0,3 m.
- **Pás je složený z jemných podélných pramínků:** tmavší a světlejší proužky a mezi nimi lehce průsvitná místa. Přetočení pás zúží už jen mírně a rub je méně tmavý.
- **Konec se stáčí do dvou vírů jako v sešitu:** tělo šály a spodní jazyky dolů a pod sebe, horní jazyky nahoru a dozadu. Víry jsou větší a volnější.
- Jazyky se drží blíž u pásu, nahoru stoupají jen mírně.
- Spirály se stáčejí dolů a pod pás a každý jazyk se zužuje až do vlasové špičky.
- Okraje vypadají jako tah štětce na sucho.
- Tmavé vrstvy jsou neprůhledné a vínové, nikde nevzniká šedý závoj.
- Dýmové nitky jsou rudé tahy, ne šedý dým.
- Posun jazyků je zrcadlově správný i při pohledu doleva.

## [0.16.8] – 2026-11-14
Šála podle key artu.

### Změněno
- **Volná šála je stuha namalovaná rudou tuší jako na key artu, ne šedý dým.**
  - Od omotávky u krku vede široký pás: 0,3 m u krku, 0,45 m o metr dál, 0,55 m tam, kde se rozpadá. Šířky jsou změřené na key artu, kde postava měří 213 px.
  - Konec se rozpadá do jazyků, které se stáčejí do velkých otevřených spirál (dvakrát větších než dřív) a víc se rozbíhají.
- **Stuha se přetáčí a ukazuje rub.**
  - Od krku je plná a zářivá, zhruba v polovině se přetočí a v místě přetočení se zúží.
  - Zadní část ukazuje tmavě vínový rub, u spirál se vrací zářivá strana.
  - Přetočení se v letu mírně posouvá.
- **Barvy:**
  - sytá karmínová (sRGB 0,85/0,06/0,06), světlejší lesk (1,0/0,30/0,25) a vínový rub (0,42/0,03/0,04);
  - okraje jako tah štětce na sucho;
  - pás je skoro neprůhledný a ke spirálám přechází v průsvitnou lazuru;
  - průsvitná místa jsou světlejší červená, ne šedá;
  - spodní vrstva a dýmové nitky jsou vínově červené místo šedé.
- Zbraně si nechávají plamenný vzhled.
- Nový přepínač materiálu `_Cloth` zapíná vzhled látky jen pro šálu, ne pro dýmové nitky.

## [0.16.7] – 2026-11-14
Delší prameny vlasů.

### Změněno
- **Prameny vlasů jsou dlouhé a tenké.** Nad hmotu vlasů sahají 1,7× dál a jsou užší, takže vlají dozadu jako na konceptu.

### Opraveno
- **Obrys kštice se z konceptu snímá i se světlejšími konci pramenů.**
  - Konce jsou na kresbě světlejší šedé (práh jasu je nově 0,42, dříve 0,12), takže je snímání dřív usekávalo.
  - Výřez je širší.
  - Kštice teď sahá o 5 cm dál dozadu a o 2,5 cm výš.
  - V obrysu se našlo 14 špiček místo 9, každá má svůj pružinový řetěz.

## [0.16.6] – 2026-11-14
Vlasy jako na konceptu: hladká hmota a pár pramenů do špiček.

### Změněno
- **Vlasy nejsou ježek z desítek bodlin, ale jedna hladká hmota vlasů, ze které trčí jen pár pramenů do špiček.**
  - Hmota z boku kopíruje obrys kštice z konceptu bez špiček. Obrys je vyhlazený a měří se po 3° kolem hlavy.
  - Pramenů je devět, každý vede do jedné špičky obrysu: ofina dopředu nad čelo, ostatní sčesané dozadu a nahoru.
  - Prameny jsou u kořene široké a lehce prohnuté.
  - Prameny sčesané dozadu mají vedle sebe po stranách ještě jeden, takže jsou vidět i zepředu.
- Model má zase 32 tisíc vrcholů (vlasy z pramínků ho nafoukly na 54 tisíc).

## [0.16.5] – 2026-11-14
Hlava s obličejem a vlasy podle konceptu.

### Změněno
- **Hlava je vymodelovaná lebka s obličejem, už ne nafouknutý obrys.**
  - Obličej má vysoké čelo, nadočnicové oblouky a hluboko posazené oči.
  - Dál rovný nos, lícní kosti, rty, špičatou bradu a výrazný úhel čelisti.
  - Uši jsou pod vlasy.
  - Proporce jsou změřené na obrysu tváře v konceptu: čelo, brada a spodní hrana čelisti.
- **Vlasy jsou rozcuchaná kštice z tenkých pramínků přiléhající k lebce**, jako na konceptu.
  - Pramínky vedou z čepice na lebce do bodů obrysu kštice v konceptu (po 3°), takže z boku je silueta kresby.
  - Do stran jsou rozházené a ke spánkům kratší, takže zepředu je to kulatá kštice, ne placka.
  - Nad ušima jsou krátké pramínky sčesané dozadu.
  - Pramínky u pružinových řetězů se dál vlní.
- Model má 54 tisíc vrcholů.

## [0.16.4] – 2026-11-14
Trup, tunika a nohy vymodelované podle podkladů (walk sheet a koncept).

### Změněno
- **Pancíř na hrudi má tvar svalů.**
  - Vpředu prsní svaly s ostrým spodním okrajem plátu, žlábek hrudní kosti, tři řady břišních plátů a šikmé svaly.
  - Vzadu lopatky, široké zádové svaly a žlábek páteře.
- **Nákrčník:** karmínová šála je omotaná v silných rolích kolem krku, vzadu nahrnutá a vpředu splývá na horní část hrudi.
- **Opasek je v pase (1,04–1,10 m) jako na podkladech, ne na bocích.**
  - Široký pás má vystouplé okraje a čtvercovou přezku s karmínovou vložkou.
  - Na levém boku je karmínový uzel šerpy, jejíž dva konce visí přes tuniku.
- **Tunika:** od opasku přiléhá k bokům, dolů se rozšiřuje a končí nad koleny.
  - Od boků dolů je po stranách rozparek.
  - Přední díl je o kousek delší.
  - Má tloušťku a v kroku částečně sleduje stehno na své straně.
- **Nohy:**
  - stehna s hýžďovým a čtyřhlavým svalem;
  - klenutý chránič kolena s lemem;
  - vysoká bota s ohrnutou manžetou pod kolenem, výrazným lýtkem a páskem u kotníku;
  - podrážka.
- Karmínový proužek přes hruď nahradila karmínová páska na nadloktí, kde je na konceptu.
- Model je hladce stínovaný, ostré hrany zůstávají jen na skutečných zlomech: okrajích plátů, lemu a rozparku.
- Model má 33 tisíc vrcholů.

### Opraveno
- **Špička boty už netrčí jako tyčka.** Profil nohy končí u špičky, za ní byl jen řádek země.

## [0.16.3] – 2026-11-14
Ruce s prsty, tvarované paže a chránič předloktí.

### Změněno
- **Ruce mají dlaň, prsty a palec.**
  - Dlaň je široká, každý ze čtyř prstů má tři články a palec dva.
  - Prsty jsou mírně roztažené a pokrčené k dlani jako uvolněná ruka v sešitech, takže jsou vidět i v boční siluetě.
- **Paže už nejsou válec:** mají ramenní sval, vpředu biceps, vzadu triceps, výrazný loket a zesílené předloktí u lokte.
- **Chránič předloktí je samostatný pancíř** s rozšířenou manžetou u lokte, dvěma žebry a lemem u zápěstí.
- Model má 21 tisíc vrcholů.

## [0.16.2] – 2026-11-14
Klidová smyčka z animačního sešitu.

### Změněno
- **Klidová smyčka je podle řádku „Idle“ z animačního sešitu** `ArtSource/ref/anim_sheet.png` (8 snímků, překryv 69–82 %).
  - Přední noha je 18° vpřed s kolenem povoleným o 7,5°, zadní noha 22° vzad.
  - Hrudník je mírně vypnutý, hlava kývne 3–11° dopředu.
  - Zadní ruka visí za tělem pokrčená v lokti (asi 50°) a pohupuje se, bližší ruka visí asi 20° vpředu.
  - Smyčka trvá 3,2 s.

### Opraveno
- **Znaménko páteře a hlavy při převodu z dohledaných póz.** U kostí mířících nahoru znamená kladný úhel předklon, minule byl převeden obráceně. Do páteře a stehen se teď započítává i natočení pánve, které hra nezná.

### Přidáno
- `ArtSource/hero_fit_anim.py`: rozřeže animační sešit na snímky (bez čar země) a pro každý dohledá úhly kloubů. Počítá s měřítkem řádku a zkouší prohozené nohy a ruce.
- `ArtSource/hero_fit_cycle.py`: pokus o běh jako jeden parametrický cyklus.
  - Pro běh, skok a útok zatím spolehlivé úhly nedává: siluety jsou malé (asi 130 px), nerozliší levou a pravou nohu a měřítko řádku se plete s přikrčením.
  - Běh, skok a útok proto zůstávají beze změny.

## [0.16.1] – 2026-11-14
Ruce a nohy podle sešitu chůze.

### Změněno
- **Ruce jsou podle sešitu chůze**, kde zadní ruka visí ve všech snímcích odděleně za tělem.
  - Nadloktí má u lokte 12 cm a u ramene i s chráničem 13 cm.
  - Chránič předloktí má 8–9,5 cm a k zápěstí se zužuje na 6,6 cm.
  - Dlaň je otevřená a široká asi 8 cm.
  - Horní polovina nadloktí v boční siluetě splývá s ramenem, tam se paže plynule dotahuje.
- **Nohy a tunika** jsou změřené ze sešitu chůze: z 12 snímků a obou nohou.
- **Trup** zůstává podle hlavního návrhu. V sešitu ho skoro ve všech snímcích zakrývají ruce: bližší vpředu, zadní za rameny.
- Pózy ze sešitu jsou dohledané znovu, se zadní rukou za tělem (překryv 74–81 %). Model se sešitem v jeho pózách kryje ze 71–78 %.

## [0.16.0] – 2026-11-14
Klidový postoj podle sešitu animace.

### Přidáno
- **Hrdina ve stoje stojí v postoji z návrhu a pohupuje se v něm.**
  - Postoj je převzatý ze sešitu 12 bočních siluet `ArtSource/ref/walk_cycle.png`: přední noha 21° vpřed s kolenem povoleným o 10°, zadní noha 23° vzad natažená, chodidla naplocho.
  - Ruce se pomalu kývají (bližší 19–27° vpřed, vzdálenější 7–23° vzad), hlava kývne o ±5° a tělo se lehce pohupuje. Smyčka trvá 3,6 s a snímky se plynule prolínají.
  - Při rozběhu se postoj plynule přelije do chůze. Hrudník dál dýchá a tasená zbraň drží ruku v úchopu.
  - Sešit ukazuje nohy ve všech 12 snímcích ve stejném rozkroku, takže chůzi samotnou nemění.
- `ArtSource/hero_fit_walk.py`: pro každý snímek sešitu dohledá úhly kloubů kostry tak, aby se silueta modelu co nejvíc kryla s kresbou (překryv 66–74 %). Hlídá meze kolen a loktů a každý snímek začne od předchozího.
- `ArtSource/walk_to_idle_table.py`: z dohledaných úhlů vygeneruje tabulku `HeroIdleCycle.cs` (ruce a hlava vyhlazené, chodidla naplocho).

### Testy
- `Idle_StandsInTheConceptStance_SwaysAndBreathes`, `IdleCycle_LoopsSmoothly_WithTheFeetFlat`.
- Upravený test tasené zbraně ve stoje (ruka drží úchop, i když ve stoje už visí mírně vpředu).

## [0.15.5] – 2026-11-14
Normálně vypadající paže.

### Opraveno
- **Paže jsou složené z obou rukou návrhu do jedné souvislé paže.** Měřit je přímo z obrysu nešlo: bližší nadloktí se v návrhu ztrácí v trupu a vzdálenější předloktí drží meč a mizí za jílcem.
  - **Vrchní část** (rameno, nadloktí s chráničem, loket) je podle zadní ruky, hluboká 10–11 cm.
  - **Spodní část** (chránič předloktí, zápěstí, pěst) je podle přední ruky: chránič se zužuje k zápěstí na 5 cm, pěst má 7,5 cm.
  - Paže se od ramene k zápěstí plynule zužuje a leží rovně na kostech ruky.

## [0.15.4] – 2026-11-14
Tělo hrdiny drží pohromadě.

### Opraveno
- **Ruce už nevisí ve vzduchu a díly těla na sebe navazují.** Dřív se každý díl z kresby přenášel přes dohledanou pózu a každá její nepřesnost ho posunula nebo pootočila: ruce stály bokem, pěsti byly odpojené, chodidla mířila špičkou dolů a povrch byl samý schod.
  - Z návrhu se teď bere jen to podstatné: v řezech každých 1,2 cm podél každé kosti, kam sahá obrys před kost a za ni.
  - Každý díl je hladká trubice navlečená na kost se zachyceným profilem, takže se díly v kloubech potkávají a z boku mají obrys z kresby.
  - Kde v návrhu leží přes trup ruka, je obrys trupu omezený na hodnotu nezakrytých míst: bližší loket přes břicho, vzdálenější ruka s jílcem přes boky, uzel šály pod bradou.
  - Chodidlo se měří vodorovně podél nakreslené boty (stojí naplocho), takže boty stojí rovně.
  - Kalhoty jsou u opasku úzké a dolů se rozšiřují (i do stran), bez hranatých výstupků nad boky.
- Model má 19 tisíc vrcholů místo 42 tisíc. V póze návrhu se silueta kryje s kresbou ze 77 %.

## [0.15.3] – 2026-11-14
Celé tělo hrdiny z obrysu návrhu.

### Změněno
- **Celé tělo je postavené přímo z boční siluety návrhu, stejně jako předtím hlava.** Týká se to trupu s hrudníkem, kalhot, chráničů kolen, lýtek, bot, chodidel, paží, chráničů předloktí a pěstí.
  - Kostra se postaví do pózy návrhu.
  - Kolem každé kosti se z obrysu vyřízne její díl. V každém řezu napříč kostí zůstane jen souvislý úsek obrysu, kterým kost prochází, takže se do stehna nepřiplete druhá noha a do trupu předloktí.
  - Díl dostane v řezu elipsu: hloubku podle šířky obrysu v návrhu, šířku do stran v rozumném poměru.
  - Kost ho pak přenese z pózy návrhu do základní polohy.
  - V póze návrhu se tak silueta modelu kryje s kresbou: boty, kalhoty i hrudník jsou kreslené tvary.
- **Doplnění z boku neviditelných částí:**
  - Pro obě strany se použije přední noha a bližší ruka z návrhu (zadní ruka drží meč).
  - Záda v ramenou zakrývá v návrhu šála, takže tam záda pokračují linií viditelných zad.
- Model je souvislý i zepředu: obrys je bez děr na schůdcích pixelů, drobné světlé tečky v kresbě (odlesk sponky) jsou zacelené a hrany jsou zjemněné.

## [0.15.2] – 2026-11-14
Hlava a vlasy přímo z obrysu návrhu, porovnání v póze návrhu.

### Změněno
- **Hlava a vlasy jsou postavené přímo z boční siluety návrhu.**
  - Obrys hlavy s rozcuchanou kštící, ofinou trčící dopředu, prameny vzadu, profilem obličeje a bradou je obtažený po 3,3 mm.
  - Obrys je „nafouknutý“ do zakulaceného tělesa (do stran až 8,5 cm), takže z boku, jak hrdinu vidí hra, odpovídá kresbě.
  - Konce pramenů, které z obrysu trčí, mají vlastní pružné řetězy kostí (9), takže se dál houpou.

### Přidáno
- `ArtSource/ref/hero_concept.jpg`: návrh postavy, ze kterého se model staví.
- `ArtSource/hero_fit_pose.py`: postaví kostru do pózy návrhu.
  - Úhly páteře, krku, kyčlí, kolen, chodidel, ramen a loktů dohledá tak, aby se boční silueta modelu co nejvíc kryla s návrhem (meč a šálu z návrhu nepočítá).
  - Výsledná póza se ukládá do `ArtSource/ref/concept_pose.json`. Silueta se teď s návrhem kryje ze 68 %.

## [0.15.1] – 2026-11-14
Hrdina přesně podle změřeného návrhu.

### Změněno
- **Proporce hrdiny jsou odměřené z koncept artu.**
  - Silueta z návrhu je vyčištěná po pixelech a změřená v měřítku asi 1,66 mm/px (temeno 1,84 m odpovídá výšce kostry). Návrh ukazuje postavu z boku, stejně jako ji vidí hra.
  - Model jsem upravoval tak dlouho, dokud se jeho boční silueta s návrhem nepřekrývala.
- **Podle návrhu:**
  - **Vlasy:** kompaktní kštice, za lebku sahá asi 6 cm a nad ni 3 cm. Ofina trčí nad čelem dopředu a nahoru.
  - **Hlava** je předsunutá na delším krku dopředu, obličej je před linií hrudníku.
  - **Trup:** hrudník vpředu nevystupuje nad břicho, záda jsou hladká (pláty jen vpředu a po stranách).
  - **Paže:** paže 10 cm, předloktí s chráničem 5,5–6,5 cm, pěst 7,6 cm.
  - **Nohy:** stehno 15 cm, lýtko 13,8 cm, štíhlá vysoká bota 7,5–9 cm, chodidlo 26 cm dlouhé.
  - **Volné kalhoty** končí ve výšce 0,66 m, **opasek** sedí níž (0,89–0,94 m) a nevyčnívá.
  - **Nárameníky** jsou menší.


## [0.15.0] – 2026-11-14
Nový model hrdiny podle koncept artu.

### Změněno
- **Hrdina je přemodelovaný podle obrázku:** štíhlý mladý válečník v lehké vrstvené zbroji.
  - Atletické tělo: hluboký hrudník, úzký pas, silnější paže a nohy.
  - Pancíř s vrstvenými pláty přes břicho a vystouplým okrajem na hrudi.
  - Nárameníky ze tří překrývajících se plátů a chrániče předloktí s rozšířenou manžetou.
  - Opasek s přezkou a karmínovou šerpou, jejíž cíp visí u boku.
  - Krátké kalhoty nad koleno, chrániče kolen, vysoké boty.
  - **Vlasy** jsou rozcuchaná kštice odvátá dozadu: 25 pramenů, každý se houpe na vlastním řetězu kostí.
  - **Karmínová šála** je dvakrát omotaná kolem krku (šála-zbraň z ní vychází) a **karmínový lem** vede přes hruď.
- Model má dva materiály: inkoustové tělo a karmínové akcenty. Stínový dvojník zůstává celý v inkoustu.
- Kostra zůstala stejná (jen vlasové řetězy jsou nové), takže animace, zbraně v ruce i salto fungují beze změny.

### Přidáno
- `ArtSource/hero_build.py`: skript, který model hrdiny postaví v Blenderu na kostru a vyexportuje do hry. Úpravy postavy jdou teď zopakovat.
- `ArtSource/hero_preview.py`: náhledy postavy z boku, zepředu a ze tří čtvrtin.
- Testy `HeroModelTests`: kosti pro animace, vlasové řetězy, karmínové akcenty a výška.

## [0.14.3] – 2026-11-14
Štít hoří.

### Změněno
- **Prstenec štítu vypadá jako plameny, ne jako hladká obruč.**
  - Z prstence šlehají ven ostré, nepravidelné jazyky ohně, které po něm běží a mihotají se. Každá vrstva šály má svoje.
  - Tloušťka prstence se mihotá.
  - Všechny vrstvy šály teď hoří celým kruhem, ne jen jeho začátkem.

### Testy
- Nový test `ShieldRing_BurnsWithTonguesOfFlame`.

## [0.14.2] – 2026-11-14
Robustnější štít, dál od postavy.

### Změněno
- **Prstenec štítu:**
  - **Menší mezera:** prstenec obepíná 97 % kruhu.
  - **Robustnější:** je širší (0,22 m místo 0,15 m) a tužší a skoro po celé délce drží plnou šířku. Zaobluje se jen úplně na konci.
  - **Dál od postavy:** poloměr 1,22 m místo 1 m, takže už neprochází hlavou a vede s rezervou nad ní i mimo nataženou ruku.

### Testy
- Nový test `ShieldRing_ClearsTheHeadAndTheHand`.

## [0.14.1] – 2026-11-14
Plynulejší salto, štít jako kroužící prstenec, rychlejší návrat do šály.

### Změněno
- **Salto při dvojskoku plyne.**
  - Tělo se otáčí bez zastavení: na krajích pomaleji, uprostřed rychleji, aniž by se na začátku zaseklo a pak trhlo.
  - Šála krouží stejnou rychlostí jako tělo. Obtáčí se plynule a po saltu se pomalu pouští.
- **Štít je čistý tenký prstenec ze šály s malou mezerou**, který krouží kolem celé postavy (asi otáčka za sekundu).
  - Místo přeplácané bubliny je to jeden pruh.
  - Prstenec není přivázaný k ruce, takže volně obíhá kolem těla.
  - Úder štítem prstenec nafoukne a roztočí rychleji.
- **Rychlejší návrat do šály po útoku.** Zbraň po úderu vydrží 0,25 s místo 0,6 s a rozplyne se zpátky v látku zhruba dvakrát rychleji.

## [0.14.0] – 2026-11-14
Dvojskok se saltem, štít jako bublina, rovný meč.

### Přidáno
- **Dvojskok se saltem.** Druhé zmáčknutí skoku ve vzduchu znovu vyhodí hrdinu nahoru (asi o 2,4 m).
  - Hrdina přitáhne kolena k hrudi a udělá jedno salto vpřed kolem boků, s dopadem zase na nohy.
  - Šálu strhne vír kolem těla, takže se kolem hrdiny obtočí a po saltu se zase rozvine.
  - Dvojskok je jeden na každý skok. Obnoví se dopadem na zem, chycením římsy, klouzáním po zdi, odrazem kopím nebo houpáním na biči.
- **Plachtění na šále teď přijde až po dvojskoku:** první zmáčknutí ve vzduchu je dvojskok, další zmáčknutí a podržení je plachtění.

### Změněno
- **Štít je bublina ze šály kolem celé postavy** (průměr asi 2 m, od hlavy k patám). Po bublině dokola běží vířící pruhy.
  - Blokuje údery z obou stran, ne jen zepředu.
  - Úder štítem bublinu prudce nafoukne a roztočí.
- **Meč je rovný.** Čepel z pěsti vede v přímé linii místo dřívějšího zahnutí a je tužší, takže se v pohybu neprohýbá.

### Testy
- Nové EditMode testy pro dvojskok, salto, obtočení šály, tvar bubliny a rovnost čepele.
- Upravené testy štítu (blok z obou stran) a plachtění (až po dvojskoku).
- Nový PlayMode test `DoubleJump_RisesAgainWithASomersault`.

## [0.13.4] – 2026-11-14
Zbraň drží v ruce a kamera po respawnu hned sedí.

### Opraveno
- **Zbraň už se při úderu neodtrhává od ruky.** Šála četla polohu ruky dřív, než ji animace v daném snímku napózovala, a navíc ji vyhlazovala. Při rychlém švihu proto čepel nebo bič zaostávaly až o půl metru a vycházely od hlavy. Teď se šála počítá až po animaci postavy (`LateUpdate`) a zbraň je v ruce pevně: za celý úder se úchop odchýlí od ruky o méně než 5 cm.
- **Kamera po teleportu nebo respawnu do kamerové zóny hned převezme její záběr** a nedorovnává ho dodatečně. Totéž platí, když hra začíná uvnitř zóny.

### Testy
- `AWeaponStaysInTheHand_ThroughAFastSwing`: rychle švihaná ruka, úchop meče musí zůstat u ní.
- `ArrivingInACameraZone_TakesItsFramingAtOnce`.

## [0.13.3] – 2026-11-14
Klidná kamera, která drží postavu v záběru.

### Opraveno
- **Kamera se při otočení nepřehazuje.** Dřív předsazovala záběr podle toho, kam se hrdina dívá, takže každé otočení k úderu přehodilo pohled o ~7 m. Teď předsazuje podle toho, kam hrdina **jde** (vyhlazená rychlost). Krátké ťuknutí ani otočení na místě záběr nerozhoupe; když hrdina stojí, kamera se vrací na střed.
- **Skok kamerou nehoupe.** Kamera drží výšku podlahy, na které hrdina stojí. Při skoku na místě se pohne jen o ~0,3 m, i když hrdina vyskočí přes 3 m. Na novou výšku se přerámuje, až když hrdina dopadne výš nebo níž, nebo vyleze či spadne mimo okno kolem podlahy.
- **Skutečný pád je vidět dopředu.** Při dlouhém pádu kamera jde s hrdinou a mírně (nejvýš 3 m) předbíhá dolů, aby bylo vidět, kam dopadne. Dřív předbíhala až o 5 m při každém skoku.
- **Pohled přes hranu jen ve stoje.** Kamera se podívá dolů do propasti, jen když u hrany stojíš, ne pokaždé, když kolem hrany běžíš.
- **Teleport a respawn kamera skočí, místo aby dlouze přejížděla.**

### Změněno
- Nová třída `CameraFocus` (logika zaostření, pokrytá testy).
- Ladicí pole kamery jsou přejmenovaná (`leadDistance`, `followSmoothX`, `followSmoothFloor`, `followSmoothAir`), aby staré uložené hodnoty ve scénách nepřebily nové výchozí.

## [0.13.2] – 2026-11-14
Prapory se znakem.

### Změněno
- **Prapory už nejsou jednobarevné.** Látka má:
  - svislé záhyby se světlejšími hřbety a tmavšími údolími;
  - tmavý lem nahoře a tkaný pruh pod ním;
  - ztmavlé okraje, skvrny;
  - opálený, roztřepený spodek, delší uprostřed.
- Látka je jemnější (16×22 místo 8×10), takže víc a plynuleji vlaje.

### Přidáno
- **Znak na praporu.** Je tmavý jako inkoust a vlaje spolu s látkou: kruh s plamenem šály uprostřed, pod ním čepel s příčkou mířící dolů a po stranách dva kosočtverce.
- **Tyč nahoře** s kosočtverečnými koncovkami. Je pevná a ve větru se nehýbe.
- Platí pro všechny prapory: na šibenici, pod oblouky i ty, které položíš v Level Editoru.

### Testy
- Látka má rozsah tónů (ne jednu barvu) a nese tmavý znak.
- Horní okraj a tyč se ve větru nehýbou.

## [0.13.1] – 2026-11-14
Klenby postavené jako skutečné klenby.

### Opraveno
- **Klenáky jsou těsně u sebe.**
  - Spára mezi nimi se omylem počítala v úhlu násobeném poloměrem, takže v Blenderových modelech měla kolem 15 cm. Teď mají všechny stejnou tenkou spáru (4 cm) jako řádkové zdivo a míří do středu oblouku.
  - Všechny klenáky mají stejnou délku a klenák uprostřed je trochu delší.
  - Zkosení hran je menší, takže kámen nevypadá jako ostrůvek v širokém tmavém pásu.
- **Zdivo nad obloukem navazuje.**
  - Dřív se kameny kolem oblouku vynechávaly celé a zůstávaly tam trojúhelníkové díry.
  - Teď jdou řady přes celou šíři zdi a kameny, které narazí na oblouk, se přitešou podle jeho vnějšího obrysu (extradosu, včetně klenáku). Řady tak dosednou na klenáky běžnou spárou, jak se to u skutečných kleneb dělá.
- **Žádné náhodně chybějící kameny** v neporušeném zdivu ani v modelech z Blenderu.
- Platí pro procedurální oblouky i pro modely z Blenderu (zeď s okny, věž, akvadukt, hrad, útes).

### Testy
- Kámen ve cviklu se ořízne podle oblouku. Kámen mimo oblouk zůstane celý a uvnitř otvoru žádný kámen není.

## [0.13.0] – 2026-11-14
Poctivé zdivo, víc detailu v pozadí, objektech i nepřátelích. Oprava mrtvoly oběšence.

### Opraveno
- **Přeseknutý oběšenec už hrdinu nepřimáčkne.**
  - Padající tělo hledalo zem paprskem, který narazil na hrdinu. Přistálo mu tak na hlavě a vytvořilo pevný blok, ve kterém hrdina uvízl.
  - Teď tělo hrdinu při pádu ignoruje a dopadne na zem.
  - Když leží přes hrdinu, zůstane průchozí, dokud hrdina neodejde. Pak se z něj zase stane pevný schod.

### Změněno
- **Zdivo už není odbyté:**
  - **Tmavá malta:** zeď za kameny je tmavší, takže je vidět každá spára.
  - **Tón kamenů:** každý kámen má trochu jiný odstín (zvětralost).
  - **Klenáky:** jsou zkosené jako ostatní kameny, místo plochých klínů s bílými čárkami.
  - **Plné řady:** na neporušené zdi už náhodně nechybějí kameny.
- Platí to pro procedurální oblouky, zděné bloky terénu i pro všechny modely z Blenderu (akvadukt, hrad, útes, zeď, věž, schody, podstavec sochy). Ty teď exportují tóny jako barvy vrcholů.
- **Sloupy:**
  - dřík je skládaný z bubnů se spárami, má 20 kanelur a mírné zúžení (entazi);
  - nahoře je krk a deska hlavice;
  - zlomené sloupy mají zubatý lom a úlomky u paty.
- **Urny** mají vroubkované břicho, patku, obruč a vyhrnutý okraj.

### Přidáno
- **Nové modely pozadí z Blenderu** (v Level Editoru v Dekoracích):
  - **Suchý strom:** pokroucený kmen s kůrou, rozložité větve, kořeny.
  - **Socha:** zahalená postava se zlomeným mečem nad hlavou a ulomenou rukou, roztržená kápě a rudý hadr na meči, na zděném podstavci s vybledlým nápisem.
  - **Visací klec:** klec s kostmi a rukou trčící ven, rudý cár, řetěz.
  - **Věže na obzoru:** silueta katedrály s věžemi, fiálami, opěrnými oblouky a rozetou.
- Ukázková scéna je má ve všech hloubkách: věže na obzoru, suché stromy, sochy v blízké a střední vrstvě a klece pod oblouky.
- **Detailnější nepřátelé:**
  - **Mnich:** záplaty se stehy, šev, papírové talismany u pasu s rudou pečetí.
  - **Oběšenec:** cáry košile, knoflíky, okov s přetrženým řetězem, provazový opasek.
  - **Kořenový lezec:** trny, houby, další malá oka.
  - **Vrána:** šupiny peří na hřbetě a krovky na křídlech.
  - **Můra:** popelavé pruhy a tečky sazí na křídlech.
  - **Lucerna:** nýty, řetízek, rudá runová destička, kapky vosku.
  - **Papírový drak:** záplaty a tahy štětcem.

### Testy
- Oběšenec přeseknutý nad hrdinou dopadne na zem, hrdina odejde a tělo pak zpevní.
- Zdivo má tmavou maltu a kameny různých tónů, i v modelech z Blenderu.
- Sloupy mají kanelury a desku hlavice, zlomené sloupy úlomky, urny vroubkování.
- Nepřátelé mají minimální počet trojúhelníků, nové modely pozadí existují.

## [0.12.1] – 2026-11-14
Ukazatel životů v levém horním rohu.

### Přidáno
- **HUD: životy jako šála.** V levém horním rohu je rudá plamenná stuha uvázaná v uzlu, která vlaje ve větru. Má tmavé a světlé plamenné pruhy, roztřepený spodní okraj a šikmé roztržené řezy.
  - **Jeden kus stuhy = jeden život.** Ztracené kusy zůstanou jako šedý popel, takže je vždycky vidět, kolik životů bylo.
  - Živý konec hoří malým jazykem plamene. Čerstvě ztracená část chvíli žhne jako uhlíky a zableskne se.
  - Při posledním životě stuha pulzuje.
- **Pod stuhou je tenká nit se silou šály** pro kouzla. Když je šála plná (a tedy pohltí příští zásah), na konci nitě se třpytí jiskra.
- HUD se vytvoří sám s hrdinou, takže ho má každá úroveň bez dalšího nastavování. Kreslí se kódem (bez textur) a škáluje s rozlišením.

### Změněno
- **Šála už neukazuje životy.** Při zranění neslábne ani netmavne. Její délka ukazuje jen sílu pro kouzla a ztlumí ji jen popel.

### Testy
- **Rozložení stuhy:** kus na život s mezerami, ubývání od volného konce, polovina života = půl kusu, prázdný stav a přetečení, vlání víc na volném konci.
- **Hrací test:** HUD se objeví s hrdinou, hned ukáže zbývající život, uhlíky po ráně a sílu šály. Šála sama při zranění neslábne.

## [0.12.0] – 2026-11-14
Zbraně se sesílají jako kouzla a šála dostala vlastní roli.

### Změněno
- **Žádné přepínání zbraní.** Každé tlačítko zbraně šálu promění, hned udeří a po chvíli ji vrátí do látky. Proměna trvá jen asi 0,12 s a slouží jako nápřah úderu.
  - **Kombo:** další stisk během úderu nebo hned po něm (0,6 s) pokračuje bez nové proměny.
  - **Řetězení:** jiná zbraň během úderu přelije šálu rovnou z jedné zbraně do druhé, bez návratu do látky.
- **Ovládání:**
  - Klávesnice: **J / LMB** meč · **K** kopí · **L** bič · **U / RMB** štít · **I** luk (dál fungují i 1–5).
  - Ovladač: **X** meč · **Y** kopí · **RT** bič · **LT** štít · **LB** luk.
- **Držení zbraně:**
  - **Luk:** ťuknutí vystřelí. Držení napne tětivu a ↑/↓ míří (až 55°). Puštění vystřelí.
  - **Štít:** ťuknutí udeří štítem. Držení drží kryt. Zvednutý těsně před úderem (0,2 s) **odrazí** útok: útočník zavrávorá a šála se nasytí. Ve vzduchu štít nad hlavou zpomalí pád jako deštník.
  - **Kopí:** ve vzduchu s **↓** bodne pod sebe. Zásah hrdinu **odrazí** vzhůru a vrátí mu výpad ve vzduchu.
  - **Bič:** když je kruh v dosahu, rozzáří se i ve chvíli, kdy je šála látkou. Seslání biče rovnou chytí kruh.

### Přidáno
- **Síla šály = její délka.**
  - Kouzla ji zkracují: bič 15 %, štít 20 % (plus průběžně, když ho držíš), kopí 25 %, luk 30 %. Meč je zdarma, takže hrdina má vždycky čím bojovat.
  - Šála po chvíli dorůstá a každý zásah ji trochu nasytí.
  - Na krátkou šálu kouzlo nevyjde. Zbraň se nesešle a šála se jen zachvěje.
  - Nepotřebuje to žádný HUD, délka je vidět přímo na šále.
- **Šála chrání.** Plná šála jednou zachytí zásah místo hrdiny a roztrhne se nakrátko. Pak musí dorůst. Kdo vyplýtvá šálu na útoky, nemá čím se chránit.
- **Šála nese:**
  - **Plachtění:** nový stisk skoku během pádu, a držet. Spotřebovává sílu. Samotné držení skoku hrdinu nikdy nezpomalí.
  - **Chycení vzdálenější hrany:** na římsu, na kterou ruce nedosáhnou (do 3,1 m nad nohama), se šála natáhne a hrdinu vytáhne. Stojí 10 %.
- **Šála jako němý společník:**
  - U **tajemství** se volná šála nakloní k němu. V Level Editoru je to neviditelná značka „Tajemství“.
  - Když je **nepřítel** blízko, šála se nervózně naježí.
- **Postupné odemykání.** Hrdina začíná jen s mečem. Ostatní zbraně najde jako **relikvie**: kamenný podstavec s plovoucím rudým obrysem zbraně.
  - V Level Editoru je najdeš jako **Relikvie: Bič / Kopí / Štít / Luk**.
  - Relikvie, které leží za hrdinou na začátku hry, se počítají jako nalezené. „Hrát odsud“ uprostřed úrovně tak nezačne bez zbraní.
- **Ukázková scéna:**
  - Bič leží u startu před kruhem.
  - Kopí je na plošině u stupňů.
  - Štít je na vysoké cestě před stínovým dvojníkem.
  - Luk je na pláni před mostem.
  - U urny na vysoké římse je tajemství.

### Testy
- **Logika:**
  - síla šály: utrácení, dorůstání, pohlcení zásahu, nasycení zásahem;
  - ceny kouzel a rychlost proměny;
  - plachtění jen po novém stisku, deštník a odraz;
  - tah šály k tajemství.
- **Hrací testy:**
  - seslání stojí sílu a ta doroste;
  - zamčená zbraň nebo krátká šála se nesešle;
  - kombo bez návratu do látky a přelití meče do kopí;
  - držený luk a výstřel po puštění;
  - odražení vs. kryt štítem a plná šála, která pohltí zásah;
  - bod kopím dolů s odrazem;
  - chycení římsy 6 m vysoko;
  - relikvie (dotykem i za hrdinou);
  - šála se nakloní k tajemství a naježí u nepřítele.
- **Level kit:** relikvie a tajemství v katalogu, hrdina začíná jen s mečem.
- Původní bojové testy jsou převedené na sesílání.

## [0.11.0] – 2026-11-14
Houpání na biči.

### Přidáno
- **Úchyt pro bič** (Level Editor ▸ Hratelnost ▸ „Úchyt pro bič“). Je to železný kruh na řetězu pod zlomeným trámem s rudou stuhou, aby byl v šedém světě vidět. Model je z Blenderu.
- **Ovládání:**
  - **Chycení:** když má hrdina v ruce bič a kruh je v dosahu (7,5 m, nad rukama, ne za zdí), kruh se rudě rozzáří. Útok pak bičem nešvihne, ale omotá ho kolem kruhu a hrdina se začne houpat.
  - **←/→** rozhoupávají: přikláněním se ve směru švihu nabírá rychlost.
  - **↑/↓** šplhá po biči nahoru a dolů (1,6–7,5 m). Když hrdina stojí na zemi, bič ho sám vytáhne do vzduchu. **↓** na zemi ho pustí.
  - **Skok** = pustit se. Hrdina vyletí rychlostí švihu (navíc +15 % a výskok nahoru). Dokud drží šipku ve směru letu, rychlost se drží. Po puštění šipky normálně zpomalí, takže se dá trefit na římsu.
  - **Zásah nepřítele** shodí hrdinu z biče. Když šálu přepneš na jinou zbraň, bič se rozplyne a hrdina spadne.
- **Fyzika:** bič je kyvadlo s pevnou délkou a jemnějším „houpavým“ gravitačním zrychlením. Když hrdina skáče ke kruhu, zachová si směr letu. Náraz do zdi švih utlumí.
- **Vzhled:**
  - Šála se natáhne od ruky ke kruhu a konec se kolem něj omotá. Uprostřed vede ostré rudé jádro biče.
  - Tělo visí podél biče a otáčí se kolem rukou. Obě ruce jsou nahoře, nohy se při švihu natahují a krčí.
- **V ukázkové scéně:**
  - U startu je kruh a nad ním vysoká římsa (6 m) s urnou, kam se dostaneš jen rozhoupáním. Šibenice se kvůli ní posunula.
  - Nad propastí před mostem (11 m) visí další kruh, takže tam vede druhá cesta vedle přestřelení mostu.
- V Scene view je vidět dosah kruhu (po výběru) a jeho bod.

### Testy
- **Motor:** zachování hybnosti při chycení, kyvadlo s pevnou délkou, rozhoupání zvětší rozkmit, šplhání po biči v mezích, výskok s rychlostí a zdvihem, setrvačnost letu, zásah shodí z biče.
- **Úchyt:** dosah (nad rukama, ve vzdálenosti) a výběr kruhu před hrdinou.
- **Level kit:** model, záře, rudá stuha a položka v katalogu.
- **Hrací testy:**
  - 13m mezeru obyčejný skok nepřeskočí, houpáním ano.
  - Kyvadlo se drží pod kruhem a bič se nenatahuje.
  - Rozhoupáním se hrdina dostane na římsu 6 m vysoko, výš než obyčejný skok.
  - Útok bičem v dosahu se chytí místo švihnutí a skok ho pustí.

## [0.10.2] – 2026-11-14
Rostliny se v Level Editoru samy přichytí k zemi.

### Přidáno
- **Přichycení k zemi (`GroundAnchor`)** pro přízemní rostliny: tráva, kapradí, ostružiní, suché byliny a spadané listí. Kdykoli rostlinu v editoru posuneš, změníš jí vrstvu nebo ji položíš z palety, sama si sedne na nejvyšší terén pod sebou.
  - V **popředí** zapustí kořeny do přední hrany skály, takže se při pohybu kamery hýbe se zemí a neujíždí.
  - **Za rovinou hry** zůstane na horní ploše bloku v hloubce, kterou jí dáš.
  - V dalších vrstvách (blízko, střed, dálka) terén není, takže ji nechá být.
  - Hloubku zapuštění kořenů lze nastavit v Inspectoru (`Sink`).
- Rostlina posunutá nad jámu se ve Scene view označí červeným kroužkem a tlačítko **✔ Zkontrolovat** v Level Editoru ji vypíše.
- Přichytávání funguje i ve chvíli, kdy Scene view zrovna nepřekresluje (hlídá ho editorový skript).
- Ukázková scéna používá stejnou komponentu, takže i její rostliny zůstanou při úpravách na zemi.

### Testy
- Tráva položená z palety do popředí sedne na horní hranu podlahy a do přední hrany skály.
- Nad jámou komponenta nahlásí, že nemá terén.
- Při změně vrstvy se rostlina znovu přichytí, ve vzdálených vrstvách ji komponenta nechá být.

## [0.10.1] – 2026-11-14
Tráva drží na zemi, nepřátelé se otáčejí ve 3D.

### Opraveno
- **Tráva v popředí už neujíždí a nevznáší se.** Trsy trávy, kapradí, ostružiní a bylin v popředí stály v hloubce −3,2 m, před skálou (čelo terénu je v −2 m). Při pohybu kamery se proto posouvaly vůči zemi (paralaxa) a nad okrajem plošin visely ve vzduchu. Teď rostou přímo z přední hrany terénu pod nimi, takže se pohybují se zemí z jakéhokoli úhlu.
- **Nepřátelé se otáčejí celým tělem, ne převrácením obrázku.**
  - Mnich se štítem se otáčí pomalu a těžce (0,5 s), vrány rychle (0,18 s).
  - Otočka je plynule zrychlená a zpomalená a v polovině je tvor natočený čelem ke kameře.
  - Dřív se model zmáčkl přes nulovou šířku a zrcadlově převrátil.
- **Stínový dvojník se konečně otáčí s hrdinou.** Dřív četl převrácení měřítka, které hrdina nepoužívá, a tak se neotáčel vůbec. Teď kopíruje hrdinovu rotaci.

### Testy
- Otočka je čistá rotace bez zmáčknutí, prochází pohledem do kamery, trvá svou dobu, je vyhlazená a zpět se vrací stejnou cestou.
- Rostliny v popředí jsou ukotvené v přední hraně terénu.

## [0.10.0] – 2026-11-14
Mnohem víc detailu ve všech objektech prostředí, podle key artu.

### Přidáno
- **Reliéf kamene ve shaderu.** Plochy natočené ke kameře si drží tón vrstvy. Horní hrany a fazety kamenů chytají měsíc, spodní a levé boky jsou ve stínu. Díky tomu se uvnitř siluet čte zdivo, klenáky i vrstvy skály. Efekt je nejsilnější v hrací ploše a směrem do dálky slábne, popředí zůstává čistě černé.
- **Procedurální oblouky jsou plně vyzděné:**
  - věnec klínových kamenů s vystouplým klenákem;
  - římsy a sokly na pilířích;
  - kvádrové zdivo s vazbou na přeskáčku, kde občas chybí kámen;
  - horní římsa se zubořezem;
  - břečťan s lístky visící z oblouku a římsy.
- **Čela bloků terénu:**
  - skála má vystouplé, mírně nakloněné vrstvy (strata) s drobnými kameny;
  - zřícenina má pravidelné kvádry se zkosenými hranami;
  - vše zůstává uvnitř bloku a pod travou.
- **Modely z Blenderu dostaly zdivo:**
  - akvadukt, hrad a útes: každé pole oblouku má klínové kameny, římsy a kvádry na pilířích i nad obloukem; hrad má navíc bosovaný sokl;
  - zeď s okny, věž a schody jsou postavené znovu a podrobněji: zeď má opěrák, zubořez a sutiny, věž gotická okna, nárožní kvádry a ulomenou korunu, schody nosy stupňů a sloupek zábradlí.

### Testy
- Reliéf materiálů slábne s hloubkou a popředí ho nemá.
- Oblouk má vystouplé kameny se zkosenými hranami a břečťan. Těleso zdi nezasahuje do otvoru.
- Vrstvy skály a kvádry zříceniny jsou uvnitř bloku a pod travou.
- Zeď a věž mají přes 1 500 trojúhelníků.

## [0.9.1] – 2026-11-14
Tráva navazuje na bloky.

### Opraveno
- **Tráva na plošinách vyrůstá přímo z přední hrany.** Dřív stála 35 cm za hranou, takže mezi ní a okrajem bloku zůstával nasvícený šedý pruh horní plochy. Tráva tam vypadala nalepená.
- Kořínky trávy jsou mírně zanořené pod hranu, takže mezi trávou a kamenem není žádná spára. Přibyla prostřední řada trávy, která zakryje celou horní plochu.
- Ke koncům bloku tráva plynule řídne a snižuje se. Dřív končila tvrdým řezem půl metru před okrajem.
- Ostružiní a byliny se ve scéně umísťují jen tam, kde se celé vejdou na plochu. Už nepřečnívají přes okraj do vzduchu.

### Testy
- Tráva roste od přední hrany, má prostřední řadu a kořínky pod hranou.
- Stébla ke koncům trsu ubývají a špičky zůstávají ve stejné výšce.
- Ostružiní se nevejde těsně u okraje, užší byliny ano.

## [0.9.0] – 2026-11-14
Světlejší a mnohem detailnější prostředí podle key artu.

### Přidáno
- **Nové modely prostředí z Blenderu** (složka `Art/Ruins`):
  - **Akvadukt**: dvě patra oblouků. Dolní řada má poslední oblouk zřícený, v horní jeden oblouk chybí. Má rozpadlou římsu a popínavé rostliny s lístky.
  - **Hrad**: palác se třemi patry oken a dvěma věžemi s gotickými okny a kuželovými střechami (jedna je proražená). Dál cimbuří, zazděná okna, zřícený roh se zubatým pahýlem zdi, sutiny a břečťan.
  - **Útes s vesnicí**: vrstevnatá skála s římsami, balvany a zářezem pro vodopád. Nahoře jsou domky, zvonice, suché stromy na hraně a visící kořeny.
  - **Ostružiní** (trnité šlahouny), **suché byliny** (okolíky, klasy, převislá stébla), **řetěz s hákem** a **hromádka rudého spadaného listí**.
- Všechny nové díly jsou v Level Editoru v kategorii Dekorace. Mají české názvy a dají se umístit do libovolné vrstvy.

### Změněno
- **Prostředí je světlejší.** Kámen pod nohama je tmavě šedý s texturou a nasvícenou horní hranou, ne černý. Vzdálenější vrstvy postupně světlají do mlhy.
- **Ukázková scéna je mnohem hustší.**
  - Ve střední vrstvě stojí akvadukt a útes s vesnicí, ze kterého padá vodopád.
  - Na obzoru je hrad, druhý akvadukt a další útes.
  - V popředí a u paty zdí roste ostružiní a byliny.
  - Z rozbitých oblouků visí řetězy.
- Šibenice má místo tyčky skutečný řetěz s hákem.
- Rudý javor má hustší korunu a pod ní leží hromádka spadaného listí.
- Rudé části modelů (listí) zůstávají rudé v každé vrstvě, kámen se tónuje podle hloubky.

### Testy
- Všechny modely prostředí existují. Velké dominanty (akvadukt, hrad, útes) mají přes 1 500 trojúhelníků.
- Kámen se tónuje podle vrstvy, rudé sloty zůstávají rudé, škála odpovídá vrstvě.
- Šibenice má modelovaný řetěz, strom má spadané listí a hustou korunu.
- Katalog nabízí nové díly s českými popisky.
- Vrstvy směrem dozadu světlají a hrací plocha není černá.

## [0.8.2] – 2026-11-14
Nepřátelé v odstínech šedi.

### Změněno
- **Nepřátelé už nejsou čistě černí a nesplývají s prostředím.** Mají tmavě šedé tělo, které je nahoře o něco světlejší (měsíc svítí shora a zepředu), takže jsou vidět záhyby hábitu, kapuce, peří i kořeny. K nohám tmavnou a po obrysu mají jemný světle šedý lem, který je oddělí od tmavého pozadí. Rudé detaily zůstávají.
- **Stínový dvojník zůstává čistě černý** (je to hrdinův stín), jen s velmi slabým lemem.
- Shader siluet má tři nové volitelné vlastnosti: modelování světlem, lem po obrysu a tmavnutí k nohám. Výchozí hodnota je vypnuto, takže prostředí ani hrdina se nemění.

### Testy
- Materiál nepřátel je tmavě šedý, se světlem a světlým lemem.
- Dvojník je čistě černý.
- Prostředí a hrdina zůstali beze změny.

## [0.8.1] – 2026-11-14
Detailní modely nepřátel z Blenderu.

### Přidáno
- **Modely sedmi nepřátel z Blenderu** místo jednoduchých tvarů. Detail je hlavně v siluetě, protože ve hře se čtou obrysy:
  - **Mnich se štítem:** hábit se záhyby a roztřepaným lemem, shrbená záda, dlouhá špičatá kapuce s rudýma očima, pelerína, rukávy do zvonu, provazový pás s uzlem. V natažené ruce drží rudý růženec s přívěskem. Štít má okovaný okraj, nýty, ulomený roh a rudou runu plačícího oka.
  - **Kořenolez:** hrbolaté klubko kořenů s kořínky, čtyři rudé oči jako pavouk a trnité zuby. Devět pokroucených kořenů s větvičkami a zahnutými drápy se samostatně vlní.
  - **Oběšenec:** vychrtlé tělo v roztrhané košili, otrhané kalhoty, bosé nohy s prsty dolů, svěšené ruce s dlouhými prsty, hlava bradou na hrudi a splihlé vlasy. Rudá oprátka má uzel se závity.
  - **Havran:** tělo s hrudí, těžký zobák, rudé oko, vějíř ocasních per, pařáty a křídla s jednotlivými letkami.
  - **Popelavá můra:** článkovaný zadeček, chlupatá hruď, hřebenovitá tykadla a šest nohou. Křídla mají roztřepený okraj jako ohořelý papír, žilky a rudé oko na předním křídle.
  - **Bludička:** kovaná šestiboká lucerna s kudrlinkami, špičatou stříškou, kroužkem a nožkami, uvnitř rudý plamen, pod ní kouřové pramínky.
  - **Papírový drak:** roztrhaný kosočtverečný papír na bambusových vzpěrách svázaných rudou šňůrou, namalovaný obličej se šikmýma očima a zubatým úsměvem, papírové třásně.
- Modely mají oddělené pohyblivé díly (štít, křídla, kořeny, plamen), takže fungují všechny animace. Rudá barva je jen tam, kam ji model namaloval.
- Zdrojové skripty modelů jsou v `ArtSource/creatures/` a dají se znovu sestavit v Blenderu. Když model chybí, použije se záloha z jednoduchých tvarů.

### Testy
- Každý nepřítel z palety má model z Blenderu, dost detailu a díly, které hra animuje. Rudá je jen na detailech.

## [0.8.0] – 2026-11-13
Bestiář: osm nových nepřátel, z toho čtyři létající.

### Přidáno
- **Mnich se štítem:** pomalý mnich s kamenným štítem, který se pomalu otáčí. Zepředu odrazí každý zásah, zezadu je zranitelný. Švih bičem mu štít na 3 s vytrhne. Na blízko se napřáhne a udeří štítem.
- **Kořenolez:** klubko černých kořenů. Visí pod stropem nebo převisem a seskočí na hrdinu, který projde pod ním. Pak běhá po zemi v krátkých skocích. Bodnutí kopím ho přišpendlí: chvíli se nehýbe a bere dvojnásobné poškození.
- **Oběšenec:** houpe se na rudém laně a rozhoupe se proti hrdinovi, dotek bolí. Údery do těla ho jen rozhoupou. Po přestřelení lana spadne a jeho tělo zůstane jako pevný předmět, na který se dá stoupnout nebo ho použít jako závaží.
- **Stínový dvojník:** opakuje hrdinovu cestu i pózy s vteřinovým zpožděním, ale nikdy neskáče. Kde hrdina přeskočil mezeru, dvojník spadne. Zbraně ho jen odstrčí, porazit se dá jen pádem.
- **Havraní roj (létající):** osm inkoustových havranů krouží kolem společného středu, pronásleduje hrdinu shora a občas jím proletí střemhlav. Každý havran je samostatný cíl, takže bič jich srazí víc najednou. Zasažený roj se rozprchne.
- **Popelavá můra (létající):** poletuje vysoko nad hrdinou, takže na ni dosáhne hlavně luk. Pod stropem letí níž. Občas sype popel: hrdinovi, který v něm stojí, na pár sekund pohasne šála a s ní i jeho síla.
- **Bludička s lucernou (létající):** sama neublíží, ale láká: zavěsí se nad nejbližší propast před hrdinou. Zásah ji chytí a zůstane jasně svítit na místě, připravená pro budoucí hádanky.
- **Papírový drak (létající):** létá na rudé niti od kolíku v zemi, houpe se ve větru a vrhá se na hrdinu v dosahu niti. Po přestřelení niti nebo sestřelení se zřítí. Hrdina, který sebere jeho látku, může 8 s plachtit: drží skok a padá pomalu.
- Bič teď zasahuje vlastním typem úderu (švih), který umí vytrhnout štít.
- Všech osm nepřátel je v paletě editoru úrovní (kategorie Hra) s českým popisem. V prototypové úrovni je každý na místě, kde něco učí. Na vzdálené straně přibyl převis pro kořenolez.

### Testy
- Pravidla všech nepřátel: blok a vytržení štítu, seskok kořenolezu, kyvadlo a pumpování oběšence, zpoždění a smrtelný pád dvojníka, střemhlavý let roje, popel můry včetně stropu, hledání propasti bludičkou, nit draka a plachtění.
- PlayMode: mnich blokuje, dokud mu bič nevytrhne štít. Přestřelený oběšenec spadne a zůstane pevný. Roj zmizí s posledním havranem. Drak po přestřelení niti spadne a nechá látku.

## [0.7.7] – 2026-11-13
Hroty zbraní a bič svinutý jako lano.

### Změněno
- **Kopí má hrot:** listovitou čepel, která se za objímkou rozšíří zhruba na trojnásobek ratiště a zúží do ostré špičky.
- **Meč končí špičkou:** ostří se v poslední šestině sbíhají do hrotu.
- Konec meče a kopí je pevný. Nekroutí se z něj plamenný jazyk a jazyky pramínků šlehají už před hrotem.
- **Smotaný bič už nevypadá jako klubko vlny.** Je svinutý do dvou volných smyček, které visí z pěsti, a konec šňůry visí dolů jako u svinutého lana. Na smotaném biči se nekreslí ovíjené prameny ani plamenné jazyky. Objeví se, až se bič rozvine.

### Testy
- Meč končí ostrou špičkou.
- Kopí má listovitý hrot širší než ratiště.
- Smotaný bič má právě tolik smyček, kolik má mít, a každá se vrací k pěsti.

## [0.7.6] – 2026-11-13

### Změněno
- **Štíhlejší zbraně:** s plným jádrem byly kopí a meč moc tlusté.
  - Kopí je teď štíhlé ratiště (0,12 → 0,07 m).
  - Meč má užší čepel (0,22 → 0,16 m).
- Test: zbraně nejsou vlasově tenké, ale kopí je štíhlé a meč není prkno.

## [0.7.5] – 2026-11-13
Plné jádro zbraní, kopí stejně silné po celé délce.

### Opraveno
- **Plné jádro zbraní.** Hlavní pás zbraně omylem končil kousek za rukou, takže čepele a ratiště byly vpředu jen z tenkých ovinutých nití. Teď jádro vede celou zbraní až ke špičce. Týká se všech zbraní, nejvíc je to vidět na meči a kopí.
- **Kopí je stejně silné před tělem i za ním.** Za rukou se dřív překrývaly všechny pramínky šály a vpředu jich ubývalo. Teď pramínky začínají až u dlaně a plamenné jazyky šlehají jen z části před rukou. Za tělem zůstává štíhlé ratiště s patkou.

### Testy
- Jádro vede až ke špičce zbraně.
- U luku vedou všechny prameny celým lukem.
- Plamenné jazyky kopí šlehají až před rukou.
- Meč se drží tam, kde začíná čepel.

## [0.7.4] – 2026-11-13
Zbraně bez vazby ke krku a šála, která nepředbíhá.

### Změněno
- **Zbraň už není přivázaná ke krku.** Při proměně se pruh šály mezi krkem a rukou plynule rozplyne. Zbraň v ruce pak vypadá jako celá šála proměněná ve zbraň. Při návratu do šály se pruh zase objeví.
- **Kopí přečuhuje víc dozadu:** patka je 1,05 m za rukou, hrot 2,1 m před ní.

### Opraveno
- **Šála po zastavení nepředběhne postavu.** Když se hrdina zastaví z běhu a šála se zhoupne, volná látka se k hrudi jen přiblíží a co by přečuhovalo dopředu, se měkce přehne zpátky. Při otáčení postavy šála dál volně přelétne na druhou stranu.
- Opravené dvě chybné interpolace ve vykreslení šály. Vzhled pramenů zůstal stejný.

### Testy
- Pruh u krku zmizí, jen když zbraň ztuhne. Volná šála ani samotná zbraň se neskrývají.
- Šála se před postavou přehne nejvýš o kousek a plynule.
- Patka kopí je dál než 0,95 m za rukou.

## [0.7.3] – 2026-11-13
Luk se stejnými rameny a kopí držené za ratiště.

### Změněno
- **Luk:** obě ramena jsou stejně silná. Dřív většina plamenných pramenů šály končila na horním rameni, takže bylo tlusté a spodní tenké. Teď všechny prameny vedou kolem celého luku.
- Ramena jsou tenčí (0,1 m) a víc prohnutá, takže je mezi madlem a tětivou vidět mezera asi 0,2 m a luk se čte jako luk, ne jako srpek.
- Tětiva je zhruba třikrát silnější.
- **Kopí** postava drží asi ve třetině ratiště, ne za konec. Šála vede z krku k patce kopí, ta přečuhuje asi 0,75 m za ruku (za záda), a ratiště prochází dlaní dopředu ke hrotu 2,2 m před rukou.

### Testy
- Obě ramena luku mají stejný podíl a mezi madlem a tětivou je mezera širší než rameno.
- Patka kopí přečuhuje za ruku, ratiště prochází dlaní a většina kopí je vpředu.

## [0.7.2] – 2026-11-13
Skutečný luk s tětivou a bič smotaný v ruce.

### Změněno
- **Luk je skutečný reflexní luk.** Šála vede od krku k ruce a z madla v dlani vyrůstají dvě zahnutá ramena se špičkami ohnutými vpřed. Mezi špičkami je pořád vidět tětiva jako tenká zářící nit, která je součástí šály. Luk je vysoký asi 1,3 m a spodní rameno je stejně pevné a výrazné jako horní.
- **Natažení luku:** při útoku ruka stáhne střed tětivy do špičky, ve chvíli výstřelu tětiva cvrnkne zpátky rovně.
- **Bič je v klidu smotaný v ruce.** Visí z dlaně jako kotouč. Při náprahu se rozvine od ruky ke špičce, po švihu se do půl sekundy zase smotá.
- Konec zbraně se už skoro nerozpouští do kouře.

### Testy
- Luk má obě ramena, tětivu od špičky ke špičce a madlo v ruce.
- Tětiva se natáhne a po výstřelu narovná.
- Bič je v klidu smotaný u ruky a má stejnou délku jako rozvinutý.
- Bič se rozvíjí během náprahu, od ruky ke špičce.

## [0.7.1] – 2026-11-13
Luk před tělem, vodorovné kopí a bič, který opravdu práská.

### Změněno
- **Bič se pohybuje jako bič.** Už se neotáčí jako tuhá tyč: každý jeho kousek opakuje pohyb rukojeti s malým zpožděním (u špičky asi 0,16 s).
  - Ruka se pomalu napřáhne za hlavu a bič zůstane ležet za ní.
  - Pak ruka rychle vyrazí vpřed a zastaví se v natažení. Po biči se odvalí smyčka až ke špičce.
  - Špička je nejrychlejší až na konci a práskne: vystříkne z ní sprška jemných vláken. Zásah platí ve chvíli prásknutí.
  - Útok bičem trvá 0,8 s.
- **Kopí míří vodorovně.** Neotáčí se s předloktím: drží se u boku s vodorovným předloktím a při bodnutí ho paže tlačí rovně vpřed s výpadem. Změřený sklon je −1° v klidu a −2° při bodnutí.
- **Luk už nezakrývá tělo.** Postava ho drží v natažené ruce před sebou a luk zůstává svisle. Jeho oblouk je mělčí, takže ramena nesahají do trupu. Je i širší.
- **Zbraň v ruce je před postavou**, šála zůstává za ní.
- Každá zbraň má vlastní postoj a útoky v něm začínají i končí.

### Testy
- Bič v klidu drží jako celek, švih vytvoří smyčku a prásknutí přijde po zastavení ruky a zasáhne.
- Kopí je vodorovné a neotáčí se.
- Luk je v natažené ruce a jeho ramena nezajíždějí do těla.
- Zbraň je před postavou.

## [0.7.0] – 2026-11-13
Zbraně z plamene a proměna jako vír.

### Přidáno
- **Zbraně ve stylu plamenné šály.** Pramínky šály v zbrani nemizí: ovíjejí čepel jako pletené plameny a podél celé čepele z nich vyšlehují plamenné jazyky, které stoupají nahoru a dozadu. Jádro čepele je plné, štít je plamenný vír. Zbraně jsou širší (luk a bič už nejsou tenké nitky) a v shaderu si drží plamenné pruhy.
- **Proměna šály ve zbraň jako vír.** Šála se stáhne spirálou do dlaně (nejrychleji u dlaně, vzdálená látka jen přitéká) a zbraň z ní vyroste. Látka drží objem, dokud skoro neztuhne. Tam, kde právě tuhne, se rozzáří čelo proměny. Proměna trvá asi 0,4 s místo 0,25 s, aby byla vidět.
- **Návrat do šály.** Čepel se při tání odvine stejným směrem, jakým vznikla. Uvolněný plamen pak vydechne nahoru a dozadu (asi 0,9 s), takže odvane zpátky za ramena, místo aby se slil k nohám.

### Opraveno
- **Šála v běhu vlaje vodorovně.** Široký pás už není vycentrovaný na krk, ale z krku visí. Jeho horní vrstvy se rozestupují méně než dolní, takže nepřečuhuje nad hlavu.

### Testy
- Spirála proměny začíná i končí v klidu.
- Vír je nejsilnější u dlaně.
- Pletení se týká jen zbraně a drží se u čepele.
- Plamenné jazyky vyrůstají podél celé čepele.
- Zbraně nejsou tenké nitky.
- Výdech při návratu do šály.
- Šála visí z krku.
- PlayMode test proměny počítá s delší proměnou.

## [0.6.4] – 2026-11-13
Šála v proporcích key artu.

### Změněno
- **Mohutnější a kratší šála.** Je kratší (3,3 → 2,7 m, i s víry asi 1,7× výška postavy) a širší (0,42 → 0,65 m, zhruba třetina výšky postavy). U krku zůstává úzká a ke středu se rozšiřuje.
- Vrstvy plamene se rozestupují víc, takže tvoří větší hmotu, a koncové víry jsou větší.
- Mezery mezi pruhy jsou méně průsvitné, takže šála působí plnější.

### Testy
- Proporce šály vůči postavě: šála není zbytečně dlouhá a je široká aspoň třetinu výšky postavy.

## [0.6.3] – 2026-11-13
Šála jako plamen: nižší, lehká a v odstínech červené.

### Změněno
- **Šála ve stoje už nevlaje nahoru.** Vlaje vodorovně za krkem a konec mírně klesá, jako na key artu. Okolní proudění je vodorovné (dřív mělo složku vzhůru, která se sčítala s novým vánkem), vztlak je menší a koncové víry kratší.
- **Šála jako plamen, ne chobotnice.** Vrstvy se překrývají v jedné hmotě a rozestupují se až ke konci. Všechny konce se stáčejí stejným směrem do jednoho volného víru (nejvýš o otáčku), žádné samostatné kroužky. Uprostřed se plamen mírně rozšiřuje.
- **Odstíny červené:** dlouhé zvlněné pruhy od tmavě vínové přes karmínovou a červenou po jasnou, mezi nimi průsvitné mezery, okraje tmavší a tělo prosvícené zevnitř. Méně neonové barvy.

### Opraveno
- **Šála už nepadá těžce na zem.** Vůči hrdinovi se svisle pohybuje nejvýš 2,5 m/s. Při skoku už nevystřelí nad hlavu (dřív až 1,7 m nad krk, teď 0,9 m) a po dopadu se snáší (2,8 m/s místo 9,4 m/s). Víry od těla už nezesilují nad rychlost běhu, takže pád nerozvíří bouři.

### Testy
- Šála ve stoje nestoupá nad krk.
- Po dopadu se snáší výrazně měkčeji než těžká látka.
- Při skoku nevyletí nad hlavu.
- Konce šály tvoří jeden vír, ne kroužky chobotnice.

## [0.6.2] – 2026-11-13
Klidnější šála a tráva, která neutíká.

### Změněno
- **Šála se hýbe ladněji a pomaleji.** Ve stoje se vlní zhruba poloviční rychlostí a víry jsou slabší, takže už nepoletuje nervózně. Pomaleji běží i vlnky, kroucení, pramínky, „dýchání“ spirál a proudění barvy v shaderu. V běhu se šála dál vlní a nenapne se do pláště.

### Opraveno
- **Tráva v popředí už neutíká.** Dřív stála v celém levelu ve stejné výšce, takže nad propastmi a pod vyššími plošinami visela ve vzduchu a při pohybu kamery ujížděla. Teď roste na zemi pod sebou, u hrany se trs zúží a nad propastí se nevysází.

### Testy
- Tráva sedí na zemi, nad propastí se nevysází, u hrany nepřečuhuje.
- Šála se ve stoje hýbe klidně (průměrné zrychlení výrazně pod dřívějším „nervózním“ laděním).

## [0.6.1] – 2026-11-13
Šála podle key artu a zbraň v běhu vepředu.

### Změněno
- **Zbraň v běhu už se netáhne za hrdinou.** Paže drží zbraň v gardě před tělem, čepel vede pohyb vpřed a mírně vzhůru.
- **Šála je jedna souvislá látka s „víc konci“** jako na key artu:
  - hlavní stuha končí velkou smyčkou,
  - z hlavní stuhy se odštěpují široké pramínky a jemná hedvábná vlákna; každý pramínek začíná přímo na stuze, takže víření zůstává spojité,
  - každý konec se stáčí do plynulé spirály (zakřivení roste ke špičce), v běhu se spirály trochu rozvinou,
  - látka je širší a v shaderu má jemná podélná vlákna, která se k okrajům rozestupují.
- **Šála se vznáší** na vlastním magickém vánku i ve stoje a vlaje za hrdinou, místo aby visela k zemi.
- **Odletující kouřové útržky** (vypadaly jako poletující lístky) jsou omezené na občasná jemná vlákna ze špičky.

### Testy
- Pramínky začínají na hlavní stuze, šála má aspoň 5 konců a každý končí spirálou.
- Spirála je spojitá a stočí se o víc než jednu otáčku.
- Šála ve stoje vlaje za hrdinou a neklesne k zemi.
- V běhu drží hrdina zbraň v gardě vpředu.

## [0.6.0] – 2026-11-13
Skutečné útoky zbraněmi, kamera, která ukazuje, kam skáčeš, a víc kouře.

### Přidáno
- **Choreografie útoků pro každou zbraň** (klíčové pózy celého těla, ne jen otočení čepele):
  - **Meč**: nápřah nad rameno, diagonální sek dolů s výpadem vpřed, dokončení švihu nízko u boku.
  - **Kopí**: stažení k boku, bodnutí s napnutou paží a dlouhým výpadem, návrat.
  - **Bič**: nápřah nad hlavu, prásknutí vpřed, švihnutí ocasem.
  - **Štít**: přikrčení za štít, úder ramenem vpřed s krátkým výpadem.
  - **Luk**: zvednutí luku, natažení tětivy k lícu (viditelná tětiva), výstřel a doznění.
- Útok s mečem, kopím a štítem posune hrdinu výpadem dopředu. V běhu útočí hlavně horní polovina těla.
- **Kamera se dívá před sebe a dolů**: před srázem nebo při pádu se sama podívá dolů, takže vidíš, kam dopadneš. Podržením W/S se rozhlédneš nahoru a dolů.
- Testy: kamera (posun dolů, zóny), choreografie útoků (meč, kopí, luk, začátek a konec v bojovém postoji), zbraň nezaostává za běžícím hrdinou.

### Opraveno
- **Lano k přestřelení je zase v záběru.** Kamerové zóny mají rozsah relativně ke svému objektu, takže zóna mostu jde s mostem, ať ho položíš kamkoliv.
- **Zbraň v běhu už nezaostává**: ztvrdlá čepel se nese pevně s rukou a hrdina ji nepředběhne.

### Změněno
- Víc kouřových kudrlinek: až 230 najednou, delší stopy a k tomu jemná vlákna, která žijí déle a stáčí se do těsnějších spirál.

## [0.5.2] – 2026-11-13

### Změněno
- **Šála místo pláště superhrdiny**: v běhu se už nenatáhne do napjaté plachty. Vítr z běhu cítí jen do 1,7 m/s, takže za hrdinou volně vlaje, a má vlastní život:
  - hadovitá vlna běží od krku ke špičce (v pohybu silnější),
  - ocas stoupá jako kouř,
  - špička se stáčí do háčku.
- Šála je užší a rovnoměrná (bez „pláštěného“ rozšíření uprostřed), vrstvy jsou tenké stuhy, které se k ocasu rozplétají.
- Délky segmentů šály se hlídají v rovině obrazu (2.5D), u krku se už látka nemačká.
- Test: běžící šála je zakřivená a není napnutá na plnou délku.

## [0.5.1] – 2026-11-13

### Změněno
- **Odrazy od zdi**: od jedné zdi se už nedá šplhat opakovanými odrazy. Další odraz musí být od protější zdi, nebo až po dotyku země či chycení hrany. Zeď, od které ses právě odrazil, ti taky nevrátí dash ve vzduchu.
- **Zbraň jako prodloužení předloktí**: čepel sleduje úhel předloktí, takže švih, nápřah i postoj dělá paže a zbraň s ní letí jako jeden celek (žádné samostatné otáčení zbraně).
- **Postoj se zbraní**: v klidu čepel vpředu a nízko, v běhu nesená nízko za tělem (jako na key artu). Při útoku tělo zabírá do seku a volná ruka dělá protiváhu.
- **Proměna ve zbraň**: čepel vyroste z dlaně s mírným přešvihnutím, látka se kolem ní rozvine ve víru kouře. Zpětná proměna se rozplyne v kouř.
- **Kouř**: mnohem víc kudrlinek (až 150 najednou, delší stopy), každá se stáčí do zrychlující spirály. Kouří i ztvrdlá čepel a při seku za špičkou zůstává kouřový oblouk. Zbraně si v shaderu drží kouřové, neklidné okraje a doutnající špičku.
- Testy: tři nové testy pravidla odrazů od zdi, postoj se zbraní v klidu a v běhu.

## [0.5.0] – 2026-11-13
Lehčí pohyb, elegantní šála a nový hrdina podle key artu.

### Změněno
- **Lehčí pohyb**: vyšší skok (3,4 m, delší stoupání), mírnější pád, pomalejší klouzání po zdi (2,3 m/s), vyšší odraz od zdi (3,3 m), kratší zámek ovládání po odrazu (lépe se řídí zpět ke zdi).
- **Šachta** v prototypu je užší (3,2 m) a levý pilíř začíná níž – dá se vyskákat. Nový PlayMode test šachtu opravdu vyleze střídavými odrazy.
- **Hrdina** přemodelovaný podle obrázku: atletická postava, široká ramena, chrániče na obou ramenou, hrudní plát, opasek se sponou, rukavice, chrániče předloktí a kolen, vysoké boty, bez sukně. Vlasy: hmota ostrých pramenů učesaných dozadu a nahoru (14 pramenů s pružinovou fyzikou).

### Přidáno
- **Shader šály** (`CrimsonVeil/Veil`): odstíny od tmavě vínové po zářivé jádro, pruhy tekoucí po délce látky, tmavé záhyby, měkké nepravidelné okraje, ocas rozpadající se v kouř. Ztvrdlé zbraně mají ostřejší hranu a jasnější jádro.
- **Vrstvená šála**: tři pruhy látky (hlavní, široký zadní, tenký světlý), které se k ocasu rozbíhají jako vrstvený plášť a při proměně ve zbraň se slijí do jedné čepele. Křivka vyhlazená Catmull-Romem (žádné lomy).
- **Kouřové kudrlinky**: z ocasu šály se odvíjejí tenké stuhy kouře, které nesou víry. Po teleportu nebo respawnu se smažou.
- **Zbraň v ruce**: zbraně vyrůstají ze skutečné kosti ruky a paže sleduje švih (nápřah, sek dolů, protiváha druhé ruky).

### Opraveno
- Výbuchy zásahů, šíp i provaz mají vlastní jednoduché materiály (nerozpadají se jako látka).

## [0.4.1] – 2026-11-13

### Přidáno
- **Ruiny z Blenderu** (`Art/Ruins`, zdroj `ArtSource/Ruins.blend`): zeď s klenutými okny a ulomenou korunou, osmiboká věž s okny a římsami, rozpadlé schody. V Level Editoru jako „Zeď s okny“, „Věž“, „Schody“, v libovolné vrstvě.
- **Kapradí** (`FernProp`): keře s obloukovitými listy a lístky, vlají ve větru. Do popředí i na hrací plochu.
- Prototypová úroveň: věže v dálce a ve střední vrstvě, zdi s okny, schody, kapradí v popředí a u paty zdí.
- Testy Level Kitu: každý dílek z palety se položí přesně na místo kliknutí, do správné vrstvy a bez chybějících materiálů; TerrainBlock má přesnou kolizi a trávu uvnitř hran; přesun do vrstvy mění hloubku, odstín i měřítko. Celkem 144 EditMode + 13 PlayMode.

### Opraveno
- Stébla trávy se mohla naklonit přes okraj trsu (a tím i přes okraj plošiny) – teď zůstávají uvnitř.
- Pásy mlhy v prototypu jsou uklizené ve skupině Dekorace.

### Přidáno
- **Pohyb po zdech**: klouzání po zdi (když tlačíš proti zdi a padáš), odraz od zdi (s krátkou „coyote“ tolerancí po opuštění zdi), dash od zdi (vždy směrem od ní). Dotek zdi obnoví dash ve vzduchu.
- **Chycení hrany a šplh**: hrdina se chytí hrany, která je v dosahu rukou, a vyšplhá nahoru (skok nebo nahoru). Dolů = pustit se, skok od hrany = odraz. Detekce hrany přes paprsky (zeď v úrovni hrudníku, volný prostor nad hranou, rovný vršek, místo pro postavení).
- Animace pro klouzání po zdi, visení a šplh (`HeroPoseSolver`).
- Testy: 11 EditMode testů stavů zdi a hrany, 2 PlayMode testy s reálnými kolizemi (chycení a vyšplhání na 4 m vysoký blok, odraz od vysoké zdi).
- **Plynulá šála**: ztvrdnutí běží jako vlna od krku ke špičce (povolení od špičky), tvary zbraní se do sebe plynule převíjejí se spirálovým vinutím, při otočce se zbraň „přetočí“ místo skoku, zbraně v klidu dýchají (meč se pohupuje, bič vlní). Špička zbraně zůstává měkká.
- **Nový štít**: šála stočená do rotujícího víru (místo ploché desky), při úderu se roztočí. Meč má zahnutou čepel. Hrdina drží zbraň zvednutou paží.
- **Vlasy**: delší prameny padající dozadu a dolů (12 pramenů, každý se 3 kostmi), pružinová fyzika (`SpringBones`, `SpringChainSolver`) – vlají při běhu, skocích a otočkách.
- **Level Editor** (menu *Crimson Veil ▸ Level Editor*, Ctrl+Shift+L): paleta dílků (Terén, Hratelnost, Dekorace, Scéna) s českými popisky, kliknutí do Scene view = položení s přichytáváním k mřížce, Shift = víc kusů, volba vrstvy hloubky pro dekorace, přesun vybraných do jiné vrstvy, „Hrát odsud“, kontrola úrovně, „Nová úroveň“, zobrazení dosahu skoku z hran vybrané plošiny.
- **TerrainBlock**: plošina/zeď/útes s úchyty ve Scene view (šířka, výška), přesná kolize, generovaná skalní silueta (praskliny, zubatý spodek, převis), tráva a visící kořeny vždy uvnitř hran.
- Dílky jako prefaby (`LevelKit/Prefabs`), generované z kódu (`LevelKitFactory`, `LevelKitCatalog`); prototypová úroveň se staví ze stejných dílků.
- Detail: javorové listy (textura s alpha-clipem), praskliny v kameni (Voronoi), sloupy s hlavicemi, větší měsíc, víc vrstev ruin, vodopád u šachty.
- Nová prototypová úroveň: blok na chycení hrany, šachta na odrazy od zdí, horní cesta, pláň s rudým stromem, propast s mostem.

### Opraveno
- Tráva přečnívala přes okraje plošin a při pohybu kamery se „rozjížděla“ – teď ji generuje sám TerrainBlock uvnitř svých hran.

## [0.3.1] – 2026-11-13

### Opraveno
- Rudý provaz hádanky visel 8–10 m vysoko, mimo záběr kamery, takže nebyl vidět. Teď visí v ~7 m (pořád mimo dosah meče ve výskoku) a u propasti se kamera plynule oddálí a zvedne (`CameraZone`).
- Rudý strom stál přímo pod provazem a jeho koruna provaz maskovala. Strom je přesunutý doleva (x ≈ 38), provaz je teď čitelný proti šedému pozadí.

### Přidáno
- `CameraZone`: oblast úrovně, ve které kamera plynule přejde na jiný offset (oddálení, zvednutí pohledu).

## [0.3.0] – 2026-11-13
Boj, první nepřítel, první hádanka a doladěný hrdina a svět.

### Přidáno
- **Boj šálou** (`VeilCombat`): během aktivní fáze švihu tvoří každý segment zbraňové části stuhy kapsli zásahu. Zasahuje simulovaná stuha, takže co vidíš, to zasáhne. Každý cíl dostane max. 1 zásah za švih. Zbraně se liší poškozením, odhozem a dosahem: kopí bodne nejdál, štít nejvíc odhodí, bič sahá nízko a daleko.
- **Luk s mířením**: W / šipka nahoru / páčka nahoru při výstřelu pošle šíp pod úhlem 50°. Šípy zraňují a přeřezávají.
- **Zdraví hrdiny** (`PlayerHealth`) bez HUD: šála se s úbytkem zdraví ztenčuje, tmavne a třepí na konci. Po chvíli bez zásahu se zdraví pomalu vrací. Po smrti se hrdina rozplyne v inkoustu a objeví se na posledním checkpointu.
- **Blok štítem**: zpevněný štít natočený proti útočníkovi zásah zablokuje a útočníka vyvede z rovnováhy.
- **Game feel**: hitstop (krátké zmrazení při zásahu), třes kamery podle „traumatu“ (Perlin, běží i během hitstopu), inkoustové výbuchy při zásahu.
- **Nepřítel – inkoustový přízrak** (`Art/Enemy/Wraith.fbx`, zdroj `ArtSource/Wraith.blend`): kápě s hroty, dlouhé ruce s drápy, potrhaný plášť, rudé oči. Chování (`EnemyBrain`): hlídkuje, pronásleduje, pak dobře čitelně telegrafuje útok (ruce se zvednou, oči se rozžhaví) a teprve potom se vrhne. Zásah wind-up přeruší. Hrdinou lze proplout, ale dotek bolí. Procedurální animace (`EnemyAnimator`) a rozpad v inkoust při smrti.
- **Reakce prostředí**: rozbitné urny (`Breakable`) se rozletí na střepy.
- **První hádanka**: u propasti stojí prkno držené vysoko nataženým rudým provazem. Rudá znamená „tohle šála dosáhne“. Přeřízne ho jen šíp vystřelený nahoru, prkno pak spadne (`HingedBridge`, zrychluje jako při pádu a na konci se odrazí) a vytvoří most. Za propastí je checkpoint a další přízraci.
- **Svět**: rozbité oblouky s rozeklaným lomem (`ArchProp.breakAmount`), hromady sutin (`RubbleProp`), urny (`UrnProp`, rotační těleso), rozpadlý akvadukt za útesem, úroveň prodloužená o propast a další plošinu.
- `RigJoint`: sdílená kalibrace kostí pro procedurální animaci (hrdina i nepřítel).
- Testy: zdraví, mozek nepřítele, bojové profily, odhoz, rozbité oblouky, sutiny, urny, padající most. PlayMode: meč rozbije urnu, kopí zraní přízraka právě jednou, přízrak telegrafuje a zraní, štít blokuje, šíp přeřízne provaz a most spadne, smrt a respawn. Celkem 84 EditMode + 11 PlayMode.

### Změněno
- **Hrdina**: tunika s potrhaným lemem, opasek, ovinutí šály kolem krku, chránič ramene, chrániče předloktí a objemnější hrudník.
- `MotorLogic` umí odhoz se stunem (`ApplyKnockback`), během kterého hráč neovládá postavu.
- `IPlayerInput` má vertikální míření `AimY`.

## [0.2.0] – 2026-11-13
Vizuální průchod: hrdina z Blenderu s procedurální animací a atmosféra blíž ke key artu.

### Přidáno
- **Hrdina** (`Art/Hero/Hero.fbx`, zdroj `ArtSource/Hero.blend`): organická silueta (Skin modifier) s rozevlátými vlasy, kostra s 19 kostmi a automatickými vahami.
- **Procedurální animace** (`HeroPoseSolver`, `HeroAnimator`): krok běhu se odvozuje od uražené vzdálenosti (nohy neskluzují), délka kroku a amplituda rostou s rychlostí, náklon vpřed, houpání pánve, dýchání v klidu, pózy pro výskok, pád, dash, útok a tlumený dopad do kolen. Osy kostí se kalibrují za běhu z rigu, takže nezáleží na konvenci os při importu. Šála se drží krku animované postavy.
- **Shadery**: `CrimsonVeil/Silhouette` (inkoustová silueta se zrnem, přízemní mlha podle výšky a vzdálenosti, vítr pro trávu a prapory), `CrimsonVeil/Mist` (driftující pásy mlhy, světelné paprsky, vodopády), `CrimsonVeil/InkGrade` (finální průchod: vše kromě rudé převede do šedi, papírové zrno).
- **Procedurální geometrie** (`ProceduralMeshes` + komponenty `ArchProp`, `GrassProp`, `BannerProp`, `TreeProp`): skutečné kulaté oblouky, trsy trávy, potrhané prapory, větvený rudý strom.
- **Scéna** ve vrstvách hloubky: černé popředí s trávou, hrací plocha s šibenicí a prapory, blízké ruiny, akvadukt s vodopádem, vzdálené věže a útesy, mlha mezi vrstvami, paprsky měsíčního světla, rudý strom s listím.
- `AtmosphereController` (globální mlha a vítr pro shadery, funguje i v editoru).
- `AutopilotInput`: skriptovaný vstup pro ukázky, záznamy a testy.
- Testy: generátory geometrie (5), řešič póz (9), PlayMode test autopilota. Celkem 62 EditMode + 5 PlayMode.

### Změněno
- Šála je delší (3,3 m, 24 bodů), víry jsou silnější, takže se šála ani v plném běhu nenatáhne do rovné čáry. Profil šíře se vlní uprostřed.
- Chomáčky šály jsou měkké inkoustové kapky a padající listí má tvar listu.
- Kamera je výš (víc nebe, méně černé země), post-processing je jemnější.

### Opraveno
- Šedé odstíny se v lineárním barevném prostoru nesrážejí do černé (kontrast se počítá v percepčním prostoru).

## [0.1.0] – 2026-11-13
První hratelný prototyp.

### Přidáno
- **Pohyb hráče** (`MotorLogic`, `PlayerController`): rychlý rozběh a otočka, coyote time (0,1 s), jump buffer (0,13 s), variabilní výška skoku, „apex hang“, rychlejší pád, dash na zemi i ve vzduchu (1× za skok), dash-jump zachovává hybnost. Squash & stretch při skoku, dopadu a dashi.
- **Kouzelná šála** (`VeilSimulation`): Verletova stuha, kterou rozvlňuje Kármánova vírová stezka. Víry se odtrhávají střídavě s frekvencí podle Strouhalova čísla (`f = St·U/D`), mají Lamb-Oseenovo jádro, unáší je vzduch a postupně slábnou.
- **Formy šály**: bič, meč, kopí, štít, luk (klávesy 1–5 / D-pad + LB; stejná klávesa = zpět na látku). Látka „ztvrdne“ do tvaru zbraně, ale víry ji dál rozvlňují. Útok (J / LMB / X) s procedurálním švihem, luk vystřelí šíp z látky.
- **Vizuál**: renderer stuhy, rudé chomáčky unášené víry, padající rudé listí, 2.5D kamera s look-aheadem.
- **Prototypová scéna** `Prototype_Ruins` stavěná z kódu (menu *Crimson Veil > Build Prototype Scene*): siluety v popředí, šedé ruiny v mlze, měsíc, rudé prapory, post-processing (bloom, vinětace, film grain, kontrast).
- **Testy**: 48 EditMode (pohyb, simulace šály, tvary zbraní) a 4 PlayMode (dopad, běh, skok, proměna v meč a útok).
- README s vizí, ovládáním a strukturou projektu.

### Změněno
- Projekt přejmenován na **Crimson Veil** (productName, companyName NeoTaxidermist).

### Odstraněno
- Ukázkový obsah ze šablony URP (Readme, TutorialInfo, SampleScene).
