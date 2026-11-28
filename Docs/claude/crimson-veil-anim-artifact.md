---
name: crimson-veil-anim-artifact
description: "The \"Crimson Veil – animace\" artifact gallery: re-record and republish only when the user asks"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-22T07:40:58.964Z
---

The artifact https://claude.ai/artifact/HL292geRFfkEMM69R2QhfY ("Crimson Veil – animace") is a gallery of clips recorded in Unity. Build it with scratchpad build_page2.py + anim_template.html (folders of JPG frames, base64-embedded, 12 fps playback). On 2026-11-19 the user noticed it was stale: it still said "Stav k verzi 0.26" while main was at v0.32.1 and mocap-mixamo had replaced the hero's motion.

**Why:** the user follows progress through it, often from their phone via Remote Control, and expects it to show the current game.

**How to apply (changed 2026-11-22):** re-record and republish the gallery ONLY when the user asks for it, not after every change (it takes ~4 min). The user said: "Prehrani Galerii jen kdyz si o ni reknu". When asked, say in chat which clips changed and for which version or branch. Related: [[crimson-veil-project]], [[crimson-veil-mocap-branch]].

2026-11-28 (user asked: "aktualizovat artefakty a popisy a obrázky v mainu"): re-recorded for v0.109 / alpha_10 with build_page5.py (20 clips, the "vlasy" clip dropped, bow now a press `cast:Bow`, hero kept invulnerable, CrowSwarm disabled too), published as version 39 (12.6 MB). The same frames made the README GIFs (`make_gifs.py Docs\moves 400 40`, 14.8 MB). README portraits of the bestiary: a temporary PlayMode test instantiating each LevelKit prefab alone (hero far away, hidden floor and ceiling), scripts disabled before the shot, camera framed on renderer bounds without threads/ropes, 720 px -> 360 px JPG. World shots: in Play, teleport the hero, then render Camera.main into a 1280x720 RenderTexture in a separate execute_code call. Never edit a script while the recording runs in Play (a recompile breaks it).

**Since v0.112 the test room is its own scene** (`Assets/_CrimsonVeil/Scenes/TestRoom.unity`, made by `ChapterScenes.Split`): record the gallery there (open TestRoom, Play, run rec_all). Its two wraiths are inactive by default (rec_all activates them). The ruins no longer contain the test room.

Pipeline (2026-11-21): in Play run scratchpad rec_all.cs.txt via execute_code (codedom; Teleport needs (pos, false)) -> writes g_<clip>/f*.png and rec_all_done.txt; then `uv run --offline --with pillow python build_page4.py` (bare python has no Pillow); keep the page under 16 MB (JPEG quality 64); publish with url.
