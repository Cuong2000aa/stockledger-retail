using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.EntityFrameworkCore;

namespace StockLedgerRetail.HttpApi.Host.Controllers;

/// <summary>Liveness/readiness probes for load balancers and rollout checks.</summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public HealthController(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Process is up — no dependency checks.</summary>
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    /// <summary>Ready to serve traffic — verifies database connectivity.</summary>
    [HttpGet("ready")]
    public async Task<IActionResult> ReadyAsync(CancellationToken cancellationToken)
    {
        var databaseOk = await _dbContext.Database.CanConnectAsync(cancellationToken);
        if (!databaseOk)
        {
            return StatusCode(503, new { status = "unhealthy", database = false, timestamp = DateTime.UtcNow });
        }

        return Ok(new { status = "ready", database = true, timestamp = DateTime.UtcNow });
    }
}
