using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("feedbacks")]
    public class Feedback
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("event_id")]
        public int EventId { get; set; }

        public Event Event { get; set; } = default!;

        [Column("user_id")]
        public string UserId { get; set; } = default!;

        public User User { get; set; } = default!;

        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

}
