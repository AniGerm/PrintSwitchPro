namespace PrintSwitchPro.Core.Models;

public sealed class ReleaseInfo
{
    public required string Version { get; init; }

    public required string AssetName { get; init; }

    public required string DownloadUrl { get; init; }

    public long Size { get; init; }

    public string? ReleaseNotes { get; init; }
}
