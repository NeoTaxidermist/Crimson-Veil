---
name: combat-feel
description: "v0.109 combat feel rules (user: smoother, predictable, better hit/miss/block reactions): startup, follow window, pin, sweep, recoil+reel, whiff, deflect, telegraph"
metadata:
  type: project
---

2026-11-28 the user asked: "boj plynulejší, zatím málo předvídatelný, lepší reakce na zásah, na minutí, případně vykrytí". Rulings: **pin the hero briefly during a blow** ("na chvilku přibít"), **a miss is shown, not punished** ("odezva bez trestu"), do all packages A-E. Done in v0.109.0:

- **A, input:** `VeilController.CastStartup` 0.05 s from every press to the blow (no longer when the weapon is half hard). `FollowWindow` 0.15 -> 0.3 s; an earlier press is kept as `heldEarly` and follows only if its button is still held when the window opens (a tap early in a blow is still let go - the bow "loosed late / twice" complaint). `FreeIn()` counts the startup while a press is pending. **Pin:** `MotorLogic.AttackPinned` (set from `VeilController.PinnedNow` each frame): facing locked, and on the ground the keys don't walk him, from the press to the end of the active window (`PinUntil`; bow 0.4); jump and dash still work; not on the whip rope. **Sweep:** `VeilCombat` also tests capsules from each joint's last-frame position to now (fast tips at low fps).
- **B, hit:** `CreatureBase` implements `IStaggerable`; every accepted hit makes it reel `HurtReel` 0.3 s (TouchDamage off while `Reeling`) and kicks a damped spring `Recoil` (up to `RecoilReach` 0.28 m, lean up to 14 deg) added on top of whatever the creature put on its "Visual" child that frame. `OnReel(staggered)` lets a creature drop its wind-up (ShieldMonk -> Recover).
- **C, miss:** when the active window ends with no hit and no block -> `CombatFx.Whiff` (pale ink streak along the tip's last motion + 6 drops). `VeilCombat.Missed` counts.
- **D, block:** `Deflection.Report(blocker, info)` from a shield that stops a hit (ShieldMonk front). `VeilCombat.OnBlocked` (source == hero): `CombatFx.Deflect` (hit-stop 0.09, shake, spark burst, `Cue.Block`), knockback 4.5 m/s back, blow interrupted. The whip tearing the shield: hit-stop 0.08 + WhipCrack. Hero's block/parry now staggers every creature (`StaggerReel` 0.7 s), not only the wraith.
- **E, telegraph:** `CombatFx.Telegraph(at)` - the same crimson glint at the start of every wind-up: wraith, monk bash and flail, kite rear, crow swarm gather, moth dust, and the RootCrawler, which now quivers `DropWarning` 0.45 s before it drops (it used to drop at once).
- No new sound assets: reused Block / WhipCrack. Tests: `CombatFeelPlayTests` (8).

**How to apply:** keep these consistent for new enemies (call Telegraph at wind-up start, override OnReel, report blocks via Deflection). Not tuned in play yet by the user - expect numbers (startup, window, reel, recoil) to be adjusted from their feedback.
