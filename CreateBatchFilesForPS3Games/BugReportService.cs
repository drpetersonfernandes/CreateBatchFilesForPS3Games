using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForPS3Games;

/// <summary>
/// Service responsible for silently sending bug reports to the BugReport API.
/// This class is designed to be used as a singleton via the App class.
/// </summary>
public class BugReportService
{
    // Use a single, static HttpClient instance for the application's lifetime
    // to prevent socket exhaustion and improve performance.
    private static readonly HttpClient HttpClient = new();

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;

    public BugReportService(string apiUrl, string apiKey, string applicationName)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
    }

    /// <summary>
    /// Silently sends a bug report to the API.
    /// </summary>
    /// <param name="message">The error message or bug report.</param>
    public async Task SendBugReportAsync(string message)
    {
        try
        {
            // Create the request payload
            var payload = new
            {
                message,
                applicationName = _applicationName
            };

            // Create a new HttpRequestMessage for each call. This is thread-safe and ensures
            // headers from one request do not interfere with another.
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-API-KEY", _apiKey);

            // Send the request using the static HttpClient
            await HttpClient.SendAsync(request);
        }
        catch
        {
            // Silently fail if there's an exception
        }
    }
}