using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ShortTree.Models
{
    [Index(nameof(UserId), nameof(Slug), IsUnique = true)]
    public record Link
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string LongUrl { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Slug { get; set; } = string.Empty;
        public bool VisibleInProfile { get; set; } = false;
        public int ClickCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UserId { get; set; }
        public User? User { get; set; }
        public ICollection<ClickLog> Clicks { get; set; } = new List<ClickLog>();
    }
}
