using System.ComponentModel.DataAnnotations;

namespace ClubActivitiesSystem.Models.ViewModel
{

    public class JoinClubApplyViewModel
    {
        [Required]
        public int ClubId { get; set; }

        public string ClubName { get; set; } = default!;

        [Display(Name = "申請留言")]
        [StringLength(500)]
        public string? Message { get; set; }
    }

}
