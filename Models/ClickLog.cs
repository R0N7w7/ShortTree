using System.ComponentModel.DataAnnotations;

namespace ShortTree.Models
{
    public record ClickLog
    {
        [Key]
        public long Id { get; set; }
        public int LinkId { get; set; }
        public Link? Link { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Referer { get; set; }
    }
}
