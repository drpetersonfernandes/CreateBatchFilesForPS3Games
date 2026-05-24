using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CreateBatchFilesForPS3Games;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public UpdateService(HttpClient httpClient, string repoOwner, string repoName)
    {
        _httpClient = httpClient;
        _apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";
    }

    internal UpdateService(HttpClient httpClient, string apiUrl)
    {
        _httpClient = httpClient;
        _apiUrl = apiUrl;
    }

    public async Task<(bool UpdateAvailable, string? LatestVersion, string? ReleaseUrl)> CheckForUpdateAsync(Version currentVersion)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            request.Headers.UserAgent.Add(
                new ProductInfoHeaderValue("PS3BatchLauncherCreator", currentVersion.ToString()));

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagNameElement) ||
                !root.TryGetProperty("html_url", out var htmlUrlElement))
                return (false, null, null);

            var tagName = tagNameElement.GetString();
            var htmlUrl = htmlUrlElement.GetString();

            var latestVersion = ParseVersion(tagName);
            if (latestVersion == null)
                return (false, null, null);

            var updateAvailable = latestVersion > currentVersion;
            return (updateAvailable, latestVersion.ToString(), htmlUrl);
        }
        catch
        {
            return (false, null, null);
        }
    }

    internal static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        tag = tag.Trim();

        if (tag.StartsWith('v') || tag.StartsWith('V'))
        {
            tag = tag[1..];
        }

        return Version.TryParse(tag, out var version) ? version : null;
    }
}
