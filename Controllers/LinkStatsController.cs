using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortTree.Data;

namespace ShortTree.Controllers
{
    [ApiController]
    [Route("api/link-stats")]
    public sealed class LinkStatsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LinkStatsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<IReadOnlyList<LinkClickStatsResponse>>> GetByUser(string username)
        {
            var links = await _db.Links
                .Where(l => l.User != null && l.User.Username == username)
                .OrderBy(l => l.Slug)
                .Select(l => new LinkClickStatsResponse(
                    l.Id,
                    l.Slug,
                    l.Title,
                    l.LongUrl,
                    l.Clicks.Count,
                    l.Clicks.Max(c => (DateTime?)c.Timestamp)))
                .ToListAsync();

            if (links.Count == 0)
            {
                return NotFound();
            }

            return links;
        }

        [HttpGet("{username}/{slug}")]
        public async Task<ActionResult<LinkClickStatsResponse>> GetBySlug(string username, string slug)
        {
            var link = await _db.Links
                .Where(l => l.User != null && l.User.Username == username && l.Slug == slug)
                .Select(l => new LinkClickStatsResponse(
                    l.Id,
                    l.Slug,
                    l.Title,
                    l.LongUrl,
                    l.Clicks.Count,
                    l.Clicks.Max(c => (DateTime?)c.Timestamp)))
                .FirstOrDefaultAsync();

            if (link == null)
            {
                return NotFound();
            }

            return link;
        }
    }

    public sealed record LinkClickStatsResponse(
        int LinkId,
        string Slug,
        string Title,
        string LongUrl,
        int ClickCount,
        DateTime? LastClickAt
    );
}
