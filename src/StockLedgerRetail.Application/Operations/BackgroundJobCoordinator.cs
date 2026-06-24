using System.Collections.Concurrent;

namespace StockLedgerRetail.Application.Operations;

public interface IBackgroundJobCoordinator
{
    void RequestManualRun(string jobKey);

    bool TryConsumeManualRun(string jobKey);

    bool IsManualRunPending(string jobKey);

    void MarkRunning(string jobKey);

    void MarkIdle(string jobKey);

    bool IsRunning(string jobKey);
}

public class BackgroundJobCoordinator : IBackgroundJobCoordinator
{
    private readonly ConcurrentDictionary<string, byte> _manualRuns = new();
    private readonly ConcurrentDictionary<string, byte> _running = new();

    public void RequestManualRun(string jobKey) => _manualRuns[jobKey] = 0;

    public bool TryConsumeManualRun(string jobKey) => _manualRuns.TryRemove(jobKey, out _);

    public bool IsManualRunPending(string jobKey) => _manualRuns.ContainsKey(jobKey);

    public void MarkRunning(string jobKey) => _running[jobKey] = 0;

    public void MarkIdle(string jobKey) => _running.TryRemove(jobKey, out _);

    public bool IsRunning(string jobKey) => _running.ContainsKey(jobKey);
}
