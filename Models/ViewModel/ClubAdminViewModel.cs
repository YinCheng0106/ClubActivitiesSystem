namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubAdminViewModel
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = default!;

        public List<ClubMemberViewModel> Members { get; set; } = new();
        public List<ClubApplicationViewModel> PendingApplications { get; set; } = new();
    }

}
