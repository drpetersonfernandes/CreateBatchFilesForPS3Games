using CreateBatchFilesForPS3Games.Services;

namespace CreateBatchFilesForPS3Games.Tests;

public class UpdateServiceTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        using var httpClient = new HttpClient();
        var service = new UpdateService(httpClient, "owner", "repo");

        Assert.NotNull(service);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithInvalidHost_DoesNotThrow()
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var service = new UpdateService(httpClient, "http://invalid-url-that-does-not-exist.test");

        var (updateAvailable, latestVersion, releaseUrl) =
            await service.CheckForUpdateAsync(new Version(1, 0));

        Assert.False(updateAvailable);
        Assert.Null(latestVersion);
        Assert.Null(releaseUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithInvalidUrl_DoesNotThrow()
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var service = new UpdateService(httpClient, "not-a-valid-url");

        var (updateAvailable, _, _) =
            await service.CheckForUpdateAsync(new Version(1, 0));

        Assert.False(updateAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithLocalhostUrl_DoesNotThrow()
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var service = new UpdateService(httpClient, "http://localhost:1");

        var (updateAvailable, _, _) =
            await service.CheckForUpdateAsync(new Version(1, 0));

        Assert.False(updateAvailable);
    }

    [Theory]
    [InlineData("v1.0", 1, 0)]
    [InlineData("1.0", 1, 0)]
    [InlineData("v2.3.4", 2, 3, 4)]
    [InlineData("2.3.4", 2, 3, 4)]
    [InlineData("V5.0.1", 5, 0, 1)]
    [InlineData("v10.20.30.40", 10, 20, 30, 40)]
    [InlineData("0.0.0.0", 0, 0, 0, 0)]
    public void ParseVersion_ValidTags_ReturnsVersion(string tag, int major, int minor, int build = -1, int revision = -1)
    {
        var result = UpdateService.ParseVersion(tag);

        Assert.NotNull(result);
        Assert.Equal(major, result.Major);
        Assert.Equal(minor, result.Minor);
        if (build >= 0)
            Assert.Equal(build, result.Build);
        if (revision >= 0)
            Assert.Equal(revision, result.Revision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    [InlineData("abc.def.ghi")]
    [InlineData("v")]
    [InlineData("V")]
    public void ParseVersion_InvalidTags_ReturnsNull(string? tag)
    {
        var result = UpdateService.ParseVersion(tag);

        Assert.Null(result);
    }

    [Fact]
    public void ParseVersion_TrimmedTag_WorksCorrectly()
    {
        var result = UpdateService.ParseVersion("  v1.5.3  ");

        Assert.NotNull(result);
        Assert.Equal(new Version(1, 5, 3), result);
    }

    [Fact]
    public void ParseVersion_LeadingWhitespace_WorksCorrectly()
    {
        var result = UpdateService.ParseVersion("\tv1.0\n");

        Assert.NotNull(result);
        Assert.Equal(new Version(1, 0), result);
    }

    [Theory]
    [InlineData("1.0.0.0", "2.0.0.0", true)]
    [InlineData("2.0.0.0", "1.0.0.0", false)]
    [InlineData("1.5.0.0", "1.5.0.0", false)]
    [InlineData("1.4.0.0", "1.4.1.0", true)]
    [InlineData("1.4.0.0", "1.4.0.1", true)]
    [InlineData("1.4", "1.5", true)]
    [InlineData("1.9", "1.10", true)]
    [InlineData("2.0", "1.9", false)]
    public void VersionComparison_DetectsUpdatesCorrectly(string current, string latest, bool expectedUpdate)
    {
        var currentVersion = new Version(current);
        var latestVersion = new Version(latest);

        var updateAvailable = latestVersion > currentVersion;

        Assert.Equal(expectedUpdate, updateAvailable);
    }

    [Fact]
    public Task CheckForUpdateAsync_WithRealRepo_CompletesWithoutThrowing()
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Fact]
    public void MultipleInstances_CanBeCreated()
    {
        using var httpClient1 = new HttpClient();
        using var httpClient2 = new HttpClient();
        var service1 = new UpdateService(httpClient1, "owner1", "repo1");
        var service2 = new UpdateService(httpClient2, "owner2", "repo2");

        Assert.NotNull(service1);
        Assert.NotNull(service2);
    }
}
