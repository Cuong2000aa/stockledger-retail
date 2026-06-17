using Serilog;
using StockLedgerRetail;
using StockLedgerRetail.EntityFrameworkCore;
using StockLedgerRetail.HttpApi.Host.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: "logs/api-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(StockLedgerRetail.Controllers.ProductsController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "StockLedger Retail API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddStockLedgerRetailEntityFrameworkCore(connectionString);
builder.Services.AddStockLedgerRetailApplication(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IntegrationApiKeyMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StockLedger Retail API v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

try
{
    Log.Information("Starting StockLedger Retail API host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "StockLedger Retail API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
