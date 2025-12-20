using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("event_sessions")]
    public class EventSession
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("event_id")]
        public int EventId { get; set; }

        public Event Event { get; set; } = default!;

        [Column("title")]
        public string Title { get; set; } = default!;

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime EndTime { get; set; }

        [Column("location")]
        public string? Location { get; set; }
    }
}
