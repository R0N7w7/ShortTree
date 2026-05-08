using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShortTree.Data;
using ShortTree.Services;

namespace ShortTree.Controllers
{
    [ApiController]
    [Route("r")]
    public sealed class RedirectController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ClickLogChannel _channel;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(10);

        public RedirectController(
            AppDbContext db,
            ClickLogChannel channel,
            IMemoryCache cache)
        {
            _db = db;
            _channel = channel;
            _cache = cache;
        }

        [HttpGet("{username}/{slug}")]
        public async Task<IActionResult> RedirectToLongUrl(string username, string slug)
        {
            var cacheKey = $"link:{username}:{slug}";
            if (!_cache.TryGetValue(cacheKey, out CachedLink? cached))
            {
                cached = await _db.Links
                    .Include(l => l.User)
                    .Where(l => l.User != null && l.User.Username == username && l.Slug == slug)
                    .Select(l => new CachedLink(l.Id, l.LongUrl))
                    .FirstOrDefaultAsync();

                if (cached == null)
                {
                    return NotFound();
                }

                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = CacheSlidingExpiration
                });
            }

            var entry = new ClickLogEntry(
                cached.Id,
                DateTime.UtcNow,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                Request.Headers.Referer.ToString());

            _channel.Channel.Writer.TryWrite(entry);

            return Redirect(cached.LongUrl);
        }
    }

    public sealed record CachedLink(int Id, string LongUrl);
}
