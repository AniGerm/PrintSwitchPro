using PrintSwitchPro.Core.Models;

namespace PrintSwitchPro.Core.Services;

/// <summary>
/// Decides which printer should be active for the current foreground process.
/// Platform printer calls stay in the Windows UI layer.
/// </summary>
public sealed class PrinterSwitchService
{
    private string _lastForegroundProcess = string.Empty;
    private string? _lastAppliedPrinter;

    public string? LastAppliedPrinter => _lastAppliedPrinter;

    public string LastForegroundProcess => _lastForegroundProcess;

    /// <summary>
    /// Returns the printer that should be set, or null if no change is needed.
    /// </summary>
    public string? ResolvePrinterToApply(string processName, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        processName = (processName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(processName))
        {
            return null;
        }

        if (string.Equals(_lastForegroundProcess, processName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _lastForegroundProcess = processName;

        var mapping = settings.Mappings.FirstOrDefault(m =>
            string.Equals(m.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        var target = mapping is not null && !string.IsNullOrWhiteSpace(mapping.MappedPrinter)
            ? mapping.MappedPrinter
            : settings.DefaultPrinter;

        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        if (string.Equals(_lastAppliedPrinter, target, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _lastAppliedPrinter = target;
        return target;
    }

    public void Reset()
    {
        _lastForegroundProcess = string.Empty;
        _lastAppliedPrinter = null;
    }
}
