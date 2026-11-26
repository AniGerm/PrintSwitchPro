using System.Net;
using System.Text;
using PrintSwitchPro.Core.Update;

namespace PrintSwitchPro.Core.Tests;

public class GitHubReleaseClientTests
{
    [Fact]
    public async Task Selects_Windows_Setup_Exe_From_Latest_Stable_Release()
    {
        var json = """
            [
              {
                "tag_name": "v3.0.1",
                "draft": false,
                "prerelease": false,
                "body": "notes",
                "assets": [
                  {
                    "name": "PrintSwitchPro-Setup-3.0.1.exe",
                    "browser_download_url": "https://github.com/AniGerm/PrintSwitchPro/releases/download/v3.0.1/PrintSwitchPro-Setup-3.0.1.exe",
                    "size": 1234
                  }
                ]
              }
            ]
            """;

        using var http = new HttpClient(new StubHandler(json));
        var client = new GitHubReleaseClient(http, "AniGerm", "PrintSwitchPro");
        var release = await client.FetchLatestSetupAsync();

        Assert.NotNull(release);
        Assert.Equal("3.0.1", release!.Version);
        Assert.Equal("PrintSwitchPro-Setup-3.0.1.exe", release.AssetName);
        Assert.StartsWith("https://", release.DownloadUrl);
    }

    [Fact]
    public async Task Skips_Draft_And_Prerelease()
    {
        var json = """
            [
              {
                "tag_name": "v9.9.9",
                "draft": true,
                "prerelease": false,
                "assets": [
                  {
                    "name": "PrintSwitchPro-Setup-9.9.9.exe",
                    "browser_download_url": "https://github.com/AniGerm/PrintSwitchPro/releases/download/v9.9.9/PrintSwitchPro-Setup-9.9.9.exe",
                    "size": 1
                  }
                ]
              },
              {
                "tag_name": "v3.1.0-beta.1",
                "draft": false,
                "prerelease": true,
                "assets": [
                  {
                    "name": "PrintSwitchPro-Setup-3.1.0-beta.1.exe",
                    "browser_download_url": "https://github.com/AniGerm/PrintSwitchPro/releases/download/v3.1.0-beta.1/PrintSwitchPro-Setup-3.1.0-beta.1.exe",
                    "size": 1
                  }
                ]
              }
            ]
            """;

        using var http = new HttpClient(new StubHandler(json));
        var client = new GitHubReleaseClient(http);
        Assert.Null(await client.FetchLatestSetupAsync());
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
