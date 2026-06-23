using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class AuthorizationBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthorizationBootstrapHostedService> _logger;
    private readonly string? _bootstrapAdminEmail;

    public AuthorizationBootstrapHostedService(
        IServiceProvider serviceProvider,
        ILogger<AuthorizationBootstrapHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapAdminEmail = configuration["Auth:BootstrapAdminEmail"];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var groupRepository = scope.ServiceProvider.GetRequiredService<IPermissionGroupRepository>();

        await permissionRepository.EnsureSeedAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_bootstrapAdminEmail))
        {
            _logger.LogInformation("Auth bootstrap admin email not configured.");
            return;
        }

        var email = _bootstrapAdminEmail.Trim().ToLowerInvariant();
        var existing = await appUserRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var adminGroup = await groupRepository.GetByCodeAsync(PermissionGroupCodes.SystemAdmin, cancellationToken);
        if (adminGroup is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = email,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await appUserRepository.InsertAsync(user, cancellationToken);
        await groupRepository.AssignUserToGroupAsync(user.Id, adminGroup.Id, cancellationToken);
        await appUserRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bootstrap admin user created for {Email}", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
