using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MatchikoMap.Services.EmailService
{
    public class EmailBackgroundWorker(
        IBackgroundEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundWorker> logger) : BackgroundService
    {
        private readonly IBackgroundEmailQueue _queue = queue;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<EmailBackgroundWorker> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (to, subject, body) =
                    await _queue.DequeueAsync(stoppingToken);

                int retryCount = 0;
                const int maxRetries = 3;

                while (true)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var emailService =
                            scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await emailService.SendEmailAsync(to, subject, body);

                        await Task.Delay(300, stoppingToken);
                        break;
                    }
                    catch (Exception)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)break;
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                        await Task.Delay(delay, stoppingToken);
                    }
                }
            }
        }
    }
}