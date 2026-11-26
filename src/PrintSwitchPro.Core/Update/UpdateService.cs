using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Services;

namespace PrintSwitchPro.Core.Update;

/// <summary>
/// Inbox-style update flow for Windows: check → download with progress → install setup .exe.
/// </summary>
public sealed class UpdateService
{
    public static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(4);

    private readonly GitHubReleaseClient _client;
    private readonly Func<string> _getCurrentVersion;
    private readonly string _downloadDirectory;
    private readonly Func<HttpClient> _httpFactory;

    private ReleaseInfo? _pending;
    private string? _downloadedPath;
    private int _installInFlight;

    public UpdateService(
        GitHubReleaseClient client,
        Func<string> getCurrentVersion,
        string downloadDirectory,
        Func<HttpClient>? httpFactory = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _getCurrentVersion = getCurrentVersion ?? throw new ArgumentNullException(nameof(getCurrentVersion));
        _downloadDirectory = downloadDirectory ?? throw new ArgumentNullException(nameof(downloadDirectory));
        _httpFactory = httpFactory ?? (() => new HttpClient { Timeout = TimeSpan.FromMinutes(30) });
        Directory.CreateDirectory(_downloadDirectory);
    }

    public ReleaseInfo? PendingRelease => _pending;

    public string? DownloadedInstallerPath => _downloadedPath;

    public event Action<UpdateStatus>? StatusChanged;

    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        Emit(new UpdateStatus.Checking());
        try
        {
            var latest = await _client.FetchLatestSetupAsync(cancellationToken).ConfigureAwait(false);
            var current = _getCurrentVersion();
            if (latest is null)
            {
                _pending = null;
                _downloadedPath = null;
                Emit(new UpdateStatus.UpdateNotAvailable(current));
                return;
            }

            if (VersionComparer.IsNewer(latest.Version, current))
            {
                _pending = latest;
                _downloadedPath = null;
                Emit(new UpdateStatus.UpdateAvailable(latest.Version));
            }
            else
            {
                _pending = null;
                _downloadedPath = null;
                Emit(new UpdateStatus.UpdateNotAvailable(current));
            }
        }
        catch (Exception ex)
        {
            Emit(new UpdateStatus.Error(ex.Message));
        }
    }

    public async Task<bool> DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (_pending is null)
        {
            Emit(new UpdateStatus.Error("Kein Update zum Herunterladen."));
            return false;
        }

        try
        {
            Emit(new UpdateStatus.DownloadProgress(0));
            var dest = Path.Combine(_downloadDirectory, _pending.AssetName);
            if (File.Exists(dest))
            {
                File.Delete(dest);
            }

            using var http = _httpFactory();
            using var response = await http.GetAsync(
                    _pending.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? _pending.Size;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var output = File.Create(dest);

            var buffer = new byte[81920];
            long received = 0;
            int read;
            var lastPercent = -1;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                received += read;
                if (total > 0)
                {
                    var percent = (int)Math.Max(0, Math.Min(100, Math.Round(received * 100.0 / total)));
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        Emit(new UpdateStatus.DownloadProgress(percent));
                    }
                }
            }

            if (total > 0 && received != total)
            {
                try
                {
                    File.Delete(dest);
                }
                catch
                {
                    // ignore cleanup failure
                }

                Emit(new UpdateStatus.Error("Download unvollständig (Größenprüfung fehlgeschlagen)."));
                return false;
            }

            _downloadedPath = dest;
            Emit(new UpdateStatus.UpdateDownloaded(_pending.Version));
            return true;
        }
        catch (Exception ex)
        {
            Emit(new UpdateStatus.Error(ex.Message));
            return false;
        }
    }

    public bool TryBeginInstall(out string installerPath, out string version)
    {
        installerPath = string.Empty;
        version = _pending?.Version ?? string.Empty;

        if (Interlocked.Exchange(ref _installInFlight, 1) == 1)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_downloadedPath) || !File.Exists(_downloadedPath))
        {
            Interlocked.Exchange(ref _installInFlight, 0);
            Emit(new UpdateStatus.Error("Update noch nicht heruntergeladen."));
            return false;
        }

        // Stay inside the updates directory (Inbox-style path validation).
        var full = Path.GetFullPath(_downloadedPath);
        var root = Path.GetFullPath(_downloadDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || !full.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Exchange(ref _installInFlight, 0);
            Emit(new UpdateStatus.Error("Installer-Pfad ungültig."));
            return false;
        }

        installerPath = full;
        Emit(new UpdateStatus.Installing(version));
        return true;
    }

    public void ResetInstallLock()
        => Interlocked.Exchange(ref _installInFlight, 0);

    private void Emit(UpdateStatus status)
        => StatusChanged?.Invoke(status);
}
