namespace PrintSwitchPro.Core.Models;

/// <summary>Mirrors Inbox UpdateStatusEvent for in-app update UX.</summary>
public abstract record UpdateStatus
{
    public sealed record Checking : UpdateStatus;

    public sealed record UpdateAvailable(string Version) : UpdateStatus;

    public sealed record UpdateNotAvailable(string? Version = null) : UpdateStatus;

    public sealed record DownloadProgress(int Percent) : UpdateStatus;

    public sealed record UpdateDownloaded(string Version) : UpdateStatus;

    public sealed record Installing(string? Version = null) : UpdateStatus;

    public sealed record Error(string Message) : UpdateStatus;
}
