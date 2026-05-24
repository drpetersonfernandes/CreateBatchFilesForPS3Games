using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CreateBatchFilesForPS3Games.Services;

public class StatsService : IStatsService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationId;
    private readonly string _version;

    public StatsService(string apiUrl, string apiKey, string applicationId, string version)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationId = applicationId;
        _version = version;
    }

    public async Task SendStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                applicationId = _applicationId,
                version = _version
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            await HttpClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            // Silently fail
        }
    }
}
