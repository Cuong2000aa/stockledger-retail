using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StockLedgerRetail.EntityFrameworkCore;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StockLedgerRetailDbContext>
{
    public StockLedgerRetailDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "host", "StockLedgerRetail.HttpApi.Host");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<StockLedgerRetailDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new StockLedgerRetailDbContext(optionsBuilder.Options);
    }
}
