---
name: health-and-crimson
description: "v0.110 economy: no autoheal, mend knot from hits (R), crimson drops -> altar upgrades at checkpoints, heart shards, crimson left at death"
metadata:
  type: project
---

2026-11-28 the user: "Ten autoheal je divný. A z nepřátel by mělo něco po zabití vypadnout... k upgradům energie a maximálního zdraví? Nebo k healování?" I proposed; the user: "Udělej to všechno do branche. Btw těch checkpointů tolik v budoucnu nebude." Death penalty variant was not chosen explicitly - I made the strict one (crimson stays where he fell) the default, switchable by `HeroProgress.DeathLeavesCrimson`; tell the user and let them decide.

Done in v0.110.0 (`Combat/HeroProgress.cs` static, session-only - no save system yet):
- **No regen** (PlayerHealth lost regenDelay/regenPerSecond).
- **Mend knot** `HeroProgress.Mend` 0..1, +`MendPerHit` 0.25 per landed blow (VeilController.NotifyHitLanded, arrows too). Full -> hold **R** (`Heal` binding, pad RB) standing on the ground: `PlayerHealth.UpdateMending`, `MendTime` 0.9 s, motor `Mending` pins him; a hit/let go/leaving the ground breaks it off, nothing spent. Heals `HealAmount` (1 + 0.5 per Bandage level). Not from the scarf's own length on purpose: it regrows by itself (that would be the autoheal again) - I had told the user "costs the whole scarf", then changed it; say so.
- **Crimson drops** `CrimsonDrop.Spill` on kill: CreatureBase.CrimsonWorth 3 (ShieldMonk 8, harmless 0), wraith `Enemy.CrimsonWorth` 4, crow 1. Drawn to the hero within 2.6 m after 0.35 s.
- **Checkpoint**: every arrival refills life + scarf (`PlayerHealth.Refill`); **F** (`Interact`, pad d-pad up) opens `UI/AltarPanel` (pauses the game) - upgrades Scarf (capacity +0.25/level: costs and drains divided, `VeilCharge.Capacity`), Bandage, Blade (+15 % damage, VeilCombat + VeilArrow); costs 20/45/80, max level 3. `ProgressHud` (under the life gauge): knot bar, crimson count, shards, key hints.
- **Heart shards** `HeartShard`: 4 -> +1 max life (`HeroProgress.ExtraLives`, PlayerHealth re-reads Max each frame, `HealthModel.SetMax`). Placed by `PrototypeSceneBuilder.ShardSpots` at four urns (-10.7,6), (148.3,26.5), (225.3,38), (284.8,-14) and added to the saved scene; LevelKit catalog entry "HeartShard".
- **Death**: `CrimsonCache.Leave` where he fell; dying again loses the old one; falling out of the world (Fell -> Respawn) leaves nothing.
- Settings controls list now 18 rows (row 36 px, cells 34 px) to fit.
- Tests: `HeroProgressTests` (EditMode 6), `ProgressPlayTests` (PlayMode 6).

**v0.111.0 chapter restart (user: "Ano udělej to" to my proposal incl. the cap):** `HeroProgress` remembers a snapshot (crimson, shards, knot, levels) when a chapter scene is loaded fresh (`OnSceneLoaded`, `ChapterOf(scene)`: Prototype_Ruins = 1); `PauseMenu` button "Začít kapitolu znovu" (press twice within 3 s) -> `RestoreChapterStart` + `Restarting` + reload of the scene (foes back). Foes never respawn otherwise, so crimson per chapter is fixed. `ChapterCap` = chapter number caps upgrade levels (`Cost` -1, altar says "Víc až v další kapitole"). `MainMenu.StartGame` resets progress (new game). The level has ~130 crimson; level 1 of all three costs 60.

**Pitfall found doing it:** `LevelKitCatalog.Prefab(e)` rebuilds ALL level-kit prefabs from `LevelKitFactory` when one entry's prefab is missing (a new catalog entry). The factory versions differ from the committed, hand-tuned prefabs (Player's CharacterController 1.8 m instead of 1.9 - the roof test failed, head 4.6 cm in the roof). After adding a catalog entry: keep only the new prefab, `git checkout -- Assets/_CrimsonVeil/LevelKit/Prefabs/` for the rest, and check `git status` for stray prefab changes before committing.

**How to apply:** prices, drop amounts and MendPerHit are first guesses - tune from the user's play. With fewer checkpoints planned, the refill + altar matter more; keep both on checkpoints.
