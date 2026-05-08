using Microsoft.EntityFrameworkCore;
using ShortTree.Data;

namespace ShortTree.Services
{
    public sealed class ClickLogBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ClickLogChannel _channel;
        private readonly ILogger<ClickLogBackgroundService> _logger;

        public ClickLogBackgroundService(
            IServiceProvider serviceProvider,
            ClickLogChannel channel,
            ILogger<ClickLogBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _channel = channel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var entry in _channel.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var clickLog = new Models.ClickLog
                    {
                        LinkId = entry.LinkId,
                        Timestamp = entry.Timestamp,
                        IpAddress = entry.IpAddress,
                        UserAgent = entry.UserAgent,
                        Referer = entry.Referer
                    };

                    db.ClickLogs.Add(clickLog);

                    var link = await db.Links.FirstOrDefaultAsync(l => l.Id == entry.LinkId, stoppingToken);
                    if (link != null)
                    {
                        link.ClickCount += 1;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist click log.");
                }
            }
        }
    }
}
