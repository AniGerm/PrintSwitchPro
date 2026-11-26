# Sicherheits- und Windows-11-Prüfung

Prüfbericht für PrintSwitchPro (vormals Automatic Printer Switcher / APS).

## Kurzfazit

**Kein Virus / keine Malware** in der geprüften Legacy-Codebasis (.NET Framework 3.5, Apache-2.0). Die App beobachtete den Vordergrundprozess und wechselte den Standarddrucker über `SetDefaultPrinter` (`winspool.drv`).

Auf **Windows 11** war die Legacy-Version grundsätzlich lauffähig, sofern die optionale Windows-Funktion **.NET Framework 3.5** aktiv war.

Ab **PrintSwitchPro 3.0** (dieser Stand) entfallen die Legacy-Risiken: self-contained .NET 8, nur Windows 10/11 x64, JSON statt `BinaryFormatter`, Updates nur über GitHub Releases (HTTPS).

---

## Was geprüft wurde

### Legacy (vor 3.0)

- Einstieg: `PrinterSwitcher/Program.cs` → aktive UI `frmV2.cs`
- Persistenz: `PSDB.cs` (`%AppData%\APS`)
- Installer: `Setup/setup.iss` + Abhängigkeits-Skripte
- Suche nach Netzwerk, Prozessstart, Registry, Obfuscation, Persistence, Keylogging, Downloads
- Binaries ohne vollständige Quellkontrolle: `Setup/src/PrinterSwitcher.exe`, `Setup/scripts/isxdl/isxdl.dll`

### Aktuell (3.0)

- `src/PrintSwitchPro` (WinForms), `src/PrintSwitchPro.Core` (Logik/Updater)
- Persistenz: `%AppData%\PrintSwitchPro\settings.json`
- Installer: `installer/PrintSwitchPro.iss`, CI-Publish self-contained `win-x64`

---

## Malware / Viren

| Ergebnis | Details |
|----------|---------|
| Urteil | **Kein Malware-Muster** |
| Kernfunktion | Timer + `GetForegroundWindow` / `GetWindowThreadProcessId` + `SetDefaultPrinter` |
| Nicht gefunden | Keylogger, Clipboard-Hooks, Screenshots, Miner, `Assembly.Load`, Base64-Payloads, `cmd`/`powershell`/`schtasks`, Writes nach System32/Startup |

### Auffälligkeiten der Legacy-Version (kein Virus, aber Sicherheitsrisiko)

1. **HTTP-Update-Check** in `frmV2.cs` zu `http://www.computedsynergy.com/aps/meta/version.txt` (Klartext, MITM möglich; kein Auto-Download der App selbst).
2. **Autostart** optional über `HKCU\...\Run` (nur wenn der Nutzer es aktiviert).
3. **`BinaryFormatter`** in `PSDB.cs` für `processPrinterMap.dat` — bekanntes Deserialisierungsrisiko, falls die Datei manipuliert wird.
4. **Tote Legacy-UI** `frmMain.cs` (nicht im `.csproj`): enthielt zusätzlich `offer.txt`-Download; wurde nicht gebaut.
5. **Installer-Binaries** ohne Bit-genaue Neuverifikation; `isxdl.dll` ohne Quellcode im Repo.

### Status nach 3.0

| Alt | Neu |
|-----|-----|
| HTTP-Update zu computedsynergy.com | GitHub API HTTPS (Inbox-Flow) |
| `offer.txt` / Special-Offer-Menü | entfernt |
| `BinaryFormatter`-Persistenz | `settings.json` |
| mitgelieferte Setup-DLLs (`isxdl`) | Inno Setup + self-contained Publish in CI |
| `Process.Kill()` nach Exit | normales Beenden |
| Alte APS-`.dat`-Dateien | werden bewusst **nicht** importiert |

Autostart bleibt optional über `HKCU\...\Run` (bewusst, nutzergesteuert).

---

## Windows 11

### Legacy (.NET 3.5)

- Target: **.NET Framework 3.5**, 32-bit (WoW64)
- Installer `MinVersion=5.0` blockierte Win11 nicht
- **Voraussetzung:** .NET 3.5 als Windows-Feature aktivieren (bzw. alter Installer-Bootstrap über HTTP-Microsoft-URLs)
- Kein DPI-Manifest → UI konnte auf High-DPI unscharf wirken
- Laufzeit `asInvoker`; Installer verlangte trotzdem Admin

### Aktuell (3.0)

- Self-contained **.NET 8**, x64, DPI `PerMonitorV2`
- **Kein** optionales .NET-3.5-Feature nötig
- Installer: Windows 10+, `PrivilegesRequired=lowest` (User-Install möglich)
- Laufzeit weiterhin `asInvoker`

---

## Code-Qualität (Legacy)

Nicht „modern sauber“, aber für das Alter erwartbar:

- Viel toter Code (`frmMain`, ungenutzte Forms, `TreeGridView` ohne Referenz)
- Viele leere `catch`-Blöcke; UI-Updates teils vom Hintergrundthread ohne `Invoke`
- `Process.Kill()` nach `Application.Exit()` beim Beenden
- Versionschaos (`2.1.1` vs `1.0` in AssemblyInfo/`version.txt`)
- Kein SQL/DB → keine SQL-Injection-Fläche

In 3.0 wurden toter Code entfernt, Persistenz/Updater neu gebaut und Core-Unit-Tests ergänzt.

---

## Empfehlungen (umgesetzt in 3.0)

1. HTTPS / GitHub Releases statt Klartext-HTTP
2. `BinaryFormatter` entfernen → JSON
3. DPI-Manifest / `PerMonitorV2`
4. Toten Code und Offer-Pfade entfernen
5. Installer ohne undurchsichtige Drittanbieter-DLLs neu bauen
