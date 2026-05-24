namespace CreateBatchFilesForPS3Games.Services;

public interface IStatsService
{
    Task SendStatsAsync(CancellationToken cancellationToken = default);
}
