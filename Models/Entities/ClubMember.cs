using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("club_members")]
    public class ClubMember
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("club_id")]
        public int ClubId { get; set; }

        public Club Club { get; set; } = default!;

        [Column("user_id")]
        public string UserId { get; set; } = default!;

        public User User { get; set; } = default!;

        [Column("role")]
        public string Role { get; set; } = "Member";

        [Column("join_date")]
        public DateTime JoinDate { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; }
    }

}
