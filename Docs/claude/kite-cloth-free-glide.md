---
name: kite-cloth-free-glide
description: "Gliding sometimes costs no scarf: the PaperKite's cloth gives 8 s of free glide; by design, the user ruled to leave it as is"
metadata:
  type: project
---

Gliding drains the scarf (`VeilCasting.GlideDrain` 0.3/s) except while `MotorLogic.GlideTime` > 0. That time comes only from the cloth a shot-down PaperKite drops (`KiteCloth`, `PaperKite.glideSeconds` 8 s, banked until used while gliding). Nothing in the game shows it.

**Why:** 2026-11-28 the user felt "plachtění na šále někdy nespotřebovává energii"; I traced it to the kite cloth (the only no-drain path) and offered: show it in the game, remove it, or leave it. The user chose **leave it as is** ("3").

**How to apply:** if gliding seems free again, check whether a kite cloth was taken before looking for a bug. Do not remove the free glide or add an indicator unless the user asks.
