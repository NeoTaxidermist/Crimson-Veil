# Changelog

Všechny podstatné změny projektu Crimson Veil.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/), verze podle [SemVer](https://semver.org/lang/cs/).

## [Unreleased]

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
