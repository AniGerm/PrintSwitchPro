namespace PrintSwitchPro.Core.Localization;

public sealed class UiStrings
{
    private readonly string _language;

    public UiStrings(string language)
    {
        _language = language is "en" ? "en" : "de";
    }

    public string Language => _language;

    public bool IsGerman => _language == "de";

    public string AppName => "PrintSwitchPro";

    public string MainTitle => IsGerman
        ? "PrintSwitchPro – Automatischer Druckerschalter"
        : "PrintSwitchPro – Automatic Printer Switcher";

    public string Processes => IsGerman ? "Prozesse" : "Processes";

    public string Printer => IsGerman ? "Drucker" : "Printer";

    public string Refresh => IsGerman ? "Aktualisieren" : "Refresh";

    public string SetDefaultPrinter => IsGerman ? "Standarddrucker" : "Default printer";

    public string EditMapping => IsGerman ? "Zuordnung bearbeiten" : "Edit mapping";

    public string RemoveMapping => IsGerman ? "Zuordnung entfernen" : "Remove mapping";

    public string Settings => IsGerman ? "Einstellungen" : "Settings";

    public string LanguageLabel => IsGerman ? "Sprache" : "Language";

    public string German => "Deutsch";

    public string English => "English";

    public string Autostart => IsGerman ? "Mit Windows starten" : "Start with Windows";

    public string MinimizeOnStartup => IsGerman
        ? "Beim Start in den Infobereich minimieren"
        : "Minimize to tray on startup";

    public string AutoCheckUpdates => IsGerman
        ? "Automatisch nach Updates suchen"
        : "Automatically check for updates";

    public string CheckForUpdates => IsGerman ? "Nach Updates suchen" : "Check for updates";

    public string Download => IsGerman ? "Herunterladen" : "Download";

    public string InstallNow => IsGerman ? "Jetzt installieren" : "Install now";

    public string SearchingUpdates => IsGerman ? "Suche nach Updates…" : "Checking for updates…";

    public string UpToDate => IsGerman ? "Du hast die neueste Version." : "You are up to date.";

    public string UpdateAvailable(string version) => IsGerman
        ? $"Version {version} verfügbar"
        : $"Version {version} available";

    public string DownloadProgress(int percent) => IsGerman
        ? $"Download… {percent} %"
        : $"Downloading… {percent} %";

    public string UpdateReady(string version) => IsGerman
        ? $"Version {version} bereit zur Installation"
        : $"Version {version} ready to install";

    public string Installing => IsGerman
        ? "App wird beendet — Installation startet…"
        : "App is closing — installation starting…";

    public string Save => IsGerman ? "Speichern" : "Save";

    public string Cancel => IsGerman ? "Abbrechen" : "Cancel";

    public string Ok => IsGerman ? "OK" : "OK";

    public string Quit => IsGerman ? "Beenden" : "Quit";

    public string Restore => IsGerman ? "Fenster wiederherstellen" : "Restore window";

    public string About => IsGerman ? "Über" : "About";

    public string ChoosePrinter => IsGerman ? "Drucker wählen" : "Choose printer";

    public string PleaseChoosePrinter => IsGerman
        ? "Bitte einen Drucker auswählen."
        : "Please choose a printer.";

    public string MapPrinterFor(string process) => IsGerman
        ? $"Drucker für [{process}] zuordnen"
        : $"Map printer for [{process}]";

    public string ConfirmRemoveMapping(string process) => IsGerman
        ? $"Zuordnung für Prozess „{process}“ wirklich entfernen?"
        : $"Remove mapping for process \"{process}\"?";

    public string RemoveMappingTitle => IsGerman ? "Zuordnung entfernen" : "Remove mapping";

    public string StatusDefaultChanged(string printer) => IsGerman
        ? $"Standarddrucker geändert: {printer}"
        : $"Default printer changed: {printer}";

    public string StatusSwitched(string printer) => IsGerman
        ? $"Aktiver Drucker: {printer}"
        : $"Active printer: {printer}";

    public string SaveFailed => IsGerman
        ? "Einstellungen konnten nicht gespeichert werden."
        : "Could not save settings.";

    public string LegacyDataHint => IsGerman
        ? "Alte APS-Zuordnungen (BinaryFormatter) werden aus Sicherheitsgründen nicht übernommen. Bitte neu zuordnen."
        : "Legacy APS mappings (BinaryFormatter) are not imported for security reasons. Please remap.";

    public string CurrentVersion(string version) => IsGerman
        ? $"Aktuelle Version: {version}"
        : $"Current version: {version}";

    public string UpdatesHint => IsGerman
        ? "Auto-Update über GitHub Releases (Windows-Installer)."
        : "Auto-update via GitHub Releases (Windows installer).";

    public string InstallConfirmTitle => IsGerman ? "Update installieren" : "Install update";

    public string InstallConfirmMessage => IsGerman
        ? "PrintSwitchPro wird jetzt beendet. Der Installer startet anschließend."
        : "PrintSwitchPro will close now. The installer will start next.";

    public string Continue => IsGerman ? "Weiter" : "Continue";

    public string CloseToTray => IsGerman
        ? "Fenster schließen minimiert in den Infobereich."
        : "Closing the window minimizes to the tray.";

    public string NoPrintersFound => IsGerman
        ? "Keine Drucker gefunden."
        : "No printers found.";

    public string OpenGitHub => IsGerman ? "GitHub öffnen" : "Open GitHub";
}
