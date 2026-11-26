# Kategorie testů

Každá testovací třída má jednu nebo víc kategorií (`[Category("...")]`) podle oblasti hry, kterou hlídá. Po změně se pouštějí jen kategorie oblastí, na které změna sáhla; celá sada jen po velkých zásazích a před sloučením do `main`.

| Kategorie | Co hlídá |
|---|---|
| `Motor` | pohyb postavy: běh, skoky, zdi, hrany, skluz, kotoul, lano, ovladač postavy |
| `Hero` | tělo a animace hrdiny: póza, mocap, ruce, obličej, vlasy, kontakt se zemí a zdí, hrana a výlez |
| `Veil` | šála a zbraně: simulace, tvary zbraní, sesílání, kombo, vykreslení |
| `Combat` | boj a nepřátelé: zdraví, zásahy, mozky a modely bytostí |
| `World` | úrovně: terén, rekvizity, stavebnice, rozšíření úrovně, testovací místnost |
| `UI` | ovládání (klávesy a přemapování), menu, pauza, HUD, mapa, záznam testovací hry |
| `Camera` | kamera |
| `Tools` | nástroje: nahrávání klipů, kontrola CHANGELOGu |

## Co pustit podle změněných souborů

| Změněno | Kategorie |
|---|---|
| `Scripts/Runtime/Player/MotorLogic.cs`, `PlayerController.cs`, `Somersault.cs`, `AutopilotInput.cs` | `Motor`, `Hero` |
| `Scripts/Runtime/Player/Hero*.cs`, `LimbReach.cs`, `SpringBones.cs` | `Hero` |
| `Scripts/Runtime/Player/InputBindings.cs`, `PlayerInputReader.cs`, `IPlayerInput.cs` | `UI`, `Motor` |
| `Scripts/Runtime/Player/ClipRecorder.cs` | `Tools` |
| `Scripts/Runtime/Veil/*` | `Veil` (+ `Combat` u `VeilCombo*`, `VeilCasting*`) |
| `Scripts/Runtime/Combat/*` | `Combat` (+ `Veil` u `VeilCombat*`) |
| `Scripts/Runtime/World/*` | `World` |
| `Scripts/Runtime/UI/*` | `UI` |
| `Scripts/Runtime/Camera/*` | `Camera` |
| `Scripts/Runtime/Core/*` | `Motor`, `Hero`, `Veil` |
| `Scripts/Editor/HeroMocapBaker.cs`, modely hrdiny | `Hero` |
| `Scripts/Editor/*` (stavebnice, scény) | `World` |
| `Shaders/Veil.shader` | `Veil` |
| `Shaders/*` ostatní | `World` |
| prefab hrdiny (`LevelKit/Prefabs/Player.prefab`) | `Motor`, `Hero`, `Veil` |
| scény | `World`, `UI` |
| `CHANGELOG.md` | `Tools` |

EditMode i PlayMode se pouštějí pro vybrané kategorie (EditMode je rychlý, PlayMode pomalý). Nový test dostane kategorii podle oblasti; test bez kategorie by se při výběru nepustil nikdy.
