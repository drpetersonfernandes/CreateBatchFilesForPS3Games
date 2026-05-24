using CreateBatchFilesForPS3Games.Services;

namespace CreateBatchFilesForPS3Games.Tests;

public class BugReportServiceTests
{
    private const string ApiUrl = "https://example.com/api";
    private const string ApiKey = "test-api-key";
    private const string AppName = "TestApp";

    [Fact]
    public void Constructor_SetsProperties()
    {
        var service = new BugReportService(ApiUrl, ApiKey, AppName);
        Assert.NotNull(service);
    }

    [Fact]
    public Task SendBugReportAsync_WithInvalidHost_DoesNotThrow()
    {
        var service = new BugReportService("http://invalid-url-that-does-not-exist.test", ApiKey, AppName);

        return service.SendBugReportAsync("test message");
    }

    [Fact]
    public Task SendBugReportAsync_WithInvalidUrl_DoesNotThrow()
    {
        var service = new BugReportService("not-a-valid-url", ApiKey, AppName);

        return service.SendBugReportAsync("test message");
    }

    [Fact]
    public Task SendBugReportAsync_WithEmptyMessage_DoesNotThrow()
    {
        var service = new BugReportService("http://localhost:1", ApiKey, AppName);

        return service.SendBugReportAsync("");
    }

    [Fact]
    public Task SendBugReportAsync_WithNullMessage_DoesNotThrow()
    {
        var service = new BugReportService("http://localhost:1", ApiKey, AppName);

        return service.SendBugReportAsync(null!);
    }

    [Fact]
    public Task SendBugReportAsync_WithVersion_DoesNotThrow()
    {
        var service = new BugReportService("http://localhost:1", ApiKey, AppName);

        return service.SendBugReportAsync("test", "1.0.0.0");
    }

    [Fact]
    public Task SendBugReportAsync_WithException_DoesNotThrow()
    {
        var service = new BugReportService("http://localhost:1", ApiKey, AppName);
        var exception = new InvalidOperationException("Test exception");

        return service.SendBugReportAsync("test", null, exception);
    }

    [Fact]
    public Task SendBugReportAsync_WithVeryLongMessage_DoesNotThrow()
    {
        var service = new BugReportService("http://localhost:1", ApiKey, AppName);
        var longMessage = new string('A', 10000);

        return service.SendBugReportAsync(longMessage);
    }

    [Fact]
    public void MultipleInstances_CanBeCreated()
    {
        var service1 = new BugReportService("http://url1", "key1", "app1");
        var service2 = new BugReportService("http://url2", "key2", "app2");

        Assert.NotNull(service1);
        Assert.NotNull(service2);
    }
}
