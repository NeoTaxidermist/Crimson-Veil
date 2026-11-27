---
name: dell-setup-pitfalls
description: Pitfalls hit while setting up the DELL (Latitude 5421) per Docs/NovyPocitac.md - Hub 4 has no headless CLI, Smart App Control blocks uv Python
metadata:
  type: project
  modified: 2026-11-27
---

Found on 2026-11-27 while setting up the DELL. Check these first on any new computer.

- **Unity Hub 4.x (winget `Unity.UnityHub` is now an MSIX package)** has no `--headless install`: it answers "'...' is not supported by the native Unity Hub, use the standalone Unity CLI". The bundled `app\unity.exe` inside `C:\Program Files\WindowsApps\...` can't be run from outside (access denied). What worked:
  - Editor: `Start-Process "unityhub://6000.3.28f1/8f082d2fe476"` (revision from `ProjectSettings/ProjectVersion.txt`) opens the Hub install dialog; the user clicks install.
  - IL2CPP module: download the signed installer from Unity and run it silently into the editor folder:
    `https://download.unity3d.com/download_unity/8f082d2fe476/TargetSupportInstaller/UnitySetup-Windows-IL2CPP-Support-for-Editor-6000.3.28f1.exe` (241 MB), check Authenticode = Unity Technologies, then `/S /D=C:\Program Files\Unity\Hub\Editor\6000.3.28f1` with RunAs. Verify `Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\*il2cpp*`.
  - winget may list Unity Hub as installed when the Hub is running; reinstall fails with 0x80073D02 (app must be closed).
- **Smart App Control** (Windows 11, `HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy` VerifiedAndReputablePolicyState = 1) blocks the unsigned Python from `uv python install` ("Zásada řízení aplikací tento soubor zablokovala", CodeIntegrity events 3033/3077). MCP for Unity's server needs it. Turning SAC off is one-way (only a Windows reinstall turns it back on), so it is the user's decision and the user does it in Windows Security.
- uv + Python must still be installed by the user in their own PowerShell, see [[claude-app-msix-virtualization]]. Git is not on PATH in Claude's shell after install until PATH is reloaded from the registry.
- Visual Studio Community from winget is now **2026 (18.x)**; the override with NativeDesktop + Windows11SDK.26100 works and asks for a restart.
