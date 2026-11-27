---
name: crimson-veil-additive-style-plan
description: Hero animation layering - additive style layer (v0.71.0) and firm footfalls (v0.77.0) are DONE; what the user ruled on gait tempo and "own character"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-22T18:38:50.948Z
---

Plan proposed 2026-11-19 (user: "zapamatuj si to, zítra se na to nejspíš koukneme"); carried out 2026-11-22:
1. Procedural pose as an additive style layer over the mocap clip - done in v0.71.0 (`HeroMocap.StyleOver`).
2. Foot planting - done in v0.77.0 (`HeroAnimator.PlantFeet`, ball of the foot locked, leg IK, early lift at full stretch) together with slightly quicker steps (`HeroMocap.WalkTempo` 1.2, `RunTempo` 1.25: ~1.7 loops of steps a second at 8 m/s).
3. Still open from the plan: tuning how much style each body part gets.
4. v0.78.0 (user asked 2026-11-22 whether body and head should follow the attack direction): `HeroAnimator.LookAlongTheBlow` (neck + head onto the aim, 44 up / 35 down off the chest) and `LeanIntoTheBlow` (trunk back up to 12 degrees on blows upwards, arms' world rotation restored so the weapon doesn't move). Level blows untouched. The user has not yet commented on the amounts.

**Pitfalls for any further bone layer after the pose/mocap:** the `UpperChest` bone is not written by the pose each frame, so a rotation added to it accumulates (it dragged the weapon arm) - only turn bones the pose sets every frame (Spine, Chest, Neck, Head, arms). Eye bones are animated by `HeroFace`: measure where he looks from the Head bone. A/B clips of attacks need an idle clip between them (combo state carries over) and must be compared at the same attack progress, not in a fixed frame window.

**Rulings to keep:**
- Rejected by the user (2026-11-19): playing the clips at 100 % - the motion would be identical to the asset and the hero would lose his own character.
- Gait tempo: an earlier bug had the legs pattering at 4 loops a second; it was fixed to the clip's own tempo (1.4). On 2026-11-22 I raised it to ~1.7 on my own judgement, because at 1.4 a footfall at 8 m/s covers 1.2 m of ground (more than a leg reaches, so the foot must slide). `WalkThenRun_TheStepsKeepTheirTempo` allows 1.1-1.9. The user has not yet commented on the new tempo: if they find the legs too quick, lower `RunTempo` and accept some slide.

**How to apply:** measure foot slide before touching the gait (per-frame dump of the toe bones' world x while `HeroAnimator.FootDown(side)`; ClipRecorder at 24 fps). Related: [[crimson-veil-mocap-branch]], [[crimson-veil-moveset-tests]], [[crimson-veil-anim-artifact]].
