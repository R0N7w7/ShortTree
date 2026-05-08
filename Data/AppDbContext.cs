using Microsoft.EntityFrameworkCore;
using ShortTree.Models;

namespace ShortTree.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Link> Links => Set<Link>();
        public DbSet<ClickLog> ClickLogs => Set<ClickLog>();
    }
}
