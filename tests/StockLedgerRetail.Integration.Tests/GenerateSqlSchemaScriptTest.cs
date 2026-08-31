using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.EntityFrameworkCore;
using Xunit;

namespace StockLedgerRetail.Integration.Tests;

public class GenerateSqlSchemaScriptTest
{
    [Fact]
    public void GenerateFullDatabaseScript()
    {
        var optionsBuilder = new DbContextOptionsBuilder<StockLedgerRetailDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=dummy");

        using var dbContext = new StockLedgerRetailDbContext(optionsBuilder.Options);
        var script = dbContext.Database.GenerateCreateScript();

        var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../database"));
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "schema.sql");
        File.WriteAllText(outputPath, script);

        Assert.NotEmpty(script);
    }
}
