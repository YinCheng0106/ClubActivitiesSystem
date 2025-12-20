using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("events")]
    public class Event
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("club_id")]
        public int ClubId { get; set; }

        public Club Club { get; set; } = default!;

        [Column("title")]
        public string Title { get; set; } = default!;

        [Column("description")]
        public string? Description { get; set; }

        [Column("location")]
        public string? Location { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime EndTime { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Draft";

        [Column("created_by")]
        public string CreatedBy { get; set; } = default!;

        public User CreatedByUser { get; set; } = default!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

}
