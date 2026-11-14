# Changelog

Všechny podstatné změny projektu Crimson Veil.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/), verze podle [SemVer](https://semver.org/lang/cs/).

## [Unreleased]

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
