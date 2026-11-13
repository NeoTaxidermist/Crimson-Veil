# Changelog

Všechny podstatné změny projektu Crimson Veil.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/), verze podle [SemVer](https://semver.org/lang/cs/).

## [Unreleased]

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
