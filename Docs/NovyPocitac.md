# Vývoj na dalším počítači

Postup pro Claude (Claude Code v aplikaci Claude) na novém počítači s Windows. Uživatel udělá jen kroky označené **[uživatel]**. Instalace se správcovskými právy vyžadují jeho potvrzení v okně UAC.

## 1. Co udělá uživatel ručně
1. **[uživatel]** Nainstaluje aplikaci Claude (https://claude.ai/download), přihlásí se a otevře záložku **Code**.
2. **[uživatel]** Jako pracovní složku zvolí `C:\Dev` (případně ji předtím vytvoří) a napíše Claudovi:
   > Nainstaluj si všechno pro vývoj Crimson Veil podle Docs/NovyPocitac.md z repozitáře https://github.com/NeoTaxidermist/Crimson-Veil (větev newFeatures).

## 2. Co udělá Claude
Každou instalaci nejdřív ohlas uživateli: co, odkud a kolik zabere. Stahuj jen se souhlasem.

1. **Git:** `winget install --id Git.Git -e`. Pak:
   ```powershell
   git clone -b newFeatures https://github.com/NeoTaxidermist/Crimson-Veil.git C:\Dev\CrimsonVeil
   cd C:\Dev\CrimsonVeil
   git config core.hooksPath .githooks
   git config push.followTags true
   ```
   Pro push na GitHub se uživatel přihlásí sám (Git Credential Manager otevře prohlížeč).
2. **uv a Python:** `winget install --id astral-sh.uv -e`, potom `uv python install 3.12`. MCP for Unity spouští svůj server přes `uvx`.
3. **Unity Hub:** `winget install --id Unity.UnityHub -e`. **[uživatel]** se v Hubu přihlásí k účtu Unity (licence Personal).
4. **Unity 6000.3.28f1 s IL2CPP** (Unity Hub 4 headless režim nemá, postup, který fungoval na DELLu, je v `Docs/claude/dell-setup-pitfalls.md`; tamtéž je Smart App Control, které blokuje Python z uv):
   ```powershell
   & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install --version 6000.3.28f1
   & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.3.28f1 -m windows-il2cpp
   ```
5. **Visual Studio Community s C++ a Windows SDK** (pro IL2CPP):
   `winget install --id Microsoft.VisualStudio.Community -e --override "--add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Component.Windows11SDK.26100 --passive --norestart"`
6. **Blender** (modely se staví skripty v `ArtSource/*_build.py`). Na Lenovu běží **Blender 5.3**, použij stejnou nebo novější verzi: `winget install --id BlenderFoundation.Blender -e`. Napojení na Claude má dvě části, obě je potřeba mít:
   - **Doplněk v Blenderu (MCP add-on od Blender Lab).** **[uživatel]** ho nainstaluje podle https://lab.blender.org/mcp-server/#addon. Instaluje se jako rozšíření (Edit → Preferences → Get Extensions, nebo přetažením staženého .zip do okna Blenderu). Na Lenovu je v `%APPDATA%\Blender Foundation\Blender\5.3\extensions\user_default\mcp`. Po instalaci v něm spusť server, jak popisuje stránka. Blender musí být při práci otevřený.
   - **Rozšíření „Blender“ v aplikaci Claude** (autor Blender Lab, https://www.blender.org/lab/mcp-server/). **[uživatel]** ho přidá v aplikaci Claude: Settings → Extensions → Browse extensions → Blender → Install. Rozšíření se spouští přes `uv` (`uv run blender-mcp`), proto musí být nainstalované `uv` (krok 2). Pak restartuj aplikaci Claude.
   - **Ověření:** v nové session jsou nástroje `mcp__Blender__*`, např. `get_objects_summary` při otevřeném Blenderu. Na Lenovu ho používá i session ve složce `C:\_ClaudeAI\blender`.
   - Blender Bridge v menu MCP for Unity se **nepoužívá** (potřebuje jiný doplněk na portu 9876). Modely se exportují z Blenderu jako FBX přímo do `Assets/_CrimsonVeil/Art`.
7. **První otevření projektu:** v Hubu Add → `C:\Dev\CrimsonVeil`, nebo
   `& "C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe" -projectPath C:\Dev\CrimsonVeil`.
   První import trvá dlouho, desítky minut, protože se staví složka `Library`. **Nespouštěj Unity s `-createProjectPath`**, otevírá jiný projekt.
8. **MCP for Unity** je v `Packages/manifest.json` a nainstaluje se s projektem. V Unity: Window → MCP for Unity → Start Server → Connect. Aby se server spouštěl sám, zapni v jeho okně automatický start. Claude se připojí přes `.mcp.json` v kořeni repozitáře (`http://127.0.0.1:8080/mcp`). Claude spusť ve složce `C:\Dev\CrimsonVeil`, nebo složku s `.mcp.json` přidej.
9. **Ověření:** `read_console` bez chyb, EditMode testy (`run_tests`) projdou. Benchmark: sestav hru do `C:\Dev\CrimsonVeil-build` a spusť `Tools/bench.ps1`.

## 3. Aby se dalo pracovat z telefonu
1. Pojmenuj svou session podle počítače, např. „Crimson Veil – DELL“ (nástroj `set_session_title`).
2. Zapni připojování nových sessions k Remote Control (nastavení `connect_new_sessions_to_remote_control` = true, uživatel to potvrdí).
3. Spusť `Tools/keep-awake.ps1 -Start` a nech ho běžet. **[uživatel]** Pokud má notebook pracovat se zavřeným víkem, nastaví ve Windows „Při zavření víka: Nedělat nic“ (na síti).
4. Do `Docs/Rozpracovano.md` zapiš, že je počítač připravený. Pak commit a push.

## Poznámky
- **Nepracovat na dvou počítačích zároveň.** Před přechodem push, na druhém `git pull`.
- Pokyny pro Claude jsou v `CLAUDE.md` v kořeni repozitáře a načítají se samy.
- Na integrované grafice (Intel UHD) hra běží výrazně pomaleji než na RTX 2060. Je to užitečné pro test slabšího hardwaru.
