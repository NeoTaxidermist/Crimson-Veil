# Crimson Veil

2.5D akční plošinovka v Unity 6.3 (URP). Pohyb a boj ve stylu *Prince of Persia: The Lost Crown*, atmosféra a vyprávění beze slov ve stylu *Limbo*.

![Prototyp](Docs/prototype-flow.png)

## Vize
- **Vizuál:** černobílý svět v odstínech šedi, vrstvy v mlze, siluety v popředí. Jedinou barvou je **ruda** – šála hrdiny, listí, prapory, detaily.
- **Šála (Veil):** kouzelná látka, která víří jako Kármánova vírová stezka. Při pohybu víří víc, na stisk klávesy „ztvrdne“ do zbraně nebo nástroje – ale pořád je to ta samá látka, napojená na krk a ruku hrdiny.
  Formy: **bič, meč, kopí, štít, luk**.
- **Hratelnost:** extrémně plynulý běh a skoky, hádanky, příběh vyprávěný bez dialogů.

## Ovládání (prototyp)
| Akce | Klávesnice | Gamepad |
|---|---|---|
| Pohyb | A/D, šipky | levá páčka |
| Skok (držet = výš) | Mezerník | A / South |
| Dash | Levý Shift | B / East, RB |
| Útok | J, levé tlačítko myši | X / West |
| Bič / Meč / Kopí / Štít / Luk | 1 / 2 / 3 / 4 / 5 (znovu = zpět na látku) | D-pad ← ↑ → ↓, LB |

## Struktura
```
Assets/_CrimsonVeil/
  Scripts/Runtime/
    Core/      MovementMath – ladění skoku přes výšku a čas
    Player/    MotorLogic (čistá logika pohybu), PlayerController (CharacterController 2.5D), vstupy
    Veil/      VeilSimulation (Verlet + Lamb-Oseen víry), formy zbraní, renderer stuhy, chomáčky, šíp
    Camera/    CameraFollow25D – look-ahead, měkčí sledování ve vzduchu
    World/     LeafFall – padající rudé listí
  Scripts/Editor/  PrototypeSceneBuilder – menu "Crimson Veil > Build Prototype Scene"
  Tests/EditMode   unit testy pohybu a simulace šály
  Tests/PlayMode   integrační testy hráče a proměny šály
```

### Pohyb (`MotorLogic`)
Coyote time, jump buffer, variabilní výška skoku (pustit = rychlejší dopad), „apex hang“ v nejvyšším bodě, rychlejší pád, dash (i ve vzduchu, 1× za skok), dash-jump zachovává hybnost. Vše laditelné v Inspectoru na `PlayerController`.

### Šála (`VeilSimulation`)
- Řetěz Verletových bodů s distančními omezeními.
- Tělo hrdiny odtrhává střídavé víry s frekvencí podle Strouhalova čísla `f = St·U/D` (+ „magický“ klidový základ) – čím rychleji běží, tím divočeji šála víří.
- Každý vír je Lamb-Oseenův (konečná rychlost v jádru, jádro roste difuzí); víry unáší vzduch a postupně slábnou.
- Formy zbraní nastaví cílový tvar a „tvrdost“: body se přitahují k tvaru, délky segmentů se plynule mění, ale víry dál působí – zbraň pořád žije.

![Meč](Docs/prototype-sword.png)

## Testy
Unity → Window → General → Test Runner → EditMode / PlayMode (48 EditMode + 4 PlayMode).
