namespace PrintSwitchPro.Core.Models;

public sealed class AppSettings
{
    public const string DefaultLanguage = "de";

    /// <summary>Fallback printer when the foreground process has no mapping.</summary>
    public string DefaultPrinter { get; set; } = string.Empty;

    public List<ProcessMapping> Mappings { get; set; } = [];

    /// <summary>Language code: "de" or "en".</summary>
    public string Language { get; set; } = DefaultLanguage;

    public bool AutoStart { get; set; }

    public bool MinimizeOnStartup { get; set; }

    /// <summary>Periodically check GitHub Releases (Inbox-style).</summary>
    public bool AutoCheckUpdates { get; set; } = true;

    public int PollIntervalMs { get; set; } = 2000;
}
