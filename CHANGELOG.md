# Changelog

Všechny podstatné změny projektu Crimson Veil.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/), verze podle [SemVer](https://semver.org/lang/cs/).

## [Unreleased]

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
