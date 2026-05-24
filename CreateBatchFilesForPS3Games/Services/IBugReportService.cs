namespace CreateBatchFilesForPS3Games.Services;

public interface IBugReportService
{
    Task SendBugReportAsync(string message, string? version = null, Exception? exception = null, CancellationToken cancellationToken = default);
}
