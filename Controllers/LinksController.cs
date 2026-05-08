using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortTree.Data;
using ShortTree.Models;

namespace ShortTree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class LinksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LinksController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult<LinkResponse>> Create([FromBody] CreateLinkRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.LongUrl) ||
                string.IsNullOrWhiteSpace(request.Slug))
            {
                return BadRequest("Required fields are missing.");
            }

            if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var longUri))
            {
                return BadRequest("LongUrl must be a valid absolute URL.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                user = new User
                {
                    Username = request.Username,
                    Email = request.Email ?? $"{request.Username}@local",
                    PasswordHash = string.Empty
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            var existing = await _db.Links.FirstOrDefaultAsync(l => l.UserId == user.Id && l.Slug == request.Slug);
            if (existing != null)
            {
                return Conflict("Slug already exists for this user.");
            }

            var link = new Link
            {
                Title = request.Title,
                LongUrl = longUri.ToString(),
                Slug = request.Slug,
                VisibleInProfile = request.VisibleInProfile,
                UserId = user.Id
            };

            _db.Links.Add(link);
            await _db.SaveChangesAsync();

            var shortUrl = $"{Request.Scheme}://{Request.Host}/r/{request.Username}/{link.Slug}";

            return CreatedAtAction(nameof(GetBySlug), new { username = request.Username, slug = link.Slug },
                new LinkResponse(link.Id, link.Title, link.LongUrl, link.Slug, shortUrl, link.ClickCount));
        }

        [HttpGet("{username}/{slug}")]
        public async Task<ActionResult<LinkResponse>> GetBySlug(string username, string slug)
        {
            var link = await _db.Links
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.User != null && l.User.Username == username && l.Slug == slug);

            if (link == null)
            {
                return NotFound();
            }

            var shortUrl = $"{Request.Scheme}://{Request.Host}/r/{username}/{link.Slug}";

            return new LinkResponse(link.Id, link.Title, link.LongUrl, link.Slug, shortUrl, link.ClickCount);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<IReadOnlyList<LinkResponse>>> GetByUser(string username)
        {
            var links = await _db.Links
                .Include(l => l.User)
                .Where(l => l.User != null && l.User.Username == username)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var response = links.Select(l =>
            {
                var shortUrl = $"{Request.Scheme}://{Request.Host}/r/{username}/{l.Slug}";
                return new LinkResponse(l.Id, l.Title, l.LongUrl, l.Slug, shortUrl, l.ClickCount);
            }).ToList();

            return response;
        }
    }

    public sealed record CreateLinkRequest(
        string Username,
        string Title,
        string LongUrl,
        string Slug,
        bool VisibleInProfile,
        string? Email
    );

    public sealed record LinkResponse(
        int Id,
        string Title,
        string LongUrl,
        string Slug,
        string ShortUrl,
        int ClickCount
    );
}
