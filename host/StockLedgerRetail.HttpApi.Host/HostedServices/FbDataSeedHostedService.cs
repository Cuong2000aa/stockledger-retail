using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Seed;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class FbDataSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FbDataSeedHostedService> _logger;
    private readonly FbDataSeedOptions _options;

    public FbDataSeedHostedService(
        IServiceProvider serviceProvider,
        ILogger<FbDataSeedHostedService> logger,
        IOptions<FbDataSeedOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var seed = scope.ServiceProvider.GetRequiredService<IFbDataSeedService>();
            await seed.EnsureSeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "F&B data seed failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
