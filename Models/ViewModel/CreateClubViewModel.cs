namespace ClubActivitiesSystem.Models.ViewModel
{
    using System.ComponentModel.DataAnnotations;

    public class CreateClubViewModel
    {
        [Required(ErrorMessage = "請輸入社團名稱")]
        [StringLength(100, ErrorMessage = "社團名稱不可超過 100 字")]
        public string ClubName { get; set; } = default!;

        [StringLength(500, ErrorMessage = "社團描述不可超過 500 字")]
        public string? Description { get; set; }
    }
}
