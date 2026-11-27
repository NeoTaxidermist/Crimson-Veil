---
name: crimson-veil-sound-judging
description: "How I judge Crimson Veil's synthesized sounds without ears (CLAP model + LUFS), what made them sound fake, and the mix targets"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-26T16:35:31.318Z
---

User found the synthesized sounds artificial, overdriven and too loud (2026-11-26). I cannot hear; user approved downloading the CLAP model (laion/clap-htsat-unfused, HF cache) + torch CPU to judge.

- Judge: scratchpad `judge.py <dir>` (uv with torch cpu index, transformers, scipy, pyloudnorm); prompt per sound vs 6 fake prompts (synth, beep, hiss, 8-bit, distorted, drum machine kick). `mixin.py` mixes each exported sound at its in-game gain over the ambience loop; judge THAT (as heard in game). v0.91 mean 0.13 -> v0.92 mean 0.79.
- Findings: falling-sine thumps = "drum machine kick"; digital silence around a sound made every impact read fake (+ -40 dB room noise flipped 0.01 -> 0.5-0.9) -> added Loop.Ambience. Plain noise alone always scores low (model calls it hiss) - ambience-alone score is meaningless.
- Mix by LUFS (SoundSynth.Loudness, GameSound.Level = LUFS target): fight ~-23, swings -30, steps -39/-36, ambience -46. Never normalise-to-peak as the play level.
- Still weak per model at v0.92: Dissolve, Slide loop, Rope creak, Roll. Scores fluctuate ±0.3 by ambience offset.
- Sync (v0.93): user hears desync. Steps fire from the ankle bone reaching its learnt stance height (not skin sole, not Carries at 7 cm); swings fire at GameSound.SwingSoundAt progress (cloth blade lags arm, sword tip fastest 0.21 s); hit cuts the swing; whip sounds at each bottom crossing. Measure with a temp PlayMode probe logging bone heights / tip speed vs sound times.
- Ringing modes (high Q) on steps = "drumsticks"; steps must be damped knocks.
- Export snippet writes wavs to C:\_ClaudeAI\unity\zvuky (in-game level) and zvuky_s_podkresem (over ambience).

**Why:** the user judges by ear and atmosphere; numbers are only my proxy - always say I did not hear them.
**How to apply:** for any sound change, export, mix over ambience, run judge, compare to the previous run; report honestly. See [[crimson-veil-test-scope]].
