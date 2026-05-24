namespace CreateBatchFilesForPS3Games.Services;

public interface IUpdateService
{
    Task<(bool UpdateAvailable, string? LatestVersion, string? ReleaseUrl)> CheckForUpdateAsync(Version currentVersion);
}
