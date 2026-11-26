# Sicherheits- und Windows-11-Prüfung (PrintSwitchPro 3.0)

## Kurzfazit

Die Legacy-Codebasis (.NET 3.5) enthielt **keine Malware**, aber veraltete Risiken (HTTP-Phone-Home, `BinaryFormatter`, tote Offer-Logik). Ab **3.0** ist die App neu aufgebaut:

- **Nur Windows 10/11 x64**
- JSON-Einstellungen statt `BinaryFormatter`
- Kein Kontakt zu `computedsynergy.com`
- Updates nur über **GitHub Releases** (HTTPS), Inbox-Flow: Suchen → Download → Installieren
- Keine Admin-Rechte zur Laufzeit (`asInvoker`)
- DPI-aware (`PerMonitorV2`)

## Entfernte Risiken

| Alt | Neu |
|-----|-----|
| HTTP-Update zu computedsynergy.com | GitHub API HTTPS |
| `offer.txt` / Special-Offer-Menü | entfernt |
| `BinaryFormatter` Persistenz | `settings.json` |
| mitgelieferte undurchsichtige Setup-DLLs (`isxdl`) | Inno Setup + self-contained Publish in CI |
| leere Catches / Process.Kill | sauberes Beenden |

## Windows 11

Voraussetzung: Windows 10/11 x64. Self-contained .NET 8 — **kein** optionales .NET-3.5-Feature nötig.
