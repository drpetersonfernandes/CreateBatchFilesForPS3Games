namespace CreateBatchFilesForPS3Games;

public interface IStatsService
{
    Task SendStatsAsync(CancellationToken cancellationToken = default);
}
