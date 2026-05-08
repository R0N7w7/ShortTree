using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortTree.Data;

namespace ShortTree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class StatsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IReadOnlyList<UserStatsResponse>>> GetUserStats()
        {
            var users = await _db.Users
                .OrderBy(u => u.Username)
                .Select(u => new UserStatsResponse(
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Links.Count,
                    u.Links.Sum(l => (int?)l.ClickCount) ?? 0,
                    u.Links.Max(l => (DateTime?)l.CreatedAt)))
                .ToListAsync();

            return users;
        }

        [HttpGet("users/{username}")]
        public async Task<ActionResult<UserStatsResponse>> GetUserStatsByUsername(string username)
        {
            var user = await _db.Users
                .Where(u => u.Username == username)
                .Select(u => new UserStatsResponse(
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Links.Count,
                    u.Links.Sum(l => (int?)l.ClickCount) ?? 0,
                    u.Links.Max(l => (DateTime?)l.CreatedAt)))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpGet("clicks")]
        public async Task<ActionResult<IReadOnlyList<ClickLogResponse>>> GetClickLogs(
            [FromQuery] string? username,
            [FromQuery] string? slug,
            [FromQuery] int take = 100)
        {
            if (take <= 0)
            {
                return BadRequest("take must be greater than zero.");
            }

            var query = _db.ClickLogs
                .Include(c => c.Link)
                .ThenInclude(l => l!.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(c => c.Link != null && c.Link.User != null && c.Link.User.Username == username);
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                query = query.Where(c => c.Link != null && c.Link.Slug == slug);
            }

            var logs = await query
                .OrderByDescending(c => c.Timestamp)
                .Take(take)
                .Select(c => new ClickLogResponse(
                    c.Id,
                    c.Timestamp,
                    c.IpAddress,
                    c.UserAgent,
                    c.Referer,
                    c.Link != null ? c.Link.Id : 0,
                    c.Link != null ? c.Link.Slug : string.Empty,
                    c.Link != null && c.Link.User != null ? c.Link.User.Username : string.Empty))
                .ToListAsync();

            return logs;
        }
    }

    public sealed record UserStatsResponse(
        int Id,
        string Username,
        string Email,
        int LinkCount,
        int TotalClicks,
        DateTime? LastLinkCreatedAt
    );

    public sealed record ClickLogResponse(
        long Id,
        DateTime Timestamp,
        string? IpAddress,
        string? UserAgent,
        string? Referer,
        int LinkId,
        string LinkSlug,
        string Username
    );
}
