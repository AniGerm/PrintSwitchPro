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

Release-Installer von GitHub laden: `PrintSwitchPro-Setup-*.exe`

Oder in PowerShell (nach dem ersten Release):

```powershell
# Beispiel — konkrete Assets stehen unter Releases
Start-Process "https://github.com/AniGerm/PrintSwitchPro/releases/latest"
```

Bei SmartScreen (unsignierter Build): **Weitere Informationen** → **Trotzdem ausführen**.

## Bauen (Windows)

Voraussetzungen: [.NET 8 SDK](https://dotnet.microsoft.com/download), optional [Inno Setup 6](https://jrsoftware.org/isinfo.php)

```powershell
dotnet test tests/PrintSwitchPro.Core.Tests/PrintSwitchPro.Core.Tests.csproj -c Release

dotnet publish src/PrintSwitchPro/PrintSwitchPro.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o artifacts/publish

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
