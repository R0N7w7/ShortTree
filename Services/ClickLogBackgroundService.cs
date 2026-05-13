using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ShortTree.Data;
using ShortTree.Hubs;

namespace ShortTree.Services
{
    public sealed class ClickLogBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ClickLogChannel _channel;
        private readonly IHubContext<ClickHub> _hub;
        private readonly ILogger<ClickLogBackgroundService> _logger;

        public ClickLogBackgroundService(
            IServiceProvider serviceProvider,
            ClickLogChannel channel,
            IHubContext<ClickHub> hub,
            ILogger<ClickLogBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _channel = channel;
            _hub = hub;
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

                    var link = await db.Links
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.Id == entry.LinkId, stoppingToken);
                    if (link != null)
                    {
                        link.ClickCount += 1;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    if (link?.User?.Username != null)
                    {
                        var clickEvent = new ClickEvent(
                            link.Id,
                            link.User.Username,
                            link.Slug,
                            link.ClickCount,
                            entry.Timestamp);

                        await _hub.Clients.Group(GroupNames.User(link.User.Username))
                            .SendAsync("click", clickEvent, stoppingToken);

                        await _hub.Clients.Group(GroupNames.Link(link.User.Username, link.Slug))
                            .SendAsync("click", clickEvent, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist click log.");
                }
            }
        }
    }
}
