namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubMemberViewModel
    {
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime JoinDate { get; set; }
    }
}
