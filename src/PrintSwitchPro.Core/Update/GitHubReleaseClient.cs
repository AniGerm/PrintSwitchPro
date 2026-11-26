using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Services;

namespace PrintSwitchPro.Core.Update;

/// <summary>
/// Windows-only GitHub Releases client (Inbox-style: search latest setup .exe).
/// </summary>
public sealed class GitHubReleaseClient
{
    public const string DefaultOwner = "AniGerm";
    public const string DefaultRepo = "PrintSwitchPro";
    public const string UserAgent = "PrintSwitchPro-Updater";

    private static readonly Regex SetupAssetRegex = new(
        @"^PrintSwitchPro-Setup-.*\.exe$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubReleaseClient(HttpClient http, string? owner = null, string? repo = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _owner = string.IsNullOrWhiteSpace(owner) ? DefaultOwner : owner.Trim();
        _repo = string.IsNullOrWhiteSpace(repo) ? DefaultRepo : repo.Trim();

        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PrintSwitchPro", "3.0"));
        }

        if (!_http.DefaultRequestHeaders.Accept.Any())
        {
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }
    }

    public async Task<ReleaseInfo?> FetchLatestSetupAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases?per_page=30";
        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (var release in doc.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean())
            {
                continue;
            }

            if (release.TryGetProperty("prerelease", out var pre) && pre.GetBoolean())
            {
                continue;
            }

            if (!release.TryGetProperty("tag_name", out var tagProp))
            {
                continue;
            }

            var version = VersionComparer.Normalize(tagProp.GetString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var asset = FindWindowsSetupAsset(assets);
            if (asset is null)
            {
                continue;
            }

            var notes = release.TryGetProperty("body", out var body)
                ? body.GetString()
                : null;

            return new ReleaseInfo
            {
                Version = version,
                AssetName = asset.Value.Name,
                DownloadUrl = asset.Value.Url,
                Size = asset.Value.Size,
                ReleaseNotes = notes,
            };
        }

        return null;
    }

    private static (string Name, string Url, long Size)? FindWindowsSetupAsset(JsonElement assets)
    {
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            if (!SetupAssetRegex.IsMatch(name))
            {
                continue;
            }

            if (name.Contains('/') || name.Contains('\\') || name.Contains('\0'))
            {
                continue;
            }

            var url = asset.TryGetProperty("browser_download_url", out var u)
                ? u.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var sz) ? sz : 0;
            return (name, url, size);
        }

        return null;
    }
}
