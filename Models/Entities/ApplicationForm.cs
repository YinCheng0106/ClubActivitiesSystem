using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    // 社團加入申請表單
     [Table("application_forms")]
    public class ApplicationForm
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; } = default!;

        [Column("club_id")]
        public int ClubId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

}
