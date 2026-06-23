using Serilog;
using StockLedgerRetail;
using StockLedgerRetail.Audit;
using StockLedgerRetail.EntityFrameworkCore;
using StockLedgerRetail.HttpApi.Host.Audit;
using StockLedgerRetail.HttpApi.Host.HostedServices;
using StockLedgerRetail.HttpApi.Host.Middleware;
using StockLedgerRetail.Inventory;

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditContext, HttpAuditContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "StockLedger Retail API", Version = "v1" });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddStockLedgerRetailEntityFrameworkCore(connectionString);
builder.Services.AddStockLedgerRetailApplication(builder.Configuration);
builder.Services.Configure<StockReconciliationOptions>(
    builder.Configuration.GetSection(StockReconciliationOptions.SectionName));
builder.Services.AddHostedService<StockReconciliationHostedService>();
builder.Services.AddHostedService<AuthorizationBootstrapHostedService>();

var app = builder.Build();

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<UserEmailAuthMiddleware>();
app.UseMiddleware<BrandScopeMiddleware>();
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
