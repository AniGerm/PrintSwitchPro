# PrintSwitchPro

Automatischer Standarddrucker-Wechsel unter **Windows 10/11**, je nach Vordergrundprogramm.

> Nur Windows. Kein Linux-/macOS-Build.

## Funktionen

- Prozess → Drucker-Zuordnung
- Infobereich (Tray), optional Autostart
- Einstellungen auf Deutsch oder Englisch umschaltbar
- **In-App-Updates** wie bei [Inbox](https://github.com/AniGerm/Inbox): GitHub Releases durchsuchen → Download mit Fortschritt → Installer starten
- Self-contained .NET 8 (kein separates Runtime-Setup)

## Installation

Releases: https://github.com/AniGerm/PrintSwitchPro/releases

### Empfohlen: Portable (ohne Installer)

Bei **Smart App Control** / **Fehler 4551** den Inno-Installer meiden und die ZIP nutzen:

1. `PrintSwitchPro-Portable-*-win-x64.zip` laden  
2. Entpacken  
3. Optional in PowerShell: `Unblock-File .\PrintSwitchPro.exe`  
4. `PrintSwitchPro.exe` starten  

Oder:

```powershell
irm https://raw.githubusercontent.com/AniGerm/PrintSwitchPro/master/scripts/run-portable.ps1 | iex
```

### Optional: Setup.exe

`PrintSwitchPro-Setup-*.exe` — kann unter Windows 11 mit Smart App Control **Fehler 4551** auslösen (unsignierte Temp-Datei). Dann Portable nutzen oder Smart App Control vorübergehend ausschalten (Windows-Sicherheit → App- & Browsersteuerung).

SmartScreen: **Weitere Informationen** → **Trotzdem ausführen**.

## Bauen (Windows)

Voraussetzungen: [.NET 8 SDK](https://dotnet.microsoft.com/download), optional [Inno Setup 6](https://jrsoftware.org/isinfo.php)

```powershell
dotnet test tests/PrintSwitchPro.Core.Tests/PrintSwitchPro.Core.Tests.csproj -c Release

dotnet publish src/PrintSwitchPro/PrintSwitchPro.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false -o artifacts/publish

Compress-Archive -Path artifacts\publish\* -DestinationPath artifacts\PrintSwitchPro-Portable-win-x64.zip
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer\PrintSwitchPro.iss
```

## Entwicklung / Qualität

```bash
# Core-Tests laufen auch unter Linux (CI)
dotnet test tests/PrintSwitchPro.Core.Tests/PrintSwitchPro.Core.Tests.csproj

# Pre-Commit (optional)
pip install pre-commit
pre-commit install
pre-commit run --all-files
```

## Daten

Einstellungen: `%AppData%\PrintSwitchPro\settings.json`
Updates: `%AppData%\PrintSwitchPro\updates\`

Alte APS-`BinaryFormatter`-Dateien werden aus Sicherheitsgründen **nicht** importiert.

## Sicherheit & Windows 11

Vollständiger Prüfbericht: [docs/SICHERHEITSPRUEFUNG.md](docs/SICHERHEITSPRUEFUNG.md)

- **Malware:** In der Legacy- und der aktuellen Codebasis kein Virus-/Trojaner-Muster gefunden.
- **Windows 11 (3.0):** Self-contained .NET 8 x64 — **kein** optionales .NET-Framework-3.5-Feature nötig.
- **Legacy (.NET 3.5):** Auf Win11 nur mit aktiviertem .NET 3.5 lauffähig gewesen; diese Variante ist entfernt.

## Lizenz

Apache-2.0 (siehe [LICENSE](LICENSE)).
