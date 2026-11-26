# Changelog

## 3.0.0 — 2026-11-26

### Neu

- Kompletter Rewrite für **Windows 10/11 x64** (.NET 8 WinForms, self-contained)
- JSON-Einstellungen, DPI-aware, Tray/Autostart
- UI-Sprache **Deutsch / Englisch** umschaltbar
- In-App-Updater nach Inbox-Vorbild: GitHub Releases suchen, Download mit Fortschritt, Setup starten
- CI + Release-Workflow (Windows-Installer `PrintSwitchPro-Setup-*.exe`)
- EditorConfig, `dotnet format`, Pre-Commit-Hooks, Core-Unit-Tests

### Entfernt / gehärtet

- .NET Framework 3.5 / alte `PrinterSwitcher`-UI
- HTTP-Phone-Home zu computedsynergy.com und Offer-Menü
- `BinaryFormatter`-Persistenz und undurchsichtige Legacy-Installer-DLLs
- TreeGridView und ungenutzter Legacy-Code
