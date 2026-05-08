using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ShortTree.Models
{
    [Index(nameof(Username), IsUnique = true)]
    public record User
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(30)]
        public string Username { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public ICollection<Link> Links { get; set; } = new List<Link>();
    }
}
