using MatchikoMap.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchikoMap.Services.MatchmakingService
{
    public class MatchmakingBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<MatchmakingBackgroundWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("MatchmakingExpirationWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var expiredEntries = await _db.MatchmakingEntries
                        .Where(x => x.ExpiringAt <= DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    _db.MatchmakingEntries.RemoveRange(expiredEntries);

                    await _db.SaveChangesAsync(stoppingToken);               
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error while expiring matchmaking entries.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    stoppingToken);
            }
        }
    }
}
