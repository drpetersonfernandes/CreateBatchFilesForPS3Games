using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;

namespace CreateBatchFilesForPS3Games;

public class BugReportService : IBugReportService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    static BugReportService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "CreateBatchFilesForPS3Games/1.0");
    }

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;

    public BugReportService(string apiUrl, string apiKey, string applicationName)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
    }

    public async Task SendBugReportAsync(string message, string? version = null, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullMessage = BuildReportMessage(message, version, exception);
            var stackTrace = BuildStackTrace(exception);
            var environment = BuildEnvironmentField();
            var userInfo = BuildUserInfoField();

            var versionField = version is { Length: > 20 } ? version[..20] : version;

            var payload = new
            {
                message = Truncate(fullMessage, 4000),
                applicationName = _applicationName,
                version = versionField,
                stackTrace = Truncate(stackTrace, 8000),
                environment = Truncate(environment, 50),
                userInfo = Truncate(userInfo, 100)
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-API-KEY", _apiKey);

            await HttpClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            // Silently fail
        }
    }

    private string BuildReportMessage(string message, string? version, Exception? exception)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Environment Details ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {_applicationName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {version ?? "N/A"}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {RuntimeInformation.OSDescription}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {Path.GetTempPath()}");

        sb.AppendLine();
        sb.AppendLine("=== Error Details ===");
        sb.AppendLine(message);

        if (exception != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== Exception Details ===");
            AppendExceptionDetails(sb, exception);
        }

        return sb.ToString();
    }

    private static void AppendExceptionDetails(StringBuilder sb, Exception exception)
    {
        var level = 0;
        var currentEx = exception;
        while (currentEx != null)
        {
            var indent = level > 0 ? new string(' ', level * 2) : "";
            if (level > 0)
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}--- Inner Exception ---");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {currentEx.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {currentEx.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Source: {currentEx.Source}");
            currentEx = currentEx.InnerException;
            level++;
        }
    }

    private static string? BuildStackTrace(Exception? exception)
    {
        if (exception == null) return null;

        var sb = new StringBuilder();
        var currentEx = exception;
        var level = 0;
        while (currentEx != null)
        {
            if (level > 0)
                sb.AppendLine("--- Inner Exception ---");
            sb.AppendLine(currentEx.StackTrace);
            currentEx = currentEx.InnerException;
            level++;
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string BuildEnvironmentField()
    {
        return $"{RuntimeInformation.OSDescription} {RuntimeInformation.ProcessArchitecture}";
    }

    private static string BuildUserInfoField()
    {
        return $"Procs:{Environment.ProcessorCount} Bitness:{(Environment.Is64BitProcess ? 64 : 32)}";
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is not { Length: > 0 }) return value;

        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
