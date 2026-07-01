using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Identity;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class AuthorizationBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthorizationBootstrapHostedService> _logger;
    private readonly string? _bootstrapAdminEmail;
    private readonly string? _bootstrapAdminPassword;

    public AuthorizationBootstrapHostedService(
        IServiceProvider serviceProvider,
        ILogger<AuthorizationBootstrapHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapAdminEmail = configuration["Auth:BootstrapAdminEmail"];
        _bootstrapAdminPassword = configuration["Auth:BootstrapAdminPassword"];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var groupRepository = scope.ServiceProvider.GetRequiredService<IPermissionGroupRepository>();

        await permissionRepository.EnsureSeedAsync(cancellationToken);
        await permissionRepository.EnsureMissingPermissionsAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_bootstrapAdminEmail))
        {
            _logger.LogInformation("Auth bootstrap admin email not configured.");
            return;
        }

        var email = _bootstrapAdminEmail.Trim().ToLowerInvariant();
        var existing = await appUserRepository.GetByEmailAsync(email, cancellationToken);
        var adminGroup = await groupRepository.GetByCodeAsync(PermissionGroupCodes.SystemAdmin, cancellationToken);
        if (adminGroup is null)
        {
            return;
        }

        var bootstrapPassword = string.IsNullOrWhiteSpace(_bootstrapAdminPassword)
            ? "1234"
            : _bootstrapAdminPassword;

        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.PasswordHash))
            {
                existing.PasswordHash = UserPasswordHasher.Hash(bootstrapPassword);
                existing.UpdatedAt = DateTime.UtcNow;
                await appUserRepository.UpdateAsync(existing, cancellationToken);
                await appUserRepository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Bootstrap admin password set for {Email}", email);
            }

            return;
        }

        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = email,
            PasswordHash = UserPasswordHasher.Hash(bootstrapPassword),
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
