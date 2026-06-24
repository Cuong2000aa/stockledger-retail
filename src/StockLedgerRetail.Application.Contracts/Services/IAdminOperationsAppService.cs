namespace StockLedgerRetail.Services;

using StockLedgerRetail.Operations;

public interface IAdminOperationsAppService
{
    Task<OperationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<List<BackgroundJobRunDto>> GetJobHistoryAsync(
        string jobKey,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<BackgroundJobDto> UpdateJobAsync(
        string jobKey,
        UpdateBackgroundJobDto input,
        CancellationToken cancellationToken = default);

    Task<TriggerBackgroundJobResponseDto> TriggerJobAsync(
        string jobKey,
        CancellationToken cancellationToken = default);
}
