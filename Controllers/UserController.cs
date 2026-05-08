using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortTree.Data;
using ShortTree.Models;

namespace ShortTree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class UserController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Username and Email are required.");
            }

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existing != null)
            {
                return Conflict("Username already exists.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = string.Empty
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByUsername), new { username = user.Username },
                new UserResponse(user.Id, user.Username, user.Email, 0));
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<UserResponse>> GetByUsername(string username)
        {
            var user = await _db.Users
                .Include(u => u.Links)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            return new UserResponse(user.Id, user.Username, user.Email, user.Links.Count);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll()
        {
            var users = await _db.Users
                .Include(u => u.Links)
                .OrderBy(u => u.Username)
                .ToListAsync();

            var response = users.Select(u => new UserResponse(u.Id, u.Username, u.Email, u.Links.Count)).ToList();
            return response;
        }
    }

    public sealed record CreateUserRequest(string Username, string Email);

    public sealed record UserResponse(int Id, string Username, string Email, int LinkCount);
}
